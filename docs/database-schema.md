# Adatbázis séma — Family OS

> Státusz: v0.3 · Dátum: 2026-07-02 · Nyelv: magyar
> Kapcsolódó: [domain-model.md](domain-model.md), [architecture.md](architecture.md)
> Rögzített döntések: [ADR-0001 pgvector](decisions/ADR-0001-vektor-tarolas-pgvector.md),
> [ADR-0002 Tesseract](decisions/ADR-0002-ocr-tesseract.md)

## Változások a v0.1 óta

A `reminder-engine.md` és `security-privacy.md` doksik során felmerült
pótlások itt aktívvá válnak:

1. **`reminder_status` enum** — új `'Cancelled'` érték: explicit user-akció,
   szétválasztva az automatikus `'Skipped'`-től (lecsúszott / eszkalált).
2. **`audit_action` enum** — új `'ExternalApiCall'` érték: a Gmail API,
   SMTP és egyéb külső szolgáltatás-hívások naplózásához (security-privacy.md 10.1).
3. **Új `notification_feed` tábla** — az InApp értesítések kézbesítési
   naplója (reminder-engine.md 5.1.1).
4. **`document_text.original_content` opcionális mező + `is_manually_edited`
   flag** — a manuális szövegkorrekció előtti eredeti OCR / kinyert állapot
   megőrzése (mvp-backlog.md C4 story). A korrekciónál a backend egy
   tranzakcióban: `original_content := content; content := new_text;
   is_manually_edited := true`. Visszaállításhoz az `original_content`
   még elérhető.

## Változások a v0.2 óta (v0.3 — 2026-07-02, a megvalósult kódhoz igazítva)

1. **`CREATE DATABASE` locale-javítás** — a `LC_COLLATE`/`LC_CTYPE`
   libc-locale-t vár; az ICU-locale külön `ICU_LOCALE` paraméter (1.1).
2. **`timestamptz` defaultok** — `now() AT TIME ZONE 'UTC'` helyett
   mindenhol `now()` (a korábbi forma `timestamp`-et ad vissza, amit a
   szerver időzónája szerint konvertálna vissza — hibaforrás).
3. **Új táblák a megvalósításból** (4.21): `pending_invite` (meghívók +
   login-allowlist bővítés), `revoked_session` (logout utáni cookie-tiltás),
   `saved_search` (mentett keresések, E7).
4. **`user_account` preferencia-oszlopok** (4.2): `email_enabled`,
   `quiet_hours_start`, `quiet_hours_end` — a B3 story a megvalósításban
   oszlopokként került a `user_account`-ra, nem külön táblába.
   (`escalation_opt_out` nem implementált — v2.)
5. **`ai_processing_job` queue-index pontosítás** (4.16): a worker a
   `Queued` ÉS `Failed` sorokat együtt veszi fel
   (`WHERE status IN ('Queued','Failed')`), a retry nem állítja vissza
   `Queued`-ra a státuszt.
6. **Enum-tárolás megjegyzés** (6.): néhány entitásnál (pl.
   `AiProcessingJob.JobType`/`Status`) a megvalósítás `HasConversion<string>`
   + `varchar` tárolást használ a natív pg-enum helyett; új érték
   (pl. `ExtractFacet`) így migráció nélkül bővíthető.

Ez a dokumentum a PostgreSQL fizikai sémáját rögzíti EF Core kontextusban.
A séma a `domain-model.md`-ből származik; az ott bevezetett közös konvenciók
(`Id Guid`, UTC timestampek, soft delete, `RowVersion`) itt fizikai típusokra
fordítódnak.

A séma **kódból generálódik** (EF Core Migrations) — ez a doksi a célállapotot
írja le, nem konkrét migrációs lépéseket. A kézzel kezelt extension/collation/
trigger részeket külön `__InitialSetup` migrációba helyezzük (raw SQL).

---

## 1. Adatbázis és környezet

### 1.1 Adatbázis létrehozása

```sql
CREATE DATABASE family_os
    ENCODING = 'UTF8'
    TEMPLATE   = template0
    LOCALE_PROVIDER = 'icu'
    ICU_LOCALE = 'hu-HU'
    LC_COLLATE = 'C.UTF-8'
    LC_CTYPE   = 'C.UTF-8';
```

Indok: az ICU-alapú magyar collation rendezést és LIKE viselkedést helyesen
kezeli ékezetes szövegekre (`á`, `é`, `ő`, `ű`). Megjegyzés: a
`LC_COLLATE`/`LC_CTYPE` mindig *libc* locale-t vár (az ICU-t az
`ICU_LOCALE` adja) — ICU-nem-létező libc locale megadása hibát dob; a
`C.UTF-8` a `pgvector/pgvector:pg16` image-ben elérhető.

### 1.2 Extensions

```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;        -- gen_random_uuid() / UUID v4 fallback
CREATE EXTENSION IF NOT EXISTS pg_trgm;         -- LIKE/ILIKE GIN indexekhez
CREATE EXTENSION IF NOT EXISTS unaccent;        -- ékezet-független keresés
CREATE EXTENSION IF NOT EXISTS btree_gin;       -- vegyes (jsonb + scalar) GIN
CREATE EXTENSION IF NOT EXISTS vector;          -- pgvector (ADR-0001)
```

UUID v7-et alkalmazás-szinten (C#) generálunk (idő-rendezett — jobb index-lokalitás),
mert a Postgres natív `uuidv7()` még nem mindenhol elérhető a v16-ig. A
`pgcrypto` csak vészforgatókönyvre van.

### 1.3 Full-text search konfiguráció

A beépített `hungarian` szótár alapján egy egyszerű, ékezet-független konfiguráció:

```sql
CREATE TEXT SEARCH CONFIGURATION hungarian_unaccent ( COPY = hungarian );

ALTER TEXT SEARCH CONFIGURATION hungarian_unaccent
    ALTER MAPPING FOR hword, hword_part, word
    WITH unaccent, hungarian_stem;
```

Ezt használja minden `tsvector` oszlop.

### 1.4 Sémák és role-ok

```sql
CREATE SCHEMA app;                  -- minden alkalmazás tábla itt él
CREATE ROLE family_app LOGIN PASSWORD :app_password;
CREATE ROLE family_migrator LOGIN PASSWORD :migrator_password;

GRANT USAGE ON SCHEMA app TO family_app;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA app TO family_app;
-- DELETE csak a soft-delete-mentes táblákra (AuditLog kivételével):
REVOKE DELETE ON app.audit_log FROM family_app;
REVOKE UPDATE ON app.audit_log FROM family_app;

ALTER DEFAULT PRIVILEGES IN SCHEMA app
    GRANT SELECT, INSERT, UPDATE ON TABLES TO family_app;

GRANT ALL ON SCHEMA app TO family_migrator;
```

A backend a `family_app` role-lal csatlakozik (kevesebb jog), a migrációk
külön connection stringgel a `family_migrator` szereppel futnak.

> **Hard delete megjegyzés:** az admin `?hard=true` fizikai törléshez
> (api-design.md 7.9) a `family_app` role célzott `GRANT DELETE`-et kap a
> `document` táblára és a cascade-elt gyerektáblákra (`document_text`,
> `document_chunk`, `document_summary`, join-táblák, facet-táblák) — az
> `audit_log`-ra a REVOKE változatlanul érvényes.

---

## 2. Enum típusok

PostgreSQL natív enumokat használunk (EF Core támogatja `HasPostgresEnum`-mal).
Indok: értelmes hibaüzenetek, kis hely, jó indexelés. Új érték `ALTER TYPE
... ADD VALUE`-val adható hozzá zero-downtime módon.

```sql
CREATE TYPE app.user_role         AS ENUM ('Admin','Adult','Child');
CREATE TYPE app.relation          AS ENUM ('Self','Spouse','Child','Parent','Other');
CREATE TYPE app.source_type       AS ENUM ('Upload','Email','Manual');
CREATE TYPE app.processing_status AS ENUM ('Pending','Extracting','Analyzing','Done','Failed');
CREATE TYPE app.origin            AS ENUM ('Manual','AiSuggested','AiApproved','ImportedEmail','ImportedFile');
CREATE TYPE app.extraction_method AS ENUM ('PdfTextLayer','TesseractOcr','ManualPaste','EmailBody');
CREATE TYPE app.task_status       AS ENUM ('Suggested','Open','InProgress','Done','Cancelled');
CREATE TYPE app.priority          AS ENUM ('Low','Normal','High');
CREATE TYPE app.deadline_status   AS ENUM ('Upcoming','Due','Passed','Resolved','Dismissed');
CREATE TYPE app.deadline_category AS ENUM ('Insurance','Invoice','Inspection','School','Medical','Subscription','Personal','Other');
CREATE TYPE app.notification_channel AS ENUM ('InApp','Email');
CREATE TYPE app.reminder_status   AS ENUM ('Scheduled','Fired','Acknowledged','Skipped','Failed','Cancelled');
CREATE TYPE app.source_kind       AS ENUM ('Upload','GmailAccount','FileWatch');
CREATE TYPE app.ingest_status     AS ENUM ('Pending','Processed','Skipped','Failed');
CREATE TYPE app.ai_job_type       AS ENUM ('ExtractText','DetectLanguage','Summarize','ExtractEntities','ExtractDeadlines','ExtractTasks','Classify','Embed');
CREATE TYPE app.job_target_type   AS ENUM ('Document','Note','EmailMessage');
CREATE TYPE app.job_status        AS ENUM ('Queued','Running','Completed','Failed','Cancelled');
CREATE TYPE app.audit_action      AS ENUM ('Create','Update','Delete','Login','LoginFailed','Approve','Reject','AiCall','FileAccess','PermissionChange','ExternalApiCall');
CREATE TYPE app.medical_record_type AS ENUM ('LabResult','Prescription','Vaccination','Imaging','Diagnosis','AppointmentNote','Other');
CREATE TYPE app.financial_record_type AS ENUM ('Invoice','Receipt','Insurance','Subscription','BankStatement','Contract','Other');
CREATE TYPE app.recurrence_period AS ENUM ('None','Monthly','Quarterly','Yearly');
```

---

## 3. Közös oszlopok és helper függvények

### 3.1 `set_updated_utc` trigger függvény

```sql
CREATE OR REPLACE FUNCTION app.set_updated_utc()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    NEW.updated_utc := now();
    RETURN NEW;
END $$;
```

Minden módosítható táblára `BEFORE UPDATE` trigger.

### 3.2 Soft delete view-k

Minden olyan táblához, amelyiknek van `deleted_utc` oszlopa, generálunk egy
`<tabla>_v` view-t a NULL-szűréssel — a backend repository ezt használja
default lekérdezésre. Az audit/admin felület a fizikai táblát olvassa.

(EF Core oldalon ezt `HasQueryFilter`-rel oldjuk meg; külön view nem
szükséges — itt csak rögzítjük az opciót, ha nyers SQL elemzéshez kell.)

---

## 4. Táblák

A táblákat függőségi sorrendben definiáljuk. Minden tábla `app` sémában van;
a `app.` prefixet a CREATE-ekben írom ki, az indexeknél nem ismétlem.

Konvenciók a következőkben:
- `id uuid PRIMARY KEY` mindenhol.
- `created_utc timestamptz NOT NULL DEFAULT now()`.
- `updated_utc timestamptz NOT NULL DEFAULT now()` + trigger.
- `deleted_utc timestamptz NULL` ott, ahol soft delete van.
- `row_version` xmin alapon (EF Core `IsRowVersion()` → `xmin` system column).
- `created_by_user_account_id` ahol a domain-model jelöli.

### 4.1 family_member

```sql
CREATE TABLE app.family_member (
    id                          uuid PRIMARY KEY,
    display_name                text NOT NULL,
    full_name                   text NULL,
    relation                    app.relation NOT NULL,
    birth_date                  date NULL,
    has_user_account            boolean NOT NULL DEFAULT false,
    notes                       text NULL,
    created_utc                 timestamptz NOT NULL DEFAULT now(),
    updated_utc                 timestamptz NOT NULL DEFAULT now(),
    deleted_utc                 timestamptz NULL,
    CONSTRAINT ck_family_member_display_name_len CHECK (char_length(display_name) BETWEEN 1 AND 100),
    CONSTRAINT ck_family_member_full_name_len    CHECK (full_name IS NULL OR char_length(full_name) <= 200),
    CONSTRAINT ck_family_member_birth_date       CHECK (birth_date IS NULL OR birth_date <= current_date)
);
CREATE INDEX ix_family_member_relation          ON app.family_member(relation) WHERE deleted_utc IS NULL;
CREATE INDEX ix_family_member_has_user_account  ON app.family_member(has_user_account) WHERE deleted_utc IS NULL;
CREATE TRIGGER trg_family_member_set_updated BEFORE UPDATE ON app.family_member
    FOR EACH ROW EXECUTE FUNCTION app.set_updated_utc();
```

### 4.2 user_account

```sql
CREATE TABLE app.user_account (
    id                          uuid PRIMARY KEY,
    family_member_id            uuid NOT NULL UNIQUE REFERENCES app.family_member(id) ON DELETE RESTRICT,
    google_subject              text NOT NULL,
    email                       text NOT NULL,
    display_name                text NOT NULL,
    role                        app.user_role NOT NULL,
    last_login_utc              timestamptz NULL,
    is_active                   boolean NOT NULL DEFAULT true,
    email_enabled               boolean NOT NULL DEFAULT true,   -- B3 preferencia (v0.3)
    quiet_hours_start           varchar(5) NULL,                 -- 'HH:mm' (v0.3)
    quiet_hours_end             varchar(5) NULL,                 -- 'HH:mm' (v0.3)
    created_utc                 timestamptz NOT NULL DEFAULT now(),
    updated_utc                 timestamptz NOT NULL DEFAULT now(),
    deleted_utc                 timestamptz NULL,
    CONSTRAINT ck_user_account_email_lower      CHECK (email = lower(email))
);
CREATE UNIQUE INDEX ux_user_account_google_subject ON app.user_account(google_subject) WHERE deleted_utc IS NULL;
CREATE UNIQUE INDEX ux_user_account_email          ON app.user_account(email)          WHERE deleted_utc IS NULL;
CREATE INDEX ix_user_account_is_active             ON app.user_account(is_active)      WHERE deleted_utc IS NULL;
CREATE TRIGGER trg_user_account_set_updated BEFORE UPDATE ON app.user_account
    FOR EACH ROW EXECUTE FUNCTION app.set_updated_utc();
```

A FK `family_member_id`-ra `ON DELETE RESTRICT` — egy `FamilyMember` soft delete-elése
nem törli a `UserAccount`-ot; egy felhasználó leválasztásához dedikált flow van.

### 4.3 source

```sql
CREATE TABLE app.source (
    id                          uuid PRIMARY KEY,
    name                        text NOT NULL,
    kind                        app.source_kind NOT NULL,
    config_json                 jsonb NOT NULL DEFAULT '{}'::jsonb,
    is_active                   boolean NOT NULL DEFAULT true,
    last_sync_utc               timestamptz NULL,
    created_utc                 timestamptz NOT NULL DEFAULT now(),
    updated_utc                 timestamptz NOT NULL DEFAULT now(),
    deleted_utc                 timestamptz NULL
);
CREATE INDEX ix_source_kind_active ON app.source(kind, is_active) WHERE deleted_utc IS NULL;
CREATE TRIGGER trg_source_set_updated BEFORE UPDATE ON app.source
    FOR EACH ROW EXECUTE FUNCTION app.set_updated_utc();
```

A `config_json` érzékeny részei (`refresh_token`, OAuth secrets) **alkalmazás-szinten
titkosítva** kerülnek be (ASP.NET Core Data Protection); a DB nyers stringként
látja.

### 4.4 email_message

```sql
CREATE TABLE app.email_message (
    id                          uuid PRIMARY KEY,
    source_id                   uuid NOT NULL REFERENCES app.source(id) ON DELETE RESTRICT,
    gmail_message_id            text NOT NULL,
    thread_id                   text NULL,
    from_address                text NOT NULL,
    to_addresses                text NOT NULL,
    subject                     text NOT NULL DEFAULT '',
    received_utc                timestamptz NOT NULL,
    body_text                   text NULL,
    body_html                   text NULL,
    snippet                     text NULL,
    has_attachments             boolean NOT NULL DEFAULT false,
    ingest_status               app.ingest_status NOT NULL DEFAULT 'Pending',
    processed_utc               timestamptz NULL,
    created_utc                 timestamptz NOT NULL DEFAULT now(),
    updated_utc                 timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_email_message_source_gmail_id
    ON app.email_message(source_id, gmail_message_id);
CREATE INDEX ix_email_message_pending
    ON app.email_message(ingest_status) WHERE ingest_status IN ('Pending','Failed');
CREATE INDEX ix_email_message_received
    ON app.email_message(received_utc DESC);
CREATE TRIGGER trg_email_message_set_updated BEFORE UPDATE ON app.email_message
    FOR EACH ROW EXECUTE FUNCTION app.set_updated_utc();
```

### 4.5 document

```sql
CREATE TABLE app.document (
    id                          uuid PRIMARY KEY,
    title                       text NOT NULL,
    original_file_name          text NOT NULL,
    mime_type                   text NOT NULL,
    size_bytes                  bigint NOT NULL,
    storage_path                text NOT NULL,
    sha256                      text NOT NULL,
    source_type                 app.source_type NOT NULL,
    source_email_message_id     uuid NULL REFERENCES app.email_message(id) ON DELETE SET NULL,
    language                    text NULL,
    document_date               date NULL,
    related_family_member_id    uuid NULL REFERENCES app.family_member(id) ON DELETE SET NULL,
    is_private                  boolean NOT NULL DEFAULT false,
    processing_status           app.processing_status NOT NULL DEFAULT 'Pending',
    origin                      app.origin NOT NULL,
    created_by_user_account_id  uuid NOT NULL REFERENCES app.user_account(id) ON DELETE RESTRICT,
    created_utc                 timestamptz NOT NULL DEFAULT now(),
    updated_utc                 timestamptz NOT NULL DEFAULT now(),
    deleted_utc                 timestamptz NULL,
    CONSTRAINT ck_document_size  CHECK (size_bytes > 0),
    CONSTRAINT ck_document_sha   CHECK (char_length(sha256) = 64)
);
CREATE UNIQUE INDEX ux_document_sha256
    ON app.document(sha256) WHERE deleted_utc IS NULL;
CREATE INDEX ix_document_source_type_created
    ON app.document(source_type, created_utc DESC) WHERE deleted_utc IS NULL;
CREATE INDEX ix_document_family_member
    ON app.document(related_family_member_id) WHERE deleted_utc IS NULL;
CREATE INDEX ix_document_processing_pending
    ON app.document(processing_status)
    WHERE processing_status IN ('Pending','Extracting','Analyzing','Failed');
CREATE INDEX ix_document_title_trgm
    ON app.document USING gin (title gin_trgm_ops) WHERE deleted_utc IS NULL;
CREATE INDEX ix_document_filename_trgm
    ON app.document USING gin (original_file_name gin_trgm_ops) WHERE deleted_utc IS NULL;
CREATE TRIGGER trg_document_set_updated BEFORE UPDATE ON app.document
    FOR EACH ROW EXECUTE FUNCTION app.set_updated_utc();
```

### 4.6 document_text

```sql
CREATE TABLE app.document_text (
    id                          uuid PRIMARY KEY,
    document_id                 uuid NOT NULL UNIQUE REFERENCES app.document(id) ON DELETE CASCADE,
    content                     text NOT NULL DEFAULT '',
    original_content            text NULL,                              -- a kinyerés / OCR eredeti
                                                                        -- állapota; csak akkor töltött,
                                                                        -- ha a felhasználó manuálisan
                                                                        -- módosította a content-et (C4)
    extraction_method           app.extraction_method NOT NULL,
    ocr_confidence              numeric(5,2) NULL,
    char_count                  int NOT NULL DEFAULT 0,
    language_detected           text NULL,
    is_manually_edited          boolean NOT NULL DEFAULT false,         -- jelzi, hogy az
                                                                        -- original_content-tel
                                                                        -- vs. content-tel dolgozunk-e
    tsv                         tsvector
        GENERATED ALWAYS AS (to_tsvector('hungarian_unaccent', content)) STORED,
    created_utc                 timestamptz NOT NULL DEFAULT now(),
    updated_utc                 timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_document_text_tsv
    ON app.document_text USING gin (tsv);
CREATE TRIGGER trg_document_text_set_updated BEFORE UPDATE ON app.document_text
    FOR EACH ROW EXECUTE FUNCTION app.set_updated_utc();
```

### 4.7 document_chunk

```sql
CREATE TABLE app.document_chunk (
    id                          uuid PRIMARY KEY,
    document_id                 uuid NOT NULL REFERENCES app.document(id) ON DELETE CASCADE,
    chunk_index                 int NOT NULL,
    content                     text NOT NULL,
    token_count                 int NOT NULL,
    embedding                   vector(768) NOT NULL,        -- nomic-embed-text dim
    embedding_model             text NOT NULL,
    created_utc                 timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_chunk_index   CHECK (chunk_index >= 0),
    CONSTRAINT ck_chunk_tokens  CHECK (token_count > 0)
);
CREATE UNIQUE INDEX ux_document_chunk_doc_idx
    ON app.document_chunk(document_id, chunk_index);
CREATE INDEX ix_document_chunk_model
    ON app.document_chunk(embedding_model);
CREATE INDEX ix_document_chunk_embed_hnsw
    ON app.document_chunk USING hnsw (embedding vector_cosine_ops)
    WITH (m = 16, ef_construction = 64);
```

> Megj.: az embedding-dimenzió a választott modelltől függ. `nomic-embed-text` →
> 768. Ha az alapmodell változik, új tábla (`document_chunk_v2`) vagy típus-
> migráció szükséges. A `embedding_model` oszlop teszi lehetővé a fokozatos
> átállást.

### 4.8 document_summary

```sql
CREATE TABLE app.document_summary (
    id                          uuid PRIMARY KEY,
    document_id                 uuid NOT NULL REFERENCES app.document(id) ON DELETE CASCADE,
    summary_text                text NOT NULL,
    language                    text NOT NULL DEFAULT 'hu',
    model                       text NOT NULL,
    prompt_version              text NOT NULL,
    is_current                  boolean NOT NULL DEFAULT true,
    created_utc                 timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_summary_len   CHECK (char_length(summary_text) >= 10)
);
CREATE UNIQUE INDEX ux_document_summary_current
    ON app.document_summary(document_id) WHERE is_current = true;
CREATE INDEX ix_document_summary_doc
    ON app.document_summary(document_id);
```

A partial UNIQUE index biztosítja, hogy egy dokumentumon egyszerre csak egy
`is_current = true` rekord legyen.

### 4.9 tag

```sql
CREATE TABLE app.tag (
    id                          uuid PRIMARY KEY,
    name                        text NOT NULL,
    color                       text NULL,
    usage_count                 int NOT NULL DEFAULT 0,
    created_utc                 timestamptz NOT NULL DEFAULT now(),
    updated_utc                 timestamptz NOT NULL DEFAULT now(),
    deleted_utc                 timestamptz NULL,
    CONSTRAINT ck_tag_name_len  CHECK (char_length(name) BETWEEN 1 AND 40),
    CONSTRAINT ck_tag_name_chars CHECK (name ~ '^[a-zA-Z0-9áéíóöőúüűÁÉÍÓÖŐÚÜŰ _\-]+$'),
    CONSTRAINT ck_tag_color     CHECK (color IS NULL OR color ~ '^#[0-9a-fA-F]{6}$')
);
CREATE UNIQUE INDEX ux_tag_name        ON app.tag(lower(name)) WHERE deleted_utc IS NULL;
CREATE INDEX        ix_tag_usage_count ON app.tag(usage_count DESC) WHERE deleted_utc IS NULL;
CREATE TRIGGER trg_tag_set_updated BEFORE UPDATE ON app.tag
    FOR EACH ROW EXECUTE FUNCTION app.set_updated_utc();
```

### 4.10 topic

```sql
CREATE TABLE app.topic (
    id                          uuid PRIMARY KEY,
    name                        text NOT NULL,
    slug                        text NOT NULL,
    parent_topic_id             uuid NULL REFERENCES app.topic(id) ON DELETE RESTRICT,
    icon                        text NULL,
    sort_order                  int NOT NULL DEFAULT 0,
    created_utc                 timestamptz NOT NULL DEFAULT now(),
    updated_utc                 timestamptz NOT NULL DEFAULT now(),
    deleted_utc                 timestamptz NULL,
    CONSTRAINT ck_topic_slug    CHECK (slug ~ '^[a-z0-9-]+$')
);
CREATE UNIQUE INDEX ux_topic_slug   ON app.topic(slug) WHERE deleted_utc IS NULL;
CREATE INDEX        ix_topic_parent ON app.topic(parent_topic_id) WHERE deleted_utc IS NULL;
CREATE TRIGGER trg_topic_set_updated BEFORE UPDATE ON app.topic
    FOR EACH ROW EXECUTE FUNCTION app.set_updated_utc();
```

### 4.11 document_tag, document_topic (join táblák)

```sql
CREATE TABLE app.document_tag (
    document_id                 uuid NOT NULL REFERENCES app.document(id) ON DELETE CASCADE,
    tag_id                      uuid NOT NULL REFERENCES app.tag(id)      ON DELETE CASCADE,
    assigned_by_user_account_id uuid NULL    REFERENCES app.user_account(id) ON DELETE SET NULL,
    origin                      app.origin NOT NULL,
    created_utc                 timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (document_id, tag_id)
);
CREATE INDEX ix_document_tag_tag ON app.document_tag(tag_id);

CREATE TABLE app.document_topic (
    document_id                 uuid NOT NULL REFERENCES app.document(id) ON DELETE CASCADE,
    topic_id                    uuid NOT NULL REFERENCES app.topic(id)    ON DELETE CASCADE,
    assigned_by_user_account_id uuid NULL    REFERENCES app.user_account(id) ON DELETE SET NULL,
    origin                      app.origin NOT NULL,
    created_utc                 timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (document_id, topic_id)
);
CREATE INDEX ix_document_topic_topic ON app.document_topic(topic_id);
```

### 4.12 note + note_chunk + note_tag + note_topic

```sql
CREATE TABLE app.note (
    id                          uuid PRIMARY KEY,
    title                       text NOT NULL,
    body                        text NOT NULL,
    related_family_member_id    uuid NULL REFERENCES app.family_member(id) ON DELETE SET NULL,
    is_private                  boolean NOT NULL DEFAULT false,
    origin                      app.origin NOT NULL,
    created_by_user_account_id  uuid NOT NULL REFERENCES app.user_account(id) ON DELETE RESTRICT,
    tsv                         tsvector
        GENERATED ALWAYS AS (to_tsvector('hungarian_unaccent', coalesce(title,'') || ' ' || body)) STORED,
    created_utc                 timestamptz NOT NULL DEFAULT now(),
    updated_utc                 timestamptz NOT NULL DEFAULT now(),
    deleted_utc                 timestamptz NULL,
    CONSTRAINT ck_note_title_len CHECK (char_length(title) BETWEEN 1 AND 200),
    CONSTRAINT ck_note_body_len  CHECK (char_length(body)  >= 1)
);
CREATE INDEX ix_note_tsv     ON app.note USING gin (tsv);
CREATE INDEX ix_note_creator ON app.note(created_by_user_account_id) WHERE deleted_utc IS NULL;
CREATE TRIGGER trg_note_set_updated BEFORE UPDATE ON app.note
    FOR EACH ROW EXECUTE FUNCTION app.set_updated_utc();

CREATE TABLE app.note_chunk (
    id                          uuid PRIMARY KEY,
    note_id                     uuid NOT NULL REFERENCES app.note(id) ON DELETE CASCADE,
    chunk_index                 int NOT NULL,
    content                     text NOT NULL,
    token_count                 int NOT NULL,
    embedding                   vector(768) NOT NULL,
    embedding_model             text NOT NULL,
    created_utc                 timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_note_chunk_note_idx ON app.note_chunk(note_id, chunk_index);
CREATE INDEX ix_note_chunk_embed_hnsw
    ON app.note_chunk USING hnsw (embedding vector_cosine_ops)
    WITH (m = 16, ef_construction = 64);

CREATE TABLE app.note_tag (
    note_id                     uuid NOT NULL REFERENCES app.note(id) ON DELETE CASCADE,
    tag_id                      uuid NOT NULL REFERENCES app.tag(id)  ON DELETE CASCADE,
    origin                      app.origin NOT NULL,
    created_utc                 timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (note_id, tag_id)
);

CREATE TABLE app.note_topic (
    note_id                     uuid NOT NULL REFERENCES app.note(id) ON DELETE CASCADE,
    topic_id                    uuid NOT NULL REFERENCES app.topic(id) ON DELETE CASCADE,
    origin                      app.origin NOT NULL,
    created_utc                 timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (note_id, topic_id)
);
```

### 4.13 task

```sql
CREATE TABLE app.task (
    id                              uuid PRIMARY KEY,
    title                           text NOT NULL,
    description                     text NULL,
    status                          app.task_status NOT NULL DEFAULT 'Open',
    priority                        app.priority    NOT NULL DEFAULT 'Normal',
    assigned_to_family_member_id    uuid NULL REFERENCES app.family_member(id) ON DELETE SET NULL,
    due_date                        date NULL,
    source_document_id              uuid NULL REFERENCES app.document(id) ON DELETE SET NULL,
    source_note_id                  uuid NULL REFERENCES app.note(id)     ON DELETE SET NULL,
    origin                          app.origin NOT NULL,
    approved_by_user_account_id     uuid NULL REFERENCES app.user_account(id) ON DELETE SET NULL,
    approved_utc                    timestamptz NULL,
    completed_utc                   timestamptz NULL,
    created_by_user_account_id      uuid NOT NULL REFERENCES app.user_account(id) ON DELETE RESTRICT,
    created_utc                     timestamptz NOT NULL DEFAULT now(),
    updated_utc                     timestamptz NOT NULL DEFAULT now(),
    deleted_utc                     timestamptz NULL,
    CONSTRAINT ck_task_title_len    CHECK (char_length(title) BETWEEN 1 AND 200),
    CONSTRAINT ck_task_suggested_origin
        CHECK (status <> 'Suggested' OR origin = 'AiSuggested'),
    CONSTRAINT ck_task_done_completed
        CHECK ((status = 'Done') = (completed_utc IS NOT NULL))
);
CREATE INDEX ix_task_status_due       ON app.task(status, due_date) WHERE deleted_utc IS NULL;
CREATE INDEX ix_task_assigned_open    ON app.task(assigned_to_family_member_id, status)
    WHERE status IN ('Open','InProgress','Suggested') AND deleted_utc IS NULL;
CREATE INDEX ix_task_suggested        ON app.task(origin) WHERE status = 'Suggested' AND deleted_utc IS NULL;
CREATE TRIGGER trg_task_set_updated BEFORE UPDATE ON app.task
    FOR EACH ROW EXECUTE FUNCTION app.set_updated_utc();
```

### 4.14 deadline

```sql
CREATE TABLE app.deadline (
    id                              uuid PRIMARY KEY,
    title                           text NOT NULL,
    due_date_utc                    timestamptz NOT NULL,
    is_all_day                      boolean NOT NULL DEFAULT true,
    category                        app.deadline_category NOT NULL DEFAULT 'Other',
    responsible_family_member_id    uuid NULL REFERENCES app.family_member(id) ON DELETE SET NULL,
    source_document_id              uuid NULL REFERENCES app.document(id) ON DELETE SET NULL,
    status                          app.deadline_status NOT NULL DEFAULT 'Upcoming',
    origin                          app.origin NOT NULL,
    approved_by_user_account_id     uuid NULL REFERENCES app.user_account(id) ON DELETE SET NULL,
    approved_utc                    timestamptz NULL,
    created_utc                     timestamptz NOT NULL DEFAULT now(),
    updated_utc                     timestamptz NOT NULL DEFAULT now(),
    deleted_utc                     timestamptz NULL,
    CONSTRAINT ck_deadline_title    CHECK (char_length(title) BETWEEN 1 AND 200)
);
CREATE INDEX ix_deadline_due           ON app.deadline(due_date_utc, status) WHERE deleted_utc IS NULL;
CREATE INDEX ix_deadline_category_due  ON app.deadline(category, due_date_utc) WHERE deleted_utc IS NULL;
CREATE INDEX ix_deadline_responsible   ON app.deadline(responsible_family_member_id, status, due_date_utc)
    WHERE deleted_utc IS NULL;
CREATE TRIGGER trg_deadline_set_updated BEFORE UPDATE ON app.deadline
    FOR EACH ROW EXECUTE FUNCTION app.set_updated_utc();
```

### 4.15 reminder

```sql
CREATE TABLE app.reminder (
    id                              uuid PRIMARY KEY,
    task_id                         uuid NULL REFERENCES app.task(id)     ON DELETE CASCADE,
    deadline_id                     uuid NULL REFERENCES app.deadline(id) ON DELETE CASCADE,
    trigger_utc                     timestamptz NOT NULL,
    offset_minutes_before_due       int NULL,
    recurrence_rule                 text NULL,
    channel                         app.notification_channel NOT NULL DEFAULT 'InApp',
    status                          app.reminder_status NOT NULL DEFAULT 'Scheduled',
    fired_utc                       timestamptz NULL,
    acknowledged_utc                timestamptz NULL,
    escalation_level                int NOT NULL DEFAULT 0,
    origin                          app.origin NOT NULL,
    created_utc                     timestamptz NOT NULL DEFAULT now(),
    updated_utc                     timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_reminder_xor      CHECK (
        (task_id IS NOT NULL)::int + (deadline_id IS NOT NULL)::int = 1
    ),
    CONSTRAINT ck_reminder_escalation CHECK (escalation_level >= 0)
);
CREATE INDEX ix_reminder_scheduled
    ON app.reminder(status, trigger_utc) WHERE status = 'Scheduled';
CREATE INDEX ix_reminder_task     ON app.reminder(task_id)     WHERE task_id     IS NOT NULL;
CREATE INDEX ix_reminder_deadline ON app.reminder(deadline_id) WHERE deadline_id IS NOT NULL;
CREATE TRIGGER trg_reminder_set_updated BEFORE UPDATE ON app.reminder
    FOR EACH ROW EXECUTE FUNCTION app.set_updated_utc();
```

A `ck_reminder_xor` adatbázis-szinten kényszeríti, hogy egy reminder
pontosan egy szülő entitásra mutasson. **Catch-up** logika
(lásd `reminder-engine.md`): a worker indulásnál a
`ix_reminder_scheduled` index alapján kiszedi az összes
`Scheduled AND trigger_utc <= now()` rekordot és tüzeli őket.

### 4.16 ai_processing_job

```sql
CREATE TABLE app.ai_processing_job (
    id                          uuid PRIMARY KEY,
    job_type                    app.ai_job_type NOT NULL,
    target_entity_type          app.job_target_type NOT NULL,
    target_entity_id            uuid NOT NULL,
    status                      app.job_status NOT NULL DEFAULT 'Queued',
    priority                    int NOT NULL DEFAULT 100,
    attempt_count               int NOT NULL DEFAULT 0,
    next_attempt_utc            timestamptz NULL,
    started_utc                 timestamptz NULL,
    finished_utc                timestamptz NULL,
    model                       text NULL,
    error_message               text NULL,
    input_payload_json          jsonb NULL,
    output_payload_json         jsonb NULL,
    created_utc                 timestamptz NOT NULL DEFAULT now(),
    updated_utc                 timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_aijob_queue
    ON app.ai_processing_job(next_attempt_utc, created_utc)
    WHERE status IN ('Queued','Failed');
-- A worker a Queued ÉS a Failed (next_attempt_utc <= now()) sorokat
-- együtt veszi fel; a retry NEM állítja vissza Queued-ra a státuszt.
CREATE INDEX ix_aijob_target
    ON app.ai_processing_job(target_entity_type, target_entity_id);
CREATE INDEX ix_aijob_type_status
    ON app.ai_processing_job(job_type, status);
CREATE TRIGGER trg_aijob_set_updated BEFORE UPDATE ON app.ai_processing_job
    FOR EACH ROW EXECUTE FUNCTION app.set_updated_utc();
```

### 4.17 audit_log

```sql
CREATE TABLE app.audit_log (
    id                          uuid PRIMARY KEY,
    occurred_utc                timestamptz NOT NULL DEFAULT now(),
    user_account_id             uuid NULL REFERENCES app.user_account(id) ON DELETE SET NULL,
    action                      app.audit_action NOT NULL,
    entity_type                 text NULL,
    entity_id                   uuid NULL,
    ip_address                  inet NULL,
    user_agent                  text NULL,
    details_json                jsonb NULL
);
CREATE INDEX ix_audit_log_occurred       ON app.audit_log(occurred_utc DESC);
CREATE INDEX ix_audit_log_user_occurred  ON app.audit_log(user_account_id, occurred_utc DESC);
CREATE INDEX ix_audit_log_entity         ON app.audit_log(entity_type, entity_id);
CREATE INDEX ix_audit_log_security
    ON app.audit_log(action)
    WHERE action IN ('Login','LoginFailed','PermissionChange');
```

Insert-only kényszer trigger:

```sql
CREATE OR REPLACE FUNCTION app.audit_log_immutable()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION 'audit_log immutable';
END $$;
CREATE TRIGGER trg_audit_log_no_update BEFORE UPDATE OR DELETE ON app.audit_log
    FOR EACH ROW EXECUTE FUNCTION app.audit_log_immutable();
```

Engedély-szinten is `REVOKE UPDATE, DELETE` a `family_app`-ról (lásd 1.4).

### 4.17.1 notification_feed

A `reminder-engine.md` 5.1.1-ben bevezetett tábla az **InApp** értesítések
kézbesítési naplójához. Szétválik a `reminder`-től, mert:
- egy `reminder` *ütemezési egység*; egy `notification_feed` rekord
  *kézbesítési egység* (lehet, hogy nem reminderből származik, hanem
  pl. új AI suggestion vagy admin üzenetből).
- a felhasználó által „olvasott" jelölés natúrális helye.

```sql
CREATE TABLE app.notification_feed (
    id                          uuid PRIMARY KEY,
    target_user_account_id      uuid NOT NULL REFERENCES app.user_account(id) ON DELETE CASCADE,
    title                       text NOT NULL,
    body                        text NOT NULL,
    related_entity_type         text NULL,                          -- 'Task','Deadline','Document','Note','Suggestion'
    related_entity_id           uuid NULL,
    reminder_id                 uuid NULL REFERENCES app.reminder(id) ON DELETE SET NULL,
    read_utc                    timestamptz NULL,
    created_utc                 timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_notification_title CHECK (char_length(title) BETWEEN 1 AND 200),
    CONSTRAINT ck_notification_body  CHECK (char_length(body)  BETWEEN 1 AND 2000)
);

CREATE INDEX ix_notification_feed_unread
    ON app.notification_feed(target_user_account_id, created_utc DESC)
    WHERE read_utc IS NULL;
CREATE INDEX ix_notification_feed_user_created
    ON app.notification_feed(target_user_account_id, created_utc DESC);
CREATE INDEX ix_notification_feed_reminder
    ON app.notification_feed(reminder_id) WHERE reminder_id IS NOT NULL;
CREATE INDEX ix_notification_feed_related
    ON app.notification_feed(related_entity_type, related_entity_id)
    WHERE related_entity_id IS NOT NULL;
```

**Megőrzés:** a `read_utc IS NOT NULL` rekordok 90 nap után takaríthatók
(opcionális napi `NotificationFeedRetentionJob`). Olvasatlanok soha nem
takarítódnak.

**Trigger:** nincs `set_updated_utc` — a feed elemei rövid élettartamúak,
nincs „módosítás" use case (kivéve a `read_utc` flagelés, ami insert
után egyszer történik).

### 4.18 warranty

```sql
CREATE TABLE app.warranty (
    id                              uuid PRIMARY KEY,
    document_id                     uuid NOT NULL UNIQUE REFERENCES app.document(id) ON DELETE CASCADE,
    product_name                    text NOT NULL,
    brand                           text NULL,
    model                           text NULL,
    serial_number                   text NULL,
    purchase_date                   date NULL,
    purchase_price                  numeric(12,2) NULL,
    currency                        text NULL,
    warranty_months                 int NULL,
    warranty_end_date               date NULL,
    seller                          text NULL,
    related_family_member_id        uuid NULL REFERENCES app.family_member(id) ON DELETE SET NULL,
    created_utc                     timestamptz NOT NULL DEFAULT now(),
    updated_utc                     timestamptz NOT NULL DEFAULT now(),
    deleted_utc                     timestamptz NULL,
    CONSTRAINT ck_warranty_price    CHECK (purchase_price IS NULL OR purchase_price >= 0),
    CONSTRAINT ck_warranty_currency CHECK (currency IS NULL OR currency ~ '^[A-Z]{3}$'),
    CONSTRAINT ck_warranty_months   CHECK (warranty_months IS NULL OR warranty_months > 0)
);
CREATE INDEX ix_warranty_end_date      ON app.warranty(warranty_end_date) WHERE deleted_utc IS NULL;
CREATE INDEX ix_warranty_brand_model   ON app.warranty(brand, model)      WHERE deleted_utc IS NULL;
CREATE TRIGGER trg_warranty_set_updated BEFORE UPDATE ON app.warranty
    FOR EACH ROW EXECUTE FUNCTION app.set_updated_utc();
```

### 4.19 medical_record

```sql
CREATE TABLE app.medical_record (
    id                          uuid PRIMARY KEY,
    document_id                 uuid NOT NULL UNIQUE REFERENCES app.document(id) ON DELETE CASCADE,
    family_member_id            uuid NOT NULL REFERENCES app.family_member(id) ON DELETE RESTRICT,
    record_type                 app.medical_record_type NOT NULL,
    record_date                 date NOT NULL,
    provider                    text NULL,
    title                       text NOT NULL,
    structured_json             jsonb NULL,
    is_private                  boolean NOT NULL DEFAULT true,
    created_utc                 timestamptz NOT NULL DEFAULT now(),
    updated_utc                 timestamptz NOT NULL DEFAULT now(),
    deleted_utc                 timestamptz NULL
);
CREATE INDEX ix_medical_member_date  ON app.medical_record(family_member_id, record_date DESC)
    WHERE deleted_utc IS NULL;
CREATE INDEX ix_medical_type_date    ON app.medical_record(record_type, record_date DESC)
    WHERE deleted_utc IS NULL;
CREATE INDEX ix_medical_structured   ON app.medical_record USING gin (structured_json jsonb_path_ops);
CREATE TRIGGER trg_medical_set_updated BEFORE UPDATE ON app.medical_record
    FOR EACH ROW EXECUTE FUNCTION app.set_updated_utc();
```

### 4.20 financial_record

```sql
CREATE TABLE app.financial_record (
    id                          uuid PRIMARY KEY,
    document_id                 uuid NOT NULL UNIQUE REFERENCES app.document(id) ON DELETE CASCADE,
    record_type                 app.financial_record_type NOT NULL,
    vendor                      text NULL,
    amount                      numeric(14,2) NULL,
    currency                    text NULL,
    issue_date                  date NULL,
    due_date                    date NULL,
    paid_date                   date NULL,
    is_paid                     boolean NOT NULL DEFAULT false,
    recurrence_period           app.recurrence_period NOT NULL DEFAULT 'None',
    related_family_member_id    uuid NULL REFERENCES app.family_member(id) ON DELETE SET NULL,
    created_utc                 timestamptz NOT NULL DEFAULT now(),
    updated_utc                 timestamptz NOT NULL DEFAULT now(),
    deleted_utc                 timestamptz NULL,
    CONSTRAINT ck_financial_amount   CHECK (amount IS NULL OR amount >= 0),
    CONSTRAINT ck_financial_currency CHECK (currency IS NULL OR currency ~ '^[A-Z]{3}$'),
    CONSTRAINT ck_financial_paid     CHECK ((is_paid = false) OR (paid_date IS NOT NULL))
);
CREATE INDEX ix_financial_unpaid_due
    ON app.financial_record(due_date) WHERE is_paid = false AND deleted_utc IS NULL;
CREATE INDEX ix_financial_type_issue
    ON app.financial_record(record_type, issue_date DESC) WHERE deleted_utc IS NULL;
CREATE INDEX ix_financial_vendor_trgm
    ON app.financial_record USING gin (vendor gin_trgm_ops) WHERE deleted_utc IS NULL;
CREATE TRIGGER trg_financial_set_updated BEFORE UPDATE ON app.financial_record
    FOR EACH ROW EXECUTE FUNCTION app.set_updated_utc();
```

### 4.21 pending_invite, revoked_session, saved_search (v0.3)

A megvalósítás során bevezetett kiegészítő táblák (a migrációk szerint):

```sql
-- Meghívók: az admin által meghívott e-mail + cél családtag + szerep.
-- Login-kor a GoogleAuthHandler feloldja; az allowlist = appsettings
-- Auth.AllowedEmails ∪ pending_invite.email.
CREATE TABLE app.pending_invite (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email             varchar(320) NOT NULL,
    family_member_id  uuid NOT NULL REFERENCES app.family_member(id) ON DELETE CASCADE,
    role              varchar(50) NOT NULL,
    created_utc       timestamptz NOT NULL DEFAULT now()
);

-- Visszavont session-ök: logout után a cookie session-id-je ide kerül,
-- a cookie-validáció minden requesten ellenőrzi.
CREATE TABLE app.revoked_session (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    session_id  varchar(256) NOT NULL UNIQUE,
    revoked_utc timestamptz NOT NULL DEFAULT now()
);

-- Mentett keresések (E7) — dashboard widget forrása.
CREATE TABLE app.saved_search (
    id              uuid PRIMARY KEY,
    name            text NOT NULL,
    query_json      text NOT NULL,
    user_account_id uuid NOT NULL REFERENCES app.user_account(id) ON DELETE CASCADE,
    created_utc     timestamptz NOT NULL DEFAULT now()
);
```

---

## 5. Seed adatok

A migrációk végén az alábbi seed-eket telepítjük (idempotens módon,
`ON CONFLICT DO NOTHING`):

### 5.1 Topic alapfa (magyar)

| Slug | Name | ParentSlug | Icon |
|---|---|---|---|
| egeszsegugy | Egészségügy | – | local_hospital |
| iskola      | Iskola | – | school |
| penzugy     | Pénzügy | – | account_balance |
| otthon      | Otthon | – | home |
| jarmu       | Jármű | – | directions_car |
| utazas      | Utazás | – | flight |
| jogi        | Jogi/Adminisztráció | – | gavel |
| egyeb       | Egyéb | – | folder |

Másodszintű opcionálisan (példa, indító készlet):
- penzugy → szamla, biztositas, elofizetes
- jarmu → kotelezo, casco, muszaki, szerviz
- otthon → garancia, kozmu, javitas
- iskola → ertesito, engedely, bizonyitvany

### 5.2 Source seed
Egyetlen `Upload` típusú forrás `Name = 'Kézi feltöltés'`, `IsActive = true`,
`ConfigJson = '{}'`. Gmail forrás csak akkor jön létre, ha az admin az UI-on
csatlakoztatja.

### 5.3 FamilyMember + UserAccount bootstrap
Az első Google login automatikusan létrehoz egy `FamilyMember`-t a
megadott `DisplayName`-mel és egy `UserAccount`-ot `Role = Admin`-nal.
További családtagokat az admin vesz fel.

---

## 6. EF Core mappolás — kulcsdöntések

- **Naming convention.** Snake_case az adatbázisban; `EFCore.NamingConventions`
  csomaggal automatikus konverzió a PascalCase C# nevekből.
- **UUID v7 generálás.** A .NET 10 natív `Guid.CreateVersion7()`-tel a
  domain-réteg factory-metódusai állítják be az ID-t létrehozáskor
  (nincs külön generátor-absztrakció, EF Core `ValueGenerator<Guid>` sem).
- **Enum mapping.** `modelBuilder.HasPostgresEnum<UserRole>("app", "user_role")`
  stb. — minden enum a `app` sémában.
- **Soft delete query filter.** Minden olyan entitásra, amelynek van
  `DeletedUtc` mezője: `entity.HasQueryFilter(x => x.DeletedUtc == null)`.
  Audit/admin kontextusban `IgnoreQueryFilters()` használatos.
- **RowVersion.** `entity.UseXminAsConcurrencyToken()`.
- **`updated_utc` trigger.** Az EF Core nem írja az `updated_utc` mezőt
  UPDATE-nél (mert a DB trigger felülírja); `Property(x => x.UpdatedUtc)
  .ValueGeneratedOnAddOrUpdate().Metadata.SetAfterSaveBehavior(
  PropertySaveBehavior.Ignore)`.
- **`tsvector` mezők.** `[NotMapped]` C# oldalon, vagy `Property` típussal
  `NpgsqlTsVector`, csak olvasásra. INSERT-nél a DB számolja.
- **`vector(768)`.** `Pgvector.EntityFrameworkCore` csomag,
  `Property<Vector>(x => x.Embedding).HasColumnType("vector(768)")`.
- **`jsonb` mezők.** `Property<T>(x => x.StructuredJson).HasColumnType("jsonb")`
  + System.Text.Json converter.
- **CHECK constraint-ek.** `entity.HasCheckConstraint("ck_...", "...")`-tal
  a modellbuilderben, így a migráció generálja őket.
- **Cascade szabályok.** EF Core alap `Restrict`; ahol cascade kell
  (chunk → document, summary → document, join táblák) kifejezetten beállítva
  `OnDelete(DeleteBehavior.Cascade)`.

---

## 7. Migrációs stratégia

- **Forrásrend.** `dotnet ef migrations add <Name>` az `Infrastructure`
  projektben; futtatás az API indulásakor automatikusan (`MigrateAsync`) —
  single-tenant, egygépes környezetben ez a vállalt egyszerűsítés
  (migráció előtt kötelező backup, DELIVERY.md 6.). Kézi futtatásra a
  `dotnet ef database update` a `family_migrator` connection stringgel.
- **Kézi SQL.** A pgvector, pg_trgm, unaccent, custom collation, ICU locale
  beállítása, trigger-függvények, FTS konfiguráció **nem EF-conventionálisak** —
  külön `__InitialSetup` migrációban raw SQL-lel kerülnek be (`migrationBuilder.Sql(...)`).
- **Adat-migrációk.** Külön projektben (`tools/data-migrations/`) script-ek,
  nem keverve a séma-migrációkkal.
- **Embedding-modell csere.** Új `embedding_model` érték → új `document_chunk_v2`
  tábla **NEM** kell, ha a dimenzió változatlan; ha változik, batch háttér-job
  generálja újra az embeddingeket (`AiProcessingJob.JobType = Embed`).
- **Backup-policy.** Naponta egy logical dump (`pg_dump -Fc`) a `data/backups/`
  alá; megőrzés 30 nap. Részletek a `architecture.md`-ben.

---

## 8. Méretezési feltételezések (sanity check)

A „kis család, ~6 fő, otthoni PC" feltevésből számolva:

| Tábla | Várható éves növekedés | 5 éves becslés |
|---|---|---|
| `document` | 1 500 / év | ~7 500 |
| `document_chunk` | 15 000 / év (átlag 10 chunk/dok) | ~75 000 |
| `note` | 500 / év | ~2 500 |
| `note_chunk` | 1 500 / év | ~7 500 |
| `email_message` | 2 000 / év (szelektív import) | ~10 000 |
| `task` | 1 000 / év | ~5 000 |
| `deadline` | 800 / év | ~4 000 |
| `reminder` | 2 500 / év | ~12 500 |
| `notification_feed` | ~5 000 / év (90 napos rotációval ~1 250 aktív) | ~25 000 (rotált) |
| `ai_processing_job` | ~20 000 / év (retry-okkal) | ~100 000 (rotálható) |
| `audit_log` | ~50 000 / év | ~250 000 |

Ezek nagyságrendben triviálisak Postgres-nek; az HNSW index ~100k vektorra
gond nélkül subsecond. Nincs partícionálás MVP-ben; `audit_log` és
`ai_processing_job` esetén éves partícionálás később opcionális, ha
fontossá válik.

# Domain modell — Family OS

> Státusz: v0.2 · Dátum: 2026-07-02 · Nyelv: magyar
> Kapcsolódó: [product-vision.md](product-vision.md), [database-schema.md](database-schema.md)
> Rögzített döntések: [ADR-0001 pgvector](decisions/ADR-0001-vektor-tarolas-pgvector.md),
> [ADR-0004 Gmail API](decisions/ADR-0004-email-gmail-api.md),
> [ADR-0009 reminder-generálás](decisions/ADR-0009-reminder-generalas-es-csatorna.md)

## Változások a v0.1 óta (v0.2 — 2026-07-02, séma v0.2/v0.3-mal szinkronban)

1. `ReminderStatus` enum: új **`Cancelled`** érték (explicit user-akció,
   elválasztva az automatikus `Skipped`-től). Remindernél nincs soft
   delete — az API `DELETE /reminders/{id}` = `Status := Cancelled`.
2. `AuditAction` enum: új **`ExternalApiCall`** érték (Gmail/SMTP hívások).
3. Új entitás: **`NotificationFeed`** (InApp kézbesítési napló) — definíció
   a [database-schema.md 4.17.1](database-schema.md)-ben, ott normatív.
4. `DocumentText`: új **`OriginalContent`** és **`IsManuallyEdited`** mezők
   (C4 manuális szövegkorrekció előtti állapot megőrzése).
5. Új kiegészítő entitások a megvalósításból: **`PendingInvite`**
   (meghívó: email + családtag + szerep), **`RevokedSession`** (logout
   utáni session-tiltólista), **`SavedSearch`** (mentett keresés, E7) —
   séma: database-schema.md 4.21. A felhasználói preferenciák
   (`EmailEnabled`, `QuietHoursStart/End`) a `UserAccount`-on élnek.
6. `Reminder`: a default reminderek a Deadline **jóváhagyásakor**
   generálódnak, nem a javaslatkor (ADR-0009).

---

## 0. Általános konvenciók

Minden entitásra az alábbi szabályok érvényesek (nem ismétlem őket az
egyedi szakaszokban):

- **Elsődleges kulcs:** `Id : Guid` (UUID v7, idő-rendezett). Indok:
  EF Core barát, audit/log korreláció előny, nincs sorrend-szivárgás.
- **Időbélyegek:** minden entitáson `CreatedUtc : timestamptz`,
  `UpdatedUtc : timestamptz` (UTC, kötelező).
- **Soft delete:** felhasználói-tartalmú entitásokon `DeletedUtc :
  timestamptz?`. A repó-szintű query alapból szűri a NULL-okat.
  Rendszer-naplók (`AuditLog`, `AiProcessingJob`) **nem** soft delete-elnek.
- **Tulajdonos:** ahol releváns, `CreatedByUserAccountId : Guid`
  (auditra és row-level szűrésre).
- **Privátság jelző:** `IsPrivate : bool` az érzékeny entitásokon
  (`Document`, `Note`, `MedicalRecord`, `FinancialRecord`); csak a tulajdonos
  és az adminok látják. (RBAC részletek: [security-privacy.md](security-privacy.md))
- **Concurrency:** `RowVersion` az `xmin` rendszeroszlopra képezve
  (32 bites `uint` EF Core-ban, `UseXminAsConcurrencyToken()`); az API-n
  base64 stringként utazik.
- **Single-tenant feltételezés:** az MVP egy család (lásd Product Vision).
  Egy implicit `Family` entitást **nem** vezetünk be — minden családtag-szintű
  rekord közvetlen jogosultság-szabályok mentén él. Multi-family bevezetésekor
  egy `FamilyId` migráció hozzáadásával bővíthető.
- **Magyar nyelv:** szöveges mezőkön `text` típus, `lc_collate = 'hu-HU-x-icu'`
  collation (database-schema.md részletez), Q&A magyar.
- **AI-eredet jelzés:** minden olyan entitásnál, amit AI is létrehozhat,
  van `Origin : enum {Manual, AiSuggested, AiApproved, ImportedEmail,
  ImportedFile}` mező. Az `AiSuggested` rekordok **nem aktívak** addig, amíg
  a felhasználó nem hagyja jóvá (`ApprovedByUserAccountId`, `ApprovedUtc`).

---

## 1. Entitások (MVP)

### 1.1 UserAccount

**Cél:** Hitelesítési identitás (Google OAuth subject). Egy `UserAccount`
egy emberi felhasználó; egy `FamilyMember`-rel 1:1 kapcsolatban áll.

| Mező | Típus | Megj. |
|---|---|---|
| Id | Guid (PK) | |
| GoogleSubject | text | egyedi, Google `sub` claim |
| Email | text | egyedi, kisbetűsített |
| DisplayName | text | a Google profil neve, módosítható |
| Role | enum `UserRole {Admin, Adult, Child}` | |
| LastLoginUtc | timestamptz? | |
| IsActive | bool | letiltott fiók = false |
| FamilyMemberId | Guid (FK) | 1:1 a `FamilyMember`-re |

**Relációk:** 1:1 `FamilyMember`. 1:N `Document` (CreatedBy),
`Note`, `Task` (CreatedBy / AssignedTo a `FamilyMember`-en keresztül),
`AuditLog`.

**Indexek:**
- UNIQUE `(GoogleSubject)`
- UNIQUE `(Email)` lowercased
- INDEX `(IsActive)` (gyakori szűrés)

**Validáció:** Email RFC-szerű minta; GoogleSubject nem üres; Role MVP-ben
fix háromból; egy Admin minimum mindig kell (alkalmazás szinten kényszerített).

---

### 1.2 FamilyMember

**Cél:** A család egy tagja — nem feltétlenül van saját login (pl. kisgyerek
csak adat-alanyként szerepelhet). Egészségügyi és iskolai rekordok ide
horgonyzódnak.

| Mező | Típus | Megj. |
|---|---|---|
| Id | Guid (PK) | |
| DisplayName | text | „Apa”, „Anya”, „Lili” |
| FullName | text? | hivatalos név (orvosi/jogi dokumentumokhoz) |
| Relation | enum `Relation {Self, Spouse, Child, Parent, Other}` | a háztartás központi user nézőpontjából |
| BirthDate | date? | életkor-érzékeny figyelmeztetésekhez |
| HasUserAccount | bool | gyorsindex; ha true, a `UserAccount.FamilyMemberId` mutat erre |
| Notes | text? | szabad megjegyzés |

**Relációk:** 0..1 `UserAccount`. 1:N `MedicalRecord`, `Task` (AssignedTo),
`Deadline` (ResponsibleMember).

**Indexek:**
- INDEX `(Relation)`
- INDEX `(HasUserAccount) WHERE DeletedUtc IS NULL`

**Validáció:** DisplayName nem üres; BirthDate ≤ ma; FullName legfeljebb 200 char.

---

### 1.3 Document

**Cél:** Univerzális dokumentum-konténer. Bármi, ami fájl-alapú adatot hordoz
(PDF, kép, email-export). Minden egyéb tartalmi entitás (DocumentText, Summary,
Tag, Topic, Warranty, MedicalRecord, FinancialRecord) erre hivatkozik.

| Mező | Típus | Megj. |
|---|---|---|
| Id | Guid (PK) | |
| Title | text | felhasználói vagy AI-javasolt cím |
| OriginalFileName | text | |
| MimeType | text | `application/pdf`, `image/jpeg`, … |
| SizeBytes | bigint | |
| StoragePath | text | a fájlrendszerbeli relatív útvonal (a `data/documents/<év>/<hónap>/<guid>.<ext>` mintát követi) |
| Sha256 | text | duplikátum-detektálás |
| SourceType | enum `SourceType {Upload, Email, Manual}` | |
| SourceEmailMessageId | Guid? (FK) | ha emailből származik |
| Language | text? | ISO 639-1 (`hu`, `en`); nyelvdetektálás eredménye |
| DocumentDate | date? | a dokumentum *dátuma* (nem a feltöltésé), ha kinyerhető |
| RelatedFamilyMemberId | Guid? (FK) | ha egy konkrét családtaghoz kötődik |
| IsPrivate | bool | |
| ProcessingStatus | enum `ProcessingStatus {Pending, Extracting, Analyzing, Done, Failed}` | |
| Origin | enum `Origin` | lásd 0. szakasz |
| CreatedByUserAccountId | Guid (FK) | |

**Relációk:**
- 1:1 `DocumentText` (a teljes kinyert szöveg)
- 1:1 `DocumentSummary` (legutóbbi AI összefoglaló)
- 1:N `DocumentChunk` (embedding-egységek)
- M:N `Tag` (via `DocumentTag`)
- M:N `Topic` (via `DocumentTopic`)
- 1:0..1 `Warranty` / `MedicalRecord` / `FinancialRecord` (specializált facet)
- 0..1 `EmailMessage` (forrás)

**Indexek:**
- UNIQUE `(Sha256) WHERE DeletedUtc IS NULL`
- INDEX `(SourceType, CreatedUtc DESC)`
- INDEX `(RelatedFamilyMemberId)`
- INDEX `(ProcessingStatus) WHERE ProcessingStatus IN ('Pending','Extracting','Analyzing','Failed')` — partial, queue-monitorozáshoz
- GIN INDEX `Title` és `OriginalFileName` `pg_trgm` operátorral (gyors „like” keresés)

**Validáció:** SizeBytes > 0 és ≤ konfigurált limit (alap: 50 MB); MimeType
whitelisten (pdf, jpeg, png, heic, txt, docx); Sha256 64 hex char.

---

### 1.4 DocumentText

**Cél:** A `Document`-ből kinyert teljes nyers szöveg. Külön táblában, mert
nagy és ritkán kell (nem listák, csak full-text search és RAG).

| Mező | Típus | Megj. |
|---|---|---|
| Id | Guid (PK) | |
| DocumentId | Guid (FK, UNIQUE) | 1:1 a Document-re |
| Content | text | teljes kinyert szöveg |
| ExtractionMethod | enum `ExtractionMethod {PdfTextLayer, TesseractOcr, ManualPaste, EmailBody}` | |
| OcrConfidence | numeric(5,2)? | átlag konfidencia, ha OCR |
| CharCount | int | gyors statisztikához |
| LanguageDetected | text? | a nyelvdetektor outputja, ha eltér a `Document.Language`-tól |
| Tsv | tsvector | generated column, a `Content`-ből magyar config-gal |

**Relációk:** 1:1 `Document`.

**Indexek:**
- GIN INDEX `Tsv` (full-text search)
- UNIQUE `(DocumentId)`

**Validáció:** Content nem NULL (üres string megengedett, ha OCR sikertelen
volt — ekkor `ProcessingStatus = Failed` a Document-en).

---

### 1.5 DocumentChunk

**Cél:** Embedding-egységek a szemantikus kereséshez. Egy dokumentumból több
darab (kb. 500–1000 token méretű chunk).

| Mező | Típus | Megj. |
|---|---|---|
| Id | Guid (PK) | |
| DocumentId | Guid (FK) | |
| ChunkIndex | int | a dokumentumon belüli sorszám |
| Content | text | a chunk szöveges tartalma |
| TokenCount | int | becsült token-szám |
| Embedding | vector(768) | pgvector; dimenzió a választott modellből, alap: `nomic-embed-text` |
| EmbeddingModel | text | „nomic-embed-text:v1.5”, későbbi migrációhoz |

**Relációk:** N:1 `Document` (cascade delete).

**Indexek:**
- HNSW INDEX `(Embedding) WITH (m=16, ef_construction=64) USING vector_cosine_ops`
- UNIQUE `(DocumentId, ChunkIndex)`
- INDEX `(EmbeddingModel)` — modellváltáskori migrációs lekérdezésekhez

**Validáció:** ChunkIndex ≥ 0; TokenCount > 0; Embedding nem NULL; az
`EmbeddingModel` változása új sor (nem update), hogy a régi embeddingek
mellett az újak fokozatosan generálódhassanak.

---

### 1.6 DocumentSummary

**Cél:** AI által generált tömör összefoglaló a dokumentumról (3–5 mondat
magyar nyelven). Külön entitás, mert verziózható (új modell, új prompt).

| Mező | Típus | Megj. |
|---|---|---|
| Id | Guid (PK) | |
| DocumentId | Guid (FK) | |
| SummaryText | text | |
| Language | text | általában `hu` |
| Model | text | „gpt-oss-20b@ollama”, „claude-haiku-4.5”, … |
| PromptVersion | text | a prompt-template verziója |
| IsCurrent | bool | egyszerre egy „aktuális” összefoglaló van Document-enként |

**Relációk:** N:1 `Document`.

**Indexek:**
- INDEX `(DocumentId, IsCurrent) WHERE IsCurrent = true`
- UNIQUE részleges: csak egy `IsCurrent = true` lehet egy `DocumentId`-re.

**Validáció:** SummaryText min. 10 char.

---

### 1.7 Tag

**Cél:** Szabad-szöveges, lapos címke (pl. „garancia”, „autó”, „2026”).
Felhasználó vagy AI hozza létre. Lapos szerkezet — nincs hierarchia.

| Mező | Típus | Megj. |
|---|---|---|
| Id | Guid (PK) | |
| Name | text | tároláskor lowercase-normalizált (ékezetek megőrizve); egyediség a normalizált néven |
| Color | text? | hex (#rrggbb) UI-hoz |
| UsageCount | int | denormalizált, gyors rendezéshez |

**Relációk:** M:N `Document` (via `DocumentTag` join: `DocumentId`, `TagId`,
`AssignedByUserAccountId`, `Origin`).

**Indexek:** UNIQUE `(Name)`; INDEX `(UsageCount DESC)`.

**Validáció:** Name 1–40 char, csak betűk/számok/`-`/`_`/szóköz.

---

### 1.8 Topic

**Cél:** Magasabb szintű, kurált témakör (pl. „Egészségügy”, „Iskola”,
„Pénzügy”, „Autó”). Hierarchikus — egy Topic-nak lehet szülője.
A `Document` és `Note` egy vagy több témakörhöz tartozhat.

| Mező | Típus | Megj. |
|---|---|---|
| Id | Guid (PK) | |
| Name | text | magyar megnevezés |
| Slug | text | URL-barát azonosító, egyedi |
| ParentTopicId | Guid? (FK) | önreferenciás |
| Icon | text? | Material Icons név vagy emoji |
| SortOrder | int | UI |

**Relációk:** Önreferenciás fa; M:N `Document` (via `DocumentTopic`);
M:N `Note` (via `NoteTopic`).

**Indexek:** UNIQUE `(Slug)`; INDEX `(ParentTopicId)`.

**Validáció:** Slug `^[a-z0-9-]+$`; mélység legfeljebb 3 szint (alkalmazás-szintű).
Seed-adatok: az MVP-ben fix induló halmaz (Egészségügy, Iskola, Pénzügy,
Otthon, Jármű, Utazás, Jogi/Adminisztráció, Egyéb).

---

### 1.9 Note

**Cél:** Manuális, szabad szöveges jegyzet. Nem fájl-alapú, de a feldolgozó
pipeline és a kereső ugyanúgy kezeli (chunkolódik, embeddingelődik).

| Mező | Típus | Megj. |
|---|---|---|
| Id | Guid (PK) | |
| Title | text | |
| Body | text (markdown) | |
| RelatedFamilyMemberId | Guid? (FK) | |
| IsPrivate | bool | |
| CreatedByUserAccountId | Guid (FK) | |
| Origin | enum `Origin` | |
| Tsv | tsvector | generated, magyar config |

**Relációk:** M:N `Topic`, M:N `Tag`. 1:N `NoteChunk` (analóg a `DocumentChunk`-kal,
külön táblában az egyszerűbb migrációért — de a vektor-keresés a `Document`+`Note`
chunkjain együtt fut).

**Indexek:** GIN `Tsv`; INDEX `(CreatedByUserAccountId)`.

**Validáció:** Title 1–200 char; Body min. 1 char.

---

### 1.10 Task

**Cél:** Konkrét, elvégzendő tennivaló családtaghoz rendelve. Lehet AI-javaslat
(`Origin = AiSuggested`), aktiválni a `Approved*` mezőkkel kell.

| Mező | Típus | Megj. |
|---|---|---|
| Id | Guid (PK) | |
| Title | text | |
| Description | text? | markdown |
| Status | enum `TaskStatus {Suggested, Open, InProgress, Done, Cancelled}` | |
| Priority | enum `Priority {Low, Normal, High}` | |
| AssignedToFamilyMemberId | Guid? (FK) | felelős |
| DueDate | date? | határidő (külön Deadline entitás komplex eseteknél) |
| SourceDocumentId | Guid? (FK) | ha dokumentumból kinyert |
| SourceNoteId | Guid? (FK) | |
| Origin | enum `Origin` | |
| ApprovedByUserAccountId | Guid? (FK) | |
| ApprovedUtc | timestamptz? | |
| CompletedUtc | timestamptz? | |
| CreatedByUserAccountId | Guid (FK) | |

**Relációk:** N:1 `FamilyMember` (AssignedTo). 0..1 `Document` / `Note`
(forrás). 1:N `Reminder`.

**Indexek:**
- INDEX `(Status, DueDate)` — dashboard query
- INDEX `(AssignedToFamilyMemberId, Status) WHERE Status IN ('Open','InProgress','Suggested')`
- INDEX `(Origin) WHERE Status = 'Suggested'` — jóváhagyásra váró javaslatok

**Validáció:** `Suggested` státusszal csak `Origin = AiSuggested` lehet;
`Status = Done` → `CompletedUtc` kötelező; `Status` átmenetek alkalmazás-szintű
gépen kontrolláltak.

---

### 1.11 Deadline

**Cél:** Idő-kötött kötelezettség (pl. „autó kötelező lejár 2026-09-14”).
Ellentétben a `Task`-kal, ez **esemény-jellegű**, nem feladat — egy
számla lejárati dátuma akkor is rögzítendő, ha még nincs hozzá Task.
Sok esetben a Deadline-ból generálódik egy Task javaslat.

| Mező | Típus | Megj. |
|---|---|---|
| Id | Guid (PK) | |
| Title | text | „Autó kötelező lejár” |
| DueDateUtc | timestamptz | |
| IsAllDay | bool | true esetén csak dátum értelmes |
| Category | enum `DeadlineCategory {Insurance, Invoice, Inspection, School, Medical, Subscription, Personal, Other}` | |
| ResponsibleFamilyMemberId | Guid? (FK) | |
| SourceDocumentId | Guid? (FK) | |
| Status | enum `DeadlineStatus {Upcoming, Due, Passed, Resolved, Dismissed}` | |
| Origin | enum `Origin` | |
| ApprovedByUserAccountId | Guid? (FK) | |
| ApprovedUtc | timestamptz? | |

**Relációk:** N:1 `FamilyMember`. 0..1 `Document`. 1:N `Reminder`.

**Indexek:**
- INDEX `(DueDateUtc, Status)` — dashboard: „következő 30 nap”
- INDEX `(Category, DueDateUtc)`
- INDEX `(ResponsibleFamilyMemberId, Status, DueDateUtc)`

**Validáció:** `DueDateUtc` >= a `CreatedUtc` (nincs múltbéli új deadline,
kivéve importnál); státusz-átmenet alkalmazás-szintű (`Upcoming → Due → Passed`
ütemezővel, `Resolved`/`Dismissed` user-akcióból).

---

### 1.12 Reminder

**Cél:** Konkrét, időzített emlékeztetés esemény. Egy `Task`-hoz vagy
`Deadline`-hoz tartozhat. Az ütemező (Hangfire/Quartz) tüzeli;
**catch-up szemantika**: ha a PC offline volt, a fire-window-ban esedékes
emlékeztetők az induláskor tüzelnek (lásd Reminder Engine doksi).

| Mező | Típus | Megj. |
|---|---|---|
| Id | Guid (PK) | |
| TaskId | Guid? (FK) | XOR DeadlineId |
| DeadlineId | Guid? (FK) | XOR TaskId |
| TriggerUtc | timestamptz | abszolút időpont |
| OffsetMinutesBeforeDue | int? | ha relatív (pl. „1 nappal előtte”); generáláshoz a TriggerUtc kiszámítva |
| RecurrenceRule | text? | RFC 5545 (RRULE) string; NULL = egyszeri |
| Channel | enum `NotificationChannel {InApp, Email}` | MVP: InApp kötelezően; Email opcionális |
| Status | enum `ReminderStatus {Scheduled, Fired, Acknowledged, Skipped, Failed, Cancelled}` | `Cancelled` = explicit user-akció |
| FiredUtc | timestamptz? | |
| AcknowledgedUtc | timestamptz? | |
| EscalationLevel | int | 0 = elsődleges, 1 = első eszkaláció, … |
| Origin | enum `Origin` | |

**Relációk:** N:1 `Task` *XOR* `Deadline`.

**Indexek:**
- INDEX `(Status, TriggerUtc) WHERE Status = 'Scheduled'` — ütemező scan
- INDEX `(TaskId)`; INDEX `(DeadlineId)`

**Validáció:** `TaskId` és `DeadlineId` közül pontosan egy nem-NULL
(adatbázis-szintű CHECK); `TriggerUtc` UTC; ha `RecurrenceRule` van,
RFC 5545 szerint parseolható (alkalmazás-szintű).

---

### 1.13 Source

**Cél:** Kis lookup tábla a beérkezési forrásokról *konfiguráció-szinten*
(pl. „Gmail fiók kmarko.net@gmail.com”, „kézi feltöltés”). Nem összekeverendő
a `Document.SourceType` enummal — az a *típust* jelöli, ez egy konkrét
integráció-példányt.

| Mező | Típus | Megj. |
|---|---|---|
| Id | Guid (PK) | |
| Name | text | „Gmail – kmarko.net” |
| Kind | enum `SourceKind {Upload, GmailAccount, FileWatch}` | |
| ConfigJson | jsonb | OAuth tokenek titkosítva, scope-ok, filter-szabályok |
| IsActive | bool | |
| LastSyncUtc | timestamptz? | |

**Relációk:** 1:N `EmailMessage` (`SourceId`).

**Indexek:** INDEX `(Kind, IsActive)`.

**Validáció:** `ConfigJson` séma JSON-séma-validációval ellenőrzött a
forrás-típus szerint; titkos mezők (refresh token) titkosítva tárolódnak
(Data Protection API).

---

### 1.14 EmailMessage

**Cél:** Egy beszívott email reprezentációja. Az MVP-ben `family-os/import`
Gmail-címkével ellátott üzenetek kerülnek ide (lásd ADR-0004). Az emailből
származó mellékletek és/vagy a body feldolgozottan `Document`(eke)t hoz létre.

| Mező | Típus | Megj. |
|---|---|---|
| Id | Guid (PK) | |
| SourceId | Guid (FK) | melyik `Source` (Gmail fiók) |
| GmailMessageId | text | a Gmail-féle stabil ID |
| ThreadId | text? | |
| FromAddress | text | |
| ToAddresses | text | vesszővel elválasztva (egyszerű MVP) |
| Subject | text | |
| ReceivedUtc | timestamptz | |
| BodyText | text? | tisztított text/plain verzió |
| BodyHtml | text? | opcionális, audithoz |
| Snippet | text? | Gmail-féle preview |
| HasAttachments | bool | |
| IngestStatus | enum `IngestStatus {Pending, Processed, Skipped, Failed}` | |
| ProcessedUtc | timestamptz? | |

**Relációk:** N:1 `Source`. 1:N `Document` (a mellékletek + body → 1 vagy
több Document; a `Document.SourceEmailMessageId` mutat vissza).

**Indexek:**
- UNIQUE `(SourceId, GmailMessageId)`
- INDEX `(IngestStatus) WHERE IngestStatus IN ('Pending','Failed')`
- INDEX `(ReceivedUtc DESC)`

**Validáció:** GmailMessageId nem üres; legalább `BodyText` vagy
`HasAttachments = true` legyen — különben skip.

---

### 1.15 AiProcessingJob

**Cél:** Hosszú futású AI-művelet sorba állítva. A „PC nem mindig fent”
feltételezés miatt durable queue: a táblát a worker pollozza, és failure-re
exponenciális backoff-fal újrapróbál. A jobok típusai: szöveg-kinyerés,
nyelvdetekt, összefoglaló, entitás-kivonás, határidő-kivonás, embedding,
osztályozás.

| Mező | Típus | Megj. |
|---|---|---|
| Id | Guid (PK) | |
| JobType | enum `AiJobType {ExtractText, DetectLanguage, Summarize, ExtractEntities, ExtractDeadlines, ExtractTasks, Classify, Embed}` | |
| TargetEntityType | enum `JobTargetType {Document, Note, EmailMessage}` | |
| TargetEntityId | Guid | a céltábla rekord-azonosítója |
| Status | enum `JobStatus {Queued, Running, Completed, Failed, Cancelled}` | |
| Priority | int | alacsonyabb = előbb futtatandó |
| AttemptCount | int | |
| NextAttemptUtc | timestamptz? | exponenciális backoff |
| StartedUtc | timestamptz? | |
| FinishedUtc | timestamptz? | |
| Model | text? | melyik modell futott |
| ErrorMessage | text? | utolsó hiba |
| InputPayloadJson | jsonb? | belső param (prompt-version, chunk-méret, …) |
| OutputPayloadJson | jsonb? | strukturált eredmény, ha kicsi (különben a céltáblába íródott) |

**Relációk:** logikai FK `(TargetEntityType, TargetEntityId)` — nincs hard FK
(polimorf reláció), alkalmazás-szinten validált.

**Indexek:**
- INDEX `(Status, Priority, NextAttemptUtc) WHERE Status = 'Queued'` — worker pickup
- INDEX `(TargetEntityType, TargetEntityId)`
- INDEX `(JobType, Status)`

**Validáció:** `AttemptCount` ≤ konfigurált max (alap: 5); failure-nél
`NextAttemptUtc = now + min(60s * 2^Attempt, 6 óra)`.

**Megj.:** Ez a tábla NEM helyettesíti a Hangfire belső storage-ét — a
Hangfire saját sémát használ. Ez a tábla a *domain-szintű* AI-feladat
nézet (üzleti rálátáshoz, retryhoz, dashboardhoz). A Hangfire ezeknek
a soroknak az átlépését triggereli (egy job = egy Hangfire enqueue).
Konkrét döntés: lásd architecture.md.

---

### 1.16 AuditLog

**Cél:** Biztonsági és változás-követő napló. Minden írási művelet ide
naplózódik (létrehozás, módosítás, törlés, jogosultság-változás, AI-jóváhagyás,
sikeres/sikertelen login).

| Mező | Típus | Megj. |
|---|---|---|
| Id | Guid (PK) | |
| OccurredUtc | timestamptz | |
| UserAccountId | Guid? (FK) | null = rendszer |
| Action | enum `AuditAction {Create, Update, Delete, Login, LoginFailed, Approve, Reject, AiCall, FileAccess, PermissionChange, ExternalApiCall}` | |
| EntityType | text | a céltábla neve |
| EntityId | Guid? | a céltábla rekord-azonosítója |
| IpAddress | inet? | |
| UserAgent | text? | |
| DetailsJson | jsonb? | strukturált diff vagy kontextus |

**Relációk:** N:1 `UserAccount` (gyenge — törölt user esetén is megmarad
a log).

**Indexek:**
- INDEX `(OccurredUtc DESC)`
- INDEX `(UserAccountId, OccurredUtc DESC)`
- INDEX `(EntityType, EntityId)`
- INDEX `(Action) WHERE Action IN ('Login','LoginFailed','PermissionChange')` — security review

**Validáció:** insert-only tábla (nincs UPDATE/DELETE). Adatbázis-szinten
trigger vagy revoke a `family_app` userre. Megőrzés alapból végtelen, de
admin-konfigurálható retention politikával.

---

### 1.17 Warranty

**Cél:** Egy `Document`-hez kötött specializált facet: vásárlás-, garancia-,
termékadatok strukturáltan, hogy gyorsan kereshetők és emlékeztethetők
legyenek.

| Mező | Típus | Megj. |
|---|---|---|
| Id | Guid (PK) | |
| DocumentId | Guid (FK, UNIQUE) | |
| ProductName | text | „Bosch WAW28560BY mosógép” |
| Brand | text? | |
| Model | text? | |
| SerialNumber | text? | |
| PurchaseDate | date? | |
| PurchasePrice | numeric(12,2)? | |
| Currency | text? | ISO 4217 (`HUF`, `EUR`) |
| WarrantyMonths | int? | gyártói garancia hossza |
| WarrantyEndDate | date? | számított vagy kinyert |
| Seller | text? | bolt / webshop |
| RelatedFamilyMemberId | Guid? (FK) | ha valakihez közvetlenül kötődik |

**Relációk:** 1:1 `Document`. 0..1 `FamilyMember`.

**Indexek:** INDEX `(WarrantyEndDate)` — közelgő lejáratokhoz; INDEX
`(Brand, Model)`; UNIQUE `(DocumentId)`.

**Validáció:** Ha `WarrantyMonths` és `PurchaseDate` adott és `WarrantyEndDate`
nem, alkalmazás-szinten számolódik; `PurchasePrice` ≥ 0.

---

### 1.18 MedicalRecord

**Cél:** Egy `Document`-hez kötött egészségügyi facet (lab eredmény, recept,
oltási bizonyítvány, lelet). **Nem** diagnosztikai eszköz — csak tárolás
és kereshetőség.

| Mező | Típus | Megj. |
|---|---|---|
| Id | Guid (PK) | |
| DocumentId | Guid (FK, UNIQUE) | |
| FamilyMemberId | Guid (FK) | kötelező |
| RecordType | enum `MedicalRecordType {LabResult, Prescription, Vaccination, Imaging, Diagnosis, AppointmentNote, Other}` | |
| RecordDate | date | a lelet/vizsgálat dátuma |
| Provider | text? | rendelő / orvos / kórház |
| Title | text | rövid megnevezés |
| StructuredJson | jsonb? | szabad strukturált adat (pl. lab-értékek key→{value, unit, refRange}) |
| IsPrivate | bool | alapból true |

**Relációk:** 1:1 `Document`. N:1 `FamilyMember` (kötelező).

**Indexek:**
- INDEX `(FamilyMemberId, RecordDate DESC)`
- INDEX `(RecordType, RecordDate DESC)`
- GIN INDEX `StructuredJson jsonb_path_ops` (későbbi szűrésekhez)

**Validáció:** `FamilyMemberId` kötelező; `IsPrivate` default true;
láthatóság: alapból csak az érintett `FamilyMember`-hez tartozó `UserAccount`,
a partnere (Adult role) és az adminok.

---

### 1.19 FinancialRecord

**Cél:** Egy `Document`-hez kötött pénzügyi facet (számla, csekk, kötelező
biztosítás, előfizetés, banki értesítő).

| Mező | Típus | Megj. |
|---|---|---|
| Id | Guid (PK) | |
| DocumentId | Guid (FK, UNIQUE) | |
| RecordType | enum `FinancialRecordType {Invoice, Receipt, Insurance, Subscription, BankStatement, Contract, Other}` | |
| Vendor | text? | szolgáltató / bolt |
| Amount | numeric(14,2)? | |
| Currency | text? | ISO 4217 |
| IssueDate | date? | |
| DueDate | date? | fizetési határidő |
| PaidDate | date? | |
| IsPaid | bool | denormalizált, gyors szűrés |
| RecurrencePeriod | enum `RecurrencePeriod {None, Monthly, Quarterly, Yearly}` | előfizetésekhez |
| RelatedFamilyMemberId | Guid? (FK) | |

**Relációk:** 1:1 `Document`. 0..1 `FamilyMember`.

**Indexek:**
- INDEX `(IsPaid, DueDate) WHERE IsPaid = false` — „kifizetetlen” query
- INDEX `(RecordType, IssueDate DESC)`
- INDEX `(Vendor)` `pg_trgm`-mel

**Validáció:** Ha `IsPaid = true`, `PaidDate` kötelező; Amount ≥ 0.

---

## 2. Jövőbeli entitások (nem MVP, de a séma tervezésénél figyelembe vesszük)

### 2.1 CalendarEvent
**Cél:** Naptári esemény (saját vagy Google Calendar-ról importált).
Megkülönböztetendő a `Deadline`-tól: az esemény *időablakot* jelöl
(kezdő-záró időponttal), nem csak egy lejárati pillanatot.

**Vázlat:** `Id`, `Title`, `StartUtc`, `EndUtc`, `Location`, `Attendees
(M:N FamilyMember)`, `ExternalCalendarSource`, `ExternalEventId`,
`RecurrenceRule`, `Origin`.

**Kapcsolat:** Egy `Deadline` később linkelhető egy `CalendarEvent`-hez,
ha a felhasználó kitűzi naptárba.

### 2.2 Asset
**Cél:** Eszköz/birtok (mosógép, autó, kazán). Egy `Warranty` opcionálisan
egy `Asset`-re hivatkozhat → így a karbantartási előzmény összegyűlik egy
helyen.

**Vázlat:** `Id`, `Name`, `AssetType {Vehicle, Appliance, Real Estate, Other}`,
`PurchaseDate`, `OwnerFamilyMemberId`, `Notes`, `IsActive`.

**Kapcsolat:** 1:N `Warranty`, 1:N `Document` (M:N `AssetDocument` join-on,
karbantartási előzmény), 1:N `MaintenanceLog` (későbbi).

### 2.3 SchoolRecord
**Cél:** Iskolai/oktatási rekord (értesítő, bizonyítvány, engedély, esemény).

**Vázlat:** `Id`, `DocumentId (1:1)`, `FamilyMemberId (gyermek)`,
`SchoolName`, `RecordType {Notice, Grade, Permission, Event, Other}`,
`AcademicYear`, `RecordDate`, `RequiresParentSignature`.

### 2.4 Subscription
**Cél:** Élő előfizetés (Netflix, mobilszolgáltató, biztosítás éves
megújulás). A `FinancialRecord` egy konkrét számláról szól; a `Subscription`
maga az élő szerződés.

**Vázlat:** `Id`, `Name`, `Vendor`, `Amount`, `Currency`, `BillingPeriod`,
`NextBillingDate`, `PaymentMethod`, `Status {Active, Paused, Cancelled}`,
`RelatedFamilyMemberId`, `Notes`.

**Kapcsolat:** 1:N `FinancialRecord` (történet), 1:N `Deadline` (megújulások).

---

## 3. Összefoglaló kapcsolatok (ER áttekintés)

```
UserAccount ──1:1── FamilyMember
                       │
                       ├──1:N── MedicalRecord (kötelező FK)
                       ├──1:N── Task (AssignedTo, opcionális)
                       └──1:N── Deadline (Responsible, opcionális)

Document ──1:1── DocumentText
         ──1:1── DocumentSummary (IsCurrent szűrt)
         ──1:N── DocumentChunk        (pgvector embedding)
         ──M:N── Tag    (DocumentTag)
         ──M:N── Topic  (DocumentTopic)
         ──0..1── Warranty  / MedicalRecord / FinancialRecord  (facet)
         ──0..1── EmailMessage (forrás)

Note ──1:N── NoteChunk
     ──M:N── Tag / Topic

EmailMessage ──N:1── Source
             ──1:N── Document  (mellékletek + body)

Task     ──1:N── Reminder
Deadline ──1:N── Reminder    (egy Reminder XOR Task/Deadline)

AiProcessingJob ──(polimorf)── Document | Note | EmailMessage
AuditLog        ──N:1── UserAccount (gyenge)
```

---

## 4. Tervezési indoklások (rövid)

- **Specializált facet vs. öröklés.** A `Warranty`, `MedicalRecord`,
  `FinancialRecord` külön táblák egy 1:1 FK-val a `Document`-re — nem
  TPH/TPT öröklés. Indok: egy dokumentum egyszerre lehet több facet
  alany (ritkán, de pl. egy számla is és egy garancia is); későbbi
  bővítéskor új facet-tábla hozzáadása nem érinti a `Document` sémát.

- **Task vs. Deadline szétválasztás.** Egy lejárati dátum (Deadline)
  *tény*, egy Task pedig *teendő*. A felhasználó egy Deadline-ból
  generálhat Task-ot, de nem kötelező — pl. a számla lejárat nem mindig
  teendő (ha automata utalás van). Az AI-pipeline először `Deadline`-t
  javasol, majd opcionálisan kapcsolódó `Task`-ot is.

- **AI-javaslat státusz mindenhol.** A `Suggested` állapot (`Task.Status`,
  `Deadline.Status`-ban indirekt, `Origin` enum mindenhol) tükrözi a
  Product Vision non-goal #7 elvét: **AI nem aktivál automatikusan**.

- **Polimorf relációk minimalizálva.** A polimorf FK-t (hard FK helyett
  típus + ID) csak ott használjuk, ahol elkerülhetetlen (`AiProcessingJob`,
  `AuditLog`). Mindenhol máshol explicit FK.

- **DocumentChunk vs. NoteChunk szétválasztva.** Egyszerűbb migráció,
  könnyebb tisztítás, ugyanaz a vektor-keresés mindkettőn — egy SQL
  view (`searchable_chunk`) egyesíti őket a search-pipeline-nak.

- **Concurrency.** `RowVersion` minden szerkeszthető entitáson, hogy
  két felhasználó (vagy user + AI) egyidejű módosítása ütközéskor
  visszajelzést adjon.

# Review — database-schema.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Kiemelkedően részletes, implementálásra kész séma-doksi: teljes DDL,
partial indexek, CHECK constraint-ek, trigger-ek, seed-adatok, EF Core
mappolási döntések, méretezési sanity check. A v0.2 changelog jó gyakorlat.
Néhány **technikai hiba a DDL-ben** javítandó, és pár tábla hiányzik az
API-igényekhez.

## Hibák

### 1. `CREATE DATABASE` — érvénytelen locale-kombináció (súlyos)
Az 1.1 DDL:
```sql
LC_COLLATE = 'hu-HU-x-icu'
LC_CTYPE   = 'hu-HU-x-icu'
LOCALE_PROVIDER = 'icu'
ICU_LOCALE = 'hu-HU';
```
Ez így **nem fut le**: `LC_COLLATE`/`LC_CTYPE` mindig *libc* locale-t vár
(pl. `C.UTF-8` vagy `hu_HU.utf8`), az ICU-t a `ICU_LOCALE` adja. A
`'hu-HU-x-icu'` nem létező libc locale → hiba. Helyesen:
```sql
CREATE DATABASE family_os
    ENCODING 'UTF8' TEMPLATE template0
    LOCALE_PROVIDER 'icu' ICU_LOCALE 'hu-HU'
    LC_COLLATE 'C.UTF-8' LC_CTYPE 'C.UTF-8';
```
(Ellenőrizendő a pgvector/pgvector:pg16 image libc locale-kínálata.)

### 2. `now() AT TIME ZONE 'UTC'` default-ok (közepes)
A `timestamptz` oszlopok defaultja mindenütt `now() AT TIME ZONE 'UTC'`.
Ez a kifejezés `timestamp`-et (tz nélkül) ad vissza, amit a Postgres a
szerver időzónája szerint **visszakonvertál** timestamptz-vé — ha a
konténer TZ nem UTC, az érték eltolódik. `timestamptz`-nél a helyes és
egyszerűbb default: **`now()`** (az mindig abszolút időpont). Ugyanez a
`set_updated_utc()` triggerben.

### 3. Hard delete vs. jogosultságok (közepes)
Az 1.4 szerint a `family_app` role csak `SELECT, INSERT, UPDATE` jogot kap
(DELETE-et nem). Az api-design.md viszont admin `?hard=true` fizikai
törlést ígér (7.9), és a cascade-ek (document → chunk/summary) is DELETE-et
igényelnek. Vagy kap a role DELETE-et a megfelelő táblákra, vagy a hard
delete külön mechanizmus (pl. `SECURITY DEFINER` függvény / migrator
kapcsolat) — jelenleg a két doksi ellentmond.

### 4. `ix_aijob_queue` nem fedi a retry-scant (közepes)
Az index `WHERE status = 'Queued'`, de az architecture.md 6.2 szerint a
scheduler a `Failed + next_attempt_utc <= now` sorokat is felveszi.
Lásd architecture.review.md #1 — döntés után az indexet vagy a
worker-logikát igazítani kell. Megjegyzés: ha a backoff a `Failed`
státuszt `Queued`-ra állítja vissza `next_attempt_utc`-vel, akkor az
index jó, és csak az architecture szövege pontosítandó. Az indexből
ekkor is hiányzik viszont a `NULLS FIRST` kérdés: új joboknál
`next_attempt_utc IS NULL`, a rendezett scan (`ORDER BY priority,
next_attempt_utc`) NULL-kezelését definiálni kell.

### 5. Hiányzó táblák az API-kontrakthoz (közepes)
Az api-design.md-hez képest nincs séma a következőkre (részletek:
domain-model.review.md #2):
- meghívó / email-allowlist (`POST /user-accounts/invite`, login-allowlist)
- mentett keresések (`/search/saved`, dashboard `savedSearches`)
- felhasználói preferenciák (quiet hours, emailEnabled, escalationOptOut)
- idempotency-kulcs tár (1.9: 24 órás megőrzés — memóriában nem éli túl
  a restartot, ami épp a projekt alapfeltevése)

### 6. Kisebb technikai megjegyzések
- **4.2 `user_account.family_member_id UNIQUE`**: oszlop-szintű UNIQUE
  constraint — soft delete-elt fiókra is érvényes, tehát egy family
  memberhez *soha* nem hozható létre új fiók, ha a régi soft-deleted.
  Ha ez use case (fiók-csere), partial unique index kell
  (`WHERE deleted_utc IS NULL`), mint a google_subject-nél.
- **4.9 tag check regex** `'^[a-zA-Z0-9áéíóöőúüűÁÉÍÓÖŐÚÜŰ _\-]+$'` — a
  domain-model lowercase-normalizálást ír; egyeztetendő (lásd
  domain-model.review.md #5).
- **4.15 reminder**: nincs `deleted_utc` — az API `DELETE /reminders/{id}`
  ide a `Cancelled` státuszt kell képezze; egy megjegyzés-sor ide is kell.
- **4.16 ai_processing_job**: nincs `max_attempts` oszlop; a limit (5)
  konfigból jön — oké, de a domain-model validációs szabálya
  („AttemptCount ≤ konfigurált max”) app-szintű, jelezni érdemes.
- **8. méretezés**: HNSW + 768 dim + ~100k vektor — reális, rendben.
- **1.2**: „UUID v7-et alkalmazás-szinten generálunk” — konzisztens a
  6. szakasz EF-döntésével, jó.

## Erősségek (megőrzendő)

- Partial unique index az `is_current` summary-ra (4.8) — helyes minta.
- `ck_reminder_xor` DB-szintű XOR (4.15) — helyes.
- `audit_log` immutability trigger + REVOKE kombináció (4.17).
- Kétszerepes DB-hozzáférés (`family_app` vs `family_migrator`).
- `hungarian_unaccent` FTS konfiguráció szintaxisa helyes.

## Verdikt

A legérettebb doksi a készletben; az #1 (locale DDL) és #2 (UTC default)
konkrét bug, javítandó még kód-generálás előtt. A hiányzó táblák sorsáról
(API-szűkítés vagy séma-bővítés) döntés kell.

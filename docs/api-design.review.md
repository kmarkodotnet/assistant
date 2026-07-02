# Review — api-design.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Következetes, jól strukturált REST-terv: egységes konvenciók (ProblemDetails,
pagination, If-Match/ETag, Idempotency-Key), akció-endpointok a státusz-
átmenetekre, SignalR hubok, jó példa-payloadok. A hibák többsége a
mögöttes séma hiányosságaiból ered, plusz néhány belső következetlenség.

## Hibák / következetlenségek

### 1. Endpointok fedezet nélküli adatmodellel (súlyos)
Az alábbi végpontokhoz nincs entitás/tábla a domain-model / database-schema
doksikban (részletezve: domain-model.review.md #2):
- `POST /user-accounts/invite` + login allowlist (3.1: „403 — email nincs
  az allowlist-en”) → nincs invite/allowlist tábla.
- `GET/POST/DELETE /search/saved` + dashboard `savedSearches` → nincs
  saved_search tábla.
- `PATCH /auth/me/preferences` (quiet hours, emailEnabled,
  escalationOptOut) → a user_account-on nincsenek ilyen oszlopok.
- Idempotency-tár (1.9: 24 órás (user, key) → response megőrzés) →
  nincs definiált tároló; memóriában nem éli túl a restartot, ami a
  projekt explicit alapfeltevése.

### 2. Idempotency-Key szabály belső ellentmondása (kicsi)
1.9: „A **202-vel záruló** POST-okra kötelező az Idempotency-Key” — a 7.1
dokumentum-feltöltés viszont **201**-gyel válaszol, és mégis kötelező rá.
A szabály helyesen: „hosszú futású vagy nem-idempotens kritikus POST-okra”
(ahogy az 1.5 táblázat mondja). Egységesítendő.

### 3. `DELETE /reminders/{id}` szemantikája (kicsi)
1.1 szerint DELETE = soft delete, de a reminder táblán nincs
`deleted_utc`; a v0.2 séma `Cancelled` reminder-státuszt vezetett be.
Írjuk ki: reminder DELETE = `Status := Cancelled` (nem soft delete).

### 4. Pagination-limit kivétel jelöletlen (kicsi)
1.6: „pageSize max 100”; 19.1 audit-log: „max 200/oldal”. Ha szándékos
kivétel, jelölni kell az 1.6-ban; ha nem, egységesíteni.

### 5. `PATCH /documents/{id}/text` — job-lista pontosítás (kicsi)
7.7: a szövegkorrekció „Embed + Summarize” jobokat indít újra — a
Classify / ExtractDeadlines / ExtractTasks nem fut újra? A C4 story és az
ai-pipeline.md-vel egyeztetendő, mert a kinyert határidők a szövegből
származnak; ha a user épp egy rossz OCR-dátumot javít, elvárná az
újra-extrakciót. Legalább dokumentálni kell a döntést.

### 6. `POST /documents` 409 dedup-válasz (kicsi)
7.1: sha256-ütközésnél 409 + meglévő rekord ID. Jó minta, de row-level
kérdés: ha az ütköző dokumentum **más user private** rekordja, a 409 +
ID információ-szivárgás (kiderül, hogy a fájl már létezik a rendszerben).
Egy mondat kell a viselkedésről (pl. ilyenkor generikus 409 ID nélkül).

### 7. Apróságok
- 16.1 Q&A rate limit (10 req/min/user) — jó; a `qa` mód timeout-ja
  (Ollama hidegindítás percekig tarthat!) nincs specifikálva; 503
  vs. 202+polling döntés hiányzik. Az 1.2 táblázat 503-a erre utal,
  de a lassú (nem elérhetetlen) provider esete nyitott.
- 22. SignalR: melyik process hosztolja a hubokat, és a Workers hogyan
  publikál? (architecture.review.md #6).
- 21.2: a `PrivacyMode` API-szinten nem állítható — konzisztens az
  architecture 5.3 kapujával, jó.
- 4.1 heartbeat auth nélkül — LAN-only környezetben elfogadható, oké.
- 2.4: `rowVersion: string (base64)` vs. domain-model „xmin” — xmin
  uint32, a base64 ábrázolás működik, csak legyen egyféle a kódban.

## Erősségek (megőrzendő)

- Suggestions inbox (15.) + batch approve — jól szolgálja a „AI nem
  aktivál automatikusan” elvet.
- Aggregált `GET /dashboard` a <200 ms cél érdekében.
- Hibapéldák konkrét payloaddal (23–24.).

## Verdikt

Kontraktnak jó alap; az #1-ben felsorolt fedezetlen végpontok ügyében
séma-bővítés vagy scope-húzás szükséges, mielőtt a BUILD fázis elindul.

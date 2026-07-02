# Review — domain-model.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Alapos domain modell: a közös konvenciók (UUIDv7, soft delete, Origin,
Approved*) egy helyen rögzítettek, a facet-minta (Warranty/Medical/Financial
1:1 a Document-re) jól indokolt, a Task/Deadline szétválasztás átgondolt.
**A fő probléma: a dokumentum v0.1-en ragadt, miközben a database-schema.md
v0.2 már továbblépett** — a két doksi több ponton széttartott.

## Hibák / következetlenségek

### 1. Elavult a database-schema v0.2-höz képest (súlyos — szinkron)
A séma-doksi „Változások a v0.1 óta” szakasza négy módosítást vezetett be,
amelyek innen hiányoznak:
- `ReminderStatus` enum: itt nincs `Cancelled` (1.12), a sémában már van.
- `AuditAction` enum: itt nincs `ExternalApiCall` (1.16), a sémában van.
- **`NotificationFeed` entitás teljesen hiányzik** — a sémában tábla,
  az api-design 14. szakaszában endpoint, itt semmi.
- `DocumentText.OriginalContent` + `IsManuallyEdited` mezők hiányoznak (1.4).

A domain-model a „forrás”, amiből a séma származik — ha a séma előreszalad,
a modell-doksit frissíteni kell, különben a fejlesztő agentek rossz
kontraktból dolgoznak.

### 2. Hiányzó entitások az API-igényekhez (súlyos)
Az api-design.md több olyan képességet definiál, amihez sem itt, sem a
sémában nincs entitás:
- **Meghívó / e-mail allowlist** (api 6.2 `POST /user-accounts/invite` és
  auth 3.1 „email nincs az allowlist-en”) — nincs `Invite`/`AllowedEmail`
  entitás.
- **Mentett keresés** (api 16.2 `search/saved`, dashboard widget) — nincs
  `SavedSearch` entitás.
- **Felhasználói preferenciák** (api 6.5: `emailEnabled`, `quietHours*`,
  `escalationOptOut`) — a `UserAccount`-on nincsenek ilyen mezők.
- **Idempotency-kulcs tár** (api 1.9: 24 órás (user, key) → response) —
  nincs hozzá tábla/mechanizmus megnevezve.

### 3. `HasUserAccount` denormalizált flag (közepes)
A `FamilyMember.HasUserAccount` bool a `UserAccount.FamilyMemberId` 1:1
kapcsolat duplikátuma — szinkronhiba-forrás (fiók deaktiválás, soft
delete). Vagy legyen kiszámolt (join/exists), vagy írjuk le, mely
tranzakciók tartják karban.

### 4. Reminder — soft delete hiánya vs. API DELETE (kicsi)
Az api-design 13.4 szerint `DELETE /reminders/{id}` = „Mégse”, az 1.1
konvenció szerint a DELETE soft delete — de a `Reminder`-en nincs
`DeletedUtc` (és a sémában sincs). Feltehetően a v0.2-ben bevezetett
`Cancelled` státusz a szándék; rögzítsük explicit, hogy remindernél
DELETE = `Status := Cancelled`.

### 5. Tag normalizálás kétértelmű (kicsi)
1.7: „Name normalizált (lowercased…)” — a séma viszont megenged nagybetűt
(check regex), és `lower(name)`-re unique-ol. Döntés kell: tároláskor
lowercase-elünk (akkor a séma-check szigorítható), vagy megőrizzük a
felhasználói írásmódot és csak az egyediség case-insensitive (akkor a
modell-szöveg pontosítandó).

### 6. Apróságok
- 0. szakasz: „RowVersion : bytea (xmin alapú)” — az xmin nem bytea,
  hanem 32 bites rendszeroszlop; EF Core-ban `uint` (a séma-doksi 6.
  szakasza már helyesen `UseXminAsConcurrencyToken`). A típusmegjelölés
  javítandó.
- 1.11 Deadline validáció: „DueDateUtc >= CreatedUtc (kivéve importnál)” —
  a kivétel-feltétel (Origin = Imported*?) legyen egzakt.
- 1.15: az `AiProcessingJob` indexe itt `(Status, Priority, NextAttemptUtc)`,
  a sémában `(priority, next_attempt_utc) WHERE status='Queued'` — kis
  eltérés, egységesítendő (és lásd architecture.review.md #1: a Failed
  sorok újrafelvétele nem fedett).
- 2. szakasz (jövőbeli entitások): jó, hogy előre gondol a
  CalendarEvent/Asset/SchoolRecord/Subscription-re — maradjon is nem-MVP.

## Verdikt

Jó modell, de **szinkronizálni kell**: v0.2-re emelés a séma-változásokkal,
plusz a hiányzó entitások (invite/allowlist, saved search, user
preferences) pótlása vagy explicit elhalasztása.

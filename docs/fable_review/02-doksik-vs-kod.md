# Doksik ↔ kód eltérések és teendők — Family OS

> Státusz: REVIEW v1.0 · Dátum: 2026-07-12 · Forrás: dokumentáció-kód összevetés (Fable review)
> Vizsgált állapot: `ai-proposal-learn` ág, commit `d2526a4`

Minden tételnél döntést igényel: **(a)** a kód igazodjon a doksihoz, vagy
**(b)** a doksi igazodjon a valósághoz. A javaslat oszlop ezt jelzi.

---

## 1. Ami pontosan egyezik (nem teendő — referenciának)

| Doksi | Kód | Megjegyzés |
|---|---|---|
| `database-schema.md` v0.3 | `FamilyOsDbContext` + migrációk | 1:1 táblamegfelelés; az új `email_message` oszlopok a kontrakt-deltában dokumentáltak |
| `reminder-engine.md` 6.x | `DueReminderDispatcher`, `EscalationScheduler` | SKIP LOCKED, catch-up, quiet-hours, Ical.Net — szinte sorról sorra |
| `ai-pipeline.md` job-típusok | `AiJobExecutor` | mind a 10 job-típus lekezelve |
| `search-strategy.md` rétegek | `Application/Search` (Intent/Rrf/Qa) + FTS + szemantikus | mappaszinten is tükröződik |
| ADR-0011 (tool-calling) | token service, confirm/reject flow | a kódkommentek szakaszra hivatkoznak |
| `security-privacy.md` 7.2–7.3 | `LocalFilesystemDocumentStorage`, `MimeDetector` | path-traversal + magic-byte védelem megvan |

---

## 2. A doksi ígéri, a kód nem tudja (implementációs adósság)

### 2.1 security-privacy.md — biztonsági ígéretek

| # | Doksi-állítás | Valóság | Javaslat |
|---|---|---|---|
| S1 | §9.7: rate limiting 100 req/min/user, AI 10 req/min | nincs semmilyen rate limiter | **kód** (lásd 01-es doksi 2.2) |
| S2 | §3.4: failed-login throttling (5 hiba/10 perc → 15 perc tiltás) | nincs | **kód** |
| S3 | §6.2/§10.1: OAuth refresh token DataProtection-titkosítással | plaintext a `source.config_json`-ban | **kód** (01-es doksi 1.4) |
| S4 | §6.4: DataProtection kulcsrotáció, kulcstár a `dp-keys` mappában | `AddDataProtection` sehol; a compose `dp_keys` volume árva; cookie-kulcsok default helyen → konténer-újraépítésnél session-vesztés | **kód** |
| S5 | §5.5: `audit_log` insert-only trigger + `REVOKE UPDATE, DELETE` | nincs; sőt `DbSeedRunner.cs:79` GRANT UPDATE/DELETE-et ad a `family_app`-nak minden táblára | **kód** |
| S6 | §5.4: audit-retention napi takarító (`Audit.RetentionDays` + `Audit.Immutable`) | nincs ilyen worker (csak `NotificationFeedRetentionJob`) | **kód vagy doksi** — dönteni kell, MVP-e |

**Teendők:**
- [ ] S1–S2: rate limiter + login throttling (01-es doksi 2.2 részletezi).
- [ ] S3–S4: `AddDataProtection` + token-titkosítás (01-es doksi 1.4).
- [ ] S5: migráció vagy seed-lépés: `REVOKE UPDATE, DELETE ON app.audit_log
      FROM family_app` + insert-only trigger; a DbSeedRunner GRANT-je
      tábla-szinten finomítandó.
- [ ] S6: döntés + vagy audit-cleaner worker, vagy a doksi-szakasz
      „post-MVP" jelölése.

### 2.2 api-design.md — API-konvenciók és végpontok

| # | Doksi-állítás | Valóság | Javaslat |
|---|---|---|---|
| A1 | §1.9: idempotencia `(user, key)` mappinggel, ~24 h célértékkel | user-szkópolás halott (middleware auth előtt fut), lejárat nincs | **kód** (01-es doksi 1.1) |
| A2 | `/api/v1/user-accounts` | a kód `/api/v1/users`-t használ | **doksi** (a FE is `/users`-höz igazodna; API-törés ne legyen) |
| A3 | `GET /api/v1/system/version` | nem létezik | **kód** (olcsó) vagy doksi-törlés |
| A4 | `POST /documents/search`, `POST /notes/search` | nem léteznek (a globális `POST /search` van) | **doksi** — a search-strategy szerinti egykapus keresés a valóság |
| A5 | `GET /settings/preferences` | a kód: `/auth/me/preferences` (PATCH) + `/settings/system` + `/settings/integrations` | **doksi** |
| A6 | warranty/medical/financial PATCH, document-tag DELETE teljes végpontként | szándékos 501-stubok (`DocumentsModule.cs:130,147-149`) | **doksi**: jelölje 501/v2 státusszal |
| A7 | §1.5: `ETag`/`If-Match` header, `X-Total-Count` | nincs; RowVersion a body-ban utazik | **doksi** (a body-s RowVersion működik) vagy kód, ha kell a header-forma |
| A8 | §1.10: `/openapi/v1.json`, `/swagger` admin-only | Swashbuckle default útvonal, csak Development, admin-kapu nélkül; a FE `gen:api` a doksi szerinti (nem létező) URL-re mutat | **kód**: stabil OpenAPI-útvonal + a `gen:api` script javítása; admin-kapu döntés |
| A9 | §1.4: CORS allowlist a háztartási hálózatra | nincs CORS-konfiguráció (nginx mögött same-origin) | **doksi**: rögzítse, hogy same-origin miatt nem kell |
| A10 | `PATCH /settings/system` működő beállításként | a handler no-op: warningot logol, sikert jelez (`PatchSystemSettingsCommandHandler.cs:15`) | **kód vagy doksi** — vagy tényleges perzisztencia, vagy a FE/doksi jelezze, hogy restart kell |
| A11 | §1.2: 429 státuszkód a katalógusban | rate limiting híján soha nem keletkezik | S1-gyel együtt oldódik |

**Teendők:**
- [ ] A1: 01-es doksi 1.1.
- [ ] A2, A4, A5, A6, A7, A9: api-design.md frissítése a tényleges API-hoz
      (egy PR-ben végigvihető).
- [ ] A3: `GET /system/version` implementálása (assembly-verzió) vagy törlés.
- [ ] A8: OpenAPI-végpont stabilizálása + `frontend/package.json gen:api`
      URL javítása; döntés a swagger admin-kapuról.
- [ ] A10: döntés a settings-patch sorsáról.

### 2.3 reminder-engine.md

| # | Doksi-állítás | Valóság | Javaslat |
|---|---|---|---|
| R1 | §6.2: `Reminders.CatchUpMaxAgeDays` konfigurálható | hardcoded 14 nap (`DueReminderDispatcher.cs:184`) | **kód** (olcsó) vagy doksi |

- [ ] R1: options-osztályba emelés vagy doksi-pontosítás.

---

## 3. Elavult státusz-doksik (a kód jobb, mint a doksi)

| # | Doksi | Elavult állítás | Teendő |
|---|---|---|---|
| D1 | `missing_implementation.md` (2026-07-08) | search/tasks/deadlines/settings-system oldalak „stub" | teljes felülvizsgálat: mindhárom oldal kész (280–310 sor, teszttel); vagy frissítés, vagy archiválás |
| D2 | `docs/qa/ui-test-scenarios.md` (2026-07-02) | E/F epic „🚫 nem tesztelhető"; „smoke fut minden PR-en" | 3. doksi (tesztek) részletezi |
| D3 | `docs/qa/ui-test-scenarios.md` §2 | full-stack E2E réteg „jövőbeli", test-login „ADR-igénylő döntés" | a `full_tests/` réteg és a test-login elkészült ADR nélkül — ADR pótlása + doksi-frissítés |

**Teendők:**
- [ ] D1: `missing_implementation.md` frissítése vagy törlése/archiválása
      (félrevezető a jelenlegi állapotban).
- [ ] D2–D3: ui-test-scenarios.md 3. és 6. szakaszának újraírása a tényleges
      spec-készlethez; test-login ADR pótlása.

---

## 4. Javasolt sorrend

1. **Gyors doksi-igazítások** (A2, A4–A7, A9, D1): félrevezető állítások
   megszüntetése — olcsó, kockázatmentes.
2. **Biztonsági adósság** (S1–S5): ezek valódi rések, nem doksi-hibák —
   a 01-es doksi P1/P2 tételeivel együtt ütemezendők.
3. **Döntést igénylő tételek** (S6, A3, A8, A10, R1): rövid döntés
   (ADR vagy backlog-item), utána implementáció vagy doksi-zárás.

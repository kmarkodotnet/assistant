# Doksik ↔ tesztek eltérések és teendők — Family OS

> Státusz: REVIEW v1.0 · Dátum: 2026-07-12 · Forrás: tesztkészlet-dokumentáció összevetés (Fable review)
> Vizsgált állapot: `ai-proposal-learn` ág, commit `d2526a4`
> Tesztleltár: backend 273 unit/integrációs teszt (2 FAIL) · frontend 172 vitest ·
> 13 mockolt e2e spec (`frontend/e2e/`) · 10 full-stack e2e spec (`full_tests/specs/`)

---

## 1. Ami jól áll (nem teendő — referenciának)

- **Kontrakt-doksik → tesztek fegyelme:** classify-email-contract →
  `ClassifyEmailJobRunnerTests` + Testcontainers-integrációs teszt +
  `OllamaEmailClassifierTests`; daily-digest-contract → `DailyDigestJobTests`;
  ADR-0011 tool-calling → planner/token/replay-guard/tool/handler unit tesztek
  + 53 FE unit teszt + `tool-calling-flow.spec.ts`. A tesztek szakaszra
  hivatkoznak.
- **ui-test-scenarios.md ✅-jelölésű forgatókönyvei** mind léteznek spec-ként
  (auth, family, rbac, preferences, documents, suggestions, reminders,
  dashboard), `@smoke` tagekkel a doksi szerint.
- **A tesztkészlet több helyen megelőzi a doksit:** tasks-, deadlines-,
  search-, notifications-flow e2e specek léteznek, amiket a QA-doksi még
  🚫-nak jelöl; a `full_tests/` full-stack réteg a doksiban „jövőbeliként"
  leírt 2. réteg megvalósulása.
- **Konvenciók:** xUnit + NSubstitute + FluentAssertions mind az 5 backend
  tesztprojektben; AAA; `Method_State_Expected` nevek (coding-standards §8.2).

---

## 2. Törött kapuk (P1 — ezek most hamis biztonságot adnak)

### 2.1 Privacy „RED GATE" nem létező projektre mutat

**Fájlok:** `Makefile` (`test-privacy` target), `.github/workflows/security.yml:23`

Mindkettő a `tests/FamilyOs.Infrastructure.Ai.Tests/` projektet futtatná —
**ilyen projekt nincs**. A `dotnet test` ott hibára fut, a security-privacy.md
§13.3 szerinti privacy-kapu nem véd. A tényleges privacy-tesztek máshol élnek:
`tests/FamilyOs.Infrastructure.Tests/Ai/AiProviderPrivacyGuardTests.cs`
(5 teszt), de `PrivacyAssertion` kategória-attribútum nélkül.

**Teendők:**
- [ ] Döntés: külön `FamilyOs.Infrastructure.Ai.Tests` projekt létrehozása,
      VAGY a Makefile + security.yml átirányítása a meglévő tesztosztályra.
- [ ] `[Trait("Category","PrivacyAssertion")]` felvétele a privacy-guard
      tesztekre, hogy a `--filter` működjön.
- [ ] A security.yml futásának ellenőrzése (zöld-e egyáltalán a workflow).

### 2.2 Két bukó backend teszt

`GmailIngestionServiceTests` 2 tesztje elavult elvárással bukik
(részletek: 01-es doksi 1.2). A „dotnet test zöld" kapu jelenleg sérül.

- [ ] Teszt-elvárások igazítása.

### 2.3 CI frontend-tesztlépés elavult flagekkel

**Fájl:** `.github/workflows/ci.yml:77`

`pnpm test -- --watch=false --browsers=ChromeHeadless` — a `--browsers` Karma-
flag, a projekt viszont vitest-re állt át (`package.json test = "vitest run"`);
a vitest az ismeretlen opción valószínűleg elhasal. Emellett a CI `pnpm`-et
használ, miközben a repóban `package-lock.json` (npm) van.

**Teendők:**
- [ ] CI FE-tesztlépés: `npm ci` + `npm test` (vagy egységesen pnpm +
      pnpm-lock.yaml — döntés kell).
- [ ] A felesleges Karma-flagek törlése.
- [ ] Ellenőrizni, hogy a CI ténylegesen zölden fut-e (a workflow-történet
      alapján).

---

## 3. Doksi-követelmény nem teljesül (tesztadósság)

### 3.1 coding-standards.md §8.1: „minden API endpoint happy + 1 hiba út"

Az integrációs tesztek (Testcontainers ✅) ~22 tesztet adnak ~8 modulra
(auth, health, documents-upload, notes, deadlines, tasks, suggestions,
search, problem-details). **Nincs API-integrációs teszt:** reminders,
notifications, family, users, tags, topics, dashboard, audit-log, ai-jobs,
ai-providers, sources, settings, tool-calls.

- [ ] Hiánylista alapján endpoint-integrációs tesztek pótlása (prioritás:
      tool-calls és reminders — állapotváltós, jogosultság-érzékeny végpontok).
- [ ] VAGY a coding-standards enyhítése („kritikus modulok" listára), ha a
      teljes lefedés nem reális cél — de ezt explicit döntsük el.

### 3.2 coding-standards.md §8.3: Respawn DB-tisztítás

A doksi Respawn-t ír elő minden teszt előtti tisztításra — a fixture-ben
nincs Respawn.

- [ ] Respawn bevezetése a `FamilyOsTestFixture`-be, VAGY a doksi igazítása
      a tényleges izolációs stratégiához.

### 3.3 coding-standards.md §8.1: coverage-célok mérés nélkül

Cél: Domain ≥ 85%, Application ≥ 75%, Infrastructure ≥ 50% — de sem a
Makefile, sem a CI nem gyűjt coverage-t, a célok ellenőrizetlenek.

- [ ] `dotnet test --collect:"XPlat Code Coverage"` + küszöb-ellenőrzés a
      CI-ban (pl. reportgenerator), VAGY a célok törlése a doksiból.

### 3.4 coding-standards.md §8.1: E2E „minden UC-01..08 + @security regresszió"

- `@security` tag **sehol nem létezik** (sem frontend/e2e, sem full_tests).
- UC-lefedés: UC-01 ✅ (01-upload-pipeline), UC-02 ✅ (02-search),
  UC-04 ✅ (04-deadlines + dashboard), UC-05 ✅ (05-notes), UC-06 ✅ (03-tasks);
  **UC-03** (garancia-keresés) csak közvetve; **UC-07** (egészségügyi rekord)
  nem tesztelhető — a feature maga 501-stub; **UC-08** (Gmail) nincs lefedve.

- [ ] `@security` regressziós készlet definiálása (minimum: RBAC-elutasítások,
      test-login tiltás Production-ben, IsPrivate láthatóság) és megírása.
- [ ] UC-03 explicit spec (garancia-lekérdezés a search-flow-ban).
- [ ] UC-07/UC-08: a doksiban jelölni, hogy feature-függő — a 04-es doksi
      CR-javaslataihoz kapcsolódik.

### 3.5 ui-test-scenarios.md: hamis CI-állítás + elavult státuszok

A doksi szerint a `@smoke` készlet „minden PR-en fut CI-ban" — a `ci.yml`-ben
**nincs Playwright-lépés**. A 3. szakasz állapottáblája és a 6. szakasz
🚫-jelölései elavultak (search/tasks/deadlines kész + spec-elt).

- [ ] Playwright smoke-lépés felvétele a CI-ba (mockolt réteg, nem igényel
      stacket), VAGY a doksi-állítás javítása.
- [ ] A forgatókönyv-státuszok (🚫→✅/📋) frissítése a tényleges spec-készlethez;
      az új specek (tasks/deadlines/search/notifications/tool-calling)
      forgatókönyveinek visszadokumentálása.
- [ ] A §2-ben ADR-igényesnek jelölt test-login döntés ADR-jének pótlása
      (lásd 01-es doksi 1.3).

### 3.6 📋-jelölésű (dokumentált, nem automatizált) forgatókönyvek

Továbbra sincs automatizálva: notes CRUD (QA-H1-01..04), topics (QA-I2-01..03),
admin szűrés/export (QA-J1..J3), Gmail-integrations UI (QA-K1), dashboard üres
állapotok (QA-L1-02..03), toast/offline (QA-A4, A5), SignalR-frissítés
(QA-D11-01, a full-stack rétegbe való).

- [ ] Prioritás szerinti pótlás; a doksi szerint ezekhez részben `data-testid`
      pótlás is kell (ui-test-scenarios.md 5. szakasz hiánylistája).

---

## 4. Javasolt sorrend

1. **2.x törött kapuk** — a privacy-gate, a 2 bukó teszt és a CI FE-lépés
   javítása (fél nap, azonnali hitelesség-nyereség).
2. **3.5** — CI-ba Playwright smoke + QA-doksi frissítés (a doksi-vs-kód
   doksi D2–D3 tételeivel együtt).
3. **3.1** — endpoint-integrációs tesztek pótlása, tool-calls + reminders
   prioritással.
4. **3.3–3.4** — coverage-mérés és @security készlet.
5. **3.2, 3.6** — Respawn-döntés, 📋-forgatókönyvek folyamatos pótlása.

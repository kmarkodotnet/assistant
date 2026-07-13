# Teszt-kapuk és tesztadósság — Sonnet-implementációs feladatkártyák

> Státusz: IMPLEMENTÁCIÓS SPEC v1.0 · Dátum: 2026-07-12
> Forrás: `docs/fable_review/03-doksik-vs-tesztek.md` · Vizsgált állapot: `ai-proposal-learn`, commit `d2526a4`
> Tesztleltár: backend 273 teszt (2 FAIL) · frontend 172 vitest · 13 mockolt e2e · 10 full-stack e2e

## Kártyák áttekintése

| Kártya | Mi | Prioritás | Függés |
|---|---|---|---|
| G1 | privacy RED GATE javítása | P1 | — |
| G2 | 2 bukó backend teszt | P1 | = 01-es doksi **T1** (NE duplikáld) |
| G3 | CI frontend-lépés javítása | P1 | — |
| TD1 | endpoint-integrációs tesztek | P2 | 01/T2, 01/T6 után érdemes |
| TD2 | Respawn-döntés | P3 | — |
| TD3 | coverage-mérés | P3 | — |
| TD4 | @security e2e készlet | P2 | 01/T3 után |
| TD5 | Playwright smoke a CI-ban + QA-doksi frissítés | P2 | G3 |
| TD6 | 📋-forgatókönyvek backlogra | P3 | — |

**NE nyúlj hozzá (jó állapot):** kontrakt-doksi ↔ teszt fegyelem
(ClassifyEmail/DailyDigest/tool-calling tesztek); ✅-jelölésű ui-forgatókönyvek;
xUnit + NSubstitute + FluentAssertions konvenciók, AAA, `Method_State_Expected` nevek.

---

## G1 — Privacy „RED GATE" javítása

**Cél:** a Makefile és a security-workflow létező tesztekre mutasson, és a
privacy-teszteknek legyen szűrhető kategóriája.
**Döntés: NEM hozunk létre új tesztprojektet** — a meglévő
`tests/FamilyOs.Infrastructure.Tests/Ai/AiProviderPrivacyGuardTests.cs`
osztályra irányítjuk a kaput, trait-tel. (Új projekt = solution-/CI-átszervezés,
aránytalan a nyereséghez.)
**Ág:** `fix/privacy-gate` · **Commit:** `fix(ci): privacy gate letezo tesztekre iranyitasa`
**Modell:** sonnet.

**Olvasd el először:**
- `Makefile` (`test-privacy` target)
- `.github/workflows/security.yml` (23. sor környéke)
- `tests/FamilyOs.Infrastructure.Tests/Ai/AiProviderPrivacyGuardTests.cs` (mind az 5 teszt)
- `docs/security-privacy.md` §13.3 (mit ígér a kapu)

**Lépések:**
1. `AiProviderPrivacyGuardTests` minden tesztmetódusára (vagy osztály-szintre):
   `[Trait("Category", "PrivacyAssertion")]`.
2. `Makefile` `test-privacy`:
   `dotnet test tests/FamilyOs.Infrastructure.Tests --filter "Category=PrivacyAssertion"`.
3. `security.yml`: ugyanez a parancs; a nem létező
   `tests/FamilyOs.Infrastructure.Ai.Tests/` hivatkozás törlése.
4. Ellenőrizd a security.yml TÖBBI lépését is: ha más nem létező útvonalra
   hivatkozik, jelentsd (ne javítsd, ha nem privacy-kapu).
5. `docs/security-privacy.md` §13.3: a kapu leírása a tényleges parancsra frissül.

**Elfogadás:** a filter pontosan az 5 privacy-tesztet futtatja (se 0, se összes).
**Ellenőrzés:**
```bash
dotnet test tests/FamilyOs.Infrastructure.Tests --filter "Category=PrivacyAssertion" --list-tests
# → pontosan 5 teszt listázódik
make test-privacy   # zöld
```
**Tilos:** új tesztprojekt; a privacy-guard tesztek logikájának módosítása;
más workflow-lépések javítása.

---

## G2 — 2 bukó GmailIngestionServiceTests

**= a 01-es doksi T1 kártyája.** NE indíts rá külön feladatot innen; ha a
01/T1 kész, ez a kapu automatikusan zöld. (Döntés ott: a tesztek igazodnak a
jelenlegi — helyes — `LogAsync` szignatúrához.)

---

## G3 — CI frontend-tesztlépés javítása

**Cél:** a CI FE-lépése a tényleges tooling-gal fusson.
**Döntés: npm + vitest** — a repóban `package-lock.json` van (npm), a
`package.json test` scriptje `vitest run`; NEM térünk át pnpm-re.
**Ág:** `fix/ci-frontend-step` · **Commit:** `fix(ci): FE tesztlepes npm+vitest`
**Modell:** haiku (sonnet eszkalációval).

**Olvasd el először:** `.github/workflows/ci.yml` (a teljes FE-job) ·
`frontend/package.json` (scripts + lockfile-típus megerősítése).

**Lépések:**
1. A FE-jobban: `pnpm` → `npm ci` (a `frontend/` munkakönyvtárban), cache-kulcs
   `package-lock.json`-ra.
2. Tesztparancs: `npm test` — a `--watch=false --browsers=ChromeHeadless`
   Karma-flagek TÖRLÉSE (a vitest run enélkül is nem-interaktív).
3. Ha a jobban `pnpm/action-setup` vagy pnpm-cache lépés van, cseréld
   `actions/setup-node` npm-cache-re.
4. Nézd át a ci.yml többi FE-hivatkozását (build-lépés, lint): ha azok is
   pnpm-et hívnak, azokat is npm-re — a CI legyen konzisztens.

**Elfogadás:** a workflow-fájl nem tartalmaz pnpm-hivatkozást és Karma-flaget.
**Ellenőrzés:** lokálisan `cd frontend && npm ci && npm test` zöld;
push után a CI-futás zöld (a zárójelentésben a run-linkkel).
**Tilos:** tesztek módosítása; backend CI-lépések átírása; pnpm-migráció.

---

## TD1 — Endpoint-integrációs tesztek pótlása

**Cél:** a coding-standards.md §8.1 („minden endpoint happy + 1 hibaút")
adósságának csökkentése a legkockázatosabb moduloknál.
**Döntés — kétlépcsős:** most CSAK a **tool-calls** és **reminders** modulok
kapnak integrációs tesztet (állapotváltós, jogosultság-érzékeny végpontok);
a többi hiányzó modul (notifications, family, users, tags, topics, dashboard,
audit-log, ai-jobs, ai-providers, sources, settings) backlog-tételként
rögzül. A coding-standards §8.1-et NEM enyhítjük — a backlog zárja majd.
**Ág:** `test/endpoint-integration-toolcalls-reminders` · **Commit:** `test(api): tool-calls es reminders integracios tesztek`
**Modell:** sonnet. **Függ:** érdemes 01/T2 (idempotencia) és 01/T6 (rate limit)
merge UTÁN futtatni, hogy a middleware-lánc végleges legyen.

**Olvasd el először:**
- egy meglévő integrációs tesztosztály (pl. a documents-upload vagy tasks
  integrációs teszt) — fixture-használat, auth-szimuláció mintája
- `FamilyOsTestFixture` (Testcontainers-setup)
- a tool-calls és reminders modulok route-definíciói + handler-eik
- ADR-0011 (tool-calling folyamat — confirm/reject szemantika)

**Lépések:**
1. **Tool-calls** tesztek (a meglévő fixture-mintával):
   - happy: proposal létrehozás → confirm → 2xx, a művelet végrehajtódott;
   - hibaút 1: lejárt/érvénytelen token → elutasítás (a kód szerinti státusz);
   - hibaút 2: replay (ugyanaz a confirm kétszer) → második elutasítva;
   - RBAC: Child-szereppel tiltott tool-hívás → 403.
2. **Reminders** tesztek:
   - happy: reminder létrehozás → listázás → resolve → állapot ellenőrzés;
   - hibaút: más családtag privát reminderének resolve-ja → 403/404
     (a kód tényleges viselkedése szerint — ELŐBB nézd meg, mit csinál);
   - validációs hibaút: múltbeli due-date vagy hiányzó kötelező mező → 400
     ProblemDetails.
3. `docs/backlog.md`-be a maradék modul-lista egy sorban, „endpoint-integrációs
   teszt hiányzik" jelöléssel.

**Elfogadás:** legalább 4 tool-calls + 3 reminders integrációs teszt zöld;
egyik sem flaky (3 egymás utáni futásban zöld).
**Ellenőrzés:** `dotnet test --filter "FullyQualifiedName~Integration" ` zöld ×3.
**Tilos:** produkciós kód módosítása (ha a teszt hibát talál a kódban:
ÁLLJ MEG, jelentsd — az külön kártya lesz); a többi modul tesztjeinek megírása.

---

## TD2 — Respawn-döntés (coding-standards §8.3)

**Döntés: doksi igazodik** — a Respawn bevezetése a jelenlegi fixture-be nem
éri meg a kockázatot, amíg a tesztek nem flaky-k; a tényleges izolációs
stratégiát dokumentáljuk.
**Ág:** WP-A doksi-ágra fűzhető vagy `docs/test-isolation` · **Modell:** haiku.

**Lépések:**
1. Nézd meg a `FamilyOsTestFixture`-t: MI a tényleges izoláció (konténer-
   újrahasznosítás? tranzakció? adatnév-prefixek?). NE találgass — a kódból írd le.
2. `docs/coding-standards.md` §8.3 átírása a tényleges stratégiára; a Respawn
   „opció, ha flaky-vá válik a készlet" megjegyzéssel marad említve.

**Elfogadás:** a §8.3 igaz állítást tesz. **Tilos:** fixture-módosítás.

---

## TD3 — Coverage-mérés bevezetése (warning-first)

**Döntés: mérés igen, bukó kapu még nem** — először láthatóság, küszöb-
enforcement csak akkor, ha a számok stabilak. A doksi-célok (Domain ≥85%,
Application ≥75%, Infrastructure ≥50%) maradnak, „mérve, még nem enforced"
jelöléssel.
**Ág:** `ci/coverage-report` · **Commit:** `ci: coverage gyujtes es riport (warning-first)`
**Modell:** sonnet.

**Lépések:**
1. CI backend-tesztlépés: `dotnet test --collect:"XPlat Code Coverage"`.
2. `reportgenerator` lépés (dotnet tool): összesített riport + a job-summary-be
   írt táblázat (Domain/Application/Infrastructure százalékok).
3. A lépés `continue-on-error: false`, de küszöb-ellenőrzés NINCS — csak riport.
4. `docs/coding-standards.md` §8.1 coverage-bekezdés: „mérve a CI-ban;
   küszöb-enforcement bevezetése külön döntés".

**Elfogadás:** CI-futás job-summary-jében látszanak a rétegenkénti számok.
**Tilos:** bukó küszöb beállítása; tesztek írása a számok javítására.

---

## TD4 — @security e2e regressziós készlet

**Cél:** a coding-standards §8.1-ben előírt `@security` tag-készlet létrehozása.
**Döntés — minimum-készlet (mockolt réteg, `frontend/e2e/`), 3 spec-terület:**
RBAC-elutasítások · test-login Production-tiltás · IsPrivate láthatóság.
**Ág:** `test/security-e2e-suite` · **Commit:** `test(e2e): @security regresszios keszlet`
**Modell:** sonnet. **Függ:** 01/T3 (test-login 404 Production-ben) merge után —
a spec a VÉGLEGES viselkedést tesztelje.

**Olvasd el először:** 2-3 meglévő spec a `frontend/e2e/` alatt (mock-minta,
tag-konvenció: hogyan van a `@smoke` téve) · `full_tests/specs/` egy spec-je
(hátha a test-login tiltás oda való — döntsd el a minta alapján, és jelezd).

**Lépések:**
1. `rbac.security.spec.ts` (vagy a repo elnevezési mintája szerint):
   Child-szerepű user admin-oldalra navigál → elutasítás/redirect; Child
   tiltott műveletgombjai nem jelennek meg (a meglévő rbac-spec kiegészítése
   is elfogadható, ha van — akkor oda kerül a @security tag).
2. Test-login spec: Production-flag melletti test-login kísérlet → 404
   (ez full-stack viselkedés; ha mockolt rétegben nem tesztelhető
   értelmesen, a `full_tests/`-be írd, és a mockolt rétegben hagyj ki —
   dokumentáld a választást a spec-kommentben).
3. IsPrivate spec: másik felhasználó privát jegyzete/dokumentuma nem jelenik
   meg listában és direkt URL-en sem.
4. Minden új/megjelölt teszt title-jében `@security` tag; a Playwright-config
   grep-pel futtatható: `npx playwright test --grep @security`.

**Elfogadás:** `--grep @security` legalább 3 spec-fájl tesztjeit futtatja, zölden.
**Tilos:** produkciós kód módosítása; @smoke készlet átszervezése.

---

## TD5 — Playwright smoke a CI-ban + ui-test-scenarios.md frissítés

**Cél:** a doksi „smoke minden PR-en fut" állítása legyen igaz; a QA-doksi
státuszai tükrözzék a valóságot (02-es doksi D2–D3 tételei is itt záródnak).
**Döntés: a mockolt réteg (`frontend/e2e/`) @smoke készlete kerül a CI-ba**
(nem igényel backend-stacket); a full-stack réteg CI-integrációja backlog.
**Ág:** `ci/playwright-smoke` · **Commit:** `ci: playwright @smoke lepes + qa doksi szinkron`
**Modell:** sonnet. **Függ:** G3 (a FE-lépés már npm-es legyen).

**Olvasd el először:** `.github/workflows/ci.yml` · `frontend/e2e/` playwright
config (webServer-beállítás — mockolt réteg hogyan indul) ·
`docs/qa/ui-test-scenarios.md` (3., 5., 6. szakasz).

**Lépések:**
1. CI-ba új job vagy lépés: `npx playwright install --with-deps chromium` +
   `npx playwright test --grep @smoke` a `frontend/` alatt; artifactként a
   playwright-report feltöltése hibánál.
2. `ui-test-scenarios.md` frissítése:
   - 3. szakasz állapottáblája: search/tasks/deadlines/notifications →
     tényleges státusz (✅ spec-elt);
   - 6. szakasz 🚫-jelölések javítása; az új specek (tasks/deadlines/search/
     notifications/tool-calling) forgatókönyveinek visszadokumentálása
     rövid, táblázatos formában (spec-fájlnévvel);
   - §2: a full-stack réteg „jövőbeli" helyett „megvalósult (`full_tests/`)";
     a test-login ADR-hivatkozás: ADR-0018 (01-es doksi T3 hozza létre).
3. UC-lefedési tábla frissítése: UC-01/02/04/05/06 ✅; UC-03 „közvetett —
   explicit spec backlog"; UC-07 „feature 501-stub, nem tesztelhető";
   UC-08 „Gmail e2e — feature-függő, 04-es doksi CR-jeihez kötve".

**Elfogadás:** CI zöld a smoke-lépéssel · a doksi állításai igazak.
**Tilos:** új forgatókönyvek automatizálása (az TD6); full-stack CI-integráció.

---

## TD6 — 📋-forgatókönyvek backlogra

**Cél:** a dokumentált-de-nem-automatizált forgatókönyvek priorizált listája
kerüljön a backlogba — most NEM íródnak meg.
**Ág:** doksi-ág · **Modell:** haiku.

**Lépések:**
1. `docs/backlog.md`-be szakasz „E2E-automatizálási adósság", priorizálva:
   - P2: notes CRUD (QA-H1-01..04) — data-testid pótlás is kell;
   - P2: SignalR-frissítés (QA-D11-01) — full-stack rétegbe;
   - P3: topics (QA-I2-01..03), admin szűrés/export (QA-J1..J3);
   - P3: Gmail-integrations UI (QA-K1), dashboard üres állapotok (QA-L1-02..03);
   - P3: toast/offline (QA-A4, A5).
2. Hivatkozás a `ui-test-scenarios.md` 5. szakasz data-testid hiánylistájára.

**Elfogadás:** backlog-szakasz létezik, minden 📋-tétel szerepel prioritással.
**Tilos:** spec-írás; data-testid-ek felvétele.

---

## Végrehajtási sorrend

1. **G1 + G3** párhuzamosan (fél nap, azonnali kapu-hitelesség) — G2 a 01/T1-gyel jön.
2. **TD5** (G3 után) + a 02-es doksi A-DOC kártyájával egy időben futhat.
3. **TD1** (01/T2+T6 után) és **TD4** (01/T3 után).
4. **TD3**, majd **TD2 + TD6** (olcsó doksi/backlog-zárások) bármikor.

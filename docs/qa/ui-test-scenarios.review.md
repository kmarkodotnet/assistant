# Review — qa/ui-test-scenarios.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)
> A hivatkozott Makefile targetek (`fe-e2e`, `fe-e2e-all`) és pnpm scriptek
> (`e2e:smoke`, `e2e:ui`) létezését a repóban ellenőriztem — megvannak.

## Összegzés

A docs-készlet egyik legjobb darabja: a forgatókönyvek a **tényleges**
frontend-kód ellen vannak kalibrálva (automatizáltsági státusz,
data-testid leltár, „nem tesztelhető” jelölés a stub-oldalakra), és a
QA-munka közben talált valódi termékhibát (QA-C4-BUG) is dokumentálja.
A mock-rétegű vs. full-stack E2E szétválasztás és a dev-login ADR-igény
jelzése helyes döntés.

## Észrevételek

### 1. QA-C4-BUG — jól dokumentált, de gazdátlan (közepes, folyamat)
A Szöveg-tab „önmagát kizáró feltétel” hibája (a `docText` csak a
Szerkesztem gombtól töltődik, ami csak betöltött `docText`-nél látszik)
pontos és értékes lelet. Viszont egy QA-doksi 6. szakaszában el fog
süllyedni: kell belőle backlog-item / issue (a C3–C4 story-k AC-je
jelenleg nem teljesül), különben a `/build-product` QA-fázisáig senki
nem javítja.

### 2. Nem létező endpoint a forgatókönyvben (kicsi)
QA-G6-01: `GET /api/v1/notifications/unread-count` — az api-design.md
14. szakasza csak `GET /api/v1/notifications?onlyUnread=true`-t definiál,
unread-count endpoint nincs. Vagy a FE valóban ilyet hív (akkor az
api-design.md frissítendő), vagy a forgatókönyv igazítandó.

### 3. roleGuard viselkedés eltér a tervtől (kicsi)
QA-B1-05 / QA-J-RBAC-01: elutasításkor „visszairányít a `/`-ra +
hibaüzenet” — a frontend-structure.md 3.3 szerint viszont „403 oldal”
a terv. A megvalósult viselkedés (redirect + toast) teljesen jó, csak a
frontend-structure.md-t kell hozzáigazítani, hogy a két doksi ne
mondjon ellent.

### 4. Child-szerepkör tesztjei a lezáratlan RBAC-politikára épülnek (kicsi)
A 4. szakasz `childUser`-e „ugyanaz mint Adult, de a szerkesztő gombok
rejtve” — ez a search-strategy-féle (megengedő) értelmezés, miközben a
product-vision explicit megosztáshoz kötné a child-láthatóságot (lásd
security-privacy.review.md #1). Amint a politika eldől, a QA-H1-04 és a
child-fixture forgatókönyvei bővítendők route-/adat-szintű esetekkel
(nem csak gomb-rejtésre).

### 5. Apróságok
- A szelektor-leltárban szerepel `settings-tab-system`, de a 3. szakasz
  route-táblájában nincs `/settings/system` sor — egyeztetendő.
- A 2. szakasz dev-login (`POST /api/v1/auth/dev-login`) ADR-igény
  jelzése helyes — ez a hiányzó láncszem a coding-standards.md 8.4
  („mock OAuth”) és a frontend-structure.md 14.2 tervéhez is; érdemes
  mielőbb ADR-0005-ként megírni.
- A „ne írj Playwright specet a stub-oldalakra” szabály (6/E, F) jó
  fegyelem — megelőzi az eldobandó tesztek írását.

## Erősségek (megőrzendő)

- Automatizáltsági státusz (✅/📋/🚫) forgatókönyvenként — azonnal
  látszik a lefedettségi rés.
- data-testid leltár + hiányzó testid-k listája konvencióval.
- A mock-réteg (PR-enként) vs. full-stack (nightly) szétválasztás
  költségtudatos.
- Valódi bugtalálat dokumentálása regresszióvédő teszttel.

## Verdikt

Kiadható; a #1 bug backlog-ba emelése és a #2–#3 apró szinkronok után
ez a doksi lehet a QA-fázis vezérfonala.

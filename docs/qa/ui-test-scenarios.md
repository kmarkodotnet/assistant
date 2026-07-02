# UI tesztelési forgatókönyvek — Family OS

> Státusz: DRAFT v0.1 · Dátum: 2026-07-02 · Nyelv: magyar
> Kapcsolódó: [mvp-backlog.md](../mvp-backlog.md), [frontend-structure.md](../frontend-structure.md),
> [architecture.md](../architecture.md)
> Implementáció: [frontend/e2e/](../../frontend/e2e/) (Playwright)

## 1. Cél és hatókör

Ez a dokumentum a Family OS frontendjének automatizált UI-tesztelési
forgatókönyveit gyűjti össze, epiconként (A–M, lásd `mvp-backlog.md`),
Given/When/Then formában. Célja kettős:

1. **QA-referencia** — minden user-flow-hoz explicit elfogadási forgatókönyv,
   függetlenül attól, hogy van-e hozzá jelenleg futtatható teszt.
2. **Playwright-forrás** — a "Golden path" jelölésű forgatókönyvek konkrét
   `.spec.ts` fájlokká lettek alakítva a `frontend/e2e/` alatt (lásd 7. szakasz).

Minden forgatókönyv tartalmazza az **automatizáltsági státuszát**:

| Jelölés | Jelentés |
|---|---|
| ✅ | Van hozzá futó Playwright teszt (`frontend/e2e/...`) |
| 📋 | Dokumentálva, de még nincs implementálva (nincs `data-testid` és/vagy backend-fixture kell hozzá) |
| 🚫 | **Nem tesztelhető** — a UI-feature még nincs megépítve (lásd 3. szakasz) |

## 2. Tesztstratégia és rétegek

A frontend jelenleg HttpOnly session-cookie-s Google OAuth-ot használ
(`A3` story), ami böngészőben futó Playwright-tesztből nem hívható valódi
Google-fiók nélkül. Ezért két réteget különböztetünk meg:

1. **UI-réteg (ez a dokumentum + `frontend/e2e/`)** — a böngésző és az Angular
   alkalmazás közötti viselkedést teszteli. A backend hívásokat
   `page.route()`-tal **mockoljuk** (rögzített JSON válaszok), így a tesztek
   gyorsak, determinisztikusak, és nem igényelnek futó Postgres/Ollama/API
   konténert. Ez felel meg szó szerint az "automatizált UI tesztelés"
   kérésnek, és ez fut CI-ban minden PR-en.
2. **Full-stack E2E-réteg (jövőbeli, `M4`/`J` hardening fázis)** — a teljes
   `docker-compose` stack ellen fut (valós API, valós DB), a `qa-playwright`
   agent felelőssége a CLAUDE.md workflow QA fázisában. Ehhez egy
   teszt-only login bypass endpoint szükséges a backendben (pl.
   `POST /api/v1/auth/dev-login` csak `Development` környezetben) — ezt
   **ADR-igénylő döntésként** kell rögzíteni, jelen dokumentum nem dönt
   helyette.

A jelen dokumentum és a hozzá tartozó spec-ek kizárólag az **1. (UI) réteget**
fedik le.

## 3. Implementációs állapot — mit lehet ma tesztelni

A frontend forráskód alapján (2026-07-02):

| Terület | Route | FE státusz |
|---|---|---|
| Auth / login | `/login` | ✅ kész |
| Dashboard | `/` | ✅ kész |
| Dokumentumok (lista, upload, részletek, szöveg-szerkesztés) | `/documents*` | ✅ kész |
| Családtagok (admin) | `/family` | ✅ kész |
| Emlékeztetők | `/reminders` | ✅ kész |
| AI javaslatok jóváhagyása | `/suggestions` | ✅ kész |
| Jegyzetek | `/notes` | ✅ kész |
| Témák (admin) | `/topics` | ✅ kész |
| Beállítások — Személyes | `/settings/preferences` | ✅ kész |
| Beállítások — Integrációk (Gmail) | `/settings/integrations` | ✅ kész |
| Beállítások — AI providerek | `/settings/ai-providers` | ✅ kész |
| Beállítások — Mentések | `/settings/backup` | ✅ kész |
| Admin — Audit log | `/admin/audit` | ✅ kész |
| Admin — AI jobs | `/admin/jobs` | ✅ kész |
| Admin — Biztonsági események | `/admin/security-events` | ✅ kész |
| **Feladatok** | `/tasks` | 🚫 **"Hamarosan" stub** — nincs UI (F1) |
| **Határidők** | `/deadlines` | 🚫 **"Hamarosan" stub** — nincs UI (F2) |
| **Kereső / Q&A** | `/search` | 🚫 **"Hamarosan" stub** — nincs UI (E1–E6) |
| Dokumentum-részletek AI-összefoglaló/tag-blokk | `/documents/:id` "Áttekintés"/"Címkék" tab | 🚫 placeholder szöveg, nincs valós adat (C3 D-/I-függő rész) |

A `tasks.page.ts`, `deadlines.page.ts`, `search.page.ts` fájlok jelenleg egy
"`common.comingSoon`" feliratú placeholder komponensek — ezekre **nem**
írunk automatizált forgatókönyvet, amíg a FE nem készül el. A 6. szakasz
ennek megfelelően E és F epicnél csak 🚫 jelölésű, dokumentált (de nem
implementált) forgatókönyveket tartalmaz.

## 4. Szerepkörök és teszt-identitások

A `CurrentUserDto.role` három érték egyike: `Admin`, `Adult`, `Child`.
A mockolt tesztekhez három fixture-identitást használunk
(`frontend/e2e/fixtures/users.ts`):

| Fixture | Szerepkör | Hozzáférés |
|---|---|---|
| `adminUser` | `Admin` | Minden route, beleértve `/family`, `/admin/*` |
| `adultUser` | `Adult` | Minden route **kivéve** `/family`, `/admin/*` (roleGuard 403-szerű elutasítás) |
| `childUser` | `Child` | Ugyanaz mint Adult, de a jegyzet/dokumentum szerkesztő gombok rejtve (`isAdult()` guard a template-ekben) |

## 5. Szelektor-leltár (data-testid)

A meglévő `data-testid` attribútumok listája (forrás: `frontend/src/app`
grep, 2026-07-02) — ez a forrása a spec-ekben használt szelektoroknak:

**Layout / globális:** `navbar-bell`, `navbar-bell-badge`, `navbar-logout`,
`bottom-dashboard`, `bottom-search`, `bottom-suggestions`, `bottom-reminders`,
`bottom-documents`, `offline-overlay`, `offline-retry`, `offline-logout`,
`toast-message`, `toast-dismiss`, `confirm-dialog-overlay`, `confirm-cancel`,
`confirm-ok`, `suggestion-approve-all`, `suggestion-reject-all`.

**Auth:** `login-google-btn`.

**Dokumentumok:** `documents-upload-btn`, `documents-dropzone`,
`documents-file-input`, `doc-card-{id}` (dinamikus), `doc-card-title`,
`doc-card-delete`, `upload-done-btn`, `detail-title`,
`detail-tab-overview` / `detail-tab-text` / `detail-tab-tags`,
`detail-text-editor`, `detail-text-edit`, `detail-text-save`.

**Családtagok:** `family-add-btn`, `family-form-dialog`,
`family-form-displayName`, `family-form-fullName`, `family-form-relation`,
`family-form-birthDate`, `family-form-cancel`, `family-form-save`,
`family-card-name`, `family-card-edit`, `family-card-delete`.

**Emlékeztetők:** `reminder-acknowledge`, `reminder-snooze`, `reminder-skip`.

**Beállítások:** `settings-tab-preferences`, `settings-tab-system`,
`settings-tab-integrations`, `settings-tab-ai-providers`,
`settings-tab-backup`, `prefs-email-enabled`, `prefs-quiet-start`,
`prefs-quiet-end`, `prefs-save`.

**Hiányzó (📋 jelölésű forgatókönyvekhez javasolt pótlás):** Notes, Topics,
Suggestions, Dashboard, Admin (`audit-log`, `ai-jobs`, `security-events`) és
Settings-Integrations/AI-providers/Backup oldalak jelenleg **nem**
rendelkeznek `data-testid`-vel — ott a forgatókönyvek szöveg/role alapú
Playwright-lokátorra (`getByRole`, `getByText`) támaszkodnak, ami törékenyebb.
Javaslat: ha ezen oldalak stabilizálódnak, kapjanak `data-testid`-t a
`feature-elem-akció` konvenció szerint (lásd meglévő minták fent).

## 6. Forgatókönyvek epiconként

### Epic A — Alapok és infrastruktúra

**QA-A3-01 — Bejelentkezési oldal megjelenik `@smoke` ✅**
- **Given** a felhasználó nincs bejelentkezve
- **When** megnyitja a `/login` oldalt
- **Then** látja a "Bejelentkezés a Family OS-be" feliratot és a Google
  bejelentkezés gombot (`login-google-btn`)
- Automatizálva: `frontend/e2e/smoke/auth-flow.spec.ts`

**QA-A3-02 — Nem bejelentkezett felhasználó átirányítása `@smoke` ✅**
- **Given** nincs érvényes session
- **When** megnyitja a `/` (dashboard) route-ot
- **Then** a router `/login`-ra irányít, `returnUrl` query paraméterrel
- Automatizálva: `frontend/e2e/smoke/auth-flow.spec.ts`

**QA-A3-03 — Bejelentkezett felhasználó nem látja a login oldalt 📋**
- **Given** a `GET /api/v1/auth/me` mockolt válasza egy érvényes `Admin` felhasználó
- **When** megnyitja a `/login` oldalt közvetlenül
- **Then** a shell (navbar + sidebar) jelenik meg dashboard tartalommal
  (a jelenlegi routing ezt nem tiltja explicit — ellenőrizendő, hogy nem
  duplikált UI-t renderel-e; ha igen, backlog-issue nyitandó)

**QA-A3-04 — Kijelentkezés `@smoke` ✅**
- **Given** be van jelentkezve (`adminUser` fixture)
- **When** a navbar `navbar-logout` gombjára kattint
- **Then** a `POST /api/v1/auth/logout` meghívódik, és a router `/login`-ra navigál
- Automatizálva: `frontend/e2e/smoke/auth-flow.spec.ts`

**QA-A4-01 — Globális hiba toast megjelenik 📋**
- **Given** be van jelentkezve, és egy API hívás `application/problem+json`
  400-at ad vissza magyar `detail` mezővel
- **When** a hibás művelet lefut (pl. dokumentum-törlés hálózati hibával mockolva)
- **Then** a `toast-message` testid alatt megjelenik a magyar hibaszöveg, és
  `toast-dismiss`-re eltűnik

**QA-A5-01 — Offline állapot overlay 📋**
- **Given** be van jelentkezve
- **When** a böngésző offline állapotba kerül (`page.context().setOffline(true)`)
- **Then** megjelenik az `offline-overlay`, `offline-retry` gombbal

### Epic B — Családtag- és felhasználó-kezelés

**QA-B1-01 — Családtag létrehozása admin által (Golden path) `@smoke` ✅**
- **Given** `adminUser` be van jelentkezve, üres családtag-lista van mockolva
- **When** a `/family` oldalon a `family-add-btn`-re kattint, kitölti a
  `family-form-displayName` és `family-form-relation` mezőket, majd
  `family-form-save`-re kattint
- **Then** a `POST /api/v1/family-members` meghívódik a helyes payloaddal, a
  dialógus bezáródik, és az új tag megjelenik a listában
- Automatizálva: `frontend/e2e/family/family-flow.spec.ts`

**QA-B1-02 — Kötelező mező validáció 📋**
- **Given** a családtag-form nyitva van
- **When** a `displayName` mezőt üresen hagyja és blur-öl
- **Then** megjelenik "A megjelenítési név kötelező." hibaüzenet, és a
  `family-form-save` gomb `disabled`

**QA-B1-03 — Családtag szerkesztése ✅**
- **Given** legalább egy családtag van a listában
- **When** a `family-card-edit`-re kattint, módosítja a nevet, majd mentés
- **Then** a `PATCH /api/v1/family-members/{id}` meghívódik, a lista frissül
- Automatizálva: `frontend/e2e/family/family-flow.spec.ts`

**QA-B1-04 — Családtag törlése megerősítéssel ✅**
- **Given** legalább egy családtag van a listában
- **When** a `family-card-delete`-re kattint, majd a megerősítő dialógusban
  `confirm-ok`-ra kattint
- **Then** a `DELETE /api/v1/family-members/{id}` meghívódik, a tag eltűnik
  a listából, és sikeres toast jelenik meg
- Automatizálva: `frontend/e2e/family/family-flow.spec.ts`

**QA-B1-05 — RBAC: Adult nem érheti el a `/family`-t (Golden path) `@smoke` ✅**
- **Given** `adultUser` (nem admin) be van jelentkezve
- **When** megpróbálja megnyitni a `/family` URL-t közvetlenül
- **Then** a `roleGuard` visszairányít a `/`-ra, és megjelenik a "Nincs
  jogosultságod ehhez az oldalhoz." hibaüzenet
- Automatizálva: `frontend/e2e/rbac/role-guard.spec.ts`

**QA-B3-01 — Saját preferenciák mentése (Golden path) `@smoke` ✅**
- **Given** be van jelentkezve, a `/settings/preferences` oldalon van
- **When** bejelöli a `prefs-email-enabled` checkboxot, beállítja a
  `prefs-quiet-start`/`prefs-quiet-end` mezőket `22:00`/`07:00`-ra, majd
  `prefs-save`-re kattint
- **Then** a `PATCH /api/v1/auth/me/preferences` meghívódik a helyes
  payloaddal, és "Beállítások mentve." toast jelenik meg
- Automatizálva: `frontend/e2e/settings/preferences-flow.spec.ts`

### Epic C — Dokumentum-kezelés

**QA-C1-01 — Dokumentum feltöltése (Golden path) `@smoke` ✅**
- **Given** be van jelentkezve, a `/documents/upload` oldalon van
- **When** egy fájlt kiválaszt a `documents-file-input` inputon keresztül
- **Then** a feltöltési kártyán "Feltöltés..." majd "Kész" badge jelenik meg,
  és a `upload-done-btn` gombra kattintva visszairányít a `/documents`-re
- Automatizálva: `frontend/e2e/documents/documents-flow.spec.ts`

**QA-C1-02 — Duplikátum-figyelmeztetés 📋**
- **Given** a mockolt upload válasz 409-et ad `existingId`-vel
- **When** ugyanazt a fájlt tölti fel másodszor
- **Then** "Ez a fájl már létezik." üzenet jelenik meg, "Megnyitom a
  meglévőt →" linkkel

**QA-C1-03 — Nem támogatott fájltípus elutasítása 📋**
- **Given** a mockolt upload válasz 415-öt ad
- **When** egy `.exe` fájlt próbál feltölteni
- **Then** hiba badge és magyar hibaüzenet jelenik meg a kártyán

**QA-C2-01 — Dokumentumlista üres állapota ✅**
- **Given** a `GET /api/v1/documents` mockolt válasza üres tömb
- **When** megnyitja a `/documents` oldalt
- **Then** az "Nincsenek dokumentumok" empty-state jelenik meg
- Automatizálva: `frontend/e2e/documents/documents-flow.spec.ts`

**QA-C2-02 — Dokumentumlista tételekkel (Golden path) `@smoke` ✅**
- **Given** a mockolt lista 2 dokumentumot tartalmaz különböző
  `processingStatus` értékekkel
- **When** megnyitja a `/documents` oldalt
- **Then** mindkét dokumentum-kártya látszik a megfelelő státusz-badge-dzsel
- Automatizálva: `frontend/e2e/documents/documents-flow.spec.ts`

**QA-C2-03 — Dokumentum törlése megerősítéssel ✅**
- **Given** legalább egy dokumentum van a listában
- **When** a `doc-card-delete`-re kattint, majd `confirm-ok`-ra
- **Then** a `DELETE /api/v1/documents/{id}` meghívódik, a kártya eltűnik
- Automatizálva: `frontend/e2e/documents/documents-flow.spec.ts`

**QA-C3-01 — Dokumentum részletek — tab-váltás ✅**
- **Given** egy dokumentum részletei mockolva vannak
- **When** megnyitja a `/documents/{id}` oldalt, majd a `detail-tab-text`
  tabra kattint
- **Then** a `detail-title` látszik, és a Szöveg tab a jelenlegi
  implementáció szerint mindig az "A szöveg kinyerése folyamatban van."
  üzenetet mutatja (lásd `QA-C4-BUG` lentebb)
- Automatizálva: `frontend/e2e/documents/documents-flow.spec.ts`

**QA-C4-BUG — 🐞 Hiba: a kinyert szöveg soha nem tölthető be/szerkeszthető 🚫**
- **Megfigyelés:** `document-detail.page.ts` Szöveg tab template-je:
  `docText()` csak akkor jelenik meg (és csak akkor van "Szerkesztem" gomb),
  ha `docText()` már nem `null` — de az egyetlen hely, ami `docText`-et
  feltölti, a `startEditText()`, amit épp az a "Szerkesztem" gomb hív meg,
  ami csak akkor látszik, ha `docText()` már nem `null`. Ez egy önmagát
  kizáró feltétel: **nincs belépési pont**, a felhasználó soha nem juthat
  el a szöveg megtekintéséhez vagy szerkesztéséhez (`C3` "kinyert szöveg"
  és `C4` "szöveg-kézikorrekció" AC-k nem teljesülnek a UI-n, annak
  ellenére, hogy a backend endpointok és a `detail-text-editor`/
  `detail-text-save` markup megvan).
- **Javasolt javítás:** egy explicit "Szöveg betöltése" gomb vagy
  `ngOnInit`/tab-váltáskor automatikus `docText` betöltés a "folyamatban
  van" ág helyett, amikor a dokumentum `processingStatus = Done`.
- **Automatizálási státusz:** a `QA-C4-01` (szöveg-szerkesztés golden path)
  forgatókönyv **nem írható meg**, amíg ez a hiba nincs javítva — a
  jelenlegi, hibás állapotot a `QA-C3-01` teszt rögzíti regresszióvédelemként.

**QA-C3-02 — AI-összefoglaló és címke-blokk placeholder 🚫**
- Jelenleg a "Áttekintés" és "Címkék" tab statikus "hamarosan elérhető"
  szöveget mutat — **nem tesztelhető** valós adattal, amíg a D/I epic FE
  integrációja el nem készül.

### Epic D — AI pipeline

A pipeline háttérfolyamat (Hangfire worker), UI-felülete nincs közvetlenül —
a hatása a dokumentum `processingStatus` badge-en (lásd `QA-C2-02`) és a
`/suggestions` oldalon (lásd `QA-F3`) jelenik meg. Külön UI-forgatókönyv nem
releváns.

**QA-D11-01 — Feldolgozási állapot valós idejű frissítése (SignalR) 📋**
- **Given** egy dokumentum `processingStatus = Extracting` állapotban van
- **When** a mockolt SignalR hub egy `documentProcessingProgress` eseményt
  küld `Done` állapottal
- **Then** a dokumentum-kártya badge-e frissül anélkül, hogy az oldalt
  újratöltenénk
- *Megjegyzés:* SignalR mock Playwright-ban bonyolultabb (WebSocket
  interception) — first candidate a full-stack E2E rétegre.

### Epic E — Kereső és Q&A

**QA-E1..E6 — Kereső oldal 🚫**
- A `/search` route jelenleg egy "hamarosan elérhető" placeholder
  (`search.page.ts`). Az `mvp-backlog.md`-ben leírt filter/FTS/szemantikus/
  hibrid/Q&A/intent forgatókönyvek (E1–E6) **dokumentálásra várnak**, amint
  a FE elkészül. Ne írj rájuk Playwright specet korábban — a szelektorok
  úgyis változni fognak.

### Epic F — Feladatok és határidők

**QA-F1, F2 — Task/Deadline oldalak 🚫**
- A `/tasks` és `/deadlines` route jelenleg "hamarosan elérhető" placeholder.
  Lásd E epic megjegyzését — azonos okból blokkolt.

**QA-F3-01 — AI-javaslatok jóváhagyása egyenként (Golden path) `@smoke` ✅**
- **Given** `GET /api/v1/suggestions` mockolt válasza 1 feladat- és 1
  határidő-javaslatot tartalmaz
- **When** megnyitja a `/suggestions` oldalt, majd az egyik javaslat sorban
  az "Elfogad" gombra kattint (`ui-suggestion-block` belső gombja)
- **Then** a `POST /api/v1/suggestions/batch` meghívódik `{approve: {...}}`
  törzzsel, és "1 elfogadva, 0 elutasítva." toast jelenik meg
- Automatizálva: `frontend/e2e/suggestions/suggestions-flow.spec.ts`

**QA-F3-02 — Összes javaslat elutasítása egy blokkban ✅**
- **Given** a javaslat-blokk több tag-javaslatot tartalmaz
- **When** a `suggestion-reject-all` gombra kattint
- **Then** a `POST /api/v1/suggestions/batch` `{reject: {...}}` törzzsel
  meghívódik az összes tag-javaslat id-jével
- Automatizálva: `frontend/e2e/suggestions/suggestions-flow.spec.ts`

**QA-F3-03 — Üres állapot ✅**
- **Given** a mockolt válasz `totalCount: 0`
- **When** megnyitja a `/suggestions` oldalt
- **Then** "Nincs jóváhagyásra váró javaslat." üzenet látszik
- Automatizálva: `frontend/e2e/suggestions/suggestions-flow.spec.ts`

### Epic G — Emlékeztetők

**QA-G3-01 — Emlékeztető nyugtázása "Kész" gombbal (Golden path) `@smoke` ✅**
- **Given** a `GET /api/v1/reminders` mockolt válasza egy `Fired` állapotú
  emlékeztetőt ad a `now` csoportban
- **When** a `reminder-acknowledge` gombra kattint
- **Then** a `POST /api/v1/reminders/{id}/acknowledge` meghívódik, "Emlékeztető
  nyugtázva" toast jelenik meg, és a lista újratöltődik
- Automatizálva: `frontend/e2e/reminders/reminders-flow.spec.ts`

**QA-G3-02 — Emlékeztető halasztása ✅**
- **Given** ugyanaz mint fent
- **When** a `reminder-snooze` gombra, majd a felugró listából "1 óra"-ra
  kattint
- **Then** a `POST /api/v1/reminders/{id}/snooze` `{snoozeMinutes: 60}`
  törzzsel meghívódik
- Automatizálva: `frontend/e2e/reminders/reminders-flow.spec.ts`

**QA-G3-03 — Emlékeztető mellőzése ✅**
- **When** a `reminder-skip` gombra kattint
- **Then** a `POST /api/v1/reminders/{id}/skip` meghívódik, info toast
  jelenik meg
- Automatizálva: `frontend/e2e/reminders/reminders-flow.spec.ts`

**QA-G3-04 — Üres állapot ✅**
- **Given** minden csoport (missed/now/week/later) üres
- **When** megnyitja a `/reminders` oldalt
- **Then** a "Nincsenek emlékeztetők" üzenet látszik
- Automatizálva: `frontend/e2e/reminders/reminders-flow.spec.ts`

**QA-G6-01 — Navbar csengő jelvény olvasatlan számmal 📋**
- **Given** a `GET /api/v1/notifications/unread-count` mockolt válasza
  `{totalCount: 3}`
- **When** bármelyik oldal betöltődik
- **Then** a `navbar-bell-badge` "3"-at mutat

### Epic H — Jegyzetek (Notes)

**QA-H1-01 — Jegyzet létrehozása (Golden path) 📋**
*(nincs `data-testid` a Notes oldalon — role/text lokátorral írható meg)*
- **Given** `adultUser` be van jelentkezve, a `/notes` oldalon van
- **When** az "+ Új feljegyzés" gombra kattint, kitölti a Cím és Tartalom
  mezőket, majd "Mentés"-re kattint
- **Then** a `POST /api/v1/notes` meghívódik, "Feljegyzés létrehozva." toast
  jelenik meg, a dialógus bezárul, az új jegyzet megjelenik a listában

**QA-H1-02 — Jegyzet megtekintése és szerkesztésre váltás 📋**
- **Given** legalább egy jegyzet van a listában
- **When** a jegyzet címére kattint (megnyílik nézet módban), majd
  "Szerkesztés"-re kattint
- **Then** a form mezők a jegyzet aktuális adataival töltődnek fel

**QA-H1-03 — Jegyzet törlése natív confirm dialógussal 📋**
- **Given** legalább egy jegyzet van
- **When** "Törl." linkre kattint, és a böngésző natív `confirm()`
  dialógusát elfogadja (`page.on('dialog', d => d.accept())`)
- **Then** a `DELETE /api/v1/notes/{id}` meghívódik
- *Megjegyzés:* natív `confirm()` használata inkonzisztens a többi oldal
  `ui-confirm-dialog` komponensével — érdemes egységesíteni egy backlog-item
  erejéig.

**QA-H1-04 — Child szerepkör nem lát szerkesztő gombokat 📋**
- **Given** `childUser` be van jelentkezve
- **When** megnyitja a `/notes` oldalt
- **Then** az "+ Új feljegyzés" gomb és a Szerk./Törl. linkek nem
  jelennek meg (csak olvasás)

### Epic I — Tag-ek és Topic-ok

**QA-I2-01 — Témafa megjelenítése behúzással 📋**
- **Given** a `GET /api/v1/topics?flat=false` mockolt válasza egy 2 szintű
  fát ad vissza
- **When** admin megnyitja a `/topics` oldalt
- **Then** az altémák nagyobb bal-margóval (`padding-left`) jelennek meg a
  szülőhöz képest

**QA-I2-02 — Új gyökértéma létrehozása (Golden path) 📋**
- **Given** admin be van jelentkezve
- **When** "+ Új gyökér téma"-ra kattint, kitölti Név/Slug mezőket, Mentés
- **Then** a `POST /api/v1/topics` meghívódik, "Téma létrehozva." toast

**QA-I2-03 — Altéma törlése hiba esetén 📋**
- **Given** a mockolt törlés API 409-et ad vissza
- **When** egy témát töröl, aminek van altémája
- **Then** "Nem sikerült törölni. Lehet, hogy altémái vannak." hibaüzenet

**QA-I1-01 — Tag autocomplete 🚫**
- A tag-multiselect komponens `mvp-backlog.md`-ben szerepel, de a
  `frontend/src/app/shared/ui` és `features` mappákban jelenleg nem
  található külön tag-input komponens — **nincs önálló UI-felület
  tesztelésre**, csak a suggestions batch-flow-n keresztül érhető el
  közvetve (lásd `QA-F3`).

### Epic J — Audit és admin felület

**QA-J1-01 — Audit log szűrés dátum és esemény szerint 📋**
*(nincs `data-testid`, role/label lokátor szükséges)*
- **Given** `adminUser` be van jelentkezve, a `/admin/audit` oldalon van
- **When** kitölti a "Dátumtól"/"Dátumig" mezőket, kiválaszt egy "Esemény"
  típust (pl. `Login`), és a szűrés gombra kattint
- **Then** a `GET /api/v1/audit-log` a megfelelő query paraméterekkel
  hívódik, és csak a szűrt sorok jelennek meg

**QA-J2-01 — Audit log CSV export 📋**
- **When** az export gombra kattint
- **Then** a letöltés `?format=csv` paraméterrel indul (Playwright
  `page.waitForEvent('download')`)

**QA-J3-01 — AI jobs admin — státusz szerinti szűrés 📋**
- **Given** a `/admin/jobs` oldal, `Failed` szűrő kiválasztva
- **When** a szűrés lefut
- **Then** csak `Failed` badge-es sorok látszanak, "Automatikus frissítés:
  30 mp" felirat mellett

**QA-J-RBAC-01 — Adult nem érheti el az `/admin`-t (Golden path) `@smoke` ✅**
- **Given** `adultUser` be van jelentkezve
- **When** megnyitja az `/admin/audit` URL-t közvetlenül
- **Then** a `roleGuard` visszairányít `/`-ra "Nincs jogosultságod..."
  hibaüzenettel
- Automatizálva: `frontend/e2e/rbac/role-guard.spec.ts`

### Epic K — Beállítások és integrációk

**QA-K1-01 — Gmail csatlakoztatás állapot megjelenítése 📋**
- **Given** a `GET /api/v1/sources` mockolt válasza egy nem-csatlakoztatott
  Gmail forrást ad
- **When** admin megnyitja a `/settings/integrations` oldalt
- **Then** "Nincs csatlakoztatva" badge és "Csatlakoztasd Gmail fiókodat..."
  szöveg látszik

**QA-K1-02 — Gmail szinkronizálás és leválasztás 📋**
- **Given** a Gmail forrás csatlakoztatva van (mock)
- **When** "Szinkronizálás"-ra majd "Leválasztás"-ra kattint
- **Then** a megfelelő API hívások lefutnak, a badge állapota frissül

**QA-K2-01 — AI provider konfiguráció — PrivacyMode csak olvasható 📋**
- **Given** admin a `/settings/ai-providers` oldalon van
- **Then** látja az "Az adatvédelmi mód a rendszer konfigurációjában van
  rögzítve..." banner szöveget, és nincs szerkeszthető PrivacyMode mező

**QA-K3-01 — Backup infó oldal megjelenik 📋**
- **When** admin megnyitja a `/settings/backup` oldalt
- **Then** a `BackupInfoComponent` tartalma (utolsó mentés időpontja, méret)
  látszik

### Epic L — Dashboard

**QA-L1-01 — Dashboard widget-ek megjelenítése (Golden path) `@smoke` ✅**
- **Given** a `GET /api/v1/dashboard` mockolt válasza tartalmaz 2 közelgő
  határidőt, 1 lejárt emlékeztetőt, összesített javaslat-számokat és 1
  legutóbbi dokumentumot
- **When** megnyitja a `/` oldalt
- **Then** mind a 4 kártya (Közelgő határidők, Lecsúszott emlékeztetők, AI
  javaslatok, Legutóbbi dokumentumok) megjeleníti a mockolt adatokat
- Automatizálva: `frontend/e2e/dashboard/dashboard-flow.spec.ts`

**QA-L1-02 — Dashboard üres állapotok 📋**
- **Given** minden dashboard-mező üres/0
- **Then** minden kártyában a megfelelő "Nincs ..." szöveg látszik

**QA-L1-03 — Navigáció widget linkekről 📋**
- **When** a "Kezelés" linkre kattint az AI javaslatok kártyán
- **Then** a router a `/suggestions`-re navigál

### Epic M — Deployment és üzemeltetés

UI-szempontból nem releváns (infra/Docker/Helm szint) — lásd
`epic-M-deploy-ops.md`. Az egyetlen UI-érintett pont a `/healthz` végpontok,
amik nem böngészős felületek, így itt nincs forgatókönyv.

## 7. Regressziós smoke-készlet

A `@smoke` taggel jelölt forgatókönyvek alkotják a minden PR-en futó gyors
regressziós készletet (cél: < 60 mp). Futtatás:

```
make fe-e2e
# vagy közvetlenül:
cd frontend && pnpm e2e:smoke
```

Az összes forgatókönyv (smoke + bővebb) éjszakai/CI-nightly futáshoz:

```
make fe-e2e-all
# vagy közvetlenül:
cd frontend && pnpm e2e
```

Fejlesztői módban (UI-nézegetővel):

```
cd frontend && pnpm e2e:ui
```

## 8. Ismert korlátok és follow-up

1. **Valós Google OAuth nem tesztelhető** böngészős Playwright-tal — minden
   bejelentkezés utáni forgatókönyv `page.route()`-tal mockolt
   `/api/v1/auth/me` választ használ, nem valódi login flow-t.
2. **Hiányzó `data-testid`-k**: Notes, Topics, Suggestions belső gombjai,
   Dashboard, Admin al-oldalak, Settings-Integrations/AI-providers/Backup —
   lásd 5. szakasz. Amíg ezek nincsenek, a role/text-alapú lokátorok
   törékenyek lehetnek fordítási (i18n) kulcsváltozásra.
3. **SignalR valós idejű frissítések** (`QA-D11-01`) nincsenek Playwright
   route-mockkal egyszerűen szimulálva — a full-stack E2E rétegbe tartozik.
4. **E és F epic UI-ja nincs megépítve** (`/search`, `/tasks`, `/deadlines`)
   — a hozzájuk tartozó, jelenleg 🚫-al jelölt forgatókönyvek a backlog
   `mvp-backlog.md` E1–E6, F1–F2 story-inak elfogadási kritériumaiból lettek
   levezetve, és készen állnak, amint a FE elkészül.
5. **Natív `confirm()` a Notes/Topics oldalon** inkonzisztens a
   `ui-confirm-dialog` komponenssel — UX-egységesítési backlog-item
   javasolt, ami egyúttal a tesztelhetőséget is javítaná.
6. **`QA-C4-BUG`**: a dokumentum-részletek Szöveg tabja jelenleg soha nem
   tölti be a kinyert szöveget (lásd C epic szakasz) — ez terméki hiba,
   nem tesztelési hiányosság, mielőbbi javítást igényel.

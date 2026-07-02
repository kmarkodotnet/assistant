# Review — epic-A-foundation-frontend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Jó FE-alapozó bontás: scaffold → state/auth → oldalvázak → tooling
sorrend logikus, az AC-k konkrétak. Két, a tervező-doksikban nyitott
kérdést itt eldöntöttek (i18n: ngx-translate a T-AFE-11-ben;
`layout/shell.component` a T-AFE-04-ben) — a frontend-structure.md
frissítendő hozzájuk.

## Észrevételek

1. **T-AFE-11 i18n-döntés visszavezetése (kicsi):** az `ngx-translate` +
   `assets/i18n/hu.json` választás feloldja a frontend-structure.md 1.1
   (`@angular/localize`) vs. 11. szakasz kettősségét
   (frontend-structure.review.md #3) — a tervező doksiban is rögzítendő.
2. **T-AFE-07 roleGuard „vagy” (kicsi):** „403 oldalra navigál (vagy
   dashboardra toast-tal)” — döntetlen maradt; a megvalósult viselkedés
   (dashboard + toast, lásd qa/ui-test-scenarios QA-B1-05) legyen a
   normatív, és a frontend-structure.md 3.3 („403 oldal”) igazítandó.
3. **T-AFE-17 husky/commitlint a repo-gyökérben (kicsi):** a `.husky/`
   és `commitlint.config.js` a gyökérbe kerül, miközben a frontend
   csomag alá tartozó pnpm-tooling — monorepóban működik, de a
   telepítési lépés (`pnpm install` a gyökérben? `prepare` script hol?)
   nincs leírva (coding-standards.review.md #4 utolsó pontja).
4. **T-AFE-16 gen:api placeholder** — jó, hogy explicit „csak Fázis 5-től
   futtatható”; ez megelőzi a korai hibás generálást.
5. Apróság: T-AFE-08 a Google Identity Services (GIS) kliens-oldali
   `id_token` flow-t rögzíti — ez az api-design.md 3.1-gyel konzisztens,
   viszont a DELIVERY.md szerver-callback URI-jával nem
   (DELIVERY.review.md #3); a flow-döntést egy helyen kell lezárni.

## Verdikt

Végrehajtásra kész; a döntés-visszavezetések (i18n, roleGuard, auth-flow)
a tervező doksikba a BUILD közben pótlandók.

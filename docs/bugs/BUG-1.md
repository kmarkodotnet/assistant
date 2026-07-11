# BUG-1 — DailyDigest `ActionUrl: "/dashboard"` nincs explicit route-hoz kötve

> Súlyosság: **Alacsony/Kicsi** (a felhasználói folyam végül működik, de a
> mechanizmus véletlenszerű/törékeny, nem szándékos routing)
> Terület: Frontend routing — `frontend/src/app/app.routes.ts` /
> `frontend/src/app/features/notifications/notifications.page.ts` (`open()`)
> Kapcsolódó kontrakt: `docs/contracts/daily-digest-contract.md` §4.4
> (`ActionUrl = "/dashboard"`)

## Lépések a reprodukáláshoz

1. Mock API-val bejelentkezett admin user, `GET /api/v1/notifications?**`
   egy `DailyDigest` típusú elemet ad vissza, `actionUrl: "/dashboard"`.
2. Navigálj a `/notifications` oldalra.
3. Kattints az értesítés elemre (`[data-testid="notification-item"]`).
4. A `NotificationsPage.open()` meghívja
   `this.router.navigateByUrl(n.actionUrl)` — azaz `navigateByUrl('/dashboard')`.

## Elvárt viselkedés

A kontrakt (`daily-digest-contract.md` §4.4) szerint az `ActionUrl` értéke
`"/dashboard"`, ami arra utal, hogy ez egy explicit route, amire a FE
navigálni tud.

## Kapott viselkedés

`frontend/src/app/app.routes.ts`-ben **nincs** `/dashboard` path definiálva —
a dashboard oldal a gyökér (`path: ''`) alatt van regisztrálva
(`frontend/src/app/features/dashboard/dashboard.routes.ts`, `path: ''`).
Amikor `navigateByUrl('/dashboard')` fut, az Angular router nem talál
egyező route-ot a `'dashboard'` szegmensre a `''` gyermek-route-ok között,
ezért a catch-all `{ path: '**', redirectTo: '' }` szabály lép életbe, és a
felhasználó a gyökér (`/`) URL-re landol — ami **véletlenül** ugyanaz a
dashboard oldal, mint amit az `ActionUrl` célzott. Playwright-teszttel
megerősítve: kattintás után `page.url()` = `http://localhost:4200/`
(nem `/dashboard`).

Ez ma működik, mert a wildcard redirect célja is `''`, de ez esetleges: ha a
jövőben a wildcard redirect célja megváltozik, vagy egy másik `ActionUrl`
(pl. jövőbeli digest-bővítés) egy ténylegesen csak URL-ként létező, de
route-ként nem regisztrált útvonalra mutatna, a navigáció rossz oldalra
vinné a felhasználót anélkül, hogy bármilyen hiba látszódna.

## Screenshot / trace

Nincs vizuális hiba (a UI helyesen a dashboardon landol), ezért screenshot
nem releváns; a bizonyíték a Playwright `page.url()` kiértékelése kattintás
után (lásd `frontend/e2e/notifications/notifications-flow.spec.ts`
QA-N-05 teszt, ahol az asszertáció emiatt megengedő regexet használ:
`/\/(dashboard)?$/`).

## Javasolt javítás (nem én implementálom)

Vagy (a) adjunk explicit `{ path: 'dashboard', redirectTo: '', pathMatch:
'full' }` (vagy loadChildren) route-ot az `app.routes.ts`-hez, hogy az
`ActionUrl: "/dashboard"` szándékosan, dokumentáltan működjön, vagy (b) a
kontraktban/`DailyDigestJob`-ban változtassuk az `ActionUrl`-t a ténylegesen
regisztrált gyökér route-ra (`"/"`), hogy a FE-BE szerződés konzisztens
legyen a valós routing-gal.

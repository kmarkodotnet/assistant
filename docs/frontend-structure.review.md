# Review — frontend-structure.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Modern, konzisztens Angular-terv: standalone + signals + facade-minta,
generált API-kliens, SignalR-integráció, LAN-detektálás, jó ASCII
mockupok. A struktúra követhető sablont ad a fejlesztő agenteknek.
Néhány technikai pontatlanság és eldöntetlen kérdés javítandó.

## Hibák / következetlenségek

### 1. `@angular/cdk/calendar` nem létezik (közepes)
8.5: a Deadlines naptár-nézete „`@angular/cdk/calendar`”-ra hivatkozik —
a CDK-ban **nincs** calendar komponens (a Material datepicker-ben van
`mat-calendar`, de a 1.1 explicit kizárja a Material-t). Döntés kell:
saját naptár-grid komponens (Tailwind-del reális), vagy 3rd party
(pl. `angular-calendar`), vagy mégis Material datepicker-modul szelektív
behúzása. Az implementáló agent ezen elakadna.

### 2. Settings UI vs. API: PrivacyMode-választó (közepes)
8.10: az admin Settings oldalon „AI provider config (LocalOnly / Hybrid /
AnyProvider, default LocalOnly)” választó szerepel — az api-design.md 21.2
szerint viszont az MVP-ben az API **minden** nem-LocalOnly értéket 422-vel
elutasít. Egy olyan UI-vezérlő, ami mindig hibát dob, rossz UX; MVP-ben a
mező legyen read-only „LocalOnly (v2-ben bővül)” felirattal. (A mögöttes
politika-ellentmondás: security-privacy.review.md #2.)

### 3. i18n döntés nyitva (kicsi)
1.1: „`@angular/localize` magyar fő nyelvvel”; a 11. szakasz viszont
`@ngx-translate` runtime megoldást javasol MVP-re, „később migrálható”.
A kettő inkompatibilis megközelítés (build-time vs. runtime) — döntsük el
(fix egynyelvű magyar app-nál a legolcsóbb: sima magyar stringek a
kódban + kulcs-fájl nélkül, vagy ngx-translate). Az architecture.md 10
(`assets/i18n/hu.json`) az ngx-translate-tel konzisztens.

### 4. Hiányzó mappák a layoutban (kicsi)
A 3.1 route-konfig `./features/auth/login.page`-t és
`./layout/shell.component`-et importál — a 2. szakasz fájlfájában sem
`features/auth/`, sem `layout/` nem szerepel (a root shell ott
`app.component.ts`). A struktúra-ábra és a kód-minta egyeztetendő.

### 5. E2E auth-mock terve hiányos (kicsi)
14.2: „Auth mockolt, backend Testcontainers” — a Google OAuth kiváltása
E2E-ben nem triviális: kell egy teszt-only auth-út a backendben (pl.
`Testing` environment-ben engedélyezett fake-login endpoint). Ez
biztonsági felület — a security-privacy.md-vel közösen kell megtervezni,
most egyik doksi sem írja le.

### 6. Kisebb észrevételek
- 10.2 breakpoint-jelölés fordítva olvasható („sm < 640px” — a Tailwind
  `sm` a ≥ 640px); a szándék érthető, a jelölés javítandó.
- 12. offline-cache: „az utoljára betöltött dashboard adatok
  read-from-storage” — a 4.5 szerint viszont csak szűrők/UI-prefek
  perzisztálódnak. Ha a dashboard-adat is localStorage-be kerül, az
  érzékeny címeket tartalmazhat — definiálandó, mi cache-elhető
  (a 8.3 sessionStorage-elve jó minta).
- 6.2: a `RealtimeService` példa csak a notifications hubra csatlakozik;
  a documents hub kezelése (két connection? egy multiplexelt?) nyitott.
- 8.4 Kanban: a `Cancelled` státusz oszlopa/nézete hiányzik (hova kerül
  egy elvetett task a UI-n?).
- 3.1: a `family` route admin-only — az api-design 5.1 szerint a listát
  minden autentikált user lekérheti; ha a gyerek/felnőtt sosem éri el az
  oldalt, a „kihez tartozik” választók (task assign) máshonnan kapnak
  adatot — rendben, csak legyen tudatos.

## Erősségek (megőrzendő)

- Facade + mini signal-store: pont elég state-kezelés ehhez a mérethez,
  NgRx-mellőzés jól indokolt.
- Generált API-kliens gitben tartva — kontraktváltozás review-ban látszik.
- „Nem vagy otthon” képernyő + heartbeat (12.) — az ADR-0003 jól le van
  képezve UX-re.
- Suggestion-block újrahasznosítható komponensként (9.2) — az „AI nem
  aktivál” elv egységes UI-mintát kap.

## Verdikt

Jó alap a frontend-fejlesztéshez; az #1 (naptár-komponens) és #3 (i18n)
döntése, valamint a #2 Settings-korrekció után kiadható a fejlesztő
agenteknek.

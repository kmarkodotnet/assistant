# ADR-0008 — Workers → UI valós idejű jelzés: MVP-ben nincs cross-process push

- Státusz: Elfogadva (a megvalósult kód rögzítése)
- Dátum: 2026-07-02
- Döntéshozó: kmarko.net@gmail.com (dokumentum-audit nyomán rögzítve)

## Kontextus

A SignalR hubokat az `Api` process hosztolja. A tervek (architecture.md
11.1, devtasks T-DBE-26 / T-GBE-13) szerint a `Workers` process
`IHubContext`-tel push-olna — ez technikailag lehetetlen: az
`IHubContext` csak a hubot hosztoló processben él. Alternatívák:

- (A) Redis/Postgres backplane a két process között.
- (B) A Workers belső HTTP-hívással szól az Api-nak, az push-ol.
- (C) MVP-ben nincs worker-oldali push; a kliens polling/refresh-sel
  frissül, push csak az Api-oldali eseményekre.

## Döntés

**(C) az MVP-ben.** A kód ezt implementálja: a Workers-ben
`NoOpProgressNotifier` / `NullNotificationPusher` fut, az Api-ban a valódi
SignalR-implementációk. Következésképp:

- A **dokumentum-feldolgozási progress** nem valós idejű; a frontend a
  dokumentum-lista/detail betöltésekor és időzített frissítéskor látja a
  státuszváltást (a `documents` oldal reload-olja a státuszt; 15 perces
  notification-polling fallback amúgy is létezik).
- Az **Api-oldali események** (pl. user-akcióból születő notification)
  valós időben mennek SignalR-en.
- A worker-oldali **reminder-tüzelés** InApp értesítése a
  `notification_feed` táblába íródik; a kliens a bell-feed
  lekérdezésekor / pollingkor látja.

Post-MVP irány: **(B) belső HTTP-hívás** (Docker-network-only endpoint,
shared-secret auth) — olcsóbb, mint a backplane, egy-gépes telepítésen.

## Indoklás

- Single-tenant, LAN-only, egy háztartás: a másodperces valós idejűség
  nem kritikus; a polling-költség elhanyagolható.
- A backplane (Redis) új infrastruktúra-komponens lenne — ellentétes a
  „minimal attack surface / kevés mozgó alkatrész" elvvel.

## Következmények

- `architecture.md` 11.1 és `api-design.md` 22. jelzi: a
  `documentProcessingProgress` események MVP-ben csak az Api-process
  eseményeire élnek; worker-progress polling útján érkezik.
- `frontend-structure.md` 6.2 polling-fallback a normatív frissítési út
  a worker-eseményekre.
- devtasks T-DBE-26 / T-GBE-13 AC-k e döntés szerint értelmezendők.
- A UX-elvárásokat (qa/ui-test-scenarios QA-D11-01) a full-stack E2E
  rétegben polling-alapúra kell fogalmazni.

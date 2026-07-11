# ADR-0011 — Napi digest: BackgroundService (nem Hangfire recurring), üres digest elhagyása

- Státusz: Elfogadva
- Dátum: 2026-07-11
- Döntéshozó: architect agent (CR260710-02 kontrakt-tervezés)
- Kapcsolódó: [CR260710-02](../change-requests/cr260710-02-proaktiv-napi-osszefoglalo.md),
  [ADR-0008](ADR-0008-workers-realtime-jelzes.md),
  [ADR-0009](ADR-0009-reminder-generalas-es-csatorna.md),
  [daily-digest-contract.md](../contracts/daily-digest-contract.md)

## Kontextus

A CR260710-02 megvalósítási terve **Hangfire recurring job**-ot ír a `DailyDigestJob`-ra.
A kódbázisban a Hangfire storage konfigurálva van (dashboard, AI job futtatás
`AiJobExecutor` révén), de **minden időzített, ütemezett worker a `BackgroundService`
mintát követi**, nem `RecurringJob.AddOrUpdate`-et:

- `DueReminderDispatcher` — 1 perces `Task.Delay` loop
- `EscalationScheduler`
- `NotificationFeedRetentionJob` — 24 órás `Task.Delay` loop
- regisztráció: `AddHostedService<T>()` a `FamilyOs.Workers/Program.cs`-ben

Két nyitott kérdés:
1. Hangfire recurring vs. BackgroundService.
2. "Nincs mai teendő" eset: küldjön-e üres digestet (a CR ezt nyíltan hagyja).

## Döntés

### 1. BackgroundService, nem Hangfire recurring

A `DailyDigestJob` a meglévő `BackgroundService` mintát követi:
- saját `ExecuteAsync` loop, `Task.Delay(PollInterval)` (alap: 15 perc)
- minden ébredéskor kiértékeli, mely aktív felhasználóknak jár **ma** még
  nem elküldött digest, és a napi digest-időpont (`RunAtLocal`, alap 07:00)
  már elmúlt-e, valamint a felhasználó nincs-e `quiet_hours` ablakban
- `AddHostedService<DailyDigestJob>()` a `Program.cs`-ben
- az "csak egyszer fusson ma" garanciát nem a scheduler adja, hanem az
  **idempotencia-kulcs** a `notification_feed`-ben (lásd kontrakt)

Indoklás:
- **Konzisztencia** a három meglévő ütemezett worker-rel; egy fejlesztő/
  reviewer egy mintát lát, nem kettőt.
- A poll-alapú megközelítés **természetesen kezeli a quiet_hours-halasztást**
  (ha a user 07:00-kor még csendes órában van, a következő poll a csendes óra
  vége után küld), pontosan úgy, ahogy a `DueReminderDispatcher` újraütemez.
- Nincs szükség Hangfire cron-kifejezésre és külön recurring-regisztrációra;
  a Hangfire szerver továbbra is csak az AI-pipeline-t futtatja.
- Az idempotencia-kulcs amúgy is kötelező (retry/restart elleni védelem az
  elfogadási kritérium szerint), így a "napi egyszer" nem a scheduler
  felelőssége — ezzel a BackgroundService semmilyen garanciát nem veszít.

Következmény: a CR "Hangfire recurring job" megfogalmazása felülírva; a
kontrakt a BackgroundService-t rögzíti.

### 2. Üres digest NEM kerül elküldésre (skip)

Ha egy felhasználónak az adott napon **egyetlen** forrásból sincs tétele
(nincs ma/holnap reminder, nincs következő 7 napos határidő, nincs elmúlt
24 órás új dokumentum), a job **nem hoz létre** `notification_feed` bejegyzést
és **nem küld** emailt.

Indoklás:
- **Értesítési fáradtság elkerülése.** Egy naponta ismétlődő "nincs mai
  teendő" üzenet nulla információt hordoz, és ráneveli a felhasználót, hogy
  figyelmen kívül hagyja a bell-t — ezzel épp a digest hasznos (nem-üres)
  napjait is elnyomná. A CR célja a jel/zaj arány javítása, nem rontása.
- A `notification_feed` így csak akkor "pittyeg", ha valóban van mondanivaló;
  a nem-üres digest jelzésértéke megmarad.
- Kompromisszum: aki egy adott napon nem kap digestet, az nem tudja
  megkülönböztetni a "nincs teendő" és az "esetleges job-hiba" esetet.
  Ezt a worker-oldali logolás (`LogSkippedEmpty`) és a Hangfire/health
  telemetria fedi le; végfelhasználói szempontból az MVP-ben elfogadható.
- A digest reggeli **pillanatkép**: ha a nap folyamán később keletkezik
  tétel, az nem generál pótdigestet (nem cél a valós idejű újraértékelés).
  Ezt a meglévő reminder-tüzelés (`DueReminderDispatcher`) amúgy is lefedi.

## Alternatívák

- **Hangfire `RecurringJob`:** eldobva, mert egyedülálló mintát vezetne be a
  worker rétegben és külön kellene kezelni a quiet_hours-halasztást.
- **Üres "nincs mai teendő" digest:** eldobva (lásd fenti indoklás). Ha a
  telemetria később azt mutatja, hogy a felhasználók bizonytalanok a digest
  megérkezésében, v2-ben opt-in "napi jelentkezés" beállítás bevezethető.

## Következmények

- A kontrakt (`docs/contracts/daily-digest-contract.md`) e döntéseket rögzíti
  a backend-dev és frontend-dev agentek felé.
- A `notification_feed` sémája **nem változik**: nincs új `related_entity_type`
  oszlop; a digest a meglévő `Type` mezőt használja `Type = "DailyDigest"`
  értékkel (nincs DB-migráció).
- A quiet_hours-logika (`IsQuietHour` / `GetQuietHoursEnd`) újrafelhasználandó
  a `DueReminderDispatcher`-ből; javasolt közös statikus helperbe kiemelni.
</content>
</invoke>

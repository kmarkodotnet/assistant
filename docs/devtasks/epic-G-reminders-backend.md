# Epic G — Reminders — Backend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` (FULL — baseline)
> - `reminder-engine.md` (FULL — az epic vezérlő dokumentuma)
> - `domain-model.md` §1.12 (Reminder), §1.10–1.11 (parent entitások)
> - `database-schema.md` §4.15 (reminder + Cancelled enum v0.2), §4.17.1 (notification_feed v0.2), §2
> - `architecture.md` §3.6 (Workers), §6.4 (recurring dispatcher), §11.3 (catch-up flow)
> - `api-design.md` §13 (Reminders), §14 (Notifications), §22 (SignalR)
> - `security-privacy.md` §10.2 (SMTP), §5 (audit a tüzeléshez)
>
> **Story-k:** G1, G2, G3, G4 (S), G5 (S), G6
> **Fázis:** Fázis 10 — **2 párhuzamos worktree** (`feature/reminders-core`, `feature/notifications-feed`)

---

## Áttekintés

Az epic biztosítja, hogy a felhasználó egyetlen fontos határidőt se
hagyjon ki, miközben a rendszer az otthoni PC ki-be kapcsolása mellett
is megbízhatóan üzemel. A katch-up logika és az eszkaláció a **kritikus
megkülönböztető szolgáltatás**.

## Taskok

### Worktree 1: `feature/reminders-core`

### T-GBE-01 — `Reminder` entity + XOR invariáns
- **Fájlok:**
  - `src/FamilyOs.Domain/Entities/Reminder.cs`
  - `src/FamilyOs.Domain/Enums/ReminderStatus.cs` (Cancelled is!),
    `NotificationChannel.cs`
  - `src/FamilyOs.Infrastructure/Persistence/Configurations/ReminderConfiguration.cs`
  - migráció.
- **AC:**
  - [ ] `database-schema.md` §4.15 séma + DB-szintű CHECK az XOR-ra.
  - [ ] Factory methods: `Reminder.ForTask(taskId, ...)`,
        `Reminder.ForDeadline(deadlineId, ...)` — kódszinten kényszerítve.
  - [ ] Unit teszt: mindkettő próbálkozás esetén `DomainException`.

### T-GBE-02 — `ReminderTriggerCalculator` domain szolgáltatás
- **Cél:** offset + dueDate → triggerUtc.
- **Fájlok:**
  - `src/FamilyOs.Domain/Services/ReminderTriggerCalculator.cs`
- **AC:**
  - [ ] Offset `OffsetMinutesBeforeDue` adott → `triggerUtc = dueDate - offset`.
  - [ ] Unit teszt időzóna-agnosztikus.

### T-GBE-03 — `IcalRecurrenceEvaluator` (RRULE)
- **Cél:** RFC 5545 RRULE → következő trigger.
- **Fájlok:**
  - `src/FamilyOs.Infrastructure.Ai/Recurrence/IcalRecurrenceEvaluator.cs`
    (`Ical.Net` csomag).
- **AC:**
  - [ ] Adott RRULE + utolsó tüzelés → következő tüzelés.
  - [ ] Hibás RRULE → `DomainBusinessRuleException`.
  - [ ] Unit teszt presetekre: havi, heti, éves.

### T-GBE-04 — Reminder CRUD endpointok
- **Fájlok:**
  - `src/FamilyOs.Application/Reminders/CreateReminderCommand.cs`
  - `src/FamilyOs.Application/Reminders/PatchReminderCommand.cs`
  - `src/FamilyOs.Application/Reminders/ListRemindersQuery.cs`
  - `src/FamilyOs.Api/Endpoints/RemindersModule.cs`
- **AC:**
  - [ ] XOR validáció `Validator`-ban.
  - [ ] `?upcoming=true` (következő 30 nap), `?status=`.
  - [ ] Csoportosított nézet a UI-nak (4 kategória: most / héten / később
        / lecsúszott).

### T-GBE-05 — Felhasználói akció endpointok
- **Cél:** acknowledge / snooze / skip / delegate.
- **Fájlok:**
  - `src/FamilyOs.Application/Reminders/Actions/*.cs`
- **AC:**
  - [ ] Snooze: új `Reminder` rekord `Scheduled` + `TriggerUtc = now + minutes`.
  - [ ] Skip: `status = Skipped` + audit log.
  - [ ] Delegate: új `Reminder` a célpontnak; eredeti `Cancelled`.

### T-GBE-06 — `DueReminderDispatcher` BackgroundService
- **Cél:** 1 percenkénti recurring scan + tüzelés.
- **Fájlok:**
  - `src/FamilyOs.Workers/Services/DueReminderDispatcher.cs`
- **AC:**
  - [ ] `SELECT FOR UPDATE SKIP LOCKED` 100 sor batch.
  - [ ] Parent (Task/Deadline) jóváhagyott (`Origin in (Manual, AiApproved)`)
        ellenőrzés.
  - [ ] Csendes órák check: `UserPreferences` alapján, ha igen →
        reschedule `QuietHoursEnd`-re, nem tüzel most.
  - [ ] Recurring esetén következő `Reminder` rekord létrehozása.
  - [ ] Audit log: `Create` (új rekord).

### T-GBE-07 — Catch-up logika
- **Cél:** worker startup → 14 napos ablakból szkennel.
- **Fájlok:**
  - `src/FamilyOs.Workers/Services/DueReminderDispatcher.StartupCatchUp.cs`
- **AC:**
  - [ ] `OnStarted` event → query `Scheduled AND trigger_utc <= now() AND
        trigger_utc > now() - 14 days`.
  - [ ] 500 batch limit (zaj-szűrés).
  - [ ] 14 napnál régebbi reminderek `Skipped` + dashboard-on
        „lecsúszott" összesítőként.

### T-GBE-08 — `EscalationScheduler` BackgroundService
- **Cél:** 5 percenként eszkalációs scan.
- **Fájlok:**
  - `src/FamilyOs.Workers/Services/EscalationScheduler.cs`
  - `src/FamilyOs.Domain/Services/EscalationPolicyEvaluator.cs`
- **AC:**
  - [ ] `Fired AND acknowledged_utc IS NULL AND fired_utc < now -
        escalation_timeout` szkennelés.
  - [ ] `reminder-engine.md` §4.1 policy szerint új `Reminder` magasabb
        EscalationLevel-en.
  - [ ] Eredeti `Fired → Skipped`; új `Scheduled` rekord.
  - [ ] Partner-delegálás opcionális (`Family.EscalateToPartner`).

---

### Worktree 2: `feature/notifications-feed`

### T-GBE-09 — `NotificationFeed` entity + migráció
- **Cél:** v0.2 séma-bevezetés.
- **Fájlok:**
  - `src/FamilyOs.Domain/Entities/NotificationFeed.cs`
  - Configuration.
  - migráció: `notification_feed` tábla + 4 partial index.
- **AC:**
  - [ ] `database-schema.md` §4.17.1 séma pontosan.
  - [ ] Index `unread` (target_user_account_id, created_utc DESC) WHERE
        read_utc IS NULL.

### T-GBE-10 — `INotificationService` + InApp implementáció
- **Cél:** InApp értesítés mint dispatcher kimenet.
- **Fájlok:**
  - `src/FamilyOs.Application/Abstractions/Notifications/INotificationService.cs`
  - `src/FamilyOs.Infrastructure/Notifications/InAppNotificationChannel.cs`
  - `src/FamilyOs.Application/Notifications/NotificationEnvelope.cs`
- **AC:**
  - [ ] InApp = `NotificationFeed` insert + SignalR push (lásd T-GBE-13).
  - [ ] `IdempotencyKey = hash(reminder_id + escalation_level)` a re-send
        ellen.

### T-GBE-11 — SMTP email csatorna (G5)
- **Cél:** email notification opt-in.
- **Fájlok:**
  - `src/FamilyOs.Infrastructure/Notifications/SmtpNotificationChannel.cs`
  - `src/FamilyOs.Infrastructure/Notifications/Templates/reminder-email.hu.html`
  - `appsettings.json`: `Notifications.Smtp` szekció.
- **AC:**
  - [ ] STARTTLS kötelező.
  - [ ] Per-user opt-in (`UserPreferences.EmailEnabled`).
  - [ ] Magyar email-template; semmi dokumentum-tartalom, csak link a UI-ra.
  - [ ] Retry 3-szor, exponenciális backoff.
  - [ ] Hiányzó SMTP konfig → channel inactive, dispatcher átlép (warning log).

### T-GBE-12 — Notification feed endpointok
- **Cél:** `GET /api/v1/notifications`, `POST /read`, `read-all`.
- **Fájlok:**
  - `src/FamilyOs.Application/Notifications/GetFeedQuery.cs`
  - `src/FamilyOs.Application/Notifications/MarkAsReadCommand.cs`
  - `src/FamilyOs.Application/Notifications/MarkAllAsReadCommand.cs`
  - `src/FamilyOs.Api/Endpoints/NotificationsModule.cs`
- **AC:**
  - [ ] `?onlyUnread=true` szűrő.
  - [ ] Pagination.
  - [ ] Read action 204.

### T-GBE-13 — `NotificationsHub` SignalR
- **Cél:** push esemény a kliens felé.
- **Fájlok:**
  - `src/FamilyOs.Api/Realtime/NotificationsHub.cs`
- **AC:**
  - [ ] `notificationCreated(NotificationDto)` event.
  - [ ] `reminderFired(ReminderDto)` sticky toast event.
  - [ ] `aiSuggestionReady(SuggestionSummaryDto)` event.
  - [ ] A worker `IHubContext`-en publikál (server-to-server).

### T-GBE-14 — Retention job a notification feed-re
- **Cél:** 90 napnál régebbi `read` rekordok takarítása.
- **Fájlok:**
  - `src/FamilyOs.Workers/Services/NotificationFeedRetentionJob.cs`
- **AC:**
  - [ ] Naponta egyszer fut.
  - [ ] Olvasatlanok SOSE törlődnek.
  - [ ] Konfigurálható retention (`appsettings.json`).

---

## Mergelés utáni közös taskok

### T-GBE-15 — Audit log integráció
- **Fájlok:**
  - kiegészítés `IAuditLogger`-be: `LogReminderFired`,
    `LogReminderAcknowledged`, `LogReminderEscalated`.
- **AC:**
  - [ ] Minden tüzelés audit logban.

### T-GBE-16 — Idő-szimulációs tesztek
- **Cél:** PC ki → be → catch-up → eszkaláció E2E.
- **Fájlok:**
  - `tests/FamilyOs.Workers.Tests/DueReminderDispatcherTests.cs`
  - `tests/FamilyOs.Workers.Tests/EscalationSchedulerTests.cs`
  - `tests/FamilyOs.Workers.Tests/TimeSimulationE2ETests.cs`
- **AC:**
  - [ ] `FakeClock` időtolás.
  - [ ] T0: Reminder Scheduled.
  - [ ] T+1 nap (PC off): nincs tüzelés.
  - [ ] T+2 nap (PC on, startup catch-up): tüzelés egyszer.
  - [ ] T+3 nap (acknowledge nélkül): eszkaláció új Reminder.
  - [ ] T+4 nap (acknowledge): minden lezáródik.

### T-GBE-17 — Konkurens dispatcher race-teszt
- **Cél:** SKIP LOCKED helyesen működik.
- **Fájlok:**
  - `tests/FamilyOs.Workers.Tests/DispatcherConcurrencyTests.cs`
- **AC:**
  - [ ] 2 worker indítása párhuzamosan → minden reminder csak egyszer tüzel.

---

## Megvalósítási sorrend

```
Worktree 1: T-GBE-01 → 02 → 03 → 04 → 05 → 06 → 07 → 08
Worktree 2: T-GBE-09 → 10 → 11 → 12 → 13 → 14

Mergelés után:
T-GBE-15 → 16 → 17
```

## Epic-DoD

- [ ] Reminder CRUD működik, XOR DB-szinten kényszerítve.
- [ ] Dispatcher 1 percenként szkennel és tüzel.
- [ ] Catch-up: PC újraindítás után a hátralék feldolgozódik.
- [ ] Eszkaláció az `reminder-engine.md` §4 policy szerint.
- [ ] NotificationFeed InApp működik, SignalR push elérhető.
- [ ] Email csatorna opt-in és működik (ha SMTP konfigurált).
- [ ] Konkurens dispatcher race-teszt zöld.
- [ ] Idő-szimuláció E2E zöld.
- [ ] Git tag `v0.10`.

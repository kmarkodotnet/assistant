# Epic G — Reminders — Frontend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` §9, §10
> - `frontend-structure.md` §7 (Notification feed + toast), §8.6 (Reminders oldal)
> - `reminder-engine.md` §10 (UI áttekintés), §4 (eszkaláció UX)
> - `api-design.md` §13 (Reminders), §14 (Notifications), §22 (SignalR)
>
> **Story-k:** G1 (FE), G3 (FE), G6 (FE)
> **Fázis:** Fázis 10

---

## Áttekintés

A reminders FE két fő része:
- **Reminders oldal** (`/reminders`) — csoportosított feed (most / héten /
  később / lecsúszott) + per-card akciók.
- **Notification feed** + **toast** — globális, a navbar bell-ből.

Plusz: **sticky toast** a `reminderFired` event-re, akció-gombokkal
(„Kész"/„Halaszt 1 óra").

## Taskok

### T-GFE-01 — Reminders API client + DTOs
- **Fájlok:**
  - `frontend/src/app/features/reminders/services/reminders.api.ts`
  - `frontend/src/app/features/reminders/models/reminder.dto.ts`
- **AC:**
  - [ ] CRUD + action endpointok (acknowledge/snooze/skip/delegate).

### T-GFE-02 — Notifications API client + DTOs
- **Fájlok:**
  - `frontend/src/app/core/notifications/notifications.api.ts`
  - `frontend/src/app/core/notifications/models/notification.dto.ts`
- **AC:**
  - [ ] List + mark-read + mark-all-read.

### T-GFE-03 — `RemindersFacade`
- **Fájlok:**
  - `frontend/src/app/features/reminders/services/reminders.facade.ts`
- **AC:**
  - [ ] `groupedReminders` computed signal: `{ today, thisWeek, later, overdue }`.
  - [ ] `acknowledge`, `snooze(minutes)`, `skip`, `delegate`, `cancel` akciók.

### T-GFE-04 — `NotificationsFeedFacade`
- **Cél:** globális, az auth-store-ral együtt initializálódik.
- **Fájlok:**
  - `frontend/src/app/core/notifications/notifications-feed.facade.ts`
- **AC:**
  - [ ] `feed: signal<NotificationDto[]>`.
  - [ ] `unreadCount: computed`.
  - [ ] `loadFeed()`, `markAsRead(id)`, `markAllRead()`.
  - [ ] SignalR `notificationCreated` → state-frissítés.

### T-GFE-05 — Reminders lista oldal (csoportosított)
- **Fájlok:**
  - `frontend/src/app/features/reminders/pages/reminders.page.ts`
  - `frontend/src/app/features/reminders/components/reminder-group.component.ts`
- **AC:**
  - [ ] 4 csoport: Most esedékes (vörös sáv), A héten (sárga), Később
        (semleges), Lecsúszott (szürke).
  - [ ] Csoport-fejléc szám-badge-dzsel.
  - [ ] Üres állapot: „Nincs esedékes emlékeztetőd, ráérsz!"

### T-GFE-06 — `reminder-card` komponens
- **Fájlok:**
  - `frontend/src/app/features/reminders/components/reminder-card.component.ts`
- **AC:**
  - [ ] Cím + parent (Task/Deadline) referencia + due date relatíven.
  - [ ] Csatorna ikon (InApp / Email).
  - [ ] Akció-gombok: Kész / Halaszt 1h / Halaszt 4h / Holnap reggel /
        Új idő / Delegálom / Elvetem.

### T-GFE-07 — Snooze + reschedule dialog
- **Fájlok:**
  - `frontend/src/app/features/reminders/components/snooze-dialog.component.ts`
  - `frontend/src/app/features/reminders/components/reschedule-dialog.component.ts`
- **AC:**
  - [ ] Snooze: gyors preset + custom időpont.
  - [ ] Reschedule: datetime input.

### T-GFE-08 — Delegate dialog
- **Fájlok:**
  - `frontend/src/app/features/reminders/components/delegate-dialog.component.ts`
- **AC:**
  - [ ] FamilyMember select (csak Adult-ok).
  - [ ] Opcionális üzenet a delegáltnak.

### T-GFE-09 — Reminder szerkesztő dialog (új létrehozás)
- **Fájlok:**
  - `frontend/src/app/features/reminders/components/reminder-edit.dialog.ts`
- **AC:**
  - [ ] Task vagy Deadline select (XOR).
  - [ ] TriggerUtc datetime vagy offset-preset.
  - [ ] RRULE preset (havi, heti, éves, egyszeri).
  - [ ] Channel (InApp / Email — Email csak ha enabled).

### T-GFE-10 — Navbar bell ikon
- **Fájlok:**
  - `frontend/src/app/layout/notification-bell.component.ts`
- **AC:**
  - [ ] Olvasatlan badge a számmal.
  - [ ] Klikkre `NotificationsFeedSheet` (jobb oldali drawer / mobile fullscreen).

### T-GFE-11 — Notifications feed sheet
- **Fájlok:**
  - `frontend/src/app/core/notifications/notifications-feed-sheet.component.ts`
- **AC:**
  - [ ] Lista a `feed` jeleiből.
  - [ ] Csoport: Új (olvasatlan) / Régebbi.
  - [ ] Klikkre megnyitja a hivatkozott rekordot + automatikus mark-read.
  - [ ] „Mind olvasottnak" gomb.

### T-GFE-12 — Sticky toast `reminderFired`-re
- **Fájlok:**
  - `frontend/src/app/core/notifications/sticky-reminder-toast.component.ts`
- **AC:**
  - [ ] Reminder cím + akciók (Kész / Halaszt 1 óra).
  - [ ] NEM tűnik el magától; csak akciótól.
  - [ ] Több reminder esetén stack (max 3 látszik).

### T-GFE-13 — SignalR connect setup
- **Cél:** `/realtime/notifications` hub csatlakozás.
- **Fájlok:**
  - `frontend/src/app/core/realtime/notifications-realtime.service.ts`
- **AC:**
  - [ ] `withAutomaticReconnect` engedélyezve.
  - [ ] `start()` az auth siker után.
  - [ ] `stop()` logout-kor.

### T-GFE-14 — Lecsúszott összesítő widget (G-L kapcsolat)
- **Fájlok:**
  - `frontend/src/app/features/reminders/components/overdue-summary.component.ts`
- **AC:**
  - [ ] Dashboard widget-ként újrahasznosítható.
  - [ ] Akció: „Újraütemezem" / „Elvetem" tömeges műveletek.

### T-GFE-15 — Tesztek
- **Fájlok:**
  - `frontend/src/app/features/reminders/pages/reminders.page.spec.ts`
  - `frontend/src/app/core/notifications/notifications-feed.facade.spec.ts`
  - `frontend/e2e/reminders/reminder-acknowledge-flow.spec.ts`
- **AC:**
  - [ ] Reminders lista renderel mind a 4 csoportot.
  - [ ] Acknowledge eltünteti a kártyát + olvasatlan szám csökken.
  - [ ] E2E: sticky toast jelenik meg mocked SignalR push-ra.

---

## Megvalósítási sorrend

```
T-GFE-01 → 02 → 03 → 04             (data)
       → 05 → 06                     (Reminders lista)
       → 07 → 08 → 09                (action dialogok)
       → 10 → 11                     (bell + feed sheet)
       → 12 → 13                     (sticky toast + SignalR)
       → 14                           (overdue widget)
       → 15                           (tesztek)
```

## Epic-DoD

- [ ] Reminders oldal csoportosítva, kártya-akciók működnek.
- [ ] Navbar bell + feed sheet implementálva.
- [ ] Sticky toast a `reminderFired` event-re.
- [ ] Snooze, delegate, reschedule dialogok működnek.
- [ ] SignalR realtime.
- [ ] Magyar UI minden részen.
- [ ] Tesztek zöldek.

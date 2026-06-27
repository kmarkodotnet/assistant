# Emlékeztető motor — Family OS

> Státusz: DRAFT v0.1 · Dátum: 2026-06-26 · Nyelv: magyar
> Kapcsolódó: [domain-model.md](domain-model.md), [database-schema.md](database-schema.md),
> [architecture.md](architecture.md), [ai-pipeline.md](ai-pipeline.md)

---

## 1. Cél és vezérlőelvek

Az emlékeztető motor azt biztosítja, hogy a felhasználó egyetlen fontos
határidőt vagy feladatot se hagyjon ki, miközben a rendszer az otthoni PC
ki-be kapcsolása mellett is megbízhatóan üzemel.

Vezérlőelvek:

1. **Catch-up first.** A worker indulásánál azonnal felismeri az offline
   állapot alatt esedékessé vált emlékeztetőket és sorrendben tüzeli őket
   (lásd 6. szakasz).
2. **Idempotens tüzelés.** Ugyanaz a `Reminder` többszöri „fire" tranzakcióban
   pontosan egyszer küld értesítést — az adatbázis-szintű állapotgép kényszeríti.
3. **Suggested ≠ Scheduled.** AI által javasolt emlékeztető csak akkor
   tüzel, ha a kapcsolódó Task vagy Deadline jóváhagyott állapotban van
   (`Origin = AiApproved`).
4. **Felhasználói kontroll.** A felhasználó bármikor jóváhagyhatja,
   elveti, halaszthatja vagy módosíthatja egy emlékeztetőt. A motor
   szervízjellegű — soha nem küld értesítést rejtett szabály alapján.
5. **In-app first, email second.** MVP-ben az in-app értesítés kötelező
   csatorna, az email opcionális. Push notification nem MVP.
6. **Csendes órák.** Az értesítés-küldés tiszteletben tartja a házban
   beállított csendes órákat — az időpontot átsorolja, nem dobja el.

---

## 2. Emlékeztető típusok

### 2.1 Egyszeri emlékeztető (one-time)

A leggyakoribb eset: egyetlen abszolút időpontban tüzel egyszer.

- `RecurrenceRule = NULL`
- `TriggerUtc = abszolút időpont`
- Állapot: `Scheduled → Fired → Acknowledged | Skipped`

### 2.2 Ismétlődő emlékeztető (recurring)

RFC 5545 RRULE-alapú ismétlés. Példa: havi 5-én („minden hónap 5-én
fizetni a kommunális számlát").

- `RecurrenceRule = 'FREQ=MONTHLY;BYMONTHDAY=5'`
- `TriggerUtc = a következő esedékes időpont`
- Tüzelés után az állapotgép kiszámolja a *következő* `TriggerUtc`-t a
  szabály alapján, és új `Reminder` rekordot hoz létre (NEM update-eli a
  régit). Ez biztosítja, hogy minden tüzelés saját audit-rekorddal jár.
- A „motherként" működő szülő-emlékeztetőre is hivatkozhatnánk egy
  `parent_reminder_id`-vel, de MVP-ben **nem vezetjük be** — a recurrence-
  rule újraszámolás minden alkalommal a `RecurrenceRule`-ból olvas.

### 2.3 Relatív emlékeztető (relative)

A `Deadline` időpontjához képest relatívan definiált: „1 nappal előtte",
„30 perccel előtte". Az `OffsetMinutesBeforeDue` mező tárolja a relatív
értéket; a `TriggerUtc` ebből + a Deadline `DueDateUtc`-ból generálódik
*létrehozáskor*.

- `OffsetMinutesBeforeDue = 1440` (1 nap)
- `TriggerUtc = DueDateUtc - OffsetMinutesBeforeDue`
- Ha a Deadline `DueDateUtc`-ja módosul, a kapcsolódó `Reminder.TriggerUtc`
  automatikusan újraszámolódik (domain esemény `DeadlineDueDateChanged`
  triggerelje az `IReminderScheduler.RescheduleAsync`-et).

### 2.4 Deadline-alapú emlékeztető (deadline-based)

Egy `Deadline` jóváhagyásakor a `ai-pipeline.md` 3.10 szakasz default
policy-ja alapján 1–3 relatív emlékeztető generálódik (pl. Insurance →
30/7/1 nap előtti). Ezek mind 2.3 típusú relatív emlékeztetők,
ugyanahhoz a Deadline-hoz kötve.

### 2.5 Eszkalációs emlékeztető (escalation)

Nem önálló típus — az állapotgép része. Egy tüzelt emlékeztető, amelyet
N órán belül senki nem nyugtáz, automatikusan **eszkalál**: új `Reminder`
keletkezik magasabb `EscalationLevel`-en, opcionálisan másik csatornán
(InApp → Email), opcionálisan másik felelőshöz (lásd 4. szakasz).

---

## 3. Állapotgép

```
                  ┌─────────────────────┐
                  │     Scheduled       │
                  └──────────┬──────────┘
       worker scan │         │ user cancel
       (trigger    │         ▼
        elérve)    │     ┌─────────────┐
                  ▼      │  Cancelled  │
              ┌─────────┐ └─────────────┘
              │  Fired  │
              └────┬────┘
       user "Done"│ │  user "Snooze"        timeout (N óra,
                   │ │                        ack nélkül)
                   │ │ ┌────────────┐         │
                   ▼ ▼ ▼            │         ▼
            ┌─────────────────┐     │  ┌──────────────────┐
            │ Acknowledged    │     │  │  Escalated*      │
            └─────────────────┘     │  │  (új Reminder)   │
                                    │  └──────────────────┘
                                    │
                              új Reminder
                              (TriggerUtc + snooze)

   * Az Escalated nem szótári enum érték — az eredeti reminder
     "Skipped"-be vagy "Fired"-ben marad, és új Reminder rekord
     jön létre EscalationLevel+1-gyel.
```

### 3.1 Enum értékek (a domain modellből)

`ReminderStatus = { Scheduled, Fired, Acknowledged, Skipped, Failed, Cancelled }`

> Megj.: a `Cancelled` érték a `database-schema.md` enumjából hiányzik — a
> migrációban hozzáadjuk: `ALTER TYPE app.reminder_status ADD VALUE 'Cancelled';`.
> Indok: a `Skipped` egy automatikus „nem ért időben oda" kategória, a
> `Cancelled` egy explicit user-akció — érdemes szétválasztani.

### 3.2 Átmenetek

| Honnan | Hova | Trigger | Mellékhatás |
|---|---|---|---|
| Scheduled | Fired | worker scan `trigger_utc <= now()` | `INotificationService.DispatchAsync`; `fired_utc = now()` |
| Scheduled | Cancelled | user / Task vagy Deadline törlés | – |
| Fired | Acknowledged | user „Kész" / „Tudomásul vettem" | `acknowledged_utc = now()`; Task `Status = Done` opcionálisan |
| Fired | Skipped | escalation timeout / user „Mellőzöm" | új Reminder az escalation policy szerint |
| Fired | Failed | `INotificationService` exception | error log, retry max 3-szor; ha mind hibázik, `Failed` |
| – | (új) Scheduled | recurrence következő, snooze, eszkaláció | új sor, sosem update |

---

## 4. Eszkalációs logika

### 4.1 Default policy

| Reminder forrás | Eszkalációs lépcsők | Idő ack-ig | Új csatorna |
|---|---|---|---|
| Deadline (Insurance/Inspection) | 1 lépés | 24 h | + Email |
| Deadline (Invoice) | 2 lépés | 12 h, 24 h | InApp + Email (1.), majd Email a partner-userhez (2.) |
| Deadline (School) | 1 lépés | 12 h | + Email |
| Deadline (egyéb) | nincs | – | – |
| Task (Priority=High) | 2 lépés | 8 h, 24 h | + Email |
| Task (Normal/Low) | nincs | – | – |

### 4.2 Másik felelőshöz delegálás

Egy eszkalált `Reminder` az eredeti `Task.AssignedToFamilyMember` mellett
az adott családtag *partnerét* (Adult role, `Relation = Spouse` vagy
fordítva) is értesítheti. A delegálás opcionális, alkalmazás-konfigurált
beállítás: `Family.EscalateToPartner = true`.

### 4.3 Eszkaláció vége

Ha az `EscalationLevel = max_for_source` szint is timeoutol acknowledge
nélkül, a Reminder `Skipped`-be megy, és egy **napi összesítő** értesítés
gyűjti azt az adminoknak (admin dashboard widget + opcionális napi email).
Nem küldünk végtelenül „még mindig nem csináltad meg" üzeneteket — a UI
felelőssége vizuálisan kiemelni a lecsúszott elemeket.

---

## 5. Csatornák és értesítés-küldés

### 5.1 InApp (kötelező MVP)

- Megvalósítás: `NotificationFeed` entitás (külön egyszerű tábla a
  `domain-model.md`-ben nem szerepelt, MVP-pótlás itt — lásd 5.1.1).
- Frontend értesítés: SignalR push az aktív tab-ra; offline tab esetén az
  ikon-jelvény (`navbar bell`) számolja a olvasatlanokat.
- Snooze, dismiss, „kész vagyok" gombok közvetlenül a feed elemekről.

#### 5.1.1 NotificationFeed entitás (MVP-kiegészítés)

| Mező | Típus | Megj. |
|---|---|---|
| Id | Guid (PK) | |
| TargetUserAccountId | Guid (FK) | |
| Title | text | |
| Body | text | |
| RelatedEntityType | text | „Task", „Deadline", … |
| RelatedEntityId | Guid | |
| ReminderId | Guid? (FK) | ha reminderből származik |
| ReadUtc | timestamptz? | |
| CreatedUtc | timestamptz | |

Indexek: `(TargetUserAccountId, CreatedUtc DESC) WHERE ReadUtc IS NULL`.

Ezt a táblát hozzáadjuk a `database-schema.md` v0.2-höz.

### 5.2 Email (opcionális MVP)

- Megvalósítás: `INotificationService` SMTP implementáció.
- Konfiguráció: `appsettings.json` → `Notifications.Smtp { Host, Port,
  User, Pass, From }`. Ha hiányzik, az email csatorna **kikapcsolt**,
  a motor logol és átesik.
- Email tartalom: magyar template (`Templates/reminder-email.hu.html`),
  egyszerű, no-link tracking, no analytics.
- Ütemezés: a SMTP relay aszinkron, hibára 3× retry, közbeeső backoff.

### 5.3 Push (nem MVP)

A jövőbeli mobil natív app (Kotlin) bevezetésekor implementálható. A
`INotificationService` interface kibővíthető, a `NotificationChannel`
enum új értéket kap (`Push`). Az MVP semmilyen FCM/APNS infrastruktúrát
nem épít.

### 5.4 Felhasználói preferenciák

Egy egyszerű per-user beállítás (a `UserAccount`-on bővítve, vagy külön
`NotificationPreference` táblában):

| Beállítás | Default | Megj. |
|---|---|---|
| `EmailEnabled` | false | per user opt-in |
| `QuietHoursStart` | 22:00 | helyi időben |
| `QuietHoursEnd` | 07:00 | |
| `EscalationOptOut` | false | true esetén nem kap eszkalációt |

A csendes órák alatt a `Fired` események NEM küldenek aktív értesítést;
az InApp feedbe bekerülnek (ott látja amikor visszanéz), de email nem
megy ki, és a következő „kifelé csatorna" tüzelést a `QuietHoursEnd`-re
ütemezi a motor (= a `TriggerUtc`-t átírja). Részlet a 6. szakaszban.

---

## 6. Worker és tüzelési ciklus

### 6.1 DueReminderDispatcher

A `FamilyOs.Workers` projektben futó `BackgroundService`. Hangfire
recurring job (`*/1 * * * *`, minden perc), de a logika defenzíven
kezeli az átfedő futásokat is (advisory lock).

```
Frequency: 1 perc
Body:
  SELECT FOR UPDATE SKIP LOCKED
  FROM   app.reminder
  WHERE  status = 'Scheduled'
    AND  trigger_utc <= now()
  ORDER  BY trigger_utc
  LIMIT  100;

  for each reminder:
      - load Task vagy Deadline (XOR)
      - check that parent is approved (Origin in (Manual, AiApproved))
          NEM approved → skip, ne tüzelj
      - check that parent is not deleted / cancelled
          deleted/cancelled → reminder.status = Cancelled
      - check current user quiet-hours
          in quiet hours → reschedule (új trigger_utc = QuietHoursEnd)
                           NEM tüzel most
      - INotificationService.DispatchAsync(InApp + opcionálisan Email)
      - reminder.status = Fired, fired_utc = now()
      - ha RecurrenceRule != NULL:
            következő trigger kiszámolása (Ical.Net library)
            új Reminder rekord, status = Scheduled
      - audit log: AuditAction = AiCall (ha LLM) vagy Create (új sor)
```

Az `SKIP LOCKED` garantálja, hogy konkurens worker (újraindításkor két
process átfedhet rövid ideig) ne tüzeljen duplán.

### 6.2 Catch-up indulásnál

A worker `OnStarted` event handler-je egyszer lefuttat egy `StartupCatchUpAsync`-et:

```
SELECT *
FROM   app.reminder
WHERE  status = 'Scheduled'
  AND  trigger_utc <= now()
  AND  trigger_utc >  now() - interval '14 days'
ORDER  BY trigger_utc
LIMIT  500;
```

- A 14 napos kapu azt jelenti: hosszabb leállás után (pl. nyaralás) NEM
  küldünk ki egy 30 napja esedékes „1 nappal előtte" emlékeztetőt — ez
  zaj lenne. Helyette a UI-on egy „lecsúszott emlékeztetők" összesítő
  jelenik meg, és az érintett `Reminder`-ek `status = Skipped` lesznek.
- A 14 napos limit konfigurálható (`appsettings.json` →
  `Reminders.CatchUpMaxAgeDays`).

### 6.3 Eszkalációs ütemező

Külön recurring job (`*/5 * * * *`, 5 percenként):

```
SELECT *
FROM   app.reminder r
WHERE  r.status = 'Fired'
  AND  r.acknowledged_utc IS NULL
  AND  r.fired_utc < now() - escalation_timeout(r)
  AND  NOT EXISTS (SELECT 1 FROM reminder e
                   WHERE e.task_id = r.task_id OR e.deadline_id = r.deadline_id
                     AND e.escalation_level = r.escalation_level + 1)
  AND  r.escalation_level < max_level(r);

for each:
    - r.status = Skipped (eredeti emlékeztető lezárva)
    - új Reminder rekord:
        escalation_level = r.escalation_level + 1
        trigger_utc = now()
        channel = next_channel_for_escalation(r)
    - audit log
```

A `escalation_timeout()` és `max_level()` a 4.1 policy szerint, függvény
a `(source_kind, deadline_category, task_priority)` triple-ön.

### 6.4 Snooze és reschedule

- **Snooze:** user-akció. A `Fired` reminder lezáródik `Acknowledged`-be (a
  user „elismerte"), és egy új `Reminder` jön létre `Scheduled` állapotban
  `TriggerUtc = now() + snooze_duration`-nal. Snooze opciók a UI-on: 1 óra,
  4 óra, holnap reggel 8:00, holnap.
- **Reschedule:** user az emlékeztető szerkesztésénél új időpontot ad —
  a meglévő `Reminder.TriggerUtc` frissül; `status` marad `Scheduled`.

---

## 7. Idempotencia és duplikáció-védelem

### 7.1 Tüzelés-szintű

- `SKIP LOCKED` + state-check egyszerre garantálja, hogy két worker ne
  tüzeljen egyetlen `Reminder`-t.
- `INotificationService` minden hívása tartalmaz egy `IdempotencyKey =
  hash(reminder_id + escalation_level)`-et. Az SMTP relay (vagy bármilyen
  push provider később) ezt használja deduphoz.

### 7.2 Reminder-generálás idempotenciája

- AI-generált default reminder set (3.10 az ai-pipeline.md-ben): ugyanazon
  `(deadline_id, offset_minutes_before_due)` kombináció már létezik?
  → skip. Indok: az AI pipeline újrafutása nem hoz létre duplikátumot.
- Recurring rule alapú új `Reminder` generálás: PRIMARY KEY a `(task_id
  OR deadline_id) + trigger_utc` unique → adatbázis védi a duplikációt.

---

## 8. Felhasználói flow példák

### 8.1 Új Deadline jóváhagyása → emlékeztető-ütemezés

```
1. AI-pipeline javasol egy Deadline-t (Origin=AiSuggested) +
   3 Reminder-t a default policy alapján (Origin=AiSuggested, mind Scheduled).
2. User UI-on: "Elfogadom" gomb a Deadline-on.
3. Backend: ApproveSuggestedDeadlineCommand
   - Deadline.Origin = AiApproved
   - kapcsolódó Reminder-ek Origin = AiApproved
   - audit log
4. A DueReminderDispatcher legközelebbi szkennelése már elismeri a
   tüzelhetőséget (parent Origin in (Manual, AiApproved)).
```

### 8.2 Recurring havi reminder

```
1. User létrehoz egy Reminder-t Task-hoz (havi 5-én):
   RecurrenceRule = 'FREQ=MONTHLY;BYMONTHDAY=5'
   TriggerUtc = 2026-07-05 09:00 UTC
2. 2026-07-05 09:00: tüzel → status = Fired
3. Ugyanabban a tranzakcióban: új Reminder generálódik:
   TriggerUtc = 2026-08-05 09:00 (Ical.Net.NextOccurrence)
4. User „Kész"-re kattint → 2026-07-05-ös Reminder Acknowledged.
   A 2026-08-05-ös Reminder marad Scheduled — az új ciklus.
```

### 8.3 Lecsúszott eszkaláció

```
1. Számla határidő 2026-09-10. Reminder offsetek: 7 nap, 1 nap.
2. 2026-09-03 09:00: 7 napos reminder tüzel.
3. User 24 órán át nem nyugtáz → 2026-09-04 09:00-ra eszkalál:
   új Reminder TriggerUtc = 2026-09-04 09:00, escalation_level = 1,
   channel = InApp + Email.
4. User most már látja az emailt is, "Kész"-re kattint.
5. Acknowledged. A 1-napos pre-due Reminder még él 2026-09-09-re,
   automatikusan Cancelled lesz, mert a kapcsolódó Task vagy Deadline
   Status = Resolved/Done.
```

---

## 9. Felelős családtag (Responsibility)

- A `Task.AssignedToFamilyMemberId` és `Deadline.ResponsibleFamilyMemberId`
  határozza meg, kihez tartozik az emlékeztető.
- A motor a `FamilyMember` → `UserAccount` (1:1) leképezésen át jut el a
  konkrét célhoz.
- Ha a `FamilyMember`-nek nincs `UserAccount`-ja (pl. kisgyerek), a
  responsible alapból a `Parent`-ekhez (Relation = Self vagy Spouse,
  Role = Adult) kerül — InApp értesítés mindkét szülőnek.
- Az UI lehetőséget ad a felelős átruházására („Delegálom anyának"); a
  delegálás új `Reminder` rekordot hoz létre a célpontnak, az eredeti
  `Cancelled` lesz.

---

## 10. Felhasználói felület (rövid áttekintés)

A teljes UI-specifikáció a `frontend-structure.md`-ben. Itt csak a
reminder-vonatkozású UI elemek:

- **Globális értesítés ikon** (navbar bell) az olvasatlan számmal.
- **Reminder feed** (`/reminders`) — chronologikus, csoportosítva:
  - Most esedékes
  - A héten
  - Később
  - Lecsúszott (skipped, manuálisan újraütemezhető)
- **Egy reminder kártya** akciói: „Kész vagyok", „Halaszt 1 óra / 4 óra /
  holnap reggel", „Új időpont", „Delegálom", „Elvetem".
- **Dashboard widget** — „Következő 7 nap" + „Lecsúszott".

---

## 11. Tesztelés

### 11.1 Unit
- `ReminderTriggerCalculator` (offset → abszolút trigger).
- `RecurrenceRuleEvaluator` (`Ical.Net` wrapper) — adott RRULE +
  utolsó tüzelés → következő trigger.
- `EscalationPolicyEvaluator` — given (Task/Deadline, category,
  fired_utc, now) → escalate yes/no, next level.

### 11.2 Integráció
- Testcontainers Postgres-szel, valódi `reminder` tábla + indexek.
- DueReminderDispatcher tesztelése `IClock` mockkal, „idő ugrás" forgatással
  (catch-up szimuláció).

### 11.3 Idő-szimulációs E2E
- `[Trait("Category", "TimeSimulation")]` xUnit teszt:
  - PC ki: nincs worker, a Reminder már esedékes.
  - PC be: worker indítás → StartupCatchUp → 1 értesítés a feedben.
  - Időtolás 2 napra: eszkaláció történik.
  - Acknowledge → minden lezáródik tisztán.

---

## 12. Konfiguráció

```json
{
  "Reminders": {
    "DispatcherIntervalSeconds": 60,
    "EscalationCheckIntervalSeconds": 300,
    "CatchUpMaxAgeDays": 14,
    "DefaultQuietHours": { "Start": "22:00", "End": "07:00", "TimeZone": "Europe/Budapest" },
    "Escalation": {
      "EscalateToPartner": true,
      "MaxEscalationLevel": 2
    },
    "Channels": {
      "InApp":  { "Enabled": true },
      "Email":  { "Enabled": false, "Smtp": { "Host": "...", "Port": 587 } }
    }
  }
}
```

---

## 13. Korlátok és későbbi bővítések

- **Időzóna kezelés.** MVP: minden user `Europe/Budapest`. A
  `Reminder.TriggerUtc` UTC-ben tárolt, de a megjelenítés és quiet hours
  számítás `Europe/Budapest`. Multi-timezone támogatás (utazás közbeni
  Reminder) későbbi feature.
- **Smart escalation.** Az MVP fix policy-t használ; egy v2-ben gépi
  tanulás állíthatja be a default offset-eket a felhasználói viselkedés
  alapján (ha mindig 2 nap előtt akarja, ne 7 nap előtt szóljon).
- **Calendar integráció.** Egy elfogadott `Deadline` később exportálható
  Google Calendar-ba (kétirányú sync nem MVP, ADR-0003 + product-vision
  non-goal #4).
- **Mobil push.** Lásd 5.3.
- **Geo-fence emlékeztető** („otthon legközelebb, ha megérkezel, oltsd
  el a kazánt") — nem cél MVP-ben; LAN-only architektúra (ADR-0003) nem
  is támogatná.

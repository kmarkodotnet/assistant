# CR260710-02 — Proaktív napi/heti összefoglaló

> Státusz: DRAFT · Dátum: 2026-07-10 · Prioritás: **S** (Should)
> Kapcsolódó: [ai_features.md §3.1](../ai_features.md#31-proaktív-napiheti-összefoglaló),
> [reminder-engine.md](../reminder-engine.md), [ai-pipeline.md](../ai-pipeline.md)
> Jelenlegi állapot: **Nincs**

## Story

Mint családtag, szeretnék minden reggel egy rövid összefoglalót kapni
a napi teendőkről és a közelgő határidőkről, hogy ne kelljen aktívan
rákeresnem — a rendszer magától figyelmeztessen, mielőtt elfelejtenék
valamit.

## Cél

A jelenlegi rendszer csak *explicit módon beütemezett* emlékeztetőket
tüzel (amiket a felhasználó vagy az AI korábban létrehozott). Nincs olyan
proaktív mechanizmus, ami napi szinten összegzi az aktuális helyzetet
("ma mi van", "mi közeleg", "mi érkezett új"). Ez csökkentené az
elfelejtett határidők és be nem fizetett számlák kockázatát, és növelné a
napi használati gyakoriságot.

## Jelenlegi állapot

- `NotificationFeedRetentionJob` csak a régi, olvasott értesítéseket
  takarítja (90 nap után).
- `DueReminderDispatcher` és `EscalationScheduler` kizárólag a
  felhasználó vagy az AI által **explicit módon beütemezett** `Reminder`
  sorokat tüzeli/eszkalálja.
- Proaktív, digest-jellegű, magától generálódó összefoglaló job nincs.

## Elfogadási kritériumok (Given/When/Then)

- **Given** egy aktív felhasználói fiók a `quiet_hours` ablakon kívül,
  **When** eljön a napi digest-időpont (pl. 07:00), **Then** a
  felhasználó egy `notification_feed` bejegyzést kap az aznapi/holnapi
  reminderekkel, a következő 7 napban esedékes határidőkkel, és az elmúlt
  24 órában érkezett új dokumentumok számával.
- **Given** egy felhasználó, akinek nincs semmilyen aznapi/közelgő
  teendője, **Then** vagy egyáltalán nem kap digest-et, vagy egy rövid
  "nincs mai teendő" jellegű üzenetet kap (a duplikált zaj elkerülésére —
  eldöntendő tervezési kérdés).
- **Given** egy `Child` szerepkörű felhasználó, **When** digest generálódik
  számára, **Then** csak a hozzá kötött, nem-privát rekordok szerepelnek
  benne (RBAC-szűrt digest).
- **Given** egy adott naptári nap, **When** a job kétszer futna le
  valamiért (retry, restart), **Then** a felhasználó csak egy digest-et
  kap aznapra (idempotencia).

## Megvalósítási terv

1. Új Hangfire recurring job (`DailyDigestJob`), napi egy futtatással
   (reggel, pl. 07:00, a `quiet_hours` beállításokat figyelembe véve).
2. Családtagonként (RBAC-szűrve) lekérdezi: aznapi/holnapi `Reminder`-eket,
   a következő 7 napban esedékes `Deadline`-okat, az elmúlt 24 órában
   érkezett új `Document`-eket.
3. Rövid, sablon-alapú összefoglaló összeállítása (nem feltétlen kell LLM
   — az adat már strukturált; opcionálisan egy kis LLM-hívás csak a
   megfogalmazást "emberivé" teszi, a tényeket nem generálja).
4. `notification_feed` insert (`related_entity_type = 'DailyDigest'`),
   csatorna: InApp alapból, Email a meglévő `SmtpNotificationService`-en
   át opcionálisan (a `user_account.email_enabled`/`quiet_hours`
   beállítások szerint).
5. Idempotencia: egy felhasználó egy naptári napra csak egy digest-et
   kapjon — dedup-ellenőrzés `target_user_account_id` + aznapi dátum
   alapján a job futásakor.

## Érintett komponensek

- Új: `src/FamilyOs.Workers/Services/DailyDigestJob.cs`
- `src/FamilyOs.Workers/Program.cs` (Hangfire recurring job regisztráció)
- `src/FamilyOs.Infrastructure/Notifications/*` (meglévő notification
  szolgáltatások újrafelhasználása)

## Kifejezetten NEM cél

- Nem cél a digest tartalmának személyre szabott AI-elemzése (pl.
  prioritás-rangsorolás) — az első verzió egy egyszerű, strukturált lista.

# Kontrakt-delta — Proaktív napi összefoglaló (DailyDigestJob)

> CR: [CR260710-02](../change-requests/cr260710-02-proaktiv-napi-osszefoglalo.md)
> · Feature-id: `daily-digest` · Státusz: **Tervezve**
> · Architekturális döntés: [ADR-0011](../decisions/ADR-0011-daily-digest-backgroundservice.md)
> · Kapcsolódó: [ADR-0007 (Child RBAC)](../decisions/ADR-0007-child-szerepkor-rbac.md),
> [ADR-0008 (worker→realtime tiltás)](../decisions/ADR-0008-workers-realtime-jelzes.md)

Ez a dokumentum a **szerződés** a `backend-dev` és `frontend-dev` agentek
között. A kontrakt kötelező és teljes; eltérés → vissza az architect agenthez.
Kód (C#/Angular) implementálása NEM része ennek a dokumentumnak.

---

## 0. Összefoglaló döntések (ADR-0011)

| Kérdés | Döntés |
|---|---|
| Ütemezés | `BackgroundService` (NEM Hangfire recurring), poll-loop |
| "Nincs mai teendő" | **Nem küld** digestet (skip), lásd §5 |
| DB-séma változás | **Nincs** — `notification_feed.Type = "DailyDigest"`, nincs új oszlop, nincs migráció |
| Real-time push | **Nincs** — csak `notification_feed` insert (ADR-0008); FE a bell/polling úton látja |

---

## 1. `DailyDigestJob` — felelősség és ütemezés

**Típus:** `sealed class DailyDigestJob : BackgroundService`
**Fájl (új):** `src/FamilyOs.Workers/Services/DailyDigestJob.cs`
**Regisztráció:** `AddHostedService<DailyDigestJob>()` a `src/FamilyOs.Workers/Program.cs`-ben,
a többi `AddHostedService` mellé.
**Minta:** `NotificationFeedRetentionJob` / `DueReminderDispatcher` (scope-onkénti
`IServiceScopeFactory.CreateAsyncScope`, `LoggerMessage.Define` strukturált log,
`OperationCanceledException` kezelés a stoppingToken-re).

### 1.1 Loop-viselkedés

```
ExecuteAsync(stoppingToken):
    log started
    while not cancelled:
        try RunOnceAsync(stoppingToken)
        catch OperationCanceledException (stopping) -> break
        catch Exception ex -> log error (loop nem áll le)
        await Task.Delay(PollInterval, stoppingToken)   // alap: 15 perc
    log stopped
```

`RunOnceAsync` minden ébredéskor:
1. Kiszámolja a "mai" naptári napot (`digestDate`) és a helyi időt (§7 időkezelés).
2. Ha `DailyDigest:Enabled == false` → azonnal visszatér.
3. Lekéri az **aktív** felhasználókat: `IsActive == true && DeletedUtc == null`.
4. Felhasználónként (§3 RBAC-szűréssel) eldönti, jár-e **most** a digest:
   - `localNow >= RunAtLocal` (a napi digest-időpont már elmúlt), **és**
   - a felhasználó **nincs** `quiet_hours` ablakban (`IsQuietHour == false`), **és**
   - még **nincs** mai digestje (§6 idempotencia-ellenőrzés).
   Ha bármelyik nem teljesül → a felhasználó kimarad **ebben a poll-ciklusban**
   (a következő poll újra kiértékeli; így a quiet_hours természetesen halaszt).
5. Ha jár: összeállítja a digestet (§4 lekérdezések), és ha **nem üres** (§5),
   elküldi (§4.4 payload, InApp + opcionális Email).

### 1.2 Miért nem garantál a scheduler "napi egyszert"

A "napi egyszer / retry-biztos" garanciát az **idempotencia-kulcs** adja
(§6), nem a poll-loop. A 15 perces poll csak azt biztosítja, hogy a
digest-időpont után "hamarosan" kimenjen, és a quiet_hours utáni halasztás
működjön. Retry/restart során a kulcs miatt nem lesz dupla digest.

---

## 2. Csatornák és szolgáltatások (újrafelhasználás)

- **InApp (kötelező):** `INotificationService.SendAsync(envelope, NotificationChannel.InApp, ct)`.
  A DI-ben ez a `CompositeNotificationService` → `InAppNotificationService`, ami
  `notification_feed` sort ír és `IInAppNotificationPusher.PushAsync`-ot hív
  (worker-ben ez `NullNotificationPusher`, ADR-0008 — nincs cross-process push).
- **Email (opcionális):** `SendAsync(envelope, NotificationChannel.Email, ct)` →
  `SmtpNotificationService`. Az SMTP maga is ellenőrzi az `EmailEnabled` opt-outot
  és a hiányzó SMTP-konfigot (skip-el), de a job **explicit** feltétele §4.5.
- **Idempotencia:** az `InAppNotificationService` már dedup-ol `IdempotencyKey`-re
  (`AnyAsync`). A job **ezen felül** korai dedup-ellenőrzést végez (§6), hogy ne
  építse fel feleslegesen a digestet, és hogy az Email se menjen ki duplán.

Fájlok (nem módosítandók, csak hivatkozás):
`src/FamilyOs.Application/Abstractions/Notifications/INotificationService.cs`,
`src/FamilyOs.Infrastructure/Notifications/{Composite,InApp,Smtp}NotificationService.cs`.

---

## 3. RBAC-szűrés a digesthez

A digest **felhasználónként (UserAccount)** generálódik. A `Role` (`Admin | Adult | Child`)
határozza meg, mely rekordok kerülhetnek bele. Referencia-minta:
`FilterSearchHandler` és [ADR-0007](../decisions/ADR-0007-child-szerepkor-rbac.md).

| Role | Deadline / Document láthatóság a digestben |
|---|---|
| `Admin`, `Adult` | `!IsPrivate \|\| CreatedByUserAccountId == user.Id` (család-szintű nem-privát + saját privát) |
| `Child` | `!IsPrivate && RelatedFamilyMemberId == user.FamilyMemberId` (kizárólag hozzá kötött, nem-privát) |

- A **Reminder**-ek eleve `TargetUserAccountId`-hoz kötöttek → a `WHERE TargetUserAccountId == user.Id`
  önmagában RBAC-helyes minden szerepkörre (Child sem lát idegen remindert).
- A `user.FamilyMemberId` a `UserAccount` entitáson elérhető (nem kell join a
  Child-szűréshez a related_family_member_id ellen).
- A soft-delete mindenhol kötelező: `DeletedUtc == null`.

---

## 4. Adatforrás-lekérdezések (pontos definíció)

Jelölés: `now = <helyi most UTC-ben>` (§7), `today00 = <mai nap 00:00 helyi, UTC-re konvertálva>`,
`tomorrow24 = today00 + 48h` (a holnap végéig).

### 4.1 Ma/holnap esedékes reminderek

```
FROM Reminders r
WHERE r.TargetUserAccountId == user.Id
  AND r.Status == ReminderStatus.Scheduled
  AND r.DeletedUtc == null
  AND r.TriggerUtc >= today00
  AND r.TriggerUtc <  tomorrow24
ORDER BY r.TriggerUtc
```
Megjelenítendő mező: cím a nav. `Task.Title` ill. `Deadline.Title` szerint
(XOR — vagy `TaskId`, vagy `DeadlineId`), `TriggerUtc`.
Entitás: `FamilyOs.Domain.Entities.Reminder` (nav: `Task`, `Deadline`).

### 4.2 Következő 7 nap esedékes határidők

```
FROM Deadlines d
WHERE (RBAC §3 Deadline-szabály a user.Role szerint)
  AND d.DeletedUtc == null
  AND d.Status == DeadlineStatus.Upcoming
  AND d.DueDateUtc >= now
  AND d.DueDateUtc <  now + <DeadlineLookaheadDays> nap   // alap 7
ORDER BY d.DueDateUtc
```
Megjelenítendő mező: `Title`, `DueDateUtc`, `Category`.
Entitás: `FamilyOs.Domain.Entities.Deadline` (`DueDateUtc`, `Status`,
`RelatedFamilyMemberId`, `IsPrivate`, `CreatedByUserAccountId`).

### 4.3 Elmúlt 24 óra új dokumentumok

```
FROM Documents doc
WHERE (RBAC §3 Document-szabály a user.Role szerint)
  AND doc.DeletedUtc == null
  AND doc.CreatedUtc >= now - <DocumentLookbackHours> óra   // alap 24
```
A digest az elfogadási kritérium szerint a **darabszámot** közli (opcionálisan
a max. 3 legutóbbi cím). `Count` + (opcionálisan) `Title`-lista.
Entitás: `FamilyOs.Domain.Entities.Document` (`CreatedUtc`, `RelatedFamilyMemberId`,
`IsPrivate`, `CreatedByUserAccountId`).

### 4.4 `notification_feed` payload

Egyetlen `NotificationEnvelope`, amit a job összeállít:

| Mező | Érték |
|---|---|
| `UserId` | `user.Id` |
| `Type` | `"DailyDigest"` (állandó string; FE ez alapján ismeri fel — §8) |
| `Title` | `"Napi összefoglaló – {digestDate:yyyy. MM. dd.}"` |
| `Body` | §4.4.1 sablon (magyar, sima szöveg — a body `IsBodyHtml=false` az SMTP-ben) |
| `ActionUrl` | `"/dashboard"` |
| `IdempotencyKey` | `$"daily-digest-{user.Id}-{digestDate:yyyy-MM-dd}"` (§6) |

#### 4.4.1 Body-sablon (magyar, sorokra bontva)

A body soronként épül fel, csak a **nem üres** szekciók kerülnek bele.
Példa (a `{n}` behelyettesítendő):

```
Jó reggelt! Íme a mai áttekintés.

📅 Mai és holnapi emlékeztetők ({n}):
- {HH:mm} · {reminder cím}
- ...

⏳ Közelgő határidők (7 nap, {n}):
- {yyyy. MM. dd.} · {határidő cím} ({kategória})
- ...

📄 Új dokumentumok az elmúlt 24 órában: {n}
- {dokumentum cím}   (opcionális, max 3)
```

- Ha egy szekció 0 elemű → a szekció **teljes egészében kimarad** (fejléc is).
- Legfeljebb ~10 reminder / ~10 határidő / 3 dokumentumcím listázandó; e felett
  "… és további {k} tétel" zárósor.
- Az LLM-es "emberibb" megfogalmazás a CR szerint **opcionális és nem cél** az
  MVP-ben — a sablon-alapú szöveg a szerződés; LLM-hívás NEM kerül bele az első
  verzióba (a "kifejezetten NEM cél" szakasszal összhangban).

### 4.5 Email-csatorna feltétele

Az InApp után a job **akkor és csak akkor** hív `SendAsync(..., NotificationChannel.Email, ct)`,
ha:
- `user.EmailEnabled == true`, **és**
- a digest nem üres (§5), **és**
- a felhasználó nincs quiet_hours-ban (ezt a §1.1/4. lépés már garantálja a
  küldés pillanatában).

Az azonos `IdempotencyKey` miatt retry esetén sem lesz dupla email: a §6 korai
dedup a teljes felhasználót kihagyja, ha a mai InApp feed-sor már létezik.
Ha az SMTP nincs konfigurálva (`SmtpOptions.Host` üres), az SMTP szolgáltatás
csendben skip-el — a job ettől nem hibázik.

---

## 5. "Nincs mai teendő" eset (ADR-0011 döntés)

Ha `reminders.Count == 0 && deadlines.Count == 0 && newDocuments.Count == 0`:
- a job **nem** hoz létre `notification_feed` bejegyzést,
- **nem** küld emailt,
- `LogSkippedEmpty(userId)` debug/info log,
- **nem** ír idempotencia-markert (a felhasználó a következő napon újra
  kiértékelésre kerül; ugyanaznap nem próbálkozik újra, mert a poll-feltétel
  minden ciklusban ugyanazt az üres eredményt adná — ez elfogadható, mert a
  lekérdezés olcsó és a §6 kulcs hiánya nem okoz duplikációt).

Indoklás: lásd ADR-0011 (értesítési fáradtság elkerülése).

---

## 6. Idempotencia — pontos mechanizmus

**Kulcs:** `$"daily-digest-{user.Id}-{digestDate:yyyy-MM-dd}"`
(a `digestDate` a digest naptári napja helyi időben).

**Korai dedup (a job elején, felhasználónként, a digest összeállítása ELŐTT):**

```
var key = $"daily-digest-{user.Id}-{digestDate:yyyy-MM-dd}";
bool already = await db.NotificationFeed.AnyAsync(n => n.IdempotencyKey == key, ct);
if (already) { continue; }   // ma már kapott digestet -> kihagyás (InApp+Email is)
```

**Második védvonal:** az `InAppNotificationService.SendAsync` ugyanezt a kulcsot
újra ellenőrzi (`AnyAsync`) insert előtt — így párhuzamos worker-instancia vagy
verseny esetén sem lesz dupla sor.

Következmény: az idempotencia-marker maga a **létrejött `notification_feed` sor**
(a `Type="DailyDigest"` + `IdempotencyKey`). Nem kell külön marker-tábla.

---

## 7. Időkezelés és quiet_hours

- A rendszer minden időbélyeget UTC-ben tárol. A `quiet_hours` és a digest-időpont
  `HH:mm` **helyi idő** stringek.
- **Kötelező újrafelhasználni** a `DueReminderDispatcher` `IsQuietHour(start, end, now)`
  és `GetQuietHoursEnd(end, now)` logikáját (átfordulás éjfélen át kezelve).
  **Javaslat:** e két statikus metódust emeld ki közös helperbe
  (pl. `src/FamilyOs.Infrastructure/Notifications/QuietHours.cs` vagy
  `FamilyOs.Domain/Services/`), és a `DueReminderDispatcher` is arra hivatkozzon
  (refaktor, viselkedés nem változik). Ha nem emeled ki, **másold pontosan** a
  meglévő logikát — a két implementáció nem térhet el.
- **Konvenció-figyelmeztetés:** a meglévő `DueReminderDispatcher` a `quiet_hours`
  `HH:mm`-et közvetlenül `DateTime.UtcNow` ellen hasonlítja (nincs zóna-konverzió).
  A `DailyDigestJob` a `RunAtLocal`-t **ugyanabban a referenciakeretben** értékelje,
  mint a quiet_hours, hogy a két feltétel konzisztens legyen. A `DailyDigest:TimeZone`
  konfigkulcs előremutató (v2 zóna-helyes kezeléshez); az MVP a meglévő
  szerver-idő/UTC konvenciót követi. A tényleges zóna-korrekt kezelés
  **nem tárgya** ennek a feature-nek (külön CR, ha kell).

---

## 8. Konfiguráció (appsettings, Workers)

Új szekció a `src/FamilyOs.Workers/appsettings.json`-ba (env-változóból is
felülírható, a CLAUDE.md konvenció szerint: `DailyDigest__RunAtLocal` stb.):

```jsonc
"DailyDigest": {
  "Enabled": true,
  "RunAtLocal": "07:00",         // napi digest-időpont (helyi HH:mm)
  "TimeZone": "Europe/Budapest", // előremutató; MVP-ben nem konvertál (§7)
  "PollInterval": "00:15:00",    // BackgroundService loop periódus
  "DeadlineLookaheadDays": 7,    // §4.2 ablak
  "DocumentLookbackHours": 24    // §4.3 ablak
}
```

- Kösd `IOptions<DailyDigestOptions>`-hoz (új options osztály a Workers vagy
  Infrastructure rétegben, a `SmtpOptions` mintájára).
- Titok nem kerül a configba (nincs is titok itt).

---

## 9. Érintett fájlok

### Backend

**Új:**
- `src/FamilyOs.Workers/Services/DailyDigestJob.cs` — a BackgroundService.
- `src/FamilyOs.Workers/Services/DailyDigestOptions.cs` (vagy Infrastructure) —
  a §8 konfig-kötés.
- (opcionális) `src/FamilyOs.Infrastructure/Notifications/QuietHours.cs` — kiemelt
  quiet-hours helper (§7).

**Módosítandó:**
- `src/FamilyOs.Workers/Program.cs` — `AddHostedService<DailyDigestJob>()` +
  `services.Configure<DailyDigestOptions>(ctx.Configuration.GetSection("DailyDigest"))`.
- `src/FamilyOs.Workers/appsettings.json` — §8 szekció.
- (ha kiemelsz helpert) `src/FamilyOs.Workers/Services/DueReminderDispatcher.cs` —
  a saját `IsQuietHour`/`GetQuietHoursEnd` helyett a közös helper hívása
  (viselkedés-azonos refaktor).

**Nem módosítandó (csak használat):** entitások (`Reminder`, `Deadline`, `Document`,
`UserAccount`, `NotificationFeed`), notification-szolgáltatások.
**Nincs DB-migráció** (ADR-0011 — `Type="DailyDigest"`, nincs új oszlop).

### Frontend

> **FONTOS FE-megállapítás:** jelenleg **nincs** notification-feed lista/panel
> komponens. A `navbar.component.ts` bell csak az **olvasatlan darabszámot**
> mutatja (`getUnreadCount`) és a `/reminders` route-ra linkel. A
> `NotificationsApiService.getFeed()` **létezik, de sehol nincs felhasználva**;
> nincs route/komponens, ami a `NotificationDto`-kat (cím/body/actionUrl/type)
> megjelenítené. A `/reminders` oldal a **Reminder** entitásokat listázza,
> NEM a `notification_feed`-et — a digest ott **nem jelenne meg**.
>
> Ezért a digest olvashatóságához a FE-nek **új notification-feed felület** kell.
> Ez a digest feature FE-részének a magja, nem mellékes.

**Új (FE, javaslat):**
- Notification-feed panel/oldal, ami `NotificationsApiService.getFeed()`-et
  hívja és listázza a `NotificationDto`-kat (cím, body többsoros megjelenítés,
  `actionUrl` navigáció, olvasott/olvasatlan állapot, `markRead`).
  Elérés: a bell (`navbar-bell`) nyisson dropdown-t vagy vezessen új
  `/notifications` route-ra (a jelenlegi `/reminders`-link helyett vagy mellett).
- `type` alapú megjelenítés: a `Type === 'DailyDigest'` kapjon saját ikont/
  címkét (pl. 📋 "Napi összefoglaló"), a `body` sortöréseit tartsa meg
  (`white-space: pre-line`). Ismeretlen `type` → alap ikon (ne törjön el).

**Módosítandó (FE):**
- `frontend/src/app/layout/navbar.component.ts` — a bell viselkedése
  (feed-panel megnyitása vagy `/notifications` route), az `unreadCount` frissítés
  megtartásával.
- `frontend/src/app/app.routes.ts` — új `/notifications` route (ha külön oldal).
- (nincs szükség új DTO-mezőre: a `NotificationDto.type` már string, a
  `DailyDigest` érték típusszinten kompatibilis.)

**Nem cél a FE-n:** real-time push (ADR-0008) — a meglévő polling/olvasatlan-szám
mechanizmus elég; a digest a következő poll-nál/oldalbetöltésnél jelenik meg.

---

## 10. Acceptance-leképezés (a QA agentnek)

| CR kritérium | Kontrakt-fedés |
|---|---|
| Aktív user, quiet_hours-on kívül, digest-időben → feed-bejegyzés a 3 forrással | §1.1, §4 |
| Nincs teendő → nem kap digestet | §5 (ADR-0011) |
| Child → csak hozzá kötött, nem-privát rekordok | §3 (ADR-0007) |
| Job kétszer fut → csak egy digest | §6 idempotencia |
| Email opcionálisan, `EmailEnabled`/quiet_hours szerint | §4.5 |
</content>

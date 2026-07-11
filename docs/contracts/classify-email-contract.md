# Kontrakt-delta — Fontos e-mailek AI-alapú felismerése (ClassifyEmailJob)

> CR: [CR260710-03](../change-requests/cr260710-03-fontos-emailek-felismerese.md)
> · Feature-id: `important-email` · Státusz: **Tervezve**
> · Architekturális döntés: [ADR-0012](../decisions/ADR-0012-classifyemail-korai-fontossag.md)
> · Kapcsolódó: [ADR-0008 (worker→realtime tiltás)](../decisions/ADR-0008-workers-realtime-jelzes.md),
> [daily-digest-contract.md](daily-digest-contract.md) (payload-stílus referencia)

Ez a dokumentum a **szerződés** a `db-engineer` és `backend-dev` agentek között.
A kontrakt kötelező és teljes; eltérés → vissza az architect agenthez.
Kód (C#) implementálása NEM része ennek a dokumentumnak.

---

## 0. Összefoglaló döntések (ADR-0012)

| Kérdés | Döntés |
|---|---|
| Új `AiJobType.ClassifyEmail` DB-migrációt igényel? | **NEM** — az enum string-konvertált (`HasConversion<string>`), nincs Postgres natív enum és nincs DB check-constraint (§1.3). Csak C# enum-bővítés. |
| `email_message` séma-változás | **IGEN** — 3 új nullable oszlop (`importance`, `category`, `has_deadline_hint`) + 1 parciális index (§6). |
| `High` fontosságú e-mail értesítés címzettje | **A háztartás legrégebbi aktív `Admin` `UserAccount`-ja** (a Document-attribúcióval azonos minta), NEM fan-out minden Adultnak (§4). |
| `ActionUrl` | `"/documents"` — a Document ekkor még nem létezik, nincs deep-link konkrét dokumentumra (§4). |
| Csatorna | **Csak InApp** (ADR-0008); Email előremutató (§4). |
| `HasDeadlineHint` | Pusztán informatív jelzés; **NEM** módosít job-ütemezést, az `ExtractDeadlines` a normál pipeline-on változatlanul lefut (§5). |
| `ClassifyEmail` vs. `Classify` | **Két különböző, egymást kiegészítő mechanizmus**; a `ClassifyEmail` nem váltja ki a `Classify` noise-discard logikáját (§7, ADR-0012). |
| Konkurencia (`ClassifyEmail` ∥ `ExtractText` ugyanazon a soron) | Nincs row-version / lock; EF per-oszlop UPDATE elég, mert a két job diszjunkt oszlophalmazt ír (§8, ADR-0012). |
| Frontend-módosítás ebben a körben | **NEM cél** — a `High` e-mail a `notification_feed`-en (a digest-feature `/notifications` felületén) jelenik meg; nincs Document-szintű importance-mező, így a `/documents` lista nem szűrne rá (§9). |

---

## 1. A pipeline beillesztési pontjai (normatív alap)

> **Megjegyzés a CR-hez:** a CR260710-03 "Érintett komponensek" szakasza az
> `EmailIngestionPoller` / `SyncSourceCommandHandler` fájlokat nevezi meg; ezek a
> hivatkozások elavultak. Az e-mail-ingest tényleges, normatív belépési pontja a
> `GmailIngestionService.SyncAsync`. Az alábbi §-ok ez utóbbira épülnek.

### 1.1 Job felsorakoztatása (enqueue)

**Fájl (módosítandó):** `src/FamilyOs.Infrastructure.Ai/Email/GmailIngestionService.cs`,
a `SyncAsync` foreach-ciklusa (jelenleg ~137–142. sor), ahol minden új
`EmailMessage`-hez már felsorakozik egy `ExtractText` job.

**Elvárás:** minden új `EmailMessage`-re a meglévő `ExtractText` job **mellé** egy
második jobot is fel kell venni ugyanabban a `SaveChangesAsync`-ban:

```
db.EmailMessages.Add(parsed);
db.AiProcessingJobs.Add(AiProcessingJob.CreateForEmailMessage(AiJobType.ExtractText,   parsed.Id));  // meglévő
db.AiProcessingJobs.Add(AiProcessingJob.CreateForEmailMessage(AiJobType.ClassifyEmail, parsed.Id));  // ÚJ
```

- Mindkét job `TargetType = JobTargetType.EmailMessage`, azonos `TargetId`
  (`parsed.Id`), különböző `JobType`. A `CreateForEmailMessage` factory már létezik
  (`AiProcessingJob.cs`), nem kell módosítani.
- A két job **egymástól függetlenül, párhuzamosan** fut (§8 konkurencia).

### 1.2 Job diszpécselése (dispatch)

**Fájl (módosítandó):** `src/FamilyOs.Workers/Services/AiJobExecutor.cs`, a
`DispatchAsync` `switch(job.JobType)`-ja.

**Elvárás:** új `case`:

```
case AiJobType.ClassifyEmail:
    var classifyEmailRunner = _serviceProvider.GetRequiredService<ClassifyEmailJobRunner>();
    await classifyEmailRunner.RunAsync(job, ct);
    break;
```

- A `ClassifyEmail` job **NEM** kerül a `parallelTypes` listába (az a Document-
  pipeline finalizálásához (`PipelineOrchestrator.CheckAndFinalizeAsync`) tartozik,
  ami Document-célú jobokra vonatkozik). A `ClassifyEmail` `EmailMessage`-t céloz,
  nem `Document`-et — ne érintse a finalizálási ágat.

### 1.3 `AiJobType` enum-bővítés — nincs migráció

**Fájl (módosítandó):** `src/FamilyOs.Domain/Enums/AiJobType.cs` — új tag: `ClassifyEmail`.

Az `AiProcessingJob.JobType` EF-konfigja string-alapú
(`HasConversion<string>().HasMaxLength(50)`, `AiProcessingJobConfiguration.cs`).
Nincs Postgres natív enum-típus és nincs DB check-constraint az értékekre, ezért az
új enum-érték **nem igényel adatbázis-migrációt** — se a `db-engineer`, se a
`backend-dev` ne írjon migrációt az enum-bővítés miatt. (Az egyetlen migráció ebben
a feature-ben az `email_message` 3 új oszlopa, §6.)

---

## 2. `IEmailImportanceClassifier` — az AI-osztályozó absztrakció

**Új absztrakció (Application réteg):**
`src/FamilyOs.Application/Abstractions/Ai/IEmailImportanceClassifier.cs`

Minta: a meglévő `IDocumentClassifier` / `OllamaDocumentClassifier`.

```
public enum   -> lásd §3 EmailImportance
public record EmailImportanceResult(
    EmailImportance Importance,   // High | Medium | Low
    string? Category,             // szabad szöveg, pl. "hivatalos", "szamla", "hirlevel"
    bool HasDeadlineHint);        // van-e explicit határidő az e-mail szövegében

public interface IEmailImportanceClassifier
{
    Task<EmailImportanceResult> ClassifyAsync(string subject, string? bodyText, CancellationToken ct = default);
}
```

**Új implementáció (Infrastructure.Ai réteg):**
`src/FamilyOs.Infrastructure.Ai/Tasks/OllamaEmailClassifier.cs`

Kötelező mintakövetés az `OllamaDocumentClassifier` alapján:
1. `IAiProviderFactory.GetProvider()`.
2. `PromptTemplate.Load(PromptCatalog.ClassifyEmail)` + `PromptTemplate.Replace(...)`
   a `{{subject}}` és `{{body}}` helyettesítőkre.
3. Body-truncálás: `bodyText` max. **8000** karakter (subject nem truncálódik).
   Ha `bodyText` null/üres → csak a subject alapján osztályoz.
4. `AiPrompt` összeállítás (`PromptId = PromptCatalog.ClassifyEmail`,
   `PromptVersion = PromptCatalog.GetVersion(...)`), `provider.CompleteAsync(prompt, ct)`.
5. **Védekező JSON-parse `try/catch (JsonException)`-csel.** Hiba / hiányzó mező
   esetén **biztonságos default**: `Importance = Low`, `Category = null`,
   `HasDeadlineHint = false`. **Soha ne dobjon kivételt** a parse-hiba miatt
   (a `Low` default nem generál `High`-értesítést — nincs zaj parse-hibából).

**DI-regisztráció (módosítandó):**
`src/FamilyOs.Infrastructure.Ai/DependencyInjection/AiServiceRegistration.cs`, a többi
`IDocumentClassifier`/`I...Extractor` mellé:

```
services.AddScoped<IEmailImportanceClassifier, OllamaEmailClassifier>();
```

---

## 3. `EmailImportance` enum + `EmailMessage` entitás-bővítés

**Új enum (Domain):** `src/FamilyOs.Domain/Enums/EmailImportance.cs`

```
public enum EmailImportance { High, Medium, Low }
```

(String-konvertáltként tárolva, mint az `IngestStatus`/`AiJobType` — nincs DB
check-constraint, nincs natív pg-enum.)

**Entitás-bővítés (módosítandó):** `src/FamilyOs.Domain/Entities/EmailMessage.cs`

Új, `private set`-es property-k (mind nullable — a job lefutásáig NULL):

| Property | Típus | Jelentés |
|---|---|---|
| `Importance` | `EmailImportance?` | fontossági szint |
| `Category` | `string?` | AI-kategória (szabad szöveg) |
| `HasDeadlineHint` | `bool?` | van-e explicit határidő a szövegben |

Új setter-metódus a meglévő `SetBody`/`MarkProcessed` mintájára:

```
public void SetImportance(EmailImportance importance, string? category, bool hasDeadlineHint)
{
    Importance = importance;
    Category = category;
    HasDeadlineHint = hasDeadlineHint;
    UpdatedUtc = DateTime.UtcNow;
}
```

> **Fontos (§8):** ez a metódus **kizárólag** a fenti 3 oszlopot + `UpdatedUtc`-t
> módosítja. Nem nyúl az `IngestStatus`/`ProcessedUtc`-hoz — így az `ExtractText`
> jobbal nincs oszlop-ütközés.

**EF-konfig (módosítandó):**
`src/FamilyOs.Infrastructure/Persistence/Configurations/EmailMessageConfiguration.cs`

```
builder.Property(x => x.Importance)
    .HasConversion<string>()          // nullable enum → nullable varchar
    .HasMaxLength(10);                // "High"|"Medium"|"Low"

builder.Property(x => x.Category)
    .HasMaxLength(100);

builder.Property(x => x.HasDeadlineHint);   // nullable boolean, alap-mapping

builder.HasIndex(x => x.Importance)
    .HasDatabaseName("ix_email_message_importance_high")
    .HasFilter("importance = 'High'");       // parciális index, lásd §6
```

---

## 4. `ClassifyEmailJobRunner` — algoritmus

**Új runner:** `src/FamilyOs.Workers/Services/ClassifyEmailJobRunner.cs`
**Minta:** `ClassifyJobRunner` / `ExtractTextJobRunner` (scoped, `FamilyOsDbContext`,
`LoggerMessage.Define` strukturált log).

**Függőségek (ctor-injektálás):** `FamilyOsDbContext db`,
`IEmailImportanceClassifier classifier`, `INotificationService notifications`,
`ILogger<ClassifyEmailJobRunner> logger`.

**DI-regisztráció (módosítandó):** `src/FamilyOs.Workers/Program.cs`, a többi
`AddScoped<...JobRunner>()` mellé: `services.AddScoped<ClassifyEmailJobRunner>();`

### 4.1 `RunAsync(AiProcessingJob job, CancellationToken ct)`

```
1. email = await db.EmailMessages.FirstOrDefaultAsync(e => e.Id == job.TargetId, ct)
   - ha null → LogEmailNotFound, return (job Done — nincs mit tenni).

2. result = await classifier.ClassifyAsync(email.Subject, email.BodyText, ct)
   // parse-hiba esetén a classifier már Low defaultot ad (§2.5), itt nincs try/catch.

3. email.SetImportance(result.Importance, result.Category, result.HasDeadlineHint)
   await db.SaveChangesAsync(ct)     // csak a 3 oszlop + UpdatedUtc íródik (§8)

4. HA result.Importance == EmailImportance.High:
       await SendImportantEmailNotificationAsync(email, result, ct)   // §4.2

5. LogClassified(email.Id, result.Importance)
```

- **Idempotencia / újrafutás:** a `SetImportance` egyszerűen felülírja a korábbi
  értéket (idempotens). Az értesítés dedup-ját az `IdempotencyKey` adja (§4.2), így
  retry / manuális re-run **nem** hoz létre dupla `notification_feed` sort.
- **Üres body:** ha `email.BodyText` null/üres, a classifier a subjectből dolgozik
  (§2.3). Ez a job **nem** állít `IngestStatus.Failed`-et (azt az `ExtractText`
  kezeli a saját üres-body ágán) — a `ClassifyEmail` sosem ír `IngestStatus`-t.

### 4.2 `High` értesítés — `SendImportantEmailNotificationAsync`

```
ownerUserId = await db.UserAccounts
    .Where(u => u.Role == UserRole.Admin && u.DeletedUtc == null)
    .OrderBy(u => u.CreatedUtc)
    .Select(u => u.Id)
    .FirstOrDefaultAsync(ct);

if (ownerUserId == Guid.Empty) { LogNoAdminForNotification(email.Id); return; }  // guard

var envelope = new NotificationEnvelope(
    UserId:         ownerUserId,
    Type:           "ImportantEmail",
    Title:          $"Fontos e-mail: {Truncate(email.Subject, 120)}",
    Body:           $"Feladó: {email.FromAddress}\nTárgy: {email.Subject}"
                    + (result.Category is null ? "" : $"\nKategória: {result.Category}")
                    + (result.HasDeadlineHint == true ? "\nHatáridőt tartalmazhat." : ""),
    ActionUrl:      "/documents",
    IdempotencyKey: $"important-email-{email.Id}");

await notifications.SendAsync(envelope, NotificationChannel.InApp, ct);
```

| Mező | Érték | Indoklás |
|---|---|---|
| `UserId` | legrégebbi aktív `Admin` | A Document-attribúcióval (`ExtractTextJobRunner.RunForEmailAsync`) **azonos minta**; a Gmail-forrást az admin köti be, a Document ekkor még nem létezik, `RelatedFamilyMember` ismeretlen → determinisztikus, single-recipient, nincs fan-out zaj (ADR-0012). |
| `Type` | `"ImportantEmail"` | Állandó string; a FE (jövőben) ez alapján ismeri fel; a digest `"DailyDigest"` mintáját követi. |
| `ActionUrl` | `"/documents"` | A Document ekkor még nem létezik → nincs konkrét deep-link; a `/documents` inbox az a hely, ahol az e-mail hamarosan Document-ként megjelenik. |
| `IdempotencyKey` | `$"important-email-{email.Id}"` | Per-email dedup; retry-biztos (az `InAppNotificationService` `AnyAsync`-kal is dedup-ol a kulcsra). |
| Csatorna | `InApp` | ADR-0008 (worker→InApp); Email-fan-out per beérkező fontos e-mail zaj lenne. Email v2. |

- `Body` sima szöveg (`\n` sortörésekkel), `IsBodyHtml=false`.
- `Truncate` egy egyszerű helper a runnerben (nincs külön absztrakció).

---

## 5. `HasDeadlineHint` — pusztán informatív

- A `HasDeadlineHint` **kizárólag jelzés** (UI-n megjeleníthető, itt a `High`
  értesítés Body-jában is), amit az AI a subject/body szövegében talált explicit
  határidő alapján ad.
- **NEM** módosít semmilyen job-ütemezést. A CR "az `ExtractDeadlines` job továbbra
  is lefusson rá" kritériumát a **meglévő normál pipeline** teljesíti: az
  `ExtractText` job létrehozza a Document-et, majd a szokásos parallel jobtípusok
  (köztük `ExtractDeadlines`) automatikusan lefutnak a Document-en — a
  `ClassifyEmail` jobtól **függetlenül**. A `ClassifyEmail` nem sorakoztat fel és nem
  hagy ki `ExtractDeadlines` jobot.

---

## 6. DB-migráció specifikáció (db-engineer)

**Egyetlen migráció**, EF Core, konvencionális névvel (a mappában:
`YYYYMMDDHHMMSS_Description`), javasolt:
`src/FamilyOs.Infrastructure/Persistence/Migrations/20260711000001_AddEmailMessageImportance.cs`.

**Séma:** `app.email_message` táblára 3 új oszlop + 1 parciális index.

| Oszlop | Típus | Null? | Alapérték | Megjegyzés |
|---|---|---|---|---|
| `importance` | `varchar(10)` | **NULL** | — | `'High'` \| `'Medium'` \| `'Low'`; NULL amíg a `ClassifyEmail` job le nem fut. Nincs check-constraint (konzisztens az `ingest_status`/`AiJobType` string-enum konvencióval). |
| `category` | `varchar(100)` | **NULL** | — | AI-kategória (szabad szöveg). |
| `has_deadline_hint` | `boolean` | **NULL** | — | NULL = még nem osztályozott; `true`/`false` a job után. |

**Index (parciális):**

```sql
CREATE INDEX ix_email_message_importance_high
    ON app.email_message (importance)
    WHERE importance = 'High';
```

- **Indoklás:** a CR és az `ai_features.md §3.2` explicit anticipálja a jövőbeli
  "fontos e-mailek" UI-szűrőt. A parciális, csak `'High'`-ra szűrt index közel
  nulla költségű (a `High` a kisebbség), és követi a táblán már meglévő
  `ix_email_message_ingest_status_pending` parciális-index konvenciót — így a
  későbbi szűrő-lekérdezés nem igényel újabb migrációt.
- A `Down()` mind a 3 oszlopot és az indexet eldobja.
- **Nincs** más migráció ebben a feature-ben (az `AiJobType.ClassifyEmail`
  enum-bővítés migráció-mentes, §1.3).

---

## 7. `ClassifyEmail` vs. `Classify` — a két mechanizmus elhatárolása (kötelező olvasmány a backend-devnek)

A két job **különböző célt, célentitást és időpontot** szolgál — **NE vond össze,
NE helyettesítsd egyikkel a másikat.**

| | `ClassifyEmail` (ÚJ, ez a feature) | `Classify` (meglévő) |
|---|---|---|
| Célentitás | `EmailMessage` | `Document` (annak `DocumentText`-je) |
| Időpont | **Korai** — ingestkor, a Document létrejötte előtt/mellett, gyors | **Késői** — a Document + szövegkinyerés után, a teljes pipeline része |
| Output | `importance` / `category` / `has_deadline_hint` az `EmailMessage`-en + `High` esetén azonnali `notification_feed` | `DocumentTag`/`DocumentTopic` társítás, `ExtractFacet` chaining |
| Noise-discard | **NINCS** — a `ClassifyEmail` sosem töröl/soft-delete-el semmit | **VAN** — ha egyetlen ismert topicot sem talál, az e-mail-eredetű Document-et **soft-delete-eli** (`ClassifyJobRunner.cs` ~148. sor, "treated as noise") |

- A `ClassifyEmail` **kiegészíti**, nem kiváltja a `Classify` noise-discard logikáját:
  a `ClassifyEmail` csak egy korai fontossági jelzést tesz hozzá; a hirdetés-jellegű
  (`Low`) e-mail attól még végigmegy a normál pipeline-on, és ha a **későbbi**
  `Classify` job úgy dönt, hogy egyetlen topic sem illik rá, akkor (és csak akkor) a
  meglévő logika soft-deleteli a Document-et. A `ClassifyEmail` `Low` besorolása
  önmagában **nem** vált ki törlést.
- A CR "alacsony fontosságú, hirdetés-jellegű e-mail → nem generál azonnali
  értesítést, csak a normál pipeline-on megy át" kritériuma pontosan ezt jelenti:
  `Low`/`Medium` → nincs `notification_feed` insert (§4.1/4. lépés), de a pipeline
  (és annak végén a `Classify` noise-discard) érintetlenül fut.

---

## 8. Konkurencia-döntés (ADR-0012)

A `ClassifyEmail` és az `ExtractText` job **ugyanarra az `EmailMessage` sorra**,
de **külön `DbContext`-scope-ból**, potenciálisan **párhuzamosan** fut (mindkettő
`CreateForEmailMessage`-dzsel, azonos `TargetId`, más `JobType`).

**Döntés: nincs szükség row-version-re, optimista concurrency token-re vagy
pesszimista lockra.** Indoklás:

- A két job **diszjunkt oszlophalmazt** ír:
  - `ClassifyEmail` → `importance`, `category`, `has_deadline_hint` (+ `UpdatedUtc`),
  - `ExtractText` (`RunForEmailAsync`) → `ingest_status`, `processed_utc`
    (+ `UpdatedUtc`), és külön táblákba `Document`/`DocumentText`.
- Az EF Core alapértelmezetten **entitásonként csak a ténylegesen módosított
  oszlopokat** teszi az `UPDATE SET`-be (nem a teljes sort). Mivel mindkét scope a
  saját, frissen betöltött entitását követi és csak a saját property-jeit módosítja,
  egyik `SaveChanges` sem írja felül a másik oszlopait — függetlenül a futási
  sorrendtől.
- **Egyetlen közös oszlop:** `UpdatedUtc`. Erre last-writer-wins érvényes, ami
  **ártalmatlan** (csak egy audit-timestamp; egyik írás sem jelent adatvesztést).
- Nincs "read-modify-write" ütközés: egyik job sem az érték a *másik* job által írt
  oszlopon alapuló feltételes írást végez.

Következmény: a `SetImportance` szigorúan csak a 3 fontosság-oszlopot + `UpdatedUtc`-t
módosíthatja (§3), a `ClassifyEmail` runner soha nem hívhat `MarkProcessed()`/
`MarkFailed()`-et (azok az `IngestStatus`-t írják, ami az `ExtractText` felségterülete).

---

## 9. Frontend — NEM cél ebben a körben

**Döntés: nincs FE-módosítás ehhez a feature-höz.** Indoklás:

- A `High` e-mail a `notification_feed`-en (`Type = "ImportantEmail"`) jelenik meg.
  A digest-feature (CR260710-02) most építi a `/notifications` felületet, ami a
  `NotificationsApiService.getFeed()`-et listázza; egy ismeretlen `type` az ott
  előírt "alap ikon → ne törjön el" viselkedéssel automatikusan megjeleníthető.
  Külön FE-munka nélkül is olvasható.
- Document-szintű `importance` mező **nincs** (a fontosság az `EmailMessage`-en él,
  nem a Document-en) → a `/documents` lista migráció nélkül **nem** tudna rá szűrni,
  ezért a "fontos jelvény a dokumentumlistán" MVP-ben nem reális FE-cél.
- A CR "Kifejezetten NEM cél" szakasza sem ír elő FE-t; a backend-only megoldás
  teljesíti mind a négy elfogadási kritériumot.

> **Előremutató (nem ebben a körben):** ha később kell dedikált "Fontos e-mailek"
> UI-nézet vagy a `notification_feed`-en `"ImportantEmail"`-specifikus ikon/címke,
> az külön CR — a §6 parciális index már előkészíti a backend-oldali szűrést.

---

## 10. Érintett fájlok

### Backend (backend-dev)

**Új:**
- `src/FamilyOs.Domain/Enums/EmailImportance.cs` — `High|Medium|Low` enum.
- `src/FamilyOs.Application/Abstractions/Ai/IEmailImportanceClassifier.cs` —
  absztrakció + `EmailImportanceResult` record (§2).
- `src/FamilyOs.Infrastructure.Ai/Tasks/OllamaEmailClassifier.cs` — implementáció (§2).
- `src/FamilyOs.Infrastructure.Ai/Prompts/classify-email.v1.txt` — prompt (§11).
- `src/FamilyOs.Workers/Services/ClassifyEmailJobRunner.cs` — a runner (§4).

**Módosítandó:**
- `src/FamilyOs.Domain/Enums/AiJobType.cs` — új tag `ClassifyEmail` (§1.3).
- `src/FamilyOs.Domain/Entities/EmailMessage.cs` — 3 property + `SetImportance` (§3).
- `src/FamilyOs.Infrastructure.Ai/Email/GmailIngestionService.cs` — 2. job enqueue (§1.1).
- `src/FamilyOs.Infrastructure.Ai/Prompts/PromptCatalog.cs` — `ClassifyEmail` konstans (§11).
- `src/FamilyOs.Infrastructure.Ai/DependencyInjection/AiServiceRegistration.cs` —
  `IEmailImportanceClassifier` regisztráció (§2).
- `src/FamilyOs.Workers/Services/AiJobExecutor.cs` — új `case ClassifyEmail` (§1.2).
- `src/FamilyOs.Workers/Program.cs` — `AddScoped<ClassifyEmailJobRunner>()` (§4).

**Nem módosítandó (csak használat):** `AiProcessingJob` (a `CreateForEmailMessage`
factory kész), `INotificationService`/`NotificationEnvelope`, notification-szolgáltatások.

### DB (db-engineer)

**Új:**
- `src/FamilyOs.Infrastructure/Persistence/Migrations/20260711000001_AddEmailMessageImportance.cs`
  — 3 oszlop + parciális index (§6).
- `FamilyOsDbContextModelSnapshot.cs` frissül a migráció generálásakor (automatikus).

**Módosítandó:**
- `src/FamilyOs.Infrastructure/Persistence/Configurations/EmailMessageConfiguration.cs`
  — property-mapping + index (§3). *(A backend-dev-vel közös fájl-terület: az entitás
  a Domain-ben, a mapping itt; a migráció ebből generálódik. Koordináció: a
  `db-engineer` a mapping + migráció gazdája, a `backend-dev` az entitás-property-ké.)*

### Frontend

- **Nincs** (§9).

---

## 11. Prompt-specifikáció

**Fájl (új):** `src/FamilyOs.Infrastructure.Ai/Prompts/classify-email.v1.txt`
(a `.csproj` `Prompts\*.txt` glob már embedded-resource-ként fordítja — nincs
csproj-módosítás).

**`PromptCatalog.cs` (módosítandó):** új konstans:
```
public const string ClassifyEmail = "classify-email.v1.txt";
```

**Prompt-tartalom (magyar, kizárólag JSON-választ kér, a `classify.v1.txt` mintájára):**

```
Osztalyozd az alabbi e-mailt a csalad szempontjabol. Add meg a fontossagi szintet,
egy rovid kategoriat, es hogy tartalmaz-e explicit hataridot (datum/esedekesseg).

Fontossag:
- High: hivatalos, hatarozott hataridos, surgos, penzugyi kotelezettseg, hatosagi/orvosi ertesito.
- Medium: szemelyes vagy hasznos, de nem surgos.
- Low: hirlevel, reklam, ertesites, automatikus rendszeruzenet.

Targy:
{{subject}}

Szoveg:
{{body}}

Valasz kizarolag ervenyes JSON formatumban:
{"importance": "High|Medium|Low", "category": "...", "hasDeadline": true|false}
```

- Az `OllamaEmailClassifier` a JSON-ból: `importance` → `EmailImportance` (parse-hiba
  vagy ismeretlen érték → `Low`), `category` → `string?` (üres/`"null"` → null),
  `hasDeadline` → `bool` (hiány → `false`).

---

## 12. Acceptance-leképezés (a QA agentnek)

| CR kritérium (Given/When/Then) | Kontrakt-fedés |
|---|---|
| Új e-mail → fontossági szint + kategória a teljes pipeline előtt | §1.1 (enqueue ingestkor), §4.1 |
| `High` e-mail → azonnali `notification_feed` bejegyzés | §4.2 (`Type="ImportantEmail"`, InApp) |
| Explicit határidős e-mail → `hasDeadline` jelzés + `ExtractDeadlines` továbbra is fut | §5 (informatív hint + normál pipeline) |
| Alacsony fontosságú, hirdetés-jellegű e-mail → nincs azonnali értesítés, csak normál pipeline | §4.1/4. (csak `High` értesít), §7 (a `Classify` noise-discard érintetlen) |
| Retry / kétszer futó job → nincs dupla értesítés | §4.1 idempotencia + §4.2 `IdempotencyKey` |
| `ClassifyEmail` ∥ `ExtractText` ugyanazon a soron → nincs adatvesztés | §8 konkurencia |

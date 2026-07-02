# Architektúra — Family OS

> Státusz: DRAFT v0.1 · Dátum: 2026-06-26 · Nyelv: magyar
> Kapcsolódó: [product-vision.md](product-vision.md), [domain-model.md](domain-model.md),
> [database-schema.md](database-schema.md), [ai-pipeline.md](ai-pipeline.md),
> [security-privacy.md](security-privacy.md)

---

## 1. Architekturális stílus és vezérlőelvek

A Family OS **Clean Architecture** felépítést követ, négy gyűrűvel (Domain →
Application → Infrastructure/AI → API/Workers). A célok:

1. **Függőség kifelé tilos.** A `Domain` projekt **semmilyen** külső csomagot
   nem importál (sem EF Core, sem Hangfire, sem HTTP, sem AI SDK). Az
   `Application` projekt csak a `Domain`-t és pár standard absztrakciót
   (MediatR, FluentValidation, `Microsoft.Extensions.*`) ismer.
2. **AI provider-függetlenség.** A backend **nem hivatkozhat** direktben
   semmilyen konkrét AI szolgáltatóra (Ollama, Anthropic, OpenAI). Mindenhol
   az `IAiProvider` és a hozzá tartozó kontraktok mennek. Adapter osztályok
   csak az `Infrastructure.Ai` projektben élnek.
3. **Durable, eventually-consistent feldolgozás.** A „PC nem mindig fent"
   feltételezés miatt a hosszú futású munkák (OCR, AI hívások, embedding)
   sorba állítva futnak, újraindítást túlélően. Lásd 7. szakasz.
4. **Single-tenant, LAN-only.** Nincs multi-tenant middleware, nincs
   publikus végpont, nincs CDN.
5. **Magyar nyelv first-class.** Az API hibakód-szövegek, a UI, az AI promptok
   alap-template-jei magyar nyelvűek; angol fallback van a logokon.
6. **No business logic in controllers** és **no AI calls from controllers** —
   minden parancs/lekérdezés MediatR handler az Application-ben.

---

## 2. Solution szerkezet

A backend egy .NET solution (`FamilyOs.sln`), az alábbi projektekkel:

```
src/
├─ FamilyOs.Domain/                    # tiszta C#, semmilyen csomag
├─ FamilyOs.Application/               # use case-ek (MediatR, FluentValidation)
├─ FamilyOs.Infrastructure/            # EF Core, fájltár, Hangfire, Auth, e-mail
├─ FamilyOs.Infrastructure.Ai/         # Ollama / Anthropic / OpenAI adapterek
├─ FamilyOs.Api/                       # ASP.NET Core minimal API host
└─ FamilyOs.Workers/                   # Hangfire worker host (különálló process)

tests/
├─ FamilyOs.Domain.Tests/              # xUnit, tiszta unit
├─ FamilyOs.Application.Tests/         # use case unit + integration
├─ FamilyOs.Infrastructure.Tests/      # Testcontainers (Postgres) integráció
└─ FamilyOs.Api.IntegrationTests/      # WebApplicationFactory
```

### Függőségi gráf

```
       Api  ──────┐
                  ▼
              Application  ─►  Domain
                  ▲             ▲
                  │             │
Infrastructure ───┘             │
Infrastructure.Ai ──────────────┘
       Workers  ──► Application + Infrastructure + Infrastructure.Ai
                    (composition root #2)
```

Két composition root: `Api` és `Workers`. Mindkettő ugyanazt az
Application + Infrastructure réteget használja, de eltérő hosztot
(WebApplication vs. BackgroundService) és eltérő middleware-stacket.
A `Workers` **nem** hallgat HTTP-n.

---

## 3. Réteg-felelősségek

### 3.1 Domain (`FamilyOs.Domain`)

- **Entitások és value object-ek** (a `domain-model.md` mappolása POCO-kra).
- **Domain események** (`DocumentUploaded`, `ReminderFired`,
  `AiSuggestionApproved`, …).
- **Domain szolgáltatások** — tisztán algoritmikus szabályok
  (`ReminderTriggerCalculator`, `EmbeddingChunker`).
- **Enum-ok**, kicsi részstruktúrák.
- **Semmilyen** I/O, adatbázis, AI, HTTP.

Tesztelhetőség: minden domain logika unit-tesztelhető *konstrukció +
metódushívás* mintával, mock nélkül.

### 3.2 Application (`FamilyOs.Application`)

- **Use case-ek MediatR handler formában**: `UploadDocumentCommand`,
  `SearchDocumentsQuery`, `AskQuestionQuery`, `ApproveSuggestedTaskCommand`,
  `FireDueRemindersCommand`, …
- **DTO-k** (be- és kimenő), `IMapper`-rel (Mapster ajánlott — kódgenerálás,
  jó teljesítmény, kevés runtime költség).
- **Pipeline behavior-ok**: validáció (FluentValidation), naplózás, hibák
  ProblemDetails-be konvertálása, tranzakció-kezelés (egyetlen DbContext
  per request).
- **Interfész-konzumensek**: a `Application` *deklarálja* a port-okat
  (`IDocumentRepository`, `IDocumentStorage`, `IAiProvider`,
  `IDocumentTextExtractor`, …); az infrastruktúra implementálja.

### 3.3 Infrastructure (`FamilyOs.Infrastructure`)

- **EF Core DbContext** + repozitóriumok + Migrations.
- **Fájltár**: `LocalFilesystemDocumentStorage` (MVP).
- **Auth**: Google OAuth handler, JWT-cookie kibocsátás, role-based policy-k.
- **Hangfire** beállítás (Postgres storage).
- **Email küldés** (`INotificationService` SMTP implementáció, opcionális).
- **Gmail API kliens** (Google.Apis.Gmail.v1) → `IEmailIngestionService`.
- **Tesseract OCR adapter** → `IDocumentTextExtractor`.

A `Infrastructure` projekt **nem** ismeri az AI providereket — azok az
`Infrastructure.Ai` projektben élnek.

### 3.4 Infrastructure.Ai (`FamilyOs.Infrastructure.Ai`)

- **Provider adapterek:**
  - `OllamaAiProvider` (alapértelmezett, lokális, ADR szerint privacy-first)
  - `AnthropicAiProvider` (opcionális, claude-haiku/sonnet)
  - `OpenAiProvider` (opcionális)
- **`AiProviderFactory`** — runtime választ az `appsettings.json` +
  feature-flag alapján, providerenként eltérő képesség-flag-ekkel (pl.
  Ollama nem támogat tool use → fallback prompt-engineering).
- **Embedder adapterek:** `OllamaEmbedder` (`nomic-embed-text`),
  `OpenAiEmbedder` (opcionális).
- **`IDocumentClassifier`, `IDeadlineExtractor`, `ITaskExtractor`,
  `IDocumentSummarizer`** implementációk — promptot konstruálnak és az
  `IAiProvider`-en hívják.
- **`ISemanticSearchService`** — pgvector-alapú lekérdezés a
  `DocumentChunk` és `NoteChunk` táblákon, modell-szűrt.

### 3.5 Api (`FamilyOs.Api`)

- ASP.NET Core Minimal API (**.NET 10 LTS** — a kód `net10.0`-t céloz).
- Endpoint-csoportok modulokba szervezve (`Endpoints/DocumentsModule.cs`,
  `Endpoints/SearchModule.cs`, …).
- Cross-cutting middleware: exception handler (ProblemDetails),
  trace-id, idempotency, auth, request-context (current user).
  CORS nem szükséges (a web nginx-proxyn át same-origin szolgálja ki az
  `/api`-t); rate-limit a Fázis 12 hardening része (a kódban még nincs —
  T-EBE-14).
- **Tilos** business logika és AI hívás közvetlenül — csak MediatR
  `Send`-eket adnak.

### 3.6 Workers (`FamilyOs.Workers`)

- `IHost` BackgroundService-ekkel + Hangfire Server.
- Bootstrap: ugyanaz a DI config mint az API, csak controller-mentes.
- **Recurring jobs:**
  - `DueReminderDispatcher` — minden percben szkenneli a `reminder` táblát
    `Scheduled AND trigger_utc <= now()` rekordokra, és Hangfire enqueue-t
    triggerel.
  - `AiJobScheduler` — minden 10 másodpercben szkenneli az
    `ai_processing_job` táblát `Queued` állapotú sorokra, és átadja
    őket Hangfire-nek.
  - `EmailIngestionPoller` — minden 5 percben hív a Gmail API-ra
    aktív `Source(Kind=GmailAccount)`-okra.
- **Catch-up mód.** A worker indulásánál először a hátralékot dolgozza fel
  (régi `Scheduled` reminderek, régi `Queued` ai_processing_job-ok).

---

## 4. Kulcsabsztrakciók (port-ok)

Az `Application` rétegben deklarált fő interfészek, az infrastruktúra adja
az implementációt. Az alábbi lista nem teljes, csak a fő portok.

### 4.1 AI réteg

```csharp
// FamilyOs.Application.Abstractions.Ai

public interface IAiProvider
{
    string Name { get; }                             // "ollama", "anthropic", ...
    AiCapabilities Capabilities { get; }             // ToolUse, JsonMode, Streaming
    Task<AiCompletion> CompleteAsync(AiPrompt prompt, CancellationToken ct);
}

public interface IEmbedder
{
    string ModelName { get; }                        // "nomic-embed-text:v1.5"
    int Dimensions { get; }                          // 768
    Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken ct);
}

public interface IDocumentTextExtractor
{
    Task<DocumentTextResult> ExtractAsync(Stream fileStream, string mimeType, CancellationToken ct);
}

public interface IDocumentSummarizer
{
    Task<string> SummarizeAsync(string text, string language, CancellationToken ct);
}

public interface IDocumentClassifier
{
    Task<ClassificationResult> ClassifyAsync(string text, IReadOnlyCollection<TopicTaxonomyEntry> taxonomy, CancellationToken ct);
}

public interface IDeadlineExtractor
{
    Task<IReadOnlyList<DeadlineSuggestion>> ExtractAsync(string text, DateOnly today, CancellationToken ct);
}

public interface ITaskExtractor
{
    Task<IReadOnlyList<TaskSuggestion>> ExtractAsync(string text, IReadOnlyCollection<FamilyMemberRef> members, CancellationToken ct);
}

public interface ISemanticSearchService
{
    Task<IReadOnlyList<SearchHit>> SearchAsync(string queryText, SearchFilters filters, int topK, CancellationToken ct);
}

public interface IQuestionAnswerService
{
    Task<AnswerResult> AnswerAsync(string question, FamilyContext context, CancellationToken ct);
    // AnswerResult tartalmaz: answer text + idézett források (Document/Note ID-k)
}
```

### 4.2 Reminder és értesítés

```csharp
public interface IReminderScheduler
{
    Task ScheduleAsync(Reminder reminder, CancellationToken ct);
    Task RescheduleAsync(Guid reminderId, DateTime newTriggerUtc, CancellationToken ct);
    Task CancelAsync(Guid reminderId, CancellationToken ct);
}

public interface INotificationService
{
    Task DispatchAsync(NotificationEnvelope envelope, CancellationToken ct);
}

public sealed record NotificationEnvelope(
    Guid TargetUserAccountId,
    NotificationChannel Channel,
    string Title,
    string Body,
    Guid? RelatedEntityId = null,
    string? RelatedEntityType = null);
```

### 4.3 Tárolás és integráció

```csharp
public interface IDocumentStorage
{
    Task<string> SaveAsync(Stream content, string suggestedFileName, CancellationToken ct);
    Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct);
    Task<bool> DeleteAsync(string storagePath, CancellationToken ct);
}

public interface IEmailIngestionService
{
    Task<EmailIngestionReport> SyncAsync(Source source, CancellationToken ct);
}

public interface IAuditLogger
{
    Task LogAsync(AuditEntry entry, CancellationToken ct);
}
```

### 4.4 Idő és identitás

```csharp
public interface IClock
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}

public interface ICurrentUserAccessor
{
    Guid? UserAccountId { get; }
    Guid? FamilyMemberId { get; }
    UserRole? Role { get; }
}
```

A teszteknél `FakeClock`, `FakeCurrentUser` — semmilyen statikus `DateTime.UtcNow`
nincs sehol a kódban.

---

## 5. AI provider absztrakció — részletek

### 5.1 Cél
Az alkalmazás akkor is működjön, ha a felhasználó később lokálisról cloud-ra
(vagy fordítva) vált, vagy különböző feladatokra különböző modellt választ
(pl. embedding lokálisan, summary cloud-on).

### 5.2 Konfigurációs modell

```json
{
  "Ai": {
    "DefaultProvider": "ollama",
    "Providers": {
      "ollama":    { "BaseUrl": "http://ollama:11434", "Model": "llama3.2:3b" },
      "anthropic": { "ApiKey": "<env>",                 "Model": "claude-haiku-4-5-20251001" }
    },
    "TaskAssignments": {
      "Summarize":        "ollama",
      "ExtractDeadlines": "ollama",
      "ExtractTasks":     "ollama",
      "Classify":         "ollama",
      "AnswerQuestion":   "ollama",
      "Embed":            "ollama"
    },
    "PrivacyMode": "LocalOnly"   // "LocalOnly" | "HybridAllowed" | "AnyProvider"
  }
}
```

### 5.3 Privacy guard

A `PrivacyMode = LocalOnly` esetén az `AiProviderFactory` **megtagadja**
a cloud provider visszaadását még akkor is, ha a `TaskAssignments`-ben
cloud van konfigurálva — explicit logbejegyzéssel és a felhasználónak
visszaadott magyar nyelvű hibával. Ez **biztonsági kapu**, nem
felülírható feature-flag-gel (megfelel a Product Vision privacy-first
elvének).

### 5.4 Failover
Ha a default provider (Ollama) nem elérhető (timeout / connection refused),
a job státusza `Failed` lesz `error_message = 'AI provider unavailable'`
mezővel, **nem fail-over automatikusan** cloud-ra (a privacy elv miatt).
A jobot a worker újrapróbálja exponenciális backoff-fal — amikor a PC
felébred, a queue feldolgozódik.

---

## 6. Háttérfeladat-architektúra: Hangfire + AiProcessingJob

### 6.1 Két rétegű queue, miért?

A `domain-model.md`-ben rögzített `ai_processing_job` tábla **domain-szintű**
nézet: a felhasználó látja a dashboardon, retry-olható, audit-elhető,
business priority-vel rendelkezik. A Hangfire ezzel szemben egy
*infrastruktúra-szintű* job runner: gondoskodik az exclusive lock-ról
(több worker, egy job), a retry-politikáról, a futás-állapotról.

### 6.2 A hozzárendelés

```
1. Application kód:  AiProcessingJob sor INSERT → status = Queued
2. AiJobScheduler:   szkennelés -> Hangfire BackgroundJob.Enqueue(...)
3. Hangfire worker:  futtatja az AiJobExecutor.RunAsync(jobId)-t
4. AiJobExecutor:    átállítja status = Running, kihúzza payload-ot, hív
                     IDocumentSummarizer / IDeadlineExtractor / ...
5. Sikeres befejezésnél: status = Completed, output_payload_json kitöltve;
   hiba esetén: status = Failed, error_message, AttemptCount++,
   next_attempt_utc = now + backoff
6. A scheduler a Queued ÉS a Failed + next_attempt_utc <= now sorokat
   együtt veszi fel (a partial index mindkét státuszt fedi:
   `WHERE status IN ('Queued','Failed')` — database-schema.md 4.16);
   a retry nem állítja vissza Queued-ra a státuszt.
```

**Idempotencia.** Az `AiJobExecutor`-nak idempotensnek kell lennie:
ugyanazon `JobId` többszöri futtatása nem csinál duplikátumot a
céltáblákon (pl. `DocumentSummary` `IsCurrent = true` upsert; chunk
generálásnál törlés-újraírás tranzakcióban).

### 6.3 Hangfire konfiguráció

- Storage: PostgreSQL (`Hangfire.PostgreSql` csomag), külön séma
  (`hangfire`), nem keveredik a `app` sémával.
- Worker host: a `FamilyOs.Workers` process; az API process **nem**
  futtat Hangfire worker-t (csak enqueue-zhat).
- Dashboard: csak admin szerepkörnek, csak LAN-on.
- Concurrency: alapból 4 worker (Ollama szekvenciális hívásai miatt
  nem érdemes többet).

### 6.4 Reminder ütemezés

A `Reminder` tábla **nem** kerül be Hangfire BackgroundJob-ként a teremtéskor
(elkerülve a Hangfire job-szám robbanást). Ehelyett:

- A `DueReminderDispatcher` recurring (1 perces) job szkenneli a táblát
  `Scheduled AND trigger_utc <= now()` sorokra.
- Találat: a `INotificationService.DispatchAsync`-et hívja, a státuszt
  `Fired`-re állítja.
- Catch-up: indításnál ugyanaz a szkennelés `now() - 14 days`-tól
  (konfigurálható: `Reminders.CatchUpMaxAgeDays`, lásd reminder-engine.md
  6.2), hogy a PC offline ablakában esedékessé vált emlékeztetőket egyben
  feldolgozza; a 14 napnál régebbiek `Skipped`-be kerülnek.

---

## 7. Tárolás stratégia

### 7.1 Fájlrendszer (MVP)

- Útvonal: `${FAMILYOS_DATA_DIR}/documents/<év>/<hónap>/<guid>.<ext>`
- `IDocumentStorage` egyetlen implementációja: `LocalFilesystemDocumentStorage`.
- Permissions: a Docker volume csak a `family-os` user (UID 1000) számára
  írható.

### 7.2 Backup

- Postgres: napi `pg_dump -Fc` cron-on a `data/backups/db/`-be, 30 nap megőrzés.
- Fájlok: napi `rsync --link-dest` snapshot a `data/backups/documents/`-be.
- Restore eljárás dokumentálva a `docs/DELIVERY.md`-ben.

### 7.3 Bővíthetőség

`IDocumentStorage` jövőbeli implementációi:
- `S3CompatibleDocumentStorage` (MinIO self-hosted)
- `NetworkSharedDocumentStorage` (SMB/NFS mount)

A migráció = új implementáció bekötése + offline copy script, nem
kódbázis-szintű refaktor.

---

## 8. Cross-cutting concern-ek

### 8.1 Hitelesítés és jogosultság
- Google OAuth 2.0 (web flow), kliens-oldali Google Sign-In + szerver-oldali
  `id_token` validáció.
- Session: HttpOnly + Secure cookie, 30 napos sliding expiration.
- ASP.NET Core authorization policies: `RequireAdmin`, `RequireAdult`,
  `RequireRead`. Részletek a `security-privacy.md`-ben.

### 8.2 Hibakezelés
- `ProblemDetails` (RFC 9457) **minden** API hibára.
- `ValidationException` → 400, `NotFoundException` → 404,
  `ForbiddenException` → 403, `ConflictException` → 409.
- Magyar nyelvű `detail` üzenet, gépi `type` URI + `traceId`.

### 8.3 Logolás
- Serilog + structured logs (`Serilog.Sinks.Console` + `Sinks.File`).
- Korreláció: minden request egy `traceId`-t kap (W3C TraceContext).
- AI hívások logban: prompt **soha nem** kerül logba teljes egészében
  (hossz, hash, modell igen) — privacy.
- Loglevel default `Information`, AI provider hibára `Warning`,
  pipeline hibákra `Error`.

### 8.4 Validáció
- FluentValidation pipeline behavior (MediatR-ben).
- Domain invariánsok kódban kényszerítve (`Reminder.Create()` factory
  metódus, nem nyilvános konstruktor).

### 8.5 Tranzakció és konzisztencia
- Egy MediatR command = egy DbContext SaveChanges = egy adatbázis-tranzakció.
- Idempotencia kulcs hosszú futású command-okra (kliens által küldött
  `Idempotency-Key` header, ASP.NET Core middleware-ben deduplikálva).
- `AiProcessingJob` írása ugyanabban a tranzakcióban, amelyikben a domain
  rekord (Document) létrejön — atomicitás biztosítva.

---

## 9. Deployment topológia (Docker Compose, MVP)

```
┌───────────────────────────────────────────────────────────────┐
│  Otthoni PC (Docker Compose)                                  │
│                                                               │
│  ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐    │
│  │  api     │──►│  postgres│◄──│ workers  │   │  ollama  │    │
│  │  :8080   │   │  :5432   │   │ (no port)│   │  :11434  │    │
│  └────┬─────┘   └────┬─────┘   └────┬─────┘   └────┬─────┘    │
│       │              │              │              │          │
│       └──► /data/documents (volume) ◄──┘          /models     │
│                                                   (volume)    │
│  ┌──────────┐                                                 │
│  │  web     │  Angular static (nginx)  :80                    │
│  └──────────┘                                                 │
└───────────────────────────────────────────────────────────────┘
        ▲
        │  csak LAN (10.x.x.x / 192.168.x.x) — ADR-0003
        │
   [PC, telefon, tablet a háztartásban]
```

**Service-ek:**
- `web` — Angular build kiszolgálva nginx-ből, `/api` proxy a `api`-ra.
- `api` — `FamilyOs.Api`, csak LAN interfész (ADR-0003 — VPN/Tailscale
  tudatosan nem cél), Kestrel TLS-mentes a belső hálózaton, reverse proxy
  (nginx) tesz HTTPS-t belső CA tanúsítvánnyal. (Fejlesztésben, HTTP-n a
  `__Host-` cookie-prefix nem működik — dev-környezetben sima cookie-név
  a kivétel.)
- `workers` — `FamilyOs.Workers`, nincs publikus port, csak DB és Ollama
  felé hív.
- `postgres` — `pgvector/pgvector:pg16` image, perzisztens volume.
- `ollama` — `ollama/ollama` image, modellek külön volume-on.

**Indulási sorrend:** `postgres` → `api` (migrációk lefutnak) → `workers` →
`ollama` (önállóan, igény szerint a workers-től függetlenül kérve).

---

## 10. Frontend áttekintés (részletek a frontend-structure.md-ben)

### Angular (MVP)
- Angular 20, standalone komponensek, signals-alapú state.
- Lazy-loaded feature module-ok: `dashboard`, `documents`, `search`, `tasks`,
  `deadlines`, `reminders`, `topics`, `family`, `settings`.
- Auth guard (`googleAuthGuard`) + role guard (`adminGuard`, `adultGuard`).
- State: feature-local signal-store + RxJS HTTP (Angular HttpClient).
- Magyar i18n alap, fordításfájl `src/assets/i18n/hu.json`.

### Kotlin (jövőbeli, nem MVP)
- Kotlin Multiplatform Mobile (KMP) Compose UI-jal — későbbi roadmap-elem.
- Megosztott `core-domain` modul az API-szerződésekkel; Android-first.
- Helyi LAN-detektálás, ha nincs otthon → „nincs kapcsolat" képernyő
  (ADR-0003 szerint).

---

## 11. Futási flow-k (szöveges szekvenciák)

### 11.1 Dokumentum feltöltés (UC-01)

```
[Felh.] ── POST /api/v1/documents (multipart) ──► [Api]
   [Api]   AuthN/AuthZ + Idempotency-Key check
   [Api]   Send(UploadDocumentCommand{stream, fileName, ...})
   [App]   Validáció (méret, mime, sha256 dedup)
   [App]   IDocumentStorage.SaveAsync → relative path
   [App]   DbContext: insert Document(processing_status=Pending),
                       insert AiProcessingJob(type=ExtractText,
                                              target=Document, status=Queued)
           SaveChanges() ── egy tranzakció
   [Api]   201 Created + Document DTO
                                                  │
                                                  ▼  (max 10s late)
[Workers] AiJobScheduler szkennel ─► Hangfire.Enqueue(AiJobExecutor.Run(jobId))
   [Workers] AiJobExecutor:
      - IDocumentTextExtractor.ExtractAsync → tisztított text
      - upsert DocumentText
      - update Document.processing_status = Extracting
      - új AiProcessingJob: DetectLanguage
   [Workers] DetectLanguage után: 5 párhuzamos AiProcessingJob
             (Classify, Summarize, ExtractDeadlines, ExtractTasks, Embed),
             processing_status = Analyzing; a Classify facet-találata
             ExtractFacet jobot láncol (ai-pipeline.md 3.8)
   [Workers] sorra futtatja: minden AI step külön AiProcessingJob
                              külön Hangfire job-ban
   [Workers] végül: PipelineOrchestrator finalizál —
                    Done (ha nem mind Failed) / Failed (ha mind az)
                    Realtime push a Workers-ből MVP-ben nincs (ADR-0008);
                    a UI polling/refresh útján frissül
```

### 11.2 Természetes nyelvű kérdés (UC-02)

```
[Felh.] ── POST /api/v1/search/qa { question } ──► [Api]
   [Api]   Send(AskQuestionQuery)
   [App]   IQuestionAnswerService.AnswerAsync:
            1. IEmbedder.EmbedAsync(question) → vektor
            2. ISemanticSearchService.SearchAsync → top-K chunk (Document+Note)
            3. RBAC szűrés a current user-re (private rekordok ki)
            4. Strukturált szűrés (deadline, family_member szűkítés a query
               natural language analízis alapján, ha alkalmazható)
            5. IAiProvider.CompleteAsync(SYS + chunks + question) → answer
            6. Hivatkozott chunk → forrás Document/Note IDs
   [Api]   200 OK { answer, sources[] }
```

### 11.3 Esedékes emlékeztető tüzelés (catch-up)

```
[Workers boot] DueReminderDispatcher.StartupCatchUp():
   query: reminder WHERE status='Scheduled' AND trigger_utc <= now()
                                                  ORDER BY trigger_utc
   for each → INotificationService.DispatchAsync (InApp + optional Email)
              update status='Fired', fired_utc = now()
[Workers run] recurring 1 perc: ugyanaz a query, csak az új belépőkre.
```

---

## 12. Megfigyelhetőség és üzemeltetés

- **Metrikák:** `OpenTelemetry.Exporter.Prometheus` (opcionális MVP-ben);
  metrikák: AI job queue méret/típus, AI hívás latency, OCR latency,
  reminder dispatch count, DB pool használat.
- **Egészségellenőrzés:** `/healthz/live` (process up) és `/healthz/ready`
  (DB elérhető → ready; az Ollama állapota csak *degraded* jelzés a
  válaszban, nem buktatja a readiness-t — a feltöltés, listázás,
  strukturált keresés AI nélkül is működik).
- **Verzió:** `/api/v1/system/version` — git sha + build date.
- **Adminisztrációs felület:** `/admin/jobs` (csak admin) — `AiProcessingJob`
  retry / cancel; Hangfire dashboard `/hangfire`.
- **SignalR hosting:** a hubokat (`/hubs/notifications`,
  `/hubs/documents`) kizárólag az `Api` process hosztolja; a `Workers`
  MVP-ben nem push-ol (no-op notifier, ADR-0008) — worker-oldali események
  a kliens polling/refresh útján jutnak el a UI-ra. Post-MVP irány: belső
  HTTP-hívás a Workers → Api irányban.

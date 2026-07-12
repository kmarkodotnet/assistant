# Kontrakt-delta — AI-javaslatok tanulása visszajelzésből (1. fázis)

> Státusz: Rögzítve (ARCH fázis) · Dátum: 2026-07-12 · Nyelv: magyar
> Forrás: [CR260710-08](../change-requests/cr260710-08-ai-feedback-tanulas.md),
> [ai_features.md §4.5](../ai_features.md#45-ai-javaslatok-tanulása-visszajelzésből)
> Döntés: [ADR-0013](../decisions/ADR-0013-ai-feedback-tanulas.md)
> Scope: **csak 1. fázis** — gyűjtés + admin AI-minőség dashboard. A few-shot
> prompt-beépítés (2. fázis) NEM tárgya (lásd §12).

Ez a fájl a **szerződés** a `db-engineer`, `backend-dev`, `frontend-dev` és
`qa-playwright` agentek között. Aki ebből dolgozik, más fájlt nem kell
elolvasnia. Kontrakt-módosítási igény bármely oldalon → STOP, vissza az
architect agenthez.

Fő terminológia:
- **Feedback-esemény:** egy AI-javaslaton (Task/Deadline/Warranty/MedicalRecord/
  FinancialRecord) vagy egy tool-call-javaslaton végzett felhasználói döntés
  (Accepted / Rejected / Corrected), amit egy `ai_feedback` sorban rögzítünk.
- **job_type:** melyik AI-job-típus termelte a javaslatot. **Statikus,
  entitástípusból levezetett** érték (ADR-0013 D3) — nincs DB-lookup, nincs új FK.

---

## 1. Domain modell változások

### 1.1 `Origin` bevezetése a facet-entitásokra

A `Warranty`, `MedicalRecord`, `FinancialRecord` domain-entitások **ma nem
tárolják**, hogy AI hozta-e létre őket. Bevezetjük a `Origin` mezőt (a meglévő
`FamilyOs.Domain.Enums.Origin` enumot használva; nincs új enum-érték), valamint
a jóváhagyás-mezőket a `Deadline` mintájára.

Új property-k **mindhárom** entitáson (backend-dev, domain réteg):

```csharp
public Origin Origin { get; private set; }
public Guid? ApprovedByUserAccountId { get; private set; }
public DateTime? ApprovedUtc { get; private set; }
```

A `Create(...)` factory-metódusok **nem** változnak szignatúrában, de az AI
extraction ág külön `CreateSuggestion(...)` factoryt kap (lásd §1.2). A `DeletedUtc`
mező mindhárom entitáson **már létezik** — a Reject a `DeletedUtc` beállítása
(soft-delete), a `RejectTask` mintájára.

Új domain-metódusok mindhárom entitáson:

```csharp
public void Approve(Guid approvedByUserId)
{
    ApprovedByUserAccountId = approvedByUserId;
    ApprovedUtc = DateTime.UtcNow;
    Origin = Origin.AiApproved;
    UpdatedUtc = DateTime.UtcNow;
}

public void Reject()                 // soft-delete, mint FamilyTask.Reject()
{
    DeletedUtc = DateTime.UtcNow;
    UpdatedUtc = DateTime.UtcNow;
}
```

A `Warranty.Patch(...)` és `FinancialRecord.Patch(...)` már léteznek (nem
változnak). `MedicalRecord`-nak **nincs** `Patch` metódusa — újat kell hozzáadni:

```csharp
public void Patch(
    MedicalRecordType? recordType,
    DateOnly? recordDate,
    string? provider,
    string? title,
    string? structuredJson,
    bool? isPrivate)
{
    if (recordType.HasValue) RecordType = recordType.Value;
    if (recordDate.HasValue) RecordDate = recordDate.Value;
    Provider = provider ?? Provider;
    if (title is not null) Title = title;
    StructuredJson = structuredJson ?? StructuredJson;
    if (isPrivate.HasValue) IsPrivate = isPrivate.Value;
    UpdatedUtc = DateTime.UtcNow;
}
```

**State machine:** ezek az entitások NEM kapnak `Status`-mezőt vagy állapotgépet
(a `FamilyTask`/`Deadline` `Status`-ától eltérően). Az életciklust kizárólag az
`Origin` (`AiSuggested` → `AiApproved`) és a `DeletedUtc` (Reject) írja le.
Business-szabály: **csak `Origin ∈ { AiSuggested, AiApproved }` és `DeletedUtc IS
NULL` esetén** engedélyezett Approve/Reject; egyébként `DomainBusinessRuleException`.

### 1.2 Extraction runner: `Origin.AiSuggested` a létrehozáskor

`ExtractFacetJobRunner` (`src/FamilyOs.Workers/Services/ExtractFacetJobRunner.cs`)
ma `Warranty.Create(...)` / `MedicalRecord.Create(...)` / `FinancialRecord.Create(...)`-t
hív. A `Create(...)` factory-t úgy kell módosítani (vagy külön `CreateSuggestion`-t
adni), hogy a **létrehozott entitás `Origin = Origin.AiSuggested`** legyen. A
kézi-létrehozás nincs jelen ezeknél az entitásoknál (kizárólag extraction hozza
létre), így minden új rekord AI-eredetű. **A meglévő sorok migrációs backfill-je
`AiSuggested`** (lásd §2.1).

> Fontos: az `ExtractFacetJobRunner` `SaveChanges`-e után **nem** keletkezik
> `ai_feedback` — a feedback kizárólag felhasználói Approve/Reject/Patch hatására
> jön létre (§4).

---

## 2. DB séma

Séma: `app`. Két külön migráció (rollback-barát szeparáció, ADR-0013 D2):

- `20260712000002_AddFacetOrigin` — a 3 facet-tábla oszlopbővítése.
- `20260712000003_AddAiFeedback` — az `ai_feedback` tábla.

A migrációk `migrationBuilder.Sql(@"...")` stílusúak, idempotens `IF NOT EXISTS`
mintával (a repó migrációs konvenciója). `Down` mindkét migrációnál
`throw new NotSupportedException(...)` (a projekt konvenciója).

### 2.1 `20260712000002_AddFacetOrigin`

```sql
-- warranty
ALTER TABLE app.warranty
    ADD COLUMN IF NOT EXISTS origin app.origin NOT NULL DEFAULT 'AiSuggested',
    ADD COLUMN IF NOT EXISTS approved_by_user_account_id uuid NULL REFERENCES app.user_account(id) ON DELETE SET NULL,
    ADD COLUMN IF NOT EXISTS approved_utc timestamptz NULL;

-- medical_record
ALTER TABLE app.medical_record
    ADD COLUMN IF NOT EXISTS origin app.origin NOT NULL DEFAULT 'AiSuggested',
    ADD COLUMN IF NOT EXISTS approved_by_user_account_id uuid NULL REFERENCES app.user_account(id) ON DELETE SET NULL,
    ADD COLUMN IF NOT EXISTS approved_utc timestamptz NULL;

-- financial_record
ALTER TABLE app.financial_record
    ADD COLUMN IF NOT EXISTS origin app.origin NOT NULL DEFAULT 'AiSuggested',
    ADD COLUMN IF NOT EXISTS approved_by_user_account_id uuid NULL REFERENCES app.user_account(id) ON DELETE SET NULL,
    ADD COLUMN IF NOT EXISTS approved_utc timestamptz NULL;
```

- Az `app.origin` **natív Postgres enum** (értékei PascalCase:
  `Manual`, `AiSuggested`, `AiApproved`, `ImportedEmail`, `ImportedFile`) —
  már létezik, nem kell újra létrehozni.
- A `DEFAULT 'AiSuggested'` gondoskodik a meglévő sorok backfilljéről az
  `ADD COLUMN` pillanatában (minden mai facet-rekord AI-extractionből származik).
- A `DEFAULT` a migráció után **megmaradhat** — az entitás `Create`/`CreateSuggestion`
  amúgy explicit beállítja, de a default ártalmatlan biztonsági háló.

### 2.2 `20260712000003_AddAiFeedback`

```sql
CREATE TABLE IF NOT EXISTS app.ai_feedback (
    id                    uuid PRIMARY KEY,
    user_account_id       uuid NOT NULL REFERENCES app.user_account(id) ON DELETE CASCADE,
    entity_type           text NOT NULL,            -- "Task" | "Deadline" | "Warranty" | "MedicalRecord" | "FinancialRecord" | "Reminder" | "ToolCall:<toolName>"
    entity_id             uuid NULL,                 -- a javaslat/létrejött entitás id-ja; tool-call reject esetén NULL (ADR-0013 D4)
    job_type              text NOT NULL,            -- "ExtractTasks" | "ExtractDeadlines" | "ExtractFacet" | "ToolCall:<toolName>"
    feedback_type         text NOT NULL,            -- "Accepted" | "Rejected" | "Corrected"
    original_result_json  jsonb NULL,               -- az AI eredeti javaslata (Corrected/Accepted/tool-call), különben NULL
    corrected_result_json jsonb NULL,               -- a felhasználó által javított végállapot; csak Corrected esetén NOT NULL
    created_utc           timestamptz NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),

    CONSTRAINT ck_ai_feedback_type
        CHECK (feedback_type IN ('Accepted','Rejected','Corrected')),
    CONSTRAINT ck_ai_feedback_corrected
        CHECK (feedback_type <> 'Corrected' OR corrected_result_json IS NOT NULL)
);

CREATE INDEX IF NOT EXISTS ix_ai_feedback_entity
    ON app.ai_feedback (entity_type, job_type, feedback_type);

CREATE INDEX IF NOT EXISTS ix_ai_feedback_created
    ON app.ai_feedback (created_utc);
```

- `entity_type` / `job_type` / `feedback_type` **`text` (string)**, nem natív enum —
  összhangban azzal, hogy az `AiJobType`/`JobTargetType` EF-mappingje is
  `HasConversion<string>` (nincs DB-enum, nincs migrációs kötöttség jövőbeli új
  értékekre). Indoklás: ADR-0013 D3.
- `entity_id` **nullable** — a tool-call-elutasításnál nem jön létre entitás
  (ADR-0013 D4). Minden más feedback-nél ki van töltve.
- A `ck_ai_feedback_corrected` constraint garantálja, hogy `Corrected` mindig
  hordozza a javított JSON-t (a `qa-playwright` erre asserthet).

### 2.3 EF mapping (backend-dev, `FamilyOs.Infrastructure`)

- Új entitás: `FamilyOs.Domain.Entities.AiFeedback` (lásd §3.1).
- Új `AiFeedbackConfiguration : IEntityTypeConfiguration<AiFeedback>`:
  `ToTable("ai_feedback","app")`, `HasKey(Id)`, a `entity_type`/`job_type`/
  `feedback_type` **plain string** oszlopok (`HasColumnType("text")`),
  `original_result_json`/`corrected_result_json` `HasColumnType("jsonb")`.
  **Nincs `HasQueryFilter`** (a feedback nem soft-delete-elhető).
- A 3 facet-config (`WarrantyConfiguration`, `MedicalRecordConfiguration`,
  `FinancialRecordConfiguration`): az `Origin` property `HasColumnType("app.origin")`
  (a `DeadlineConfiguration:28` mintájára), `ApprovedByUserAccountId` /
  `ApprovedUtc` `HasColumnName(...)` a snake_case oszlopokra.
- `IFamilyOsDbContext` (`FamilyOs.Application/Abstractions/Persistence`) és
  `FamilyOsDbContext` bővítése: `DbSet<AiFeedback> AiFeedback { get; }`.

---

## 3. Új entitás + logger-kontraktus

### 3.1 `AiFeedback` domain-entitás

`src/FamilyOs.Domain/Entities/AiFeedback.cs`:

```csharp
public sealed class AiFeedback
{
    private AiFeedback() { }

    public Guid Id { get; private set; }
    public Guid UserAccountId { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid? EntityId { get; private set; }
    public string JobType { get; private set; } = string.Empty;
    public string FeedbackType { get; private set; } = string.Empty;   // Accepted|Rejected|Corrected
    public string? OriginalResultJson { get; private set; }
    public string? CorrectedResultJson { get; private set; }
    public DateTime CreatedUtc { get; private set; }

    public static AiFeedback Create(
        Guid userAccountId, string entityType, Guid? entityId, string jobType,
        string feedbackType, string? originalJson, string? correctedJson)
        => new()
        {
            Id = Guid.CreateVersion7(),
            UserAccountId = userAccountId,
            EntityType = entityType,
            EntityId = entityId,
            JobType = jobType,
            FeedbackType = feedbackType,
            OriginalResultJson = originalJson,
            CorrectedResultJson = correctedJson,
            CreatedUtc = DateTime.UtcNow,
        };
}
```

### 3.2 `IAiFeedbackLogger` — hook-mechanizmus

**Miért nem MediatR pipeline-behavior?** Az `AuditBehavior` csak a request/response-t
látja, a **mutáció előtti** entitás-állapotot nem — a `Corrected` diffhez viszont
kell az „eredeti" (mutáció előtti) JSON. Ezért az `AiFeedbackLogger` **explicit
service**, amit az egyes handlerek hívnak, nem automatikus behavior (ADR-0013 D5).

Interfész (`src/FamilyOs.Application/Common/Abstractions/IAiFeedbackLogger.cs`):

```csharp
public interface IAiFeedbackLogger
{
    /// Approve/confirm ág — feedback_type = Accepted.
    void LogAccepted(string entityType, Guid? entityId, Guid userAccountId,
                     string jobType, string? originalJson);

    /// Reject ág — feedback_type = Rejected.
    void LogRejected(string entityType, Guid? entityId, Guid userAccountId,
                     string jobType, string? originalJson);

    /// Patch ág — feedback_type = Corrected (original vs corrected).
    void LogCorrected(string entityType, Guid entityId, Guid userAccountId,
                      string jobType, string originalJson, string correctedJson);
}
```

**Implementáció-kontraktus (`AiFeedbackLogger : IAiFeedbackLogger`, scoped):**

1. **Tranzakciós egység:** a logger **NEM** hív `SaveChangesAsync`-et. Csak
   `db.AiFeedback.Add(AiFeedback.Create(...))`-et. A hívó handler meglévő
   `SaveChangesAsync`-e perzisztálja atomikusan, a mutációval **egy tranzakcióban**.
   (Ugyanaz a minta, mint `ApproveDeadlineCommandHandler` a `Reminder`-eknél.)
2. **AI-origin guard (a facet + Task/Deadline hívásoknál):** a logger csak akkor
   ír sort, ha a hívó jelezte, hogy a javaslat AI-eredetű (lásd a hívási minta a
   §4-ben: a handler csak `Origin ∈ {AiSuggested, AiApproved}` esetén hívja). A
   logger önmagában nem néz DB-t.
3. **JSON serializálás:** camelCase, `JsonStringEnumConverter`, `WriteIndented=false`
   — ugyanaz a `JsonSerializerOptions`, mint az `AuditBehavior`-ban. A snapshot a
   feedback-releváns üzleti mezőkből épül (lásd §4 mezőlisták), **nem** a teljes
   entitásból (audit-timestamp-ek, RowVersion kihagyva).

**job_type feloldás (a loggeren belüli statikus map, ADR-0013 D3):**

| entity_type (input) | job_type (output) |
|---|---|
| `Task` | `ExtractTasks` |
| `Deadline` | `ExtractDeadlines` |
| `Warranty` | `ExtractFacet` |
| `MedicalRecord` | `ExtractFacet` |
| `FinancialRecord` | `ExtractFacet` |
| `Reminder` / `ToolCall:*` | a hívó adja át (`ToolCall:<toolName>`) |

> A logger a facet/task/deadline hívásoknál a `jobType`-ot **maga vezeti le** az
> `entityType`-ból a fenti map alapján — a handler csak az `entityType`-ot adja át.
> A tool-call ág explicit `jobType`-ot ad át (`ToolCall:<toolName>`).

DI-regisztráció (`FamilyOs.Application/DependencyInjection.cs`):
`services.AddScoped<IAiFeedbackLogger, AiFeedbackLogger>();`

---

## 4. Hívási pontok (feedback-hook beépítése handlerekbe)

Minden érintett handler kap egy `IAiFeedbackLogger` konstruktor-függőséget. A
hívás a mutáció/`Approve`/`Reject` **után**, de a `SaveChangesAsync` **előtt**
történik (kivéve ahol jelezve).

### 4.1 Task

- **`ApproveTaskCommandHandler`** (`Tasks/Actions/`): a `task.Approve(...)` után,
  **ha** `task.Origin == Origin.AiSuggested` (approve előtti állapot!), akkor
  `LogAccepted("Task", task.Id, request.ApprovedByUserId, "ExtractTasks",
  originalJson: TaskSnapshot(task))`. A snapshot mezői: `title, description,
  dueDateUtc, priority, assignedToFamilyMemberId`.
  > Sorrend: az `Origin`-t az `Approve()` `AiApproved`-ra írja, ezért az
  > AI-eredet ellenőrzését az `Approve()` **hívása előtt** kell elvégezni
  > (mentsd el egy `bool wasAiSuggested = task.Origin == Origin.AiSuggested;`
  > lokálisba).
- **`RejectTaskCommandHandler`**: `task.Reject()` előtt/után `wasAiSuggested`
  guard, majd `LogRejected("Task", task.Id, request-UserId, "ExtractTasks",
  originalJson: TaskSnapshot(task))`. A `RejectTaskCommand` **ma nem hordoz
  user-id-t** — bővíteni kell: `RejectTaskCommand(Guid TaskId, Guid UserId)`, a
  `TasksModule.cs:124` a `userAccessor.UserAccountId.Value`-t adja át.
- **`PatchTaskCommandHandler`** (`Tasks/PatchTaskCommandHandler.cs`): a
  `task.UpdateDetails(...)` **előtt** rögzítsd az eredeti mezőket
  (`originalJson = TaskSnapshot(task)`), a `UpdateDetails` után
  `correctedJson = TaskSnapshot(task)`. **Ha** `task.Origin ∈ {AiSuggested,
  AiApproved}` **és** a két JSON eltér, akkor `LogCorrected("Task", task.Id,
  request.UserId, "ExtractTasks", originalJson, correctedJson)`. Ha a két snapshot
  azonos (nincs tényleges változás), **nem** logol.

### 4.2 Deadline

- **`ApproveDeadlineCommandHandler`** (`Deadlines/Actions/`): `deadline.Approve(...)`
  előtti `wasAiSuggested` guard, majd a `SaveChanges` **előtt** `LogAccepted(
  "Deadline", deadline.Id, request.ApprovedByUserId, "ExtractDeadlines",
  DeadlineSnapshot(deadline))`. (A meglévő reminder-generálás után is hívható,
  de mindenképp az utolsó `SaveChanges` előtt.) Snapshot: `title, description,
  dueDateUtc, category, relatedFamilyMemberId`.
- **Reject/Dismiss:** a `Deadline` reject-megfelelője a **`DismissDeadlineCommandHandler`**.
  A `DeadlineStateMachine.Transition(..., Dismissed)` után, **ha** `wasAiSuggested`
  (a `Origin == AiSuggested` a dismiss előtt), `LogRejected("Deadline",
  deadline.Id, request.UserId, "ExtractDeadlines", DeadlineSnapshot(deadline))`.
  A `DismissDeadlineCommand` **ma nem hordoz user-id-t** — bővíteni kell
  `DismissDeadlineCommand(Guid DeadlineId, Guid UserId)`, endpoint a
  `userAccessor.UserAccountId.Value`-t adja át.
- **`PatchDeadlineCommandHandler`**: mint a Task Patch — snapshot előtte/utána,
  `LogCorrected("Deadline", ...)` ha AI-origin és eltér.

### 4.3 Warranty / MedicalRecord / FinancialRecord (ÚJ Approve/Reject/Patch réteg)

Ezekhez **nulláról** kell command+handler (kiváltják a `DocumentsModule.cs:147-149`
501-stubokat). Mind a `RequireAdult` policy alatt. A command mediátoron megy át,
így az `AuditBehavior` automatikusan naplóz (Approve/Reject/Update action).

Új command/handler-lista (namespace-javaslat zárójelben):

| Command | Handler | Feedback-hívás |
|---|---|---|
| `ApproveWarrantyCommand(Guid WarrantyId, Guid UserId)` | `ApproveWarrantyCommandHandler` (`Documents/Warranties/Actions`) | `LogAccepted("Warranty", id, userId, "ExtractFacet", WarrantySnapshot)` ha `wasAiSuggested` |
| `RejectWarrantyCommand(Guid WarrantyId, Guid UserId)` | `RejectWarrantyCommandHandler` | `LogRejected("Warranty", ...)` ha `wasAiSuggested` |
| `PatchWarrantyCommand(...)` | `PatchWarrantyCommandHandler` | `LogCorrected("Warranty", ...)` ha AI-origin és eltér |
| `ApproveMedicalRecordCommand(Guid MedicalRecordId, Guid UserId)` | `ApproveMedicalRecordCommandHandler` | `LogAccepted("MedicalRecord", ...)` |
| `RejectMedicalRecordCommand(Guid MedicalRecordId, Guid UserId)` | `RejectMedicalRecordCommandHandler` | `LogRejected("MedicalRecord", ...)` |
| `PatchMedicalRecordCommand(...)` | `PatchMedicalRecordCommandHandler` | `LogCorrected("MedicalRecord", ...)` |
| `ApproveFinancialRecordCommand(Guid FinancialRecordId, Guid UserId)` | `ApproveFinancialRecordCommandHandler` | `LogAccepted("FinancialRecord", ...)` |
| `RejectFinancialRecordCommand(Guid FinancialRecordId, Guid UserId)` | `RejectFinancialRecordCommandHandler` | `LogRejected("FinancialRecord", ...)` |
| `PatchFinancialRecordCommand(...)` | `PatchFinancialRecordCommandHandler` | `LogCorrected("FinancialRecord", ...)` |

Handler-minta (kövesd az `ApproveDeadlineCommandHandler`-t):

```csharp
var warranty = await _db.Warranties.FirstOrDefaultAsync(w => w.Id == request.WarrantyId, ct)
    ?? throw new NotFoundException("Warranty", request.WarrantyId);

if (warranty.Origin is not (Origin.AiSuggested or Origin.AiApproved))
    throw new DomainBusinessRuleException("Csak AI-javasolt garancia hagyható jóvá.");

var wasAiSuggested = warranty.Origin == Origin.AiSuggested;
warranty.Approve(request.UserId);
if (wasAiSuggested)
    _feedback.LogAccepted("Warranty", warranty.Id, request.UserId, "ExtractFacet",
        WarrantySnapshot(warranty));
await _db.SaveChangesAsync(ct);
```

Snapshot-mezők entitásonként (a JSON tartalma):
- **Warranty:** `productName, brand, model, serialNumber, purchaseDate,
  purchasePrice, currency, warrantyMonths, warrantyEndDate, seller,
  relatedFamilyMemberId`.
- **MedicalRecord:** `recordType, recordDate, provider, title, structuredJson,
  isPrivate`.
- **FinancialRecord:** `recordType, vendor, amount, currency, issueDate, dueDate,
  isPaid, recurrencePeriod, relatedFamilyMemberId`.

`PatchWarrantyCommand` mezői = `Warranty.Patch(...)` paraméterei; `PatchFinancialRecordCommand`
= `FinancialRecord.Patch(...)`; `PatchMedicalRecordCommand` = az új
`MedicalRecord.Patch(...)` (§1.1). A Patch-handlerekbe **be kell építeni** a
concurrency-guardot csak ha az entitáson van RowVersion — ezeken **nincs**, tehát
elég a sima `SaveChanges`.

### 4.4 API-endpointok (a 501-stubok kiváltása)

`DocumentsModule.cs` — a `warranty|medical-record|financial-record` facet-PATCH
stubok helyére valódi mapping, plusz új approve/reject alútvonalak. **RBAC:**
`RequireAdult` (a meglévő facet-stubokkal azonos).

```
PATCH  /api/v1/documents/{id}/warranty                 -> PatchWarrantyCommand
POST   /api/v1/documents/{id}/warranty/approve         -> ApproveWarrantyCommand
POST   /api/v1/documents/{id}/warranty/reject          -> RejectWarrantyCommand
PATCH  /api/v1/documents/{id}/medical-record           -> PatchMedicalRecordCommand
POST   /api/v1/documents/{id}/medical-record/approve   -> ApproveMedicalRecordCommand
POST   /api/v1/documents/{id}/medical-record/reject    -> RejectMedicalRecordCommand
PATCH  /api/v1/documents/{id}/financial-record         -> PatchFinancialRecordCommand
POST   /api/v1/documents/{id}/financial-record/approve -> ApproveFinancialRecordCommand
POST   /api/v1/documents/{id}/financial-record/reject  -> RejectFinancialRecordCommand
```

- Az `{id}` a **document id** (a facet 1:1 a documenttel; a handler a
  `DocumentId == id`-vel keresi ki a facet-rekordot — a `warranty.document_id`
  UNIQUE, a másik kettőnél a legfrissebb nem-törölt rekord). Az approve/reject a
  `UserId`-t az `ICurrentUserAccessor`-ból (`userAccessor.UserAccountId.Value`)
  veszi, request-body nélkül.
- PATCH request-body DTO-k = a Patch command mezői (opcionális/nullable). A PATCH
  200 OK-t ad, az approve/reject szintén 200 OK.
- Hiba-mapping (a meglévő ProblemDetails/RFC9457 alapján): NotFound → 404,
  DomainBusinessRule → 422, Forbidden → 403.

### 4.5 Tool-call integráció

- **`ConfirmToolCallCommandHandler`** (`ToolCalls/`): a sikeres
  `tool.ExecuteAsync(...)` és a meglévő `auditLogger.LogAsync(...)` **után**, a
  return **előtt**:
  ```csharp
  _feedback.LogAccepted(
      entityType: result.ResultType,          // pl. "Reminder"
      entityId:   result.ResultId,
      userAccountId: userAccountId,
      jobType:    $"ToolCall:{tool.Name}",     // pl. "ToolCall:create_reminder"
      originalJson: JsonSerializer.Serialize(envelope.Args, AuditJsonOpts));
  ```
  Mivel a `ConfirmToolCall` handler **nem** ír DbContexten keresztül (tokent
  validál + `ISender`-t hív), az `AiFeedbackLogger` itt **kivételesen saját
  `SaveChangesAsync`-et végez** (külön scoped `IFamilyOsDbContext`). Ehhez a
  loggernek legyen egy `LogAcceptedAsync(...)`/`FlushAsync(ct)` async
  változata is (lásd megjegyzés lent).
- **`RejectToolCallCommandHandler`**: a meglévő reject-audit után:
  ```csharp
  _feedback.LogRejected(
      entityType: $"ToolCall:{toolName}",
      entityId:   null,                        // nincs létrejött entitás (ADR-0013 D4)
      userAccountId: userAccountId ?? Guid.Empty-ellenőrzés,
      jobType:    $"ToolCall:{toolName}",
      originalJson: valid token esetén JsonSerializer.Serialize(envelope.Args, ...) különben null);
  ```
  Csak akkor logol, ha `userAccountId` nem null (anonim reject nem feedback).

> **Logger két hívási módja (kontrakt-kényszer):** az `IAiFeedbackLogger`
> void-metódusai a DbContextbe **add**-elnek (a handler `SaveChanges`-e ír) —
> ez a facet/Task/Deadline eset. A tool-call handlereknek nincs saját
> `SaveChanges`-ük, ezért az `AiFeedbackLogger` implementáció egy **explicit
> `Task FlushAsync(CancellationToken ct)`** metódust is kínál, amit a két
> tool-call handler a végén meghív (`await _feedback.FlushAsync(ct);`). A
> facet/Task/Deadline handlerek NEM hívnak `FlushAsync`-et — ott a saját
> `SaveChanges` perzisztál. Az `AiFeedbackLogger` idempotens a flush nélküli
> add-re nézve (a hozzáadott entitás a scoped context része).

---

## 5. Admin API — AI-minőség dashboard

Új modul: `AiQualityAdminModule` (`FamilyOs.Api/Endpoints/`), group:
`/api/v1/ai-quality`, `RequireAuthorization("RequireAdmin")` (az
`AiJobsAdminModule` mintájára). Kimenet-formátum: JSON, camelCase.

### 5.1 `GET /api/v1/ai-quality/summary`

Query paraméterek (mind opcionális): `fromUtc` (ISO), `toUtc` (ISO).

MediatR: `GetAiQualitySummaryQuery(DateTime? FromUtc, DateTime? ToUtc)`.

Aggregáció-logika (`GetAiQualitySummaryQueryHandler`):
```
FROM app.ai_feedback
[WHERE created_utc >= @fromUtc] [AND created_utc < @toUtc]
GROUP BY entity_type, job_type
```
minden csoportra: `accepted = COUNT(feedback_type='Accepted')`,
`rejected = COUNT(...='Rejected')`, `corrected = COUNT(...='Corrected')`,
`total = accepted+rejected+corrected`.

Response DTO:
```csharp
public sealed record AiQualitySummaryRow(
    string EntityType,
    string JobType,
    int Accepted,
    int Rejected,
    int Corrected,
    int Total,
    double AcceptanceRate,   // accepted / total  (0..1, total=0 -> 0)
    double RejectionRate,    // rejected / total
    double CorrectionRate);  // corrected / total

public sealed record AiQualitySummaryDto(
    IReadOnlyList<AiQualitySummaryRow> Rows,
    int GrandTotal);
```
A `*Rate` mezőket a **handler** számolja (nem SQL) a lekérdezett count-okból,
`total == 0` esetén `0.0` (nulla-osztás elkerülése).

### 5.2 `GET /api/v1/ai-quality/field-corrections`

„Mely mezőket javítják leggyakrabban" — a `Corrected` sorok `original` vs
`corrected` JSON-diffjéből.

Query paraméterek: `entityType` (kötelező), `jobType` (opcionális),
`fromUtc`/`toUtc` (opcionális), `top` (opcionális, default 20).

MediatR: `GetFieldCorrectionsQuery(string EntityType, string? JobType,
DateTime? FromUtc, DateTime? ToUtc, int Top)`.

Handler-logika (`GetFieldCorrectionsQueryHandler`):
1. Lekérdezi a szűrt `Corrected` sorokat (`original_result_json`,
   `corrected_result_json`), `AsNoTracking`.
2. **In-memory** diff soronként: mindkét JSON-t `Dictionary<string, JsonElement>`-té
   parse-olva, minden kulcsra összehasonlítja az érték szöveges reprezentációját
   (`JsonElement.GetRawText()`); ha eltér → az adott mező `changeCount`-ját növeli.
   (Egy családi app volumenén ez tized-ezres nagyságrend, in-memory diff elég —
   nincs szükség jsonb-path SQL-re.)
3. `top` mező visszaadása változás-gyakoriság szerint csökkenően.

Response DTO:
```csharp
public sealed record FieldCorrectionEntry(string Field, int ChangeCount);
public sealed record FieldCorrectionsDto(
    string EntityType,
    string? JobType,
    int CorrectedSampleSize,          // hány Corrected sort néztünk
    IReadOnlyList<FieldCorrectionEntry> Fields);
```

---

## 6. Frontend kontraktus

Új admin-oldal: **`/admin/ai-quality`**.

### 6.1 Route (`frontend/src/app/features/admin/admin.routes.ts`)

Új gyerek-route a meglévő `admin.page` alatt (a `jobs`/`providers` mintájára):
```ts
{
  path: 'ai-quality',
  loadComponent: () => import('./pages/ai-quality.page').then(m => m.AiQualityPage),
  title: 'AI minőség — Family OS',
},
```
Az admin-navigációba (ahol a `jobs`/`providers` link van) új menüpont:
„AI minőség" → `/admin/ai-quality`.

### 6.2 API service (`frontend/src/app/features/admin/services/ai-quality.api.ts`)

```ts
export interface AiQualitySummaryRow {
  entityType: string; jobType: string;
  accepted: number; rejected: number; corrected: number; total: number;
  acceptanceRate: number; rejectionRate: number; correctionRate: number;
}
export interface AiQualitySummaryDto { rows: AiQualitySummaryRow[]; grandTotal: number; }

export interface FieldCorrectionEntry { field: string; changeCount: number; }
export interface FieldCorrectionsDto {
  entityType: string; jobType: string | null;
  correctedSampleSize: number; fields: FieldCorrectionEntry[];
}

@Injectable({ providedIn: 'root' })
export class AiQualityApiService {
  // GET /api/v1/ai-quality/summary?fromUtc=&toUtc=
  getSummary(fromUtc?: string, toUtc?: string): Observable<AiQualitySummaryDto>;
  // GET /api/v1/ai-quality/field-corrections?entityType=&jobType=&fromUtc=&toUtc=&top=
  getFieldCorrections(entityType: string, jobType?: string, fromUtc?: string,
                      toUtc?: string, top?: number): Observable<FieldCorrectionsDto>;
}
```
Base: `/api/v1/ai-quality`. `HttpParams`-szal az opcionális query-k. (A meglévő
`ai-jobs.api.ts` mintáját kövesd.)

### 6.3 Page (`frontend/src/app/features/admin/pages/ai-quality.page.ts`)

- Standalone, `ChangeDetectionStrategy.OnPush`, signals (`ai-jobs.page.ts` minta).
- **Összesítő táblázat**: soronként entity_type / job_type, oszlopok: Accepted,
  Rejected, Corrected, Total, és az arányok százalékban (`acceptanceRate * 100`).
  Magas elutasítási arányt (pl. `rejectionRate > 0.3`) vizuálisan kiemel
  (`ui-badge variant="danger"`).
- **Mező-korrekció drilldown**: egy sor kiválasztásakor (entity_type + job_type)
  `getFieldCorrections(...)` hívás, a top javított mezők listája change-count-tal.
- Üres állapot: „Nincs még AI-visszajelzés." Betöltés: `ui-skeleton`.
- Opcionális dátumszűrő (from/to) — MVP-ben elhagyható, de a service támogatja.
- `data-testid` horgok a QA-nak: `ai-quality-summary-row`,
  `ai-quality-field-correction`, `ai-quality-empty`.

---

## 7. Acceptance criteria — feature-enként, agentenként

### 7.1 db-engineer (T-AIFB-DB)

- [ ] `20260712000002_AddFacetOrigin` migráció létrejön; `warranty`,
      `medical_record`, `financial_record` táblák megkapják az `origin`
      (`app.origin NOT NULL DEFAULT 'AiSuggested'`), `approved_by_user_account_id`,
      `approved_utc` oszlopokat. A meglévő sorok `origin = 'AiSuggested'`.
- [ ] `20260712000003_AddAiFeedback` migráció: `app.ai_feedback` tábla a §2.2
      pontos sémájával, a 2 index és a 2 CHECK-constraint.
- [ ] `AiFeedbackConfiguration` + a 3 facet-config Origin/approved mapping;
      `IFamilyOsDbContext` + `FamilyOsDbContext` `DbSet<AiFeedback>`.
- [ ] `dotnet ef migrations` hibamentesen fut, a snapshot frissül;
      `dotnet build` zöld.

### 7.2 backend-dev (T-AIFB-BE)

- [ ] `AiFeedback` entitás + `IAiFeedbackLogger`/`AiFeedbackLogger` a §3 szerint,
      DI-regisztráció.
- [ ] `Origin` + `Approve`/`Reject` domain-metódusok a 3 facet-entitáson;
      `MedicalRecord.Patch(...)`; extraction runner `Origin.AiSuggested`.
- [ ] Task/Deadline Approve/Reject/Patch handlerek feedback-hívása a §4.1–4.2
      szerint (Reject/Dismiss command user-id bővítés).
- [ ] 9 új facet command+handler (§4.3) + a 9 endpoint (§4.4) a 501-stubok
      helyén; RBAC `RequireAdult`.
- [ ] Tool-call confirm/reject feedback-integráció (§4.5), `FlushAsync`.
- [ ] `GetAiQualitySummaryQuery(Handler)` + `GetFieldCorrectionsQuery(Handler)`
      + `AiQualityAdminModule` (§5), `RequireAdmin`.
- [ ] `dotnet build` + `dotnet test` zöld.

### 7.3 frontend-dev (T-AIFB-FE)

- [ ] `ai-quality.api.ts` service a §6.2 DTO-kkal és 2 metódussal.
- [ ] `ai-quality.page.ts` összesítő táblázat + mező-korrekció drilldown (§6.3),
      `data-testid` horgokkal.
- [ ] Route + admin-nav menüpont (`/admin/ai-quality`), `adminGuard` alatt.
- [ ] `ng build` + unit tesztek zöldek.

### 7.4 qa-playwright (T-AIFB-QA)

A CR260710-08 Given/When/Then kritériumaira:
- [ ] AI-javasolt Task jóváhagyása változtatás nélkül → 1 `ai_feedback` sor
      `feedback_type='Accepted'`, `entity_type='Task'`, `job_type='ExtractTasks'`,
      `corrected_result_json IS NULL`.
- [ ] AI-javasolt Deadline dátum-módosítással jóváhagyás (Patch) → sor
      `feedback_type='Corrected'`, `original_result_json` és `corrected_result_json`
      kitöltve, a diff a `dueDateUtc` mezőt tartalmazza.
- [ ] AI-javaslat elutasítása (bármely entitástípus) → `feedback_type='Rejected'`.
- [ ] Facet (Warranty/MedicalRecord/FinancialRecord) approve/reject/patch a 9 új
      endpointon → megfelelő `ai_feedback` sor (a 501-stub megszűnt).
- [ ] Tool-call confirm → `Accepted` (`entity_type='Reminder'`,
      `job_type='ToolCall:create_reminder'`); tool-call reject → `Rejected`
      (`entity_id IS NULL`).
- [ ] **Manuális** (nem AI-eredetű) Task szerkesztése → **NEM** keletkezik
      `ai_feedback` sor.
- [ ] Admin megnyitja `/admin/ai-quality`-t → entitástípus/job-típus bontásban
      látja az accept/reject/correct arányokat; a mező-korrekció drilldown a
      leggyakrabban javított mezőt mutatja. `RequireAdmin` — Child/Adult 403.

---

## 8. Kontrakt-invariánsok (a párhuzamos ágak közös igazsága)

1. `entity_type` értékkészlet: `Task | Deadline | Warranty | MedicalRecord |
   FinancialRecord | Reminder | ToolCall:<toolName>`. A frontend NEM feltételez
   zárt listát (jövőbeli tool-ok miatt) — string-alapon jelenít meg.
2. `job_type` az `entity_type`-ból determinisztikus a §3 map szerint (kivéve
   tool-call, ahol `ToolCall:<toolName>`). A backend a **single source of truth**;
   a frontend csak megjeleníti.
3. `ai_feedback` sor **csak** AI-eredetű javaslatra (`Origin ∈ {AiSuggested,
   AiApproved}`) vagy tool-call-javaslatra keletkezik. Manuális entitás
   szerkesztése/törlése soha nem ír `ai_feedback`-et.
4. A feedback-írás a kiváltó mutációval **egy tranzakcióban** történik (add +
   közös `SaveChanges`), kivéve a tool-call ágat (külön `FlushAsync`).
5. `entity_id` csak tool-call-reject esetén NULL; minden más feedback-nél kitöltve.

---

## 9. Migrációs kockázat összefoglaló

- **Alacsony.** Két additív migráció: 3 facet-tábla oszlopbővítése (default-tal
  backfillelve) + 1 új tábla. Nincs adatmigráció, nincs breaking change, nincs
  meglévő oszlop-átértelmezés. A natív `app.origin` enum már létezik. Az
  `entity_type`/`job_type` string, így jövőbeli entitás/tool bővítés migráció-mentes.

---

## 10. Kifejezetten NEM tárgya ennek a körnek (2. fázis / jövőbeli munka)

- **Few-shot prompt-beépítés** (a leggyakrabban javított minták promptba
  injektálása) — a CR260710-08 2. lépése. Ez az adat (`ai_feedback` +
  field-corrections) most **gyűlik**; a promptba-visszacsatolás külön CR
  (jövőbeli munka), NEM tervezendő meg itt.
- Modell-finetuning / retraining — a CR szerint sem cél.
- `ai_feedback` retenció/GDPR-törlés a felhasználó-törléssel: a `user_account`
  FK `ON DELETE CASCADE` fedi az alap-esetet; finomabb retenciós politika külön CR.

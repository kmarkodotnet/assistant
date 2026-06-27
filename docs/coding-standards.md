# Coding standards — Family OS

> Státusz: DRAFT v0.1 · Dátum: 2026-06-26 · Nyelv: magyar (a doksi),
> kód: angol identifierek, magyar user-facing szövegek.
> Kapcsolódó: [architecture.md](architecture.md), [api-design.md](api-design.md),
> [security-privacy.md](security-privacy.md)

A szabályok **nem** stilisztikai ízlés-kifejtések — azt szolgálják, hogy a
factory párhuzamos agentjei (architect / backend-dev / frontend-dev /
code-reviewer) konzisztensen olvasható, biztonságos, karbantartható kódot
adjanak vissza.

A nem-stilisztikai szabályokat **enforcer** kategóriába soroljuk: amelyik
szabályt lehet, formatter / analyzer / lint kényszeríti. A többi a
`code-reviewer` agent ellenőrzési listája.

---

## 1. Vezérlőelvek (rövid)

1. **Az identifierek angolul, a felhasználói szövegek magyarul.** Class / metódus
   / variable név angol, log üzenet angol, exception message angol;
   ProblemDetails `detail`, UI text, email body magyar.
2. **Boy Scout rule.** Hagyd jobb állapotban, mint találtad — de minimális
   scope-ban (a feature-rel összefüggő tisztításra korlátozva).
3. **Olvashatóság > kódgolf.** Inkább két olvasható sor, mint egy „okos"
   ternary chain.
4. **A típusrendszer az igazság.** Nincs `dynamic`, nincs `any`. A nullok
   explicit `?` jelöléssel.
5. **Kis felület, kis változás.** Egy PR egy célt szolgál. Refactor +
   feature ne keveredjen.
6. **Tesztelhetőség.** Új kód önállóan tesztelhető (függőség-injekció,
   nincs statikus state).

---

## 2. .NET / C# standardok

### 2.1 Verzió, beállítások

- **.NET 8 LTS.** TargetFramework `net8.0`.
- `Directory.Build.props`:
  ```xml
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors>CS8600;CS8602;CS8603;CS8625;CS8618;CS8669</WarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  ```
- **EditorConfig** + **csharpier** formatter, CI futtatja.

### 2.2 Naming convention

| Elem | Konvenció | Példa |
|---|---|---|
| Namespace | `FamilyOs.<Layer>.<Feature>` | `FamilyOs.Application.Documents.Commands` |
| Class / Record / Struct | `PascalCase` | `UploadDocumentCommand` |
| Interface | `IPascalCase` | `IDocumentStorage` |
| Method / Property | `PascalCase` | `ExtractAsync` |
| Public field | (tilos public field) | – |
| Private field | `_camelCase` | `_repository` |
| Local variable / param | `camelCase` | `documentId` |
| Constant | `PascalCase` | `MaxUploadSizeBytes` |
| Generic type param | `T`, `TKey`, `TResult` | – |
| Async metódus | suffix `Async` kötelező | `SaveAsync` |
| Enum értékek | `PascalCase` (DB-ben snake_case) | `Suggested` |
| File | `<Type>.cs`, egy public type / file | `UploadDocumentCommand.cs` |

**Tilos:**
- `var` használata, ahol a típus nem evidens a jobb oldalból. `var x = GetX();` → `var x` ok ha `GetX` neve egyértelmű, egyébként explicit típus.
- Hungarian notation (`strName`, `intCount`).
- `m_`, `s_` prefixek; az `_` private fielden elég.

### 2.3 Class / type design

- **Új típus először `sealed record` formában** (immutable). Csak akkor
  `class`, ha tényleg kell mutability vagy ősosztály.
- **Domain entity:** `class`, private setterekkel; állapot-változások
  metóduson keresztül (`ApproveTask(userId)`, nem `task.Status = ...`).
- **DTO:** `sealed record`, csak primitives + más DTO-k.
- **Command / Query (MediatR):** `sealed record` `IRequest<TResponse>` impl.

```csharp
public sealed record UploadDocumentCommand(
    string Title,
    Stream FileStream,
    string FileName,
    string MimeType,
    Guid? RelatedFamilyMemberId,
    bool IsPrivate) : IRequest<DocumentDto>;
```

### 2.4 Null és error semantika

- `Nullable<T>` explicit (`int?`, `Guid?`); reference type-on `string?`
  vs `string`.
- **Soha ne dobj `Exception` típust** önmagában. Saját kivételhierarchia:
  `DomainException` ← `ValidationException`, `NotFoundException`,
  `ConflictException`, `ForbiddenException`, `AiProviderException`.
- **Use Result/Either csak ott, ahol értelmes** (pipeline-szintű ágak).
  MVP-ben nem kötelező; exceptions + global handler elég.

### 2.5 Async / threading

- **Mindenhol `async/await`.** Soha `.Result`, soha `.Wait()`.
- I/O metódus suffix `Async` (`SaveAsync`, `GetAsync`).
- `CancellationToken ct` minden külső I/O metódus utolsó paramétere.
- `ConfigureAwait(false)` **nem kötelező** — a backend nincs SynchronizationContext.

### 2.6 Tilos / Avoid

- `Console.WriteLine` — Serilog van.
- Statikus mutable state.
- Service Locator pattern (`IServiceProvider.GetService<...>()` az alkalmazás
  kódban — csak composition root-ban).
- Reflection a hot pathon (DI / EF Core elég).
- Magic number / string — konstans vagy enum.

---

## 3. Projekt- és mappastruktúra

A solution-szintű struktúrát az `architecture.md` 2. szakasza rögzíti.
Projekt-belül egységesen:

```
src/FamilyOs.Application/
├─ Abstractions/                    # interfészek, amelyeket az infra implementál
│  ├─ Ai/
│  ├─ Storage/
│  └─ Common/                        # IClock, ICurrentUserAccessor
├─ Common/
│  ├─ Behaviors/                    # MediatR pipeline behaviors
│  ├─ Errors/                       # exception types
│  └─ Mapping/                      # Mapster profiles
├─ <Feature>/                        # pl. Documents/
│  ├─ Commands/
│  │  ├─ UploadDocument/
│  │  │  ├─ UploadDocumentCommand.cs
│  │  │  ├─ UploadDocumentHandler.cs
│  │  │  └─ UploadDocumentValidator.cs
│  │  └─ ...
│  ├─ Queries/
│  ├─ Dtos/
│  └─ Events/                       # domain event handlerek
└─ DependencyInjection.cs            # AddApplication() extension
```

Backendben a feature-mappa **nem keveredik réteggel**. Az
`Application/Documents/` az use case-ek; az `Infrastructure/Persistence/`
és `Infrastructure.Ai/Tasks/` a megvalósítások.

---

## 4. DTO vs Entity

| Szempont | Entity | DTO |
|---|---|---|
| Hol él | `Domain` projekt | `Application/<Feature>/Dtos` |
| Mutability | private setter, behavior method | immutable record |
| Validáció | invariáns a metódusban | bemeneten: FluentValidation; kimeneten: nincs |
| Hivatkozás | navigation property-k EF Core-rel | csak primitív + sub-DTO |
| API exposure | **soha nem szivároghat** API-ra | API-n ez megy ki/be |

**Tilos:** entity-t controller-ből visszaadni.
**Tilos:** entity-vel command-ot meghívni.
**Mapper:** Mapster (gyors, kódgenerálás).

```csharp
// Application/Documents/Mapping/DocumentMappingProfile.cs
public sealed class DocumentMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config) =>
        config.NewConfig<Document, DocumentDto>()
              .Map(dest => dest.RowVersion,
                   src => Convert.ToBase64String(src.RowVersion ?? Array.Empty<byte>()));
}
```

---

## 5. Validáció

### 5.1 Réteg-szabályok

| Réteg | Validáció típus | Példa |
|---|---|---|
| API endpoint | model binding (formátum) | `[FromRoute] Guid id` |
| Application command | FluentValidation pipeline | min/max length, mező-kombináció |
| Domain entity | invariáns metódusban | `Reminder.Create(...)` → `throw new DomainException(...)` |
| DB | CHECK constraint | belt-and-suspenders |

### 5.2 FluentValidation konvenciók

- Egy command → egy validator file (`<Command>Validator.cs`).
- Hibák magyar nyelven a felhasználói üzenetre (`ProblemDetails.detail`),
  de a `PropertyName` angol identifier (a frontend ezt mappelheti
  visszafelé).

```csharp
public sealed class UploadDocumentValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("A cím megadása kötelező.")
            .MaximumLength(200).WithMessage("A cím maximum 200 karakter lehet.");

        RuleFor(x => x.FileName)
            .Must(HasAllowedExtension)
            .WithMessage("Nem támogatott fájltípus.");
    }
    // ...
}
```

### 5.3 Domain invariánsok

```csharp
public sealed class Reminder
{
    private Reminder() { } // EF Core

    public static Reminder ForTask(Guid taskId, DateTime triggerUtc, NotificationChannel channel)
    {
        if (triggerUtc.Kind != DateTimeKind.Utc)
            throw new DomainException("Reminder.TriggerUtc must be UTC.");
        return new Reminder { Id = Guid.NewGuid(), TaskId = taskId, ... };
    }

    public static Reminder ForDeadline(Guid deadlineId, ...) { /* analóg */ }
}
```

---

## 6. Hibakezelés

### 6.1 Exception hierarchia

```
DomainException
├─ ValidationException        (400)
├─ NotFoundException          (404)
├─ ConflictException          (409)
├─ ForbiddenException         (403)
├─ UnsupportedMediaException  (415)
└─ DomainBusinessRuleException(422)

InfrastructureException
├─ AiProviderUnavailableException  (503)
├─ ExternalServiceException        (502)
└─ StorageException                (500)
```

### 6.2 Globális handler

Az API `ExceptionToProblemDetailsMiddleware`-je egységesen alakít:
- `DomainException` → 4xx megfelelően + magyar `detail`.
- `InfrastructureException` → 5xx + sanitized magyar `detail` (a belső
  hibaüzenet csak a logba megy).
- Egyéb (`Exception`) → 500 + „Ismeretlen hiba történt.", traceId-vel.

### 6.3 Application réteg

Soha nem fogjuk el a `DomainException`-t — feljebb folyik. Az
infrastruktúra-szintű külső hibákat (`HttpRequestException` →
`AiProviderUnavailableException`) ott konvertáljuk, ahol a határt
átlépjük.

### 6.4 Logging vs. exception

- **`throw` minden „abnormal" esetben.** Nem kell minden hibára IF-elni.
- Logoljuk az `InfrastructureException` típusokat `Warning`-on (a global
  handler), a 500-asokat `Error`-on, a `DomainException`-t `Information`-on
  (várt felhasználói hiba).

---

## 7. Logolás

### 7.1 Konvenciók

- **Serilog**, structured logging.
- **Üzenet template** angol, fix string; a változó értékek property-k.
- **Magyar üzenet** soha nem kerül logba (a logba angol, a UI-ra magyar).

```csharp
_logger.LogInformation(
    "Document {DocumentId} uploaded by user {UserAccountId}, size={SizeBytes}",
    document.Id, userId, document.SizeBytes);
```

### 7.2 Loglevel-ek

| Level | Mikor |
|---|---|
| Trace | jelenleg nem használjuk |
| Debug | fejlesztői részletek (DI registration, raw SQL) |
| Information | sikeres üzleti művelet (document uploaded, reminder fired) |
| Warning | self-correcting hibák (provider retry, transient connection) |
| Error | hibás állapot (job failed, exception caught) |
| Critical | service down, data loss kockázat |

### 7.3 Tilos a logban

(lásd `security-privacy.md` 5.3, 11.3):
- AI prompt teljes szövege (csak hash + hossz).
- AI válasz teljes szövege.
- OAuth token, jelszó, cookie érték.
- `Document.Content` egészében.
- Health-data (`MedicalRecord.StructuredJson`) bármilyen formában.

### 7.4 Audit log vs. operational log

| Audit (DB `audit_log`) | Operational (Serilog) |
|---|---|
| üzleti esemény (Create, Update, Approve) | technikai esemény (HTTP request, exception) |
| immutable, append-only | rotált, törölhető |
| User → query | DevOps → grep |

---

## 8. Tesztelési standardok

### 8.1 Piramis

| Szint | Eszköz | Lefedettség |
|---|---|---|
| Unit | xUnit + FluentAssertions + NSubstitute | minden Domain szolgáltatás, minden Application handler |
| Integration | xUnit + Testcontainers (Postgres) + WebApplicationFactory | minden API endpoint legalább happy + 1 hiba út |
| E2E | Playwright | minden UC-01..UC-08 + `@security` regresszió |

**Cél lefedettség MVP-re:** Domain ≥ 85%, Application ≥ 75%, Infrastructure ≥ 50%
(line coverage). Az API integrációs tesztek elsősorban kontraktus-megfelelést
mérnek, nem coverage-t.

### 8.2 Unit teszt konvenciók

- File-név: `<TypeUnderTest>Tests.cs`.
- Metódusnév: `Method_State_ExpectedBehavior` (angol).
- AAA pattern (Arrange / Act / Assert).
- **NSubstitute** preferált a Moq fölött (egyszerűbb API).
- Egyetlen `Assert.That`/`.Should()` egy logikai check-re.

```csharp
[Fact]
public async Task Handle_WhenSha256Exists_Returns409Conflict()
{
    // Arrange
    var existing = new Document { Sha256 = "abc..." };
    _docRepo.GetBySha256Async("abc...", default).Returns(existing);
    var command = new UploadDocumentCommand(...);

    // Act
    var action = () => _handler.Handle(command, default);

    // Assert
    await action.Should().ThrowAsync<ConflictException>();
}
```

### 8.3 Integration teszt konvenciók

- Testcontainers-szel friss Postgres + alkalmazás-szinten ugyanaz a DI.
- `WebApplicationFactory<Program>` az API teszteknél.
- **Adatbázis tisztítás** minden teszt előtt: `Respawn` library.
- Real Tesseract és Ollama **nem** integration testben — stub provider.

```csharp
public sealed class DocumentsEndpointTests : IClassFixture<FamilyOsTestFixture>
{
    private readonly HttpClient _client;
    public DocumentsEndpointTests(FamilyOsTestFixture fixture) { _client = fixture.CreateClient(); }

    [Fact]
    public async Task POST_Documents_WithValidPdf_Returns201()
    {
        var content = TestData.LoadPdf("axa-kotveny.pdf");
        // ...
    }
}
```

### 8.4 E2E (Playwright)

- Magyar nyelvű UI assertion-ek (`page.getByText('Dokumentumok')`).
- A backend Testcontainer-rel hisztorikus auth állapot beadva (mock OAuth).
- **Címkék:** `@smoke`, `@e2e-pipeline`, `@security`. CI matrix futtatja.

### 8.5 Golden samples

- 15-20 magyar mintadokumentum a `tests/Goldens/`-ben.
- Determinisztikus stub provider (`InMemoryAiProvider`) ad fix választ.
- Új prompt-verzió bevezetésekor a golden sample-ek átvizsgálandók.

---

## 9. Angular / TypeScript standardok

### 9.1 TypeScript beállítások

```json
// tsconfig.json
{
  "compilerOptions": {
    "strict": true,
    "noUncheckedIndexedAccess": true,
    "noImplicitOverride": true,
    "noPropertyAccessFromIndexSignature": true,
    "exactOptionalPropertyTypes": true
  }
}
```

### 9.2 Naming

| Elem | Konvenció | Példa |
|---|---|---|
| Class | `PascalCase` | `DocumentsFacade` |
| Interface | `PascalCase`, **nincs `I` prefix** | `DocumentFilter` |
| Method / function | `camelCase` | `loadDocuments` |
| Variable / property | `camelCase` | `currentUser` |
| Constant (module-level) | `SCREAMING_SNAKE` | `MAX_UPLOAD_BYTES` |
| Component selector | `app-<kebab>` | `app-document-card` |
| Component file | `<kebab>.component.ts` | `document-card.component.ts` |
| Service file | `<kebab>.service.ts` | `auth.service.ts` |
| Facade file | `<kebab>.facade.ts` | `documents.facade.ts` |
| Signal | `camelCase`, név állapot (nem akció) | `currentUser`, `isLoading` |

### 9.3 Angular komponens szabályok

- **Standalone csak.** `NgModule` tilos.
- **`changeDetection: OnPush`** kötelező minden komponensben.
- **Signal-state.** A `@Input` és `@Output` helyett `input()`, `output()`
  függvény-API (Angular 17+).
- **No logic in template** — komputált értékek `computed()`-be.
- **Stateless component preferált**, állapot a facade-ban.
- **HostListener** kerülendő; eseménykezelést template binding-gel.

```ts
@Component({
  selector: 'app-document-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HuDatePipe, FileSizePipe],
  templateUrl: './document-card.component.html',
})
export class DocumentCardComponent {
  doc = input.required<DocumentDto>();
  selected = input<boolean>(false);
  approve = output<Guid>();

  showSuggestionBanner = computed(() => this.doc().origin === 'AiSuggested');
}
```

### 9.4 Facade pattern

A komponensek **nem** hívnak HTTP-t közvetlenül. A feature `facade` szolgáltatás:
- Tartja a state-et (signal-store).
- Hívja az API-klienst.
- Egyszerű, deklaratív API a komponenseknek.

### 9.5 RxJS használat

- HTTP / SignalR: `Observable`.
- Multi-shot reaktív stream: `Observable`.
- UI state: `signal` / `computed` / `effect`.
- **Tilos:** `subscribe()` a komponens body-jában `takeUntil` nélkül.
  Inkább `toSignal(...)` adapter vagy `firstValueFrom` az async metódusban.

### 9.6 Stílus

- Tailwind utility-osztály első (a 90% case).
- Komplex / ismétlődő → `@apply` egy komponens SCSS-be vagy global theme.
- **Inline `[style]` binding** tilos, kivéve dinamikus érték (pl. progress bar width).

### 9.7 Hibakezelés a UI-on

- `errorInterceptor` egy `AppError`-t dob.
- A komponens facadja `try/catch` után state-et frissít:
  `store.update({ loading: false, error: e.message })`.
- A toast a globális error pop-up — facade nem hív közvetlenül toast-ot,
  hanem a global `notification.service` figyel a `effect()`-en.

---

## 10. Frontend mappastruktúra

Lásd `frontend-structure.md` 2. Egyetlen tovább finomítás itt:

- **Egy komponens / egy mappa**, ha legalább 2 fájl tartozik hozzá
  (`.ts`, `.html`, opcionálisan `.scss`, `.spec.ts`):

```
features/documents/
├─ components/
│  └─ document-card/
│     ├─ document-card.component.ts
│     ├─ document-card.component.html
│     └─ document-card.component.spec.ts
```

- Ha csak `.ts` (egyfile komponens), akkor a mappa nem kötelező.

---

## 11. Controller / endpoint szabályok (no business logic)

A `api-design.md` és `architecture.md` 3.5 megfogalmazta: **nincs üzleti logika
az endpointokon, nincs AI provider hívás onnan**.

Endpoint sablon:

```csharp
internal static class DocumentsModule
{
    public static IEndpointRouteBuilder MapDocuments(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/documents").RequireAuthorization();

        group.MapPost("", UploadDocumentAsync)
             .DisableAntiforgery()
             .WithName("UploadDocument");

        return app;
    }

    private static async Task<Results<Created<DocumentDto>, ProblemHttpResult>>
        UploadDocumentAsync(
            [FromForm] UploadDocumentForm form,
            ISender sender,
            CancellationToken ct)
    {
        var cmd = form.ToCommand();              // egyszerű, ostoba mapping
        var dto = await sender.Send(cmd, ct);    // MediatR -> handler
        return TypedResults.Created($"/api/v1/documents/{dto.Id}", dto);
    }
}
```

**Engedélyezett a controller-ben:**
- Model binding, mapping a command-ra (`form.ToCommand()`).
- `ISender.Send(command, ct)` hívás.
- HTTP status / location header beállítás.
- Authorization policy.

**Tilos a controller-ben:**
- `DbContext` használat.
- `HttpClient` hívás.
- AI provider hívás.
- `if`/`switch` üzleti döntésekkel.
- Logolás (a Handler logol).

---

## 12. Tooling és enforcement

| Eszköz | Cél | Hol |
|---|---|---|
| `dotnet format` | C# formatter | local + CI |
| `csharpier` | opinion-formatter | pre-commit hook |
| Roslyn analyzers (`Microsoft.CodeAnalysis.NetAnalyzers`) | C# code quality | CI build |
| `dotnet list package --vulnerable` | sebezhető függőség | CI |
| `eslint` + `@angular-eslint` | TS lint | local + CI |
| `prettier` | TS/HTML/SCSS formatter | pre-commit |
| `npm audit` / `pnpm audit` | sebezhető JS | CI |
| `commitlint` | conventional commits | git hook |
| `husky` | git hook orchestrator | per-clone |

CI „red gate" (PR-on át nem mehet, ha ezek vörösek):
- build, test, format check, eslint, vulnerable scan.

CI „warning gate" (PR mehet, de figyelmeztet):
- ZAP baseline, coverage csökkenés > 2%.

---

## 13. Git workflow

### 13.1 Branch elnevezés

- `feature/<feature-slug>` — új feature.
- `fix/<issue-slug>` — bug.
- `chore/<task-slug>` — karbantartás.
- `factory/<scope>` — factory-engineer önfejlesztő ágak (lásd CLAUDE.md).

### 13.2 Conventional commits

- `feat: add document upload endpoint`
- `fix: handle empty OCR output gracefully`
- `chore: bump npm packages`
- `docs: clarify reminder catch-up window`
- `test: add golden samples for AXA invoice`
- `refactor: extract embedding chunker to domain service`
- `perf: add HNSW ef_search tuning to settings`

### 13.3 PR követelmények

- Egy PR egy célt szolgál (ld. 1.5).
- Description tartalmazza:
  - Mit változtat (1-3 mondat).
  - Hogy tesztelhető manuálisan.
  - Mit nem érint (negative scope).
- Reviewer: legalább `code-reviewer` agent + 1 humán (admin).

### 13.4 Merge stratégia

- Squash merge a feature ágakra.
- Merge commit a release-ágakra (`release/v1.0`).
- `main` mindig zöld; ha CI piros, nem mergelünk.

---

## 14. Dokumentáció a kódban

### 14.1 XML doc-comment

- Public Application interface (Abstractions/Ai/*, Storage/*): kötelező
  `<summary>` magyarul **vagy** angolul (csapatdöntés: az `IDocumentTextExtractor`
  XML doc-ja angolul, mert a metódus neve és szignatúrája is angol).
- Public domain entity factory metódus: rövid `<summary>` angolul.
- Belső class-ok: nem kötelező; ha a név magától beszél, nem kell.

### 14.2 Kommentár tilos esetek

- A „mit csinál" magyarázat redundáns a jó név mellett.
- TODO comment lejárati dátum nélkül (`// TODO`) tilos. Helyette:
  `// TODO(2026-09-01): handle empty OCR fallback` vagy GitHub issue link.

### 14.3 Architecture Decision Record (ADR)

- Új jelentős döntés → `docs/decisions/ADR-NNNN-rövid-cím.md`.
- A formátum: `# ADR-NNNN — Cím`, *Státusz*, *Dátum*, *Döntéshozó*,
  *Kontextus*, *Döntés*, *Indoklás*, *Következmények*.
- Lásd a meglévő ADR-eket (`ADR-0001` … `ADR-0004`).

---

## 15. Performance és resource

- **Hot path:** ne allokálj feleslegesen (`StringBuilder` long-running
  concat-ra; `Span<T>` ahol indokolt).
- **N+1 query** tilos. EF Core `Include` / projekció.
- **Lazy loading kikapcsolva** az `OnConfiguring`-ban (csak explicit
  `Include`).
- **CancellationToken** átadása minden async hívásnak.
- **Streaming endpointok** (`IAsyncEnumerable<T>`) nagy export-okra
  (audit log CSV).

---

## 16. Konfiguráció és titkok

- **Soha nem commitolunk titkot.** `.env*` `gitignore`-ben.
- Konfiguráció hierarchia: `appsettings.json` → `appsettings.<Env>.json`
  → ENV változók → `dotnet user-secrets` (dev).
- Production-on minden titok ENV-ben (Docker secrets vagy mount).
- A `factory-engineer` agent **nem módosíthatja** az `appsettings.Production.json`
  privacy beállítását (lásd `CLAUDE.md`, `security-privacy.md` 8.1).

---

## 17. Mit ellenőriz a code-reviewer agent

Minimum-checklist (a `code-reviewer` agent prompt-tervében rögzítve):

- [ ] Build + test zöld.
- [ ] Conventional commit + branch név megfelel.
- [ ] Üzleti logika nincs az endpointban.
- [ ] AI provider hívás nincs az endpointban.
- [ ] DTO sosem entity, entity sosem szivárog API-ra.
- [ ] FluentValidation szabály van minden új command-ra.
- [ ] Hibák magyar `detail`-lel, ProblemDetails formátum.
- [ ] Új public-facing felület (endpoint, DTO) szerepel az
      `api-design.md`-ben (vagy módosítás van az ADR-ben).
- [ ] Új AI-prompt verzió rögzítve (`prompt_version`).
- [ ] Privacy szempont — semmilyen prompt / dokumentum-content nem
      kerül logba.
- [ ] Tesztek megfelelőek (lásd 8.).
- [ ] EF Core query nem N+1.
- [ ] Új migráció Testcontainer-tesztelt.

---

## 18. Mit NEM szabályoz ez a doksi

- **Üzleti döntés / scope** — a backlog és product-vision dönt.
- **UX / dizájn** — a frontend-structure megemlíti, de a részletes
  design system v2.
- **Performance benchmark targets** — a `search-strategy.md` 9.1 ad
  becslést, de nincs SLO MVP-ben.
- **CI/CD vagy K8s** — a `architecture.md` 9. és a `DELIVERY.md` (v0.12)
  fedi.

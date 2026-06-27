# Epic A — Alapok és infra — Backend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` (FULL — baseline)
> - `architecture.md` (FULL)
> - `api-design.md` §1 (konvenciók), §3 (Auth), §4 (System), §1.3 (ProblemDetails)
> - `security-privacy.md` §3 (Authentication), §4 (Authorization), §5 (Audit alap), §11.3 (logok)
> - `database-schema.md` §1 (env), §2 (enumok), §4.1–4.2 (family_member, user_account)
> - `domain-model.md` §1.1 (UserAccount), §1.2 (FamilyMember), §0 (közös konvenciók)
> - ADR-0003 (LAN-only — CORS allowlist)
>
> **Story-k:** A1, A2, A3, A4, A5 (BE rész)
> **Fázis:** Fázis 1 (solution), Fázis 2 (DB), Fázis 3 (auth)

---

## Áttekintés

A backend megalapozása három logikai blokkban: (1) solution + projektek
csontváz, (2) PostgreSQL séma és EF Core, (3) Google OAuth + cookie session
+ ProblemDetails + logolás. Az epic végén a `POST /api/v1/auth/login/google`
és `GET /api/v1/family-members` működik.

## Taskok

### T-ABE-01 — Solution és projekt-csontvázak
- **Cél:** 6 .NET projekt + `Directory.Build.props` strict warning-szabályokkal.
- **Fájlok:**
  - `FamilyOs.sln`
  - `Directory.Build.props` (Nullable enable, TreatWarningsAsErrors,
    AnalysisLevel latest-recommended)
  - `.editorconfig`
  - `src/FamilyOs.Domain/FamilyOs.Domain.csproj`
  - `src/FamilyOs.Application/FamilyOs.Application.csproj`
  - `src/FamilyOs.Infrastructure/FamilyOs.Infrastructure.csproj`
  - `src/FamilyOs.Infrastructure.Ai/FamilyOs.Infrastructure.Ai.csproj`
  - `src/FamilyOs.Api/FamilyOs.Api.csproj`
  - `src/FamilyOs.Workers/FamilyOs.Workers.csproj`
- **AC:**
  - [ ] Adott `dotnet build` parancs, és minden csomag zöld.
  - [ ] A dependency graph megfelel az `architecture.md` 2.-nek (Domain
        zero-dep, Application csak Domain-t, stb.).
  - [ ] `Domain` projekt **csak** standard `Microsoft.Extensions.Logging.Abstractions`-t
        importál külső csomagból (semmilyen EF Core / HTTP / AI SDK).
- **Függőség:** —

### T-ABE-02 — Roslyn analyzers + csharpier konfiguráció
- **Cél:** kódminőség enforcer.
- **Fájlok:**
  - `Directory.Packages.props` (central package management).
  - `.csharpierrc.yaml`.
  - `.config/dotnet-tools.json` (csharpier mint local tool).
- **AC:**
  - [ ] `dotnet csharpier --check .` zöld.
  - [ ] `Microsoft.CodeAnalysis.NetAnalyzers` aktív.
  - [ ] Warning-as-Error a `coding-standards.md` 2.1 listája szerint.

### T-ABE-03 — `IClock` és `ICurrentUserAccessor` abstractions
- **Cél:** időt és current usert sehol ne statikusan érjük el.
- **Fájlok:**
  - `src/FamilyOs.Application/Abstractions/Common/IClock.cs`
  - `src/FamilyOs.Application/Abstractions/Common/ICurrentUserAccessor.cs`
  - `src/FamilyOs.Infrastructure/Common/SystemClock.cs`
  - `tests/FamilyOs.Application.Tests/Common/FakeClock.cs`
- **AC:**
  - [ ] Sehol nincs `DateTime.UtcNow` közvetlen hívás az Application-ben.
  - [ ] Egy unit teszt `FakeClock`-kal időtolást szimulál.

### T-ABE-04 — PostgreSQL DbContext és EF Core bootstrap
- **Cél:** alap DbContext + `EFCore.NamingConventions` snake_case mappolás.
- **Fájlok:**
  - `src/FamilyOs.Infrastructure/Persistence/FamilyOsDbContext.cs`
  - `src/FamilyOs.Infrastructure/Persistence/DesignTimeFactory.cs`
  - `src/FamilyOs.Infrastructure/DependencyInjection.cs`
- **AC:**
  - [ ] `AddInfrastructure(IServiceCollection, IConfiguration)` bekonfigurálja
        a `Npgsql` + pgvector + Mapster + snake_case provider-t.
  - [ ] Connection string olvasása `appsettings.json` + env override.
- **Függőség:** T-ABE-01.

### T-ABE-05 — `__InitialSetup` raw SQL migráció
- **Cél:** extensions, ICU collation, FTS config, trigger függvények.
- **Fájlok:**
  - `src/FamilyOs.Infrastructure/Persistence/Migrations/<ts>_InitialSetup.cs`
- **AC:**
  - [ ] `pgvector`, `pg_trgm`, `unaccent`, `pgcrypto`, `btree_gin` extensions
        létrejönnek (`database-schema.md` §1.2).
  - [ ] `hungarian_unaccent` text search configuration létrejön.
  - [ ] `app.set_updated_utc()` és `app.audit_log_immutable()` plpgsql függvények.
  - [ ] Idempotens: `IF NOT EXISTS` guardokkal.
- **Függőség:** T-ABE-04.

### T-ABE-06 — Enum-típusok PostgreSQL-ben
- **Cél:** 22 enum `CREATE TYPE` (lásd `database-schema.md` §2, v0.2 a
  `Cancelled` és `ExternalApiCall` értékekkel együtt).
- **Fájlok:**
  - ugyanaz a `InitialSetup` migráció vagy külön `CreateEnums.cs`.
  - `src/FamilyOs.Domain/Enums/*.cs` (C# oldali enumok).
  - `src/FamilyOs.Infrastructure/Persistence/FamilyOsDbContext.cs` —
    `modelBuilder.HasPostgresEnum<UserRole>("app", "user_role")` stb.
- **AC:**
  - [ ] Minden enum a `app` sémában él.
  - [ ] A `reminder_status` tartalmazza a `'Cancelled'` értéket.
  - [ ] Az `audit_action` tartalmazza a `'ExternalApiCall'` értéket.
- **Függőség:** T-ABE-05.

### T-ABE-07 — `FamilyMember` és `UserAccount` entity konfigurációk
- **Cél:** a két entitás EF Core mappolása és táblák létrejönnek.
- **Fájlok:**
  - `src/FamilyOs.Domain/Entities/FamilyMember.cs`
  - `src/FamilyOs.Domain/Entities/UserAccount.cs`
  - `src/FamilyOs.Domain/Enums/UserRole.cs`, `Relation.cs`
  - `src/FamilyOs.Infrastructure/Persistence/Configurations/FamilyMemberConfiguration.cs`
  - `src/FamilyOs.Infrastructure/Persistence/Configurations/UserAccountConfiguration.cs`
  - `<ts>_Initial.cs` migráció (EF generálva).
- **AC:**
  - [ ] `domain-model.md` §1.1–1.2 szerinti összes mező.
  - [ ] Indexek: UNIQUE `(google_subject)`, UNIQUE `(email)` (partial soft-delete).
  - [ ] `HasQueryFilter` a `DeletedUtc IS NULL`-ra.
  - [ ] `UseXminAsConcurrencyToken()` mindkettőn.
- **Függőség:** T-ABE-06.

### T-ABE-08 — `DbSeedRunner` (topic-fa + default Source)
- **Cél:** idempotens seed a magyar topic-fára és Upload source-ra.
- **Fájlok:**
  - `src/FamilyOs.Infrastructure/Persistence/Seed/DbSeedRunner.cs`
  - `src/FamilyOs.Infrastructure/Persistence/Seed/SeedData.cs`
- **AC:**
  - [ ] A 8 root topic + 11 subtopic (`database-schema.md` §5.1) létrejön.
  - [ ] Default `Source` (`Name = 'Kézi feltöltés'`, `Kind = Upload`).
  - [ ] Második futtatásra `ON CONFLICT DO NOTHING` — ugyanaz az állapot.
- **Függőség:** T-ABE-07.

### T-ABE-09 — Common types: ProblemDetails kategóriák + magyar üzenet-katalógus
- **Cél:** egységes hibatípusok és magyar `detail` üzenetek.
- **Fájlok:**
  - `src/FamilyOs.Application/Common/Errors/DomainException.cs` (+ alosztályok:
    `ValidationException`, `NotFoundException`, `ConflictException`,
    `ForbiddenException`, `UnsupportedMediaException`,
    `DomainBusinessRuleException`).
  - `src/FamilyOs.Application/Common/Errors/InfrastructureException.cs`
    (+ alosztályok: `AiProviderUnavailableException`,
    `ExternalServiceException`, `StorageException`).
  - `src/FamilyOs.Api/ErrorCatalog.hu.resx` (magyar üzenetek).
- **AC:**
  - [ ] Minden hiba típus magyar message-ekkel.
  - [ ] `coding-standards.md` §6.1 hierarchia betartva.

### T-ABE-10 — `ExceptionToProblemDetailsMiddleware`
- **Cél:** minden 4xx/5xx response `application/problem+json` formátum, magyar `detail`.
- **Fájlok:**
  - `src/FamilyOs.Api/Middleware/ExceptionToProblemDetailsMiddleware.cs`
  - `src/FamilyOs.Api/Extensions/ProblemDetailsExtensions.cs` (helper).
- **AC:**
  - [ ] `DomainException` ágak → megfelelő status code (`api-design.md` §1.2).
  - [ ] `traceId` minden válaszban (W3C TraceContext).
  - [ ] `fieldErrors` a `ValidationException`-ből kibontva.
  - [ ] `InfrastructureException` belső szöveg nem szivárog ki — sanitized.
- **Függőség:** T-ABE-09.

### T-ABE-11 — FluentValidation pipeline behavior
- **Cél:** minden MediatR command-ra automatikus validáció.
- **Fájlok:**
  - `src/FamilyOs.Application/Common/Behaviors/ValidationBehavior.cs`
  - `src/FamilyOs.Application/DependencyInjection.cs` (MediatR + FluentValidation
    regisztráció).
- **AC:**
  - [ ] Hibás command esetén `ValidationException` dobódik a magyar
        üzenettel.
  - [ ] Validátor automatikusan felismerve az assembly scan-ből.

### T-ABE-12 — Google OAuth handler + cookie session
- **Cél:** `__Host-family-os-session` HttpOnly cookie sliding 30 nappal.
- **Fájlok:**
  - `src/FamilyOs.Infrastructure/Auth/GoogleAuthHandler.cs` (id_token validáció).
  - `src/FamilyOs.Infrastructure/Auth/CookieAuthSetup.cs`.
  - `src/FamilyOs.Infrastructure/Auth/AllowlistService.cs`.
  - `src/FamilyOs.Infrastructure/DependencyInjection.cs` update.
  - `appsettings.json`: `Auth { GoogleClientId, AllowedEmails[], BootstrapAdmin }`.
- **AC:**
  - [ ] A `id_token` validáció: issuer, expiry, audience.
  - [ ] Email nincs allowlist-en → 403 magyar üzenet.
  - [ ] Első login a `BootstrapAdmin` email-en `Role = Admin`.
  - [ ] Cookie attribútumok: HttpOnly, Secure, SameSite=Lax, `__Host-` prefix.
- **Függőség:** T-ABE-07.

### T-ABE-13 — `RevokedSessions` és logout
- **Cél:** logout után a cookie azonnal érvénytelen.
- **Fájlok:**
  - `src/FamilyOs.Domain/Entities/RevokedSession.cs`
  - `src/FamilyOs.Infrastructure/Persistence/Configurations/RevokedSessionConfiguration.cs`
  - migráció (séma-frissítés).
  - `src/FamilyOs.Infrastructure/Auth/RevokedSessionChecker.cs` (cookie events).
- **AC:**
  - [ ] `POST /api/v1/auth/logout` 204; ugyanaz a cookie-érték 401 utána.

### T-ABE-14 — `CurrentUserService` és RBAC policy-k
- **Cél:** `[Authorize(Policy = "RequireAdult")]` és társai.
- **Fájlok:**
  - `src/FamilyOs.Infrastructure/Auth/CurrentUserService.cs` (impl).
  - `src/FamilyOs.Api/Auth/AuthorizationPolicies.cs`.
- **AC:**
  - [ ] `RequireAdmin`, `RequireAdult`, `RequireAuthenticated` policy-k.
  - [ ] `CurrentUserService` a claim-ekből építkezik (UserAccountId,
        FamilyMemberId, Role).

### T-ABE-15 — Auth endpointok
- **Cél:** `POST /api/v1/auth/login/google`, `POST /logout`, `GET /me`.
- **Fájlok:**
  - `src/FamilyOs.Application/Auth/LoginGoogleCommand.cs` + Handler.
  - `src/FamilyOs.Application/Auth/GetCurrentUserQuery.cs` + Handler.
  - `src/FamilyOs.Application/Auth/LogoutCommand.cs` + Handler.
  - `src/FamilyOs.Application/Auth/Dtos/CurrentUserDto.cs`.
  - `src/FamilyOs.Api/Endpoints/AuthModule.cs`.
- **AC:**
  - [ ] Integration teszt: Google id_token mock → 200 + cookie.
  - [ ] `/me` 401 cookie nélkül, 200 cookie-val.
- **Függőség:** T-ABE-12, T-ABE-14.

### T-ABE-16 — `FamilyMember` CRUD endpointok
- **Cél:** `api-design.md` §5 szerinti összes endpoint.
- **Fájlok:**
  - `src/FamilyOs.Application/Family/*.cs` (Create/Get/List/Update/Delete commands+queries+handlers+validators+DTOs).
  - `src/FamilyOs.Api/Endpoints/FamilyModule.cs`.
- **AC:**
  - [ ] Lista, get, create (Admin), patch (Admin, `If-Match`),
        soft delete (Admin) — működnek.
  - [ ] Child role korlátozott láthatóság szűrése.
- **Függőség:** T-ABE-15.

### T-ABE-17 — `UserAccount` invite + szerepkör-módosítás
- **Cél:** admin meghív, meghívott login-jakor aktiválódik.
- **Fájlok:**
  - `src/FamilyOs.Application/Users/InviteUserCommand.cs` + Handler.
  - `src/FamilyOs.Application/Users/PatchUserCommand.cs` (csak `role`, `isActive`).
  - `src/FamilyOs.Api/Endpoints/UsersModule.cs`.
  - `GoogleAuthHandler` módosítás: invite aktiválás.
- **AC:**
  - [ ] Meghívás → invite rekord az allowlist-en.
  - [ ] Meghívott első Google login → `UserAccount.IsActive = true`,
        `FamilyMemberId` beállítva.
  - [ ] Audit log: `Action = PermissionChange`.
- **Függőség:** T-ABE-16.

### T-ABE-18 — `auth/me/preferences` PATCH endpoint (B3)
- **Cél:** csendes órák + email opt-in.
- **Fájlok:**
  - `src/FamilyOs.Domain/Entities/UserPreferences.cs` (vagy `UserAccount` JSONB mező).
  - `src/FamilyOs.Application/Users/UpdatePreferencesCommand.cs` + Handler.
  - `src/FamilyOs.Api/Endpoints/AuthModule.cs` kiegészítés.
- **AC:**
  - [ ] `HH:mm` validáció a quiet hours-ra.
  - [ ] 200 + frissített DTO.

### T-ABE-19 — Serilog + W3C TraceContext
- **Cél:** structured log + per-request trace id.
- **Fájlok:**
  - `src/FamilyOs.Api/Program.cs` (Serilog hosting).
  - `src/FamilyOs.Api/Middleware/TraceIdEnrichmentMiddleware.cs`.
  - `appsettings.json`: Serilog konfig (Console + File rolling).
- **AC:**
  - [ ] Log üzenet template angol; magyar string sosem logban.
  - [ ] Minden response `X-Request-Id` headerben a traceId.
  - [ ] AI prompt teljes szöveg sosem logban (csak hash + hossz) —
        helper enforcement később, MVP-ben dokumentum-szintű jegyzet.

### T-ABE-20 — Healthcheckek
- **Cél:** `/healthz/live` és `/healthz/ready`.
- **Fájlok:**
  - `src/FamilyOs.Api/Program.cs` (`AddHealthChecks`).
  - `src/FamilyOs.Infrastructure/Health/OllamaHealthCheck.cs` (placeholder
    — később aktiválva).
- **AC:**
  - [ ] `live` mindig 200 (process up).
  - [ ] `ready` 200, ha DB elérhető; degraded ha Ollama nem (de még 200,
        mert az AI nem blocking az auth-hoz).

### T-ABE-21 — Integration test fixture
- **Cél:** Testcontainers Postgres + `WebApplicationFactory<Program>`.
- **Fájlok:**
  - `tests/FamilyOs.Api.IntegrationTests/FamilyOsTestFixture.cs`
  - `tests/FamilyOs.Api.IntegrationTests/AuthFlowTests.cs`
  - `tests/FamilyOs.Api.IntegrationTests/ProblemDetailsFormatTests.cs`
  - `tests/FamilyOs.Infrastructure.Tests/MigrationsTests.cs`
  - `tests/FamilyOs.Infrastructure.Tests/DbSeedRunnerTests.cs`
- **AC:**
  - [ ] `Respawn` adatbázis tisztítás minden teszt előtt.
  - [ ] Google `id_token` mock-olva (saját privát kulcs + JWT).
  - [ ] ProblemDetails formátum minden hiba útra ellenőrizve.

---

## Megvalósítási sorrend

```
T-ABE-01 → 02 → 03
        → 04 → 05 → 06 → 07 → 08            (Fázis 2: DB)
        → 09 → 10 → 11                       (Fázis 3 előkészület)
        → 12 → 13 → 14 → 15                  (Auth flow)
        → 16 → 17 → 18                        (User CRUD)
        → 19 → 20 → 21                        (logging + tests)
```

## Epic-DoD

- [ ] `make build && make test` zöld.
- [ ] Integration teszt-suite zöld (Testcontainers Postgres).
- [ ] Admin Google-lal belép, családtagot vesz fel, másik usert meghív.
- [ ] Bármilyen hiba ProblemDetails magyar `detail`-lel.
- [ ] `code-reviewer` (opus) jóváhagyta a security flow-t (T-ABE-12..18).
- [ ] Git tag `v0.3`.

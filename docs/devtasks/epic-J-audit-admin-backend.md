# Epic J — Audit + admin — Backend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` (FULL — különösen §7 Logging, §11 No business logic in controllers)
> - `security-privacy.md` (FULL — különösen §5 Audit log, §11 Üzemeltetés)
> - `domain-model.md` §1.16 (AuditLog)
> - `database-schema.md` §4.17 (audit_log + insert-only trigger), §2 (audit_action enum + ExternalApiCall v0.2)
> - `api-design.md` §18 (AI processing admin), §19 (Audit log), §6 (UserAccounts)
> - `architecture.md` §8 (cross-cutting), §12 (admin felület)
>
> **Story-k:** J1, J2, J3, J4
> **Fázis:** Fázis 12 (Hardening)

---

## Áttekintés

Az audit logger rendszerszerű (MediatR pipeline behavior + DI service),
nem szétszórt. A J epic biztosítja, hogy minden security event audit-ben
megjelenik, az admin böngészheti és exportálhatja, az AI jobok adminisztrálhatók.

## Taskok

### T-JBE-01 — `AuditLog` entity + insert-only trigger
- **Fájlok:**
  - `src/FamilyOs.Domain/Entities/AuditLog.cs` (insert-only, nincs setter
    az ID-n).
  - `src/FamilyOs.Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs`
  - migráció: `audit_log` tábla + immutable trigger + REVOKE.
- **AC:**
  - [ ] `database-schema.md` §4.17 v0.2 (`ExternalApiCall` is benne).
  - [ ] DB trigger: UPDATE / DELETE attempt → exception.
  - [ ] Engedély-szintű REVOKE a `family_app` role-on.

### T-JBE-02 — `IAuditLogger` service + DI
- **Fájlok:**
  - `src/FamilyOs.Application/Abstractions/Audit/IAuditLogger.cs`
  - `src/FamilyOs.Infrastructure/Audit/AuditLogger.cs`
- **AC:**
  - [ ] `LogAsync(AuditEntry)` non-blocking, fire-and-forget pattern
        (channel/queue), de durable (DB-be ír).
  - [ ] `details_json` builder helper: csak engedélyezett kulcsmezők
        (`security-privacy.md` §5.2 listája).

### T-JBE-03 — MediatR pipeline behavior az auditra
- **Cél:** minden command-on automatikusan audit log.
- **Fájlok:**
  - `src/FamilyOs.Application/Common/Behaviors/AuditBehavior.cs`
- **AC:**
  - [ ] Sikeres command → `Create`/`Update`/`Delete` AuditEntry típus
        szerint.
  - [ ] Custom audit attribute-tal felülírható.
  - [ ] Sensitive command kihagyható (`[NoAudit]` attribute).

### T-JBE-04 — Security event auditing
- **Cél:** auth-mozzanatok és RBAC változások.
- **Fájlok:**
  - kiegészítések: `GoogleAuthHandler` (Login, LoginFailed),
    `PatchUserCommand` (PermissionChange).
- **AC:**
  - [ ] `Login`: IP + UserAgent.
  - [ ] `LoginFailed`: email + ok.
  - [ ] `PermissionChange`: old/new role.

### T-JBE-05 — `ExternalApiCall` audit a Gmail/SMTP hívásokra
- **Cél:** v0.2 új audit action használata.
- **Fájlok:**
  - kiegészítés `GmailIngestionService`-ben (Epic K).
  - kiegészítés `SmtpNotificationChannel`-ban.
- **AC:**
  - [ ] Minden külső API hívás (provider, endpoint, status, latency).
  - [ ] **NEM** logoljuk a fetchelt email tartalmát.

### T-JBE-06 — `GET /api/v1/audit-log` admin lista
- **Fájlok:**
  - `src/FamilyOs.Application/Audit/ListAuditLogQuery.cs`
  - `src/FamilyOs.Api/Endpoints/AuditModule.cs`
- **AC:**
  - [ ] Szűrés: `?from=`, `?to=`, `?userAccountId=`, `?action=`,
        `?entityType=`, `?entityId=`.
  - [ ] Pagination kötelező (max 200/oldal).
  - [ ] `RequireAdmin` policy.

### T-JBE-07 — Pre-szűrt security-events nézet
- **Fájlok:**
  - `src/FamilyOs.Application/Audit/GetSecurityEventsQuery.cs`
- **AC:**
  - [ ] Csak `Login`, `LoginFailed`, `PermissionChange`, `ExternalApiCall`.
  - [ ] Default 7 nap visszamenőleg.

### T-JBE-08 — Audit log CSV/JSON export
- **Cél:** streaming nagy datasetre.
- **Fájlok:**
  - `src/FamilyOs.Application/Audit/ExportAuditLogQuery.cs`
  - `src/FamilyOs.Api/Endpoints/AuditModule.cs` `/export` kiegészítés.
- **AC:**
  - [ ] `?format=csv|json`.
  - [ ] `IAsyncEnumerable<>` streaming.
  - [ ] Audit a hozzáférés is (J-recursive: az export request maga
        AuditAction = `FileAccess`-ként loggolva).

### T-JBE-09 — AI jobs admin endpointok
- **Cél:** `api-design.md` §18.
- **Fájlok:**
  - `src/FamilyOs.Application/Admin/AiJobs/*.cs`
  - `src/FamilyOs.Api/Endpoints/AiJobsAdminModule.cs`
- **AC:**
  - [ ] `GET /api/v1/ai-jobs?status=Failed` lista.
  - [ ] `POST /api/v1/ai-jobs/{id}/retry` → status `Queued`, `next_attempt = now`.
  - [ ] `POST /api/v1/ai-jobs/{id}/cancel` → `Cancelled`.
  - [ ] `GET /api/v1/ai-jobs/queue-stats` aggregátum.

### T-JBE-10 — `AiProvider` admin endpointok (J4 előkészület)
- **Cél:** providerek listája + enabled/model szerkesztés.
- **Fájlok:**
  - `src/FamilyOs.Application/Admin/AiProviders/*.cs`
  - `src/FamilyOs.Api/Endpoints/AiProvidersAdminModule.cs`
- **AC:**
  - [ ] `GET /api/v1/ai-providers` lista (név, enabled, lastHealth).
  - [ ] `PATCH /api/v1/ai-providers/{name}` csak `enabled`, `model`.
  - [ ] **`PrivacyMode` nem szerkeszthető** itt — 422 magyar üzenettel.

### T-JBE-11 — Hangfire dashboard auth filter
- **Cél:** csak Admin férjen hozzá a `/hangfire`-hez.
- **Fájlok:**
  - `src/FamilyOs.Infrastructure/Hangfire/HangfireAuthorizationFilter.cs`
- **AC:**
  - [ ] Cookie session-ből current user; `Role != Admin` → 403.

### T-JBE-12 — Integration tesztek
- **Fájlok:**
  - `tests/FamilyOs.Api.IntegrationTests/Audit/AuditImmutabilityTests.cs`
  - `tests/FamilyOs.Api.IntegrationTests/Audit/AuditExportTests.cs`
  - `tests/FamilyOs.Api.IntegrationTests/Admin/AiJobsAdminTests.cs`
- **AC:**
  - [ ] UPDATE attempt audit_log-on → exception.
  - [ ] Export: stream nem load-ol a memóriába mindent.
  - [ ] Retry job: status állapot frissül.

---

## Megvalósítási sorrend

```
T-JBE-01 → 02 → 03                  (alap audit pipeline)
       → 04 → 05                    (security events + external API)
       → 06 → 07 → 08                (audit query + export)
       → 09 → 10                    (admin felület)
       → 11                           (Hangfire auth)
       → 12                           (tesztek)
```

## Epic-DoD

- [ ] Audit log immutability DB-szinten kényszerítve.
- [ ] Minden security event audit-ben.
- [ ] Admin lista, security view, export elérhető.
- [ ] AI jobs admin: retry, cancel, queue stats.
- [ ] Hangfire dashboard admin-only.
- [ ] PrivacyMode védve (kódba égetett kapu).

# Epic B — Felhasználó-kezelés — Backend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` (FULL — baseline)
> - `security-privacy.md` (FULL — különösen §3 Auth, §4 RBAC, §4.3 row-level)
> - `api-design.md` §5 (Family), §6 (Users), §3 (Auth)
> - `domain-model.md` §1.1 (UserAccount), §1.2 (FamilyMember)
> - `database-schema.md` §4.1, §4.2, §2 (user_role enum)
> - `reminder-engine.md` §5.4 (preferenciák — B3)
> - `architecture.md` §3.5 (Api), §8.1 (auth policy)
>
> **Story-k:** B1, B2, B3 (BE)
> **Fázis:** Fázis 3 (auth + alap CRUD), részben az Epic A-val átfedés

---

## Áttekintés

Az Epic A-ban már létrejött a `FamilyMember` és `UserAccount` mappolás +
auth + alap endpoint. Itt:
- **B1** kibővítjük a Family-t a teljes CRUD-dal és RBAC ellenőrzéssel.
- **B2** invite flow (admin meghív, meghívott login-jakor aktiválódik).
- **B3** felhasználói preferenciák (csendes órák, email opt-in).

> **Megjegyzés az átfedésre:** ha az Epic A taskokban a Family CRUD és
> invite már megvalósult (T-ABE-16, T-ABE-17, T-ABE-18), akkor ezek a
> taskok csak finomítások.

## Taskok

### T-BBE-01 — Row-level authorization service
- **Cél:** centralizált `IAuthorizationService.AuthorizeAsync(user,
  entity, requirement)` (security-privacy.md §4.3).
- **Fájlok:**
  - `src/FamilyOs.Application/Common/Authorization/IFamilyOsAuthorizationService.cs`
  - `src/FamilyOs.Infrastructure/Authorization/FamilyOsAuthorizationService.cs`
  - `src/FamilyOs.Application/Common/Authorization/Requirements/*.cs`
    (`CanReadDocument`, `CanWriteDocument`, `CanReadMedicalRecord`, …).
- **AC:**
  - [ ] Admin minden → engedélyezve.
  - [ ] Adult: saját + nem-private idegen olvasható; idegen private tilos.
  - [ ] Child: csak a `RelatedFamilyMemberId = ownFamilyMemberId` ÉS
        `!IsPrivate` rekordok.
  - [ ] Unit teszt minden kombinációra.

### T-BBE-02 — `FamilyMember` CRUD: List + Detail
- **Cél:** `GET /api/v1/family-members`, `GET /{id}` RBAC szűréssel.
- **Fájlok:**
  - `src/FamilyOs.Application/Family/ListFamilyMembersQuery.cs` +
    `FamilyMemberDto.cs`.
  - `src/FamilyOs.Application/Family/GetFamilyMemberQuery.cs`.
  - `src/FamilyOs.Api/Endpoints/FamilyModule.cs` (kiegészítés).
- **AC:**
  - [ ] Lista mindenkinek elérhető, de Child a `birthDate` és `notes`
        mezőket nem látja (DTO szintű maszkolás).
  - [ ] `?relation=Spouse` szűrés.

### T-BBE-03 — `FamilyMember` CRUD: Create, Patch, Delete
- **Cél:** Admin-only írási műveletek.
- **Fájlok:**
  - `src/FamilyOs.Application/Family/CreateFamilyMemberCommand.cs` +
    Handler + Validator.
  - `src/FamilyOs.Application/Family/PatchFamilyMemberCommand.cs` (`If-Match`).
  - `src/FamilyOs.Application/Family/DeleteFamilyMemberCommand.cs` (soft).
- **AC:**
  - [ ] `RequireAdmin` policy.
  - [ ] `If-Match` kötelező PATCH-nál, 409 ütközésnél.
  - [ ] Soft delete csak akkor, ha nincs élő `UserAccount` — egyébként 409.
  - [ ] Audit log: `Create`, `Update`, `Delete`.

### T-BBE-04 — `UserAccount` invite + activation flow
- **Cél:** B2 story teljes megvalósítása.
- **Fájlok:**
  - `src/FamilyOs.Application/Users/InviteUserCommand.cs` + Handler + Validator.
  - `src/FamilyOs.Domain/Entities/UserAccountInvite.cs` (vagy pre-créated
    UserAccount `IsActive = false`-szal — az architect döntse el az
    egyszerűbbet; lásd backlog B2).
  - `src/FamilyOs.Infrastructure/Auth/GoogleAuthHandler.cs` kiegészítés:
    invite resolution login-kor.
  - migráció, ha új tábla.
- **AC:**
  - [ ] Admin `POST /api/v1/user-accounts/invite` → 201 + Invite DTO.
  - [ ] Meghívott első Google login → szerepkör és `FamilyMemberId`
        beállítva, audit `PermissionChange`.
  - [ ] Allowlist automatikusan bővül a meghívás email-jével.
  - [ ] Idempotens: ugyanaz az email nem hoz létre duplikátumot.

### T-BBE-05 — `UserAccount` List + Patch (admin)
- **Cél:** `api-design.md` §6.1, §6.3.
- **Fájlok:**
  - `src/FamilyOs.Application/Users/ListUsersQuery.cs`.
  - `src/FamilyOs.Application/Users/PatchUserCommand.cs` (csak `role`,
    `isActive`).
  - `src/FamilyOs.Api/Endpoints/UsersModule.cs`.
- **AC:**
  - [ ] `?role=Admin` szűrés.
  - [ ] Adminok száma >=1 invariáns (egyetlen aktív admint nem lehet
        Adult-ra átállítani, ha ő az utolsó).
  - [ ] Audit log: `PermissionChange`.

### T-BBE-06 — `UserAccount` Delete (soft)
- **Cél:** session revoke + soft delete.
- **Fájlok:**
  - `src/FamilyOs.Application/Users/DeleteUserCommand.cs`.
- **AC:**
  - [ ] Soft delete, allowlist eltávolítva, RevokedSessions-be cookie hash.
  - [ ] Saját accountot törölni tilos (`DomainBusinessRuleException`).

### T-BBE-07 — `UserPreferences` entity és bevezetés
- **Cél:** B3 — csendes órák, email opt-in tárolása.
- **Fájlok:**
  - `src/FamilyOs.Domain/Entities/UserPreferences.cs` (külön tábla,
    egyszerűbb migráció).
  - `src/FamilyOs.Infrastructure/Persistence/Configurations/UserPreferencesConfiguration.cs`.
  - migráció: új `user_preferences` tábla.
- **AC:**
  - [ ] `(UserAccountId)` UNIQUE.
  - [ ] Mezők: `EmailEnabled`, `QuietHoursStart`, `QuietHoursEnd` (string
        `HH:mm`), `EscalationOptOut`.
  - [ ] Defaultok: `EmailEnabled = false`, `QuietHoursStart = '22:00'`,
        `QuietHoursEnd = '07:00'`, `EscalationOptOut = false`.

### T-BBE-08 — Preferenciák GET + PATCH
- **Cél:** `GET /api/v1/settings/preferences`, `PATCH /api/v1/auth/me/preferences`.
- **Fájlok:**
  - `src/FamilyOs.Application/Users/GetPreferencesQuery.cs`.
  - `src/FamilyOs.Application/Users/UpdatePreferencesCommand.cs` + Validator.
  - `src/FamilyOs.Api/Endpoints/AuthModule.cs` (kiegészítés).
- **AC:**
  - [ ] `HH:mm` regex validáció.
  - [ ] Email opt-in csak akkor mehet `true`-ra, ha az SMTP konfigurált
        (warning, de nem block).
  - [ ] 200 + frissített DTO.

### T-BBE-09 — Bootstrap admin auto-onboarding
- **Cél:** első login a `BootstrapAdmin` email-en automatikusan
  `FamilyMember`-t is létrehoz (`DisplayName = 'Admin'`, `Relation = Self`)
  és köti az UserAccount-hoz.
- **Fájlok:**
  - `src/FamilyOs.Infrastructure/Auth/BootstrapAdminInitializer.cs`.
- **AC:**
  - [ ] Üres adatbázis + első Google login a `BootstrapAdmin`-on →
        `FamilyMember` + `UserAccount(Role=Admin, IsActive=true)`.
  - [ ] Idempotens: második login nem hoz létre újat.

### T-BBE-10 — Audit log a security event-ekre
- **Cél:** minden auth-mozzanat audit-ben.
- **Fájlok:**
  - `src/FamilyOs.Infrastructure/Audit/AuditLoggerExtensions.cs` (helper:
    `LogLogin`, `LogLoginFailed`, `LogPermissionChange`).
  - integráció a `GoogleAuthHandler`-be és az invite/role-change flow-kba.
- **AC:**
  - [ ] `Login` + IP + UserAgent.
  - [ ] `LoginFailed` + email + ok (token expired, not in allowlist).
  - [ ] `PermissionChange` minden role + isActive változásra.

### T-BBE-11 — Integration tesztek a B epicre
- **Cél:** RBAC + invite + preferences végigtesztelve.
- **Fájlok:**
  - `tests/FamilyOs.Api.IntegrationTests/Family/FamilyMemberCrudTests.cs`
  - `tests/FamilyOs.Api.IntegrationTests/Users/InviteFlowTests.cs`
  - `tests/FamilyOs.Api.IntegrationTests/Users/PreferencesTests.cs`
  - `tests/FamilyOs.Api.IntegrationTests/Security/RowLevelAuthorizationTests.cs`
- **AC:**
  - [ ] Child user nem fér hozzá idegen private rekordhoz (403 + audit log).
  - [ ] Adult meghívása → meghívott Google login-ja után a megfelelő
        szerepkört kapja.
  - [ ] Csendes órák validáció hibás formátumra 400 + magyar üzenet.

---

## Megvalósítási sorrend

```
T-BBE-01 (auth service)
  → T-BBE-02 → 03                  (Family CRUD)
  → T-BBE-04 → 05 → 06             (User invite + admin)
  → T-BBE-07 → 08                  (Preferenciák)
  → T-BBE-09 → 10                  (Bootstrap + audit)
  → T-BBE-11                       (tesztek)
```

## Epic-DoD

- [ ] Admin képes meghívni Adult / Child felhasználót.
- [ ] Új user Google-lal belép, a megfelelő szerepkört kapja.
- [ ] Row-level RBAC működik — Child nem lát private rekordot.
- [ ] Preferenciák szerkeszthetők; csendes órák tárolva.
- [ ] Audit log tartalmazza az összes auth-mozzanatot.
- [ ] Integration tesztek zöldek.
- [ ] `code-reviewer` jóváhagyta a security-flow-t.

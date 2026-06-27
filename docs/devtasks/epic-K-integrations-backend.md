# Epic K — Beállítások + integrációk — Backend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` (FULL — különös figyelem §16 titkok)
> - `security-privacy.md` (FULL — különösen §6 encryption, §8 AI privacy, §10 külső integrációk, §11.2 backup)
> - ADR-0004 (Gmail API — FULL)
> - `api-design.md` §17 (Sources), §18 (AI providers), §21 (Settings)
> - `architecture.md` §3.4 (Infra.Ai), §5 (AI provider), §7 (storage backup)
> - `ai-pipeline.md` §1 (vezérlőelvek — privacy mód)
> - ADR-0003 (LAN-only — backup off-site policy)
> - `reminder-engine.md` §5.2 (SMTP konfig — átfedés Epic G-vel)
>
> **Story-k:** K1 (Gmail), K2 (AI provider config UI BE), K3 (Backup)
> **Fázis:** Fázis 12

---

## Áttekintés

K1: Gmail OAuth + szelektív beszívás (`family-os/import` címke alapján).
K2: AI provider konfigurációs endpointok (admin, de PrivacyMode védve).
K3: Backup és restore infrastruktúra + dokumentáció.

## Taskok

### T-KBE-01 — `Source` entity + CRUD
- **Cél:** integrációs forrás-rekord (Gmail account, Upload, …).
- **Fájlok:**
  - `src/FamilyOs.Domain/Entities/Source.cs`
  - `src/FamilyOs.Domain/Enums/SourceKind.cs`
  - Configuration.
  - migráció.
- **AC:**
  - [ ] `database-schema.md` §4.3 séma pontosan.
  - [ ] `ConfigJson` titkos részei (OAuth refresh token) titkosítva
        Data Protection-szel.

### T-KBE-02 — Google OAuth Gmail flow
- **Fájlok:**
  - `src/FamilyOs.Infrastructure.Ai/Email/GmailOAuthFlow.cs`
  - `src/FamilyOs.Api/Endpoints/SourcesModule.cs`
- **AC:**
  - [ ] `POST /api/v1/sources/gmail/connect` → redirect URL Google felé.
  - [ ] Callback endpoint: `GET /api/v1/sources/gmail/callback?code=...`
        → token csere → `Source` rekord insert.
  - [ ] Scope: `gmail.readonly`.
  - [ ] Refresh token mentve Data Protection-szel titkosítva.

### T-KBE-03 — `IEmailIngestionService` + `GmailIngestionService`
- **Cél:** szelektív beszívás `family-os/import` címkével.
- **Fájlok:**
  - `src/FamilyOs.Application/Abstractions/Email/IEmailIngestionService.cs`
  - `src/FamilyOs.Infrastructure.Ai/Email/GmailIngestionService.cs`
- **AC:**
  - [ ] `Google.Apis.Gmail.v1` használat.
  - [ ] Lekérdezés `q="label:family-os/import"`.
  - [ ] Page-token alapú lapozás.
  - [ ] Audit log: `ExternalApiCall` (provider, scope, status).

### T-KBE-04 — `EmailMessage` insert + Document(ek) létrehozás
- **Cél:** body + mellékletek dokumentumokká.
- **Fájlok:**
  - `src/FamilyOs.Application/Email/IngestEmailMessageCommand.cs`
- **AC:**
  - [ ] EmailMessage insert, UNIQUE `(SourceId, GmailMessageId)` dedup.
  - [ ] Body → Document `SourceType = Email`, `Origin = ImportedEmail`.
  - [ ] Mellékletek → Document(ek), `SourceEmailMessageId` linkelve.
  - [ ] Pipeline lefutása (`AiProcessingJob ExtractText`) ugyanúgy mint
        feltöltésnél.

### T-KBE-05 — `EmailIngestionPoller` BackgroundService
- **Cél:** 5 percenként minden aktív Gmail Source-on sync.
- **Fájlok:**
  - `src/FamilyOs.Workers/Services/EmailIngestionPoller.cs`
- **AC:**
  - [ ] Hangfire recurring job (`*/5 * * * *`).
  - [ ] `Source.LastSyncUtc` frissítve sikeres sync után.
  - [ ] Hibás source → exception caught, audit log, retry később.

### T-KBE-06 — Manuális sync trigger endpoint
- **Fájlok:**
  - `src/FamilyOs.Application/Sources/SyncSourceCommand.cs`
  - `src/FamilyOs.Api/Endpoints/SourcesModule.cs` kiegészítés.
- **AC:**
  - [ ] `POST /api/v1/sources/{id}/sync` → Hangfire enqueue.
  - [ ] 202 + job id.

### T-KBE-07 — Source disconnect endpoint
- **Fájlok:**
  - `src/FamilyOs.Application/Sources/DisconnectSourceCommand.cs`
- **AC:**
  - [ ] Soft delete a Source-on.
  - [ ] Google OAuth token revoke a Google felé.
  - [ ] Audit log: `Delete`.

### T-KBE-08 — AI provider config endpointok
- **Cél:** `api-design.md` §18.5–18.6.
- **Fájlok:**
  - `src/FamilyOs.Application/Admin/AiProviders/*.cs`
  - `src/FamilyOs.Api/Endpoints/AiProvidersAdminModule.cs` (ha még nincs
    az Epic J-ben).
- **AC:**
  - [ ] `GET /api/v1/ai-providers` lista (name, enabled, lastHealth, model).
  - [ ] `PATCH /api/v1/ai-providers/{name}` — csak `enabled`, `model`.
  - [ ] **PrivacyMode módosítási attempt 422** magyar üzenettel.

### T-KBE-09 — System settings endpoint
- **Cél:** `api-design.md` §21.
- **Fájlok:**
  - `src/FamilyOs.Application/Settings/GetSystemSettingsQuery.cs`
  - `src/FamilyOs.Application/Settings/PatchSystemSettingsCommand.cs`
  - `src/FamilyOs.Api/Endpoints/SettingsModule.cs`
- **AC:**
  - [ ] AI provider mode (read-only), SMTP konfig (titok nélkül),
        retention, csendes órák.
  - [ ] PATCH csak nem-érzékeny mezőkre.

### T-KBE-10 — Backup script
- **Cél:** napi `pg_dump` + `age` titkosítás + manifest.
- **Fájlok:**
  - `scripts/backup.sh`
  - `docker-compose.yml` `backup` service (cron-alapú).
  - `docs/DELIVERY.md` backup szakasz.
- **AC:**
  - [ ] `pg_dump -Fc | age -r <pubkey>` → `data/backups/db/YYYY-MM-DD.dump.age`.
  - [ ] Sha256 hash a `manifest.txt`-be (append-only).
  - [ ] 30 nap retention.

### T-KBE-11 — Restore script
- **Fájlok:**
  - `scripts/restore.sh`
- **AC:**
  - [ ] Hash ellenőrzés a manifest-ből.
  - [ ] `age --decrypt` → `pg_restore`.
  - [ ] Confirm prompt elindulás előtt.

### T-KBE-12 — Backup integration teszt
- **Fájlok:**
  - `tests/FamilyOs.Infrastructure.Tests/BackupRestoreTests.cs`
- **AC:**
  - [ ] Backup futás → restore staging-be → adat egyezik.

### T-KBE-13 — Gmail integration teszt (mocked)
- **Fájlok:**
  - `tests/FamilyOs.Infrastructure.Ai.Tests/GmailIngestionTests.cs`
- **AC:**
  - [ ] Mocked Gmail API response → EmailMessage + Document insertek.
  - [ ] Idempotens második sync (ugyanaz a GmailMessageId nem hoz létre duplikátumot).

---

## Megvalósítási sorrend

```
T-KBE-01 → 02 → 03 → 04 → 05 → 06 → 07    (Gmail integráció)
       → 08 → 09                            (AI provider config + settings)
       → 10 → 11 → 12                       (Backup + restore)
       → 13                                   (Gmail teszt)
```

## Epic-DoD

- [ ] Gmail csatlakoztatható; `family-os/import` címkével ellátott
      email-ek 5 percen belül feldolgozva.
- [ ] AI provider config admin endpointokon szerkeszthető (kivéve
      PrivacyMode).
- [ ] Napi backup futtatva, restore drill dokumentált.
- [ ] OAuth refresh token titkosítva tárolva.
- [ ] Integration tesztek zöldek.

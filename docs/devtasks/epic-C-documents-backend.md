# Epic C — Dokumentum-kezelés — Backend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` (FULL — baseline)
> - `api-design.md` (FULL §7 dokumentumok, §1 konvenciók, §23 példa upload)
> - `domain-model.md` §1.3 (Document), §1.4 (DocumentText), §1.17–1.19 (facets), §0 (közös)
> - `database-schema.md` §4.5 (document), §4.6 (document_text + v0.2 mezők), §4.18–4.20 (facets), §5.3 (bootstrap)
> - `security-privacy.md` §7 (Fájl-tárolás), §4 (Authorization), §9 (input validáció), §5 (audit)
> - `architecture.md` §7 (Tárolás), §11.1 (upload flow)
> - `ai-pipeline.md` §3.1 (Bemenet rögzítés)
>
> **Story-k:** C1, C2, C3 (BE), C4 (BE), C5 (BE)
> **Fázis:** Fázis 5 (upload + tárolás)

---

## Áttekintés

A Document a központi entitás — minden további (text, summary, chunk, facet,
tag, topic) ehhez kapcsolódik. Itt csak a **konténer** és a **CRUD** réteg
épül; a tényleges AI feldolgozás az Epic D-ben.

A dokumentum-státusz `Pending`-re kerül felhívóláncon — a worker az Epic D-ben
veszi át.

## Taskok

### T-CBE-01 — `Document` és `DocumentText` entitások
- **Cél:** EF Core mappolás a két entitásra + a v0.2 séma-frissítések.
- **Fájlok:**
  - `src/FamilyOs.Domain/Entities/Document.cs`
  - `src/FamilyOs.Domain/Entities/DocumentText.cs`
  - `src/FamilyOs.Domain/Enums/ProcessingStatus.cs`, `SourceType.cs`,
    `Origin.cs`, `ExtractionMethod.cs`
  - `src/FamilyOs.Infrastructure/Persistence/Configurations/DocumentConfiguration.cs`
  - `src/FamilyOs.Infrastructure/Persistence/Configurations/DocumentTextConfiguration.cs`
  - migráció: `document`, `document_text` táblák.
- **AC:**
  - [ ] Minden mező a `database-schema.md` §4.5–4.6 v0.2 szerint, ide
        értve `original_content` és `is_manually_edited` mezőket.
  - [ ] HasQueryFilter `DeletedUtc == null` a Document-en.
  - [ ] Sha256 UNIQUE partial index.
  - [ ] Generated `tsv` mező a `hungarian_unaccent` configgal.

### T-CBE-02 — Facet entitások: Warranty, MedicalRecord, FinancialRecord
- **Cél:** három 1:1-es entitás a Document-re.
- **Fájlok:**
  - `src/FamilyOs.Domain/Entities/Warranty.cs`
  - `src/FamilyOs.Domain/Entities/MedicalRecord.cs`
  - `src/FamilyOs.Domain/Entities/FinancialRecord.cs`
  - `src/FamilyOs.Domain/Enums/MedicalRecordType.cs`,
    `FinancialRecordType.cs`, `RecurrencePeriod.cs`
  - Configurations.
  - migráció.
- **AC:**
  - [ ] `(DocumentId)` UNIQUE mindhárom táblán.
  - [ ] MedicalRecord: `FamilyMemberId` kötelező FK, `IsPrivate` default true.
  - [ ] CHECK constraint-ek a `database-schema.md` §4.18–4.20 szerint.

### T-CBE-03 — `IDocumentStorage` interfész + `LocalFilesystemDocumentStorage`
- **Cél:** fájltár absztrakció és lokális implementáció.
- **Fájlok:**
  - `src/FamilyOs.Application/Abstractions/Storage/IDocumentStorage.cs`
  - `src/FamilyOs.Infrastructure/Storage/LocalFilesystemDocumentStorage.cs`
  - `src/FamilyOs.Infrastructure/Storage/DocumentStoragePathBuilder.cs`
- **AC:**
  - [ ] Útvonal: `${FAMILYOS_DATA_DIR}/documents/<év>/<hónap>/<guid>.<ext>`.
  - [ ] `SaveAsync` visszaadja a relatív path-et.
  - [ ] Path traversal védelem: validáció `..` ellen, prefix-check.
  - [ ] Unit teszt: malicious path attempt → kivétel.

### T-CBE-04 — `MimeDetector` magic byte alapon
- **Cél:** a feltöltött fájl MIME-jét a tényleges tartalomból állapítja meg.
- **Fájlok:**
  - `src/FamilyOs.Infrastructure/Common/MimeDetector.cs`
  - `src/FamilyOs.Application/Abstractions/Common/IMimeDetector.cs`
- **AC:**
  - [ ] Whitelist: PDF, JPEG, PNG, HEIC, TXT, DOCX (`security-privacy.md`
        §7.3).
  - [ ] Magic byte teszt mintafájlokra.
  - [ ] `.exe`-szerű fájl → kivétel → 415 ProblemDetails.

### T-CBE-05 — Sha256 dedup helper
- **Cél:** stream-alapon Sha256 számolás + DB lookup.
- **Fájlok:**
  - `src/FamilyOs.Infrastructure/Common/Sha256StreamHasher.cs`
  - `src/FamilyOs.Application/Documents/Common/IDuplicateDocumentChecker.cs`
  - `src/FamilyOs.Infrastructure/Documents/DuplicateDocumentChecker.cs`
- **AC:**
  - [ ] Stream egyszer fogyasztva (no double-read).
  - [ ] Duplikátum esetén a meglévő `DocumentDto` visszaadva 409-cel.

### T-CBE-06 — `Idempotency-Key` middleware
- **Cél:** `api-design.md` §1.9 — POST-okhoz dedup 24 órán át.
- **Fájlok:**
  - `src/FamilyOs.Api/Middleware/IdempotencyMiddleware.cs`
  - `src/FamilyOs.Infrastructure/Idempotency/IdempotencyStore.cs`
    (memóriában MVP-ben, jövőben Postgres tábla).
- **AC:**
  - [ ] Ugyanaz a key + user → cached response.
  - [ ] 24 óra TTL.
  - [ ] Header hiánya 400 a kötelező endpointokon.

### T-CBE-07 — `UploadDocumentCommand` + Handler
- **Cél:** a fő upload use case.
- **Fájlok:**
  - `src/FamilyOs.Application/Documents/UploadDocument/UploadDocumentCommand.cs`
  - `src/FamilyOs.Application/Documents/UploadDocument/UploadDocumentHandler.cs`
  - `src/FamilyOs.Application/Documents/UploadDocument/UploadDocumentValidator.cs`
  - `src/FamilyOs.Application/Documents/Dtos/DocumentDto.cs`
- **AC:**
  - [ ] Tranzakció: Sha256 check → storage save → Document insert →
        `AiProcessingJob(ExtractText)` insert → commit.
  - [ ] Hiba esetén storage rollback (mentett fájl törlés).
  - [ ] `processing_status = Pending`, `origin = Manual`.
  - [ ] Audit log: `Create`.

### T-CBE-08 — Upload endpoint + multipart support
- **Cél:** `POST /api/v1/documents` (multipart/form-data).
- **Fájlok:**
  - `src/FamilyOs.Api/Endpoints/DocumentsModule.cs`
  - `src/FamilyOs.Api/Endpoints/Forms/UploadDocumentForm.cs` (binding +
    `ToCommand()`).
- **AC:**
  - [ ] Multipart parts: `file`, `title?`, `relatedFamilyMemberId?`,
        `isPrivate?`, `documentDate?`.
  - [ ] `Idempotency-Key` header kötelező.
  - [ ] 201 + `Location` header + ETag.
  - [ ] Kestrel `MaxRequestBodySize = 60MB`.

### T-CBE-09 — `ListDocumentsQuery` + handler
- **Cél:** szűrhető listázás + RBAC.
- **Fájlok:**
  - `src/FamilyOs.Application/Documents/ListDocuments/ListDocumentsQuery.cs`
  - Handler + Validator.
  - `src/FamilyOs.Application/Documents/Dtos/DocumentListItemDto.cs` (lite).
- **AC:**
  - [ ] Query paraméterek: `topicSlug`, `tagId`, `relatedFamilyMemberId`,
        `from`, `to`, `sourceType`, `processingStatus`.
  - [ ] RBAC: `IsPrivate` szűrése a current user-re (lásd Epic B
        T-BBE-01 auth service).
  - [ ] Pagination (page+pageSize, default 50, max 100).
  - [ ] `X-Total-Count` válasz fejléc.

### T-CBE-10 — `GetDocumentDetailQuery`
- **Cél:** eager loading: summary, tags, topics, facet.
- **Fájlok:**
  - `src/FamilyOs.Application/Documents/GetDocumentDetail/GetDocumentDetailQuery.cs`
  - `src/FamilyOs.Application/Documents/Dtos/DocumentDetailDto.cs`
  - `src/FamilyOs.Application/Documents/Dtos/SuggestionsBlockDto.cs`
- **AC:**
  - [ ] 200 + teljes DTO.
  - [ ] 404 ha nincs vagy soft-deleted.
  - [ ] 403 ha nem látható (RBAC).
  - [ ] Audit log: `FileAccess` opcionálisan (`Read`-szintű — MVP-ben
        csak a download-on logoljuk).

### T-CBE-11 — Document letöltés és streaming
- **Cél:** `GET /api/v1/documents/{id}/content`.
- **Fájlok:**
  - kiegészítés `DocumentsModule.cs`-ben.
  - `src/FamilyOs.Application/Documents/DownloadDocument/DownloadDocumentQuery.cs`.
- **AC:**
  - [ ] `Content-Disposition: inline` default, `?download=true` esetén
        attachment.
  - [ ] Streaming (no full-load memóriába).
  - [ ] Audit log: `FileAccess`.

### T-CBE-12 — `GetDocumentTextQuery`
- **Cél:** szöveg-tartalom külön endpoint.
- **Fájlok:**
  - `src/FamilyOs.Application/Documents/GetDocumentText/GetDocumentTextQuery.cs`
  - DTO: `DocumentTextDto`.
- **AC:**
  - [ ] 200 + content + extraction_method + language + char_count.
  - [ ] 404 ha még nincs feldolgozva.

### T-CBE-13 — `UpdateDocumentTextCommand` (manual correction — C4)
- **Cél:** `PATCH /api/v1/documents/{id}/text` — `original_content` →
  régi content, `content` → new, `is_manually_edited = true`.
- **Fájlok:**
  - `src/FamilyOs.Application/Documents/UpdateDocumentText/UpdateDocumentTextCommand.cs`
- **AC:**
  - [ ] Egyszerű domain logika: ha `is_manually_edited == false`, akkor
        `original_content := content` (egyszer).
  - [ ] Új `AiProcessingJob`-ok aszinkron: `Embed`, `Summarize` (a régi
        summary `IsCurrent = false`-ra).
  - [ ] Audit log: `Update`.

### T-CBE-14 — `PatchDocumentCommand` (metaadat)
- **Cél:** `title`, `documentDate`, `relatedFamilyMemberId`, `isPrivate`.
- **Fájlok:**
  - `src/FamilyOs.Application/Documents/PatchDocument/PatchDocumentCommand.cs`
- **AC:**
  - [ ] `If-Match` kötelező; 409 ütközés.
  - [ ] Audit log: `Update` + diff JSON `details_json`-ben.

### T-CBE-15 — `DeleteDocumentCommand` (soft + hard)
- **Cél:** `DELETE /api/v1/documents/{id}?hard=true`.
- **Fájlok:**
  - `src/FamilyOs.Application/Documents/DeleteDocument/DeleteDocumentCommand.cs`
- **AC:**
  - [ ] Soft default; hard delete csak `RequireAdmin` + `?hard=true` query.
  - [ ] Hard delete fizikai fájl törlés is.
  - [ ] Audit log: `Delete`.

### T-CBE-16 — `ReprocessDocumentCommand` (C5)
- **Cél:** `POST /api/v1/documents/{id}/reprocess`.
- **Fájlok:**
  - `src/FamilyOs.Application/Documents/ReprocessDocument/ReprocessDocumentCommand.cs`
- **AC:**
  - [ ] Body: `{ jobs: ["Summarize", "ExtractDeadlines", ...] }`.
  - [ ] Új `AiProcessingJob`-okat queue-zál; nem törli a meglévő facet
        rekordokat (idempotens upsert).
  - [ ] 202 + queued job IDs.

### T-CBE-17 — Tag és Topic kapcsoló endpointok
- **Cél:** `POST /tags`, `DELETE /tags/{tagId}`, ugyanaz topic-ra.
- **Fájlok:**
  - `src/FamilyOs.Application/Documents/Linking/*.cs`
- **AC:**
  - [ ] Idempotens upsert (`ON CONFLICT DO NOTHING`).
  - [ ] Az `Origin` a current user kontextusából (`Manual`-ra állítva, ha
        a user kapcsolja).

### T-CBE-18 — Facet PATCH endpointok (Warranty, Medical, Financial)
- **Cél:** `PATCH /api/v1/documents/{id}/warranty` stb.
- **Fájlok:**
  - `src/FamilyOs.Application/Documents/Facets/*.cs`
  - Külön `PatchWarrantyCommand`, `PatchMedicalRecordCommand`,
    `PatchFinancialRecordCommand`.
- **AC:**
  - [ ] Idempotens upsert.
  - [ ] MedicalRecord PATCH csak ha a current user a `family_member`
        adatalanya vagy admin.

### T-CBE-19 — Integration tesztek a C epicre
- **Cél:** upload, dedup, list, download, patch, delete.
- **Fájlok:**
  - `tests/FamilyOs.Api.IntegrationTests/Documents/UploadDocumentTests.cs`
  - `tests/FamilyOs.Api.IntegrationTests/Documents/ListDocumentsTests.cs`
  - `tests/FamilyOs.Api.IntegrationTests/Documents/PathTraversalSecurityTests.cs`
  - `tests/FamilyOs.Api.IntegrationTests/Documents/IdempotencyTests.cs`
- **AC:**
  - [ ] Valódi PDF feltöltés → 201 → fájl a volume-on → DB rekord.
  - [ ] Ismételt upload → 409 + meglévő DTO.
  - [ ] Path traversal kísérlet (`../../etc/passwd`) → 400 + audit log.
  - [ ] Idempotency-Key: ugyanazzal kétszer → ugyanaz a válasz.

---

## Megvalósítási sorrend

```
T-CBE-01 → 02                       (entitások)
       → 03 → 04 → 05 → 06          (storage + dedup + idempotency)
       → 07 → 08                    (upload)
       → 09 → 10 → 11 → 12          (read)
       → 13 → 14 → 15 → 16          (write)
       → 17 → 18                    (linking + facets)
       → 19                          (tesztek)
```

## Epic-DoD

- [ ] Dokumentum feltölthető, lemezen és DB-ben tárolt.
- [ ] Sha256 dedup működik.
- [ ] Path traversal védelem teszttel igazolt.
- [ ] Lista szűrhető, RBAC betartva.
- [ ] Részletes oldal és letöltés működik.
- [ ] Manuális szövegkorrekció `original_content`-be menti az eredetit.
- [ ] Reprocess új AI jobokat enqueue (az Epic D után válik teljes egészében hasznossá).
- [ ] Integration tesztek zöldek.

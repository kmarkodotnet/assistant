# Epic H — Notes — Backend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` (FULL — baseline)
> - `domain-model.md` §1.9 (Note)
> - `database-schema.md` §4.12 (note + note_chunk + join táblák)
> - `api-design.md` §8 (Notes)
> - `ai-pipeline.md` §3.9 (embedding — analóg dokumentummal)
> - `search-strategy.md` §2.2 (FTS), §2.3 (vektor) — note_chunk is része
> - `security-privacy.md` §4 (IsPrivate Note-on), §9.2 (XSS, markdown sanitize)
> - `architecture.md` §3 (réteg-szerep)
>
> **Story-k:** H1, H2
> **Fázis:** Fázis 11

---

## Áttekintés

A Note egy dokumentum-jellegű, de file nélküli entitás. Markdown body,
tag/topic kapcsolódás, ugyanaz a chunkolás és embedding pipeline mint
a dokumentumoknál.

## Taskok

### T-HBE-01 — `Note` entity + chunk-tábla
- **Fájlok:**
  - `src/FamilyOs.Domain/Entities/Note.cs`
  - `src/FamilyOs.Domain/Entities/NoteChunk.cs`
  - Configurations.
  - migráció: `note`, `note_chunk`, `note_tag`, `note_topic`.
- **AC:**
  - [ ] `database-schema.md` §4.12 séma pontosan.
  - [ ] `tsv` generált oszlop magyar configgal.
  - [ ] HNSW index a `note_chunk.embedding`-en.

### T-HBE-02 — `CreateNoteCommand`
- **Fájlok:**
  - `src/FamilyOs.Application/Notes/CreateNote/CreateNoteCommand.cs` +
    Handler + Validator.
- **AC:**
  - [ ] Body: `title`, `body` (markdown), `relatedFamilyMemberId?`,
        `isPrivate?`, `tagIds[]?`, `topicIds[]?`.
  - [ ] Insert → `AiProcessingJob(Embed)` aszinkron.
  - [ ] Audit log: `Create`.

### T-HBE-03 — `ListNotesQuery` + `GetNoteQuery`
- **Fájlok:**
  - `src/FamilyOs.Application/Notes/ListNotesQuery.cs`
  - `src/FamilyOs.Application/Notes/GetNoteQuery.cs`
- **AC:**
  - [ ] Szűrés: `?relatedFamilyMemberId=`, `?tagId=`, `?topicSlug=`.
  - [ ] RBAC szűrés (Private csak tulajdonosnak).
  - [ ] `?includeBody=false` opcionális (listához rövidítés).

### T-HBE-04 — `PatchNoteCommand` + `DeleteNoteCommand`
- **Fájlok:**
  - `src/FamilyOs.Application/Notes/PatchNoteCommand.cs`
  - `src/FamilyOs.Application/Notes/DeleteNoteCommand.cs`
- **AC:**
  - [ ] `If-Match` PATCH-nál.
  - [ ] Body változás → új embedding job (re-chunk + re-embed).
  - [ ] Soft delete.

### T-HBE-05 — Note Tag/Topic kapcsoló endpointok (H2)
- **Fájlok:**
  - `src/FamilyOs.Application/Notes/Linking/*.cs`
- **AC:**
  - [ ] Analóg a Documents tag/topic endpointokkal.

### T-HBE-06 — `POST /api/v1/notes/search`
- **Fájlok:**
  - kiegészítés a `SearchModule`-ban (Notes is forrás).
- **AC:**
  - [ ] FTS és vector search a notes-ra is.

### T-HBE-07 — Embed pipeline kiterjesztése Note-ra
- **Cél:** D10 worker felismeri a `JobTargetType = Note` esetet.
- **Fájlok:**
  - kiegészítés `EmbedJobRunner`-ben.
- **AC:**
  - [ ] Note body → chunkolás → `note_chunk` insert.
  - [ ] Idempotens upsert.

### T-HBE-08 — Notes endpoint module
- **Fájlok:**
  - `src/FamilyOs.Api/Endpoints/NotesModule.cs`
- **AC:**
  - [ ] `api-design.md` §8 minden endpoint.

### T-HBE-09 — Markdown sanitize backend-szinten
- **Cél:** ne tudjon a UI-on script-injection-t okozni a body.
- **Fájlok:**
  - `src/FamilyOs.Application/Notes/Common/MarkdownSanitizer.cs`
- **AC:**
  - [ ] `HtmlSanitizer` csomag.
  - [ ] Render-eredmény a frontendnek külön endpoint (GET `/notes/{id}/rendered`).
  - [ ] Vagy: tárolt body raw markdown, kliens render + sanitize (lásd FE
        T-HFE-).

### T-HBE-10 — Integration tesztek
- **Fájlok:**
  - `tests/FamilyOs.Api.IntegrationTests/Notes/NoteCrudTests.cs`
  - `tests/FamilyOs.Api.IntegrationTests/Notes/NoteSearchTests.cs`
- **AC:**
  - [ ] CRUD path-ek.
  - [ ] FTS visszahozza a magyar szövegben elrejtett kulcsszót.
  - [ ] Note vektor-keresés releváns chunkot ad.

---

## Megvalósítási sorrend

```
T-HBE-01 → 02 → 03 → 04 → 05         (CRUD)
       → 06 → 07                     (search + embed)
       → 08                           (endpoint module)
       → 09 → 10                     (sanitize + tesztek)
```

## Epic-DoD

- [ ] Note CRUD működik.
- [ ] Tag/Topic kapcsolható.
- [ ] Note chunkolás + embedding aszinkron.
- [ ] Search visszahozza a Note-okat is.
- [ ] Markdown sanitize aktív.

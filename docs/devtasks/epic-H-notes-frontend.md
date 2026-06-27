# Epic H — Notes — Frontend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` §9, §10
> - `frontend-structure.md` §8.2 (analóg dokumentumokkal)
> - `api-design.md` §8 (Notes)
> - `security-privacy.md` §9.2 (XSS, markdown sanitize a kliensen is)
>
> **Story-k:** H1 (FE), H2 (FE)
> **Fázis:** Fázis 11

---

## Áttekintés

Notes feature: lista oldal, szerkesztő (markdown editor + preview), tag /
topic kapcsolás. Egyszerű UI; a magneten a kereshetőség és a strukturáltság
a háttérben (embedding).

## Taskok

### T-HFE-01 — Notes API client + DTOs
- **Fájlok:**
  - `frontend/src/app/features/notes/services/notes.api.ts`
  - `frontend/src/app/features/notes/models/note.dto.ts`
- **AC:**
  - [ ] CRUD + search.

### T-HFE-02 — `NotesFacade`
- **Fájlok:**
  - `frontend/src/app/features/notes/services/notes.facade.ts`
- **AC:**
  - [ ] State: `items`, `filters`, `loading`, `currentNote`.
  - [ ] `load`, `loadDetail`, `create`, `update`, `delete`.

### T-HFE-03 — Notes lista oldal
- **Fájlok:**
  - `frontend/src/app/features/notes/pages/notes-list.page.ts`
- **AC:**
  - [ ] Kártya-grid: cím + body preview (első 200 char).
  - [ ] „Új jegyzet" gomb.
  - [ ] Filter panel: family member, tag, topic.

### T-HFE-04 — Note részletes oldal
- **Fájlok:**
  - `frontend/src/app/features/notes/pages/note-detail.page.ts`
- **AC:**
  - [ ] Fejléc: cím, IsPrivate toggle, családtag.
  - [ ] Markdown body render (`MarkdownPipe`).
  - [ ] Tag/Topic chip-ek + kapcsoló.
  - [ ] „Szerkesztés" gomb.

### T-HFE-05 — Note szerkesztő oldal
- **Fájlok:**
  - `frontend/src/app/features/notes/pages/note-edit.page.ts`
  - `frontend/src/app/features/notes/components/markdown-editor.component.ts`
- **AC:**
  - [ ] Reactive form.
  - [ ] Markdown editor (egyszerű textarea + preview split view).
  - [ ] Auto-save vagy explicit Mentés gomb.
  - [ ] `hasUnsavedChangesGuard` integráció.

### T-HFE-06 — Markdown render pipe sanitize-zal
- **Fájlok:**
  - `frontend/src/app/shared/pipes/markdown.pipe.ts`
- **AC:**
  - [ ] `marked` library + DOMPurify sanitize.
  - [ ] Inline `<script>` és `<iframe>` tiltva.
  - [ ] Külső link `target="_blank" rel="noopener"`.

### T-HFE-07 — Note Tag/Topic kapcsoló
- **Fájlok:**
  - `frontend/src/app/features/notes/components/note-tags-editor.component.ts`
- **AC:**
  - [ ] Tag multiselect (autocomplete a Tags API-ből).
  - [ ] Topic tree-select.

### T-HFE-08 — Tesztek
- **Fájlok:**
  - `frontend/src/app/features/notes/pages/note-edit.page.spec.ts`
  - `frontend/src/app/shared/pipes/markdown.pipe.spec.ts`
- **AC:**
  - [ ] Editor `dirty` állapot → guard megjelenít megerősítést.
  - [ ] XSS attempt (`<script>alert(1)</script>`) sanitized.

---

## Megvalósítási sorrend

```
T-HFE-01 → 02 → 03 → 04 → 05 → 06 → 07 → 08
```

## Epic-DoD

- [ ] Note CRUD UI működik.
- [ ] Markdown editor + preview.
- [ ] XSS sanitize teszttel ellenőrizve.
- [ ] Tag/topic kapcsolható.

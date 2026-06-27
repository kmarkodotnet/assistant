# Epic I — Tag + Topic — Frontend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` §9, §10
> - `frontend-structure.md` §8.7 (Topics oldal), §9 (Tag multiselect shared)
> - `api-design.md` §9 (Tags), §10 (Topics)
>
> **Story-k:** I1 (FE), I2 (FE)
> **Fázis:** Fázis 11

---

## Áttekintés

- **Tag multiselect** — újrahasznosítható komponens autocomplete-tel.
- **Topic-fa adminisztrációs oldal** — tree-view drag-to-reorder-rel.
- **Topic mini-dashboard** — topic-click → kapcsolódó tartalom listája.

## Taskok

### T-IFE-01 — Tags API client + DTOs
- **Fájlok:**
  - `frontend/src/app/features/tags-control/services/tags.api.ts`
  - `frontend/src/app/features/tags-control/models/tag.dto.ts`
- **AC:**
  - [ ] `?q=` debounce 300 ms.

### T-IFE-02 — Topics API client + DTOs
- **Fájlok:**
  - `frontend/src/app/features/topics/services/topics.api.ts`
  - `frontend/src/app/features/topics/models/topic.dto.ts`,
    `topic-tree.dto.ts`
- **AC:**
  - [ ] Tree fetch + flat fetch.

### T-IFE-03 — `tag-multiselect` shared komponens
- **Fájlok:**
  - `frontend/src/app/shared/forms/tag-multiselect.component.ts`
- **AC:**
  - [ ] Autocomplete `tags.api.list({q})`.
  - [ ] Új tag létrehozás Enter-rel.
  - [ ] Chip-kijelölés Backspace-szel.
  - [ ] `input.required<Guid[]>()` + `output()` az ID listára.

### T-IFE-04 — `topic-tree-select` shared komponens
- **Fájlok:**
  - `frontend/src/app/shared/forms/topic-tree-select.component.ts`
- **AC:**
  - [ ] Nested checkbox tree.
  - [ ] Több topic egyszerre kiválasztható.
  - [ ] Mobile: bottom-sheet variánsban.

### T-IFE-05 — Topics oldal (admin tree-view)
- **Fájlok:**
  - `frontend/src/app/features/topics/pages/topics.page.ts`
  - `frontend/src/app/features/topics/components/topic-tree-view.component.ts`
- **AC:**
  - [ ] Hierarchikus tree expand/collapse.
  - [ ] „Új altopic" + per-node kontextus menü (szerkesztés / törlés).
  - [ ] Sortolás `sortOrder` szerint, drag-to-reorder MVP-ben opcionális.

### T-IFE-06 — Topic szerkesztő dialog
- **Fájlok:**
  - `frontend/src/app/features/topics/components/topic-edit.dialog.ts`
- **AC:**
  - [ ] Name, slug (auto-suggest a name-ből), parentTopicId select, icon
        (material name).
  - [ ] 422 (mélység / kör) magyar hibaüzenetként mező-szinten.

### T-IFE-07 — Topic mini-dashboard
- **Cél:** topic-click → kapcsolódó documents + notes + tasks lista.
- **Fájlok:**
  - `frontend/src/app/features/topics/pages/topic-detail.page.ts`
- **AC:**
  - [ ] Filter `?topicSlug=` átadása a lista-oldalakra.
  - [ ] Statisztika: tartalom-szám / kategória.

### T-IFE-08 — Tag-multiselect integráció
- **Cél:** a Documents detail, Notes edit, Tasks edit oldalakra beépítés.
- **Fájlok:**
  - kiegészítések a megfelelő `*-edit.dialog.ts` fájlokban.
- **AC:**
  - [ ] Egységes UX minden helyen.

### T-IFE-09 — Tesztek
- **Fájlok:**
  - `frontend/src/app/shared/forms/tag-multiselect.component.spec.ts`
  - `frontend/src/app/features/topics/components/topic-tree-view.component.spec.ts`
- **AC:**
  - [ ] Tag autocomplete debounce-tetten.
  - [ ] Tree expand/collapse jól.

---

## Megvalósítási sorrend

```
T-IFE-01 → 02                       (data)
       → 03 → 04                    (shared form-controls)
       → 05 → 06 → 07                (Topics oldal)
       → 08                           (integráció)
       → 09                           (tesztek)
```

## Epic-DoD

- [ ] Tag multiselect működik, autocomplete-tel.
- [ ] Topic-fa admin tree-view, CRUD.
- [ ] Topic mini-dashboard.
- [ ] Mindkét shared komponens más feature-ökön újrahasznosítva.

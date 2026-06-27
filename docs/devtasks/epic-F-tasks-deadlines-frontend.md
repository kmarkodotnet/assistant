# Epic F — Task + Deadline — Frontend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` §9, §10
> - `frontend-structure.md` §8.4 (Tasks kanban), §8.5 (Deadlines naptár), §8.9 (Suggestions inbox), §9 (SuggestionBlock shared)
> - `api-design.md` §11 (Tasks), §12 (Deadlines), §15 (Suggestions batch)
> - `domain-model.md` §1.10, §1.11 (mit mutatunk)
> - `reminder-engine.md` §3.10 (UI: deadline → default reminderek visualizálva)
>
> **Story-k:** F1 (FE), F2 (FE), F3 (FE)
> **Fázis:** Fázis 10

---

## Áttekintés

- **Tasks oldal** — kanban vagy lista nézet, family-member szerinti szűrés,
  AI-suggested kiemelve.
- **Deadlines oldal** — naptár + lista, kategória színkód.
- **Suggestions inbox** — egyetlen helyen az összes nyitott javaslat
  batch-jóváhagyással.

## Taskok

### T-FFE-01 — Tasks API client + DTOs
- **Fájlok:**
  - `frontend/src/app/features/tasks/services/tasks.api.ts`
  - `frontend/src/app/features/tasks/models/task.dto.ts`
- **AC:**
  - [ ] CRUD + state action endpointok.

### T-FFE-02 — Deadlines API client + DTOs
- **Fájlok:**
  - `frontend/src/app/features/deadlines/services/deadlines.api.ts`
  - `frontend/src/app/features/deadlines/models/deadline.dto.ts`
- **AC:**
  - [ ] CRUD + state action endpointok.

### T-FFE-03 — `TasksFacade`
- **Fájlok:**
  - `frontend/src/app/features/tasks/services/tasks.facade.ts`
- **AC:**
  - [ ] `kanbanGrouped` computed signal: Suggested / Open / InProgress / Done.
  - [ ] `assignedToFilter`, `priorityFilter` signal-ek.

### T-FFE-04 — `DeadlinesFacade`
- **Fájlok:**
  - `frontend/src/app/features/deadlines/services/deadlines.facade.ts`
- **AC:**
  - [ ] `calendarItems` computed signal: dátum → deadline lista.
  - [ ] `dateRange`, `categoryFilter` signal-ek.

### T-FFE-05 — Tasks lista + kanban toggle
- **Fájlok:**
  - `frontend/src/app/features/tasks/pages/tasks.page.ts`
  - `frontend/src/app/features/tasks/components/tasks-kanban.component.ts`
  - `frontend/src/app/features/tasks/components/tasks-list.component.ts`
- **AC:**
  - [ ] Toggle gomb (kanban / lista).
  - [ ] Kanban oszlopok: 4 állapot.
  - [ ] Suggested oszlop sárga háttér + batch Approve gomb.

### T-FFE-06 — `task-card` komponens
- **Fájlok:**
  - `frontend/src/app/features/tasks/components/task-card.component.ts`
- **AC:**
  - [ ] Priority indikátor (szín).
  - [ ] Felelős családtag avatar.
  - [ ] Due date relative-format (`huRelativeDate`).
  - [ ] Akció-gombok: Indítom / Kész / Mégse (állapot-alapján).

### T-FFE-07 — Task szerkesztő dialog
- **Fájlok:**
  - `frontend/src/app/features/tasks/components/task-edit.dialog.ts`
- **AC:**
  - [ ] Form: title, description (markdown), priority, dueDate,
        assignedTo (FamilyMember select).
  - [ ] Reactive form + FluentValidation hibák.

### T-FFE-08 — Deadlines naptár nézet
- **Fájlok:**
  - `frontend/src/app/features/deadlines/components/deadlines-calendar.component.ts`
- **AC:**
  - [ ] Hónap-nézet (egyszerű grid), nyíl-gombok hónap-váltáshoz.
  - [ ] Esemény-jelölő (category szerinti szín).
  - [ ] Klikk a dátumra → lista popover.

### T-FFE-09 — Deadlines lista nézet
- **Fájlok:**
  - `frontend/src/app/features/deadlines/components/deadlines-list.component.ts`
  - `frontend/src/app/features/deadlines/components/deadline-card.component.ts`
- **AC:**
  - [ ] Csoportosítás: Ezen a héten / Hónapban / Később.
  - [ ] Kibontva: kapcsolódó Reminder-ek visualizációja (offset chip-ek).

### T-FFE-10 — Deadline szerkesztő dialog
- **Fájlok:**
  - `frontend/src/app/features/deadlines/components/deadline-edit.dialog.ts`
- **AC:**
  - [ ] Category select kategória-ikonokkal.
  - [ ] Responsible FamilyMember select.

### T-FFE-11 — Suggestions inbox oldal
- **Fájlok:**
  - `frontend/src/app/features/suggestions/pages/suggestions.page.ts`
  - `frontend/src/app/features/suggestions/services/suggestions.facade.ts`
- **AC:**
  - [ ] 5 szekció: Tasks, Deadlines, Tags, Topics, Facets.
  - [ ] Csoport-szintű „Elfogad mindet" gomb.
  - [ ] Per-item checkbox + batch action a fejlécben.

### T-FFE-12 — Batch approval flow
- **Fájlok:**
  - `frontend/src/app/features/suggestions/services/suggestions.facade.ts`
  - `frontend/src/app/features/suggestions/components/batch-action-bar.component.ts`
- **AC:**
  - [ ] Több kategóriából összegyűjtött suggestion-ök egyetlen POST-on.
  - [ ] Success toast: „5 elfogadva, 2 elvetve".
  - [ ] Partial failure toast: hibák felsorolva.

### T-FFE-13 — `SuggestionBlock` integráció Documents-en
- **Cél:** újrahasznosítható komponens (Epic C T-CFE-12) használata
  Document-detail oldalon a kapcsolódó suggestion-ökhöz.
- **Fájlok:**
  - kiegészítés `document-detail.page.ts`-ben.
- **AC:**
  - [ ] Kapcsolódó task-ok és deadline-ok suggestion-ként jelennek meg.
  - [ ] Approve közvetlenül a Document-oldalon működik.

### T-FFE-14 — Tesztek
- **Fájlok:**
  - `frontend/src/app/features/tasks/pages/tasks.page.spec.ts`
  - `frontend/src/app/features/deadlines/components/deadlines-calendar.component.spec.ts`
  - `frontend/src/app/features/suggestions/pages/suggestions.page.spec.ts`
  - `frontend/e2e/suggestions/batch-approval.spec.ts`
- **AC:**
  - [ ] Suggested task drag (mock backend) → Open.
  - [ ] Batch approval E2E: 3 javaslat kijelölve → POST → mind az 5 panel
        frissül.

---

## Megvalósítási sorrend

```
T-FFE-01 → 02 → 03 → 04             (data)
       → 05 → 06 → 07                (Tasks UI)
       → 08 → 09 → 10                (Deadlines UI)
       → 11 → 12 → 13                (Suggestions inbox)
       → 14                           (tesztek)
```

## Epic-DoD

- [ ] Tasks és Deadlines oldal működik (lista + kanban / naptár).
- [ ] Suggestion inbox elérhető, batch action működik.
- [ ] AI-javaslatok vizuálisan elkülönülnek (sárga sáv).
- [ ] Magyar UI; relatív dátumformázás.
- [ ] Tesztek zöldek.

# Epic F — Task + Deadline — Backend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` (FULL — baseline)
> - `domain-model.md` (FULL — különösen §1.10 Task, §1.11 Deadline, §0 Origin/Approval, §4 Task vs Deadline indoklás)
> - `api-design.md` §11 (Tasks), §12 (Deadlines), §15 (Suggestions batch)
> - `database-schema.md` §4.13, §4.14, §2 (task/deadline enumok)
> - `reminder-engine.md` §3.10 (default reminder policy minden deadline-hoz)
> - `security-privacy.md` §4 (RBAC), §5 (audit Approve/Reject)
> - `ai-pipeline.md` §5 (Suggestion → Approved állapotgép)
> - `architecture.md` §11.1 (suggestion-ök a feldolgozás végén)
>
> **Story-k:** F1, F2, F3
> **Fázis:** Fázis 8 (D-Tartalom mergelése után) → Fázis 10 elején

---

## Áttekintés

Két központi entitás: **Task** (teendő, családtaghoz rendelve) és
**Deadline** (idő-kötött kötelezettség, esemény-jellegű). A pipeline AI
által javasolt mindkettő `Origin=AiSuggested`-ből indul, jóváhagyásra
vár. Az Epic biztosítja a teljes CRUD + állapotgép + batch-jóváhagyás
flow-t.

## Taskok

### T-FBE-01 — `Task` és `Deadline` entitások
- **Cél:** EF Core mappolás.
- **Fájlok:**
  - `src/FamilyOs.Domain/Entities/Task.cs`
  - `src/FamilyOs.Domain/Entities/Deadline.cs`
  - `src/FamilyOs.Domain/Enums/TaskStatus.cs`, `Priority.cs`,
    `DeadlineStatus.cs`, `DeadlineCategory.cs`
  - Configurations.
  - migráció.
- **AC:**
  - [ ] `database-schema.md` §4.13–4.14 séma pontosan.
  - [ ] CHECK constraint-ek (Suggested csak AiSuggested origin-nel; Done →
        completed_utc kötelező).
  - [ ] Indexek partial-szerűen a backlog-szerinti gyors query-khez.

### T-FBE-02 — `TaskStateMachine` domain szolgáltatás
- **Cél:** állapot-átmenetek invariánsai egyetlen helyen.
- **Fájlok:**
  - `src/FamilyOs.Domain/Services/TaskStateMachine.cs`
- **AC:**
  - [ ] Megengedett: `Suggested → Open`, `Open → InProgress | Done | Cancelled`,
        `InProgress → Done | Cancelled`.
  - [ ] Tiltott átmenet → `DomainException`.
  - [ ] Unit teszt minden átmenetre.

### T-FBE-03 — `DeadlineStateMachine`
- **Cél:** analóg.
- **Fájlok:**
  - `src/FamilyOs.Domain/Services/DeadlineStateMachine.cs`
- **AC:**
  - [ ] `Upcoming → Due` automatikus a worker scan-ben (lásd reminder-engine).
  - [ ] `Due → Passed` automatikus.
  - [ ] `Upcoming/Due → Resolved | Dismissed` user akcióból.

### T-FBE-04 — Task CRUD endpointok
- **Cél:** `api-design.md` §11.
- **Fájlok:**
  - `src/FamilyOs.Application/Tasks/*.cs` (Create/Get/List/Update/Delete).
  - `src/FamilyOs.Api/Endpoints/TasksModule.cs`.
- **AC:**
  - [ ] Szűrés `?status=`, `?assignedToFamilyMemberId=`, `?priority=`,
        `?origin=AiSuggested`.
  - [ ] Row-level RBAC: csak a saját + nem-private látható.
  - [ ] PATCH `If-Match`.

### T-FBE-05 — Task állapot-akció endpointok
- **Cél:** `/approve`, `/reject`, `/start`, `/complete`, `/cancel`.
- **Fájlok:**
  - `src/FamilyOs.Application/Tasks/Actions/*.cs`
- **AC:**
  - [ ] Approve: `Suggested → Open`, `Origin = AiApproved`,
        `approvedByUserAccountId`, `approvedUtc` kitöltve.
  - [ ] Reject: soft-delete + `Origin` megmarad audit-célra.
  - [ ] Complete: `completed_utc = now()`.
  - [ ] Audit log: `Approve` / `Reject` / `Update`.

### T-FBE-06 — Deadline CRUD endpointok
- **Cél:** analóg a Task-nak.
- **Fájlok:**
  - `src/FamilyOs.Application/Deadlines/*.cs`
  - `src/FamilyOs.Api/Endpoints/DeadlinesModule.cs`
- **AC:**
  - [ ] Szűrés `?from=`, `?to=`, `?category=`, `?status=`.
  - [ ] Default szortírozás `due_date_utc`.

### T-FBE-07 — Deadline állapot-akció endpointok
- **Cél:** `/approve`, `/resolve`, `/dismiss`.
- **Fájlok:**
  - `src/FamilyOs.Application/Deadlines/Actions/*.cs`
- **AC:**
  - [ ] Approve: `Upcoming` marad + `Origin = AiApproved`, **+ generál
        default reminder-eket** a `DeadlineCategory` szerint
        (reminder-engine.md §3.10 policy).
  - [ ] Resolve / Dismiss: státusz-átmenet + audit.

### T-FBE-08 — Default reminder policy resolver
- **Cél:** kategória → offset-ek + csatorna.
- **Fájlok:**
  - `src/FamilyOs.Domain/Services/DefaultReminderPolicy.cs`
- **AC:**
  - [ ] `reminder-engine.md` §3.10 táblázat alapján.
  - [ ] `Insurance → [30, 7, 1] napos offset-ek, InApp + Email csatorna`.
  - [ ] Konfigurálható `appsettings.json`-on (de defaults).

### T-FBE-09 — `SuggestionsAggregator` (F3)
- **Cél:** `GET /api/v1/suggestions` — current user-hez tartozó összes
  nyitott javaslat.
- **Fájlok:**
  - `src/FamilyOs.Application/Suggestions/GetSuggestionsQuery.cs`
  - `src/FamilyOs.Application/Suggestions/Dtos/SuggestionsAggregateDto.cs`
- **AC:**
  - [ ] 5 kategória: tasks, deadlines, tags, topics, facets.
  - [ ] Document-szintű csoportosítás a tag-ek és topic-ok esetében.

### T-FBE-10 — Batch jóváhagyási command
- **Cél:** `POST /api/v1/suggestions/batch`.
- **Fájlok:**
  - `src/FamilyOs.Application/Suggestions/BatchApproveCommand.cs` +
    Handler + Validator.
- **AC:**
  - [ ] Egy tranzakció: minden megjelölt javaslat státusz-átmenete.
  - [ ] Reject: soft delete vagy `Dismissed` (entitás-függő).
  - [ ] Audit log minden műveletre.
  - [ ] Partial failure tolerált: 200 + `{ approved: N, rejected: M, errors: [] }`.

### T-FBE-11 — Integration tesztek
- **Cél:** állapotgép + batch flow.
- **Fájlok:**
  - `tests/FamilyOs.Api.IntegrationTests/Tasks/TaskLifecycleTests.cs`
  - `tests/FamilyOs.Api.IntegrationTests/Deadlines/DeadlineApprovalTriggersRemindersTests.cs`
  - `tests/FamilyOs.Api.IntegrationTests/Suggestions/BatchApprovalTests.cs`
- **AC:**
  - [ ] Suggested → Open átmenet engedélyezve, fordítva nem.
  - [ ] Deadline approve → 3 reminder rekord létrejön kategóriához igazodóan.
  - [ ] Batch: 5 javaslatot egyszerre elfogadunk; mindegyik aktív.

---

## Megvalósítási sorrend

```
T-FBE-01 → 02 → 03                  (entitások + statemachine)
       → 04 → 05                     (Task CRUD + akciók)
       → 06 → 07 → 08                (Deadline CRUD + akciók + policy)
       → 09 → 10                     (Suggestions inbox + batch)
       → 11                           (tesztek)
```

## Epic-DoD

- [ ] Task és Deadline manuálisan létrehozható, módosítható, törölhető.
- [ ] AI suggestion-ök elfogadhatók egyenként vagy batch-ben.
- [ ] Deadline approve → default reminderek létrejönnek a kategóriából.
- [ ] Audit log minden állapot-átmenetnél.
- [ ] Integration tesztek zöldek.

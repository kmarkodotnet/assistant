# Epic L — Dashboard — Backend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` (FULL — baseline)
> - `api-design.md` §20 (Dashboard)
> - `search-strategy.md` §10 (UI integráció — saved searches)
> - `reminder-engine.md` §10 (UI — overdue összesítő)
> - `database-schema.md` §8 (méretezés — performance budget)
>
> **Story-k:** L1, L2
> **Fázis:** Fázis 11

---

## Áttekintés

Egyetlen aggregált endpoint a dashboard összes widget-jéhez < 200 ms p95.
A handler 3-4 párhuzamos query-vel optimalizál.

## Taskok

### T-LBE-01 — `GetDashboardQuery` + DTO
- **Fájlok:**
  - `src/FamilyOs.Application/Dashboard/GetDashboardQuery.cs`
  - `src/FamilyOs.Application/Dashboard/Dtos/DashboardDto.cs`
  - `src/FamilyOs.Application/Dashboard/Dtos/DashboardWidgetsDto.cs`
- **AC:**
  - [ ] Tartalom: `upcomingDeadlines (10)`, `pendingSuggestions
        { tasks, deadlines, facets }`, `recentDocuments (5)`,
        `overdueReminders (5)`, `savedSearches`.

### T-LBE-02 — `DashboardHandler` párhuzamos query
- **Fájlok:**
  - `src/FamilyOs.Application/Dashboard/GetDashboardHandler.cs`
- **AC:**
  - [ ] 4 párhuzamos `Task<>`: deadlines, suggestions count, recent docs,
        overdue reminders.
  - [ ] Saved searches gyors single query.
  - [ ] p95 < 200 ms 5 év szimulált adaton (lásd database-schema.md §8).

### T-LBE-03 — Dashboard endpoint
- **Fájlok:**
  - `src/FamilyOs.Api/Endpoints/DashboardModule.cs`
- **AC:**
  - [ ] `GET /api/v1/dashboard` 200 + `DashboardDto`.
  - [ ] `RequireAuthenticated`.
  - [ ] Cache header `Cache-Control: private, max-age=30` (rövid kliens-cache).

### T-LBE-04 — RBAC szűrés a dashboard query-ben
- **Cél:** csak a current user által látható rekordok aggregálva.
- **Fájlok:**
  - kiegészítések a handler-ben.
- **AC:**
  - [ ] Child user csak a saját rekordjait látja.
  - [ ] Adult: nem-private idegen rekordok is, ha relevánsak.
  - [ ] MedicalRecord: szigorú szűrés.

### T-LBE-05 — Saved Searches endpoint (L2 / E7 átfedés)
- **Fájlok:**
  - már Epic E T-EBE-18-ban — itt csak ellenőrizzük az integrációt.
- **AC:**
  - [ ] A dashboardon a current user mentett kereséseit listázza.

### T-LBE-06 — Overdue reminders aggregátor
- **Cél:** L2 dedikált handler.
- **Fájlok:**
  - `src/FamilyOs.Application/Dashboard/GetOverdueRemindersQuery.cs`
- **AC:**
  - [ ] `Skipped` és `Fired AND acknowledged_utc IS NULL` reminder-ek.
  - [ ] Csoportosítás dátum szerint.
  - [ ] Akció gomb metaadatok (újraütemezhető? elvethető?).

### T-LBE-07 — Performance tesztek
- **Fájlok:**
  - `tests/FamilyOs.Performance.Tests/DashboardLatencyTests.cs`
- **AC:**
  - [ ] 5 év szimulált adat (Bogus library).
  - [ ] p95 < 200 ms.

---

## Megvalósítási sorrend

```
T-LBE-01 → 02 → 03 → 04             (alap dashboard)
       → 05 → 06                    (saved + overdue)
       → 07                          (perf)
```

## Epic-DoD

- [ ] Dashboard endpoint < 200 ms p95 5 év szimulált adaton.
- [ ] Minden widget releváns RBAC-szűréssel.
- [ ] Overdue reminders külön aggregátum.

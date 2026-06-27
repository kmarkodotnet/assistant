# Epic L — Dashboard — Frontend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` §9, §10
> - `frontend-structure.md` (FULL — különösen §8.1 Dashboard wireframe)
> - `api-design.md` §20 (Dashboard)
> - `search-strategy.md` §10 (mentett keresések widget)
> - `reminder-engine.md` §10 (lecsúszott összesítő)
>
> **Story-k:** L1 (FE), L2 (FE)
> **Fázis:** Fázis 11

---

## Áttekintés

A dashboard a felhasználó belépésekor megjelenő képernyő — áttekintés egy
pillantásra. 4-6 widget, mind kompakt és cselekvésre orientált.

## Taskok

### T-LFE-01 — Dashboard API client + DTO
- **Fájlok:**
  - `frontend/src/app/features/dashboard/services/dashboard.api.ts`
  - `frontend/src/app/features/dashboard/models/dashboard.dto.ts`
- **AC:**
  - [ ] Egyetlen `GET /api/v1/dashboard` call.

### T-LFE-02 — `DashboardFacade`
- **Fájlok:**
  - `frontend/src/app/features/dashboard/services/dashboard.facade.ts`
- **AC:**
  - [ ] State: `widgets`, `loading`, `error`, `lastUpdatedUtc`.
  - [ ] Auto-refresh 5 percenként (interval-stop a tab inactive-ra).
  - [ ] `refresh()` manuális frissítés.

### T-LFE-03 — Dashboard page layout
- **Fájlok:**
  - `frontend/src/app/features/dashboard/pages/dashboard.page.ts`
  - `frontend/src/app/features/dashboard/pages/dashboard.page.html`
- **AC:**
  - [ ] Grid layout: 2 oszlop desktop, 1 oszlop mobile.
  - [ ] Üdvözlés magyar napszakkal („Jó reggelt, Apa!").
  - [ ] Felül a globális keresősáv (re-used Epic E).
  - [ ] Loading skeleton minden widget-re.

### T-LFE-04 — `UpcomingDeadlinesWidget`
- **Fájlok:**
  - `frontend/src/app/features/dashboard/components/upcoming-deadlines-widget.component.ts`
- **AC:**
  - [ ] Top 5 közelgő deadline.
  - [ ] Megjelenít: cím, dátum, hátralévő nap relatíven, kategória ikon.
  - [ ] „Mindet megnézem" link a `/deadlines`-re.

### T-LFE-05 — `PendingSuggestionsWidget`
- **Fájlok:**
  - `frontend/src/app/features/dashboard/components/pending-suggestions-widget.component.ts`
- **AC:**
  - [ ] Számláló az 5 kategórián (tasks/deadlines/tags/topics/facets).
  - [ ] Klikkre `/suggestions`-re ugrik.
  - [ ] Sárga sáv.

### T-LFE-06 — `RecentDocumentsWidget`
- **Fájlok:**
  - `frontend/src/app/features/dashboard/components/recent-documents-widget.component.ts`
- **AC:**
  - [ ] Utolsó 5 dokumentum (created_utc DESC).
  - [ ] Mini-kártyák ikonnal + címmel.
  - [ ] „Mindet" link `/documents`-re.

### T-LFE-07 — `OverdueRemindersWidget`
- **Fájlok:**
  - `frontend/src/app/features/dashboard/components/overdue-reminders-widget.component.ts`
- **AC:**
  - [ ] Lecsúszott reminder lista (top 5).
  - [ ] Per-item akciók: „Újraütemezem" / „Elvetem".
  - [ ] Üres állapot: „Nincs lecsúszott emlékeztető. 🎉" (emoji csak ha
        a user explicit kéri).
  - [ ] „Mindet megnézem" link `/reminders?status=Skipped`-re.

### T-LFE-08 — `SavedSearchesWidget`
- **Fájlok:**
  - `frontend/src/app/features/dashboard/components/saved-searches-widget.component.ts`
- **AC:**
  - [ ] Saved searches lista.
  - [ ] Klikkre az adott keresés lefut (`/search?q=...&mode=...`).
  - [ ] Üres állapot „Még nincs mentett keresésed" + magyar tipp.

### T-LFE-09 — Üdvözlés napszak szerint
- **Fájlok:**
  - `frontend/src/app/features/dashboard/components/greeting.component.ts`
- **AC:**
  - [ ] Reggel 5-10: „Jó reggelt".
  - [ ] 10-17: „Szia".
  - [ ] 17-22: „Jó estét".
  - [ ] 22-5: „Jó éjt".

### T-LFE-10 — Performance: parallel widget rendering
- **Cél:** `OnPush` + signals → render gyors.
- **Fájlok:**
  - finomítás minden widget komponensében.
- **AC:**
  - [ ] First contentful paint < 1 s lokálban.
  - [ ] Layout shift nélkül skeletonok.

### T-LFE-11 — Tesztek
- **Fájlok:**
  - `frontend/src/app/features/dashboard/pages/dashboard.page.spec.ts`
  - `frontend/e2e/dashboard/dashboard-smoke.spec.ts`
- **AC:**
  - [ ] Üres állapot (új user) jól render.
  - [ ] Adat-mock-kal minden widget tartalom megjelenik.

---

## Megvalósítási sorrend

```
T-LFE-01 → 02                       (data)
       → 03                           (page layout)
       → 04 → 05 → 06 → 07 → 08      (widget-ek)
       → 09                           (greeting)
       → 10                           (perf)
       → 11                           (tesztek)
```

## Epic-DoD

- [ ] Dashboard betölt < 1 s.
- [ ] 5 widget működik magyar tartalommal.
- [ ] Üres állapot új user-re informatív.
- [ ] Adult / Admin / Child eltérő widget-lista (RBAC alapján).

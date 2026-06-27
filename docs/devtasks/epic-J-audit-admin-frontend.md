# Epic J — Audit + admin — Frontend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` §9, §10
> - `frontend-structure.md` §1.1 (admin route), §8.10 (Settings — admin tabok)
> - `api-design.md` §18 (AI jobs admin), §19 (Audit log)
> - `security-privacy.md` §11.1 (admin felület RBAC)
>
> **Story-k:** J2 (FE), J3 (FE), J4 (FE)
> **Fázis:** Fázis 12

---

## Áttekintés

Admin-only `/admin` szekció:
- **Audit log böngészés** — szűrhető lista + export gomb.
- **Security events** — pre-szűrt nézet, dashboard widget.
- **AI jobs admin** — failed/queued lista, retry/cancel akciók.
- **AI provider config** — providerek listája, enable/disable.

## Taskok

### T-JFE-01 — Admin API client + DTOs
- **Fájlok:**
  - `frontend/src/app/features/admin/services/audit.api.ts`
  - `frontend/src/app/features/admin/services/ai-jobs.api.ts`
  - `frontend/src/app/features/admin/services/ai-providers.api.ts`
  - `frontend/src/app/features/admin/models/*.dto.ts`
- **AC:**
  - [ ] CRUD + szűrés + export trigger.

### T-JFE-02 — Admin route + sidebar
- **Fájlok:**
  - `frontend/src/app/features/admin/admin.routes.ts`
  - kiegészítés `shell.component.ts`-ben (admin menü).
- **AC:**
  - [ ] `RequireAdmin` roleGuard.
  - [ ] Aldroutes: `/admin/audit`, `/admin/security-events`,
        `/admin/jobs`, `/admin/providers`.

### T-JFE-03 — Audit log böngésző oldal
- **Fájlok:**
  - `frontend/src/app/features/admin/pages/audit-log.page.ts`
  - `frontend/src/app/features/admin/components/audit-filter.component.ts`
  - `frontend/src/app/features/admin/components/audit-entry-row.component.ts`
- **AC:**
  - [ ] Szűrők: user select, action multiselect, dátumtartomány,
        entityType, entityId.
  - [ ] Virtual scroll a hosszú listához.
  - [ ] `details_json` kibontható JSON viewer-rel.

### T-JFE-04 — Security events oldal
- **Fájlok:**
  - `frontend/src/app/features/admin/pages/security-events.page.ts`
- **AC:**
  - [ ] Default 7 nap visszamenőleg.
  - [ ] `LoginFailed` rekordok kiemelve.
  - [ ] „Részletek" gomb kibontja az IP / UserAgent infót.

### T-JFE-05 — Audit log export
- **Fájlok:**
  - kiegészítés a `audit-log.page.ts`-ben.
- **AC:**
  - [ ] „Export CSV" / „Export JSON" gomb.
  - [ ] Confirm dialog: „Az export az audit logba is bekerül."
  - [ ] Letöltés `a` tag-gel + `download` attribútum.

### T-JFE-06 — AI jobs admin oldal
- **Fájlok:**
  - `frontend/src/app/features/admin/pages/ai-jobs.page.ts`
  - `frontend/src/app/features/admin/components/ai-job-row.component.ts`
- **AC:**
  - [ ] Lista: id, jobType, status, attempt, lastError, age.
  - [ ] Akciók: Retry, Cancel.
  - [ ] Szűrés: status, jobType.

### T-JFE-07 — Queue stats dashboard widget
- **Fájlok:**
  - `frontend/src/app/features/admin/components/queue-stats-widget.component.ts`
- **AC:**
  - [ ] Per-jobType × status mátrix.
  - [ ] Auto-refresh 10 mp-enként.

### T-JFE-08 — AI providers oldal
- **Fájlok:**
  - `frontend/src/app/features/admin/pages/ai-providers.page.ts`
- **AC:**
  - [ ] Provider kártyák: név, enabled toggle, model szelektor.
  - [ ] Health indikátor (zöld/sárga/piros).
  - [ ] PrivacyMode jelölve (read-only): „LocalOnly (kódba égetve, MVP-ben
        nem szerkeszthető)".

### T-JFE-09 — Tesztek
- **Fájlok:**
  - `frontend/src/app/features/admin/pages/audit-log.page.spec.ts`
  - `frontend/src/app/features/admin/pages/ai-jobs.page.spec.ts`
- **AC:**
  - [ ] Audit-szűrés mocked backenddel.
  - [ ] AI job retry akció hívás.

---

## Megvalósítási sorrend

```
T-JFE-01 → 02                       (data + routing)
       → 03 → 04 → 05                (audit)
       → 06 → 07                    (AI jobs)
       → 08                           (providers)
       → 09                           (tesztek)
```

## Epic-DoD

- [ ] Admin szekció elérhető Admin-nak, Adult/Child-nek tiltva.
- [ ] Audit log böngészhető és exportálható.
- [ ] AI jobs retry/cancel működik.
- [ ] PrivacyMode védve, vizuálisan jelezve.

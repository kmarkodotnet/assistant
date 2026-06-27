# Epic A — Alapok és infra — Frontend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` (FULL — különösen §9 Angular, §10 FE struktúra)
> - `frontend-structure.md` (FULL — §2, §3, §4, §6, §13)
> - `api-design.md` §3 (Auth), §1.3 (ProblemDetails formátum)
> - `architecture.md` §10 (Frontend áttekintés)
> - `security-privacy.md` §3.2 (cookie session — kliens oldali kezelés)
> - ADR-0003 (LAN-only — heartbeat)
>
> **Story-k:** A3 (FE), A4 (FE), A5 (FE), C2 (skeleton)
> **Fázis:** Fázis 4 (Angular shell)

---

## Áttekintés

Angular 20 standalone alkalmazás Tailwind + signals alapokon. Az epic végén:
login Google-lal, üres feature oldalak közötti navigáció, logout, alap
toast hibakezelés. Még semmilyen valós adat-CRUD — csak az auth + váz.

## Taskok

### T-AFE-01 — Angular standalone scaffold + Tailwind
- **Cél:** `frontend/` projekt készen áll, Tailwind konfigurálva, magyar design tokens.
- **Fájlok:**
  - `frontend/angular.json`
  - `frontend/package.json` (pnpm, scripts: `start`, `build`, `test`, `lint`,
    `gen:api`)
  - `frontend/tailwind.config.ts`
  - `frontend/postcss.config.js`
  - `frontend/src/styles/tailwind.css`
  - `frontend/src/styles/theme.css` (CSS variables: primary, warn, danger,
    success).
  - `frontend/src/main.ts`, `index.html`.
- **AC:**
  - [ ] `pnpm install && pnpm build` zöld.
  - [ ] Tailwind purge biztonságos (safelist a dinamikus class-okra).
  - [ ] Téma: light/dark, `prefers-color-scheme` alap.

### T-AFE-02 — TypeScript strict beállítások
- **Cél:** `coding-standards.md` §9.1 szerinti TS config.
- **Fájlok:**
  - `frontend/tsconfig.json` (strict + `noUncheckedIndexedAccess` +
    `exactOptionalPropertyTypes`).
  - `frontend/tsconfig.app.json`, `tsconfig.spec.json`.
- **AC:**
  - [ ] Build zöld a strict beállítások mellett.

### T-AFE-03 — Routing csontváz
- **Cél:** lazy-loaded feature routes (`frontend-structure.md` §3).
- **Fájlok:**
  - `frontend/src/app/app.config.ts` (`provideRouter` +
    `withEnabledBlockingInitialNavigation`).
  - `frontend/src/app/app.routes.ts`.
  - `frontend/src/app/app.component.ts`.
- **AC:**
  - [ ] Minden route lazy-loaded (`loadChildren` vagy `loadComponent`).
  - [ ] `**` redirect `''`-re.

### T-AFE-04 — Shell layout (navbar + sidebar + outlet)
- **Cél:** desktop: bal sidebar; mobile: bottom nav.
- **Fájlok:**
  - `frontend/src/app/layout/shell.component.ts`
  - `frontend/src/app/layout/shell.component.html`
  - `frontend/src/app/layout/navbar.component.ts` (bell ikon placeholder).
  - `frontend/src/app/layout/sidebar.component.ts` (desktop).
  - `frontend/src/app/layout/bottom-nav.component.ts` (mobile).
- **AC:**
  - [ ] Tailwind breakpoint-ok (`md`-től sidebar, alatta bottom-nav).
  - [ ] Magyar feliratok az `i18n`-ből.
  - [ ] OnPush change detection mindenhol.

### T-AFE-05 — `createStore` signal-store helper
- **Cél:** `coding-standards.md` §9 + `frontend-structure.md` §4.2 helper.
- **Fájlok:**
  - `frontend/src/app/core/state/create-store.ts`
  - `frontend/src/app/core/state/persist.ts` (localStorage helper).
- **AC:**
  - [ ] Generic `createStore<T>(initial)` ad: `state`, `update`, `set`,
        `select`.
  - [ ] Unit teszt: select a state változására reagál.

### T-AFE-06 — `AuthService` + globális `authStore`
- **Cél:** current user signal, login/logout flow indítók.
- **Fájlok:**
  - `frontend/src/app/core/auth/auth.service.ts`
  - `frontend/src/app/core/auth/auth.store.ts` (`createStore`-ral).
  - `frontend/src/app/core/auth/current-user.dto.ts`.
- **AC:**
  - [ ] `currentUser`, `isAuthenticated`, `isAdmin` computed signal-ek.
  - [ ] `loadCurrentUser()` aszinkron, 401-re `status = 'anonymous'`.
  - [ ] `logout()` `POST /api/v1/auth/logout` + auth.store reset.

### T-AFE-07 — `authGuard` és `roleGuard`
- **Cél:** route protection (`frontend-structure.md` §3.3).
- **Fájlok:**
  - `frontend/src/app/core/auth/auth.guard.ts`
  - `frontend/src/app/core/auth/role.guard.ts`
- **AC:**
  - [ ] `authGuard` 401 esetén redirect `/login?returnUrl=...`.
  - [ ] `roleGuard` `data.roles`-ról dolgozik, 403 oldalra navigál (vagy
        dashboardra toast-tal).

### T-AFE-08 — `LoginPage`
- **Cél:** egyetlen Google login gomb + magyar privacy szöveg.
- **Fájlok:**
  - `frontend/src/app/features/auth/login.page.ts`
  - `frontend/src/app/features/auth/login.page.html`
- **AC:**
  - [ ] Google Identity Services script betöltve.
  - [ ] `id_token` callback → `POST /api/v1/auth/login/google` → cookie
        beáll → redirect dashboard-ra (vagy `returnUrl`-re).
  - [ ] Hiba esetén magyar toast.

### T-AFE-09 — HTTP interceptorok: trace, error, credentials
- **Cél:** kontextus minden requesten.
- **Fájlok:**
  - `frontend/src/app/core/api/trace-id.interceptor.ts` (W3C `traceparent`
    fejléc).
  - `frontend/src/app/core/api/http-error.interceptor.ts` (ProblemDetails
    → `AppError`).
  - `frontend/src/app/core/api/with-credentials.interceptor.ts`
    (`withCredentials: true` az `/api/`-ra).
- **AC:**
  - [ ] 401-re a `httpError` interceptor `redirect('/login')` triggerel.
  - [ ] 403-ra toast magyar üzenettel.
  - [ ] `fieldErrors` továbbítva a komponensbe a `AppError.fieldErrors`-ban.

### T-AFE-10 — Empty placeholder feature pages
- **Cél:** minden route bevezethető üres komponenssel.
- **Fájlok:**
  - `frontend/src/app/features/dashboard/dashboard.routes.ts` + page.
  - `frontend/src/app/features/documents/documents.routes.ts` + placeholder
    list page.
  - `frontend/src/app/features/search/search.routes.ts` + placeholder.
  - `frontend/src/app/features/tasks/`, `deadlines/`, `reminders/`,
    `topics/`, `family/`, `suggestions/`, `settings/`, `admin/`.
- **AC:**
  - [ ] Minden top-level route URL betöltődik, üres „hamarosan" oldalt
        mutat.
  - [ ] A sidebar és bottom-nav linkek mindegyike működik.

### T-AFE-11 — i18n setup magyar nyelvvel
- **Cél:** `ngx-translate` + `assets/i18n/hu.json` ~30 string.
- **Fájlok:**
  - `frontend/src/assets/i18n/hu.json`
  - `frontend/src/app/core/i18n/i18n.providers.ts`
- **AC:**
  - [ ] `{{ 'documents.title' | translate }}` magyar string-et renderel.
  - [ ] Defaultnyelv `hu`, fallback `hu` (nincs angol).

### T-AFE-12 — `huDate`, `huRelativeDate`, `fileSize` pipe-ok
- **Cél:** magyar formátum.
- **Fájlok:**
  - `frontend/src/app/shared/pipes/hu-date.pipe.ts`
  - `frontend/src/app/shared/pipes/hu-relative-date.pipe.ts`
  - `frontend/src/app/shared/pipes/file-size.pipe.ts`
  - `frontend/src/app/shared/pipes/document-icon.pipe.ts`
- **AC:**
  - [ ] `Intl.DateTimeFormat('hu-HU')` használat.
  - [ ] Egységtesztek minden pipe-ra.

### T-AFE-13 — Shared UI primitívek (csontváz)
- **Cél:** `ui-button`, `ui-card`, `ui-badge`, `ui-empty-state`, `ui-skeleton`.
- **Fájlok:**
  - `frontend/src/app/shared/ui/button.component.ts`
  - `frontend/src/app/shared/ui/card.component.ts`
  - `frontend/src/app/shared/ui/badge.component.ts`
  - `frontend/src/app/shared/ui/empty-state.component.ts`
  - `frontend/src/app/shared/ui/skeleton.component.ts`
- **AC:**
  - [ ] Variants Tailwind class-okkal (`primary`, `ghost`, `danger`,
        `warning-suggested`).
  - [ ] `OnPush` mindenhol.

### T-AFE-14 — Toast / notification rendszer (csontváz)
- **Cél:** globális hibák és sikerek megjelenítése.
- **Fájlok:**
  - `frontend/src/app/core/notifications/notification.service.ts`
    (toast trigger signal).
  - `frontend/src/app/core/notifications/toast.component.ts` (felül-jobb).
- **AC:**
  - [ ] `notify.error('üzenet')`, `.success(...)` API.
  - [ ] 3 mp után automatikusan eltűnik (kivéve sticky).

### T-AFE-15 — LAN heartbeat detection
- **Cél:** `frontend-structure.md` §12 — 60 mp-enként `GET /api/v1/system/heartbeat`.
- **Fájlok:**
  - `frontend/src/app/core/realtime/heartbeat.service.ts`
  - `frontend/src/app/layout/offline-overlay.component.ts` („Nem vagy
    otthon" képernyő).
- **AC:**
  - [ ] 3 egymást követő fail → `offline-overlay` jelenik meg.
  - [ ] Visszaállás esetén az overlay eltűnik, az app folytatja.

### T-AFE-16 — API kliens generálás pipeline
- **Cél:** `pnpm gen:api` futtatható (a backend OpenAPI elérhetősége
  után aktiválható).
- **Fájlok:**
  - `frontend/package.json` `scripts.gen:api`.
  - `frontend/nswag.json` vagy `openapi-typescript-codegen` config.
  - `frontend/src/app/api-client/.gitkeep` (placeholder mappa).
- **AC:**
  - [ ] Script létezik és dokumentált; de futtatás csak akkor, ha a backend
        OpenAPI JSON elérhető (Fázis 5-től releváns).

### T-AFE-17 — ESLint + Prettier + commitlint
- **Cél:** lint és formatter enforcer (`coding-standards.md` §12).
- **Fájlok:**
  - `frontend/.eslintrc.json` (`@angular-eslint/recommended`).
  - `frontend/.prettierrc.json`.
  - `.husky/pre-commit`, `.husky/commit-msg`.
  - `commitlint.config.js` (conventional commits).
- **AC:**
  - [ ] `pnpm lint` zöld.
  - [ ] Hibás conventional commit elutasítva pre-push előtt.

### T-AFE-18 — Vitest + Angular Testing Library setup
- **Cél:** unit teszt infrastruktúra.
- **Fájlok:**
  - `frontend/vitest.config.ts`
  - `frontend/src/test-setup.ts`
- **AC:**
  - [ ] `pnpm test` egy „smoke" teszten zöld (auth.guard.spec).

### T-AFE-19 — Playwright @smoke E2E
- **Cél:** login → dashboard → logout end-to-end.
- **Fájlok:**
  - `frontend/playwright.config.ts`
  - `frontend/e2e/smoke/auth-flow.spec.ts`
- **AC:**
  - [ ] Headless Chromium-en zöld.
  - [ ] A teszt magyar UI assertion-eket használ
        (`page.getByText('Bejelentkezés')`).

---

## Megvalósítási sorrend

```
T-AFE-01 → 02 → 03 → 04                       (vázalap)
        → 05 → 06 → 07 → 09                    (state + auth)
        → 08 → 10 → 11 → 12 → 13 → 14          (login + üres oldalak + UI)
        → 15                                    (offline overlay)
        → 16 → 17 → 18 → 19                    (CI / teszt)
```

## Epic-DoD

- [ ] `pnpm build --configuration production` zöld.
- [ ] `pnpm test` zöld.
- [ ] Playwright `@smoke` zöld.
- [ ] Manuálisan: Google login → dashboard → logout működik.
- [ ] Minden top-level route üres-de-elérhető oldal.
- [ ] Magyar UI mindenhol, no fallback angol szöveg.
- [ ] `code-reviewer` jóváhagyta a folder layout-ot és a strict TS-t.

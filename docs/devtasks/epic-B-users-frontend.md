# Epic B — Felhasználó-kezelés — Frontend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` §9, §10 (Angular + FE struktúra)
> - `frontend-structure.md` §13 (Auth flow UI), §8.8 (Family page), §8.10 (Settings — preferences szakasz)
> - `api-design.md` §5 (Family), §6 (Users), §6.5 (preferences)
> - `security-privacy.md` §4 (RBAC) — szerepkör-tudatos UI rendering
> - `reminder-engine.md` §5.4 (preferenciák szemantikája)
>
> **Story-k:** B1 (FE), B2 (FE), B3 (FE)
> **Fázis:** Fázis 3-4 átfedés

---

## Áttekintés

A felhasználói felület felelőssége az Epic B-ben:
- **Family management** oldal (admin) — lista, létrehozás, szerkesztés,
  törlés modal-okkal.
- **Invite flow** — meghívási dialog (admin).
- **Settings → saját preferenciák** — csendes órák, email opt-in.
- **Szerepkör-tudatos navigáció** — Child-nak a Family menü rejtett.

## Taskok

### T-BFE-01 — `FamilyMember` típus + API client
- **Cél:** generált client használata vagy kézi DTO az MVP korai szakaszában.
- **Fájlok:**
  - `frontend/src/app/api-client/family.api.ts` (generated) VAGY
  - `frontend/src/app/features/family/services/family.api.ts` (kézi MVP).
  - `frontend/src/app/features/family/models/family-member.dto.ts`.
- **AC:**
  - [ ] Connect `GET /api/v1/family-members`, `POST`, `PATCH`, `DELETE`.
  - [ ] HTTP error → `AppError` mapping (lásd Epic A interceptor).

### T-BFE-02 — `FamilyFacade` (signal-store)
- **Cél:** `frontend-structure.md` §4 mintára state-store.
- **Fájlok:**
  - `frontend/src/app/features/family/services/family.facade.ts`.
- **AC:**
  - [ ] State: `members[]`, `loading`, `error`.
  - [ ] Akciók: `load()`, `create(dto)`, `update(id, dto, rowVersion)`,
        `softDelete(id)`.
  - [ ] Optimistic update vagy reload (MVP-ben reload elég).

### T-BFE-03 — `/family` lista oldal (admin)
- **Cél:** táblázat / kártya-grid a családtagokról.
- **Fájlok:**
  - `frontend/src/app/features/family/pages/family-list.page.ts`
  - `frontend/src/app/features/family/pages/family-list.page.html`
  - `frontend/src/app/features/family/components/family-member-card.component.ts`
- **AC:**
  - [ ] Magyar oszlop-fejlécek.
  - [ ] „Új családtag" gomb modal-t nyit.
  - [ ] Kártyák kontextus menüvel: Szerkesztés, Törlés.
  - [ ] Üres állapot: `ui-empty-state` magyar szöveggel.

### T-BFE-04 — Family member szerkesztő dialog
- **Cél:** Create + Edit egy közös dialog.
- **Fájlok:**
  - `frontend/src/app/features/family/components/family-member-form.dialog.ts`
- **AC:**
  - [ ] Reactive form: `displayName`, `fullName`, `relation` (select),
        `birthDate` (datepicker).
  - [ ] FluentValidation hibák → mező alatti magyar szöveg.
  - [ ] Mentés után dialog bezárul, lista frissül.

### T-BFE-05 — Soft delete megerősítő dialog
- **Cél:** ne véletlen kattintásból töröljön.
- **Fájlok:**
  - `frontend/src/app/shared/ui/confirm-dialog.component.ts` (újrahasznosítható).
- **AC:**
  - [ ] Magyar megerősítő szöveg, „Mégse" + „Törlés".
  - [ ] 409 esetén magyar toast: „Ennek a családtagnak van élő
        felhasználói fiókja. Előbb deaktiváld."

### T-BFE-06 — Invite user dialog
- **Cél:** email + family-member kiválasztás + szerepkör.
- **Fájlok:**
  - `frontend/src/app/features/family/components/invite-user.dialog.ts`
- **AC:**
  - [ ] Email mező validáció.
  - [ ] Family-member-select (a meglévők közül).
  - [ ] Role select (`Admin`, `Adult`, `Child`).
  - [ ] Sikeres meghívás után toast + a card jelzi: „Meghívva".

### T-BFE-07 — `roleGuard` a `/family` route-on
- **Cél:** csak admin férjen hozzá.
- **Fájlok:**
  - `frontend/src/app/features/family/family.routes.ts` (kiegészítés
    `canActivate: [roleGuard], data: { roles: ['Admin'] }`).
- **AC:**
  - [ ] Adult / Child user a `/family`-t nem éri el (403 oldal vagy
        dashboard redirect + magyar toast).

### T-BFE-08 — Szerepkör-tudatos navigáció
- **Cél:** sidebar / bottom-nav rejtse a `Family`, `Admin`, `Suggestions`
  menüket a megfelelő szerepkör hiányában.
- **Fájlok:**
  - `frontend/src/app/layout/sidebar.component.ts` (computed signal:
    `visibleMenuItems`).
  - `frontend/src/app/layout/bottom-nav.component.ts` (analóg).
- **AC:**
  - [ ] Child user csak: Dashboard, Reminders, Search, Settings.
  - [ ] Adult: + Documents, Notes, Tasks, Deadlines, Suggestions.
  - [ ] Admin: + Family, Admin.

### T-BFE-09 — `/settings` saját preferenciák
- **Cél:** csendes órák + email opt-in szerkesztése.
- **Fájlok:**
  - `frontend/src/app/features/settings/pages/preferences.page.ts`
  - `frontend/src/app/features/settings/services/preferences.facade.ts`
  - `frontend/src/app/features/settings/services/preferences.api.ts`
- **AC:**
  - [ ] Time-input mezők (`<input type="time">` HTML natívan magyar).
  - [ ] Email opt-in toggle, ha SMTP nincs konfigurálva a backend-en, info
        szöveg jelenik meg: „Az email értesítés a háztartási SMTP
        konfigurálása után válik aktívvá."
  - [ ] Mentés után toast: „Beállítások mentve".

### T-BFE-10 — Settings oldal struktúra (placeholder a többi szakasznak)
- **Cél:** `/settings` route több tabot kap (preferenciák, később AI,
  Gmail, backup info).
- **Fájlok:**
  - `frontend/src/app/features/settings/pages/settings.page.ts`
  - `frontend/src/app/features/settings/settings.routes.ts`
- **AC:**
  - [ ] Tab-ok: „Saját" (preferenciák, aktív), „Rendszer" (placeholder
        admin only), „Integrációk" (placeholder admin only).
  - [ ] Admin felhasználó látja az admin tabokat is.

### T-BFE-11 — Komponens-tesztek
- **Cél:** kulcs interakciók tesztelve.
- **Fájlok:**
  - `frontend/src/app/features/family/pages/family-list.page.spec.ts`
  - `frontend/src/app/features/family/components/family-member-form.dialog.spec.ts`
  - `frontend/src/app/features/settings/pages/preferences.page.spec.ts`
- **AC:**
  - [ ] Family list: load() hívódik, kártya render, „Új" gomb dialog-ot nyit.
  - [ ] Form: validáció hibás bemenetnél, mentés sikeres path-on.

---

## Megvalósítási sorrend

```
T-BFE-01 → 02                       (data layer)
       → 03 → 04 → 05               (Family UI)
       → 06                          (Invite)
       → 07 → 08                    (RBAC UI)
       → 09 → 10                    (Settings)
       → 11                          (tesztek)
```

## Epic-DoD

- [ ] Admin a `/family` oldalon teljes CRUD-ot tud csinálni.
- [ ] Meghívás dialog létezik és a backend-del integrált.
- [ ] Child user nem látja a `Family` menüt.
- [ ] Preferenciák lap mentés után toast + a backend tárolja.
- [ ] Magyar UI mindenhol; nincs angol felirat.
- [ ] Vitest tesztek zöldek.

# Epic E — Kereső + Q&A — Frontend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` §9, §10
> - `frontend-structure.md` §8.3 (AI Search UI)
> - `search-strategy.md` §10 (UI integráció)
> - `api-design.md` §16 (Search endpoint)
> - `security-privacy.md` §4 (RBAC — kliens-oldali RBAC hint nem auth, de UX-hez)
>
> **Story-k:** E1–E7 (FE)
> **Fázis:** Fázis 9

---

## Áttekintés

Az AI Search a Family OS központi felhasználói élménye. A felülete
chat-szerű: kérdés, válasz hivatkozott forrásokkal, extracted slot chip-ek
opcionális szűkítéshez. Emellett globális keresősáv a navbar-ban + minden
lista oldal `?q=` paramétert kapja.

## Taskok

### T-EFE-01 — Search API client + DTOs
- **Cél:** generated client a search endpoint-okhoz.
- **Fájlok:**
  - `frontend/src/app/features/search/services/search.api.ts`
  - `frontend/src/app/features/search/models/search.dto.ts`
  - `frontend/src/app/features/search/models/answer-result.dto.ts`
- **AC:**
  - [ ] `POST /api/v1/search` mind az 5 mode támogatva.
  - [ ] Saved searches CRUD.

### T-EFE-02 — `SearchFacade`
- **Cél:** chat-history + jelenlegi query.
- **Fájlok:**
  - `frontend/src/app/features/search/services/search.facade.ts`
- **AC:**
  - [ ] `messages: signal<ChatMessage[]>` (user kérdés és asszisztens válasz
        váltakozva).
  - [ ] `ask(question, mode)` async — push user message → API → push
        answer message.
  - [ ] `clearHistory()`.
  - [ ] **sessionStorage** persistance (LocalOnly elv — nem localStorage).

### T-EFE-03 — AI Search oldal layout
- **Cél:** chat felület.
- **Fájlok:**
  - `frontend/src/app/features/search/pages/search.page.ts`
  - `frontend/src/app/features/search/pages/search.page.html`
- **AC:**
  - [ ] Felső input + mode-select (auto/filter/text/semantic/qa).
  - [ ] Középső scroll-able chat-list.
  - [ ] Lassú válasz indikátor („AI gondolkodik...").
  - [ ] „Új beszélgetés" gomb a fejlécen.

### T-EFE-04 — `ChatMessage` komponens (kérdés + válasz)
- **Cél:** message bubble-ök.
- **Fájlok:**
  - `frontend/src/app/features/search/components/chat-user-message.component.ts`
  - `frontend/src/app/features/search/components/chat-assistant-message.component.ts`
- **AC:**
  - [ ] User message: kérdés szöveg, jobb oldalon.
  - [ ] Assistant: válasz + hivatkozott források mint kártya-csempék.
  - [ ] „Másold" gomb a válaszon.
  - [ ] Markdown render (link-ek aktívak, de XSS sanitized).

### T-EFE-05 — `SourceCitation` kártya
- **Cél:** a Q&A válaszhoz tartozó forrás-csempék.
- **Fájlok:**
  - `frontend/src/app/features/search/components/source-citation.component.ts`
- **AC:**
  - [ ] Ikon (Document / Note / Deadline) + cím + snippet.
  - [ ] Klikkre megnyitja a megfelelő rekordot új tab-on.

### T-EFE-06 — `ExtractedSlotChips`
- **Cél:** a slot-extractor visszaadott szűrőit chip-ként mutatja.
- **Fájlok:**
  - `frontend/src/app/features/search/components/extracted-slot-chips.component.ts`
- **AC:**
  - [ ] Klikkre eltávolítható chip (új query a módosított filter-rel).
  - [ ] Példa: „[Pénzügy] [2025] [Apa]".

### T-EFE-07 — Globális keresősáv a navbar-ban
- **Cél:** bárhonnan elérhető univerzális kereső.
- **Fájlok:**
  - `frontend/src/app/layout/global-search-bar.component.ts`
- **AC:**
  - [ ] Enter → navigálás `/search?q=...&mode=auto`.
  - [ ] Mobile: ikonra koppintásra fullscreen sheet.

### T-EFE-08 — Mode-toggle ikon
- **Cél:** a search input mellett mode kiválasztó.
- **Fájlok:**
  - `frontend/src/app/features/search/components/mode-toggle.component.ts`
- **AC:**
  - [ ] Default `auto`.
  - [ ] Tooltip magyar magyarázat minden mode-ra.

### T-EFE-09 — Filter mode lista render
- **Cél:** ha a backend `mode: 'filter'`-rel válaszol, lista renderelődik
  (nem chat üzenet).
- **Fájlok:**
  - `frontend/src/app/features/search/components/filter-results-list.component.ts`
- **AC:**
  - [ ] Reusable a többi lista oldalon is.
  - [ ] Pagination.

### T-EFE-10 — Mentett keresés UI
- **Cél:** E7 story.
- **Fájlok:**
  - `frontend/src/app/features/search/components/save-search-button.component.ts`
  - `frontend/src/app/features/search/components/saved-searches-panel.component.ts`
- **AC:**
  - [ ] „Mentés" gomb a chat fejlécen → dialog (név megadás).
  - [ ] Dashboard widget használja a saved search-eket.

### T-EFE-11 — Lista-oldalakon `?q=` integráció
- **Cél:** Documents/Notes/Tasks/Deadlines lista oldal megérti a query.
- **Fájlok:**
  - kiegészítés a `documents-list.page.ts`-ben és társaiban.
- **AC:**
  - [ ] `route.queryParams` → `searchFilter.q` mapping.
  - [ ] FTS mód a backend felé.

### T-EFE-12 — Üres állapot magyar copy-val
- **Cél:** „Nincs erre vonatkozó adat..." válasz kezelése.
- **Fájlok:**
  - `frontend/src/app/features/search/components/empty-answer.component.ts`
- **AC:**
  - [ ] Javaslat-szövegek: „Próbáld kevesebb szóval", „Mutatok hasonló
        dokumentumokat" — utóbbira automatikusan fallback semantic search.

### T-EFE-13 — Komponens-tesztek + Playwright E2E
- **Cél:** UC-02 (Q&A) end-to-end.
- **Fájlok:**
  - `frontend/src/app/features/search/pages/search.page.spec.ts`
  - `frontend/e2e/search/qa-flow.spec.ts`
- **AC:**
  - [ ] Mockolt backend válasz → message bubble render.
  - [ ] Playwright: kérdés begépelve → válasz + idézett források
        (mockolt backend).

---

## Megvalósítási sorrend

```
T-EFE-01 → 02                       (data)
       → 03 → 04 → 05 → 06          (chat UI)
       → 07 → 08                    (globális sáv + mode)
       → 09                          (filter render)
       → 10                          (mentett keresés)
       → 11 → 12                    (integráció listákra + empty)
       → 13                          (tesztek)
```

## Epic-DoD

- [ ] AI Search oldal működik chat-szerűen.
- [ ] Q&A válaszok hivatkozott forrás-kártyákkal.
- [ ] Filter mode-ban lista renderel, nem chat.
- [ ] Globális kereső a navbar-ból elérhető.
- [ ] Mentett keresések dashboard widget-en megjelennek.
- [ ] Magyar UI mindenhol.

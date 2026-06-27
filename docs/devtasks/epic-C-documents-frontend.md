# Epic C — Dokumentum-kezelés — Frontend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` §9, §10
> - `frontend-structure.md` §8.2 (Documents oldalak), §2 (folder layout), §9 (megosztott UI)
> - `api-design.md` §7 (Documents endpointok), §1 (konvenciók)
> - `domain-model.md` §1.3, §1.17–1.19 (mit jelenít meg)
> - `security-privacy.md` §4 (RBAC — UI jelzés private rekordon)
> - `architecture.md` §11.1 (upload flow)
>
> **Story-k:** C1 (FE), C2 (FE), C3 (FE), C4 (FE), C5 (FE)
> **Fázis:** Fázis 5

---

## Áttekintés

A documents feature három fő oldala:
- **Lista** (`/documents`) — filter panel + kártya-grid.
- **Upload** (`/documents/upload`) — drag-and-drop multipart upload.
- **Részletek** (`/documents/:id`) — tabs (áttekintés / szöveg / címkék /
  kapcsolódó), suggestion-block, facet szerkesztők.

A feldolgozási státusz real-time (SignalR) frissül.

## Taskok

### T-CFE-01 — Documents API client + DTOs
- **Cél:** generált vagy kézi tipuskönyvtár.
- **Fájlok:**
  - `frontend/src/app/features/documents/services/documents.api.ts`
  - `frontend/src/app/features/documents/models/document.dto.ts`
  - `frontend/src/app/features/documents/models/document-filter.model.ts`
- **AC:**
  - [ ] Minden endpoint (`api-design.md` §7) támogatott.
  - [ ] Multipart upload progress event-támogatás
        (`reportProgress: true`).

### T-CFE-02 — `DocumentsFacade`
- **Cél:** signal-store + akciók.
- **Fájlok:**
  - `frontend/src/app/features/documents/services/documents.facade.ts`
- **AC:**
  - [ ] State: `items`, `filters`, `loading`, `error`, `pagination`.
  - [ ] Akciók: `load`, `setFilter`, `loadDetail`, `upload`, `reprocess`,
        `softDelete`.
  - [ ] SignalR `documentProcessingProgress` event → state-frissítés
        a megfelelő document-en.

### T-CFE-03 — `documents-list.page`
- **Cél:** kártya-grid + pagination + üres állapot.
- **Fájlok:**
  - `frontend/src/app/features/documents/pages/documents-list.page.ts`
  - `frontend/src/app/features/documents/pages/documents-list.page.html`
- **AC:**
  - [ ] „Új feltöltés" gomb a route `/documents/upload`-ra.
  - [ ] Kártya: title, dátum, méret, MIME-ikon, processing-status badge,
        topic-chip-ek.
  - [ ] Üres állapot magyar segítő szöveggel.
  - [ ] Lapozás 50/oldal.

### T-CFE-04 — `document-filter-panel` komponens
- **Cél:** bal oldali (vagy mobile-on dropdown) szűrőpanel.
- **Fájlok:**
  - `frontend/src/app/features/documents/components/document-filter-panel.component.ts`
- **AC:**
  - [ ] Topic-tree szűrés.
  - [ ] Tag multiselect.
  - [ ] `relatedFamilyMember` select.
  - [ ] Dátumtartomány.
  - [ ] „Szűrés törlése" gomb.
  - [ ] Mobile-on egy bottom-sheet-be összecsukva.

### T-CFE-05 — `document-card` komponens
- **Cél:** standalone, újrahasznosítható kártya komponens.
- **Fájlok:**
  - `frontend/src/app/features/documents/components/document-card.component.ts`
- **AC:**
  - [ ] `input.required<DocumentListItemDto>()`.
  - [ ] „Suggestion" jelölő ha `origin === 'AiSuggested'`.
  - [ ] „Private" ikon ha `isPrivate === true`.
  - [ ] „Processing" badge `Pending/Extracting/Analyzing` állapotokban.
  - [ ] Kontextus menü: Megnyit, Letöltés, Szerkesztés, Reprocess, Törlés.

### T-CFE-06 — Upload page: drag-and-drop
- **Cél:** intuitív drag-and-drop UI + file picker fallback.
- **Fájlok:**
  - `frontend/src/app/features/documents/pages/document-upload.page.ts`
  - `frontend/src/app/features/documents/components/dropzone.component.ts`
- **AC:**
  - [ ] Drop event → fájl-lista state-be.
  - [ ] File picker `<input type="file" multiple>` ugyanaz a flow.
  - [ ] Hibás MIME a kliensoldalon is szűrve (gyors feedback).

### T-CFE-07 — Upload progress + dedup warning
- **Cél:** minden fájlra külön progress bar; 409 esetén link a meglévőre.
- **Fájlok:**
  - `frontend/src/app/features/documents/components/upload-progress-item.component.ts`
- **AC:**
  - [ ] HttpEvent progress alapján 0-100%.
  - [ ] 409 → kártya warning állapotba kerül + „Megnyitom a meglévőt" gomb.
  - [ ] 415 → „Nem támogatott fájltípus" magyar üzenet.
  - [ ] Sikeres upload → kártya success + „Megnyitom" link.

### T-CFE-08 — `document-detail.page`
- **Cél:** részletes oldal tab-okkal.
- **Fájlok:**
  - `frontend/src/app/features/documents/pages/document-detail.page.ts`
  - `frontend/src/app/features/documents/components/document-header.component.ts`
- **AC:**
  - [ ] Fejléc: cím, feltöltő, dátum, méret, IsPrivate toggle.
  - [ ] Tab-ok: Áttekintés / Szöveg / Címkék és témák / Kapcsolódók.
  - [ ] Felül `SuggestionBlock` ha vannak nyitott javaslatok.
  - [ ] Real-time refresh SignalR-en.

### T-CFE-09 — Áttekintés tab (summary + facet)
- **Cél:** AI summary + facet (Warranty/Medical/Financial) szerkeszthetően.
- **Fájlok:**
  - `frontend/src/app/features/documents/components/document-overview-tab.component.ts`
  - `frontend/src/app/features/documents/components/warranty-editor.component.ts`
  - `frontend/src/app/features/documents/components/medical-record-editor.component.ts`
  - `frontend/src/app/features/documents/components/financial-record-editor.component.ts`
- **AC:**
  - [ ] Summary doboz: jelzi a model nevét és prompt_version-t kis betűkkel.
  - [ ] Facet form: a Document típusától függően egyik renderelt.
  - [ ] Mentés gomb → PATCH a megfelelő endpointra.

### T-CFE-10 — Szöveg tab (text + manuális korrekció)
- **Cél:** kinyert szöveg megjelenítés + opcionális szerkesztés.
- **Fájlok:**
  - `frontend/src/app/features/documents/components/document-text-tab.component.ts`
- **AC:**
  - [ ] Read-only textarea default; „Szerkesztem" gomb engedélyezi.
  - [ ] „Mentés" → PATCH `/text`, után warning toast: „A keresési index
        és az AI összefoglaló újragenerálódik a háttérben."
  - [ ] „Eredeti visszaállítása" gomb (ha `is_manually_edited === true`).

### T-CFE-11 — Címkék és témák tab
- **Cél:** Tag/Topic multiselect kapcsolás.
- **Fájlok:**
  - `frontend/src/app/features/documents/components/document-tags-tab.component.ts`
- **AC:**
  - [ ] Tag input autocomplete-tel.
  - [ ] Topic tree-select.
  - [ ] AI-javasolt chip-ek sárga sávval, batch-Approve gomb.

### T-CFE-12 — `SuggestionBlock` shared komponens
- **Cél:** újrahasznosítható minden entitás-card-on (Document, Task,
  Deadline list).
- **Fájlok:**
  - `frontend/src/app/shared/ui/suggestion-block.component.ts`
- **AC:**
  - [ ] Sárga sáv „AI javasolta" felirattal.
  - [ ] `Elfogadom mindet` és `Elvetem mindet` batch akciók.
  - [ ] Per-item Approve/Reject is lehetséges.

### T-CFE-13 — PDF előnézet
- **Cél:** dokumentum részleteknél PDF inline.
- **Fájlok:**
  - `frontend/src/app/features/documents/components/pdf-preview.component.ts`
  - `frontend/package.json` `pdfjs-dist` dependency.
- **AC:**
  - [ ] PDF render a worker `pdf.worker.min.js`-szel.
  - [ ] Page-lapozás.
  - [ ] Egyéb MIME (kép) `<img>` tag-gel, TXT `<pre>`-vel.

### T-CFE-14 — Reprocess + Delete UI
- **Cél:** kontextus menüből indítható akciók.
- **Fájlok:**
  - `frontend/src/app/features/documents/components/reprocess-dialog.component.ts`
- **AC:**
  - [ ] Reprocess dialog: melyik lépéseket futtassuk újra (Summarize,
        Extract*, Embed) checkbox-okkal.
  - [ ] Delete megerősítő (Epic A T-AFE confirm-dialog újrahasznosítás).
  - [ ] Hard delete csak admin-nak látszik (RBAC kontroll a UI-on).

### T-CFE-15 — SignalR realtime kapcsolódás dokumentumokra
- **Cél:** processing progress + processed/failed eseményekre frissítés.
- **Fájlok:**
  - `frontend/src/app/features/documents/services/documents-realtime.service.ts`
- **AC:**
  - [ ] `documentProcessingProgress` event → DocumentsFacade-be patch.
  - [ ] `documentProcessed` → toast + lista frissül.
  - [ ] `documentFailed` → toast magyar hibaüzenettel.

### T-CFE-16 — Komponens-tesztek és E2E
- **Cél:** kulcs flow-k.
- **Fájlok:**
  - `frontend/src/app/features/documents/pages/documents-list.page.spec.ts`
  - `frontend/src/app/features/documents/pages/document-upload.page.spec.ts`
  - `frontend/e2e/documents/upload-flow.spec.ts` (Playwright `@e2e`)
- **AC:**
  - [ ] Upload-flow E2E: drop egy PDF → 201 → kártya megjelenik a listán.
  - [ ] Dedup E2E: ugyanaz a PDF újra → warning + a meglévő kártya kiemelve.

---

## Megvalósítási sorrend

```
T-CFE-01 → 02                       (data layer)
       → 03 → 04 → 05               (lista UI)
       → 06 → 07                    (upload)
       → 08 → 09 → 10 → 11          (detail oldal)
       → 12                          (suggestion shared)
       → 13                          (PDF)
       → 14 → 15                    (reprocess + realtime)
       → 16                          (tesztek)
```

## Epic-DoD

- [ ] Adult user feltölt egy PDF-et drag-and-drop-pal.
- [ ] A lista frissül; a kártyán processing-status real-time változik.
- [ ] A részletes oldal mutatja a kinyert szöveget, summary placeholdert
      és facet űrlapot.
- [ ] Dedup esetén informatív warning.
- [ ] Manuális szövegkorrekció működik.
- [ ] Magyar UI mindenhol.
- [ ] Vitest + Playwright tesztek zöldek.

# Review — epic-C-documents-frontend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Jó FE-bontás a legnagyobb felhasználói felületre: lista + filter panel,
drag-and-drop upload progress-szel és dedup-warninggal, tab-os detail
oldal, újrahasznosítható `SuggestionBlock`, SignalR-frissítés, PDF-előnézet.
Az AC-k a tényleges UX-viselkedést írják le, magyar szövegekkel.

## Észrevételek

1. **T-CFE-10 — az AC helyes volt, az implementáció tért el (közepes,
   folyamat):** a task AC-je („read-only textarea default; Szerkesztem
   gomb engedélyezi”) pontosan azt írja le, aminek lennie kéne — a
   megvalósult kód viszont a qa/ui-test-scenarios QA-C4-BUG szerint
   önkizáró feltétellel sosem tölti be a szöveget. A task-AC és a QA-lelet
   együtt egyértelmű javítási utasítást ad — backlog-itemként rögzítendő,
   és a T-CFE-16 tesztkészlete egészüljön ki a szöveg-tab golden path-szal.
2. **T-CFE-09 facet-form: „a Document típusától függően egyik renderelt”
   (kicsi):** a domain-model 4. indoklása szerint egy dokumentumnak
   *több* facetje is lehet (számla + garancia egyszerre) — a UI
   egy-facet feltevése ezzel ütközik. MVP-re az egy-facet elfogadható,
   de mondjuk ki (és a domain-doksi többes-facet állítását jelöljük
   v2-nek).
3. **T-CFE-11 tag-autocomplete** az Epic I `tag-multiselect`
   komponensére támaszkodik — a qa-doksi szerint ilyen komponens még
   nincs (QA-I1-01 🚫); a függőség (I1 → C-FE címke-tab) a sorrend-ábrában
   nem jelenik meg. Jelölendő, különben a C-FE worktree elakad.
4. **T-CFE-13 PDF-előnézet** — a frontend-structure 16. „nagy PDF lassú”
   kockázatát érdemes AC-be emelni (pl. lazy render, max előnézeti
   oldalszám), mert a Pi-deploy (deploy-raspberry-pi.md) klienseknél is
   gyenge eszközök várhatók.

## Verdikt

Végrehajtható; az #1 bug-javítás backlog-ba emelése és a #3
komponens-függőség jelölése a legfontosabb teendő.

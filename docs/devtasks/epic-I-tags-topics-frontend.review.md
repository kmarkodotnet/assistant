# Review — epic-I-tags-topics-frontend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Jó bontás; a két shared form-control (`tag-multiselect`,
`topic-tree-select`) külön taskként, más feature-ökbe integrálással
(T-IFE-08) — pontosan így kell újrahasznosítható komponenst tervezni.

## Észrevételek

1. **Ütemezési ütközés a C-FE-vel (közepes):** a `tag-multiselect`
   (T-IFE-03) a Fázis 11-ben készül, de az Epic C frontend
   (T-CFE-11, Fázis 5) már használná a Documents címke-tabon
   (epic-C-documents-frontend.review.md #3). A qa/ui-test-scenarios
   QA-I1-01 megerősíti, hogy a komponens jelenleg nem létezik. Vagy a
   T-IFE-03 kerül előre a Fázis 5-be, vagy a C-FE címke-tab csúszik a
   Fázis 11-re — a devtasks README sorrend-ábrájában rendezendő.
2. **T-IFE-03 „Új tag létrehozás Enter-rel” (kicsi):** a tag-létrehozás
   Adult-jog (security-privacy 4.1) — child-nál az Enter-létrehozás
   tiltandó; egy szerepkör-AC pótlandó.
3. **T-IFE-05 drag-to-reorder „MVP-ben opcionális”** — jó szűkítés;
   a frontend-structure 8.7 „drag-to-reorder” ígérete ehhez igazítandó.
4. **T-IFE-07 topic mini-dashboard** — a `?topicSlug=` filter-átadás a
   lista-oldalakra egyszerű és jó; a „statisztika: tartalom-szám /
   kategória” viszont új aggregáló endpointot igényelhet, ami az
   api-design.md-ben nincs — vagy kliens-oldali számolás a listákból
   (pontatlan lapozásnál), vagy endpoint-igény jelölendő.

## Verdikt

Végrehajtásra kész; az #1 sorrend-döntés az orchestrátor feladata a
BUILD ütemezésekor.

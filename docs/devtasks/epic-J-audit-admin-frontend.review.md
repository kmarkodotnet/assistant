# Review — epic-J-audit-admin-frontend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Rendben lévő admin-UI bontás: audit-böngésző virtual scroll-lal és
JSON-viewerrel, security-events kiemeléssel, AI-jobs retry/cancel,
queue-stats widget auto-refresh-sel. Az export-confirm („az export az
audit logba is bekerül”) jó transzparencia-részlet.

## Észrevételek

1. **AI providers oldal duplikáció a K epic-kel (közepes):** itt
   `/admin/providers` (T-JFE-08), az Epic K-ban `/settings/ai-providers`
   (T-KFE-03) — ugyanaz a funkció két helyen, két route-on (a megvalósult
   kód a qa-doksi szerint a `/settings/ai-providers`-t hozta létre).
   Egy helyre kell konszolidálni, és a másik taskot törölni; a
   frontend-structure.md 8.10 (Settings) az irányadó.
2. **T-JFE-07 queue-stats auto-refresh 10 mp (kicsi):** a J3 qa-forgatókönyv
   (QA-J3-01) 30 mp-es feliratot említ — a megvalósítás és a task AC
   eltér; egységesítendő (30 mp bőven elég, olcsóbb).
3. **T-JFE-03 user-select szűrő** — a user-lista lekéréséhez a
   `GET /api/v1/user-accounts` admin-endpoint kell; függőség az Epic B
   T-BBE-05-re, jelölendő.
4. A security-events oldal (T-JFE-04) route-ja `/admin/security-events` —
   a qa-doksi route-táblájával egyezik, rendben.

## Verdikt

Végrehajtásra kész; az #1 konszolidáció az orchestrátor döntése a Fázis
12 indításakor.

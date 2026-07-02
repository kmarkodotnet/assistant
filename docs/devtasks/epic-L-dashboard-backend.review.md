# Review — epic-L-dashboard-backend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Kompakt, jó epic: egyetlen aggregált endpoint párhuzamos query-kkel,
explicit performance-budget (p95 < 200 ms) és dedikált perf-teszt 5 év
szimulált adaton (Bogus) — a database-schema.md 8. méretezési táblájára
építve. A T-LBE-05 helyesen csak integrációt ellenőriz (a SavedSearch az
Epic E-ben születik), nem duplikál.

## Észrevételek

1. **SavedSearch-függőség prioritás-inverziója (kicsi):** a dashboard
   (L1, Must) a `savedSearches` mezőt is adja, miközben az E7 Could —
   ha az E7 kiesik az MVP-vágásból, a DTO-mező és a widget üresen
   marad. A mvp-backlog.review.md #3 szerint döntés kell (E7 emelése
   vagy a widget üres-állapotúra tervezése) — a T-LBE-01 AC-je
   készüljön fel a `savedSearches: []` esetre.
2. **T-LBE-02 párhuzamos query-k egy DbContext-en (kicsi, technikai):**
   „4 párhuzamos `Task<>`” — az EF Core DbContext nem thread-safe;
   párhuzamos query-khez query-nként külön context (IDbContextFactory)
   kell. Az AC-be ez az implementációs kényszer kívánkozik, különben a
   sonnet az első futásnál `InvalidOperationException`-t kap.
3. **T-LBE-04 child-szűrés** — ismét a lezáratlan child-politikára épül
   (security-privacy.review.md #1); függőségként jelölendő.
4. **T-LBE-03 `Cache-Control: private, max-age=30`** — jó, olcsó
   optimalizáció; a FE 5 perces auto-refresh-ével (T-LFE-02) együtt
   konzisztens.

## Verdikt

Végrehajtásra kész; a #2 technikai megjegyzés AC-be emelése ajánlott.

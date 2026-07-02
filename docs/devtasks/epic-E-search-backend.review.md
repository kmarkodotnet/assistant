# Review — epic-E-search-backend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Jó bontás a hibrid keresőre: a rétegek (filter/FTS/vector) külön
handler-ek, RRF és anti-hallucination guard önálló, tesztelhető taskok,
és a search-strategy.md 7. szakaszának példakérdései integration-tesztté
válnak (T-EBE-19) — ez utóbbi kifejezetten jó minta. A `SavedSearch`
entitás (T-EBE-18) végre gazdát kapott — a database-schema.md /
domain-model.md ehhez igazítandó (domain-model.review.md #2).

## Hibák / észrevételek

1. **T-EBE-14: „Hangfire `RateLimitOptions`” (kicsi, de zavaró):** a
   rate-limitingnek semmi köze a Hangfire-hez — a helyes eszköz a
   `Microsoft.AspNetCore.RateLimiting` middleware (a security-privacy.md
   9.7 helyesen ezt írja). Javítandó, mert a sonnet szó szerint követné.
2. **T-EBE-04 feloldja a slot-sorrend ellentmondást — szinkron kell
   (pozitív):** itt a slot-extraction külön (retrieval előtti) LLM-hívás —
   ez a search-strategy.md 4.1 vs. 5.3 ellentmondásának (search-strategy.review.md #2)
   egyik irányú lezárása; a search-strategy.md 5.3 frissítendő.
3. **T-EBE-02 audit: minden query `AiCall`-ként (kicsi):** a filter-mód
   nem AI-hívás — kereséseket `AiCall` action alatt naplózni félrevezető
   a security-eventek szűrésénél. Javasolt külön `Search` audit-action
   vagy csak a qa/semantic módok naplózása AiCall-ként.
4. **Kisebb pontok:**
   - T-EBE-01 fájlnév-elgépelés: „`Source citation.cs`” (szóköz).
   - T-EBE-17 revision-hash (`MAX(updated_utc)`) nem érzékeli a
     törlést/insertet azonos timestamp-nél — MVP-re elég, de a
     „bármely insert/update → invalidate” AC-vel együtt kétszeres
     mechanizmus; egyiket válasszuk.
   - A 429-es válasz UX-oldali kezelése (mi történik a 11. kérdésnél)
     sem itt, sem a FE-fájlban nincs lefedve — egy AC a FE-be kívánkozik.

## Verdikt

Végrehajtásra kész; az #1 javítása kötelező, a #2–#3 doksi/naming
szinkron.

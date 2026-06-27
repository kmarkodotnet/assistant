# Epic E — Kereső + Q&A — Backend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` (FULL — baseline)
> - `search-strategy.md` (FULL)
> - `api-design.md` §16 (Search), §1 konvenciók
> - `ai-pipeline.md` §4.6 (Q&A prompt template)
> - `domain-model.md` §1.3, §1.5, §1.6, §1.9 (forrásentitások)
> - `database-schema.md` §4.5–4.8, §4.12, §1.3 (FTS config)
> - `security-privacy.md` §4 (RBAC a kereső-szűréshez)
> - `architecture.md` §4.1 (`ISemanticSearchService`, `IQuestionAnswerService`)
> - ADR-0001 (embedding modell + dimenzió konzisztencia)
>
> **Story-k:** E1–E7
> **Fázis:** Fázis 9

---

## Áttekintés

Egyetlen `POST /api/v1/search` endpoint öt módban: `filter`, `text`,
`semantic`, `qa`, `auto`. A három retrieval rétegre (strukturált / FTS /
pgvector) épülő hibrid kereső + LLM válasz-szintézis.

Központi elv: **filter módban LLM nem hívódik** (gyors, biztos); a többi
módban LLM csak hivatkozott chunkból dolgozhat (anti-hallucination
validáció).

## Taskok

### T-EBE-01 — Search common DTO-k és request/response
- **Cél:** közös típusok.
- **Fájlok:**
  - `src/FamilyOs.Application/Search/Dtos/SearchRequest.cs`
  - `src/FamilyOs.Application/Search/Dtos/SearchResponse.cs`
  - `src/FamilyOs.Application/Search/Dtos/SearchHit.cs`,
    `SearchFacets.cs`, `Source citation.cs`, `AnswerResult.cs`.
- **AC:**
  - [ ] Pontosan az `api-design.md` §16.1 szerint.
  - [ ] `mode` enum: `Auto`, `Filter`, `Text`, `Semantic`, `Qa`.

### T-EBE-02 — `SearchCommand` + dispatcher
- **Cél:** mode-routing.
- **Fájlok:**
  - `src/FamilyOs.Application/Search/SearchCommand.cs`
  - `src/FamilyOs.Application/Search/SearchHandler.cs`
- **AC:**
  - [ ] `Auto` mód → `IntentClassifier` futtatás, majd a megfelelő
        handler.
  - [ ] Audit log: minden query egy `AiCall` (model name, mode, latency,
        hit count — **NEM** a query teljes szövege).

### T-EBE-03 — `IntentClassifier` (szabály-alapú)
- **Cél:** magyar kérdés-mintázatokra heurisztika.
- **Fájlok:**
  - `src/FamilyOs.Application/Search/Intent/IntentClassifier.cs`
- **AC:**
  - [ ] `Filter` / `Lookup` / `Find` / `Summarize` osztályok.
  - [ ] Magyar kulcsszó-szabályok: „összes/minden/mutasd" → filter;
        „mikor/hány/melyik dátum" → lookup; „hol van/hol találom" → find;
        „mit döntöttünk/foglald össze" → summarize.
  - [ ] Konfidencia < 0.55 → mind filter mind hybrid futtatás párhuzamosan
        (vagy a `SearchHandler`-ben jelölő).

### T-EBE-04 — Slot extractor (egyszerű LLM hívás)
- **Cél:** dátumtartomány, családtag, kategória kinyerése.
- **Fájlok:**
  - `src/FamilyOs.Application/Search/Intent/SlotExtractor.cs`
  - `src/FamilyOs.Infrastructure.Ai/Prompts/extract-search-slots.v1.txt`
- **AC:**
  - [ ] Magyar prompt, JSON output (date range, familyMember, category).
  - [ ] FamilyMember név-egyezés a `FamilyMember.DisplayName`-re.

### T-EBE-05 — `FilterSearchHandler`
- **Cél:** tisztán strukturált — LLM nem hívódik.
- **Fájlok:**
  - `src/FamilyOs.Application/Search/Handlers/FilterSearchHandler.cs`
- **AC:**
  - [ ] EF Core LINQ a Documents / FinancialRecords / Deadlines / Tasks /
        MedicalRecords táblákra.
  - [ ] `entityTypes` query alapján multi-table.
  - [ ] Pagination + facets aggregálás.
  - [ ] p95 < 50 ms.

### T-EBE-06 — `FtsSearchHandler`
- **Cél:** magyar `hungarian_unaccent` configon `tsvector`.
- **Fájlok:**
  - `src/FamilyOs.Application/Search/Handlers/FtsSearchHandler.cs`
- **AC:**
  - [ ] `websearch_to_tsquery` a usery-szövegre.
  - [ ] `ts_headline` snippet generálva.
  - [ ] Document + Note UNION ALL.
  - [ ] RBAC szűrő WHERE-ben (`!IsPrivate OR createdByUserAccountId = current`).

### T-EBE-07 — `ISemanticSearchService` + pgvector implementáció
- **Cél:** HNSW vektor lekérdezés.
- **Fájlok:**
  - `src/FamilyOs.Application/Abstractions/Ai/ISemanticSearchService.cs`
  - `src/FamilyOs.Infrastructure.Ai/Search/SemanticSearchService.cs`
- **AC:**
  - [ ] Query embedding: `IEmbedder.EmbedAsync` (Epic D).
  - [ ] DocumentChunk + NoteChunk együtt, cosine similarity.
  - [ ] `embedding_model` szűrő (vegyes vektor nem hasonlítható).
  - [ ] p95 < 80 ms 75k vektorra.

### T-EBE-08 — `SemanticSearchHandler`
- **Cél:** semantic mode endpoint logika.
- **Fájlok:**
  - `src/FamilyOs.Application/Search/Handlers/SemanticSearchHandler.cs`
- **AC:**
  - [ ] Hits group-by document, chunk → snippet a top score-os chunkból.
  - [ ] RBAC szűrés (defenzív, mert már a vektor query is szűr).

### T-EBE-09 — RRF (Reciprocal Rank Fusion) helper
- **Cél:** `k=60` hibrid fúzió.
- **Fájlok:**
  - `src/FamilyOs.Application/Search/Rrf/ReciprocalRankFusion.cs`
- **AC:**
  - [ ] Unit teszt determinisztikus inputon.
  - [ ] Exact-match boost (2× súly).

### T-EBE-10 — `HybridSearchHandler`
- **Cél:** strukturált + FTS + vector → RRF.
- **Fájlok:**
  - `src/FamilyOs.Application/Search/Handlers/HybridSearchHandler.cs`
- **AC:**
  - [ ] Párhuzamos lekérdezés mindhárom rétegen.
  - [ ] Boost-szabályok alkalmazva (search-strategy.md §8.1).
  - [ ] Facets aggregálás (topic, év, family member).

### T-EBE-11 — `IQuestionAnswerService` + `OllamaQuestionAnswerer`
- **Cél:** Q&A LLM válasz hivatkozott forrásokkal.
- **Fájlok:**
  - `src/FamilyOs.Application/Abstractions/Ai/IQuestionAnswerService.cs`
  - `src/FamilyOs.Infrastructure.Ai/Tasks/OllamaQuestionAnswerer.cs`
  - `src/FamilyOs.Infrastructure.Ai/Prompts/qa-magyar.v1.txt`
- **AC:**
  - [ ] Prompt: ai-pipeline.md §4.6 (csak idézett forrásokból, „Nincs erre
        vonatkozó adat..." fallback).
  - [ ] Output: `AnswerResult { answer, citedSourceIds[], confidence }`.

### T-EBE-12 — Anti-hallucination validáció
- **Cél:** ha a válasz hivatkozott ID nincs a retrieved chunk-okban → fallback.
- **Fájlok:**
  - `src/FamilyOs.Application/Search/Qa/HallucinationGuard.cs`
- **AC:**
  - [ ] Idézett ID-k subset-je a retrieved-nek; nem → válasz visszaesik
        a „Nincs erre vonatkozó adat..."-ra.
  - [ ] Fuzzy validáció: kinyert dátumok regex check (`\d{4}-\d{2}-\d{2}`),
        hogy szerepel-e a chunkban.

### T-EBE-13 — `QaHandler`
- **Cél:** Q&A mode end-to-end.
- **Fájlok:**
  - `src/FamilyOs.Application/Search/Handlers/QaHandler.cs`
- **AC:**
  - [ ] Hibrid retrieval (top 20) → Q&A LLM → validáció.
  - [ ] Audit log: `AiCall`.

### T-EBE-14 — Search rate limiting
- **Cél:** Q&A és Semantic mode: 10 req/min/user.
- **Fájlok:**
  - `src/FamilyOs.Api/RateLimiting/SearchRateLimitPolicy.cs`
- **AC:**
  - [ ] Hangfire `RateLimitOptions` user-szinten.
  - [ ] 429 magyar ProblemDetails.

### T-EBE-15 — Search endpoint
- **Cél:** `POST /api/v1/search`.
- **Fájlok:**
  - `src/FamilyOs.Api/Endpoints/SearchModule.cs`
- **AC:**
  - [ ] `RequireAuthenticated`.
  - [ ] Rate limit a Q&A / semantic-re.
  - [ ] 200 + SearchResponse mode-specifikus formátumban.

### T-EBE-16 — Query embedding LRU cache
- **Cél:** ismétlődő kérdésekre azonnali találat.
- **Fájlok:**
  - `src/FamilyOs.Infrastructure.Ai/Caching/QueryEmbeddingCache.cs`
- **AC:**
  - [ ] `IMemoryCache` LRU, ~500 entry, 1 órás TTL.
  - [ ] Cache key: a query lower-cased + trimmed.

### T-EBE-17 — Q&A cache
- **Cél:** 15 perces cache azonos (question + user + db-revision-hash).
- **Fájlok:**
  - `src/FamilyOs.Application/Search/Qa/QaCacheKeyBuilder.cs`
  - `src/FamilyOs.Application/Search/Qa/QaCache.cs`
- **AC:**
  - [ ] DB revision hash: legutóbbi `document.updated_utc` MAX vagy
        ehhez hasonló olcsó indikátor.
  - [ ] Bármely Document/Note insert/update → cache invalidate.

### T-EBE-18 — `SavedSearch` entity + CRUD endpointok (E7)
- **Cél:** dashboard widget alapja.
- **Fájlok:**
  - `src/FamilyOs.Domain/Entities/SavedSearch.cs`
  - migráció.
  - `src/FamilyOs.Application/Search/Saved/*.cs`
  - `src/FamilyOs.Api/Endpoints/SearchModule.cs` kiegészítés.
- **AC:**
  - [ ] CRUD + per-user szűrés.

### T-EBE-19 — Integration tesztek a 7 példa kérdésre
- **Cél:** `search-strategy.md` §7 mintái → helyes routing + válasz
  (stub provider determinisztikus válasz-fixture-rel).
- **Fájlok:**
  - `tests/FamilyOs.Api.IntegrationTests/Search/ExampleQuestionsTests.cs`
- **AC:**
  - [ ] „Mikor jár le az autó kötelező?" → lookup → válasz dátummal +
        hivatkozott Document.
  - [ ] „Melyek a kifizetetlen számlák?" → filter → lista LLM nélkül.
  - [ ] „Mutasd az összes 2025-ös egészségügyi dokumentumot." → filter
        + topic+dátum szűrés.
  - [ ] További 4 példa az §7-ből.

### T-EBE-20 — Performance tesztek
- **Cél:** SLO ellenőrzés.
- **Fájlok:**
  - `tests/FamilyOs.Performance.Tests/SearchLatencyTests.cs`
- **AC:**
  - [ ] FTS < 150 ms p95 5k dokumentumon.
  - [ ] Vektor < 80 ms p95 75k chunkon.
  - [ ] Q&A teljes < 3 s p95 valódi Ollama-val (nightly).

---

## Megvalósítási sorrend

```
T-EBE-01 → 02 → 03 → 04           (DTO + dispatcher + intent + slot)
       → 05 → 06 → 07 → 08         (három alap retrieval mód)
       → 09 → 10                   (hibrid + RRF)
       → 11 → 12 → 13              (Q&A + anti-hallu)
       → 14 → 15                   (rate limit + endpoint)
       → 16 → 17                   (cache)
       → 18                         (saved searches)
       → 19 → 20                   (tesztek)
```

## Epic-DoD

- [ ] Az `search-strategy.md` §7 mind a 7 példa kérdésére helyes
      eredményt ad determinisztikus stub providerrel.
- [ ] Filter mode LLM nélkül; Q&A mód hivatkozott forrásokkal.
- [ ] Anti-hallucination guard zöld teszttel ellenőrizve.
- [ ] Rate limit aktív.
- [ ] Performance smoke zöld.
- [ ] `code-reviewer` jóváhagyta a Q&A privacy szempontból (audit hash,
      no prompt szivárgás).

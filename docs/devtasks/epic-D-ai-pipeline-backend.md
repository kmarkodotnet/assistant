# Epic D — AI pipeline — Backend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` (FULL — baseline, különös figyelem §7 logolás és §16 titkok)
> - `ai-pipeline.md` (FULL — az epic vezérlő doksija)
> - `architecture.md` (FULL — különösen §3.4 Infra.Ai, §4 interfészek, §5 provider, §6 queue, §11.1 orchestráció)
> - ADR-0001 (pgvector — FULL)
> - ADR-0002 (Tesseract — FULL)
> - `security-privacy.md` §8 (AI privacy + LocalOnly kapu), §5.3 (mit NEM logolunk)
> - `domain-model.md` §1.3–1.5 (Document/Text/Chunk), §1.6 (Summary), §1.7–1.8 (Tag/Topic), §1.10 (Task), §1.11 (Deadline), §1.15 (AiProcessingJob), §1.17–1.19 (facets)
> - `database-schema.md` §4.5–4.10, §4.13–4.14, §4.16, §4.18–4.20, §2 (enumok)
> - `frontend-structure.md` §6 (SignalR client) — csak T-DBE-26 kontextushoz
>
> **Story-k:** D1–D11
> **Fázis:** Fázis 6 (OCR), Fázis 7 (AI infra + Hangfire), Fázis 8 (tartalmi pipeline — 3 párhuzamos worktree)

---

## Áttekintés és alcsomagok

Az epic két logikai alcsomagra bomlik a `implementation-context-matrix.md`
szerint:

- **D-Infra (T-DBE-01..09)** — AI provider absztrakció, Hangfire integráció,
  szövegkinyerés (Tesseract + PdfPig), nyelvdetektálás. Egy worktree.
- **D-Tartalom (T-DBE-10..25)** — Summary / Classify / Deadline / Task /
  Facet / Embedding + Orchestráció + SignalR. **Három párhuzamos worktree**
  (`feature/ai-summary`, `feature/ai-extract`, `feature/ai-embed`).

A `code-reviewer` agent (opus) **kötelező** minden privacy-érzékeny PR-hoz
(provider hívás, prompt log kezelés).

---

## D-Infra alcsomag (Fázis 6-7)

### T-DBE-01 — `IAiProvider` és `IEmbedder` interfészek
- **Cél:** core absztrakció rögzítése (kontraktus).
- **Fájlok:**
  - `src/FamilyOs.Application/Abstractions/Ai/IAiProvider.cs`
  - `src/FamilyOs.Application/Abstractions/Ai/IEmbedder.cs`
  - `src/FamilyOs.Application/Abstractions/Ai/AiPrompt.cs`,
    `AiCompletion.cs`, `AiCapabilities.cs`.
  - `docs/contracts/ai-provider.md` (kontraktus rögzítés).
- **AC:**
  - [ ] A signatures pontosan az `architecture.md` §4.1 szerint.
  - [ ] `AiCapabilities` flag-ek: `ToolUse`, `JsonMode`, `Streaming`.
  - [ ] Kontraktus doksiban: a Hibrid/Provider/Embedding viselkedési
        szerződés rögzítve.

### T-DBE-02 — Magyar prompt-template infrastruktúra
- **Cél:** template-ek mint embedded resource + `prompt_version` követés.
- **Fájlok:**
  - `src/FamilyOs.Infrastructure.Ai/Prompts/*.txt` (embedded resource).
  - `src/FamilyOs.Infrastructure.Ai/Prompts/PromptTemplate.cs` (loader +
    placeholder-replace).
  - `src/FamilyOs.Infrastructure.Ai/Prompts/PromptCatalog.cs` (id ↔ filename).
- **AC:**
  - [ ] A közös rendszer-prompt sysprefix (ai-pipeline.md §4.0) magyar
        nyelven, csak JSON output kérve, no-hallucination kötelező.
  - [ ] `prompt_version` minden template fájlnévben.

### T-DBE-03 — `OllamaAiProvider`
- **Cél:** alapértelmezett lokális provider.
- **Fájlok:**
  - `src/FamilyOs.Infrastructure.Ai/Providers/OllamaAiProvider.cs`
  - `src/FamilyOs.Infrastructure.Ai/Providers/OllamaHttpClient.cs`
- **AC:**
  - [ ] `HttpClient` `BaseUrl = http://ollama:11434`.
  - [ ] `POST /api/chat` Ollama API-ra.
  - [ ] Streaming mód optional (MVP-ben batch).
  - [ ] 503 / timeout esetén `AiProviderUnavailableException`.
  - [ ] Connection retry (Polly): 3× exponenciális.

### T-DBE-04 — `OllamaEmbedder`
- **Cél:** `nomic-embed-text:v1.5` modell + 768 dim.
- **Fájlok:**
  - `src/FamilyOs.Infrastructure.Ai/Providers/OllamaEmbedder.cs`
- **AC:**
  - [ ] Batch embed (több text egyszerre, ha támogatott).
  - [ ] `ModelName` és `Dimensions` property-k.
  - [ ] Verziókövetés: `embedding_model = "nomic-embed-text:v1.5"`.

### T-DBE-05 — `AiProviderFactory` + PrivacyMode kapu
- **Cél:** **kódba égetett biztonsági kapu** (`security-privacy.md` §8.1).
- **Fájlok:**
  - `src/FamilyOs.Infrastructure.Ai/Providers/AiProviderFactory.cs`
  - `src/FamilyOs.Infrastructure.Ai/Providers/AiProviderNotAllowedException.cs`
  - `appsettings.json` `Ai` szekció.
- **AC:**
  - [ ] `LocalOnly` mód: csak `ollama` provider visszaadható; minden más
        request → `AiProviderNotAllowedException` + audit log.
  - [ ] `HybridAllowed` és `AnyProvider` *létezik a típusban*, de MVP-ben
        a kódban explicit `throw new NotImplementedException("Hybrid mode is post-MVP.")`.
  - [ ] Privacy assertion teszt: mocked `HttpClient` panaszt emel ha
        bárhova más URL-re menne, mint `ollama` host.

### T-DBE-06 — Hangfire setup PostgreSQL storage-szel
- **Cél:** durable queue infrastruktúra.
- **Fájlok:**
  - `src/FamilyOs.Infrastructure/Hangfire/HangfireSetup.cs`
  - `src/FamilyOs.Workers/Program.cs` kiegészítés (Hangfire server).
  - `src/FamilyOs.Api/Program.cs` (csak enqueue képesség, nem worker).
  - migráció: a `Hangfire.PostgreSql` saját sémát hoz létre (`hangfire`).
- **AC:**
  - [ ] `hangfire` séma külön a `app`-tól.
  - [ ] Worker host indul, `/hangfire` admin dashboard auth filter-rel
        (csak Admin).
  - [ ] 4 worker concurrency default (Ollama szekvenciális I/O miatt).

### T-DBE-07 — `AiProcessingJob` entity + repo
- **Cél:** domain-szintű AI queue tábla.
- **Fájlok:**
  - `src/FamilyOs.Domain/Entities/AiProcessingJob.cs`
  - `src/FamilyOs.Domain/Enums/AiJobType.cs`, `JobTargetType.cs`,
    `JobStatus.cs`.
  - `src/FamilyOs.Infrastructure/Persistence/Configurations/AiProcessingJobConfiguration.cs`
  - migráció: `ai_processing_job` tábla.
  - `src/FamilyOs.Application/Common/Ai/IAiProcessingJobRepository.cs`
  - `src/FamilyOs.Infrastructure/Persistence/Repositories/AiProcessingJobRepository.cs`
- **AC:**
  - [ ] `database-schema.md` §4.16 séma pontos megvalósítása.
  - [ ] Exponenciális backoff számolás helper-ben:
        `next_attempt_utc = now + min(60s * 2^attempt, 6h)`.

### T-DBE-08 — `AiJobScheduler` recurring (BackgroundService)
- **Cél:** 10 mp-enként `Queued` AI jobokra → Hangfire enqueue.
- **Fájlok:**
  - `src/FamilyOs.Workers/Services/AiJobScheduler.cs`
  - `src/FamilyOs.Workers/Services/AiJobExecutor.cs` (Hangfire job runner).
- **AC:**
  - [ ] `SELECT FOR UPDATE SKIP LOCKED` (vagy advisory lock) konkurens
        scheduler ellen.
  - [ ] OnStarted catch-up: a `Queued` + `Failed AND next_attempt_utc<=now`
        sorokat felveszi.
  - [ ] AiJobExecutor idempotens (lásd ai-pipeline.md §6.2).

### T-DBE-09 — Tesseract OCR + PdfPig kompozit text extractor (D3)
- **Cél:** szövegkinyerés MVP-pipeline első lépéseként.
- **Fájlok:**
  - `src/FamilyOs.Application/Abstractions/Ai/IDocumentTextExtractor.cs`
  - `src/FamilyOs.Infrastructure.Ai/Extraction/PdfTextLayerExtractor.cs`
  - `src/FamilyOs.Infrastructure.Ai/Extraction/TesseractOcrExtractor.cs`
  - `src/FamilyOs.Infrastructure.Ai/Extraction/CompositeDocumentTextExtractor.cs`
  - `src/FamilyOs.Workers/Services/ExtractTextJobRunner.cs`
  - `docker/workers.Dockerfile`: `apt-get install tesseract-ocr
    tesseract-ocr-hun tesseract-ocr-eng`.
- **AC:**
  - [ ] PDF text-layer first (PdfPig); ha < 100 char vagy < 80% nyomtatható,
        Tesseract fallback.
  - [ ] Kép → közvetlenül Tesseract.
  - [ ] `DocumentText` upsert (1:1 a Document-re).
  - [ ] Üres OCR → `Document.processing_status = Failed`.
  - [ ] Performance smoke: 2 MB PDF < 30 s.

### T-DBE-10 — Nyelvdetektor (D4)
- **Cél:** lokális, nem AI provider hívás.
- **Fájlok:**
  - `src/FamilyOs.Application/Abstractions/Ai/ILanguageDetector.cs`
  - `src/FamilyOs.Infrastructure.Ai/Lang/NTextCatLanguageDetector.cs`
  - `src/FamilyOs.Workers/Services/DetectLanguageJobRunner.cs`
- **AC:**
  - [ ] Magyar 1000+ char szövegre 95%+ helyes detekció.
  - [ ] Eredmény: `Document.Language` + `DocumentText.LanguageDetected`.

---

## D-Tartalom alcsomag (Fázis 8 — 3 párhuzamos worktree)

### Worktree 1: `feature/ai-summary` (T-DBE-11..13)

### T-DBE-11 — `DocumentSummary` entity + repo + idempotens upsert
- **Cél:** új summary insert + régi `IsCurrent=false`.
- **Fájlok:**
  - `src/FamilyOs.Domain/Entities/DocumentSummary.cs`
  - Configuration.
  - migráció.
- **AC:**
  - [ ] Partial UNIQUE `(document_id) WHERE is_current = true`.
  - [ ] Tranzakcióban: régi `IsCurrent = false`, új insert `IsCurrent = true`.

### T-DBE-12 — `IDocumentSummarizer` + `OllamaDocumentSummarizer`
- **Cél:** D5 magyar összefoglaló.
- **Fájlok:**
  - `src/FamilyOs.Application/Abstractions/Ai/IDocumentSummarizer.cs`
  - `src/FamilyOs.Infrastructure.Ai/Tasks/OllamaDocumentSummarizer.cs`
  - `src/FamilyOs.Infrastructure.Ai/Prompts/summarize.v1.txt`
- **AC:**
  - [ ] Prompt: ai-pipeline.md §4.2.
  - [ ] Output: 3-5 magyar mondat.
  - [ ] Hosszú szövegre (>16k char) map-reduce: chunk-szintű minisummary
        → meta-summary.

### T-DBE-13 — `SummarizeJobRunner`
- **Cél:** worker, ami a `JobType=Summarize` AI jobokat futtatja.
- **Fájlok:**
  - `src/FamilyOs.Workers/Services/SummarizeJobRunner.cs`
- **AC:**
  - [ ] Idempotens újrafutás (új `IsCurrent=true` insert tranzakcióban).
  - [ ] Audit log: `AiCall` + prompt hash + hossz (NEM a teljes prompt).

---

### Worktree 2: `feature/ai-extract` (T-DBE-14..20)

### T-DBE-14 — `Tag`, `Topic`, `DocumentTag`, `DocumentTopic` entitások
- **Cél:** classify outputjának fogadására előkészület (ha az Epic I-vel
  átfedés van, ezt csak validáljuk).
- **Fájlok:**
  - `src/FamilyOs.Domain/Entities/Tag.cs`, `Topic.cs`, `DocumentTag.cs`,
    `DocumentTopic.cs`
  - Configurations.
- **AC:**
  - [ ] Lásd domain-model.md §1.7, §1.8 + database-schema.md §4.9–§4.11.

### T-DBE-15 — `IDocumentClassifier` + `OllamaDocumentClassifier`
- **Cél:** D6 — topic + tag + facet típus.
- **Fájlok:**
  - `src/FamilyOs.Application/Abstractions/Ai/IDocumentClassifier.cs`
  - `src/FamilyOs.Infrastructure.Ai/Tasks/OllamaDocumentClassifier.cs`
  - `src/FamilyOs.Infrastructure.Ai/Prompts/classify.v1.txt`
- **AC:**
  - [ ] Topic-feloldás: a meglévő taxonómiához igazít, **új topic-ot
        NEM hoz létre**.
  - [ ] Tag: új tag létrehozható (lapos, olcsó), `usage_count` növelve.
  - [ ] Facet enum (`Warranty`/`Medical`/`Financial`/`null`) visszaadva.
  - [ ] JSON-séma validáció a kimenetre.

### T-DBE-16 — `IDeadlineExtractor` + `OllamaDeadlineExtractor`
- **Cél:** D7 határidő-kivonás.
- **Fájlok:**
  - `src/FamilyOs.Application/Abstractions/Ai/IDeadlineExtractor.cs`
  - `src/FamilyOs.Infrastructure.Ai/Tasks/OllamaDeadlineExtractor.cs`
  - `src/FamilyOs.Infrastructure.Ai/Prompts/extract-deadlines.v1.txt`
- **AC:**
  - [ ] Prompt: ai-pipeline.md §4.3 + mai dátum injektálva.
  - [ ] Output: `DeadlineSuggestion[]`.
  - [ ] Csak jövőbeli (>= today) dátumok.
  - [ ] Dedup: `(source_document_id, title, due_date)` triple-re skip.

### T-DBE-17 — `ITaskExtractor` + `OllamaTaskExtractor`
- **Cél:** D8 feladat-kivonás.
- **Fájlok:**
  - `src/FamilyOs.Application/Abstractions/Ai/ITaskExtractor.cs`
  - `src/FamilyOs.Infrastructure.Ai/Tasks/OllamaTaskExtractor.cs`
  - `src/FamilyOs.Infrastructure.Ai/Prompts/extract-tasks.v1.txt`
  - `src/FamilyOs.Application/Common/FamilyMemberResolver.cs` (hint→ID).
- **AC:**
  - [ ] Prompt: ai-pipeline.md §4.4 + family member lista.
  - [ ] `assignedToHint` resolvolva a `FamilyMember` listára; nem
        egyezésre `null`.
  - [ ] `Status = Suggested`, `Origin = AiSuggested`.

### T-DBE-18 — Facet entitás-kinyerés (Warranty)
- **Cél:** D9 első facet.
- **Fájlok:**
  - `src/FamilyOs.Application/Abstractions/Ai/IWarrantyExtractor.cs`
  - `src/FamilyOs.Infrastructure.Ai/Tasks/OllamaWarrantyExtractor.cs`
  - `src/FamilyOs.Infrastructure.Ai/Prompts/extract-warranty.v1.txt`
- **AC:**
  - [ ] Prompt: ai-pipeline.md §4.5.
  - [ ] `Warranty` insert idempotens (UNIQUE `DocumentId`).

### T-DBE-19 — Facet entitás-kinyerés (MedicalRecord)
- **Cél:** D9 második facet — speciális family_member_id követelmény.
- **Fájlok:**
  - `src/FamilyOs.Application/Abstractions/Ai/IMedicalRecordExtractor.cs`
  - `src/FamilyOs.Infrastructure.Ai/Tasks/OllamaMedicalRecordExtractor.cs`
  - `src/FamilyOs.Infrastructure.Ai/Prompts/extract-medical.v1.txt`
- **AC:**
  - [ ] Ha `family_member_id` nem feloldható biztosan,
        `MedicalRecord` **NEM** jön létre — javaslat-blokkban marad
        megerősítésre (lásd ai-pipeline.md §3.8).
  - [ ] `IsPrivate = true` default.

### T-DBE-20 — Facet entitás-kinyerés (FinancialRecord)
- **Cél:** D9 harmadik facet.
- **Fájlok:**
  - `src/FamilyOs.Application/Abstractions/Ai/IFinancialRecordExtractor.cs`
  - `src/FamilyOs.Infrastructure.Ai/Tasks/OllamaFinancialRecordExtractor.cs`
  - `src/FamilyOs.Infrastructure.Ai/Prompts/extract-financial.v1.txt`
- **AC:**
  - [ ] `IsPaid` és `RecurrencePeriod` kinyerése.

---

### Worktree 3: `feature/ai-embed` (T-DBE-21..23)

### T-DBE-21 — `DocumentChunk` entity + pgvector mapping
- **Cél:** embedding tárolás táblába.
- **Fájlok:**
  - `src/FamilyOs.Domain/Entities/DocumentChunk.cs`
  - `Pgvector.EntityFrameworkCore` használat (`HasColumnType("vector(768)")`).
  - Configuration.
  - migráció: HNSW index (`m=16, ef_construction=64`).
- **AC:**
  - [ ] UNIQUE `(document_id, chunk_index)`.
  - [ ] `embedding_model` szűrő mező.

### T-DBE-22 — `EmbeddingChunker` domain service
- **Cél:** bekezdés-határos chunkolás, max 800 token, ~100 overlap.
- **Fájlok:**
  - `src/FamilyOs.Domain/Services/EmbeddingChunker.cs`
- **AC:**
  - [ ] Unit teszt fixture szövegekre (rövid, hosszú, code-block-szerű).
  - [ ] Tokenizáció heurisztika: magyar ~1.4 token/szó.

### T-DBE-23 — `EmbedJobRunner`
- **Cél:** D10 — chunkolás + embedding insert.
- **Fájlok:**
  - `src/FamilyOs.Workers/Services/EmbedJobRunner.cs`
- **AC:**
  - [ ] Batch embed Ollama-ra.
  - [ ] Idempotens: `(document_id, chunk_index)` upsert.
  - [ ] Modell-csere esetén batch regenerálás külön jobbal (későbbi).

---

## Orchestráció és SignalR (mindhárom worktree után)

### T-DBE-24 — `PipelineOrchestrator`
- **Cél:** a teljes pipeline sorrend-vezérlés egy helyen.
- **Fájlok:**
  - `src/FamilyOs.Workers/Services/PipelineOrchestrator.cs`
- **AC:**
  - [ ] ExtractText befejezte → `DetectLanguage` enqueue.
  - [ ] DetectLanguage befejezte → 5 párhuzamos job enqueue
        (Classify, Summarize, ExtractDeadlines, ExtractTasks, Embed),
        + `processing_status = Analyzing`.
  - [ ] Mind az 5 lefutott → `processing_status = Done`.
  - [ ] Ha bármely lépés `Failed`, a többi folytatódik; csak ha mind
        `Failed`, a Document `Failed`.

### T-DBE-25 — Suggestion idempotencia (Tag/Topic/Deadline/Task)
- **Cél:** újrafutás nem hoz létre duplikátumot.
- **Fájlok:**
  - `src/FamilyOs.Application/Documents/Suggestions/SuggestionDedupService.cs`
- **AC:**
  - [ ] `Deadline`: `(source_document_id, title, due_date_utc)` szignatúra.
  - [ ] `Task`: ugyanaz cím + duedate alapján.
  - [ ] `DocumentTag` / `DocumentTopic`: PK miatt PK-conflict-on skip.

### T-DBE-26 — SignalR `DocumentsHub` + processing events
- **Cél:** real-time UI frissítés D11.
- **Fájlok:**
  - `src/FamilyOs.Api/Realtime/DocumentsHub.cs`
  - `src/FamilyOs.Application/Documents/Events/DocumentProcessingProgressNotification.cs`
  - `src/FamilyOs.Application/Documents/Events/DocumentProcessedNotification.cs`
- **AC:**
  - [ ] `documentProcessingProgress` event `{ documentId, stage, percent }`.
  - [ ] `documentProcessed` és `documentFailed` event-ek.
  - [ ] A workers a Pipeline minden lépésnél SignalR client-en küld (de
        nem az Api-n keresztül — a Workers szerver-szerveren `IHubContext`-szel
        publikál).

### T-DBE-27 — Privacy guard tesztek
- **Cél:** `security-privacy.md` §13.3 privacy assertions.
- **Fájlok:**
  - `tests/FamilyOs.Infrastructure.Ai.Tests/AiProviderPrivacyGuardTests.cs`
  - `tests/FamilyOs.Workers.Tests/PromptLoggingTests.cs`
- **AC:**
  - [ ] Mocked `HttpClient`: `LocalOnly` módban nincs request `ollama` hosthoz
        képest máshova.
  - [ ] `audit_log.details_json` sosem tartalmazza a teljes prompt-text-et —
        csak hash + length.

### T-DBE-28 — Golden samples + regressziós teszt
- **Cél:** 15 mintadokumentum determinisztikus stub providerrel.
- **Fájlok:**
  - `tests/Goldens/01_axa_kotelezo.pdf` + `expected.json`
  - …
  - `tests/FamilyOs.Workers.Tests/PipelineGoldenTests.cs`
  - `tests/Common/InMemoryAiProvider.cs` (determinisztikus stub).
- **AC:**
  - [ ] Mind a 15 minta végigfut a pipeline-on stub providerrel.
  - [ ] Az elvárt outputok (summary, deadlines, tags, facet adatok)
        megegyeznek a `expected.json`-okkal.

### T-DBE-29 — E2E pipeline nightly teszt
- **Cél:** valódi Ollama-val, egy minta.
- **Fájlok:**
  - `tests/FamilyOs.E2E/AiPipelineE2ETests.cs` (`@e2e-pipeline` címke).
- **AC:**
  - [ ] Egy AXA-kötvény minta végigfut, summary magyar, facet =
        `FinancialRecord`, legalább 1 deadline javasolt.
  - [ ] CI nightly job (nem PR-on).

---

## Megvalósítási sorrend

```
D-Infra:
  T-DBE-01 → 02 → 03 → 04 → 05    (provider absztrakció)
         → 06 → 07 → 08            (queue)
         → 09 → 10                  (OCR + lang)

D-Tartalom (3 párhuzamos worktree):
  Worktree A:  T-DBE-11 → 12 → 13
  Worktree B:  T-DBE-14 → 15 → 16 → 17 → 18 → 19 → 20
  Worktree C:  T-DBE-21 → 22 → 23

Mergelés után:
  T-DBE-24 → 25                    (orchestráció)
         → 26                       (SignalR)
         → 27 → 28 → 29             (tesztek)
```

## Epic-DoD

- [ ] Egy PDF upload → 2-5 percen belül teljes feldolgozás `Done` státuszra.
- [ ] AI summary magyar, facet entitás létrejön (típustól függően).
- [ ] Suggestions (Tag/Topic/Deadline/Task) az `Origin = AiSuggested`
      állapotban, jóváhagyásra várnak.
- [ ] `DocumentChunk` embeddingek (HNSW indexen elérhetők).
- [ ] Privacy assertion teszt zöld: `LocalOnly`-ben semmi cloud felé.
- [ ] `PromptLoggingTests` zöld: teljes prompt-text sosem logban.
- [ ] Golden teszt zöld.
- [ ] `code-reviewer` (opus) jóváhagyta az AI provider hívásokat.
- [ ] Git tag `v0.8`.

# Megvalósítási terv — Family OS

> Státusz: DRAFT v0.1 · Dátum: 2026-06-26 · Nyelv: magyar
> Kapcsolódó: [mvp-backlog.md](mvp-backlog.md), [architecture.md](architecture.md),
> [CLAUDE.md](../CLAUDE.md) (factory orchestráció)

A megvalósítás 12 fázisban történik, mindegyik **önmagában szállítható**
(merge-elhető, tesztelhető, rollback-elhető) inkrementum. Egyik fázis sem
követel meg „nagy mindenmégegyszer" merget — a kontraktusok (`docs/contracts/`)
biztosítják a párhuzamos munkát.

Minden fázis tartalmazza:
- **Cél** — mit kell elérni
- **Backlog story-k** — a `mvp-backlog.md` hivatkozásai
- **Agent-routing** — melyik agentek dolgoznak (CLAUDE.md modellrouting)
- **Új / változó fájlok** — fő scope
- **Tesztek** — minimum elvárás
- **Manuális ellenőrzés** — what to click
- **Kockázat + rollback**
- **Definition of Done (DoD)**

A fázisok elején minden szakaszt az `architect` agent nyit (~5 perces
előkészület: kontraktus felülvizsgálat, függőség-check, scope-confirm).

---

## Globális Definition of Done — fázisonként megkövetelt

Minden fázis befejezésekor:
1. `dotnet build` zöld a teljes solution-re.
2. `dotnet test` zöld a fázishoz kapcsolódó tesztekre.
3. `pnpm build` és `pnpm test` zöld (ha a fázis FE-t érint).
4. `code-reviewer` agent jóváhagyta (minőségi kapuk, lásd CLAUDE.md).
5. Conventional commits + a fázis-tag (`v0.<fázis>`) létrejön.
6. `docs/DELIVERY.md` (vagy a megfelelő doksi) frissítve, ha a fázis
   új felhasználói képességet ad.

Helm chart, devops-szintű DoD a 12. fázis része.

---

## Fázis 1 — Repository és solution struktúra

**Cél:** működő solution, üres-de-fordítható projektek, alap CI build.

**Backlog:** A1, A4 (váz), A5 (váz).

**Agent-routing:**
- `architect` (opus) — solution layout és Directory.Build.props kontraktus.
- `triage` (haiku) — projektgenerálás, csproj referenciák, üres class-ok.
- `devops` (sonnet) — Makefile, Dockerfile multi-stage csontvázak.

**Új / változó fájlok:**
- `FamilyOs.sln`
- `src/FamilyOs.Domain/FamilyOs.Domain.csproj` + üres `Class1.cs`
- `src/FamilyOs.Application/...`
- `src/FamilyOs.Infrastructure/...`
- `src/FamilyOs.Infrastructure.Ai/...`
- `src/FamilyOs.Api/Program.cs` (minimális `WebApplication`, `/healthz/live`)
- `src/FamilyOs.Workers/Program.cs` (üres `BackgroundService` host)
- `frontend/` — `ng new` standalone + Tailwind init
- `docker/api.Dockerfile`, `docker/workers.Dockerfile`, `docker/web.Dockerfile`
- `docker-compose.yml` (api, workers, web, postgres, ollama — még üres
  konfiggal)
- `Makefile` (`up`, `down`, `build`, `test`, `lint`)
- `.editorconfig`, `Directory.Build.props` (.NET 8, nullable enable,
  treat warnings as errors).

**Tesztek:**
- `FamilyOs.Api.IntegrationTests/HealthEndpointTests.cs` — `/healthz/live`
  HTTP 200.
- `FamilyOs.Domain.Tests/SmokeTest.cs` — trivial pass.
- Frontend: `app.component.spec.ts` smoke.

**Manuális ellenőrzés:**
- `make build` → zöld.
- `make up` → minden konténer fut (akkor is, ha még semmit nem csinálnak).
- Böngészőből `http://localhost:8080/healthz/live` → 200.

**Kockázat + rollback:**
- Kockázat: csproj-referencia ciklus. → A `Directory.Build.props`-ban
  kényszerítjük a dependency-ellenőrzést.
- Rollback: a feature ágat eldobjuk; main üres-marad-érintetlen.

**DoD:** `v0.1` git tag, `make up` indít, healthcheck 200.

---

## Fázis 2 — Adatbázis és EF Core modell

**Cél:** a `database-schema.md` v0.2 (lásd alább, „változás-lista") teljes
sémája EF Core migrációval létrejön.

**Backlog:** A2, B1 (Family entitás CRUD), részben B2.

**Változás-lista a database-schema.md v0.2-re (a `reminder-engine.md` és
`security-privacy.md` által felvetett pótlások):**
1. `reminder_status` enum + `'Cancelled'` érték.
2. `audit_action` enum + `'ExternalApiCall'` érték.
3. Új `notification_feed` tábla a NotificationFeed entitásra.
4. `Document.OriginalContent` opcionális mező a `C4` szövegkorrekció előtti
   állapot megőrzéséhez.

Ezeket ebben a fázisban átvezetjük a doksiba és a séma kódba egyaránt.

**Agent-routing:**
- `architect` — séma-átolvasás, a 4 változás validálása.
- `db-engineer` (sonnet) — EF Core entity konfigurációk, migrációk, seed.
- `backend-dev` (sonnet) — repository absztrakciók (`IDocumentRepository`,
  `IFamilyMemberRepository` stb.) az Application rétegben.

**Új / változó fájlok:**
- `src/FamilyOs.Domain/Entities/*.cs` (~20 entity).
- `src/FamilyOs.Domain/Enums/*.cs`.
- `src/FamilyOs.Infrastructure/Persistence/FamilyOsDbContext.cs`.
- `src/FamilyOs.Infrastructure/Persistence/Configurations/*Configuration.cs`.
- `src/FamilyOs.Infrastructure/Persistence/Migrations/__InitialSetup.cs`
  (raw SQL: extensions, collation, FTS config, trigger függvények).
- `src/FamilyOs.Infrastructure/Persistence/Migrations/<timestamp>_Initial.cs`
  (a többi tábla EF generálva).
- `src/FamilyOs.Infrastructure/Persistence/Seed/DbSeedRunner.cs` (topic-fa,
  default `Source`, bootstrap admin placeholder).
- `docs/database-schema.md` v0.2 update.

**Tesztek:**
- `FamilyOs.Infrastructure.Tests/MigrationsTests.cs` — Testcontainers,
  a migráció lefut tisztán üres DB-n.
- `FamilyOs.Infrastructure.Tests/DbSeedRunnerTests.cs` — idempotens
  futás (kétszer: ugyanaz az eredmény).
- `FamilyOs.Domain.Tests/EntityInvariantsTests.cs` — factory metódusok
  validációja (pl. `Reminder.Create()` XOR-kényszerítés).

**Manuális ellenőrzés:**
- `make migrate-up` (új Make target) → migrációk lefutnak.
- `psql` → enumok, táblák, indexek megvannak (`\dT`, `\dt`, `\di`).
- `SELECT * FROM app.topic` → seed-elt topic-fa.

**Kockázat + rollback:**
- Kockázat: PostgreSQL ICU `hu-HU` locale nem elérhető a base imagéban.
  → A pgvector base image (`pgvector/pgvector:pg16`) `ICU` támogatott.
  Telepítési ellenőrzés `SELECT * FROM pg_collation WHERE collname LIKE 'hu%';`.
- Kockázat: a `__InitialSetup` raw SQL nem futtatható egy meglévő DB-n.
  → A migrációs script `IF NOT EXISTS` guardokkal.
- Rollback: a fázis-ág eldobható; üres DB-vel újra indítható.

**DoD:** `v0.2` git tag, üres Postgres-en a teljes séma létrejön <30s alatt.

---

## Fázis 3 — Alap API: auth, family, system

**Cél:** Google OAuth bejelentkezés működik, `/api/v1/family-members` és
`/api/v1/auth/me` válaszol.

**Backlog:** A3, A4 (teljes), B1 (BE), B2, B3.

**Agent-routing:**
- `backend-dev` (sonnet) — endpointok, MediatR command/query-k, auth handler.
- `code-reviewer` (opus) — security review az auth flow-ra (kötelező opus).

**Új / változó fájlok:**
- `src/FamilyOs.Application/Auth/*` — `LoginGoogleCommand`,
  `GetCurrentUserQuery`, `LogoutCommand`.
- `src/FamilyOs.Application/Family/*` — CRUD command-ok.
- `src/FamilyOs.Infrastructure/Auth/GoogleAuthHandler.cs`,
  `CookieAuthSetup.cs`, `AllowlistService.cs`.
- `src/FamilyOs.Api/Endpoints/AuthModule.cs`, `FamilyModule.cs`,
  `SystemModule.cs`.
- `src/FamilyOs.Api/Middleware/ExceptionToProblemDetailsMiddleware.cs`.
- `src/FamilyOs.Application/Common/CurrentUserService.cs`.
- `appsettings.json` — `Auth` szekció (allowlist, Google client ID).

**Tesztek:**
- `FamilyOs.Api.IntegrationTests/AuthFlowTests.cs` — Google ID token
  mock, end-to-end login.
- `FamilyOs.Application.Tests/Family/CreateFamilyMemberHandlerTests.cs`.
- `FamilyOs.Api.IntegrationTests/ProblemDetailsFormatTests.cs` — minden
  hiba `application/problem+json` magyar `detail`-lel.

**Manuális ellenőrzés:**
- Google OAuth flow lefut (Postman vagy curl-rel `id_token` küldés).
- `GET /api/v1/auth/me` 200 cookie-val, 401 anélkül.
- `POST /api/v1/family-members` 201; `?role=Child` rejected admin-only
  endpointokra.

**Kockázat + rollback:**
- Kockázat: hibásan konfigurált Google OAuth client → blokkoló.
  → Egy „loopback" dev-client local-hostra a fejlesztés alatt.
- Kockázat: allowlist regresszió (üres listára 403-mal kizárja a bootstrap
  admint is). → DbSeedRunner bootstrap admint az allowlist-be is felveszi.
- Rollback: ág eldobás; az auth nincs a main-en, semmi nem törik.

**DoD:** `v0.3` tag, az admin be tud lépni és családtagot hoz létre.

---

## Fázis 4 — Angular shell

**Cél:** működő Angular alkalmazás auth flow-val, üres oldalakkal, és a
backend API-val való kapcsolódással.

**Backlog:** A3 (FE), A5 (FE), A4 (FE), C2 (skeleton).

**Agent-routing:**
- `architect` — folder layout, routing kontraktus.
- `frontend-dev` (sonnet) — komponensek, services, állomány.
- `triage` (haiku) — Tailwind config, ikon-szettek, alap-stilizálás.

**Új / változó fájlok:**
- `frontend/src/app/app.config.ts`, `app.routes.ts`, `app.component.ts`.
- `frontend/src/app/core/auth/*`, `core/api/*`, `core/state/create-store.ts`.
- `frontend/src/app/features/auth/login.page.ts`.
- `frontend/src/app/features/dashboard/dashboard.page.ts` (placeholder).
- `frontend/src/app/features/documents/documents.routes.ts` +
  `documents-list.page.ts` (placeholder lista).
- `frontend/src/app/layout/shell.component.ts` (navbar + sidebar +
  router-outlet).
- `frontend/src/assets/i18n/hu.json` — első ~30 string.
- `frontend/tailwind.config.ts`, `styles/theme.css`.

**Tesztek:**
- Vitest: `auth.guard.spec.ts`, `shell.component.spec.ts`,
  `login.page.spec.ts`.
- Playwright `@smoke`: login → dashboard ürességre eljut → logout.

**Manuális ellenőrzés:**
- `http://localhost:4200/login` Google-lal sikeresen belép.
- A sidebar / bottom nav minden link működik (üres oldalra navigál).
- `pnpm test` zöld; `pnpm build --configuration production` zöld.

**Kockázat + rollback:**
- Kockázat: Angular 20 standalone router timing bug. → `provideRouter`
  init-órán `withEnabledBlockingInitialNavigation()`.
- Kockázat: TailwindCSS purge fals pozitív (dynamic class-ok). → A safelist
  konfigurált.
- Rollback: a frontend mappa elsőként külön repo / külön git worktree
  könnyen eldobható.

**DoD:** `v0.4` tag, login és navigáció működik. Backend nincs kötelező
adat-funkció — csak az auth.

---

## Fázis 5 — Dokumentum upload és tárolás

**Cél:** felhasználó fel tud tölteni egy fájlt, az tárolódik fizikailag és
a DB-ben; listázás megjelenik.

**Backlog:** C1, C2 (BE+FE), C3 skeleton.

**Agent-routing:**
- `backend-dev` — upload command, storage service, dedup.
- `frontend-dev` — upload page, lista, kártya komponens.
- `db-engineer` — index-monitorozás (sha256 dedup, partial indexek).

**Új / változó fájlok:**
- `src/FamilyOs.Application/Documents/*` — `UploadDocumentCommand`,
  `ListDocumentsQuery`, `GetDocumentDetailQuery`.
- `src/FamilyOs.Application/Common/Storage/IDocumentStorage.cs`.
- `src/FamilyOs.Infrastructure/Storage/LocalFilesystemDocumentStorage.cs`.
- `src/FamilyOs.Infrastructure/Common/MimeDetector.cs` (magic byte).
- `src/FamilyOs.Api/Endpoints/DocumentsModule.cs`.
- `src/FamilyOs.Api/Middleware/IdempotencyMiddleware.cs`.
- `frontend/src/app/features/documents/pages/document-upload.page.ts`.
- `frontend/src/app/features/documents/pages/documents-list.page.ts`
  (teljes).
- `frontend/src/app/features/documents/services/documents.facade.ts`.

**Tesztek:**
- Integration: upload → 201, ismételt upload → 409.
- Integration: hibás MIME → 415, túl nagy fájl → 400 (vagy 413 nginx-ről).
- Playwright `@e2e`: drag-drop egy PDF-et, lista frissül.

**Manuális ellenőrzés:**
- PDF feltöltés, megjelenik a listában, lemezen `data/documents/2026/06/<guid>.pdf`.
- Dupla upload warning a UI-on.

**Kockázat + rollback:**
- Kockázat: nagy fájl streamelés Kestrelben (50 MB chunked) → konfig
  `KestrelServerOptions.Limits.MaxRequestBodySize` 60 MB.
- Kockázat: path traversal CVE → `IDocumentStorage` belső validáció +
  unit teszt.
- Rollback: a `documents` tábla törölhető; storage volume kiüríthető.

**DoD:** `v0.5` tag, felhasználó képes feltölteni és listázni dokumentumot;
AI még nem fut, status marad `Pending`.

---

## Fázis 6 — Szövegkinyerés (PdfPig + Tesseract)

**Cél:** a feltöltött dokumentumokból kinyerődik a szöveg és a
`DocumentText`-be kerül; a worker process feldolgozza.

**Backlog:** D3, D4 (nyelvdetekt), és előkészület a queue infrastruktúrára.

**Agent-routing:**
- `backend-dev` — `IDocumentTextExtractor` interfész és kompozit
  implementáció.
- `devops` — Tesseract binary + `hun`/`eng` nyelvi csomag a workers
  Dockerfile-ban.
- `architect` — döntés: a queue infrastruktúra (Hangfire) ebben a fázisban
  belép-e, vagy csak in-process worker (single-thread)? **MVP-döntés:**
  in-process worker most, Hangfire a 7. fázisban.

**Új / változó fájlok:**
- `src/FamilyOs.Application/Common/IDocumentTextExtractor.cs`.
- `src/FamilyOs.Infrastructure.Ai/Extraction/PdfTextLayerExtractor.cs` (PdfPig).
- `src/FamilyOs.Infrastructure.Ai/Extraction/TesseractOcrExtractor.cs`.
- `src/FamilyOs.Infrastructure.Ai/Extraction/CompositeDocumentTextExtractor.cs`.
- `src/FamilyOs.Infrastructure.Ai/Lang/NTextCatLanguageDetector.cs`.
- `src/FamilyOs.Workers/Services/ExtractTextJobRunner.cs` (BackgroundService,
  pollozza az `ai_processing_job`-ot `JobType=ExtractText`-re).
- `docker/workers.Dockerfile` — `apt-get install tesseract-ocr
  tesseract-ocr-hun tesseract-ocr-eng`.

**Tesztek:**
- Unit: `PdfTextLayerExtractor` egy fixture PDF-fel.
- Integration: feltöltött kép → OCR → `DocumentText` insert, magyar
  detekció.
- Performance smoke: 2 MB PDF-en az extraction < 30 s.

**Manuális ellenőrzés:**
- Feltöltés → 1-2 perc múlva a UI-n a `processing_status = Done`
  (még csak az extract status után, summary nélkül).
- `DocumentText.Content` a DB-ben kitöltött.

**Kockázat + rollback:**
- Kockázat: Tesseract memóriát eszik nagy képeken → resize előtt
  (max 2000 px hosszanti oldal).
- Kockázat: digitális PDF-ben „szöveg-réteg" de valójában image
  → `IsDigital()` heurisztikára (min 100 char + 80% printable).
- Rollback: a `JobRunner` leállítható; a táblák nem törlődnek.

**DoD:** `v0.6` tag, ténylegesen működő OCR pipeline egyetlen worker thread-en.

---

## Fázis 7 — AI absztrakció és durable queue

**Cél:** `IAiProvider` + `OllamaAiProvider` működik; Hangfire integrálva;
az `AiProcessingJob` queue durable + catch-up logikával.

**Backlog:** D1, D2.

**Agent-routing:**
- `architect` — `IAiProvider`, `IEmbedder` kontraktus rögzítése.
- `backend-dev` — `AiProviderFactory`, privacy guard, Hangfire setup.
- `devops` — `ollama/ollama` image, modell-pull script (`gpt-oss:20b`,
  `nomic-embed-text`).

**Új / változó fájlok:**
- `src/FamilyOs.Application/Abstractions/Ai/*` — interfészek.
- `src/FamilyOs.Infrastructure.Ai/Providers/OllamaAiProvider.cs`,
  `OllamaEmbedder.cs`.
- `src/FamilyOs.Infrastructure.Ai/Providers/AiProviderFactory.cs`
  (PrivacyMode kapuval).
- `src/FamilyOs.Infrastructure/Hangfire/HangfireSetup.cs`,
  `AiJobScheduler.cs` (recurring 10s poll), `AiJobExecutor.cs`.
- `src/FamilyOs.Workers/Program.cs` — Hangfire server bekötés.
- `docker-compose.yml` — `ollama` service health-check, model-volume.
- `scripts/pull-models.sh` — `gpt-oss:20b` és `nomic-embed-text` letöltés.

**Tesztek:**
- Unit: `AiProviderFactory.GetProvider` `LocalOnly` módban cloud
  provider kérésre throw.
- Integration: Testcontainers + Hangfire postgres storage; egy
  `AiProcessingJob` enqueued → worker felveszi.
- Privacy assertion: `HttpClient` mocked → `LocalOnly`-ben nem megy
  külső URL-re (`AiProviderPrivacyGuardTests`).

**Manuális ellenőrzés:**
- `make pull-models` → Ollama elérhető a modellekkel.
- `/hangfire` admin felület → recurring job „AiJobScheduler" látszik.
- Egy korábban feltöltött dokumentumra manuálisan enqueue egy
  `Summarize` jobot (még prompt nélkül) → futás → `Failed`-be esik
  ürességtől.

**Kockázat + rollback:**
- Kockázat: az Ollama első indulás lassú (modell betöltés ~30 s).
  → A health probe időtűréses (5 perc).
- Kockázat: Hangfire migráció ütközés. → Külön `hangfire` séma.
- Rollback: `AiJobScheduler` leállítható; a queue passzív marad.

**DoD:** `v0.7` tag, AI infrastruktúra él, de még tartalmi feldolgozás nincs.

---

## Fázis 8 — Összefoglaló, kinyerés, embedding

**Cél:** a 4–8. AI-pipeline lépések működnek end-to-end; a felhasználó
látja az AI-által javasolt mezőket.

**Backlog:** D5–D11.

**Agent-routing:**
- `backend-dev` × 3 párhuzamosan, külön worktree-ken
  (`feature/ai-summary`, `feature/ai-extract`, `feature/ai-embed`).
- `architect` — kontraktus-szinkron a 3 worktree között
  (`docs/contracts/ai-prompts.md` — prompt-template-ek verzionálása).
- `qa-playwright` (sonnet) — E2E pipeline tesztek.

**Új / változó fájlok:**
- `src/FamilyOs.Infrastructure.Ai/Tasks/*.cs` — implementációk:
  `OllamaDocumentSummarizer`, `OllamaDocumentClassifier`,
  `OllamaDeadlineExtractor`, `OllamaTaskExtractor`,
  `OllamaWarrantyExtractor`, `OllamaMedicalExtractor`,
  `OllamaFinancialExtractor`.
- `src/FamilyOs.Infrastructure.Ai/Prompts/*.txt` — magyar prompt
  template-ek (lásd ai-pipeline.md 4).
- `src/FamilyOs.Infrastructure.Ai/Embedding/EmbeddingChunker.cs` (domain
  szolgáltatás).
- `src/FamilyOs.Workers/Services/PipelineOrchestrator.cs` — a sorrend
  vezérlése (Extract → Lang → 5 párhuzamos → Status=Done).
- `src/FamilyOs.Api/Realtime/DocumentsHub.cs` — SignalR push a
  `documentProcessingProgress` eseményre.
- `tests/Goldens/` — 15 mintadokumentum + elvárt outputok.

**Tesztek:**
- Unit: `EmbeddingChunker` (bekezdés-határ, overlap, tokenizáció heurisztika).
- Integration: stub provider-rel a teljes pipeline lefut a golden sample-eken
  determinisztikusan.
- E2E (`@e2e-pipeline`, nightly): valódi Ollama-val, egyetlen mintadokumentum
  → Done.

**Manuális ellenőrzés:**
- Feltöltés egy AXA-kötvény mintán → 1-2 perc múlva: summary magyar,
  facet = Financial, határidő javasolt, embedding chunks létrejönnek.
- Suggestions inbox 3-5 elemmel feltöltődik.

**Kockázat + rollback:**
- Kockázat: az `gpt-oss:20b` JSON-mode nem mindig konform → retry „javítsd
  a JSON-t" prompt (lásd ai-pipeline.md 6.3).
- Kockázat: párhuzamos worktree-k konfliktusa a `PipelineOrchestrator`-on.
  → A kontraktus rögzíti, melyik `JobType`-okat ad enqueue-ba az orchestrator
  — az implementációk *önállóan* fejleszthetők.
- Rollback: az adott `JobType` enqueue-zás kikapcsolható feature-flag-gel
  (`Ai:Enabled:Summarize=false`).

**DoD:** `v0.8` tag, az 1. felhasználói use case (UC-01 dokumentum + AI)
end-to-end működik magyar kimenettel.

---

## Fázis 9 — Kereső és Q&A

**Cél:** a `search-strategy.md` szerinti hibrid kereső és Q&A működik a
UI-on.

**Backlog:** E1–E5 (E6/E7 best-effort).

**Agent-routing:**
- `backend-dev` — search handler-ek, RRF, FTS, semantic.
- `architect` — `IQuestionAnswerService` kontraktus + Q&A prompt fix
  template.
- `frontend-dev` — AI Search oldal.

**Új / változó fájlok:**
- `src/FamilyOs.Application/Search/*` — `SearchCommand` (handle az
  összes módra).
- `src/FamilyOs.Application/Search/Handlers/*` — `FilterHandler`,
  `FtsHandler`, `SemanticHandler`, `HybridHandler`, `QaHandler`.
- `src/FamilyOs.Application/Search/Rrf/ReciprocalRankFusion.cs`.
- `src/FamilyOs.Infrastructure.Ai/Tasks/OllamaQuestionAnswerer.cs`.
- `src/FamilyOs.Infrastructure/Search/SemanticSearchService.cs` (pgvector
  query).
- `src/FamilyOs.Api/Endpoints/SearchModule.cs`.
- `frontend/src/app/features/search/pages/search.page.ts` (chat-szerű UI).

**Tesztek:**
- Unit: RRF, intent-classifier heurisztika.
- Integration: 7 példa kérdés (`search-strategy.md` 7) → helyes routing
  és válasz (golden válaszok stub-bal).
- E2E: a UI-n kérdés → válasz + source citation kattintható.

**Manuális ellenőrzés:**
- „Mikor jár le az autó kötelező?" → magyar válasz + dokumentum-link.
- „Melyek a kifizetetlen számlák?" → filter mode, lista render.

**Kockázat + rollback:**
- Kockázat: az Ollama latency 3-5 s-ig kúszik. → UI-on „dolgozom..." indicator + Q&A cache.
- Kockázat: az anti-hallucination validáció fals pozitív. → MVP-ben
  „lazy" validáció: csak akkor riassz, ha a válasz tartalmaz idézett ID-t,
  ami nincs a retrieved chunkok között.
- Rollback: a `qa` mode kikapcsolható, FE fallback a hybrid lista renderre.

**DoD:** `v0.9` tag, UC-02 / UC-03 / UC-04 / UC-05 működnek.

---

## Fázis 10 — Emlékeztetők és értesítések

**Cél:** a `reminder-engine.md` szerinti motor él, az értesítési feed
működik.

**Backlog:** F1–F3, G1–G3, G6 (M), G4–G5 (S best-effort).

**Agent-routing:**
- `backend-dev` × 2 párhuzamosan (`feature/reminders-core`,
  `feature/notifications-feed`).
- `architect` — `IReminderScheduler`, `INotificationService` kontraktus.
- `frontend-dev` — reminders oldal + bell ikon + toast.

**Új / változó fájlok:**
- `src/FamilyOs.Domain/Entities/NotificationFeed.cs` (a fázis 2 v0.2
  séma-változás itt aktívvá válik).
- `src/FamilyOs.Application/Reminders/*` — CRUD + akció command-ok.
- `src/FamilyOs.Application/Notifications/*` — feed query + read.
- `src/FamilyOs.Workers/Services/DueReminderDispatcher.cs`
  (1 perces recurring + `OnStarted` catch-up).
- `src/FamilyOs.Workers/Services/EscalationScheduler.cs` (5 perces).
- `src/FamilyOs.Infrastructure.Ai/Recurrence/IcalRecurrenceEvaluator.cs`.
- `src/FamilyOs.Api/Endpoints/RemindersModule.cs`,
  `NotificationsModule.cs`.
- `src/FamilyOs.Api/Realtime/NotificationsHub.cs`.
- `frontend/src/app/features/reminders/pages/reminders.page.ts`.
- `frontend/src/app/core/notifications/notification.service.ts`.

**Tesztek:**
- Unit: `ReminderTriggerCalculator`, `IcalRecurrenceEvaluator`,
  `EscalationPolicyEvaluator`.
- Integration: idő-szimulációs E2E — PC ki → be → catch-up tüzelés.
- E2E: snooze, acknowledge, delegate.

**Manuális ellenőrzés:**
- Egy elfogadott Deadline-ra a 3 default reminder generálódik (Insurance:
  30/7/1 nap).
- Egy korábban tüzelt reminder a bell ikonon számláló-növelést okoz +
  toast pop.
- Eszkaláció 24 órán át nem nyugtázott Insurance-reminderre.

**Kockázat + rollback:**
- Kockázat: a Hangfire scheduler ütközik a `DueReminderDispatcher`-rel
  (kettős worker). → `SKIP LOCKED` + advisory lock.
- Kockázat: az `Ical.Net` magyar locale tippelhetetlen recurring rule-okra.
  → A UI csak előre definiált presetekkel kínálja (havi, heti, éves).
- Rollback: a dispatcher kikapcsolható; a táblák megmaradnak,
  reminderek nem tüzelnek.

**DoD:** `v0.10` tag, UC-04, UC-06 működik. Emlékeztetők megbízhatóan
tüzelnek, catch-up szimulált teszten zöld.

---

## Fázis 11 — Dashboard, topic, tag, suggestions

**Cél:** a UI integrálva — egyetlen összesítő dashboard, topic-fa
adminisztrációval, tag autocomplete, suggestions inbox.

**Backlog:** L1, L2, I1, I2, F3 (FE), H1 (BE), H2 (BE).

**Agent-routing:**
- `backend-dev` — aggregált dashboard query, tag/topic CRUD, notes.
- `frontend-dev` — minden UI oldal befejezése.
- `code-reviewer` — A search-strategy.md 8 boost-szabályok regresszióját
  szemantikus mintakeresésekkel.

**Új / változó fájlok:**
- `src/FamilyOs.Application/Dashboard/GetDashboardQuery.cs`.
- `src/FamilyOs.Application/Tags/*`, `Topics/*`, `Notes/*`.
- `src/FamilyOs.Api/Endpoints/DashboardModule.cs`, `TagsModule.cs`,
  `TopicsModule.cs`, `NotesModule.cs`, `SuggestionsModule.cs`.
- `frontend/src/app/features/dashboard/*`,
  `features/topics/*`, `features/tags-control/*`,
  `features/suggestions/*`, `features/notes/*`.

**Tesztek:**
- Performance: dashboard < 200 ms p95.
- E2E: új user első login → dashboard üres állapot, tooltip help.

**Manuális ellenőrzés:**
- Egy teljes nap szimulált használat: upload, ai-feldolgozás, kérdés,
  emlékeztető, dashboard használat.

**Kockázat + rollback:**
- Kockázat: dashboard egyetlen query-vel túl bonyolult; idő-overhead.
  → 3-4 párhuzamos query a handlerben.
- Rollback: a `/admin` és `/suggestions` oldalakat el lehet rejteni
  feature-flag-gel ha késik.

**DoD:** `v0.11` tag, az MVP 8 UC-ja közül 6 (UC-01…UC-06, UC-07, UC-08
kivételével) működik production-szerű környezetben.

---

## Fázis 12 — Hardening, tesztek, security

**Cél:** az MVP biztonsági, üzemeltetési, dokumentációs DoD-ja teljesül.

**Backlog:** J1–J4, K1, K2, K3, M1–M4.

**Agent-routing:**
- `code-reviewer` (opus) — teljes security review (`security-privacy.md`
  összes szakasza minimum-elvárás).
- `devops` — Helm chart MVP-pótlék (de a Docker Compose marad az elsődleges),
  TLS belső CA-val, backup script, monitoring alap.
- `doc-writer` (haiku) — `DELIVERY.md`, release notes.
- `qa-playwright` — `@security` és `@e2e-pipeline` teljes futás.

**Új / változó fájlok:**
- `src/FamilyOs.Infrastructure/Audit/AuditLogger.cs` + MediatR pipeline.
- `src/FamilyOs.Api/Endpoints/AuditModule.cs`, `AiJobsAdminModule.cs`,
  `SourcesModule.cs`.
- `src/FamilyOs.Infrastructure/Email/GmailIngestionService.cs` +
  `EmailIngestionPoller`.
- `src/FamilyOs.Infrastructure/Notifications/SmtpNotificationChannel.cs`.
- `docker/nginx/`, `docker/nginx/Dockerfile`, `docker/nginx/family-os.conf`.
- `scripts/backup.sh`, `scripts/restore.sh`, `scripts/init-tls-ca.sh`.
- `docs/DELIVERY.md` — telepítési, üzemeltetési, incident runbook.
- `helm/family-os/` — Helm chart (opcionális MVP).

**Tesztek:**
- Privacy assertion: `LocalOnly`-ben mocked `HttpClient` nem megy ki.
- Audit assertion: minden security event-re bejegyzés.
- E2E security: privát rekord más userrel → 403.
- ZAP baseline scan az API-ra.

**Manuális ellenőrzés:**
- Teljes telepítési próba: tiszta gép, `docs/DELIVERY.md` követése,
  60 percen belül élesít.
- Backup → restore drill: feltöltött adatok visszatérnek.
- TLS belső CA telepítése egy telefonra, böngészés HTTPS-en.

**Kockázat + rollback:**
- Kockázat: Gmail OAuth verification kötelezővé válása. → A self-hosted
  felhasználó saját GCP projektet használ, a verification nem releváns.
- Kockázat: ZAP false positive blokkol egy release-t. → Csak warning
  szinten, nem fail.
- Rollback: a release branch nem kerül main-re; a hibás komponensek
  egyenként kikapcsolhatók.

**DoD:** `v1.0` tag, `docs/DELIVERY.md` end-to-end követhető, az MVP
8 UC-ja működik egy átlag családi háztartásban telepítve.

---

## Globális kockázatok és mitigáció

| Kockázat | Valószínűség | Hatás | Mitigáció |
|---|---|---|---|
| Ollama / gpt-oss minőség gyengébb a vártnál | közepes | magyar Q&A minőség | golden samples + prompt-iteráció + opcionális hybrid mód v2-ben |
| Hangfire / DueReminderDispatcher kettős worker race | alacsony | dupla notification | `SKIP LOCKED` + advisory lock + idempotency-key SMTP-re |
| Postgres `hu-HU` collation hiba | alacsony | rendezés/keresés | előzetes ellenőrzés a base image-ben |
| Tesseract OCR pontatlan magyar kézírásra | magas | kézi korrekció kell | a UI engedi a szöveg kézi szerkesztését (C4) |
| PrivacyMode kapu körülmegyés (refactor során) | alacsony | privacy szivárgás | privacy assertion teszt + code-reviewer audit minden AI-related PR-on |
| EF Core 8 migrációs eltérés a meglévő SQL-től | közepes | migráció failure | Testcontainers integration teszt minden migrációhoz |

---

## Worktree stratégia (összefoglaló)

A factory-megközelítésnek megfelelően (CLAUDE.md):
- **1 feature = 1 worktree = 1 ág**: `git worktree add ../wt-<feature> -b feature/<feature>`.
- Párhuzamos szálak fázisonként:
  - Fázis 7: 1 worktree (egyetlen AI infra).
  - Fázis 8: 3 worktree (summary / extract / embed).
  - Fázis 10: 2 worktree (reminders-core / notifications-feed).
  - Egyébként: 1 worktree.
- Kontraktus-mappa: `docs/contracts/` — minden közös interfész
  itt rögzítve, mielőtt párhuzamos munka kezdődik.
- Merge sorrend a fázis végén:
  1. kontraktus-implementációk (interfészek és DI).
  2. feature-implementációk.
  3. integrációs tesztek + frontend bekötés.

---

## 13. Mit NEM tartalmaz az MVP fázisterv

(Kross-referencia a `product-vision.md` non-goal listájával.)

- Kotlin mobil natív kliens — későbbi külön roadmap.
- Push notification — későbbi.
- Két irányú Google Calendar sync — későbbi.
- Multi-tenant — soha (architecturally döntés).
- Geo-fence / mobil delegation — ADR-0003 LAN-only miatt nincs értelme.

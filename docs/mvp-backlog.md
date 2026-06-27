# MVP backlog — Family OS

> Státusz: DRAFT v0.1 · Dátum: 2026-06-26 · Nyelv: magyar
> Kapcsolódó: [product-vision.md](product-vision.md), [implementation-plan.md](implementation-plan.md),
> [architecture.md](architecture.md), [api-design.md](api-design.md)

A backlog 13 epicre, ~50 user story-ra van bontva. Minden story-hoz:
- backend / frontend / AI / infra task-bontás,
- Given/When/Then acceptance criteria,
- prioritás: **M** (Must) / **S** (Should) / **C** (Could).

A javasolt megvalósítási sorrend a 14. szakaszban (a 12 fázisra leképezve,
összhangban az `implementation-plan.md`-vel).

---

## 1. Epic A — Alapok és infrastruktúra

### A1. Solution szerkezet és CI alap **[M]**

**Story:** Mint fejlesztő, szeretnék egy működő solution-t és CI build-pipeline-t,
hogy minden további munka stabil alapra építsen.

**AC:**
- Adott `dotnet new sln` + 6 projekt (`Domain`, `Application`, `Infrastructure`,
  `Infrastructure.Ai`, `Api`, `Workers`) a `architecture.md` 2. szerint.
- Amikor `dotnet build` fut, akkor zöld.
- Amikor `pnpm install && pnpm build` fut, akkor a frontend Angular 20 build zöld.
- A CI (GitHub Actions vagy lokális `make ci`) build + unit test + lint
  zölden lefut.

**Tasks:**
- **BE:** csontvázprojektek, `Directory.Build.props` shared verziókkal,
  `csproj` referenciák a függőségi gráf szerint.
- **FE:** `pnpm create vite@latest` Angular-rel? — NEM, `ng new` standalone,
  Tailwind + Vitest config.
- **Infra:** Dockerfile multi-stage minden host-projekthez, `docker-compose.yml`
  alap (api, workers, postgres, ollama, web).
- **DevOps:** `Makefile` (`make up`, `make build`, `make test`).

---

### A2. PostgreSQL séma és EF Core migrációk **[M]**

**Story:** Mint fejlesztő, szeretném, hogy az adatbázis-séma a
`database-schema.md` szerint létrejöjjön egyetlen migrációs pipeline-ban.

**AC:**
- Adott üres Postgres, és amikor `dotnet ef database update` fut, akkor
  a 20 tábla, 22 enum, FTS config, extensions (pgvector, pg_trgm, unaccent),
  triggerek (`set_updated_utc`, `audit_log_immutable`) létrejönnek.
- Amikor a Workers indul, akkor a Hangfire séma is migrálódik (`hangfire`
  séma).
- Seed adatok (topic-fa, default `Source`) idempotens módon betöltődnek.

**Tasks:**
- **BE:** EF Core DbContext + entity konfigurációk; `__InitialSetup`
  migráció raw SQL-el az extension/collation/FTS részekhez.
- **BE:** seed data infrastruktúra (`DbSeedRunner`).
- **Infra:** `pgvector/pgvector:pg16` Docker image, ICU `hu-HU` locale.

---

### A3. Auth — Google OAuth + session **[M]**

**Story:** Mint felhasználó, szeretnék Google-fiókkal belépni, hogy ne
kelljen külön jelszót kezelnem.

**AC:**
- Amikor a UI-on rákattintok a „Bejelentkezés Google-lal" gombra, akkor
  a Google flow után visszairányítva be vagyok jelentkezve, és a `/api/v1/auth/me`
  visszaadja a profilomat.
- Amikor a Google email-em nincs az allowlist-en, akkor 403, magyar
  üzenettel: „Az email-címed nincs engedélyezve ezen a Family OS-en."
- Az első login a `Auth.BootstrapAdmin` címen automatikusan `Role = Admin`
  szerepkört ad.
- Logout után a cookie azonnal érvénytelen.

**Tasks:**
- **BE:** `Microsoft.AspNetCore.Authentication.Google` setup, ID token
  validáció, allowlist policy, cookie config (`__Host-` prefix).
- **BE:** `RevokedSessions` mini-tábla a logout-ra.
- **FE:** `LoginPage`, `AuthService`, `authInterceptor`, `authGuard`.
- **Infra:** Google Cloud projekt + OAuth client + dev/staging/prod
  redirect URI-k (csak a háztartási LAN-on belül).

---

### A4. ProblemDetails hibakezelés **[M]**

**Story:** Mint frontend fejlesztő, szeretnék egységes hibaformátumot
kapni, hogy a UI minden hibát ugyanúgy renderelhessen.

**AC:**
- Minden 4xx/5xx válasz `application/problem+json` MIME-mel jön,
  magyar `detail`-lel és `traceId`-vel.
- Validációs hibák (`fieldErrors`) struktúrált formában.

**Tasks:**
- **BE:** globális `ExceptionHandlerMiddleware`, magyar üzenet-katalógus,
  `IProblemDetailsFactory` testreszabás.
- **FE:** `errorInterceptor`, `AppError` típus, toast a globális hibákra.

---

### A5. Logolás, trace, healthcheck **[M]**

**Story:** Mint üzemeltető, szeretnék strukturált logot és
healthcheck endpointokat, hogy lássam, működik-e a rendszer.

**AC:**
- Serilog console + rotating file (`/var/log/family-os/`).
- `traceparent` header end-to-end (FE → API → Workers, ha alkalmazható).
- `/healthz/live` és `/healthz/ready` HTTP 200, ha DB és Ollama elérhető.

**Tasks:**
- **BE:** Serilog config, OpenTelemetry minimal setup (csak trace MVP-ben).
- **BE:** healthcheck (`AddNpgSqlHealthCheck`, custom Ollama health).
- **FE:** `traceIdInterceptor`.

---

## 2. Epic B — Családtag- és felhasználó-kezelés

### B1. Családtagok CRUD **[M]**

**Story:** Mint admin, szeretnék családtagokat felvenni / szerkeszteni /
soft-delete-elni, hogy a többi entitást hozzájuk kapcsolhassam.

**AC:**
- `POST /api/v1/family-members` 201 + DTO.
- Soft delete, ha nincs élő `UserAccount` rajta.
- `Display name`, `relation`, `birthDate` szerkeszthető.

**Tasks:**
- **BE:** `FamilyMember` entity + `MembersController` (Modul) +
  MediatR command/query-k.
- **FE:** `/family` admin oldal (lista + szerkesztő modal).
- **AI/Infra:** —

---

### B2. UserAccount meghívó + szerepkör-kezelés **[M]**

**Story:** Mint admin, szeretnék új felhasználót meghívni egy
családtaghoz kötve, hogy a meghívott Google-loginnal automatikusan
hozzáférést kapjon.

**AC:**
- `POST /api/v1/user-accounts/invite` allowlist-be tesz egy email-t,
  összerendelve egy `familyMemberId`-vel és `role`-lal.
- Amikor a meghívott email-mel logol be Google-lal, automatikusan a
  meghívásban szereplő szerepkört kapja.
- Audit log bejegyzés a meghívásról és az aktiválásról.

**Tasks:**
- **BE:** invite entity (`UserAccountInvite` egyszerű tábla az MVP-ben, vagy
  egyszerűen `UserAccount` `IsActive=false`-szal előlétrehozva); login
  callback-ben aktiválás.
- **FE:** invite dialog a Family oldalon.

---

### B3. Saját preferenciák szerkesztése **[M]**

**Story:** Mint felhasználó, szeretnék csendes órákat beállítani és az
email-csatornát ki/be kapcsolni, hogy az emlékeztetők ne zavarjanak
nyugalmi időszakban.

**AC:**
- `PATCH /api/v1/auth/me/preferences` 200 + frissített DTO.
- A `quietHoursStart/End` érvényes 24h formátum (`HH:mm`).

**Tasks:**
- **BE:** preference store (a `UserAccount`-on JSONB mező, vagy külön
  `user_preferences` tábla az MVP-ben).
- **FE:** `/settings` saját szakasza.

---

## 3. Epic C — Dokumentum-kezelés

### C1. Dokumentum upload + dedup **[M]**

**Story:** Mint Adult, szeretnék dokumentumot feltölteni, hogy automatikusan
feldolgozásra kerüljön.

**AC:**
- `POST /api/v1/documents` (multipart) → 201 + `processingStatus = Pending`.
- Sha256 dedup: ugyanaz a fájl második feltöltésre 409 + meglévő rekord
  hivatkozása.
- 415 nem támogatott MIME (whitelist: PDF, JPEG, PNG, HEIC, TXT, DOCX).
- 413 / domain 400 ha fájl > 50 MB.
- `Idempotency-Key` támogatott.
- Audit log: `Create`.

**Tasks:**
- **BE:** `UploadDocumentCommand`, `LocalFilesystemDocumentStorage`,
  MIME magic-byte detekció.
- **BE:** `Idempotency-Key` middleware.
- **FE:** drag-and-drop upload page, progress bar, dedup warning UI.

---

### C2. Dokumentumok listázása + szűrés **[M]**

**Story:** Mint felhasználó, szeretnék listázni a dokumentumokat
szűrőkkel, hogy gyorsan megtaláljam, amit keresek.

**AC:**
- `GET /api/v1/documents?topicSlug=&relatedFamilyMemberId=&from=&to=`
  helyesen szűr.
- A lista pagination-támogatott, alapból 50/oldal.
- RBAC: a privát rekordok más felhasználónak nem jönnek vissza.

**Tasks:**
- **BE:** `ListDocumentsQuery` + EF Core query az indexek használatára.
- **FE:** `/documents` lista oldal, filter panel, kártya-grid.

---

### C3. Dokumentum részletek oldal **[M]**

**Story:** Mint felhasználó, szeretném a dokumentum részleteit látni
(metaadat, AI összefoglaló, kapcsolódó facet adatok, kinyert szöveg,
tag-ek, javaslatok).

**AC:**
- `GET /api/v1/documents/{id}` egyetlen aggregált válaszban adja az össze
  adatot.
- A javaslat-blokk Approve/Reject akciók elérhetők.
- A fájl előnézet (PDF) megnyitható az oldalon.

**Tasks:**
- **BE:** `GetDocumentDetailQuery` (eager loading: summary, tags, topics, facet).
- **FE:** `DocumentDetailPage` tab-okkal, PDF.js integráció, suggestion block.

---

### C4. Dokumentum letöltés + szöveg-kézikorrekció **[S]**

**Story:** Mint Adult, szeretném a kinyert szöveget módosítani, ha az OCR
hibázott, hogy a kereső pontosabban találja meg.

**AC:**
- `PATCH /api/v1/documents/{id}/text` 200; a módosítás után
  re-embed + re-summarize jobok queued.
- Eredeti OCR szöveg megőrződik egy `OriginalContent` mezőben (séma-bővítés)
  vagy audit logban.

**Tasks:**
- **BE:** szövegszerkesztő endpoint, reprocess trigger.
- **FE:** `/documents/{id}` Szöveg tab szerkeszthető textarea-val.

---

### C5. Dokumentum törlés + reprocess **[M / S]**

**Story (M):** Mint Adult, szeretném a saját dokumentumomat törölni
(soft) → `DELETE /api/v1/documents/{id}` 204.

**Story (S):** Mint Adult, szeretném a feldolgozást manuálisan újraindítani,
ha az AI elszúrta → `POST /api/v1/documents/{id}/reprocess`.

**Tasks:**
- **BE:** soft-delete kaszkád megfontolása (DocumentText, Chunks → cascade
  fizikai delete; facet → soft delete).
- **FE:** kontextus menü kártyán + részletek oldalon.

---

## 4. Epic D — AI pipeline

### D1. AI provider absztrakció + Ollama adapter **[M]**

**Story:** Mint fejlesztő, szeretnék egy `IAiProvider` absztrakciót és egy
lokális Ollama implementációt, hogy a többi AI lépés ezen át hívjon.

**AC:**
- `OllamaAiProvider` implementálja a `IAiProvider`-t (`Complete`, `Stream`).
- Egy unit teszt mock provider-rel zöld.
- `appsettings.json`-ban a default `LocalOnly`.
- 503 ha az Ollama nem elérhető.

**Tasks:**
- **AI:** `OllamaAiProvider` (`HttpClient` + `application/json` body),
  capability flag-ek.
- **BE:** `AiProviderFactory`, DI registration.
- **Infra:** `ollama/ollama` image a compose-ban, `gpt-oss:20b` model pull
  script.

---

### D2. AiProcessingJob queue + Hangfire integráció **[M]**

**Story:** Mint fejlesztő, szeretnék egy durable queue-t és Hangfire workert,
hogy a hosszú AI feladatok offline ablak után is feldolgozódjanak.

**AC:**
- `AiProcessingJob` insert → Hangfire enqueue az `AiJobScheduler` recurring job-on
  keresztül.
- `OnAppStart` catch-up szkennelés.
- Idempotens lefutás (lásd ai-pipeline.md 6).
- A Hangfire dashboard `/hangfire` admin-only.

**Tasks:**
- **BE:** `AiJobScheduler` BackgroundService, `AiJobExecutor.RunAsync(jobId)`.
- **BE:** Hangfire postgres storage, separate `hangfire` schema.
- **Infra:** worker konténer indítása compose-ban.

---

### D3. Szövegkinyerés (text layer + Tesseract OCR) **[M]**

**Story:** Mint felhasználó, szeretném, hogy a feltöltött PDF-ekből
automatikusan kinyerődjön a szöveg.

**AC:**
- PDF text layer → `PdfPig` (ha elegendő).
- Egyébként Tesseract `hun + eng` OCR.
- `DocumentText` rekord létrejön; `extraction_method` enum helyesen.
- Üres OCR output → `Document.processing_status = Failed`.

**Tasks:**
- **AI:** `TesseractOcr` adapter (NuGet), `PdfTextLayerExtractor`.
- **AI:** `IDocumentTextExtractor` implementáció.
- **Infra:** Tesseract binary + nyelvi csomagok a worker image-ben.

---

### D4. Nyelvdetektálás **[M]**

**Story:** Mint rendszer, szeretnék automatikusan nyelvet detektálni a
kinyert szövegből, hogy a megfelelő FTS configot használjuk és releváns
promptokat generáljunk.

**AC:**
- `JobType = DetectLanguage` futása után `Document.Language` kitöltve
  (`hu`, `en`).
- Magyar 1000+ karakteres szövegre 95%+ helyes detekció.

**Tasks:**
- **AI:** `NTextCat` vagy `LanguageDetection.NET` integráció.

---

### D5. AI összefoglaló **[M]**

**Story:** Mint felhasználó, szeretnék egy 3-5 mondatos magyar összefoglalót
látni minden feltöltött dokumentumon.

**AC:**
- `JobType = Summarize` futása után `DocumentSummary` insert `IsCurrent=true`-val.
- A summary magyar, és csak a forrásszövegre épül (no hallucination
  validáció lazán).
- Reprocess esetén a régi `IsCurrent` átvált `false`-ra.

**Tasks:**
- **AI:** `IDocumentSummarizer` impl + magyar prompt template
  (lásd ai-pipeline.md 4.2).

---

### D6. Osztályozás (Topic + Tag + facet) **[M]**

**Story:** Mint felhasználó, szeretném, hogy az AI javasoljon nekem
topic-ot, tag-eket és facet típust (Warranty/Medical/Financial).

**AC:**
- A javasolt topic-ok a meglévő taxonómiából.
- Tag-eket az AI hozhat létre (új vagy meglévő).
- A facet típus megfelelő (Warranty / Medical / Financial / null).
- `Origin = AiSuggested` minden új link/rekordra.

**Tasks:**
- **AI:** `IDocumentClassifier` + magyar prompt (ai-pipeline.md 4.1).

---

### D7. Határidő-kinyerés **[M]**

**Story:** Mint felhasználó, szeretném, hogy az AI javasoljon határidőket
a feltöltött dokumentum alapján (lejárati dátum, vizsga, megújulás).

**AC:**
- `Deadline` rekord(ok) jönnek létre `Origin = AiSuggested`, `Status = Upcoming`.
- Csak `>= today` dátumokra javasol.
- A javaslatok jóváhagyásáig **nem** generál Reminder-eket aktívra.

**Tasks:**
- **AI:** `IDeadlineExtractor` + prompt + JSON validáció.

---

### D8. Feladat-kinyerés **[S]**

**Story:** Mint felhasználó, szeretném, hogy az AI javasoljon konkrét
tennivalókat a dokumentum alapján, felelős családtaggal.

**AC:**
- `Task` rekord(ok) `Status = Suggested`, `Origin = AiSuggested`.
- Az `assignedToHint` a `FamilyMember.DisplayName` listán keresztül
  feloldódik vagy `null`.

**Tasks:**
- **AI:** `ITaskExtractor` + prompt + családtag-resolver.

---

### D9. Facet-entitás kinyerés (Warranty / Medical / Financial) **[M]**

**Story:** Mint felhasználó, szeretném, hogy az AI strukturáltan kitöltse
a garancia / orvosi / pénzügyi facet mezőket a dokumentum alapján.

**AC:**
- A facet típus szerint a megfelelő entitás (`Warranty` / `MedicalRecord`
  / `FinancialRecord`) jön létre, ha az AI talál releváns adatokat.
- MedicalRecord: ha `family_member_id` nem feloldható biztosan, a facet
  rekord nem jön létre, hanem javaslat-blokkban marad megerősítésre.

**Tasks:**
- **AI:** három facet-prompt template + extractor szolgáltatás.

---

### D10. Embedding generálás **[M]**

**Story:** Mint rendszer, szeretném minden dokumentum / jegyzet szövegét
chunkolni és embeddinget tárolni a szemantikus keresőhöz.

**AC:**
- Chunkolás: bekezdés-határok, max 800 token, ~100 overlap.
- `nomic-embed-text` modell, 768 dim, HNSW indexelés.
- `DocumentChunk` / `NoteChunk` insertek idempotensek
  (`(document_id, chunk_index)` UNIQUE).

**Tasks:**
- **AI:** `EmbeddingChunker` domain szolgáltatás, `OllamaEmbedder` adapter.

---

### D11. Pipeline orchestráció + status push **[M]**

**Story:** Mint felhasználó, szeretném valós időben látni a dokumentum
feldolgozási folyamatát.

**AC:**
- A `Document.processing_status` `Pending → Extracting → Analyzing →
  Done | Failed` átmenetei valós időben push-olódnak.
- A frontend a dokumentum-kártyán mutatja a progress-t.

**Tasks:**
- **BE:** SignalR `DocumentsHub`, `documentProcessingProgress` event.
- **BE:** orchestrator a job lefutási sorrendre.
- **FE:** `RealtimeService` + dokumentumkártya progress.

---

## 5. Epic E — Kereső és Q&A

### E1. Strukturált filter kereső **[M]**

**Story:** Mint felhasználó, szeretnék listákat szűrni jól indexelt mezőkre
(határidő, kategória, családtag).

**AC:**
- `POST /api/v1/search { mode: 'filter' }` < 50 ms p95.
- A `filter` módban LLM nem hívódik.

**Tasks:**
- **BE:** `FilterSearchHandler` MediatR.
- **FE:** filter-panel komponensek (lista oldalakon).

---

### E2. Full-text search **[M]**

**Story:** Mint felhasználó, szeretnék szabad-szöveges keresőt a
dokumentumokra és jegyzetekre.

**AC:**
- `POST /api/v1/search { mode: 'text' }` magyar `hungarian_unaccent`
  configgal.
- `ts_headline` snippet a UI-on.

**Tasks:**
- **BE:** `FtsSearchHandler`.

---

### E3. Szemantikus / vektor keresés **[M]**

**Story:** Mint felhasználó, szeretnék jelentés-alapú keresést, ha a
szó-egyezés gyenge.

**AC:**
- `POST /api/v1/search { mode: 'semantic' }` HNSW indexen.
- `embedding_model` szűrés betartva (vegyes vektorok nem keverednek).

**Tasks:**
- **BE:** `SemanticSearchHandler`, `ISemanticSearchService`.

---

### E4. Hibrid kereső RRF-fel **[M]**

**Story:** Mint felhasználó, szeretnék egyetlen univerzális keresőt, ami
a strukturált + FTS + szemantikus találatokat összesúlyozza.

**AC:**
- `POST /api/v1/search { mode: 'auto' | undefined }` RRF eredménnyel.
- A találat-payload tartalmaz facet-aggregátumot.

**Tasks:**
- **BE:** `HybridSearchHandler`, RRF fúzió.
- **FE:** AI Search oldal.

---

### E5. Q&A LLM válasz-szintézis **[M]**

**Story:** Mint felhasználó, szeretnék magyar nyelvű választ kapni a
kérdéseimre, hivatkozott forrásokkal.

**AC:**
- `POST /api/v1/search { mode: 'qa' }` válasz tartalmaz `answer`,
  `citedSources`, `confidence`.
- A válaszban hivatkozott `documentId`-k mind benne vannak a retrieved
  chunk-ok között (anti-hallucination validáció).
- Nincs forrás → „Nincs erre vonatkozó adat..." magyar válasz.

**Tasks:**
- **AI:** `IQuestionAnswerService` + magyar Q&A prompt.
- **BE:** rate limit 10 req/min/user.
- **FE:** AI search chat-szerű UI, source citation kártyák.

---

### E6. Intent classifier + slot kinyerés **[S]**

**Story:** Mint felhasználó, szeretnék jó találatokat akkor is, ha nem írom
le pontosan, mire kérdezek.

**AC:**
- `mode: 'auto'`-ban az intent osztályozó (`filter` / `lookup` / `find` /
  `summarize`) helyesen routol.
- Slot-kinyerés (dátum, családtag, kategória) chip-ekként megjelenik a UI-on.

**Tasks:**
- **AI:** szabály-alapú intent classifier + opcionális LLM slot-extraction.

---

### E7. Mentett keresések **[C]**

**Story:** Mint felhasználó, szeretnék mentett kereséseket a dashboardon.

**AC:**
- `GET/POST/DELETE /api/v1/search/saved`.
- Dashboard widget renderelve.

**Tasks:**
- **BE:** `SavedSearch` mini entity.
- **FE:** „Mentés" gomb + widget.

---

## 6. Epic F — Feladatok és határidők

### F1. Task CRUD + státusz-átmenetek **[M]**

**Story:** Mint Adult, szeretnék manuálisan feladatokat felvenni,
módosítani, lezárni.

**AC:**
- `POST/PATCH/DELETE /api/v1/tasks`.
- Státusz akció endpointok (`/approve`, `/start`, `/complete`, `/cancel`).
- A status diagram `Suggested → Open → InProgress → Done` betartva.

**Tasks:**
- **BE:** `TaskCommand`-ok + `TaskStateMachine` domain szolgáltatás.
- **FE:** Tasks kanban / lista oldal.

---

### F2. Deadline CRUD + státusz-átmenetek **[M]**

**Story:** Mint Adult, szeretnék határidőket felvenni naptár-jelleggel.

**AC:**
- `POST/PATCH/DELETE /api/v1/deadlines`.
- `/approve`, `/resolve`, `/dismiss` akciók.

**Tasks:**
- **BE:** analóg.
- **FE:** Deadlines lista + naptár nézet.

---

### F3. AI-javaslatok jóváhagyási flow **[M]**

**Story:** Mint felhasználó, szeretnék egyetlen helyen jóváhagyni vagy
elvetni az AI-javaslatokat (Task, Deadline, Tag, Topic, facet).

**AC:**
- `GET /api/v1/suggestions` aggregált.
- `POST /api/v1/suggestions/batch` egyszerre jóváhagyja az összeset.
- Jóváhagyásra a `Origin` `AiSuggested → AiApproved`.

**Tasks:**
- **BE:** `SuggestionsAggregator` + `BatchApproveCommand`.
- **FE:** Suggestions oldal (inbox-szerű).

---

## 7. Epic G — Emlékeztetők

### G1. Reminder CRUD + XOR validáció **[M]**

**Story:** Mint Adult, szeretnék emlékeztetőket létrehozni Task-okra vagy
Deadline-okra.

**AC:**
- XOR (`taskId` vagy `deadlineId`) érvényesítve.
- `triggerUtc` jövőbeli (kivéve admin kontextusban).
- RRULE szabály validálva (`Ical.Net`).

**Tasks:**
- **BE:** `ReminderCommands`, RRULE parser.
- **FE:** reminder szerkesztő dialog.

---

### G2. DueReminderDispatcher + catch-up **[M]**

**Story:** Mint rendszer, szeretnék minden esedékessé vált emlékeztetőt
megbízhatóan tüzelni, beleértve az offline ablak alattiakat.

**AC:**
- A worker indulásakor a `Scheduled AND trigger_utc <= now() AND >
  now() - 14 days` rekordok kitüzelődnek.
- 1 perces recurring scan.
- `SELECT FOR UPDATE SKIP LOCKED` deduplikáció.

**Tasks:**
- **BE:** `DueReminderDispatcher` BackgroundService.
- **BE:** in-app notification feed dispatch.
- **Infra:** worker konténer healthcheck.

---

### G3. Snooze, acknowledge, delegate UX **[M]**

**Story:** Mint felhasználó, szeretnék az értesítésen egyszerű akciókat:
„Kész", „Halaszt 1 óra", „Delegálom".

**AC:**
- `POST /api/v1/reminders/{id}/acknowledge|snooze|skip|delegate` endpointok
  helyesen működnek.
- Snooze esetén új Reminder rekord (`Scheduled`).

**Tasks:**
- **BE:** action endpointok.
- **FE:** reminder kártya akció-gombok, sticky toast.

---

### G4. Eszkalációs ütemező **[S]**

**Story:** Mint felhasználó, szeretném, hogy ha kihagyok egy fontos
emlékeztetőt, kapjak egy nyomatékosabb értesítést.

**AC:**
- A `reminder-engine.md` 4.1 policy szerint az eszkaláció működik.
- Új `Reminder` rekord magasabb `EscalationLevel`-en, opcionálisan
  másik csatornán.

**Tasks:**
- **BE:** `EscalationScheduler` BackgroundService.

---

### G5. Email értesítés csatorna **[S]**

**Story:** Mint felhasználó, szeretnék emailt is kapni a fontosabb
emlékeztetőkről.

**AC:**
- SMTP konfig admin-on; opt-in user-szinten.
- HTML email magyar tartalommal.
- Hiba esetén retry max 3-szor.

**Tasks:**
- **BE:** `SmtpNotificationChannel`, email template (Razor / Liquid).
- **Infra:** SMTP relay config dokumentáció.

---

### G6. NotificationFeed (InApp) **[M]**

**Story:** Mint felhasználó, szeretném látni az olvasatlan értesítéseimet
a navbar bell ikonon és a `/notifications` oldalon.

**AC:**
- `GET /api/v1/notifications`, `POST .../read`, `read-all`.
- SignalR push-szal real-time.

**Tasks:**
- **BE:** `NotificationFeed` tábla séma (lásd reminder-engine.md 5.1.1),
  EF + endpointok.
- **FE:** bell ikon, feed sheet, toast.

---

## 8. Epic H — Jegyzetek (Notes)

### H1. Note CRUD + chunkolás **[S]**

**Story:** Mint felhasználó, szeretnék manuális jegyzeteket írni, amelyek
ugyanúgy kereshetők, mint a dokumentumok.

**AC:**
- `POST/PATCH/DELETE /api/v1/notes`.
- Markdown body.
- `NoteChunk` embedding minden insert / update után aszinkron.

**Tasks:**
- **BE:** `Note` entity + command-ok + chunkoló pipeline.
- **FE:** Notes oldal, markdown szerkesztő (egyszerű textarea + preview).

---

### H2. Tag / Topic kapcsolás Note-okra **[S]**

**AC:** analóg a dokumentumokhoz.

---

## 9. Epic I — Tag-ek és Topic-ok

### I1. Tag CRUD + autocomplete **[M]**

**Story:** Mint felhasználó, szeretnék tag-eket létrehozni és használni a
listák szűréséhez.

**AC:**
- `GET /api/v1/tags?q=` autocomplete < 50 ms.
- Soft delete + reuse cont (`usage_count`).

**Tasks:**
- **BE:** `Tag` CRUD.
- **FE:** tag-multiselect komponens.

---

### I2. Topic-fa megtekintése + adminisztráció **[M]**

**Story:** Mint admin, szeretném a topic-fát szerkeszteni
(altopicokat hozzáadni / törölni).

**AC:**
- `GET /api/v1/topics?flat=false` tree.
- Admin szerkesztés a `/topics` oldalon.

**Tasks:**
- **BE:** `Topic` CRUD.
- **FE:** tree-view + drag-to-reorder.

---

## 10. Epic J — Audit és admin felület

### J1. Audit log írása **[M]**

**Story:** Mint admin, szeretném látni, ki mit csinált.

**AC:**
- Minden domain event és security event audit logba kerül (lásd
  security-privacy.md 5.2).
- A log immutable (DB trigger).

**Tasks:**
- **BE:** `IAuditLogger` szolgáltatás, MediatR pipeline behavior, AuditLog
  domain repo.

---

### J2. Audit log böngészés és export **[S]**

**AC:**
- `GET /api/v1/audit-log` szűrőkkel.
- `?format=csv` export streaming.

**Tasks:**
- **BE:** query handler + CSV streaming.
- **FE:** `/admin/audit` oldal.

---

### J3. AI jobs admin felület **[S]**

**AC:**
- `GET /api/v1/ai-jobs?status=Failed` lista.
- Retry / cancel akciók.

**Tasks:**
- **BE:** `AiJobsAdminController`.
- **FE:** `/admin/jobs` oldal.

---

### J4. Hangfire dashboard auth integráció **[S]**

**AC:**
- A `/hangfire` route admin-only, sessionnel hitelesítve.

**Tasks:**
- **BE:** `IDashboardAuthorizationFilter` impl.

---

## 11. Epic K — Beállítások és integrációk

### K1. Gmail OAuth + szelektív beszívás **[S]**

**Story:** Mint admin, szeretném a Gmail fiókomat csatlakoztatni, hogy a
`family-os/import` címkével ellátott emailek bekerüljenek.

**AC:**
- OAuth scope `gmail.readonly`.
- `Source` rekord aktív; `EmailIngestionPoller` 5 percenként szinkronizál.
- `EmailMessage` insert + a body / mellékletek `Document`-be feldolgozva.

**Tasks:**
- **BE:** `Google.Apis.Gmail.v1` integráció, OAuth refresh-token mentés DP-vel.
- **BE:** `EmailIngestionPoller`.
- **FE:** Settings oldal Gmail connect gomb.

---

### K2. AI provider config UI **[S]**

**Story:** Mint admin, szeretném látni és (korlátozottan) konfigurálni
az AI providereket.

**AC:**
- `GET /api/v1/ai-providers` lista.
- `PATCH` korlátozott mezőkre (`enabled`, `model`).
- A `PrivacyMode` admin felületen csak megjeleníthető — nem módosítható
  (lásd api-design.md 21.2).

**Tasks:**
- **BE:** `ProvidersController`.
- **FE:** Settings oldal AI szakasz.

---

### K3. Backup és restore útmutató **[M]**

**Story:** Mint admin, szeretnék pontos útmutatást a backup és restore
folyamatra.

**AC:**
- `docs/DELIVERY.md` tartalmazza a `pg_dump` + `age` parancsokat.
- Compose `backup` szervízzel cron-jellegű napi dumpolás.

**Tasks:**
- **Infra:** backup script + image, `data/backups/manifest.txt` append-only
  log.
- **Doc:** runbook.

---

## 12. Epic L — Dashboard

### L1. Aggregált dashboard endpoint **[M]**

**AC:**
- `GET /api/v1/dashboard` < 200 ms.
- Tartalom: közelgő határidők (10), suggestions count, recent docs (5),
  overdue reminders (5), saved searches.

**Tasks:**
- **BE:** `DashboardQuery` (3-4 párhuzamos SQL).
- **FE:** dashboard 4 widget.

---

### L2. Lecsúszott emlékeztetők összesítő **[S]**

**AC:**
- A dashboard külön blokkban mutatja a `Skipped`-be került reminder-eket.
- Akció: „Újraütemezem" / „Elvetem".

---

## 13. Epic M — Deployment és üzemeltetés

### M1. Docker Compose alap stack **[M]**

**AC:**
- `make up` indítja az `api`, `workers`, `postgres`, `ollama`, `web`
  szervízeket.
- Volume-ok: `pgdata`, `documents`, `ollama-models`, `logs`.
- Csak LAN bind (csak `127.0.0.1` és LAN IP-k).

**Tasks:**
- **Infra:** compose, nginx config (`web` service), volume-ok.

---

### M2. nginx TLS belső CA-val **[S]**

**AC:**
- mkcert-tel generált belső CA + tanúsítvány.
- Telepítési útmutató a háztartási eszközökre.

**Tasks:**
- **Infra:** nginx config, mkcert-script, dokumentáció.

---

### M3. Health probes, metrics **[S]**

**AC:**
- `/healthz/live`, `/healthz/ready` HTTP probe-ok.
- (Opcionális) Prometheus exporter.

---

### M4. Telepítési dokumentáció (DELIVERY.md) **[M]**

**AC:**
- Egy átlagos technikai userszerű admin követni tudja, és 1 órán belül
  élesít az otthoni PC-jén.

**Tasks:**
- **Doc:** `docs/DELIVERY.md` runbook (setup, backup, restore,
  incident response).

---

## 14. Megvalósítási sorrend (12 fázis)

A fázisok ugyanazok, mint az `implementation-plan.md`-ben — minden fázis a
megfelelő epic-eket / story-kat foglalja magába. Itt csak a high-level
hozzárendelés (a részleteket a következő doksi adja).

| Fázis | Cél | Tartalom (story-k) |
|---|---|---|
| 1 | Solution + CI | A1, A4, A5 |
| 2 | DB és EF Core | A2, B1 |
| 3 | Alap API + auth | A3, B2, B3, C1 (csak upload skeleton) |
| 4 | Angular shell | A3 (FE), A5 (FE), C2 (skeleton) |
| 5 | Dokumentum upload + tárolás | C1 (teljes), C2, C3 (skeleton) |
| 6 | Text extraction (PdfPig + Tesseract) | D1 (queue előkészület), D3, D4 |
| 7 | AI absztrakció | D1 (teljes), D2 |
| 8 | Summary + extraction + embedding | D5–D11 |
| 9 | Search & Q&A | E1–E5 (E6/E7 opc.) |
| 10 | Reminders & notifications | F1–F3, G1–G3, G6 |
| 11 | Dashboard + topic/tag | I1, I2, L1 |
| 12 | Hardening, tests, security | J1–J4, K1 (S), K2 (S), K3, M1–M4, security audit |

**Total story count:** ~50 (Must: ~28, Should: ~18, Could: ~4)

---

## 15. Kapacitás-becslés (sanity check)

Egyetlen full-stack fejlesztő AI-asszisztens támogatással:

| Fázis | Becslés | Megj. |
|---|---|---|
| 1 | 1 nap | scaffold |
| 2 | 2 nap | séma + EF Core |
| 3 | 2 nap | auth + alap CRUD |
| 4 | 2 nap | shell |
| 5 | 2 nap | upload + tárolás |
| 6 | 2 nap | OCR |
| 7 | 2 nap | AI provider |
| 8 | 4 nap | pipeline összes lépés |
| 9 | 3 nap | search & Q&A |
| 10 | 3 nap | reminders |
| 11 | 2 nap | dashboard + topic |
| 12 | 3 nap | hardening, tests, docs |

**Összes:** ~28 nap egyedi fejlesztő + 2-3 napos puffer integrációra =
**~6 hét** kalendárium szerint.

A factory párhuzamos fejlesztéssel (architect → 3-4 párhuzamos feature-ág)
ezt jelentősen rövidítheti — várható **2-3 hetes** futási idő, ha a
worktree-stratégia és a kontraktusok zöld zónán maradnak.

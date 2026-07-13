# AI-minőségjavítás F0–F8 — Sonnet-implementációs feladatkártyák

> Státusz: IMPLEMENTÁCIÓS SPEC v1.0 · Dátum: 2026-07-12
> Forrás: `docs/fable_review/06-ai-minoseg-vegrehajtasi-terv.md` · Háttér: [05-ai-minoseg-javitasok.md](05-ai-minoseg-javitasok.md) (REFERENCIA)
> Ágséma: `feature/ai-q-f<N>-<nev>` · Egy fázis = egy merge-elhető egység.

## Vezérelvek (a subagent-promptba másolandó)

1. **F2 után minden minőségi változás csak eval-bizonyítékkal merge-elhető**
   (F1 és F3 kivétel: determinisztikus hibajavítások).
2. **Visszafordíthatóság:** minden viselkedés-változás konfiggal kikapcsolható
   vagy prompt-verzióval visszaállítható.
3. A [00-README](00-README-sonnet-utasitasok.md) globális szabályai érvényesek
   (scope-fegyelem, 2-próbálkozás-szabály, zárójelentés).

## Sorrend és függések (zárt döntés)

**F0 → F1 → F2 → F3 → (F4 ∥ F5) → F6 → F7 → F8.**
F0/F1/F2/F3 egymástól függetlenek, párhuzamosan is indíthatók külön worktree-ben;
F4–F6/F8 KAPUJA az F2 (eval nélkül tilos merge-elni őket), F5/F6 az F1-et is kéri, F7 az F1-et.

---

## F0 — Szemantikus keresés RBAC-ellenőrzése (biztonsági — AZONNAL)

**Ág:** `feature/ai-q-f0-semantic-rbac` · **Effort:** 0,5 nap · **Modell:** sonnet.
**Ez elsősorban VIZSGÁLAT** — a javítás csak akkor scope, ha a teszt rést bizonyít.

**Olvasd el először:**
- `SemanticSearchService` (a SQL-szűrő: `is_private = false OR owner` — és a
  4 UNION-ág WHERE-jei)
- `FamilyOsAuthorizationService` (`CanReadDocument` és a Child-szabály)
- a search-handler réteg (van-e utószűrés a service UTÁN?)
- `TaskDeadlineFtsSearchService` (ugyanez a kérdés az FTS-ágra)
- egy meglévő Testcontainers-integrációs teszt (fixture-minta)

**Lépések:**
1. Reprodukciós integrációs teszt: Child user + MÁSIK családtaghoz rendelt,
   nem-privát dokumentum → szemantikus keresés Child-ként.
   - Ha a handler utószűr és a találat NEM jön át → a teszt zöld; F0 ennyi:
     a teszt regresszióként marad, commit, kész.
   - Ha a találat átjön → IDOR-jellegű rés, folytasd a 2. lépéssel.
2. Javítás (csak rés esetén): `SemanticSearchService.SearchAsync`
   szignatúra-bővítés (`userId` mellé `role`, `familyMemberId`), a 4 UNION-ág
   WHERE-je a `FamilyOsAuthorizationService` szabályaihoz igazítva.
   Ugyanez az ellenőrzés + igazítás az FTS-ágra.
3. Komment mindkét search-service-be: „a szabálykészlet forrása:
   security-privacy.md §4.3 + FamilyOsAuthorizationService — módosítás csak
   együtt".

**Elfogadás:** Child nem kap találatot idegen dokumentumból (szemantikus +
FTS + hibrid ágon) · Adult/Admin viselkedés változatlan (meglévő tesztek zöldek).
**Ellenőrzés:** `dotnet test` zöld (új teszttel) ×2 futás.
**Tilos:** a keresés-relevancia bármilyen hangolása; RRF/threshold módosítás.

---

## F1 — Inference-paraméterek (num_ctx, temperature, format, seed, keep_alive)

**Ág:** `feature/ai-q-f1-inference-opts` · **Effort:** 1 nap · **Modell:** sonnet.
**Ez a legnagyobb hatású javítás a csomagban** (a 2048-as implicit num_ctx ma
csendben levágja a bemenet felét).

**Olvasd el először:**
- `src/FamilyOs.Infrastructure.Ai/Options/OllamaOptions.cs`
- `OllamaHttpClient.cs` (`PostChatAsync` + a kérés-DTO-k)
- `OllamaAiProvider.cs` (hol fut ki a hívás; hol érhető el a `PromptId`)
- `docker-compose.yml` + `docker-compose.rpi.yml` (env-minta)

**Lépések:**
1. `OllamaOptions.cs` bővítése — PONTOSAN ez a struktúra:
```csharp
public sealed class OllamaInferenceOptions
{
    public int NumCtx { get; set; } = 8192;
    public double Temperature { get; set; } = 0.1;
    public int? Seed { get; set; }              // eval-futásokhoz
    public string KeepAlive { get; set; } = "30m";
    public bool JsonMode { get; set; } = true;
}
// OllamaOptions-be:
public OllamaInferenceOptions Defaults { get; set; } = new();
public Dictionary<string, OllamaInferenceOptions> Tasks { get; set; } = new();
// kulcs: normalizált PromptId ("extract-deadlines.v1.txt" → "extract-deadlines")
```
   **Döntés:** a per-feladat felülbírálást a PromptId vezérli — az `AiPrompt`
   már minden hívásban hordozza, ezért hívóhelyet NEM kell módosítani.
   Kulcs-normalizálás: fájlnév → alapnév verzió/kiterjesztés nélkül (így a
   konfig prompt-verzióváltáskor nem törik).
2. `OllamaChatRequest` bővítése:
```csharp
[JsonPropertyName("format")]     public string? Format { get; set; }   // "json"
[JsonPropertyName("keep_alive")] public string? KeepAlive { get; set; }
[JsonPropertyName("options")]    public OllamaRequestOptions? Options { get; set; }
// OllamaRequestOptions: num_ctx, temperature, seed (snake_case JSON-nevekkel)
```
   `PostChatAsync` szignatúra: `(model, systemPrompt, userPrompt, inferenceOpts, ct)`.
   A `format:"json"` CSAK `JsonMode=true` esetén megy ki (a Q&A-nál is true —
   a válasz ott is JSON).
3. `OllamaAiProvider` opció-feloldás:
```csharp
var opts = _options.Tasks.TryGetValue(Normalize(prompt.PromptId), out var t)
    ? t : _options.Defaults;
```
4. Konfig: `appsettings.json` defaultok + compose env-kulcsok
   (`Ai__Ollama__Defaults__NumCtx` stb.); `docker-compose.rpi.yml`-ben
   `NumCtx=4096` (memória). `docs/ai-pipeline.md` 8. szakasz kontrakt-delta.

**Tesztek (kötelező):**
- unit: a kérés-JSON tartalmazza a `format`/`options`/`keep_alive` mezőket;
  per-task felülbírálás érvényesül; ismeretlen PromptId → Defaults;
  `JsonMode=false` → nincs format-mező.
- regresszió: `PipelineGoldenTests` zöld (stub-útvonal érintetlen).

**Elfogadás + mérés:** kézi füstteszt valódi Ollama ellen (`make up` +
1 dokumentum-upload — összefoglaló/kinyerés működik, érvényes JSON);
a PR-leírásban rögzítve: ugyanazon mintadokumentumon a kinyert határidők
száma a régi (implicit 2048) vs. új (8192) beállítással.
**Kockázat/rollback:** nagyobb num_ctx → nagyobb RAM; rollback = env-visszaállítás.
**Tilos:** modellcsere (F6); prompt-módosítás (F4); embedder érintése (F5).

---

## F2 — Eval-harness és golden-készlet (KAPU minden továbbihoz)

**Ág:** `feature/ai-q-f2-eval-harness` · **Effort:** 2–3 nap · **Modell:** sonnet
(a készlet-tartalom generálásához opus megfontolható — minőségkritikus).
**⛔ EMBERI KAPU:** ADR-0014 (eval-folyamat + kapu-szabály) DRAFT-ig; továbbá
a küszöb-szabály CLAUDE.md-be emelése emberi jóváhagyást igényel.

**Olvasd el először:**
- `PipelineGoldenTests` (mit véd ma — a stub-minta)
- `JsonSchemaLite` (újrahasznosítható az expected-fájlok validálásához)
- `Makefile` (target-minta) · ADR-0006 (a megsértett eval-követelmény)

**Lépések:**
1. Készlet-struktúra létrehozása — PONTOSAN így:
```
eval/
  datasets/
    documents/           # 20–30 magyar mintadokumentum (txt — az ExtractText
      szamla-01.txt      #   UTÁNI állapotot reprezentálja)
      biztositas-02.txt
      iskola-03.txt ...
    emails/              # 15–20 e-mail (subject + body)
    questions.jsonl      # QA-párok: kérdés + elvárt tények + forrás-doksi
    intents.jsonl        # kérdés → elvárt SearchIntent
  expected/
    szamla-01.expected.json  # {classify, deadlines[], tasks[], summaryMustContain[]}
  scanned/               # F8-hoz: 5–10 szkennelt PDF/kép
```
   **A készlet SZINTETIKUS adatokból áll** — valós családi adat TILOS
   (security-privacy.md §12). Tartalom: számla, biztosítás, iskolai levél,
   orvosi lelet, garancia — realisztikus magyar szövegek, változatos
   dátumformátumokkal (2026. 07. 12. / 2026.07.12 / július 12.).
2. Runner: új projekt `tests/FamilyOs.Eval/` (xUnit,
   `[Trait("Category","Eval")]`); a default `make test` és a CI KIZÁRJA;
   futtatás: `make eval`. Konfig: `OLLAMA_BASE_URL` env (default
   `http://localhost:11434`), `Seed` kötelezően beállítva (F1-ből).
3. Metrikák feladatonként:

| Feladat | Metrika | Kezdeti kapu |
|---|---|---|
| ExtractDeadlines | precision/recall/F1 (dátum+cím, cím fuzzy ≥ 0.8) | baseline − 5%p |
| Classify (dok) | accuracy + tévesztési mátrix | baseline − 5%p |
| ClassifyEmail | accuracy + külön High-recall | High-recall ≥ baseline |
| Summarize | `summaryMustContain` kulcstény-lefedés % | baseline − 5%p |
| Q&A | groundedness (citációk validak) + tény-egyezés | baseline − 5%p |
| IntentClassifier | accuracy az intents.jsonl-en | baseline − 2%p |
| Retrieval | recall@10 (elvárt forrásdoksi a top-10-ben) | baseline |

4. Kimenet: `eval/results/<datum>-<git-sha>-<modell>.json` + konzol-összegző;
   az eredményfájl commitolható (trend; később a cr260712-06 dashboard olvassa).
5. Baseline-futás a jelenlegi állapoton (F1 utáni paraméterekkel, default
   modellel) → `eval/results/baseline-*.json` commitolva; küszöbök a
   baseline-ból számítva `eval/thresholds.json`-ba.
6. ADR-0014 DRAFT: eval-folyamat + szabály („prompt/modell/embedding/
   inference-paraméter PR kötelezően friss eval-eredménnyel") — ⛔ kapu.

**Elfogadás:** `make eval` egy paranccsal fut, seed-elt · ≥20 dok + 15 e-mail
+ 15 QA + 20 intent · baseline commitolva, küszöbök rögzítve · ADR-0014 DRAFT.
**Tilos:** produkciós kód módosítása; a CI-ba eval-lépés bekötése (lokális
Ollama kell — a CI-integráció külön döntés).

---

## F3 — Parse-robusztusság és truncation

**Ág:** `feature/ai-q-f3-parse-truncation` · **Effort:** 1,5 nap · **Modell:** sonnet.

**Olvasd el először:**
- `ToolCallPlanner` (a MÁSOLANDÓ robusztus minta: `JsonBlockExtractor` +
  séma-validálás + 1 korrekciós retry)
- `JsonBlockExtractor`
- a 9 extractor: `OllamaDeadlineExtractor`, `OllamaDocumentClassifier`,
  `OllamaDocumentSummarizer`, `OllamaEmailClassifier`,
  `OllamaFinancialRecordExtractor`, `OllamaMedicalRecordExtractor`,
  `OllamaQuestionAnswerer`, `OllamaTaskExtractor`, `OllamaWarrantyExtractor`
  (hol hívnak nyers `JsonDocument.Parse`-t és hol vágnak `text[..12000]`-t)
- `ai_processing_job` entitás (hiba-mező szerkezete)

**Lépések:**
1. Közös parser — `AiJsonResponse` (elhelyezés: Infrastructure.Ai alá, ha a
   függőségi irány úgy tisztább; döntsd el a projektreferenciák alapján és
   indokold a zárójelentésben):
```csharp
public static class AiJsonResponse
{
    // JsonBlockExtractor.ExtractFirstObject + JsonDocument.Parse,
    // hibánál (null, "diagnosztikai ok") — SOHA nem dob.
    public static (JsonDocument? Doc, string? Error) TryParse(string content);
}
```
   Bevezetés mind a 9 extractorban a nyers `JsonDocument.Parse` helyett.
2. Korrekciós retry: a `ToolCallPlanner` 1-retry mintája közös helperbe —
   parse-hibánál EGYSZER újrapróbál „Az előző válaszod nem volt érvényes
   JSON…" utasítással. (F1 `format:"json"` mellett ritka — védőháló.)
3. Láthatóság: parse-hiba → Warning-log (PromptId + hiba + kimenet első
   200 karaktere); az `ai_processing_job`-on strukturált ok (`parse_failed`)
   a meglévő hiba-mezőben — az adminon elváljon a „0 találat" a
   „nem tudtam értelmezni"-től.
4. Truncation-helper:
```csharp
AiTextWindow.HeadTail(text, headChars: 8000, tailChars: 4000, separator: "\n[…]\n")
```
   Bevezetés az ÖT `text[..12000]` helyén (deadline/task/classifier/
   financial/medical). A summarizer NEM változik (map-reduce jól működik).
   A vágás ténye Debug-logba (dokumentumhossz + ablakméret).

**Tesztek:** unit — kerítéses (```json), prózával kevert, csonka JSON →
helyes parse vagy strukturált hiba; retry pontosan egyszer fut; HeadTail —
rövid szöveg érintetlen, hosszúnál fej+vég+jelölő; `PipelineGoldenTests`
bővítése kerítéses stub-válasz esetekkel.
**Elfogadás:** mind a 9 extractor a közös parsert használja · eval (ha F2 kész):
deadline-recall nem romlik — PR-ben rögzítve.
**Tilos:** prompt-szöveg módosítása; a summarizer átírása; QA snippet-méret (F5).

---

## F4 — Prompt v2 (ékezetes magyar + few-shot) — KAPU: F2

**Ág:** `feature/ai-q-f4-prompts-v2` · **Effort:** 1,5 nap · **Modell:** sonnet.
**Merge-feltétel: eval-eredmény a PR-ben. F2 nélkül el se kezdd.**

**Olvasd el először:** `Prompts/` teljes katalógus (a `qa-magyar.v1` a JÓ
minta — ékezetes) · `PromptCatalog` (verzió-mechanika) · `PromptTemplateTests`.

**Lépések (promptonként KÜLÖN commit, saját eval-futással):**
1. `sysprefix.v2.txt` — ékezetes magyar, változatlan szabálytartalom.
2. `classify.v2.txt`, `classify-email.v2.txt`, `extract-deadlines.v2.txt`,
   `extract-tasks.v2.txt` — ékezetesítés + 1–2 few-shot példa elvárt
   JSON-kimenettel. **A few-shot példák NEM szerepelhetnek az
   eval-készletben** (adatszivárgás-tilalom) — írj újakat.
3. System-promptok nyelvi egységesítése: az extractorok angol egysorosai
   („You are a helpful assistant…") magyarra, a sysprefixszel konzisztensen.
4. `PromptCatalog` átállítás v2-re EGYENKÉNT, eval-futással; ha egy v2 ront,
   az a prompt v1-en marad, a tanulság a v2-fájl fejlécében dokumentálva.

**Elfogadás:** minden átállított prompt eval-metrikája ≥ baseline (PR-ben
dokumentálva) · few-shot ∉ eval-készlet · `PromptCatalog.GetVersion` v2-t ad
(`PromptTemplateTests` bővítve).
**Tilos:** szabálytartalom-változtatás ékezetesítés közben; v1-fájlok törlése.

---

## F5 — Embedding-minőség — KAPU: F1+F2

**Ág:** `feature/ai-q-f5-embedding` · **Effort:** 2 nap · **Modell:** sonnet.

**Olvasd el először:** `OllamaEmbedder.cs` (a 38. sor környéke — prefix nélküli
hívás; `EmbedBatchAsync`; melyik végpontot hívja) · `IEmbedder` interfész +
MINDEN hívóhelye (`EmbedJobRunner`, `EmbedBackfillService`,
`QueryEmbeddingCache`) · `SemanticSearchService` (embedding_model szűrés) ·
`OllamaQuestionAnswerer.cs:72` · `docs/search-strategy.md` §2.3.

**Lépések:**
1. **Task-prefixek** — `IEmbedder` bővítése:
```csharp
public enum EmbeddingPurpose { Document, Query }
Task<float[]> EmbedAsync(string text, EmbeddingPurpose purpose, CancellationToken ct);
```
   `OllamaEmbedder`: `Document` → `"search_document: " + text`, `Query` →
   `"search_query: " + text` — modell-függő prefix-térképpel (dictionary),
   hogy a későbbi modellcsere ne igényeljen kódmódosítást.
   Hívóhelyek: EmbedJobRunner/EmbedBackfillService → Document;
   QueryEmbeddingCache → Query.
2. **Vektortér-verziózás:** a prefixes embedding MÁS vektortér — régi és új
   vektor nem keverhető. `OllamaEmbedder.ModelName` → logikai azonosító:
   `"nomic-embed-text:v1.5#p1"` (`#p1` = prefix-séma verzió).
   `EmbedBackfillService` bővítése: a RÉGI `embedding_model`-lel tárolt
   chunkokat is újra-embeddelje (batch, megszakítható, loggal +
   folyamatjelzéssel — nagy korpusznál órákig futhat).
   **Ismert, elfogadott mellékhatás:** amíg a backfill fut, az át nem
   embeddelt tartalom szemantikusan nem találódik (FTS-ág él) — ezt a
   PR-leírásban jelezd.
3. **API-modernizálás:** deprecated `/api/embeddings` → `/api/embed`
   (natív batch: `input: [...]`); `EmbedBatchAsync` egy hívásban,
   szekvenciális fallback megtartva.
4. **QA-kontextus:** `OllamaQuestionAnswerer` snippet 500 → 1500 char,
   konfigból (`Ai:Qa:SnippetChars`); NumCtx-szel összehangolva (8 forrás ×
   1500 char ≈ 4,5k token + prompt — fér a 8192-be).
   `minSimilarity` 0.60 → konfig (`Ai:Search:MinSimilarity`).
5. **(FELTÉTELES — külön döntés, ne csináld automatikusan):**
   embedding-modell csere (pl. bge-m3) CSAK ha az eval recall@10-en ≥ 5%p
   javulást mutat; ADR-0015 DRAFT (⛔ EMBERI KAPU); dimenzió-változásnál
   migráció kell (a `vector(768)` oszloptípus dimenzió-függő!).

**Tesztek:** unit — purpose-alapú prefixelés, batch-formátum; integrációs —
backfill újra-embeddel régi modell-ID-jű chunkot.
**Elfogadás:** eval recall@10 és QA-metrika ≥ baseline (PR-ben) ·
`docs/search-strategy.md` §2.3 frissítve (prefix + modell-ID séma).
**Tilos:** RRF/fúzió logika módosítása; chunk-méret változtatás; modellcsere
eval-bizonyíték nélkül.

---

## F6 — Modell-stratégia (compose-default + per-feladat routing) — KAPU: F1+F2

**Ág:** `feature/ai-q-f6-model-routing` · **Effort:** 1–2 nap · **Modell:** sonnet.
**⛔ EMBERI KAPU:** ADR-0006 frissítése DRAFT-ig; a compose-default modell
cseréje emberi jóváhagyás után.

**Olvasd el először:** F1 kész kódja (`Tasks` szótár) · `OllamaOptions.cs`
(DefaultModel) · docker-compose*.yml modell-env · ADR-0006 ·
`docs/deploy-raspberry-pi.md`.

**Lépések:**
1. Per-feladat modell: az F1-es `OllamaInferenceOptions` kap `Model` mezőt
   (nullable — null = DefaultModel); feloldás a PromptId alapján az
   `OllamaAiProvider`-ben (F1-gyel azonos mechanika).
2. Compose-default felülvizsgálat: a `qwen3-coder:30b` (kód-modell!)
   kiváltása általános instruct-modellel. Jelöltek erős gépre:
   qwen3-instruct variáns / gemma3 / llama3.1-8b+; RPi-n marad 3b-osztály.
   **Döntés KIZÁRÓLAG eval-eredmény alapján:** teljes készlet, legalább
   2 jelölt, seed-elt futás, eredmények commitolva.
3. ADR-0006 v2 DRAFT: default + ajánlott modellek gépprofilonként,
   eval-linkekkel; `docker-compose.strong-pc.yml` + `deploy-raspberry-pi.md`
   szinkron.

**Elfogadás:** unit — PromptId → modell-feloldás, ismeretlen → DefaultModel ·
eval-összevetés ≥ 2 jelölttel commitolva · ADR-0006 v2 DRAFT.
**Tilos:** modell merge-elése eval nélkül; a 30b letöltésének automatizálása
CI-ban.

---

## F7 — Ollama-konkurrencia és ops — KAPU: F1

**Ág:** `feature/ai-q-f7-ollama-concurrency` · **Effort:** 1 nap · **Modell:** sonnet.

**Olvasd el először:** Hangfire-konfiguráció a workers Program.cs-ben
(WorkerCount=4, queue-k) · `OllamaHttpClient` (timeout-kezelés,
`TimeoutSeconds=120`) · az admin `queue-stats` végpont (mit vár).

**Lépések:**
1. AI-hívás throttle: közös `SemaphoreSlim` az `OllamaHttpClient` köré
   (konfig: `Ai:Ollama:MaxConcurrentRequests`, default 2; RPi-profil: 1).
   Megjegyzés: processzenként külön él (API + workers), ezért kell a 2. is.
2. Hangfire AI-queue: az AI-jobok dedikált `ai` queue-ra (WorkerCount 1–2);
   nem-AI jobok (retention, digest) maradnak a default queue-n 4 workerrel.
   Ellenőrizd: a job-regisztrációknál queue-attribútum vagy enqueue-paraméter
   a repo mintája szerint.
3. Hideg-latency mérés: keep_alive (F1) hatása — worker-újraindulás utáni
   első job időtartama logból; az eredmény a PR-leírásba.
4. Timeout-differenciálás: `TimeoutSeconds` feladatfüggővé az F1-es
   per-task options mintájával (summarize nagy doksin > 120 s lehet;
   classify-nek sok a 120).

**Elfogadás:** terheléses füstteszt — 10 dokumentum egyidejű feltöltése:
nincs timeout-hiba, a jobok sorban lefutnak · a queue-bontás nem töri a
meglévő `queue-stats` végpontot (admin AI-jobs felület működik).
**Tilos:** WorkerCount globális csökkentése (a nem-AI jobokat lassítaná);
retry-politika módosítása.

---

## F8 — Nyelvkezelés és OCR — KAPU: F2

**Ág:** `feature/ai-q-f8-lang-ocr` · **Effort:** 2 nap · **Modell:** sonnet.

**Olvasd el először:** `DetectLanguageJobRunner` + a `SupportedLanguages`
konfig MINDEN felhasználási helye (mely jobok skippelnek nem-támogatott
nyelvnél — térképezd fel, mielőtt nyúlsz hozzá) · a Tesseract-ág (traineddata
betöltés, előfeldolgozás hiánya) · `eval/scanned/` (F2-ből).

**Lépések:**
1. Angol támogatás: `SupportedLanguages` `"hu"` → `"hu;en"` — ELŐBB az
   elágazási pontok feltérképezése és az eval-készlet bővítése 5 angol
   dokumentummal (angol számla, jegy); eval-futás előtte/utána.
2. Tesseract: `hun+eng` együttes traineddata; kép-előfeldolgozás
   (deskew + binarizálás) az OCR elé; mérés az `eval/scanned/` mintákon
   (karakterhiba-arány + downstream deadline-recall).
3. Vegyes nyelvű bemenet: a sysprefix 2. szabálya („magyarul fogalmazz…")
   kezeli — eval-esettel lefedni.

**Elfogadás:** angol számla-minta végigmegy a pipeline-on (classify +
deadline) · OCR-minőség dokumentálva, preprocessing-javulás mérve.
**Tilos:** OCR-könyvtár csere; új nyelvek a hu+en-en túl.

---

## Ütemterv-összegzés

| Fázis | Effort | Kapu | Fő kockázat |
|---|---|---|---|
| F0 RBAC | 0,5 nap | – | ha rés van: azonnali javítás |
| F1 inference | 1 nap | – | RAM-igény (profilonként hangolható) |
| F2 eval | 2–3 nap | – | készlet-minőség = mérés-minőség |
| F3 parse+trunc | 1,5 nap | – | alacsony |
| F4 prompt v2 | 1,5 nap | F2 | prompt-regresszió (eval véd) |
| F5 embedding | 2 nap | F1,F2 | re-embed átmeneti keresés-kiesés |
| F6 modell | 1–2 nap | F1,F2 | VRAM; eval dönt; ⛔ ADR |
| F7 ops | 1 nap | F1 | queue-átszervezés mellékhatásai |
| F8 nyelv/OCR | 2 nap | F2 | Tesseract-plafon |

**Összesen:** ~13–15 munkanap. Kritikus út: F1 → F2 → (F4/F5/F6 mérve).

## Definition of Done (a teljes programra)

- [ ] Minden fázis merge-elve zöld kapukkal (build, unit, integrációs, review).
- [ ] Baseline vs. záró eval: egyetlen metrika sem romlott; cél-metrikák
      (deadline-F1, e-mail High-recall, QA groundedness, recall@10) javulása
      dokumentálva.
- [ ] ADR-0006 frissítve; ADR-0014 (eval) elfogadva; ADR-0015 (embedding)
      ha csere történt.
- [ ] `docs/ai-pipeline.md` + `docs/search-strategy.md` szinkronban a kóddal
      (ne keletkezzen új doksi-kód rés — 02-es doksi tanulsága).
- [ ] A 05-ös referencia-doksi tételei lezárva vagy explicit elhalasztva.

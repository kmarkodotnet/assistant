# AI-minőségjavítás — részletes végrehajtási terv

> Státusz: TERV v1.0 · Dátum: 2026-07-12
> Alap: [05-ai-minoseg-javitasok.md](05-ai-minoseg-javitasok.md)
> Munkamódszer: fázisonként külön feature-ág (`feature/ai-q-f<N>-<nev>`),
> conventional commits, merge előtt code-review + zöld kapuk (CLAUDE.md).

## Vezérelvek

1. **Előbb mérni, aztán optimalizálni.** Az F2 (eval-harness) után minden
   minőségi változtatás (prompt, modell, embedding) csak eval-bizonyítékkal
   merge-elhető. Az F1 és F3 kivétel: azok determinisztikus hibajavítások.
2. **Egy fázis = egy merge-elhető egység**, saját acceptance-kritériumokkal.
3. **Visszafordíthatóság:** minden viselkedés-változás konfiggal
   kikapcsolható vagy prompt-verzióval visszaállítható.

## Fázistérkép és függések

```
F0 (RBAC-ellenőrzés) ──────────────► független, AZONNAL indítható
F1 (inference-paraméterek) ────────► független
F2 (eval-harness) ─────────────────► független
F3 (parse + truncation) ───────────► független (F2 után mérve jobb)
F4 (prompt v2) ────────────────────► F2 KÖTELEZŐ előfeltétel
F5 (embedding) ────────────────────► F1, F2 előfeltétel
F6 (modell-routing/választás) ─────► F1, F2 előfeltétel
F7 (konkurrencia/ops) ─────────────► F1 után érdemes
F8 (nyelv/OCR) ────────────────────► F2 előfeltétel
```

Javasolt sorrend: **F0 → F1 → F2 → F3 → (F4 ∥ F5) → F6 → F7 → F8.**

---

## F0 — Szemantikus keresés RBAC-ellenőrzése (biztonsági, azonnali)

**Ág:** `feature/ai-q-f0-semantic-rbac` · **Effort:** 0,5 nap

A `SemanticSearchService` SQL-szűrője (`is_private = false OR owner`)
lazább, mint a `FamilyOsAuthorizationService.CanReadDocument` Child-szabálya
(Child csak a hozzá rendelt, nem-privát elemet láthatja).

**Lépések:**
1. Reprodukció integrációs teszttel (Testcontainers): Child user +
   másik családtaghoz tartozó nem-privát dokumentum → szemantikus keresés.
   - Ha a handler-réteg utószűr → a teszt zöld, F0 lezárva egy
     regressziós teszttel.
   - Ha átjön a találat → tényleges IDOR-jellegű rés, javítás kötelező.
2. Javítás (ha kell): a role + familyMemberId lekötése a SQL-be
   (`SemanticSearchService.SearchAsync` szignatúra-bővítés: `userId` mellé
   `role`, `familyMemberId`), a négy UNION-ág WHERE-jének igazítása a
   `FamilyOsAuthorizationService` szabályaihoz. Ugyanez az ellenőrzés az
   FTS-ágra (`TaskDeadlineFtsSearchService`).
3. A szabálykészlet EGY helyen dokumentálása (security-privacy.md 4.3-ra
   hivatkozó komment mindkét search-service-ben).

**Acceptance:**
- [ ] Integrációs teszt: Child nem kap találatot idegen dokumentumból
      (szemantikus + FTS + hibrid ágon).
- [ ] Adult/Admin viselkedés változatlan (meglévő tesztek zöldek).

---

## F1 — Inference-paraméterek (num_ctx, temperature, format, seed, keep_alive)

**Ág:** `feature/ai-q-f1-inference-opts` · **Effort:** 1 nap

### 1.1 `OllamaOptions` bővítése

`src/FamilyOs.Infrastructure.Ai/Options/OllamaOptions.cs`:

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
//                        ^ kulcs: PromptId (pl. "extract-deadlines.v1.txt"
//                          → normalizált: "extract-deadlines")
```

A per-feladat felülbírálást a **PromptId** vezérli — az `AiPrompt` már
minden hívásban hordozza, ezért **egyetlen hívóhelyet sem kell módosítani**.
Kulcs-normalizálás: fájlnév → alapnév verzió/kiterjesztés nélkül, hogy a
konfig ne törjön prompt-verzióváltáskor.

### 1.2 `OllamaHttpClient` kérés-bővítés

`OllamaChatRequest`-be:

```csharp
[JsonPropertyName("format")]  public string? Format { get; set; }      // "json"
[JsonPropertyName("keep_alive")] public string? KeepAlive { get; set; }
[JsonPropertyName("options")] public OllamaRequestOptions? Options { get; set; }
// OllamaRequestOptions: num_ctx, temperature, seed
```

`PostChatAsync` szignatúra: `(model, systemPrompt, userPrompt, inferenceOpts, ct)`.
A `format: "json"` **csak** akkor menjen ki, ha a feladat JSON-t vár
(`JsonMode = true` — a Q&A-nál is igaz, mert a válasz ott is JSON).

### 1.3 `OllamaAiProvider` — opció-feloldás

```csharp
var opts = _options.Tasks.TryGetValue(Normalize(prompt.PromptId), out var t)
    ? t : _options.Defaults;
```

### 1.4 Konfiguráció

- `docker-compose*.yml` + `appsettings.json`: `Ai__Ollama__Defaults__NumCtx`
  stb. env-kulcsok; RPi-profilban (`docker-compose.rpi.yml`) kisebb
  `NumCtx` (4096) a memória miatt.
- Dokumentálás: `docs/ai-pipeline.md` 8. szakasz kontrakt-delta.

**Tesztek / acceptance:**
- [ ] Unit: a kérés-JSON tartalmazza a `format`/`options`/`keep_alive`
      mezőket; per-task felülbírálás érvényesül; ismeretlen PromptId →
      Defaults.
- [ ] Kézi füstteszt valódi Ollama ellen (`make up` + 1 dokumentum-upload):
      összefoglaló/kinyerés továbbra is működik; a válasz érvényes JSON.
- [ ] Regresszió: `PipelineGoldenTests` zöld (stub-útvonal érintetlen).
- [ ] Mérés rögzítése a PR-leírásban: ugyanazon mintadokumentumon a
      kinyert határidők száma num_ctx=2048(implicit) vs 8192 mellett.

**Kockázat / rollback:** nagyobb `num_ctx` → nagyobb RAM-igény; a
compose-értékek gépprofilonként hangolhatók, rollback = env-visszaállítás.

---

## F2 — Eval-harness és golden-készlet (a kapu minden továbbihoz)

**Ág:** `feature/ai-q-f2-eval-harness` · **Effort:** 2–3 nap

### 2.1 Készlet-struktúra

```
eval/
  datasets/
    documents/           # 20–30 magyar mintadokumentum (txt — a pipeline
      szamla-01.txt      #   ExtractText UTÁNI állapotát reprezentálja)
      biztositas-02.txt
      iskola-03.txt ...
    emails/              # 15–20 e-mail (subject + body)
    questions.jsonl      # QA-párok: kérdés + elvárt tények + forrás-doksi
    intents.jsonl        # kérdés → elvárt SearchIntent
  expected/
    szamla-01.expected.json   # elvárt kimenetek feladatonként:
                               # {classify, deadlines[], tasks[], summaryMustContain[]}
  scanned/               # F8-hoz: 5–10 szkennelt PDF/kép OCR-mintának
```

Az elvárt-fájl sémáját a runner validálja (JsonSchemaLite újrahasznosítható).
A készlet **szintetikus vagy anonimizált** adatokból áll — valós családi
adat nem kerülhet a repóba (security-privacy.md 12.).

### 2.2 Runner

Új projekt: `tests/FamilyOs.Eval/` (xUnit, `[Trait("Category","Eval")]`,
a default `make test` és a CI **kizárja**; futtatás: `make eval`).

- Konfig: `OLLAMA_BASE_URL` env (default `http://localhost:11434`),
  `Seed` kötelezően beállítva (F1) a reprodukálhatósághoz.
- Feladatonkénti mérés:

| Feladat | Metrika | Kezdeti kapu |
|---|---|---|
| ExtractDeadlines | precision / recall / F1 (dátum+cím egyezés, cím fuzzy ≥ 0.8) | baseline − 5%p |
| Classify (dokumentum) | accuracy + tévesztési mátrix | baseline − 5%p |
| ClassifyEmail | accuracy (importance), külön High-recall | High-recall ≥ baseline |
| Summarize | `summaryMustContain` kulcstények lefedése (%) | baseline − 5%p |
| Q&A | groundedness (citációk validak), tény-egyezés | baseline − 5%p |
| IntentClassifier | accuracy az intents.jsonl-en | baseline − 2%p |
| Retrieval (F5-höz) | recall@10: az elvárt forrásdoksi benne van-e a top-10-ben | baseline |

- Kimenet: `eval/results/<datum>-<git-sha>-<modell>.json` + konzol-összegző.
  Az eredményfájl commitolható (trend követhető; később a telemetria-
  dashboard [cr260712-06] olvassa).

### 2.3 Baseline-futás és kapuk élesítése

1. Baseline a **jelenlegi** állapoton (F1 utáni paraméterekkel, default
   modellel) → az eredmény bekerül `eval/results/baseline-*.json`-ként.
2. A kapuértékek (fenti tábla) a baseline-ból számítva kerülnek a runner
   konfigjába (`eval/thresholds.json`).
3. Szabály a CLAUDE.md-be / coding-standards-be: prompt-, modell-,
   embedding- vagy inference-paraméter-változás PR-je **kötelezően**
   tartalmaz friss eval-eredményt.

**Acceptance:**
- [ ] `make eval` egy paranccsal fut lokális Ollama ellen, seed-elt.
- [ ] Legalább 20 dokumentum + 15 e-mail + 15 QA-pár + 20 intent-minta.
- [ ] Baseline-eredmény commitolva, küszöbök rögzítve.
- [ ] ADR: az eval-folyamat és a kapu-szabály rögzítése (ADR-0014 javaslat).

---

## F3 — Parse-robusztusság és truncation-stratégia

**Ág:** `feature/ai-q-f3-parse-truncation` · **Effort:** 1,5 nap

### 3.1 Közös JSON-válasz-parser

Új: `src/FamilyOs.Application/Ai/AiJsonResponse.cs` (vagy Infrastructure.Ai
alá, ha a függőségi irány úgy tisztább):

```csharp
public static class AiJsonResponse
{
    // JsonBlockExtractor.ExtractFirstObject + JsonDocument.Parse,
    // hibánál (null, "diagnosztikai ok") visszatérés — SOHA nem dob.
    public static (JsonDocument? Doc, string? Error) TryParse(string content);
}
```

Bevezetés mind a 9 extractorban (`OllamaDeadlineExtractor`,
`OllamaDocumentClassifier`, `OllamaDocumentSummarizer`,
`OllamaEmailClassifier`, `OllamaFinancialRecordExtractor`,
`OllamaMedicalRecordExtractor`, `OllamaQuestionAnswerer`,
`OllamaTaskExtractor`, `OllamaWarrantyExtractor`) a nyers
`JsonDocument.Parse` helyett.

### 3.2 Korrekciós retry (planner-minta általánosítása)

A `ToolCallPlanner` 1-retry mintája közös helper-be:
parse-hiba esetén **egyszer** újrapróbál a
„Az előző válaszod nem volt érvényes JSON…" korrekciós utasítással.
F1 `format:"json"` mellett ez ritkán aktiválódik — védőháló.

### 3.3 Láthatóság

- Parse-hiba → `LogAiParseFailure` (Warning, PromptId + hiba + a kimenet
  első 200 karaktere).
- Az `ai_processing_job` kapjon megkülönböztetést: új mező vagy a meglévő
  hiba-mezőben strukturált ok (`parse_failed`), hogy az admin-felületen
  elváljon a „0 találat" a „nem tudtam értelmezni"-től.

### 3.4 Truncation: fej+vég mintavétel

Közös helper: `AiTextWindow.HeadTail(text, headChars: 8000, tailChars: 4000,
separator: "\n[…]\n")` — bevezetés az öt `text[..12000]` helyén
(deadline/task/classifier/financial/medical extractor). A summarizer marad
map-reduce. A vágás ténye Debug-logba (dokumentumhossz + ablakméret).

**Tesztek / acceptance:**
- [ ] Unit: kerítéses (```json), prózával kevert, csonka JSON-kimenetek →
      helyes parse vagy strukturált hiba; retry egyszer fut.
- [ ] Unit: HeadTail — rövid szöveg érintetlen, hosszúnál fej+vég + jelölő.
- [ ] Eval (F2): ExtractDeadlines recall nem romlik, várhatóan javul a
      dokumentumvégi határidőkön → PR-ben rögzíteni.
- [ ] `PipelineGoldenTests` bővítése: kerítéses stub-válasz esetek.

---

## F4 — Prompt v2 kör (ékezetes magyar + few-shot)

**Ág:** `feature/ai-q-f4-prompts-v2` · **Effort:** 1,5 nap · **Kapu:** F2

**Lépések (promptonként külön commit, eval-eredménnyel):**
1. `sysprefix.v2.txt` — ékezetes magyar, változatlan szabálytartalom.
2. `classify.v2.txt`, `classify-email.v2.txt`, `extract-deadlines.v2.txt`,
   `extract-tasks.v2.txt` — ékezetesítés + **1–2 few-shot példa** az
   eval-készletből *kihagyott* (nem tesztelt!) mintákból, elvárt
   JSON-kimenettel együtt.
3. System-promptok nyelvi egységesítése: az extractorok angol egysorosai
   („You are a helpful assistant…") magyarra, a sysprefixszel konzisztensen.
4. `PromptCatalog` konstansok átállítása v2-re — **egyenként**, a hozzá
   tartozó eval-futással; ha egy prompt v2 rontana, v1-en marad és a
   tanulság a prompt-fájl fejlécében dokumentálódik.

**Acceptance:**
- [ ] Minden átállított prompt: eval-metrika ≥ baseline (dokumentálva a PR-ben).
- [ ] Few-shot példák nem szerepelnek az eval-tesztkészletben (adatszivárgás
      tilalma).
- [ ] `PromptCatalog.GetVersion` a v2-t adja vissza (meglévő
      `PromptTemplateTests` bővítése).

---

## F5 — Embedding-minőség

**Ág:** `feature/ai-q-f5-embedding` · **Effort:** 2 nap · **Kapu:** F1+F2

### 5.1 Task-prefixek (nomic követelmény)

`IEmbedder` bővítése cél-paraméterrel:

```csharp
public enum EmbeddingPurpose { Document, Query }
Task<float[]> EmbedAsync(string text, EmbeddingPurpose purpose, CancellationToken ct);
```

- `OllamaEmbedder`: `Document` → `"search_document: " + text`,
  `Query` → `"search_query: " + text` (modell-függő prefix-térkép, hogy
  a későbbi modellcsere ne igényeljen kódmódosítást).
- Hívóhelyek: `EmbedJobRunner`/`EmbedBackfillService` → `Document`;
  `QueryEmbeddingCache` → `Query`.

### 5.2 Vektortér-verziózás és újra-embeddelés

A prefixes embedding **más vektortér** — a régi és új vektorok nem
keverhetők. A meglévő séma ezt már kezeli (`embedding_model` oszlop +
modell-szűrés a `SemanticSearchService`-ben):

- `OllamaEmbedder.ModelName` → **logikai azonosító**:
  `"nomic-embed-text:v1.5#p1"` (a `#p1` a prefix-séma verziója).
- `EmbedBackfillService` bővítése: ne csak a hiányzó chunkokat pótolja,
  hanem a **régi `embedding_model`-lel tárolt** chunkokat is újra-embeddelje
  (batch, ütemezett, megszakítható — nagy korpusznál órákig futhat, ezért
  loggal + folyamatjelzéssel).
- Amíg a backfill fut, a keresés az új modell-ID-t szűri → a még át nem
  embeddelt tartalom átmenetileg nem találódik szemantikusan (az FTS-ág
  továbbra is él). Ez elfogadható; a backfill-státusz logból követhető.

### 5.3 API-modernizálás

- `/api/embeddings` → `/api/embed` (natív batch: `input: [..]`);
  `EmbedBatchAsync` egy hívásban, a szekvenciális fallback megtartásával.

### 5.4 QA-kontextus bővítése

- `OllamaQuestionAnswerer.cs:72`: 500 → 1500 char/chunk (konfigból:
  `Ai:Qa:SnippetChars`), F1-es `NumCtx`-szel összehangolva
  (becslés: 8 forrás × 1500 char ≈ 4,5k token + prompt — fér a 8192-be).
- `minSimilarity` (0.60) konfigurációba (`Ai:Search:MinSimilarity`) —
  modellcserénél újrakalibrálandó.

### 5.5 (Feltételes) embedding-modell csere

Az eval retrieval-metrikáján (recall@10) `bge-m3` (vagy más többnyelvű
modell) összevetése a prefixes nomickal. **Csak akkor** csere, ha ≥ 5%p
javulás; döntés ADR-ben (ADR-0015 javaslat), dimenzió-változás esetén
migráció (a `vector(768)` oszloptípus dimenzió-függő!).

**Acceptance:**
- [ ] Unit: purpose-alapú prefixelés; batch-hívás formátuma.
- [ ] Integrációs: backfill újra-embeddel régi modell-ID-jű chunkot.
- [ ] Eval: recall@10 és QA-metrika ≥ baseline; eredmény a PR-ben.
- [ ] `docs/search-strategy.md` 2.3 frissítése (prefix + modell-ID séma).

---

## F6 — Modell-stratégia (compose-default + per-feladat routing)

**Ág:** `feature/ai-q-f6-model-routing` · **Effort:** 1–2 nap · **Kapu:** F1+F2

1. **Per-feladat modell:** az F1-es `Tasks` szótár kap `Model` mezőt is —
   pl. classify/e-mail → kis-gyors modell; QA/summarize → nagyobb modell.
   A feloldás a PromptId alapján az `OllamaAiProvider`-ben (F1-gyel azonos
   mechanika, csak a `model` mező is felülbírálható).
2. **Compose-default felülvizsgálata:** a `qwen3-coder:30b` (kód-modell!)
   kiváltása általános instruct-modellel. Jelöltek erős gépre:
   általános qwen3-instruct variáns / gemma3 / llama3.1-8b+; RPi-re marad
   a 3b-osztály. **Döntés kizárólag eval-eredmény alapján** (teljes
   készlet, mindkét jelölttel, seed-elt futás).
3. **ADR-0006 frissítése**: default + ajánlott modellek gépprofilonként,
   az eval-eredmények linkjével; `docker-compose.strong-pc.yml` és
   `deploy-raspberry-pi.md` szinkronizálása.

**Acceptance:**
- [ ] Unit: PromptId → modell-feloldás; ismeretlen → DefaultModel.
- [ ] Eval-összevetés legalább 2 jelölt modellel, eredmény commitolva.
- [ ] ADR-0006 v2 + doksi-szinkron (product-vision/architecture hivatkozások).

---

## F7 — Ollama-konkurrencia és ops

**Ág:** `feature/ai-q-f7-ollama-concurrency` · **Effort:** 1 nap · **Kapu:** F1

1. **AI-hívás throttle:** közös `SemaphoreSlim` az `OllamaHttpClient`
   köré (konfig: `Ai:Ollama:MaxConcurrentRequests`, default 2; RPi: 1).
   Ez az API-oldali (search/tool-call) és a worker-oldali hívásokat is
   fedi — de processz-enként külön él, ezért:
2. **Hangfire AI-queue:** az AI-joboknak dedikált queue
   (`hangfire-queue: ai`, WorkerCount az AI-queue-ra 1–2; a nem-AI jobok
   — retention, digest — maradnak a default queue-n 4 workerrel). Így a
   30b-modelles környezetben nem indul 4 párhuzamos kinyerés.
3. **Hideg-latency mérés:** keep_alive (F1) hatásának ellenőrzése —
   worker-újraindulás utáni első job időtartama logból.
4. **Timeout-differenciálás:** a 120 s `TimeoutSeconds` feladatfüggővé
   (a map-reduce summarize nagy doksin túllépheti; a classify-nek sok).

**Acceptance:**
- [ ] Terheléses füstteszt: 10 dokumentum egyidejű feltöltése → nincs
      timeout-hiba, a jobok sorban lefutnak.
- [ ] A queue-bontás látszik az admin AI-jobs felületen (nem borítja fel
      a meglévő `queue-stats` végpontot).

---

## F8 — Nyelvkezelés és OCR kör

**Ág:** `feature/ai-q-f8-lang-ocr` · **Effort:** 2 nap · **Kapu:** F2

1. **Angol támogatás:** `SupportedLanguages`: `"hu"` → `"hu;en"` — előtte
   annak feltérképezése, hogy a nyelvi kapu pontosan hol ágazik el
   (`DetectLanguageJobRunner` → mely jobok skippelnek nem-támogatott
   nyelvnél), és az eval-készlet bővítése 5 angol dokumentummal.
2. **Tesseract:** `hun+eng` együttes traineddata; kép-előfeldolgozás
   (deskew/binarizálás) bekötése az OCR elé; mérés az `eval/scanned/`
   mintákon (OCR-kimenet karakterhiba-arány + downstream deadline-recall).
3. A promptok viselkedése vegyes nyelvű bemenetnél: a sysprefix 2. szabálya
   („magyarul fogalmazz…") már kezeli — eval-esettel lefedni.

**Acceptance:**
- [ ] Angol számla-minta végigmegy a pipeline-on (classify + deadline).
- [ ] Szkennelt minták OCR-minősége dokumentálva; javulás mérve a
      preprocessing bekapcsolásával.

---

## Ütemterv-összegzés

| Fázis | Effort | Kapu | Fő kockázat |
|---|---|---|---|
| F0 RBAC | 0,5 nap | – | ha rés van: azonnali javítás kell |
| F1 inference | 1 nap | – | RAM-igény nő (profilonként hangolható) |
| F2 eval | 2–3 nap | – | készlet-minőség = mérés-minőség |
| F3 parse+trunc | 1,5 nap | – | alacsony |
| F4 prompt v2 | 1,5 nap | F2 | prompt-regresszió (eval véd) |
| F5 embedding | 2 nap | F1,F2 | re-embed átmeneti keresés-kiesés |
| F6 modell | 1–2 nap | F1,F2 | modell-letöltés/VRAM; eval dönt |
| F7 ops | 1 nap | F1 | queue-átszervezés mellékhatásai |
| F8 nyelv/OCR | 2 nap | F2 | Tesseract-minőség plafonja |

**Összesen:** ~13–15 munkanap. Kritikus út: F1 → F2 → (F4/F5/F6 mérve).

## Definition of Done (a teljes programra)

- [ ] Minden fázis merge-elve zöld kapukkal (build, unit, érintett
      integrációs tesztek, code-review).
- [ ] Eval-baseline és záró eval-futás összevetése: egyetlen metrika sem
      romlott, a cél-metrikák (deadline-F1, e-mail High-recall, QA
      groundedness, recall@10) javulása dokumentálva.
- [ ] ADR-0006 frissítve, ADR-0014 (eval-folyamat), ADR-0015 (embedding,
      ha csere történt) elfogadva.
- [ ] `docs/ai-pipeline.md`, `docs/search-strategy.md` szinkronban a
      megvalósítással (a 02-es review-doksi tanulsága: ne keletkezzen új
      doksi-kód rés).
- [ ] A 05-ös doksi tételei lezárva vagy explicit elhalasztva, indoklással.

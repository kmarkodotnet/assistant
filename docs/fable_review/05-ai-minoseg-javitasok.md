# AI-megoldások minőségjavítási terve — Family OS

> Státusz: REVIEW v1.0 · Dátum: 2026-07-12 · Forrás: AI-réteg átvizsgálása (Fable review)
> Vizsgált állapot: `ai-proposal-learn` ág, commit `d2526a4`
> Hatókör: `FamilyOs.Infrastructure.Ai`, `Application/Ai`, `Application/Search`,
> worker job-runnerek, promptok, embedding/retrieval réteg

---

## 1. AI-feature leltár (mi van ma)

| Feature | Megvalósítás | Modell |
|---|---|---|
| Dokumentum-összefoglaló | `OllamaDocumentSummarizer` — 12k char felett map-reduce | LLM (default) |
| Dokumentum-osztályozás + facet | `OllamaDocumentClassifier`, `Extract{Warranty,Medical,Financial}` | LLM |
| Határidő/feladat-kinyerés | `OllamaDeadlineExtractor`, `OllamaTaskExtractor` → suggestion flow | LLM |
| E-mail fontosság | `OllamaEmailClassifier` (High/Medium/Low + kategória + határidő-jel) | LLM |
| Q&A (RAG) | `OllamaQuestionAnswerer` + `HallucinationGuard` (citáció-validálás) | LLM |
| Szemantikus keresés | `OllamaEmbedder` (nomic-embed-text, 768d) + pgvector + RRF-fúzió FTS-sel | embedding |
| NL parancsok (tool-calling) | `ToolCallPlanner` + HMAC-token + confirm flow (ADR-0011) | LLM |
| Intent-osztályozás | `IntentClassifier` — 31 soros magyar kulcsszó-heurisztika | nincs (szabály) |
| Nyelvfelismerés | `NTextCatLanguageDetector` | nincs (n-gram) |
| OCR / szövegkinyerés | PDF text-layer + Tesseract fallback | nincs |
| Napi digest | `DailyDigestContent` — szándékosan sablon-alapú, nem LLM | nincs |

**Erős alapok (ezekre lehet építeni):** provider-absztrakció privacy-kapuval
(`AiProviderFactory` — LocalOnly kényszerítve), verziózott prompt-katalógus
(`Prompts/*.vN.txt`), token-számok visszaadva minden hívásból,
`HallucinationGuard`, RRF (k=60), chunk-overlap (3200/400 char),
query-embedding cache (1 h TTL), job-szintű retry (MaxAttempts=5),
a tool-calling pipeline sémavalidálása és emberi megerősítése.

---

## 2. Javítási javaslatok — hatás/költség szerint rendezve

### 2.1 ⚡ Inference-paraméterek beállítása (legnagyobb hatás, legolcsóbb)

**Probléma:** Az `OllamaHttpClient.PostChatAsync` kérése csak
`model + messages + stream` mezőt küld — **semmilyen opciót nem állít be**:

- **`num_ctx` nincs megadva** → az Ollama default kontextus **2048 token**.
  Az extractorok 12 000 karaktert (~4–5k magyar token) küldenek be — az
  Ollama ezt **csendben levágja**, tehát a dokumentumok második fele soha
  nem jut el a modellhez. Ez valószínűleg a legnagyobb rejtett
  minőségromlás a rendszerben.
- **`temperature` nincs megadva** → default 0.8. Extrakcióhoz/osztályozáshoz
  0.0–0.2 való; a jelenlegi beállítás fölöslegesen kreatív (ingadozó JSON,
  ingadozó dátumok).
- **`format: "json"` nincs használva**, pedig az `OllamaAiProvider.Capabilities`
  deklarálja a `JsonMode`-ot, és minden prompt JSON-t kér. Az Ollama
  structured output (akár teljes JSON-séma) kikényszeríti az érvényes JSON-t
  — a parse-hibák nagy része eltűnne.
- **`seed` nincs** → az eval/golden futások nem reprodukálhatók.
- **`keep_alive` nincs** → ritka hívásoknál minden kérés modell-újratöltéssel
  indulhat (percekig tartó latency 30b modellnél).

**Teendők:**
- [ ] `OllamaOptions` bővítése: `NumCtx`, `Temperature`, `Seed`, `KeepAlive`,
      és per-feladat felülbírálás (lásd 2.6).
- [ ] `format: "json"` (vagy feladatonként JSON-séma) a chat-kérésben minden
      strukturált feladatnál; a Q&A-nál is (a válasz ott is JSON).
- [ ] `num_ctx` összehangolása az app-oldali truncation-limitekkel (ha 12k
      char megy be, legalább 8k token kontextus kell).

**Érintett:** `OllamaHttpClient.cs`, `OllamaOptions.cs`, docker-compose env.

### 2.2 🔍 Embedding-minőség (a retrieval a RAG plafonja)

**Probléma:**
1. A `nomic-embed-text:v1.5` **task-prefixek nélkül** fut
   (`OllamaEmbedder.cs:38`). Ez a modell dokumentáltan prefix-érzékeny:
   indexeléskor `search_document: `, kereséskor `search_query: ` prefix
   kell — enélkül mérhetően gyengébb a retrieval.
2. A nomic elsősorban **angol** modell; a korpusz magyar. Többnyelvű
   alternatíva (pl. `bge-m3`, `paraphrase-multilingual`) magyar szövegen
   tipikusan jobb. A séma felkészült a váltásra (`embedding_model` oszlop +
   modell-szűrés a lekérdezésben + `EmbedBackfillService`) — a csere olcsó.
3. `EmbedBatchAsync` szekvenciális, és a **deprecated** `/api/embeddings`
   végpontot használja — az `/api/embed` natívan batch-elhető.
4. A QA-kontextusba chunkonként csak **500 karakter** jut
   (`OllamaQuestionAnswerer.cs:72`) — a 3200 karakteres chunkból a válasz
   szempontjából releváns rész gyakran kimarad.

**Teendők:**
- [ ] Task-prefixek bevezetése (indexelés vs. lekérdezés útvonal
      megkülönböztetése az `IEmbedder`-ben) + teljes re-embed backfill.
- [ ] Magyar/többnyelvű embedding-modell kipróbálása az eval-készleten
      (lásd 2.5), győztes rögzítése ADR-ben.
- [ ] `/api/embed` batch-hívás; backfillnél párhuzamosítás.
- [ ] QA-forrásrészlet 500 → 1500+ char (a `num_ctx` emelésével együtt).

### 2.3 🛡️ Parse-robusztusság és retry egységesítése

**Probléma:** Két külön világ él a kódban:
- A `ToolCallPlanner` a **robusztus** mintát használja: `JsonBlockExtractor`
  (kifejti a JSON-t prózából/kerítésből) + séma-validálás + 1 korrekciós
  retry.
- A **9 task-extractor** (`OllamaDeadlineExtractor` stb.) nyers
  `JsonDocument.Parse`-t hív a teljes modell-kimeneten; ha a modell
  ```` ```json ````-be csomagol vagy magyarázatot ír, a parse **csendben
  elhasal**, és üres eredmény megy tovább — a job "sikeres", csak épp nem
  talált semmit. Se warning-log, se metrika, se retry.

**Teendők:**
- [ ] Közös `AiJsonResponseParser` (JsonBlockExtractor + korrekciós retry)
      kivonása és bevezetése mind a 9 extractorban.
- [ ] Parse-hiba → legalább Warning-log + `ai_processing_job`-on
      megkülönböztethető kimenet ("parse-failed", nem "0 találat").
- [ ] A 2.1-es `format:"json"` ezt nagyrészt megelőzi — a parser a védőháló.

### 2.4 ✂️ Truncation-stratégia

**Probléma:** Öt helyen egységesen `text[..12000]` — **fejtől vágás**.
A határidők, összegek, garanciafeltételek tipikusan a dokumentum **végén**
vannak (számlák fizetési blokkja, szerződések záró rendelkezései) — pont az
esik le. Az e-mail-osztályozó `MaxBodyChars` vágása ugyanígy.

**Teendők:**
- [ ] Kinyerő feladatoknál eleje+vége mintavétel (pl. első 8k + utolsó 4k),
      vagy chunk-alapú kinyerés unióval (a summarizer map-reduce mintájára —
      az már jól csinálja).
- [ ] A vágás ténye kerüljön logba/metrikába (hány dokumentumot érint —
      most láthatatlan).

### 2.5 📏 Valódi minőségmérés: eval-harness (enélkül a többi vakrepülés)

**Probléma:** A `PipelineGoldenTests` **stub-providerrel** fut — a parsing
plumbingot védi, nem a modell-minőséget. Az ai-pipeline.md 9.1 és az
ADR-0006 előírja a golden-sample regressziót „a mindenkori default
modellel", de ilyen nem létezik. Következmény: modell-, prompt- vagy
paraméter-csere hatása jelenleg mérhetetlen.

**Teendők:**
- [ ] Címkézett magyar minta-készlet (20–50 dokumentum/e-mail: számla,
      biztosítás, iskolai levél, orvosi lelet…), elvárt kimenetekkel
      (határidők, osztályok, fontosság).
- [ ] Eval-runner (külön `make eval` target, valódi Ollama ellen, `seed`-del):
      precision/recall határidő-kinyerésre, accuracy osztályozásra,
      groundedness a Q&A-ra (citációk helyessége).
- [ ] Küszöbök rögzítése; modell/prompt-váltásnál kötelező futtatás
      (ADR-0006 követelménye végre betartható lenne).
- [ ] Az eredmények a tervezett AI-telemetria dashboardba
      (04-es doksi, cr260712-06) — prompt-verziónként trendelve.

### 2.6 🧠 Modell-stratégia rendbetétele

**Probléma:**
1. **ADR-0006 vs. valóság:** az ADR default-ja `llama3.2:3b`
   (`OllamaOptions.cs`), a docker-compose viszont **`qwen3-coder:30b`**-t
   állít be — ami egy **kódgenerálásra hangolt** modell; magyar
   természetes-nyelvű osztályozásra/összefoglalásra nem ideális választás
   azonos méretű instruct-modellhez képest.
2. **Egyetlen modell mindenre:** a `DefaultModel` minden feladatra ugyanaz,
   pedig az igények eltérnek (classify: kicsi+gyors elég; Q&A/summarize:
   nagyobb kell).

**Teendők:**
- [ ] Compose-modell felülvizsgálata: erős gépen általános instruct-modell
      (pl. qwen3-instruct / gemma3 / llama3.1-8b+) — az eval-készleten (2.5)
      mérve, ne érzésre.
- [ ] Per-feladat modell-konfiguráció (`Ai:Ollama:Models:{taskId}` fallback
      a DefaultModel-re) — a `PromptId` már minden hívásban ott van, a
      routing olcsón beköthető.
- [ ] ADR-0006 frissítése a tényleges ajánlással.

### 2.7 🇭🇺 Prompt-minőség

**Probléma:**
1. Több prompt (sysprefix, summarize, classify-email) **ékezet nélküli**
   magyarsággal íródott („Foglald ossze… ervenyes JSON"). A modellnek ez
   zajos bemenet: rontja a magyar kimenet minőségét és a followed-instructions
   arányt. (A qa-magyar.v1 már helyesen ékezetes — a minta megvan.)
2. Minden prompt zero-shot. Kis modellnél (3b) 1–2 **few-shot példa** a
   classify/extract feladatokon tipikusan a legnagyobb egyedi
   prompt-javulás.
3. A rendszer-promptok egy része angol egysoros („You are a helpful
   assistant that extracts deadlines…"), miközben a sysprefix magyar —
   vegyes nyelvű instrukció-halmaz.

**Teendők:**
- [ ] Összes prompt ékezetes magyarra (v2-verzióként — a katalógus
      verziókezelése ezt már támogatja).
- [ ] Few-shot példák a classify.v2, classify-email.v2, extract-deadlines.v2
      promptokba (az eval-készletből vett, kézzel ellenőrzött példákkal).
- [ ] Egységes nyelvpolitika a system-promptokban.
- [ ] Prompt-változat → eval-futás → merge munkafolyamat (2.5-re épül).

### 2.8 🔀 Retrieval / Q&A finomhangolás

**Teendők:**
- [ ] **RBAC-konzisztencia ellenőrzése:** a `SemanticSearchService` SQL-szűrője
      (`is_private = false OR owner`) **lazább, mint** a
      `FamilyOsAuthorizationService` Child-szabálya (Child csak a hozzá
      rendelt, nem-privát elemeket láthatná). Ellenőrizni kell, van-e
      utószűrés a handler-rétegben; ha nincs, a Child szemantikus úton többet
      lát, mint a lista-végpontokon. (Biztonsági jellegű — a 01-es doksihoz
      is kapcsolódik.)
- [ ] `HallucinationGuard`: most **egyetlen** rossz citáció az egész választ
      eldobja. Finomítás: a rossz citációk szűrése, és csak akkor fallback,
      ha nem marad érvényes forrás.
- [ ] `IntentClassifier`: a kulcsszó-heurisztika jó kezdet, de nincs mérve.
      Címkézett kérdés-készlet (az eval-harness része), tévesztési mátrix;
      alacsony konfidenciánál (< 0.5) LLM-alapú intent-fallback megfontolása
      (a search-strategy.md 5.2 konfidencia-kapuja ma csak heurisztika).
- [ ] `minSimilarity = 0.60` fix küszöb a szemantikus ágon — az
      embedding-modell cseréjekor (2.2) újra kell kalibrálni; kerüljön
      konfigurációba.

### 2.9 🌐 Nyelvkezelés és OCR

**Teendők:**
- [ ] `SupportedLanguages = "hu"`: tisztázni (és evalben mérni), mi történik
      angol/vegyes nyelvű dokumentumokkal — családi korpuszban gyakori az
      angol számla/jegy; az `en` felvétele a támogatott listára valószínűleg
      olcsó nyereség.
- [ ] Tesseract-ág: `hun+eng` traineddata együttes használata,
      kép-előfeldolgozás (deskew/denoise/kontraszt) mérése az eval-készlet
      szkennelt mintáin; rossz OCR-minőség a teljes downstream pipeline-t
      (embedding, kinyerés, QA) mérgezi.

### 2.10 ⚙️ Üzemeltetési minőség

**Teendők:**
- [ ] **Ollama-konkurrencia:** a Hangfire `WorkerCount = 4` akár 4 párhuzamos
      LLM-hívást enged ugyanarra az Ollama-instance-ra (plusz az API-oldali
      search/tool-call hívások). Egy 30b modellnél ez timeout-lavinát okozhat.
      Javaslat: AI-hívások köré közös `SemaphoreSlim` (konfigurálható
      párhuzamosság, default 1–2), vagy dedikált Hangfire-queue az AI-joboknak
      1 workerrel.
- [ ] `keep_alive` beállítás (2.1) + a modell-betöltési hideg-latency mérése.
- [ ] A token/latency adatok aggregálása (cr260712-06 telemetria CR) — a
      2.5-ös eval és a napi üzem közös műszerfala.

---

## 3. Priorizált ütemterv

| # | Tétel | Hatás | Költség | Függés |
|---|---|---|---|---|
| 1 | 2.1 inference-paraméterek (`num_ctx`, `temperature`, `format:json`) | ★★★★★ | S | – |
| 2 | 2.5 eval-harness + címkézett készlet | ★★★★★ | M | – |
| 3 | 2.3 parse-robusztusság egységesítése | ★★★★ | S | – |
| 4 | 2.2 embedding-prefixek + QA-kontextus bővítés | ★★★★ | S–M | 2.5 (méréshez) |
| 5 | 2.7 promptok ékezetes v2 + few-shot | ★★★★ | S | 2.5 |
| 6 | 2.6 modell-felülvizsgálat + per-feladat routing | ★★★★ | M | 2.5 |
| 7 | 2.4 truncation-stratégia | ★★★ | S | – |
| 8 | 2.8 RBAC-ellenőrzés + guard/intent finomítás | ★★★ | S–M | – (RBAC-rész sürgős) |
| 9 | 2.2 embedding-modell csere (ha az eval igazolja) | ★★★ | M | 2, 4 |
| 10 | 2.10 Ollama-konkurrencia + keep_alive | ★★ | S | – |
| 11 | 2.9 nyelv/OCR kör | ★★ | M | 2.5 |

**A kulcsgondolat:** előbb *mérhetővé* tenni a minőséget (eval-harness),
és a két „csendes gyilkost" (2048-as `num_ctx`, néma parse-hibák)
megszüntetni — minden további optimalizálás (modell, prompt, embedding)
csak ezután ítélhető meg tárgyilagosan.

# AI-minőség háttér-elemzés — REFERENCIA (nem feladatlista!)

> Státusz: REFERENCIA v1.0 · Dátum: 2026-07-12
> Forrás: `docs/fable_review/05-ai-minoseg-javitasok.md` · Vizsgált: `ai-proposal-learn`, commit `d2526a4`
>
> ⚠️ **NE EBBŐL IMPLEMENTÁLJ.** A végrehajtási igazságforrás a
> [06-ai-minoseg-vegrehajtasi-terv.md](06-ai-minoseg-vegrehajtasi-terv.md)
> (F0–F8 fázisok). Ez a doksi a „miért"-eket és a tényleltárt őrzi —
> subagentnek csak akkor add oda, ha a 06-os kártya explicit hivatkozik ide.

---

## 1. AI-feature leltár (mi van ma)

| Feature | Megvalósítás | Modell |
|---|---|---|
| Dokumentum-összefoglaló | `OllamaDocumentSummarizer` — 12k char felett map-reduce | LLM |
| Dok-osztályozás + facet | `OllamaDocumentClassifier`, `Extract{Warranty,Medical,Financial}` | LLM |
| Határidő/feladat-kinyerés | `OllamaDeadlineExtractor`, `OllamaTaskExtractor` → suggestion flow | LLM |
| E-mail fontosság | `OllamaEmailClassifier` (High/Medium/Low + kategória) | LLM |
| Q&A (RAG) | `OllamaQuestionAnswerer` + `HallucinationGuard` | LLM |
| Szemantikus keresés | `OllamaEmbedder` (nomic-embed-text, 768d) + pgvector + RRF | embedding |
| NL parancsok | `ToolCallPlanner` + HMAC-token + confirm (ADR-0011) | LLM |
| Intent-osztályozás | `IntentClassifier` — magyar kulcsszó-heurisztika | szabály |
| Nyelvfelismerés | `NTextCatLanguageDetector` | n-gram |
| OCR | PDF text-layer + Tesseract fallback | — |
| Napi digest | `DailyDigestContent` — szándékosan sablon | — |

**Erős alapok (VÉDETT — ne bontsd meg):** `AiProviderFactory` privacy-kapu
(LocalOnly kényszerítve) · verziózott prompt-katalógus (`Prompts/*.vN.txt`) ·
token-számok minden hívásból · `HallucinationGuard` · RRF (k=60) ·
chunk 3200/400 overlap · query-embedding cache (1 h) · job-retry (MaxAttempts=5) ·
tool-calling sémavalidálás + emberi megerősítés.

---

## 2. Hibaleltár (tény + hely) → melyik 06-os fázis oldja

| # | Tény | Hely | Következmény | 06-fázis |
|---|---|---|---|---|
| 1 | `PostChatAsync` csak model+messages+stream mezőt küld; nincs `num_ctx` → Ollama-default **2048 token**, miközben 12k char (~4-5k token) megy be | `OllamaHttpClient.cs` | a dokumentumok második fele **csendben levágva** — a legnagyobb rejtett minőségvesztés | **F1** |
| 2 | nincs `temperature` (default 0.8) extrakciós feladatokon | `OllamaHttpClient.cs` | ingadozó JSON/dátumok | **F1** |
| 3 | nincs `format:"json"`, pedig a Capabilities deklarálja s minden prompt JSON-t kér | `OllamaHttpClient.cs` | elkerülhető parse-hibák | **F1** |
| 4 | nincs `seed`, nincs `keep_alive` | `OllamaHttpClient.cs` | eval nem reprodukálható; hideg-latency 30b modellnél | **F1** |
| 5 | 9 extractor nyers `JsonDocument.Parse`-t hív; ```json-kerítésnél csendben üres eredmény, a job „sikeres" | `Ollama*Extractor` osztályok | néma adatvesztés, se log, se metrika | **F3** |
| 6 | 5 helyen `text[..12000]` fejtől-vágás; a határidők/összegek a dok VÉGÉN vannak | extractorok + e-mail `MaxBodyChars` | pont a lényeg esik le | **F4** |
| 7 | nomic-embed task-prefixek hiányoznak (`search_document:` / `search_query:`) | `OllamaEmbedder.cs:38` | dokumentáltan gyengébb retrieval | **F5** |
| 8 | `EmbedBatchAsync` szekvenciális, deprecated `/api/embeddings` végponton | `OllamaEmbedder.cs` | lassú backfill | **F5** |
| 9 | QA-kontextusba chunkonként csak 500 char jut | `OllamaQuestionAnswerer.cs:72` | a releváns rész kimarad a válaszból | **F5** |
| 10 | `PipelineGoldenTests` stub-providerrel fut; valódi modell-eval nincs (ADR-0006 megsértve) | tests | modell/prompt-csere hatása mérhetetlen | **F2** |
| 11 | compose `qwen3-coder:30b`-t állít (kód-modell!), ADR-0006 `llama3.2:3b`-t mond; egy modell mindenre | docker-compose vs `OllamaOptions.cs` | magyar NL-feladatra rossz illeszkedés | **F6** |
| 12 | promptok részben ékezet nélküliek („Foglald ossze… ervenyes JSON"), zero-shot, vegyes EN/HU system-promptok | `Prompts/` | rontja a magyar kimenetet (qa-magyar.v1 a jó minta) | **F7** |
| 13 | `SemanticSearchService` SQL-szűrő (`is_private=false OR owner`) **lazább**, mint a Child RBAC-szabály | `SemanticSearchService` | Child szemantikus úton többet láthat — biztonsági jellegű | **F0** (sürgős) |
| 14 | `HallucinationGuard`: 1 rossz citáció → teljes válasz eldobva | `HallucinationGuard` | túl agresszív fallback | **F5** |
| 15 | `minSimilarity = 0.60` fix, nem konfig | szemantikus ág | modellcserénél újrakalibrálhatatlan | **F5** |
| 16 | Hangfire `WorkerCount=4` → akár 4 párhuzamos LLM-hívás egy Ollamára | workers | timeout-lavina 30b modellnél | **F8** |
| 17 | `SupportedLanguages="hu"`; angol/vegyes dok viselkedése tisztázatlan; Tesseract `hun+eng` és előfeldolgozás mérve nincs | config + OCR-ág | angol számla/jegy gyakori a családi korpuszban | backlog (eval után) |
| 18 | `IntentClassifier` heurisztika mérve nincs; konfidencia-kapu csak heurisztika | `IntentClassifier` | tévesztés láthatatlan | F2 (mérés) + 07-doksi B-hullám |

---

## 3. A kulcsgondolat (a 06-os sorrend indoklása)

Előbb **mérhetővé** tenni a minőséget (eval-harness, F2) és a két „csendes
gyilkost" (2048-as `num_ctx` → F1, néma parse-hibák → F3) megszüntetni;
minden további optimalizálás (embedding-prefix F5, modell F6, prompt F7)
csak eval-lel ítélhető meg. A RBAC-rés (13-as tétel) biztonsági jellegű,
ezért az F0-ban azonnal ellenőrizendő.

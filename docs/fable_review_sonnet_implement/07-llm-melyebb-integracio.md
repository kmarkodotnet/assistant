# LLM mélyebb integrációja — Sonnet-implementációs hullám-kártyák

> Státusz: IMPLEMENTÁCIÓS SPEC v1.0 · Dátum: 2026-07-12
> Forrás: `docs/fable_review/07-llm-melyebb-integracio.md`
> **KAPU: a 06-os terv F1 és F2 fázisa merge-elve legyen, mielőtt bármelyik
> hullám indul.** A 4. hullám az F6-ot (modellválasztás) is kéri.

## 🔒 MUST-invariánsok (MINDEN kártya subagent-promptjába szó szerint másolandó)

1. **LocalOnly:** minden hívás a helyi Ollamára megy — az `AiProviderFactory`
   privacy-kapuja érintetlen; felhő-szolgáltatás bevonása TILOS.
2. **Írás = proposal + confirm:** az ADR-0011 kapuja NEM gyengíthető; a
   read-only és write tool-terek a registry-ben szétválasztva.
3. **RBAC a retrievalben:** minden új keresési út a meglévő szűrt
   query-rétegeken át megy (F0 után).
4. **Mérhetőség:** minden hullám az eval-készlet bővítésével együtt érkezik.
5. **Auditálhatóság:** minden LLM-kör `AiCall` audit-bejegyzés; forrás-ID
   nélküli proaktív észrevétel eldobódik.

## Hullám-sorrend (zárt döntés)

| Hullám | Kártyák | Effort | Feltétel |
|---|---|---|---|
| 1 | A1 + A2 | ~3,5 nap | F1–F2 kész |
| 2 | B1 + B3 | ~4–5 nap | 1. hullám |
| 3 | A3 + B2 | ~2,5 nap | 1. hullám (A1 a B2-höz nem kell) |
| 4 | C1 + C2 | ~6–8 nap | F6 modellválasztás |
| 5 | C3 + C4 | ~6–7 nap | 4. hullám tapasztalatai |

Kiinduló állapot (kontextus): a mai flow egyfordulós és reaktív —
`SearchRequest`-ben nincs beszélgetés-előzmény, nincs streaming, az intent
kulcsszó-heurisztika, az LLM sosem kezdeményez.

---

## 1. hullám

### Kártya A1 — Többfordulós kontextus (query condensation)

**Ág:** `feature/llm-a1-conversation` · **Effort:** 1–1,5 nap · **Modell:** sonnet.

**Olvasd el először:** `SearchRequest` DTO + a search-handler lánc
(hol ágazik el a SearchMode) · `frontend/.../search.facade.ts` (a `history`
signal — már tárolja az előzményt, csak fel kell küldeni) ·
`OllamaQuestionAnswerer` (a QA-prompt összeállítása) · a prompt-katalógus
verziózási mintája.

**Döntések (zárva):**
- `SearchRequest` új mezője: `conversation` — az utolsó **6** fordulat
  `{question, answerSummary, referencedIds[]}` alakban; opcionális
  (üres/null = mai viselkedés, teljes visszafele-kompatibilitás).
- Új prompt: `condense-question.v1.txt` — a follow-up kérdést önálló
  kérdéssé írja át („És a Fordé?" → „Mikor jár le a Ford biztosítása?").
  A kondenzált kérdés megy a MEGLÉVŐ retrieval-be — a kereső-pipeline
  (FTS+szemantikus+RRF) NEM változik.
- Kondenzálás CSAK akkor fut, ha van conversation ÉS a kérdés rövid/utaló
  (egyszerű heurisztika: < 40 char vagy névmással/kötőszóval indul —
  a pontos szabályt implementáláskor rögzítsd kommentben); különben
  felesleges latency.
- A qa-magyar prompt kontextus-szekciót kap (az előzmény rövid összefoglalója)
  — új verzióként (v2 vagy a F4 utáni következő), eval-futással.

**Lépések:** kontrakt-delta doksi (`docs/api-design.md` search-szakasz) →
BE: DTO + condensation-hívás + QA-prompt kontextus → FE: a facade felküldi
az előzményt → eval-készlet bővítése follow-up kérdés-párokkal → tesztek.
**Tesztek:** BE unit (kondenzáció hívódik/nem hívódik a heurisztika szerint;
üres conversation = régi út); FE vitest (a request tartalmazza az előzményt);
eval: follow-up készleten a helyes válasz-arány rögzítve a PR-ben.
**Elfogadás:** „És a Fordé?"-típusú kérdés helyes találatot ad · üres
előzménnyel minden meglévő teszt zöld.
**Tilos:** a retrieval/RRF módosítása; server-oldali beszélgetés-perzisztencia
(a kontextus a kérésben utazik — session-tárolás NEM scope).

### Kártya A2 — Streamelt válasz

**Ág:** `feature/llm-a2-streaming` · **Effort:** 2 nap · **Modell:** sonnet.

**Olvasd el először:** `OllamaHttpClient` (a `stream:false` hívás) ·
`NotificationsHub` + SignalR-regisztráció (van-e minta streamre) ·
a FE search-eredmény komponens (hogyan jeleníti meg a QA-választ) ·
`HallucinationGuard` (mikor fut).

**Döntések (zárva):**
- Csatorna: **SignalR** (a meglévő hub-infrastruktúra), NEM új SSE-endpoint —
  külön metódus/csoport a QA-streamnek (`QaStream`, connection-scoped).
- A strukturált mezők (citációk, konfidencia) a stream VÉGÉN, záró
  eseményben érkeznek.
- `HallucinationGuard` a stream végén fut: ha elbukik, záró esemény
  `fallback:true`-val + a fallback-szöveggel — a FE lecseréli a bufferelt
  szöveget. (F1 `format:json` mellett ritka.)
- A nem-streamelő út MEGMARAD (a `POST /search` válasza változatlan) —
  a stream opt-in a FE-ről; így a régi kliensek/tesztek nem törnek.

**Lépések:** `OllamaHttpClient` stream-mód (chunk-olvasás) → hub-metódus +
auth (a meglévő hub-auth mintával) → FE: chat-bubble folyamatos építés +
záró esemény kezelés → tesztek (BE unit a chunk-összefűzésre; FE vitest a
bubble-építésre; e2e mockolt spec a fallback-cserére).
**Elfogadás:** 30b modellnél a first-token láthatóan gyorsabb, mint a teljes
válasz (kézi mérés a PR-ben) · fallback-csere működik · nem-streamelt út zöld.
**Tilos:** a QA-válasz JSON-sémájának megváltoztatása; a guard gyengítése.

---

## 2. hullám

### Kártya B1 — LLM-alapú lekérdezés-tervező (kétlépcsős router)

**Ág:** `feature/llm-b1-query-planner` · **Effort:** 2–3 nap · **Modell:** sonnet.

**Olvasd el először:** `IntentClassifier` (a heurisztika + konfidencia) ·
`ToolCallPlanner` (a MÁSOLANDÓ minta: séma-validált JSON + 1 korrekciós
retry + NowUtc+TimeZone dátum-normalizálás) · `FilterSearchHandler`
(mit fogad a `filters`) · `eval/datasets/intents.jsonl` (F2-ből).

**Döntések (zárva):**
- Kétlépcsős: a heurisztika MARAD a gyorsút; LLM-hívás CSAK konfidencia
  < 0.5 alatt.
- A planner kimeneti sémája PONTOSAN:
```json
{
  "intent": "filter|lookup|find|summarize|aggregate|qa",
  "filters": {
    "topics": ["jármű"],
    "familyMember": "Peti",
    "dateRange": {"from": "2026-01-01", "to": "2026-06-30"},
    "docKinds": ["szamla"]
  },
  "rewrittenQuery": "gépjármű biztosítás díj",
  "confidence": 0.9
}
```
- Az LLM **nem keres, hanem fordít**: a `filters` a meglévő
  `FilterSearchHandler`-be, a `rewrittenQuery` a FTS+szemantikus ágba megy.
- Relatív dátumok („múlt hónapban") a planner-promptban normalizálódnak —
  a tool-calling NowUtc+TimeZone mintát használd újra.
- Kikapcsolható: `Ai:QueryPlanner:Enabled` (default true), hiba/timeout
  esetén némán visszaesik a heurisztika eredményére (a keresés SOSEM
  bukhat a planner miatt).

**Lépések:** prompt (`query-plan.v1.txt`) → planner-service a ToolCallPlanner
mintájával → bekötés az intent-elágazásba → intents.jsonl bővítése ragozott/
körülírt esetekkel → eval A/B (heurisztika vs. kétlépcsős) a PR-ben → tesztek
(unit: séma-validálás, fallback hiba esetén, magas konfidenciánál NINCS
LLM-hívás).
**Elfogadás:** intent-accuracy ≥ baseline az eval-en · alacsony konfidenciás
ragozott kérdések javulnak (dokumentálva) · planner-hiba nem töri a keresést.
**Tilos:** a heurisztika törlése; a search-handler kontraktjainak módosítása.

### Kártya B3 — Tisztázó visszakérdezés (clarify)

**Ág:** `feature/llm-b3-clarify` · **Effort:** 2 nap · **Modell:** sonnet ·
**Függ:** A1 (conversation kell a második körhöz).

**Olvasd el először:** `ToolCallPlanner` (a `ResolveFailed` ág) · `RefMatcher`
(a több-találatos ág — ebből következik a clarify) · a FE proposal-kártya
komponens (ide kerülnek az opció-gombok) · A1 kész kódja.

**Döntések (zárva):**
- A planner (és B1-router) harmadik kimenete:
```json
{"action": "clarify",
 "question": "Két »orvos« feladatot találtam: a) Peti fogorvos (júl. 20.), b) Anya vérvétel (júl. 15.). Melyikre gondolsz?",
 "options": [{"label": "a", "ref": "..."}, {"label": "b", "ref": "..."}]}
```
- A FE gomb-opciókként jeleníti meg; a választás az A1-es `conversation`
  kontextussal megy vissza; a második kör már egyértelmű proposalt ad.
- A clarify NEM műveletvégzés — nincs token, nincs confirm-kapu érintés;
  az opció `ref` értéke a második kör bemenete, nem végrehajtható hivatkozás.
- Max 1 clarify-kör kérdésenként; ha a második kör is bizonytalan →
  a mai `ResolveFailed` üzenet.

**Lépések:** planner-séma bővítés + RefMatcher több-találatos ág bekötése →
FE opció-gombok → tesztek (unit: több találat → clarify, egy találat →
proposal; FE vitest: gombválasztás visszaküldése; e2e mockolt clarify-flow).
**Elfogadás:** „orvos feladat kész" két találatnál visszakérdez, választás
után helyes proposal · egy-találatos út változatlan.
**Tilos:** a confirm/token flow bármely módosítása.

---

## 3. hullám

### Kártya A3 — Dokumentum-chat (detail-oldali kérdezés)

**Ág:** `feature/llm-a3-doc-chat` · **Effort:** 1,5 nap · **Modell:** sonnet.

**Olvasd el először:** `OllamaQuestionAnswerer` (újrahasznosítandó) ·
a QA-retrieval útja (hova szúrható be `documentId`-szűrés) · a dokumentum-
részletoldal FE-komponense · `CanReadDocument` (RBAC).

**Döntések (zárva):**
- Új végpont: `POST /api/v1/documents/{id}/ask` — body: `{question}` +
  opcionális A1-es `conversation`; RBAC: `CanReadDocument` az elején.
- Retrieval a kiválasztott dokumentum chunkjaira szűkítve (nincs globális
  keresés); a meglévő QA-prompt fut.
- FE: „Kérdezz erről a dokumentumról" panel a detail-oldalon; ha A2 kész,
  streamelve — ha a sorrend miatt előbb készül, nem-streamelt első verzió
  is elfogadható.

**Lépések:** BE endpoint + documentId-szűrős retrieval + integrációs teszt
(RBAC: idegen privát dok → 403/404) → FE panel + vitest → api-design.md delta.
**Elfogadás:** „Mennyi az önrész?" a biztosítási mintadokumentumon helyes,
citált választ ad · RBAC-teszt zöld.
**Tilos:** több-dokumentumos chat; a globális QA-út módosítása.

### Kártya B2 — Multi-query lekérdezés-bővítés

**Ág:** `feature/llm-b2-multiquery` · **Effort:** 1 nap · **Modell:** sonnet.

**Olvasd el először:** a szemantikus keresés útja (`QueryEmbeddingCache` →
pgvector) · az RRF-fúzió bemenete (hogyan kap listákat) · F5 kész kódja
(prefixek).

**Döntések (zárva):**
- Kérdésenként 2 alternatív átfogalmazás egy olcsó LLM-hívással
  (`expand-query.v1.txt`); mindhárom (eredeti + 2) embeddelve
  (`QueryEmbeddingCache` cache-el), a találati listákat a MEGLÉVŐ RRF
  fésüli — az infrastruktúra kész, csak több listát kap.
- CSAK Qa/Find intentnél fut; konfig: `Ai:Search:MultiQuery:Enabled`
  (default **false** — latency-érzékeny, mérés után kapcsolható be).
- LLM-hiba → egyetlen query-vel megy tovább (némán, Debug-log).

**Lépések:** prompt → bővítő-hívás + párhuzamos embed → RRF-bekötés →
eval recall@10 összevetés (be/ki) a PR-ben → unit tesztek (flag ki = régi út;
hiba = fallback).
**Elfogadás:** recall@10 javulás dokumentálva; flag-off viselkedés bitre
azonos a maival.
**Tilos:** RRF-paraméterek (k=60) módosítása; default bekapcsolás mérés nélkül.

---

## 4. hullám — KAPU: F6 (modellválasztás) kész

### Kártya C1 — Read-only eszközkészlet („kutató asszisztens")

**Ág:** `feature/llm-c1-readonly-tools` · **Effort:** 4–5 nap · **Modell:**
sonnet (a design-review-ra opus ajánlott merge előtt — biztonság-érzékeny).

**Olvasd el először:** ADR-0011 (a write-kapu — MIT NEM érintünk) ·
tool-registry (hogyan lesz két tool-tér) · az Ollama natív `tools`
paraméterének dokumentációja/kliens-támogatása az `OllamaHttpClient`-ben ·
a mögöttes query-utak: hibrid keresés, `document_summary`, Deadlines/Tasks
query, `AggregateSearchHandler`.

**Döntések (zárva):**
- Read-only tool-készlet (MIND meglévő, RBAC-szűrt query-utakra épül):

| Tool | Mögötte |
|---|---|
| `search_documents(query, filters)` | meglévő hibrid keresés |
| `get_document_summary(id)` | `document_summary` tábla |
| `list_deadlines(range, member)` | Deadlines query |
| `list_tasks(status, member)` | Tasks query |
| `aggregate_financial(kind, range)` | `AggregateSearchHandler` |

- Ollama natív tool-calling (`tools` paraméter a `/api/chat`-ben), NEM kézi
  prompt-katalógus.
- **Max 5 tool-kör/kérdés** (költségplafon, konfigból); minden köztes hívás
  `AiCall` audit-bejegyzés.
- A registry-ben KÉT tool-tér: read-only (confirm nélkül) és write
  (ADR-0011 proposal+confirm) — a read-only tool DEFINÍCIÓ SZERINT nem
  hívhat commandot, csak query-t; ezt a registry típusszinten kényszerítse ki
  (pl. külön interfész: `IReadOnlyTool` query-visszatérésekkel).
- Minden tool-végrehajtás a hívó user RBAC-kontextusával fut (role,
  familyMemberId átadva) — Child-dal a tool sem lát többet.

**Lépések:** IReadOnlyTool absztrakció + 5 tool → agent-loop az
OllamaHttpClient natív tools-móddal + körlimit → bekötés a Qa/Command útba →
eval agent-forgatókönyvek (F2-készlet bővítés: „Mennyit költöttünk idén
autóra?" típusú többlépéses kérdések) → tesztek (unit: körlimit, RBAC-átadás,
write-tool a read-only térből nem hívható; integrációs: Child-szűrés).
**Elfogadás:** többlépéses kérdés helyes, forrás-citált választ ad · körlimit
érvényesül · audit-bejegyzések keletkeznek · 3b-modellen mért megbízhatóság
dokumentálva (ha gyenge: csak erős-gép profilon engedélyezett, konfig-flaggel).
**Tilos:** write-tool confirm nélkül; a körlimit feloldása; új query-út írása
(csak meglévőre építs).

### Kártya C2 — Láncolt írási javaslatok (batch-proposal)

**Ág:** `feature/llm-c2-batch-proposal` · **Effort:** 2–3 nap · **Modell:** sonnet.
**Kapcsolódik:** cr260712-02 (tool-katalógus) — előbb az legyen kész.

**Olvasd el először:** ADR-0011 (token/replay-guard tételes érvényessége) ·
a planner proposal-kimenete · FE proposal-kártya.

**Döntések (zárva):**
- A planner kimenete javaslat-LISTA lehet (2–3 összefüggő tool-hívás);
  a FE EGY kártyán mutatja, a jóváhagyás EGY gombbal történik, DE a
  végrehajtás tételenként tokenizált — a replay-guard tételenként érvényes.
  **A confirm-kapu nem gyengül, csak a UX rövidül.**
- Részleges hiba: ha a lista 2. tétele elbukik, az 1. eredménye megmarad,
  a hiba tételenként jelenik meg a kártyán (nincs rollback — a tételek
  függetlenek; összefüggő tételeknél a planner felelőssége a sorrend).
- Max 3 tétel/batch.

**Lépések:** planner-séma bővítés (lista) → token-generálás tételenként →
FE kártya (tételenkénti státusz) → tesztek (unit: tételenkénti token/replay;
e2e mockolt batch-flow; részleges hiba megjelenítés).
**Elfogadás:** „fogorvos + emlékeztető" egy kártyán, egy jóváhagyással,
két auditált végrehajtással · replay-védelem tételenként bizonyított (teszt).
**Tilos:** batch-szintű közös token; tranzakciós összevonás.

---

## 5. hullám — a 4. hullám tapasztalatai után

### Kártya C3 — Proaktív insight-ok (heti észrevételek)

**Ág:** `feature/llm-c3-insights` · **Effort:** 3 nap · **Modell:** sonnet.

**Olvasd el először:** `DailyDigestContent` + a digest-job ütemezése (minta a
heti jobhoz) · notification-feed írási útja · `HallucinationGuard` (a
forrás-validálási minta).

**Döntések (zárva):**
- Heti EGY futás (Hangfire, az F7-es ai-queue-n); bemenet: a hét eseményei
  (új doksik/határidők/lejáratok) + aggregátumok; kimenet: MAX 3 rövid
  észrevétel a notification-feedbe.
- Séma-validált JSON; MINDEN észrevétel forrás-ID-kkal — ami nem
  hivatkozható, eldobódik (HallucinationGuard-minta).
- **Szigorúan jelzés, sosem cselekvés:** az észrevétel gombja előre kitöltött
  tool-proposalra visz (ott a szokásos confirm).
- Kikapcsolható: `Ai:Insights:Enabled` (default **false** — opt-in).

**Lépések:** job + prompt (`weekly-insights.v1.txt`) + forrás-validálás →
feed-bekötés + FE gomb → proposal-elővezetés → tesztek (unit: forrás nélküli
észrevétel eldobódik, max 3; integrációs: feed-bejegyzés keletkezik).
**Elfogadás:** minta-héten értelmes észrevételek, mind forrás-hivatkozott ·
flag-off = semmi nem fut.
**Tilos:** automatikus cselekvés; napi gyakoriság; push-csatorna (cr260712-05
után külön).

### Kártya C4 — Vision-bemenet (fotózott dokumentum)

**Ág:** `feature/llm-c4-vision` · **Effort:** 3–4 nap · **Modell:** sonnet.
**⛔ EMBERI KAPU:** a vision-modell kiválasztása (RAM-igény!) emberi döntés —
javaslatot készíts (qwen-VL / llava osztály, eval-mérésekkel), a compose-módosítás
DRAFT.

**Olvasd el először:** `CompositeDocumentTextExtractor` (az illesztési pont) ·
az OCR-konfidencia elérhetősége a Tesseract-ágból · `eval/scanned/` (F2/F8).

**Döntések (zárva):**
- A vision-ág a Tesseract MELLETT (nem helyett) fut: akkor aktiválódik, ha
  az OCR-konfidencia alacsony (küszöb konfigból).
- Csak erős-gép profilon engedélyezett (`Ai:Vision:Enabled`, default false).
- Kimenet ugyanaz a szöveg-kinyerési kontrakt, mint az OCR-é — downstream
  változatlan.

**Lépések:** vision-hívás az OllamaHttpClient-ben (image-input) →
Composite-ág + konfidencia-kapu → eval a scanned-készleten (OCR vs. vision
vs. hibrid) → tesztek.
**Elfogadás:** gyűrött/kézírásos mintán a vision-ág mérhetően jobb kinyerést
ad (eval-lel dokumentálva) · flag-off = változatlan pipeline.
**Tilos:** a Tesseract-ág eltávolítása; alap-profilon bekapcsolás.

---

## Ollama platform-képességek térképe (referencia)

| Képesség | Hol használjuk |
|---|---|
| `format: <json-schema>` | F1 (06-os terv) |
| natív `tools` paraméter | C1 |
| `stream: true` | A2 |
| vision-modellek | C4 |
| `keep_alive` | F1/F7 |
| `OLLAMA_MAX_LOADED_MODELS` | F6 routing (kis router- + nagy QA-modell együtt) |

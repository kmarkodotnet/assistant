# A helyi LLM mélyebb bevonása — természetesebb kérdezés és keresés

> Státusz: JAVASLAT v1.0 · Dátum: 2026-07-12
> Alap: az AI-réteg átvizsgálása ([05](05-ai-minoseg-javitasok.md)) és a
> végrehajtási terv ([06](06-ai-minoseg-vegrehajtasi-terv.md)); kapcsolódó
> CR-ötletek: [04-cr-otletek.md](04-cr-otletek.md)
> Vezérelv: minden javaslat a meglévő biztonsági mintákra épül — LocalOnly
> privacy-kapu, RBAC-szűrt retrieval, írás kizárólag aláírt+megerősített
> tool-híváson át (ADR-0011).

---

## 0. Honnan indulunk

A mai LLM-bevonás **egy fordulós és reaktív**: a felhasználó ír egy
kérdést/parancsot (`SearchMode: Auto|Filter|Text|Semantic|Qa|Command`),
a rendszer egyszer válaszol. A `SearchRequest`-ben nincs beszélgetés-
előzmény (a FE `history` signalja csak megjelenítésre való), a válasz nem
streamel, az intent-döntés kulcsszó-heurisztika, és az LLM sosem
kezdeményez. Az alábbi javaslatok ezt mozdítják el egy **beszélgető,
kontextust tartó, több lépésben gondolkodó** asszisztens felé — úgy, hogy
minden írási művelet megmarad a proposal→confirm kapun belül.

A javaslatok három érettségi szintbe rendeződnek:

- **A szint — beszélgetés** (a meglévő egy-fordulós flow kiterjesztése)
- **B szint — értelmezés és visszakérdezés** (a lekérdezés-megértés mélyítése)
- **C szint — ügynöki viselkedés** (több lépéses, eszközhasználó LLM)

---

## A szint — Valódi beszélgetés

### A1. Többfordulós kontextus (follow-up kérdések) ⭐ a legnagyobb UX-ugrás

**Ma:** „Mikor jár le a Suzuki biztosítása?" → válasz. „És a Fordé?" →
a rendszer nem tudja, miről beszélünk.

**Javaslat:** a `SearchRequest` kapjon `conversation` mezőt (az utolsó
N=6–10 fordulat: kérdés + válasz-összefoglaló + hivatkozott entitás-ID-k).
Két felhasználási pont:

1. **Kérdés-önállósítás (query condensation):** egy olcsó LLM-hívás a
   follow-up kérdést önálló kérdéssé írja át („És a Fordé?" →
   „Mikor jár le a Ford biztosítása?"), és **ez** megy a meglévő
   retrieval-be. Így a teljes kereső-pipeline (FTS+szemantikus+RRF)
   változatlan marad — csak a bemenete lesz jobb.
2. **Q&A-prompt kontextus:** a qa-magyar prompt megkapja az előzményt,
   hogy a válasz stílusban/tartalomban kapcsolódjon.

**Építőelemek:** `SearchRequest` bővítés (kontrakt-delta), 1 új prompt
(`condense-question.v1`), a FE `search.facade.ts` már tárolja az
előzményt — csak fel kell küldeni. RBAC-kockázat nincs: a retrieval
ugyanúgy szűrt marad.

**Effort:** 1–1,5 nap. **Előfeltétel:** F2 eval (follow-up tesztkérdésekkel
bővítve).

### A2. Streamelt válasz (SSE/SignalR)

**Ma:** `stream: false` — 30b modellnél a felhasználó 10–30 mp-ig nézi a
spinnert.

**Javaslat:** az Ollama natívan streamel; a Q&A-válasz tokenenként menjen
ki SSE-n vagy a meglévő `NotificationsHub`/külön hub-on át, a FE
chat-bubble folyamatosan épüljön. A strukturált mezők (citációk,
konfidencia) a stream végén, záró eseményben érkeznek. A
`HallucinationGuard` a stream végén fut — ha elbukik, a UI lecseréli a
szöveget a fallbackre (ritka, F1 `format:json` mellett).

**Építőelemek:** `OllamaHttpClient` stream-móddal, 1 SSE endpoint vagy
hub-metódus, FE fogadó. **Effort:** 2 nap. **Megjegyzés:** a vélt sebesség
javulása nagyobb, mint bármely modellcseréé.

### A3. Beszélgetés a dokumentummal (detail-oldali chat)

**Javaslat:** a dokumentum-részletoldalon „Kérdezz erről a dokumentumról"
mező: a retrieval kihagyható (a kontextus adott — a dokumentum chunkja(i)),
csak a QA-prompt fut a kiválasztott doksira szűkítve. Olcsó, mert a
meglévő `OllamaQuestionAnswerer` újrahasznosítható egy `documentId`-szűrős
retrieval-lel; a RBAC-ellenőrzés a meglévő `CanReadDocument`.

Használati példák: „Mennyi az önrész?", „Meddig érvényes?",
„Mit kell magammal vinnem?" (orvosi beutalónál).

**Effort:** 1,5 nap (BE szűkített QA + FE panel).

---

## B szint — Mélyebb kérdés-értelmezés

### B1. LLM-alapú lekérdezés-tervező (a heurisztikus intent-router fölé)

**Ma:** a 31 soros `IntentClassifier` kulcsszavakra illeszt; ami nem
illeszkedik, alacsony konfidenciájú `Find` lesz. A magyar ragozás
(„biztosításról", „biztosításaink") a kulcsszavas ágat könnyen elviszi.

**Javaslat:** kétlépcsős router — a heurisztika marad az olcsó gyorsútnak,
de **alacsony konfidenciánál** (< 0.5) egy LLM-hívás ad strukturált
lekérdezés-tervet (a `ToolCallPlanner` bevált mintájával: séma-validált
JSON, 1 korrekciós retry):

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

A `filters` a meglévő `FilterSearchHandler`-be, a `rewrittenQuery` a
FTS+szemantikus ágba megy — tehát az LLM **nem keres, hanem fordít**:
természetes magyarról a rendszer saját lekérdező-nyelvére. A relatív
dátumok („múlt hónapban", „tavaly ősszel") itt normalizálódnak — a
tool-calling planner NowUtc+TimeZone mintája újrahasznosítható.

**Effort:** 2–3 nap. **Előfeltétel:** F2 (intents.jsonl bővítése ragozott/
körülírt esetekkel — a heurisztika vs. LLM-router A/B itt mérhető).

### B2. Lekérdezés-bővítés a magyar morfológia ellen (multi-query)

**Ma:** egyetlen query-embedding megy a pgvectorba; a magyar toldalékolás
és szinonimák (kocsi/autó/gépjármű) rontják a találatot.

**Javaslat:** a szemantikus ág kérdésenként 2–3 átfogalmazást generáltat
(egy olcsó hívás: „add meg a kérdés 2 alternatív megfogalmazását"),
mindet embeddeli (a `QueryEmbeddingCache` cache-eli), és a találatokat a
**meglévő RRF** fésüli össze — az infrastruktúra kész, csak több listát
kap. Latency-érzékeny, ezért csak Qa/Find intentnél és konfigból
kapcsolhatóan.

**Effort:** 1 nap. **Mérés:** recall@10 az eval-készleten (F5-tel közös
metrika).

### B3. Tisztázó visszakérdezés (clarification)

**Ma:** ha a tool-calling planner nem tud egyértelmű javaslatot adni,
`ResolveFailed` üzenet jön („nem találtam ilyen feladatot") — zsákutca.

**Javaslat:** a planner (és a B1-router) kapjon harmadik kimenetet:

```json
{"action": "clarify", "question": "Két »orvos« feladatot találtam:
  a) Peti fogorvos (júl. 20.), b) Anya vérvétel (júl. 15.). Melyikre gondolsz?",
  "options": [{"label": "a", "ref": "..."}, {"label": "b", "ref": "..."}]}
```

A FE a kérdést gomb-opciókkal jeleníti meg; a választás a `conversation`
kontextussal (A1) megy vissza, és a második kör már egyértelmű javaslatot
ad. A `RefMatcher` (már létezik a tool-oknál) több-találatos ágából ez
természetesen következik.

**Effort:** 2 nap (planner-séma + FE opció-gombok). **Előfeltétel:** A1.

---

## C szint — Ügynöki viselkedés (eszközhasználó LLM)

### C1. Csak-olvasó eszközök megerősítés nélkül — „kutató asszisztens"

**Kulcsgondolat:** az ADR-0011 megerősítés-kapuja az **írási** műveleteket
védi. A **csak-olvasó** műveletekhez (keresés, számolás, lekérdezés) nem
kell confirm — itt az LLM szabadon dolgozhat több lépésben. Ollama-oldalon
a natív tool-calling API (`tools` paraméter a `/api/chat`-ben) ezt
közvetlenül támogatja.

Read-only tool-készlet (mind a meglévő, RBAC-szűrt query-utakra épül):

| Tool | Mögötte |
|---|---|
| `search_documents(query, filters)` | meglévő hibrid keresés |
| `get_document_summary(id)` | `document_summary` tábla |
| `list_deadlines(range, member)` | Deadlines query |
| `list_tasks(status, member)` | Tasks query |
| `aggregate_financial(kind, range)` | `AggregateSearchHandler` (CR260710-01) |

A Q&A így iteratívvá válik: „Mennyit költöttünk idén autóra?" → a modell
`aggregate_financial`-t hív, majd `search_documents`-et a részletekhez,
és a végén idézett forrásokkal válaszol. Max 3–5 tool-kör/kérdés
(költségplafon), minden köztes hívás auditálva (`AiCall`).

**Írás továbbra is kizárólag** a proposal→confirm úton — a két tool-tér
(read-only vs. write) a registry-ben szétválasztva.

**Effort:** 4–5 nap. **Előfeltétel:** F1 (tool-hívás formátum), F2
(end-to-end eval-kérdések), F6 (ehhez már közepes+ modell kell — 3b-n
mérni, hogy megbízható-e).

### C2. Láncolt írási javaslatok (batch-proposal)

„Vidd fel a jövő heti fogorvost Petinek és emlékeztess előtte 1 nappal"
ma két külön parancs. **Javaslat:** a planner adhasson **javaslat-listát**
(2–3 összefüggő tool-hívás), a FE egy kártyán mutatja, a felhasználó
**egyben** hagyja jóvá — de a végrehajtás tételenként tokenizált (a
meglévő replay-guard tételenként érvényes marad). A confirm-kapu tehát
nem gyengül, csak a UX rövidül.

**Effort:** 2–3 nap. **Kapcsolódik:** cr260712-02 (tool-katalógus bővítés).

### C3. Proaktív insight-ok — az LLM kezdeményez (óvatosan)

A meglévő proaktív felületek (DailyDigest — sablonos, ImportantEmail —
osztályozó) mellé egy heti egyszeri „észrevételek" job:

- bemenet: a hét eseményei (új doksik/határidők/lejáratok) + aggregátumok;
- kimenet: max 3 rövid észrevétel a notification-feedbe
  („A kocsi műszakija 30 napon belül lejár, és nincs hozzá időpont-feladat.",
  „Ez a harmadik emelt összegű villanyszámla egymás után.");
- séma-validált JSON-kimenet, minden észrevétel forrás-ID-kkal — ami nem
  hivatkozható, eldobódik (HallucinationGuard-minta).

Szigorúan **jelzés, sosem cselekvés**: az észrevétel gombja egy előre
kitöltött tool-proposalra visz (ott a szokásos confirm). Kikapcsolható
(`Ai:Insights:Enabled`).

**Effort:** 3 nap. **Előfeltétel:** F2 (az észrevétel-minőség máskülönben
mérhetetlen), C1 read-only toolok előny.

### C4. Multimodális bővítés — fotó mint bemenet

Az Ollama vision-modellekkel (pl. qwen-VL / llava osztály) a
telefonnal fotózott számla/levél **egy lépésben** érthető meg: a vision-LLM
a Tesseract-ág *mellett* (nem helyett) fut, és strukturált kinyerést ad
gyűrött/kézírásos dokumentumon is, ahol az OCR elvérzik. Illesztési pont:
a `CompositeDocumentTextExtractor` kap egy vision-ágat, amely akkor fut,
ha az OCR-konfidencia alacsony.

**Effort:** 3–4 nap + modellválasztás (RAM!). **Előfeltétel:** F2
`scanned/` készlet (F8-cal közös), erős-gép profil.

---

## Ollama-specifikus lehetőségek (kihasználatlan platform-képességek)

| Képesség | Mire jó itt | Hivatkozás |
|---|---|---|
| `format: <json-schema>` | garantáltan séma-helyes kimenet minden strukturált feladatra | F1 (06-os terv) |
| natív `tools` paraméter | C1 read-only agent-kör kézi prompt-katalógus helyett | C1 |
| `stream: true` | A2 streamelt válasz | A2 |
| vision-modellek | C4 fotó-bemenet | C4 |
| `keep_alive` | válasz-latency (hidegindítás ellen) | F1/F7 |
| párhuzamos modellek (`OLLAMA_MAX_LOADED_MODELS`) | kis router-modell + nagy QA-modell egyszerre memóriában | F6 routing |

---

## Ütemezési javaslat

| Hullám | Tartalom | Effort | Feltétel |
|---|---|---|---|
| 1. | **A1 többfordulós kontextus + A2 streaming** | ~3,5 nap | F1–F2 kész |
| 2. | **B1 LLM-router + B3 visszakérdezés** | ~4–5 nap | 1. hullám |
| 3. | **A3 dokumentum-chat + B2 multi-query** | ~2,5 nap | – |
| 4. | **C1 read-only agent + C2 batch-proposal** | ~6–8 nap | F6 modellválasztás |
| 5. | **C3 proaktív insight + C4 vision** | ~6–7 nap | 4. hullám tapasztalatai |

**Miért ez a sorrend:** az 1–2. hullám adja a „természetesebben lehet
kérdezni" érzés zömét (kontextus + azonnali visszajelzés + okosabb
értelmezés + zsákutca helyett visszakérdezés), kis kockázattal és a
meglévő pipeline-ra építve. A C szint a legnagyobb képesség-ugrás, de
modell-minőség-függő — előbb az eval-harness (06-os terv F2) és a
modell-döntés (F6) legyen meg.

## Változatlan garanciák (minden hullámra)

1. **LocalOnly:** minden hívás a helyi Ollamára megy (`AiProviderFactory`
   kapuja érintetlen); egyetlen javaslat sem igényel felhő-szolgáltatást.
2. **Írás = proposal + confirm:** a C szint sem old fel semmit az
   ADR-0011-ből; a read-only/write tool-terek szét vannak választva.
3. **RBAC a retrievalben:** minden új keresési út a meglévő szűrt
   query-rétegeken át megy (F0 ellenőrzés után).
4. **Mérhetőség:** minden hullám az eval-készlet bővítésével együtt
   érkezik (follow-up kérdések, ragozott intentek, agent-forgatókönyvek).
5. **Auditálhatóság:** minden LLM-kör `AiCall` audit-bejegyzés, a
   proaktív észrevételek forrás-ID nélkül eldobódnak.

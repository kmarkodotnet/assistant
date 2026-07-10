# Kereső stratégia — Family OS

> Státusz: DRAFT v0.1 · Dátum: 2026-06-26 · Nyelv: magyar
> Kapcsolódó: [database-schema.md](database-schema.md), [ai-pipeline.md](ai-pipeline.md),
> [domain-model.md](domain-model.md), [security-privacy.md](security-privacy.md)

---

## 1. Cél és vezérlőelvek

A kereső egyszerre szolgál ki **listázó** (pl. „kifizetetlen számlák"),
**megtaláló** (pl. „mosógép garancia") és **kérdés-válasz** (pl. „mikor jár
le az autó kötelező?") use case-eket. Ezekhez három alaprétegre épülő
**hibrid retrieval** kell, fölötte egy **kérdés-routerrel** és — opcionálisan
— egy LLM-alapú **válasz-szintézissel**.

Vezérlőelvek:

1. **Az AI nem találhat ki tényt.** Minden válasz csak a saját adatbázisból
   származó hivatkozott források alapján készül. „Nincs adat" egy elfogadható,
   sőt elvárt válasz.
2. **Strukturált adat erősebb a vektornál.** Ha egy kérdés
   egyértelműen strukturált mezőre szól (lejárati dátum, kifizetetlen
   számla), a strukturált lekérdezés autoritatív. A vektorkeresés csak
   ezt egészíti ki vagy fallback.
3. **RBAC mindenhol.** A keresés első lépése a current user-re vetített
   láthatóság-szűrés. `IsPrivate = true` rekord más felhasználónak nem jön
   vissza, és az LLM elé sem kerül.
4. **Magyar nyelv.** Tokenizer, stemmer (`hungarian_unaccent` FTS config),
   embedding modell (`nomic-embed-text` — magyar elfogadható), Q&A prompt,
   UI mind magyar.
5. **Egyetlen API.** Egyetlen `POST /api/v1/search` endpoint fogad mindent
   (`mode = filter | text | semantic | qa | auto`); az `auto` az alapértelmezett,
   és az intent classifier dönt a routingról.

---

## 2. A három retrieval réteg

### 2.1 Strukturált szűrés (filter)

- **Cél:** ismert mezők szerinti listázás (kifizetetlen számlák, közelgő
  határidők, családtag dokumentumai, dátumtartomány, kategória).
- **Implementáció:** EF Core LINQ → SQL, indexelt mezők szerinti scan.
- **Példa endpoint paraméterek:**
  ```
  POST /api/v1/search
  {
    "mode": "filter",
    "entityTypes": ["Document", "FinancialRecord"],
    "filters": {
      "isPaid": false,
      "dueDateBefore": "2026-07-31",
      "topics": ["penzugy/szamla"],
      "relatedFamilyMemberId": "..."
    },
    "sort": { "field": "dueDate", "order": "asc" },
    "page": 1, "pageSize": 50
  }
  ```
- **Indexelés:** ezt a réteget a `database-schema.md`-ben rögzített partial
  index-ek hajtják (pl. `ix_financial_unpaid_due`, `ix_deadline_due`,
  `ix_document_processing_pending`).

### 2.2 Teljes szöveges keresés (full-text)

- **Cél:** szabad-szavas kereső dokumentumokban, jegyzetekben, feladatokban
  és határidőkben (klasszikus „kereső doboz").
- **Lefedett entitások és implementáció:**
  - **Document** (tartalomszöveg + cím) — `tsvector` alapú (`document_text.tsv`).
  - **Note** (cím + törzs) — EF Core LINQ `.Contains()` (kis adatmennyiség
    mellett elegendő; v2-ben tsvector-ra válthat).
  - **Task** (nem-Suggested) és **Suggestion** (Suggested státuszú Task) —
    `tsvector` alapú (`task.tsv`), lásd 2.2.2.
  - **Deadline** — `tsvector` alapú (`deadline.tsv`), lásd 2.2.2.
  - **Reminder** (kapcsolódó Task/Deadline cím + snoozeNote) —
    EF Core LINQ `.Contains()` (join-jellegű lekérdezés, kis tábla).
- **Document FTS lekérdezés-minta (tsvector alapú):**
  ```sql
  SELECT d.id, d.title, ts_rank(dt.tsv, q) AS rank,
         ts_headline('hungarian_unaccent', dt.content, q,
                     'MaxFragments=2, MaxWords=12, MinWords=5') AS snippet
  FROM   app.document d
  JOIN   app.document_text dt ON dt.document_id = d.id
  CROSS  JOIN websearch_to_tsquery('hungarian_unaccent', :q) q
  WHERE  dt.tsv @@ q
    AND  d.deleted_utc IS NULL
    AND  (d.is_private = false OR d.created_by_user_account_id = :current_user)
  ORDER  BY rank DESC, d.created_utc DESC
  LIMIT  50;
  ```
- **Notes:**
  - `websearch_to_tsquery` támogatja a `"kifejezés"`, `-szó`, `OR` szintaxist.
  - A `ts_headline` adja a UI-snippet-et.

#### 2.2.1 Korábbi megközelítés (levált — dokumentálva a döntés indoklásához)

Task/Deadline/Suggestion FTS korábban EF Core `.Contains()` (`LIKE`) alapú
volt, tsvector index nélkül. Több szavas lekérdezésnél (pl. „áramszámla
határideje") ezt egy **PrimaryWord fallback** heurisztikával egészítettük ki
(a leghosszabb, ≥4 karakteres szó önálló OR-keresése), mert a teljes phrase
LIKE-egyezés nem talált olyan találatot, ahol csak egy szó egyezik (pl.
„áramszámla befizetése" határidő cím). Ez működött, de: (1) nem indexelt
(teljes táblaszkennelés minden keresésnél), (2) nincs ékezet-/tő-kezelés,
(3) csak EGY szót próbál OR-ral, nem az összeset. Levált — lásd 2.2.2.

#### 2.2.2 Task/Deadline `tsvector` alapú FTS (jelenlegi megközelítés)

A `task` és `deadline` táblák `tsv` generated stored oszlopot kapnak
(`database-schema.md` 4.13.2, 4.14.2) — ugyanaz a `hungarian_unaccent`
konfiguráció, mint a Document/Note-nál. A lekérdezés a
`websearch_to_tsquery`-t **OR-relációval** hívja a szavak között (nem a
`websearch_to_tsquery` alapértelmezett AND-jával), hogy a régi PrimaryWord
fallback szemantikáját megőrizze és kiterjessze **minden** szóra, nem csak
a leghosszabbra:

```csharp
// "áramszámla határideje" → "áramszámla or határideje" → tsquery: 'aramszamla' | 'hataridej'
var tokens = query.Split(' ', RemoveEmpty | TrimEntries);
var tsQueryText = tokens.Length > 1 ? string.Join(" or ", tokens) : query;
```

```sql
SELECT t.id, t.title, t.description AS snippet,
       ts_rank(t.tsv, websearch_to_tsquery('hungarian_unaccent', @q)) AS rank
FROM   app.task t
WHERE  t.tsv @@ websearch_to_tsquery('hungarian_unaccent', @q)
  AND  t.deleted_utc IS NULL
  AND  t.status <> 'Suggested'   -- Suggestion lekérdezésnél: t.status = 'Suggested'
  AND  (@userId IS NULL OR t.is_private = false
        OR t.created_by_user_account_id = @userId::uuid)
ORDER BY rank DESC
LIMIT  @limit;
```

Ugyanez a minta a `deadline` táblára. Ez a réteg indexelt (GIN), tő- és
ékezet-kezelt, és `ts_rank` szerint rendez (nem létrehozási dátum szerint).

**Architektúra:** az `IFamilyOsDbContext.Database.SqlQueryRaw` az
Application rétegből nem érhető el (nincs `Microsoft.EntityFrameworkCore.
Relational` referencia — tudatos réteghatár). A raw SQL ezért egy külön
`ITaskDeadlineFtsSearchService` absztrakció mögé kerül
(`Application/Abstractions/Persistence`), Infrastructure-beli
implementációval — ugyanaz a minta, mint az `ISemanticSearchService`-nél.

### 2.3 Szemantikus / vektor keresés (pgvector)

- **Cél:** Jelentés-alapú találat, ha a szó-egyezés gyenge („Philippines út"
  → „Fülöp-szigetek tervezés").
- **Implementáció:**
  - Kérdés/szöveg embedding: `IEmbedder.EmbedAsync(query)` →
    `vector(768)` (`nomic-embed-text`).
  - HNSW index `document_chunk`, `note_chunk`, `task_chunk` és
    `deadline_chunk` táblákon, cosine similarity.
- **Minimum similarity küszöb:**
  - Standalone szemantikus keresés (`mode=semantic`): **0.60** — a korábbi
    0.50 még átengedett triviálisan tárgyatlan találatokat (pl. „áramszámla"
    keresésre egészségügyi dokumentumot); a 0.60 tapasztalati úton szűkebb,
    de még nem vág el valódi releváns találatot a `nomic-embed-text`
    modell cosine-similarity eloszlásán.
  - Hibrid / Q&A kontextus-gyűjtés (`mode=auto`, `mode=qa`): **0.20** —
    szélesebb hálóval dolgozik, az RRF-fusion úgyis rendet rak.
- **Lekérdezés-minta:**
  ```sql
  SELECT entity_type, entity_id, chunk_id, snippet, score FROM (
      SELECT 'document' AS entity_type,
             dc.document_id AS entity_id, dc.id AS chunk_id,
             dc.content AS snippet,
             1 - (dc.embedding <=> :vector) AS score
      FROM app.document_chunk dc
      JOIN app.document d ON d.id = dc.document_id
      WHERE dc.embedding IS NOT NULL AND dc.embedding_model = :model
        AND d.deleted_utc IS NULL
        AND (:userId IS NULL OR d.is_private = false
             OR d.created_by_user_account_id = :userId)
      UNION ALL
      SELECT 'note', nc.note_id, nc.id, nc.content,
             1 - (nc.embedding <=> :vector)
      FROM app.note_chunk nc JOIN app.note n ON n.id = nc.note_id
      WHERE nc.embedding IS NOT NULL AND nc.embedding_model = :model
        AND n.deleted_utc IS NULL
        AND (:userId IS NULL OR n.is_private = false
             OR n.created_by_user_account_id = :userId)
      UNION ALL
      SELECT 'task', tc.task_id, tc.id, tc.content,
             1 - (tc.embedding <=> :vector)
      FROM app.task_chunk tc JOIN app.task t ON t.id = tc.task_id
      WHERE tc.embedding IS NOT NULL AND tc.embedding_model = :model
        AND t.deleted_utc IS NULL
        AND (:userId IS NULL OR t.is_private = false
             OR t.created_by_user_account_id = :userId)
      UNION ALL
      SELECT 'deadline', dc2.deadline_id, dc2.id, dc2.content,
             1 - (dc2.embedding <=> :vector)
      FROM app.deadline_chunk dc2 JOIN app.deadline d2 ON d2.id = dc2.deadline_id
      WHERE dc2.embedding IS NOT NULL AND dc2.embedding_model = :model
        AND d2.deleted_utc IS NULL
        AND (:userId IS NULL OR d2.is_private = false
             OR d2.created_by_user_account_id = :userId)
  ) combined
  WHERE score >= :minSimilarity
  ORDER BY score DESC
  LIMIT :limit;
  ```
- **`embedding_model` szűrés kötelező** — vegyes modellből származó vektorokat
  nem hasonlítunk össze; ez a fokozatos modell-migráció biztonsági kapuja.

---

## 3. Hibrid retrieval (a három réteg kombinálása)

Egy „dokumentum-megtaláló" lekérdezésnél a három forrásból párhuzamosan
gyűjtünk találatokat, majd egyetlen relevancia-pontszámban összeolvasztjuk
(**Reciprocal Rank Fusion**, RRF — egyszerű, jó, paraméter-független).

### 3.1 Algoritmus

```
def hybrid_search(query, filters, current_user, top_k=20):
    # 1. Strukturált pre-filter (szigorú: belőle dolgozik mind a 3 alréteg)
    candidate_ids = sql_structured(filters, current_user)
    # 'candidate_ids' egy bitset; ha túl nagy (>10k), csak akkor szűrjük

    # 2. Párhuzamos retrieve
    fts_hits   = fts_search(query, candidate_ids, limit=50)
    vec_hits   = vector_search(query_embedding, candidate_ids, limit=50)
    exact_hits = exact_match_search(query, candidate_ids, limit=10)
       # exact: title / vendor / serial_number ILIKE '%query%'

    # 3. RRF fusion
    scores = {}
    for rank, hit in enumerate(fts_hits):   scores[hit.id] += 1/(60 + rank)
    for rank, hit in enumerate(vec_hits):   scores[hit.id] += 1/(60 + rank)
    for rank, hit in enumerate(exact_hits): scores[hit.id] += 2/(60 + rank)  # boost

    # 4. RBAC ellenőrzés (defenzív, mert FTS/vektor query already szűrt)
    return [h for h in sorted_by_score(scores) if visible(h, current_user)][:top_k]
```

A `k = 60` egy bevett RRF-konstans. Az exact-match találatokat dupla súllyal
vesszük (felhasználói intuíció szerint az „pontosan így keresem" magas
prioritású).

### 3.2 Eredmény-payload

```json
{
  "hits": [
    {
      "id": "...",
      "type": "Document",
      "title": "AXA kötelező biztosítás 2025-2026",
      "snippet": "…lejárati dátum: 2026.09.14…",
      "score": 0.0428,
      "highlights": ["AXA", "kötelező", "lejár"],
      "url": "/documents/<id>"
    },
    { "type": "Note", ... },
    { "type": "FinancialRecord", ... }
  ],
  "totalEstimate": 7,
  "facets": {
    "topic":  [{"slug":"jarmu/kotelezo","count":3}, ...],
    "year":   [{"value":"2026","count":4}, {"value":"2025","count":3}],
    "memberId": [...]
  }
}
```

A `facets` blokk a UI bal oldali szűrősávjához kerül — pre-aggregált
számláló az ismert facetekre (topic, év, családtag).

---

## 4. Természetes nyelvű kérdés-válasz (Q&A)

Ez a hibrid retrieval fölötti réteg — egyetlen rövid, hivatkozott válasz
természetes magyar nyelven. A `IQuestionAnswerService.AnswerAsync` ezt
implementálja.

### 4.1 Folyamat

```
1. Intent classification (lokális, szabály-alapú — lásd 5. szakasz):
   - "filter"   : strukturált listázás (pl. "kifizetetlen számlák")
   - "lookup"   : konkrét tény (pl. "mikor jár le X")
   - "find"     : dokumentum/jegyzet megtalálása (pl. "hol van Y garancia")
   - "summarize": tartalmi összegzés (pl. "mit döntöttünk Z-ről")

2. Slot extraction (kis LLM hívás vagy szabályok):
   - dátum-tartomány, családtag-név, kategória, kulcsszavak

3. Retrieval ág a routing alapján:
   - filter   → tisztán strukturált SQL → eredménylista
   - lookup   → strukturált SQL (facet entitásokra) + ha üres, hibrid
   - find     → hibrid retrieval (FTS + vektor + exact)
   - summarize → hibrid retrieval szélesebb top_k-val (50)

4. Válasz-szintézis:
   - "filter" módban LLM NEM hívódik, a UI listát renderel a structured
     eredményből (gyorsabb, olcsóbb, biztosabb).
   - A többi módban LLM (`IQuestionAnswerService`):
       prompt = sysprefix + retrieved_chunks_json + question
       output = { answer, citedSources[] }
   - A citedSources csak olyan ID lehet, amit a retrieval visszaadott.

5. Válasz validáció:
   - Ha a prompt mentális hibrid kimenetet ad (pl. azt mondja "2026.09.14",
     de ezt a chunk nem tartalmazza), az ellenőrzés szúr (regex / fuzzy).
   - Ha validációs hiba: visszaesés "Nincs erre vonatkozó adat..."-ra.
```

### 4.2 Példa kimenet (UC-02)

```json
{
  "mode": "lookup",
  "answer": "Az autó kötelező biztosítása 2026-09-14-én jár le, "
            "az AXA 2025-09-15-én kiállított kötvénye alapján.",
  "citedSources": [
    {
      "type": "Document",
      "id": "...",
      "title": "AXA kötelező biztosítás 2025-2026",
      "snippet": "…lejárati dátum: 2026.09.14…",
      "url": "/documents/<id>"
    }
  ],
  "confidence": 0.92,
  "tookMs": 1840
}
```

---

## 5. Intent classification — magyar szabályok

A 4.1-es 1. lépés egy **lokális, szabály-alapú** osztályozó, kevés LLM
hívással. Indok: olcsó, gyors, debuggolható, és a magyar kérdés-mintázatok
kicsik.

### 5.1 Heurisztikák

```
filter   : tartalmaz "összes", "minden", "mutasd", "listázd",
           "milyen ... vannak", "kik", "hányadik" + névszói kifejezés
lookup   : tartalmaz "mikor", "hány", "mennyi", "melyik dátum",
           "ki a felelős" + egy konkrét nominális tárgy
find     : tartalmaz "hol van", "hol találom", "küldd el", "nyisd meg" +
           tárgy
summarize: tartalmaz "mit döntöttünk", "mi volt", "foglald össze",
           "összefoglalva"
```

Több szabály egyszerre tüzelhet — fallback prioritás: `filter > lookup >
find > summarize`. Ha egyik sem tüzel, az `auto` mód a `find`-et választja.

### 5.2 Konfidencia kapu

Ha az intent classifier konfidenciája < 0.55, a router **párhuzamosan** futtat
filter + hybrid retrievalt, és az LLM-nek átadja mindkettő top-3 találatát.
A költség kisebb, mint egy téves intent miatti üres lista.

### 5.3 Slot-kinyerés

A slot-kinyerés (dátumtartomány, családtag, kategória) a **retrieval
előtt** fut, külön kis LLM-hívással (`extract-search-slots` prompt) vagy
szabályokkal — az eredménye szűkíti a retrieval-t (4.1/2. lépés). A Q&A
válasz emellett visszaadja a ténylegesen alkalmazott slotokat a UI
chip-jeihez:

```json
{
  "answer": "...",
  "citedSources": [...],
  "extractedSlots": {
    "dateRange": { "from": "2026-06-01", "to": "2026-06-30" },
    "familyMember": "Lili",
    "category": "School"
  }
}
```

A UI ezeket szűrő-chip-ekként mutatja, és a felhasználó kattintással
korrigálhatja → új query.

---

## 6. RBAC — láthatóság-szűrés

Minden retrieval-lépés a current user kontextusába van vetítve. Ez a
`SearchAuthorizationService`-ben centralizált:

```csharp
public IQueryable<Document> ApplyVisibility(IQueryable<Document> q, CurrentUser u)
{
    if (u.Role == UserRole.Admin) return q;

    if (u.Role == UserRole.Child)
        // ADR-0007: child csak a hozzá kötött, nem-privát rekordokat látja
        return q.Where(d => !d.IsPrivate
                        && d.RelatedFamilyMemberId == u.FamilyMemberId);

    return q.Where(d => !d.IsPrivate
                    || d.CreatedByUserAccountId == u.UserAccountId
                    /* + MedicalRecord partner-spouse kivétel a facet-szintű
                       szabályban (security-privacy.md 4.3) */);
}
```

- **Private rekord** csak a tulajdonosnak (és adminnak) látszik.
- **MedicalRecord** alapból `IsPrivate = true`, és a default láthatóság
  csak a `family_member` saját UserAccount-jához + adminokhoz tartozik.
- **Child** szerepkör ([ADR-0007](decisions/ADR-0007-child-szerepkor-rbac.md),
  normatív mátrix: security-privacy.md 4.1): **csak olvasás**, és csak a
  hozzá kötött (`related_family_member_id` = saját) nem-privát rekordok.

A szűrés **adatbázis-szintű** WHERE-be kerül, nem post-filter; így a relevancia-
ranking sem szivárogtat („találat van X dokumentumon, de nem mondom el, mi az").

---

## 7. Specifikus példa-kérdések — routing és válasz

A 13 példa kérdésből 7-et a brief is felsorol; itt mindegyikre konkrét
routing-tervet adunk.

### 7.1 „Mikor jár le az autó kötelező biztosítása?"
- **Intent:** `lookup` (`mikor`)
- **Slot:** kulcsszó = „autó kötelező biztosítás"
- **Retrieval:**
  - Strukturált: `Deadline WHERE category = 'Insurance' AND title ILIKE '%kötelező%' AND status IN ('Upcoming','Due') ORDER BY due_date_utc LIMIT 5`
  - Hibrid (FTS+vec) ugyanerre a szóra, dokumentumokon belül
- **Válasz-szintézis:** ha a strukturált egy hitre vezetett → LLM csak
  „magyarosít": *„Az autó kötelező biztosítása 2026-09-14-én jár le…"*

### 7.2 „Keresd meg a feleségem legutóbbi lab eredményét."
- **Intent:** `find` + `lookup` (legutóbbi)
- **Slot:** familyMember = „feleségem" → feloldás: `FamilyMember WHERE
  relation = 'Spouse'` és a current user nézőpontjából
- **Retrieval:** strukturált:
  ```sql
  SELECT mr.*, d.title, d.id AS document_id
  FROM   medical_record mr
  JOIN   document d ON d.id = mr.document_id
  WHERE  mr.family_member_id = :spouse_id
    AND  mr.record_type = 'LabResult'
    AND  mr.deleted_utc IS NULL
  ORDER  BY mr.record_date DESC
  LIMIT  1;
  ```
- **Válasz:** UI-on a dokumentum kártya + nyitógomb; LLM-szöveg opcionális.

### 7.3 „Milyen iskolai határidők vannak ebben a hónapban?"
- **Intent:** `filter`
- **Slot:** dateRange = aktuális hónap, category = `School`
- **Retrieval:** tisztán strukturált, LLM nem hívódik
- **Válasz:** lista UI, dátum szerint rendezve.

### 7.4 „Hol van a mosógép garanciája?"
- **Intent:** `find`
- **Slot:** kulcsszó „mosógép garancia"
- **Retrieval:**
  - Strukturált: `Warranty JOIN Document WHERE product_name ILIKE '%mosógép%' OR brand ILIKE '%mosógép%'`
  - Hibrid (FTS+vec) ha az előbbi üres
- **Válasz:** dokumentum kártya, `purchase_date`, `warranty_end_date`
  metaadattal.

### 7.5 „Mit döntöttünk a Philippines útról?"
- **Intent:** `summarize`
- **Slot:** kulcsszó „Philippines"
- **Retrieval:** hibrid, szélesebb top_k (30); várhatóan `Note` táblából
  jön a fő találat (jegyzet-jellegű döntés).
- **Válasz-szintézis:** LLM összegzi a 3-5 legmagasabb pontszámú chunkot
  egy 2-4 mondatos magyar válaszba forrás-jelöléssel.

### 7.6 „Melyek a kifizetetlen számlák?"
- **Intent:** `filter`
- **Retrieval:**
  ```sql
  SELECT * FROM financial_record
   WHERE is_paid = false AND deleted_utc IS NULL
   ORDER BY due_date ASC NULLS LAST;
  ```
- **Válasz:** lista, határidő-közelség jelölővel.

### 7.7 „Mutasd az összes 2025-ös egészségügyi dokumentumot."
- **Intent:** `filter`
- **Slot:** topic = `egeszsegugy/*`, dateRange = 2025-01-01 .. 2025-12-31
- **Retrieval:** strukturált, `document` + `document_topic` JOIN.

### 7.8 „Mikor volt Lili oltása?"
- **Intent:** `lookup`
- **Slot:** familyMember = „Lili" (név-egyezés), kulcsszó = „oltás"
- **Retrieval:** `MedicalRecord WHERE family_member_id = :lili AND record_type = 'Vaccination' ORDER BY record_date DESC`

### 7.9 „Hol vannak a kazán szerelési papírjai?"
- **Intent:** `find`
- **Slot:** kulcsszó „kazán"
- **Retrieval:** hibrid (Document topic = `otthon`, FTS „kazán", vektor).
- **Válasz:** lista, ha több találat van; pontosító kérdés („melyik
  szerelőé?") ha túl sok.

A többi 4 kérdés (UC-02 példáin túl) hasonló mintát követ — a routing,
intent + slot kinyerés, hibrid retrieval, és vagy közvetlen render vagy
LLM összegzés.

---

## 8. Ranking és relevancia-finomítás

### 8.1 Boost-szabályok

A nyers RRF-pontszámot az alábbi business-súlyokkal módosítjuk:

| Szabály | Súly |
|---|---|
| `Document.document_date` az utóbbi 12 hónapban | × 1.20 |
| Találat a current user által létrehozott rekordon | × 1.10 |
| Találat AI-jóváhagyott facet entitáson (Warranty/Financial/Medical) | × 1.30 |
| Találat `IsPrivate = true` rekordon, mások keresésben | szűrve (0) |
| Találat `Document.processing_status = Failed`-en | × 0.50 |

### 8.2 Tipikus visszacsatolás

A kattintott találatok aggregátum-számolása (`SearchClick` esemény →
async log): az ismételt kereséseknél a top-3-ban gyakran kattintott
rekord +0.05 boost-ot kap (per current user, max 30 nap).
**MVP-ben:** ezt csak loggoljuk, a boost-ot nem aktiváljuk — adatgyűjtés.

---

## 9. Teljesítmény

### 9.1 Várható számok

- **Strukturált filter** (10k document tábla): < 50 ms p95, indexelt mezőkön.
- **FTS** (75k chunk, magyar `tsvector`): < 150 ms p95.
- **Vektor (HNSW)** (75k chunk, 768-dim, top-50): < 80 ms p95 Postgres-ben.
- **Q&A teljes pipeline** Ollama-val: 1–3 s (intent + slot ~150 ms; retrieval
  ~200 ms; LLM válasz ~1500-2500 ms `gpt-oss:20b`-vel).

### 9.2 Cache stratégia

- **Embedding cache:** a felhasználói query embedding cache-elt 1 órára
  (LRU, ~500 entry) — gyakori ismétlődő kérdésekre („mit kell csinálni?")
  azonnali ismételt lefutást ad.
- **Q&A cache:** azonos question + current user + adatbázis-revízió
  hash → cache 15 percre. Invalidáció: bármely Document/Note insert/update
  esemény.
- **Topic taxonomy cache:** memóriában 5 percre — gyakran hivatkozott
  a Classify promptban is.

### 9.3 Pagination

- A `POST /api/v1/search` `page` + `pageSize` (max 100) támogatott.
- A `qa` mód NEM lapozható — egy válasz, max 5 idézett forrás. A felhasználó
  ha többet akar látni, mode-ot vált.

---

## 10. UI integráció (rövid, részletek a frontend-structure.md-ben)

- **Globális kereső sáv** a fejlécben — egyetlen szövegmező, default `auto`
  mód. Enter → `POST /api/v1/search { mode: 'auto', query }`.
- **AI-kereső oldal** (`/search`) — chat-szerű interfész, korábbi kérdések
  történettel, idézett források linkjeivel.
- **Filter panel** — minden listás nézeten (Documents, Tasks, Deadlines):
  bal oldalon az facet aggregátorok, a felhasználó kattintással szűkíti.
- **„Mentett keresés"** — egy `auto` mód query elmenthető és a dashboardon
  widget-ként jelenik meg (pl. „Kifizetetlen számlák", „Közelgő határidők
  Lili-nek").
- **Üres találat:** segítő javaslatok („Próbáld kevesebb szóval", „Tárolt
  dokumentumok a témakörben: …").

---

## 11. Korlátok és későbbi bővítések

- **Cross-lingual query:** angol kérdés → magyar dokumentum nem cél MVP-ben.
  (Embedding modell magyarra van hangolva.)
- **Konverzációs Q&A** (több fordulós követő kérdés) — MVP-ben single-turn;
  thread-context kontextus a v2-ben.
- **Reranking modell** — pl. cross-encoder lokálisan — a top-50 RRF
  találat újrarendezéséhez, MVP-ben nem futtatjuk.
- **Felhasználói visszajelzés** (Hasznos? Nem hasznos?) — UI mező MVP-ben,
  adatként gyűjtjük, a ranking-modell finomítására v2-ben.
- **Streaming válasz** (SSE) — a Q&A módban a token-szintű streaming UI-
  élmény jobb, de MVP-ben szinkron-batch.

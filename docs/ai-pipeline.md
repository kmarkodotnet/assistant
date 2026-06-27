# AI feldolgozó pipeline — Family OS

> Státusz: DRAFT v0.1 · Dátum: 2026-06-26 · Nyelv: magyar
> Kapcsolódó: [architecture.md](architecture.md), [domain-model.md](domain-model.md),
> [search-strategy.md](search-strategy.md), [security-privacy.md](security-privacy.md)
> Rögzített döntések: [ADR-0001 pgvector](decisions/ADR-0001-vektor-tarolas-pgvector.md),
> [ADR-0002 Tesseract](decisions/ADR-0002-ocr-tesseract.md)

---

## 1. Cél és vezérlőelvek

A pipeline minden bejövő tartalmat (feltöltött dokumentum, manuális jegyzet,
Gmail-üzenet) ugyanazon a feldolgozó lánc-on enged át, hogy a struktúrázott
metaadat (összefoglaló, dátumok, javasolt feladatok, embedding) végül a saját
adatbázisba kerüljön.

Vezérlőelvek:

1. **Az AI nem aktivál.** Minden AI-eredmény `Origin = AiSuggested` státusszal
   kerül be (Task, Deadline, Tag, Topic — kapcsolódás). A user `Approve`-val
   állítja `AiApproved`-ra; addig nem tüzelnek belőle emlékeztetők.
2. **Idempotens lépések.** Egy lépés (`AiProcessingJob`) többszöri futása
   nem hoz létre duplikátumot. A target oldalon vagy upsert (egyetlen
   `IsCurrent = true` summary), vagy törlés-újraírás tranzakcióban
   (chunkok), vagy „suggestion ID" kulcs (deadline, task).
3. **Stage-enkénti queue.** Minden lépés külön `AiProcessingJob` sor.
   Indok: ha egy lépés elbukik (pl. embedding generálás), a többi nem
   szakad meg; külön retry, külön backoff.
4. **Privacy-guard.** A `PrivacyMode = LocalOnly` esetén a pipeline
   garantáltan csak lokális provider (Ollama) felé hív. Cloud provider
   még explicit task-konfigurációval sem aktiválható.
5. **Magyar nyelv first.** A promptok magyar nyelvűek, a kimenetnek
   magyarul kell visszajönnie (kivéve nyelvdetektálás és technikai mezők).
6. **Költségminimalizálás.** Minden lépés bemenete a *legkisebb* szükséges
   tartalom (chunk, summary), nem a teljes dokumentum, ha elkerülhető.

---

## 2. Pipeline áttekintés

```
                                    ┌──────────────────────────┐
                                    │ Bemenet (3 forrás)       │
                                    │  - Feltöltés (PDF, kép)  │
                                    │  - Manuális jegyzet      │
                                    │  - Gmail üzenet          │
                                    └────────────┬─────────────┘
                                                 │
                                                 ▼
                          ┌──────────────────────────────────────┐
                          │ 1. Bemenet rögzítés (Document/Note/  │
                          │    EmailMessage) + Sha256 dedup      │
                          │    processing_status = Pending       │
                          │    AiProcessingJob: ExtractText      │
                          └────────────┬─────────────────────────┘
                                       ▼
                          ┌──────────────────────────────────────┐
                          │ 2. Szövegkinyerés                    │
                          │    PDF text layer → Tesseract OCR fb │
                          │    → DocumentText (tsv generated)    │
                          │    AiProcessingJob: DetectLanguage   │
                          └────────────┬─────────────────────────┘
                                       ▼
                          ┌──────────────────────────────────────┐
                          │ 3. Nyelvdetektálás                   │
                          │    Document.Language frissítés       │
                          │    AiProcessingJob: Classify, Summa- │
                          │    rize, ExtractDeadlines, Extract-  │
                          │    Tasks, Embed (mind Queued)        │
                          │    processing_status = Analyzing     │
                          └────────────┬─────────────────────────┘
                                       │
            ┌──────────┬───────────────┼───────────────┬─────────────┐
            ▼          ▼               ▼               ▼             ▼
       ┌────────┐ ┌─────────┐    ┌──────────┐    ┌──────────┐  ┌──────────┐
       │ 4.     │ │ 5.      │    │ 6.       │    │ 7.       │  │ 8.       │
       │Classify│ │Summary  │    │Deadline  │    │Task      │  │Embedding │
       │+ facet │ │(hu)     │    │extract   │    │extract   │  │(chunks)  │
       │detect  │ │         │    │          │    │          │  │          │
       └───┬────┘ └────┬────┘    └────┬─────┘    └────┬─────┘  └────┬─────┘
           │           │              │               │             │
           ▼           ▼              ▼               ▼             ▼
   Topic/Tag link  DocumentSummary  Deadline      Task         DocumentChunk
   (Origin=AiSug)  (IsCurrent=true) (Origin=      (Status=     (vector 768)
                                    AiSuggested)  Suggested,
                                                  Origin=
                                                  AiSuggested)
           │           │              │               │             │
           └───────────┴──────────────┴───────────────┴─────────────┘
                                       ▼
                          ┌──────────────────────────────────────┐
                          │ 9. processing_status = Done          │
                          │    Domain event: DocumentProcessed   │
                          │    → SignalR push az UI-nak          │
                          │    → user jóváhagy / elvet            │
                          └──────────────────────────────────────┘
```

**Sorrend megjegyzések:**
- A 4–8. lépések közül a 4. (osztályozás) befejezésére várhat a 6. és 7.
  (topic-érzékeny prompttal jobb deadline/task kinyerés). MVP-ben a négy
  AI lépés szekvenciálisan fut egymás után (egy worker = egy modell),
  mert az Ollama lokálisan szekvenciális, így a párhuzamosítás nem nyerő.
- Az 5. (summary) függetlenül futtatható; 8. (embedding) szintén — ezek
  bármikor előzhetik a többit, ha a queue így ütemezi.

---

## 3. Lépésenkénti specifikáció

### 3.1 Bemenet rögzítés (job nélkül, szinkron API hívás)

- **Trigger:** REST endpoint (`POST /api/v1/documents`, `POST /api/v1/notes`)
  vagy `EmailIngestionPoller`.
- **Műveletek:**
  - Sha256 dedup ellenőrzés (`document.sha256` UNIQUE) — duplikátum esetén
    a meglévő rekord URL-jét adjuk vissza, új feldolgozás nélkül.
  - Fájltár-mentés (`IDocumentStorage.SaveAsync`).
  - `Document` insert (`processing_status = Pending`, `origin = Manual`
    vagy `ImportedFile`/`ImportedEmail`).
  - Egyetlen `AiProcessingJob` (type = `ExtractText`) létrehozása ugyanabban
    a tranzakcióban.
- **Sikermérce:** 201 + Document DTO < 500 ms (50 MB-os fájlra is, mert
  ez I/O dominált, nem AI).

### 3.2 Szövegkinyerés (`JobType = ExtractText`)

- **Cél:** Egy nyers `DocumentText.Content` előállítása. Output: `DocumentText`
  rekord vagy `failed`.
- **Algoritmus:**
  1. MIME alapján megnézi: PDF → próbáljon szöveg-réteget kinyerni (`PdfPig`).
     Ha a kinyert text > 100 char és > 80% nyomtatható, OK.
  2. Egyébként **Tesseract OCR** (`hun + eng` nyelvi csomag, `--oem 3 --psm 6`).
  3. Kép → közvetlenül Tesseract.
  4. TXT / DOCX → natív parser (DOCX-hez `DocumentFormat.OpenXml`).
  5. EmailMessage body → már elérhető a `EmailMessage.BodyText`-ben; csak
     copy.
- **Eredmény tárolás:**
  - `DocumentText` upsert (1:1 `document_id` UNIQUE).
  - `extraction_method` enum kitöltése (`PdfTextLayer`, `TesseractOcr`,
    `EmailBody`, `ManualPaste`).
  - `ocr_confidence` átlag, ha OCR futott.
  - `char_count` cache.
- **Következő lépés:** új `AiProcessingJob` (`DetectLanguage`).
- **Hibakezelés:** ha az OCR is üres outputot ad → `DocumentText.Content = ''`,
  `Document.processing_status = Failed`. **NEM** próbáljuk újra automatikusan —
  csak emberi újrafeltöltéssel (rosszabb minőségű scan jellemzően OCR-rel sem
  megy magasabb retry-jal).

### 3.3 Nyelvdetektálás (`JobType = DetectLanguage`)

- **Cél:** `Document.Language` kitöltése.
- **Algoritmus:** Lokális, NEM AI provider hívás. `NTextCat` vagy
  `LanguageDetection` csomag, a `DocumentText.Content` első 2000 karakteréből.
- **Indok a külön lépésre:** olcsó, gyors, de nem kell minden írásnál újra
  futtatni; külön job-ként jól mérhető és retry-olható.
- **Eredmény:** `Document.Language` (`hu`, `en`, …); ha eltér a feltöltéskor
  manuálisan megadottól, marad a manuális. `DocumentText.LanguageDetected`
  külön mező a tényleges detekcióhoz (audit célra).
- **Következő:** egyszerre 5 új `AiProcessingJob` enqueue:
  `Classify`, `Summarize`, `ExtractDeadlines`, `ExtractTasks`, `Embed`.
  `Document.processing_status = Analyzing`.

### 3.4 Osztályozás (`JobType = Classify`)

- **Cél:** Topic(ok) és Tag(ek) javaslat + facet típus felismerés (ez egy
  Warranty / MedicalRecord / FinancialRecord-e?).
- **Bemenet:**
  - A `DocumentText.Content` első 4000 karaktere (vagy a teljes, ha rövidebb).
  - A topic-taxonómia (`Topic` tábla — lapos lista a slug-okkal).
- **Prompt template (magyar):** lásd 4.1.
- **Kimenet (várt JSON):**
  ```json
  {
    "topics": ["jarmu/kotelezo", "penzugy/biztositas"],
    "tags":   ["AXA", "2026"],
    "facet":  "Financial",     // "Warranty" | "Medical" | "Financial" | null
    "confidence": 0.84
  }
  ```
- **Adatbázis-művelet:**
  - Topic-slug feloldás → ha létezik, `DocumentTopic` insert
    (`origin = AiSuggested`); ha nem létezik, **nem hozunk létre** új Topic-ot
    automatikusan — csak az ismert taxonómiához igazítunk.
  - Tag insert idempotens (`name lower(.)` UNIQUE) — új Tag-et **létrehozhatunk**
    (lapos, olcsó), `usage_count` növelése.
  - Facet típus → ennek alapján az 5. lépés után létrejön a megfelelő
    facet entitás (Warranty / MedicalRecord / FinancialRecord). A facet
    mezőit (`product_name`, `purchase_date`, `vendor`, `amount`, …) az
    entitás-kinyerés tölti — lásd a megfelelő lépéseket.

### 3.5 Összefoglaló (`JobType = Summarize`)

- **Cél:** 3–5 magyar mondat tömör összefoglaló a dokumentumról.
- **Bemenet:** `DocumentText.Content` (max 16 000 char-ig; ha hosszabb,
  map-reduce: chunkonként mini-összefoglaló → meta-összefoglaló).
- **Prompt template:** 4.2.
- **Kimenet:** plain szöveg.
- **Tárolás:** `DocumentSummary` insert (`is_current = true`); a korábbi
  `is_current` átállít `false`-ra ugyanabban a tranzakcióban (partial UNIQUE
  index miatt sorrend kötelező).
- **Megj.:** Ez a leggyakrabban újrafuttatott lépés (új prompt-verzió / új
  modell esetén). A `prompt_version` mező teszi átláthatóvá.

### 3.6 Határidő-kinyerés (`JobType = ExtractDeadlines`)

- **Cél:** Strukturált `DeadlineSuggestion` lista a dokumentum szövegéből.
- **Bemenet:**
  - `DocumentText.Content` első ~8000 char (a határidők jellemzően az elején
    vagy a végén vannak — itt a teljes egészét adjuk MVP-ben, költség viseli).
  - A mai dátum (`IClock.Today`), hogy a relatív dátumok („30 napon belül")
    értelmezhetők legyenek.
- **Prompt template:** 4.3.
- **Kimenet (várt JSON tömb):**
  ```json
  [
    {
      "title": "Autó kötelező biztosítás lejár",
      "dueDate": "2026-09-14",
      "category": "Insurance",
      "confidence": 0.92,
      "evidenceQuote": "Kötvény lejárati dátuma: 2026.09.14."
    }
  ]
  ```
- **Adatbázis-művelet:** minden bejegyzés egy `Deadline` insert
  (`status = Upcoming`, `origin = AiSuggested`, `source_document_id` mutatva
  a dokumentumra). Mindegyikhez egy default Reminder javaslat is születik
  (lásd 3.10).
- **Validáció:** `dueDate` >= ma; `category` ismert enum érték; ha
  confidence < 0.6, akkor mellőzzük (zaj-szűrés).
- **De-duplikáció:** ugyanazon `(source_document_id, title, due_date_utc)`
  kombinációra nem hozunk létre újat (idempotencia újrafutásra).

### 3.7 Feladat-kinyerés (`JobType = ExtractTasks`)

- **Cél:** `TaskSuggestion` lista — tennivalók, amik a dokumentumból
  következnek (nem azonosak a határidőkkel).
- **Bemenet:**
  - `DocumentText.Content` (mint a 3.6).
  - A háztartás `FamilyMember`-ek listája (név + relation), hogy a modell
    javasolhasson felelőst.
- **Prompt template:** 4.4.
- **Kimenet (várt JSON tömb):**
  ```json
  [
    {
      "title":  "Kötelező biztosítás megújítása",
      "description": "Ajánlatot kérni 30 napon belül több biztosítótól.",
      "dueDate": "2026-08-15",
      "assignedToHint": "Apa",
      "priority": "Normal",
      "confidence": 0.78
    }
  ]
  ```
- **Adatbázis-művelet:** `Task` insert, `status = Suggested`,
  `origin = AiSuggested`. Az `assignedToHint` alapján próbáljuk
  feloldani egy konkrét `FamilyMember`-re (egyezés `display_name` vagy
  `relation` alapján); ha bizonytalan, `assigned_to_family_member_id = null`.
- **Megj.:** A `Task` és `Deadline` overlapping. A pipeline megengedi,
  hogy ugyanaz az esemény mindkettőben szerepeljen — a felhasználó
  döntheti el, melyiket fogadja el (vagy mindkettőt). De-duplikáció:
  ha egy `Task` és egy `Deadline` ugyanarra a dokumentumra utal, és a
  cím + dátum 85%+ levenshtein hasonlóságú, az UI vizuálisan összevonja.

### 3.8 Entitás-kinyerés a facet-hez (sub-step a 3.6/3.7 mellett)

A 3.4 osztályozó által visszaadott `facet` érték alapján egy extra lépés fut:

- **Ha `facet = Warranty`:** `JobType = ExtractEntities` egy Warranty-specifikus
  prompttal, kimeneti JSON-ja: `product_name`, `brand`, `model`,
  `serial_number`, `purchase_date`, `purchase_price`, `currency`,
  `warranty_months`, `warranty_end_date`, `seller`. → `Warranty` insert (1:1
  a Document-re).
- **Ha `facet = Medical`:** `record_type` (LabResult/Prescription/…),
  `record_date`, `provider`, `title`. Kötelező a `family_member_id` — ha az
  AI nem tudja eldönteni, akkor a facet-rekord nem jön létre automatikusan,
  hanem egy *megerősítendő* javaslat-blokk jelenik meg a UI-on.
- **Ha `facet = Financial`:** `record_type`, `vendor`, `amount`, `currency`,
  `issue_date`, `due_date`, `recurrence_period`. → `FinancialRecord` insert.

A facet-entitás-kinyerés MVP-ben **egyetlen** AI hívás dokumentumonként;
a prompt template a facet típusa alapján kerül kiválasztásra.

### 3.9 Embedding generálás (`JobType = Embed`)

- **Cél:** A dokumentum/jegyzet szövegét chunkolni, és minden chunk-ra
  embeddinget generálni → `DocumentChunk` / `NoteChunk`.
- **Algoritmus:**
  1. A `DocumentText.Content`-et szakaszokra bontjuk (preferáltan
     bekezdés-határok mentén, max 800 token / chunk, ~100 token overlap-pel).
  2. Tokenizáció: heurisztika — átlagos magyar szöveg ~1.4 token / szó.
     Pontos count csak ha a modell adja vissza (Ollama nem mindig).
  3. `IEmbedder.EmbedAsync(batch)` — alapmodell `nomic-embed-text:v1.5`
     (768-dim, magyar elfogadható).
  4. Insert `DocumentChunk` batch (ON CONFLICT — `(document_id, chunk_index)`
     UNIQUE → upsert).
- **Idempotencia újrafuttatáskor:** ha az `embedding_model` változatlan,
  a `chunk_index`-en upsert; ha modellt cserélünk, törlünk minden chunkot
  a régi modellnévre és újra generáljuk.
- **Hibakezelés:** modell timeout / hosszú text → exponenciális backoff.
  Ha 5 attempt után sem sikerül: `Document.processing_status = Failed`,
  de a Summary/Deadline/Task lépések eredménye **megmarad** (külön job).

### 3.10 Default Reminder javaslat

A 3.6 lépés minden elfogadott Deadline-hoz default Reminder javaslatot
generál a `DeadlineCategory` szerinti policy alapján:

| Category | Default reminder offset-ek | Csatorna |
|---|---|---|
| Insurance | 30 nap, 7 nap, 1 nap előtt | InApp + Email |
| Invoice | 7 nap, 1 nap előtt | InApp |
| Inspection | 30 nap, 7 nap előtt | InApp + Email |
| School | 7 nap, 1 nap előtt | InApp |
| Medical | 14 nap, 2 nap előtt | InApp |
| Subscription | 14 nap, 3 nap előtt | InApp |
| Personal | 1 nap előtt | InApp |
| Other | 7 nap előtt | InApp |

Ezek `Reminder` rekordok `origin = AiSuggested`, `status = Scheduled`,
de **csak akkor tüzelnek**, ha a kapcsolódó Deadline-t a felhasználó
jóváhagyta (`approved_utc IS NOT NULL`). Részletek a `reminder-engine.md`-ben.

---

## 4. Prompt template-ek

Minden prompt egy közös rendszer-prompt prefixet kap, amely magyar nyelvű
JSON kimenetet kér és tiltja a kitalálást.

### 4.0 Közös rendszer-prompt (sysprefix)

```
Te egy magyar nyelvű családi információkezelő rendszer feldolgozó modulja vagy.
A feladatod mindig egy konkrét feladatra korlátozódik, amelyet alább kapsz.

Általános szabályok:
- A válaszodnak SZIGORÚAN érvényes JSON formátumúnak kell lennie a kért
  sémában. Semmi extra szöveg, semmi magyarázat, semmi markdown blokk.
- Ha az információ nem áll rendelkezésre, használj null-t. Soha ne találj ki adatot.
- A dátumokat ISO 8601 formátumban add vissza (YYYY-MM-DD).
- A nyelv: magyar, ahol szöveges output van.
- Ha a forrásszöveg nem magyar nyelvű, a kimenetnek akkor is magyarul kell lennie
  (kivéve a tulajdonneveket, márkákat, számokat).
- Minden kinyert tényhez add meg a "confidence" mezőt 0.0-1.0 között.
```

### 4.1 Osztályozó prompt

```
Feladat: a dokumentum besorolása témakörökbe és típusokba.

Engedélyezett témakörök (slug-okkal):
{topic_taxonomy_json}

Lehetséges facet típusok: "Warranty", "Medical", "Financial", null.

Add vissza:
{
  "topics": [string],          // legfeljebb 3, csak a fenti slug-okból
  "tags":   [string],          // 0..5 szabad-szöveges címke
  "facet":  string | null,
  "confidence": number
}

Dokumentum szövege:
"""
{text}
"""
```

### 4.2 Összefoglaló prompt

```
Feladat: a dokumentum tartalmának 3-5 mondatos, magyar nyelvű, tényszerű
összefoglalása.

Szabályok:
- Csak a dokumentumban szereplő tények.
- Nincs vélemény, nincs következtetés.
- Kerüld a "valószínűleg", "valószínű" megfogalmazásokat.

Válasz formátum:
{
  "summary": string
}

Dokumentum szövege:
"""
{text}
"""
```

### 4.3 Határidő-kinyerő prompt

```
Feladat: a dokumentumban szereplő összes idő-kötött határidő, lejárati dátum,
megújulási dátum kinyerése.

Mai dátum: {today_iso}

Add vissza JSON tömb formájában (üres tömb, ha nincs):
[
  {
    "title": string,                       // rövid, magyar
    "dueDate": "YYYY-MM-DD",
    "category": "Insurance" | "Invoice" | "Inspection" | "School" |
                "Medical" | "Subscription" | "Personal" | "Other",
    "confidence": number,                  // 0.0-1.0
    "evidenceQuote": string                // a forrásszöveg pontos idézete
  }
]

Csak olyan határidőket vegyél fel, amelyek explicit dátumot tartalmaznak,
vagy a mai dátumhoz képest egyértelműen kiszámolhatók (pl. "30 napon belül").

Dokumentum szövege:
"""
{text}
"""
```

### 4.4 Feladat-kinyerő prompt

```
Feladat: javasolt teendők a dokumentum alapján.

A háztartás tagjai (név | szerep):
{family_members_list}

Add vissza JSON tömb formájában (üres tömb, ha nincs egyértelmű teendő):
[
  {
    "title":   string,                    // imperatívuszban, max 80 char
    "description": string,                // 1-2 mondat, opcionális
    "dueDate": "YYYY-MM-DD" | null,
    "assignedToHint": string | null,      // egy név vagy szerep a fenti listából
    "priority": "Low" | "Normal" | "High",
    "confidence": number
  }
]

Csak akkor javasolj feladatot, ha a dokumentum konkrét emberi cselekvést kíván
(megrendelés, megújítás, ügyintézés). A puszta információ-jellegű tartalom
NEM teendő.

Dokumentum szövege:
"""
{text}
"""
```

### 4.5 Facet-entitás prompt (példa: Warranty)

```
Feladat: jótállási / garancia adatok kinyerése a dokumentumból.

Add vissza:
{
  "productName": string,
  "brand": string | null,
  "model": string | null,
  "serialNumber": string | null,
  "purchaseDate": "YYYY-MM-DD" | null,
  "purchasePrice": number | null,
  "currency": "HUF" | "EUR" | "USD" | null,
  "warrantyMonths": number | null,
  "warrantyEndDate": "YYYY-MM-DD" | null,
  "seller": string | null,
  "confidence": number
}

Dokumentum szövege:
"""
{text}
"""
```

### 4.6 Q&A prompt (kérdés-válasz, használati felület)

```
Te egy magyar nyelvű családi tudásbázis asszisztense vagy. Válaszolj a
felhasználó kérdésére KIZÁRÓLAG a megadott idézett források alapján.

Szabályok:
- Ha a források nem tartalmazzák a választ, mondd ki egyértelműen, hogy
  "Nincs erre vonatkozó adat a rögzített dokumentumok között."
- Soha ne találj ki tényt.
- Minden ténynél hivatkozz vissza a forrásra a "[forrás: <doc_id>]" jelöléssel.
- Légy tömör (max 3 mondat), kivéve, ha a kérdés részletes kifejtést kér.

Idézett források:
{retrieved_chunks_json}

Kérdés:
{question}
```

---

## 5. Javaslat → jóváhagyás állapotgép

Minden AI-eredeti entitásra (Task, Deadline, DocumentTopic, DocumentTag,
Warranty / Medical / Financial facet, Reminder) az alábbi állapotok
értelmezettek:

```
            ┌─────────────────────┐
            │  AiSuggested        │   alap állapot, NEM aktív
            └─────────┬───────────┘
            user     │ user
            "Reject" │ "Approve"        ┌──────────────┐
            ┌────────┴────────┐         │ AiApproved   │ aktív,
            │ (törlés vagy    │         │ (Origin=     │ tüzelnek
            │  IsDismissed)   │         │  AiApproved) │ reminderek
            └─────────────────┘         └──────────────┘
```

- A **Task**-nál: a `Status` átáll `Suggested → Open`-re a jóváhagyás-akcióban
  *és* az `Origin` átáll `AiSuggested → AiApproved`-ra ugyanabban a
  tranzakcióban; `approved_by_user_account_id` + `approved_utc` kitöltve.
- A **Deadline**-nál: `Status` marad `Upcoming` (vagy `Due`/`Passed`
  számolódva), de az `Origin = AiApproved` lesz; csak ettől kezdve generálja
  a Reminder Dispatcher a kapcsolódó emlékeztetőket.
- A **DocumentTag/Topic** join-rekordnál: jóváhagyás = az `Origin` átírása.
- A **facet** (Warranty/Medical/Financial) entitásnál: a soft-delete-tel
  törli, ha elveti; a jóváhagyás = `Origin = AiApproved`.

A jóváhagyási UI batch-műveletként is működik: egy dokumentumra az összes
javaslat egyetlen kattintással elfogadható (lásd `frontend-structure.md`).

---

## 6. Hibakezelés, retry, idempotencia

### 6.1 Retry mátrix

| Lépés | Max attempt | Backoff | Megj. |
|---|---|---|---|
| ExtractText (text layer) | 1 | – | determinisztikus, nincs retry |
| ExtractText (OCR) | 2 | 30s | csak ha futási hiba (segfault), nem üres output |
| DetectLanguage | 3 | 10s, 30s | |
| Classify | 5 | 60s × 2^attempt, max 6h | LLM hívás, hálózati hiba |
| Summarize | 5 | 60s × 2^attempt, max 6h | |
| ExtractDeadlines | 5 | 60s × 2^attempt, max 6h | |
| ExtractTasks | 5 | 60s × 2^attempt, max 6h | |
| Embed | 5 | 60s × 2^attempt, max 6h | hosszú szöveg → chunk-szintű retry |

### 6.2 Idempotencia kulcsok

- **DocumentSummary:** partial UNIQUE `(document_id) WHERE is_current = true`
  → tranzakció: régi `is_current = false`, új insert `is_current = true`.
- **DocumentChunk:** `(document_id, chunk_index)` UNIQUE → upsert.
- **Deadline / Task suggestion:** ha ugyanaz a `(source_document_id, title,
  due_date)` tripla már létezik nem-Dismissed státuszban, skip insert.
- **Topic/Tag link:** `(document_id, topic_id)` / `(document_id, tag_id)`
  PRIMARY KEY → idempotens insert ON CONFLICT DO NOTHING.

### 6.3 JSON parse-hibák

Az LLM kimenet validációja Newtonsoft/STJ + sémavalidátor (JsonSchema). Ha
a kimenet nem érvényes:
- 1. retry: ugyanaz a prompt, de a sysprefixhez hozzáfűzve „Az előző válasz
  nem érvényes JSON volt: <röviden mi a hiba>. Pontosan a kért séma szerint
  válaszolj."
- 2. retry után: `Failed` státusz, error log, **nem** szakítja meg a többi
  lépést.

### 6.4 PrivacyMode kapu

Ha futás közben a config átáll `LocalOnly`-ra, a futó cloud-job
befejezi a jelenlegi hívást (nem szakítjuk meg adatszivárgási kockázat
nélkül), de a *következő* lépés Ollama provider-rel megy.
**Új** dokumentum cloud provider-t soha nem fog hívni `LocalOnly` alatt.

---

## 7. Offline / catch-up viselkedés

### 7.1 PC kikapcsolt → bekapcsolt

- Új feltöltés a PC-n nem történik (LAN-only, ADR-0003).
- A `EmailIngestionPoller` indulás után azonnal lefut egy „catch-up sync"-et
  a Gmail-en (a `last_sync_utc` óta érkezett üzenetek).
- Az `ai_processing_job` régi `Queued` és `Failed AND next_attempt_utc <=
  now()` rekordok feldolgozása megkezdődik, prioritás szerint.
- A `DueReminderDispatcher` az 1.3 katch-up módban a kihagyott
  emlékeztetőket egyszerre küldi ki (batch-elve, hogy az UI ne robbanjon).

### 7.2 Ollama nem elérhető

- Az AI lépések `Failed`-be esnek `error_message = 'AI provider unavailable'`-lal.
- Recurring health-check (`/healthz/ready`-ben): 5 percenként próbálkozik
  reset-elni a `next_attempt_utc`-t a friss attempt-re.
- Nincs cloud-fallback `LocalOnly` esetén.

### 7.3 Hibrid mobil → lokális PC (jövőbeli, nem MVP)

Az `idea.md` brief eredetileg felvetette, hogy a telefon detektálja a hazai
hálózatot és az AI feladatokat „delegálja" a PC-nek. Ez **az ADR-0003 LAN-only
döntéssel egybevágva** azt jelenti: a mobil app *eleve* csak otthon kommunikál
a backenddel — nincs külön „remote delegation" mechanizmus szükséges.
A mobil offline tartalmat (pl. lefotózott számla) feltölti, amint a telefon
visszacsatlakozik a háztartási Wi-Fi-re. A „stacked tasks" fogalmát a
durable AI job queue valósítja meg — semmilyen kliens-oldali sorba állítás
nem kell.

---

## 8. Provider képességmátrix

| Képesség | Ollama (`gpt-oss:20b`) | Anthropic Claude | OpenAI |
|---|---|---|---|
| Magyar nyelvű kimenet | jó | kiváló | jó |
| Strict JSON mode | promptengineering | natív | natív |
| Tool use | nincs (prompt-engineering) | natív | natív |
| Embedding modell | igen (`nomic-embed-text`) | nincs külön | `text-embedding-3-*` |
| Költség | 0 (lokális) | token-alapú | token-alapú |
| Latency (LAN, 4000 token in) | 5-15 s | 1-3 s | 1-3 s |
| Privacy-compliant `LocalOnly`-ben | igen | nem | nem |

A pipeline-nak ezt a mátrixot az `IAiProvider.Capabilities` flag-ekkel
látja: amennyiben a kiválasztott provider nem támogat „strict JSON mode"-ot,
az alkalmazás extra parse + retry réteget használ.

---

## 9. Tesztelési stratégia

### 9.1 Golden samples

A `tests/Goldens/` mappában 15-20 magyar nyelvű mintadokumentum (PDF, scan,
email-export), elvárt kimenettel (summary, deadlines, tasks, facet adatok)
JSON-ban. A pipeline regressziós teszt minden lépést rájuk futtat egy
**stub provider**-rel, ami determinisztikus választ ad — a determinisztikus
viselkedés (sémakonformitás, idempotencia, dedup) tesztelhető.

### 9.2 End-to-end smoke

Egy `@e2e-pipeline` címkés Playwright + xUnit kombóteszt feltölt egy
mintadokumentumot, vár a `Document.processing_status = Done`-ra, és
ellenőrzi a generált entitásokat (DocumentSummary, DocumentChunk count,
Deadline + Task suggestion). Lokális Ollama-val fut, csak nightly CI-ben.

### 9.3 Promptregresszió

A `Prompts/` mappa minden prompt template-ének van verziója
(`prompt_version`). Új verzió bevezetésekor mind a 15-20 golden sample-ön
újra fut a pipeline, és emberi reviewer validálja a kimenet minőségét
egy diff-tooling-gal.

---

## 10. Korlátok és későbbi bővítések

- **Long context** — MVP-ben 16 000 char dokumentum-limit egy prompton.
  Hosszabb szövegnél map-reduce. Ennek pontosítása későbbi mérési feladat.
- **Multi-modal** — kép → text-aware OCR-en túl nem értelmezünk
  fotótartalmat (pl. autó kárfotók). Későbbi képi modell-bővítés.
- **Streaming response a Q&A-ban** — MVP-ben szinkron, batch-válasz.
  Streaming SSE-vel későbbi enhancement.
- **Fine-tuning** — nem cél; minden javítás prompt-engineering-gel.
- **Aktív AI-akciók** — pl. „Az AI maga indítsa el a renewal folyamatot
  a biztosítónál" — szándékosan **kizárva**, agent autonómia nem cél
  családi adatokon.

# Review — ai-pipeline.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Kiforrott pipeline-terv: lépésenkénti spec bemenettel/kimenettel/DB-művelettel,
konkrét magyar prompt template-ek, retry-mátrix, idempotencia-kulcsok,
golden-sample tesztstratégia. A „AI nem aktivál” elv következetesen érvényesül.
A hibák főleg a lépések összehangolásában és a szomszédos doksikkal való
összhangban vannak.

## Hibák / következetlenségek

### 1. Ki állítja be a `processing_status = Done`-t? (súlyos)
A 2. ábra 9. lépése szerint a státusz `Done` lesz és SignalR esemény megy ki —
de az 5 párhuzamos/szekvenciális job (Classify, Summarize, ExtractDeadlines,
ExtractTasks, Embed) **külön** `AiProcessingJob` sorok, külön retry-jal.
Sehol nincs leírva a befejezés-koordináció: melyik komponens észleli, hogy
mind az öt lezárult (és mi van, ha egy `Failed`, négy `Completed` — lásd
3.9, ahol Embed-hiba `Failed`-re állítja a dokumentumot a többi sikeres
eredmény mellett)? Kell egy explicit szabály (pl. minden job-befejezés után
aggregát-check; Done = mind Completed; PartialFailed állapot bevezetése
vagy dokumentált döntés).

### 2. `ExtractEntities` (facet) lépés hiányzik az ábrából és az ütemezésből (közepes)
A 3.8 leírja a facet-kinyerést (`JobType = ExtractEntities`), de a 2. ábra
4–8. lépései között nem szerepel, és a 3.3 „egyszerre 5 job” felsorolása
sem tartalmazza. Mikor enqueue-zódik (feltehetően a Classify eredménye
után, feltételesen)? Ezt a láncolást explicit le kell írni, különben az
implementáló agent kihagyja.

### 3. Reminder csatorna: „InApp + Email” vs. egyértékű enum (közepes)
A 3.10 táblázat csatornaként „InApp + Email”-t ad több kategóriára, de a
`reminder.channel` a sémában **egyetlen** enum érték (`InApp` | `Email`).
Két csatorna = két Reminder sor? Vagy flags-enum? A reminder-engine.md 6.1
is „InApp + opcionálisan Email”-t ír. Döntés + séma-igazítás kell
(ugyanez a hiba a reminder-engine.review.md-ben is jelezve).

### 4. Topic-slug formátum: útvonal vs. slug (közepes)
A 3.4 példa-kimenet és a 4.1 prompt `"jarmu/kotelezo"` formátumú
topic-hivatkozást használ — a séma `slug` mezője viszont `^[a-z0-9-]+$`
(per-slug, `/` nélkül), az egyediség slug-szintű. Definiálni kell, hogy a
prompt-taxonómiában path szerepel (parent/child), és a feloldás hogyan
történik; jelenleg a classifier kimenete nem illeszkedik a sémára.

### 5. Architektúra-flow eltérés: DetectLanguage kimarad (kicsi)
Az architecture.md 11.1 flow-jában az ExtractText után közvetlenül az 5
elemző job jön létre — itt (2. ábra, 3.2/3.3) közéjük ékelődik a
`DetectLanguage` lépés, és az 5 jobot az hozza létre. A két doksi
szinkronizálandó (az itteni a részletesebb, valószínűleg ez a helyes).

### 6. Kisebb észrevételek
- 3.6: `dueDate >= ma` validáció eldobja a múltbeli dátumokat — régi
  dokumentumok utólagos feltöltésénél (pl. tavalyi számla) ez adatvesztés
  a facet-számára; a domain-model „kivéve importnál” kitételével
  egyeztetendő.
- 3.7: a Task↔Deadline 85% levenshtein-összevonás „UI vizuálisan összevonja”
  — hol fut a hasonlóság-számítás (backend? frontend?) és mi a küszöb
  pontos definíciója? Implementációs döntés hiányzik.
- 4.2 prompt: JSON `{"summary": string}` formátumot kér, a 3.5 „Kimenet:
  plain szöveg”-et mond — a kettő ellentmond (a JSON a helyes a 6.3
  parse-logika miatt).
- 6.4 PrivacyMode-átállásnál „a futó cloud-job befejezi a jelenlegi hívást
  (nem szakítjuk meg adatszivárgási kockázat nélkül)” — a mondat kétértelmű;
  fogalmazandó: a már elküldött prompt nem visszahívható, ezért hagyjuk
  befejeződni.
- 7.2: az Ollama-health-check a `/healthz/ready`-hez kötése keveri a
  readiness probe-ot a job-újraütemezéssel — a kettő legyen külön
  mechanizmus (lásd architecture.review.md #2).
- 8. képességmátrix: `gpt-oss:20b` „Tool use: nincs” — a gpt-oss modellek
  támogatnak tool use-t Ollamán; az állítás ellenőrizendő az aktuális
  verzióra (a terv nem épít rá, így alacsony kockázat).

## Erősségek (megőrzendő)

- Prompt template-ek verziózása (`prompt_version`) + golden-sample
  regresszió (9.1, 9.3) — ez ritkán van ilyen jól előre megtervezve.
- Retry-mátrix lépés-típusonként differenciálva (6.1).
- A 7.3 szakasz jól oldja fel a brief „mobil delegálja az AI-t” ötletét
  a durable queue-val — fölösleges komplexitás elkerülve.

## Verdikt

Implementálható spec; az #1 (Done-koordináció) és #2 (ExtractEntities
láncolás) kötelezően tisztázandó a BUILD előtt, a #3–#4 séma-egyeztetés.

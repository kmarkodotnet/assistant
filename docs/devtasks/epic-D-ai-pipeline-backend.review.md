# Review — epic-D-ai-pipeline-backend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

A legnagyobb és legjobban szervezett epic-fájl: D-Infra / 3 párhuzamos
worktree / orchestráció bontás a kontextus-mátrixszal összhangban,
privacy-assertion és golden-sample tesztekkel. Két tervezési kérdést
itt zártak le jól (PrivacyMode kapu: T-DBE-05; Done-koordináció:
T-DBE-24) — de az utóbbi definíciója még pontatlan, és egy technikai
hiba van a SignalR-tervben.

## Hibák / észrevételek

### 1. T-DBE-26: a Workers nem publikálhat `IHubContext`-tel (közepes—súlyos)
Az AC szerint „a Workers szerver-szerveren `IHubContext`-szel publikál” —
az `IHubContext` csak **ugyanabban a processzben** működik, amelyik a
SignalR hubot hosztolja (ez az Api). A Workers külön process
(architecture.md 2.), tehát vagy (a) Redis backplane kell, vagy (b) a
Workers egy belső HTTP/gRPC hívással szól az Api-nak, vagy (c) a
Workers Postgres NOTIFY/polling útján jelez. Egy gépen a (b) a
legegyszerűbb. E nélkül a D11 real-time frissítés nem fog működni —
architect-döntés kell a BUILD előtt (architecture.review.md #6 ugyanez
a kérdés, itt vált konkrét hibává).

### 2. T-DBE-24: a részleges hiba állapota definiálatlan (közepes)
„Mind az 5 lefutott → Done; ha bármely lépés Failed, a többi folytatódik;
csak ha mind Failed, a Document Failed.” — mi az állapot 4 sikeres + 1
végleg Failed jobnál? A szöveg szerint Done (ami elfedi a hibát) — az
ai-pipeline.md 3.9 szerint viszont Embed-hiba után a Document `Failed`
(ami elfedi a 4 sikert). A két doksi ellentmond; javasolt egy explicit
szabály (pl. Done + `has_failed_steps` jelző, vagy `PartialFailed`
státusz) és mindkét doksi igazítása.

### 3. A facet-kinyerés (ExtractEntities) láncolása itt is hiányzik (közepes)
A T-DBE-18..20 megvalósítja a három facet-extractort, de a T-DBE-24
orchestrator AC-je csak az 5 alap jobot enqueue-olja — sehol nincs
kimondva, hogy a Classify facet-eredménye nyomán ki és mikor indítja az
`ExtractEntities` jobot (ai-pipeline.review.md #2 ugyanez). Az
orchestrator AC-jébe fel kell venni: „Classify Completed és facet != null
→ ExtractEntities enqueue”.

### 4. T-DBE-05 — jó lezárás, doksi-szinkron kell (pozitív)
A „Hybrid/AnyProvider típusban létezik, de `NotImplementedException`
MVP-ben” pontosan feloldja a security-privacy vs. api-design ellentmondást
(security-privacy.review.md #2) — a security-privacy.md 8.2–8.3 ehhez
igazítandó.

### 5. Kisebb észrevételek
- T-DBE-16 (deadline-kinyerés) után a **default Reminder-javaslatok**
  generálása (ai-pipeline.md 3.10 policy-tábla) egyik taskban sem
  szerepel — se itt, se az Epic G-ben nem láttam eddig explicit taskot;
  ellenőrizendő, hova tartozik (valószínű: a Deadline-jóváhagyás
  flow-jába, Epic F/G határán). Gazdát kell neki jelölni.
- T-DBE-03: `POST /api/chat` — az Ollama embedding-hez `/api/embed`
  kell (T-DBE-04-ben nincs kiírva az útvonal); apróság.
- T-DBE-28: 15 golden sample elkészítése önmagában jelentős munka
  (valós magyar mintadokumentumok kellenek) — a task nem jelzi, honnan
  származnak (szintetikus generálás?); becslés/felelős hiányzik.

## Verdikt

A bontás kiváló, de az #1 (SignalR cross-process) és #3 (facet-láncolás)
javítása nélkül az implementáció elakad vagy hibás lesz — mindkettő
architect-szintű pontosítás, a BUILD előtt kötelező.

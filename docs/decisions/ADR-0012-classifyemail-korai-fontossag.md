# ADR-0012 — ClassifyEmail: korai e-mail-fontosság, konkurencia és elhatárolás a Classify-tól

- Státusz: Elfogadva
- Dátum: 2026-07-11
- Döntéshozó: architect agent (CR260710-03 kontrakt-tervezés)
- Kapcsolódó: [CR260710-03](../change-requests/cr260710-03-fontos-emailek-felismerese.md),
  [ADR-0008](ADR-0008-workers-realtime-jelzes.md),
  [classify-email-contract.md](../contracts/classify-email-contract.md)

## Kontextus

A CR260710-03 egy `Document`-létrehozás *előtti*, e-mail-specifikus
fontosság-felismerést kér: minden beérkező Gmail-üzenet kapjon fontossági szintet
(`High`/`Medium`/`Low`) + kategóriát, és `High` esetén azonnal jöjjön létre egy
`notification_feed` bejegyzés — a lassabb, teljes dokumentum-pipeline megvárása
nélkül.

A jelenlegi pipeline: `GmailIngestionService.SyncAsync` minden új `EmailMessage`-re
felsorakoztat egy `ExtractText` jobot, ami `RunForEmailAsync`-ban `Document`-té
konvertálja az e-mailt, majd a Document végigmegy a normál AI-pipeline-on
(`Classify`/`Summarize`/`ExtractDeadlines`/stb.). A meglévő `Classify` job az
e-mail-eredetű Document-et **soft-delete-eli**, ha egyetlen ismert topicot sem talál
("noise"-kezelés).

Négy nem triviális kérdés merült fel, amit itt rögzítünk.

## Döntés

### 1. Új `ClassifyEmail` job az `EmailMessage`-en, migráció-mentes enum-bővítéssel

Új `AiJobType.ClassifyEmail` job, amit a `GmailIngestionService` az `ExtractText`
job **mellé** sorakoztat fel (azonos `EmailMessage.Id`, más `JobType`). A két job
egymástól függetlenül, párhuzamosan fut. A `ClassifyEmail` az `EmailMessage`
`subject`/`body_text`-jét osztályozza (új `IEmailImportanceClassifier`), és az
eredményt 3 új `email_message` oszlopba írja.

Az `AiJobType` EF-mappingje string-alapú (`HasConversion<string>`), nincs natív
Postgres enum és nincs DB check-constraint, ezért az új enum-érték **nem igényel
migrációt**. Az egyetlen séma-változás a 3 új `email_message` oszlop.

Indoklás: a meglévő e-mail-ingest belépési pont (`GmailIngestionService`) minimális
bővítéssel egészíthető ki; a fontosság-jelzés így a Document létrejötte előtt/mellett,
gyorsan elkészül (CR "a teljes pipeline lefutása előtt" kritériuma).

### 2. `High` értesítés címzettje: a háztartás legrégebbi aktív `Admin`-ja (nem fan-out)

A `High` fontosságú e-mail `notification_feed` bejegyzését a háztartás **legrégebbi
aktív `Admin` `UserAccount`-ja** kapja — pontosan azzal a lekérdezéssel, amivel az
`ExtractTextJobRunner.RunForEmailAsync` a keletkező Document `ownerUserId`-ját
beállítja (`Role == Admin && DeletedUtc == null`, `OrderBy(CreatedUtc)`).

- `Type = "ImportantEmail"`, `ActionUrl = "/documents"` (a Document ezen a ponton
  még nem létezik, nincs konkrét deep-link), `IdempotencyKey =
  $"important-email-{email.Id}"`, csatorna **InApp** (ADR-0008).

Alternatíva (eldobva): értesítés **minden** aktív Adult/Admin számára. Indok az
elvetésre: ingestkor a Document és a `RelatedFamilyMember` még nem ismert, tehát nem
tudjuk, kihez tartozik az e-mail; a fan-out minden felhasználónak **értesítési
fáradtságot** okozna minden beérkező fontos e-mailnél — épp a jel/zaj arányt rontaná,
amit a CR javítani akar (összhangban az ADR-0011 üres-digest-elhagyás filozófiájával).
A Gmail-forrást a háztartásban az admin köti be, így ő a természetes, determinisztikus,
egyszeri címzett; a többi családtag a digest/feed felületén amúgy is látja a
végül létrejövő Document-et.

### 3. `ClassifyEmail` ≠ `Classify` — két kiegészítő mechanizmus

A `ClassifyEmail` (korai, gyors, `EmailMessage`-re ható fontosság-jelzés) és a
meglévő `Classify` (késői, `Document`-re ható teljes osztályozás + noise-discard)
**két külön, egymást kiegészítő** mechanizmus. A `ClassifyEmail`:

- **nem** hoz létre/soft-delete-el `Document`-et (sosem töröl semmit),
- **nem** helyettesíti és **nem** váltja ki a `Classify` noise-discard logikáját,
- csak fontosság/kategória/határidő-hint jelzést tesz az `EmailMessage`-re, és
  `High` esetén értesít.

Egy `Low` besorolású e-mail attól még végigmegy a normál pipeline-on; ha a **későbbi**
`Classify` job egyetlen topicot sem talál rá, akkor (és csak akkor) a meglévő logika
soft-deleteli a Document-et. A `ClassifyEmail` `Low`-ja önmagában nem vált ki törlést.

Indoklás: a két job célentitása (EmailMessage vs. Document), időzítése (ingestkor vs.
pipeline végén) és mellékhatása (jelzés vs. soft-delete) különbözik; összevonásuk
összemosná a "gyors prioritás-jelzés" és a "végleges relevancia-szűrés" felelősséget,
és megbontaná a meglévő, tesztelt noise-discard viselkedést.

### 4. Konkurencia: nincs row-version, elég az EF per-oszlop UPDATE

A `ClassifyEmail` és az `ExtractText` job ugyanarra az `EmailMessage` sorra, de külön
`DbContext`-scope-ból, párhuzamosan futhat. **Nincs szükség optimista concurrency
token-re, row-version-re vagy pesszimista lockra**, mert a két job **diszjunkt
oszlophalmazt** ír:

- `ClassifyEmail` → `importance`, `category`, `has_deadline_hint` (+ `UpdatedUtc`),
- `ExtractText` → `ingest_status`, `processed_utc` (+ `UpdatedUtc`) és külön táblák.

Az EF Core entitásonként csak a ténylegesen módosított oszlopokat teszi az
`UPDATE SET`-be; mindkét scope a saját frissen betöltött entitását követi, így egyik
`SaveChanges` sem írja felül a másik oszlopait, futási sorrendtől függetlenül. Az
egyetlen közös oszlop az `UpdatedUtc` — erre a last-writer-wins ártalmatlan (audit-
timestamp, nincs adatvesztés). Nincs read-modify-write ütközés a jobok között.

Következmény (kontrakt-kényszer): az `EmailMessage.SetImportance` kizárólag a 3
fontosság-oszlopot + `UpdatedUtc`-t módosíthatja, és a `ClassifyEmail` runner soha nem
hívhat `MarkProcessed()`/`MarkFailed()`-et (azok `IngestStatus`-t írnak — az
`ExtractText` felségterülete).

## Alternatívák

- **Fontosság a Document-en tárolva** (a CR 3. pontja említi lehetőségként): eldobva.
  A fontosság-jelzésnek a Document létrejötte *előtt* kell léteznie, ezért az
  `EmailMessage` a helyes gazda; a Document-szintű mező külön migrációt és későbbi
  időzítést jelentene, ellentétben a CR "pipeline előtt" kritériumával.
- **A `ClassifyEmail` beolvasztása a `Classify`-ba egyetlen jobként:** eldobva
  (lásd 3. döntés).
- **Optimista concurrency token az `email_message`-en:** eldobva (lásd 4. döntés) —
  felesleges komplexitás diszjunkt oszlopírásnál.
- **Email-csatorna a `High` értesítéshez az MVP-ben:** eldobva (InApp elég, ADR-0008;
  per-email e-mail-fan-out zaj lenne) — v2 opció.

## Következmények

- A kontrakt (`docs/contracts/classify-email-contract.md`) e döntéseket rögzíti a
  `db-engineer` és `backend-dev` agentek felé.
- `email_message` séma: 3 új nullable oszlop + 1 parciális index
  (`ix_email_message_importance_high WHERE importance = 'High'`), a meglévő
  parciális-index konvenciót követve; egyetlen migráció.
- Az `AiJobType.ClassifyEmail` enum-bővítés migráció-mentes.
- Frontend-módosítás nem tárgya ennek a körnek (a `High` e-mail a digest-feature
  `/notifications` feed-felületén megjelenik); a parciális index előkészíti egy
  későbbi "Fontos e-mailek" nézet backend-szűrését (külön CR).

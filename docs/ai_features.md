# AI feature roadmap — Family OS

> Státusz: DRAFT v0.1 · Dátum: 2026-07-10 · Nyelv: magyar
> Kapcsolódó: [ai-pipeline.md](ai-pipeline.md), [search-strategy.md](search-strategy.md),
> [database-schema.md](database-schema.md)

Ez a dokumentum egy külső AI-feature-ötletlista (10 javasolt funkció, 3 fázisra
bontva) állapotfelmérése a jelenlegi kódbázishoz képest, kiegészítve minden
feature céljával és egy önálló megvalósítási tervvel. Az állapotfelmérés
2026-07-10-én, a kódbázis tételes átvizsgálásával készült.

---

## 1. Áttekintő táblázat

| Fázis | Feature | Megvan-e | Beilleszthető-e | Impact | User story |
|---|---|---|---|---|---|
| 1. fázis — AI dokumentum-inbox | [Strukturált adatkinyerés](#11-strukturált-adatkinyerés-vendor-összeg-dátum-ismétlődés) (vendor, összeg, dátum, ismétlődés) | **Megvan** (2026-07-10-én javítva egy hibát, lásd 1.1) | — | Nagy | — |
| 1. fázis | [Feladat/határidő-javaslat jóváhagyással](#12-feladathatáridő-javaslat-jóváhagyással) | **Megvan** | — | Nagy | — |
| 1. fázis | [Embedding generálás kereséshez](#13-embedding-generálás-kereséshez) | **Megvan** | — | Nagy | — |
| 2. fázis — Keresés és családi asszisztens | [RAG chat forráshivatkozással](#21-rag-chat-forráshivatkozással) | **Megvan** | — | Nagy | — |
| 2. fázis | [SQL-aggregáció a Q&A-ban](#22-sql-aggregáció-a-qa-ban) | **Nincs** | Igen, közepes effort | Közepes-Nagy | [CR260710-01](change-requests/cr260710-01-sql-aggregacio-qa.md) |
| 3. fázis — Proaktív működés | [Proaktív napi/heti összefoglaló](#31-proaktív-napiheti-összefoglaló) | **Nincs** | Igen, közepes effort | Nagy | [CR260710-02](change-requests/cr260710-02-proaktiv-napi-osszefoglalo.md) |
| 3. fázis | [Fontos e-mailek AI-alapú felismerése](#32-fontos-e-mailek-ai-alapú-felismerése) | **Részben** | Igen, közepes effort | Közepes | [CR260710-03](change-requests/cr260710-03-fontos-emailek-felismerese.md) |
| Nem fázisolt | [Egészségügyi AI-idővonal](#41-egészségügyi-ai-idővonal) | **Nincs** | Igen, nagyobb effort | Közepes | [CR260710-04](change-requests/cr260710-04-egeszsegugyi-idovonal.md) |
| Nem fázisolt | [Pénzügyi intelligencia](#42-pénzügyi-intelligencia-kategorizálás-anomália-detektálás) (kategorizálás, anomália) | **Részben** | Igen, közepes-nagy effort | Közepes-Nagy | [CR260710-05](change-requests/cr260710-05-penzugyi-intelligencia.md) |
| Nem fázisolt | [Dokumentumkapcsolatok / tudásgráf](#43-dokumentumkapcsolatok--tudásgráf) | **Nincs** | Igen, nagyobb effort | Kicsi-Közepes | [CR260710-06](change-requests/cr260710-06-dokumentumkapcsolatok.md) |
| Nem fázisolt | [Természetes nyelvű parancsok](#44-természetes-nyelvű-parancsok-llm-tool-calling) (LLM tool-calling) | **Nincs** | Igen, legnagyobb effort + biztonsági kockázat | Közepes | [CR260710-07](change-requests/cr260710-07-termeszetes-nyelvu-parancsok.md) |
| Nem fázisolt | [AI-javaslatok tanulása visszajelzésből](#45-ai-javaslatok-tanulása-visszajelzésből) | **Nincs** | Igen, alacsony-közepes effort | Kicsi-Közepes | [CR260710-08](change-requests/cr260710-08-ai-feedback-tanulas.md) |

---

## 2. Fázis 1 — AI dokumentum-inbox

### 1.1 Strukturált adatkinyerés (vendor, összeg, dátum, ismétlődés)

**Cél:** amikor bekerül egy számla, garancialevél vagy egyéb pénzügyi
dokumentum, a rendszer ne csak tárolja, hanem azonnal kinyerje belőle a
lényeget (szolgáltató, összeg, pénznem, dátumok, ismétlődés), hogy a
felhasználónak ne kelljen kézzel adatot bevinnie — csak jóváhagynia.

**Státusz:** Megvan. 2026-07-10-ig a `FinancialRecord` ág hibás volt: az
`IFinancialRecordExtractor` lekérte az AI-tól az adatokat, de az
`ExtractFacetJobRunner.ProcessFinancialAsync` sosem alkalmazta őket a
rekordra (nem is volt hozzá `Patch` metódus), így minden pénzügyi dokumentum
gyakorlatilag üres rekordot kapott. A `Vendor` mező pedig eleve nem is
szerepelt a kinyerési sémában.

**Megvalósítási terv (elvégezve):**
1. `FinancialRecord.Patch()` metódus hozzáadása a `Warranty.Patch` mintájára,
   a `ck_financial_paid` DB-constraint védelmével (csak akkor áll `IsPaid`
   `true`-ra, ha van hozzá használható dátum).
2. `FinancialRecordExtraction` DTO bővítése `RecordType` és `Vendor`
   mezőkkel, `IssueDate`/`DueDate` szétválasztása egyetlen `RecordDate`
   helyett — ezzel a kinyerés most már megegyezik az `ai-pipeline.md` 3.8-ban
   eredetileg dokumentált szerződéssel.
3. Új prompt (`extract-financial.v2.txt`), ami a `recordType`-ot és a
   `vendor`-t is kéri.
4. `ExtractFacetJobRunner.ProcessFinancialAsync` ténylegesen hívja a
   `Patch()`-et (Create és Update ágon is), enum-feloldással
   (`ParseFinancialRecordType`, `ParseRecurrencePeriod`).

**Hátralévő, opcionális finomítás:**
- A Warranty/Medical ágakhoz hasonlóan egy admin "reprocess" trigger vagy
  egyszeri backfill job a korábban (a bugfix előtt) létrejött üres
  `FinancialRecord` rekordok újrafeldolgozására.
- Duplikátum-számla észlelés (lásd [4.2 Pénzügyi intelligencia](#42-pénzügyi-intelligencia-kategorizálás-anomália-detektálás)).

### 1.2 Feladat/határidő-javaslat jóváhagyással

**Cél:** a rendszer proaktívan vegye észre a dokumentumban rejlő teendőket és
határidőket ("Kérjük 8 napon belül...", "A garancia 2027.03.10-én lejár"),
de a felhasználó tartsa a kontrollt — semmi nem válik automatikusan élő
feladattá vagy határidővé AI-jóváhagyás nélkül.

**Státusz:** Megvan. `ExtractTasksJobRunner` és `ExtractDeadlinesJobRunner`
a dokumentum szövegéből `Task`/`Deadline` javaslatokat hoz létre
`Origin = AiSuggested`, `Status = Suggested` állapotban; a jóváhagyás
(`approved_by_user_account_id`, `approved_utc`) külön endpointon történik
(`GetSuggestionsQueryHandler`, `BatchApproveCommandHandler`).

**Megvalósítási terv:** nincs teendő az alapfunkción. Finomítási irányok,
ha a gyakorlatban indokolttá válik:
1. Confidence-küszöb hangolása (jelenleg `ai-pipeline.md` 3.6 szerint 0.6 —
   ha sok az irreleváns javaslat, emelni érdemes).
2. Deduplikáció pontosítása (jelenleg cím+dátum egyezés — Levenshtein-alapú
   fuzzy matching a `3.7`-ben már említett Task/Deadline-átfedésekhez).

### 1.3 Embedding generálás kereséshez

**Cél:** a szemantikus és a kérdés-válasz (Q&A) keresés a friss tartalmakon
is működjön — dokumentumok, jegyzetek, feladatok és határidők egyaránt
kereshetők legyenek jelentés alapján, nem csak kulcsszóra.

**Státusz:** Megvan. `document_chunk`, `note_chunk`, `task_chunk`,
`deadline_chunk` táblák HNSW indexszel, `EmbedJobRunner` generálja az
embeddinget Create/Patch után, `EmbedBackfillService` pótolja a régi
(infrastruktúra előtti) rekordokat induláskor.

**Megvalósítási terv:** nincs teendő.

---

## 3. Fázis 2 — Keresés és családi asszisztens

### 2.1 RAG chat forráshivatkozással

**Cél:** a család természetes nyelven kérdezhessen a saját adatairól
("Mikor jár le a mosógép garanciája?"), és a válasz mindig visszahivatkozzon
a forrásdokumentumra/rekordra — az AI soha ne találjon ki tényt, és ha nincs
adat, mondja ki egyértelműen.

**Státusz:** Megvan. `QaHandler` hívja az `IQuestionAnswerService`-t a
`HybridSearchHandler` által visszaadott kontextus-chunkokra, a választ
`HallucinationGuard` ellenőrzi (a válasz csak olyan tényt tartalmazhat, ami
a chunk-forrásokban tényleg szerepel). Frontend chat felület is kész
(`frontend/src/app/features/search/search.page.ts`,
`chat-answer-message.component.ts`).

**Megvalósítási terv:** nincs teendő az alapfunkción.

### 2.2 SQL-aggregáció a Q&A-ban

**Cél:** az olyan kérdésekre, mint *"mennyi villanyszámlát fizettünk az
elmúlt 6 hónapban"*, pontos, számolt választ adjon a rendszer — ne bízza a
vektorkeresésre, mert az összegzési/számolási kérdéseknél a RAG
természeténél fogva pontatlan vagy félrevezető (a chunk-alapú retrieval nem
összesít, csak hasonló szövegrészeket talál).

**Státusz:** Nincs. Az `IntentClassifier`
(`Application/Search/Intent/IntentClassifier.cs`) csak UX-routingra szolgál
(filter/lookup/find/summarize), aggregációs intent nincs. A
`FilterSearchHandler` egyszerű szűrést végez, `SUM`/`GroupBy` sehol nincs
implementálva.

**Megvalósítási terv:**
1. Új intent hozzáadása az `IntentClassifier`-hez (`aggregate`) — magyar
   kulcsszavak: "mennyi", "összesen", "átlagosan", "hányszor" + pénzügyi
   kontextus (a `search-strategy.md` 5.1-es heurisztika-mintájára).
2. Slot-kinyerés bővítése: entitástípus (elsőként `FinancialRecord`),
   dátumtartomány, vendor/kategória szűrő — a meglévő slot-extraction
   mechanizmus (`search-strategy.md` 5.3) mintájára.
3. Új `AggregateSearchHandler`: LINQ `SUM`/`AVG`/`COUNT` lekérdezés a
   `FinancialRecord`-okon, ugyanazzal az RBAC-szűréssel, mint
   `FilterSearchHandler` (`IsPrivate`/`CreatedByUserAccountId`).
4. `QaHandler` routing bővítése: `aggregate` intent esetén NEM hívja az
   LLM-et RAG-módban — a számolt eredményt egy sablon-mondatba illeszti
   (pl. *"Az elmúlt 6 hónapban összesen 108 400 Ft villanyszámlát
   fizettünk."*), opcionálisan LLM csak a megfogalmazást "magyarosítja",
   a számot nem generálhatja.
5. Validáció: a válaszban szereplő összeg kizárólag a ténylegesen lefutott
   SQL-aggregáció eredménye lehet (nem az LLM szabadon generált száma) —
   ugyanaz az elv, mint a `HallucinationGuard`-nál.

---

## 4. Fázis 3 — Proaktív működés

### 3.1 Proaktív napi/heti összefoglaló

**Cél:** a család ne csak akkor kapjon információt, ha kérdez — a rendszer
magától figyelmeztessen a mai/holnapi teendőkre, csökkentve az elfelejtett
határidők és be nem fizetett számlák kockázatát.

**Státusz:** Nincs. `NotificationFeedRetentionJob` csak a régi olvasott
értesítéseket takarítja; `DueReminderDispatcher` és `EscalationScheduler`
kizárólag a felhasználó vagy az AI által **explicit módon beütemezett**
reminder-eket tüzeli/eszkalálja — proaktív digest-generáló job nincs.

**Megvalósítási terv:**
1. Új Hangfire recurring job (`DailyDigestJob`), napi egy futtatással
   (reggel, pl. 07:00, a `quiet_hours` beállításokat figyelembe véve).
2. Családtagonként (RBAC-szűrve) lekérdezi: aznapi/holnapi `Reminder`-eket,
   a következő 7 napban esedékes `Deadline`-okat, az elmúlt 24 órában
   érkezett új `Document`-eket.
3. Rövid, sablon-alapú összefoglaló összeállítása (nem feltétlen kell LLM —
   az adat már strukturált; opcionálisan egy kis LLM-hívás csak a
   megfogalmazást "emberivé" teszi, a tényeket nem generálja).
4. `notification_feed` insert (`related_entity_type = 'DailyDigest'`),
   csatorna: InApp alapból, Email a meglévő `SmtpNotificationService`-en át
   opcionálisan (a `user_account.email_enabled`/`quiet_hours` beállítások
   szerint).
5. Idempotencia: egy felhasználó egy naptári napra csak egy digest-et
   kapjon — dedup-ellenőrzés `target_user_account_id` + aznapi dátum
   alapján a job futásakor.

### 3.2 Fontos e-mailek AI-alapú felismerése

**Cél:** a bejövő Gmail-üzenetek közül a rendszer emelje ki a ténylegesen
fontosakat (határidős, hivatalos, sürgős leveleket), ne kezelje egyformán
generikus dokumentumként az összes beérkező e-mailt.

**Státusz:** Részben. `EmailIngestionPoller` és `SyncSourceCommandHandler`
az e-mailt `Document`-té alakítja, és az így létrejött dokumentum megy át a
szokásos pipeline-on (Classify/Summarize/stb.) — de nincs email-specifikus,
a `Document`-létrehozás *előtti* fontosság/címzett-felismerés.

**Megvalósítási terv:**
1. Új `AiJobType` (`ClassifyEmail`), ami a `SyncSourceCommandHandler`-ben a
   `Document`-létrehozás előtt vagy azzal párhuzamosan fut, az
   `email_message.body_text`/`subject` alapján.
2. Új prompt (`classify-email.v1.txt`): fontosság (`High`/`Medium`/`Low`),
   kategória, érintett családtag-hint, van-e explicit határidő az e-mail
   szövegében — a meglévő `Classify` prompt (`ai-pipeline.md` 4.1)
   mintájára.
3. Adatbázis-bővítés: `email_message` táblára `importance` és
   `category` oszlop (migráció), vagy a létrejövő `Document` megfelelő
   mezőinek (`related_family_member_id`) pontosítására felhasználva.
4. `High` fontosságú e-mailnél azonnali `notification_feed` bejegyzés —
   nem várva meg a teljes (lassabb) dokumentum-pipeline lefutását.

---

## 5. Nem fázisolt bővítések

### 4.1 Egészségügyi AI-idővonal

**Cél:** egy családtag egészségügyi történései időben összefüggő képet
adjanak (pl. labor-trendek kontrollvizsgálatok között), ne csak egyedi,
egymástól független dokumentumok halmaza legyen.

**Státusz:** Nincs. Csak szabad `medical_record.structured_json` mező van,
amit jelenleg az `ExtractFacetJobRunner.ProcessMedicalAsync` **nem is tölt
ki** (csak a `record_type`/`record_date`/`title` mezőket állítja be) —
összehasonlító/trend-logika sehol nincs.

**Megvalósítási terv:**
1. `structured_json` séma szabványosítása legalább a gyakori
   labor-paraméterekre: `[{ "parameter": "CRP", "value": 12.4, "unit":
   "mg/L", "referenceRange": "0-5" }, ...]`.
2. `ExtractFacetJobRunner.ProcessMedicalAsync` bővítése: a
   `structured_json` tényleges kitöltése az `IMedicalRecordExtractor`
   eredményéből (jelenleg ez a mező érintetlen marad — hasonló hiányosság,
   mint a Financial ágnál volt).
3. Új query/handler: egy adott családtag adott paraméterének idősorát
   lekérdezi (`family_member_id` + `record_type = LabResult` +
   `structured_json` kulcs szerint — a meglévő `ix_medical_structured` GIN
   index, `jsonb_path_ops`, már készen áll erre).
4. UI: idővonal-komponens (grafikon/táblázat) a családtag egészségügyi
   oldalán.
5. Opcionális AI-réteg: automatikus eltérés-kiemelés, ha az új érték
   szignifikánsan eltér az előzőtől vagy a referenciatartománytól — de itt
   az AI szerepe kizárólag rendszerezés és kiemelés, nem diagnózis.

### 4.2 Pénzügyi intelligencia (kategorizálás, anomália-detektálás)

**Cél:** a család lássa a kiadási mintázatait, és időben értesüljön a
szokatlan tételekről (áremelkedés, duplikált számla, elfeledett fizetés),
ne csak utólag, egyesével átnézve a számlákat vegye észre ezeket.

**Státusz:** Részben. A `Classify` job a topic-taxonómián keresztül ad
gyenge kategorizálási jelet (pl. `penzugy/szamla`), és az 1.1-es bugfix óta
a `RecordType`/`RecurrencePeriod` mezők is helyesen töltődnek — de dedikált
kategorizálás és anomália-detektálás (ismétlődés-felismerés,
áremelkedés-riasztás, duplikátum-észlelés) nincs implementálva.

**Megvalósítási terv:**
1. Kategorizálás pontosítása: a `FinancialRecord.RecordType` (most már
   megbízhatóan töltődik, lásd 1.1) és a `Topic`-taxonómia
   összekapcsolása egy pénzügyi al-kategória nézethez (rezsi/élelmiszer/
   egészség/autó/biztosítás/előfizetés/egyéb).
2. Ismétlődő költség felismerés: a `RecurrencePeriod` mező (szintén 1.1
   óta megbízható) alapján egyszerű SQL `GROUP BY vendor,
   recurrence_period` lekérdezéssel azonosítható.
3. Új batch job (`FinancialAnomalyScanner`, napi/heti Hangfire recurring
   job): vendor+recurrence csoportokon belül összehasonlítja az új
   `Amount`-ot az előző N hónap átlagával/utolsó értékével; küszöb feletti
   eltérésnél `notification_feed` bejegyzés (pl. *"A villanyszámla 31%-kal
   magasabb az előző 3 hónap átlagánál."*).
4. Duplikátum-számla észlelés: azonos `vendor` + hasonló `amount` +
   közeli `issue_date` kombináció → figyelmeztetés — hasonló elv, mint a
   `Document.sha256` alapú fájl-dedup, de fuzzy (nem exact-match) logikával.

### 4.3 Dokumentumkapcsolatok / tudásgráf

**Cél:** az összetartozó dokumentumok (pl. vásárlási számla + garancialevél
+ szervizmunkalap, vagy labor lelet + szakorvosi vélemény + felírt
gyógyszer) egy kattintással elérhetők legyenek egymásból, ne kelljen
külön-külön, kereséssel megtalálni őket.

**Státusz:** Nincs. Nincs `entity_relation` (vagy hasonló) tábla és
kapcsolódó AI-logika a kódbázisban.

**Megvalósítási terv:**
1. Új `app.entity_relation` tábla és migráció:
   ```sql
   CREATE TABLE app.entity_relation (
       id uuid PRIMARY KEY,
       source_entity_type text NOT NULL,
       source_entity_id uuid NOT NULL,
       target_entity_type text NOT NULL,
       target_entity_id uuid NOT NULL,
       relation_type text NOT NULL,
       origin app.origin NOT NULL,
       confidence numeric(5,4),
       is_approved boolean NOT NULL DEFAULT false,
       created_utc timestamptz NOT NULL DEFAULT now()
   );
   ```
2. Új AI job (`LinkEntitiesJob`), ami egy új dokumentum embeddingje és
   metaadatai (vendor, dátum, családtag) alapján megkeresi a
   hasonló/kapcsolódó meglévő dokumentumokat/rekordokat (szemantikus
   hasonlóság VAGY azonos vendor + közeli dátum), és javasolt kapcsolatot
   hoz létre (`is_approved = false`, `origin = AiSuggested`).
3. UI: a dokumentum/rekord részletező oldalán egy "Kapcsolódó elemek"
   szekció, jóváhagyás/elutasítás lehetőséggel — ugyanaz a
   javaslat→jóváhagyás minta, mint a Task/Deadline-nél
   (`ai-pipeline.md` 5.).

### 4.4 Természetes nyelvű parancsok (LLM tool-calling)

**Cél:** a felhasználó ne csak kérdezhessen, hanem utasíthasson is
természetes nyelven (pl. *"Emlékeztess 3 nappal a garancia lejárta
előtt."*), anélkül hogy be kéne lépnie a megfelelő űrlapra.

**Státusz:** Nincs. Nincs semmilyen tool/function-calling absztrakció a
kódbázisban (`IAiProvider` csak `CompleteAsync`-et ismer).

**Megvalósítási terv:**
1. Kontrollált tool-registry kialakítása (`ITool` interfész: `Name`,
   `JsonSchema`, `ExecuteAsync`), kezdetben szűk whitelisttel
   (`create_reminder`, `assign_document`, `add_tag`).
2. Ollama-oldali tool-use támogatás ellenőrzése — az `ai-pipeline.md` 8.
   provider-mátrixa szerint az Ollama-nál ez jelenleg csak
   prompt-engineeringgel oldható meg (nincs natív tool-use), tehát a
   modellnek egy szigorú JSON-sémában kell "tool-hívást" visszaadnia,
   amit a backend parse-ol.
3. **Kritikus biztonsági szabály:** az LLM válasza csak egy tool-hívási
   JAVASLAT — a felhasználónak egy megerősítő UI-n jóvá kell hagynia,
   mielőtt a backend ténylegesen végrehajtja. Az LLM soha nem futtathat
   tetszőleges SQL-t vagy közvetlen írási műveletet.
4. Minden tool-végrehajtás naplózása a meglévő `AuditBehavior` mintájára
   (`AuditAction`, `entity_id`, `details_json`).

### 4.5 AI-javaslatok tanulása visszajelzésből

**Cél:** a rendszer idővel pontosabb javaslatokat adjon azáltal, hogy
megjegyzi, mit fogadtak el / utasítottak el / javítottak a felhasználók az
AI-javaslatokból, és ez hosszú távon visszahat a promptokra.

**Státusz:** Nincs. Nincs `ai_feedback` (vagy hasonló) tábla/entitás a
kódbázisban.

**Megvalósítási terv:**
1. Új `app.ai_feedback` tábla és migráció:
   ```sql
   CREATE TABLE app.ai_feedback (
       id uuid PRIMARY KEY,
       user_account_id uuid NOT NULL,
       entity_type text NOT NULL,
       entity_id uuid NOT NULL,
       job_type text NOT NULL,
       feedback_type text NOT NULL,       -- Accepted | Rejected | Corrected
       original_result_json jsonb,
       corrected_result_json jsonb,
       created_utc timestamptz NOT NULL DEFAULT now()
   );
   ```
2. A meglévő Approve/Reject/Patch command handlerekbe (Task, Deadline,
   Warranty, MedicalRecord, FinancialRecord) egy feedback-log hook
   beépítése: mi volt az AI eredeti javaslata vs. mi lett a végleges
   (jóváhagyott vagy módosított) állapot — hasonló pipeline-behavior
   mintában, mint az `AuditBehavior`.
3. Első lépésben csak gyűjtés + egy admin-dashboard nézet (mely mezőket
   javítják leggyakrabban, mely javaslattípusok elutasítási aránya magas)
   — ez már önmagában értékes diagnosztika modell-finomhangolás nélkül is.
4. Második lépésben: a leggyakrabban javított minták few-shot példaként
   kerülnek be a promptba (pl. *"Korábban ezt javítottuk ki hasonló
   esetben: ..."*) — ez még nem modell-finetuning, csak
   prompt-engineering, de érdemben javíthatja a találati arányt.

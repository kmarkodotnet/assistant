# ADR-0013 — AI-visszajelzés gyűjtése: facet-Origin modell, ai_feedback séma, job_type-feloldás, tool-call integráció

- Státusz: Elfogadva
- Dátum: 2026-07-12
- Döntéshozó: architect agent (CR260710-08 kontrakt-tervezés)
- Kapcsolódó: [CR260710-08](../change-requests/cr260710-08-ai-feedback-tanulas.md),
  [ai_features.md §4.5](../ai_features.md#45-ai-javaslatok-tanulása-visszajelzésből),
  [ai-feedback-contract.md](../contracts/ai-feedback-contract.md),
  [ADR-0011 (tool-calling)](ADR-0011-tool-calling-vegrehajtasi-modell.md),
  [ADR-0012](ADR-0012-classifyemail-korai-fontossag.md)

## Kontextus

A CR260710-08 1. fázisa strukturáltan meg akarja jegyezni, mit fogadnak el /
utasítanak el / javítanak a felhasználók az AI-javaslatokból (Task, Deadline,
Warranty, MedicalRecord, FinancialRecord + tool-call), hogy hosszú távon a
prompt-minőség javítható legyen. A cél 1. fázisban **kizárólag gyűjtés + admin
dashboard** — nem prompt-visszacsatolás.

Öt nem triviális kérdést kell eldönteni a párhuzamos fejlesztés előtt, mert
mindegyik érint migrációt, meglévő kontraktot vagy a hook-mechanizmust:

1. Hogyan kapnak a facet-entitások (Warranty/MedicalRecord/FinancialRecord)
   AI-eredet-jelölést és Approve/Reject/Patch réteget, ha ma nincs `Origin`-juk?
2. Hány migrációval és milyen backfill-lel vezessük be?
3. Hogyan derüljön ki egy feedback-eseménynél a `job_type` (melyik AI-job
   termelte a javaslatot)?
4. Hol tároljuk a `ai_feedback`-et és milyen típusokkal (string vs enum,
   `entity_id` nullázhatóság)?
5. Milyen mechanizmus írja a feedback-et — pipeline-behavior vagy explicit hook?

## Döntés

### D1 — Facet-Origin lightweight modell: `Origin` + soft-delete, NINCS státusz-állapotgép

A `Warranty`, `MedicalRecord`, `FinancialRecord` a `Deadline` mintáját követi, de
**leegyszerűsítve**: kap `Origin` (a meglévő `app.origin` natív enum), valamint
`ApprovedByUserAccountId` / `ApprovedUtc` mezőt, továbbá `Approve()` és `Reject()`
domain-metódust. **Nem** kap `Status` mezőt vagy állapotgépet.

Indoklás: ezeknek az entitásoknak ma nincs jóváhagyási életciklusuk — az
`ExtractFacetJobRunner` létrehozáskor azonnal élővé teszi őket. A CR viszont
Approve/Reject/Patch-et kér. A `FamilyTask`/`Deadline` teljes `Status`-állapotgépe
(`Suggested`/`Open`/`Upcoming`/...) itt túlméretezett lenne: az egyetlen valós
állapotátmenet az „AI-javasolt → jóváhagyott" (`Origin`) és a „elutasítva"
(`DeletedUtc`, a `FamilyTask.Reject()` mintájára). Az `Origin ∈ {AiSuggested,
AiApproved} ∧ DeletedUtc IS NULL` invariáns elég a business-szabályhoz, kevesebb
kóddal és migrációval, mint egy külön státusz-enum.

Az extraction-runner létrehozáskor `Origin.AiSuggested`-et állít; kézi
facet-létrehozás nincs, így minden rekord AI-eredetű (ez fontos a D2 backfillnél).

**Elvetett alternatíva:** külön `WarrantyStatus`/`MedicalRecordStatus`/... enum +
állapotgép a Task/Deadline teljes analógiájára. Eldobva: nincs több valós állapot,
mint amit az `Origin` + `DeletedUtc` lefed; 3 új enum + 3 natív DB-enum-típus +
állapotgép-szolgáltatás felesleges komplexitás és migrációs felület.

### D2 — Két additív migráció, DEFAULT-backfill, alacsony kockázat

- `20260712000002_AddFacetOrigin`: a 3 facet-táblára `origin app.origin NOT NULL
  DEFAULT 'AiSuggested'` + `approved_by_user_account_id` + `approved_utc`. A
  `DEFAULT 'AiSuggested'` az `ADD COLUMN` pillanatában backfilleli a meglévő
  sorokat — helyes, mert minden mai facet-rekord AI-extractionből származik (D1).
- `20260712000003_AddAiFeedback`: az új `app.ai_feedback` tábla + 2 index + 2
  CHECK-constraint.

Két külön migráció (nem egy) a rollback-barát szeparációért: a facet-oszlopok és
az új tábla független deploy-egységek. Mindkettő tisztán additív — nincs
adatmigráció, nincs meglévő oszlop-átértelmezés, nincs breaking change.

**Elvetett alternatíva:** egyetlen összevont migráció. Eldobva: kisebb rollback-
granularitás; a repó eddig is entitáscsoportonként külön migrációt használt.

### D3 — `job_type` = az entitástípusból levezetett statikus érték (NINCS lookup, NINCS új FK)

A `ai_feedback.job_type` értékét **nem** DB-lookuppal és **nem** egy új
`SourceAiJobId` oszloppal állítjuk elő, hanem az `entity_type`-ból egy **statikus
map**-pel az `AiFeedbackLogger`-ben:

| entity_type | job_type |
|---|---|
| Task | ExtractTasks |
| Deadline | ExtractDeadlines |
| Warranty / MedicalRecord / FinancialRecord | ExtractFacet |
| tool-call | ToolCall:&lt;toolName&gt; |

Indoklás:
- A javaslatot termelő job-típus **teljesen determinált** az entitástípusból
  (1:1). Az `ai_feedback` aggregációs célja épp „melyik javaslattípus
  elutasítási aránya magas" — ehhez a **típus** kell, nem a konkrét job-instancia.
- A `SourceAiJobId` FK **migrációs kockázatot** és létrehozáskori írási terhet
  jelentene mind az 5 entitáson, minimális haszonnal 1. fázisban.
- A `TargetType+TargetId` lookup ráadásul **nem is működne közvetlenül**: az
  `AiProcessingJob` a `Document`-re mutat (`TargetType=Document`, `TargetId=
  documentId`), nem a létrejött Task/Deadline/facet-rekordra. A lookup így egy
  törékeny, több-értékű „legutóbbi Done ExtractX job a source-documenthez"
  keresés lenne — felesleges, ha a típus statikusan levezethető.

**Következmény (jövőbeli munka):** ha a 2. fázis a konkrét job-instanciát (pl.
modell-verzió, prompt-verzió) is rögzíteni akarja, akkor bevezethető egy
`SourceAiJobId` — de az külön CR, itt tudatosan kihagyva.

**Elvetett alternatívák:** (a) `SourceAiJobId` FK az 5 entitáson létrehozáskor —
migráció + írási teher, 1. fázisban felesleges; (b) `TargetType+TargetId`
runtime-lookup a legutóbbi Done jobra — törékeny, több-értékű, és a job a
documentre mutat, nem a rekordra.

### D4 — `ai_feedback`: string-mezők, nullable `entity_id`

- `entity_type` / `job_type` / `feedback_type` **`text` (string)**, nem natív
  Postgres enum. Összhangban azzal, hogy az `AiJobType`/`JobTargetType`
  EF-mappingje is `HasConversion<string>` (nincs DB-enum) — így a jövőbeli új
  entitás/tool/feedback-típusok **migráció-mentesek** (ugyanaz az érv, mint
  ADR-0012 D1-ben a `ClassifyEmail` enum-bővítésnél). A `feedback_type`-ot egy
  CHECK-constraint zárja `Accepted|Rejected|Corrected`-re.
- `entity_id` **nullable**. Ok: a tool-call **elutasításakor** nem jön létre
  entitás (a felhasználó a javaslatot dobja el, mielőtt bármi keletkezne), így
  nincs mire hivatkozni. Minden más feedback-nél (Accepted/Corrected, és a
  suggestion-Rejected is) az `entity_id` kitöltött. A CR eredeti sémája
  `entity_id NOT NULL`-t javasolt — tudatosan lazítunk rajta a tool-call-reject
  eset kezelésére.
- `ck_ai_feedback_corrected` constraint: `Corrected` mindig hordozza a
  `corrected_result_json`-t — így a diagnosztika (mely mezőket javítják) mindig
  számolható.

**Elvetett alternatíva:** natív `app.feedback_type`/`entity_type` enumok. Eldobva:
minden új tool/entitás DB-enum-migrációt igényelne; a string + CHECK olcsóbb és
konzisztens a meglévő job-enum-kezeléssel.

### D5 — Explicit `IAiFeedbackLogger` hook, NEM MediatR pipeline-behavior

A feedback-írás **explicit service** (`IAiFeedbackLogger`), amit az egyes
Approve/Reject/Patch handlerek hívnak — **nem** automatikus
`IPipelineBehavior` az `AuditBehavior` mintájára.

Indoklás:
- **A `Corrected` diffhez kell a mutáció ELŐTTI állapot.** Az `AuditBehavior`
  csak a request/response-t látja, az entitás mutáció előtti mezőit nem. A
  Patch-handlerekben a `UpdateDetails`/`Patch` **felülírja** a mezőket, ezért az
  „eredeti" JSON-t explicit, a mutáció előtt kell elmenteni — ezt csak a
  handlerből lehet megtenni.
- **A `feedback_type` handler-specifikus:** Accepted (Approve), Rejected
  (Reject), Corrected (Patch, ha ténylegesen változott). Ez nem vezethető le
  generikusan a request-névből úgy, hogy az AI-origin guardot és a
  „ténylegesen változott-e" feltételt is helyesen kezelje.
- **AI-origin guard:** csak `Origin ∈ {AiSuggested, AiApproved}` esetén szabad
  írni — egy manuálisan létrehozott Task szerkesztése nem AI-feedback. Ezt a
  handler tudja (van betöltött entitása), egy generikus behavior nem.

A logger a feedback-sort a **hívó handler DbContextébe add-eli**, és a handler
meglévő `SaveChangesAsync`-e perzisztálja — így a feedback a kiváltó mutációval
**egy tranzakcióban** áll vagy bukik (ugyanaz a minta, mint a reminder-generálás
az `ApproveDeadlineCommandHandler`-ben). Kivétel a **tool-call** ág: a
`ConfirmToolCall`/`RejectToolCall` handler nem ír DbContexten keresztül (tokent
validál + `ISender`-t hív), ezért ott a logger explicit `FlushAsync(ct)`-t végez
saját scoped contextjén.

**Elvetett alternatíva:** `AiFeedbackBehavior : IPipelineBehavior`. Eldobva:
nem látja a pre-mutation állapotot (Corrected-diff lehetetlen), és a
feedback_type/AI-origin logika nem generikus.

### D6 — Tool-call feedback: Accepted a confirmre, Rejected a rejectre

A tool-call `ai_feedback`-et az ADR-0011 stateless proposal-token modelljére
illesztjük:
- **Confirm** (sikeres `ExecuteAsync` után): `Accepted`, `entity_type =
  result.ResultType` (pl. `Reminder`), `entity_id = result.ResultId`,
  `job_type = ToolCall:<toolName>`, `original_result_json = envelope.Args`
  (a feloldott, megerősített argumentumok).
- **Reject:** `Rejected`, `entity_type = ToolCall:<toolName>`, `entity_id =
  NULL` (D4), `job_type = ToolCall:<toolName>`. Csak azonosított
  felhasználónál logol (anonim reject nem feedback).

Indoklás: a tool-call-nak nincs „korrekció" útja (a felhasználó a megerősítő
kártyán vagy elfogadja a javaslatot ahogy van, vagy elutasítja), ezért csak
Accepted/Rejected keletkezik — Corrected nem. Ez a dashboard-nak per-tool
elfogadási/elutasítási arányt ad (`job_type = ToolCall:create_reminder` stb.).

## Következmények

- A kontrakt (`docs/contracts/ai-feedback-contract.md`) e döntéseket rögzíti a
  `db-engineer`, `backend-dev`, `frontend-dev`, `qa-playwright` felé; a backlog
  (`docs/backlog.md`, FEAT-AIFB) a feladatbontást és a párhuzamosítható
  csoportokat.
- **DB:** 2 additív migráció (3 facet-oszlop + `ai_feedback` tábla), alacsony
  kockázat, adatmigráció nélkül.
- **Domain:** 3 facet-entitás `Origin`+`Approve`/`Reject`; `MedicalRecord.Patch`;
  új `AiFeedback` entitás; extraction-runner `Origin.AiSuggested`.
- **Application:** `IAiFeedbackLogger` + hook 5 entitás Approve/Reject/Patch
  handlerében + 2 tool-call handlerében; 9 új facet-command/handler a 3
  `DocumentsModule` 501-stub kiváltására; 2 admin query/handler.
- **API:** 9 új facet-endpoint (`RequireAdult`) + 2 admin-endpoint
  (`/api/v1/ai-quality/*`, `RequireAdmin`).
- **Frontend:** `/admin/ai-quality` oldal + service + route (`adminGuard`).
- **Jövőbeli munka (kifejezetten NEM e kör tárgya):** few-shot
  prompt-visszacsatolás (CR 2. lépés), esetleges `SourceAiJobId` a modell-/
  prompt-verzió rögzítéséhez, finomabb `ai_feedback` retenciós politika — mind
  külön CR.

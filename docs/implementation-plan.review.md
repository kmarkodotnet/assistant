# Review — implementation-plan.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Jó minőségű, végrehajtható fázisterv: minden fázis önállóan szállítható,
DoD-val, kockázattal, rollback-kel, agent-routinggal. A 6. fázis
„in-process worker előbb, Hangfire később” döntése és a worktree-stratégia
összefoglaló kifejezetten jó. A fő gondok: a backlog-leképezés eltér a
mvp-backlog.md-től, és a 12. fázis túlzsúfolt.

## Hibák / következetlenségek

### 1. Fázis↔story leképezés eltér a mvp-backlog.md 14. szakaszától (közepes)
Példák:
- **Fázis 3** itt: „A3, A4 (teljes), B1 (BE), B2, B3” — a backlogban:
  „A3, B2, B3, **C1 (csak upload skeleton)**”. A C1-skeleton itt nem
  szerepel, a B1 ott a 2. fázisban van.
- **Fázis 11** itt: „L1, L2, I1, I2, F3 (FE), H1 (BE), H2 (BE)” — a
  backlog táblázatában csak „I1, I2, L1”.
A két táblának bitre egyeznie kell, különben az orchestrátor két forrásból
kétféle scope-ot ad az agenteknek. Javaslat: a leképezés csak az egyik
doksiban éljen (itt), a backlog hivatkozzon rá.

### 2. Nem ütemezett Must/Should story-k (közepes)
A **C5 (dokumentum törlés, [M])** és a **C4 (szöveg-kézikorrekció, [S])**
egyik fázishoz sincs hozzárendelve sem itt, sem a backlogban — a C4-hez
ráadásul séma-mező is készült (v0.2 `original_content`). A H1/H2 (Notes)
frontendje szintén nincs sehova ütemezve (itt csak „H1 (BE), H2 (BE)”).
Pótolni kell a leképezésben (C4/C5 természetes helye az 5. vagy 11. fázis).

### 3. 12. fázis túlzsúfolt: hardening + 3 új feature (közepes)
A „Hardening” fázisban érkezik a teljes Gmail-integráció (K1), az SMTP
csatorna, az audit-modul, az admin-endpointok, a Helm chart, a TLS és a
DELIVERY.md is. Ez ellentmond a fázis címének, és a legkockázatosabb
integrációt (Gmail OAuth) a legvégére teszi puffer nélkül. Javaslat: a
K1/SMTP kerüljön külön (opcionális) fázisba vagy a 10–11. fázisba; a 12.
maradjon tisztán stabilizálás.

### 4. Pontatlanságok
- Fázis 2 változás-lista 4. pont: „`Document.OriginalContent`” — helyesen
  **`document_text.original_content`** (a séma v0.2 szerint).
- Fázis 10: `IcalRecurrenceEvaluator` a
  `FamilyOs.Infrastructure.Ai/Recurrence/` alá kerül — a recurrence-nek
  semmi köze az AI-hoz; helye `FamilyOs.Infrastructure/Recurrence/`.
- Fázis 11 DoD: „az MVP 8 UC-ja közül 6 (UC-01…UC-06, UC-07, UC-08
  kivételével)” — nyelvtanilag zavaros (az UC-07 kimarad vagy sem?);
  az UC-07 (egészségügyi rekord) a 8–9. fázis után már működnie kellene.
- Fázis 8 bevezeti a `PipelineOrchestrator`-t („Extract → Lang → 5
  párhuzamos → Status=Done”) — ez válasz az ai-pipeline.md nyitott
  Done-koordinációs kérdésére (ai-pipeline.review.md #1), de a pipeline-
  doksi nem tud róla; szinkronizálandó.
- Agent-routing: `triage`, `db-engineer`, `qa-playwright`, `doc-writer`
  stb. agentekre hivatkozik — a `.claude/agents/` könyvtár jelenleg üres
  (lásd README.review.md #1); a terv előfeltétele, hogy ezek elkészüljenek.

### 5. Kisebb észrevételek
- Globális DoD 3. pontja `pnpm build`/`pnpm test` — a backlog A1 az
  `ng new`-t rögzíti; a parancsok konzisztensek, csak a CI-definíció
  (GitHub Actions vs. `make ci`) marad nyitva (A1-ben „vagy”).
- Fázis 6 manuális ellenőrzés: „a UI-n `processing_status = Done`” — ekkor
  még nincs Analyzing lépés; jelezve van zárójelben, de érdemes átmeneti
  „Extracted” viselkedést definiálni, hogy a 8. fázis ne törje meg a
  UI-elvárást.
- Kapacitás-becslés (15.) reális nagyságrend; a 2–3 hetes párhuzamosított
  becslés optimista, de vállalható kommunikáció.

## Erősségek (megőrzendő)

- Fázisonkénti kockázat + rollback — ritkán készül el, itt végig ott van.
- Explicit MVP-döntés a 6. fázisban (in-process worker először).
- Worktree-stratégia fázisonkénti párhuzamossági térképpel.
- Globális kockázat-tábla a végén, mitigációkkal.

## Verdikt

Végrehajtható terv; az #1–#2 leképezési hibák javítása kötelező (ez a
factory vezérlő-dokumentuma), a #3 átütemezés erősen ajánlott.

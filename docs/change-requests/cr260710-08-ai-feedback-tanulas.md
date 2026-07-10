# CR260710-08 — AI-javaslatok tanulása visszajelzésből

> Státusz: DRAFT · Dátum: 2026-07-10 · Prioritás: **C** (Could)
> Kapcsolódó: [ai_features.md §4.5](../ai_features.md#45-ai-javaslatok-tanulása-visszajelzésből),
> [ai-pipeline.md §5](../ai-pipeline.md) (jóváhagyás állapotgép)
> Jelenlegi állapot: **Nincs**

## Story

Mint termékfelelős/üzemeltető, szeretném, ha a rendszer megjegyezné, mit
fogadnak el / utasítanak el / javítanak a felhasználók az AI-javaslatokból,
hogy idővel pontosabb javaslatokat tudjunk adni a promptok finomításával.

## Cél

Jelenleg minden Approve/Reject/Patch esemény (Task, Deadline, Warranty,
MedicalRecord, FinancialRecord AI-javaslatokon) elvész — nincs strukturált
nyoma annak, hogy az AI eredeti javaslata mennyire volt pontos, és mely
mezőket javítják a felhasználók leggyakrabban. Ez a visszajelzés hosszú
távon a prompt-minőség javításának alapja lenne.

## Jelenlegi állapot

Nincs `ai_feedback` (vagy hasonló) tábla/entitás a kódbázisban.

## Elfogadási kritériumok (Given/When/Then)

- **Given** egy AI-javasolt `Task`, **When** a felhasználó jóváhagyja
  változtatás nélkül, **Then** egy `ai_feedback` bejegyzés jön létre
  `feedback_type = Accepted`-del.
- **Given** egy AI-javasolt `Deadline`, **When** a felhasználó a
  jóváhagyás előtt módosítja (pl. a dátumot), **Then** a bejegyzés
  `feedback_type = Corrected`-del jön létre, `original_result_json` és
  `corrected_result_json` mezőkkel.
- **Given** egy AI-javaslat, **When** a felhasználó elutasítja, **Then**
  `feedback_type = Rejected` bejegyzés jön létre.
- **Given** egy admin felhasználó, **When** megnyitja az AI-minőség
  dashboardot, **Then** látja entitástípusonként/job-típusonként az
  elfogadási/elutasítási/javítási arányokat.

## Megvalósítási terv

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

## Érintett komponensek

- Új migráció: `app.ai_feedback`
- Új: `src/FamilyOs.Domain/Entities/AiFeedback.cs`
- A meglévő Approve/Reject/Patch handlerek (Tasks, Deadlines, Warranties,
  MedicalRecords, FinancialRecords) — feedback-log hook hozzáadása
- Frontend: admin AI-minőség dashboard

## Kifejezetten NEM cél

- Nem cél modell-finetuning vagy retraining az első verzióban — csak
  strukturált adatgyűjtés és prompt-szintű few-shot felhasználás.

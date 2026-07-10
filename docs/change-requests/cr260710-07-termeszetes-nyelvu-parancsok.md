# CR260710-07 — Természetes nyelvű parancsok (LLM tool-calling)

> Státusz: DRAFT · Dátum: 2026-07-10 · Prioritás: **C** (Could)
> Kapcsolódó: [ai_features.md §4.4](../ai_features.md#44-természetes-nyelvű-parancsok-llm-tool-calling),
> [ai-pipeline.md §8](../ai-pipeline.md) (provider-mátrix)
> Jelenlegi állapot: **Nincs**

## Story

Mint családtag, szeretnék természetes nyelvű utasításokat adni a
rendszernek (pl. *"Emlékeztess 3 nappal a garancia lejárta előtt."*),
hogy ne kelljen minden apró műveletért a megfelelő űrlapra navigálnom.

## Cél

Jelenleg a családi AI-asszisztens csak *kérdezésre* válaszol
(RAG/Q&A) — nincs mód arra, hogy a felhasználó utasítást adjon
neki, amit a rendszer ténylegesen végre is hajt (pl. reminder
létrehozása, dokumentum családtaghoz rendelése, címke hozzáadása).

## Jelenlegi állapot

Nincs semmilyen tool/function-calling absztrakció a kódbázisban —
`IAiProvider` csak `CompleteAsync`-et ismer (szöveg be, szöveg ki).

## Elfogadási kritériumok (Given/When/Then)

- **Given** egy felhasználó a chat felületen begépeli: *"Emlékeztess 3
  nappal a mosógép garancia lejárta előtt"*, **When** az LLM felismeri a
  szándékot, **Then** egy konkrét, megerősítésre váró tool-hívási
  javaslatot mutat a UI-n (pl. "Létrehozzak egy emlékeztetőt: [Warranty:
  Mosógép], 3 nappal a lejárat előtt?").
- **Given** egy tool-hívási javaslat, **When** a felhasználó megerősíti,
  **Then** a backend a whitelistelt tool-t (pl. `create_reminder`)
  hajtja végre, kontrollált paraméterekkel — az LLM soha nem futtat
  közvetlen SQL-t vagy tetszőleges műveletet.
- **Given** egy tool-hívási javaslat, **When** a felhasználó elutasítja
  vagy nem erősíti meg, **Then** semmilyen módosítás nem történik az
  adatbázisban.
- **Given** egy sikeresen végrehajtott tool-hívás, **Then** az
  `AuditBehavior`-hoz hasonló módon naplózásra kerül (ki, mikor, milyen
  tool-t, milyen paraméterekkel).

## Megvalósítási terv

1. Kontrollált tool-registry kialakítása (`ITool` interfész: `Name`,
   `JsonSchema`, `ExecuteAsync`), kezdetben szűk whitelisttel
   (`create_reminder`, `assign_document`, `add_tag`).
2. Ollama-oldali tool-use támogatás ellenőrzése — az `ai-pipeline.md` 8.
   provider-mátrixa szerint ez jelenleg csak prompt-engineeringgel
   oldható meg (nincs natív tool-use), tehát a modellnek egy szigorú
   JSON-sémában kell "tool-hívást" visszaadnia, amit a backend parse-ol.
3. **Kritikus biztonsági szabály:** az LLM válasza csak egy tool-hívási
   JAVASLAT — a felhasználónak egy megerősítő UI-n jóvá kell hagynia,
   mielőtt a backend ténylegesen végrehajtja. Az LLM soha nem futtathat
   tetszőleges SQL-t vagy közvetlen írási műveletet.
4. Minden tool-végrehajtás naplózása a meglévő `AuditBehavior` mintájára
   (`AuditAction`, `entity_id`, `details_json`).

## Érintett komponensek

- Új: `src/FamilyOs.Application/Abstractions/Ai/ITool.cs`
- Új: `src/FamilyOs.Infrastructure.Ai/Tools/*` (tool-implementációk)
- `src/FamilyOs.Application/Common/Behaviors/AuditBehavior.cs`
  (mintaként, tool-végrehajtás naplózásához)
- Frontend: tool-hívási javaslat megerősítő UI a chat felületen

## Kifejezetten NEM cél

- Nem cél tetszőleges, nyílt végű LLM-vezérelt automatizáció — a
  whitelistelt tool-kör szűk marad, és minden végrehajtás emberi
  megerősítést igényel. Ez a legérzékenyebb biztonsági kockázatú
  feature a listán, ezért óvatos, fokozatos bevezetést igényel.

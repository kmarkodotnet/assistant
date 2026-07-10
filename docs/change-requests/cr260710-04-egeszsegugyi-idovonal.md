# CR260710-04 — Egészségügyi AI-idővonal

> Státusz: DRAFT · Dátum: 2026-07-10 · Prioritás: **C** (Could)
> Kapcsolódó: [ai_features.md §4.1](../ai_features.md#41-egészségügyi-ai-idővonal),
> [database-schema.md §4.19](../database-schema.md), [ai-pipeline.md](../ai-pipeline.md)
> Jelenlegi állapot: **Nincs**

## Story

Mint családtag, szeretném egy adott családtag egészségügyi történéseit
(labor eredmények, kontrollvizsgálatok) időrendi, összehasonlítható
nézetben látni, hogy felismerjem a trendeket (pl. javuló/romló
laborérték), ne csak egymástól független dokumentumokként kelljen
átnéznem őket.

## Cél

Jelenleg minden orvosi dokumentum önálló, strukturálatlan
`MedicalRecord` rekordként létezik — nincs mód arra, hogy ugyanannak a
paraméternek (pl. CRP-érték) az időbeli alakulását lássa a felhasználó
anélkül, hogy minden dokumentumot egyenként megnyitna és összehasonlítana.

## Jelenlegi állapot

- Csak szabad `medical_record.structured_json` mező van, amit jelenleg az
  `ExtractFacetJobRunner.ProcessMedicalAsync` **nem is tölt ki** (csak a
  `record_type`/`record_date`/`title` mezőket állítja be) —
  összehasonlító/trend-logika sehol nincs.
- Nincs standardizált labor-paraméter séma, nincs idősor-lekérdezés,
  nincs UI-oldali idővonal-komponens.

## Elfogadási kritériumok (Given/When/Then)

- **Given** egy laboreredményt tartalmazó dokumentum, **When** a facet
  extraction lefut, **Then** a `structured_json` a felismert
  paramétereket (`parameter`, `value`, `unit`, `referenceRange`)
  tartalmazza.
- **Given** egy családtag, akinek legalább 2 laboreredménye van ugyanarra
  a paraméterre, **When** a felhasználó megnyitja az egészségügyi
  idővonalat, **Then** időrendben, összehasonlítható formában (grafikon
  vagy táblázat) látja az értékeket.
- **Given** egy új laboreredmény, aminek értéke szignifikánsan eltér az
  előzőtől vagy a referenciatartománytól, **Then** a rendszer ezt
  vizuálisan kiemeli (nem diagnosztizál, csak rendszerez és kiemel).
- **Given** egy `Child` szerepkörű vagy nem érintett családtag,
  **Then** nem fér hozzá más családtag egészségügyi idővonalához (a
  meglévő `MedicalRecord.IsPrivate` + RBAC szabályok szerint).

## Megvalósítási terv

1. `structured_json` séma szabványosítása legalább a gyakori
   labor-paraméterekre: `[{ "parameter": "CRP", "value": 12.4, "unit":
   "mg/L", "referenceRange": "0-5" }, ...]`.
2. `ExtractFacetJobRunner.ProcessMedicalAsync` bővítése: a
   `structured_json` tényleges kitöltése az `IMedicalRecordExtractor`
   eredményéből (jelenleg ez a mező érintetlen marad).
3. Új query/handler: egy adott családtag adott paraméterének idősorát
   lekérdezi (`family_member_id` + `record_type = LabResult` +
   `structured_json` kulcs szerint — a meglévő `ix_medical_structured`
   GIN index, `jsonb_path_ops`, már készen áll erre).
4. UI: idővonal-komponens (grafikon/táblázat) a családtag egészségügyi
   oldalán.
5. Opcionális AI-réteg: automatikus eltérés-kiemelés, ha az új érték
   szignifikánsan eltér az előzőtől vagy a referenciatartománytól — az AI
   szerepe kizárólag rendszerezés és kiemelés, nem diagnózis.

## Érintett komponensek

- `src/FamilyOs.Workers/Services/ExtractFacetJobRunner.cs`
  (`ProcessMedicalAsync`)
- `src/FamilyOs.Infrastructure.Ai/Tasks/OllamaMedicalRecordExtractor.cs`
- Új: `src/FamilyOs.Application/MedicalRecords/GetHealthTimelineQuery*.cs`
- Frontend: új idővonal-komponens

## Kifejezetten NEM cél

- Nem cél diagnózis-jellegű orvosi tanács adása — az AI kizárólag
  adatot rendszerez és eltérést jelez, orvosi értelmezést nem ad.

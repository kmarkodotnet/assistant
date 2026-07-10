# CR260710-01 — SQL-aggregáció a Q&A-ban

> Státusz: DRAFT · Dátum: 2026-07-10 · Prioritás: **S** (Should)
> Kapcsolódó: [ai_features.md §2.2](../ai_features.md#22-sql-aggregáció-a-qa-ban),
> [search-strategy.md](../search-strategy.md), [ai-pipeline.md](../ai-pipeline.md)
> Jelenlegi állapot: **Nincs**

## Story

Mint családtag, szeretnék összesítő pénzügyi kérdéseket feltenni a
családi AI-asszisztensnek (pl. *"Mennyi villanyszámlát fizettünk az elmúlt
6 hónapban?"*), hogy pontos, számolt választ kapjak ahelyett, hogy magamnak
kellene átnéznem és összeadnom a számlákat.

## Cél

A vektoros/RAG keresés chunk-alapú, tehát *hasonló szövegrészeket* talál,
nem *összesít*. Egy "mennyi" típusú kérdésre a RAG természeténél fogva
pontatlan vagy félrevezető választ adhat. Ezekre a kérdésekre strukturált
SQL-aggregációval (SUM/AVG/COUNT) kell válaszolni, nem szemantikus
kereséssel.

## Jelenlegi állapot

- Az `IntentClassifier` (`Application/Search/Intent/IntentClassifier.cs`)
  csak UX-routingra szolgál (`filter`/`lookup`/`find`/`summarize`),
  aggregációs intent nincs.
- A `FilterSearchHandler` egyszerű szűrést végez, `SUM`/`GroupBy` sehol
  nincs implementálva.
- Nincs dedikált pénzügyi riport endpoint.

## Elfogadási kritériumok (Given/When/Then)

- **Given** egy bejelentkezett felhasználó a Q&A felületen, **When**
  megkérdezi *"Mennyi villanyszámlát fizettünk az elmúlt 6 hónapban?"*,
  **Then** a válasz egy konkrét, SQL-lel számolt összeget tartalmaz (nem
  LLM által kitalált számot), és megjelöli az időszakot.
- **Given** ugyanez a kérdés, **When** a felhasználónak nincs jogosultsága
  egy adott `FinancialRecord`-hoz (`IsPrivate = true`, más felhasználóé),
  **Then** az az összegben nem szerepel (RBAC-szűrt aggregáció).
- **Given** egy aggregációs kérdés, amire nincs találat (pl. nincs
  `FinancialRecord` az adott időszakban), **Then** a válasz egyértelműen
  jelzi, hogy nincs adat — nem hibázik és nem generál hamis nullát.
- **Given** egy nem-aggregációs kérdés (pl. *"Mikor jár le a biztosítás?"*),
  **Then** a routing nem téríti el a meglévő `lookup`/`find` flow-t — az
  aggregáció csak a specifikusan összesítő kérdéseknél aktiválódik.

## Megvalósítási terv

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
   LLM-et RAG-módban — a számolt eredményt egy sablon-mondatba illeszti,
   opcionálisan LLM csak a megfogalmazást "magyarosítja", a számot nem
   generálhatja.
5. Validáció: a válaszban szereplő összeg kizárólag a ténylegesen lefutott
   SQL-aggregáció eredménye lehet — ugyanaz az elv, mint a
   `HallucinationGuard`-nál.

## Érintett komponensek

- `src/FamilyOs.Application/Search/Intent/IntentClassifier.cs`
- `src/FamilyOs.Application/Search/Handlers/QaHandler.cs`
- Új: `src/FamilyOs.Application/Search/Handlers/AggregateSearchHandler.cs`

## Kifejezetten NEM cél

- Nem cél tetszőleges szabad SQL generálása LLM-mel — az aggregáció
  típusa és entitása fixen kódolt (whitelist), csak a szűrőparaméterek
  (dátum, vendor) jönnek a slot-extractionből.

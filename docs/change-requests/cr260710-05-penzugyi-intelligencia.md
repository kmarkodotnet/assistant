# CR260710-05 — Pénzügyi intelligencia (kategorizálás, anomália-detektálás)

> Státusz: DRAFT · Dátum: 2026-07-10 · Prioritás: **S** (Should)
> Kapcsolódó: [ai_features.md §4.2](../ai_features.md#42-pénzügyi-intelligencia-kategorizálás-anomália-detektálás),
> [cr260710-01](cr260710-01-sql-aggregacio-qa.md) (előfeltétel jellegű kapcsolat)
> Jelenlegi állapot: **Részben**

## Story

Mint családtag, szeretném látni a kiadási mintázatainkat, és időben
értesülni a szokatlan tételekről (áremelkedés, duplikált számla,
elfeledett fizetés), hogy ne csak utólag, egyesével átnézve a számlákat
vegyem ezeket észre.

## Cél

A `FinancialRecord` adatok (a [CR260710 pénzügyi extractor
bugfix](../ai_features.md#11-strukturált-adatkinyerés-vendor-összeg-dátum-ismétlődés)
óta megbízhatóan) most már tartalmazzák a szükséges nyers adatokat
(vendor, összeg, típus, ismétlődés) — de nincs rájuk épülő
kategorizálási/anomália-logika, ami a családi pénzügyi kontrollt aktívan
segítené.

## Jelenlegi állapot

- A `Classify` job a topic-taxonómián keresztül ad gyenge kategorizálási
  jelet (pl. `penzugy/szamla`).
- 2026-07-10 óta a `FinancialRecord.RecordType`/`RecurrencePeriod` mezők
  megbízhatóan töltődnek (lásd `ai_features.md` §1.1).
- Dedikált kategorizálás (rezsi/élelmiszer/egészség/stb.) és
  anomália-detektálás (ismétlődés-felismerés, áremelkedés-riasztás,
  duplikátum-észlelés) nincs implementálva.

## Elfogadási kritériumok (Given/When/Then)

- **Given** legalább 3 hónapnyi `FinancialRecord` egy adott vendorra
  ismétlődő típussal, **When** egy új számla érkezik ugyanattól a
  vendortól, **Then** a rendszer összeveti az új összeget az előző N hónap
  átlagával, és jelentős eltérésnél (küszöb feletti %) figyelmeztetést ad.
- **Given** két `FinancialRecord`, azonos vendorral, hasonló összeggel és
  közeli `issue_date`-tel, **Then** a rendszer duplikátum-gyanús
  figyelmeztetést ad, nem hozza létre automatikusan mindkettőt
  jóváhagyott státuszban.
- **Given** egy felhasználó a pénzügyi áttekintő nézeten, **When**
  megnyitja azt, **Then** kategóriánkénti bontást lát (rezsi, élelmiszer,
  egészség, autó, biztosítás, előfizetés, egyéb).
- **Given** egy anomália-riasztás, **Then** az egy `notification_feed`
  bejegyzésként jelenik meg, forrás-hivatkozással az érintett
  `FinancialRecord`(ok)ra.

## Megvalósítási terv

1. Kategorizálás pontosítása: a `FinancialRecord.RecordType` és a
   `Topic`-taxonómia összekapcsolása egy pénzügyi al-kategória nézethez
   (rezsi/élelmiszer/egészség/autó/biztosítás/előfizetés/egyéb).
2. Ismétlődő költség felismerés: a `RecurrencePeriod` mező alapján
   egyszerű SQL `GROUP BY vendor, recurrence_period` lekérdezéssel
   azonosítható.
3. Új batch job (`FinancialAnomalyScanner`, napi/heti Hangfire recurring
   job): vendor+recurrence csoportokon belül összehasonlítja az új
   `Amount`-ot az előző N hónap átlagával/utolsó értékével; küszöb feletti
   eltérésnél `notification_feed` bejegyzés (pl. *"A villanyszámla 31%-kal
   magasabb az előző 3 hónap átlagánál."*).
4. Duplikátum-számla észlelés: azonos `vendor` + hasonló `amount` +
   közeli `issue_date` kombináció → figyelmeztetés — hasonló elv, mint a
   `Document.sha256` alapú fájl-dedup, de fuzzy (nem exact-match) logikával.

## Érintett komponensek

- Új: `src/FamilyOs.Workers/Services/FinancialAnomalyScanner.cs`
- `src/FamilyOs.Application/Search/Handlers/FilterSearchHandler.cs`
  (kategória-szűrés bővítése)
- Frontend: pénzügyi áttekintő/dashboard nézet

## Kifejezetten NEM cél

- Nem cél automatikus fizetés-indítás vagy bank-integráció — csak
  megfigyelés és riasztás a meglévő, dokumentumból kinyert adatokon.

# CR260710-06 — Dokumentumkapcsolatok / tudásgráf

> Státusz: DRAFT · Dátum: 2026-07-10 · Prioritás: **C** (Could)
> Kapcsolódó: [ai_features.md §4.3](../ai_features.md#43-dokumentumkapcsolatok--tudásgráf),
> [database-schema.md](../database-schema.md)
> Jelenlegi állapot: **Nincs**

## Story

Mint családtag, szeretném, hogy az összetartozó dokumentumok (pl.
vásárlási számla + garancialevél + szervizmunkalap, vagy labor lelet +
szakorvosi vélemény + felírt gyógyszer) egy kattintással elérhetők
legyenek egymásból, ne kelljen külön-külön, kereséssel megtalálnom őket.

## Cél

Jelenleg minden dokumentum/rekord önálló entitás — nincs explicit
kapcsolat két összetartozó elem között, még akkor sem, ha egyértelműen
ugyanahhoz az ügyhöz tartoznak (azonos vendor, közeli dátum, vagy
szemantikailag kapcsolódó tartalom).

## Jelenlegi állapot

Nincs `entity_relation` (vagy hasonló) tábla és kapcsolódó AI-logika a
kódbázisban.

## Elfogadási kritériumok (Given/When/Then)

- **Given** egy dokumentum (pl. mosógép vásárlási számla) és egy másik,
  hozzá kapcsolódó dokumentum (pl. garancialevél), **When** mindkettő
  feldolgozásra kerül, **Then** a rendszer felismeri a kapcsolatot (azonos
  vendor + közeli dátum VAGY magas szemantikus hasonlóság), és egy
  javasolt (`is_approved = false`) kapcsolatot hoz létre.
- **Given** egy javasolt dokumentumkapcsolat, **When** a felhasználó
  megnyitja a dokumentum részletező oldalát, **Then** látja a "Kapcsolódó
  elemek" szekciót, és jóváhagyhatja/elutasíthatja a javaslatot.
- **Given** egy jóváhagyott kapcsolat, **Then** mindkét irányból (A→B és
  B→A) navigálható a UI-n.
- **Given** egy törölt (soft-delete) dokumentum, **Then** a hozzá tartozó
  kapcsolatok is inaktívvá válnak (nem jelennek meg aktívként).

## Megvalósítási terv

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
   javaslat→jóváhagyás minta, mint a Task/Deadline-nél.

## Érintett komponensek

- Új migráció: `app.entity_relation`
- Új: `src/FamilyOs.Domain/Entities/EntityRelation.cs`
- Új: `src/FamilyOs.Workers/Services/LinkEntitiesJobRunner.cs`
- Frontend: "Kapcsolódó elemek" szekció a dokumentum/rekord részletező
  oldalakon

## Kifejezetten NEM cél

- Nem cél egy teljes, általános gráf-vizualizáció (node-graph UI) az
  első verzióban — csak egyszerű, listaszerű "kapcsolódó elemek" nézet.

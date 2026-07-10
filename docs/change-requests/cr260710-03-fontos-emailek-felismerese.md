# CR260710-03 — Fontos e-mailek AI-alapú felismerése

> Státusz: DRAFT · Dátum: 2026-07-10 · Prioritás: **C** (Could)
> Kapcsolódó: [ai_features.md §3.2](../ai_features.md#32-fontos-e-mailek-ai-alapú-felismerése),
> [ai-pipeline.md](../ai-pipeline.md)
> Jelenlegi állapot: **Részben**

## Story

Mint családtag, szeretném, hogy a Gmail-importból érkező üzenetek közül a
rendszer kiemelje a ténylegesen fontosakat (határidős, hivatalos, sürgős
leveleket), hogy ne kelljen minden beérkező levelet egyformán átnéznem.

## Cél

A jelenlegi email-import minden üzenetet ugyanazon az általános
dokumentum-pipeline-on visz át (szövegkinyerés → osztályozás →
összefoglalás → stb.), email-specifikus fontosság-/sürgősség-felismerés
nélkül. Egy fontossági jelzés lehetővé tenné, hogy a felhasználó azonnal
lássa, mire érdemes odafigyelnie, még mielőtt a teljes (lassabb)
pipeline lefutna.

## Jelenlegi állapot

- `EmailIngestionPoller` és `SyncSourceCommandHandler` az e-mailt
  `Document`-té alakítja, és az így létrejött dokumentum megy át a
  szokásos pipeline-on (Classify/Summarize/ExtractDeadlines/stb.).
- Nincs email-specifikus, a `Document`-létrehozás *előtti*
  fontosság/címzett-felismerés — minden email egyformán, generikus
  dokumentumként kezelt.

## Elfogadási kritériumok (Given/When/Then)

- **Given** egy új e-mail érkezik a szinkronizált Gmail-forrásból,
  **When** az ingestion lefut, **Then** az e-mail egy fontossági szintet
  (`High`/`Medium`/`Low`) és kategóriát kap, mielőtt a teljes
  dokumentum-pipeline lefutna.
- **Given** egy `High` fontosságúra minősített e-mail, **Then** azonnal
  (nem várva meg a teljes pipeline-t) egy `notification_feed` bejegyzés
  jön létre.
- **Given** egy e-mail, amiben explicit határidő szerepel (pl. "a díj
  augusztus 5-én esedékes"), **Then** a fontossági minősítés ezt jelzi
  (`hasDeadline = true` vagy hasonló), és a meglévő `ExtractDeadlines` job
  továbbra is lefut rá.
- **Given** egy alacsony fontosságú, hirdetés-jellegű e-mail, **Then** nem
  generál azonnali értesítést, csak a normál pipeline-on megy át.

## Megvalósítási terv

1. Új `AiJobType` (`ClassifyEmail`), ami a `SyncSourceCommandHandler`-ben
   a `Document`-létrehozás előtt vagy azzal párhuzamosan fut, az
   `email_message.body_text`/`subject` alapján.
2. Új prompt (`classify-email.v1.txt`): fontosság (`High`/`Medium`/`Low`),
   kategória, érintett családtag-hint, van-e explicit határidő az e-mail
   szövegében — a meglévő `Classify` prompt (`ai-pipeline.md` 4.1)
   mintájára.
3. Adatbázis-bővítés: `email_message` táblára `importance` és `category`
   oszlop (migráció), vagy a létrejövő `Document` megfelelő mezőinek
   (`related_family_member_id`) pontosítására felhasználva.
4. `High` fontosságú e-mailnél azonnali `notification_feed` bejegyzés —
   nem várva meg a teljes (lassabb) dokumentum-pipeline lefutását.

## Érintett komponensek

- `src/FamilyOs.Workers/Services/EmailIngestionPoller.cs`
- `src/FamilyOs.Application/*/SyncSourceCommandHandler.cs`
- Új: `src/FamilyOs.Infrastructure.Ai/Tasks/OllamaEmailClassifier.cs`
  (a meglévő `OllamaDocumentClassifier` mintájára)
- Migráció: `email_message.importance`, `email_message.category` oszlopok

## Kifejezetten NEM cél

- Nem cél az email automatikus törlése/archiválása fontosság alapján —
  csak jelzés/priorizálás, a felhasználó dönt.

# Review — epic-A-foundation-backend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Kiváló task-bontás: 21 jól méretezett task, egyértelmű fájllistákkal,
gépi ellenőrzésre alkalmas AC-kkel, függőségi sorrenddel. Több, a
tervező-doksikban nyitva hagyott kérdést itt oldottak meg jól
(RevokedSession tábla: T-ABE-13; readiness Ollama nélkül is 200:
T-ABE-20) — ezeket vissza kell vezetni a tervező doksikba.

## Észrevételek

1. **T-ABE-01 AC ellentmond az architektúrának (kicsi):** „`Domain`
   projekt csak `Microsoft.Extensions.Logging.Abstractions`-t importál” —
   az architecture.md 2. és 3.1 szerint a Domain **semmilyen** külső
   csomagot nem importál. Vagy az architektúra enyhítendő (logging
   abstractions megengedett), vagy az AC szigorítandó zero-dep-re.
2. **T-ABE-06: „22 enum” (kicsi):** a database-schema.md §2-ben 21 enum
   típus van (a `Cancelled`/`ExternalApiCall` értékek, nem új típusok).
   Szám helyett hivatkozás javasolt (mvp-backlog.review.md #2 ugyanez).
3. **T-ABE-13 / T-ABE-17 / T-ABE-18 — séma-visszavezetés (közepes):**
   a `RevokedSession`, az invite-rekord és a `UserPreferences` itt
   megszületik, de a database-schema.md és domain-model.md nem tartalmazza
   őket (domain-model.review.md #2). A T-ABE-18 ráadásul még mindig
   „vagy” döntést hagy (entity vs. JSONB mező) — task-szinten már
   döntés kell.
4. **T-ABE-20 — jó döntés, doksi-szinkron kell (pozitív):** a „ready 200,
   degraded ha Ollama nem elérhető” pontosan a helyes viselkedés — az
   architecture.md 12. és mvp-backlog A5 AC (amelyek Ollama-t hard
   feltételként írják) frissítendő erre.
5. **T-ABE-12:** az allowlist itt appsettings-ből jön, a T-ABE-17 invite
   „allowlist-re tesz” — a kettő viszonya (statikus bootstrap lista +
   dinamikus invite-rekordok uniója?) egy mondatban rögzítendő
   (security-privacy.review.md #3).

## Verdikt

Végrehajtásra kész; az 1–3. pont doksi-szinkronjai a BUILD alatt
elvégzendők, hogy a kontraktus-doksik ne maradjanak le a valóságtól.

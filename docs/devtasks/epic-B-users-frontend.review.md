# Review — epic-B-users-frontend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Rendben lévő FE-bontás: data layer → Family UI → invite → RBAC-tudatos
navigáció → settings sorrend jó, az AC-k UI-szinten konkrétak (magyar
szövegekkel). A megerősítő dialog (T-BFE-05) újrahasznosítható komponensként
készül — a qa/ui-test-scenarios ezt vissza is igazolja.

## Észrevételek

1. **T-BFE-08 menü-mátrix vs. child-jogosultság (közepes):** a Child
   navigációja „Dashboard, Reminders, Search, Settings” — nincs Documents
   és Notes, miközben a T-BBE-01 szerint a Child olvashat hozzá kötött
   dokumentumokat, és a qa/ui-test-scenarios QA-H1-04 a `/notes`-t
   read-only-ként tesztelné childdal. A menü-rejtés és az adat-jogosultság
   nem ugyanaz — döntsük el, a child lát-e Documents/Notes menüt
   (read-only), és ez kerüljön be a végleges RBAC-mátrixba
   (security-privacy.review.md #1).
2. **Topics menü kimaradt a szerepkör-listákból (kicsi):** a T-BFE-08
   felsorolásaiban a `Topics` egyik szerepnél sem szerepel — a
   frontend-structure 3.1 route-jai közt viszont guard nélkül ott van.
   Pótolandó (feltehetően Adult+).
3. **T-BFE-01 „generated VAGY kézi” (kicsi):** MVP-korai kézi DTO
   megengedett, de akkor legyen kimondva a migrációs pont (Fázis 5-től
   `pnpm gen:api` a forrás, a kézi fájl törlendő) — különben a kézi
   kliens ottragad.
4. **T-BFE-09 `<input type="time">`** — pragmatikus; a natív time-input
   viselkedése böngészőnként eltér, a Vitest-teszt (T-BFE-11) fedje le a
   value-formátumot (`HH:mm`).

## Verdikt

Végrehajtható; az #1–#2 menü/jogosultság kérdés a child-politika
lezárásával együtt rendezendő.

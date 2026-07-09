# UC03 — AI javaslat elfogadása / elutasítása (Task / Deadline)

## Leírás
Teszteli, hogy az AI által generált javaslatok (Suggested státuszú feladatok és
határidők) elfogadhatók vagy elutasíthatók a batch approve endpoint segítségével.

## Előfeltételek
- Bejelentkezett felhasználó (Adult vagy Admin szerepkör)
- Az adatbázisban létezik legalább egy `Suggested` státuszú task vagy deadline
  (AI pipeline hozta létre, vagy manuálisan seedelt teszt adat)

## Tesztlépések
1. `GET /api/v1/suggestions` — lekéri az aktuális javaslatokat
2. Ellenőrzi a válasz struktúráját (`tasks`, `deadlines`, `tags`, `topics`, `totalCount`)
3. Ha van javaslat:
   - `POST /api/v1/suggestions/batch` body: `{ "approve": { "tasks": ["<id>"] } }`
   - Ellenőrzi, hogy a task státusza `Open`-re változott
4. Ha van másik javaslat:
   - `POST /api/v1/suggestions/batch` body: `{ "reject": { "tasks": ["<id>"] } }`
   - Ellenőrzi, hogy a task eltűnt a javaslatok közül

## Elvárások / Assertions
- `GET /api/v1/suggestions` → HTTP 200, valid struktúra
- `POST /api/v1/suggestions/batch` → HTTP 200
- Elfogadott task/deadline státusza megváltozott (`Open` / `Upcoming`)
- Elutasított elem nem szerepel többé a javaslatok között

## Megjegyzés
**Teszttípus: API / Integráció**
Ha nincs Suggested javaslat az adatbázisban, a teszt csak a GET struktúrát
validálja és a POST-ot üres listával hívja (graceful degradation).

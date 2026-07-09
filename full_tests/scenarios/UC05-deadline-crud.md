# UC05 — Határidő kezelése (CRUD + szekciók)

## Leírás
Teszteli a határidő teljes életciklusát: létrehozás, listázás (Upcoming/Passed/Resolved
szekciók), módosítás, lezárás (resolve), törlés.

## Előfeltételek
- Bejelentkezett felhasználó (Adult vagy Admin szerepkör — `RequireAdult` policy)

## Tesztlépések
1. `POST /api/v1/deadlines` body:
   ```json
   {
     "title": "E2E Teszt Határidő",
     "dueDateUtc": "2027-01-15T12:00:00Z",
     "category": "Other"
   }
   ```
   → 201, visszakapott `id`
2. `GET /api/v1/deadlines` → az új deadline megjelenik
3. `GET /api/v1/deadlines/{id}` → valid részletek
4. `PATCH /api/v1/deadlines/{id}` body: `{ "title": "Módosított határidő" }` → 200
5. `GET /api/v1/deadlines/{id}` → `title === "Módosított határidő"`
6. `POST /api/v1/deadlines/{id}/resolve` → 200, `status === "Resolved"`
7. `DELETE /api/v1/deadlines/{id}` → 204

## Elvárások / Assertions
- 201 a létrehozásnál, valid JSON body `id`-val
- A deadline megjelenik a listában
- PATCH frissíti a title-t
- Resolve után `status === "Resolved"`
- DELETE után a rekord eltűnik

## Megjegyzés
**Teszttípus: API / Integráció**
Szekciók (Upcoming/Passed/Resolved) a `status` és `dueDateUtc` alapján kerülnek
meghatározásra — a UI szekciók külön UI tesztben validálhatók.
Cleanup: afterAll törli a tesztobjektumot.

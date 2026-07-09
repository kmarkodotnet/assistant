# UC15 — Családtag kezelése (CRUD)

## Leírás
Teszteli a családtagok (FamilyMember) teljes életciklusát:
létrehozás, listázás, módosítás, törlés. Az endpoint `/api/v1/family-members`.

## Előfeltételek
- Bejelentkezett **Admin** felhasználó (`RequireAdmin` policy a CUD műveletekhez)

## Tesztlépések
1. `POST /api/v1/family-members` body:
   ```json
   {
     "displayName": "E2E Teszt Tag",
     "relation": "Other",
     "birthDate": "1990-01-01"
   }
   ```
   → 201, visszakapott `id`
2. `GET /api/v1/family-members` → az új tag megjelenik
3. `GET /api/v1/family-members/{id}` → valid részletek
4. `PATCH /api/v1/family-members/{id}` body:
   ```json
   {
     "displayName": "Módosított Tag",
     "relation": "Other",
     "rowVersion": "<current>"
   }
   ```
   → 200
5. `DELETE /api/v1/family-members/{id}` → 204

## Elvárások / Assertions
- 201 a létrehozásnál, `id` a válaszban
- A tag megjelenik a listában
- PATCH sikeres (optimistic concurrency: rowVersion szükséges)
- DELETE után 204

## Megjegyzés
**Teszttípus: API / Integráció**
A CUD műveletek Admin jogosultsággal érhetők el; GET bármely bejelentkezett
felhasználónak elérhető.
Cleanup: afterAll törli a létrehozott tagot.

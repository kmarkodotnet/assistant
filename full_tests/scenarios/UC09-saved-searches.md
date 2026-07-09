# UC09 — Keresési előzmény / Mentett keresések

## Leírás
Teszteli a mentett keresések (SavedSearch) CRUD funkcionalitását:
mentés, listázás, törlés.

## Előfeltételek
- Bejelentkezett felhasználó (bármely szerepkör)

## Tesztlépések
1. `POST /api/v1/search/saved` body:
   ```json
   {
     "name": "E2E Mentett keresés",
     "query": { "query": "teszt", "mode": "Text" }
   }
   ```
   → 201, visszakapott saved search `id`
2. `GET /api/v1/search/saved` → az új mentett keresés megjelenik
3. `DELETE /api/v1/search/saved/{id}` → 204
4. `GET /api/v1/search/saved` → az elem már nem szerepel

## Elvárások / Assertions
- 201 a mentésnél, valid body `id`-val
- A mentett keresés megjelenik a listában
- DELETE után 204
- A törölt elem nem szerepel a listában

## Megjegyzés
**Teszttípus: API**
A keresési előzmény (history) külön entityként nem létezik — a SavedSearch
az explicit felhasználói mentési funkció.

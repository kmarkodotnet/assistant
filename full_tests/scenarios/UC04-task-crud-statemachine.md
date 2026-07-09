# UC04 — Feladat kezelése (CRUD + állapotgép)

## Leírás
Teszteli a feladat teljes életciklusát: létrehozás, listázás, módosítás,
állapotváltások (Suggested→Open→InProgress→Done), majd törlés.

## Előfeltételek
- Bejelentkezett felhasználó (Adult vagy Admin szerepkör — `RequireAdult` policy)

## Tesztlépések
1. `POST /api/v1/tasks` body: `{ "title": "E2E Teszt Feladat", "priority": "Normal" }` → 201
2. Elmentjük a visszakapott task `id`-t
3. `GET /api/v1/tasks` → ellenőrzi, hogy a task megjelenik a listában
4. `PATCH /api/v1/tasks/{id}` body: `{ "title": "Módosított cím" }` → 200
5. `GET /api/v1/tasks/{id}` → `title === "Módosított cím"`
6. `POST /api/v1/tasks/{id}/start` → 200, `status === "InProgress"`
7. `POST /api/v1/tasks/{id}/complete` → 200, `status === "Done"`
8. `DELETE /api/v1/tasks/{id}` → 204
9. `GET /api/v1/tasks/{id}` → 404

## Elvárások / Assertions
- Minden lépésnél a várt HTTP státusz
- A task állapota pontosan követi az átmeneti szabályokat
- Törlés után 404 a részletes lekérdezésnél

## Megjegyzés
**Teszttípus: API / Integráció**
Az állapotgép: Suggested → (approve) → Open → (start) → InProgress → (complete) → Done
A teszt manuálisan létrehozott taskot (Open státusz) visz végig.
Cleanup: afterAll/afterEach törli a létrehozott taskot.

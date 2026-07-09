# UC08 — Feljegyzés (Note) létrehozása + embedding

## Leírás
Teszteli a Note CRUD műveleteket és ellenőrzi, hogy a note létrehozása után
az Embed AI job elindult-e a háttérben.

## Előfeltételek
- Bejelentkezett felhasználó (bármely szerepkör — `RequireAuthenticated` policy)

## Tesztlépések
1. `POST /api/v1/notes` body:
   ```json
   {
     "title": "E2E Feljegyzés",
     "body": "Teszt feljegyzés szövege az embedding teszteléshez."
   }
   ```
   → 201, visszakapott note `id`
2. `GET /api/v1/notes` → az új note megjelenik a listában
3. `GET /api/v1/notes/{id}` → valid részletek (`title`, `body` mezők)
4. `PATCH /api/v1/notes/{id}` body: `{ "title": "Módosított feljegyzés" }` → 200
5. `GET /api/v1/notes/{id}` → `title === "Módosított feljegyzés"`
6. `GET /api/v1/ai-jobs?targetId={id}` — ellenőrzi, hogy létrejött-e Embed job
   (opcionális: csak ha az endpoint létezik)
7. `DELETE /api/v1/notes/{id}` → 204

## Elvárások / Assertions
- 201 a létrehozásnál, valid body
- A note megjelenik a listában
- PATCH frissíti a title-t
- Embed AI job létrejött (ha az endpoint elérhető)
- DELETE után 204

## Megjegyzés
**Teszttípus: API / Integráció**
Az Embed job aszinkron indul — a teszt nem várja meg a befejezését,
csak annak meglétét ellenőrzi az `ai-jobs` listában.

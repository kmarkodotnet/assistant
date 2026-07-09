# UC12 — Admin AI feladatok figyelése (job státuszok)

## Leírás
Teszteli az AI job adminisztrációs felületet: aktív jobok listázása,
queue statisztikák lekérdezése.

## Előfeltételek
- Bejelentkezett **Admin** felhasználó (`RequireAdmin` policy)

## Tesztlépések
1. `GET /api/v1/ai-jobs` → összes AI job listázása (szűrés nélkül)
2. Ellenőrzi a válasz struktúráját: `items` tömb és lapozási adatok
3. `GET /api/v1/ai-jobs?status=Pending` → szűrés státusz szerint
4. `GET /api/v1/ai-jobs/queue-stats` → queue statisztikák
5. Ha van Failed job: `POST /api/v1/ai-jobs/{id}/retry` → 200

## Elvárások / Assertions
- HTTP 200 minden GET kérésnél
- Valid JSON struktúra `items` tömbbel
- `queue-stats` valid statisztikákat tartalmaz
- Non-Admin userrel meghívva: 403 Forbidden

## Megjegyzés
**Teszttípus: API**
Az AI job adminisztrációs endpoint base path-ja: `/api/v1/ai-jobs`
(NEM `/api/v1/admin/ai-jobs` — lásd `AiJobsAdminModule.cs`).

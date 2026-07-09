# UC11 — Audit napló megtekintése (Admin)

## Leírás
Teszteli az audit napló lekérdezési funkcióját Admin jogosultsággal.
Ellenőrzi az audit bejegyzések meglétét és szűrhetőségét.

## Előfeltételek
- Bejelentkezett **Admin** felhasználó (`RequireAdmin` policy)
- Az adatbázisban léteznek audit bejegyzések (bejelentkezések, CRUD műveletek)

## Tesztlépések
1. `GET /api/v1/audit-log` → listaoldal alapértelmezett paraméterekkel
2. Ellenőrzi a válasz struktúráját (lapozható lista)
3. `GET /api/v1/audit-log?action=Login` → szűrt lista
4. `GET /api/v1/audit-log/security-events` → biztonsági esemény szűrő

## Elvárások / Assertions
- HTTP 200 mindhárom kérésnél
- Valid JSON válasz, `items` tömb megléte
- A lap méret és szám benne van a válaszban
- Non-Admin userrel meghívva: 403 Forbidden

## Megjegyzés
**Teszttípus: API**
Az audit log exportálás (`/export`) külön tesztet igényelne — ez a scenario
csak az alap lekérdezést fedi le.

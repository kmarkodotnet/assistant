# UC13 — Értesítési feed kezelése (notifications)

## Leírás
Teszteli az értesítési feed lekérdezését és az olvasottnak jelölés funkcióját.

## Előfeltételek
- Bejelentkezett felhasználó (bármely szerepkör)

## Tesztlépések
1. `GET /api/v1/notifications` → értesítések listázása
2. Ellenőrzi a válasz struktúráját: `items`, `totalCount`, `hasMore`
3. Ha vannak olvasatlan értesítések:
   - `POST /api/v1/notifications/{id}/read` → 200 vagy 204
4. `GET /api/v1/notifications?unreadOnly=true` → csak olvasatlanok

## Elvárások / Assertions
- HTTP 200 a listázásnál
- Valid JSON struktúra `items` tömbbel
- Olvasottnak jelölés sikeres (ha endpoint létezik)
- Az olvasottnak jelölt értesítés eltűnik az unread szűrőből

## Megjegyzés
**Teszttípus: API**
Az értesítések szerver-oldali push SignalR-en keresztül is érkeznek,
de az API polling is támogatott. A teszt csak az API réteget teszteli.

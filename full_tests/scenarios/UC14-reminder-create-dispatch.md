# UC14 — Emlékeztető (Reminder) létrehozás + dispatching

## Leírás
Teszteli az emlékeztetők CRUD funkcióját. Az automatikus küldés (dispatch)
nem tesztelhető szinkron módon, ezért csak a rekord létrehozása és listázása
kerül ellenőrzésre.

## Előfeltételek
- Bejelentkezett felhasználó (Adult vagy Admin szerepkör)

## Tesztlépések
1. `POST /api/v1/reminders` body:
   ```json
   {
     "title": "E2E Emlékeztető",
     "remindAtUtc": "2027-06-01T09:00:00Z",
     "channel": "InApp"
   }
   ```
   → 201, visszakapott `id`
2. `GET /api/v1/reminders` → az emlékeztető megjelenik
3. `GET /api/v1/reminders/{id}` → valid részletek
4. `DELETE /api/v1/reminders/{id}` → 204

## Elvárások / Assertions
- 201 a létrehozásnál
- A reminder megjelenik a listában `Pending` státusszal
- DELETE után 204

## Megjegyzés
**Teszttípus: API**
A dispatch (tényleges küldés) worker-oldali folyamat, amely
`remindAtUtc` elérése után fut — ezt az E2E teszt nem várja meg.
A `channel` mező értékei: `InApp`, `Email`.

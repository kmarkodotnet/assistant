# UC02 — Dokumentum keresése (FTS + szemantikus)

## Leírás
Teszteli a kétféle keresési módot: szöveg alapú teljes szöveges keresés (Text FTS)
és szemantikus/vektoros keresés (Semantic). Mindkét módnak valid JSON választ kell
visszaadnia, és nem szabad 500-as hibával elszállnia.

## Előfeltételek
- Bejelentkezett felhasználó (bármely szerepkör)
- Legalább 1 feldolgozott (`processingStatus === 'Done'`) dokumentum az adatbázisban
  (UC01 lefutása után teljesített)

## Tesztlépések
1. `POST /api/v1/search` body: `{ "query": "teszt dokumentum", "mode": "Text" }`
2. Ellenőrzi a 200 OK választ és a válasz struktúráját
3. `POST /api/v1/search` body: `{ "query": "teszt", "mode": "Semantic" }`
4. Ellenőrzi a 200 OK választ (szemantikus keresés nem crashol)
5. (UI teszt) Navigál `/search` oldalra, beír "teszt" szöveget, Submit → nincs 500

## Elvárások / Assertions
- HTTP 200 mindkét keresési módnál
- Válasz JSON tartalmaz `hits` (vagy `items`) tömböt
- A tömbnek `>= 0` eleme van (üres eredmény is valid, crashelés nem)
- UI oldalon nem jelenik meg hibaüzenet / 500 error oldal

## Megjegyzés
**Teszttípus: API / Integráció / UI**
Szemantikus keresés Ollama embedding-ektől függ — ha az embedding nem futott le,
a szemantikus keresés üres eredményt ad vissza (és nem hibát).

# UC10 — Dokumentum letöltése (content endpoint)

## Leírás
Teszteli, hogy egy feltöltött dokumentum tartalma letölthető a `/content` endpointon
keresztül, és a válasz helyes MIME-type-ot és fájlnevet tartalmaz.

## Előfeltételek
- Bejelentkezett felhasználó (bármely szerepkör)
- Legalább egy feltöltött dokumentum az adatbázisban

## Tesztlépések
1. Feltölt egy dokumentumot (vagy UC01 eredményét használja): `POST /api/v1/documents`
2. Lekéri a dokumentum részleteit: `GET /api/v1/documents/{id}`
3. Letölti a tartalmat: `GET /api/v1/documents/{id}/content`
4. Ellenőrzi a válasz fejléceket és tartalmát

## Elvárások / Assertions
- `GET /content` → HTTP 200
- `Content-Type` fejléc nem üres
- `Content-Disposition` fejléc tartalmaz `filename` értéket
- A response body nem üres (bináris tartalom)

## Megjegyzés
**Teszttípus: API / Integráció**
A letöltés a MinIO/S3 storage-ból streameli a tartalmat.
A teszt API-szinten ellenőriz (nem böngészős letöltési flow).

# UC06 — Dokumentum újrafeldolgozása (reprocess)

## Leírás
Teszteli, hogy egy már feldolgozott dokumentum újrafeldolgozása helyesen indítja
újra az ExtractText AI job-ot, és a dokumentum `processingStatus`-a visszaáll
`Pending`-re, majd ismét `Done`-ra kerül.

## Előfeltételek
- Bejelentkezett felhasználó (Adult vagy Admin szerepkör)
- Legalább egy `Done` státuszú dokumentum az adatbázisban (UC01 lefutása után)

## Tesztlépések
1. Feltölt egy dokumentumot (vagy UC01 eredményét használja)
2. Megvárja, hogy `processingStatus === 'Done'` (max 90s)
3. `POST /api/v1/documents/{id}/reprocess` body: `{}` → 200
4. `GET /api/v1/documents/{id}` → `processingStatus` visszaállt `Pending`-re (vagy azonnal `Processing`)
5. Pollozza az állapotot amíg ismét `Done` nem lesz (max 90s)

## Elvárások / Assertions
- `POST /reprocess` → HTTP 200
- A `processingStatus` visszaáll nem-Done értékre a reprocess után
- A dokumentum ismét `Done` státuszba kerül a pipeline lefutása után

## Megjegyzés
**Teszttípus: Integráció / AI-pipeline**
A reprocess endpoint `jobs` paraméterrel korlátozható (pl. csak ExtractText),
de az alap tesztnél üres `jobs` lista → minden job újraindul.
Timeout: 90 másodperc a pipeline újrafutáshoz.

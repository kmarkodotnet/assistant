# UC01 — Dokumentum feltöltése + AI pipeline

## Leírás
Teszteli, hogy egy feltöltött dokumentum végigmegy az AI feldolgozási pipeline-on:
ExtractText → DetectLanguage → Summarize → Embed. A pipeline végén a dokumentum
`processingStatus` értéke `Done` legyen, és a kinyert szöveg elérhető legyen.

## Előfeltételek
- Bejelentkezett felhasználó (Admin vagy Adult szerepkör)
- Az Ollama worker fut és a modellek elérhetők
- A feltöltendő fájl nem duplikátum (egyedi SHA256)

## Tesztlépések
1. `POST /api/v1/documents` multipart/form-data kéréssel feltölt egy `.txt` fájlt
2. Ellenőrzi, hogy a válasz 201 Created és tartalmaz egy document `id`-t
3. Pollozza a `GET /api/v1/documents/{id}` endpointot 3 másodpercenként, max 90 másodpercig
4. Megvárja, hogy `processingStatus` értéke `Done` (vagy `Failed`) legyen
5. `GET /api/v1/documents/{id}/text` — ellenőrzi, hogy a `content` nem üres
6. (UC07 ráépítés) Ugyanazt a fájlt újra feltölti → 409 Conflict várható

## Elvárások / Assertions
- HTTP 201 a feltöltés után, valid JSON body `id` mezővel
- `processingStatus === 'Done'` 90 másodpercen belül
- `GET /text` válaszban `content.length > 0`
- Duplikált feltöltésnél HTTP 409

## Megjegyzés
**Teszttípus: Integráció / AI-pipeline**
Ez a legkritikusabb teszt — valódi Ollama worker futást igényel.
Timeout értéke magasabb (90s), mert az AI modellek első betöltése lassabb lehet.

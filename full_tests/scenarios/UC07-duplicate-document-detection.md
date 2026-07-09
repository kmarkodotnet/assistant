# UC07 — Duplikált dokumentum észlelése (SHA256)

## Leírás
Teszteli, hogy a rendszer SHA256 hash alapján felismeri és elutasítja a duplikált
dokumentum feltöltéseket.

## Előfeltételek
- Bejelentkezett felhasználó (Adult vagy Admin szerepkör)
- Egy azonos tartalmú fájl már fel van töltve (UC01 lefutása után)

## Tesztlépések
1. Feltölt egy dokumentumot (`sample.txt`) → 201 (első feltöltés)
2. Ugyanazt a fájlt újra feltölti:
   `POST /api/v1/documents` ugyanazzal a fájltartalommal
3. Ellenőrzi a választ

## Elvárások / Assertions
- Az első feltöltés: HTTP 201
- A második (duplikált) feltöltés: HTTP 409 Conflict
- A 409 válasz tartalmaz hibaüzenetet (ProblemDetails formátumban)

## Megjegyzés
**Teszttípus: API / Integráció**
A duplikáció detekció a feltöltéskor számított SHA256 hash alapján történik.
Az UC01 spec-ben ez az UC07 ráépítésként szerepel.

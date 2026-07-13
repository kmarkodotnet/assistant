# ELAVULT — ne implementáld

> Az eredeti `docs/fable_review/09-telepitheto-termek-terv.md` egy 67 soros,
> mondat közben megszakadó VÁZLAT (a „### D4 —" fejlécnél ér véget).
> Tartalmát a teljes, kidolgozott terv váltotta fel.

**A végrehajtási igazságforrás:**
[09-hordozhato-telepitokeszlet.md](09-hordozhato-telepitokeszlet.md) (L0–L8 kártyák).

A csonkban felvetett témák mind ott (vagy a kapcsolódó kártyákon) élnek tovább:

| Csonk-tétel | Hol él tovább |
|---|---|
| B1–B10 akadály-leltár | 09-hordozható 1. fejezet (P1–P5) |
| D1 Docker-alapú telepítő (ADR-0017) | 09-hordozható zárt döntések |
| D2 helyi-jelszó auth (`Auth:Mode`) | 09-hordozható **L6** = cr260712-09 |
| D3 webes first-run wizard | részben L5 (telepítő-varázsló); a runtime-settings rész: 02/C-A10 + backlog |
| B6 `PATCH /settings/system` no-op | 02/C-A10 kártya |
| B7 `system/version` hiánya | 02/C-A3 kártya |

**Utasítás subagentnek:** ha ezt a fájlt kaptad feladatként, NE csinálj
semmit — jelezd az orchestrátornak, hogy a 09-hordozhato-telepitokeszlet.md
kártyáit kell futtatni.

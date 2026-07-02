# Review — README.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

A README az „AI Software Factory” meta-rendszert írja le (a CLAUDE.md-vel
összhangban), tömör és jól olvasható. **A fő probléma: a leírt
infrastruktúra jelentős része nem létezik a repóban** — a README jelenleg
célállapotot dokumentál, nem a valóságot, és ezt sehol nem jelzi.

## Hibák

### 1. Nem létező komponensekre hivatkozik (súlyos)
Ellenőrizve a repó tartalmán (2026-07-02):

| README-állítás | Valóság |
|---|---|
| `.claude/agents — 9 specializált agent` | `.claude/` alatt csak `settings.json` és `settings.local.json` van |
| `.claude/commands — slash parancsok` (`/build-product`, `/discover`, `/operate`, `/retro`, `/qa`, `/status`…) | nincs `commands` könyvtár, egyik parancs sem létezik |
| `.claude/skills — stack-szabványok` | nem létezik |
| `docs/requirements/<termek>.md` munkafolyamat | nincs `docs/requirements/` könyvtár |
| `docs/retros/`, `factory-metrics/runs/` | nem léteznek |
| `docs/DELIVERY.md` mint gyártási output | létezik, de a Family OS-hez tartozik, nem a factory outputja |

A `scripts/` viszont valóban tartalmazza a hivatkozott scripteket
(`new-feature.sh`, `log-run.sh`, `metrics-summary.sh` stb.) — ez a rész rendben.

**Javaslat:** vagy (a) jelezni a README elején, hogy ez a factory
*terv/target* állapota és mi van készen, vagy (b) létrehozni a hiányzó
`.claude/agents` + `commands` struktúrát, mielőtt bárki a 3 lépéses
„Használat” szerint próbálja indítani — jelenleg a `/build-product` hívás
azonnal elhasal.

### 2. A repó tényleges tartalma nincs megemlítve (közepes)
A repóban ott van egy teljes Family OS tervezési dokumentáció (`docs/`,
`frontend/`), de a README egy szót sem szól róla. Aki a repót megnyitja,
nem tudja meg, hogy az első „termék” (Family OS) tervezési fázisban van.
Egy rövid „Aktuális állapot / folyamatban lévő termék” szakasz hiányzik.

### 3. Windows-környezet vs. bash scriptek (kicsi)
A repo Windows-on él (D:\Claude\Assistant), a scriptek `.sh`-k, a cron-os
példa (`0 * * * * cd /path/...`) unixos. Ha a futtatókörnyezet valóban
Windows, kell egy megjegyzés (WSL? Git Bash?), különben az útmutató nem
reprodukálható.

## Verdikt

Tartalmilag jó vízió-README, de dokumentál nem létező funkciókat, és nem
dokumentálja a létezőket. A „Használat (3 lépés)” jelenleg nem működik.

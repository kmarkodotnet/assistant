# Review — deploy-raspberry-pi.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)
> Megjegyzés: a doksi frissen (2026-07-02) készült; a hivatkozott Makefile
> targetek (`rpi-buildx-setup`, `rpi-build`, `rpi-save`, `rpi-push`,
> `rpi-up`, `rpi-down`) és a `docker-compose.rpi.yml` létezését a repóban
> ellenőriztem — megvannak.

## Összegzés

Jó kiegészítő útmutató: arm64-kompatibilitási tábla (dátumozott
ellenőrzéssel), reális build-stratégia (cross-build a fejlesztői gépen),
cgroup-kernelparaméter csapda, SD-kártya vs. SSD figyelmeztetés,
Pi-specifikus troubleshooting (OOM, throttling, exec format error).
A DELIVERY.md-vel jól komplementer, nem duplikál.

## Észrevételek

### 1. A modell-alapértelmezés állítása nem egyezik a kóddal (közepes)
Az 5. szakasz szerint „a `docker-compose.yml`-ben szereplő alapértelmezett
modellek (pl. `gpt-oss:20b`) több tíz GB RAM-ot igényelnek” — a kód
tényleges defaultja viszont **`llama3.2:3b`**
(`OllamaOptions.DefaultModel`), ami egy Pi 5 (8 GB)-on elfut. A doksi a
tervezési doksik (gpt-oss:20b) és a kód (llama3.2:3b) közti divergenciát
örökli (lásd DELIVERY.review.md #2). A modell-döntés ADR-ben való
lezárása után ez a szakasz egyszerűsödik. Mellékesen: a `gpt-oss:20b`
igénye ~13–16 GB, nem „több tíz GB” — a nagyságrendi állítás pontosítandó.

### 2. `qwen2.5:1.5b` ajánlás minőségi kockázata (kicsi)
Az 1. opció kis modellje magyar nyelvű összefoglalásra/kinyerésre
várhatóan gyenge (a product-vision Q&A pontossági metrikáját — 8/10 —
aligha hozza). Érdemes odaírni, hogy az 1. opció degradált minőségű
üzemmód, és az érdemi AI-hoz a 2. opció (Ollama erősebb gépen) az
ajánlott út — a szöveg ezt sugallja („ajánlott”), de a minőség-trade-off
nincs kimondva, csak a sebesség.

### 3. A 2. opció és a „PC nem mindig fent” feltevés (megjegyzés — pozitív)
Az Ollama külön gépre helyezése pontosan illeszkedik a product-vision
hardver-feltevéséhez (a fő PC nem 24/7): a Pi mindig fut, a durable
queue pedig akkor dolgozza fel az AI-jobokat, amikor az Ollama-gép
elérhető. Ezt az összefüggést érdemes egy mondatban explicitté tenni —
most csak az ár/érték érv szerepel.

### 4. Apróságok
- 1. szakasz: `dotnet/sdk:8.0` — a .NET-verzió kérdés (lásd
  idea.review.md #1) ezt a táblát is érinti majd.
- 7. szakasz: `docker inspect family-os-api:arm64-latest` — a tag-séma
  (`arm64-latest`) itt jelenik meg először; egy sor a 4b-ben a
  tag-konvencióról (mit állít elő a `make rpi-build`) hasznos lenne.
- 6. szakasz: jó, hogy a `--no-build` kötelezőségét megindokolja.

## Verdikt

Kiadható; az #1 modell-állítás igazítása a tényleges compose/kód
defaulthoz az egyetlen érdemi teendő.

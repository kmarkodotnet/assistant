# Family OS telepítés Raspberry Pi-n

> Státusz: v0.2 · Dátum: 2026-07-02 · Nyelv: magyar
> Kiegészíti: [DELIVERY.md](DELIVERY.md) (OAuth, backup, TLS, incidenskezelés —
> ott van részletesen, itt nincs duplikálva)

Ez a dokumentum azt írja le, hogyan futtasd a Family OS-t egy Raspberry
Pi-n. A cél egy alacsony fogyasztású, mindig bekapcsolva hagyható háztartási
szerver — összhangban az [ADR-0003](decisions/ADR-0003-mobil-csak-lan.md)
LAN-only elvével.

## 0. Melyik topológia illik hozzád?

A Family OS 5 Docker service-ből áll: `api`, `workers`, `web` (.NET +
Angular/nginx — **32-bit Pi-n is futtathatók**), valamint `postgres`
(pgvector-ral) és `ollama` (**csak 64-bit-en futtathatók** — nincs 32-bit
image-ük). Két support-olt elrendezés van:

| | **A) Osztott** (AJÁNLOTT, ha van egy erős, gyakran bekapcsolt PC-d) | **B) Minden a Pi-n** |
|---|---|---|
| Pi szerepe | `api` + `workers` + `web` | `api` + `workers` + `web` + `postgres` + `ollama` |
| Erős PC szerepe | `postgres` + `ollama` | — (nincs rá szükség) |
| Pi OS | **32-bit VAGY 64-bit** — mindkettő jó | **kizárólag 64-bit** |
| Ollama teljesítmény | jó (az erős gépen fut) | gyenge (lásd 6. szakasz) |
| Hálózati függés | a Pi-nek mindig el kell érnie az erős PC-t | nincs külső függés |

**Ha az Ollama már amúgy is egy erős PC-n fut** (ahogy a te esetedben),
egyértelműen az **A) Osztott** topológia az ajánlott — ez teszi lehetővé,
hogy a Pi 32-bit OS-en maradhasson, és egyben ez adja a jobb AI-teljesítményt
is. Ez a dokumentum ezt az elrendezést részletezi elsődlegesen, a B) opciót
a 2. és 6. szakasz jelöléssel tér ki rá.

## 1. Kompatibilitás — mit ellenőriztünk

`docker buildx imagetools inspect <image>` paranccsal ellenőrizve
(2026-07-02):

| Image | linux/arm64 | linux/arm/v7 (32-bit) | Melyik service |
|---|---|---|---|
| `mcr.microsoft.com/dotnet/sdk:8.0` | ✅ | ✅ | api, workers (build stage) |
| `mcr.microsoft.com/dotnet/aspnet:8.0` | ✅ | ✅ | api, workers (runtime) |
| `node:22-alpine` | ✅ | ✅ | web (build stage) |
| `nginx:1.25-alpine` | ✅ | ✅ | web (runtime) |
| `pgvector/pgvector:pg16` | ✅ | ❌ **nincs 32-bit** | postgres |
| `ollama/ollama` | ✅ | ❌ **nincs 32-bit** | ollama |
| `alpine:3.19` | ✅ | ✅ | backup (opcionális) |

Ez a táblázat pontosan megmagyarázza a 0. szakasz döntési logikáját: az
`api`/`workers`/`web` bármelyik architektúrán fut, a `postgres`/`ollama`
csak 64-biten.

## 2. Hardver- és OS-választás

### A) Osztott topológia (32-bit vagy 64-bit Pi + erős PC)

| Komponens | Ajánlott |
|---|---|
| Pi modell | Raspberry Pi 3/4/5 vagy Zero 2 W (32- vagy 64-bit OS, bármelyik) |
| Pi tárhely | USB 3.0 SSD, vagy 32 GB+ A2 microSD (kisebb terhelés, mert a DB nincs rajta) |
| Erős PC | bármi, ami amúgy is fut Ollamával (a te fő géped) |
| Hálózat | Ethernet mindkét gépen (a Pi ↔ erős PC forgalom stabilitása miatt) |

### B) Minden a Pi-n (csak akkor, ha nincs külön erős géped)

| Komponens | Ajánlott | Minimum |
|---|---|---|
| Modell | Raspberry Pi 5 (8 GB) | Raspberry Pi 4 (4 GB) |
| Tárhely | USB 3.0 SSD (NVMe HAT-tal még jobb) | 64 GB A2 microSD |
| OS | **kizárólag 64-bit** (Raspberry Pi OS Lite, Bookworm) | Ubuntu Server 24.04 LTS arm64 |

### Ellenőrzés: 32-bit vagy 64-bit fut a Pi-den?

```bash
uname -m
# aarch64       → 64-bit OS
# armv7l/armv6l → 32-bit OS
```

**A) Osztott topológiánál mindkét eredmény jó, nincs teendő.**

**B) Minden-a-Pi-n topológiánál 64-bit kötelező.** Ha 32-bit-et kaptál
vissza, újra kell flash-elni az SD-kártyát/SSD-t **Raspberry Pi Imager**-rel
— a **"Raspberry Pi OS Lite (64-bit)"** opciót válaszd, NEM a "(Legacy,
32-bit)"-et. A Pi 3/4/5 és a Zero 2 W hardvere (Cortex-A53 vagy újabb) mind
64-bit-képes, csak az OS-t kell cserélni (32→64 között nincs helyben
frissítés).

**SD kártya vs. SSD:** ha a Pi-n fut a Postgres is (B topológia), az sok
kis írást generál, ami microSD-n néhány hónap alatt komoly
teljesítményromlást/korai meghibásodást okozhat — használj USB SSD-t. Az A)
topológiánál a Pi-n csak a dokumentum-fájlok (nem a DB) írásigénye van,
microSD is elfogadhatóbb, de SSD itt is jobb hosszú távon.

### cgroup memória — kötelező kernel-paraméter

A Raspberry Pi OS alapból **nem** engedélyezi a memória-cgroupot, ami miatt
a Docker `mem_limit` és a konténerek memóriakorlátozása nem működik.
Ellenőrizd:

```bash
docker info | grep -i cgroup
# Ha ezt látod: "WARNING: No memory limit support" — hiányzik a beállítás
```

Javítás — szerkeszd a `/boot/firmware/cmdline.txt` fájlt (Raspberry Pi OS
Bookworm) vagy a `/boot/cmdline.txt`-t (régebbi verziók), és fűzd hozzá a
sor **végéhez** (egy sorban kell maradnia, szóközzel elválasztva):

```
cgroup_enable=memory cgroup_memory=1
```

Majd `sudo reboot`. (Ugyanezt az erős PC-n is érdemes ellenőrizni, ha az
is Linux/arm vagy Linux/amd64 gépen fut Dockerrel — Windows/macOS Docker
Desktopnál ez nem releváns, azok VM-en belül már helyesen konfiguráltak.)

## 3. Docker telepítése

**A Pi-n** (és a Linux erős PC-n, ha van ilyen):
```bash
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER
# jelentkezz ki/be, hogy a csoporttagság érvénybe lépjen
docker compose version   # ellenőrzés — v2.20+ kell
```

Windows/macOS erős PC-n: Docker Desktop (ez már tartalmazza a buildx-et és
a QEMU cross-platform emulációt, nincs külön teendő).

## 4. Build-stratégia — miért NE a Pi-n buildelj

A `dotnet publish` (3 backend projekt) + az `npm run build` (Angular,
production móddal) egy erős PC-n is percekig tart — egy Raspberry Pi 4/5
CPU-ján ez **15-30 percet** vehet igénybe *image-enként* natív buildnél is
(nem az architektúra az akadály, hanem a nyers teljesítmény).

**Javasolt megoldás: cross-build a fejlesztői/erős gépeden, majd az
elkészült image-ek átvitele a Pi-re.** Ehhez a repo tartalmaz Makefile
targeteket és egy `docker-compose.rpi.yml` felülbírálást.

### 4a. Egyszeri előkészítés a build-gépen

```bash
make rpi-buildx-setup
```

Ez létrehoz egy `docker-container` driverű buildx buildert QEMU
emulációval. Windows/macOS Docker Desktopon nincs külön teendő, natívan
tartalmazza a szükséges binfmt-handlereket.

### 4b. Platform kiválasztása — igazítsd a Pi architektúrájához

```bash
uname -m   # ezt futtasd a Pi-n, hogy tudd, melyik kell
```

| Pi eredménye | Build parancs |
|---|---|
| `aarch64` (64-bit) | `make rpi-build` *(alapértelmezett: `RPI_PLATFORM=linux/arm64 RPI_TAG=arm64-latest`)* |
| `armv7l`/`armv6l` (32-bit) | `make rpi-build RPI_PLATFORM=linux/arm/v7 RPI_TAG=armv7-latest` |

### 4c. Image-ek átvitele — két opció

**Opció A — helyi export, nincs szükség registry-fiókra** (illik a
LAN-only, privacy-first filozófiához, lásd ADR-0003):

```bash
make rpi-save RPI_PLATFORM=linux/arm/v7 RPI_TAG=armv7-latest
# → dist/rpi/family-os-images-armv7-latest.tar.gz

scp dist/rpi/family-os-images-armv7-latest.tar.gz pi@<pi-ip>:~/
ssh pi@<pi-ip>
gunzip -c family-os-images-armv7-latest.tar.gz | docker load
```

**Opció B — push egy container registry-be** (docker.io, ghcr.io, vagy egy
saját LAN-on belüli registry):

```bash
docker login ghcr.io   # vagy docker.io
make rpi-push RPI_PLATFORM=linux/arm/v7 RPI_TAG=armv7-latest RPI_REGISTRY=ghcr.io/felhasznalonev/
```

A Pi-n ez esetben a `.env`-ben állítsd be:
```
RPI_IMAGE_PREFIX=ghcr.io/felhasznalonev/
RPI_TAG=armv7-latest
```

### 4d. Alternatíva: natív build a Pi-n

Ha mégis a Pi-n akarsz buildelni, a meglévő `docker-compose.yml` `build:`
szakaszai változtatás nélkül működnek (32-bit Pi-n is, hiszen az
api/workers/web bázis-image-ei 32-biten is elérhetők) — csak legyél
türelmes:

```bash
docker compose build api workers web   # ~30-60 perc a 3 image-hez, Pi 4-en
```

## 5. Az erős PC oldala — postgres + ollama közzététele a LAN-on

Az alap `docker-compose.yml`-ben a `postgres` és `ollama` service **nem**
publikál portot a hosztra (csak a compose-belső Docker hálózaton érhető el
más konténerekből, mivel eredetileg egy-gépes elrendezésre lett tervezve).
Az osztott topológiához fel kell nyitni őket a LAN-ra — ehhez való a
`docker-compose.strong-pc.yml` felülbírálás:

```bash
# az erős PC-n, a repo gyökeréből:
cp .env.example .env   # ha még nincs
# töltsd ki a .env-et (POSTGRES_PASSWORD, APP_DB_PASSWORD stb. — lásd DELIVERY.md 2b.)

make strong-pc-up
# ekvivalens: docker compose -f docker-compose.yml -f docker-compose.strong-pc.yml up -d --no-deps postgres ollama
```

Ez elindítja a `postgres`-t az `5432`-es, az `ollama`-t a `11434`-es porton,
a gép LAN-interfészén elérhetően. Jegyezd fel ennek a gépnek a LAN IP-jét
(pl. `192.168.1.50`) — ez kell a Pi oldali `.env`-be (7. szakasz).

> **Tűzfal:** ha az erős PC-n fut valamilyen tűzfal (Windows Defender
> Firewall, ufw), engedélyezd az 5432 és 11434 portokat a LAN alhálózatról
> (ne nyisd meg őket a teljes internet felé — ez ellentmondana az
> ADR-0003 LAN-only elvének).

## 6. Ollama modell-méretezés

Ha Ollama az erős PC-n fut (A topológia), a modellválasztás ugyanúgy
történik, mint egy normál asztali telepítésnél — nincs Pi-specifikus
korlátozás, lásd `docs/ai-pipeline.md`.

Ha mégis a Pi-n futtatnád az Ollamát (B topológia): a rendszer
alapértelmezett modellje a **`llama3.2:3b`** (ADR-0006), ami ~3–4 GB
RAM-mal egy **Pi 5 (8 GB)**-on elfér; a jobb minőségű `gpt-oss:20b`
opció ~13–16 GB RAM-ot igényel, Pi-n kizárt. 4 GB-os Pi-n még kisebb
modell kell (minőség-áldozattal):

```bash
docker compose exec ollama ollama pull llama3.2:3b     # default (Pi 5, 8 GB)
docker compose exec ollama ollama pull qwen2.5:1.5b    # 4 GB-os Pi-re
docker compose exec ollama ollama pull nomic-embed-text
```

Számíts rá, hogy egy dokumentum-összefoglalás **10-60 másodpercig** is
eltarthat egy kis modellel Pi-n (GPU nélkül), szemben egy asztali gép
1-5 másodpercével. Ez a fő oka annak, hogy az A) topológiát ajánljuk.

## 7. Indítás

### A) Osztott topológia

**1. Az erős PC-n** (ha még nem tetted meg): lásd 5. szakasz (`make strong-pc-up`).

**2. A Pi-n:**

```bash
git clone <repo-url> family-os
cd family-os
cp .env.example .env
```

Egészítsd ki a `.env`-et (a DELIVERY.md 2b. szakaszában leírtak mellett):

```
# ugyanaz a jelszó, mint amit az erős PC .env-jében APP_DB_PASSWORD-nek beállítottál:
APP_DB_PASSWORD=<ugyanaz-mint-az-eros-gepen>

# az erős PC LAN IP-je:
DB_HOST=192.168.1.50
OLLAMA_HOST=192.168.1.50

# ha helyi docker load-dal töltötted be az image-eket:
RPI_TAG=armv7-latest   # vagy arm64-latest, a Pi architektúrája szerint
```

```bash
make init-tls   # vagy: ./scripts/init-tls-ca.sh family-os.lan

make rpi-up
# ekvivalens: docker compose -f docker-compose.yml -f docker-compose.rpi.yml \
#             up -d --no-build --no-deps api workers web
```

A `--no-deps` fontos: enélkül Compose megpróbálná a (Pi-n nem létező,
mert az erős PC-n futó) `postgres`/`ollama` service-t is helyben
elindítani, mert az `api` service `depends_on`-ban hivatkozik rájuk.

### B) Minden a Pi-n

```bash
git clone <repo-url> family-os
cd family-os
cp .env.example .env
# töltsd ki a .env-et (lásd DELIVERY.md 2b. szakasz)

make init-tls
make rpi-up-all
# ekvivalens: docker compose -f docker-compose.yml -f docker-compose.rpi.yml up -d --no-build
```

### Mindkét esetben utána

A [DELIVERY.md](DELIVERY.md) 2c–3d szakaszai (TLS-CA telepítés az
eszközökre, Google OAuth, első bejelentkezés) változtatás nélkül
alkalmazhatók.

## 8. Ellenőrzés

**A Pi-n:**
```bash
docker compose -f docker-compose.yml -f docker-compose.rpi.yml ps
curl -k https://localhost/healthz/ready

# arch-ellenőrzés — ha "exec format error"-t kapsz, rossz architektúrájú
# image-et töltöttél be:
docker inspect family-os-api:${RPI_TAG:-arm64-latest} --format '{{.Architecture}}'
# várt: arm vagy arm64, a Pi-nek megfelelően
```

**Osztott topológia esetén az erős PC-n is:**
```bash
docker compose -f docker-compose.yml -f docker-compose.strong-pc.yml ps
curl http://localhost:5432   # kapcsolat-teszt (hibaüzenetet ad, de mutatja, hogy a port nyitva van)
curl http://localhost:11434/api/tags
```

**A Pi-ről elérhető-e az erős PC?**
```bash
# a Pi-n:
nc -zv $DB_HOST 5432
nc -zv $OLLAMA_HOST 11434
```

## 9. RPi-specifikus hibaelhárítás

**`exec format error` egy konténer indításakor**
→ rossz architektúrájú image került a Pi-re (pl. `linux/arm64`-et
buildeltél 32-bit Pi-hez, vagy fordítva). Nézd meg `uname -m`-mel, melyik
kell (4b. szakasz táblázat), és buildelj újra a helyes `RPI_PLATFORM`-mal.

**Az `api`/`workers` nem tud csatlakozni a Postgres-hez/Ollamához
(osztott topológia)**
→ ellenőrizd: `DB_HOST`/`OLLAMA_HOST` helyesen van-e kitöltve a Pi `.env`-jében;
az erős PC-n fut-e a `strong-pc` override (`docker compose ... -f
docker-compose.strong-pc.yml ps`); a két gép ugyanazon a LAN-on van-e
(`ping <eros-pc-ip>` a Pi-ről); tűzfal nem blokkolja-e az 5432/11434 portot.

**A konténer folyamatosan újraindul (`OOMKilled`) — csak B) topológiánál**
→ `docker compose logs <service>` és `dmesg | grep -i oom`. Tipikusan az
`ollama` fut ki a memóriából nagyobb modellel — válts kisebb modellre
(6. szakasz), vagy térj át A) topológiára.

**Nagyon lassú I/O, magas `iowait`**
→ `vmstat 1` — ha `wa` oszlop tartósan magas, valószínűleg SD kártyáról
futsz. Költözz USB SSD-re (2. szakasz).

**Hőmérséklet-throttling**
```bash
vcgencmd measure_temp
vcgencmd get_throttled
# 0x0 = nincs throttling; bármi más érték jelzi, hogy a Pi korlátozta magát
```

**`docker buildx build --platform ...` nagyon lassú a build-gépen
(QEMU-emuláció)**
→ ez várható, ha a build-géped más architektúrájú, mint a cél (pl. amd64
gépről arm/v7-re fordítasz); a 3 image cross-buildje összesen 10-20 percet
is igénybe vehet. Ha ez zavaró, fontold meg egy natív arm buildx-buildert
(pl. egy felhő-alapú arm runner, vagy magán a Pi-n futtatott
`docker buildx create --append` remote node).

## 10. Frissítés

Ugyanaz, mint a [DELIVERY.md 6. szakaszában](DELIVERY.md#6-frissítés), egy
különbséggel: a `docker compose build` helyett újra a cross-build +
átvitel workflow-t kell lefuttatni (4. szakasz), majd a Pi-n:

```bash
git pull
gunzip -c family-os-images-<uj-tag>.tar.gz | docker load
make rpi-up          # A) osztott topológia
# vagy: make rpi-up-all   # B) minden a Pi-n
```

Osztott topológiánál az erős PC oldalán (`postgres`/`ollama` image-ek,
ha azok is frissültek):
```bash
docker compose -f docker-compose.yml -f docker-compose.strong-pc.yml pull
make strong-pc-up
```

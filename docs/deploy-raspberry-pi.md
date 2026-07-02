# Family OS telepítés Raspberry Pi-n (arm64)

> Státusz: v0.1 · Dátum: 2026-07-02 · Nyelv: magyar
> Kiegészíti: [DELIVERY.md](DELIVERY.md) (OAuth, backup, TLS, incidenskezelés —
> ott van részletesen, itt nincs duplikálva)

Ez a dokumentum azt írja le, hogyan futtasd a Family OS teljes Docker Compose
stackjét (`api`, `workers`, `web`, `postgres`, `ollama`) egy Raspberry Pi-n.
A cél egy alacsony fogyasztású, mindig bekapcsolva hagyható háztartási
szerver — összhangban az [ADR-0003](decisions/ADR-0003-mobil-csak-lan.md)
LAN-only elvével.

## 1. Kompatibilitás — mit ellenőriztünk

A stack minden image-e publikál `linux/arm64` variánst
(`docker buildx imagetools inspect <image>` paranccsal ellenőrizve,
2026-07-02):

| Image | arm64 | Megjegyzés |
|---|---|---|
| `mcr.microsoft.com/dotnet/sdk:8.0` | ✅ | build stage |
| `mcr.microsoft.com/dotnet/aspnet:8.0` | ✅ | runtime (api, workers) |
| `node:22-alpine` | ✅ | frontend build stage |
| `nginx:1.25-alpine` | ✅ | web runtime |
| `pgvector/pgvector:pg16` | ✅ | postgres |
| `ollama/ollama` | ✅ | lokális AI |
| `alpine:3.19` | ✅ | backup service |

Tehát a stack **natívan** fut arm64-en, nincs szükség emulációra a Pi-n
futás közben (csak a *build* fázisban, ha nem natívan cross-buildeled —
lásd 4. szakasz).

## 2. Hardver- és OS-választás

### Ajánlott konfiguráció

| Komponens | Ajánlott | Minimum |
|---|---|---|
| Modell | Raspberry Pi 5 (8 GB) | Raspberry Pi 4 (4 GB) |
| Tárhely | USB 3.0 SSD (NVMe HAT-tal még jobb) | 64 GB A2 microSD |
| OS | Raspberry Pi OS Lite (64-bit, Bookworm) | Ubuntu Server 24.04 LTS arm64 |
| Hálózat | Ethernet | Wi-Fi (elfogadható, de Ethernet stabilabb) |

**Fontos: kizárólag 64-bit OS-t telepíts.** A `linux/arm/v7` (32-bit) image-ek
ugyan léteznek a .NET-hez, de a `pgvector` és az `ollama` image-eknek
**nincs** 32-bit variánsuk (lásd fenti táblázat) — 32-bit OS-en a stack nem
indul el.

**SD kártya vs. SSD:** a Postgres + a dokumentum-tárolás sok kis írást
generál. Egy sima microSD kártyán ez néhány hónap alatt komoly
teljesítményromlást és korai meghibásodást okozhat. USB 3.0 SSD-ről
futtatva a Pi-t (boot is onnan) sokkal megbízhatóbb hosszú távra.

### cgroup memória — kötelező kernel-paraméter

A Raspberry Pi OS alapból **nem** engedélyezi a memória-cgroupot, ami miatt
a Docker `mem_limit` és a konténerek memóriakorlátozása nem működik, és
bizonyos Docker verziók el sem indulnak megfelelően. Ellenőrizd:

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

Majd `sudo reboot`.

## 3. Docker telepítése a Pi-n

```bash
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER
# jelentkezz ki/be, hogy a csoporttagság érvénybe lépjen
docker compose version   # ellenőrzés — v2.20+ kell
```

## 4. Build-stratégia — miért NE a Pi-n buildelj

A `dotnet publish` (3 backend projekt) + a `pnpm build` (Angular,
production móddal) egy erős PC-n is percekig tart — egy Raspberry Pi 4/5
CPU-ján ez **15-30 percet** vehet igénybe *image-enként* natív buildnél is,
mert a Pi CPU-ja lényegesen lassabb egy asztali/szerver CPU-nál (nem az
architektúra az akadály, hanem a nyers teljesítmény).

**Javasolt megoldás: cross-build a fejlesztői gépeden, majd az elkészült
image-ek átvitele a Pi-re.** Ehhez a repo tartalmaz Makefile targeteket és
egy `docker-compose.rpi.yml` felülbírálást.

### 4a. Egyszeri előkészítés a fejlesztői gépen

```bash
make rpi-buildx-setup
```

Ez létrehoz egy `docker-container` driverű buildx buildert, ami QEMU
emulációval képes arm64-re fordítani akkor is, ha a fejlesztői géped
amd64 (pl. a te esetedben Windows/Docker Desktop — ez natívan tartalmazza
a QEMU binfmt-handlereket, nincs külön teendő).

### 4b. Image-ek buildelése és átvitele — két opció

**Opció A — helyi export, nincs szükség registry-fiókra** (illik a
LAN-only, privacy-first filozófiához, lásd ADR-0003):

```bash
make rpi-save
# → dist/rpi/family-os-images-arm64-latest.tar.gz

scp dist/rpi/family-os-images-arm64-latest.tar.gz pi@<pi-ip>:~/
ssh pi@<pi-ip>
gunzip -c family-os-images-arm64-latest.tar.gz | docker load
```

**Opció B — push egy container registry-be** (docker.io, ghcr.io, vagy egy
saját LAN-on belüli registry), ha több gépről szeretnéd húzni az image-eket:

```bash
docker login ghcr.io   # vagy docker.io
make rpi-push RPI_REGISTRY=ghcr.io/felhasznalonev/
```

A Pi-n ez esetben a `.env`-ben állítsd be:
```
RPI_IMAGE_PREFIX=ghcr.io/felhasznalonev/
```

### 4c. Alternatíva: natív build a Pi-n

Ha mégis a Pi-n akarsz buildelni (pl. nincs erős fejlesztői géped), a
meglévő `docker-compose.yml` `build:` szakaszai változtatás nélkül működnek
— csak legyél türelmes:

```bash
docker compose build   # ~30-60 perc a 3 image-hez, Pi 4-en
```

## 5. Ollama / AI pipeline — méretezés gyenge hardverre

Ez a legkritikusabb pont. A `docs/ai-pipeline.md`-ben és
`docker-compose.yml`-ben szereplő alapértelmezett modellek (pl.
`gpt-oss:20b`) **több tíz GB RAM-ot** igényelnek — egy Raspberry Pi-n ez
kizárt.

### Válaszd ki az egyiket:

**Opció 1 — kicsi modell fut a Pi-n** (csak Pi 5 8 GB-nál javasolt):

```bash
docker compose exec ollama ollama pull qwen2.5:1.5b
docker compose exec ollama ollama pull nomic-embed-text
```

Állítsd be a `.env`-ben (vagy közvetlenül az `api`/`workers` service
`Ai__` env változóiban), hogy ezt a modellt használja az alkalmazás.
Számíts rá, hogy egy dokumentum-összefoglalás **10-60 másodpercig** is
eltarthat egy kis modellel Pi-n (GPU nélkül), szemben egy asztali gép
1-5 másodpercével.

**Opció 2 (ajánlott) — az Ollama egy másik, erősebb LAN-gépen fut**, a Pi
csak az API-t/DB-t/frontendet szolgálja ki:

```bash
# a Pi-n NE indítsd el az ollama service-t:
docker compose -f docker-compose.yml -f docker-compose.rpi.yml \
  up -d --no-build postgres api workers web

# az api/workers env-jében (docker-compose.yml vagy .env):
Ollama__BaseUrl=http://<masik-gep-lan-ip>:11434
```

Ez a legjobb ár/érték arány: a Pi csendes, alacsony fogyasztású háztartási
szerverként üzemel éjjel-nappal, a nehéz AI-számítást pedig az az erősebb
gép végzi, amit amúgy is bekapcsolsz időnként (pl. a fő PC-d, amin ez a
repó is van).

## 6. Indítás a Pi-n

```bash
git clone <repo-url> family-os
cd family-os
cp .env.example .env
# töltsd ki a .env-et (lásd DELIVERY.md 2b. szakasz) +
# RPI_IMAGE_PREFIX / RPI_TAG, ha registry-t használsz

make init-tls   # vagy: ./scripts/init-tls-ca.sh family-os.lan

make rpi-up
# ekvivalens: docker compose -f docker-compose.yml -f docker-compose.rpi.yml up -d --no-build
```

> A `--no-build` kötelező elem — enélkül Compose megpróbálná lokálisan
> buildelni a `build:` szakaszt, ha a megadott image-tag pontosan nem
> létezik már betöltve/pull-olva.

Utána a [DELIVERY.md](DELIVERY.md) 2c–3d szakaszai (TLS-CA telepítés az
eszközökre, Google OAuth, első bejelentkezés) változtatás nélkül
alkalmazhatók.

## 7. Ellenőrzés

```bash
docker compose ps
# minden service "healthy", az ollama-nak van start_period: 60s

curl -k https://localhost/healthz/ready

# arch-ellenőrzés — ha "exec format error"-t kapsz valamelyik service
# indításakor, rossz architektúrájú image-et töltöttél be:
docker inspect family-os-api:arm64-latest --format '{{.Architecture}}'
# várt: arm64
```

## 8. RPi-specifikus hibaelhárítás

**`exec format error` egy konténer indításakor**
→ amd64 image került a Pi-re (pl. elfelejtetted a `--platform linux/arm64`-et
a buildnél, vagy sima `docker compose build`-et futtattál a fejlesztői
gépeden és azt próbáltad átvinni). Buildelj újra `make rpi-build`-del.

**A konténer folyamatosan újraindul (`OOMKilled`)**
→ `docker compose logs <service>` és `dmesg | grep -i oom`. A Pi-n
tipikusan az `ollama` fut ki a memóriából nagyobb modellel. Válts kisebb
modellre (5. szakasz, Opció 1), vagy tedd át az Ollamát másik gépre
(Opció 2).

**Nagyon lassú I/O, magas `iowait`**
→ `vmstat 1` — ha `wa` oszlop tartósan magas, valószínűleg SD kártyáról
futsz. Költözz USB SSD-re (2. szakasz).

**Hőmérséklet-throttling**
```bash
vcgencmd measure_temp
vcgencmd get_throttled
# 0x0 = nincs throttling; bármi más érték jelzi, hogy a Pi korlátozta magát
```
→ tégy rá hűtőbordát/ventilátort, különösen ha az Ollama service is fut
a Pi-n (folyamatos CPU-terhelés AI-hívások alatt).

**`docker buildx build --platform linux/arm64` nagyon lassú a fejlesztői
gépen (QEMU-emuláció)**
→ ez várható amd64 gépen; a 3 image cross-buildje összesen 10-20 percet is
igénybe vehet. Ha ez zavaró, fontold meg egy natív arm64 buildx-buildert
(pl. egy felhő-alapú arm64 runner, vagy magán a Pi-n futtatott
`docker buildx create --append` remote node).

## 9. Frissítés

Ugyanaz, mint a [DELIVERY.md 6. szakaszában](DELIVERY.md#6-frissítés), egy
különbséggel: a `docker compose build` helyett újra a cross-build +
átvitel workflow-t kell lefuttatni (4b. szakasz), majd a Pi-n:

```bash
git pull
gunzip -c family-os-images-<uj-tag>.tar.gz | docker load
docker compose -f docker-compose.yml -f docker-compose.rpi.yml up -d --no-build
```

# Hordozható telepítés — „next-next-finish" telepítőkészlet

> Státusz: TERV v1.0 · Dátum: 2026-07-12
> Cél: a Family OS bármely (Windows/macOS/Linux) gépen telepíthető legyen egy
> grafikus, néhány kattintásos telepítővel, feltéve hogy a hálózaton elérhető
> egy Ollama-példány. Fejlesztői/forrás-clone tudás ne legyen előfeltétel.
> Kapcsolódó: `docs/DELIVERY.md`, [ADR-0003](../decisions/ADR-0003-mobil-csak-lan.md)
> (LAN-only), [ADR-0010](../decisions/ADR-0010-compose-first-helm-kivetel.md)
> (compose-first), [08-android-port-terv.md](08-android-port-terv.md)

---

## 1. Hol tartunk ma és mi az akadály

**Ma a telepítés fejlesztő-orientált** (DELIVERY.md): `git clone` → `.env`
kézi szerkesztése → `init-tls-ca.sh` futtatása → `docker compose build`
(forrásból fordít!) → CA kézi telepítése minden eszközre → első login.
Ez erős-PC-n működik, de „bármely gépen, next-next-finish" célhoz öt konkrét
akadály van:

| # | Akadály | Miért blokkol | Célállapot |
|---|---|---|---|
| P1 | **Forrásból buildel** (`build:` minden service-nél) | .NET SDK + Node + fordítási idő + git kell | előre buildelt **image-ek** registry-ből vagy tarból |
| P2 | **`host.docker.internal:11434`** fix Ollama-cím | csak ugyanazon a gépen futó Ollamára jó; „hálózaton elérhető" nem | **konfigurálható Ollama-URL** + felderítés |
| P3 | **`.env` kézi szerkesztése** (jelszavak, Google ID) | felhasználó nem tud/akar szövegfájlt írni | **telepítő GUI** generálja |
| P4 | **TLS belső CA kézi generálás + eszközönkénti telepítés** | parancssor + biztonsági beállítások | telepítő generálja; opcionális egyszerűbb HTTP-mód |
| P5 | **Docker Desktop megléte feltételezett** | laikusnak nincs | telepítő ellenőrzi/telepíti, vagy beágyazott runtime |

A jó hír: az architektúra **compose-first és 12-factor** (minden config
env-ből, `AiProviderFactory` már absztrahálja a providert, health-checkek
megvannak) — a hordozhatóság főleg **csomagolás és telepítő**, nem
alkalmazás-újraírás. A backend logikájából keveset kell módosítani.

---

## 2. Célkép — mit lát a felhasználó

1. Letölt egy telepítőt: `FamilyOS-Setup-1.0.exe` / `.dmg` / `.AppImage`.
2. Elindítja → üdvözlő képernyő → **Tovább**.
3. A telepítő ellenőrzi/kínálja a Docker runtime-ot → **Tovább**.
4. Beállítás-varázsló:
   - **Ollama:** „Keresés a hálózaton" gomb → megtalált példányok listája,
     vagy kézi cím; „Teszt" gomb zölddel.
   - **Bejelentkezés módja:** Google (Client ID beillesztése segített
     útmutatóval) *vagy* egyszerű helyi jelszó (lásd 4.3).
   - **Admin e-mail** (az első admin).
   - **Elérési mód:** HTTPS (ajánlott, telepítő intézi a CA-t) *vagy*
     HTTP (egyszerű, csak megbízható LAN-on).
5. **Telepítés** gomb → a telepítő legenerálja a konfigot, letölti/betölti
   az image-eket, elindítja a stacket, megvárja a health-check zöldet.
6. **Kész** képernyő: „A Family OS elérhető: https://ez-a-gep.lan" + QR-kód
   a telefonokhoz + „Megnyitás böngészőben" gomb.

Ugyanez **fejmentes/szerver-módban**: egy `install.sh --headless
--config answers.json` út a NAS/Linux-szerver esetére.

---

## 3. Architektúra-döntések

### 3.1 Csomagolási stratégia: előre buildelt image-ek (P1)

- A CI **multi-arch** (amd64 + arm64) image-eket publikál egy registry-be
  (GitHub Container Registry / Docker Hub): `familyos/api`, `familyos/workers`,
  `familyos/web`, verzió-tagekkel. A `postgres` a hivatalos
  `pgvector/pgvector:pg16`.
- A `docker-compose.yml` **build-mentes** változata (`docker-compose.dist.yml`)
  csak `image:` hivatkozásokat tartalmaz — semmi `build:`.
- **Offline-telepítéshez** (nincs internet a célgépen): a telepítő tartalmaz
  egy `images.tar`-t (`docker save`) is; a telepítő `docker load`-dal
  tölti be. Így a next-next-finish internet nélkül is megy.

### 3.2 Ollama a hálózaton (P2) — a felhasználó fő kérése

- Az Ollama-URL **konfigurálható** (már env: `Ai__Ollama__BaseUrl`) — a fix
  `host.docker.internal` csak *default*. A telepítő ezt írja felül.
- **Felderítés** a varázslóban:
  1. lokális próba (`http://host.docker.internal:11434/api/tags`),
  2. LAN-szkennelés (a megadott alhálón a 11434 port `/api/tags`-ét
     pingeli, párhuzamosan),
  3. kézi megadás.
- **Modell-ellenőrzés:** a „Teszt" a `/api/tags`-ból ellenőrzi, hogy a
  szükséges modellek (chat + `nomic-embed-text`) megvannak-e; ha nem,
  felajánlja a `ollama pull`-t a távoli gépen (ha az elérhető), vagy
  útmutatót ad. **Új health-check kell:** a mai `OllamaHealthCheck` a
  reachability-t nézi — bővítendő „a kért modell létezik-e" ellenőrzéssel,
  hogy a telepítő és a `/healthz/ready` értelmes hibát adjon.
- **Backend-hatás:** minimális — az URL már env- vezérelt; a health-check
  bővítése az egyetlen érdemi kódmódosítás ebben a pontban.

### 3.3 Elérési név és TLS (P4)

Két profil, a telepítő választatja:

| Profil | Hogyan | Kinek |
|---|---|---|
| **Egyszerű (HTTP)** | nginx 80-on, self-signed nélkül; `http://<gépnév>.local` | megbízható otthoni LAN, gyors start |
| **Biztonságos (HTTPS)** ✅ | a telepítő futtatja az `init-tls-ca.sh`-t, és **automatikusan** telepíti a CA-t a *telepítő gépére*; más eszközökre QR-os útmutató | ajánlott alap |

- **Gépnév:** a telepítő az mDNS-t használja (`<hostname>.local`) — nincs
  szükség statikus IP-re vagy DNS-re. A compose kap egy avahi/mDNS
  service-t (a 08-as Android-doksi is ezt igényli — közös építőelem).
- A CA-telepítés a *saját* gépen platformonként automatizálható (Windows:
  `certutil -addstore Root`, macOS: `security add-trusted-cert`, Linux:
  `update-ca-certificates`). A többi eszközre marad a QR-os DELIVERY-útmutató.

### 3.4 Konfiguráció-generálás (P3)

- A telepítő GUI gyűjti a válaszokat és **generálja** a `.env`-et
  (erős jelszavak automatikus, kriptográfiai véletlennel — a felhasználó
  nem lát/ír jelszót), plusz a compose-override-ot (Ollama-URL, HTTP/HTTPS
  profil, portok).
- Titkok tárolása: a generált `.env` a telepítési mappában, szűkített
  jogosultsággal; a `TOOLCALL_SIGNING_KEY` és DP-kulcsok is itt generálódnak.
- **Validáció telepítés előtt:** Ollama elérhető? Portok szabadok? Elég
  a RAM/lemez? — a varázsló addig nem enged tovább, amíg piros van.

### 3.5 Docker runtime (P5)

- **Windows/macOS:** a telepítő ellenőrzi a Docker Desktopot; ha nincs,
  linkel/letölt (a Docker EULA miatt csendes-telepítés korlátos — a
  varázsló vezeti végig). Alternatíva kiértékelendő: **Podman** (kötöttebb
  licenc nélkül) — spike-feladat.
- **Linux:** a `install.sh` telepíti a `docker`/`podman` csomagot a
  disztró csomagkezelőjével.
- A stack **rendszerindításkor** feljön (`restart: unless-stopped` már be
  van állítva) — a telepítő regisztrálja a Docker autostartot.

### 3.6 Frissítés és eltávolítás

- **Frissítés:** a telepítő „Frissítés" módja új image-tageket húz +
  `docker compose up -d` (a migrációk a `DbSeedRunner`-rel automatikusan
  futnak indításkor — ez már így van). Adat érintetlen (named volume-ok).
- **Eltávolítás:** „Uninstall" — stack leállítása; a *volume-ok
  megtartása* alapértelmezés (adatvédelem), külön pipa a teljes törléshez.
- Backup a meglévő `backup` profil + `scripts/backup.sh` — a telepítő
  felajánlja az ütemezett titkosított mentést (age-kulcs generálva).

---

## 4. Nyitott döntések (ADR-t igényelnek)

### 4.1 Telepítő-technológia

| Opció | Előny | Hátrány |
|---|---|---|
| **Tauri** (Rust+web-UI) ✅ | kicsi, multiplatform, a varázsló-UI webtechből (csapat-kompetencia); natív parancs-futtatás | Rust build a CI-ban |
| Electron | ismerős | nagy méret |
| WiX/NSIS (Win) + pkg (mac) + sh (Linux) külön | natív | 3 külön kódbázis |
| BurntToast/Ansible headless-only | egyszerű szerverre | nincs GUI a laikusnak |

Javaslat: **Tauri** GUI + közös `install-core` szkript-réteg (a tényleges
munkát — docker load/up, CA, .env — egy jól tesztelt CLI végzi, a GUI csak
vezérli). Így a headless út (`install-core --headless`) ingyen adódik.

### 4.2 Registry vs. teljesen offline

- Alap: publikus registry (kis telepítő, image-ek húzása).
- Offline-változat: nagy telepítő beágyazott `images.tar`-ral.
- **Javaslat:** mindkettő buildelése a CI-ból; a letöltőoldal választ
  („online, kisebb" / „offline, teljes").

### 4.3 Auth laikus felhasználónak (fontos!)

A Google OAuth Client ID beszerzése **nem** next-next-finish élmény
(Google Cloud Console projekt kell). Két út:

- **Google mód** (mai): a varázsló képes útmutatóval segít — de ez a
  legnagyobb súrlódási pont.
- **Helyi-jelszó mód** (új, ajánlott default a telepítőben): egyszerű
  e-mail+jelszó (Argon2id hash), a Google-flow megkerülésével.
  **Ez érdemi backend-munka:** a mai auth kizárólag Google-idToken-alapú
  (`LoginGoogleCommandHandler`); egy `LocalPasswordAuthProvider` +
  `POST /auth/login/local` + jelszókezelés kell. Külön CR
  (cr260712-09 javaslat), a security-privacy.md 3.1 aktualizálásával.

**Javaslat:** a telepítő MVP-je a Google-módot vezeti végig (kevesebb új
kód), a helyi-jelszó mód gyors követő CR — mert enélkül a „bármely gép"
cél laikusnak féloldalas marad.

### 4.4 Compose vs. beépített orkesztráció

Marad a **compose** (ADR-0010 compose-first). A telepítő a Docker Compose
v2 plugint használja; Kubernetes/Helm nem cél ehhez.

---

# II. rész — Végrehajtási lépések (sorrendben)

## L0 — Döntések és spike (1–2 nap)

- [ ] **ADR-0017:** hordozható telepítő (Tauri + install-core), registry-
      stratégia, HTTP/HTTPS profil, auth-mód MVP-döntés (4.3).
- [ ] Spike: `docker save`/`load` méret + betöltési idő mérése (offline út).
- [ ] Spike: Podman mint Docker-alternatíva Windows/mac-en (licenc-barát) —
      megy-e a mai compose módosítás nélkül?

## L1 — Build-mentes disztribúció (2–3 nap)

- [ ] `docker-compose.dist.yml`: minden `build:` → `image:` (verziózott tag).
- [ ] CI: multi-arch (amd64+arm64) image-build + push GHCR-be; a meglévő
      `api/workers/web` Dockerfile-okból (`docker/*.Dockerfile`).
- [ ] CI-artefaktum: `images.tar` (`docker save`) az offline-telepítőhöz.
- [ ] Verziózás: git-tag → image-tag → app `GET /system/version` (a 02-es
      doksi A3 tétele itt is kell).

## L2 — Ollama-hálózati konfiguráció (2 nap)

- [ ] `Ai__Ollama__BaseUrl` marad env-vezérelt (kész) — a fix default
      dokumentálása „csak azonos gép" figyelmeztetéssel.
- [ ] `OllamaHealthCheck` bővítése: reachability **+ kért modellek
      megléte** (`/api/tags`); a `/healthz/ready` és a telepítő közös
      igazságforrása.
- [ ] Kis felderítő segéd (`ollama-probe`): a telepítő és opcionálisan egy
      admin-diagnosztika hívja (LAN-szkennelés a 11434-en).

## L3 — install-core CLI (a telepítő motorja) (3–4 nap)

- [ ] `answers.json` séma (Ollama-URL, auth-mód, admin-email, profil, portok).
- [ ] `.env` + compose-override **generálás** (kriptográfiai jelszavak,
      `TOOLCALL_SIGNING_KEY`, DP-kulcs) az answers-ből.
- [ ] Előfeltétel-ellenőrzés: Docker jelen? portok szabadok? RAM/lemez?
      Ollama+modell elérhető?
- [ ] Telepítési folyamat: (offline) `docker load` → `compose -f dist up -d`
      → health-poll (`/healthz/ready`) → első-admin bootstrap.
- [ ] `--headless` mód (szerver-telepítés answers.json-ból).
- [ ] Uninstall/Update alparancsok (volume-megtartással).

## L4 — TLS és mDNS automatizálás (2 nap)

- [ ] `init-tls-ca.sh` meghívása az install-core-ból; CA auto-telepítés a
      *telepítő gépre* platformonként (certutil / security / update-ca-cert).
- [ ] Compose: avahi/mDNS service (`<hostname>.local`) — a 08-as Android-
      doksival közös; a nginx-conf a gépnévre igazítva generálódik.
- [ ] HTTP-profil (self-signed nélkül) opció a compose-override-ban.

## L5 — Telepítő GUI (Tauri) (4–5 nap)

- [ ] Varázsló-képernyők: Üdvözlés → Docker-ellenőrzés → Ollama-felderítés
      (Teszt-gombbal) → Auth → Admin/Profil → Összegzés → Telepítés
      (folyamatjelző a health-pollból) → Kész (URL + QR + „Megnyitás").
- [ ] A GUI CSAK vezérel: minden művelet az install-core CLI-n át (tesztelhető).
- [ ] Hibakezelés: minden lépés érthető magyar üzenettel + „Naplók mentése"
      gomb a support-hoz.
- [ ] Nyelv: magyar (az app-pal konzisztens).

## L6 — (Követő CR) Helyi-jelszó auth (3–4 nap) — cr260712-09

- [ ] `LocalPasswordAuthProvider` (Argon2id), `POST /auth/login/local`,
      admin jelszó-beállítás az install-core bootstrapból.
- [ ] Login-oldal: a telepítéskor választott mód szerint Google **vagy**
      helyi-jelszó űrlap.
- [ ] security-privacy.md 3.1 frissítése; a Google-mód opcionálissá válik.

## L7 — Csomagolás, aláírás, kiadás (2–3 nap)

- [ ] Windows: kódaláírt `.exe` (SmartScreen elkerülése); macOS: notarizált
      `.dmg`; Linux: `.AppImage` + `install.sh`.
- [ ] Letöltőoldal: online (kicsi) / offline (teljes) változat.
- [ ] Kézi telepítési tesztmátrix: friss Win11 / macOS / Ubuntu, Docker
      nélküli kiindulásból, LAN-beli külön Ollama-géppel.

## L8 — Dokumentáció és próbatelepítés (1–2 nap)

- [ ] `DELIVERY.md` átírása: „Fejlesztői telepítés" (mai) + „Egyszerű
      telepítés (telepítővel)" szakaszra.
- [ ] Videó/képes gyorsindító a next-next-finish útról.
- [ ] **Elfogadási próba:** egy nem-fejlesztő családtag telepíti külső
      segítség nélkül, külön gépen futó Ollamával — ez a tényleges DoD.

## Ütemterv-összegzés

| Lépés | Effort | Függés |
|---|---|---|
| L0 döntés/spike | 1–2 nap | – |
| L1 build-mentes image-ek | 2–3 nap | L0 |
| L2 Ollama-hálózat | 2 nap | – |
| L3 install-core CLI | 3–4 nap | L1, L2 |
| L4 TLS/mDNS | 2 nap | L3 |
| L5 telepítő GUI | 4–5 nap | L3, L4 |
| L6 helyi-jelszó auth (CR) | 3–4 nap | párhuzamos |
| L7 csomagolás/aláírás | 2–3 nap | L5 |
| L8 doksi/próba | 1–2 nap | mind |

**Összesen: ~17–23 munkanap** (az L6 helyi-jelszó nélkül a szűkebb becslés).
Kritikus út: L1 → L3 → L5. Az L6 (helyi-jelszó) technikailag opcionális, de
a „laikus, bármely gépen" cél gyakorlati beváltásához erősen ajánlott.

## Definition of Done

- [ ] Nem-fejlesztő felhasználó friss gépen, LAN-beli **külön** Ollamával,
      a telepítőt követve működő Family OS-t kap — parancssor nélkül.
- [ ] Az Ollama-cím a varázslóban felderíthető/megadható és tesztelhető;
      a `/healthz/ready` a modell-hiányt is jelzi.
- [ ] Online és offline telepítő is elérhető, aláírva.
- [ ] Frissítés és eltávolítás adatvesztés nélkül működik.
- [ ] A mai fejlesztői (`git clone` + build) út továbbra is működik
      (a `docker-compose.yml` build-változata megmarad).
- [ ] DELIVERY.md kétutas; elfogadási próbatelepítés jegyzőkönyvezve.

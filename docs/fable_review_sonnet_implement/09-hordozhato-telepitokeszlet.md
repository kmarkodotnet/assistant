# Hordozható telepítőkészlet — Sonnet-implementációs L-kártyák

> Státusz: IMPLEMENTÁCIÓS SPEC v1.0 · Dátum: 2026-07-12
> Forrás: `docs/fable_review/09-hordozhato-telepitokeszlet.md`
> Kapcsolódó: `docs/DELIVERY.md`, ADR-0003 (LAN-only), ADR-0010 (compose-first),
> [08-android-port-terv.md](08-android-port-terv.md) (mDNS/CA közös építőelem).
> A régi `09-telepitheto-termek-terv.md` ELAVULT csonk — ne implementáld.

## Zárt döntések (NE nyisd újra — ADR-0017-be kerülnek)

| Kérdés | Döntés | Indok |
|---|---|---|
| Futtatókörnyezet | Docker-alapú marad (compose-first, ADR-0010) | teljes stack változatlan; natív csomagolás = hetek |
| Telepítő-technológia | **Tauri GUI + `install-core` CLI** — a GUI csak vezérel, minden műveletet a CLI végez | headless út (`install-core --headless`) ingyen adódik; a CLI tesztelhető |
| Disztribúció | előre buildelt **multi-arch image-ek GHCR-ből** + offline `images.tar` változat — MINDKETTŐ a CI-ból | P1 akadály (forrásból build) megszűnik |
| Elérési mód | két profil: HTTPS (ajánlott, auto-CA) és HTTP (egyszerű LAN) | P4 |
| Gépnév | mDNS `<hostname>.local` (avahi a compose-ban — a 08/M3.2-vel KÖZÖS) | nincs statikus IP/DNS igény |
| Auth az MVP-telepítőben | **Google-mód** vezetett útmutatóval; helyi-jelszó = KÖVETŐ CR (cr260712-09, L6) | kevesebb új kód az MVP-ben |
| Orkesztráció | compose v2 plugin; K8s/Helm NEM cél | ADR-0010 |
| Konfig | a telepítő GENERÁLJA a `.env`-et (kripto-véletlen jelszavak, TOOLCALL_SIGNING_KEY) — a felhasználó jelszót nem lát | P3 |
| Eltávolítás | volume-megtartás a default; teljes törlés külön pipa | adatvédelem |

**⛔ EMBERI KAPUK:** ADR-0017 elfogadása (L0) · kódaláíró tanúsítványok
beszerzése (L7) · cr260712-09 auth-CR indítása (L6 — API-kontrakt-változás).

**Kereszthivatkozások (előfeltétel-kártyák másutt):**
- `GET /system/version` → **02/C-A3** (L1 verziózáshoz kell)
- `PATCH /settings/system` őszinte válasz → 02/C-A10 (first-run wizard későbbi alapja)
- compose-jelszavak `${VAR:?}` → 01/T8 (a dist-compose is így készül)

---

## L0 — Döntések és spike-ok (1–2 nap)

**Ág:** `spike/installer-l0` · **Modell:** sonnet.

**Lépések:**
1. **ADR-0017 DRAFT** a fenti zárt döntésekkel + a spike-eredményekkel (⛔).
2. **Spike:** `docker save`/`docker load` — a teljes stack image-méretének
   és betöltési idejének mérése. PASS: dokumentált számok (GB + perc).
3. **Spike:** Podman Windows/mac-en — megy-e a MAI compose módosítás nélkül
   (`podman compose -f docker-compose.yml up`)? PASS/FAIL jegyzőkönyv;
   **döntési szabály:** ha a compose módosítás nélkül fut → a telepítő
   Podman-t is elfogad detektáláskor; ha nem → Docker Desktop-only az MVP,
   Podman backlog.

**Tilos:** telepítő-kód írása; compose-módosítás.

---

## L1 — Build-mentes disztribúció (2–3 nap) — Függ: L0

**Ág:** `feature/installer-l1-dist-images` · **Modell:** sonnet (CI-rész: devops).

**Olvasd el először:** `docker/*.Dockerfile` (api/workers/web) ·
docker-compose.yml (build-szekciók, volume-ok, env-ek) · a CI workflow
(hova kerül az image-build job).

**Lépések:**
1. `docker-compose.dist.yml`: a compose-ból minden `build:` → `image:`
   (pl. `ghcr.io/<org>/familyos-api:<tag>`); a `postgres` →
   `pgvector/pgvector:pg16`; MINDEN más (volume, env, health-check, network)
   azonos a fejlesztői compose-zal. A jelszavak `${VAR:?hiba}` formában
   (01/T8 mintája).
2. CI: multi-arch (amd64+arm64) buildx job + push GHCR-be, git-tag →
   image-tag megfeleltetéssel; csak tag-pushra fut (ne minden PR-re).
3. CI-artefaktum: `images.tar` (`docker save` az összes image-ről) a
   release-hez csatolva.
4. Verziólánc: git-tag → image-tag → `GET /system/version` (02/C-A3 —
   ha még nincs kész, előbb az) — az assembly informational version a
   CI-ban a git-tagből jöjjön.

**Elfogadás:** friss gépen `docker compose -f docker-compose.dist.yml up -d`
forráskód NÉLKÜL működő stacket ad (kézzel kitöltött .env-vel) ·
`docker load < images.tar` után ugyanez internet nélkül.
**Tilos:** a fejlesztői compose törlése/átnevezése (a `git clone`-os út
marad!); Dockerfile-átírás (csak ha a multi-arch build kényszeríti — akkor
jelezd).

---

## L2 — Ollama-hálózati konfiguráció (2 nap) — Független

**Ág:** `feature/installer-l2-ollama-net` · **Modell:** sonnet.

**Olvasd el először:** `OllamaHealthCheck` (mai reachability-logika) ·
`Ai__Ollama__BaseUrl` env-út (OllamaOptions) · docker-compose.yml:44,75
(a fix `host.docker.internal:11434` + modellnév).

**Lépések:**
1. A compose-ban az Ollama-URL és modellnév env-paraméter legyen defaulttal:
   `${OLLAMA_BASE_URL:-http://host.docker.internal:11434}` — dokumentálva
   („a default csak azonos gépen futó Ollamára jó").
2. `OllamaHealthCheck` bővítése: reachability **+ a konfigurált modellek
   megléte** a `/api/tags`-ből (chat-modell + `nomic-embed-text`). Hiányzó
   modell → Unhealthy, a hiányzó modell nevével az adatban. Ez a
   `/healthz/ready` és a telepítő KÖZÖS igazságforrása.
3. `ollama-probe` segéd (kis önálló eszköz az install-core részeként, vagy
   szkript): (a) lokális próba, (b) LAN-szkennelés a 11434-en párhuzamos
   `/api/tags` hívásokkal, (c) adott URL tesztje + modell-lista.
4. Unit teszt a health-checkre (mockolt /api/tags: megvan / hiányzik / nem
   elérhető).

**Elfogadás:** másik gépen futó Ollamával, `OLLAMA_BASE_URL` átírásával a
stack működik · `/healthz/ready` modell-hiánynál a hiányzó modellt nevesíti.
**Tilos:** OllamaHttpClient hívási logika módosítása (F1 területe).

---

## L3 — install-core CLI (3–4 nap) — Függ: L1, L2

**Ág:** `feature/installer-l3-core-cli` · **Modell:** sonnet.
**Nyelv-döntés:** az install-core a Tauri-oldali Rust-ba VAGY önálló
platformfüggetlen binárisba (Go/Rust) kerül — az implementáló a Tauri-spike
(L5-höz) tanulságai szerint dönt, a döntést az ADR-0017-be visszaírva.
NEM lehet: bash-only (Windows-on futnia kell), .NET (ne kelljen runtime).

**Lépések:**
1. `answers.json` séma: `{ollamaUrl, authMode: "google"|"local",
   googleClientId?, adminEmail, profile: "https"|"http", ports{}, offline: bool}`
   — séma-validálás betöltéskor.
2. Generálás az answers-ből: `.env` (kripto-véletlen DB/backup jelszavak,
   `TOOLCALL_SIGNING_KEY`; szűkített fájljogosultság) + compose-override
   (Ollama-URL, profil, portok).
3. Előfeltétel-ellenőrzés parancs (`install-core check`): Docker/Podman
   jelen + fut? portok szabadok? RAM/lemez elég? Ollama elérhető + modellek
   megvannak (L2 probe)? — strukturált (JSON) kimenet a GUI-nak.
4. `install-core install`: (offline esetén `docker load`) →
   `compose -f docker-compose.dist.yml up -d` → health-poll
   (`/healthz/ready`, timeout + érthető hiba) → első-admin bootstrap
   (`BOOTSTRAP_ADMIN_EMAIL` env — a meglévő mechanizmus).
5. `install-core update` (új image-tag pull + up -d; a migrációk a
   DbSeedRunner-rel automatikusan futnak — ez már így van) és
   `install-core uninstall` (default: volume-ok MARADNAK; `--purge` a
   teljes törléshez, dupla megerősítéssel).
6. `--headless --config answers.json` út minden parancsra.
7. Tesztek: a generátor unit-tesztjei (jelszó-erősség, séma-validálás,
   override-tartalom); integrációs füstteszt legalább Linux CI-ban
   (check + install egy üres docker-in-docker környezetben).

**Elfogadás:** `install-core install --headless --config answers.json` friss
Linux-gépen működő stackig fut · uninstall volume-megtartással · minden
parancs értelmes magyar hibaüzenettel bukik.
**Tilos:** GUI-kód; a fejlesztői út érintése.

---

## L4 — TLS és mDNS automatizálás (2 nap) — Függ: L3

**Ág:** `feature/installer-l4-tls-mdns` · **Modell:** sonnet.

**Olvasd el először:** `scripts/init-tls-ca.sh` · nginx-conf sablon a
repóban · 08/M3.2 (avahi — ha már bement, HASZNÁLD, ne duplikáld).

**Lépések:**
1. HTTPS-profil: az install-core meghívja az `init-tls-ca.sh`-t (vagy
   annak portolt logikáját), majd a CA-t AUTOMATIKUSAN telepíti a telepítő
   gépére platformonként: Windows `certutil -addstore Root`, macOS
   `security add-trusted-cert`, Linux `update-ca-certificates`.
   Más eszközökre: QR-os útmutató (a meglévő DELIVERY-anyag + 08/M3.4).
2. mDNS: avahi-service a compose-ban (`<hostname>.local` + `_familyos._tcp`)
   — a 08-as Android-tervvel KÖZÖS építőelem; a nginx server_name a
   gépnévre igazítva generálódik az override-ban.
3. HTTP-profil: nginx 80-on TLS nélkül az override-ból; a cookie
   `SecurePolicy.Always` miatt ELLENŐRIZD: HTTP-módban a `__Host-` cookie
   nem működik — ehhez a profilhoz a backend cookie-policy env-vezérelt
   lazítása kell (`Auth:CookieSecurePolicy=None` CSAK http-profilban,
   a compose-override állítja). Ez kódmódosítás: kis, env-kapcsolós —
   a security-privacy.md-be kerüljön figyelmeztetés („HTTP-profil csak
   megbízható LAN-ra").
4. Tesztek: a cookie-policy kapcsoló unit-tesztje; kézi próba mindkét
   profillal.

**Elfogadás:** HTTPS-profil: a telepítő gépén böngésző-zöld lakat kézi
CA-műveletek nélkül · HTTP-profil: működő login `http://<gépnév>.local`-on.
**Tilos:** a HTTPS-út gyengítése (a default profil marad HTTPS-ajánlott).

---

## L5 — Telepítő GUI (Tauri) (4–5 nap) — Függ: L3, L4

**Ág:** `feature/installer-l5-gui` · **Modell:** sonnet.

**Döntések (zárva):**
- A GUI **CSAK vezérel** — minden művelet az install-core CLI-n át
  (strukturált JSON ki/be); üzleti logika a GUI-ban TILOS.
- Nyelv: magyar; a varázsló-szövegek érthetőek laikusnak.
- Képernyő-sorrend: Üdvözlés → Docker-ellenőrzés (link/letöltés-vezetés,
  EULA miatt csendes telepítés nincs) → Ollama-felderítés (lista + kézi cím
  + „Teszt" gomb zöld/piros, hiányzó modellnél `ollama pull` útmutató) →
  Auth-mód (Google Client ID beillesztés képes útmutatóval) → Admin-email +
  profil (HTTPS/HTTP) → Összegzés → Telepítés (folyamatjelző a
  health-pollból) → Kész (URL + QR a telefonokhoz + „Megnyitás böngészőben").
- Minden hiba érthető magyar üzenettel + „Naplók mentése" gomb.
- A varázsló addig nem enged tovább, amíg az adott lépés ellenőrzése piros.

**Lépések:** Tauri-projekt scaffold (külön repo-mappa: `installer/`) →
képernyők → install-core bekötés → hibautak → build mindhárom OS-re a CI-ban.
**Elfogadás:** a 2. fejezet szerinti teljes next-next-finish út végigmegy
Windows-on (kézi jegyzőkönyv) · headless út változatlanul működik.
**Tilos:** üzleti logika a GUI-ban; app-oldali (Angular) módosítás.

---

## L6 — Helyi-jelszó auth (3–4 nap) — KÖVETŐ CR: cr260712-09 — párhuzamosítható

**Ág:** `feature/cr260712-09-local-auth` · **Modell:** sonnet.
**⛔ EMBERI KAPU:** API-kontrakt-változás + DB-migráció (`PasswordHash`
oszlop) — ARCH-egyeztetés és DRAFT-migráció emberi jóváhagyással (CLAUDE.md).

**Olvasd el először:** `LoginGoogleCommandHandler` + a session-kiadási út
(`SignInAsync`) · `UserAccount` entitás · `docs/security-privacy.md` §3.1 ·
01/T6 (login-throttling — a helyi loginra IS érvényes legyen!).

**Döntések (zárva):**
- `Auth:Mode = Local | Google | Both` konfiguráció.
- `UserAccount.PasswordHash` (nullable — Google-fióknál üres); hash:
  **Argon2id** (könyvtár: `Konscious.Security.Cryptography` vagy
  `Isopoh.Cryptography.Argon2` — az implementáló választ, aktív karbantartás
  alapján, indoklással).
- `POST /api/v1/auth/login/local` — a session/cookie/RBAC út VÁLTOZATLAN
  (ugyanaz a `SignInAsync`, mint a Google-ágon).
- Admin-jelszó beállítás az install-core bootstrapból (első indításkor
  generált egyszeri beállító-token vagy env — az implementáló a bootstrap
  meglévő mintájához igazítja, a választást dokumentálva).
- A 01/T6 failed-login throttling kulcsa (email+IP) a helyi loginra is.
- Login-oldal: a `GET /auth/config` (vagy meglévő config-endpoint) szerint
  Google-gomb és/vagy jelszó-űrlap.

**Lépések:** migráció DRAFT (⛔) → Argon2id-szolgáltatás + teszt (hash/verify,
timing) → command + endpoint + throttling-integráció + integrációs teszt
(happy, rossz jelszó, throttling-küszöb) → FE login-űrlap + vitest →
security-privacy.md §3.1 frissítés → ADR-0017 kiegészítés.
**Elfogadás:** Local-módban teljes login-flow Google nélkül · Google-mód
változatlan · throttling a helyi loginon bizonyított (teszt).
**Tilos:** jelszó-visszaállító e-mail-flow (backlog); a Google-út módosítása;
jelszó bárminemű logolása.

---

## L7 — Csomagolás, aláírás, kiadás (2–3 nap) — Függ: L5

**Ág:** `feature/installer-l7-packaging` · **Modell:** devops/sonnet.
**⛔ EMBERI KAPU:** kódaláíró tanúsítvány (Windows) és Apple Developer
notarizáció beszerzése emberi művelet — kérd el időben.

**Lépések:**
1. Windows: kódaláírt `.exe`; macOS: notarizált `.dmg`; Linux: `.AppImage`
   + `install.sh` — mind a CI-ból.
2. Letöltőoldal/release-struktúra: „online (kisebb)" és „offline (teljes,
   images.tar-ral)" változat.
3. Kézi tesztmátrix: friss Win11 / macOS / Ubuntu, Docker nélküli
   kiindulás, KÜLÖN gépen futó Ollamával — jegyzőkönyv-sablon.

**Elfogadás:** mindhárom OS-re letölthető, aláírt telepítő a release-ben.

---

## L8 — Dokumentáció és elfogadási próba (1–2 nap) — Függ: mind

**Ág:** `docs/installer-l8` · **Modell:** sonnet/haiku (doksi) + emberi próba.

**Lépések:**
1. `DELIVERY.md` kétutasra: „Egyszerű telepítés (telepítővel)" +
   „Fejlesztői telepítés" (a mai út VÁLTOZATLANUL dokumentálva marad).
2. Képes gyorsindító a next-next-finish útról.
3. **⛔ Elfogadási próba (a tényleges DoD):** nem-fejlesztő családtag
   telepíti külső segítség nélkül, külön gépen futó Ollamával —
   jegyzőkönyvezve. Ez emberi kapu: az orchestrátor előkészíti a
   jegyzőkönyv-sablont, a próbát ember végzi.

---

## Ütemterv és kritikus út

| Lépés | Effort | Függés |
|---|---|---|
| L0 | 1–2 nap | – |
| L1 | 2–3 nap | L0 |
| L2 | 2 nap | – (L1-gyel párhuzamos) |
| L3 | 3–4 nap | L1, L2 |
| L4 | 2 nap | L3 |
| L5 | 4–5 nap | L3, L4 |
| L6 (CR) | 3–4 nap | párhuzamos, ⛔ |
| L7 | 2–3 nap | L5 |
| L8 | 1–2 nap | mind |

**Összesen ~17–23 nap** (L6 nélkül a szűkebb becslés).
**Kritikus út: L1 → L3 → L5.** Az L6 technikailag opcionális, de a „laikus,
bármely gépen" cél gyakorlati beváltásához erősen ajánlott.

## Definition of Done

- [ ] Nem-fejlesztő felhasználó friss gépen, LAN-beli KÜLÖN Ollamával,
      parancssor nélkül működő Family OS-t kap.
- [ ] Ollama-cím felderíthető/tesztelhető a varázslóban; `/healthz/ready`
      a modell-hiányt nevesíti.
- [ ] Online és offline telepítő elérhető, aláírva.
- [ ] Frissítés és eltávolítás adatvesztés nélkül.
- [ ] A fejlesztői (`git clone` + build) út változatlanul működik.
- [ ] DELIVERY.md kétutas; elfogadási próba jegyzőkönyvezve.

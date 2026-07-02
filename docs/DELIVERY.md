# Family OS — Telepítési és Üzemeltetési Útmutató

**Verzió:** 1.0 — Fázis 12 (DevOps)  
**Célközönség:** Otthoni rendszergazda (te magad)  
**Környezet:** LAN-only, self-hosted, egyetlen otthoni PC

> Raspberry Pi-n futtatnád? Lásd [deploy-raspberry-pi.md](deploy-raspberry-pi.md)
> (arm64 kompatibilitás, cross-build workflow, Ollama méretezés gyenge
> hardverre) — ez a dokumentum a kiegészítője, nem helyettesíti.

---

## Tartalomjegyzék

1. [Előfeltételek](#1-előfeltételek)
2. [Első telepítés](#2-első-telepítés)
3. [Google OAuth beállítás](#3-google-oauth-beállítás)
4. [Backup és Restore](#4-backup-és-restore)
5. [TLS belső CA részletek](#5-tls-belső-ca-részletek)
6. [Frissítés](#6-frissítés)
7. [Lemez-titkosítás](#7-lemez-titkosítás)
8. [Incidens válasz](#8-incidens-válasz)
9. [Monitoring és logok](#9-monitoring-és-logok)
10. [Troubleshooting](#10-troubleshooting)

---

## 1. Előfeltételek

A következő szoftverekre van szükség a gazdagépen (otthoni PC):

### Kötelező

| Szoftver | Verzió | Telepítés |
|---|---|---|
| Docker Engine | 24+ | https://docs.docker.com/engine/install/ |
| Docker Compose | v2.20+ (beépített a Docker-be) | `docker compose version` |
| openssl | 3.x | Linux: `apt install openssl` / Windows: Git Bash-ből elérhető |
| curl | bármely | általában előre telepített |
| git | 2.x | https://git-scm.com/ |

### Opcionális

- **mkcert** — alternatív TLS megoldás, ha a belső CA-t nem akarod kézzel kezelni:  
  `brew install mkcert` (macOS) / `choco install mkcert` (Windows)
- **age** — backup titkosításhoz (ha BACKUP_AGE_PUBKEY be van állítva):  
  https://github.com/FiloSottile/age/releases

### Rendszerkövetelmények

- RAM: minimum 4 GB (Ollama modellel 8 GB ajánlott)
- Tárhely: minimum 20 GB szabad hely
- OS: Linux (ajánlott), Windows 10/11 Pro (Docker Desktop-pal), macOS

---

## 2. Első telepítés

### 2a. Repository clone

```bash
git clone <repo-url> family-os
cd family-os
```

### 2b. `.env` fájl létrehozása

Másold le a példa fájlt és töltsd ki a valós értékekkel:

```bash
cp .env.example .env
```

Nyisd meg a `.env` fájlt és cseréld le az összes `changeme_production` értéket:

```bash
# FONTOS: Mindkét jelszót állítsd be erős, véletlenszerű értékre!
POSTGRES_PASSWORD=<valami-hosszu-veletlenszeru-jelszo>
APP_DB_PASSWORD=<masik-hosszu-veletlenszeru-jelszo>

BOOTSTRAP_ADMIN_EMAIL=te@email.com
ALLOWED_EMAILS=te@email.com,partner@email.com

# Ezt later töltsd ki, ha a backup titkosítást akarod (lásd 4. fejezet)
BACKUP_AGE_PUBKEY=

# Google OAuth — lásd 3. fejezet
GOOGLE_CLIENT_ID=
GOOGLE_CLIENT_SECRET=
```

**Jelszó generáláshoz használhatod:**
```bash
openssl rand -hex 32
```

### 2c. TLS tanúsítvány generálás

```bash
make init-tls
```

Ez létrehozza a `docker/nginx/certs/` mappában:
- `ca.crt` — belső CA tanúsítvány (ezt kell a háztartási eszközökre telepíteni)
- `ca.key` — CA privát kulcs (tartsd biztonságban, nem kell eszközökre)
- `family-os.crt` — nginx szerver tanúsítvány
- `family-os.key` — nginx szerver privát kulcs

A default hostname `family-os.lan`. Más hostname-hez:
```bash
./scripts/init-tls-ca.sh nas.home.local
```

### 2d. CA tanúsítvány telepítése a háztartási eszközökre

A `docker/nginx/certs/ca.crt` fájlt minden eszközre fel kell telepíteni, ahol a Family OS-t böngészni fogod. Ez teszi lehetővé, hogy a böngésző megbízzon a tanúsítványban.

#### Windows

1. Másold át a `ca.crt` fájlt az eszközre (USB, hálózati mappa, stb.)
2. Kattints duplán a `ca.crt` fájlra → "Tanúsítvány telepítése"
3. Válaszd: "Helyi gép" → Tovább
4. "A következő tárolóba helyezem a tanúsítványt" → Tallóz → "Megbízható legfelső szintű hitelesítésszolgáltatók"
5. Befejezés → Igen (biztonsági figyelmeztetés)

Vagy parancssorból (adminisztrátorként):
```powershell
Import-Certificate -FilePath "ca.crt" -CertStoreLocation Cert:\LocalMachine\Root
```

#### macOS

```bash
sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain ca.crt
```

Vagy: Kulcstároló alkalmazás → Fájl → Importálás → ca.crt → Jobb klikk → "Adatok beállítása" → SSL: "Mindig megbízom"

#### iOS / iPadOS

1. Küldd el a `ca.crt` fájlt emailben, AirDrop-pal, vagy egy belső weboldalon
2. Érintsd meg a fájlt → "Profil letöltve" üzenet jelenik meg
3. Beállítások → Általános → VPN és Eszközkezelés → Family OS Internal CA → Telepítés
4. Beállítások → Általános → Névjegy → Tanúsítvány megbízhatósági beállítások → Family OS Internal CA → Kapcsold BE

#### Android

1. Másold a `ca.crt` fájlt az eszközre
2. Beállítások → Biztonság → Hitelesítő adatok → CA-tanúsítvány telepítése (vagy "Tárolóból telepítés")
3. Válaszd ki a `ca.crt` fájlt
4. Fogadd el a figyelmeztetést

> **Megjegyzés:** Android Pie (9+) óta az alkalmazások alapértelmezetten nem bíznak meg a felhasználói CA-kban. Ez a böngészős használathoz elegendő.

### 2e. Stack indítás

```bash
make up
```

Ez ekvivalens a `docker compose up -d` paranccsal. Az első indítás tovább tarthat, mert le kell tölteni a Docker image-eket.

Ellenőrzés:
```bash
docker compose ps
```

Minden service `healthy` állapotban kell legyen (kb. 2-3 percen belül). Az Ollama service-nek `start_period: 60s` van beállítva, tehát annak normal, ha az elején `starting` állapotban van.

Ha az api vagy workers service `unhealthy` marad:
```bash
docker compose logs api --tail 50
```

### 2f. Első login

1. Nyisd meg a böngészőben: `https://family-os.lan` (vagy `https://localhost` ha a gazdagépen nyitod)
2. A böngésző megkérdezi, megbízol-e a tanúsítványban — ha telepítetted a CA-t, ez nem jelenik meg
3. A "Bejelentkezés Google-lal" gombra kattintva add meg a `BOOTSTRAP_ADMIN_EMAIL`-ben megadott email-t
4. Az első bejelentkezés után admin jogosultságod lesz

> **Ha még nem állítottad be a Google OAuth-ot** (3. fejezet), a login gomb nem fog működni. Ebben az esetben először végezd el a Google OAuth konfigurálást.

---

## 3. Google OAuth beállítás

### 3a. Google Cloud Console — projekt és OAuth kliens

1. Nyisd meg: https://console.cloud.google.com/
2. Hozz létre egy új projektet: "Family OS" (vagy bármilyen név)
3. Bal menü → "APIs & Services" → "OAuth consent screen"
   - User Type: **External** (belső Google Workspace nélkül ez az egyetlen lehetőség)
   - App name: Family OS
   - User support email: a te email-ed
   - Developer contact: a te email-ed
   - Scopes: `email`, `profile`, `openid`
   - Test users: add hozzá az összes email-t, amelyet az `ALLOWED_EMAILS`-ben megadtál
4. Bal menü → "Credentials" → "Create Credentials" → "OAuth 2.0 Client ID"
   - Application type: **Web application**
   - Name: Family OS Web
   - Authorized JavaScript origins: `https://family-os.lan`
   - Authorized redirect URIs:
     - `https://family-os.lan/api/v1/auth/google/callback`
     - `https://localhost/api/v1/auth/google/callback` (fejlesztéshez)
5. "Create" → másold ki a **Client ID** és **Client Secret** értékeket

### 3b. Env változók kitöltése

A `.env` fájlban:
```
GOOGLE_CLIENT_ID=<Client ID>.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=GOCSPX-<Client Secret>
```

Majd indítsd újra a stack-et:
```bash
docker compose up -d api
```

### 3c. Gmail API engedélyezés (K1 — email feldolgozás)

Ha az email import funkciót is használni szeretnéd:

1. Google Cloud Console → "APIs & Services" → "Library"
2. Keresd meg: "Gmail API" → Enable
3. Az OAuth consent screen-en add hozzá a scope-ot:
   - `https://www.googleapis.com/auth/gmail.readonly`
4. A felhasználóknak újra be kell jelentkezni a scope jóváhagyásához

### 3d. Szükséges további konfiguráció

A Google OAuth app "Testing" módban 7 nap után lejár a refresh token. A production módhoz:
- Nyisd meg az OAuth consent screen-t
- "Publishing status" → "Publish App"
- Kis alkalmazásokhoz Google általában azonnal jóváhagyja

---

## 4. Backup és Restore

### 4a. Napi automatikus backup (backup profil)

A backup service egy külön Docker Compose profil alatt fut, hogy ne induljon el alapértelmezetten:

```bash
# Backup service-szel együtt indítás:
docker compose --profile backup up -d
```

A backup minden nap 02:00-kor fut le (crond által). A fájlok a `backups` Docker volume-ba kerülnek (`/data/backups/db/`).

**Backup titkosítás age-gel:**

1. Generálj egy age kulcspárt:
   ```bash
   age-keygen -o ~/.age/family-os.key
   # A kimenetből másold ki a public key-t (age1...)
   ```
2. A `.env` fájlban állítsd be:
   ```
   BACKUP_AGE_PUBKEY=age1xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
   ```
3. A privát kulcsot (`~/.age/family-os.key`) tárold biztonságos helyen (pl. jelszókezelőben)

### 4b. Manuális backup

```bash
make backup
```

Ez azonnal lefuttatja a backup scriptet. A kimenet megmutatja a fájl elérési útját és SHA-256 hash-ét.

### 4c. Restore eljárás

**1. lépés — Hash ellenőrzés:**
```bash
make restore /data/backups/db/family-os-20241215_020001.dump
```

A script automatikusan ellenőrzi a hash-t a manifest fájl alapján. Ha a hash nem egyezik, a restore leáll.

**2. lépés — Manuális restore (ha szükséges):**
```bash
docker compose run --rm \
  -v $(pwd)/scripts/restore.sh:/restore.sh:ro \
  backup /restore.sh /data/backups/db/family-os-20241215_020001.dump
```

Titkosított backup visszaállításához szükség van az age privát kulcsra:
```bash
# Dekriptálás helyi gépen:
age --decrypt -i ~/.age/family-os.key \
  family-os-20241215_020001.dump.age \
  -o family-os-20241215_020001.dump
```

**3. lépés — Ellenőrzés:**
```bash
make up
curl -f https://family-os.lan/healthz/ready
```

### 4d. Havi restore drill

Havonta érdemes elvégezni egy restore tesztet, hogy megbizonyosodj arról, hogy a backup valóban visszaállítható:

```bash
# Csak hash ellenőrzés, tényleges restore nélkül:
docker compose run --rm \
  -v $(pwd)/scripts/restore.sh:/restore.sh:ro \
  backup /restore.sh /data/backups/db/<legfrissebb>.dump --verify-only
```

---

## 5. TLS belső CA részletek

### 5a. A certs mappa tartalma

```
docker/nginx/certs/
├── ca.crt          — belső CA tanúsítvány (telepítsd az eszközökre)
├── ca.key          — CA privát kulcs (NE oszd meg, NE commitold)
├── family-os.crt   — nginx szerver tanúsítvány
└── family-os.key   — nginx szerver privát kulcs (NE oszd meg, NE commitold)
```

> **Fontos:** A `docker/nginx/certs/` mappa `.gitignore`-ban van. A kulcsok soha nem kerülnek a repóba. Mentsd el őket biztonságos helyre (jelszókezelő, titkosított USB).

### 5b. Tanúsítvány lejárat

A generált tanúsítványok **10 évig érvényesek** (3650 nap). A lejárat dátumát ellenőrizheted:

```bash
openssl x509 -in docker/nginx/certs/family-os.crt -noout -dates
```

### 5c. Újragenerálás

Ha a tanúsítvány lejár, vagy a hostname-t megváltoztatod:

```bash
# Régi tanúsítványok törlése:
rm docker/nginx/certs/family-os.{crt,key,csr}

# Új generálás (az eredeti CA-val, hogy ne kelljen újratelepíteni):
openssl genrsa -out docker/nginx/certs/family-os.key 2048
openssl req -new -key docker/nginx/certs/family-os.key \
  -out docker/nginx/certs/family-os.csr \
  -subj "/C=HU/O=Family OS/CN=family-os.lan"

cat > /tmp/ext.cnf << EOF
[v3_req]
subjectAltName = DNS:family-os.lan,DNS:localhost,IP:127.0.0.1
EOF

openssl x509 -req -days 3650 \
  -in docker/nginx/certs/family-os.csr \
  -CA docker/nginx/certs/ca.crt \
  -CAkey docker/nginx/certs/ca.key \
  -CAcreateserial \
  -out docker/nginx/certs/family-os.crt \
  -extfile /tmp/ext.cnf \
  -extensions v3_req

# nginx újraindítása:
docker compose restart web
```

Ha a CA-t is regenerálni kell (pl. kompromittálódott), akkor a `make init-tls` parancsot futtasd le, és telepítsd újra a CA tanúsítványt minden eszközre.

### 5d. Hostname beállítás a LAN-on

A `family-os.lan` hostname eléréséhez valamelyik opció:

**1. Router DNS (ajánlott):** A router admin felületén add hozzá a DNS rekordot:
- Hostname: `family-os.lan`
- IP: a gazdagép LAN IP-je (pl. `192.168.1.100`)

**2. hosts fájl (minden eszközön):** Minden eszközön add hozzá a `/etc/hosts` (Linux/macOS) vagy `C:\Windows\System32\drivers\etc\hosts` (Windows) fájlhoz:
```
192.168.1.100  family-os.lan
```

---

## 6. Frissítés

### Normál frissítés (kód változás)

```bash
git pull
docker compose build
docker compose up -d
```

### Image frissítés (pl. postgres, nginx)

```bash
docker compose pull
docker compose up -d
```

### Adatbázis migráció

A migráció az API indításakor automatikusan lefut (EF Core `MigrateAsync`). Ha manuálisan szeretnéd futtatni:

```bash
docker compose run --rm api dotnet-ef database update \
  --project src/FamilyOs.Infrastructure \
  --startup-project src/FamilyOs.Api
```

> **Figyelem:** Adatbázis migrációk előtt mindig készíts manuális backupot: `make backup`

---

## 7. Lemez-titkosítás

A Family OS érzékeny dokumentumokat (személyi iratok, pénzügyi dokumentumok) tárol. Erősen ajánlott a gazdagép merevlemezének titkosítása.

### 7a. Linux — LUKS

A teljes lemez titkosításhoz LUKS2 szükséges. Ha az OS még nincs titkosítva, a legegyszerűbb megoldás a friss telepítés titkosított partícióval.

Alternatívaként csak az adatkönyvtár titkosítható:

```bash
# Új titkosított konténer létrehozása (ha külön adatlemez van):
sudo cryptsetup luksFormat /dev/sdb1
sudo cryptsetup luksOpen /dev/sdb1 family-data
sudo mkfs.ext4 /dev/mapper/family-data
sudo mount /dev/mapper/family-data /var/lib/docker/volumes
```

Automatikus mount bootkor (`/etc/crypttab`):
```
family-data  /dev/sdb1  none  luks
```

Recovery key mentése:
```bash
sudo cryptsetup luksHeaderBackup /dev/sdb1 --header-backup-file luks-header-backup.img
```

### 7b. Windows — BitLocker

1. Vezérlőpult → Rendszer és biztonság → BitLocker meghajtótitkosítás
2. A rendszermeghajtónál (C:) → "BitLocker bekapcsolása"
3. Választhatsz: PIN + kulcs, vagy csak automatikus feloldás (TPM-mel)
4. A helyreállítási kulcsot mentsd el a Microsoft-fiókba VAGY nyomtasd ki és tárold biztonságos helyen
5. Titkosítás futtatása (akár több óra is lehet)

Docker Desktop-nál a volume-ok a WSL2-n belül vannak, a BitLocker védi ezeket is ha a WSL2 VHD a titkosított meghajtón van.

### 7c. macOS — FileVault

1. Rendszerbeállítások → Adatvédelem és biztonság → FileVault
2. FileVault bekapcsolása → Folytatás
3. Mentsd el a helyreállítási kulcsot — **ezt NE tárold a Family OS-ben**, hanem külső helyen (jelszókezelő, papír)
4. Az iCloud-ba való mentés kerülje ha teljes adatvédelmet akarsz

### 7d. Recovery key biztonságos tárolása

A lemeztitkosítási kulcsok és a Family OS CA privát kulcs tárolásához:
- **Bitwarden** (self-hosted vagy cloud) — ajánlott
- **KeePassXC** + titkosított USB meghajtón tárolt adatbázis
- Fizikai mentés: leírt kulcsok tűzálló széfben
- Soha ne tárold a Family OS saját dokumentumtárában (cirkuláris függőség!)

---

## 8. Incidens válasz

### 8a. Kompromittálás gyanúja — azonnali lépések

Ha gyanú merül fel, hogy illetéktelen személy hozzáfért a rendszerhez:

**1. Azonnali leállítás:**
```bash
docker compose down
```

**2. Hálózati izoláció** (ha szükséges): húzd ki a router Ethernet kábelt, vagy tiltsd le a WiFi-t a gazdagépen.

**3. Logok mentése** (leállítás előtt ha lehetséges):
```bash
docker compose logs --no-color > incident-$(date +%Y%m%d_%H%M%S).log
```

### 8b. Audit log lekérdezés

Az admin UI-ban: Beállítások → Audit log (admin jogosultság szükséges).

Parancssorból:
```bash
docker compose logs api | grep -E "(AUDIT|WARN|ERROR)" | tail -100
```

Adatbázisból közvetlenül:
```bash
docker compose exec postgres psql -U family_migrator -d family_os \
  -c "SELECT * FROM audit_logs ORDER BY created_at DESC LIMIT 50;"
```

### 8c. OAuth tokenek visszavonása

1. Google Account → Biztonság → Harmadik féltől származó alkalmazások → Family OS → Hozzáférés visszavonása
2. Az adatbázisban érvényteleníts minden tokent:
```bash
docker compose exec postgres psql -U family_migrator -d family_os \
  -c "UPDATE refresh_tokens SET revoked_at = NOW() WHERE revoked_at IS NULL;"
```
3. Indítsd újra az API-t:
```bash
docker compose restart api
```

### 8d. Data Protection kulcs rotáció

Az ASP.NET Core Data Protection kulcsok a `dp_keys` volume-ban vannak. Kompromittálódás esetén:

```bash
# Volume tartalmának törlése (minden aktív session érvénytelen lesz):
docker compose down
docker volume rm family-os_dp_keys
docker compose up -d
```

> **Hatás:** Minden bejelentkezett felhasználónak újra be kell jelentkeznie.

### 8e. Restore staging környezetre

Ha az adatok sérültek és tiszta állapotba kell visszaállni:

```bash
# Teljes stack leállítás + adatbázis volume törlése:
docker compose down
docker volume rm family-os_pgdata

# Friss start + restore:
docker compose up -d postgres
make restore /data/backups/db/<legfrissebb-clean-backup>.dump
docker compose up -d
```

---

## 9. Monitoring és logok

### 9a. Logok megtekintése

```bash
# Összes service:
docker compose logs -f

# Csak API:
docker compose logs -f api --tail 100

# Csak hibák:
docker compose logs api | grep -E "ERROR|CRITICAL|FATAL"

# Workers logok:
docker compose logs -f workers
```

A logok JSON fájlként tárolódnak a gazdagépen (`/var/lib/docker/containers/`), max 10 MB / fájl, max 3-5 fájl service-enként.

### 9b. Health endpoint-ok

```bash
# Liveness (a service él-e?):
curl -f https://family-os.lan/healthz/live

# Readiness (a service kész-e kiszolgálni?):
curl -f https://family-os.lan/healthz/ready

# Részletes (belső hálózatról):
curl http://localhost:8080/healthz/ready | python3 -m json.tool
```

Várható válasz (healthy):
```json
{
  "status": "Healthy",
  "results": {
    "database": { "status": "Healthy" },
    "ollama": { "status": "Healthy" }
  }
}
```

### 9c. Hangfire dashboard

A háttérfeladatok monitorozásához: `https://family-os.lan/hangfire`

Admin bejelentkezés szükséges. A dashboard mutatja:
- Feldolgozás alatt álló jobokat
- Sikeres/sikertelen futásokat
- Ütemezett feladatokat (email indexelés, dokumentum OCR)

### 9d. Docker resource monitoring

```bash
# CPU, RAM, hálózat, I/O statisztikák:
docker stats

# Egy service:
docker stats family-os-api-1
```

### 9e. Postgres monitoring

```bash
# Aktív kapcsolatok:
docker compose exec postgres psql -U family_migrator -d family_os \
  -c "SELECT count(*) FROM pg_stat_activity;"

# Táblaméret-ek:
docker compose exec postgres psql -U family_migrator -d family_os \
  -c "SELECT schemaname, tablename, pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) FROM pg_tables ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC LIMIT 10;"
```

---

## 10. Troubleshooting

### A stack nem indul el (`docker compose up` hibával leáll)

**Probléma:** `Error response from daemon: driver failed programming external connectivity`

**Ok:** A 80-as vagy 443-as port foglalt (pl. más webszerver fut).

**Megoldás:**
```bash
# Mi foglalja a portot?
sudo lsof -i :80
sudo lsof -i :443
# Vagy Windows-on:
netstat -ano | findstr :80
# Állítsd le az ütköző service-t, vagy módosítsd a docker-compose.yml portokat
```

---

**Probléma:** `postgres: unhealthy` — az API nem tud csatlakozni

**Megoldás:**
```bash
docker compose logs postgres | tail -20
# Ha "password authentication failed": ellenőrizd a .env fájl POSTGRES_PASSWORD értékét
# Ha "database does not exist": a volume sérülhetett, töröld és hozd létre újra:
docker volume rm family-os_pgdata
docker compose up -d postgres
```

---

### TLS hibák a böngészőben

**Probléma:** `NET::ERR_CERT_AUTHORITY_INVALID`

**Ok:** A CA tanúsítvány nincs telepítve az eszközre.

**Megoldás:** Kövesd a 2d. fejezet útmutatóját az adott operációs rendszerhez.

---

**Probléma:** `ERR_CERT_COMMON_NAME_INVALID` vagy `Subject Alternative Name missing`

**Ok:** A tanúsítványban nem szerepel a használt hostname.

**Megoldás:** Generáld újra a tanúsítványt a helyes hostname-mel:
```bash
rm docker/nginx/certs/family-os.{crt,key}
./scripts/init-tls-ca.sh <helyes-hostname>
docker compose restart web
```

---

### Google OAuth nem működik

**Probléma:** `redirect_uri_mismatch`

**Ok:** A Google Cloud Console-ban beállított redirect URI nem egyezik.

**Megoldás:**
1. Ellenőrizd a Google Cloud Console → Credentials → OAuth 2.0 Client → Authorized redirect URIs
2. Pontosan egyezzen: `https://family-os.lan/api/v1/auth/google/callback`
3. Mentés után akár 5 percet is várni kell

---

**Probléma:** `access_denied` — a felhasználó nem engedélyezett

**Ok:** Az email nincs az `ALLOWED_EMAILS` listában, vagy a Google OAuth app Testing módban van és a felhasználó nincs a test users listában.

**Megoldás:**
```bash
# Ellenőrizd a .env fájlt:
grep ALLOWED_EMAILS .env
# Adj hozzá emaileket vesszővel elválasztva, majd:
docker compose up -d api
```

---

### Backup nem fut le

**Probléma:** A backup service nem generál fájlokat

**Megoldás:**
```bash
# Ellenőrizd, hogy a backup profil el van-e indítva:
docker compose ps backup

# Ha nem fut, indítsd el:
docker compose --profile backup up -d backup

# Manuális teszt:
make backup
```

---

### Ollama nem válaszol (AI funkciók nem működnek)

**Probléma:** A dokumentum-összefoglalás és az OCR AI-assziszált feldolgozása nem működik

**Megoldás:**
```bash
# Ellenőrizd az Ollama service állapotát:
docker compose logs ollama | tail -30

# Teszteld kézzel:
curl http://localhost:11434/api/tags

# Ha a modell nem töltődött le:
docker compose exec ollama ollama pull llama3.2:3b
docker compose exec ollama ollama pull nomic-embed-text
```

---

### Magas CPU/RAM használat

**Ok:** Az Ollama modell töltődik be, vagy aktív dokumentumfeldolgozás folyik.

**Ellenőrzés:**
```bash
docker stats --no-stream
docker compose logs workers | tail -20
```

Ha a workers service folyamatosan magas CPU-n van:
```bash
# Függő Hangfire jobbok:
# → Nyisd meg a Hangfire dashboardot: https://family-os.lan/hangfire
# → Ellenőrizd az "Enqueued" és "Processing" jobokat
```

---

### Lemez megtelik

**Probléma:** `No space left on device`

**Megoldás:**
```bash
# Melyik Docker volume foglal legtöbbet?
docker system df -v

# Régi Docker image-ek törlése:
docker image prune -a

# Régi backupok törlése (ha a retain szám be van állítva, automatikus):
ls -lah /var/lib/docker/volumes/family-os_backups/_data/db/

# Logok mérete:
docker system df
```

Ha a `documents` volume telik meg: ez a valódi dokumentumtárolás, itt nem törölhetsz automatikusan — az admin UI-ból kell dokumentumokat archiválni/törölni.

---

*Ez a dokumentum a Family OS Fázis 12 (DevOps) implementáció részét képezi. Frissítve: 2026-06-28.*

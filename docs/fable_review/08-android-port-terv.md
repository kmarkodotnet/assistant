# Android port — fejlesztési dokumentáció

> Státusz: TERV v1.0 · Dátum: 2026-07-12
> Tárgy: a Family OS Angular frontend androidos portja
> Kapcsolódó: [ADR-0003](../decisions/ADR-0003-mobil-csak-lan.md) (mobil csak
> LAN-on), `docs/frontend-structure.md`, `scripts/init-tls-ca.sh` (belső CA),
> [04-cr-otletek.md](04-cr-otletek.md) (cr260712-05 push-csatorna)

---

# I. rész — Tervezés

## 1. Célok és nem-célok

**Célok**
- A meglévő Angular 20 alkalmazás teljes funkcionalitása Android-eszközön,
  natív app-élménnyel (ikon, splash, offline-jelzés, kamera).
- A LAN-only architektúra (ADR-0003) **változatlan marad**: az app csak
  otthoni hálózaton működik, ezt barátságosan jelzi máshol.
- Kód-újrafelhasználás maximalizálása: **egy** frontend-kódbázis marad.
- Telepítés családi körben: **sideload APK** (saját szerverről letöltve),
  Play Store publikáció nem cél.

**Nem-célok (MVP-ben)**
- Távoli (LAN-on kívüli) elérés — ADR-0003 explicit tiltja.
- Teljes offline munkavégzés — csak offline-*jelzés* + később sorba
  állított feltöltés (az ADR ezt „későbbi feature"-ként nevesíti).
- iOS port (a technológiaválasztás ne zárja ki, de nem tervezzük).

## 2. Technológiaválasztás

| Szempont | (A) PWA/TWA | **(B) Capacitor** ✅ | (C) Natív Kotlin | (D) Flutter/RN |
|---|---|---|---|---|
| Kód-újrafelhasználás | ~100% | **~95%** | 0% | ~0% |
| Google Sign-In | ⚠️ böngészős | **natív (Credential Manager)** | natív | natív |
| Kamera / megosztás-cél | korlátozott | **teljes (plugin)** | teljes | teljes |
| Belső CA (family-os.lan) | ⚠️ user-CA + böngésző-függő | **networkSecurityConfig-gal kezelhető** | teljes kontroll | kezelhető |
| Háttér-értesítés | nincs | **foreground service / helyi értesítés** | teljes | teljes |
| Karbantartási teher | minimális | **alacsony** | magas (2. kódbázis) | magas |
| Csapat-kompetencia | ✅ | ✅ (web) + kevés natív | ❌ | ❌ |

**Döntési javaslat: Capacitor** (ADR-t igényel — ADR-0016 javaslat).
A PWA/TWA azért esik ki, mert (1) a Google GIS bejelentkezés és a
(2) belső CA-s HTTPS LAN-on a böngésző/telepítés oldalán törékeny, és
(3) nincs hozzáférés kamerához/megosztás-célhoz/értesítésekhez azon a
szinten, amit egy családi dokumentum-app igényel. A natív/Flutter újraírás
a meglévő, tesztelt Angular-kódbázis mellett nem indokolható.

## 3. Architektúra

```
┌─────────────────────────────── Android app (APK) ──────────────────────────┐
│  Capacitor shell (Kotlin, minimális)                                       │
│  ├── WebView: a meglévő Angular build (dist/) BE-CSOMAGOLVA az APK-ba      │
│  ├── Pluginek: Camera, Share-target, Local Notifications, App (back gomb), │
│  │             Preferences, Biometric lock, @capacitor/http (l. 4.2)       │
│  └── Natív modul: Google Credential Manager (idToken) + NSD/mDNS discovery │
└──────────────┬──────────────────────────────────────────────────────────────┘
               │ HTTPS (belső CA, family-os.lan) — LAN-on
┌──────────────▼──────────────┐
│  meglévő nginx + API + hubok │   ← a backend NEM változik (1 kivétel: CORS,
└─────────────────────────────┘      lásd 4.2/B opció)
```

- Az Angular bundle az APK része (nem a szerverről töltődik) → az app
  szerver nélkül is elindul, és értelmes hibaképernyőt tud adni.
- A `environment`-kezelés bővül: az API-alap-URL futásidőben derül ki
  (szerver-felderítés, 4.4), nem build-time konstans.

## 4. Kritikus tervezési kérdések

### 4.1 Google bejelentkezés (a legfontosabb eltérés a webtől)

**Probléma:** a Google **tiltja** az OAuth-ot beágyazott WebView-ban
(`disallowed_useragent` hiba). A webes GIS-gombos flow tehát az appban
nem használható.

**Megoldás:** natív **Credential Manager / Sign in with Google** flow a
Kotlin-oldalon → a kapott **idToken** átadása a WebView-nak → a meglévő
`POST /api/v1/auth/login/google` hívás változatlanul validál
(`GoogleTokenValidator` audience-ellenőrzése miatt az Android OAuth-klienshez
**web client ID**-t kell audience-ként használni — a Google Cloud
Console-ban Android-klienst kell regisztrálni SHA-1 fingerprinttel, de az
idToken `aud`-ja a meglévő web client ID marad).

**Backend-hatás: nincs** — ugyanaz a végpont, ugyanaz a validáció.
A FE-ben a login-oldal platform-detektálással vagy a webes gombot, vagy a
natív hívást indítja (`Capacitor.isNativePlatform()`).

### 4.2 Session-cookie és origin-kérdés (döntést igényel)

**Probléma:** a webapp `__Host-family-os-session` cookie-t kap
(Secure, SameSite=Lax). Az app WebView-jának origin-je
`https://localhost` (Capacitor `androidScheme: https`), az API viszont
`https://family-os.lan` — ez **cross-site**: a Lax cookie a fetch/XHR
hívásokkal nem utazik.

**Opciók:**

| | Megoldás | Előny | Hátrány |
|---|---|---|---|
| **A ✅** | **`@capacitor/http`** (a `CapacitorHttp` a fetch-et natív HTTP-re cseréli, natív cookie-jar-ral) | nincs backend-változás; a cookie-kezelés a natív rétegben történik, a `__Host-` prefix és SameSite nem játszik | a SignalR WebSocket NEM megy át rajta — a hub-kapcsolatot külön kell kezelni (access_token query vagy cookie-header kézi beállítása a WS-handshake-nél) |
| B | az app a szerverről töltődik (remote URL, `server.url = https://family-os.lan`) | same-origin, a cookie natívan működik, webbel azonos viselkedés | az app szerver nélkül üres; frissítés = szerver-deploy; a Capacitor-pluginok remote-módban korlátozottak |
| C | SameSite=None + CORS allowlist a backendben | szabványos | a webes biztonsági szigor lazul mindenki számára — kerülendő |

**Javaslat: A opció**, a SignalR-kivétel kezelésével (a hub-oknál a
cookie-t a natív jar-ból kiolvasva kézzel tesszük a kapcsolat headerére,
vagy MVP-ben a mobil app polling-ra esik vissza a két hubnál — a
`notifications` feed amúgy is lekérdezhető). Spike-feladat igazolja (M1).

### 4.3 TLS — belső CA megbízhatóvá tétele

A LAN TLS belső CA-val megy (`init-tls-ca.sh` → `ca.crt`). Android 7+
a user-CA-kat appok felé alapból **nem** fogadja el.

**Megoldás:** `network_security_config.xml` az APK-ban:

```xml
<domain-config>
  <domain includeSubdomains="true">family-os.lan</domain>
  <trust-anchors>
    <certificates src="user"/>   <!-- a családtag telepíti a ca.crt-t -->
    <certificates src="system"/>
  </trust-anchors>
</domain-config>
```

Így a CA-t egyszer kell a telefonra telepíteni (Beállítások → CA telepítés),
és **csak** a family-os.lan domainre bízunk meg benne. Alternatíva
(kényelmesebb, de merevebb): a `ca.crt` beégetése az APK-ba
(`src="@raw/family_ca"`) — ekkor CA-rotációnál új APK kell. **Javaslat:**
mindkettő engedélyezése (user + beégetett), a telepítési útmutató a
user-CA utat dokumentálja.

### 4.4 Szerver-felderítés és LAN-detektálás

- **Felderítés:** első indításkor (a) mDNS/NSD keresés
  (`_familyos._tcp.local` — a compose nginx mellé egy avahi-service megy),
  (b) fallback: kézi URL-megadás + QR-kód a webes felületről
  („Mobil párosítás" oldal a settings-ben: QR = `https://family-os.lan` + CA-letöltési link).
- **LAN-detektálás futás közben:** a meglévő
  `GET /api/v1/system/heartbeat` + `offline-overlay` minta újrahasznosítható;
  az app hálózatváltás-eseményre (ConnectivityManager) is heartbeatel.
  Nem elérhető → a meglévő offline-overlay szövege ADR-0003 szerint:
  „Otthoni hálózat szükséges."

### 4.5 Értesítések (LAN-only kompatibilisen)

FCM **nem** használható a privacy-elv feladása nélkül (Google-felhőn át
menne minden értesítés-trigger). Lépcsős terv:

1. **MVP:** app előtérben → a meglévő SignalR/polling feed; helyi
   értesítés (Local Notifications plugin) az appon belül érkező
   reminder/digest eseményekből.
2. **MVP+1:** időzített **helyi** emlékeztető-tükrözés: az app szinkronizálja
   a saját user közelgő reminder-eit (`GET /api/v1/reminders`), és helyi
   értesítésként előjegyzi (AlarmManager) — így akkor is szól, ha az app
   nincs előtérben, hálózat nélkül is.
3. **Később:** önhostolt ntfy-csatorna (cr260712-05) — igazi push LAN-on
   kívül is *jelezni* tud („van 3 új értesítésed otthon"), tartalom nélkül,
   így az ADR-0003 szellemével összeegyeztethető.

### 4.6 Natív többletérték (ettől lesz „app" és nem becsomagolt web)

- **Megosztás-cél (Share Target):** PDF/kép megosztása bármely appból →
  Family OS feltöltés (a meglévő upload-endpoint + Idempotency-Key).
  A dokumentum-beviteli súrlódás legnagyobb csökkentője.
- **Kamera-feltöltés:** „Dokumentum fotózása" gomb → Camera plugin →
  upload → a meglévő OCR/AI pipeline dolgozik (a 07-es doksi C4
  vision-iránya ezt később felerősíti).
- **Biometrikus app-zár:** opcionális ujjlenyomat/arc a megnyitáshoz
  (a session-cookie 30 napos — a telefonon ez plusz védelem gyerek/vendég
  kéz ellen); `FLAG_SECURE` a képernyőképek ellen érzékeny nézeteken.
- **Android back-gomb:** Angular Router-integráció (App plugin), különben
  a back azonnal kilép.

### 4.7 A meglévő FE mobil-készültsége

Jó kiindulás: bottom-nav már létezik (`bottom-dashboard`, `bottom-search`…
testid-k), viewport meta rendben, offline-overlay + heartbeat kész.
Átvizsgálandó: táblázatos nézetek (audit-log, ai-jobs) kis képernyőn,
touch-célméretek, safe-area (kivágott kijelzők), `hu` dátum-inputok mobil
billentyűzettel, fájl-input viselkedés WebView-ban.

### 4.8 Terjesztés és frissítés

- **Aláírt APK**, CI-ban buildelve (gradle + keystore a CI-secretben).
- Letöltés a szerverről: nginx `/downloads/family-os.apk` + a settings
  „Mobil" oldala QR-kóddal. Verzió-ellenőrzés: az app induláskor
  összeveti a saját verzióját a `GET /api/v1/system/version`-nel
  (a 02-es doksi A3 hiánya itt válik szükségessé!) → „új verzió érhető el"
  banner.
- Play Store / belső teszt-track: nem cél; ha később mégis, a Capacitor
  nem zárja ki.

## 5. Kockázatok

| Kockázat | Hatás | Kezelés |
|---|---|---|
| CapacitorHttp + SignalR inkompatibilitás | realtime elvész | M1 spike; fallback: polling (a feed-végpontok megvannak) |
| Google idToken audience-hiba natív flow-ból | login nem megy | M2 spike legelőre; web client ID audience-ként |
| Belső CA telepítése a családtagoknak bonyolult | onboarding-súrlódás | QR-kódos párosító oldal + képes útmutató; beégetett CA fallback |
| WebView-verziók szórása (régi Android) | megjelenítési hibák | minSdk 29 (Android 10); WebView-frissítés feltétele a doksiban |
| Egy kódbázis — de platform-elágazások szaporodnak | FE-komplexitás | `PlatformService` absztrakció, elágazás CSAK ott (login, upload, back) |

---

# II. rész — Részfeladatok (work breakdown)

## M0 — Döntés és spike-ok (1–2 nap)

| # | Feladat | Elfogadás |
|---|---|---|
| M0.1 | ADR-0016: Android-port technológia (Capacitor) + session-stratégia (4.2/A) | ADR elfogadva |
| M0.2 | **Spike:** Capacitor hello-world a meglévő Angular buildből, fizikai eszközön, `https://family-os.lan` API-val (CA + networkSecurityConfig) | GET /healthz/ready válaszol az appból |
| M0.3 | **Spike:** natív Google Sign-In → idToken → `POST /auth/login/google` → session-cookie a CapacitorHttp jar-ban → `GET /auth/me` 200 | teljes login-kör bizonyított |
| M0.4 | **Spike:** SignalR-kapcsolat cookie-header kézi beállításával VAGY polling-fallback döntés | jegyzőkönyv az ADR-be |

## M1 — Projekt-alap és build-pipeline (2 nap)

| # | Feladat | Elfogadás |
|---|---|---|
| M1.1 | `frontend/android/` Capacitor-projekt; `capacitor.config.ts` (appId `hu.familyos.app`, androidScheme https); minSdk 29 | `npx cap run android` működik |
| M1.2 | `PlatformService` az FE-ben (`isNative()`, `getApiBaseUrl()`); az API-hívások base-URL-je futásidejű | web-build viselkedése változatlan (vitest zöld) |
| M1.3 | CapacitorHttp bekötése + interceptor-lánc ellenőrzése (trace-id, error-interceptor natív úton is fut) | dokumentumlista betölt az appban |
| M1.4 | CI: `make android-apk` target — Angular build → `cap sync` → gradle assembleRelease → aláírt APK artifact | CI-ból letölthető APK |
| M1.5 | App-ikon, splash, magyar app-név | eszközön natív megjelenés |

## M2 — Autentikáció (2–3 nap)

| # | Feladat | Elfogadás |
|---|---|---|
| M2.1 | Google Cloud: Android OAuth-kliens (SHA-1) regisztráció; doksi a `Auth:GoogleClientId` viszonyáról | idToken kiadódik |
| M2.2 | Kotlin: Credential Manager sign-in modul + Capacitor-bridge (`GoogleAuth.signIn(): idToken`) | natív fiókválasztó megjelenik |
| M2.3 | FE login-oldal elágazás: natív → bridge-hívás, web → GIS-gomb (változatlan) | mindkét platformon működő login |
| M2.4 | Session-életciklus: 401-interceptor natív úton; logout (cookie-jar ürítés); a `RevokedSessionChecker` viselkedés ellenőrzése | ki/be-jelentkezés stabil |
| M2.5 | (Opció) biometrikus app-zár + `FLAG_SECURE` a dokumentum-nézeten | beállításokból kapcsolható |

## M3 — Hálózat: felderítés, TLS, offline (2 nap)

| # | Feladat | Elfogadás |
|---|---|---|
| M3.1 | `network_security_config.xml` (user + beégetett CA, csak family-os.lan) | belső CA-s HTTPS zölden |
| M3.2 | Compose: avahi/mDNS service-hirdetés (`_familyos._tcp`) | NSD megtalálja a szervert |
| M3.3 | Első-indítás varázsló: NSD-keresés → találat megerősítése; fallback kézi URL + QR-beolvasás | friss telefonon 2 percen belül működő app |
| M3.4 | Webes „Mobil" beállítás-oldal: QR (szerver-URL + CA-letöltés + APK-link) | QR-ról végigvihető onboarding |
| M3.5 | Heartbeat hálózatváltás-eseményre; offline-overlay szövege ADR-0003 szerint | Wi-Fi-ről lelépve azonnali, érthető jelzés |

## M4 — Natív dokumentum-bevitel (2–3 nap)

| # | Feladat | Elfogadás |
|---|---|---|
| M4.1 | Kamera-feltöltés: Camera plugin → tömörítés (max ~2500 px hosszabb él, JPEG) → meglévő upload endpoint Idempotency-Key-jel | fotó → feldolgozási pipeline elindul |
| M4.2 | Share-target: PDF/kép „Megosztás → Family OS" intent-filter → upload-képernyő előtöltve | bármely appból megosztott PDF feltöltődik |
| M4.3 | Fájlválasztó (SAF) ellenőrzése a meglévő upload-oldalon WebView alatt | dokumentum-upload fájlból is megy |
| M4.4 | Feltöltési sor offline-hoz (ADR-0003 „későbbi feature" előkészítése): sikertelen upload → Preferences-be sorolva, LAN-ra érve retry | reptéri módban fotózott számla otthon magától felmegy |

## M5 — Értesítések (2 nap, a 4.5 lépcsői szerint)

| # | Feladat | Elfogadás |
|---|---|---|
| M5.1 | Local Notifications plugin; előtérben érkező SignalR/polling esemény → helyi értesítés | reminder-toast helyett rendszer-értesítés |
| M5.2 | Emlékeztető-tükrözés: saját user közelgő reminder-ei → AlarmManager-előjegyzés; szinkron app-nyitáskor + 6 óránként (WorkManager) | app zárva is szól a reminder |
| M5.3 | Értesítés-koppintás → app a megfelelő oldalon (deep link a feed `ActionUrl`-jére) | navigáció helyes |
| M5.4 | (Későbbre jegyzett) ntfy-integráció csatolása a cr260712-05-höz | backlog-item hivatkozással |

## M6 — Mobil UX-kör (2 nap)

| # | Feladat | Elfogadás |
|---|---|---|
| M6.1 | Android back-gomb ↔ Angular Router integráció; gyökér-oldalon dupla-back kilépés | intuitív navigáció |
| M6.2 | Safe-area (notch), status-bar szín, billentyűzet-viselkedés (resize) | vizuális átvizsgálás 2 eszközön |
| M6.3 | Táblázatos admin-nézetek mobil-audit (audit-log, ai-jobs): görgethető konténer vagy kártya-nézet | nincs vízszintes oldal-scroll |
| M6.4 | Touch-célok ≥ 48dp a lista-akciókon (reminder-gombok, doc-card) | átvizsgálás + javítások |

## M7 — Tesztelés és minőségkapuk (2 nap + folyamatos)

| # | Feladat | Elfogadás |
|---|---|---|
| M7.1 | A meglévő Playwright-készlet marad a web-rétegé; a platform-elágazások unit-tesztje (PlatformService mock) vitest-ben | CI zöld |
| M7.2 | Maestro (vagy Appium) smoke-flow eszközön/emulátoron: login (test-login végponttal, dev-only!) → dokumentumlista → kamera-mock-upload → reminder-értesítés | `make android-smoke` |
| M7.3 | Kézi tesztmátrix-doksi: 2 gyártó × 2 Android-verzió (10/14) × CA-telepítés útvonal | jegyzőkönyv sablon a docs/qa alá |
| M7.4 | A `docs/qa/ui-test-scenarios.md` bővítése Android-szakasszal (a 03-as review-doksi frissítésével együtt) | doksi-szinkron |

## M8 — Kiadás és üzemeltetés (1 nap)

| # | Feladat | Elfogadás |
|---|---|---|
| M8.1 | Keystore-kezelés doksi (hol él, ki fér hozzá, rotáció); CI-secret | dokumentálva |
| M8.2 | nginx `/downloads/` + APK-publikálás a CI-ból a szerverre | QR-ról telepíthető |
| M8.3 | `GET /api/v1/system/version` implementálása (02-es doksi A3!) + app-oldali verzió-banner | frissítés-jelzés működik |
| M8.4 | `docs/DELIVERY.md` + telepítési útmutató bővítése (CA-telepítés képekkel, párosítás) | családtag-teszt: külső segítség nélküli telepítés |

## Ütemterv-összegzés

| Mérföldkő | Effort | Függés |
|---|---|---|
| M0 döntés + spike-ok | 1–2 nap | – |
| M1 projekt-alap | 2 nap | M0 |
| M2 auth | 2–3 nap | M0.3 |
| M3 hálózat/TLS | 2 nap | M1 |
| M4 natív bevitel | 2–3 nap | M2 |
| M5 értesítések | 2 nap | M2 |
| M6 UX-kör | 2 nap | M1 |
| M7 tesztek | 2 nap | M2–M6 |
| M8 kiadás | 1 nap | mind |

**Összesen: ~16–19 munkanap.** Kritikus út: M0 spike-ok (login + cookie +
SignalR) — ha a 4.2/A stratégia a spike-on elbukik, a B opcióra (remote URL)
kell váltani, ami az ütemtervet nem nyújtja, de a 4.6-os pluginok körét
szűkíti.

## Definition of Done

- [ ] ADR-0016 elfogadva (technológia + session-stratégia + értesítési lépcső).
- [ ] Aláírt APK a CI-ból, QR-os telepítési úttal, verzió-jelzéssel.
- [ ] Login → dokumentum-feltöltés (kamera + megosztás) → keresés/Q&A →
      reminder-értesítés flow eszközön demózva.
- [ ] LAN-elhagyás/visszatérés kezelt (ADR-0003 szerinti üzenettel).
- [ ] Maestro-smoke zöld; tesztmátrix-jegyzőkönyv kitöltve.
- [ ] DELIVERY.md és qa-doksik frissítve; a web-funkcionalitás regressziója
      nulla (meglévő Playwright + vitest zöld).

# Android port (Capacitor) — Sonnet-implementációs mérföldkő-kártyák

> Státusz: IMPLEMENTÁCIÓS SPEC v1.0 · Dátum: 2026-07-12
> Forrás: `docs/fable_review/08-android-port-terv.md`
> Kapcsolódó: ADR-0003 (mobil csak LAN-on — VÁLTOZATLAN marad),
> `docs/frontend-structure.md`, `scripts/init-tls-ca.sh`, cr260712-05 (push).

## Zárt döntések (NE nyisd újra)

| Kérdés | Döntés | Indok röviden |
|---|---|---|
| Technológia | **Capacitor** (ADR-0016) | ~95% kód-újrafelhasználás; natív Google Sign-In; CA kezelhető networkSecurityConfig-gal; PWA/TWA a GIS-login + belső CA miatt törékeny |
| Session-stratégia | **A opció: `@capacitor/http`** natív cookie-jarral | nincs backend-változás; a `__Host-`/SameSite probléma a natív rétegben megszűnik |
| SignalR a natív úton | cookie-header kézi beállítása a WS-handshake-nél; ha a spike-on elbukik → **polling-fallback** a 2 hubnál | a feed-végpontok lekérdezhetők |
| TLS | `network_security_config.xml`: user-CA **és** beégetett CA együtt, csak `family-os.lan` domainre | egyszeri CA-telepítés; rotációnál a user-CA út él |
| Terjesztés | sideload APK a szerverről (nginx `/downloads/` + QR); Play Store NEM cél | családi kör |
| minSdk | 29 (Android 10) | WebView-szórás kezelése |
| Értesítés | FCM TILOS (privacy); lépcső: előtér-notification → helyi reminder-tükrözés → később ntfy (cr260712-05) | LocalOnly elv |
| Offline | csak jelzés + feltöltési sor; teljes offline munka NEM cél | ADR-0003 |
| iOS | nem tervezett (de a választás nem zárja ki) | — |

**⛔ EMBERI KAPUK:** ADR-0016 elfogadása (M0.1 után) · Google Cloud Console
Android-kliens regisztráció (M2.1 — emberi művelet) · keystore/CI-secret
létrehozás (M8.1).

## M0 fallback-döntési fa (a spike-ok eredménye szerint)

```
M0.3 login-spike OK?
├─ NEM (audience-hiba nem oldható) → ÁLLJ MEG, emberi döntés kell
│   (a teljes terv a natív loginon áll)
└─ IGEN → M0.4 SignalR-spike OK?
    ├─ IGEN → A opció teljes (cookie-header a WS-nél) → ADR-be rögzítve
    ├─ NEM, de CapacitorHttp+REST OK → A opció + polling-fallback a 2 hubnál
    └─ CapacitorHttp alapjaiban hibás (cookie-jar nem tartja a sessiont)
        → B opció: remote URL (server.url = https://family-os.lan)
          — ütemtervet nem nyújtja, de M4 plugin-kör szűkül; ADR-módosítás ⛔
```

---

## M0 — Döntés és spike-ok (1–2 nap) — MINDEN MÁS ELŐFELTÉTELE

**Ág:** `spike/android-m0` (a spike-kód ELDOBHATÓ — nem merge-elendő minőség).
**Modell:** sonnet.

**Olvasd el először:** ADR-0003 · `scripts/init-tls-ca.sh` (CA-generálás) ·
`Infrastructure/DependencyInjection.cs` (cookie-beállítások:
`__Host-family-os-session`, Secure, SameSite=Lax) · `GoogleTokenValidator`
(audience-ellenőrzés) · `frontend/` build-kimenet (dist-struktúra).

| # | Feladat | PASS-kritérium | FAIL-teendő |
|---|---|---|---|
| M0.2 | Capacitor hello-world a meglévő Angular buildből, FIZIKAI eszközön, `https://family-os.lan` API-val (CA telepítve + networkSecurityConfig) | `GET /healthz/ready` 200 az appból | CA/DNS hibakeresés max 2 kör, utána jelentés |
| M0.3 | Natív Google Sign-In (Credential Manager) → idToken → `POST /api/v1/auth/login/google` → session-cookie a CapacitorHttp jarban → `GET /auth/me` 200 | a teljes kör lefut; az idToken `aud`-ja a MEGLÉVŐ web client ID | ÁLLJ MEG — emberi döntés (lásd döntési fa) |
| M0.4 | SignalR-kapcsolat a natív cookie kézi header-beállításával | hub-esemény megérkezik az appban | polling-fallback rögzítése az ADR-be (nem blokkoló) |
| M0.1 | ADR-0016 DRAFT megírása a spike-jegyzőkönyvekkel (technológia + session + értesítési lépcső + fallback-eredmények) | ADR DRAFT kész — ⛔ emberi elfogadás | — |

**Google Cloud előfeltétel az M0.3-hoz:** Android OAuth-kliens SHA-1
fingerprinttel (⛔ emberi művelet — kérd el előre); az idToken audience-ként
a web client ID-t kell kérni a Credential Manager-hívásban.

---

## M1 — Projekt-alap és build-pipeline (2 nap) — Függ: M0

**Ág:** `feature/android-m1-scaffold` · **Modell:** sonnet.

**Olvasd el először:** `frontend/angular.json` + `package.json` (build-target) ·
a FE http-interceptorok (trace-id, error-interceptor) · `Makefile`.

**Lépések / elfogadás:**
1. **M1.1** `frontend/android/` Capacitor-projekt; `capacitor.config.ts`:
   appId `hu.familyos.app`, `androidScheme: 'https'`; minSdk 29.
   ✓ `npx cap run android` működik.
2. **M1.2** `PlatformService` az FE-ben (`isNative()`, `getApiBaseUrl()`);
   az API base-URL futásidejű (nem build-konstans). **Platform-elágazás
   KIZÁRÓLAG e service mögött** (login, upload, back — más helyen tilos).
   ✓ web-build viselkedése változatlan (vitest zöld).
3. **M1.3** CapacitorHttp bekötés + interceptor-lánc ellenőrzés (trace-id és
   error-interceptor a natív úton is fut). ✓ dokumentumlista betölt az appban.
4. **M1.4** CI: `make android-apk` — Angular build → `cap sync` →
   `gradle assembleRelease` → aláírt APK artifact. ✓ CI-ból letölthető APK.
5. **M1.5** App-ikon, splash, magyar app-név. ✓ natív megjelenés eszközön.

**Tilos:** FE-funkciók módosítása; backend-változás.

---

## M2 — Autentikáció (2–3 nap) — Függ: M0.3

**Ág:** `feature/android-m2-auth` · **Modell:** sonnet.

**Olvasd el először:** a login-oldal FE-komponense (GIS-gomb) ·
`GoogleTokenValidator` · a 401-interceptor + `RevokedSessionChecker` viselkedés
· M0.3 spike-jegyzőkönyv.

**Lépések / elfogadás:**
1. **M2.1** Google Cloud Android-kliens doksi: a `Auth:GoogleClientId`
   (web client ID) és az Android-kliens viszonyának leírása a
   DELIVERY/telepítési doksiban. ✓ idToken kiadódik. (⛔ a regisztráció emberi.)
2. **M2.2** Kotlin Credential Manager modul + Capacitor-bridge:
   `GoogleAuth.signIn(): idToken`. ✓ natív fiókválasztó megjelenik.
3. **M2.3** Login-oldal elágazás a `PlatformService`-en: natív → bridge,
   web → GIS-gomb (VÁLTOZATLAN). ✓ mindkét platformon működő login;
   web-login vitest zöld.
4. **M2.4** Session-életciklus: 401-interceptor a natív úton; logout =
   cookie-jar ürítés; `RevokedSessionChecker` viselkedés-ellenőrzés
   (revokált session az appban is kiléptet). ✓ ki/be-jelentkezés stabil.
5. **M2.5** (Opció — csak ha az M2.1–M2.4 kész és zöld) biometrikus app-zár +
   `FLAG_SECURE` a dokumentum-nézeten. ✓ beállításból kapcsolható.

**Backend-hatás: NULLA** — ugyanaz a végpont, ugyanaz a validáció. Ha úgy
tűnik, backend-módosítás kell: ÁLLJ MEG és jelentsd.

---

## M3 — Hálózat: felderítés, TLS, offline (2 nap) — Függ: M1

**Ág:** `feature/android-m3-network` · **Modell:** sonnet.

**Olvasd el először:** `init-tls-ca.sh` · docker-compose nginx-szekció ·
a `GET /api/v1/system/heartbeat` + FE `offline-overlay` minta.

**Lépések / elfogadás:**
1. **M3.1** `network_security_config.xml` — PONTOSAN ez a minta:
```xml
<domain-config>
  <domain includeSubdomains="true">family-os.lan</domain>
  <trust-anchors>
    <certificates src="user"/>
    <certificates src="system"/>
    <certificates src="@raw/family_ca"/>
  </trust-anchors>
</domain-config>
```
   (user + beégetett CA együtt; a bizalom CSAK a family-os.lan domainre.)
   ✓ belső CA-s HTTPS zöld.
2. **M3.2** Compose: avahi/mDNS service-hirdetés (`_familyos._tcp`).
   ✓ NSD megtalálja a szervert. (compose-módosítás — review-n jelezd.)
3. **M3.3** Első-indítás varázsló: NSD-keresés → találat megerősítése;
   fallback: kézi URL + QR-beolvasás. ✓ friss telefonon ≤ 2 perc a működésig.
4. **M3.4** Webes „Mobil" beállítás-oldal: QR (szerver-URL + CA-letöltés +
   APK-link). ✓ QR-ról végigvihető onboarding.
5. **M3.5** Heartbeat hálózatváltás-eseményre (ConnectivityManager);
   offline-overlay szöveg ADR-0003 szerint: „Otthoni hálózat szükséges."
   ✓ Wi-Fi-ről lelépve azonnali, érthető jelzés.

---

## M4 — Natív dokumentum-bevitel (2–3 nap) — Függ: M2

**Ág:** `feature/android-m4-capture` · **Modell:** sonnet.

**Olvasd el először:** a meglévő upload-endpoint + Idempotency-Key használat
(01/T2 utáni állapot!) · a FE upload-oldal · Capacitor Camera/Share plugin doksi.

**Lépések / elfogadás:**
1. **M4.1** Kamera-feltöltés: Camera plugin → tömörítés (hosszabb él max
   ~2500 px, JPEG) → meglévő upload endpoint Idempotency-Key-jel.
   ✓ fotó → pipeline elindul (OCR/AI dolgozik).
2. **M4.2** Share-target: PDF/kép „Megosztás → Family OS" intent-filter →
   upload-képernyő előtöltve. ✓ bármely appból megosztott PDF feltöltődik.
3. **M4.3** Fájlválasztó (SAF) ellenőrzése WebView alatt a meglévő
   upload-oldalon. ✓ fájlból is megy az upload.
4. **M4.4** Feltöltési sor offline-hoz: sikertelen upload → Preferences-be
   sorolva, LAN-ra érve retry (az Idempotency-Key dupla-feltöltés ellen véd).
   ✓ reptéri módban fotózott számla otthon magától felmegy.

**Tilos:** upload-endpoint módosítása; kép-előfeldolgozás az OCR-hez (F8 scope).

---

## M5 — Értesítések (2 nap) — Függ: M2

**Ág:** `feature/android-m5-notifications` · **Modell:** sonnet.

**Olvasd el először:** a notification-feed végpontjai + `ActionUrl` mező ·
a SignalR/polling fogadás a FE-ben · `GET /api/v1/reminders` válasz-struktúra.

**Lépések / elfogadás:**
1. **M5.1** Local Notifications plugin; előtérben érkező SignalR/polling
   esemény → rendszer-értesítés (toast helyett). ✓ működik.
2. **M5.2** Emlékeztető-tükrözés: a saját user közelgő reminder-ei →
   AlarmManager-előjegyzés; szinkron app-nyitáskor + 6 óránként (WorkManager).
   ✓ zárt app mellett is szól a reminder (hálózat nélkül is).
3. **M5.3** Értesítés-koppintás → deep link a feed `ActionUrl`-jére.
   ✓ navigáció helyes.
4. **M5.4** ntfy-integráció NEM készül — backlog-hivatkozás a cr260712-05-re.

**Tilos:** FCM bármilyen formában; backend-értesítési logika módosítása.

---

## M6 — Mobil UX-kör (2 nap) — Függ: M1

**Ág:** `feature/android-m6-ux` · **Modell:** sonnet (részek haikura adhatók).

Kiindulás (már JÓ): bottom-nav létezik (`bottom-dashboard`, `bottom-search`…
testid-k), viewport meta rendben, offline-overlay + heartbeat kész.

**Lépések / elfogadás:**
1. **M6.1** Android back-gomb ↔ Angular Router (App plugin); gyökér-oldalon
   dupla-back kilépés. ✓ intuitív navigáció.
2. **M6.2** Safe-area (notch), status-bar szín, billentyűzet-resize.
   ✓ vizuális átvizsgálás 2 eszközön, jegyzőkönyvvel.
3. **M6.3** Táblázatos admin-nézetek (audit-log, ai-jobs): görgethető
   konténer VAGY kártya-nézet mobilon. ✓ nincs vízszintes oldal-scroll.
4. **M6.4** Touch-célok ≥ 48dp a lista-akciókon (reminder-gombok, doc-card).
   ✓ átvizsgálás + javítások.

**Tilos:** desktop-nézetek átformálása; design-rendszer csere.

---

## M7 — Tesztelés és minőségkapuk (2 nap + folyamatos) — Függ: M2–M6

**Ág:** `test/android-m7` · **Modell:** sonnet.

**Lépések / elfogadás:**
1. **M7.1** Platform-elágazások unit-tesztje (PlatformService mock) vitestben;
   a meglévő Playwright-készlet marad a web-rétegé. ✓ CI zöld.
2. **M7.2** Maestro smoke-flow emulátoron: login (test-login végponttal —
   KIZÁRÓLAG dev-környezet, a 01/T3 Production-tiltása érvényes!) →
   dokumentumlista → kamera-mock-upload → reminder-értesítés.
   ✓ `make android-smoke` zöld.
3. **M7.3** Kézi tesztmátrix-doksi: 2 gyártó × 2 Android-verzió (10/14) ×
   CA-telepítési út. ✓ jegyzőkönyv-sablon a `docs/qa/` alá.
4. **M7.4** `docs/qa/ui-test-scenarios.md` Android-szakasz (a 03/TD5
   frissítéssel összehangolva — ha az még nem ment be, jelezd a konfliktust).

---

## M8 — Kiadás és üzemeltetés (1 nap) — Függ: mind

**Ág:** `feature/android-m8-release` · **Modell:** sonnet.
**⛔ EMBERI KAPU:** keystore létrehozás + CI-secret feltöltés emberi művelet.

**Lépések / elfogadás:**
1. **M8.1** Keystore-kezelési doksi (hol él, ki fér hozzá, rotáció);
   CI-secret hivatkozás. ✓ dokumentálva. (Titok a repóba SOHA.)
2. **M8.2** nginx `/downloads/` + APK-publikálás CI-ból. ✓ QR-ról telepíthető.
3. **M8.3** App-oldali verzió-banner a `GET /api/v1/system/version`-ből —
   **előfeltétel: 02/C-A3 kártya kész** (ha nincs, előbb azt).
   ✓ frissítés-jelzés működik.
4. **M8.4** `docs/DELIVERY.md` + telepítési útmutató (CA-telepítés képekkel,
   párosítás). ✓ családtag-teszt: külső segítség nélküli telepítés.

---

## Ütemterv és kritikus út

| Mérföldkő | Effort | Függés |
|---|---|---|
| M0 | 1–2 nap | – |
| M1 | 2 nap | M0 |
| M2 | 2–3 nap | M0.3 |
| M3 | 2 nap | M1 |
| M4 | 2–3 nap | M2 |
| M5 | 2 nap | M2 |
| M6 | 2 nap | M1 |
| M7 | 2 nap | M2–M6 |
| M8 | 1 nap | mind |

**Összesen ~16–19 nap.** Kritikus út: M0 spike-ok — az eredményük dönti el
az A/B session-stratégiát (lásd a döntési fát fent). M3∥M4∥M5∥M6 részben
párhuzamosítható külön worktree-ben.

## Definition of Done

- [ ] ADR-0016 elfogadva (⛔ emberi kapu).
- [ ] Aláírt APK CI-ból, QR-os telepítési út, verzió-jelzés.
- [ ] Login → feltöltés (kamera + megosztás) → keresés/Q&A →
      reminder-értesítés flow eszközön demózva.
- [ ] LAN-elhagyás/visszatérés kezelt (ADR-0003 üzenettel).
- [ ] Maestro-smoke zöld; tesztmátrix kitöltve.
- [ ] DELIVERY.md + qa-doksik frissítve; web-regresszió nulla
      (Playwright + vitest zöld).

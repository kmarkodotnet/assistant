# Bárhol futtatható Family OS — „next-next-finish" telepítő terve

> Státusz: TERV v1.0 · Dátum: 2026-07-12
> Cél: az alkalmazás bármilyen számítógépen futtatható legyen (feltéve, hogy a
> hálózaton elérhető egy Ollama), és készüljön belőle varázslós telepítőkészlet.
> Kapcsolódó: `docs/DELIVERY.md` (jelenlegi kézi telepítés), docker-compose.yml,
> [02-doksik-vs-kod.md](02-doksik-vs-kod.md) (A3/A10 tételek),
> [08-android-port-terv.md](08-android-port-terv.md) (CA-terjesztés)

---

## 1. Mi akadályozza ma a hordozhatóságot? (akadály-leltár)

A jelenlegi telepítés (DELIVERY.md) fejlesztői munkafolyamat, nem terméké:

| # | Akadály | Hol |
|---|---|---|
| B1 | `git clone` + kézi `.env`-szerkesztés kell (jelszavak, kulcsok kézzel) | DELIVERY.md 2a–2b |
| B2 | A Google OAuth klienst minden telepítőnek magának kell kiváltania a Google Cloud Console-ban | DELIVERY.md 3. fejezet |
| B3 | TLS: shell-szkript (`init-tls-ca.sh`) + kézi CA-telepítés minden eszközre | DELIVERY.md 2c–2d |
| B4 | Az Ollama-URL **fixen drótozott** a compose-ban (`host.docker.internal:11434`), nem env-paraméter; a modellnév szintén | docker-compose.yml:44,75 |
| B5 | A képek helyben buildelődnek (`build:` szekciók) — a célgépen fordul a teljes stack, .NET SDK/node nélkül csak Dockerrel, de lassan | docker-compose.yml |
| B6 | Konfiguráció-módosítás (SMTP, retention) csak `.env`/appsettings + restart — a `PATCH /settings/system` no-op (02-es doksi A10) | SettingsModule |
| B7 | Nincs `system/version` végpont, nincs frissítés-jelzés (02-es doksi A3) | Program.cs |
| B8 | Első admin: `BOOTSTRAP_ADMIN_EMAIL` env — a wizard-élményhez futásidejű első-indítás folyamat kell | LoginGoogleCommandHandler |
| B9 | A cookie `SecurePolicy.Always` + `__Host-` prefix → HTTPS kötelező, tehát a TLS-t a telepítőnek automatizálnia kell (nem kikerülhető) | Infrastructure/DependencyInjection.cs |
| B10 | Backup-passphrase, allowlist, e-mail beállítás — mind kézi env | .env.example |

## 2. Alapdöntések (a terv ezekre épül — ADR-t igényelnek)

### D1 — Futtatókörnyezet: Docker-alapú marad ✅ (ADR-0017)

| | Docker-alapú telepítő | Natív (Windows-service + bundled PostgreSQL) |
|---|---|---|
| Újrafelhasználás | a teljes meglévő stack változatlan | API/worker átcsomagolás + pgvector Windows-build + saját szolgáltatás-kezelés |
| Telepítő dolga | Docker meglétének biztosítása + bundle + konfig | minden komponens telepítése kézzel |
| Effort | **kicsi-közepes** | nagy (több hét) |
| Hátrány | Docker Desktop függés (Windows: WSL2) | — |

**Döntés-javaslat:** első körben Docker-alapú; a natív csomagolás későbbi
opció, ha a Docker Desktop függés a gyakorlatban súrlódásnak bizonyul.

### D2 — Bejelentkezés Google nélkül is (a legnagyobb termék-változás)

A B2 akadály varázslóval nem automatizálható (a Google Cloud-regisztráció
mindig kézi). Ezért a telepíthető termékhez kell egy **helyi
fiók-alternatíva**:

- új `Auth:Mode = Local | Google | Both` konfiguráció;
- Local mód: e-mail + jelszó (Argon2id hash), a meglévő
  `UserAccount`-ra épülve (`PasswordHash` oszlop, nullable — Google-fiókoknál üres);
- a session/cookie/RBAC réteg **változatlan** (ugyanaz a `SignInAsync` út,
  mint a Google-ágon);
- a Google-integráció opcionális extra marad, a wizard „haladó" lépése.

Ez az egyetlen tétel, ami érdemi backend-fejlesztés (~3–4 nap), de enélkül
nincs „next-next-finish" — a Google Cloud Console-lépés minden nem-fejlesztő
felhasználót elveszít.

### D3 — A konfiguráció helye: minimál-telepítő + webes első-indítás varázsló

A telepítő csak a *futtatókörnyezetet* rakja össze; minden érdemi beállítás
(admin-fiók, Ollama, modell, e-mail) a **böngészős first-run wizardban**
történik — ott, ahol amúgy is él az alkalmazás, magyarul, validálva.
Így a telepítő OS-független logikát nem duplikál.

### D4 —
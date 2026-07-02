# Review — DELIVERY.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)
> A review a repó tényleges kódja ellen is ellenőrzött (src/, docker-compose.yml).

## Összegzés

Jó felépítésű, gyakorlatias runbook: telepítés, OAuth-setup, backup/restore
+ drill, TLS-CA eszközönkénti telepítési útmutatóval, incidens-runbook,
troubleshooting. A célközönségnek (technikás otthoni admin) megfelelő
mélységű. **A fő gond: több parancs a valóságban nem létező táblákra /
rossz modellnévre hivatkozik** — egy runbookban ez élesben derül ki,
a legrosszabbkor.

## Hibák

### 1. Nem létező DB-objektumokra hivatkozó SQL (súlyos — runbook-hiba)
- 8b: `SELECT * FROM audit_logs ORDER BY created_at DESC` — a tényleges
  tábla **`app.audit_log`** (ellenőrizve:
  `AuditLogConfiguration.cs: ToTable("audit_log", "app")`), az oszlop
  pedig nem `created_at`. A parancs élesben hibát dob.
- 8c: `UPDATE refresh_tokens SET revoked_at = NOW()` — **nincs
  `refresh_tokens` tábla** sem a sémában, sem a kódban (a session
  cookie-alapú; a Gmail-tokenek a `source.config_json`-ban élnek).
  Az incidens-lépést a valós mechanizmusra kell átírni (DP-kulcs rotáció
  — a 8d ezt már helyesen fedi — + Google-oldali revoke).

### 2. Modellnév-káosz: `llama3.2:3b` vs. `gpt-oss:20b` (közepes)
A 10. fejezet trouble-shootingja `ollama pull llama3.2:3b`-t ír, és a
kód defaultja is ez (`OllamaOptions.DefaultModel = "llama3.2:3b"`) — az
összes tervező doksi (product-vision, architecture, ai-pipeline,
implementation-plan) viszont `gpt-oss:20b`-t rögzít. Az implementáció
láthatóan pragmatikusan kisebb modellre váltott (a 4–8 GB RAM
követelmény is csak ezzel reális, a 20b-hez ~16 GB kellene). Ez a
divergencia **ADR-t érdemel** (modellválasztás felülvizsgálva), és a
doksikat egy irányba kell húzni — most a telepítő nem tudja, melyik
modellt húzza le.

### 3. Auth-flow eltérés a tervektől (közepes)
A 3a redirect URI-ja (`/api/v1/auth/google/callback`) szerver-oldali
authorization-code flow-t jelez — az api-design.md 3.1 viszont
`POST /auth/login/google { idToken }` kliens-oldali flow-t definiál, a
security-privacy.md 3.1 pedig „Authorization Code with PKCE”-t. Három
leírás, két különböző flow. A megvalósult változatot kell normatívvá
tenni és a másik két doksit igazítani.

### 4. Migráció-futtatás ellentmondás (kicsi)
6. fejezet: „A migráció az API indításakor automatikusan lefut” — a
database-schema.md 7. szerint prod-on külön `migrate` lépés a terv
(és a `family_migrator` role szétválasztás erre épül). Ha a startup-
migráció a valóság, a séma-doksi frissítendő; ha nem, a runbook.
(Ugyanez: security-privacy.review.md #6.)

### 5. Kisebb észrevételek
- 1. fejezet RAM-követelmény (4–8 GB) csak a 3b modellel áll; lásd #2.
- 4c: a `make restore <fájl>` argumentum-átadás Makefile-ban szokatlan
  (`make restore FILE=...` a bevett) — ellenőrizendő a tényleges
  Makefile-lal.
- 9b: a részletes health-válasz Ollama-t „Healthy”-ként várja — az
  architecture.review.md #2 szerint az Ollama ne legyen hard readiness-
  feltétel; ha marad, a runbookban jelezni kell, hogy Ollama-leállásnál
  a ready endpoint 503-at ad, miközben a webes rész működik.
- A dátum-példák a backup fájlnevekben `20241215` — 2026-os projektben
  zavaró, kozmetika.
- A Raspberry Pi kiegészítő doksi linkje él (deploy-raspberry-pi.md
  létezik) — rendben.

## Erősségek (megőrzendő)

- CA-tanúsítvány telepítés eszközönként (Windows/macOS/iOS/Android)
  lépésről lépésre — pont ez szokott hiányozni.
- Restore-drill (`--verify-only`) és hash-ellenőrzés a manifestből.
- Troubleshooting valós hibaüzenetekkel (redirect_uri_mismatch,
  ERR_CERT_*, port-ütközés).
- A Google OAuth „Testing mód 7 napos refresh token” csapda explicit
  dokumentálva (3d) — ez sok órányi debuggolást spórol meg.

## Verdikt

Használható runbook, de az #1 SQL-hibák javítása kötelező (incidens
közben nem debuggolunk), és a #2–#3 divergenciákat ADR-rel kell lezárni.

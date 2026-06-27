# Epic M — Deploy + ops — dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` (FULL — baseline)
> - `architecture.md` (FULL — különösen §9 Docker Compose topológia, §12 megfigyelhetőség)
> - `security-privacy.md` (FULL — különösen §6 encryption, §7.6 lemez, §11 üzemeltetés, §13 red team)
> - ADR-0003 (LAN-only — FULL, minden M-story érinti)
> - ADR-0004 (Gmail API — K1-mel együttműködik)
> - `reminder-engine.md` §12 (konfiguráció minta)
> - `database-schema.md` §1 (env), §7 (backup hivatkozás)
> - `api-design.md` §4 (System healthz)
> - `frontend-structure.md` §12 (LAN-detection)
>
> **Story-k:** M1, M2, M3, M4
> **Fázis:** Fázis 1 (M1 csontváz) → Fázis 12 (M2-M4 teljes)

---

## Áttekintés

Az epic nem FE/BE bontásra megy, mert tisztán infra és üzemeltetés.
A devops agent (`CLAUDE.md` szerint sonnet) hajtja végre, code-reviewer
(opus) validálja minden security-érintette PR-on.

## Taskok

### T-MOP-01 — Docker Compose alap stack (M1)
- **Cél:** `make up` indít mindent.
- **Fájlok:**
  - `docker-compose.yml`
  - `docker/api.Dockerfile`
  - `docker/workers.Dockerfile`
  - `docker/web.Dockerfile` (nginx-szel Angular static build)
  - `docker/postgres.Dockerfile` (a `pgvector/pgvector:pg16` image-en
    `hu-HU` ICU + extension pre-load).
  - `docker/ollama.Dockerfile` (vagy a base image).
  - `Makefile` target-ek: `up`, `down`, `build`, `test`, `pull-models`,
    `migrate`, `backup`, `restore`.
- **AC:**
  - [ ] 5 service indul: `api`, `workers`, `web`, `postgres`, `ollama`.
  - [ ] Csak LAN-bind: `127.0.0.1` és LAN IP-k, nincs `0.0.0.0` external.
  - [ ] Volume-ok: `pgdata`, `documents`, `ollama-models`, `logs`,
        `backups`.
  - [ ] A `web` service `/api` requesteket az `api`-ra proxyzza.

### T-MOP-02 — `gen:api` integráció a build pipeline-ba
- **Cél:** a frontend a backend OpenAPI sémájából frissít.
- **Fájlok:**
  - `Makefile` `gen-api` target.
  - CI workflow (`.github/workflows/ci.yml`).
- **AC:**
  - [ ] Backend build → OpenAPI export → frontend client generálás → FE build.

### T-MOP-03 — nginx + belső CA TLS (M2)
- **Cél:** HTTPS LAN-belül belső CA tanúsítvánnyal.
- **Fájlok:**
  - `docker/nginx/Dockerfile`
  - `docker/nginx/family-os.conf`
  - `scripts/init-tls-ca.sh` (mkcert).
  - `docs/DELIVERY.md` TLS szakasz.
- **AC:**
  - [ ] HTTPS endpoint LAN-belül (`https://family-os.lan` vagy mDNS).
  - [ ] Belső CA tanúsítvány telepíthető a háztartási eszközökre
        (útmutató Win/Mac/iOS/Android-ra).
  - [ ] CSP fejléc beállítva.

### T-MOP-04 — Healthcheck integráció (M3)
- **Fájlok:**
  - kiegészítések `docker-compose.yml`-ben (healthcheck minden service-re).
  - `docker/healthcheck.sh` minden konténerre.
- **AC:**
  - [ ] Compose `healthy` állapot a `depends_on: condition: service_healthy`-re.
  - [ ] `api` várja a `postgres healthy`-t.
  - [ ] `workers` várja a `postgres + ollama` healthy-t (ollama-ra
        opcionális dependency).

### T-MOP-05 — OpenTelemetry + Prometheus (opcionális MVP)
- **Fájlok:**
  - `src/FamilyOs.Api/Program.cs` kiegészítés OTel SDK-val.
  - `docker-compose.yml` `prometheus` service (opcionális profil).
- **AC:**
  - [ ] Trace export console (dev) vagy OTLP endpoint (prod).
  - [ ] Metrikák: AI job queue méret, AI hívás latency, OCR latency,
        reminder dispatch count, DB pool használat.
  - [ ] **Csak ha az MVP idő engedi.** Halasztható.

### T-MOP-06 — Lemez-titkosítás dokumentáció
- **Cél:** host OS szintű telepítési útmutató.
- **Fájlok:**
  - `docs/DELIVERY.md` lemez-titkosítás szakasz (LUKS / BitLocker / FileVault).
- **AC:**
  - [ ] Lépésről-lépésre útmutató mindhárom OS-re.
  - [ ] Recovery key biztosítás javaslat.

### T-MOP-07 — Backup automatizálás Docker-ben
- **Fájlok:**
  - `docker-compose.yml` `backup` service `crond`-dal.
  - `scripts/backup.sh` (Epic K T-KBE-10).
  - `scripts/restore.sh` (Epic K T-KBE-11).
- **AC:**
  - [ ] Napi cron 02:00 helyi időben.
  - [ ] 30 nap retention.
  - [ ] Sha256 manifest.

### T-MOP-08 — Log rotation
- **Fájlok:**
  - `docker-compose.yml` log driver konfig: `max-size`, `max-file`.
  - `appsettings.json` Serilog file rolling: 10 MB × 14 fájl.
- **AC:**
  - [ ] Stdout-ot rotálja Docker.
  - [ ] Filebound logok rotálódnak rolling-szerűen.

### T-MOP-09 — `docs/DELIVERY.md` runbook (M4)
- **Cél:** telepítés, üzemeltetés, incident response egyetlen doksiban.
- **Fájlok:**
  - `docs/DELIVERY.md`
- **AC:**
  - [ ] **Telepítés**: prereq (Docker, mkcert), `make up`, első login.
  - [ ] **Setup**: Google OAuth client készítés, allowlist, bootstrap admin.
  - [ ] **Backup és restore**: `make backup`, `make restore`.
  - [ ] **Restore drill**: havi rutin, staging restore.
  - [ ] **Incident response**: compromise gyanújára 5 lépés
        (security-privacy.md §11.5).
  - [ ] **TLS belső CA**: kézi tanúsítvány-telepítés guide.
  - [ ] **Frissítés**: `docker compose pull && up -d`, migrációk.
  - [ ] Egy átlagos technikai felhasználó követni tudja, 60 percen
        belül élesít.

### T-MOP-10 — Privacy assertion CI gate
- **Cél:** `LocalOnly` privacy szivárgás regresszió tilos.
- **Fájlok:**
  - `tests/FamilyOs.Infrastructure.Ai.Tests/AiProviderPrivacyGuardTests.cs`
    (már Epic D-ben).
  - CI workflow: ez a teszt **kötelező zöld** a merge-höz.
- **AC:**
  - [ ] CI red gate.

### T-MOP-11 — ZAP baseline scan (havi nightly)
- **Cél:** automated security regresszió.
- **Fájlok:**
  - `.github/workflows/security.yml`
- **AC:**
  - [ ] ZAP baseline scan a `staging` ellen.
  - [ ] Warning gate (riasztás, de nem blokk).
  - [ ] Riport-link a CI artifaktban.

### T-MOP-12 — Telepítési smoke teszt
- **Cél:** tiszta gépen + `DELIVERY.md` lépéseivel a stack indul.
- **Fájlok:**
  - `tests/integration/install-smoke-test.sh` (manuális futtatás).
- **AC:**
  - [ ] Tiszta VM-en végigfutva 60 percen belül login lehetséges.
  - [ ] Egy minta-dokumentum feldolgozása sikeres.

---

## Megvalósítási sorrend

```
Fázis 1 (kezdetkor): T-MOP-01 csontváz, T-MOP-04 alap healthcheck.
Fázis 12 (hardening): T-MOP-02 → 03 → 05 → 06 → 07 → 08 → 09 →
                       T-MOP-10 → 11 → 12.
```

## Epic-DoD

- [ ] `make up` egyetlen paranccsal indítja az MVP-t.
- [ ] HTTPS belső CA-val működik a LAN-on.
- [ ] Backup napi script + restore drill dokumentálva.
- [ ] Healthcheck minden service-en.
- [ ] `docs/DELIVERY.md` 60 perces telepítési smoke teszttel ellenőrizve.
- [ ] Privacy assertion CI red gate.
- [ ] ZAP havi nightly riport.
- [ ] Git tag `v1.0`.

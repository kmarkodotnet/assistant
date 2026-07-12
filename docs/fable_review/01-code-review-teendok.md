# Code review teendők — Family OS

> Státusz: REVIEW v1.0 · Dátum: 2026-07-12 · Forrás: teljes kódbázis-átvizsgálás (Fable review)
> Vizsgált állapot: `ai-proposal-learn` ág, commit `d2526a4`
> Ellenőrzött kapuk: `dotnet build` ✅ (0 warning) · backend tesztek ❌ 271/273 (2 FAIL) ·
> frontend `vitest` ✅ 172/172

---

## P1 — Javítandó hibák

### 1.1 IdempotencyMiddleware: rossz pipeline-pozíció + korlátlan cache

**Fájlok:** `src/FamilyOs.Api/Middleware/IdempotencyMiddleware.cs`, `src/FamilyOs.Api/Program.cs:84`

A middleware a `UseAuthentication()` **előtt** fut, ezért a
`context.User.FindFirst("sub")` mindig null — minden felhasználó a közös
`anon:` kulcstérbe kerül. Azonos `Idempotency-Key` esetén az egyik user
megkaphatja a másik cache-elt válaszát. További gondok:

- a statikus `ConcurrentDictionary` korlátlanul nő, nincs TTL/pruning
  (memórialeak, a kulcs örökre foglalt);
- hibaválaszokat (pl. 500) is cache-el — a kliens ugyanazzal a kulccsal
  retry-olva örökre a cache-elt hibát kapja.

**Teendők:**
- [ ] A middleware regisztrációja kerüljön a `UseAuthentication()` mögé.
- [ ] Csak 2xx válasz kerüljön a cache-be.
- [ ] Lejárat bevezetése (pl. `IMemoryCache`, sliding expiration; az
      api-design.md 1.9 célértéke 24 óra).
- [ ] Unit/integrációs teszt: két különböző user azonos kulccsal NEM kaphatja
      egymás válaszát; 500 után a retry újra végrehajtódik.

### 1.2 Két bukó unit teszt: GmailIngestionServiceTests

**Fájl:** `tests/FamilyOs.Infrastructure.Tests/Email/GmailIngestionServiceTests.cs`

`SyncAsync_AuditLogWritten_Always` és `SyncAsync_MultipleMessages_InsertsOnlyNew`
bukik: a produkciós kód a `{"fetched":..,"inserted":..}` JSON-t már a
`detailsJson` paraméterben adja át az `IAuditLogger.LogAsync`-nek, a tesztek a
régi paraméterpozíción várják. A CLAUDE.md minőségi kapuja („dotnet test zöld")
sérül.

**Teendők:**
- [ ] A két teszt elvárásának igazítása a jelenlegi `LogAsync` szignatúrához.

### 1.3 `test-login`: egy env-változó egy nyitott admin-regisztrációtól

**Fájl:** `src/FamilyOs.Api/Endpoints/AuthModule.cs:82`

A végpont akkor is engedélyezett, ha `Auth:AllowTestLogin == "true"` —
**Productionben is**. Anonim, tetszőleges e-maillel, tetszőleges role-lal
(akár `Admin`) hoz létre fiókot, és megkerüli az allowlist-ellenőrzést,
amit a rendes Google-login kikényszerít.

**Teendők:**
- [ ] Production környezetben feltétel nélkül 404 (a config-felülbírálás
      megszüntetése), VAGY legalább:
- [ ] allowlist-ellenőrzés a test-loginban is;
- [ ] `Admin` role tiltása test-login útvonalon.
- [ ] A döntés rögzítése ADR-ben (a ui-test-scenarios.md 2. szakasza eleve
      ADR-igényes döntésként jelölte, ami elmaradt).

### 1.4 Gmail refresh token titkosítatlanul a DB-ben

**Fájl:** `src/FamilyOs.Application/Sources/ConnectGmailCommandHandler.cs:14`

A refresh token plain JSON-ként kerül a `source.config_json`-ba. Egy DB-dump
teljes Gmail-hozzáférést ad. A security-privacy.md 6.2 explicit DataProtection-
titkosítást ír elő (a compose-ban a `dp_keys` volume már létezik).

**Teendők:**
- [ ] `AddDataProtection()` konfigurálása `/var/lib/family-os/dp-keys`
      kulcstárral (lásd még 2.x doksi: a volume jelenleg árván áll).
- [ ] `IDataProtector`-os titkosítás a config_json írásánál/olvasásánál
      (ConnectGmailCommandHandler + GmailIngestionService oldalon).
- [ ] Migrációs terv a meglévő plaintext tokenekre (re-encrypt vagy
      újracsatlakoztatás kérése).

---

## P2 — Keményítenivalók

### 2.1 Role-fallback felfelé eskalál

**Fájl:** `src/FamilyOs.Application/ToolCalls/ConfirmToolCallCommandHandler.cs:47`

`currentUser.Role ?? "Adult"` — hiányzó role claim esetén a *magasabb*
jogosultság a default. Ugyanitt hardcoded `"Europe/Budapest"` timezone.

- [ ] Least-privilege default (`Child`) vagy explicit hiba hiányzó role-nál.
- [ ] Timezone konfigurációba (env / appsettings).

### 2.2 Nincs rate limiting

Sem a `login/google`, sem a search Command-mód (minden hívás Ollama
LLM-hívást indít) nincs korlátozva; a security-privacy.md 9.7 konkrét
számokat ígér (100 req/min/user, AI 10 req/min).

- [ ] `AddRateLimiter` globális + szigorúbb AI-partíció a doksi számaival.
- [ ] Login endpoint throttling (security-privacy.md 3.4: 5 hiba/10 perc).

### 2.3 JSON string-interpoláció az audit logban

**Fájl:** `src/FamilyOs.Application/Auth/Commands/LoginGoogleCommandHandler.cs:31`

A `claims.Email` escapelés nélkül kerül JSON-ba.

- [ ] `JsonSerializer.Serialize`-zal képzett detailsJson.

### 2.4 docker-compose default jelszavak

**Fájl:** `docker-compose.yml`

`changeme` fallbackok a `POSTGRES_PASSWORD` / `APP_DB_PASSWORD`-nál.

- [ ] `${VAR:?hibaüzenet}` szintaxis — a stack ne induljon el jelszó nélkül.

### 2.5 RevokedSessionChecker minden requestnél DB-t olvas

**Fájl:** `src/FamilyOs.Infrastructure/Auth/RevokedSessionChecker.cs:16`

Családi méretben elfogadható, de olcsón cache-elhető.

- [ ] Rövid TTL-ű memory-cache a revoked-jti halmazra (opcionális).

---

## Ami kifejezetten jó (nem teendő — referenciának)

- A tool-calling pipeline mintaszerű: LLM csak *javasol*; végrehajtás
  kizárólag HMAC-aláírt, TTL-es, user-höz kötött, sémavalidált, emberi
  megerősítéshez és replay-guardhoz kötött tokennel. Prompt injection ezen a
  felületen gyakorlatilag ártalmatlan.
- Szisztematikus authorizáció: minden endpoint-csoport policy-vel indul;
  egységes Admin/Adult/Child + IsPrivate szabályok
  (`FamilyOsAuthorizationService`), a tool-resolve lépés is ezt használja.
- Jó alapok: `__Host-` cookie (HttpOnly, Secure), revoked-session check,
  Google audience-validáció, allowlist + bootstrap-admin + invite flow,
  path-traversal elleni kettős védelem, ProblemDetails-middleware nem
  szivárogtat stack trace-t.
- Üzemi igényesség: source-generated `LoggerMessage`, Serilog, health
  check-ek, Testcontainers-integrációs tesztek, tiszta repo.

## Javasolt sorrend

1. 1.2 (bukó tesztek — kapu-sértés, olcsó)
2. 1.1 (IdempotencyMiddleware)
3. 1.3 (test-login szigorítás)
4. 1.4 (token-titkosítás + DataProtection)
5. 2.2 (rate limiting), majd a többi P2.

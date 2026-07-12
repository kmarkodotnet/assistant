# Code review javítások — Sonnet feladatkártyák (T1–T9)

> Státusz: IMPLEMENTÁCIÓS SPEC v1.0 · Dátum: 2026-07-12
> Forrás: `docs/fable_review/01-code-review-teendok.md` (Fable review, `ai-proposal-learn` ág, commit `d2526a4`)
> Globális szabályok: [00-README-sonnet-utasitasok.md](00-README-sonnet-utasitasok.md) — MINDEN kártyára érvényesek.
> Kiinduló állapot: `dotnet build` ✅ 0 warning · backend tesztek ❌ 271/273 · vitest ✅ 172/172

**Végrehajtási sorrend (kötelező):** T1 → T2 → T3 → T4 → T5 → T6 → T7 → T8 → (T9 opcionális).
T1–T4 P1 (hibák), T5–T9 P2 (keményítés). T5, T7, T8 egymástól függetlenek, párhuzamosíthatók.

---

## T1 — Két bukó GmailIngestion teszt javítása

**Prioritás:** P1 · **Effort:** ~0,5 óra · **Ág:** `fix/gmail-ingestion-tests` · **Függ:** –

**Cél:** a `dotnet test` újra zöld legyen (jelenleg 271/273).

**Olvasd el először:**
- `tests/FamilyOs.Infrastructure.Tests/Email/GmailIngestionServiceTests.cs`
- `src/FamilyOs.Infrastructure/Email/GmailIngestionService.cs` (a `LogAsync` hívás helye)
- az `IAuditLogger.LogAsync` interfész-definíciója

**Kontextus:** a produkciós kód a `{"fetched":..,"inserted":..}` JSON-t már a
`detailsJson` paraméterben adja át az `IAuditLogger.LogAsync`-nek; a
`SyncAsync_AuditLogWritten_Always` és `SyncAsync_MultipleMessages_InsertsOnlyNew`
tesztek a régi paraméterpozíción várják.

**Döntés:** a TESZTEKET igazítsd a jelenlegi `LogAsync` szignatúrához — a
produkciós kód a helyes.

**Lépések:**
1. Futtasd a két tesztet, rögzítsd a pontos hibaüzenetet.
2. Igazítsd a teszt-elvárásokat (NSubstitute `Received()` argumentumok) a
   tényleges hívási szignatúrához. Az assert továbbra is ellenőrizze, hogy a
   `detailsJson` tartalmazza a `fetched` és `inserted` kulcsokat.

**Tilos / nem scope:** a `GmailIngestionService` produkciós kód módosítása.

**Elfogadás:**
- [ ] Mindkét teszt zöld, a teljes backend tesztkészlet 273/273.

**Ellenőrzés:**
```bash
dotnet test --filter "FullyQualifiedName~GmailIngestionServiceTests"
dotnet test
```

---

## T2 — IdempotencyMiddleware: pipeline-pozíció + cache-szabályok

**Prioritás:** P1 · **Effort:** ~0,5 nap · **Ág:** `fix/idempotency-middleware` · **Függ:** T1

**Cél:** az idempotencia-kulcs user-höz kötött legyen, csak sikeres válasz
cache-elődjön, és a cache lejárjon.

**Olvasd el először:**
- `src/FamilyOs.Api/Middleware/IdempotencyMiddleware.cs`
- `src/FamilyOs.Api/Program.cs` (middleware-regisztrációs sorrend, ~84. sor)
- `docs/api-design.md` §1.9 (idempotencia-kontrakt: `(user, key)` mapping, 24 h)

**Hibakép (mindhárom javítandó):**
1. A middleware a `UseAuthentication()` ELŐTT fut → `context.User.FindFirst("sub")`
   mindig null → minden user a közös `anon:` kulcstérbe kerül → az egyik user
   megkaphatja a másik cache-elt válaszát.
2. A statikus `ConcurrentDictionary` korlátlanul nő — nincs TTL.
3. Hibaválasz (pl. 500) is cache-elődik — a kliens retry-ja örökre a hibát kapja.

**Döntések:**
- A regisztráció a `UseAuthentication()` (és `UseAuthorization()`) MÖGÉ kerül.
- A statikus `ConcurrentDictionary`-t `IMemoryCache` váltja ki, **abszolút lejárat
  24 óra** (api-design.md 1.9 szerint), a lejárat `IConfiguration`-ből
  felülbírálható (`Idempotency:TtlHours`, default 24).
- KIZÁRÓLAG 2xx státuszú válasz kerül a cache-be.
- Kulcsképzés: `"{sub}:{idempotencyKey}"`; hitelesítetlen kérésnél a middleware
  NE cache-eljen semmit (engedje át a kérést cache-kezelés nélkül).

**Lépések:**
1. `Program.cs`: mozgasd a middleware-regisztrációt az auth mögé.
2. Írd át a middleware-t `IMemoryCache`-re a fenti szabályokkal.
3. Tesztek (a meglévő API-integrációs tesztminta szerint — nézd meg a
   `tests/` alatti WebApplicationFactory/Testcontainers használatot):
   - két különböző user azonos `Idempotency-Key`-jel NEM kapja egymás válaszát;
   - 500-as első válasz után az azonos kulcsú retry ÚJRA végrehajtódik;
   - 2xx válasz azonos kulcsú ismétlése a cache-elt választ adja (a handler
     nem fut le kétszer).

**Tilos / nem scope:** más middleware sorrendjének módosítása; elosztott
(Redis) cache bevezetése.

**Elfogadás:**
- [ ] Mindhárom új teszt zöld; teljes tesztkészlet zöld.
- [ ] A middleware auth utáni pozícióban fut (Program.cs diff igazolja).

**Ellenőrzés:**
```bash
dotnet build && dotnet test
```

---

## T3 — `test-login` végpont szigorítása

**Prioritás:** P1 · **Effort:** ~0,5 nap · **Ág:** `fix/test-login-hardening` · **Függ:** T1

**Cél:** a test-login Production-ben semmilyen konfigurációval ne legyen
elérhető; nem-Production környezetben se adjon admin-utat.

**Olvasd el először:**
- `src/FamilyOs.Api/Endpoints/AuthModule.cs` (~82. sor, test-login végpont)
- a rendes Google-login allowlist-ellenőrzése:
  `src/FamilyOs.Application/Auth/Commands/LoginGoogleCommandHandler.cs`
- `docs/qa/ui-test-scenarios.md` §2 (a test-login ADR-igényes döntésként volt jelölve)

**Hibakép:** a végpont `Auth:AllowTestLogin == "true"` esetén Production-ben
is él; anonim, tetszőleges e-maillel, tetszőleges role-lal (akár `Admin`)
hoz létre fiókot, allowlist-ellenőrzés nélkül.

**Döntések (mindhárom együtt implementálandó):**
1. `IHostEnvironment.IsProduction()` esetén a végpont FELTÉTEL NÉLKÜL 404-et
   ad — a config nem bírálhatja felül.
2. Nem-Production környezetben is fut az allowlist-ellenőrzés (ugyanaz a
   logika, mint a Google-loginban — emeld ki közös helyre, ha duplikálni
   kellene).
3. `Admin` role kérése a test-login útvonalon tilos → 400 ProblemDetails.

**Lépések:**
1. Implementáld a három szabályt.
2. Tesztek: Production-env szimulációval 404; allowlisten kívüli e-mail → 403;
   `role=Admin` kérés → 400; érvényes nem-admin kérés Development-ben → 200.
3. Ellenőrizd, hogy a `full_tests/` e2e réteg test-login használata továbbra
   is működik (az ott használt role/e-mail kombináció megfelel-e az új
   szabályoknak) — ha spec módosítás kell, tedd meg.
4. ⛔ EMBERI KAPU: írd meg DRAFT-ként az ADR-t
   (`docs/decisions/ADR-0018-test-login.md`): mi a végpont célja, milyen
   környezetben él, milyen korlátokkal. A fájl elejére: `> Státusz: DRAFT`.

**Tilos / nem scope:** a Google-login flow módosítása; a test-login teljes
törlése (az e2e réteg használja).

**Elfogadás:**
- [ ] 4 új/módosított teszt zöld; teljes készlet zöld.
- [ ] ADR-0018 DRAFT létezik.
- [ ] `full_tests/` specek változatlanul futnak (vagy igazítva).

**Ellenőrzés:**
```bash
dotnet test
```

---

## T4 — Gmail refresh token titkosítása (DataProtection)

**Prioritás:** P1 · **Effort:** ~1 nap · **Ág:** `fix/gmail-token-encryption` · **Függ:** T1

**Cél:** a Gmail refresh token ne plaintextben álljon a DB-ben; a
DataProtection kulcstár perzisztens legyen.

**Olvasd el először:**
- `src/FamilyOs.Application/Sources/ConnectGmailCommandHandler.cs` (~14. sor)
- `src/FamilyOs.Infrastructure/Email/GmailIngestionService.cs` (config_json olvasás)
- `docs/security-privacy.md` §6.2, §6.4, §10.1
- `docker-compose.yml` (a `dp_keys` volume már létezik, de árván áll)
- az Infrastructure DI-regisztráció (`DependencyInjection.cs`)

**Döntések:**
- `AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo("/var/lib/family-os/dp-keys"))`
  + `SetApplicationName("family-os")`; a compose `dp_keys` volume-ot erre az
  útvonalra kell mountolni az api ÉS a workers konténerbe (mindkét processz
  ugyanazt a kulcstárat használja).
- A titkosítás egy `ISourceConfigProtector` absztrakción át történik
  (Application-rétegben interfész, Infrastructure-ben `IDataProtector`-os
  implementáció, purpose string: `"FamilyOs.Sources.ConfigJson"`), hogy a
  handler tesztelhető maradjon.
- **Migrációs stratégia meglévő plaintext tokenekre — „lazy re-encrypt":**
  olvasáskor először dekódolási kísérlet; ha `CryptographicException` →
  a tartalom plaintext örökség: használd fel, és AZONNAL írd vissza
  titkosítva. Külön adat-migráció nem kell.

**Lépések:**
1. DI: `AddDataProtection` a fenti konfigurációval; compose-ban a volume-mount
   pótlása api+workers alá.
2. `ISourceConfigProtector` (Protect/Unprotect) + implementáció + DI-regisztráció.
3. `ConnectGmailCommandHandler`: írás előtt `Protect`.
4. `GmailIngestionService` (és minden más config_json-olvasó — keress rá:
   `config_json` / `ConfigJson` a repóban): `Unprotect` + lazy re-encrypt ág.
5. Tesztek: unit a protector round-tripre; unit a lazy re-encrypt ágra
   (plaintext bemenet → sikeres olvasás + titkosított visszaírás);
   a meglévő Gmail-tesztek zöldek maradnak (mockolt protectorral).

**Tilos / nem scope:** más titok (pl. SMTP-jelszó) titkosítása; kulcsrotációs
automatika.

**Elfogadás:**
- [ ] Új Connect után a `source.config_json` nem tartalmaz felismerhető
      tokent (integrációs vagy kézi ellenőrzés dokumentálva a PR-ben).
- [ ] Plaintext örökség-rekord első olvasás után titkosítva áll.
- [ ] Konténer-újraindítás után a token olvasható marad (perzisztens kulcstár
      — kézi füstteszt `make up`-pal, eredmény a PR-leírásban).

**Ellenőrzés:**
```bash
dotnet test
docker compose config   # a dp_keys mount megjelenik api + workers alatt
```

---

## T5 — Role-fallback és timezone konfiguráció

**Prioritás:** P2 · **Effort:** ~2 óra · **Ág:** `fix/role-fallback-timezone` · **Függ:** T1

**Olvasd el először:**
- `src/FamilyOs.Application/ToolCalls/ConfirmToolCallCommandHandler.cs` (~47. sor)

**Hibakép:** `currentUser.Role ?? "Adult"` — hiányzó role claim esetén FELFELÉ
eskalál; ugyanitt hardcoded `"Europe/Budapest"`.

**Döntések:**
- Hiányzó role claim → NE fallback, hanem explicit hiba (401/403 ProblemDetails)
  — hiányzó role érvénytelen session-t jelez, nem „átlagos felnőttet".
- Timezone: `App:TimeZone` konfigkulcs (appsettings + env `App__TimeZone`),
  default `"Europe/Budapest"`. Keress rá a repóban más hardcoded
  `"Europe/Budapest"` előfordulásra, és mindet erre a kulcsra kösd.

**Lépések:** implementáció + unit teszt (hiányzó role → hiba; konfigolt
timezone érvényesül).

**Tilos / nem scope:** a role-modell vagy a policy-k átalakítása.

**Elfogadás:**
- [ ] Tesztek zöldek; nincs több hardcoded timezone a src/ alatt
      (`grep -rn "Europe/Budapest" src/` csak default-értékként, konfig-
      olvasás mellett találja).

---

## T6 — Rate limiting (globális + AI + login)

**Prioritás:** P2 · **Effort:** ~1 nap · **Ág:** `feat/rate-limiting` · **Függ:** T2 (middleware-sorrend tisztázva)

**Cél:** a security-privacy.md §9.7 és §3.4 számainak beváltása.

**Olvasd el először:**
- `src/FamilyOs.Api/Program.cs` (pipeline)
- `docs/security-privacy.md` §9.7, §3.4
- a search Command-mód végpontja (Ollama-hívást indít — ez az AI-partíció)
- a tool-calls végpontok (szintén AI-partíció)

**Döntések (a doksi számai):**
- Globális, user-szkópolt (sub claim; anonimnál IP) fixed-window limiter:
  **100 req/min**.
- AI-partíció (search Command/Qa mód + tool-calls proposal): **10 req/min/user**.
- Login-throttling a `login/google` végpontra: **5 sikertelen kísérlet /
  10 perc → 15 perc tiltás** — ez NEM a beépített rate limiter, hanem
  kis, memóriabeli failed-login számláló a login-handler körül (e-mail+IP
  kulccsal), mert csak a SIKERTELEN kísérlet számít.
- Minden limit konfigból (`RateLimiting:*` szekció), tesztben kikapcsolható.
- 429-es válasz ProblemDetails formátumban, `Retry-After` headerrel.

**Lépések:**
1. `AddRateLimiter` + partíciók + `UseRateLimiter` a pipeline-ban.
2. Endpoint-csoportok címkézése (`RequireRateLimiting("ai")` az AI-utakra).
3. Failed-login throttle a login-handler előtt/körül.
4. Tesztek: limit túllépés → 429 + Retry-After; AI-partíció szigorúbb;
   5 hibás login után a 6. kísérlet 429/423, 15 perc múlva (idő-mockkal) újra
   engedélyezett; sikeres login nulláz.
5. `docker-compose.yml` / `.env.example`: az új env-kulcsok dokumentálása.

**Tilos / nem scope:** elosztott (Redis-alapú) limiter; a frontend
429-kezelésének UI-fejlesztése (külön feladat, jegyezd fel).

**Elfogadás:**
- [ ] Tesztek zöldek; a 429 megjelenik a ProblemDetails-katalógusban
      (api-design.md §1.2 státusz-tábla — 1 soros doksi-frissítés ide tartozik).

---

## T7 — Audit-log JSON injektálhatóság

**Prioritás:** P2 · **Effort:** ~1 óra · **Ág:** `fix/audit-json-serialize` · **Függ:** –

**Olvasd el először:**
- `src/FamilyOs.Application/Auth/Commands/LoginGoogleCommandHandler.cs` (~31. sor)

**Hibakép:** a `claims.Email` string-interpolációval, escapelés nélkül kerül
a detailsJson-ba.

**Lépések:**
1. A detailsJson-t `JsonSerializer.Serialize`-zal (anonim objektumból) képezd.
2. Keress rá a repóban minden további kézzel interpolált JSON-ra az
   audit-hívásokban (`grep -rn 'detailsJson' src/ | grep -v Serialize` jellegű
   átvizsgálás), és javítsd ugyanígy.
3. Unit teszt: e-mail `"a\"b@x.hu"` értékkel érvényes JSON keletkezik.

**Elfogadás:** [ ] teszt zöld; nincs interpolált JSON audit-útvonalon.

---

## T8 — docker-compose: kötelező jelszavak

**Prioritás:** P2 · **Effort:** ~0,5 óra · **Ág:** `chore/compose-required-secrets` · **Függ:** –

**Olvasd el először:** `docker-compose.yml`, `.env.example`

**Hibakép:** `changeme` fallback a `POSTGRES_PASSWORD` / `APP_DB_PASSWORD`-nál.

**Lépések:**
1. Minden jelszó-jellegű változó `${VAR:?A VAR beallitasa kotelezo a .env-ben}`
   formára (POSTGRES_PASSWORD, APP_DB_PASSWORD + nézd át a többi compose-fájlt:
   `docker-compose*.yml`).
2. `.env.example`-ben placeholder + megjegyzés maradjon.

**Elfogadás:**
- [ ] `docker compose config` a változók NÉLKÜL hibával leáll, kitöltött
      `.env`-vel lefut.

**Ellenőrzés:**
```bash
docker compose config
```

---

## T9 — (Opcionális) RevokedSessionChecker cache

**Prioritás:** P3 · **Effort:** ~2 óra · **Ág:** `perf/revoked-session-cache` · **Függ:** T1

**Olvasd el először:** `src/FamilyOs.Infrastructure/Auth/RevokedSessionChecker.cs` (~16. sor)

**Döntés:** `IMemoryCache`, **30 s abszolút TTL** a revoked-jti halmazra;
revokáláskor (logout/kill-session útvonal) a cache azonnal invalidálódik.
A 30 s ablak elfogadott kompromisszum családi méretben.

**Lépések:** implementáció + unit teszt (cache-hit nem üt DB-t; revokálás
után legfeljebb a TTL-ablakon belül él a session; invalidálás azonnali a
revokáló útvonalon).

**Tilos / nem scope:** elosztott cache; a session-modell átalakítása.

---

## Referencia — ami kifejezetten jó (NE nyúlj hozzá)

A review pozitív megállapításai — ezek védendő minták, a fenti feladatok
egyike sem ronthatja el őket:

- Tool-calling pipeline: LLM csak javasol; végrehajtás HMAC-aláírt, TTL-es,
  user-höz kötött, sémavalidált, megerősítés+replay-guard mögött (ADR-0011).
- Szisztematikus policy-alapú authorizáció (`FamilyOsAuthorizationService`).
- `__Host-` cookie, revoked-session check, Google audience-validáció,
  allowlist+invite flow, path-traversal kettős védelem, ProblemDetails.
- Source-generated `LoggerMessage`, Serilog, health checkek, Testcontainers.

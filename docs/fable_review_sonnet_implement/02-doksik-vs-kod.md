# Doksi ↔ kód szinkron — Sonnet-implementációs feladatkártyák

> Státusz: IMPLEMENTÁCIÓS SPEC v1.0 · Dátum: 2026-07-12
> Forrás: `docs/fable_review/02-doksik-vs-kod.md` · Vizsgált állapot: `ai-proposal-learn`, commit `d2526a4`
> Minden „vagy-vagy" döntés ELŐRE MEGHOZVA — a kártyákon „Döntés:" jelöléssel.

## Munkacsomagok áttekintése

| Csomag | Kártyák | Jelleg | Függés |
|---|---|---|---|
| WP-A | A-kártyák (doksi-javítás) | csak `docs/` módosul, kockázatmentes | nincs — azonnal indítható |
| WP-B | B-kártyák (kód-adósság) | biztonsági kód, a 01-es doksi T-kártyáira mutat | 01-es doksi |
| WP-C | C-kártyák (eldöntött tételek) | kis kód + doksi vegyesen | nincs |

**NE nyúlj hozzá (jó, egyező állapot — referencia):** `database-schema.md` ↔
`FamilyOsDbContext`; `reminder-engine.md` §6 ↔ `DueReminderDispatcher`/`EscalationScheduler`;
`ai-pipeline.md` ↔ `AiJobExecutor` (10 job-típus); `search-strategy.md` ↔ `Application/Search`;
ADR-0011 ↔ tool-calling kód; `security-privacy.md` §7.2–7.3 ↔ `LocalFilesystemDocumentStorage`, `MimeDetector`.

---

## WP-A — Doksi-igazítások (egyetlen kártya, egy PR)

### Kártya A-DOC — api-design.md + missing_implementation.md igazítása a valósághoz

**Cél:** a félrevezető doksi-állítások megszüntetése. CSAK dokumentum módosul, kód NEM.
**Ág:** `docs/api-design-sync` · **Commit:** `docs: api-design es missing_implementation szinkron a koddal`
**Modell:** haiku is elég, sonnet biztos.

**Olvasd el először:**
- `docs/api-design.md` (a módosítandó szakaszok: végpontlista, §1.4, §1.5)
- `src/FamilyOs.Api/Modules/` — a TÉNYLEGES végpontok forrása (route-definíciók);
  minden doksi-állítást a kódból ellenőrizz, ne ebből a kártyából másolj vakon
- `docs/missing_implementation.md`

**Lépések (tételenként a döntés):**
1. **A2** — a doksi `/api/v1/user-accounts`-ot ír, a kód `/api/v1/users`-t használ.
   **Döntés: a doksi igazodik** (API-törés tilos, a FE már `/users`-re épül).
   Írd át a doksiban minden előfordulását `/api/v1/users`-re.
2. **A4** — a doksi `POST /documents/search` + `POST /notes/search` végpontokat ír;
   a valóság az egykapus `POST /search`. **Döntés: doksi.** Töröld a két
   per-modul search-végpontot, és hivatkozz a `search-strategy.md`-re
   („keresés kizárólag a globális POST /search végponton").
3. **A5** — a doksi `GET /settings/preferences`-t ír; a valóság:
   `/auth/me/preferences` (PATCH) + `/settings/system` + `/settings/integrations`.
   **Döntés: doksi.** Írd át a settings-szakaszt a három tényleges útvonalra
   (módszerekkel együtt — ellenőrizd a kódban a HTTP-igéket).
4. **A6** — warranty/medical/financial PATCH és document-tag DELETE a doksiban
   teljes végpontként szerepel, a kódban szándékos 501-stub
   (`DocumentsModule.cs:130,147-149`). **Döntés: doksi.** Jelöld ezeket
   „**501 — v2-re halasztva**" státusszal, ne töröld őket.
5. **A7** — §1.5 `ETag`/`If-Match` + `X-Total-Count` headereket ír; a valóság:
   RowVersion a body-ban, header nincs. **Döntés: doksi.** Írd át a §1.5-öt:
   az optimista konkurencia a body-beli `rowVersion` mezővel működik;
   az ETag-headeres forma „nem tervezett" megjegyzést kap.
6. **A9** — §1.4 CORS-allowlistet ír; a valóság: nincs CORS-konfig, mert az
   nginx mögött same-origin a kiszolgálás. **Döntés: doksi.** Írd át:
   „same-origin architektúra miatt CORS-konfiguráció szándékosan nincs;
   ha valaha külön originre kerül a FE, allowlist kötelező".
7. **D1** — `docs/missing_implementation.md` (2026-07-08) a search/tasks/
   deadlines/settings-system oldalakat „stub"-nak jelöli, pedig készek
   (280–310 soros komponensek, teszttel). **Döntés: archiválás.** Mozgasd
   `docs/archive/missing_implementation-2026-07-08.md` néven (hozd létre a
   mappát, ha nincs), és a fájl tetejére írd: „ARCHIVÁLT — 2026-07-12-én a
   tartalom elavult volt; az aktuális állapotot a kód és a
   `docs/fable_review/` tükrözi." Az eredeti helyre NE maradjon fájl.

**Elfogadás:**
- api-design.md egyetlen végpontja sem hivatkozik nem létező útvonalra
  (A2/A4/A5 átírva, A6 501-jelölt, A7/A9 szándék-rögzített).
- missing_implementation.md az archive-mappában, fejléc-megjegyzéssel.

**Ellenőrzés:**
```bash
grep -rn "user-accounts" docs/api-design.md            # 0 találat
grep -rn "documents/search\|notes/search" docs/api-design.md  # 0 találat
test ! -f docs/missing_implementation.md && echo OK
```

**Tilos / nem scope:** kódmódosítás; a doksik más szakaszainak átfogalmazása;
A3/A8/A10/R1 (azok WP-C kód-kártyák); ui-test-scenarios.md (az a 03-as doksi).

---

## WP-B — Biztonsági kód-adósság (kereszthivatkozás + 1 saját kártya)

Az S1–S4 tételeket a **01-es doksi kártyái fedik le** — NE duplikáld őket:

| Tétel | Mi hiányzik | Végrehajtó kártya |
|---|---|---|
| S1 | rate limiting (100 req/min/user, AI 10/min) | 01 / **T6** |
| S2 | failed-login throttling (5 hiba/10 perc → 15 perc) | 01 / **T6** |
| S3 | Gmail refresh token titkosítás | 01 / **T4** |
| S4 | `AddDataProtection` + kulcstár + árva `dp_keys` volume | 01 / **T4** |

### Kártya B-S5 — audit_log insert-only védelem

**Cél:** az `app.audit_log` tábla módosítás- és törlésvédelme DB-szinten,
ahogy a `security-privacy.md` §5.5 ígéri.
**Ág:** `fix/audit-log-immutable` · **Commit:** `fix(db): audit_log insert-only vedelem (REVOKE + trigger)`
**Függ:** semmi. **Modell:** sonnet.
**⛔ EMBERI KAPU:** DB-migráció — a migráció DRAFT-ként készül el, merge/apply
emberi jóváhagyás után (CLAUDE.md 2-es szint szabálya).

**Olvasd el először:**
- `src/FamilyOs.Infrastructure/Persistence/DbSeedRunner.cs` (különösen a 79. sor
  környéke — a GRANT-logika)
- a legutóbbi EF Core migráció a migrációs mappában (minta a raw SQL-migrációhoz)
- `docs/security-privacy.md` §5.5

**Lépések:**
1. Új EF Core migráció (üres migráció + raw SQL a `Up`-ban):
   ```sql
   REVOKE UPDATE, DELETE ON app.audit_log FROM family_app;
   CREATE OR REPLACE FUNCTION app.audit_log_block_mutation() RETURNS trigger AS $$
   BEGIN RAISE EXCEPTION 'audit_log is insert-only'; END;
   $$ LANGUAGE plpgsql;
   CREATE TRIGGER trg_audit_log_immutable
     BEFORE UPDATE OR DELETE ON app.audit_log
     FOR EACH ROW EXECUTE FUNCTION app.audit_log_block_mutation();
   ```
   `Down`: trigger + function DROP, GRANT visszaállítás.
2. `DbSeedRunner.cs:79` finomítása: a jelenlegi „GRANT UPDATE/DELETE minden
   táblára" logika hagyja ki az `audit_log`-ot (vagy a GRANT után futtasson
   REVOKE-ot az audit_log-ra), különben a seed visszaadja a jogot.
   **Döntés:** a seed-runner explicit kivétellistát kap (`audit_log`), NEM
   tábla-szintű GRANT-felsorolásra írjuk át az egészet (az nagyobb scope).
3. Teszt (Testcontainers-integrációs): UPDATE és DELETE kísérlet az
   audit_log-on `family_app` szerepkörrel → kivétel; INSERT továbbra is megy.

**Elfogadás:** migráció DRAFT kész · seed nem adja vissza a jogot · teszt zöld.
**Ellenőrzés:** `dotnet build` 0 warning · `dotnet test` zöld (az új teszttel).
**Tilos / nem scope:** más táblák jogosultságainak átrendezése; a migráció
éles alkalmazása jóváhagyás nélkül; audit-retention worker (az C-S6).

---

## WP-C — Eldöntött tételek (5 kis kártya)

### Kártya C-S6 — audit-retention: post-MVP jelölés (csak doksi)

**Döntés: doksi**, nem kód — az audit-cleaner worker nem MVP-kritérium; a
tétel a backlogra kerül.
**Ág:** WP-A ágára fűzhető, vagy `docs/audit-retention-postmvp`.

**Lépések:**
1. `docs/security-privacy.md` §5.4-be jelölés: „**POST-MVP** — a napi
   audit-takarító worker (`Audit.RetentionDays`) még nincs implementálva;
   backlog-tétel." A szakaszt NE töröld.
2. `docs/backlog.md`-be (ha van; ha nincs, hozd létre) egy sor:
   „audit-retention worker (security-privacy.md §5.4) — post-MVP".

**Tilos:** worker-implementáció; a `NotificationFeedRetentionJob` módosítása.

### Kártya C-A3 — GET /api/v1/system/version implementálása

**Döntés: kód** — olcsó, és a 09-es telepítő-terv (frissítés-jelzés) is igényli.
**Ág:** `feat/system-version-endpoint` · **Commit:** `feat(api): GET /system/version vegpont`
**Modell:** sonnet (haiku eszkalációval).

**Olvasd el először:** egy meglévő egyszerű modul a `src/FamilyOs.Api/Modules/`
alatt (route-regisztrációs minta) · `docs/api-design.md` version-szakasza.

**Lépések:**
1. Új végpont: `GET /api/v1/system/version` — auth KELL (bejelentkezett
   felhasználó, bármely szerep), Admin-kapu NEM kell.
2. Válasz: `{ "version": "<informational version>", "commit": "<git sha vagy null>" }`
   — forrás: `Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()`.
   Commit-hash: az informational version `+` utáni része, ha van; különben null.
3. Integrációs teszt: bejelentkezve 200 + nem üres `version`; kijelentkezve 401.

**Elfogadás:** végpont él, teszt zöld, api-design.md szakasz változatlanul igaz.
**Tilos:** frissítés-ellenőrző logika (az a 09-es terv része); FE-fogyasztás.

### Kártya C-A8 — Stabil OpenAPI-útvonal + gen:api javítás

**Döntés: kód** — az OpenAPI JSON stabil útvonalra kerül: `/openapi/v1.json`,
minden környezetben elérhető, **Admin-szerephez kötve** (a Swagger UI marad
Development-only). A FE `gen:api` erre a stabil útvonalra mutat.
**Ág:** `fix/openapi-stable-route` · **Commit:** `fix(api): stabil /openapi/v1.json utvonal + gen:api URL`

**Olvasd el először:** `src/FamilyOs.Api/Program.cs` (Swashbuckle-konfiguráció) ·
`frontend/package.json` (`gen:api` script) · `docs/api-design.md` §1.10.

**Lépések:**
1. Swashbuckle route-konfiguráció: a JSON `/openapi/v1.json` útvonalon,
   NEM csak Development-ben; a végpont elé Admin-authorization
   (a meglévő authorization-minta szerint — nézd meg, hogyan védenek más
   admin-végpontot, és ugyanazt a policy-t használd).
2. Swagger UI: marad Development-only (nem változik).
3. `frontend/package.json` `gen:api`: a script URL-je a tényleges
   `/openapi/v1.json`-ra mutasson. Futtasd le (`npm run gen:api`) — ha a
   generált kliens diffet ad, azt NE commitold, csak jelezd a zárójelentésben.
4. `docs/api-design.md` §1.10 frissítése: JSON admin-only minden környezetben,
   UI Development-only.

**Elfogadás:** Development-ben és Production-módban (helyi próbán) a JSON
adminként 200, nem-adminként 403, anonim 401; `gen:api` hibátlanul lefut.
**Tilos:** a generált API-kliens diffjének commitolása; Swagger UI production-ös
kinyitása.

### Kártya C-A10 — PATCH /settings/system: őszinte válasz (doksi + FE-jelzés)

**Döntés: doksi + minimális kód** — a tényleges perzisztencia (DB-alapú
runtime-config) NEM készül el most (nagy scope, a 09-es terv D3 wizardja
fedi majd). Addig a no-op megtévesztő sikerjelzését szüntetjük meg.
**Ág:** `fix/settings-system-honest` · **Commit:** `fix(api): settings/system PATCH oszinte valasza`

**Olvasd el először:**
`src/FamilyOs.Application/.../PatchSystemSettingsCommandHandler.cs` (15. sor
környéke) · a settings-FE komponens, amely ezt a végpontot hívja.

**Lépések:**
1. A handler a no-op ágon NE „sikert" jelezzen: a válasz DTO-ba kerüljön egy
   `applied: false` + `note: "restart szükséges — a beállítás env-változóból él"`
   mező (a pontos DTO-formát a meglévő válaszstruktúrához igazítsd).
   A HTTP-státusz maradjon 200 (nem hiba — szándékolt viselkedés).
2. FE: a settings-system oldal a `applied:false` válaszra jelenítsen meg
   figyelmeztető jelzést („A módosítás újraindítás után érvényesül; a
   beállítások env-változókból töltődnek"). Meglévő toast/alert mintát használj.
3. `docs/api-design.md` settings-szakaszába: a PATCH jelenleg nem perzisztál,
   `applied:false`-szal jelzi; a runtime-perzisztencia a telepítő-terv (09)
   first-run wizardjával érkezik.
4. Backend unit teszt a handler válaszára; FE vitest a figyelmeztetés
   megjelenésére.

**Elfogadás:** a PATCH nem állít hamis sikert · FE-jelzés látszik · tesztek zöldek.
**Tilos:** tényleges DB-perzisztencia bevezetése; env-olvasási logika átírása.

### Kártya C-R1 — Reminders.CatchUpMaxAgeDays konfigurálhatóvá tétele

**Döntés: kód** — options-osztályba emelés, default 14 (viselkedés nem változik).
**Ág:** `fix/reminder-catchup-config` · **Commit:** `fix(workers): CatchUpMaxAgeDays konfiguralhato`
**Modell:** haiku (sonnet eszkalációval).

**Olvasd el először:**
`src/FamilyOs.Workers/.../DueReminderDispatcher.cs` (184. sor környéke) ·
egy meglévő options-osztály a repóban (regisztrációs minta:
`services.Configure<T>(config.GetSection(...))`).

**Lépések:**
1. `ReminderOptions` osztály (vagy ha már van reminder-options, abba új
   property): `CatchUpMaxAgeDays { get; set; } = 14;` — szekció: `"Reminders"`.
2. `DueReminderDispatcher` konstruktor-injektálással (`IOptions<ReminderOptions>`)
   használja a hardcoded 14 helyett.
3. `appsettings.json`-ba a default kiírása; `.env.example`-be megjegyzés-sor
   (`Reminders__CatchUpMaxAgeDays`).
4. Unit teszt: 14 a default; egyedi értékkel a dispatcher azt használja
   (a meglévő DueReminderDispatcher-tesztek mintájára).

**Elfogadás:** viselkedés defaulton változatlan · teszt zöld · doksi
(`reminder-engine.md` §6.2) állítása mostantól igaz.
**Tilos:** a catch-up algoritmus bármely más részének módosítása.

---

## Végrehajtási sorrend

1. **A-DOC** (azonnal, kockázatmentes) — párhuzamosan bármelyik mással.
2. **C-R1, C-A3** (kicsik, függetlenek) — párhuzamosan.
3. **C-A8, C-A10** — függetlenek, de FE-t is érintenek; egyenként.
4. **B-S5** — migráció, ⛔ emberi kapu az apply előtt.
5. **C-S6** — bármikor, akár az A-DOC PR-be fűzve.

D2–D3 tételek (ui-test-scenarios.md) ebben a doksiban NINCSENEK — azokat a
[03-doksik-vs-tesztek.md](03-doksik-vs-tesztek.md) G/TD-kártyái fedik.

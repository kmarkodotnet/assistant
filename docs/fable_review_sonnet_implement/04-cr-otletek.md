# CR-minispecek — Sonnet-implementációra előkészítve

> Státusz: IMPLEMENTÁCIÓS SPEC v1.0 · Dátum: 2026-07-12
> Forrás: `docs/fable_review/04-cr-otletek.md` · CR-számozás: `cr260712-01…08`
> (nem duplikálja a futó cr260710-04/05/08 CR-eket).

## Hogyan indíts egy CR-t (orchestrátornak)

1. A CR-t ELŐBB backlog-tétellé kell tenni (`docs/backlog.md`), majd a
   szokásos workflow szerint: ARCH (ha kontraktot érint) → BUILD → QA → REVIEW.
2. Minden CR-hez külön ág: `feature/cr260712-XX-<slug>`.
3. A mini-spec „MVP-scope" szakasza a kőbe vésett határ — ami a „Tilos (scope-fence)"
   alatt van, az AKKOR SEM készül el, ha kézenfekvőnek tűnik.
4. Kötelező sorrend (indoklás az eredeti doksiban):
   **01 → 06 → 02 → 07 → 03 → 04 → 05 → 08.**

---

## cr260712-01 — Biztonsági keményítési csomag

**Státusz: NEM ÖNÁLLÓ IMPLEMENTÁCIÓ** — ez a CR a 01-es és 02-es doksi már
kártyázott tételeinek gyűjtőneve. Végrehajtás: 01/T4 (DataProtection+token),
01/T6 (rate limiting + login-throttling), 01/T8 (compose-jelszavak),
02/B-S5 (audit_log). NE indíts rá külön feladatot — a kártyák lezárásával
ez a CR automatikusan kész. Zárás: a backlogban a CR mellé a kártya-hivatkozások
felsorolása.

---

## cr260712-02 — Tool-katalógus bővítése (NL parancsok 2. üteme)

**Cél:** 4 új tool a kész ADR-0011 pipeline-ra: `complete_task`,
`snooze_reminder`, `create_note`, `resolve_deadline`.
**Ág:** `feature/cr260712-02-tool-catalog` (toolonként külön commit).
**Modell:** sonnet. **Effort:** 0,5 nap/tool.

**Olvasd el először:**
- ADR-0011 (kötelező — a confirm-flow szemantikája)
- egy MEGLÉVŐ tool teljes vertikuma: tool-osztály az `Application/Ai/Tools/`
  alatt + regisztrációja a tool-registry-ben + unit tesztje — ez a másolandó minta
- a planner prompt-katalógusa (hova kerül az új tool leírása)
- az érintett domain-handler (pl. task-complete command) — a tool NEM ír
  saját üzleti logikát, a meglévő commandot hívja

**Döntések (zárva):**
- Minden tool **proposal + confirm** kötésű marad (write-művelet) — kivétel nincs.
- A tool a meglévő application-command-ot hívja; RBAC-ellenőrzés a command
  szintjén marad (a tool nem kerüli meg).
- `snooze_reminder` paramétere: `snooze_until` (ISO datetime) VAGY
  `snooze_minutes` (int) — pontosan az egyik kötelező; a planner-prompt
  példát kap mindkettőre.
- Batch-proposal (több javaslat egy körben) NEM része ennek a CR-nek —
  az a 07-es doksi C2 hulláma.

**Lépések (toolonként ismételve):**
1. Tool-osztály a meglévő minta szerint (név, paraméter-séma, leírás magyarul
   a planner számára).
2. Registry-regisztráció + prompt-katalógus bővítés.
3. Unit tesztek: érvényes paraméterekkel proposal születik; érvénytelen
   paraméter → strukturált hiba; RBAC-tiltott felhasználóval a command
   elutasít (NSubstitute-mockolt command-oldallal, a meglévő tool-tesztek
   mintájára).
4. FE: a proposal-kártya generikus — ellenőrizd, hogy az új tool-típusok
   címkéi megjelennek-e; ha címke-map van a FE-ben, bővítsd + vitest.

**Elfogadás:** 4 tool regisztrálva · planner e2e próbában (manuális vagy
`tool-calling-flow.spec.ts` bővítés) legalább `complete_task` végigmegy ·
minden teszt zöld.
**Tilos (scope-fence):** confirm-lépés kihagyása bármely toolnál; batch-proposal;
read-only toolok (07-es doksi C1); a token-service/replay-guard módosítása.

---

## cr260712-06 — AI-telemetria dashboard

**Cél:** admin-aloldal a már gyűjtött AI-adatok aggregálásával: job-típusonkénti
token-fogyasztás, átlag-latency, hibaráta, modell-verzió, napi trend.
**Ág:** `feature/cr260712-06-ai-telemetry` · **Modell:** sonnet · **Effort:** 1–1,5 nap.

**Olvasd el először:**
- `ai_processing_job` tábla sémája (mely oszlopok léteznek: tokenszámok,
  futásidő, státusz, modell — ELLENŐRIZD, mit tárol ténylegesen; ha a
  tokenszám nincs perzisztálva, ÁLLJ MEG és jelentsd, mert akkor előbb
  tároló-oszlop kell)
- a meglévő ai-jobs admin API endpoint + FE ai-jobs oldal (minta mindkét réteghez)

**Döntések (zárva):**
- Aggregáció SQL-ben (GROUP BY nap + job-típus), NEM memóriában.
- Új endpoint: `GET /api/v1/ai-jobs/stats?days=30` (Admin-only, a meglévő
  ai-jobs authorization-mintával); válasz: napi bontású sorok
  `{date, jobType, count, failed, avgDurationMs, inputTokens, outputTokens, model}`.
- FE: az ai-jobs oldal ÚJ FÜLE (nem külön route) — táblázat + egyszerű
  sávdiagram; NEM vezetünk be chart-könyvtárat, CSS-alapú sávok elegendők.
- Retenció: a meglévő job-rekordokra épül; külön aggregációs tábla NEM készül.

**Lépések:**
1. Aggregáló query + handler + endpoint + integrációs teszt (Admin 200,
   nem-admin 403).
2. FE fül: signal-alapú lekérés, táblázat + sávok; vitest a komponensre.
3. `docs/api-design.md` bővítése az új endponttal.

**Elfogadás:** admin látja a 30 napos bontást · tesztek zöldek.
**Tilos (scope-fence):** chart-könyvtár bevezetése; aggregációs háttértábla;
riasztások/küszöbök; a CR260710-08 feedback-adatainak bekötése (később).

---

## cr260712-07 — Automatizált, titkosított backup + restore-próba

**Cél:** ütemezett `pg_dump` + dokumentum-volume mentés `age`-titkosítással,
retenció, és automatikus restore-verifikáció; valós státusz a `/settings/backup` oldalon.
**Ág:** `feature/cr260712-07-backup` · **Modell:** sonnet (compose-rész: devops
agent) · **Effort:** 2 nap.
**⛔ EMBERI KAPU:** a backup-passphrase kezelése env-ben marad
(`BACKUP_PASSPHRASE`), a compose-módosítás merge előtt emberi jóváhagyást igényel.

**Olvasd el először:**
- `scripts/backup.sh` (jelenlegi állapot) · `docs/security-privacy.md` §11.2
- docker-compose.yml (volume-nevek, hálózat) · a `/settings/backup` FE-oldal
  és API-ja (mit mutat ma)

**Döntések (zárva):**
- Megvalósítás: **backup sidecar konténer** a compose-ban (alpine + postgres-client
  + age), cron-nal — NEM host-cron (hordozhatóság, 09-es terv kompatibilitás).
- Ütemezés: napi 03:00, retenció: 7 napi + 4 heti; env-ből felülírható
  (`Backup__Schedule`, `Backup__RetainDaily`, `Backup__RetainWeekly`).
- Restore-próba: minden backup után a dump visszatöltése throwaway
  `postgres`-konténerbe + smoke-query (`SELECT count(*) FROM app.user_account`);
  eredmény státuszfájlba (`/backups/status.json`).
- Státusz-API: `GET /api/v1/settings/backup/status` a státuszfájl tartalmát
  adja vissza (utolsó futás, sikeres restore-próba ideje, méret) — Admin-only.

**Lépések:**
1. `scripts/backup.sh` kibővítése: dump + volume-tar + age-titkosítás +
   retenció + restore-verify + status.json írás.
2. Sidecar-service a compose-ba (DRAFT — ⛔ kapu).
3. Státusz-endpoint + a backup UI valós adatra kötése + tesztek.
4. `docs/DELIVERY.md` backup-szakasz frissítése.

**Elfogadás:** kézi próbafutás: titkosított artefakt keletkezik, restore-próba
zöld, UI a valós időpontot mutatja.
**Tilos (scope-fence):** felhő-célpontok (S3 stb.); backup-visszaállító UI;
passphrase-generálás/tárolás appon belül.

---

## cr260712-03 — Több Gmail-fiók támogatása

**Cél:** fiókonkénti Gmail-forrás tulajdonos-hozzárendeléssel és fiókonkénti
sync-státusszal.
**Ág:** `feature/cr260712-03-multi-gmail` · **Modell:** sonnet · **Effort:** 2 nap.
**⛔ EMBERI KAPU:** DB-migráció (unique-constraint feloldás) — DRAFT-ig.

**Olvasd el először:**
- `ConnectGmailCommandHandler` (a „első GmailAccount sor frissítése" logika)
- `source` tábla sémája + constraint-jei · `EmailIngestionPoller`
  (forrás-iteráció — állítólag már listán megy, ELLENŐRIZD)
- integrations FE-oldal · a notification-címzés „oldest-admin" mintája
  (hol dől el, ki kap értesítést)

**Döntések (zárva):**
- `source` tábla: `owner_user_id` oszlop (FK `user_account`, NOT NULL új
  fiókoknál; meglévő sor backfill: a legrégebbi admin) + a `Kind`-unique
  constraint cseréje `(kind, config_json->>'email')`-jellegű egyediségre —
  a PONTOS megoldást a séma ismeretében az implementáló dönti a migrációban,
  de fiókonként egy forrás legyen kikényszerítve.
- Connect-flow: a bejelentkezett user lesz az owner; ugyanazon Gmail-cím
  újra-connectje a meglévő forrást frissíti (nem duplikál).
- E-mail-alapú értesítések címzettje: a forrás OWNERE (nem oldest-admin) —
  ez a viselkedésváltozás dokumentálandó a release note-ban.
- UI: az integrations oldalon forrás-lista (cím, owner, utolsó sync, státusz)
  + „másik fiók csatlakoztatása" gomb.

**Lépések:** migráció (DRAFT) → handler-módosítás → poller-ellenőrzés →
UI-lista → tesztek (handler unit + integrations UI vitest + meglévő
Gmail-tesztek zöldön tartása).
**Elfogadás:** 2 fiók párhuzamosan szinkronizál · owner-alapú címzés · tesztek zöldek.
**Tilos (scope-fence):** más forrástípusok (IMAP stb.); fiókonkénti
feature-kapcsolók; a Gmail OAuth-scope bővítése.

---

## cr260712-04 — Egységes családi naptárnézet + iCal export

**Cél:** hónap/hét naptárnézet (deadlines+tasks+reminders, családtag-színkód)
+ read-only tokenes iCal feed.
**Ág:** `feature/cr260712-04-calendar` · **Modell:** sonnet · **Effort:** 2–3 nap.

**Olvasd el először:**
- dashboard FE-feature (adatlekérési minta) · deadlines/tasks/reminders
  list-endpointok · Ical.Net használata a `reminder-engine` kódban (már dependency)

**Döntések (zárva):**
- FE: saját rács-implementáció (CSS grid), NEM naptár-könyvtár; hónap- és
  hétnézet, kattintás a meglévő részletoldalra navigál.
- Aggregáló endpoint: `GET /api/v1/calendar?from=&to=` — a három forrás
  egyesített, RBAC-szűrt listája `{date, type, title, memberId, link}` sorokkal.
- iCal: `GET /api/v1/calendar.ics?token=<feed-token>` — cookie-auth NÉLKÜL,
  dedikált feed-tokennel. Token: felhasználónként generált random (32 byte,
  hash-elve tárolva), a preferences-oldalon generálható/VISSZAVONHATÓ.
  A feedben CSAK dátum+cím megy ki (leírás, linkek nem).
- Child-szerep: a naptár a saját + családi nyilvános tételeket mutatja
  (ugyanaz a szűrés, mint a listaoldalakon — a meglévő RBAC-mintát kövesd).

**Lépések:** aggregáló endpoint + teszt → FE naptárnézet + vitest →
feed-token domain (tábla/oszlop, ⛔ migráció DRAFT) → .ics endpoint + teszt →
preferences UI a tokenhez → api-design.md bővítés.
**Elfogadás:** naptár mindhárom típust mutatja színkóddal · .ics importálható
(Google Naptár próba) · visszavont token 401 · tesztek zöldek.
**Tilos (scope-fence):** írható naptár (drag&drop áthelyezés); külső naptár
IMPORT; ismétlődés-szerkesztés a naptárból.

---

## cr260712-05 — Web push értesítési csatorna

**Cél:** a `NotificationChannel` harmadik ága a reminder-eszkalációhoz.
**Ág:** `feature/cr260712-05-push` · **Modell:** sonnet · **Effort:** 3–4 nap.

**Döntés (zárva): önhostolt ntfy konténer**, NEM natív Web Push/VAPID —
indok: nincs service-worker + böngésző-kulcs komplexitás, a LocalOnly elvvel
kompatibilis, mobilon az ntfy-app is használható. (VAPID akkor kerül elő, ha
az ntfy a gyakorlatban elvérzik — külön CR.)

**Olvasd el először:**
- `CompositeNotificationService` + a meglévő csatorna-implementációk (minta)
- `reminder-engine.md` §5.3 (eszkalációs csatorna-sorrend) + `EscalationScheduler`
- docker-compose.yml (service-minta, TLS-CA script a HTTPS-hez)

**Lépések:**
1. ntfy service a compose-ba (belső hálózat, topic-per-user:
   `familyos-<userId>-<random>`; a random a kitalálhatóság ellen).
2. `NtfyNotificationPusher` a Composite-ba; user-preferencia: push
   engedélyezése + topic-QR/link megjelenítése a preferences-oldalon.
3. Eszkalációs lánc bővítése: InApp → Push → Email (a §5.3 sorrend-döntése
   szerint; a doksit frissítsd a végleges sorrendre).
4. Tesztek: pusher unit (HTTP-hívás mock), eszkaláció-sorrend unit,
   preferences UI vitest.
**Elfogadás:** reminder-eszkaláció ntfy-appban megérkezik (kézi próba
dokumentálva) · tesztek zöldek.
**Tilos (scope-fence):** VAPID/service-worker; kétirányú push (gombok a
push-ban); ntfy-account/ACL finomhangolás MVP-ben.

---

## cr260712-08 — Gyerek-nézet: feladat-gamifikáció

**Cél:** dedikált Child-dashboard nagy feladatkártyákkal, pont+streak
számítással, szülői jóváhagyással (meglévő approve-flow).
**Ág:** `feature/cr260712-08-child-view` · **Modell:** sonnet · **Effort:** 2–3 nap.
**⛔ EMBERI KAPU:** pont-tábla migráció DRAFT-ig.

**Olvasd el először:**
- task állapotgép (start/complete/approve) + ADR-0007 (RBAC)
- dashboard FE-feature (route-guard minta a szerep-alapú eltereléshez)

**Döntések (zárva):**
- MVP = **pont + streak, jutalomkatalógus NÉLKÜL** (kőbe vésett scope-fence).
- Pontszámítás: approve-olt task = 10 pont; napi streak = van-e aznap
  approve-olt task; a számítás lekérdezés-alapú, DE a streak-hez egy kis
  `child_score` tábla (userId, points, currentStreak, lastApprovedDate) —
  az approve-handler eseményére frissül.
- Child-user belépés után automatikusan a gyerek-dashboardra kerül
  (route-guard); a normál dashboard Childnak nem elérhető.
- A pontok a szülői felületen is látszanak (family-oldal kis kiegészítése).

**Lépések:** score-tábla (⛔ DRAFT) + score-service + approve-hook + teszt →
gyerek-dashboard FE (nagy kártyák, pipálás → a meglévő complete-flow) +
vitest → route-guard + teszt → family-oldal pont-megjelenítés.
**Elfogadás:** Child-flow e2e (mockolt spec): belépés → feladat pipálás →
szülő approve → pont nő · tesztek zöldek.
**Tilos (scope-fence):** jutalomkatalógus, pont-beváltás, badge-ek,
ranglisták; task-létrehozás Child-szereppel; RBAC-lazítás.

---

## Priorizálási sorrend (zárt döntés — ebben a sorrendben töltsd a backlogba)

| # | CR | Indok |
|---|---|---|
| 1 | cr260712-01 | adósság-zárás — de csak kereszthivatkozás a 01/02 kártyákra |
| 2 | cr260712-06 | olcsó, a CR260710-08-at is kiszolgálja |
| 3 | cr260712-02 | kész infrastruktúrán magas érték/költség |
| 4 | cr260712-07 | üzemeltetési alapkő |
| 5 | cr260712-03 | e-mail-feature-ök értékét sokszorozza |
| 6 | cr260712-04 | látványos felhasználói érték |
| 7 | cr260712-05 | reminder-eszkaláció kiteljesítése |
| 8 | cr260712-08 | a fentiek után |

# CR-ötletek — Family OS (Fable review nyomán)

> Státusz: JAVASLAT v1.0 · Dátum: 2026-07-12
> Forrás: kód- és doksi-átvizsgálás (01–03. review-doksik) + meglévő CR-sor
> (cr260710-01…08) áttekintése. A számozás a következő szabad CR-slotokat
> követi: `cr260712-XX`. Egyik sem duplikálja a futó CR-eket
> (04 egészségügyi idővonal, 05 pénzügyi intelligencia, 08 AI-feedback).

---

## cr260712-01 — Biztonsági keményítési csomag (a doksi-ígéretek beváltása)

**Mi:** A security-privacy.md-ben leírt, de nem implementált védelmi rétegek
egy csomagban: rate limiting (globális + AI-partíció), login-throttling,
Gmail-token DataProtection-titkosítás, `AddDataProtection` a `dp_keys`
volume-ra, audit_log insert-only + REVOKE.
**Miért:** A 02-es review-doksi S1–S5 tételei — a rendszer dokumentált
biztonsági szintje és a valóság közti rés bezárása. LAN-only környezetben is
indokolt: a Gmail-token és az audit-integritás a két legérzékenyebb pont.
**Érintett:** API middleware/DI, `ConnectGmailCommandHandler`,
`GmailIngestionService`, `DbSeedRunner`, 1 migráció, docker-compose.
**Effort:** közepes (2–3 nap). **Kockázat:** alacsony; a token-migráció
(meglévő plaintext → titkosított) igényel átállási lépést.
**Kapcsolódik:** 01-es doksi 1.3–1.4, 2.2; 02-es doksi S1–S5.

## cr260712-02 — Tool-katalógus bővítése (NL parancsok 2. üteme)

**Mi:** A meglévő, bizonyítottan biztonságos tool-calling pipeline-ra
(ADR-0011) új toolok: `complete_task`, `snooze_reminder` / `acknowledge_reminder`,
`create_note`, `resolve_deadline`. Opcionálisan: több javaslat egy körben
(batch-proposal kártyák).
**Miért:** Az infrastruktúra (planner, token, replay-guard, confirm UI) kész
és tesztelt — minden új tool marginális költségű, a felhasználói érték
(„pipáld ki a bevásárlást", „halaszd holnapra") magas. A CR260710-07 természetes
folytatása.
**Érintett:** `Application/Ai/Tools/*` (tool-onként 1 osztály + teszt),
tool-registry, FE proposal-kártya (változatlan), prompt-katalógus.
**Effort:** tool-onként kicsi (0,5 nap/tool). **Kockázat:** alacsony —
a végrehajtás továbbra is megerősítéshez kötött.

## cr260712-03 — Több Gmail-fiók támogatása

**Mi:** Jelenleg a rendszer egyetlen Gmail-forrást kezel
(`ConnectGmailCommandHandler` a `Kind == GmailAccount` első sorát frissíti).
Családi rendszerben tipikus a 2+ szülői fiók: fiókonkénti forrás,
tulajdonos-hozzárendelés (melyik családtag fiókja), fiókonkénti sync-státusz
az integrációs oldalon.
**Miért:** A „családi OS" értékajánlat féloldalas, ha csak egy szülő
postaládája folyik be; az e-mail-alapú feature-ök (fontos e-mail, határidő-
kinyerés, digest) értéke fiókszámmal skálázódik.
**Érintett:** `Sources` domain (unique-constraint feloldás), connect/callback
flow, `EmailIngestionPoller` (már most forrás-listán iterál — kevés munka),
integrations UI.
**Effort:** közepes (2 nap). **Kockázat:** alacsony; a
notification-címzés (oldest-admin minta) átgondolandó fiók-tulajdonos alapúra.

## cr260712-04 — Egységes családi naptárnézet + iCal export

**Mi:** A deadlines/tasks/reminders adatok egyetlen hónap/hét naptárnézetben,
családtag-színkóddal; read-only iCal feed (`/api/v1/calendar.ics`, tokenes
URL), hogy a meglévő telefonos naptárakba beköthető legyen.
**Miért:** A dashboard lista-alapú; a határidő-sűrűség naptárban válik
áttekinthetővé. Az iCal export a „LAN-only, mégis elérhető útközben" rést
kezeli adatkiadási kompromisszum nélkül (csak dátum+cím megy ki).
**Érintett:** 1 új FE feature-mappa, 1 read-only API endpoint, Ical.Net
(már dependency!) a feed-generáláshoz.
**Effort:** közepes (2–3 nap). **Kockázat:** a tokenes feed-URL biztonsági
átgondolást igényel (revokálhatóság).

## cr260712-05 — Web push értesítési csatorna (ntfy vagy Web Push API)

**Mi:** A `NotificationChannel` enum harmadik ága: önhostolt ntfy-konténer
vagy natív Web Push (VAPID) a docker-compose stackben; a reminder-eszkaláció
következő csatornájaként (`reminder-engine.md` 5.3 „nem MVP" feloldása).
**Miért:** Az InApp csak nyitott fülnél ér célt, az e-mail lassú — a
reminder-motor eszkalációs logikája (kész!) push nélkül nem tud igazán
„utolérni" senkit. Önhostolt ntfy a LocalOnly elvvel is kompatibilis.
**Érintett:** `CompositeNotificationService` + új pusher, compose-service,
FE service-worker / ntfy-előfizetés UI, eszkalációs csatorna-váltó.
**Effort:** közepes-nagy (3–4 nap). **Kockázat:** service-worker + HTTPS
követelmények LAN-on (a meglévő TLS-CA script segít).

## cr260712-06 — AI-telemetria dashboard (token, latency, siker-ráta)

**Mi:** Az `OllamaHttpClient` már visszaadja az input/output tokenszámot,
az `ai_processing_job` a futásidőt — de sehol nem aggregálódik. Admin-oldal:
job-típusonkénti token-fogyasztás, átlag-latency, hibaráta, modell-verzió;
napi trend. A CR260710-08 (AI-feedback) kiértékeléséhez is ez a mérőműszer.
**Miért:** A qwen3-coder:30b Raspberry Pi/strong-PC profilokon nagyon eltérően
viselkedik; modell- és promptcserék hatása jelenleg vakrepülés. Olcsó CR,
mert az adat már megvan, csak lekérdezés + UI kell.
**Érintett:** 1–2 aggregáló query (`ai-jobs` admin API bővítés), FE admin
aloldal (a meglévő ai-jobs oldal mintájára).
**Effort:** kicsi (1–1,5 nap). **Kockázat:** minimális.

## cr260712-07 — Automatizált, titkosított backup + restore-próba

**Mi:** A `scripts/backup.sh` + security-privacy.md 11.2 formalizálása:
ütemezett `pg_dump` + dokumentum-volume mentés `age`-titkosítással, retenciós
policy, és — a lényeg — automatikus **restore-verifikáció** (a dump
visszatöltése egy throwaway konténerbe + smoke-query). Backup-státusz a
meglévő `/settings/backup` oldalon valós adatokkal.
**Miért:** Családi rendszernél a teljes adatvagyon egyetlen gépen él; a nem
tesztelt backup nem backup. A settings/backup UI ma statikus infót mutat.
**Érintett:** compose (backup sidecar vagy host-cron), scripts, 1 kis API
endpoint a státuszhoz, backup UI.
**Effort:** közepes (2 nap). **Kockázat:** alacsony.

## cr260712-08 — Gyerek-nézet: feladat-gamifikáció

**Mi:** A Child szerepkör ma „lebutított Adult". Dedikált gyerek-dashboard:
saját feladatok nagy kártyákon, elvégzés-pipálás → pontok/sorozatok (streak),
szülői jóváhagyás a kész-jelöléshez (a meglévő approve-flow újrahasznosítása).
**Miért:** A family-OS akkor él, ha a gyerekek is használják; a task
állapotgép (start/complete/approve) és az RBAC (ADR-0007) készen áll hozzá,
csak nézet + pontszámítás kell.
**Érintett:** FE gyerek-dashboard, kis pontszámító service + tábla,
task-approve flow kötés.
**Effort:** közepes (2–3 nap). **Kockázat:** scope-csúszás veszélye —
az MVP legyen pont+streak, jutalomkatalógus nélkül.

---

## Priorizálási javaslat

| Sorrend | CR | Indok |
|---|---|---|
| 1 | cr260712-01 (biztonsági csomag) | adósság-zárás, minden mást megelőz |
| 2 | cr260712-06 (AI-telemetria) | olcsó, és a futó CR260710-08-at is kiszolgálja |
| 3 | cr260712-02 (tool-bővítés) | kész infrastruktúrán magas érték/költség arány |
| 4 | cr260712-07 (backup) | üzemeltetési alapkő, restore-próbával |
| 5 | cr260712-03 (több Gmail) | az e-mail-alapú feature-ök értékét sokszorozza |
| 6 | cr260712-04 (naptár + iCal) | jól látható felhasználói érték |
| 7 | cr260712-05 (web push) | a reminder-eszkaláció kiteljesítése |
| 8 | cr260712-08 (gyerek-nézet) | érdekes, de a fentiek után |

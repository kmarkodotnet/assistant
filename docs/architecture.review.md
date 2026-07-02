# Review — architecture.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Átgondolt, pragmatikus architektúra: tiszta rétegzés, két composition root
(Api + Workers), jól indokolt kétszintű job-queue (domain `ai_processing_job`
+ Hangfire), privacy guard a provider-váltásra, catch-up szemantika.
A dokumentum konkrét — interfész-szignatúrákkal, konfigurációs példával.
Néhány következetlenség és technikai kockázat javítandó.

## Hibák / következetlenségek

### 1. Failed jobok újrafelvétele vs. DB-index (közepes)
A 6.2 szerint a scheduler „a `Failed + next_attempt_utc <= now` sorokat is”
felveszi — a database-schema.md `ix_aijob_queue` indexe viszont
`WHERE status = 'Queued'` partial index. Két lehetőség: (a) a retry
visszaállítja a státuszt `Queued`-ra (akkor a 6.2 szövege pontosítandó),
vagy (b) a scan a `Failed` sorokra is fut (akkor az index nem fedi).
A két doksi jelenleg ellentmond; döntsük el és rögzítsük mindkét helyen.

### 2. `/healthz/ready` = DB **és** Ollama (közepes)
A 12. szakasz szerint a readiness az Ollama elérhetőségét is nézi. Az
Ollama viszont a saját tervek szerint is lehet lassan induló / opcionálisan
futó komponens (9. szakasz: „igény szerint kérve”). Ha a ready az Ollamára
is vár, az API sosem lesz „ready”, amíg a modell be nem töltött — pedig a
feltöltés, listázás, strukturált keresés AI nélkül is működik. Javaslat:
Ollama csak *degraded* jelzés legyen a ready-ben, ne fail.

### 3. .NET verzió (közepes)
„ASP.NET Core 8 Minimal API” — lásd idea.review.md: 2026-ban a .NET 8
támogatása az év végén lejár; induló projektnek .NET 10 LTS javasolt.
A CLAUDE.md is „legújabb LTS”-t ír elő. Egy sorban rögzítendő ADR-ként.

### 4. DueReminderDispatcher — race és duplikált tüzelés (közepes)
A 6.4/11.3 folyamat (scan → dispatch → státusz `Fired`) nem tér ki arra,
mi történik, ha a dispatch (email küldés) sikerül, de a státusz-írás előtt
a process meghal → dupla értesítés újrainduláskor. Egy mondat kell az
idempotencia/at-least-once vállalásról (a notification_feed alapján
dedup-olható). A 6.2 az AiJobExecutorra ezt példásan rögzíti — ugyanez
hiányzik a reminder-ágon (a reminder-engine.md-ben ellenőrizendő).

### 5. Catch-up ablak következetlenség (kicsi)
6.4: induláskor a scan `now() - 7 days`-tól fut — 11.3-ban ugyanez a
StartupCatchUp *minden* `trigger_utc <= now()` Scheduled sort felvesz,
időkorlát nélkül. Melyik igaz? (A reminder-engine.md-vel is egyeztetendő.)
A 7 napnál régebbi „lecsúszott” emlékeztető sorsa (Skipped?) definiálandó.

### 6. SignalR nem szerepel a szerkezetben (kicsi)
A 11.1 flow végén „SignalR push az UI-nak”, az api-design.md 22. szakasza
két hubot definiál — de a 2–3. szakasz solution-szerkezete és a 9. szakasz
deployment-ábrája nem említi a SignalR-t (melyik process hosztolja? Api.
A Workers hogyan triggerel? Redis backplane nincs — egy gépen oké, de
írjuk le). Egy rövid alszakasz hiányzik.

### 7. Apróságok
- 3.2: „Mapster ajánlott” — a coding-standards.md-vel egyeztetendő, hogy
  döntés vagy ajánlás (a devtaskokban már függőségként szerepel-e).
- 5.2 példában `"Model": "claude-haiku-4-5-20251001"` — külső modellnév
  gyorsan avul; jelezni, hogy placeholder.
- 9. szakasz: „Kestrel TLS-mentes… nginx tesz HTTPS-t belső CA-val” —
  a `__Host-` cookie prefix (api-design 1.4) **HTTPS-t követel**; a
  láncnak (nginx TLS → api HTTP) működnie kell, de a cookie `Secure`
  flag miatt a fejlesztői HTTP-s felállás nem fog menni — dev-módra
  legyen leírt kivétel.
- A 10. szakasz „lazy-loaded feature module-ok” — Angular 20 standalone
  világban „lazy-loaded route-ok” a pontos fogalom (a frontend-structure.md
  helyesen kezeli).

## Verdikt

Szolid architektúra, implementálható. A #1 (retry-szemantika) és #4
(reminder idempotencia) tisztázása kód-írás előtt kötelező; a többi
pontosítás.

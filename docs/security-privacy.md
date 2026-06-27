# Biztonság és adatvédelem — Family OS

> Státusz: DRAFT v0.1 · Dátum: 2026-06-26 · Nyelv: magyar
> Kapcsolódó: [product-vision.md](product-vision.md), [architecture.md](architecture.md),
> [domain-model.md](domain-model.md), [ai-pipeline.md](ai-pipeline.md)
> Rögzített döntések: [ADR-0003 LAN-only](decisions/ADR-0003-mobil-csak-lan.md),
> [ADR-0004 Gmail API](decisions/ADR-0004-email-gmail-api.md)

---

## 1. Vezérlőelvek

A Family OS érzékeny családi adatokat (egészségügyi, pénzügyi, jogi) kezel
egy önhostolt környezetben. A biztonsági modell ennek megfelelően
**privacy-first**, **minimal-attack-surface**, és a **felhasználói kontroll**
elvét követi.

1. **A felhasználó az adatok tulajdonosa.** Az adat soha nem hagyja el a
   háztartást olyan módon, amit a felhasználó nem engedélyezett expliciten.
2. **Defense in depth.** Minden réteg (hálózat, alkalmazás, adatbázis,
   fájltár, AI provider) önállóan is megakadályoz bizonyos támadási
   osztályokat.
3. **Least privilege.** Minden komponens (DB user, OS folyamat, OAuth scope)
   a minimum szükséges jogot kapja.
4. **Auditábilis.** Minden lényeges művelet (login, hozzáférés-változás,
   AI hívás, fájl letöltés) naplózva van.
5. **Felülbírálhatatlan kapuk.** A privacy-érzékeny kapuk (LocalOnly AI
   mód, audit log immutability) nem felülírhatók feature-flag-gel vagy
   konfigurációval — kódba égetve, factory-engineer számára tiltott terület
   (lásd `CLAUDE.md`).

---

## 2. Fenyegetés-modell (rövid)

### 2.1 Védendő javak (assets)

| Asset | Érzékenység | Megj. |
|---|---|---|
| Dokumentumok (PDF/kép) | Magas | egészségügyi, pénzügyi, jogi |
| Strukturált adatok (Warranty, Medical, Financial) | Magas | |
| Embedding-ek (`document_chunk.embedding`) | Közepes | rekonstruálható szövegrészletek |
| OAuth refresh tokenek (Gmail) | Magas | hosszú élettartamú |
| Google login session cookie | Magas | |
| Audit log | Magas | integritás-kritikus |
| Postgres backup fájlok | Magas | |

### 2.2 Támadói modell (kit védünk ellen)

| Támadó | Vektor | Mit védünk |
|---|---|---|
| **Külső internet** | nem releváns | ADR-0003: LAN-only, nincs publikus port |
| **LAN-ra bejutott eszköz** (vendég Wi-Fi, IoT) | LAN-belüli HTTP | autentikáció + TLS |
| **Családtag-jogosultság visszaélés** | hozzáfér más családtag privát rekordhoz | RBAC + IsPrivate + audit |
| **Adat-szivárgás külső AI-ra** | rosszul konfigurált provider | LocalOnly hard-gate |
| **PC ellopás** | fizikai hozzáférés a diszkhez | disk-szintű titkosítás (lásd 7.5) |
| **Adminisztratív hiba** | véletlen törlés, rosszul konfigurált jog | soft-delete, audit log, backup |

### 2.3 Explicit nem-célok

- DDoS védelem (LAN-only — irreleváns).
- Anti-CSRF egy publikus webapp-ra (LAN-only, same-site cookie elég).
- Side-channel támadások a host gépen (PC fizikai biztonsága a felhasználó
  felelőssége).

---

## 3. Hitelesítés (authentication)

### 3.1 Google OAuth 2.0

- **Flow:** Authorization Code with PKCE.
- **OpenID Connect** profil + email scope.
- **Backend validáció:** a Google `id_token` aláírás-, kibocsátó-, lejárat-
  és audience-validáció (`Microsoft.AspNetCore.Authentication.Google`).
- **Engedélyezett `hd` (hosted domain) vagy email allowlist:** opcionálisan
  konfigurálható — a Family OS alapból egy fix `allowed_emails` listát
  használ (`appsettings.json` → `Auth.AllowedEmails`).
- **Első login bootstrap:** az `Auth.BootstrapAdmin` email cím első
  login-ja `Role = Admin`-nal hozza létre az accountot. Minden további
  email az allowlist-ről `Role = Child` kezdő szerepkört kap — az admin
  felemeli, ha kell.

### 3.2 Session

- **Cookie:** HTTP-only, Secure (TLS LAN-belül), SameSite=Lax. Név:
  `__Host-family-os-session`.
- **Tárolás:** ASP.NET Core Data Protection-szel titkosított cookie
  (`CookieAuthenticationHandler`); nincs server-side session table.
- **Élettartam:** 30 nap sliding expiration, 90 nap absolute. Manual
  logout → cookie azonnali invalidálása + `RevokedSessions` (kis blacklist
  tábla a re-use ellen).
- **Inactivity timeout:** 7 nap idle után újra-auth (Google session
  szilent reauth, ha még él).

### 3.3 Mobil / API kliens

- Csak ugyanaz a Google OAuth folyamat web-view-ban. **Nincs külön API-key**
  vagy Personal Access Token MVP-ben — single-tenant + LAN-only környezetben
  nincs realisztikus szükséglet.

### 3.4 Brute force / login throttling

- **Failed-login throttling.** Az `audit_log`-ban a `LoginFailed`
  bejegyzések alapján: 5 hibás kísérlet 10 percen belül → 15 perc
  IP-tiltás (LAN belül, részleges védelem, nem kritikus).
- Google OAuth: maga a Google végzi a credential-szintű védelmet — a
  Family OS csak az `id_token` validáció szintjén.

---

## 4. Engedélyezés (authorization)

### 4.1 Szerepkörök (RBAC)

A három szerepkör (`Admin`, `Adult`, `Child`) értelmezése:

| Művelet | Admin | Adult | Child |
|---|:---:|:---:|:---:|
| Saját rekord olvasás/írás | ✓ | ✓ | ✓ (csak saját, megosztott) |
| Más adult rekordja (nem private) | ✓ | ✓ | ✗ |
| Más adult rekordja (private) | ✓ | ✗ | ✗ |
| Saját facet (Medical) olvasás | ✓ | ✓ | ✓ (csak ha hozzá kötött) |
| Más családtag Medical olvasás | ✓ | csak partner-spouse (config) | ✗ |
| Tag/Topic módosítás | ✓ | ✓ | ✗ |
| Dokumentum törlés (soft) | ✓ | ✓ (saját) | ✗ |
| Hard delete / végleges törlés | ✓ | ✗ | ✗ |
| Családtag létrehozás / szerep módosítás | ✓ | ✗ | ✗ |
| AI provider konfiguráció | ✓ | ✗ | ✗ |
| Audit log megtekintés | ✓ | ✗ | ✗ |
| Hangfire dashboard | ✓ | ✗ | ✗ |
| OAuth integráció hozzáadás (Gmail) | ✓ | ✗ | ✗ |

### 4.2 Authorization policy implementáció

```csharp
services.AddAuthorization(opts =>
{
    opts.AddPolicy("RequireAdmin",
        p => p.RequireRole("Admin"));
    opts.AddPolicy("RequireAdult",
        p => p.RequireRole("Admin", "Adult"));
    opts.AddPolicy("RequireAuthenticated",
        p => p.RequireAuthenticatedUser());
});
```

Endpoint-szinten:
```csharp
group.MapPost("/", UploadDocument)
     .RequireAuthorization("RequireAdult");
```

### 4.3 Row-level (rekord-szintű) engedélyezés

Az endpoint-policy nem elég — minden olvasás/írás után a
`IAuthorizationService.AuthorizeAsync(user, entity, Requirement)` hívódik,
amely az alábbi szabályok szerint dönt:

```
Document:
  - admin: minden
  - tulajdonos (CreatedByUserAccountId == current.UserAccountId): minden
  - Adult, isPrivate=false: olvasás (és írás, ha a related_family_member
    nem egy idegen private rekord)
  - Adult, isPrivate=true: tilt
  - Child: csak ha related_family_member_id == current.FamilyMemberId
    ÉS isPrivate=false

MedicalRecord:
  - admin: minden
  - érintett FamilyMember saját UserAccount-ja: olvasás+írás
  - érintett partnere (Relation=Spouse, Role=Adult), config-bound: olvasás
  - egyébként: tilt
```

A `SearchAuthorizationService` (lásd `search-strategy.md` 6.) ugyanezt a
logikát adatbázis-szintű `WHERE`-be vetíti — post-filter nem elég, az
oldal-aggregátumok és relevancia-ranking szivárogtatást engednének.

### 4.4 Privát rekord (IsPrivate) szemantika

- A `IsPrivate = true` jelzés azt jelenti: **a rekord láthatósága
  szűkebb, mint a default RBAC**.
- A `MedicalRecord` default `IsPrivate = true` (séma-szinten).
- A `Document` és `Note` default `IsPrivate = false`; a felhasználó
  kapcsolóval beállíthatja.
- A `Document.RelatedFamilyMemberId` egy *cél-családtagot* jelöl
  (kinek szól), nem feltétlenül a tulajdonost; az `IsPrivate` a
  *tulajdonos és a megosztási kör* dolga.

---

## 5. Audit log

### 5.1 Tárolás

- `app.audit_log` tábla, insert-only kényszerrel (DB trigger + REVOKE).
- Részletek a `database-schema.md` 4.17-ben.

### 5.2 Mit logolunk?

| Esemény | Action | Részletek |
|---|---|---|
| Login (sikeres) | `Login` | UserAgent, IP |
| Login (sikertelen) | `LoginFailed` | Email, hibakód |
| Rekord létrehozás | `Create` | EntityType, EntityId, kulcsmezők JSON-ban |
| Rekord módosítás | `Update` | EntityType, EntityId, diff (JSON) — csak változott mezők |
| Rekord törlés (soft) | `Delete` | EntityType, EntityId |
| Jogosultság-változás | `PermissionChange` | UserAccountId, oldRole → newRole |
| AI hívás | `AiCall` | Provider, model, jobType, target, **prompt hash**, latency |
| Suggestion jóváhagyás / elvetés | `Approve` / `Reject` | EntityType, EntityId |
| Dokumentum letöltés / megnyitás | `FileAccess` | DocumentId, ki olvasta |

### 5.3 Mit NEM logolunk

- **Prompt teljes szövege.** Csak hash + hossz. Indok: az audit log
  privacy-érzékeny rekordok szövegét nem duplikálhatja.
- **AI válasz teljes szövege.** Csak méret + hash.
- **Jelszó / token.** Egyetlen titok sem kerül logba.
- **Az érintett rekord teljes tartalma.** A `details_json` mező csak
  kulcsmezőket, nem nyers szöveget tartalmaz.

### 5.4 Megőrzés és titkosítás

- **Megőrzés:** alapból végtelen. Admin-konfigurálható retention (`appsettings.json`
  → `Audit.RetentionDays`); ha beállított, egy napi takarító törli a régi
  rekordokat — de **csak** ha a `Audit.Immutable = false` (alapból
  `true`, ezzel a takarító inaktív).
- **Titkosítás:** ugyanaz a DB-szintű volume-titkosítás, mint a többi
  táblára (7.5).

### 5.5 Integritás-védelem

A `audit_log` insert-only DB triggerrel + `REVOKE UPDATE, DELETE` az `app`
role-on. Egy támadó, aki a DB-hez hozzáfér superuser jogosultsággal,
módosíthatja — ez ellen MVP-ben nincs Merkle-chain / külső WORM tár;
későbbi enhancement: napi audit-hash a `data/audit-anchors/` mappába
write-once módban (append-only fájl).

---

## 6. Titkosítás

### 6.1 Átvitelben (in transit)

- **Web → backend:** HTTPS belső CA-val (mkcert / self-signed root). Az
  admin telepíti a CA-tanúsítványt minden családi eszközre (egyszeri
  művelet). A Kestrel HTTP-en hallgat, az nginx reverse proxy ad TLS-t.
- **Backend → Postgres:** Docker network internal, TLS opcionális
  (default nem). Indok: ugyanaz a host, minimal threat.
- **Backend → Ollama:** Docker network internal, plain HTTP, nincs
  titkosítás (egy hoston belül).
- **Backend → Gmail API / OpenAI / Anthropic:** HTTPS kötelező (`HttpClient`
  alap).
- **Backend → SMTP:** STARTTLS kötelező.

### 6.2 Nyugalomban (at rest)

- **Adatbázis:** a Postgres volume titkosítva a host OS lemezén
  (LUKS Linux / BitLocker Windows / FileVault macOS). MVP-ben **dokumentált
  felhasználói felelősség** a setup során — a `docs/DELIVERY.md`-ben
  egyszerű útmutató.
- **Fájltár:** ugyanaz a volume / partíció.
- **OAuth tokenek a `source.config_json`-ban:** alkalmazás-szintű
  titkosítás ASP.NET Core Data Protection-szel. A kulcs a host
  fájlrendszerén (`/var/lib/family-os/dp-keys/`), csak a `family-os`
  OS-user olvashatja.
- **Backup fájlok:** `pg_dump`-ot egy `age`-szel titkosítva mentjük a
  `data/backups/`-ba; a passphrase a hostos jelszó-tárolóban (KeePass
  / 1Password) van — a felhasználó feladata.

### 6.3 Hash-elés és aláírás

- **Sha256** a dokumentum-dedup-hoz (`document.sha256`).
- **Bcrypt** — nincs jelszó-tárolás (Google OAuth), tehát nincs bcrypt
  igény.
- **JWT** — nem használunk; cookie-alapú session.

### 6.4 Kulcsmenedzsment

- **ASP.NET Core Data Protection** kulcsok: rotáció 90 naponta, automatikus,
  régi kulcs 180 napig olvasásra megőrződik.
- **OAuth client secret** (Google): `appsettings.json` titkosított vagy
  ENV változóból; sosem repository-ba.

---

## 7. Biztonságos fájl-tárolás

### 7.1 Útvonal és jogosultság

- A fájlok a `${FAMILYOS_DATA_DIR}/documents/<év>/<hónap>/<guid>.<ext>`
  útvonalon élnek (`architecture.md` 7.1).
- A `family-os` OS-user (UID 1000 a Docker konténerben) az egyetlen
  író, és az olvasáshoz is a backend folyamat kell.
- A host volume mount `mode=0750`, `owner=1000:1000`.

### 7.2 Path traversal védelem

- Minden `IDocumentStorage.OpenReadAsync(storagePath)` validálja, hogy a
  `storagePath` a `documents/` prefix alatt van, és nem tartalmaz
  `..` szegmenset.
- Az endpoint-ok soha nem fogadnak el user-controlled fájlútvonalat —
  csak `DocumentId`-t; a tényleges path-t a DB-ből olvassák.

### 7.3 MIME / magic byte validáció

- Feltöltéskor a MIME header **csak hint**, a valódi típust a fájl első
  bájtjaiból állapítjuk meg (`Mime-Detective` vagy hasonló).
- Whitelist: PDF, JPEG, PNG, HEIC, TXT, DOCX. Egyéb → 400 + magyar
  hibaüzenet.

### 7.4 Méret-limit

- Default 50 MB / fájl, alkalmazás-szinten + nginx-szinten
  (`client_max_body_size 50m;`). Konfigurálható: `appsettings.json`
  → `Documents.MaxSizeBytes`.

### 7.5 Vírusellenőrzés

- **MVP-ben nincs.** Indok: zárt családi környezet, minden upload egy
  ismert családtagtól jön, a backend nem hajt végre fájlokat.
- Későbbi enhancement: ClamAV sidecar konténer, `IDocumentStorage.SaveAsync`
  visszaadja a vírus-státuszt; gyanús fájl karanténba kerül.

### 7.6 Lemez-titkosítás

- Lásd 6.2 — host OS-szintű full-disk encryption telepítési követelmény.
  A `docs/DELIVERY.md` rögzíti a setup-checklist-et.

---

## 8. AI privacy és LocalOnly kapu

### 8.1 LocalOnly mód

A `PrivacyMode = LocalOnly` az alapértelmezett. Ez egy **kemény kapu**:

- Az `AiProviderFactory.GetProvider(taskType)` egyetlen erre vonatkozó
  feltételt ellenőriz először: `Config.PrivacyMode == LocalOnly &&
  provider.Name != "ollama"` → kivétel (`AiProviderNotAllowedException`).
- A kivétel utat ad az audit logba (`AiCall` jelzéssel, error mezővel).
- Ez a logika **nincs feature flagben** és nem kapcsolható ki konfigon —
  csak kódmódosítással. A `factory-engineer` agent számára `CLAUDE.md`
  szerint érinthetetlen terület.

### 8.2 HybridAllowed mód

- Lehetővé teszi a vegyes használatot (pl. embedding lokálisan, summary
  cloud-on), de minden cloud-bound hívás előtt egy **explicit warning** és
  user confirmation a UI-on (egyszeri opt-in családtag-tagonként,
  napra/kategóriára).
- Az MVP **nem** futtatja default-ban; az admin kapcsolja be (`Settings`
  oldal).

### 8.3 AnyProvider mód

- Tetszőleges provider tetszőleges feladatra; nincs warning. A UI-on
  vörös értesítés jelez, hogy a privát adatok cloud-ra mennek.
- Csak fejlesztői / tesztelési használat — production deployment-en
  letiltva (`appsettings.Production.json` → `Ai.PrivacyMode: LocalOnly`).

### 8.4 Provider-szintű adatkezelési ígéretek

A cloud providerek beállításánál a UI rögzíti, melyik provider az adott
config-on `data retention = 0` mode-ban van (pl. Anthropic API
„zero data retention" opció, OpenAI `data sharing off`). Ez egy
checkbox: az admin felelőssége a provider-szintű beállítás, a Family OS
csak dokumentálja.

### 8.5 Prompt scrubbing (opcionális)

- A `HybridAllowed` és `AnyProvider` módban a backend egy lokális
  scrubber-en futtatja a prompt-okat, ami:
  - email-címeket, telefonszámokat redaktál (`[email]`, `[phone]`).
  - magyar TAJ-szám mintákat (`xxx xxx xxx`) redaktál.
  - személynevet (a `FamilyMember.FullName` mezők alapján) `[családtag]`
    helyettesítéssel cserél.
- A scrubber MVP-ben **opcionális** (admin kapcsolható), default
  `HybridAllowed`-ben bekapcsolva.

### 8.6 Embedding visszafejtés

- A `document_chunk.embedding` mezők elméletben részleges szövegrekonstrukcióra
  alkalmasak.
- Védelem: a vektor-mezők ugyanazt a DB-szintű volume-titkosítást kapják,
  mint a többi adat. Külön mezőtitkosítást MVP-ben nem alkalmazunk
  (performance miatt).

---

## 9. Input validáció és OWASP

### 9.1 SQL injection

- EF Core paraméterezett query-k mindenhol. Raw SQL csak a
  `__InitialSetup` migráció és pár teljesítmény-kritikus query — mind
  fix string, nincs konkatenáció felhasználói input-tal.

### 9.2 XSS

- Az Angular alapból escape-eli a string-interpolációt.
- A `Note.Body` markdown — a backend HTML-re renderelt outputja
  sanitize-elve (`HtmlSanitizer` csomag, `unsafe-inline` script tag-ek
  letiltva).
- CSP fejléc: `default-src 'self'; script-src 'self'; connect-src 'self'
  /api/; img-src 'self' data:;` (LAN-only környezet ezt lehetővé teszi).

### 9.3 CSRF

- SameSite=Lax cookie + anti-forgery token a state-változó endpointokra
  (ASP.NET Core beépített). Single-tenant LAN-only ellenére tartjuk —
  defense in depth.

### 9.4 IDOR (Insecure Direct Object Reference)

- Minden endpoint a `IAuthorizationService.AuthorizeAsync(user, entity)`-en
  átfut a válasz előtt. UUID-k random-szerű azonosítók (UUIDv7 — időrend
  van benne, de a tartalom nem kitalálható).

### 9.5 Mass assignment

- Bejövő DTO-k explicit property-listával; nincs `[FromBody] Entity`
  binding. A mapper a DTO → Domain átmenetnél csak engedélyezett mezőket
  állít be.

### 9.6 Open redirect, deserialization, server-side request forgery (SSRF)

- **Open redirect:** redirect URL whitelist (csak a Family OS saját
  domainjére).
- **Deserialization:** csak System.Text.Json (safe defaults), nincs
  `BinaryFormatter` vagy `XmlSerializer` user-input-on.
- **SSRF:** a backend nem fogad URL-t felhasználói inputból amiből HTTP
  hívást kezdeményezne (kivéve a Gmail API és AI providerek, melyek
  konfigurációban rögzített, fix URL-ek).

### 9.7 Rate limiting

- Globális rate limit (`Microsoft.AspNetCore.RateLimiting`): 100 req/min
  / user. AI Q&A endpoint: 10 req/min / user (drága).
- Adminisztratív endpointok korlátlan (admin user).

---

## 10. Háttér- és külső integrációk biztonsága

### 10.1 Gmail API

- **OAuth scope-ok:** csak `gmail.readonly` MVP-ben (lásd ADR-0004).
- **Refresh token** titkosítva tárolódik (lásd 6.2).
- **Hozzáférési naplózás:** minden Gmail API hívás `audit_log` rekord
  (`Action = AiCall`-ot ide nem használjuk; külön `Action = ExternalApiCall`-t
  hozzáadunk a migrációban).
- **Felhasználói tájékoztatás:** a UI minden szinkronizáció után mutatja,
  hány email lett beszívva, és bármelyik tételt törölhető a Family OS
  rendszeréből (a Gmail-ben marad).

### 10.2 SMTP

- TLS kötelező (STARTTLS).
- A felhasználó saját SMTP serverét vagy egy app-password-ed Gmail SMTP-t
  konfigurálhat — felelős a hitelesítő adatok biztonságáért.
- Email tartalom: a Family OS sosem küld dokumentum-tartalmat emailben,
  csak emlékeztető-szöveget és link-et a belső webes felületre.

### 10.3 Ollama

- Localhost / docker network only; nincs port-forwarding kifelé.
- Verzió-frissítés admin manuális (`docker compose pull && up -d`); nincs
  automatikus pull.

---

## 11. Üzemeltetési biztonság

### 11.1 Admin felület

- A `/admin` és `/hangfire` route-ok csak `Role = Admin`-nak elérhetők.
- A Hangfire dashboard saját auth filter-rel (nem csak a saját Hangfire
  auth-tal).
- IP-allowlist (LAN-only) plusz auth.

### 11.2 Backup és restore

- **Backup integritás:** napi `pg_dump` után SHA-256 hash, mentve egy
  `data/backups/manifest.txt`-be (append-only). Restore-nál a hash
  ellenőrzése kötelező.
- **Restore drill:** havonta egyszer az admin lefuttat egy
  staging-restore-t (külön DB), és ellenőrzi az integritást — eljárás
  a `DELIVERY.md`-ben.
- **Backup titkosítás:** `age` vagy `gpg`, passphrase a hostos
  jelszó-tárolóban.
- **Off-site copy:** a felhasználó manuálisan másolja egy USB-re vagy
  családi NAS-ra (nem automatizált MVP-ben — privacy elv).

### 11.3 Logok

- Serilog kimenet:
  - Stdout (Docker logs)
  - `/var/log/family-os/` rotálva (10 MB × 14 fájl)
- **Loglevel:** Information default; Debug fejlesztésben.
- **Soha nem logolunk:** OAuth token, jelszó, AI prompt teljes szöveg,
  document content (csak méret + hash).

### 11.4 Frissítések

- Konténer-image frissítés: admin manuális (`docker compose pull`).
- Adatbázis-migráció: az API indulásánál fut, ha új migráció van; rollback
  csak manuális SQL-lel (a EF Core migrations down-method generálódik, de
  nem futtatjuk automatikusan).
- **Soha automatikusan a `factory-engineer` agent nem deploy-ol éles
  migrációt** (lásd `CLAUDE.md` „2-es szint: önüzemeltetés" szakasz).

### 11.5 Incidens-válasz

- **Compromise gyanúja:** admin azonnali teendői (a `DELIVERY.md`
  incident-runbook-ban kifejtve):
  1. Backend leállítása (`docker compose stop api`).
  2. Audit log lekérdezése `LoginFailed`, `Login`, `PermissionChange`,
     `ExternalApiCall` action-ökre az utolsó 7 napra.
  3. OAuth tokenek invalidálása (Google → Security → Connected Apps).
  4. Data Protection kulcs rotáció.
  5. Backup restore staging-en az utolsó tudottan tiszta állapotból.

---

## 12. Megfelelőség és felhasználói tájékoztatás

- **GDPR (csak alap szinten):** single-tenant + saját adat → a felhasználó
  egyben adatkezelő és adatalany. A Family OS biztosítja:
  - Adatok exportja (admin → JSON dump + fájlok zip).
  - Adatok teljes törlése (admin → soft-delete-eken kívül hard delete
    folyamat 30 napos cooling-off után, dokumentált).
- **Egészségügyi adatok:** a Family OS nem orvosi rendszer, és nem
  hivatott megfelelni HIPAA / hazai egészségügyi adatkezelési
  szabályozásnak. A felhasználó saját felelősségére tárolja a Medical
  Record-okat.

---

## 13. Tesztelés és red-team

### 13.1 Statikus elemzés
- Roslyn analyzers + Sonar default ruleset.
- `dotnet list package --vulnerable` a CI-ben minden buildnél.
- npm audit a frontend buildnél.

### 13.2 Dinamikus
- ZAP baseline scan az indulási API-ra havonta (CI staging job).
- Auth-flow tesztek a Playwright `@security` címkével: privát rekord
  hozzáférési kísérlet más userrel → 403, audit log bejegyzés.

### 13.3 Privacy assertions
- xUnit teszt: `LocalOnly` módban semmilyen `HttpClient` hívás nem mehet
  az `ollama` és `localhost` kívülre. Mocked `HttpMessageHandler` panaszt
  emel, ha igen.
- xUnit teszt: a prompt-szöveg sosem jelenik meg az `audit_log.details_json`
  mezőben.

---

## 14. Korlátok és későbbi bővítések

- **MFA Google session után:** MVP-ben Google saját MFA-ja az egyetlen
  réteg. Saját app-szintű TOTP/security key v2-ben.
- **Külső SIEM integráció:** audit log syslog-ra exportálva — későbbi.
- **WORM audit:** Merkle-chain / külső anchor — későbbi (lásd 5.5).
- **HSM / külső kulcskezelés:** MVP-ben fájl-alapú DP keys; HSM nem cél.
- **Zero-knowledge titkosítás kliens oldalon** — érdekes, de jelentősen
  bonyolítja az AI pipeline-t (a backend nem tudna a tartalmon dolgozni);
  nem cél.

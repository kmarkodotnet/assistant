# Review — security-privacy.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Kifejezetten alapos biztonsági terv egy self-hosted rendszerhez:
fenyegetés-modell, RBAC-mátrix, row-level szabályok, LocalOnly kapu,
kulcskezelés, incidens-runbook, privacy-assertion tesztek. A gyenge pont:
a jogosultsági politika **nem konzisztens a többi dokumentummal**, és a
LocalOnly kapu „kódba égetettsége” önellentmondásos.

## Hibák / következetlenségek

### 1. Child-szerepkör: három doksi, három politika (súlyos)
- **product-vision.md:** child = „Csak olvasás a számára **explicit
  megosztott** rekordokon” (opt-in láthatóság, read-only).
- **search-strategy.md 6.:** child látja **az összes** `IsPrivate = false`
  rekordot + a hozzá kötötteket (opt-out láthatóság).
- **Itt (4.1/4.3):** child „Saját rekord olvasás/**írás**” + a hozzá kötött
  nem-privát rekordok olvasása.
Ez adatvédelmi szempontból a terv legfontosabb eldöntetlen kérdése.
Egyetlen normatív mátrix kell (javasolt hely: ez a doksi), a többi csak
hivatkozzon rá; és dönteni kell: a child írhat-e egyáltalán (a vízió
szerint nem).

### 2. A LocalOnly kapu „kódba égetve” — de configból jön (súlyos)
8.1: a kapu „nincs feature flagben és nem kapcsolható ki konfigon — csak
kódmódosítással”. Ugyanakkor:
- az architecture.md 5.2 config-modelljében a `PrivacyMode` sima
  `appsettings.json` érték;
- itt a 8.3 szerint az `AnyProvider` tiltása épp egy
  `appsettings.Production.json` beállításon múlik;
- a 8.2 szerint az admin a Settings oldalon bekapcsolhatja a
  `HybridAllowed`-et — az api-design.md 21.2 viszont azt mondja, MVP-ben
  az API **minden** nem-LocalOnly értéket 422-vel blokkol, `HybridAllowed`
  csak v2.
Döntés kell: MVP-ben a `PrivacyMode` fixen `LocalOnly` **kódban** (és akkor
a config-mező sem létezik), vagy configolható és a kapu csak a
TaskAssignments felülbírálását védi. A négy leírás jelenleg nem fér össze.

### 3. Allowlist / meghívó: appsettings vs. dinamikus invite (közepes)
3.1: az allowlist az `appsettings.json`-ban él (`Auth.AllowedEmails`),
minden új email `Child` kezdő szerepet kap. Az api-design.md 6.2 viszont
runtime `POST /user-accounts/invite`-ot definiál (email + familyMemberId +
role), ami configfájlba nem írhat. Továbbá: allowlistes első loginnál
melyik `FamilyMember`-hez kötődik a fiók (a `user_account.family_member_id`
NOT NULL)? A meghívó-flow DB-táblát igényel (lásd
domain-model.review.md #2) — az appsettings-lista legfeljebb bootstrap.

### 4. „Nincs server-side session table” — de van (kicsi)
3.2: „nincs server-side session table”, majd ugyanott: „`RevokedSessions`
(kis blacklist tábla)”. A kettő együtt: a session maga cookie-ban él, de
a revokáció táblát igényel — ez rendben van, csak a megfogalmazás
ellentmondásos. Fontosabb: a `RevokedSessions` tábla **hiányzik a
database-schema.md-ből** — pótolandó (mi a kulcs? session-id claim kell
a cookie-ba).

### 5. CSRF: non-goal és mégis cél (kicsi)
2.3 explicit nem-célnak mondja az anti-CSRF-et („same-site cookie elég”),
a 9.3 viszont anti-forgery tokent ír elő defense-in-depth alapon. A 9.3 a
helyes irány — a 2.3-ból törlendő, vagy átfogalmazandó („teljeskörű
publikus-webes CSRF-védelem nem cél, SameSite + anti-forgery marad”).

### 6. Migráció futtatás: startup vs. külön lépés (kicsi)
11.4: „Adatbázis-migráció: az API indulásánál fut” — a database-schema.md
7. szakasza szerint viszont prod-on **külön** `migrate` Docker-parancs fut,
startup-migráció csak lokális dev. Egységesítendő (a séma-doksi változata
a biztonságosabb, és a `family_migrator` szerepkör-szétválasztás is csak
azzal működik).

### 7. Kisebb észrevételek
- 9.2 CSP: a `connect-src 'self' /api/;` érvénytelen — a `/api/` nem
  CSP source expression (a `'self'` már lefedi). Törlendő.
- 3.1 bootstrap: „minden további allowlist-email `Child` szerepet kap” vs.
  database-schema 5.3 seed („további családtagokat az admin vesz fel”) és
  az invite-flow (role a meghívóban) — a három onboarding-leírás
  egyesítendő egyetlen flow-vá.
- 12. GDPR hard-delete: a `family_app` DB-role-nak nincs DELETE joga
  (database-schema.review.md #3) — a törlési folyamat mechanizmusa
  definiálandó.
- 8.5 prompt scrubbing: a TAJ-minta (`xxx xxx xxx`) így minden 9 jegyű
  csoportosított számot redaktál (telefonszámokat is) — jelezni, hogy ez
  vállalt túl-redaktálás.

## Erősségek (megőrzendő)

- Fenyegetés-modell táblázatos, a LAN-only feltevéshez igazítva (2.).
- Audit „mit NEM logolunk” szakasz (5.3) — ritka és értékes.
- Privacy-assertion tesztek (13.3): a LocalOnly kapu *tesztben* is
  kikényszerítve.
- Path traversal + magic-byte validáció + méret-limit hármas (7.2–7.4).

## Verdikt

Tartalmilag erős, de a #1 (child-politika) és #2 (PrivacyMode kapu)
döntése nélkül a fejlesztő agentek egymásnak ellentmondó szabályokat
implementálnának. Ez a két pont blokkoló a BUILD fázis előtt.

# BUG-1: `POST /api/v1/tool-calls/confirm` 401 (lejárt proposal-token) tévesen kijelentkezteti a felhasználót

- **Súlyosság:** Magas (funkcionális törés + félrevezető UX a E8 feature fő hibaágán)
- **Feature:** E8. Természetes nyelvű parancsok (LLM tool-calling) — `docs/mvp-backlog.md` E8. AC: „Hibás/nem-értelmezhető LLM-kimenet → nincs végrehajtás… hibaüzenet jelenik meg, nem crashel a UI".
- **Érintett fájlok:**
  - `frontend/src/app/core/api/http-error.interceptor.ts` (sor 33–34: globális 401 kezelés)
  - `frontend/src/app/features/search/services/search.facade.ts` (`confirmToolCall`, sor 58–90)
  - Kontraktum: `docs/api-design.md` §16.3.1 — „**401** lejárt/érvénytelen token" a `tool-calls/confirm` válaszra.

## Lépések a reprodukcióhoz

1. Bejelentkezett admin user, `/search` oldal, mód = **Parancs**.
2. Utasítás beküldése (`create_reminder` javaslat érkezik, `toolCallProposal` a válaszban).
3. A megjelenő javaslat-kártyán "Jóváhagyás" gomb megnyomása.
4. A backend a `POST /api/v1/tool-calls/confirm`-ra **401**-et ad (api-design.md §16.3.1 szerint ez azt jelenti: *„lejárt/érvénytelen [proposal-]token"* — NEM a felhasználó munkamenetének lejárta).

Mock/repro script: standalone Playwright script futtatva a `frontend` mappából
(`node repro-bug1.cjs`, a mock route-ok pontosan az api-design.md §16.3 példáját
követik). Ugyanez determinisztikusan reprodukálható az e2e tesztből is:
`frontend/e2e/tool-calling/tool-calling-flow.spec.ts` `QA-E8-05` teszt —
lásd a hozzá tartozó `test-results/.../error-context.md`-t.

Screenshotok:
- `docs/bugs/BUG-1-before-click.png` — a javaslat-kártya "Jóváhagyás" előtt.
- `docs/bugs/BUG-1-after-click.png` — a kattintás után: a felhasználó a
  `/login` oldalon köt ki, és emellett egy MÁSODIK, kaszkádolt hibaüzenet is
  megjelenik ("Nem sikerült betölteni a bejelentkezési konfigurációt."), mert
  a login oldal egy nem-mockolt OAuth-konfigurációs végpontot próbál betölteni.

## Elvárt viselkedés

Az api-design.md §16.3.1 szerint a `tool-calls/confirm` 401-e a **proposal-token**
állapotára vonatkozik (lejárt ~10 perces HMAC-token vagy érvénytelen aláírás),
nem a felhasználó munkamenetére. Az AC ("hibaüzenet jelenik meg, nem crashel a
UI") alapján elvárt:
- A javaslat-kártya helyben marad, `toolCallStatus` visszaáll `pending`-re
  (ezt a `search.facade.ts` `confirmToolCall` catch ága már helyesen csinálja).
- Egyetlen, releváns hibaüzenet jelenik meg: "Nem sikerült végrehajtani a
  parancsot." (vagy specifikusabb: "A javaslat lejárt, kérdezz újra.").
- A felhasználó a `/search` oldalon marad, bejelentkezett állapotban, és
  megpróbálhatja újra a keresést vagy elutasíthatja a javaslatot.

## Kapott (tényleges) viselkedés

1. A `search.facade.ts` `confirmToolCall` helyesen elkapja a hibát, és
   megjeleníti a "Nem sikerült végrehajtani a parancsot." toastot, majd
   visszaállítja a kártya állapotát `pending`-re.
2. **DE** párhuzamosan a globális `httpErrorInterceptor`
   (`http-error.interceptor.ts` 33. sor) **minden** 401-et — a session-auth
   lejártától megkülönböztetés nélkül — a felhasználó munkamenetének
   lejárataként kezel, és azonnal `router.navigate(['/login'], ...)`-ot hív.
3. Ennek eredményeként a felhasználó a javaslat-kártya "pending" állapotba
   visszaállítását soha nem látja — a böngésző szinte azonnal átnavigál a
   `/login` oldalra, elveszítve a keresési előzményt/kontextust, miközben a
   session valójában érvényes maradt (csak a proposal-token járt le).
4. Bónuszként a `/login` oldal — mivel a mock e2e/manuális tesztkörnyezetben
   nincs mockolva a login-konfigurációs endpoint — egy MÁSODIK, teljesen
   független hibaüzenetet is felugrat ("Nem sikerült betölteni a
   bejelentkezési konfigurációt."), tovább rontva a UX-et és megzavarva a
   hibakeresést (ez önmagában jelzi, hogy ez a redirect nem várt/tesztelt
   útvonal volt fejlesztéskor).

## Javasolt irány (nem implementálva — backend/frontend fejlesztőnek)

A `httpErrorInterceptor`-nak meg kellene különböztetnie a
`tool-calls/confirm|reject` (és általában a domain-specifikus, nem
session-token 401-eket adó) végpontokat a session-auth 401-től — pl. a
backend adjon vissza egyedi `type`/`code`-ot a ProblemDetails-ben
(pl. `"tool-call-token-expired"`), és az interceptor csak akkor navigáljon
`/login`-ra, ha ez a kód HIÁNYZIK (azaz valódi session-401). Alternatíva:
a `tool-calls/*` endpointokat kizárni a globális 401→redirect logikából, és a
hívó oldalon (facade) kezeltetni a hibát — ami már most is megtörténik, csak
az interceptor "elkapja előle" a navigációt.

## Teszt, ami lebuktatta

`frontend/e2e/tool-calling/tool-calling-flow.spec.ts` → **QA-E8-05** teszt
piros; a `docs/bugs/BUG-1-after-click.png` screenshot és a fenti repro-lépések
mutatják a hibát. A többi 4 teszt (QA-E8-01…04) zöld.

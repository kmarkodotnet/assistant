# Sonnet-implementációs csomag — használati útmutató

> Státusz: IMPLEMENTÁCIÓS SPEC v1.0 · Dátum: 2026-07-12
> Forrás: `docs/fable_review/` (Fable review) — Sonnet-implementátorra optimalizált átirat.
> Ez a mappa a VÉGREHAJTÁSI igazságforrás; az eredeti `docs/fable_review/` a háttér-elemzés.

## Hogyan használd (orchestrátornak)

- Minden doksi önálló, sorszámozott **feladatkártyákra** bontott munkacsomag.
- Egy feladatkártya = egy subagent-feladat = egy ág + egy(-két) commit.
- A subagentnek CSAK az adott kártyát + a kártya „Olvasd el először" listáját add át,
  soha ne a teljes doksit vagy repót.
- A kártyák közti függést a kártya fejléce jelzi (`Függ: …`). Független kártyák
  párhuzamosan futtathatók külön worktree-ben.

## Globális szabályok minden feladathoz (a subagent promptjába másolandó)

1. **Olvasás előbb:** a kártya „Olvasd el először" fájljait NYISD MEG és értsd meg,
   mielőtt bármit módosítasz. Ne találgass API-t vagy szignatúrát — nézd meg a kódban.
2. **Scope-fegyelem:** KIZÁRÓLAG a kártya „Lépések" szakaszában leírtakat valósítsd
   meg. A „Tilos / nem scope" szakaszban felsoroltakhoz ne nyúlj. Ha a feladat
   közben más hibát találsz, NE javítsd — jegyezd fel a zárójelentésbe.
3. **Döntések:** a kártyákban minden döntés előre meg van hozva („Döntés:" jelöléssel).
   Ne térj el tőle. Ha a döntés a valósággal ütközik (pl. a hivatkozott fájl nem
   létezik), ÁLLJ MEG és jelentsd — ne improvizálj.
4. **⛔ EMBERI KAPU jelölés:** ahol ez szerepel, ott a munka DRAFT-ig mehet
   (pl. ADR-szöveg megírása), de merge/aktiválás emberi jóváhagyás nélkül tilos.
5. **Kapuk a lezárás előtt (egyik sem átugorható):**
   - `dotnet build` 0 warning, `dotnet test` zöld (backendes változásnál),
   - `npm test` (vitest) zöld (frontendes változásnál),
   - a kártya saját „Ellenőrzés" parancsai zölden futnak,
   - conventional commit (`fix:`/`feat:`/`test:`/`docs:`/`chore:`).
6. **Új teszt kötelező** minden viselkedés-változáshoz; a kártya megnevezi, mit.
   Meglévő tesztet csak akkor módosíts, ha a kártya explicit előírja.
7. **Elakadás:** 2 sikertelen próbálkozás után állj meg, és írj rövid összefoglalót:
   mit próbáltál, mi a hibaüzenet, mi a hipotézised. Ne iterálj tovább vakon.
8. **Zárójelentés formátuma:** módosított fájlok listája · futtatott ellenőrzések
   és eredményük · elfogadási kritériumok kipipálva · talált, de nem javított
   problémák.

## Doksik és ajánlott végrehajtási sorrend

| Sorrend | Doksi | Tartalom | Jelleg |
|---|---|---|---|
| 1 | [01-code-review-teendok.md](01-code-review-teendok.md) | P1/P2 hibajavítások (T1–T9) | azonnal indítható |
| 2 | [03-doksik-vs-tesztek.md](03-doksik-vs-tesztek.md) | törött CI/teszt-kapuk (G1–G3), tesztadósság (TD1–TD6) | G-kártyák azonnal |
| 3 | [02-doksik-vs-kod.md](02-doksik-vs-kod.md) | doksi-szinkron (WP-A), kód-adósság (WP-B), döntött tételek (WP-C) | WP-A azonnal |
| 4 | [06-ai-minoseg-vegrehajtasi-terv.md](06-ai-minoseg-vegrehajtasi-terv.md) | AI-minőség F0–F8 fázisok | F0/F1/F2 azonnal |
| 5 | [05-ai-minoseg-javitasok.md](05-ai-minoseg-javitasok.md) | háttér-elemzés a 06-hoz | REFERENCIA, nem feladat |
| 6 | [07-llm-melyebb-integracio.md](07-llm-melyebb-integracio.md) | beszélgető/ügynöki LLM-hullámok | F1–F2 után |
| 7 | [04-cr-otletek.md](04-cr-otletek.md) | 8 CR-minispec | backlog-betöltés után |
| 8 | [08-android-port-terv.md](08-android-port-terv.md) | Android-port M0–M8 | M0 spike-ok után |
| 9 | [09-hordozhato-telepitokeszlet.md](09-hordozhato-telepitokeszlet.md) | telepítőkészlet L0–L8 | L0 után |
| — | [09-telepitheto-termek-terv.md](09-telepitheto-termek-terv.md) | ELAVULT csonk — átirányítás | ne implementáld |

## Miért ez a formátum (Sonnet-optimalizálás elvei)

- **Zárt döntések:** Sonnet akkor teljesít a legjobban, ha nem kap nyitott
  „vagy-vagy" kérdést — minden választás előre eldőlt, indoklással.
- **Kis, ellenőrizhető egységek:** minden kártya önmagában buildel/tesztel.
- **Explicit kontextus:** fájl+sor pontosságú hivatkozások, elvárt kód-irányok,
  „olvasd el először" lista — nincs implicit tudásfeltételezés.
- **Verifikáció beépítve:** minden kártya végén futtatható ellenőrzés, nem
  csak szöveges elvárás.
- **Guardrail-ek:** minden kártyán „Tilos" szakasz a scope-csúszás ellen.

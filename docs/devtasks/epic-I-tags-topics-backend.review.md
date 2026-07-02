# Review — epic-I-tags-topics-backend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Jó, kompakt bontás; a Topic-hierarchia invariánsai (mélység-korlát,
önreferencia- és kör-védelem, T-IBE-05) és a seed-viselkedés („nem írja
felül a manuális módosítást”, T-IBE-07) átgondolt részletek. A
„AI Tag-et hozhat létre, Topic-ot nem” elv következetes.

## Észrevételek

1. **T-IBE-01 eldönti a tag-normalizálást (pozitív, szinkron kell):**
   „Lowercase normalization a nevekre + UNIQUE” — ez lezárja a
   domain-model vs. séma kettősséget (domain-model.review.md #5) a
   lowercase-tárolás javára; a database-schema.md CHECK-je (amely
   nagybetűt is enged) szigorítható, vagy legalább a séma-doksi
   megjegyzést kapjon.
2. **T-IBE-03 „behavior vagy DB trigger” (kicsi):** a usage_count
   karbantartása két úton van felajánlva — döntés kell (a MediatR-
   behavior a konzisztensebb a többi mintával; DB-trigger viszont a
   cascade-delete-eknél is működik, amit a behavior nem lát). A napi
   recompute-job „opcionális” helyett legyen kötelező sanity-check.
3. **Ütemezés-függőség (közepes):** az Epic D (Fázis 8) T-DBE-14-e már
   létrehozza a Tag/Topic entitásokat, és a T-DBE-15 osztályozó ír
   beléjük — ez az epic (Fázis 11) „már létezhet; finomítás”
   megjegyzése jó, de a CRUD-endpointok nélkül a Fázis 8–10 között a
   felhasználó nem tud tag-et kezelni. Megfontolandó a T-IBE-01/04
   előrehozása a Fázis 8 környékére (a devtasks README sorrend-ábrája
   ezt most nem tükrözi).

## Verdikt

Végrehajtásra kész; a #3 ütemezési kérdést az orchestrátornak érdemes
mérlegelnie.

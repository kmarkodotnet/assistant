# Review — ADR-0001 (pgvector)

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Jó ADR: valós alternatívák (Qdrant/Weaviate/Milvus), a döntés a
single-tenant méretezéshez igazított, az indoklás (egy backup, egy
tranzakció, EF Core támogatás) helytálló. A döntés a többi doksiban
konzisztensen végig van vezetve (séma, keresés, architektúra).

## Észrevételek

1. **`IVectorStore` absztrakció** — a Következmények szakasz ezt írja
   elő, de az architecture.md 4. szakaszában nincs ilyen port; ott a
   `ISemanticSearchService` és `IEmbedder` fedi a szerepet. Vagy az ADR
   szóhasználata frissítendő a tényleges interfész-nevekre, vagy az
   architektúrában kell IVectorStore. (Tartalmilag a szándék teljesül.)
2. **Modellváltás következménye** — itt: „modellváltás új tábla/oszlop
   migrációt igényel”; a database-schema.md 7. pontosabb: csak
   dimenzió-változásnál kell séma-migráció, egyébként elég az
   `embedding_model` oszlop + újragenerálás. Az ADR-be érdemes ezt a
   finomítást átvenni.
3. Elgépelés: „embeding” (2×).

## Verdikt

Elfogadva státusz indokolt; a szóhasználat-szinkron kozmetika.

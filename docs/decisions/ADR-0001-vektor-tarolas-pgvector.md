# ADR-0001 — Vektor-tárolás: pgvector

- Státusz: Elfogadva
- Dátum: 2026-06-26
- Döntéshozó: kmarko.net@gmail.com

## Kontextus
A Family OS hibrid keresést használ: strukturált szűrés + full-text + szemantikus
(vektor) keresés. Dönteni kellett, hogy a vektor-embedinget a meglévő PostgreSQL-en
tároljuk (pgvector kiterjesztés), vagy külön dedikált vektor-adatbázist vezessünk be
(pl. Qdrant, Weaviate, Milvus).

## Döntés
A vektorok a meglévő PostgreSQL-ben, **pgvector** kiterjesztéssel tárolódnak.
Külön vektor-DB-t nem vezetünk be az MVP-ben.

## Indoklás
- Egyetlen család, várhatóan tízezres nagyságrendű chunk-számig — ez bőven
  a pgvector skálázhatósági tartományán belül van.
- Egyetlen adatbázis = egy backup, egy restore, egy hozzáférés-szabályozás,
  egy Docker Compose service-szel kevesebb.
- A dokumentumok és a hozzájuk tartozó embedingek így ugyanabban a tranzakcióban
  írhatók, nincs konzisztencia-kockázat két rendszer között.
- EF Core támogatás `Pgvector.EntityFrameworkCore` csomaggal stabil.
- HNSW index támogatott — a várható query-mintákhoz elegendő.

## Következmények
- `IVectorStore` absztrakció a backendben, hogy egy későbbi migráció dedikált
  vektor-DB-re (ha kinövi) ne legyen kódbázis-szintű átírás.
- A Postgres image-nek `pgvector` kiterjesztést tartalmaznia kell (pl.
  `pgvector/pgvector:pg16` image vagy saját Dockerfile).
- Az embedding dimenzió (a választott modelltől függ) a séma része lesz —
  modellváltás új tábla/oszlop migrációt igényel.

# Review — epic-C-documents-backend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Erős, teljes lefedettségű bontás (19 task): storage-absztrakció,
magic-byte MIME, dedup, idempotency, teljes CRUD + facet + linking,
biztonsági tesztekkel (path traversal). Fontos: a C4/C5 story-k — amelyek
az implementation-plan/mvp-backlog fázis-tábláiból kimaradtak
(implementation-plan.review.md #2) — **itt be vannak ütemezve a Fázis 5-be**;
a tervező doksik ehhez igazítandók.

## Észrevételek

1. **T-CBE-06 idempotency-tár memóriában (közepes):** az MVP in-memory
   store restartnál elveszti a kulcsokat — épp a projekt „PC nem mindig
   fent” alapfeltevése mellett. A 24 órás TTL-ígéret (api-design 1.9) így
   nem tartható; vagy Postgres-tábla már MVP-ben, vagy az api-design
   szövege enyhítendő („best effort, restartig”).
2. **T-CBE-15 hard delete vs. DB-jogosultság (közepes):** a hard delete
   fizikai DELETE-et igényel, de a `family_app` role-nak a séma 1.4
   szerint nincs DELETE joga (database-schema.review.md #3). A task nem
   tér ki rá — a megoldás (grant szűkített táblákra vagy SECURITY DEFINER
   fn) az architecté.
3. **T-CBE-13 — reprocess-scope döntés dokumentálva (pozitív):** a
   szövegkorrekció után csak `Embed` + `Summarize` fut újra — ez lezárja
   az api-design.review.md #5 kérdését, de az ai-pipeline.md-ben is
   rögzítendő (és megfontolandó, hogy a determinisztikusan a szövegből
   származó Deadline-kinyerés kimaradása tudatos-e).
4. **T-CBE-07 storage-rollback:** jó, hogy a hibaági fájltörlés AC-ben
   van; a fordított sorrend (DB-commit után fájl-írás bukik) esetét is
   érdemes egy mondattal lefedni (a tranzakció a storage-ot nem fedi).
5. Apróság: T-CBE-10 „Audit log: FileAccess opcionálisan… MVP-ben csak a
   download-on” — konzisztens a security-privacy 5.2-vel, jó szűkítés.

## Verdikt

Végrehajtásra kész; az #1–#2 infrastruktúra-döntések az architect
asztalán, a #3 doksi-szinkron a BUILD alatt pótlandó.

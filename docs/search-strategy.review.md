# Review — search-strategy.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Jó felépítésű keresési terv: három retrieval-réteg + RRF fúzió + szabály-
alapú intent-router + hivatkozott Q&A. A „strukturált adat erősebb a
vektornál” és a „citedSources csak retrievalből” elvek helyesek. Néhány
SQL-minta hibás, és két belső ellentmondás javítandó.

## Hibák / következetlenségek

### 1. A 2.3 vektor-SQL szintaktikailag hibás (közepes)
```sql
FROM    app.document_chunk dc, q
JOIN    app.document d ON d.id = dc.document_id
```
A vessző-join és az explicit `JOIN` keverése miatt a `JOIN ... ON`
feltétele a `q`-hoz kötődik, és a `dc`-re hivatkozás az `ON`-ban
Postgres-hiba („invalid reference to FROM-clause entry”). Helyesen:
```sql
FROM app.document_chunk dc
JOIN app.document d ON d.id = dc.document_id
CROSS JOIN q
```
Mintakódként is javítandó, mert az implementáló ebből dolgozik.

### 2. Slot-kinyerés sorrendi ellentmondás (közepes)
A 4.1 folyamat 2. lépése szerint a slot-extraction a **retrieval előtt**
fut (kell a szűréshez: dátumtartomány, családtag) — az 5.3 viszont azt
mondja, a slotokat „ugyanaz a hívás adja vissza, mint a Q&A LLM prompt”,
ami a retrieval **után** fut. A kettő egyszerre nem igaz. Valószínű
szándék: szabály-alapú slot-kinyerés előre + az LLM-válaszban visszaadott
finomított slotok a UI-chipekhez. Ezt így kell leírni.

### 3. Child-szerepkör láthatósága ellentmond a víziónak (közepes)
6. szakasz: a Child „az `IsPrivate = false` rekordokat és a saját
family_member-hez kapcsolt rekordokat látja”. A product-vision.md
szerepkör-táblája szerint viszont a child **csak az explicit számára
megosztott** rekordokat látja. A kettő nagyon különböző adatvédelmi
politika (opt-out vs. opt-in). A security-privacy.md-vel együtt egyetlen
mátrixban kell rögzíteni, és a többi doksi csak hivatkozzon rá.

### 4. Intent classifier: az NTextCat nem erre való (kicsi)
4.1/1. lépés: „Intent classification (lokálisan, NTextCat + szabályok)” —
az NTextCat **nyelvdetektáló** könyvtár, nem intent-osztályozó. A leírt
megoldás valójában tisztán szabály-alapú (5.1) — a könyvtár-hivatkozás
törlendő vagy cserélendő. Az 5.2 „konfidencia” fogalma is definiálatlan
szabály-alapú rendszerben (hány szabály tüzelt? súlyozás?).

### 5. Kisebb észrevételek
- 3.1 pszeudokód: a `candidate_ids` komment („ha túl nagy (>10k), csak
  akkor szűrjük”) logikailag fordítva hangzik — feltehetően: ha a
  strukturált pre-filter *nem szűkít érdemben*, kihagyjuk. Fogalmazandó.
- 3.1: `scores[hit.id] +=` inicializálás nélkül — pszeudokódban oké, de
  jelezni, hogy default 0.
- 6. kód-komment: a „spouse-share” megjegyzés a `!IsPrivate` ágon van,
  pedig a nem-privát rekord amúgy is látható; a szándék (partner láthatja
  a családtag *privát* rekordjait, pl. MedicalRecord) a rossz helyen
  szerepel.
- 2.2: a document-FTS a `document_text.tsv`-n fut — a `document.title`
  nincs a tsv-ben (külön trgm-index van rá), a title-találatot csak az
  exact-match ág fedi; érdemes jelezni, hogy ez tudatos.
- 7.1: a strukturált ág `Deadline`-on keres — csak *jóváhagyott/rögzített*
  deadline-nál ad találatot; friss, még nem jóváhagyott javaslatnál a
  hibrid ágra esik. Egy mondat a viselkedésről hasznos lenne.
- 9.1 teljesítmény-számok reálisak; a Q&A 1–3 s Ollama-val optimista
  hidegindításnál (modell-betöltés perc nagyságrend) — SLA-ba „meleg
  modell” feltétel írandó.
- A `topics: ["penzugy/szamla"]` path-formátum ugyanaz a slug-vs-path
  kérdés, mint az ai-pipeline.review.md #4.

## Erősségek (megőrzendő)

- RRF a súlyhangolós fúzió helyett — pragmatikus, jó döntés.
- „filter módban LLM NEM hívódik” (4.1/4.) — költség és megbízhatóság.
- RBAC WHERE-szinten, nem post-filter (6.) — a szivárgás-érv helyes.
- Válasz-validáció (4.1/5.): a nem-forrásolt tény visszaesik „nincs
  adat”-ra — a hallucináció-védelem konkrét.

## Verdikt

Jó terv, a #1 SQL-minta és a #2 sorrendi ellentmondás javításával, plusz
a #3 láthatósági politika egységesítésével kész a kontrakt-szerepre.

# ADR-0007 — Child szerepkör: csak olvasás, hozzá kötött nem-privát rekordok

- Státusz: Elfogadva
- Dátum: 2026-07-02
- Döntéshozó: kmarko.net@gmail.com (dokumentum-audit nyomán rögzítve)

## Kontextus

Három dokumentum három eltérő child-politikát írt le:
- `product-vision.md`: „csak olvasás a számára **explicit megosztott**
  rekordokon" (opt-in).
- `search-strategy.md` 6.: child lát minden `IsPrivate = false` rekordot
  + a hozzá kötötteket (opt-out).
- `security-privacy.md` 4.1: child a *saját* rekordjait **írhatja** is.

Az `idea.md` brief eredeti szándéka egyértelmű: „child/read-only".

## Döntés

A **Child** szerepkör az MVP-ben:

1. **Csak olvasás.** Child semmilyen entitást nem hozhat létre, nem
   módosíthat és nem törölhet (tag-létrehozás sem).
2. **Láthatóság:** kizárólag azok a rekordok, amelyek
   `related_family_member_id` / `family_member_id` mezője a **saját**
   `FamilyMember`-ére mutat ÉS `IsPrivate = false`.
   A rekord child-hoz *kötése* (linkelés) az explicit megosztási gesztus —
   ezt Adult/Admin teszi meg.
3. **MedicalRecord:** child a saját, hozzá kötött rekordjait láthatja
   (IsPrivate=true default ellenére), mert ő az adatalany — de a
   partneri/adminisztrátori láthatósági szabályok nem vonatkoznak rá
   mások rekordjaira.
4. **UI:** a navigáció a Dashboard / Documents (read-only) / Notes
   (read-only) / Reminders / Search / Settings(saját) elemeket mutatja;
   szerkesztő gombok és a Suggestions inbox rejtve.

## Indoklás

- A brief „read-only" kikötése a felhasználó explicit szándéka.
- A „linkelés = megosztás" értelmezés implementálhatóvá teszi az
  „explicit megosztott" vision-elvet külön sharing-mechanizmus (új tábla,
  új UI) nélkül — MVP-kompatibilis.
- Az opt-out változat (minden nem-privát látható) gyerekeknél
  adatvédelmileg kockázatos default lett volna (pl. szülők pénzügyi
  dokumentumai).

## Következmények

- `security-privacy.md` 4.1 mátrix a normatív forrás; a 4.1 child-oszlopa
  és 4.3 szabályai e döntés szerint frissítve; `product-vision.md` és
  `search-strategy.md` 6. hivatkozik rá.
- `T-BBE-01` (row-level auth service) és `SearchAuthorizationService`
  e szabályt implementálja; a child-írási útvonalak endpoint-szinten is
  tiltottak (`RequireAdult` a írási műveleteken).
- Jövőbeli finomítás (v2): explicit per-rekord megosztási lista, ha a
  „linkelés = megosztás" kevésnek bizonyul.

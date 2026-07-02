# Review — reminder-engine.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Átgondolt motor-terv: catch-up-first elv, állapotgép, eszkalációs policy,
quiet hours, idempotencia-fejezet, idő-szimulációs tesztterv. A
„tüzelés = mindig új sor” elv jól auditálható rendszert ad. Több konkrét
logikai hiba és séma-eltérés javítandó.

## Hibák / következetlenségek

### 1. Jóváhagyatlan reminderek örökre a scan-ben ragadnak (súlyos)
A 6.1 dispatcher a `status = 'Scheduled' AND trigger_utc <= now()` sorokat
veszi fel `LIMIT 100`-zal, `ORDER BY trigger_utc`, és a jóvá nem hagyott
szülőjű remindereket **skip**-eli (nem tüzel, státusz marad `Scheduled`).
Következmény: minden lejárt, de soha jóvá nem hagyott AI-javaslat reminder
percenként újra bekerül a scanbe — és mivel a rendezés `trigger_utc ASC`,
**a legrégebbi 100 stale sor kitöltheti a batch-et, és a friss, valóban
tüzelendő reminderek soha nem kerülnek sorra**. Megoldás kell: pl. a skip
állítsa a reminder-t `Skipped`/`Suspended`-re, vagy a scan zárja ki a
jóváhagyatlan szülőjű sorokat (join + WHERE), vagy a javaslat-reminderek
ne `Scheduled`, hanem külön státuszban szülessenek, és a jóváhagyás
aktiválja őket.

### 2. SQL-precedencia bug az eszkalációs queryben (közepes)
6.3:
```sql
WHERE e.task_id = r.task_id OR e.deadline_id = r.deadline_id
  AND e.escalation_level = r.escalation_level + 1
```
Az `AND` erősebben köt, mint az `OR` → a feltétel valójában
`task_id-egyezés VAGY (deadline-egyezés ÉS szint-egyezés)` — task-alapú
remindernél a szint-feltétel nem érvényesül. Zárójelezés kell:
`(e.task_id = r.task_id OR e.deadline_id = r.deadline_id) AND e.escalation_level = ...`.
(Plusz: NULL = NULL sosem igaz, ezért az XOR-os oldalak külön kezelendők —
`e.task_id IS NOT DISTINCT FROM r.task_id` vagy explicit ágak.)

### 3. „InApp + Email” vs. egyértékű `channel` enum (közepes)
A 4.1 eszkalációs tábla és a 6.1 dispatch („InApp + opcionálisan Email”)
több csatornát ír egy reminderre — a séma `reminder.channel` egyetlen
enum. Vagy csatorna-flags, vagy csatornánként külön sor, vagy az InApp
mindig implicit és a `channel` csak a *plusz* csatornát jelöli. Döntés +
séma-igazítás kell (ai-pipeline.review.md #3 ugyanez).

### 4. A 7.2 „adatbázis védi a duplikációt” állítás nem igaz (közepes)
„Recurring rule alapú új Reminder generálás: PRIMARY KEY a
`(task_id OR deadline_id) + trigger_utc` unique” — a database-schema.md-ben
**nincs ilyen unique index** a reminder táblán (csak PK az `id`-n).
Vagy fel kell venni két partial unique indexet
(`(task_id, trigger_utc) WHERE task_id IS NOT NULL`, ill. deadline-ra
ugyanígy), vagy az állítást app-szintű védelemre átfogalmazni.
Megjegyzés: a snooze/eszkaláció szintén új sort hoz létre potenciálisan
azonos `(parent, trigger_utc)`-vel — a unique index ezekkel ütközhet,
gondold végig az `escalation_level`-t is a kulcsban.

### 5. Elavult kereszthivatkozások (kicsi)
- 3.1 megjegyzés: „a `Cancelled` érték a database-schema.md enumjából
  hiányzik” — a séma v0.2 már tartalmazza; a megjegyzés törlendő.
- 5.1.1: „Ezt a táblát hozzáadjuk a database-schema.md v0.2-höz” —
  megtörtént; frissítendő „hozzáadva” státuszra. A séma-verzió ráadásul
  bővebb (related_entity_type CHECK-ek, retention) — az itteni táblázat
  legyen csak hivatkozás, ne duplikált definíció.

### 6. Catch-up ablak: 14 nap vs. 7 nap (kicsi)
Itt 14 nap (konfigurálható, 6.2) — az architecture.md 6.4 pontja 7 napot
ír. Egységesítendő (a konfigurálható 14 napos itteni változat a jobb).

### 7. Kisebb észrevételek
- 2.2 vs. 6.4: a „sosem update, mindig új sor” elv (2.2, 3.2 utolsó sora)
  és a reschedule/quiet-hours `TriggerUtc`-átírás (5.4, 6.4) feszültségben
  van — jelezni, hogy a szabály csak a *tüzelési* eseményekre vonatkozik.
- 6.1: „check current user quiet-hours” — pontosítandó: a *címzett*
  (responsible → UserAccount, ill. mindkét szülő) quiet-hours beállítása,
  nem a „current user”-é (worker-kontextusban nincs current user).
- 8.3/5. lépés: az 1-napos reminder „automatikusan Cancelled lesz, mert a
  Deadline Resolved” — de a példában a user csak a *reminder*-t nyugtázta;
  semmi nem állította a Deadline-t `Resolved`-ra. Vagy a flow-ba kell egy
  „Deadline lezárása” lépés, vagy a Cancelled-lánc indoklása pontosítandó.
- 2.2 recurring + hosszú offline: a catch-up a lejárt occurrence-t tüzeli
  és a *következőt* generálja — több kimaradt ciklusnál (2 hónap offline,
  havi szabály) definiálandó, hogy a köztes occurrence-ek kimaradnak-e
  (javasolt: igen, csak a legutolsó + következő).
- 5.4 preferenciák: „UserAccount-on bővítve, vagy külön táblában” —
  döntetlen; a séma egyiket sem tartalmazza (lásd
  database-schema.review.md #5). Döntés kell.

## Erősségek (megőrzendő)

- `SELECT FOR UPDATE SKIP LOCKED` + állapot-check a dupla tüzelés ellen.
- Eszkaláció = új sor magasabb szinten, nem in-place mutáció.
- 14 napos catch-up kapu + „lecsúszott” összesítő — jó zajszűrés.
- Idő-szimulációs tesztterv (11.3) — pontosan a kritikus forgatókönyvek.

## Verdikt

A motor koncepciója helyes, de az #1 (stale suggestion reminderek) valós
üzemi hibát okozna — a BUILD előtt kötelező javítás. A #2–#4 séma/SQL
szintű pontosítások.

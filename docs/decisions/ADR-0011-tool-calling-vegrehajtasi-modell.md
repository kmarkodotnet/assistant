# ADR-0011 — Természetes nyelvű parancsok (LLM tool-calling) végrehajtási modellje

- Státusz: Elfogadva (architect fázis, CR260710-07)
- Dátum: 2026-07-11
- Döntéshozó: architect agent (orchestrátor felügyelet alatt)
- Kapcsolódó: [CR260710-07](../change-requests/cr260710-07-termeszetes-nyelvu-parancsok.md),
  [ai_features.md §4.4](../ai_features.md), [ai-pipeline.md §11](../ai-pipeline.md),
  [api-design.md §16.3](../api-design.md), ADR-0009 (reminder-generálás)

## Kontextus

A CR260710-07 három whitelistelt tool-t vezet be (`create_reminder`,
`assign_document`, `add_tag`), amelyeket az LLM egy chat-üzenetből *javasol*,
és a felhasználó explicit megerősítése után a backend hajt végre. Négy
nyitott architektúra-kérdést kell eldönteni a párhuzamos fejlesztés előtt,
mert mindegyik érinthet migrációt, biztonságot vagy meglévő kontraktot:

1. **Hol tároljuk a megerősítésre váró javaslatot?** (proposal store)
2. **Warranty-gap:** a `create_reminder` XOR-t vár (`TaskId` VAGY
   `DeadlineId`), de a `Reminder` entitásnak nincs FK-ja `Warranty`-ra,
   miközben a minta-utasítás épp *"a garancia lejárta előtt"* szól.
3. **`add_tag` scope:** a `POST /documents/{id}/tags` jelenleg 501 stub
   (T-CBE-17), nincs mögötte command/handler.
4. **Auditálás:** hogyan teljesül a "ki, mikor, milyen tool-t, milyen
   paraméterekkel" AC a meglévő `AuditBehavior` mellett.

## Döntés

### D1 — Állapotmentes, aláírt javaslat-token (nincs proposal-tábla, nincs migráció)

A megerősítésre váró tool-javaslatot **nem** perzisztáljuk adatbázisban.
A `POST /api/v1/search` (Command mód) a feloldott (resolved) javaslatot egy
rövid életű, **HMAC-aláírt, base64url borítékként** (`proposalToken`) adja
vissza a kliensnek. A boríték tartalma:

```
{ "v":1, "tool":"create_reminder", "args": { ...resolved... },
  "uid":"<userAccountId>", "iat": <utc>, "exp": <iat+600s>, "sig":"<hmac>" }
```

A megerősítő endpoint a tokent visszakapja, ellenőrzi az aláírást, a lejáratot
(`exp`, alap 10 perc), és hogy a `uid` == az aktuális bejelentkezett user.
Csak ezután hajt végre. **Indok:** MVP-ben (prioritás C) elkerüli az új
táblát + migrációt, nincs "árva javaslat" GC-igény, és a tokent nem lehet
más userre lejátszani. A titkos kulcs env-változóból jön
(`TOOLCALL_SIGNING_KEY`), soha nem kerül repóba.

### D2 — Warranty-reminder: szintetizált AiSuggested Deadline (NINCS séma-változás)

A `Reminder` séma **változatlan** marad (XOR Task/Deadline). Ha a parancs egy
garancia lejáratára hivatkozik (nincs hozzá Deadline), a `create_reminder`
tool **kétlépéses, láncolt** végrehajtást javasol egyetlen megerősítés mögött:

1. `CreateDeadlineCommand` — a `Warranty.WarrantyEndDate` → `DueDateUtc`,
   `Category = Other` (nincs dedikált Warranty kategória),
   `Description` = "Garancia lejárat: {ProductName}", a warranty
   `SourceDocumentId`/`RelatedFamilyMemberId` átörökítve. `Origin` a
   command-handler szerint `Manual` — de a UI-summary jelzi, hogy AI-javasolt.
2. `CreateReminderCommand(DeadlineId = <új deadline>, TriggerUtc =
   DueDateUtc - offsetDays)`.

Mindkét command az `ISender.Send`-en megy át, így az `AuditBehavior`
mindkettőt automatikusan naplózza. **Indok:** hű az ADR-0009 elvhez
("javaslatból jóváhagyás nélkül nincs ütemezett művelet") — a Deadline is
csak a felhasználó megerősítése után jön létre; nincs migráció; a `Reminder`
egyértékű anchor-modellje sértetlen. **Elvetett alternatíva:** `WarrantyId`
FK a `Reminder`-re — migrációt és XOR→3-utas validáció-átírást igényelne,
aránytalan egy C-prioritású feature-höz.

### D3 — `add_tag` scope-ba kerül: minimális `AddDocumentTagCommand`

Az `add_tag` **marad** a whitelistben. Bevezetünk egy minimális
`AddDocumentTagCommand(Guid DocumentId, Guid TagId)`-t, ami 1:1 az
`AddDocumentTopicCommand` mintáját követi (auth `CanWriteDocument`, tag-lét
ellenőrzés, dedup, `DocumentTag { Origin = Manual, IsApproved = true }`).
Ez egyúttal lezárja a T-CBE-17 501-stubot (`POST /documents/{id}/tags`).
A tool csak **létező** tag-re hivatkozhat: a resolve lépés a `tagName`-et
case-insensitive feloldja `Tag.Id`-re; ha nincs találat → a javaslat
figyelmeztetést kap és nem megerősíthető (MVP-ben az LLM nem hoz létre új
tag-et). **Indok:** kicsi, jól határolt kiegészítés, valódi terméki értéket
ad (stub megszűnik), és nem tágítja az LLM írási felületét.

### D4 — Kétszintű audit: dedikált tool-nyom + entitás-nyom

A megerősítő command (`ConfirmToolCallCommand`) `[NoAudit]` attribútumot kap,
és a handler **explicit** `IAuditLogger.LogAsync(AuditAction.Approve, ...)`
bejegyzést ír `entityType = "ToolCall:<toolName>"`, `detailsJson =
{ toolName, resolvedArgs }` tartalommal — ez a "milyen tool-t, milyen
paraméterekkel" nyom. A tényleges hatást a láncolt üzleti command-ok
(`CreateReminderCommand` stb.) `AuditBehavior`-a naplózza (`Create`/`Update`,
entitás-ID-vel). **Indok:** a két nyom együtt fedi le a teljes AC-t, és nem
kell módosítani az `AuditBehavior` névkonvenció-logikáját.

## Következmények

- **Új Application-absztrakció:** `ITool`, `IToolRegistry`,
  `ToolExecutionContext`, `ToolResolution`, `ToolResult` (kontrakt:
  ai-pipeline.md §11.1).
- **Nincs DB-migráció** ehhez a feature-höz (D1 + D2 következménye).
- `AddDocumentTagCommand` + handler + a `POST /documents/{id}/tags` valódi
  implementáció (a 501 stub helyett).
- `SearchResponse` bővül egy opcionális `toolCallProposal` mezővel; új
  `SearchMode.Command`; új `POST /api/v1/tool-calls/confirm` és
  `/tool-calls/reject` endpoint (api-design.md §16.3).
- Env: `TOOLCALL_SIGNING_KEY` (kötelező, ha a Command mód engedélyezett),
  `TOOLCALL_PROPOSAL_TTL_SECONDS` (alap 600).
- Rate limit: a Command mód a Q&A-val közös 10 req/perc/user keret alatt.
- Elutasításkor semmilyen adatváltozás; a `reject` endpoint csak egy
  `AuditAction.Reject` nyomot ír (a §4.5 feedback-tanuláshoz később hasznos).

---

## Kiegészítés — 2026-07-12 (CR260710-07 utókövetés)

- Státusz: Elfogadva (architect fázis)
- Kiváltó ok: az E8 `create_reminder` tool nem tud kezelni horgony nélküli
  parancsot (pl. *"hozz létre emlékeztetőt holnapra"*), mert nincs benne
  feladat/határidő/termék megnevezve, a `Reminder` séma pedig DB-szinten
  kikényszeríti a Task↔Deadline XOR-t.
- Ez a szakasz a meglévő D1–D4 döntéseket **NEM** módosítja; egy új döntési
  pontot (D5) ad hozzá.

### D5 — Horgony nélküli (standalone) emlékeztető: az XOR-constraint lazítása

**Döntés.** Bevezetjük a horgony nélküli emlékeztetőt, ahol `task_id` ÉS
`deadline_id` egyaránt `NULL`, a `trigger_utc` pedig közvetlenül a
felhasználó által megadott időpont. **Nem** kerül új oszlop a `reminder`
táblába — a meglévő `trigger_utc` mező teljesen elég a szabad dátumú esethez.
Az egyetlen sémaszintű változás a `chk_reminder_xor` CHECK-constraint
lazítása 2-utas XOR-ról **"legfeljebb egy horgony"** szabályra:

```
-- régi (elvetett): pontosan egy horgony
(task_id IS NOT NULL AND deadline_id IS NULL) OR
(task_id IS NULL AND deadline_id IS NOT NULL)

-- új: legfeljebb egy horgony (mindkettő NULL megengedett, mindkettő kitöltve tilos)
NOT (task_id IS NOT NULL AND deadline_id IS NOT NULL)
```

A constraint-et megtartjuk (drop + recreate ugyanazon a néven,
`chk_reminder_xor`), hogy a "mindkét horgony egyszerre" adathiba továbbra is
DB-szinten lehetetlen legyen — csak a "se-se" ág nyílik meg.

**Doménmodell.** A `Reminder` entitás kap egy harmadik factory-t,
`ForStandalone(targetUserAccountId, triggerUtc, channel, createdBy, rrule?)`,
ami mindkét horgony-ID-t `null`-ra hagyja. A meglévő `ForTask`/`ForDeadline`
változatlan. Az entitás XOR-t magyarázó kommentje frissül ("legfeljebb egy").

**Validáció.** A `CreateReminderCommandValidator` XOR-szabálya
(`RuleFor(x => x)…Must`) helyére a gyengébb "legfeljebb egy" invariáns kerül:
`!(x.TaskId.HasValue && x.DeadlineId.HasValue)` — mindkettő null immár valid.
A handler `if TaskId → ForTask / else if DeadlineId → ForDeadline / else →
ForStandalone` ágra bővül (a jelenlegi `else` implicit deadline-ág helyett).

**Tool-kontrakt (`create_reminder`).** Új `anchorType` érték: `"none"`.
Ekkor az `anchorRef` és `offsetDays` **nem** kötelező; helyette egy explicit
`"dueDate"` (ISO `yyyy-MM-dd`, kötelező) és opcionális `"dueTime"`
(`HH:mm`, alap `09:00`) mező adja meg a trigger időpontját, a felhasználó
időzónájában (`ToolExecutionContext.TimeZoneId`), majd UTC-re konvertálva —
ugyanaz a `ToUtc0900`-mintájú normalizálás, mint a warranty-ágban, csak a
9:00 helyett a `dueTime` paraméterrel. A séma feltételes: az `if/then`
ág (JSON Schema draft 2020-12 `allOf`) az `anchorType` szerint kényszeríti
ki a megfelelő kötelező mezőket, hogy a `null` és a vegyes kombináció
strukturálisan is kizárt legyen.

**System prompt.** Az LLM-nek egyértelmű instrukció kell, hogy a relatív
kifejezéseket (*"holnap"*, *"jövő hétfőn"*, *"3 nap múlva"*) **abszolút**
`dueDate`-té alakítsa a `ToolExecutionContext.NowUtc` + `TimeZoneId`
alapján, mielőtt a `"none"` ágat választja. Ha egy parancs egyértelműen
horgonyra utal (feladat/határidő/termék neve elhangzik), a `"none"` ág
**nem** választható — az elsőbbség a konkrét horgonyé.

**Végrehajtás (`ExecuteAsync`).** Az `anchorType == "none"` ág a
`CreateReminderCommand(TaskId: null, DeadlineId: null, TriggerUtc: <dueDate+
dueTime UTC-re>, …)`-t küldi. Nincs láncolt Deadline (ellentétben a
warranty-ággal), nincs resolve-időbeli re-check horgonyra (nincs mit
ellenőrizni); a `targetUserAccountId`/`createdByUserId` továbbra is
`ctx.UserAccountId`.

**Indoklás.** Ez a lehető legkisebb, visszafelé kompatibilis változás: nincs
új oszlop, nincs adatmigráció a meglévő sorokon (mind horgonyzott marad, a
lazább constraint-et továbbra is kielégítik), a warranty-modell (D2) és a
proposal-token (D1) érintetlen. **Elvetett alternatíva:** külön
`standalone_reminder` tábla vagy diszkriminátor-oszlop (`anchor_type`) —
felesleges, mert a két nullable FK + `trigger_utc` már pontosan kifejezi az
állapotteret; a diszkriminátor redundáns lenne a FK-k nullságával.

**Következmények.**
- **1 DB-migráció** (drop + recreate `chk_reminder_xor`) — ez az egyetlen
  sémaszintű változás; a `/operate` autonóm sávban **emberi jóváhagyást**
  igényel (DB-migráció + API-kontrakt-változás kapu, lásd CLAUDE.md 2-es szint).
- A `ReminderConfiguration` `HasCheckConstraint` kifejezése frissül a
  migrációval szinkronban (különben a `dotnet ef` model-diff eltérést jelez).
- A frontend `reminders.page.ts` jelenleg **csak megjelenít** (nincs kézi
  create-form), a fejléc-címke `r.taskId ? 'Feladat…' : 'Határidő…'` bináris.
  Standalone esetben mindkét ID null → a címke harmadik ágat kap
  (pl. *"Emlékeztető"*). Ez a FE-oldal egyetlen érintett pontja.
- A `down` migráció visszaállítja a szigorú XOR-t; ha addigra létezik
  standalone sor, a `down` elhasal — ez elfogadott (a szigorítás nem
  automatikusan biztonságos, dokumentált korlát).

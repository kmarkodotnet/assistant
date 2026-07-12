# Implementációs jegyzék — Horgony nélküli (standalone) emlékeztető

> Forrás-döntés: [ADR-0011 D5](ADR-0011-tool-calling-vegrehajtasi-modell.md#d5--horgony-nélküli-standalone-emlékeztető-az-xor-constraint-lazítása).
> Ez a jegyzék önmagában elég a `db-engineer` és `backend-dev` agenteknek —
> nem kell más kontextus. Kód-implementáció NEM ennek a dokumentumnak a tárgya;
> ez a pontos, soronkénti változtatás-terv.
>
> **Séma-scope:** 1 db migráció (constraint-csere), új oszlop NINCS.
> **Kockázati kapu:** DB-migráció → emberi jóváhagyás kell (CLAUDE.md 2-es szint).

## Feladatfelosztás

- **db-engineer:** (A) migráció, (B) `ReminderConfiguration` constraint.
- **backend-dev:** (C) domain factory, (D) validator, (E) handler, (F) tool
  séma + resolve + execute, (G) system prompt.
- **frontend-dev (kicsi):** (H) `reminders.page.ts` címke harmadik ág.

A (C)–(H) a (B) kontraktjától függ (constraint kifejezés), de a fájlok nem
ütköznek → párhuzamosíthatók, ha mindenki a D5 constraint-szöveget használja.

---

## (A) DB-migráció — db-engineer

**Új fájl:** `src/FamilyOs.Infrastructure/Persistence/Migrations/20260712000001_RelaxReminderAnchorConstraint.cs`

Kövesd a `20260711000001_AddEmailMessageImportance.cs` mintáját (nyers
`migrationBuilder.Sql`, `[DbContext]` + `[Migration]` attribútum, `partial
class : Migration`, `Up`/`Down`).

- `Up`:
  ```sql
  ALTER TABLE app.reminder DROP CONSTRAINT IF EXISTS chk_reminder_xor;
  ALTER TABLE app.reminder ADD CONSTRAINT chk_reminder_xor
      CHECK (NOT (task_id IS NOT NULL AND deadline_id IS NOT NULL));
  ```
- `Down` (dokumentált korlát: elhasal, ha időközben lett standalone sor):
  ```sql
  ALTER TABLE app.reminder DROP CONSTRAINT IF EXISTS chk_reminder_xor;
  ALTER TABLE app.reminder ADD CONSTRAINT chk_reminder_xor
      CHECK ((task_id IS NOT NULL AND deadline_id IS NULL)
          OR (task_id IS NULL AND deadline_id IS NOT NULL));
  ```
- A migráció után **frissítsd** a `FamilyOsDbContextModelSnapshot.cs` reminder
  check-constraint bejegyzését az új kifejezésre (a `dotnet ef migrations add`
  ezt automatikusan megteszi; ha kézzel írod a migrációt, kézzel is kell).

## (B) EF Core konfiguráció — db-engineer

**Fájl:** `src/FamilyOs.Infrastructure/Persistence/Configurations/ReminderConfiguration.cs`, sor 44-46.

Cseréld a `HasCheckConstraint` kifejezést (a név marad `chk_reminder_xor`):

- Régi: `"(task_id IS NOT NULL AND deadline_id IS NULL) OR (task_id IS NULL AND deadline_id IS NOT NULL)"`
- Új: `"NOT (task_id IS NOT NULL AND deadline_id IS NOT NULL)"`

Frissítsd a felette lévő kommentet `// DB-level XOR check` →
`// DB-level "at most one anchor" check (ADR-0011 D5)`.

---

## (C) Domain factory — backend-dev

**Fájl:** `src/FamilyOs.Domain/Entities/Reminder.cs`

- Sor 10 komment: `// XOR: either TaskId or DeadlineId…` → `// At most one
  anchor: TaskId, DeadlineId, or neither (standalone). Never both. (ADR-0011 D5)`.
- Új factory a `ForDeadline` (sor 81) után, azonos mintával, de horgony nélkül:
  ```
  public static Reminder ForStandalone(
      Guid targetUserAccountId, DateTime triggerUtc, NotificationChannel channel,
      Guid createdBy, string? rrule = null)
  ```
  Tartalma megegyezik `ForTask`-kal, kivéve: `TaskId = null`, `DeadlineId =
  null`, és nincs `Guid.Empty` guard (nincs horgony-arg). Minden más mező
  (`Status = Scheduled`, `EscalationLevel = 0`, timestampek) azonos.

## (D) Validator — backend-dev

**Fájl:** `src/FamilyOs.Application/Reminders/CreateReminderCommandValidator.cs`, sor 13-17.

Cseréld az XOR-szabályt "legfeljebb egy"-re:
- Régi `.Must(x => (x.TaskId.HasValue && !x.DeadlineId.HasValue) || (!x.TaskId.HasValue && x.DeadlineId.HasValue))`
- Új `.Must(x => !(x.TaskId.HasValue && x.DeadlineId.HasValue))`
- Üzenet: `"Egyszerre nem adható meg TaskId és DeadlineId; horgony nélküli
  emlékeztetőnél mindkettő elhagyható."`

A `TargetUserAccountId` NotEmpty és a `TriggerUtc` múlt-ellenőrzés marad.

## (E) Handler — backend-dev

**Fájl:** `src/FamilyOs.Application/Reminders/CreateReminderCommandHandler.cs`, sor 19-40.

Az `if TaskId → ForTask / else → ForDeadline` szerkezetet háromágúra bővítsd:
```
if (request.TaskId.HasValue)        reminder = Reminder.ForTask(...);
else if (request.DeadlineId.HasValue) reminder = Reminder.ForDeadline(...);
else                                 reminder = Reminder.ForStandalone(
                                         request.TargetUserAccountId, request.TriggerUtc,
                                         request.Channel, request.CreatedByUserId, request.RruleExpression);
```
`CreateReminderCommand` record (`CreateReminderCommand.cs`) **változatlan** —
a `TaskId`/`DeadlineId` már nullable, a standalone eset = mindkettő null.
`MapToDto` változatlan.

## (F) Tool — backend-dev

**Fájl:** `src/FamilyOs.Application/Ai/Tools/CreateReminderTool.cs`

1. **JSON schema (sor 29-46):**
   - `required` gyökér tömbből vedd ki az `anchorRef` és `offsetDays`
     mezőt, hagyd bent az `anchorType`-ot.
   - `anchorType.enum`: `["task", "deadline", "warranty", "none"]`.
   - Új property-k: `"dueDate": { "type": "string", "format": "date",
     "description": "ISO yyyy-MM-dd; abszolút dátum, a relatív kifejezéseket
     előbb alakítsd át." }` és `"dueTime": { "type": "string", "pattern":
     "^\\d{2}:\\d{2}$", "default": "09:00" }`.
   - Feltételes kötelezőség `allOf` + `if/then`:
     - ha `anchorType ∈ {task, deadline, warranty}` → `required: [anchorRef, offsetDays]`,
     - ha `anchorType == "none"` → `required: [dueDate]`.
2. **ResolveAsync (sor 48-67):** a property-olvasásnál `anchorRef`/`offsetDays`
   csak akkor kötelező, ha nem `"none"` (védekező `TryGetProperty`). Új switch-ág:
   `"none" => await ResolveStandaloneAsync(dueDate, dueTime, channel, recurrence, ctx, ct)`.
3. **Új privát metódus `ResolveStandaloneAsync`:** nincs DB-lookup, nincs
   `RefMatcher`. A `dueDate`+`dueTime`-ból számold a `triggerUtc`-t a
   `ToUtc0900` mintájára — általánosítsd egy `ToUtc(DateOnly, TimeOnly,
   timeZoneId)` helperre (a meglévő `ToUtc0900` hívja ezt `09:00`-val, hogy a
   warranty-ág ne törjön). `resolved` args: `anchorType="none"`, `triggerUtc`,
   `channel`, `recurrence`, `targetUserAccountId=ctx.UserAccountId`,
   `createdByUserId=ctx.UserAccountId` (taskId/deadlineId NINCS). `summary`:
   `"Emlékeztetőt hozok létre {FormatLocal(triggerUtc)}-ra."`; `display`:
   `[("Emlékeztető", FormatLocal(triggerUtc)), ("Csatorna", DisplayChannel(channel))]`.
4. **ExecuteAsync (sor 203-268):** a jelenlegi `if warranty / else if task /
   else (deadline)` láncot bővítsd `else if deadline`-ra, és adj egy `else`
   (== `"none"`) ágat, ami `taskId`/`deadlineId`-t null-on hagyja, semmilyen
   re-checket nem futtat. A közös `CreateReminderCommand`-küldés (sor 256-265)
   változatlanul jó (mindkét ID null-lal is működik).

## (G) System prompt — backend-dev

**Fájl:** a tool-calling prompt-építő (keresd: az a hely, ahol a
`create_reminder` tool leírása/instrukciója a rendszerüzenetbe kerül —
ai-pipeline.md §11.3; tipikusan egy `ToolCallPlanner`/prompt-builder osztály).

Told hozzá: *"Ha az utasítás nem nevez meg feladatot, határidőt vagy terméket,
használd az `anchorType: \"none\"` ágat, és a relatív időt (holnap, jövő
hétfőn, 3 nap múlva) számold ki a megadott `NowUtc`+`TimeZoneId` alapján
abszolút `dueDate` (yyyy-MM-dd) formára. Ha konkrét horgony elhangzik, azt
részesítsd előnyben a `none` helyett."* A prompt kapja meg a `NowUtc`-t és a
`TimeZoneId`-t (a `ToolExecutionContext`-ből már elérhető).

## (H) Frontend címke — frontend-dev (kicsi)

**Fájl:** `frontend/src/app/features/reminders/reminders.page.ts`, sor 112-114.

A `{{ r.taskId ? 'Feladat emlékeztető' : 'Határidő emlékeztető' }}` bináris
kifejezés harmadik ágat kap:
`r.taskId ? 'Feladat emlékeztető' : (r.deadlineId ? 'Határidő emlékeztető' : 'Emlékeztető')`.
Nincs create-form-változás (a page jelenleg csak megjelenít). A `ReminderDto`
(`reminders.api.ts`) már hordozza a nullable `taskId`/`deadlineId`-t.

---

## Minőségi kapuk erre a változásra

- `dotnet build` + `dotnet test` zöld (a meglévő reminder-tesztek + új
  standalone happy-path teszt a handler/tool szintjén).
- `dotnet ef migrations` model-diff üres (a snapshot és a
  `ReminderConfiguration` constraint-je egyezik a migrációval).
- E2E-hez nem kötelező új flow (E8 [C] opcionális), de a NL-tool unit-tesztje
  fedje a `"holnap"` → abszolút `dueDate` → standalone reminder útvonalat.

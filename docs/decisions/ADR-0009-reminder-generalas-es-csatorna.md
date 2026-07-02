# ADR-0009 — Reminder-generálás jóváhagyáskor; egycsatornás reminder

- Státusz: Elfogadva (a megvalósult kód rögzítése)
- Dátum: 2026-07-02
- Döntéshozó: kmarko.net@gmail.com (dokumentum-audit nyomán rögzítve)

## Kontextus

Két összefüggő ellentmondás volt a tervekben:

1. **Mikor születik a default reminder?** Az `ai-pipeline.md` 3.10 és a
   `reminder-engine.md` 8.1 szerint már a Deadline-*javaslat*
   létrehozásakor (`Origin=AiSuggested`, `Status=Scheduled`), és a
   dispatcher tüzeléskor ellenőrzi a szülő jóváhagyottságát. Ez a minta
   hibás: a lejárt, soha jóvá nem hagyott javaslat-reminderek örökre a
   dispatcher-scan elejére ragadnak (`ORDER BY trigger_utc LIMIT 100`),
   és kiszoríthatják a valódi remindereket.
2. **Csatorna.** A policy-táblák „InApp + Email" csatornát írtak, de a
   `reminder.channel` egyértékű enum (`InApp` | `Email`).

A megvalósult kód (`ApproveDeadlineCommandHandler`,
`DefaultReminderPolicy`) mindkettőt eldöntötte.

## Döntés

1. **Reminder csak jóváhagyáskor jön létre.** Az AI-pipeline Deadline-
   *javaslatot* hoz létre reminder nélkül; a `POST /deadlines/{id}/approve`
   generálja a default remindereket a `DefaultReminderPolicy` szerint
   (csak jövőbeli trigger-időpontokra). Így jóváhagyatlan szülőjű
   `Scheduled` reminder nem létezik; a dispatcher szülő-ellenőrzése
   defenzív guard marad.
2. **Egy reminder = egy csatorna.** A default reminderek `InApp`
   csatornával születnek. Email-értesítés két úton lehetséges:
   explicit `channel = Email` reminder (user hozza létre), vagy az
   **eszkaláció**, amely új, `Email` csatornás reminder-sort hoz létre
   a policy szerint.
3. **Default offsetek (a kód szerint, normatív):**
   Insurance 30/7/1 · Inspection 30/7 · Invoice 14/3 · Subscription 14/3 ·
   Medical 7/1 · School 7/1 · egyéb 7/1 nap.

## Indoklás

- A jóváhagyás-kori generálás megszünteti a stale-scan hibaosztályt, és
  hűen tükrözi az „AI nem aktivál" elvet: javaslatból semmilyen ütemezett
  művelet nem származik jóváhagyás előtt.
- Az egycsatornás modell egyszerű sémát tart; az eszkaláció úgyis új
  sort hoz létre, így a csatorna-váltás ott természetes.

## Következmények

- `ai-pipeline.md` 3.10 átírva: a policy-tábla a *jóváhagyáskor*
  generálandó remindereket írja le, csatorna = InApp.
- `reminder-engine.md` 2.4, 6.1, 8.1 igazítva; a dispatcher szülő-check
  defenzív (találat esetén WARN log + reminder `Cancelled`, nem skip-loop).
- Deadline-módosításkor (`DueDateUtc` változás) a kapcsolódó reminderek
  trigger-újraszámítása változatlanul él (reminder-engine.md 2.3).
- A `(deadline_id, offset)` idempotencia a jóváhagyás-flow-ban értelmezett
  (ismételt approve nem duplikál).

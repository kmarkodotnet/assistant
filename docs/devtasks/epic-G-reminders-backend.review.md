# Review — epic-G-reminders-backend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Alapos bontás a rendszer legérzékenyebb komponensére: XOR-invariáns
factory-kkal, catch-up, eszkaláció, quiet-hours, retention-job, és — ami
a legértékesebb — konkurencia- és idő-szimulációs tesztek (T-GBE-16/17)
külön taskként. A worktree-bontás (core vs. feed) tiszta.

## Hibák / észrevételek

### 1. T-GBE-13: worker `IHubContext` — ugyanaz a cross-process hiba (közepes—súlyos)
„A worker `IHubContext`-en publikál (server-to-server)” — a Workers külön
process, az Api-ban hosztolt hub `IHubContext`-je ott nem elérhető
(részletesen: epic-D-ai-pipeline-backend.review.md #1). A megoldás
(belső API-hívás / backplane / Postgres NOTIFY) mindkét epicre közös
architect-döntés.

### 2. A dispatcher „skip ha nem jóváhagyott” maradványa (közepes)
A T-GBE-06 megtartja a parent-approved ellenőrzést — a T-FBE-07 szerint
viszont reminder **csak jóváhagyáskor** jön létre, tehát jóváhagyatlan
szülőjű Scheduled reminder elvileg nem létezhet. Az ellenőrzés maradjon
defenzív guardnak, de a viselkedés (skip → örökre Scheduled marad?
`Suspended`?) definiálandó, különben a reminder-engine.review.md #1
batch-eltömődési hibája visszajöhet. Emellett a reminder-engine.md 6.1
„parent deleted/cancelled → reminder Cancelled” ága hiányzik az AC-ből —
pótolandó.
Hiányzik továbbá a **Deadline `Upcoming → Due → Passed` státusz-frissítő
job** gazdája (epic-F-backend.review.md #3) — ebben az epicben lenne a
természetes helye (a dispatcher mellett), de nincs rá task.

### 3. T-GBE-03: `IcalRecurrenceEvaluator` az `Infrastructure.Ai`-ban (kicsi)
A recurrence-kiértékelésnek semmi köze az AI-hoz — helye a sima
`FamilyOs.Infrastructure` (ugyanez az implementation-plan.review.md #4-ben).

### 4. Csatorna-kérdés itt is nyitott (kicsi)
T-GBE-10/11: InApp + Email csatornák külön implementációk (jó), de az
egy remindert két csatornán kézbesítés (policy: „InApp + Email”) és az
egyértékű `reminder.channel` enum viszonya továbbra sincs lezárva
(reminder-engine.review.md #3).

### 5. Pozitívumok
- T-GBE-10 `IdempotencyKey = hash(reminder_id + escalation_level)` — a
  dupla-küldés elleni védelem konkrét.
- T-GBE-16 idő-szimulációs forgatókönyv pontosan a product-vision
  hardver-feltevését teszteli.
- T-GBE-07: a 14 napnál régebbi reminderek `Skipped`-be tétele lezárja
  az architecture.md 7 nap vs. reminder-engine 14 nap kérdést a 14 nap
  javára — az architecture.md 6.4 frissítendő.

## Verdikt

Jó epic; az #1 (SignalR) és #2 (dispatcher-viselkedés + hiányzó
deadline-státusz job) architect-szintű rendezése kötelező a BUILD előtt.

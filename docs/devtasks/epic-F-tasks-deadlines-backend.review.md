# Review — epic-F-tasks-deadlines-backend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Jó bontás: state machine-ek külön domain-taskok unit-tesztekkel, a
suggestions-inbox és batch-approve a F3 story-t pontosan fedi. **A
legfontosabb: a T-FBE-07 a default remindereket a Deadline jóváhagyásakor
generálja** — nem a javaslat-készítéskor, ahogy az ai-pipeline.md 3.10 és
a reminder-engine.md 8.1 írja. Ez a jobb terv, és mellesleg megoldja a
reminder-engine.review.md #1 súlyos hibáját (lejárt, jóvá nem hagyott
reminderek örökös újra-szkennelése) — de a két tervező-doksit hozzá kell
igazítani, különben a pipeline-worktree a régi (rossz) minta szerint is
legyárthat remindereket.

## Észrevételek

1. **Reminder-generálás időpontja — doksi-szinkron kötelező (súlyos,
   de itt jól megoldott):** lásd fent. Teendő: ai-pipeline.md 3.10
   („minden javaslathoz Reminder is születik `Scheduled`-ben”) és
   reminder-engine.md 8.1/6.1 átírása az approve-time generálásra;
   az Epic D-ben (T-DBE-16) explicit kimondani, hogy a deadline-kinyerés
   **nem** hoz létre Reminder-t.
2. **T-FBE-08: „InApp + Email csatorna” (közepes):** az egyértékű
   `reminder.channel` enum-probléma itt is jelen van
   (reminder-engine.review.md #3) — a policy-resolver kimenete
   csatornánként külön Reminder-sor vagy flags-enum; döntés kell.
3. **T-FBE-03: `Upcoming → Due → Passed` automatikus átmenet — nincs
   gazdája (közepes):** az AC „a worker scan-ben (lásd reminder-engine)”
   hivatkozik, de a reminder-engine.md-ben csak reminder-dispatcher és
   eszkalációs job van — deadline-státusz scanner sehol nincs
   specifikálva (Epic G-ben sem). Vagy a DueReminderDispatcher mellé
   kell egy `DeadlineStatusUpdater` recurring job (task hiányzik), vagy
   az állapot számított (query-time) — döntés + task pótlandó.
4. **T-FBE-05 Reject: „soft-delete + Origin megmarad”** — a Task
   státuszgépben van `Cancelled`; a reject = soft-delete vs. `Cancelled`
   döntés az api-design 11.5-tel („Suggested → Cancelled, soft delete”)
   nagyjából egyezik, de a kettő együtt (státusz is, törlés is?) legyen
   egyértelmű.

## Verdikt

Jó epic; az #1 doksi-szinkron és a #3 hiányzó job-gazda a BUILD előtt
rendezendő, a #2 a séma-döntéssel együtt.

# Review — epic-G-reminders-frontend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Jó FE-bontás: csoportosított reminder-feed, akció-dialogok (snooze/
reschedule/delegate), navbar bell + feed sheet, sticky toast stackkel,
SignalR lifecycle (auth után start, logoutkor stop). A
qa/ui-test-scenarios G-szekciója visszaigazolja, hogy a fő flow-k
meg is valósultak és tesztelve vannak.

## Észrevételek

1. **T-GFE-04 `unreadCount` vs. API (kicsi):** a facade a feed-ből
   számolja az olvasatlanokat — a qa-doksi QA-G6-01 viszont egy
   `GET /api/v1/notifications/unread-count` endpointra hivatkozik, ami
   az api-design.md-ben nem létezik (qa/ui-test-scenarios.review.md #2).
   Egy forrás legyen: vagy feed-alapú számolás (elég MVP-re), vagy új
   endpoint + api-design frissítés.
2. **T-GFE-09 Email-csatorna választó (kicsi):** „Email csak ha enabled” —
   pontosítandó, hogy ez a user `EmailEnabled` preferenciája ÉS a
   rendszer-SMTP konfiguráltság együttese (T-GBE-11 kétszintű kapuja).
3. **T-GFE-12 sticky toast stack (max 3)** — jó; catch-up után (PC
   visszakapcsolás, sok egyszerre tüzelő reminder) a 4. és további
   toast sorsa (queue? összesítő „+N további”?) definiálandó — a
   reminder-engine 7.1 „batch-elve, hogy az UI ne robbanjon” elvének
   UI-oldala pont ez lenne.
4. **T-GFE-14 tömeges „Újraütemezem”** — a backendben nincs batch-
   reschedule endpoint (api-design §13 csak per-reminder akciókat ad);
   vagy loop a kliensből (jelölendő), vagy endpoint-igény az api-design
   felé.

## Verdikt

Végrehajtásra kész; az 1. és 4. API-egyeztetés rendezendő, a 3. UX-él
a catch-up teszttel együtt próbálandó ki.

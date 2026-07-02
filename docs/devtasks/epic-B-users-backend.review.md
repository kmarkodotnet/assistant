# Review — epic-B-users-backend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Jó bontás; a B3 preferencia-tárolás itt végre eldőlt (T-BBE-07: külön
`user_preferences` tábla, defaultokkal) — ez a döntés visszavezetendő a
database-schema.md-be és domain-model.md-be. A last-admin invariáns
(T-BBE-05) és a bootstrap-onboarding (T-BBE-09) jól lefedett.

## Észrevételek

1. **T-BBE-01 a vitatott child-politikát kódolja (közepes):** az AC a
   megengedő változatot rögzíti (Child: „RelatedFamilyMemberId = saját ÉS
   !IsPrivate”) — a product-vision explicit-megosztás elvével szemben
   (security-privacy.review.md #1). Ez az a task, ahol a rossz döntés
   kódba égne; a BUILD előtt a politikát le kell zárni.
2. **T-BBE-04 nyitva hagyott entitás-döntés (kicsi):** „`UserAccountInvite`
   vagy pre-created `UserAccount` IsActive=false — az architect döntse
   el”. Task-szinten ez már döntést igényel, különben a sonnet választ
   (az eltérés-protokoll — context-matrix 2. tipp — szerint eszkalálnia
   kellene; jelezzük explicit, hogy ez eszkalációs pont).
3. **Epic A átfedés (kicsi):** a T-ABE-16..18 ugyanazokat a képességeket
   hozza létre; a „csak finomítások” megjegyzés jó, de a task-ID-k
   szintjén érdemes kimondani, melyik A-task melyik B-taskkal azonos
   (T-ABE-17 ≈ T-BBE-04, T-ABE-18 ≈ T-BBE-07/08), hogy két worktree ne
   implementálja kétszer.
4. **T-BBE-08 SMTP-warning** — jó UX-döntés (opt-in engedett, de
   figyelmeztet); a reminder-engine 5.2 „ha hiányzik, kikapcsolt”
   viselkedésével konzisztens.
5. **T-BBE-06:** „RevokedSessions-be cookie hash” — a cookie-ból mit
   hash-elünk (session id claim?) a T-ABE-13-mal közösen definiálandó.

## Verdikt

Végrehajtható; az #1 politikai döntés és a #2 entitás-döntés lezárása
után indítható a sonnet.

# Review — epic-H-notes-backend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Tömör, jó bontás — a Notes valóban a Documents-minta olcsó ismétlése,
és a fájl ezt ki is használja (analógiákra hivatkozik ahelyett, hogy
duplikálna). Az embedding-pipeline kiterjesztés (T-HBE-07) helyesen az
Epic D `EmbedJobRunner`-ére épül.

## Észrevételek

1. **T-HBE-09 sanitize-stratégia eldöntetlen (közepes):** a task két
   alternatívát hagy nyitva (backend-render + `HtmlSanitizer` VAGY raw
   markdown tárolás + kliens-oldali render), miközben a FE-fájl
   (T-HFE-06) már a kliens-oldali `marked` + DOMPurify megoldást
   építi. Döntés kell (a kliens-oldali az egyszerűbb és elég, ha a body
   sosem kerül más kontextusban HTML-ként kiszolgálásra) — és a
   security-privacy.md 9.2 (amely backend-oldali sanitize-t ír) ehhez
   igazítandó. A kettő együtt fölösleges duplikáció.
2. **Note-oknál nincs Classify/Summarize (kicsi):** a pipeline a Note-ra
   csak `Embed`-et futtat (T-HBE-02) — az ai-pipeline.md 1. szakasza
   szerint viszont „minden bejövő tartalom ugyanazon a láncon” megy át.
   MVP-szűkítésnek jó (a jegyzet nem igényel összefoglalót), csak
   mondjuk ki az ai-pipeline.md-ben, hogy Note → csak Embed.
3. **RBAC-hivatkozás:** a T-HBE-03 „Private csak tulajdonosnak” — a
   child-politika lezárásától függ (security-privacy.review.md #1),
   jelölendő függőségként.

## Verdikt

Végrehajtásra kész az #1 döntés lezárása után.

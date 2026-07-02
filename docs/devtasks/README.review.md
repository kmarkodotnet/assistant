# Review — devtasks/README.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Jó index-doksi: fájl-konvenció, task-formátum (ID, cél, fájlok, AC,
függőség), epic→fázis tábla és függőségi sorrend-ábra. A
kontextus-mátrixra való ráépülés (fejléc-hivatkozások anchoringhoz)
következetes.

## Észrevételek

1. **Fázis-hozzárendelés vs. implementation-plan** — az index F epicnél
   „Fázis 8/10”-et ír (az implementation-plan az F1–F3-at a 10. fázisba
   teszi; a 8-as feltehetően a suggestion-generálás miatt került ide) —
   egy fél mondatos magyarázat kell, különben ellentmondásnak látszik.
   A H epic itt „Fázis 11” — a mvp-backlog 14. táblájából viszont a H
   teljesen hiányzik (mvp-backlog.review.md #1); ez az index a
   teljesebb, a backlog-tábla javítandó hozzá.
2. **D epic frontend „—”** — a megjegyzés („SignalR push a BE fájlban”)
   alapján a D11 FE-részének (dokumentumkártya progress, RealtimeService)
   taskjai a C vagy A frontend fájlban kell legyenek — érdemes ide egy
   pontos hivatkozás, hogy melyik fájl melyik taskja fedi, különben a
   FE-munka gazdátlan.
3. **Megvalósítási sorrend ábra** — a „B → M.M1 → C” lánc jó kiegészítés
   (a compose-függőség az implementation-planben nem volt explicit);
   ugyanakkor az M1 az implementation-plan 1. fázisában (compose skeleton)
   már részben elkészül — a kettő közti különbség (skeleton vs. teljes)
   egy lábjegyzetet érdemel.

## Verdikt

Jó vezérlő-index; az 1–2. pont hivatkozás-pontosításai után kész.

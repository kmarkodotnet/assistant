# Review — epic-E-search-frontend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Jó FE-bontás a chat-szerű keresőre: message-komponensek, forrás-citáció
kártyák, slot-chipek, globális keresősáv, filter-mód lista-render (nem
chat) — a search-strategy.md UI-elveit hűen követi. A sessionStorage-
history (T-EFE-02) a LocalOnly elv helyes alkalmazása.

## Észrevételek

1. **429 rate-limit UX hiányzik (kicsi):** a backend 10 req/min/user
   limitet vezet be a qa/semantic módra (T-EBE-14) — egyik task sem írja
   le, mit mutat a UI a limit elérésekor (magyar üzenet + visszaszámláló
   lenne a jó). Egy AC pótlandó a T-EFE-03-ba vagy T-EFE-12-be.
2. **Lassú Q&A jelzés (kicsi):** a „AI gondolkodik...” indikátor jó, de
   az Ollama hidegindításnál a válasz percekig is tarthat
   (search-strategy.review.md „meleg modell” megjegyzés) — timeout-UX
   (mennyi után adjuk fel, mit mondunk) nincs definiálva.
3. **T-EFE-11 `?q=` integráció Tasks/Deadlines oldalra** — ezek az
   oldalak az Epic F FE-ben készülnek; a függőség (F-FE után futtatható
   rész) jelölendő a sorrend-ábrában, különben a worktree elakad.
4. **T-EFE-12 automatikus semantic-fallback** — jó ötlet, de ez plusz
   API-hívást indít user-akció nélkül; a rate-limitbe ez is beleszámít —
   egy mondat a viselkedésről (csak egyszer, nem loop) hasznos.

## Verdikt

Végrehajtásra kész; az 1–2. UX-élek pótlása ajánlott még a fejlesztés
előtt, mert utólag drágább.

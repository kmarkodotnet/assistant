# Review — epic-H-notes-frontend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Egyszerű, arányos bontás: lista + detail + split-view markdown editor +
sanitize pipe, XSS-teszttel. A `hasUnsavedChangesGuard` integráció
(T-HFE-05) jó részlet.

## Észrevételek

1. **Sanitize-döntés függősége (közepes):** a T-HFE-06 kliens-oldali
   `marked` + DOMPurify — a BE-fájl T-HBE-09 még backend-renderes
   alternatívát is nyitva hagy (epic-H-notes-backend.review.md #1).
   Egy megoldás legyen; ha a kliens-oldali marad, a
   security-privacy.md 9.2 frissítendő.
2. **Child read-only viselkedés hiányzik (kicsi):** a qa/ui-test-scenarios
   QA-H1-04 childdal rejtett szerkesztő-gombokat vár — itt nincs task a
   szerepkör-feltételes renderelésre. Egy AC pótlandó (T-HFE-03/04-be),
   a child-politika lezárása után.
3. **T-HFE-05 „Auto-save vagy explicit Mentés” (kicsi):** döntetlen —
   auto-save embedding-jobot triggerelne minden gépelésnél (T-HBE-04
   szerint body-változás → re-embed); explicit mentés a helyes válasz,
   mondjuk ki.
4. A qa-doksi jelezte, hogy a megvalósult Notes-oldal natív `confirm()`-ot
   használ a shared confirm-dialog helyett — a T-HFE-ben nincs törlés-
   megerősítő task; pótolható a QA follow-up #5 szerint.

## Verdikt

Rendben; az 1. és 3. döntések kimondása után adható ki.

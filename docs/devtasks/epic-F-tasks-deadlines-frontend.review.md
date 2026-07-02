# Review — epic-F-tasks-deadlines-frontend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Jó FE-bontás: kanban/lista toggle, saját hónap-grid naptár, suggestions
inbox batch-flow-val. **A T-FFE-08 saját naptár-komponenst épít** — ez
feloldja a frontend-structure.md hibás `@angular/cdk/calendar` hivatkozását
(frontend-structure.review.md #1); a tervező-doksi frissítendő.

## Észrevételek

1. **Kanban „Cancelled” oszlop hiánya (kicsi):** a T-FFE-03/05 négy
   oszlopa Suggested/Open/InProgress/Done — az elvetett (Cancelled)
   taskok nem látszanak sehol (frontend-structure.review.md is jelezte).
   Egy „Elvetettek” szűrő/nézet AC-ként pótlandó, vagy explicit döntés,
   hogy a Cancelled csak listanézet-szűrővel érhető el.
2. **T-FFE-14 drag-approve (kicsi):** a „Suggested task drag → Open”
   gesztus jóváhagyásnak számít — feleljen meg az „AI nem aktivál” elv
   explicit-megerősítés követelményének (a drag az; csak legyen
   visszavonási lehetőség / undo-toast, mert a drag könnyen véletlen).
3. **T-FFE-09 offset-chipek** — a deadline-kártyán a kapcsolódó
   reminderek vizualizációja az approve-time reminder-generálással
   (T-FBE-07) konzisztens; jóváhagyás *előtt* a chip-ek a *policy
   szerinti leendő* remindereket mutassák (ez UX-döntés, egy mondat
   pótlandó).
4. **Naptár-nézet hatóköre** — hónap-grid + popover elég MVP-re; a
   frontend-structure 8.5 „@angular/cdk/calendar” sora törlendő, és
   érdemes a T-FFE-08-ra hivatkozni.

## Verdikt

Végrehajtásra kész; kis UX-döntések (1–3) tisztázása ajánlott a
fejlesztés előtt.

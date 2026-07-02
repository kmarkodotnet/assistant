# Review — epic-L-dashboard-frontend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Jó, widget-enkénti bontás a frontend-structure.md 8.1 wireframe-je
alapján: skeleton-loading, auto-refresh tab-inaktivitás kezeléssel,
napszak-üdvözlés, üres állapotok. A qa/ui-test-scenarios QA-L1-01
visszaigazolja a megvalósult 4 fő widgetet.

## Észrevételek

1. **Kiszivárgott meta-instrukció (kicsi, de vicces):** T-LFE-07 AC:
   „Üres állapot: »Nincs lecsúszott emlékeztető. 🎉« (emoji csak ha a
   user explicit kéri)” — a zárójeles rész egy asszisztens-írási
   irányelv maradványa, nem termékkövetelmény. Törlendő, és döntsük el
   simán: emoji igen vagy nem (javaslat: nem).
2. **Szerepkör-specifikus widget-lista definiálatlan (kicsi):** az
   Epic-DoD „Adult / Admin / Child eltérő widget-lista” — de egyik task
   sem mondja meg, melyik szerepkör mit lát (pl. child lát-e
   PendingSuggestions-t? — feltehetően nem, mert jóváhagyni sem tud).
   Egy rövid mátrix pótlandó, a child-politika lezárása után.
3. **T-LFE-08 saved-searches widget** — az E7 (Could) függőség miatt
   legyen defenzív: üres állapota már specifikálva van, jó; a widget
   feltételes megjelenítése (elrejtés, ha a feature kiesik) egy sor AC.
4. **T-LFE-09 üdvözlés-sávok** — a „Jó éjt (22-5)” belépéskor furcsa
   üdvözlés; apróság, de az UX-írónak (vagy a magyar copy-nak) érdemes
   átnézni.

## Verdikt

Végrehajtásra kész; az 1. sor törlése és a 2. widget-mátrix pótlása
gyors javítás.

# Review — epic-K-integrations-frontend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Rendben lévő Settings-bontás: integrációk (Gmail connect/sync/disconnect),
AI-provider kártyák lakat-ikonos PrivacyMode jelzéssel, rendszer-tab,
backup-info widget, szerepkör-tudatos tab-render. A PrivacyMode
read-only UI (T-KFE-03) helyesen oldja fel a frontend-structure.md 8.10
választós verzióját (frontend-structure.review.md #2).

## Észrevételek

1. **T-KFE-07 OAuth-callback a kliensen (közepes):** ütközik a
   T-KBE-02 backend-callback tervével — lásd
   epic-K-integrations-backend.review.md #1; a FE-oldal szerepe a
   döntés után pontosítandó (valószínűleg csak egy „sikeres
   csatlakozás” visszairányító oldal kell).
2. **AI providers oldal helye (kicsi):** `/settings/ai-providers` itt vs.
   `/admin/providers` az Epic J-ben — konszolidálandó
   (epic-J-audit-admin-frontend.review.md #1); a megvalósult kód a
   settings-változatot követte.
3. **T-KFE-05 backup-info adatforrása (kicsi):** az „utolsó backup dátum
   + méret + manifest hash” megjelenítéséhez backend-endpoint kell
   (a backup a hoston fut, fájlrendszerből olvasandó) — a T-KBE-09
   system-settings query nem tartalmazza; endpoint-igény jelölendő az
   api-design.md felé.
4. **T-KFE-04 „default csendes órák (új user default)”** — ilyen
   rendszer-szintű default a reminder-engine 12. konfigjában létezik
   (`DefaultQuietHours`), de a system-settings PATCH scope-jában
   (T-KBE-09) nincs nevesítve; a két AC összehangolandó.

## Verdikt

Végrehajtásra kész; az 1–2. konszolidációk az orchestrátor Fázis 12-es
scope-döntései.

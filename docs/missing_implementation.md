# Hiányzó implementációk — Family OS

**Keletkezett:** 2026-07-08  
**Forrás:** futó alkalmazás auditja + frontend forráskód átvizsgálása

---

## Összefoglaló

A backend API-k (Epic A–L) teljes egészében elkészültek. A frontend oldalak
egy részénél azonban csak placeholder stub maradt — ezek `comingSoon` szövegét
jelenítik meg, a tényleges UI-t soha nem implementálták.

---

## 1. Teljes egészében hiányzó frontend oldalak (stub)

### Epic E — Keresés (search)

**Érintett fájl:** `frontend/src/app/features/search/search.page.ts`  
**Állapot:** üres placeholder, egyetlen `comingSoon` szöveggel

Hiányzó fejlesztés:
- `search/services/search.api.ts` — Search API client + DTOs
- `search/services/search.facade.ts` — chat-history, `ask()`, `clearHistory()`
- `search/pages/search.page.ts` — chat-szerű keresési felület (input, mode-select, scroll-able válasz lista)
- `search/components/chat-user-message.component.ts` — kérdés bubble
- `search/components/chat-answer-message.component.ts` — válasz bubble, forráshivatkozásokkal
- `search/components/answer-sources.component.ts` — hivatkozott dokumentumok listája
- Globális keresősáv bekötése a navbar-ba

---

### Epic F — Feladatok (tasks)

**Érintett fájl:** `frontend/src/app/features/tasks/tasks.page.ts`  
**Állapot:** üres placeholder

Hiányzó fejlesztés:
- `tasks/services/tasks.api.ts` — CRUD + state action endpointok
- `tasks/models/task.dto.ts`
- `tasks/services/tasks.facade.ts` — kanban állapotkezelés
- `tasks/tasks.page.ts` — kanban vagy lista nézet, family member szűrés, AI-suggested kiemelés
- `tasks/components/task-card.component.ts`
- `tasks/components/task-form.dialog.ts`
- Route kibővítése detail/create aloldalakkal

---

### Epic F — Határidők (deadlines)

**Érintett fájl:** `frontend/src/app/features/deadlines/deadlines.page.ts`  
**Állapot:** üres placeholder

Hiányzó fejlesztés:
- `deadlines/services/deadlines.api.ts` — CRUD + state action endpointok
- `deadlines/models/deadline.dto.ts`
- `deadlines/deadlines.page.ts` — naptár + lista nézet, kategória színkóddal
- `deadlines/components/deadline-card.component.ts`
- `deadlines/components/deadline-form.dialog.ts`

---

### Epic J — Admin oldal (admin)

**Érintett fájl:** `frontend/src/app/features/admin/admin.page.ts`  
**Állapot:** üres placeholder (a suboldalak — audit log, AI jobs, AI providers — el vannak készítve, de az admin shell-oldal stub)

Hiányzó fejlesztés:
- `admin/admin.page.ts` — shell/navigáció az admin aloldalakhoz  
  *(az `admin.routes.ts` a suboldalakat betölti, de a shell üres)*

---

### Epic B — Család (family) — fő shell

**Érintett fájl:** `frontend/src/app/features/family/family.page.ts`  
**Állapot:** üres placeholder

Megjegyzés: a `family.routes.ts` már a `family-list.page.ts`-re mutat (az el van készítve), így ez a stub fájl valójában **nem kerül betöltésre** — a routing kihagyja. A Család menüpont funkcionálisan működik.

---

## 2. Részleges implementációk (meglévő oldal, hiányos tartalom)

### Epic D — Dokumentum részletoldal — AI összefoglaló szekció

**Érintett fájl:** `frontend/src/app/features/documents/pages/document-detail.page.ts:51`  
**Hiányos blokk:** AI összefoglaló szekció — `hamarosan elérhető (Epic D után)` placeholder szöveg látható, az összefoglaló API-hívás nincs bekötve.

---

### Epic I — Dokumentum részletoldal — Címkék és témák szekció

**Érintett fájl:** `frontend/src/app/features/documents/pages/document-detail.page.ts:83`  
**Hiányos blokk:** Tagek és topics megjelenítése/szerkesztése — `hamarosan elérhető (Epic I után)` placeholder szöveg, az API-hívás nincs bekötve.

---

### Epic J — Beállítások — Rendszer aloldal

**Érintett fájl:** `frontend/src/app/features/settings/pages/settings-system.page.ts`  
**Állapot:** `A rendszerbeállítások hamarosan elérhetők lesznek.` — üres stub, a rendszerbeállítások API nincs bekötve.

---

### Epic K — Beállítások — AI Providerek aloldal

**Érintett fájl:** `frontend/src/app/features/settings/pages/ai-providers-settings.page.ts`  
**Állapot:** részlegesen implementálva, néhány szekció `hamarosan elérhető` megjegyzéssel.

---

## 3. Működő oldalak (referencia)

| Oldal | Fájl | Állapot |
|---|---|---|
| Irányítópult | `dashboard/dashboard.page.ts` | ✅ kész |
| Dokumentumlista | `documents/pages/documents-list.page.ts` | ✅ kész |
| Dokumentum feltöltés | `documents/pages/document-upload.page.ts` | ✅ kész |
| Emlékeztetők | `reminders/reminders.page.ts` | ✅ kész |
| Jegyzetek | `notes/notes.page.ts` | ✅ kész |
| Javaslatok | `suggestions/suggestions.page.ts` | ✅ kész |
| Témák | `topics/topics.page.ts` | ✅ kész |
| Család / taglista | `family/pages/family-list.page.ts` | ✅ kész |
| Beállítások / Személyes | `settings/pages/preferences.page.ts` | ✅ kész |
| Beállítások / Integrációk | `settings/pages/integrations.page.ts` | ✅ kész |
| Beállítások / Backup | `settings/pages/backup.page.ts` | ✅ kész |
| Admin / Audit log | `admin/pages/audit-log.page.ts` | ✅ kész |
| Admin / AI jobs | `admin/pages/ai-jobs.page.ts` | ✅ kész |

---

## 4. Prioritási javaslat

| Prioritás | Feature | Megjegyzés |
|---|---|---|
| 🔴 Magas | Feladatok oldal (Epic F) | Core feature, irányítópulton is szerepel |
| 🔴 Magas | Határidők oldal (Epic F) | Core feature, irányítópulton is szerepel |
| 🟡 Közepes | Keresés oldal (Epic E) | AI search, komplex UI |
| 🟡 Közepes | Dokumentum részletoldal — AI összefoglaló (Epic D) | Kész az API, csak a FE hiányzik |
| 🟡 Közepes | Dokumentum részletoldal — Tagek/témák (Epic I) | Kész az API, csak a FE hiányzik |
| 🟢 Alacsony | Rendszerbeállítások (Epic J) | Admin funkció |
| 🟢 Alacsony | Admin shell oldal (Epic J) | A suboldalak elérhetők közvetlen URL-lel |

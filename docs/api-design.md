# API tervezés — Family OS

> Státusz: DRAFT v0.1 · Dátum: 2026-06-26 · Nyelv: magyar (üzenet-szövegek), API: angol path-ek
> Kapcsolódó: [architecture.md](architecture.md), [domain-model.md](domain-model.md),
> [security-privacy.md](security-privacy.md), [frontend-structure.md](frontend-structure.md)

---

## 1. Konvenciók

### 1.1 Stílus

- **REST**, JSON, UTF-8.
- **Verziózás:** path-ban (`/api/v1/...`). Új major verzió kompatibilitást
  törhet; addig csak hozzáadunk (additive change).
- **Erőforrás-orientált path-ek**, többes számban, kisbetűsen,
  kebab-case (`/documents`, `/family-members`).
- **HTTP metódusok hagyományosan:**
  - `GET` — listázás / lekérés (idempotens, oldalsó hatás nélkül).
  - `POST` — létrehozás vagy „nem-CRUD" akció (`/.../approve`).
  - `PUT` — teljes csere (ritka MVP-ben).
  - `PATCH` — részleges módosítás (alapértelmezett MVP-ben).
  - `DELETE` — soft delete (vagy `?hard=true` admin-akció).

### 1.2 Státuszkódok

| Kód | Mikor |
|---|---|
| 200 | OK, GET / PATCH / akció |
| 201 | Created, POST új rekord |
| 202 | Accepted, hosszú futású munka elindítva (sync válasz a queue ID-vel) |
| 204 | No Content, sikeres DELETE |
| 400 | Validation error |
| 401 | Authentikáció hiányzik / lejárt |
| 403 | Tiltva (RBAC / row-level) |
| 404 | Nem található |
| 409 | Konfliktus (RowVersion ütközés, dedup hit) |
| 415 | Nem támogatott MIME |
| 422 | Szemantikai validáció (`due_date < today`) |
| 429 | Rate limit |
| 500 | Belső szerver hiba |
| 503 | AI provider nem elérhető |

### 1.3 Hibák — ProblemDetails (RFC 9457)

Minden hibás válasz `application/problem+json`:

```json
{
  "type":      "https://family-os/errors/validation",
  "title":     "Érvénytelen kérés",
  "status":    400,
  "detail":    "A dokumentum mérete meghaladja az 50 MB-os limitet.",
  "traceId":   "00-1d2c...-01",
  "fieldErrors": {
    "file": ["Túl nagy fájl (max 50 MB)."]
  }
}
```

- `type` URI **kategória** (a backend egységes katalógusa, nem dereferálható).
- `title` rövid kategória-cím (angolul).
- `detail` mindig **magyar** felhasználói üzenet.
- `traceId` minden válaszban (W3C TraceContext).
- `fieldErrors` opcionális, validációs hibákhoz.

### 1.4 Authentikáció és session

- **Cookie alapú**, HttpOnly + Secure + SameSite=Lax (`__Host-family-os-session`).
- Frontend a `GET /api/v1/auth/me`-re hivatkozik az inicializációkor.
- 401-et a frontend `authInterceptor`-ja kezeli (`/login` redirect).
- LAN-only architektúra → CORS allowlist a háztartási hálózatra
  (lásd `security-privacy.md`).

### 1.5 Fejlécek

| Header | Irány | Cél |
|---|---|---|
| `Idempotency-Key` | request | hosszú futású / kritikus POST-okra dedup-érvény |
| `If-Match` | request | optimistic concurrency (`RowVersion` hex) |
| `ETag` | response | rowversion + last update hash |
| `traceparent` | request/response | W3C TraceContext |
| `X-Request-Id` | response | backend trace ID (= ProblemDetails `traceId`) |
| `X-Total-Count` | response | listázás teljes találatszám (estimate) |

### 1.6 Pagination

Minden lista cursor + page hibrid:

```
GET /api/v1/documents?page=1&pageSize=50&sort=createdUtc:desc
```

Válasz:
```json
{
  "items":     [ ... ],
  "page":      1,
  "pageSize":  50,
  "totalCount": 138,
  "totalPages": 3
}
```

- `pageSize` max 100 (kivétel: audit-log export/lista max 200, lásd 19.1);
  ha túl nagyot kér, 400 + magyar üzenet.
- `sort` formátum `mező:asc|desc`. Több sort vesszővel.
- `cursor`-alapú pagination (a `nextCursor` mező) opcionális, a search
  válaszainál használt.

### 1.7 Szűrés

Egyszerű listázáshoz query-paraméterek (`?topicSlug=jarmu&isPaid=false`).
Komplex szűréshez `POST /api/v1/<resource>/search` testtel.

### 1.8 RBAC jelzése

Minden endpoint-specifikációban kiírjuk:

> **Policy:** `RequireAdmin` | `RequireAdult` | `RequireAuthenticated`
> **Row-level:** rekord-szintű ellenőrzés (`security-privacy.md` 4.3).

### 1.9 Idempotencia

A hosszú futású vagy nem-idempotens kritikus POST-okra (dokumentum-upload,
reprocess, sync — az endpoint-leírás jelzi) a kliens **kötelezően** küld
`Idempotency-Key`-t (GUID). A backend a `(user, key)` → response mappingot
**best-effort, memóriában** őrzi (célérték 24 óra; process-restart üríti —
MVP-vállalás, Postgres-tár v2); duplikált hívás ugyanazt a választ adja
vissza. A tartalmi dedup-ot ettől függetlenül a sha256 (7.1) védi.

### 1.10 OpenAPI

- A backend `/openapi/v1.json` és `/openapi/v1.yaml` végpontot szolgáltatja
  (Swashbuckle vagy NSwag).
- Az UI: `/swagger` (csak admin role).
- A frontend `pnpm gen:api` ebből generálja a TS klienst.

---

## 2. Közös DTO minták

### 2.1 ID-k

UUIDv7 string formátumban (`"01910a0c-...-...."`).

### 2.2 Időbélyegek

ISO 8601 UTC: `"2026-06-26T14:30:00Z"`. Dátum-mezők dátum-csak: `"2026-06-26"`.

### 2.3 Soft-delete

Listák alapból csak nem-törölt rekordokat adnak vissza. `?includeDeleted=true`
admin-only, és ekkor a DTO `deletedUtc` mezőt is tartalmaz.

### 2.4 RowVersion

Minden szerkeszthető rekord DTO-ja tartalmaz `rowVersion: string` (base64).
PATCH-nél kötelezően `If-Match` header-ben vagy body-ban.

### 2.5 Origin és Approval mezők

```json
{
  "origin": "Manual" | "AiSuggested" | "AiApproved" | "ImportedEmail" | "ImportedFile",
  "approvedByUserAccountId": "..." | null,
  "approvedUtc": "..." | null
}
```

---

## 3. Auth

### 3.1 `POST /api/v1/auth/login/google`
**Cél:** Google ID token validáció + session cookie
([ADR-0005](decisions/ADR-0005-auth-flow-id-token.md) — kliens-oldali
GIS flow; login-redirect-URI nincs).
- Body: `{ "idToken": "..." }`
- Válasz 200: `CurrentUserDto`. Set-Cookie: `__Host-family-os-session`.
- Hiba: 401 (érvénytelen token), 403 (email nincs az allowlist-en).
- **Policy:** anonim.

### 3.2 `POST /api/v1/auth/logout`
- Válasz 204. Cookie törlés + revoked session list-be jegyzés.
- **Policy:** `RequireAuthenticated`.

### 3.3 `GET /api/v1/auth/me`
- Válasz 200: `CurrentUserDto { userAccountId, familyMemberId, displayName,
  email, role, preferences }`.
- 401, ha nincs session.
- **Policy:** `RequireAuthenticated`.

---

## 4. System

### 4.1 `GET /api/v1/system/heartbeat`
**Cél:** frontend LAN-detect ping (lásd `frontend-structure.md` 12).
- Válasz 200: `{ "ok": true, "serverTimeUtc": "..." }`. Auth NEM kell.

### 4.2 `GET /api/v1/system/version`
- Válasz 200: `{ "version": "0.1.0", "gitSha": "...", "builtUtc": "..." }`.
- **Policy:** anonim.

### 4.3 `GET /healthz/live` és `/healthz/ready`
- Standard ASP.NET Core health endpointok (nem `/api/v1/` alatt).
- `live`: process up. `ready`: DB + Ollama elérhető.

---

## 5. Family Members

### 5.1 `GET /api/v1/family-members`
- Lista (lapozás nélkül — kicsi). `?relation=Spouse` szűrés.
- **Policy:** `RequireAuthenticated`. **Row-level:** Child csak a saját és a public rekordot látja (display name, relation), nem a notes / birthDate-et.

### 5.2 `GET /api/v1/family-members/{id}`
- 200 / 404. **Row-level:** mindenki.

### 5.3 `POST /api/v1/family-members`
- Body: `CreateFamilyMemberDto { displayName, fullName?, relation, birthDate? }`.
- 201 + `FamilyMemberDto`.
- **Policy:** `RequireAdmin`.

### 5.4 `PATCH /api/v1/family-members/{id}`
- `If-Match` header kötelező.
- 200 / 409 (conflict).
- **Policy:** `RequireAdmin`.

### 5.5 `DELETE /api/v1/family-members/{id}`
- Soft delete. 204 / 409 (van élő `UserAccount` → előbb azt kell deaktiválni).
- **Policy:** `RequireAdmin`.

---

## 6. User Accounts (admin)

### 6.1 `GET /api/v1/user-accounts`
- Lista. `?role=Admin` szűrés.
- **Policy:** `RequireAdmin`.

### 6.2 `POST /api/v1/user-accounts/invite`
- Body: `{ "email": "...", "familyMemberId": "...", "role": "Adult" }`.
- A `email` allowlist-be kerül; az érintett személy a következő Google
  login-jakor automatikusan a megfelelő szerepet kapja.
- 201 + `InviteDto`. **Policy:** `RequireAdmin`.

### 6.3 `PATCH /api/v1/user-accounts/{id}`
- Csak `role` és `isActive` módosítható; minden más a UserAccount tulaja
  szerkesztheti a `/me`-n.
- **Policy:** `RequireAdmin`.

### 6.4 `DELETE /api/v1/user-accounts/{id}`
- Soft delete + revoked session list.
- **Policy:** `RequireAdmin`.

### 6.5 `PATCH /api/v1/auth/me/preferences`
- Body: `{ emailEnabled, quietHoursStart, quietHoursEnd, escalationOptOut }`.
- **Policy:** `RequireAuthenticated`.

---

## 7. Documents

### 7.1 `POST /api/v1/documents`
**Cél:** dokumentum feltöltés. `multipart/form-data`.
- Részek: `file` (kötelező), `title?`, `relatedFamilyMemberId?`,
  `isPrivate?`, `documentDate?`.
- `Idempotency-Key` kötelező.
- Válasz **201** + `DocumentDto` (`processingStatus = Pending`).
- 409 ha sha256 dedup: a body tartalmazza a meglévő rekord ID-t —
  **kivéve**, ha a meglévő rekord a current user számára nem látható
  (más user privát dokumentuma): ekkor generikus 409, ID nélkül
  (információ-szivárgás elkerülése).
- 415 nem támogatott MIME.
- **Policy:** `RequireAdult`.

### 7.2 `GET /api/v1/documents`
- Listázás + szűrés (query):
  `?topicSlug=`, `?tagId=`, `?relatedFamilyMemberId=`, `?from=`, `?to=`,
  `?sourceType=`, `?processingStatus=`.
- **Policy:** `RequireAuthenticated`. **Row-level:** szigorú szűrés.

### 7.3 `POST /api/v1/documents/search`
- Body: `DocumentSearchRequest { query?, filters, sort, page, pageSize }`.
- 200 + `DocumentSearchResponse { items, totalCount, facets }`.
- A `query` `mode=text` (FTS) vagy `mode=hybrid` (search-strategy.md 3).
- **Policy:** `RequireAuthenticated`.

### 7.4 `GET /api/v1/documents/{id}`
- 200 + `DocumentDto` (inkluzív: summary, tags, topics, facet, suggestions).
- 404 / 403.

### 7.5 `GET /api/v1/documents/{id}/content`
- Bináris fájl letöltés. `Content-Disposition: inline` alap, `?download=true`
  attachment.
- Audit log: `Action = FileAccess`.

### 7.6 `GET /api/v1/documents/{id}/text`
- 200 + `DocumentTextDto { content, extractionMethod, language, charCount }`.

### 7.7 `PATCH /api/v1/documents/{id}/text`
- Body: `{ content }`. Manual correction.
- A módosítás újragenerálási jobokat indít: **Embed + Summarize**
  (tudatos szűkítés — a Deadline/Task-kinyerés nem fut újra
  automatikusan, mert a már jóváhagyott javaslatokat duplikálná;
  igény esetén a 7.12 reprocess endpointtal kérhető).
- **Policy:** `RequireAdult` + row-level.

### 7.8 `PATCH /api/v1/documents/{id}`
- `If-Match`. Módosítható: `title`, `documentDate`, `relatedFamilyMemberId`,
  `isPrivate`.
- **Policy:** `RequireAdult` + row-level.

### 7.9 `DELETE /api/v1/documents/{id}`
- Soft delete. Hard: `?hard=true` admin-only.
- **Policy:** `RequireAdult` (saját), `RequireAdmin` (idegen).

### 7.10 Facet endpointok (Warranty / Medical / Financial)

`PATCH /api/v1/documents/{id}/warranty`
`PATCH /api/v1/documents/{id}/medical-record`
`PATCH /api/v1/documents/{id}/financial-record`

- Body: a megfelelő `*Dto`. Idempotens upsert.
- **Policy:** `RequireAdult` + row-level (Medical: `MedicalRecord` szabályok).

### 7.11 Tag / Topic kapcsolás

`POST /api/v1/documents/{id}/tags` Body: `{ tagIds: [...] }`
`DELETE /api/v1/documents/{id}/tags/{tagId}`
`POST /api/v1/documents/{id}/topics` Body: `{ topicIds: [...] }`
`DELETE /api/v1/documents/{id}/topics/{topicId}`

### 7.12 Re-process

`POST /api/v1/documents/{id}/reprocess`
- Body: `{ jobs: ["Summarize", "ExtractDeadlines", ...] }` — opcionálisan a
  kiválasztott AI lépéseket újrafuttatja.
- 202 + queued job ID-k.
- **Policy:** `RequireAdult` + row-level.

---

## 8. Notes

### 8.1 `GET /api/v1/notes`, `GET /api/v1/notes/{id}`, `POST`, `PATCH`, `DELETE`
- Hasonló konvenció a dokumentumokhoz.
- `POST` body: `CreateNoteDto { title, body, relatedFamilyMemberId?, isPrivate?, tagIds?, topicIds? }`.
- A `POST` és `PATCH` után az embedding job aszinkron újrafut.

### 8.2 `POST /api/v1/notes/search`
- Body / response analóg a `/documents/search`-csel.

---

## 9. Tags

### 9.1 `GET /api/v1/tags`
- `?q=` autocomplete. `?sort=usageCount:desc`.

### 9.2 `POST /api/v1/tags`
- Body: `{ name, color? }`.
- 201 / 409 (név ütközés).

### 9.3 `PATCH /api/v1/tags/{id}`
- `If-Match`. Módosítható: `name`, `color`.

### 9.4 `DELETE /api/v1/tags/{id}`
- Soft delete; ha `usageCount > 0`, csak `?force=true`.
- **Policy:** `RequireAdult` (saját) / `RequireAdmin` (idegen).

---

## 10. Topics

### 10.1 `GET /api/v1/topics`
- Fa-struktúra: `?flat=true` lapos lista, default rekurzív tree.

### 10.2 `POST /api/v1/topics`, `PATCH`, `DELETE`
- Body: `{ name, slug, parentTopicId?, icon?, sortOrder? }`.
- **Policy:** `RequireAdmin` (a topic-fa kontroll alatt marad).

---

## 11. Tasks

### 11.1 `GET /api/v1/tasks`
- `?status=`, `?assignedToFamilyMemberId=`, `?priority=`, `?origin=AiSuggested`.

### 11.2 `POST /api/v1/tasks`
- Body: `CreateTaskDto`. `Origin = Manual` alapból.
- **Policy:** `RequireAdult`.

### 11.3 `PATCH /api/v1/tasks/{id}`
- Mezők: `title`, `description`, `priority`, `dueDate`, `assignedToFamilyMemberId`.
- Státusz külön endpointon (lásd 11.5).

### 11.4 `DELETE /api/v1/tasks/{id}`
- Soft delete. **Policy:** tulajdonos vagy admin.

### 11.5 Státusz átmenetek

| Akció | Endpoint |
|---|---|
| Jóváhagyás (Suggested → Open) | `POST /api/v1/tasks/{id}/approve` |
| Elvetés (Suggested → Cancelled, soft delete) | `POST /api/v1/tasks/{id}/reject` |
| Indítás (Open → InProgress) | `POST /api/v1/tasks/{id}/start` |
| Kész (… → Done) | `POST /api/v1/tasks/{id}/complete` |
| Mégse (… → Cancelled) | `POST /api/v1/tasks/{id}/cancel` |

Mindegyik 200 + frissített `TaskDto`. Audit log bejegyzés keletkezik.

---

## 12. Deadlines

### 12.1 `GET /api/v1/deadlines`
- `?from=`, `?to=`, `?category=`, `?status=`, `?responsibleFamilyMemberId=`.

### 12.2 `POST /api/v1/deadlines`, `PATCH`, `DELETE`
- Analóg.

### 12.3 Státusz akciók

`POST /api/v1/deadlines/{id}/approve` — Suggested → Upcoming + Origin=AiApproved
`POST /api/v1/deadlines/{id}/resolve` — `Status = Resolved`
`POST /api/v1/deadlines/{id}/dismiss` — `Status = Dismissed`

---

## 13. Reminders

### 13.1 `GET /api/v1/reminders`
- `?status=`, `?upcoming=true` (következő 30 nap).
- Csoportosított nézet a `frontend-structure.md` 8.6 szerint.

### 13.2 `POST /api/v1/reminders`
- Body: `CreateReminderDto { taskId?, deadlineId?, triggerUtc, recurrenceRule?, channel }`.
- XOR validáció (taskId / deadlineId pontosan az egyik).

### 13.3 `PATCH /api/v1/reminders/{id}`
- `triggerUtc`, `channel`, `recurrenceRule` módosítható.

### 13.4 Felhasználói akciók

| Akció | Endpoint |
|---|---|
| Nyugtázás | `POST /api/v1/reminders/{id}/acknowledge` |
| Halasztás | `POST /api/v1/reminders/{id}/snooze` Body: `{ minutes: 60 }` vagy `{ until: "..." }` |
| Mellőzés (kihagyom) | `POST /api/v1/reminders/{id}/skip` |
| Mégse | `DELETE /api/v1/reminders/{id}` — a reminder táblán nincs soft delete; a művelet `Status := Cancelled` |
| Delegálás | `POST /api/v1/reminders/{id}/delegate` Body: `{ familyMemberId }` |

---

## 14. Notifications

### 14.1 `GET /api/v1/notifications`
- A current user feed-je, lapozással.
- `?onlyUnread=true`.

### 14.2 `POST /api/v1/notifications/{id}/read`
- 204. Audit nem szükséges.

### 14.3 `POST /api/v1/notifications/read-all`
- 204.

---

## 15. Suggestions inbox

### 15.1 `GET /api/v1/suggestions`
- A current user-hez tartozó összes nyitott AI-javaslat aggregálva:
  ```json
  {
    "tasks":      [ ... ],
    "deadlines":  [ ... ],
    "tags":       [ ... ],   // dokumentumonként csoportosítva
    "topics":     [ ... ],
    "facets":     [ ... ]
  }
  ```

### 15.2 `POST /api/v1/suggestions/batch`
- Body:
  ```json
  {
    "approve": { "taskIds": [...], "deadlineIds": [...], "documentTags": [{docId, tagIds}], ... },
    "reject":  { ... }
  }
  ```
- 200 + aggregált eredmény (`{ approved: N, rejected: M, errors: [...] }`).

---

## 16. Search

### 16.1 `POST /api/v1/search`
**Cél:** univerzális keresés és Q&A. Lásd `search-strategy.md` 2–4.

Request:
```json
{
  "query": "Mikor jár le az autó kötelező biztosítása?",
  "mode":  "auto" | "filter" | "text" | "semantic" | "qa",
  "filters": { ... },
  "page":  1, "pageSize": 20
}
```

Response (mode függő):
```json
{
  "mode":       "qa",
  "answer":     "Az autó kötelező biztosítása 2026-09-14-én jár le...",
  "citedSources": [ { "type": "Document", "id": "...", "title": "...", "url": "/documents/..." } ],
  "extractedSlots": { "dateRange": null, "familyMember": null, "category": "Insurance" },
  "hits":       [ ... ],            // a strukturált hits, ha van
  "confidence": 0.92,
  "tookMs":     1840
}
```

**Rate limit:** `qa` és `semantic` mode: 10 req/min/user.

### 16.2 `GET /api/v1/search/saved`, `POST`, `DELETE`
- Mentett keresések kezelése a dashboard widgethez.

---

## 17. Sources (Gmail, manuális)

### 17.1 `GET /api/v1/sources`
- **Policy:** `RequireAdmin`.

### 17.2 `POST /api/v1/sources/gmail/connect`
- Indítja a Google OAuth flow-t a `gmail.readonly` scope-pal.
- Válasz: redirect URL.
- **Policy:** `RequireAdmin`.

### 17.3 `DELETE /api/v1/sources/{id}`
- Soft delete + Google token revoke.
- **Policy:** `RequireAdmin`.

### 17.4 `POST /api/v1/sources/{id}/sync`
- Trigger explicit szinkron (egyébként az `EmailIngestionPoller` 5 percenként).
- 202 + sync job ID.
- **Policy:** `RequireAdmin`.

---

## 18. AI processing (admin)

### 18.1 `GET /api/v1/ai-jobs`
- Lista. `?status=Failed`, `?jobType=Embed`.
- **Policy:** `RequireAdmin`.

### 18.2 `POST /api/v1/ai-jobs/{id}/retry`
- 202.

### 18.3 `POST /api/v1/ai-jobs/{id}/cancel`
- 204.

### 18.4 `GET /api/v1/ai-jobs/queue-stats`
- Live counters per `JobType` × `Status`. UI dashboard widget.

### 18.5 `GET /api/v1/ai-providers`
- Konfigurált providerek (`name`, `enabled`, `lastHealth`).
- **Policy:** `RequireAdmin`.

### 18.6 `PATCH /api/v1/ai-providers/{name}`
- Body: `{ enabled, model, ... }`. **A `PrivacyMode` itt NEM módosítható**
  (kódba égetett kapu — lásd `security-privacy.md` 8.1). A `Settings` oldal
  külön endpoint-on (lásd 21.1) kéri, de szigorú validációval.

---

## 19. Audit log (admin)

### 19.1 `GET /api/v1/audit-log`
- Lista. Szűrés:
  `?from=`, `?to=`, `?userAccountId=`, `?action=`, `?entityType=`,
  `?entityId=`.
- Pagination kötelező (max 200/oldal).
- **Policy:** `RequireAdmin`.

### 19.2 `GET /api/v1/audit-log/security-events`
- Pre-szűrt nézet: `Login`, `LoginFailed`, `PermissionChange`,
  `ExternalApiCall`.

### 19.3 `GET /api/v1/audit-log/export?from=&to=&format=csv|json`
- Streaming nagy datasetre. Audit a hozzáférés is.

---

## 20. Dashboard

### 20.1 `GET /api/v1/dashboard`
- Aggregált válasz a dashboard widget-ekhez:
  ```json
  {
    "upcomingDeadlines": [...],
    "pendingSuggestions": { "tasks": N, "deadlines": N, "facets": N },
    "recentDocuments": [...],
    "overdueReminders": [...],
    "savedSearches": [...]
  }
  ```
- Egyetlen hívás, hogy a dashboard < 200 ms-en betöltsön.

---

## 21. Settings

### 21.1 `GET /api/v1/settings/system`
- AI provider mód, retention, csendes órák, SMTP konfig (titkok nélkül).
- **Policy:** `RequireAdmin`.

### 21.2 `PATCH /api/v1/settings/system`
- Korlátozott mezők. `PrivacyMode` itt is *kötelezően* `LocalOnly` —
  ha egyébre próbálna állítani, **422** + magyar üzenet, hogy ez a kapu
  kódba van égetve a védelem érdekében (`HybridAllowed` v2-ben fog
  megjelenni; addig nem konfigurálható).
  > Megjegyzés: a `security-privacy.md` 8.2 leírja a `HybridAllowed` /
  > `AnyProvider` szándékot — az MVP-ben ezeket az API blokkolja.

### 21.3 `GET /api/v1/settings/preferences`
- A current user prefereciái (rövidítés a 6.5-höz).

---

## 22. SignalR hubok

A `frontend-structure.md` 6 szakaszában már részletes — itt a hub-ok és
események összegezve:

### 22.1 `/realtime/notifications`
- Server → client események:
  - `notificationCreated(NotificationDto)`
  - `reminderFired(ReminderDto)` (sticky toast)
  - `aiSuggestionReady(SuggestionSummaryDto)` (új batch-jóváhagyásra
    vár valami).

### 22.2 `/realtime/documents`
- Server → client események:
  - `documentProcessingProgress({ documentId, stage, percent })`
  - `documentProcessed(DocumentDto)`
  - `documentFailed({ documentId, error })`

A hubokra ugyanaz a cookie-alapú auth alkalmazódik.

> **MVP-korlát ([ADR-0008](decisions/ADR-0008-workers-realtime-jelzes.md)):**
> a hubokat az Api process hosztolja; a Workers-ben keletkező események
> (feldolgozási progress, reminder-tüzelés) MVP-ben **nem** érkeznek
> valós időben — a kliens polling/refresh útján frissül. Az olvasatlan
> értesítés-számláló a `GET /api/v1/notifications?onlyUnread=true`
> feed-ből számolódik (nincs külön unread-count endpoint).

---

## 23. Példa: dokumentum upload pillanatfelvétel

```http
POST /api/v1/documents HTTP/1.1
Host: family-os.lan
Content-Type: multipart/form-data; boundary=----X
Idempotency-Key: 7b9b3f1c-...

------X
Content-Disposition: form-data; name="title"

AXA kötelező biztosítás 2025-2026
------X
Content-Disposition: form-data; name="relatedFamilyMemberId"

01910a0c-...-...
------X
Content-Disposition: form-data; name="file"; filename="AXA-kotveny.pdf"
Content-Type: application/pdf

<binary>
------X--
```

```http
HTTP/1.1 201 Created
Location: /api/v1/documents/01910a0c-...
ETag: "vAB1QQ=="
Content-Type: application/json

{
  "id": "01910a0c-...",
  "title": "AXA kötelező biztosítás 2025-2026",
  "originalFileName": "AXA-kotveny.pdf",
  "mimeType": "application/pdf",
  "sizeBytes": 384921,
  "language": null,
  "documentDate": null,
  "relatedFamilyMemberId": "01910a0c-...",
  "isPrivate": false,
  "processingStatus": "Pending",
  "origin": "Manual",
  "createdByUserAccountId": "01910a0c-...",
  "createdUtc": "2026-06-26T14:30:00Z",
  "rowVersion": "AAAAAAAAAB0=",
  "_links": {
    "self":    "/api/v1/documents/01910a0c-...",
    "content": "/api/v1/documents/01910a0c-.../content",
    "text":    "/api/v1/documents/01910a0c-.../text"
  }
}
```

---

## 24. Példa: hiba payload

```http
HTTP/1.1 415 Unsupported Media Type
Content-Type: application/problem+json

{
  "type":   "https://family-os/errors/unsupported-mime",
  "title":  "Unsupported MIME type",
  "status": 415,
  "detail": "A '.exe' kiterjesztésű fájlokat nem támogatjuk.",
  "traceId": "00-1d2c...-01",
  "supportedMimeTypes": [
    "application/pdf", "image/jpeg", "image/png",
    "image/heic", "text/plain",
    "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
  ]
}
```

---

## 25. Korlátok és későbbi bővítések

- **GraphQL felület** — nem cél MVP-ben; ha a Q&A inbound miatt később
  pótkérdések jönnek, REST + dedikált aggregátor endpointok elégségesek.
- **Webhook kifelé** — nincs; a Family OS nem küld push-t harmadik félnek.
- **Bulk import endpoint** — egyszerű loop a frontendből; bulk REST API
  csak akkor, ha mérési alapon kell.
- **Public API kulcs** — single-tenant + LAN-only → értelmetlen MVP-ben.
- **Streaming válasz a Q&A-ban** — SSE, v2.

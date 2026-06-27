# Frontend struktúra — Family OS

> Státusz: DRAFT v0.1 · Dátum: 2026-06-26 · Nyelv: magyar
> Kapcsolódó: [architecture.md](architecture.md), [api-design.md](api-design.md),
> [search-strategy.md](search-strategy.md), [reminder-engine.md](reminder-engine.md)

---

## 1. Tech stack és vezérlőelvek

### 1.1 Választott stack (MVP)

- **Angular 20** standalone komponensekkel (nincs NgModule).
- **TypeScript strict mode** (`strict: true`, `noUncheckedIndexedAccess`).
- **Signals** alapú state — `signal`, `computed`, `effect`. RxJS csak ott,
  ahol natív stream-szemantika kell (HTTP, SignalR).
- **Routing**: standalone `provideRouter` + lazy-load minden feature.
- **HTTP**: `HttpClient` interceptor-okkal (auth, trace-id, error).
- **Realtime**: SignalR (.NET) → `@microsoft/signalr` kliens.
- **Forms**: Reactive Forms (FormBuilder) — template-driven csak triviális
  esetekre.
- **Styling**: Tailwind CSS + Angular CDK (primitívek), nincs Material UI
  egész book — egyszerűbb, könnyebb magyar tipográfia.
- **i18n**: `@angular/localize` magyar fő nyelvvel; egyetlen build, nincs
  multi-locale build a fix `hu`-ra.
- **Tesztelés**: Vitest + Angular Testing Library + Playwright (E2E).
- **Csomagkezelő**: pnpm.

### 1.2 Vezérlőelvek

1. **Kontent-first UI.** A felhasználó dokumentumokat és emlékeztetőket
   keres — nem chat-felületet és nem dashboard-játékot. A globális keresősáv
   és a feed minden oldalról elérhető.
2. **„Javaslat" mindenhol látszik.** Az AI által javasolt entitások (Task,
   Deadline, Tag, Topic, facet) vizuálisan elkülönülnek (sárga „javaslat"
   sáv) és batch-ben jóváhagyhatók.
3. **Magyar első, akcentus-tolerancia mindenhol.** A keresősáv és minden
   szöveges szűrő ékezet-toleráns; a UI strings magyarul a fordításfájlból.
4. **Mobil-reszponzív.** Minden oldal Tailwind breakpoint-okra (sm/md/lg)
   tesztelt; a navigáció mobile-on alulra kerül (bottom nav), desktop-on
   bal sidebar.
5. **Realtime észrevehető.** A dokumentum feldolgozási státusza, az új
   notification-ek és más usertől eredő változások SignalR push-on érkeznek.
6. **Offline-tűrés.** A LAN-only architektúra miatt offline = "kliens
   otthonon kívül". Ekkor a UI explicit „nem vagyunk otthon" képernyőt
   mutat (lásd 12. szakasz).

---

## 2. Project layout

```
frontend/
├─ src/
│  ├─ app/
│  │  ├─ app.config.ts                # provideRouter, interceptors, signalr
│  │  ├─ app.routes.ts                # top-level lazy routes
│  │  ├─ app.component.ts             # root shell (navbar, sidebar)
│  │  ├─ core/
│  │  │  ├─ auth/
│  │  │  │  ├─ auth.service.ts        # current user signal, login/logout
│  │  │  │  ├─ auth.guard.ts          # CanActivate
│  │  │  │  ├─ role.guard.ts          # data: { roles: ['Admin'] }
│  │  │  │  └─ auth.interceptor.ts    # cookie auth handling
│  │  │  ├─ api/
│  │  │  │  ├─ http-error.interceptor.ts
│  │  │  │  ├─ trace-id.interceptor.ts
│  │  │  │  └─ api-base.ts            # generated client base URL
│  │  │  ├─ realtime/
│  │  │  │  ├─ signalr.service.ts     # connection + hub events as signals
│  │  │  │  └─ realtime.tokens.ts
│  │  │  ├─ state/
│  │  │  │  ├─ create-store.ts        # tiny signal-store factory
│  │  │  │  └─ persist.ts             # localStorage persistence helper
│  │  │  └─ notifications/
│  │  │     ├─ notification.service.ts # feed signal + toast trigger
│  │  │     └─ toast.component.ts
│  │  ├─ shared/                       # components, pipes, directives
│  │  │  ├─ ui/                        # button, card, badge, dialog, sheet
│  │  │  ├─ forms/                     # field, datepicker, multiselect
│  │  │  ├─ pipes/                     # huDate, fileSize, ago, etc.
│  │  │  └─ icons/                     # icon-only components
│  │  ├─ features/
│  │  │  ├─ dashboard/
│  │  │  ├─ documents/
│  │  │  ├─ notes/
│  │  │  ├─ search/                    # AI search + Q&A
│  │  │  ├─ tasks/
│  │  │  ├─ deadlines/
│  │  │  ├─ reminders/
│  │  │  ├─ topics/
│  │  │  ├─ family/
│  │  │  ├─ suggestions/               # AI javaslatok approval inbox
│  │  │  ├─ settings/
│  │  │  └─ admin/                     # admin-only oldalak
│  │  └─ api-client/                   # generált, NEM kézzel írt
│  │     ├─ index.ts                   # NSwag / openapi-typescript output
│  │     └─ ... (típusok + service-ek)
│  ├─ assets/
│  │  ├─ i18n/hu.json
│  │  └─ icons/
│  ├─ styles/
│  │  ├─ tailwind.css
│  │  └─ theme.css
│  ├─ main.ts
│  └─ index.html
├─ public/
├─ angular.json
├─ tailwind.config.ts
├─ tsconfig.json
└─ vitest.config.ts
```

### Feature module belső szerkezet (példa: `documents/`)

```
features/documents/
├─ documents.routes.ts                  # nested lazy routes
├─ pages/
│  ├─ documents-list.page.ts
│  ├─ document-detail.page.ts
│  └─ document-upload.page.ts
├─ components/
│  ├─ document-card.component.ts
│  ├─ document-filter-panel.component.ts
│  ├─ document-suggestions-block.component.ts
│  └─ document-facet-editor.component.ts
├─ services/
│  ├─ documents.facade.ts               # store + komponens-szintű API
│  └─ document-upload.service.ts
└─ models/
   └─ document-filter.model.ts          # frontend-specifikus típus
```

Minden feature ugyanezt a `pages / components / services / models`
hierarchiát követi. A `services/<feature>.facade.ts` az API-kliens és a
signal-store közötti integrációs réteg — a komponensek csak a facade-ot
látják.

---

## 3. Routing

### 3.1 Top-level routes (`app.routes.ts`)

```ts
export const APP_ROUTES: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.page'),
    title: 'Bejelentkezés — Family OS',
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell.component'),
    children: [
      { path: '',           loadChildren: () => import('./features/dashboard/dashboard.routes') },
      { path: 'documents',  loadChildren: () => import('./features/documents/documents.routes') },
      { path: 'notes',      loadChildren: () => import('./features/notes/notes.routes') },
      { path: 'search',     loadChildren: () => import('./features/search/search.routes') },
      { path: 'tasks',      loadChildren: () => import('./features/tasks/tasks.routes') },
      { path: 'deadlines',  loadChildren: () => import('./features/deadlines/deadlines.routes') },
      { path: 'reminders',  loadChildren: () => import('./features/reminders/reminders.routes') },
      { path: 'topics',     loadChildren: () => import('./features/topics/topics.routes') },
      { path: 'family',     loadChildren: () => import('./features/family/family.routes'),
                            canActivate: [roleGuard], data: { roles: ['Admin'] } },
      { path: 'suggestions', loadChildren: () => import('./features/suggestions/suggestions.routes') },
      { path: 'settings',   loadChildren: () => import('./features/settings/settings.routes') },
      { path: 'admin',      loadChildren: () => import('./features/admin/admin.routes'),
                            canActivate: [roleGuard], data: { roles: ['Admin'] } },
    ],
  },
  { path: '**', redirectTo: '' },
];
```

### 3.2 Nested routes — példa: documents

```ts
export default [
  { path: '',         component: DocumentsListPage,    title: 'Dokumentumok' },
  { path: 'upload',   component: DocumentUploadPage,   title: 'Új dokumentum',
                      canActivate: [roleGuard], data: { roles: ['Admin','Adult'] } },
  { path: ':id',      component: DocumentDetailPage,   title: 'Dokumentum',
                      resolve: { doc: documentResolver } },
] satisfies Routes;
```

### 3.3 Guards

- **`authGuard`** — ha nincs current user (HTTP 401 a `/api/v1/auth/me`-re),
  redirect `/login`-ra a `returnUrl` query-paramwel.
- **`roleGuard`** — a route-on `data.roles`-on definiált szerepkörök alapján;
  ha a current user szerepe nem szerepel, 403 oldal.
- **`hasUnsavedChangesGuard`** — szerkesztő oldalakon (`CanDeactivate`),
  ha a form `dirty`, megerősítő dialog.

### 3.4 Resolverek

- `documentResolver`, `taskResolver`, stb. — egyszerű
  `(route) => inject(...).getById(route.params.id)`. Hibára `/404`.

---

## 4. State management

### 4.1 Filozófia

- **Signal-első.** A `signal`, `computed`, `effect` lefedik a UI-state
  90%-át.
- **Globális store-ok minimalizálva.** Csak `auth`, `notifications` és
  `current-user-prefs` globális. Minden más feature-szintű.
- **Nincs NgRx.** Túl sok boilerplate a single-tenant méretre. Egy
  házi `createStore` helper-rel kezeljük a feature store-okat.

### 4.2 Mini signal-store helper

```ts
// core/state/create-store.ts
export function createStore<T extends object>(initial: T) {
  const state = signal(initial);
  return {
    state: state.asReadonly(),
    update: (patch: Partial<T> | ((s: T) => Partial<T>)) =>
      state.update(s => ({ ...s, ...(typeof patch === 'function' ? patch(s) : patch) })),
    set: (next: T) => state.set(next),
    select: <R>(fn: (s: T) => R) => computed(() => fn(state())),
  };
}
```

### 4.3 Példa: documents store

```ts
// features/documents/services/documents.facade.ts
@Injectable({ providedIn: 'root' })
export class DocumentsFacade {
  private api = inject(DocumentsApiClient);
  private store = createStore({
    items: [] as DocumentDto[],
    filters: emptyFilters(),
    loading: false,
    error: null as string | null,
  });

  state = this.store.state;
  items = this.store.select(s => s.items);
  filteredCount = computed(() => this.items().length);

  async load() {
    this.store.update({ loading: true, error: null });
    try {
      const res = await firstValueFrom(this.api.list(this.store.state().filters));
      this.store.update({ items: res.items, loading: false });
    } catch (e: any) {
      this.store.update({ loading: false, error: e.message });
    }
  }

  setFilter(patch: Partial<DocumentFilter>) {
    this.store.update(s => ({ filters: { ...s.filters, ...patch } }));
    this.load();
  }
}
```

A komponensek a facade `items()` / `state().loading` jeleit olvassák,
és akciókat (`load`, `setFilter`) hívnak.

### 4.4 Globális `auth` store

```ts
export const authStore = createStore({
  user: null as CurrentUserDto | null,
  status: 'unknown' as 'unknown' | 'authenticated' | 'anonymous',
});

export const currentUser = computed(() => authStore.state().user);
export const isAuthenticated = computed(() => authStore.state().status === 'authenticated');
export const isAdmin = computed(() => currentUser()?.role === 'Admin');
```

### 4.5 Persistence

A `currentFilters` és néhány UI-preferencia (`sidebarCollapsed`,
`densityMode`) a `localStorage`-be perzisztálódik a `persist()` helper-rel.
**Soha** nem perzisztálunk dokumentumtartalmat vagy AI Q&A választ — a
LocalOnly elvet a kliens-oldali tárolásnál is tartjuk.

---

## 5. API kliens (generált)

### 5.1 Generálás

- A backend OpenAPI sémája build-időben elérhető (`/openapi/v1.json`).
- Build előtt egy `pnpm gen:api` parancs `nswag` vagy `openapi-typescript-codegen`-szel
  generálja a `src/app/api-client/`-be.
- **Soha nem szerkesztjük kézzel.** A generált fájlok gitben tartva
  (review során látszik a kontraktus változása).

### 5.2 Kliens forma

```ts
// example: api-client/documents.api.ts (generated)
@Injectable({ providedIn: 'root' })
export class DocumentsApi {
  constructor(private http: HttpClient) {}
  list(filter: DocumentFilterDto): Observable<DocumentListResponse> { ... }
  get(id: string): Observable<DocumentDto> { ... }
  upload(form: FormData): Observable<HttpEvent<DocumentDto>> { ... }
  delete(id: string): Observable<void> { ... }
}
```

### 5.3 Interceptorok

- **`authInterceptor`** — kezeli a 401-et (redirect `/login`), 403-at (toast).
- **`traceIdInterceptor`** — minden request-hez kibocsát egy `traceparent`
  fejlécet a backend logoláshoz.
- **`errorInterceptor`** — `ProblemDetails` formátumú response-okat egységes
  hibákká alakít (`AppError { code, message, traceId, fieldErrors }`).

---

## 6. Realtime — SignalR

### 6.1 Hub-ok

A backend két hub-ot exponál:
- `/realtime/notifications` — push events: `notificationCreated`,
  `reminderFired`, `aiSuggestionReady`.
- `/realtime/documents` — `documentProcessingProgress`, `documentProcessed`,
  `documentFailed`.

### 6.2 Kliens integráció

```ts
// core/realtime/signalr.service.ts
@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private conn = new HubConnectionBuilder()
      .withUrl('/realtime/notifications')
      .withAutomaticReconnect()
      .build();

  notificationCreated = signal<NotificationDto | null>(null);

  async start() {
    this.conn.on('notificationCreated', (n: NotificationDto) =>
        this.notificationCreated.set(n));
    await this.conn.start();
  }
}
```

A globális `NotificationService.feed()` signal-je merge-eli a SignalR
push-okat és a HTTP polling fallback-et (15 percenként, ha a SignalR
nem áll fenn).

---

## 7. Notification feed és toast

### 7.1 Feed

- Globális `NotificationService.feed: Signal<NotificationDto[]>` — a
  navbar `bell` ikon számláló a `feed().filter(n => !n.readUtc).length`-ot
  mutatja.
- A `/notifications` modal-sheet jeleníti meg a feed-et.

### 7.2 Toast

- Új SignalR `notificationCreated` esemény → toast jelenik meg jobb alul
  3 másodpercig.
- A `reminderFired` esemény (saját reminderre) speciális, sticky toast,
  ami nem tűnik el magától; akcióval ('Kész', 'Halaszt 1 óra') zárul.

---

## 8. Oldalak (pages)

### 8.1 Dashboard (`/`)

**Cél:** áttekintés egy pillantásra — közelgő emlékeztetők, javaslat-inbox,
utolsó feldolgozott dokumentumok.

```
┌──────────────────────────────────────────────────────────────┐
│  Üdv, Apa!  [Globális kereső sáv ━━━━━━━━━━━━━━━━━━━━━━━━━━] │
├───────────────────┬──────────────────────────────────────────┤
│ Közelgő határidők │  Jóváhagyásra váró javaslatok       (5)  │
│ (3)               │  ─────────────────────────────────────── │
│ • Autó kötelező   │  • Új deadline: Műszaki vizsga 2026-08… │
│   2026-09-14 (78n)│  • Új task: Biztosítás megújítása        │
│ • Lili oltás      │  [Megnézem mindet →]                     │
│   2026-07-12 (16n)│                                          │
│ ...               │                                          │
├───────────────────┼──────────────────────────────────────────┤
│ Friss dokumentumok│  Lecsúszott emlékeztetők (1)             │
│ (utolsó 7 nap)    │  ─────────────────────────────────────── │
│ • AXA-kötvény.pdf │  • Számla fizetés (esedékes 3 napja)     │
│ ...               │                                          │
└───────────────────┴──────────────────────────────────────────┘
```

**Komponensek:**
- `UpcomingDeadlinesWidget`, `SuggestionsInboxWidget`,
  `RecentDocumentsWidget`, `OverdueRemindersWidget`,
  `SavedSearchWidget` (mentett keresések — opcionális kártya).

**Adatok:** egyetlen `GET /api/v1/dashboard` aggregált endpoint
(performance-ra optimalizált).

### 8.2 Documents

#### 8.2.1 List (`/documents`)
- Bal: szűrő-panel (`DocumentFilterPanel`) — facet: topic, év, családtag,
  típus, isPrivate.
- Közép: kártya-grid vagy lista (toggle), pagination 50/oldal.
- Felül: keresősáv (auto mode → search redirect ha komplex query).

#### 8.2.2 Upload (`/documents/upload`)
- Drag-and-drop area + file picker (multi-file).
- Sha256 dedup figyelmeztetés: ha a fájl már létezik, link a meglévő rekordra.
- Feltöltés után: progress bar minden fájlra, majd `Document.processing_status`
  realtime push (SignalR), és a kártya zöldre vált, ha `Done`.

#### 8.2.3 Detail (`/documents/:id`)
- Fejléc: cím, feltöltő, dátum, méret, hivatkozott családtag, IsPrivate
  toggle.
- Tab-ok:
  - **Áttekintés**: AI summary (`DocumentSummary`), facet adatok
    (Warranty / Medical / Financial) szerkeszthetően.
  - **Szöveg**: `DocumentText.Content` szerkeszthető (manual correction).
  - **Címkék és témák**: Tag/Topic kapcsolás, javaslat-jelölés.
  - **Kapcsolódó feladatok és határidők**: linkelt `Task`-ok és
    `Deadline`-ok listája, közvetlen jóváhagyás-akció.
- Felül: **Suggestions block** ha vannak még jóváhagyatlan AI-javaslatok
  → batch „Elfogadom mind" / „Elvetem mind" gomb.

### 8.3 AI Search (`/search`)

**Cél:** chat-szerű kérdés-válasz interfész + szabad-szavas dokumentum-kereső.

```
┌──────────────────────────────────────────────────────────────┐
│  AI kereső                                                   │
│  ┌──────────────────────────────────────────────────────┐    │
│  │ Kérdezz a családi adataidról…  [mód: auto ▾]   [⏎]   │    │
│  └──────────────────────────────────────────────────────┘    │
│                                                              │
│  → "Mikor jár le az autó kötelező biztosítása?"              │
│  ← Az autó kötelező biztosítása 2026-09-14-én jár le, az     │
│    AXA 2025-09-15-én kiállított kötvénye alapján.            │
│    Forrás: [📄 AXA-kotveny-2025-2026.pdf]                    │
│                                                              │
│  ⌖ Szűrők ezen választhoz: [Pénzügy] [Biztosítás] [×]        │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐    │
│  │ Új kérdés…                                            │    │
│  └──────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────┘
```

**Komponensek:** `SearchInput`, `AnswerCard`, `SourceCitation`,
`ExtractedSlotChips`, `SavedSearchSaveButton`.

**State:** kérdés-history sessionStorage-ben tartva (LocalOnly elv — nem
localStorage, ne maradjon a logout után).

### 8.4 Tasks (`/tasks`)
- Kanban (`Suggested` / `Open` / `InProgress` / `Done`) vagy lista nézet.
- Filter: assignedTo (családtag), priority, hasDueDate, origin
  (`AiSuggested` vs. `Manual`).
- A `Suggested` oszlop sárga sávval, batch-Approve gombbal.

### 8.5 Deadlines (`/deadlines`)
- Naptár-nézet (`@angular/cdk/calendar`) + lista nézet.
- Filter: category, responsible, status.
- Egy deadline-card kibontva: kapcsolódó `Reminder`-ek, default policy
  visualizáció ("7 nap előtt InApp + Email").

### 8.6 Reminders (`/reminders`)
- Csoportosított feed (lásd `reminder-engine.md` 10):
  - Most esedékes
  - A héten
  - Később
  - Lecsúszott
- Kártya akciók: `Kész` / `Halaszt 1h / 4h / holnap reggel` / `Új idő` /
  `Delegálom` / `Elvetem`.

### 8.7 Topics (`/topics`)
- Tree-view a topic-hierarchiáról.
- Topic-click → mini-dashboard az adott topic-hoz tartozó tartalomról.
- Új altopic csak admin (a fa kontroll alatt marad).

### 8.8 Family (`/family`) — admin only
- Családtagok listája + szerkesztés.
- `Add user account` flow: meghívó email a Google-fiókhoz.
- Szerepkör módosítás, soft delete.

### 8.9 Suggestions (`/suggestions`)
- AI által javasolt entitások **inbox-szerű** áttekintése, ha valaki
  egy ülésben akarja elfogadni / elvetni mindet.
- Csoportosítva: Task / Deadline / Tag / Topic / Facet.
- Per-row Approve / Reject; batch action a fejléc kijelölésével.

### 8.10 Settings (`/settings`)
- **Saját:** Display name, quiet hours, notification preferences
  (email enabled, escalation opt-out).
- **Admin:** AI provider config (LocalOnly / Hybrid / AnyProvider, default
  LocalOnly), Gmail integráció hozzáadás, SMTP konfiguráció, backup
  útmutató link, audit log link.

---

## 9. Megosztott komponensek (`shared/ui`)

### 9.1 UI primitívek

- `ui-button` (variants: `primary`, `ghost`, `danger`, `warning-suggested`).
- `ui-card`, `ui-card-header`, `ui-card-actions`.
- `ui-badge` (status pills: Open, Suggested, Approved, Failed).
- `ui-dialog`, `ui-sheet` (CDK overlay alapján).
- `ui-skeleton` loading state-re.
- `ui-empty-state` (üres lista magyar segítő szöveggel).

### 9.2 Suggestion overlay

Egy dedikált `SuggestionBlock` komponens, amelyet bármely entitás-card-ra
rá lehet húzni — felül egy sárga sáv „AI javasolta" felirattal, mellette
`Elfogadom` / `Elvetem` gombokkal. Ezt használja:
- DocumentDetail (a doksiból AI által kinyert Tag/Topic/Facet),
- TasksList (a `Suggested` státusz),
- DeadlinesList (az `Origin=AiSuggested`).

### 9.3 Pipe-ok

- `huDate` — magyar dátumformat (`2026. szept. 14.`).
- `huRelativeDate` — „3 nap múlva", „2 hete".
- `fileSize` — KB / MB / GB magyar.
- `documentIcon` — MIME-szerinti ikon.

---

## 10. Styling és theming

### 10.1 Tailwind

- `tailwind.config.ts`:
  - magyar tipográfia: `Inter` mint sans, `Source Serif Pro` mint serif
    (opcionális olvasáshoz).
  - design tokens: `primary` (kékes), `warn` (borostyán a Suggestions-höz),
    `danger` (piros), `success` (zöld).
- Sötét/világos téma a CSS variable-eken — `prefers-color-scheme` +
  manual toggle a Settings-ben.

### 10.2 Mobil reszponzív

- Breakpointok: `sm < 640px` (mobil), `md < 1024px` (tablet),
  `lg ≥ 1024px` (desktop).
- Mobile shell: bottom nav (5 ikon: Dashboard, Search, Suggestions,
  Reminders, More). Desktop shell: bal sidebar minden feature-rel.

---

## 11. i18n

- `@angular/localize` magyar (`hu`) default.
- Forrás fájl: `src/locale/messages.hu.xlf` (vagy `assets/i18n/hu.json`
  egyszerűbb runtime megoldással `@ngx-translate/core`-ral — MVP-ben
  utóbbi gyorsabb a fejlesztésnél, később `@angular/localize`-ra
  migrálható).
- Konvenció: a kódban magyar `id`-k, fordítás kulcsai pl.
  `documents.upload.title`. Angol fallback nem cél MVP-ben.
- Dátum/szám/valuta lokalizáció: `Intl.DateTimeFormat('hu-HU')`,
  `Intl.NumberFormat('hu-HU', { style: 'currency', currency: 'HUF' })`.

---

## 12. Offline és LAN-detektálás

A LAN-only döntés (ADR-0003) alapján:

- A kliens minden 60 másodpercben pingol egy
  `GET /api/v1/system/heartbeat`-et.
- Ha 3 egymást követő ping fail (vagy `ECONNREFUSED`), a UI „Nem vagy
  otthon" képernyőre vált:
  ```
  Family OS csak az otthoni hálózaton érhető el.
  Csatlakozz a háztartási Wi-Fi-re, és próbáld újra.
  [Újrapróbálás] [Kijelentkezés]
  ```
- Cache: az utoljára betöltött dashboard adatok read-only nézetben
  elérhetők (read-from-storage), de minden írási művelet tiltva.

Push notification és service worker MVP-ben **nincs** — a PWA-szerű
offline funkcionalitást a v2 tervezi.

---

## 13. Auth flow UI

### 13.1 Login (`/login`)
- Egyetlen Google bejelentkezés gomb.
- Háttér: röviden mit csinál a Family OS, ki látja az adatokat.
- A redirect után a backend állít be HttpOnly cookie-t és a frontend
  `/api/v1/auth/me`-ből olvassa a current user-t.

### 13.2 Logout
- `POST /api/v1/auth/logout` → cookie törlés + redirect `/login`-ra.
- A `RealtimeService.stop()` lezárja a SignalR-t; `localStorage`-ben
  a non-szenzitív UI preferenciák megmaradnak.

---

## 14. Tesztelés

### 14.1 Unit / komponens (Vitest + Angular Testing Library)
- Minden facade `select`-jét: input state → output computed.
- Minden komponens render snapshot + key interakciók (klikk, form submit).
- Pipe-okat különálló unit-tesztben.

### 14.2 E2E (Playwright)
- Smoke tesztek minden főoldal megnyitásra (Auth mockolt, backend
  Testcontainers).
- Use case-szintű tesztek a `product-vision.md` UC-01…UC-08-ra.
- `@security` címkés tesztek a `security-privacy.md` 13.2 szerint.

### 14.3 Vizuális regresszió
- Playwright snapshot a dashboard, document-card, suggestion-block UI-ra.
- Nightly futás CI-ben.

---

## 15. Kotlin Multiplatform (jövőbeli, nem MVP)

Nagy vonalakban — részletek később külön doksiban:

- **Cél:** Android-first natív kliens (Compose Multiplatform UI-jal),
  iOS megoldás opció.
- **Megosztott modul:** `core-domain` — DTO-k, API-kliens (kotlinx.serialization
  + Ktor), auth handler. A backend OpenAPI sémájából generálva
  (`openapi-generator` Kotlin client).
- **Notification:** native push (FCM) — v2.
- **Offline:** lokális Room cache + lefotózott dokumentumok queue-ja;
  ha otthon, sync. Az ADR-0003 LAN-only kapu marad: kívülről csak a
  „nem vagy otthon" képernyő mutatható.
- **Auth:** Google OAuth natív SDK (Android) + Family OS backend cookie.

---

## 16. Korlátok és nyitott kérdések

- **Dokumentum-előnézet (PDF rendering):** MVP-ben `pdf.js` cliens-oldali
  rendering. Nagyon nagy PDF-eknél lassú lehet — később server-side
  thumbnail-generálás (`Magick.NET`) megfontolandó.
- **Konfliktus-feloldás:** ha két user egyszerre szerkeszti ugyanazt a
  rekordot, a backend 409-et ad (`RowVersion`); a UI „valaki más is
  módosította, kérlek frissítsd" modal-t mutat. Merge UI nincs.
- **Drag-and-drop bulk upload:** támogatott, de a 20+ fájl egyidejű
  feltöltés tesztelése és UI feedback finomítása későbbi feladat.

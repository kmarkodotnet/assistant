# ADR-0011 — Természetes nyelvű parancsok (LLM tool-calling) végrehajtási modellje

- Státusz: Elfogadva (architect fázis, CR260710-07)
- Dátum: 2026-07-11
- Döntéshozó: architect agent (orchestrátor felügyelet alatt)
- Kapcsolódó: [CR260710-07](../change-requests/cr260710-07-termeszetes-nyelvu-parancsok.md),
  [ai_features.md §4.4](../ai_features.md), [ai-pipeline.md §11](../ai-pipeline.md),
  [api-design.md §16.3](../api-design.md), ADR-0009 (reminder-generálás)

## Kontextus

A CR260710-07 három whitelistelt tool-t vezet be (`create_reminder`,
`assign_document`, `add_tag`), amelyeket az LLM egy chat-üzenetből *javasol*,
és a felhasználó explicit megerősítése után a backend hajt végre. Négy
nyitott architektúra-kérdést kell eldönteni a párhuzamos fejlesztés előtt,
mert mindegyik érinthet migrációt, biztonságot vagy meglévő kontraktot:

1. **Hol tároljuk a megerősítésre váró javaslatot?** (proposal store)
2. **Warranty-gap:** a `create_reminder` XOR-t vár (`TaskId` VAGY
   `DeadlineId`), de a `Reminder` entitásnak nincs FK-ja `Warranty`-ra,
   miközben a minta-utasítás épp *"a garancia lejárta előtt"* szól.
3. **`add_tag` scope:** a `POST /documents/{id}/tags` jelenleg 501 stub
   (T-CBE-17), nincs mögötte command/handler.
4. **Auditálás:** hogyan teljesül a "ki, mikor, milyen tool-t, milyen
   paraméterekkel" AC a meglévő `AuditBehavior` mellett.

## Döntés

### D1 — Állapotmentes, aláírt javaslat-token (nincs proposal-tábla, nincs migráció)

A megerősítésre váró tool-javaslatot **nem** perzisztáljuk adatbázisban.
A `POST /api/v1/search` (Command mód) a feloldott (resolved) javaslatot egy
rövid életű, **HMAC-aláírt, base64url borítékként** (`proposalToken`) adja
vissza a kliensnek. A boríték tartalma:

```
{ "v":1, "tool":"create_reminder", "args": { ...resolved... },
  "uid":"<userAccountId>", "iat": <utc>, "exp": <iat+600s>, "sig":"<hmac>" }
```

A megerősítő endpoint a tokent visszakapja, ellenőrzi az aláírást, a lejáratot
(`exp`, alap 10 perc), és hogy a `uid` == az aktuális bejelentkezett user.
Csak ezután hajt végre. **Indok:** MVP-ben (prioritás C) elkerüli az új
táblát + migrációt, nincs "árva javaslat" GC-igény, és a tokent nem lehet
más userre lejátszani. A titkos kulcs env-változóból jön
(`TOOLCALL_SIGNING_KEY`), soha nem kerül repóba.

### D2 — Warranty-reminder: szintetizált AiSuggested Deadline (NINCS séma-változás)

A `Reminder` séma **változatlan** marad (XOR Task/Deadline). Ha a parancs egy
garancia lejáratára hivatkozik (nincs hozzá Deadline), a `create_reminder`
tool **kétlépéses, láncolt** végrehajtást javasol egyetlen megerősítés mögött:

1. `CreateDeadlineCommand` — a `Warranty.WarrantyEndDate` → `DueDateUtc`,
   `Category = Other` (nincs dedikált Warranty kategória),
   `Description` = "Garancia lejárat: {ProductName}", a warranty
   `SourceDocumentId`/`RelatedFamilyMemberId` átörökítve. `Origin` a
   command-handler szerint `Manual` — de a UI-summary jelzi, hogy AI-javasolt.
2. `CreateReminderCommand(DeadlineId = <új deadline>, TriggerUtc =
   DueDateUtc - offsetDays)`.

Mindkét command az `ISender.Send`-en megy át, így az `AuditBehavior`
mindkettőt automatikusan naplózza. **Indok:** hű az ADR-0009 elvhez
("javaslatból jóváhagyás nélkül nincs ütemezett művelet") — a Deadline is
csak a felhasználó megerősítése után jön létre; nincs migráció; a `Reminder`
egyértékű anchor-modellje sértetlen. **Elvetett alternatíva:** `WarrantyId`
FK a `Reminder`-re — migrációt és XOR→3-utas validáció-átírást igényelne,
aránytalan egy C-prioritású feature-höz.

### D3 — `add_tag` scope-ba kerül: minimális `AddDocumentTagCommand`

Az `add_tag` **marad** a whitelistben. Bevezetünk egy minimális
`AddDocumentTagCommand(Guid DocumentId, Guid TagId)`-t, ami 1:1 az
`AddDocumentTopicCommand` mintáját követi (auth `CanWriteDocument`, tag-lét
ellenőrzés, dedup, `DocumentTag { Origin = Manual, IsApproved = true }`).
Ez egyúttal lezárja a T-CBE-17 501-stubot (`POST /documents/{id}/tags`).
A tool csak **létező** tag-re hivatkozhat: a resolve lépés a `tagName`-et
case-insensitive feloldja `Tag.Id`-re; ha nincs találat → a javaslat
figyelmeztetést kap és nem megerősíthető (MVP-ben az LLM nem hoz létre új
tag-et). **Indok:** kicsi, jól határolt kiegészítés, valódi terméki értéket
ad (stub megszűnik), és nem tágítja az LLM írási felületét.

### D4 — Kétszintű audit: dedikált tool-nyom + entitás-nyom

A megerősítő command (`ConfirmToolCallCommand`) `[NoAudit]` attribútumot kap,
és a handler **explicit** `IAuditLogger.LogAsync(AuditAction.Approve, ...)`
bejegyzést ír `entityType = "ToolCall:<toolName>"`, `detailsJson =
{ toolName, resolvedArgs }` tartalommal — ez a "milyen tool-t, milyen
paraméterekkel" nyom. A tényleges hatást a láncolt üzleti command-ok
(`CreateReminderCommand` stb.) `AuditBehavior`-a naplózza (`Create`/`Update`,
entitás-ID-vel). **Indok:** a két nyom együtt fedi le a teljes AC-t, és nem
kell módosítani az `AuditBehavior` névkonvenció-logikáját.

## Következmények

- **Új Application-absztrakció:** `ITool`, `IToolRegistry`,
  `ToolExecutionContext`, `ToolResolution`, `ToolResult` (kontrakt:
  ai-pipeline.md §11.1).
- **Nincs DB-migráció** ehhez a feature-höz (D1 + D2 következménye).
- `AddDocumentTagCommand` + handler + a `POST /documents/{id}/tags` valódi
  implementáció (a 501 stub helyett).
- `SearchResponse` bővül egy opcionális `toolCallProposal` mezővel; új
  `SearchMode.Command`; új `POST /api/v1/tool-calls/confirm` és
  `/tool-calls/reject` endpoint (api-design.md §16.3).
- Env: `TOOLCALL_SIGNING_KEY` (kötelező, ha a Command mód engedélyezett),
  `TOOLCALL_PROPOSAL_TTL_SECONDS` (alap 600).
- Rate limit: a Command mód a Q&A-val közös 10 req/perc/user keret alatt.
- Elutasításkor semmilyen adatváltozás; a `reject` endpoint csak egy
  `AuditAction.Reject` nyomot ír (a §4.5 feedback-tanuláshoz később hasznos).

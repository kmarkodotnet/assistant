# Implementációs kontextus-mátrix (Sonnet)

> Státusz: v0.1 · Dátum: 2026-06-26 · Cél: a megvalósító agentek (sonnet)
> kontextus-betöltésének optimalizálása.
> Kapcsolódó: [mvp-backlog.md](mvp-backlog.md), [implementation-plan.md](implementation-plan.md),
> [CLAUDE.md](../CLAUDE.md) (modellrouting)

---

## Cél

Sonnet-szintű implementációs feladatoknál a **kontextus-választás** a
minőség egyik döntő tényezője. Ha egy dokumentum nincs betöltve, a sonnet
nem tudja a kontraktust követni; ha minden be van töltve, a fontos jelek
elvesznek a zajban és a token-költség is felesleges.

Ez a mátrix arra ad választ: **egy adott epic megvalósításakor mely
dokumentumokat kell betölteni a sonnet kontextusába, és milyen mélységig.**

A mátrix az `mvp-backlog.md`-ben rögzített 13 epicet (A–M) leképezi a 13 fő
tervezési dokumentumra + 4 ADR-re.

---

## Jelölés

| Szint | Jelentés | Mit kell betölteni |
|---|---|---|
| **★★★** | Elsődleges — a feladat alapja | a teljes dokumentum |
| **★★** | Erősen releváns — meghatározott szakaszok | csak a hivatkozott szakaszok |
| **★** | Konzultáció szintű — referencia | TOC + a hivatkozott pont szükség szerint |
| **—** | Nem releváns | nem kell betölteni |

---

## Mindig betöltendő alapcsomag (baseline)

Minden epic indításakor a sonnet-nek az alábbi minimumot kell látnia,
függetlenül attól, melyik feature van soron:

| Dokumentum | Mit | Miért |
|---|---|---|
| `CLAUDE.md` | FULL | factory orchestráció, modellrouting, minőségi kapuk |
| `coding-standards.md` | FULL | naming, hibakezelés, tesztelés, tilos minták |
| `implementation-plan.md` | csak az aktuális fázis szakasza | DoD, fájllista, kockázat |
| `product-vision.md` | §1, §3, §4, §5 | vízió + szerepkörök + UC-k + non-goal-ok |
| `docs/contracts/` (ha létezik) | aktuális kontraktusok | szigorúan kötelező |

Ez ~12-15k token. Marad bőven hely a feature-specifikus dokumentumoknak.

---

## Mátrix — Epicek × Dokumentumok

A táblázat csillagai a mélység-jelzők; a következő szakaszban epicenként
fel van bontva, mely szakaszok kellenek a ★★ szinten.

| Epic | domain | db-schema | arch | ai-pipe | search | reminder | sec-priv | front | api | ADR-1 | ADR-2 | ADR-3 | ADR-4 |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| **A — Alapok és infra** | ★ | ★★ | ★★★ | — | — | — | ★★ | ★ | ★★ | — | — | ★ | — |
| **B — Felhasználó-kezelés** | ★★ | ★★ | ★ | — | — | ★ | ★★★ | ★ | ★★ | — | — | — | — |
| **C — Dokumentum-kezelés** | ★★ | ★★ | ★★ | ★ | — | — | ★★ | ★★ | ★★★ | — | — | — | — |
| **D — AI pipeline** | ★★ | ★★ | ★★★ | ★★★ | ★ | ★ | ★★ | ★ | ★ | ★★★ | ★★★ | — | — |
| **E — Kereső + Q&A** | ★★ | ★★ | ★★ | ★★ | ★★★ | — | ★★ | ★★ | ★★ | ★★ | — | — | — |
| **F — Task + Deadline** | ★★★ | ★★ | ★ | ★ | — | ★★ | ★★ | ★★ | ★★★ | — | — | — | — |
| **G — Reminders** | ★★ | ★★ | ★★ | ★ | — | ★★★ | ★★ | ★★ | ★★ | — | — | — | — |
| **H — Notes** | ★★ | ★★ | ★ | ★★ | ★★ | — | ★★ | ★★ | ★★ | — | — | — | — |
| **I — Tag + Topic** | ★★ | ★★ | ★ | ★ | ★ | — | ★ | ★★ | ★★ | — | — | — | — |
| **J — Audit + admin** | ★★ | ★★ | ★ | — | — | — | ★★★ | ★ | ★★ | — | — | — | — |
| **K — Beállítások + integrációk** | ★ | ★ | ★★ | ★ | — | ★ | ★★★ | ★★ | ★★ | — | — | ★ | ★★★ |
| **L — Dashboard** | ★ | ★ | ★ | — | ★★ | ★★ | ★ | ★★★ | ★★ | — | — | — | — |
| **M — Deploy + ops** | — | ★ | ★★★ | — | — | ★ | ★★★ | ★ | ★ | — | — | ★★★ | ★ |

Rövidítések: `domain` = domain-model.md · `db-schema` = database-schema.md ·
`arch` = architecture.md · `ai-pipe` = ai-pipeline.md ·
`search` = search-strategy.md · `reminder` = reminder-engine.md ·
`sec-priv` = security-privacy.md · `front` = frontend-structure.md ·
`api` = api-design.md.

---

## Epicenkénti részletes felbontás

A ★★ szintű dokumentumokhoz konkrét szakasz-hivatkozások (mit kell betölteni).
A ★★★ szintű dokumentumokat teljes egészében kell betölteni.

### Epic A — Alapok és infra (story-k: A1–A5)

| Dok | Szint | Mit kell olvasni |
|---|---|---|
| architecture.md | ★★★ | FULL — solution layout, layer felelősségek, cross-cutting |
| api-design.md | ★★ | §1 (konvenciók), §3 (Auth), §4 (System), §1.3 (ProblemDetails) |
| security-privacy.md | ★★ | §3 (Authentication), §4 (Authorization), §11.3 (logok), §5 (audit alap) |
| database-schema.md | ★★ | §1 (env), §2 (enumok), §4.1–4.2 (family_member, user_account) |
| domain-model.md | ★ | §1.1 (UserAccount), §1.2 (FamilyMember) |
| frontend-structure.md | ★ | §13 (Auth flow UI), §3 (Routing) — csak A3/A4-hez |
| ADR-0003 | ★ | CORS / LAN allowlist konfighoz |

**Tipikus betöltés:** ~25-30k token.

---

### Epic B — Felhasználó-kezelés (B1–B3)

| Dok | Szint | Mit kell olvasni |
|---|---|---|
| security-privacy.md | ★★★ | FULL — RBAC, IsPrivate, allowlist, audit |
| api-design.md | ★★ | §5 (Family), §6 (UserAccounts), §3 (Auth újra) |
| domain-model.md | ★★ | §1.1, §1.2 |
| database-schema.md | ★★ | §4.1, §4.2, §2 (user_role enum) |
| reminder-engine.md | ★ | §5.4 (felhasználói preferenciák) — B3-hoz |
| architecture.md | ★ | §3.5 (Api), §8.1 (auth policy) |
| frontend-structure.md | ★ | §13 (Auth), §8.8 (Family page) |

**Tipikus betöltés:** ~25k token.

---

### Epic C — Dokumentum-kezelés (C1–C5)

| Dok | Szint | Mit kell olvasni |
|---|---|---|
| api-design.md | ★★★ | FULL — §7 dokumentumok (a teljes szakasz), §1, §23 (példa) |
| frontend-structure.md | ★★ | §8.2 (Documents oldalak), §2 (folder layout) |
| domain-model.md | ★★ | §1.3 (Document), §1.4 (DocumentText), §1.17–1.19 (facets) |
| database-schema.md | ★★ | §4.5 (document), §4.6 (document_text + új mezők v0.2), §4.18–4.20 (facets) |
| security-privacy.md | ★★ | §7 (Fájl-tárolás), §4 (Authorization), §9 (input validáció), §5 (audit) |
| architecture.md | ★★ | §7 (Tárolás), §11.1 (upload flow) |
| ai-pipeline.md | ★ | §3.1 (Bemenet rögzítés) — C1-hez |

**Tipikus betöltés:** ~35-40k token. Nagy epic.

---

### Epic D — AI pipeline (D1–D11) — KRITIKUS

Ez a legnagyobb és legrétegzettebb epic. A 11 story-t két alcsomagra
érdemes bontani:

**D-Infra alcsomag (D1, D2, D3, D4):** AI provider absztrakció, Hangfire,
szövegkinyerés, nyelvdetekt.

| Dok | Szint | Mit kell olvasni |
|---|---|---|
| ai-pipeline.md | ★★★ | FULL |
| architecture.md | ★★★ | FULL — különösen §3.4 (Infra.Ai), §4 (interfészek), §5 (provider), §6 (queue) |
| ADR-0001 (pgvector) | ★★★ | FULL — embedding tárolás döntése |
| ADR-0002 (Tesseract) | ★★★ | FULL — OCR döntése |
| security-privacy.md | ★★ | §8 (AI privacy guard, PrivacyMode kapu), §5.3 (mit NEM logolunk) |
| domain-model.md | ★★ | §1.15 (AiProcessingJob), §1.3–1.5 (Document/Text/Chunk) |
| database-schema.md | ★★ | §4.7 (chunk), §4.16 (ai_processing_job), §2 (enumok) |
| coding-standards.md | (baseline FULL) | különös figyelem §8 (tesztelés), §16 (titkok) |

**Tipikus betöltés:** ~50-55k token.

**D-Tartalom alcsomag (D5–D11):** Summary, classify, deadlines, tasks,
facet, embedding, orchestráció. Három párhuzamos worktree-re bontható
(`feature/ai-summary`, `feature/ai-extract`, `feature/ai-embed`).

| Dok | Szint | Mit kell olvasni |
|---|---|---|
| ai-pipeline.md | ★★★ | FULL — különösen §3.4–3.10, §4 (prompt template-ek) |
| domain-model.md | ★★ | §1.6 (Summary), §1.7 (Tag), §1.8 (Topic), §1.10 (Task), §1.11 (Deadline), §1.17–1.19 (facets), §1.5 (DocumentChunk) |
| database-schema.md | ★★ | §4.7, §4.8, §4.9, §4.10, §4.13, §4.14, §4.18–4.20 |
| architecture.md | ★★ | §4.1 (AI interfészek), §6 (queue), §11.1 (orchestráció) |
| security-privacy.md | ★★ | §8 (PrivacyMode), §5.3 (mit nem logolunk) |
| frontend-structure.md | ★ | §6 (SignalR), §9.2 (SuggestionBlock) — csak D11-hez |
| ADR-0001 | ★★★ | FULL — D10-hez (embedding) |

**Tipikus betöltés worktree-nként:** ~40-45k token.

---

### Epic E — Kereső + Q&A (E1–E7)

| Dok | Szint | Mit kell olvasni |
|---|---|---|
| search-strategy.md | ★★★ | FULL |
| api-design.md | ★★ | §16 (Search) |
| ai-pipeline.md | ★★ | §4.6 (Q&A prompt template) |
| domain-model.md | ★★ | §1.3, §1.5, §1.6, §1.9 (forrásentitások) |
| database-schema.md | ★★ | §4.5–4.8, §4.12, §1.3 (FTS config) |
| security-privacy.md | ★★ | §4 (RBAC a kereső-szűréshez) |
| architecture.md | ★★ | §4.1 (ISemanticSearchService, IQuestionAnswerService) |
| frontend-structure.md | ★★ | §8.3 (AI Search UI) |
| ADR-0001 | ★★ | embedding modell + dimenzió konzisztencia |

**Tipikus betöltés:** ~40-45k token.

---

### Epic F — Task + Deadline (F1–F3)

| Dok | Szint | Mit kell olvasni |
|---|---|---|
| domain-model.md | ★★★ | FULL — különösen §1.10 (Task), §1.11 (Deadline), §0 (Origin/Approval), §4 (Task vs Deadline indoklás) |
| api-design.md | ★★★ | FULL — §11 (Tasks), §12 (Deadlines), §15 (Suggestions batch) |
| database-schema.md | ★★ | §4.13, §4.14, §2 (task/deadline enumok) |
| reminder-engine.md | ★★ | §3.10 (default reminder policy minden deadline-hoz) — F2-hez |
| frontend-structure.md | ★★ | §8.4, §8.5, §8.9 (Suggestions inbox) |
| security-privacy.md | ★★ | §4 (RBAC), §5 (audit Approve/Reject) |
| ai-pipeline.md | ★ | §5 (Suggestion → Approved állapotgép) |
| architecture.md | ★ | §11.1 (a feldolgozás végén suggestion-ök) |

**Tipikus betöltés:** ~35k token.

---

### Epic G — Reminders (G1–G6) — KRITIKUS

| Dok | Szint | Mit kell olvasni |
|---|---|---|
| reminder-engine.md | ★★★ | FULL — az epic vezérlő dokumentuma |
| domain-model.md | ★★ | §1.12 (Reminder), §1.10–1.11 (parent entitások) |
| database-schema.md | ★★ | §4.15 (reminder + Cancelled enum v0.2), §4.17.1 (notification_feed v0.2), §2 |
| architecture.md | ★★ | §3.6 (Workers), §6.4 (recurring dispatcher), §11.3 (catch-up flow) |
| api-design.md | ★★ | §13 (Reminders), §14 (Notifications), §22 (SignalR) |
| frontend-structure.md | ★★ | §7 (Notification feed + toast), §8.6 (Reminders oldal) |
| security-privacy.md | ★★ | §10.2 (SMTP), §5 (audit a tüzeléshez) — G5-höz |

**Tipikus betöltés:** ~40k token.

---

### Epic H — Notes (H1–H2)

Az H epic párhuzamos a Documents pattern-nel — kevesebb új koncepció,
de a chunkolás analóg.

| Dok | Szint | Mit kell olvasni |
|---|---|---|
| domain-model.md | ★★ | §1.9 (Note) |
| database-schema.md | ★★ | §4.12 (note + note_chunk + join táblák) |
| api-design.md | ★★ | §8 (Notes) |
| ai-pipeline.md | ★★ | §3.9 (embedding) — analóg dokumentum-chunkolással |
| search-strategy.md | ★★ | §2.2 (FTS), §2.3 (vektor) — note_chunk is része a hibrid keresőnek |
| frontend-structure.md | ★★ | §8.2 (analóg dokumentumokkal) |
| security-privacy.md | ★★ | §4 (IsPrivate Note-on), §9.2 (XSS, markdown sanitize) |
| architecture.md | ★ | §3 (réteg-szerep) |

**Tipikus betöltés:** ~25-30k token.

---

### Epic I — Tag + Topic (I1–I2)

| Dok | Szint | Mit kell olvasni |
|---|---|---|
| domain-model.md | ★★ | §1.7 (Tag), §1.8 (Topic) |
| database-schema.md | ★★ | §4.9 (tag), §4.10 (topic), §4.11 (join), §5.1 (seed topic-fa) |
| api-design.md | ★★ | §9 (Tags), §10 (Topics) |
| frontend-structure.md | ★★ | §8.7 (Topics oldal), §9 (megosztott UI: tag multiselect) |
| search-strategy.md | ★ | §2.1 (filter by tag/topic) |
| ai-pipeline.md | ★ | §3.4 (osztályozó: új tag létrehozhat, új topic NEM) |
| security-privacy.md | ★ | §4 (admin only topic, adult tag) |

**Tipikus betöltés:** ~25k token.

---

### Epic J — Audit + admin (J1–J4)

| Dok | Szint | Mit kell olvasni |
|---|---|---|
| security-privacy.md | ★★★ | FULL — különösen §5 (Audit log) |
| domain-model.md | ★★ | §1.16 (AuditLog) |
| database-schema.md | ★★ | §4.17 (audit_log + insert-only trigger), §2 (audit_action enum + ExternalApiCall) |
| api-design.md | ★★ | §18 (AI processing admin), §19 (Audit log), §6 (UserAccounts) |
| architecture.md | ★ | §8 (cross-cutting), §12 (admin felület) |
| frontend-structure.md | ★ | §1.1 (admin route), §8.10 (Settings) |

**Tipikus betöltés:** ~30k token.

---

### Epic K — Beállítások + integrációk (K1–K3)

| Dok | Szint | Mit kell olvasni |
|---|---|---|
| security-privacy.md | ★★★ | FULL — különösen §6 (encryption), §8 (AI privacy), §10 (külső integrációk), §11.2 (backup) |
| ADR-0004 (Gmail API) | ★★★ | FULL — K1-hez |
| api-design.md | ★★ | §17 (Sources), §18 (AI providers), §21 (Settings) |
| architecture.md | ★★ | §3.4 (Infra.Ai), §5 (AI provider absztrakció), §7 (storage backup) |
| frontend-structure.md | ★★ | §8.10 (Settings page) |
| ai-pipeline.md | ★ | §1 (vezérlőelvek — privacy mód) |
| ADR-0003 (LAN-only) | ★ | K3 backup off-site stratégiához |
| reminder-engine.md | ★ | §5.2 (SMTP konfig) — K3 nem érinti, de K1 SMTP relay K3-mal együtt |

**Tipikus betöltés:** ~35k token.

---

### Epic L — Dashboard (L1–L2)

| Dok | Szint | Mit kell olvasni |
|---|---|---|
| frontend-structure.md | ★★★ | FULL — különösen §8.1 (Dashboard wireframe) |
| api-design.md | ★★ | §20 (Dashboard) |
| search-strategy.md | ★★ | §10 (UI integráció: mentett keresések widget) |
| reminder-engine.md | ★★ | §10 (UI: lecsúszott összesítő) |
| domain-model.md | ★ | rövid áttekintés, ha bizonytalan |
| database-schema.md | ★ | §8 (méretezés — performance budget) |

**Tipikus betöltés:** ~25-30k token.

---

### Epic M — Deploy + ops (M1–M4)

| Dok | Szint | Mit kell olvasni |
|---|---|---|
| architecture.md | ★★★ | FULL — különösen §9 (Docker Compose topológia), §12 (megfigyelhetőség) |
| security-privacy.md | ★★★ | FULL — különösen §6 (encryption), §7.6 (lemez), §11 (üzemeltetés), §13 (red team) |
| ADR-0003 (LAN-only) | ★★★ | FULL — minden M-story érinti |
| ADR-0004 (Gmail API) | ★ | K1-mel együttműködik az M-fázisban |
| reminder-engine.md | ★ | §12 (konfiguráció minta) |
| database-schema.md | ★ | §1 (env), §7 (backup hivatkozás) |
| api-design.md | ★ | §4 (System healthz) |
| frontend-structure.md | ★ | §12 (LAN-detection a kliensoldalon) |

**Tipikus betöltés:** ~35k token.

---

## Sonnet-specifikus minőség-tippek

A sonnet jól dolgozik a fent megadott terjedelmekben (~25-50k input
+ baseline), de a hibrid implementáció minőségét néhány konkrét gyakorlat
emeli:

### 1. Kontextus rögzítése a feladat indításakor

A sonnet feladat-promptjának *legelején* legyen egy explicit lista a
betöltött dokumentumokról, pl.:

> Az alábbi tervezési dokumentumokra építkezel: `coding-standards.md`,
> `architecture.md` (FULL), `api-design.md` §7, `domain-model.md` §1.3,
> `database-schema.md` §4.5–4.6, `security-privacy.md` §7. Bármely
> ezekkel ellentmondó döntés előtt állj meg és jelezd.

Ez a sonnet **anchoring**-ját kalibrálja: nem fog kitalálni dolgokat,
amelyek a doksiban szerepelnek.

### 2. Eltérés-protokoll

Ha a sonnet úgy érzi, hogy a doksi nem fedi a konkrét döntést (pl.
egy mező adat-típusa nem szerepel a sémában), **ne** találjon ki — adjon
vissza egy „nyitott kérdés" listát az `architect` agentnek
(opus eszkaláció a CLAUDE.md szerint).

### 3. Kontraktus-doksi prioritás

Ha párhuzamos worktree-k vannak (Fázis 8 és 10), a `docs/contracts/`
mappa **mindig** be van töltve a baseline-be. Egy adott worktree
nem módosíthatja a kontraktust — csak az architect.

### 4. „Negatív kontextus"

A sonnet hajlamos a *teljes mást* is felhasználni („szépítés"). A
nem-célokat (`product-vision.md` §5, az adott epic explicit non-goal
listája) szándékosan a baseline-ben tartjuk, hogy ne építsen olyan
funkciókat, amelyek nem kellenek.

### 5. Tokenbudget rule of thumb

| Csomag | Méret | Marad action-ra |
|---|---|---|
| Baseline (~13k) + epic ★★★/★★ (~25k) | ~38k | ~160k a kódra |
| Baseline + nagy epic (D, G) ~50k | ~63k | ~135k |
| Két párhuzamos doksi (worktree-átfedés) | ~75k | ~120k |

Az `mvp-backlog.md` és `implementation-plan.md` általában **NEM** kell
teljesen — csak a releváns story és fázis-szakasz. Ez egy gyakori
„zaj-forrás" tipikus rosszul kalibrált promptokban.

### 6. Mit ne tölts be alapból

- A többi epic doksi-csomagját (irreleváns zaj).
- Az ADR-eket, amelyek nem szerepelnek a mátrixban (a többi 4 ADR a
  tervezés-során vagy egyszer-egyszer kell, nem futtatás-szinten).
- A teljes `coding-standards.md`-t, ha csak frontend-feature van soron
  — ekkor a §9 (Angular), §10 (FE mappastruktúra), §11 (endpoint NEM)
  + §12 (tooling) elég, a többi C# rész kihagyható.

---

## A code-reviewer agent kontextusa (külön)

A `code-reviewer` agent (opus) **nem ugyanazt a csomagot kapja**, mint a
sonnet implementer — neki egy teljesebb keresztmetszet kell, mert a
review keresztkapcsolatokat ellenőriz:

- **Mindig:** `coding-standards.md` §17 (review checklist), a feladathoz
  vonatkozó ★★★ és ★★ szintű doksik (a fenti mátrix szerint), valamint
  a `security-privacy.md` minden olyan szakasza, ami az adott epicben
  szerepel.
- **Kötelező plusz a sonnet csomaghoz képest:** `security-privacy.md` §17
  (privacy assertions), `api-design.md` §1.3 (ProblemDetails), `domain-model.md`
  §0 (közös konvenciók).

Ez a `code-reviewer` agent saját routing-szabálya — a sonnet implementálónak
nem kell foglalkoznia vele.

---

## Karbantartás

Ez a mátrix **élő dokumentum**: amikor új tervezési doksi, ADR vagy
jelentős szakasz kerül be, a megfelelő epicek sorát frissíteni kell. A
`factory-engineer` agent (CLAUDE.md szerint) `/retro` után erre is
jegyzetet készít, ha rendszerszerű mismatch látszik (pl. egy ★ szintű
doksit túl gyakran kérdez vissza a sonnet — akkor ★★-re kell emelni).

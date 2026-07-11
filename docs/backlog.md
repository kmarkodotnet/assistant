# Backlog

A gyártás feature-listája függőségi gráffal. A párhuzamosítható munkacsoportokat
külön jelöljük. A `PARALLEL` blokkokon belüli tételek egyszerre indíthatók
(külön git worktree / subagent), amint a közös **kontrakt** kész (ARCH fázis).

---

## FEAT-DIGEST — Proaktív napi összefoglaló

- **Forrás:** [CR260710-02](change-requests/cr260710-02-proaktiv-napi-osszefoglalo.md)
- **Kontrakt (szerződés):** [daily-digest-contract.md](contracts/daily-digest-contract.md)
- **Döntés:** [ADR-0011](decisions/ADR-0011-daily-digest-backgroundservice.md)
- **Státusz:** Tervezve (kontrakt kész → BUILD indítható)
- **Prioritás:** S (Should) — CR szerint
- **DB-migráció:** nincs (a `notification_feed.Type` mezőt használja)

### Feladatbontás és függőségi gráf

```
[T-DIGEST-ARCH] Kontrakt + ADR  (KÉSZ)
       │
       ├──────────────► PARALLEL (a kontrakt kész, egyszerre indítható)
       │                 │
       │   [T-DIGEST-BE]  DailyDigestJob (BackgroundService)      ─┐
       │   [T-DIGEST-FE]  Notification-feed panel/oldal (getFeed)  ─┤
       │                                                            │
       └──────────────────────────────────────────────────────────┘
                                    │
                          [T-DIGEST-QA] Playwright E2E
                                    │
                          [T-DIGEST-REVIEW] code-reviewer
```

### Tételek

| Id | Leírás | Modell | Függ | Párhuzam |
|---|---|---|---|---|
| **T-DIGEST-ARCH** | Kontrakt-delta + ADR-0011 (ez a dokumentum outputja) | opus | — | — |
| **T-DIGEST-BE** | `DailyDigestJob` BackgroundService, options, Program.cs regisztráció, quiet-hours helper, InApp+Email küldés, idempotencia (kontrakt §1–§8) | sonnet | T-DIGEST-ARCH | **igen** (FE-vel) |
| **T-DIGEST-FE** | Notification-feed panel/oldal (`getFeed`), `Type='DailyDigest'` megjelenítés, bell→feed nyitás, `markRead` (kontrakt §9 FE) | sonnet | T-DIGEST-ARCH | **igen** (BE-vel) |
| **T-DIGEST-QA** | Playwright E2E: nem-üres digest megjelenik; üres eset nem küld; Child RBAC-szűrt; idempotencia (kontrakt §10) | sonnet | T-DIGEST-BE, T-DIGEST-FE | — |
| **T-DIGEST-REVIEW** | code-reviewer jóváhagyás merge előtt mindkét ágon | opus | T-DIGEST-QA | — |

### Párhuzamosítható csoport

- **PARALLEL-DIGEST-1:** `{ T-DIGEST-BE, T-DIGEST-FE }`
  - Feltétel: a kontrakt (T-DIGEST-ARCH) rögzítve — teljesül.
  - Külön worktree/ág: `feature/digest-be`, `feature/digest-fe`.
  - A két ág **csak** a kontrakton (`daily-digest-contract.md`) keresztül függ
    egymástól; a `NotificationDto.type = "DailyDigest"` az egyetlen közös pont.
  - Kontrakt-módosítási igény bármely oldalon → STOP, vissza az architect agenthez.

### Kockázatok / figyelmeztetések

- **FE gap:** jelenleg nincs notification-feed lista a frontenden (csak bell
  unread-szám → `/reminders`). A digest olvashatósága a T-DIGEST-FE tétel új
  felületén múlik — ez nem opcionális (kontrakt §9 FE).
- **Idővonal-konvenció:** a quiet_hours/digest-idő a meglévő UTC-alapú
  összehasonlítást követi (kontrakt §7); zóna-korrekt kezelés külön CR.
- **Nincs LLM az MVP-ben:** a body sablon-alapú (CR "kifejezetten NEM cél").

---

## FEAT-IMPORTANT-EMAIL — Fontos e-mailek AI-alapú felismerése

- **Forrás:** [CR260710-03](change-requests/cr260710-03-fontos-emailek-felismerese.md)
- **Kontrakt (szerződés):** [classify-email-contract.md](contracts/classify-email-contract.md)
- **Döntés:** [ADR-0012](decisions/ADR-0012-classifyemail-korai-fontossag.md)
- **Státusz:** Tervezve (kontrakt kész → BUILD indítható)
- **Prioritás:** C (Could) — CR szerint
- **DB-migráció:** **van** — `email_message` +3 nullable oszlop (`importance`,
  `category`, `has_deadline_hint`) + 1 parciális index (kontrakt §6). Az új
  `AiJobType.ClassifyEmail` enum-érték migráció-mentes (string-enum, kontrakt §1.3).

### Feladatbontás és függőségi gráf

```
[T-IMPMAIL-ARCH] Kontrakt + ADR-0012  (KÉSZ)
       │
       ├──────────────► PARALLEL (a kontrakt kész, egyszerre indítható)
       │                 │
       │   [T-IMPMAIL-DB]  email_message migráció + EF-mapping (kontrakt §3, §6) ─┐
       │   [T-IMPMAIL-BE]  ClassifyEmail job + Ollama-classifier + prompt +       │
       │                   enqueue/dispatch + High-notification (kontrakt §1–§5)  ─┤
       │                                                                          │
       └──────────────────────────────────────────────────────────────────────────┘
                                    │
                          [T-IMPMAIL-QA] Playwright / integrációs E2E
                                    │
                          [T-IMPMAIL-REVIEW] code-reviewer
```

### Tételek

| Id | Leírás | Modell | Függ | Párhuzam |
|---|---|---|---|---|
| **T-IMPMAIL-ARCH** | Kontrakt-delta + ADR-0012 (ez a dokumentum outputja) | opus | — | — |
| **T-IMPMAIL-DB** | `20260711000001_AddEmailMessageImportance` migráció (3 oszlop + parciális index), `EmailMessageConfiguration` property-mapping (kontrakt §3, §6) | haiku | T-IMPMAIL-ARCH | **igen** (BE-vel, koordinációval) |
| **T-IMPMAIL-BE** | `EmailImportance` enum, `AiJobType.ClassifyEmail`, `EmailMessage.SetImportance`, `IEmailImportanceClassifier`+`OllamaEmailClassifier`, `classify-email.v1.txt` prompt, enqueue (`GmailIngestionService`), dispatch (`AiJobExecutor`), `ClassifyEmailJobRunner` + High-notification, DI (kontrakt §1–§5, §10, §11) | sonnet | T-IMPMAIL-ARCH | **igen** (DB-vel, koordinációval) |
| **T-IMPMAIL-QA** | E2E/integráció: új e-mail → importance+category; `High` → `notification_feed` (`Type=ImportantEmail`); `Low`/hirdetés → nincs értesítés; retry → nincs dupla; `ExtractDeadlines` továbbra is fut (kontrakt §12) | sonnet | T-IMPMAIL-DB, T-IMPMAIL-BE | — |
| **T-IMPMAIL-REVIEW** | code-reviewer jóváhagyás merge előtt | opus | T-IMPMAIL-QA | — |

### Párhuzamosítható csoport

- **PARALLEL-IMPMAIL-1:** `{ T-IMPMAIL-DB, T-IMPMAIL-BE }`
  - Feltétel: a kontrakt (T-IMPMAIL-ARCH) rögzítve — teljesül.
  - Külön worktree/ág: `feature/impmail-db`, `feature/impmail-be`.
  - **Koordinációs pont (kontrakt §10):** az `EmailMessage` entitás-property-k a
    `backend-dev` (BE) tulajdona, a `EmailMessageConfiguration` mapping + a migráció
    a `db-engineer` (DB) tulajdona. A séma (oszlopnevek/típusok) a kontrakt §3/§6-ban
    fixált — ez a két ág közös szerződése. A migráció akkor generálható hibátlanul,
    ha az entitás-property-k neve/típusa a kontrakt szerinti; ütközés → STOP,
    vissza az architect agenthez.
  - **Frontend:** nincs FE-tétel ebben a feature-ben (kontrakt §9) — a `High` e-mail
    a digest-feature `/notifications` feed-felületén jelenik meg.

### Kockázatok / figyelmeztetések

- **CR elavult fájl-hivatkozás:** a CR260710-03 az `EmailIngestionPoller`/
  `SyncSourceCommandHandler`-t nevezi meg; a normatív belépési pont a
  `GmailIngestionService.SyncAsync` (kontrakt §1.1).
- **Konkurencia (ADR-0012 §4):** a `ClassifyEmail` és az `ExtractText` ugyanazon az
  `email_message` soron, párhuzamosan fut; nincs lock/row-version, mert diszjunkt
  oszlopokat írnak — a `SetImportance` szigorúan csak a 3 fontosság-oszlopot +
  `UpdatedUtc`-t módosíthatja (kontrakt §8).
- **Elhatárolás (ADR-0012 §3):** a `ClassifyEmail` (korai jelzés) és a meglévő
  `Classify` (késői noise-discard) NEM vonható össze; a `ClassifyEmail` sosem
  töröl/soft-delete-el (kontrakt §7).
- **Notification-címzett:** legrégebbi aktív `Admin` (nem fan-out), `ActionUrl` =
  `/documents` (a Document ekkor még nem létezik) — kontrakt §4.

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
</content>

# Dev taskok — Family OS

> Státusz: v0.1 · Dátum: 2026-06-26
> Forrás: [implementation-context-matrix.md](../implementation-context-matrix.md),
> [mvp-backlog.md](../mvp-backlog.md), [implementation-plan.md](../implementation-plan.md)

Ez a mappa epicenként és diszciplinánként (BE / FE) bontva tartalmazza
az implementer-actionable taskokat. A taskok a sonnet model által
végrehajthatók a [implementation-context-matrix.md](../implementation-context-matrix.md)
által előírt kontextus-csomaggal.

## Fájl-konvenció

`epic-<betű>-<rövid-feature-név>-{backend,frontend}.md`

Minden task fájl tetején:
- **Felolvasott tervezési dokumentumok** — a mátrix szerinti pontos
  szakasz-hivatkozások (anchoring promptba másolható).
- **Story-k** — `mvp-backlog.md`-re hivatkozás.
- **Fázis** — `implementation-plan.md`-re hivatkozás.

Minden task egyetlen egységnyi munka (~½ – 1 nap a sonnet-nek
asszisztálva):
- Task ID (`T-<epic><BE|FE>-NN`)
- Cél (1-2 sor)
- Fájlok (létrehozandó / módosítandó)
- AC (Given/When/Then checkboxokkal)
- Függőség (más taskokra)

## Index

| Epic | Backend | Frontend | Megjegyzés |
|---|---|---|---|
| A — Alapok és infra | [epic-A-foundation-backend.md](epic-A-foundation-backend.md) | [epic-A-foundation-frontend.md](epic-A-foundation-frontend.md) | Fázis 1, 3, 4 |
| B — Felhasználó-kezelés | [epic-B-users-backend.md](epic-B-users-backend.md) | [epic-B-users-frontend.md](epic-B-users-frontend.md) | Fázis 3 |
| C — Dokumentum-kezelés | [epic-C-documents-backend.md](epic-C-documents-backend.md) | [epic-C-documents-frontend.md](epic-C-documents-frontend.md) | Fázis 5 |
| D — AI pipeline | [epic-D-ai-pipeline-backend.md](epic-D-ai-pipeline-backend.md) | — (SignalR push a BE fájlban) | Fázis 6-8 |
| E — Kereső + Q&A | [epic-E-search-backend.md](epic-E-search-backend.md) | [epic-E-search-frontend.md](epic-E-search-frontend.md) | Fázis 9 |
| F — Task + Deadline | [epic-F-tasks-deadlines-backend.md](epic-F-tasks-deadlines-backend.md) | [epic-F-tasks-deadlines-frontend.md](epic-F-tasks-deadlines-frontend.md) | Fázis 8/10 |
| G — Reminders | [epic-G-reminders-backend.md](epic-G-reminders-backend.md) | [epic-G-reminders-frontend.md](epic-G-reminders-frontend.md) | Fázis 10 |
| H — Notes | [epic-H-notes-backend.md](epic-H-notes-backend.md) | [epic-H-notes-frontend.md](epic-H-notes-frontend.md) | Fázis 11 |
| I — Tag + Topic | [epic-I-tags-topics-backend.md](epic-I-tags-topics-backend.md) | [epic-I-tags-topics-frontend.md](epic-I-tags-topics-frontend.md) | Fázis 11 |
| J — Audit + admin | [epic-J-audit-admin-backend.md](epic-J-audit-admin-backend.md) | [epic-J-audit-admin-frontend.md](epic-J-audit-admin-frontend.md) | Fázis 12 |
| K — Beállítások + integrációk | [epic-K-integrations-backend.md](epic-K-integrations-backend.md) | [epic-K-integrations-frontend.md](epic-K-integrations-frontend.md) | Fázis 12 |
| L — Dashboard | [epic-L-dashboard-backend.md](epic-L-dashboard-backend.md) | [epic-L-dashboard-frontend.md](epic-L-dashboard-frontend.md) | Fázis 11 |
| M — Deploy + ops | [epic-M-deploy-ops.md](epic-M-deploy-ops.md) | (egyetlen fájl) | Fázis 1, 12 |

## Megvalósítási sorrend

A fázis-sorrend az `implementation-plan.md` szerint kötelező. Egy epic
nem indítható el a függőségei nélkül:

```
A (BE+FE Fázis 1-4) →
  B (BE+FE Fázis 3) →
    M.M1 (Docker Compose) →
      C (BE+FE Fázis 5) →
        D-Infra (Fázis 6-7) →
          D-Tartalom (Fázis 8, 3 párhuzamos worktree) →
            E (Fázis 9) →
              F (BE+FE) → G (BE+FE) párhuzamos (Fázis 10) →
                H + I + L (Fázis 11) →
                  J + K + M (Fázis 12 — hardening)
```

Részletek a [implementation-plan.md](../implementation-plan.md) 12 fázisában.

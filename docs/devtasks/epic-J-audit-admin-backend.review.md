# Review — epic-J-audit-admin-backend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Jó bontás: az audit rendszerszintű (MediatR behavior + `[NoAudit]`
kivétel-mechanizmus), az immutability DB-trigger + REVOKE + teszt hármassal
védett, az export streaming és önmagát is auditálja. Az AI-jobs admin a
teljes api-design §18-at fedi.

## Észrevételek

1. **T-JBE-02 „fire-and-forget, de durable” (közepes):** a kettő
   feszültségben áll — ha az audit-írás aszinkron csatornán megy és a
   process elhal, a bejegyzés elveszik, ami a security-privacy „minden
   lényeges művelet naplózva” elvét sérti. Döntés kell: kritikus
   action-öknél (Login, PermissionChange, Delete) szinkron írás ugyanabban
   a tranzakcióban, a többinél mehet a channel. Az AC-be ez a
   megkülönböztetés kívánkozik.
2. **Átfedés az Epic K-val (kicsi):** a T-JBE-10 és a T-KBE-08 ugyanazokat
   az AI-provider endpointokat írja le; a K-fájl jelzi is („ha még nincs
   az Epic J-ben”). Mivel mindkettő Fázis 12, jelölje ki az orchestrátor,
   melyik worktree-é — különben merge-konfliktus lesz.
3. **T-JBE-03 auto-audit minden commandra (kicsi):** a details_json-be
   „csak engedélyezett kulcsmezők” — a generikus behaviorban ezt
   command-onként kell tudni (allowlist per DTO); a helper terve
   (T-JBE-02) jó, de a kulcsmező-lista forrása (attribútum? konvenció?)
   definiálandó, különben a sonnet mindent logol.
4. **T-JBE-08 export `FileAccess` actionnel** — jó önreferenciális
   megoldás; a 19.3 api-design-nal konzisztens.

## Verdikt

Végrehajtásra kész; az #1 szinkron/aszinkron megkülönböztetés a
code-reviewer security-kapujához tartozó döntés.

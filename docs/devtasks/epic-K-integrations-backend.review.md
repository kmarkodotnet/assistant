# Review — epic-K-integrations-backend.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Jó bontás a legkockázatosabb integrációra: OAuth-flow, címke-alapú
szelektív beszívás lapozással, dedup, poller, disconnect token-revoke-kal,
backup/restore scriptek integrációs teszttel. Az ADR-0004 elvei (readonly
scope, explicit címke-gesztus) hűen megvalósulnak.

## Észrevételek

1. **OAuth-callback design ütközés a FE-vel (közepes):** itt a callback
   a backendre fut be (`GET /api/v1/sources/gmail/callback?code=...`,
   T-KBE-02), az Epic K frontend T-KFE-07-e viszont
   `/settings/oauth-callback` **frontend** oldalt definiál, ami POST-tal
   továbbítja a code-ot. A kettő közül egy kell (a backend-callback az
   egyszerűbb és biztonságosabb — a code nem jár a kliens JS-ben);
   a FE-task ez esetben csak a redirect-visszatérés fogadása.
   A Google Console-ban regisztrált redirect URI-nak (DELIVERY.md 3a)
   is ezzel kell egyeznie.
2. **Epic J átfedés (kicsi):** T-KBE-08 = T-JBE-10 (AI provider
   endpointok) — gazdát kell jelölni (epic-J-audit-admin-backend.review.md #2).
3. **T-KBE-04 melléklet-korlátok (kicsi):** az email-mellékletek
   Document-té alakításánál a C-epic MIME-whitelist és méret-limit
   érvényesítése nincs kimondva (mi történik egy 60 MB-os vagy .zip
   melléklettel? — skip + jelölés az EmailMessage-en). Egy AC pótlandó.
4. **T-KBE-05 poller ütemzés:** Hangfire recurring `*/5` — konzisztens az
   architecture 3.6-tal; a „PC bekapcsoláskor azonnali catch-up sync”
   (ai-pipeline 7.1) viszont nincs AC-ben — az OnStarted-hez egy
   azonnali futtatás pótolandó.
5. **T-KBE-10 fájlnév-séma** (`YYYY-MM-DD.dump.age`) vs. DELIVERY.md
   példái (`family-os-20241215_020001.dump`) — kozmetikai, de a
   restore-runbook fájlmintáinak egyeznie kell a scripttel.

## Verdikt

Végrehajtásra kész; az #1 callback-döntés lezárása kötelező az
implementáció előtt (három doksit érint).

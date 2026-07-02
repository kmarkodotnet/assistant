# ADR-0010 — Szállítási cél: Docker Compose; Helm-kapu alóli termék-kivétel

- Státusz: **Elfogadva** (a felhasználó 2026-07-02-én explicit
  jóváhagyta a CLAUDE.md Helm-kapuja alóli termék-szintű kivételt)
- Dátum: 2026-07-02
- Döntéshozó: kmarko.net@gmail.com

## Kontextus

A factory (CLAUDE.md) alapértelmezett tech stackje Kubernetest ír elő, és
a minőségi kapuk között szerepel: „Helm chart `helm lint` + `helm template`
hibamentes". A Family OS terméke ugyanakkor definíció szerint
**single-tenant, self-hosted, LAN-only, egyetlen otthoni PC-n** fut
(ADR-0003, product-vision) — Kubernetes-fürtnek nincs értelme ebben a
környezetben, és a célfelhasználó (otthoni admin) számára a Compose a
reális üzemeltetési szint. A repo Helm chartot nem tartalmaz.

## Döntés

- A Family OS **szállítási célja a Docker Compose** (`make up`), az M
  epic ennek megfelelően épül; Raspberry Pi-változat a
  `docker-compose.rpi.yml`-lel.
- **Helm chart az MVP-ben nem készül.** A CLAUDE.md Helm-kapuja erre a
  termékre nem alkalmazandó; a SHIP fázis minőségi kapuja helyette:
  `docker compose config` hibamentes + a telepítési smoke-teszt
  (T-MOP-12) zöld.
- Ha később multi-node / távoli deployment igény merül fel, új ADR
  nyithatja meg a Helm-utat.

## Indoklás

- A kapu célja (reprodukálható, validált deployment-artefakt) a
  Compose + smoke-teszt párossal ebben a környezetben teljesül.
- Egy sosem használt Helm chart karbantartási teher és hamis
  biztonságérzet lenne.

## Következmények

- `implementation-plan.md` 12. fázis: a „Helm chart (opcionális MVP)"
  tétel törölve a kötelező körből; a DoD a Compose-smoke-ra hivatkozik.
- A factory szintjén a CLAUDE.md kapu **általánosan érvényben marad** más
  termékekre; ez az ADR kizárólag a Family OS-re ad kivételt, a
  felhasználó 2026-07-02-i jóváhagyásával. A CLAUDE.md „Minőségi kapuk"
  szakasza változatlan marad (a factory-engineer számára érinthetetlen);
  a Family OS SHIP-kapuja: `docker compose config` hibamentes +
  telepítési smoke-teszt (T-MOP-12) zöld.

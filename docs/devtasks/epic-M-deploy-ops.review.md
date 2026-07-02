# Review — epic-M-deploy-ops.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Jó infra-epic: compose-stack LAN-bindekkel, TLS belső CA-val, backup-cron,
log-rotation, privacy-assertion **CI red gate** (T-MOP-10) és telepítési
smoke-teszt tiszta VM-en (T-MOP-12) — az utolsó kettő kifejezetten erős
minőségi kapu. A DELIVERY.md-runbook AC-listája (T-MOP-09) teljes.

## Észrevételek

1. **Helm chart: a CLAUDE.md kapuja vs. a terv (közepes):** a CLAUDE.md
   minőségi kapui között szerepel a „Helm chart `helm lint` +
   `helm template` hibamentes”, és a tech stack Kubernetest ír elő — ez
   az epic viszont **egyáltalán nem tartalmaz Helm-taskot** (az
   implementation-plan 12. fázisa is csak „opcionális MVP”-ként említi).
   A Family OS-nél a Compose-only döntés teljesen indokolt (self-hosted,
   egy gép), de akkor ezt **ADR-ben kell rögzíteni** mint tudatos eltérést
   a factory-alapértelmezéstől, különben a code-reviewer/QA kapu
   formálisan buktatja a SHIP fázist.
2. **T-MOP-04 workers→ollama „opcionális dependency” (jó)** — konzisztens
   a T-ABE-20 degraded-readiness döntésével; az architecture.md 12.
   frissítése után teljesen kerek.
3. **T-MOP-05 OTel „csak ha az MVP idő engedi”** — jó, hogy explicit
   halasztható; a metrika-lista az architecture.md 12-vel egyezik.
4. **T-MOP-03 CSP-fejléc** — a security-privacy.md 9.2 CSP-mintája hibás
   (`connect-src /api/` — security-privacy.review.md #7); a helyes
   direktíva-készletet itt, az nginx-confban kell véglegesíteni.
5. **T-MOP-12 telepítési smoke** — a 60 perces cél a DELIVERY.md-vel és
   az M4 AC-vel konzisztens; javasolt a Raspberry Pi-utat
   (deploy-raspberry-pi.md) is felvenni legalább „best effort” jelöléssel,
   mert az arm64 build-lánc külön törési felület.

## Verdikt

Végrehajtásra kész; az #1 Helm-eltérés ADR-esítése kötelező, hogy a
factory minőségi kapui és a termék-valóság ne ütközzenek a SHIP-nél.

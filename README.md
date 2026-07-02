# AI Software Factory

Otthoni, autonóm szoftvergyár Claude Code-ra építve.
Stack: C#/.NET · Angular · PostgreSQL · Docker · Kubernetes · Playwright.

> **Aktuális állapot (2026-07):** a factory-infrastruktúra egy része
> (`.claude/agents`, `.claude/commands`, `.claude/skills`,
> `docs/requirements/`, `factory-metrics/`) még **nem létezik** — az
> alábbi parancsok célállapotot írnak le. A `scripts/` viszont él.
> Az első termék, a **Family OS** (családi információkezelő) ebben a
> repóban készül: tervek a `docs/`-ban (review-k: `*.review.md`,
> döntések: `docs/decisions/ADR-*`), kód a `src/` + `frontend/` alatt,
> telepítés: `docs/DELIVERY.md`.

## Használat (3 lépés)
1. Másold a specifikációdat: `docs/requirements/<termek>.md`
   (sablon: `docs/templates/requirements-template.md`)
2. Indítsd Claude Code-ot a repo gyökerében: `claude`
3. Add ki: `/build-product docs/requirements/<termek>.md`
   → a végén a `docs/DELIVERY.md` írja le a kész termék indítását.

## Parancsok
- `/build-product <spec>` — teljes gyártás specből
- `/new-feature <leírás>` — feature hozzáadása kész termékhez
- `/fix-bug <leírás>` — javítás eszkalációs lépcsővel (haiku→sonnet→opus)
- `/qa [szűrő]` — E2E kör; `/review` — code review; `/status` — állapot

## Hogyan spórol tokent?
- Modellrouting: triviális→haiku, fejlesztés→sonnet, architektúra/review→opus
  (agentenként a frontmatter `model` mezője rögzíti)
- Subagentek minimális kontextust kapnak (kontrakt + acceptance criteria)
- Hosszú kimenetek fájlba mennek, a chatbe csak összefoglaló kerül

## Párhuzamosság
Az architect által rögzített API/DB-kontrakt a "szerződés"; minden feature
saját git worktree-ben épül (`scripts/new-feature.sh`), így a backend-,
frontend- és QA-agentek ütközés nélkül futnak egyszerre.

## Struktúra
.claude/agents — 9 specializált agent · .claude/commands — slash parancsok
.claude/skills — stack-szabványok · scripts — worktree-kezelés · docs — kontraktok, ADR-ek, bugok

## Önfejlesztés (meta-szint)
A `factory-engineer` agent minden gyártási futás után retrospektívát ír
(`docs/retros/`), és a telemetria (`factory-metrics/runs/`) alapján max 3
módosítást javasol a gyár saját konfigurációjára — külön `factory/*` ágon,
code-review kapuval. Kézi indítás: `/retro [fókusz]`,
nyers adatok összesítése: `./scripts/metrics-summary.sh`.

## 1-es szint: discovery
`/discover <cél>` — a product-manager agent interjúzik, alternatívákat vázol,
DRAFT-specet ír; a jóváhagyásod után indulhat a gyártás.

## 2-es szint: önüzemeltetés
`/operate <termék>` — ops-watcher (haiku) észlel, a fix-bug lépcső javít,
zöld smoke után automatikus deploy rollbackkel. Időzítve:
`0 * * * * cd /path/ai-sw-factory && claude -p "/operate <termek>"`
Kockázatos változás (migráció, kontrakt) mindig a te jóváhagyásodra vár.

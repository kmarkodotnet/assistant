# Review — templates/requirements-template.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Tömör, jól célzott sablon a factory bemenetéhez: cél/szerepkörök/
funkcionális követelmények AC-vel/NFR/adatok/out-of-scope/átadás. A
minimalizmus itt erény — a CLAUDE.md workflow-ja (SPEC fázis) úgyis
feltárja a hiányokat.

## Észrevételek

1. **Hiányzó szakasz: környezet / integrációk** — a Family OS
   tapasztalata alapján (Google OAuth, Gmail, lokális AI) érdemes lenne
   egy „Külső függőségek / integrációk” pont: mihez kell kapcsolódni,
   milyen fiókok/kulcsok állnak rendelkezésre. Ez tipikusan blokkoló
   kérdésként jön elő a BUILD közben, ha a spec nem tér ki rá.
2. **Hiányzó szakasz: adatérzékenység** — a 4. NFR-sor említ
   „biztonság”-ot, de egy explicit kérdés („van-e különösen érzékeny
   adat — egészségügyi, pénzügyi — és elhagyhatja-e a gépet?”) korán
   rögzítené a privacy-követelményt, ami a Family OS-nél utólag
   ADR-szintű döntéssorozatot igényelt.
3. **Prioritás-jelölés** — az F-1 blokkban „must / should / could”
   kisbetűs, a mvp-backlog M/S/C rövidítést használ; egységesítés
   kozmetika, de az agentek számára a konzisztens jelölés hasznos.
4. A 7. „Átadás” szakasz (compose vs. k8s) jó — a CLAUDE.md K8s/Helm
   elvárásával összhangban kérdez rá a célkörnyezetre.

## Verdikt

Használható; az 1–2. pont szerinti két plusz kérdéssel erősebb lenne.

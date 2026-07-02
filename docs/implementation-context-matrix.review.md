# Review — implementation-context-matrix.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Ritka és értékes műfaj: explicit kontextus-betöltési terv a megvalósító
(sonnet) agenteknek, epic × dokumentum mátrixszal, szakasz-szintű
felbontással és tokenbudget-ökölszabályokkal. A CLAUDE.md
tokentakarékossági elvét ez teszi végrehajthatóvá. Kisebb
hivatkozás-hibák javítandók.

## Hibák / következetlenségek

### 1. Halott szakasz-hivatkozás: „security-privacy.md §17” (kicsi)
A code-reviewer kontextus-szakasz „`security-privacy.md` §17 (privacy
assertions)”-t ír — a security-privacy.md-nek **14 szakasza** van; a
privacy assertions a **§13.3**. Javítandó, mert a reviewer-agent szó
szerint fogja keresni.

### 2. ★★★ jelentése vs. szakaszlista (kicsi)
A jelmagyarázat szerint ★★★ = „a teljes dokumentum”, de több helyen a
★★★ sor mellé szakaszlista kerül (pl. Epic C: api-design ★★★ „FULL — §7…,
§1, §23”; Epic D: architecture ★★★ „FULL — különösen §3.4…”). A
„különösen” formula rendben van, de az Epic C-nél a lista úgy olvasható,
mintha csak azok kellenének. Egységesítés: ★★★ mindig FULL, a lista csak
kiemelés.

### 3. Zavaros megjegyzés az Epic K reminder-sorában (kicsi)
„§5.2 (SMTP konfig) — K3 nem érinti, de K1 SMTP relay K3-mal együtt” —
az SMTP-csatorna a G5 story-hoz tartozik, nem K1-hez (az a Gmail
*beolvasás*). A mondat átfogalmazandó vagy törlendő.

### 4. Frissítési kötelezettség a review-k után (megjegyzés)
A mátrix a tervező doksik jelen állapotára épül. A doksi-review-kban
jelzett blokkoló döntések (child-RBAC, PrivacyMode, invite/preferences
séma) átvezetése után a mátrix érintett sorai (B, J, K epic) is
ellenőrizendők — a „Karbantartás” szakasz pont ezt írja elő, csak
jelezzük, hogy most esedékes lesz.

## Erősségek (megőrzendő)

- A baseline-csomag (CLAUDE.md + coding-standards + aktuális fázis +
  non-goal-ok) jó minimum — a „negatív kontextus” (4. tipp) különösen.
- Az eltérés-protokoll (2. tipp: hiányzó specifikációnál nem kitalálni,
  hanem architect-eszkaláció) a leghasznosabb szabály az egész factory
  számára.
- Tokenbudget-tábla konkrét számokkal — mérhető, betartatható.
- A code-reviewer eltérő (szélesebb) csomagjának külön kezelése helyes.

## Verdikt

Kis javítások után kész; a felsorolt hivatkozás-hibákon túl tartalmi
probléma nincs. A doksi értéke akkor áll fenn, ha a review-k utáni
doksi-frissítésekkel együtt karbantartják.

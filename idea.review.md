# Review — idea.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Az `idea.md` a projekt kiinduló briefje: jól strukturált, konkrét, és a
13 kért deliverable (product vision → coding standards) ténylegesen le is
fedi a tervezési fázist. A dokumentum betölti a szerepét — a belőle
származtatott docs/ készlet láthatóan követi. Az alábbi észrevételek főleg
a scope-ra és az elavulásra vonatkoznak.

## Észrevételek

### 1. Elavult verzió-rögzítés — .NET 8 (közepes)
A brief `.NET 8`-at ír elő, és az architektúra-doksik ezt átvették.
2026 közepén a .NET 8 LTS támogatása 2026 novemberében lejár; az aktuális
LTS a .NET 10. A repo CLAUDE.md-je is „legújabb LTS”-t ír elő. Érdemes a
briefben (vagy egy ADR-ben) explicit módon .NET 10-re frissíteni, mielőtt
kód készül — utólag drágább.

### 2. Az MVP-lista túl széles (közepes)
A „Core MVP” 13 tétele között szerepel a Gmail-integráció is (13. pont),
miközben a product-vision.md ezt már helyesen „későbbi MVP-fázisra” (UC-08)
tolja. A brief és a vízió között így enyhe feszültség van: a briefben MVP,
a vízióban félig non-goal. Egyértelműsítendő, melyik érvényes (a
mvp-backlog.md a mérvadó a gyakorlatban).

### 3. Ellentmondás a non-goal listával (kicsi)
A brief „later Kotlin frontend”-et és „facebook integration”-t említ az MVP
felsorolásban/környékén, a product-vision.md non-goals ezeket explicit
kizárja. A leszármaztatott doksik jól oldották fel, de a brief önmagában
félreérthető.

### 4. Modellnév-pontatlanság (kicsi)
„GPT:oss 20b” — a helyes név `gpt-oss:20b` (Ollama-jelölés). A többi doksi
már helyesen írja.

### 5. Apróságok
- Elgépelés: „for wxample” (57. sor).
- Kevert nyelv: a brief angol, de magyar kimenetet kér — ez rendben van,
  csak legyen tudatos (a docs/ konzekvensen magyar lett).
- A „If vector search is useful” (15. sor) nyitva hagyott kérdés — az
  ADR-0001 lezárta (pgvector), a briefben jelölni lehetne, hogy eldőlt.

## Verdikt

A brief céljának megfelel; a tervezési dokumentumok jó minőségben
származtak belőle. Frissítendő: .NET verzió, MVP-scope (Gmail), modellnév.

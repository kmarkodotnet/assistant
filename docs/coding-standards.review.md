# Review — coding-standards.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Gyakorlatias standards-doksi: enforce-olható szabályok (analyzer, lint,
formatter) elválasztva a reviewer-checklistától, jó példakódok, világos
DTO-vs-entity és endpoint-szabályok. A code-reviewer agent 17. szakaszbeli
checklistája kifejezetten hasznos. Néhány példa ellentmond a saját
szabályainak.

## Hibák / következetlenségek

### 1. `Guid.NewGuid()` a példakódban vs. UUIDv7 döntés (közepes)
Az 5.3 `Reminder.ForTask` példa `Id = Guid.NewGuid()`-ot ír — a
domain-model.md és database-schema.md (6. szakasz) szerint viszont
**UUIDv7**-et generálunk saját `IUuidGenerator`/`UuidV7Generator`-ral
(index-lokalitás miatt). A példa pont azt a mintát mutatja, amit a
fejlesztő agentek másolni fognak — javítandó
`Id = uuidGenerator.NewV7()`-re (és a factory-metódus szignatúrája
kapja meg a generátort vagy az entitás a konstruktorban).

### 2. `DisableAntiforgery()` vs. security-privacy CSRF-szabály (közepes)
A 11. szakasz endpoint-sablonja `.DisableAntiforgery()`-t hív a
multipart upload-on — a security-privacy.md 9.3 viszont anti-forgery
tokent ír elő a state-változtató endpointokra. Multipart + SPA esetén
bevett az antiforgery kiváltása (SameSite + custom header), de akkor ezt
a kivételt a security-doksiban kell rögzíteni, ne csak egy kód-mintában
bujkáljon.

### 3. .NET 8 rögzítés (közepes)
2.1: „.NET 8 LTS, TargetFramework net8.0” — 2026 közepén a .NET 8
támogatása novemberben lejár; új projektnek .NET 10 LTS való (lásd
idea.review.md #1, architecture.review.md #3). Ez a doksi a harmadik
hely, ahol a verzió be van égetve — érdemes egyetlen ADR-ben rögzíteni
és onnan hivatkozni.

### 4. Kisebb észrevételek
- 9.3 példa: `approve = output<Guid>();` — TypeScript-ben nincs `Guid`
  típus (a generált kliens `string`-et ad). `output<string>()` a helyes.
- 6.1: a saját `ValidationException` név ütközik a
  `FluentValidation.ValidationException`-nel — működik, de állandó
  using-alias kényszer; érdemes pl. `AppValidationException`-nek nevezni,
  vagy explicit rögzíteni a FluentValidation kivétel-mappelést.
- 14.1 XML-doc nyelve: „magyarul **vagy** angolul (csapatdöntés…)” —
  az 1.1 elv (identifier + log angol) alapján az angol a konzisztens;
  a „vagy” döntetlent hagy, ami agent-műhelyben divergenciát szül.
  Döntsük el: angol.
- 18.: „a `DELIVERY.md` (v0.12) fedi” — a DELIVERY.md fejlécében nincs
  ilyen verzió; elgépelésnek tűnik.
- 2.1: `TreatWarningsAsErrors=true` mellett a szűkített `WarningsAsErrors`
  lista redundáns — nem hiba, de egyszerűsíthető.
- 12.: a `husky` + `commitlint` node-os eszközök a .NET repo gyökerében —
  működik monorepóban, de a devtaskokban legyen explicit setup-lépés.

## Erősségek (megőrzendő)

- Enforcer vs. reviewer-checklist szétválasztás (bevezető) — a lint által
  kikényszeríthető szabályok nem szubjektív review-témák.
- A 17. code-reviewer checklist konkrét és a projekt-specifikus kapukat
  (prompt-verzió, privacy-log, api-design szinkron) is tartalmazza.
- DTO vs. Entity táblázat (4.) és a controller-tilalmak (11.) — pontosan
  a tipikus agent-hibák ellen.
- TODO lejárati dátummal (14.2) — jó konvenció.

## Verdikt

Kiadható standard; az #1 és #2 példajavítás fontos, mert a példakód
erősebben hat az agentekre, mint a prózai szabály.

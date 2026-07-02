# Review — mvp-backlog.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Jól strukturált backlog: 13 epic, ~50 story, mindenhol Given/When/Then AC
és BE/FE/AI/Infra task-bontás, MoSCoW prioritással. A story-k a tervező
doksikra hivatkoznak, ami a fejlesztő agenteknek pontos kontextust ad.
A hibák a fázis-leképezésben és néhány számszaki/függőségi pontban vannak.

## Hibák / következetlenségek

### 1. A 14. szakasz fázis-táblája hiányos és eltér az implementation-plantől (közepes)
- A **H epic (Notes)** egyetlen fázishoz sincs rendelve a táblában —
  a Notes így sosem készülne el (az implementation-plan 11. fázisa
  legalább a BE-t tartalmazza).
- A **C4** (szöveg-korrekció, S) és **C5** (törlés, **M!**) szintén nem
  szerepel egyik fázisban sem — egy Must story ütemezés nélkül.
- **L2** és **G4/G5** szintén kimaradt a táblából, míg az
  implementation-plan tartalmazza őket.
- Fázis 3 sora („C1 csak upload skeleton”) eltér az implementation-plan
  fázis 3-tól (ott nincs C1).
Javaslat: a tábla törlése és hivatkozás az implementation-plan-re (egy
igazságforrás), vagy tételes szinkron.

### 2. Számszaki hiba az A2 AC-ben (kicsi)
„a 20 tábla, 22 enum … létrejönnek” — a database-schema.md v0.2 alapján
**25 tábla** és **21 enum** van. Az AC-t érdemes nem konkrét számmal,
hanem „a database-schema.md v0.2 teljes sémája” megfogalmazással írni,
így nem avul el.

### 3. Prioritás-inverzió: L1 [M] függ E7-től [C] (kicsi)
Az L1 dashboard AC-je „saved searches” tartalmat ír elő, miközben az E7
(mentett keresések) csak Could. Vagy az L1-ből kivesszük a saved-searches
widgetet (üres állapottal), vagy az E7 Should-ra emelendő. (A mögöttes
`SavedSearch` entitás ráadásul a sémából is hiányzik —
domain-model.review.md #2.)

### 4. D1 AC: `Stream` metódus (kicsi)
„`OllamaAiProvider` implementálja a `IAiProvider`-t (`Complete`,
`Stream`)” — az architecture.md 4.1 interfészében csak `CompleteAsync`
van; a streaming az api-design.md 25. szerint v2. A `Stream` törlendő az
AC-ből, vagy az interfész bővítendő.

### 5. Nyitva hagyott séma-döntések a story-kban (kicsi)
- B2: „`UserAccountInvite` tábla **vagy** `UserAccount` `IsActive=false`
  előlétrehozva” — döntés kell (a login-allowlist viselkedést is ez
  határozza meg; lásd security-privacy.review.md #3).
- B3: „`UserAccount`-on JSONB mező **vagy** külön `user_preferences`
  tábla” — döntés kell; egyik sincs a database-schema.md-ben.
A backlog jó helyen jelzi az alternatívákat, de a BUILD előtt az
architectnek választania kell, és a sémát frissíteni.

### 6. Kisebb észrevételek
- A5 AC: „`/healthz/ready` HTTP 200, ha DB **és Ollama** elérhető” — lásd
  architecture.review.md #2: az Ollama ne buktassa a readiness-t.
- C5 story kettős prioritása „[M / S]” — bontandó két külön story-ra,
  különben a szűrés (Must-only MVP-vágás) kétértelmű.
- I1 AC: „reuse cont (`usage_count`)” — elgépelés („count”), és a
  `usage_count` karbantartási szabálya (mikor nő/csökken, mi történik
  unlink-nél) sehol nincs specifikálva.
- G2 AC a 14 napos catch-up ablakkal konzisztens a reminder-engine.md-vel
  — jó; az architecture.md 7 napja az eltérő (ott javítandó).

## Erősségek (megőrzendő)

- Given/When/Then AC-k konkrét HTTP-válaszokkal — gépi ellenőrzésre
  alkalmasak (QA agent tudja tesztre fordítani).
- A story-k pontosan hivatkoznak a tervező doksik szakaszaira.
- MoSCoW következetesen alkalmazva, a Must-halmaz (28) reális MVP-vágás.

## Verdikt

Használható backlog; az #1 leképezés-szinkron és a C5/H ütemezés pótlása
kötelező a /build-product indítása előtt.

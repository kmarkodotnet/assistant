# Termékvízió — Family OS

> Státusz: DRAFT v0.3 · Dátum: 2026-06-26 · Nyelv: magyar
> Forrás: `idea.md` (a felhasználó által megadott brief)

---

## 1. Rövid leírás

A **Family OS** egy családi szintű, magán használatra szánt információkezelő
rendszer, amely egy helyen tárolja, rendszerezi és kereshetővé teszi
a család mindennapi életéhez tartozó fontos adatokat: dokumentumokat,
határidőket, jegyzeteket, számlákat, garanciákat, egészségügyi és iskolai
iratokat, autóhoz/házhoz kapcsolódó nyilvántartásokat.

Az AI a rendszerben **nem az igazság forrása**, hanem egy intelligens réteg
a saját adatbázis fölött: osztályoz, kivonatol, határidőket javasol,
szemantikusan keres és természetes nyelvű kérdésekre válaszol — mindig
a saját, lokálisan tárolt adatokra hivatkozva.

A megközelítés **privacy-first**: az alapértelmezett modell egy
lokális futtatású LLM (Ollama; default `llama3.2:3b`, erős hardveren
`gpt-oss:20b` — ADR-0006), külső AI szolgáltató az MVP-ben egyáltalán
nem hívható (kódba égetett LocalOnly kapu). Az érzékeny családi
adatok soha nem hagyják el a háztartást a felhasználó tudta nélkül.

A termék MVP-ben **egyetlen család** kiszolgálására készül (single-tenant,
self-hosted), Docker Compose-ban futtatható otthoni szerveren / NAS-on / PC-n.

---

## 2. Fő felhasználói problémák

A célzott problémák, amelyeket a Family OS megold:

1. **„Hol van a papír?”** — A garancia, biztosítás, oltási könyv, szerződés,
   szervizlap szétszórva van email mellékletekben, fiókokban, telefon
   galériában, felhőtárhelyeken. Keresni órákig tart.
2. **Elfelejtett határidők** — Műszaki vizsga, biztosítás megújítása,
   gyerek iskolai engedélyek, számlák lejárata, recept kiváltása.
   Nincs egy közös naptár, ami emlékeztet.
3. **Tudás-szilók a családban** — Az egyik szülő tudja, hol a kazán
   garanciája, a másik tudja, mikor volt a gyerek utolsó védőoltása,
   a nagyszülő tudja, melyik orvos írta fel a gyógyszert.
   Megosztott, kereshető családi tudásbázis hiányzik.
4. **Dokumentumok feldolgozatlanok** — Egy PDF-ben szöveges adat van
   (dátum, összeg, lejárat), de manuálisan kell kibogarászni és felírni
   valahova. Ez nem skálázódik napi szinten.
5. **Email-túlterhelés** — Fontos információk (rendelés visszaigazolás,
   foglalás, iskolai értesítő) emailben érkeznek, és elsüllyednek
   a hírlevelek között.
6. **Privacy aggály a cloud AI-okkal szemben** — A család egészségügyi,
   pénzügyi, jogi iratait sok háztartás nem szeretné feltölteni
   ChatGPT-be vagy Google Drive-ra. Saját, lokális megoldás kell.
7. **Felelősség-ködösség** — Nem világos, ki a felelős egy adott
   feladatért (pl. autó vizsga intézése). Kell egy rendszer, ami
   konkrét családtaghoz rendel feladatokat.

---

## 3. Célfelhasználók

### Elsődleges (MVP)
- **Saját család** (1 háztartás, ~2–6 fő). A felhasználó és a házastárs
  felnőttként, technikailag képes szülők; gyerekek korlátozott
  hozzáféréssel. Self-hosted üzemmód.

### Felhasználói szerepkörök az MVP-ben
| Szerepkör | Jogosultságok |
|---|---|
| **admin** | Teljes hozzáférés, családtagok kezelése, beállítások, AI-provider váltás, törlés, audit log megtekintése. |
| **adult** | Dokumentumok, jegyzetek, feladatok, határidők CRUD; AI keresés és Q&A; nem láthat „private” jelölt rekordot, ha nem ő a tulajdonos. |
| **child / read-only** | Csak olvasás a hozzá *kötött* (related_family_member = ő), nem-privát rekordokon — a rekord child-hoz kötése az explicit megosztási gesztus. Normatív mátrix: security-privacy.md 4.1, [ADR-0007](decisions/ADR-0007-child-szerepkor-rbac.md). |

### Másodlagos (későbbi fázis, nem MVP)
- További rokonok (nagyszülők), akik bizonyos dokumentumokhoz olvasó
  hozzáférést kapnak.
- Más családok — opcionálisan, ha multi-tenant verzió születik. Az MVP
  ezt **nem** célozza.

---

## 4. Elsődleges használati esetek (MVP)

Konkrét, mérhető használati esetek, amelyekhez az MVP-nek működnie kell:

### UC-01 — Dokumentum feltöltés és automatikus feldolgozás
A felhasználó feltölt egy PDF-et (pl. autó kötelező biztosítás). A rendszer
kinyeri a szöveget, nyelvet detektál, javaslatot tesz osztályozásra
(„Pénzügy / Biztosítás / Autó”), kivonatol dátumokat (lejárat),
összefoglalót generál (3–5 mondat), és **javasol** egy határidőt és
egy emlékeztetőt. A felhasználó egy kattintással elfogadja.

### UC-02 — Természetes nyelvű kérdés a családi adatokra
A felhasználó beír: *„Mikor jár le az autó kötelező biztosítása?”* —
a rendszer megnézi a strukturált adatbázist és/vagy szemantikus keresést
indít, és válaszol: *„2026-09-14 — az AXA 2025-09-15-én kiállított
kötvénye alapján. [forrás dokumentum link]”*. Minden válasz forrásra
hivatkozik.

### UC-03 — Garancia/szerződés gyors megtalálása
*„Hol van a mosógép garanciája?”* — a rendszer kilistázza a vonatkozó
dokumentumot, megnyithatóval és metaadatokkal (vásárlás dátuma,
garancia végdátum, bolt).

### UC-04 — Közelgő határidők dashboardja
A főoldalon a felhasználó látja a következő 30 nap határidőit
családtagonként szűrhetően (műszaki vizsga, számlák, iskolai
engedélyek, oltás), prioritás szerint sorbarakva.

### UC-05 — Manuális jegyzet rögzítése és kereshetővé tétele
*„A nyaralásról: a Philippines-i utat 2027 tavaszán szervezzük, max
3 hét, max 2,5M Ft.”* — a felhasználó beírja, a rendszer eltárolja,
és később a *„Mit döntöttünk a Philippines útról?”* kérdésre megtalálja.

### UC-06 — Feladat családtaghoz rendelése
*„Apa: műszaki vizsga intézése 2026-08-30-ig.”* — feladat létrejön,
felelős hozzárendelve, emlékeztető előtte 7 és 1 nappal.

### UC-07 — Egészségügyi rekord rögzítése
Lab eredmény PDF feltöltése egy adott családtaghoz kötve. Később:
*„Mutasd a feleségem legutóbbi lab eredményét.”* → találat, forrással.

### UC-08 — Gmail-ből származó értesítés befogadása (későbbi MVP-fázis)
A rendszer (felhasználói engedéllyel) szelektíven beszív egy Gmail
fiókból bizonyos címkével ellátott emaileket, és ugyanazon a pipeline-on
átfuttatja, mint egy feltöltött dokumentumot.

---

## 5. Non-goals az MVP-ben

Explicit kizárt funkciók — ezek **nem** célok az MVP-nél, ne kezdjük el
ezeket megépíteni:

1. **Multi-tenant / SaaS** — Az MVP egyetlen család, self-hosted.
   Nincs regisztrációs felület idegen felhasználóknak, nincs előfizetés,
   nincs központi szerver több család adataival.
2. **Mobil natív app (Kotlin)** — A Kotlin frontend a *későbbi* roadmap része.
   MVP-ben csak Angular webes felület mobil-reszponzív megjelenéssel.
3. **Push notification infrastruktúra** — MVP-ben emlékeztető = az
   alkalmazásban megjelenő dashboard-elem + opcionálisan email. Web push
   / mobile push nem cél.
4. **Külső naptár két-irányú szinkron** (Google Calendar írás-olvasás) —
   MVP-ben legfeljebb olvasás opcionális. Két irányú szinkron későbbi fázis.
5. **OneDrive / Drive automata szinkron** — A fájlok kézi feltöltéssel
   kerülnek be MVP-ben. Felhő-szinkron későbbi fázis.
6. **Facebook integráció** — Külön említve a briefben, de **későbbi** lépés,
   nem MVP.
7. **AI ágens automatikus cselekvés** — Az AI **nem** hoz létre automatikusan
   aktív feladatot, határidőt vagy emlékeztetőt. Mindig javaslatként
   kerül a felületre, és emberi jóváhagyás kell az aktiválásához.
   (Konfigurációval, későbbi fázisban opcionálisan engedélyezhető lesz
   bizonyos kategóriákra — de **nem MVP**.)
8. **Pénzügyi tranzakció / banki integráció** — Számlák PDF-ként
   feldolgozhatók, de nincs banki API, kategorizáló költségvetés-tervező,
   automatikus utalás.
9. **Orvosi diagnosztika / döntéstámogatás** — A rendszer **tárolja** és
   keresi az egészségügyi rekordokat, de **nem** ad orvosi tanácsot,
   nem értelmez lab eredményt diagnózis céljából.
10. **Külső felhasználók megosztása** (link sharing kifelé) — Az MVP
    zárt: csak a regisztrált családtagok látnak adatot.
11. **Verziókezelés dokumentumokra** — Új verzió = új dokumentum. Diff,
    revízió-történet nem MVP.
12. **OCR kép- és kézírás-felismerés magas pontosságra hangolva** —
    MVP-ben elegendő egy alap OCR (pl. Tesseract) a digitális PDF-ek
    szövegkinyerésére. Kézzel írt jegyzetek scan-elt felismerése
    nem cél.
13. **Hangbevitel / TTS** — Nem MVP.
14. **Több nyelv támogatása a felületen** — A felület és a kérdés-válasz
    rendszer **magyar** nyelven működik. Más nyelvű dokumentumokat fel
    lehet tölteni (nyelvdetektálás van), de a UI és a Q&A magyar.

---

## 6. Sikermetrikák (MVP)

A „kész és működik” objektíven mérhető:

- **Aktiválás:** A felhasználó az első héten feltölt legalább 10 dokumentumot
  és létrehoz legalább 3 emlékeztetőt.
- **Q&A pontosság:** 10 kontrollált kérdésből legalább 8-ra a rendszer
  helyes választ ad, hivatkozással a forrás-rekordra.
- **Határidő-megbízhatóság:** Egyetlen aktiválva tartott emlékeztető sem
  vész el: az esedékes emlékeztető legkésőbb a rendszer következő
  indulásakor tüzel (catch-up), és a 14 napnál régebben lecsúszottak is
  láthatóan megjelennek a „lecsúszott" összesítőben.
- **Adatszuverenitás:** Az MVP-ben működik a teljes lánc úgy, hogy
  **egyetlen byte sem** megy ki külső AI szolgáltatóhoz (lokális Ollama
  + lokális storage + lokális DB).
- **Felhasználói percepció:** A felhasználó és házastársa szubjektíven
  azt mondja, hogy *„most már egy helyen van.”*

---

## 7. Feltételezések és rögzített döntések

### Feltételezések (a brief alapján, döntésként rögzítve)
- **Kis család**, ~2–6 felhasználó. Self-hosted, **Docker Compose otthoni
  PC-n** (egyetlen gép). NAS / dedikált szerver nem cél.
- **Hardver — nem mindig elérhető:** az otthoni PC, amin az Ollama és a backend
  fut, **nem garantáltan üzemel 24/7**. A rendszernek úgy kell viselkednie,
  hogy amikor a PC elérhető, **feldolgozza a stackelődött (várakozó)
  feladatokat** (feltöltések, AI elemzések, esedékessé vált emlékeztetők).
  Következmény: minden hosszú futású munka durable queue-ra megy
  (Hangfire vagy Quartz a Postgres-en), újraindítást túlélően.
  Az emlékeztetők „lemaradt ablak”-kezelést kapnak: ha esedékes lett, amíg
  a PC offline volt, az első indulás után tüzelnek (catch-up logika).
- A felhasználók ismerik a Google login folyamatát. Az **admin technikailag
  jártas user** (Docker Compose indítás, alap konfiguráció, image frissítés
  vállalható).
- **Internet-függés a loginnál (vállalt korlát):** a Google-bejelentkezés
  kifelé irányuló internetkapcsolatot igényel — ha otthon az internet
  megszakad (a LAN él), új bejelentkezés nem lehetséges; a 30 napos
  sliding session a már bejelentkezett eszközökön mérsékli. Vészhelyzeti
  helyi auth-fallback nem MVP.
- **AI-modell alapértelmezés:** `llama3.2:3b` (fut 4–8 GB RAM-on);
  erős hardveren `gpt-oss:20b` konfigurálható a jobb magyar minőségért
  ([ADR-0006](decisions/ADR-0006-ai-modell-llama32.md)).
- **Magyar nyelv mindenhol:** a UI, a kérdés-válasz és a feldolgozott
  **dokumentumok is magyar nyelvűek**. Többnyelvű dokumentum-feldolgozás
  nem cél — alkalmi nem-magyar dokumentum lehetséges, de optimalizálás
  csak magyarra történik.

### Architekturális döntések (ADR-ekben rögzítve)

| Téma | Döntés | ADR |
|---|---|---|
| Vektor-tárolás | **pgvector** a meglévő PostgreSQL-en, nincs külön vektor-DB. | [ADR-0001](decisions/ADR-0001-vektor-tarolas-pgvector.md) |
| OCR motor | **Tesseract** (lokális, ingyenes), magyar nyelvi csomaggal. | [ADR-0002](decisions/ADR-0002-ocr-tesseract.md) |
| Mobil ↔ lokális AI kapcsolat | **Csak LAN.** A mobil eszközök csak otthon, helyi hálózaton érik el az AI-szervert. Távoli/VPN elérés nem cél. | [ADR-0003](decisions/ADR-0003-mobil-csak-lan.md) |
| Email integráció | **Gmail API** (OAuth 2.0), nem IMAP. | [ADR-0004](decisions/ADR-0004-email-gmail-api.md) |
| Login flow | Kliens-oldali Google id_token POST; nincs login-redirect. | [ADR-0005](decisions/ADR-0005-auth-flow-id-token.md) |
| AI-modell | Default `llama3.2:3b`; `gpt-oss:20b` opció erős hardveren. | [ADR-0006](decisions/ADR-0006-ai-modell-llama32.md) |
| Child RBAC | Csak olvasás; láthatóság = hozzá kötött, nem-privát rekordok. | [ADR-0007](decisions/ADR-0007-child-szerepkor-rbac.md) |
| Workers realtime | MVP-ben nincs cross-process push; polling/refresh. | [ADR-0008](decisions/ADR-0008-workers-realtime-jelzes.md) |
| Reminder-generálás | Deadline-jóváhagyáskor; egy reminder = egy csatorna. | [ADR-0009](decisions/ADR-0009-reminder-generalas-es-csatorna.md) |
| Deployment | Docker Compose a szállítási cél; Helm nem MVP (factory-kapu kivétel). | [ADR-0010](decisions/ADR-0010-compose-first-helm-kivetel.md) |

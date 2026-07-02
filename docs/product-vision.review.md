# Review — product-vision.md

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Erős, konkrét termékvízió: mérhető use case-ek (UC-01…UC-08), explicit
non-goal lista (14 tétel), objektív sikermetrikák, és a feltételezések
döntésként rögzítve. A privacy-first elv konzekvensen végigvonul. A doksi
a docs/ készlet egyik legjobb darabja. Az alábbiak finomítások.

## Észrevételek

### 1. UC-08 (Gmail) státusza kétértelmű (közepes)
Az UC-08 „későbbi MVP-fázis” címkével szerepel az *elsődleges MVP use
case-ek* között, miközben a non-goals nem zárja ki. A brief (idea.md)
MVP-nek mondja. Három doksi háromféleképp kezeli — a backlogban K epicként
Could/Should prioritással szerepel. Javaslat: itt is egyértelműen „MVP-n
kívüli, első bővítés” besorolás, vagy vegyük ki az MVP use case listából.

### 2. Google login vs. offline működés (közepes)
A feltételezések rögzítik, hogy az otthoni PC nem mindig elérhető, és a
rendszer LAN-only — de a **Google OAuth internetet igényel**. Ha otthon
megszakad az internet (a LAN él), senki nem tud bejelentkezni (a 30 napos
sliding cookie mérsékli, de új eszközön/lejáratkor blokkol). Ezt a
kockázatot érdemes explicit feltételezésként rögzíteni, vagy fallback
auth-ot (pl. helyi admin jelszó vészhelyzetre) a roadmapre tenni.

### 3. Sikermetrika vs. email-emlékeztető (kicsi)
A „Határidő-megbízhatóság” metrika azt mondja, egyetlen emlékeztető sem
maradhat ki „a megadott időpontban” — ugyanakkor a hardver-feltételezés
szerint a PC offline lehet, ilyenkor a catch-up *késleltetve* tüzel.
A metrika így szó szerint nem teljesíthető. Pontosítás: „legkésőbb a
rendszer következő indulásakor tüzel, és nem vész el”.

### 4. Szerepkör-tábla vs. security-privacy.md (kicsi)
Itt: az adult „nem láthat private jelölt rekordot, ha nem ő a tulajdonos”.
A domain-model MedicalRecord szabálya viszont a *partnert* (Adult role)
is beengedi az érintett családtag rekordjaiba. A pontos row-level
mátrix a security-privacy.md-ben van — ide elég egy hivatkozás, de a
két megfogalmazás ne mondjon ellent (most enyhén ellentmond).

### 5. Apróság
- A „Non-goals” 3. pontja (push notification) és a Reminder `Channel`
  enum (InApp, Email) konzisztens — rendben.
- Verziószám a fejlécben v0.3, de nincs changelog — a database-schema.md
  mintájára hasznos lenne egy rövid „változások” szakasz.

## Verdikt

Kiadható vízió-dokumentum; a Gmail-scope és az offline-login kockázat
tisztázása után stabil alapja a gyártásnak.

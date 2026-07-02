# Review — ADR-0004 (Gmail API)

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Jó ADR: a Gmail API vs. IMAP mérlegelés érdemi (címke-alapú szelektív
beszívás, scope-granularitás, későbbi Pub/Sub push), és a
`family-os/import` címke mint explicit felhasználói gesztus szépen
illeszkedik a privacy-first elvhez. A read-only scope döntése
konzisztens a security-privacy.md 10.1-gyel.

## Észrevételek

1. **Az import-címke eltávolítása** — a Következmények megemlíti az
   opcionális `gmail.modify` scope-ot, de nem dönt: MVP-ben a feldolgozott
   üzenetről a címke a Gmailben rajta marad (újra-szinkronnál a
   `(source_id, gmail_message_id)` unique véd a duplikátumtól — a séma ezt
   fedi). Egy mondatban érdemes explicit rögzíteni: MVP = readonly, címke
   marad, dedup a DB-ben.
2. **Vendor lock-in jelzés** — a döntés Gmail-specifikus; az utolsó pont
   (Outlook/ProtonMail későbbi ADR) ezt jól kezeli. Az
   `IEmailIngestionService` absztrakció (architecture.md 4.3) a
   Következmények közé kívánkozik, mint a jövőbeli bővítés kapuja.
3. **Testing-mód refresh-token csapda** — a DELIVERY.md 3d jól
   dokumentálja a 7 napos lejáratot; egy hivatkozás ide is elférne, mert
   üzemeltetési következménye a döntésnek.

## Verdikt

Elfogadva státusz indokolt; apró kiegészítések elegendők.

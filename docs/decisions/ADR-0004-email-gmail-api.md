# ADR-0004 — Email integráció: Gmail API

- Státusz: Elfogadva
- Dátum: 2026-06-26
- Döntéshozó: kmarko.net@gmail.com

## Kontextus
A Family OS-nek be kell tudnia szívni szelektíven emaileket (visszaigazolások,
foglalások, iskolai értesítők, számlák), ugyanazon a feldolgozó pipeline-on
átfuttatva, mint a feltöltött dokumentumokat. Dönteni kellett: Gmail API
(OAuth 2.0) vagy generikus IMAP.

## Döntés
**Gmail API** OAuth 2.0 hitelesítéssel. IMAP-ot **nem** támogatunk az MVP-ben.

## Indoklás
- A felhasználó Gmail-fiókot használ; az auth amúgy is Google login (lásd brief).
  Ugyanaz az OAuth-konfiguráció bővíthető a Gmail scope-okkal.
- Gmail API natívan kezeli a címkéket (labels), amelyek a szelektív beszívás
  alapját adják: csak az `family-os/import` címkével ellátott üzenetek kerülnek be.
  IMAP-pal a címkéket folder-ekre kéne fordítani, ami törékeny.
- Granulárisabb scope-ok (read-only beolvasás `gmail.readonly`-vel), nincs
  szükség jelszóra vagy app-jelszóra a felhasználótól.
- Push értesítések (Gmail Pub/Sub watch) később hozzáadhatók, anélkül hogy
  pollozni kellene — IMAP IDLE-lel ez sokkal körülményesebb.

## Következmények
- A Google Cloud projektben engedélyezni kell a Gmail API-t, és kérni kell
  a megfelelő OAuth scope-okat (`gmail.readonly`, opcionálisan `gmail.modify`
  ha a beszívás után el akarjuk távolítani az import-címkét).
- Az MVP-ben **csak olvasás** (`gmail.readonly`). Címkemódosítás, küldés,
  törlés nem cél.
- A felhasználónak a Gmail-fiókjában manuálisan kell létrehoznia és
  ráhúznia az `family-os/import` címkét azokra az üzenetekre, amelyeket
  be akar húzni — ez **explicit** felhasználói gesztus, megfelel a privacy-first
  elvnek.
- A Gmail API-hoz tartozó OAuth verification (külső felhasználók esetén)
  itt **nem releváns**, mert single-tenant, self-hosted, és a felhasználó
  saját Google Cloud projektjében kell beállítania.
- Külön Google fiók típusú accountokra (pl. Outlook, ProtonMail) az MVP
  nem terjed ki — későbbi ADR-rel bővíthető.

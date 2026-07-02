# ADR-0005 — Google auth flow: kliens-oldali id_token POST

- Státusz: Elfogadva (a megvalósult kód rögzítése)
- Dátum: 2026-07-02
- Döntéshozó: kmarko.net@gmail.com (dokumentum-audit nyomán rögzítve)

## Kontextus

Három dokumentum három különböző Google-bejelentkezési folyamatot írt le:
- `api-design.md` 3.1: kliens-oldali Google Identity Services (GIS) →
  `POST /api/v1/auth/login/google { idToken }`.
- `security-privacy.md` 3.1: „Authorization Code with PKCE" (szerver-oldali
  redirect flow).
- `DELIVERY.md` 3a: szerver-oldali callback redirect URI
  (`/api/v1/auth/google/callback`).

A megvalósult kód (`AuthModule.cs`) a **kliens-oldali id_token POST**
változatot implementálja.

## Döntés

**Kliens-oldali GIS id_token flow.** A frontend a Google Identity Services
JS-könyvtárral szerez `id_token`-t, és a
`POST /api/v1/auth/login/google { idToken }` végpontra küldi. A backend
validálja (aláírás, issuer, expiry, audience), majd HttpOnly session
cookie-t bocsát ki. Szerver-oldali OAuth-redirect a *bejelentkezéshez*
nincs.

**Kivétel:** a Gmail-integráció (K1) továbbra is szerver-oldali
authorization-code flow-t használ (ott refresh token kell) — az a
`/api/v1/sources/gmail/...` végpontokon él, a login-tól függetlenül.

## Indoklás

- Egyszerűbb: nincs szerver-oldali state/PKCE-kezelés a loginhoz.
- A GIS könyvtár kezelést, one-tap-et, session-megújítást ad.
- Az id_token rövid életű; a backend session cookie-ja a tényleges
  hitelesítés — refresh tokenre a loginhoz nincs szükség.

## Következmények

- Google Cloud Console-ban a loginhoz csak **Authorized JavaScript
  origins** kell (`https://family-os.lan`); login-redirect-URI **nem**.
  Redirect URI kizárólag a Gmail-connect flow-hoz kell.
- `security-privacy.md` 3.1 és `DELIVERY.md` 3a ehhez igazítva.
- E2E tesztekhez a Google-flow nem automatizálható → teszt-only
  dev-login végpont kérdése külön döntés (lásd qa/ui-test-scenarios.md 2.).

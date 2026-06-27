# ADR-0003 — Mobil ↔ lokális AI: csak LAN-on

- Státusz: Elfogadva
- Dátum: 2026-06-26
- Döntéshozó: kmarko.net@gmail.com

## Kontextus
Az `idea.md` brief eredetileg felvetett egy hibrid modellt, ahol a telefon
észleli az otthoni hálózatot, és ha igen, átadja a feladatokat a lokális AI
szervernek; ha nem, akkor saját vagy cloud AI-t használ. Dönteni kellett,
milyen mértékben támogassuk a távoli/otthonon kívüli elérést.

Vizsgált alternatívák:
- (A) Csak LAN: a mobil app csak otthoni Wi-Fi-n / helyi hálózaton éri el a backendet.
- (B) VPN mesh (pl. Tailscale, WireGuard): otthonon kívülről is elérhető a saját szerver.
- (C) Publikus végpont reverse proxy-val + dynamic DNS-szel.
- (D) Cloud relay / push-szerver közbeiktatása.

## Döntés
**(A) Csak LAN.** A mobil és a webes kliensek csak akkor érik el a Family OS
backendet, ha a helyi hálózaton vannak. Távoli elérés (VPN, publikus végpont,
relay) **nem cél**, sem MVP-ben, sem a közvetlen roadmapen.

## Indoklás
- A privacy-first elv ezzel a legszigorúbb: a rendszer támadási felülete
  a háztartási hálózatra korlátozódik. Nincs publikus port, nincs külső
  cloud-relay, nincs identity-szivárgási kockázat.
- A self-hosted családi tipikus használat (PC/NAS otthon mindig elérhető)
  a LAN-elérést is bőven kielégítően lefedi.
- Sokkal egyszerűbb üzemeltetés: nincs VPN-konfiguráció, nincs TLS-tanúsítvány
  külső doménre, nincs dynamic DNS, nincs port-forward.
- A mobil offline használat (cache, sorba állított feltöltés) későbbi feature,
  függetlenül attól, hogy a backend távolról elérhető-e.

## Következmények
- A mobil/web kliens hálózat-detektálási logikája egyszerű: backend URL elérhető
  a LAN-on → működik; nem elérhető → "otthon kívül vagy, ez a funkció helyi
  hálózatot igényel" üzenet.
- A backend Docker Compose alapértelmezésben csak a LAN-interfészen hallgat.
- TLS LAN-on belül belső CA-val vagy mDNS + Let's Encrypt DNS-01 challenge-szel
  oldható meg (későbbi, security-privacy ADR).
- **Tudatosan vállalt korlátozás:** a felhasználó otthonon kívülről nem fér
  hozzá a dokumentumokhoz. Ha ez a jövőben fontossá válik, új ADR módosíthatja
  ezt a döntést (pl. Tailscale opcionális engedélyezésével).

# Review — ADR-0003 (Mobil ↔ AI: csak LAN)

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

A készlet legfontosabb ADR-je: a négy alternatíva (LAN / VPN mesh /
publikus végpont / cloud relay) tisztán mérlegelt, a döntés a
privacy-first elvvel és az üzemeltetési egyszerűséggel jól indokolt, a
vállalt korlátozás (otthonon kívül nincs hozzáférés) explicit. A döntés
következetesen végigvonul a doksikon (deployment, frontend
„nem vagy otthon” képernyő, CORS, security fenyegetés-modell).

## Észrevételek

1. **Architektúra-ellentmondás: „Tailscale-kompatibilis bridge”** — az
   architecture.md 9. szakasza az `api` service-nél „csak LAN interfész +
   Tailscale-kompatibilis bridge”-et említ. Az ADR viszont a VPN mesh-t
   (B alternatíva, Tailscale nevesítve) explicit elvetette, és csak egy
   *jövőbeli új ADR* nyithatja újra. A „Tailscale-kompatibilis” kitétel az
   architektúrából törlendő, vagy itt kell egy mondat, hogy az előkészítés
   (bind-konfiguráció) megengedett, a használat nem.
2. **TLS-döntés ADR nélkül maradt** — a Következmények „későbbi
   security-privacy ADR”-t ígér a belső CA vs. mDNS+DNS-01 kérdésre; a
   belső CA időközben megvalósult (DELIVERY.md 5., `init-tls-ca.sh`) ADR
   nélkül. Érdemes egy rövid ADR-0005-tel utólag rögzíteni, a
   dev-login/E2E-auth döntéssel együtt (qa/ui-test-scenarios.review.md).
3. **Google OAuth internet-függése** — a LAN-only elv mellett a login
   internetet igényel; ez nem mond ellent az ADR-nek (kifelé irányuló
   forgalom), de a „támadási felület a háztartási hálózatra korlátozódik”
   állítás mellett egy mondatban érdemes rögzíteni ezt a kivételt
   (lásd product-vision.review.md #2).

## Verdikt

A döntés jó és jól dokumentált; az #1 architektúra-szinkron elvégzendő.

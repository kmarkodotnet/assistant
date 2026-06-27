# Epic K — Beállítások + integrációk — Frontend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` §9, §10
> - `frontend-structure.md` §8.10 (Settings page)
> - `api-design.md` §17 (Sources), §18 (AI providers), §21 (Settings)
> - `security-privacy.md` §8 (AI privacy — UI jelzés), §10 (külső integrációk)
>
> **Story-k:** K1 (FE), K2 (FE), K3 (FE)
> **Fázis:** Fázis 12

---

## Áttekintés

Settings oldal admin szekcióval:
- **Integrációk** — Gmail OAuth connect/disconnect, manuális sync.
- **AI providerek** — lista, enable/disable, PrivacyMode read-only jelzés.
- **Backup info** — háttér-script státusza, utolsó backup ideje, restore
  útmutató link.

## Taskok

### T-KFE-01 — Settings + Sources + Providers API client
- **Fájlok:**
  - `frontend/src/app/features/settings/services/sources.api.ts`
  - `frontend/src/app/features/settings/services/providers.api.ts`
  - `frontend/src/app/features/settings/services/system-settings.api.ts`
- **AC:**
  - [ ] CRUD + connect/sync/disconnect.

### T-KFE-02 — Integrációk tab a Settings oldalon
- **Fájlok:**
  - `frontend/src/app/features/settings/pages/integrations.page.ts`
  - `frontend/src/app/features/settings/components/source-card.component.ts`
- **AC:**
  - [ ] Gmail-source kártya: connected? igen/nem.
  - [ ] „Csatlakoztatás" gomb → Google OAuth redirect.
  - [ ] „Sync most" gomb manuális trigger.
  - [ ] „Lecsatlakozom" gomb confirm dialog-gal.
  - [ ] Utolsó sync ideje (`huRelativeDate`).

### T-KFE-03 — AI providerek tab
- **Fájlok:**
  - `frontend/src/app/features/settings/pages/ai-providers.page.ts`
  - `frontend/src/app/features/settings/components/provider-card.component.ts`
- **AC:**
  - [ ] Provider kártyák: name, model select, enabled toggle.
  - [ ] Health badge (zöld / sárga / piros).
  - [ ] PrivacyMode kijelző: „LocalOnly" + lakat ikon + magyar info-szöveg:
        „Adatvédelmi okból ez a beállítás kódba van égetve. Cloud
        provider használata MVP-ben nem támogatott."

### T-KFE-04 — Rendszer-beállítások tab (admin)
- **Fájlok:**
  - `frontend/src/app/features/settings/pages/system-settings.page.ts`
- **AC:**
  - [ ] SMTP konfiguráció (host, port, from) — titok nélkül.
  - [ ] Retention beállítás (audit, notification feed).
  - [ ] Default csendes órák (új user default).
  - [ ] Mentés gomb.

### T-KFE-05 — Backup info widget
- **Fájlok:**
  - `frontend/src/app/features/settings/components/backup-info.component.ts`
- **AC:**
  - [ ] Utolsó backup dátum + méret.
  - [ ] Backup-manifest hash (információs jelleggel).
  - [ ] Link a `DELIVERY.md` restore szakaszára.

### T-KFE-06 — Integráció a Settings shell-be
- **Cél:** a Settings oldal tab-okkal teljes (saját / rendszer / integrációk
  / AI / backup).
- **Fájlok:**
  - kiegészítés `settings.page.ts`-ben.
- **AC:**
  - [ ] Adult: csak „Saját" tab.
  - [ ] Admin: minden tab.

### T-KFE-07 — Gmail OAuth callback handling
- **Fájlok:**
  - `frontend/src/app/features/settings/pages/oauth-callback.page.ts`
- **AC:**
  - [ ] Callback URL: `/settings/oauth-callback?code=...`.
  - [ ] Backend POST → success → redirect az `integrations` tabra +
        toast.

### T-KFE-08 — Tesztek
- **Fájlok:**
  - `frontend/src/app/features/settings/pages/integrations.page.spec.ts`
  - `frontend/src/app/features/settings/pages/ai-providers.page.spec.ts`
- **AC:**
  - [ ] Integráció card-ok render.
  - [ ] PrivacyMode read-only állapot.
  - [ ] Connect gomb redirect-et trigger.

---

## Megvalósítási sorrend

```
T-KFE-01                              (data)
       → 02 → 07                      (Integrációk + OAuth callback)
       → 03                            (AI providers)
       → 04 → 05                       (Rendszer + backup info)
       → 06                            (shell integráció)
       → 08                            (tesztek)
```

## Epic-DoD

- [ ] Gmail csatlakoztatható UI-on keresztül.
- [ ] AI providerek tab működik, PrivacyMode védve és vizuálisan jelezve.
- [ ] Backup info kártya admin-on látható.
- [ ] Settings minden tab szerepkör-tudatosan renderelődik.

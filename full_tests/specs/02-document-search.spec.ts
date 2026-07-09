/**
 * UC02 — Dokumentum keresése UI-on keresztül (chat-alapú keresési oldal)
 *
 * A /search oldal chat-szerű felületen fogadja a keresési lekérdezéseket.
 * Az oldal betöltésekor üres állapotot mutat, majd a felhasználó begépeli
 * a keresést és elküldi. A válasz az előzmények listájában jelenik meg.
 */
import { test, expect } from '@playwright/test';

test.describe('UC02 — Document search UI', () => {

  test('UC02-1 keresési oldal betölt, üres állapot látható @smoke', async ({ page }) => {
    await test.step('Keresési oldal megnyitása', async () => {
      await page.goto('/search');
      await expect(page.getByRole('heading', { name: 'Keresés' })).toBeVisible({ timeout: 15_000 });
    });

    await test.step('Keresési input és küldés gomb megjelenik', async () => {
      await expect(page.getByTestId('search-input')).toBeVisible();
      await expect(page.getByTestId('search-submit')).toBeVisible();
    });

    await test.step('Üres állapot üzenet látható', async () => {
      await expect(page.getByText('Kérdezz bármit a dokumentumaidról')).toBeVisible();
    });
  });

  test('UC02-2 mód-választó megjelenik és váltható', async ({ page }) => {
    await test.step('Keresési oldal megnyitása', async () => {
      await page.goto('/search');
    });

    await test.step('Mód-választó alapértelmezetten Auto', async () => {
      const modeSelect = page.getByTestId('search-mode-select');
      await expect(modeSelect).toBeVisible();
      await expect(modeSelect).toHaveValue('Auto');
    });

    await test.step('Váltás Szöveges módra', async () => {
      await page.getByTestId('search-mode-select').selectOption('Text');
      await expect(page.getByTestId('search-mode-select')).toHaveValue('Text');
    });

    await test.step('Váltás Szemantikus módra', async () => {
      await page.getByTestId('search-mode-select').selectOption('Semantic');
      await expect(page.getByTestId('search-mode-select')).toHaveValue('Semantic');
    });
  });

  test('UC02-3 FTS keresés elindítása és eredmény megjelenítése', async ({ page }) => {
    await test.step('Keresési oldal megnyitása', async () => {
      await page.goto('/search');
    });

    await test.step('Szöveges mód beállítása', async () => {
      await page.getByTestId('search-mode-select').selectOption('Text');
    });

    await test.step('Keresési szöveg beírása és elküldése', async () => {
      await page.getByTestId('search-input').fill('teszt dokumentum');
      await page.getByTestId('search-submit').click();
    });

    await test.step('Felhasználói üzenet megjelenik az előzményekben', async () => {
      await expect(page.getByText('teszt dokumentum')).toBeVisible({ timeout: 30_000 });
    });

    await test.step('Betöltési jelző eltűnik', async () => {
      await expect(page.locator('.animate-bounce').first()).not.toBeVisible({ timeout: 30_000 });
    });
  });

  test('UC02-4 szemantikus keresés elindítása', async ({ page }) => {
    await test.step('Keresési oldal megnyitása', async () => {
      await page.goto('/search');
    });

    await test.step('Szemantikus keresés elküldése', async () => {
      await page.getByTestId('search-mode-select').selectOption('Semantic');
      await page.getByTestId('search-input').fill('teszt');
      await page.getByTestId('search-submit').click();
    });

    await test.step('Felhasználói üzenet megjelenik az előzményekben', async () => {
      await expect(page.getByText('teszt').first()).toBeVisible({ timeout: 30_000 });
    });

    await test.step('Betöltési jelző eltűnik', async () => {
      await expect(page.locator('.animate-bounce').first()).not.toBeVisible({ timeout: 30_000 });
    });
  });

  test('UC02-5 előzmények törlése gomb működik', async ({ page }) => {
    await test.step('Keresési oldal megnyitása', async () => {
      await page.goto('/search');
    });

    await test.step('Keresés elküldése (előzmények megjelenítéséhez)', async () => {
      await page.getByTestId('search-input').fill('keresés törlés teszt');
      await page.getByTestId('search-submit').click();
      await expect(page.getByTestId('search-clear-history')).toBeVisible({ timeout: 30_000 });
    });

    await test.step('Előzmények törlése', async () => {
      await page.getByTestId('search-clear-history').click();
    });

    await test.step('Üres állapot visszaáll', async () => {
      await expect(page.getByText('Kérdezz bármit a dokumentumaidról')).toBeVisible();
    });
  });

  test('UC02-6 Enter billentyűvel is elküldhető a keresés', async ({ page }) => {
    await test.step('Keresési oldal megnyitása', async () => {
      await page.goto('/search');
    });

    await test.step('Keresés elküldése Enter-rel', async () => {
      const input = page.getByTestId('search-input');
      await input.fill('enter teszt keresés');
      await input.press('Enter');
    });

    await test.step('Keresés szövege megjelenik az előzményekben', async () => {
      await expect(page.getByText('enter teszt keresés')).toBeVisible({ timeout: 30_000 });
    });
  });
});

/**
 * UI Smoke tesztek — ellenőrzi, hogy a főbb oldalak betöltenek
 * 500 hiba nélkül.
 *
 * Ezek a tesztek az admin storage state-et használják (global-setup.ts-ből),
 * amely valódi cookie-alapú autentikációt biztosít.
 * Az /api/v1/auth/me hívás is valódi lesz (nem mock) — a cookie tartalmazza
 * a bejelentkezett admint.
 *
 * Ha az Angular frontend a dev-szerveren fut (port 4200), az nginx proxy
 * a http://localhost:80 alá irányítja a kéréseket.
 */
import { test, expect } from '@playwright/test';

/** Smoke: navigál az adott oldalra és ellenőrzi, hogy betölt */
async function smokeCheck(
  page: import('@playwright/test').Page,
  url: string,
  label: string
): Promise<void> {
  await page.goto(url);

  // Wait for Angular to finish initial render (no skeleton spinner)
  await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {
    // networkidle may never fire if there are long-poll connections — ignore
  });

  // Must not show a server error page
  const title = await page.title();
  expect(title.toLowerCase(), `${label} title must not indicate error`).not.toMatch(/500|server error/);

  // Must not show an error banner/toast with HTTP 500 text
  const errorText = page.getByText(/500|internal server error/i);
  await expect(errorText, `${label} must not show 500 error`).not.toBeVisible();
}

test.describe('UI Smoke — page load checks @smoke', () => {
  test('Dashboard (/) loads without 500', async ({ page }) => {
    await smokeCheck(page, '/', 'Dashboard');
  });

  test('/tasks loads without 500', async ({ page }) => {
    await smokeCheck(page, '/tasks', 'Tasks');
  });

  test('/deadlines loads without 500', async ({ page }) => {
    await smokeCheck(page, '/deadlines', 'Deadlines');
  });

  test('/documents loads without 500', async ({ page }) => {
    await smokeCheck(page, '/documents', 'Documents');
  });

  test('/search loads without 500', async ({ page }) => {
    await smokeCheck(page, '/search', 'Search');
  });

  test('/suggestions loads without 500', async ({ page }) => {
    await smokeCheck(page, '/suggestions', 'Suggestions');
  });

  test('/admin loads without 500 (admin user)', async ({ page }) => {
    await smokeCheck(page, '/admin', 'Admin');
  });
});

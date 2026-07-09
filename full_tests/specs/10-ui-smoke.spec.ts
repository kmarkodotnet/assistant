/**
 * UI Smoke tesztek — ellenőrzi, hogy a főbb oldalak betöltenek
 * 500 hiba nélkül, és az oldalak fő elemei megjelennek.
 *
 * Az admin storage state valódi cookie-alapú autentikációt biztosít.
 * Minden route-ot ellenőrzünk: dashboard, dokumentumok, feladatok,
 * határidők, jegyzetek, keresés, javaslatok, admin oldalak, beállítások.
 */
import { test, expect } from '@playwright/test';

/**
 * Smoke check: navigál az adott oldalra, ellenőrzi, hogy betölt és
 * nem mutat 500-as hibát; opcionálisan ellenőriz egy elvárt elemet is.
 */
async function smokeCheck(
  page: import('@playwright/test').Page,
  url: string,
  label: string,
  expectedText?: string
): Promise<void> {
  await test.step(`Navigálás: ${url}`, async () => {
    await page.goto(url);
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {
      // A networkidle esetleg nem áll be long-poll kapcsolatoknál — ignorálható
    });
  });

  await test.step(`Hibamentesség ellenőrzése: ${label}`, async () => {
    const title = await page.title();
    expect(title.toLowerCase(), `${label}: a cím nem jelezhet hibát`).not.toMatch(/500|server error/);
    await expect(
      page.getByText(/500|internal server error/i),
      `${label}: ne legyen 500-as hibaüzenet`
    ).not.toBeVisible();
  });

  if (expectedText) {
    await test.step(`"${expectedText}" szöveg látható`, async () => {
      await expect(
        page.getByText(expectedText, { exact: false }),
        `${label}: "${expectedText}" szöveg megjelenik`
      ).toBeVisible({ timeout: 15_000 });
    });
  }
}

test.describe('UI Smoke — oldalak betöltési ellenőrzése @smoke', () => {

  test('Dashboard (/) betölt hiba nélkül', async ({ page }) => {
    await smokeCheck(page, '/', 'Dashboard');
  });

  test('/tasks betölt, fejléc és létrehozás gomb látható', async ({ page }) => {
    await smokeCheck(page, '/tasks', 'Tasks', 'Feladatok');
    await test.step('Létrehozás gomb megjelenik', async () => {
      await expect(page.getByTestId('task-create-btn')).toBeVisible();
    });
  });

  test('/deadlines betölt, fejléc és létrehozás gomb látható', async ({ page }) => {
    await smokeCheck(page, '/deadlines', 'Deadlines', 'Határidők');
    await test.step('Létrehozás gomb megjelenik', async () => {
      await expect(page.getByTestId('deadline-create-btn')).toBeVisible();
    });
  });

  test('/documents betölt, feltöltés gomb látható', async ({ page }) => {
    await smokeCheck(page, '/documents', 'Documents');
    await test.step('Feltöltés gomb megjelenik', async () => {
      await expect(page.getByTestId('documents-upload-btn')).toBeVisible();
    });
  });

  test('/documents/upload betölt, dropzone látható', async ({ page }) => {
    await smokeCheck(page, '/documents/upload', 'Document Upload', 'Dokumentum feltöltése');
    await test.step('Dropzone megjelenik', async () => {
      await expect(page.getByTestId('documents-dropzone')).toBeVisible();
    });
  });

  test('/search betölt, input és küldés gomb látható', async ({ page }) => {
    await smokeCheck(page, '/search', 'Search', 'Keresés');
    await test.step('Keresési input és küldés gomb megjelenik', async () => {
      await expect(page.getByTestId('search-input')).toBeVisible();
      await expect(page.getByTestId('search-submit')).toBeVisible();
    });
  });

  test('/suggestions betölt, fejléc látható', async ({ page }) => {
    await smokeCheck(page, '/suggestions', 'Suggestions', 'AI javaslatok');
  });

  test('/notes betölt, fejléc látható', async ({ page }) => {
    await smokeCheck(page, '/notes', 'Notes', 'Feljegyzések');
  });

  test('/family betölt, hozzáadás gomb látható', async ({ page }) => {
    await smokeCheck(page, '/family', 'Family');
    await test.step('Hozzáadás gomb megjelenik', async () => {
      await expect(page.getByTestId('family-add-btn')).toBeVisible({ timeout: 15_000 });
    });
  });

  test('/admin/ai-jobs betölt (admin felhasználóval)', async ({ page }) => {
    await smokeCheck(page, '/admin/ai-jobs', 'Admin AI Jobs');
    await test.step('"Automatikus frissítés" felirat látható', async () => {
      await expect(page.getByText('Automatikus frissítés: 30 mp')).toBeVisible({ timeout: 15_000 });
    });
  });

  test('/admin/audit-log betölt (admin felhasználóval)', async ({ page }) => {
    await smokeCheck(page, '/admin/audit-log', 'Admin Audit Log');
  });

  test('/settings betölt hiba nélkül', async ({ page }) => {
    await smokeCheck(page, '/settings', 'Settings');
  });
});

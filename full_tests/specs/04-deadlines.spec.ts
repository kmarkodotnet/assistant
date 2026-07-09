/**
 * UC05 — Határidő kezelése UI-on keresztül (CRUD)
 */
import { test, expect } from '@playwright/test';
import { apiDelete, apiGet } from '../helpers/api';

test.describe('UC05 — Deadlines CRUD UI', () => {
  let deadlineId: string;

  test.afterAll(async ({ request }) => {
    if (deadlineId) {
      await apiDelete(request, `/deadlines/${deadlineId}`).catch(() => { /* already deleted */ });
    }
  });

  test('UC05-1 határidő létrehozása UI-on keresztül', async ({ page }) => {
    await test.step('Határidők oldal megnyitása', async () => {
      await page.goto('/deadlines');
      await expect(page.getByRole('heading', { name: 'Határidők' })).toBeVisible();
    });

    await test.step('Létrehozó dialog megnyitása', async () => {
      await page.getByTestId('deadline-create-btn').click();
      // Csak az h3-at matcheljük, nem a gomb "+ Új határidő" szövegét
      await expect(page.getByRole('heading', { name: 'Új határidő', exact: true })).toBeVisible();
    });

    await test.step('Form kitöltése (cím, dátum, kategória)', async () => {
      await page.getByTestId('deadline-form-title').fill('E2E Teszt Határidő');
      await page.getByTestId('deadline-form-dueDate').fill('2027-06-15');
      await page.locator('select[formControlName="category"]').selectOption('Insurance');
    });

    await test.step('Határidő mentése', async () => {
      await page.getByTestId('deadline-form-submit').click();
      await expect(page.getByRole('heading', { name: 'Új határidő', exact: true })).not.toBeVisible({ timeout: 5_000 });
    });

    await test.step('Határidő megjelenik a listán', async () => {
      await expect(page.getByText('E2E Teszt Határidő')).toBeVisible({ timeout: 10_000 });
    });

    await test.step('Határidő ID kinyerése API-n keresztül', async () => {
      const resp = await apiGet(page.request, '/deadlines');
      expect(resp.ok()).toBeTruthy();
      const body = await resp.json() as unknown;
      const items: unknown[] = Array.isArray(body)
        ? body
        : (body as Record<string, unknown[]>)['items'] ?? [];
      const found = items.find(d => (d as Record<string, unknown>)['title'] === 'E2E Teszt Határidő');
      expect(found, 'A létrehozott határidőt vissza kell adnia az API-nak').toBeTruthy();
      deadlineId = (found as Record<string, unknown>)['id'] as string;
      console.log(`[UC05-1] deadlineId = ${deadlineId}`);
    });
  });

  test('UC05-2 határidő szerkesztése UI-on keresztül', async ({ page }) => {
    expect(deadlineId, 'UC05-1-nek be kell állítania a deadlineId-t').toBeTruthy();

    await test.step('Határidők oldal megnyitása', async () => {
      await page.goto('/deadlines');
      await expect(page.getByText('E2E Teszt Határidő')).toBeVisible({ timeout: 10_000 });
    });

    await test.step('Szerkesztő dialog megnyitása', async () => {
      const card = page.getByText('E2E Teszt Határidő').locator('../..');
      await card.getByTestId('deadline-edit').click();
      await expect(page.getByRole('heading', { name: 'Határidő szerkesztése' })).toBeVisible();
    });

    await test.step('Cím módosítása', async () => {
      const titleInput = page.getByTestId('deadline-form-title');
      await titleInput.clear();
      await titleInput.fill('Módosított E2E Határidő');
    });

    await test.step('Módosítás mentése', async () => {
      await page.getByTestId('deadline-form-submit').click();
      await expect(page.getByRole('heading', { name: 'Határidő szerkesztése' })).not.toBeVisible({ timeout: 5_000 });
    });

    await test.step('Módosított cím megjelenik a listán', async () => {
      await expect(page.getByText('Módosított E2E Határidő')).toBeVisible({ timeout: 10_000 });
    });
  });

  test('UC05-3 határidő megoldottnak jelölése UI-on keresztül', async ({ page }) => {
    expect(deadlineId, 'UC05-1-nek be kell állítania a deadlineId-t').toBeTruthy();

    await test.step('Határidők oldal megnyitása', async () => {
      await page.goto('/deadlines');
      await expect(page.getByText('Módosított E2E Határidő')).toBeVisible({ timeout: 10_000 });
    });

    await test.step('Megoldva gombra kattintás', async () => {
      const card = page.getByText('Módosított E2E Határidő').locator('../..');
      await card.getByTestId('deadline-resolve').click();
    });

    await test.step('API-ban Resolved státusz ellenőrzése', async () => {
      await page.waitForTimeout(800);
      const resp = await apiGet(page.request, `/deadlines/${deadlineId}`);
      const body = await resp.json() as Record<string, unknown>;
      expect(body['status']).toBe('Resolved');
      console.log(`[UC05-3] status = ${body['status']}`);
    });

    await test.step('Határidő megjelenik a Megoldva szekcióban', async () => {
      await expect(page.getByText('Módosított E2E Határidő')).toBeVisible({ timeout: 5_000 });
    });
  });

  test('UC05-4 kategória szűrő működik', async ({ page }) => {
    await test.step('Határidők oldal megnyitása', async () => {
      await page.goto('/deadlines');
      await expect(page.getByRole('heading', { name: 'Határidők' })).toBeVisible();
    });

    await test.step('Insurance kategória szűrő beállítása', async () => {
      await page.getByTestId('deadlines-filter-category').selectOption('Insurance');
      await page.waitForTimeout(500);
    });

    await test.step('Oldal hiba nélkül frissül', async () => {
      await expect(page.getByRole('heading', { name: 'Határidők' })).toBeVisible();
      await expect(page.getByText(/500|internal server error/i)).not.toBeVisible();
    });
  });

  test('UC05-5 státusz szűrő "Megoldva" működik', async ({ page }) => {
    await test.step('Határidők oldal megnyitása', async () => {
      await page.goto('/deadlines');
      await expect(page.getByRole('heading', { name: 'Határidők' })).toBeVisible();
    });

    await test.step('Megoldva státusz szűrő beállítása', async () => {
      await page.getByTestId('deadlines-filter-status').selectOption('resolved');
      await page.waitForTimeout(500);
    });

    await test.step('Oldal hiba nélkül frissül', async () => {
      await expect(page.getByRole('heading', { name: 'Határidők' })).toBeVisible();
      await expect(page.getByText(/500|internal server error/i)).not.toBeVisible();
    });
  });

  test('UC05-6 Megoldva/Mellőzve szekció összecsukható', async ({ page }) => {
    await test.step('Határidők oldal megnyitása', async () => {
      await page.goto('/deadlines');
      await expect(page.getByRole('heading', { name: 'Határidők' })).toBeVisible();
    });

    await test.step('Szekció összecsukása', async () => {
      const toggle = page.getByText('Megoldva / Mellőzve');
      await expect(toggle).toBeVisible({ timeout: 10_000 });
      await toggle.click();
      await page.waitForTimeout(300);
    });

    await test.step('Szekció visszanyitása', async () => {
      await page.getByText('Megoldva / Mellőzve').click();
      await page.waitForTimeout(300);
    });

    await test.step('Oldal hiba nélkül működik', async () => {
      await expect(page.getByRole('heading', { name: 'Határidők' })).toBeVisible();
    });
  });
});

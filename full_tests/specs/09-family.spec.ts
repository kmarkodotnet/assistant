/**
 * UC15 — Családtag kezelése UI-on keresztül (CRUD)
 *
 * CUD műveletek Admin szerepkört igényelnek.
 * Az API végpont: /api/v1/family-members
 * A UI-n a /family route a FamilyListPage-et tölti be.
 * PATCH optimista konkurencia: rowVersion kell.
 * Cleanup: afterAll API request-tel.
 */
import { test, expect } from '@playwright/test';
import { apiDelete } from '../helpers/api';

test.describe('UC15 — Family members CRUD UI', () => {
  let memberId: string;

  test.afterAll(async ({ request }) => {
    if (memberId) {
      await apiDelete(request, `/family-members/${memberId}`).catch(() => { /* már törölve */ });
    }
  });

  test('UC15-1 családtagok oldal betölt @smoke', async ({ page }) => {
    await test.step('Családtagok oldal megnyitása', async () => {
      await page.goto('/family');
      await expect(page).not.toHaveTitle(/500|error/i, { timeout: 15_000 });
      await expect(page.getByText(/500|internal server error/i)).not.toBeVisible();
    });

    await test.step('Hozzáadás gomb megjelenik', async () => {
      await expect(page.getByTestId('family-add-btn')).toBeVisible({ timeout: 15_000 });
    });
  });

  test('UC15-2 családtag létrehozása UI-on keresztül', async ({ page }) => {
    await test.step('Családtagok oldal megnyitása', async () => {
      await page.goto('/family');
    });

    await test.step('Létrehozó dialog megnyitása', async () => {
      await page.getByTestId('family-add-btn').click();
      await expect(page.getByTestId('family-form-dialog')).toBeVisible({ timeout: 10_000 });
    });

    await test.step('Form kitöltése', async () => {
      await page.getByTestId('family-form-displayName').fill('E2E Teszt Tag');
      await page.getByTestId('family-form-fullName').fill('E2E Teszt Tag Teljes Neve');
      await page.getByTestId('family-form-relation').selectOption('Other');
      await page.getByTestId('family-form-birthDate').fill('1990-01-01');
    });

    await test.step('Családtag mentése', async () => {
      await page.getByTestId('family-form-save').click();
      await expect(page.getByTestId('family-form-dialog')).not.toBeVisible({ timeout: 5_000 });
      await expect(page.getByText('E2E Teszt Tag')).toBeVisible({ timeout: 10_000 });
    });

    await test.step('Családtag ID kinyerése API-n keresztül', async () => {
      const resp = await page.request.get('/api/v1/family-members');
      if (resp.ok()) {
        const body = await resp.json() as unknown;
        const items: unknown[] = Array.isArray(body)
          ? body
          : (body as Record<string, unknown[]>)['items'] ?? [];
        const found = items.find(
          (m) => (m as Record<string, unknown>)['displayName'] === 'E2E Teszt Tag'
        );
        if (found) {
          memberId = (found as Record<string, unknown>)['id'] as string;
        }
      }
      console.log(`[UC15-2] memberId = ${memberId}`);
    });
  });

  test('UC15-3 családtag szerkesztése UI-on keresztül', async ({ page }) => {
    expect(memberId, 'UC15-2-nek be kell állítania a memberId-t').toBeTruthy();

    await test.step('Családtagok oldal megnyitása', async () => {
      await page.goto('/family');
      const memberText = page.getByText('E2E Teszt Tag').first();
      await expect(memberText).toBeVisible({ timeout: 10_000 });
    });

    await test.step('Szerkesztő dialog megnyitása', async () => {
      const memberText = page.getByText('E2E Teszt Tag').first();
      const card = memberText.locator('../..');
      const editBtn = card.getByRole('button').filter({ hasText: /Szerkeszt|edit/i }).first();
      await editBtn.click();
      await expect(page.getByTestId('family-form-dialog')).toBeVisible({ timeout: 10_000 });
    });

    await test.step('Megjelenítési név módosítása és mentése', async () => {
      const displayNameInput = page.getByTestId('family-form-displayName');
      await displayNameInput.clear();
      await displayNameInput.fill('Módosított E2E Tag');
      await page.getByTestId('family-form-save').click();
    });

    await test.step('Módosított név megjelenik a listában', async () => {
      await expect(page.getByTestId('family-form-dialog')).not.toBeVisible({ timeout: 5_000 });
      await expect(page.getByText('Módosított E2E Tag')).toBeVisible({ timeout: 10_000 });
    });
  });

  test('UC15-4 mégse gomb bezárja a dialógot', async ({ page }) => {
    await test.step('Családtagok oldal megnyitása és dialog nyitása', async () => {
      await page.goto('/family');
      await page.getByTestId('family-add-btn').click();
      await expect(page.getByTestId('family-form-dialog')).toBeVisible({ timeout: 10_000 });
    });

    await test.step('Mégse gombra kattintás', async () => {
      await page.getByTestId('family-form-cancel').click();
    });

    await test.step('Dialog bezárul', async () => {
      await expect(page.getByTestId('family-form-dialog')).not.toBeVisible({ timeout: 5_000 });
    });
  });

  test('UC15-5 családtag törlése UI-on keresztül', async ({ page }) => {
    expect(memberId, 'UC15-2-nek be kell állítania a memberId-t').toBeTruthy();

    await test.step('Családtagok oldal megnyitása', async () => {
      await page.goto('/family');
      const memberText = page.getByText('Módosított E2E Tag').first();
      await expect(memberText).toBeVisible({ timeout: 10_000 });
    });

    await test.step('Törlés gombra kattintás', async () => {
      const memberText = page.getByText('Módosított E2E Tag').first();
      const card = memberText.locator('../..');
      const deleteBtn = card.getByRole('button').filter({ hasText: /Törl|delete/i }).first();
      await deleteBtn.click();
    });

    await test.step('Törlés megerősítése', async () => {
      await expect(page.getByText('Biztosan törlöd ezt a családtagot?')).toBeVisible({ timeout: 5_000 });
      await page.getByText('Törlés').click();
    });

    await test.step('Családtag eltűnik a listából', async () => {
      await expect(page.getByText('Módosított E2E Tag')).not.toBeVisible({ timeout: 10_000 });
    });

    memberId = ''; // Cleanup már megtörtént
  });

  test('UC15-6 API szintű ellenőrzés: family-members lista érvényes', async ({ request }) => {
    await test.step('Family members API lekérése és válasz ellenőrzése', async () => {
      const resp = await request.get('/api/v1/family-members');
      expect(resp.ok()).toBeTruthy();

      const body = await resp.json() as unknown;
      const items: unknown[] = Array.isArray(body)
        ? body
        : (body as Record<string, unknown[]>)['items'] ?? [];

      expect(Array.isArray(items)).toBeTruthy();
      console.log(`[UC15-6] family-members items.length = ${items.length}`);
    });
  });
});

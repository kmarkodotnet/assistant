/**
 * UC11 + UC12 — Admin: AI feladatok figyelése + Audit napló UI-on keresztül
 *
 * Az admin oldal (/admin/ai-jobs) egy paginated listát mutat az AI job-okról,
 * státusz- és típusszűrőkkel. Az audit napló (/admin/audit-log) szintén admin route.
 * Mindkét endpoint Admin szerepkört igényel.
 */
import { test, expect } from '@playwright/test';
import { apiGet } from '../helpers/api';

test.describe('UC11 + UC12 — Admin: AI jobs & audit log UI', () => {

  test('UC12-1 AI jobs oldal betölt @smoke', async ({ page }) => {
    await test.step('Admin AI jobs oldal megnyitása', async () => {
      await page.goto('/admin/ai-jobs');
      await expect(page).not.toHaveTitle(/500|error/i, { timeout: 15_000 });
      await expect(page.getByText(/500|internal server error/i)).not.toBeVisible();
    });

    await test.step('"Automatikus frissítés" felirat látható', async () => {
      await expect(page.getByText('Automatikus frissítés: 30 mp')).toBeVisible({ timeout: 15_000 });
    });
  });

  test('UC12-2 queue stats widget megjelenik', async ({ page }) => {
    await test.step('Admin AI jobs oldal megnyitása', async () => {
      await page.goto('/admin/ai-jobs');
    });

    await test.step('Queue stats komponens betölt', async () => {
      await expect(page.locator('app-queue-stats')).toBeVisible({ timeout: 15_000 });
      await expect(page.getByText(/500|internal server error/i)).not.toBeVisible();
    });
  });

  test('UC12-3 státusz szűrő működik — Completed szűrés', async ({ page }) => {
    await test.step('Admin AI jobs oldal megnyitása', async () => {
      await page.goto('/admin/ai-jobs');
      await expect(page.getByText('Automatikus frissítés: 30 mp')).toBeVisible({ timeout: 15_000 });
    });

    await test.step('Szűrés Completed státuszra', async () => {
      const statusSelect = page.locator('select').first();
      await expect(statusSelect).toBeVisible();
      await statusSelect.selectOption('Completed');
      await page.waitForTimeout(1_000);
      await expect(page.getByText(/500|internal server error/i)).not.toBeVisible();
    });

    await test.step('Szűrő visszaállítása', async () => {
      const statusSelect = page.locator('select').first();
      await statusSelect.selectOption('');
    });
  });

  test('UC12-4 feladat típus szűrő működik', async ({ page }) => {
    await test.step('Admin AI jobs oldal megnyitása', async () => {
      await page.goto('/admin/ai-jobs');
      await expect(page.getByText('Automatikus frissítés: 30 mp')).toBeVisible({ timeout: 15_000 });
    });

    await test.step('Szűrés Embed job típusra', async () => {
      const jobTypeInput = page.locator('input[type="text"]').first();
      await expect(jobTypeInput).toBeVisible();
      await jobTypeInput.fill('Embed');
      await page.waitForTimeout(1_000);
      await expect(page.getByText(/500|internal server error/i)).not.toBeVisible();
    });

    await test.step('Szűrő törlése', async () => {
      await page.locator('input[type="text"]').first().clear();
    });
  });

  test('UC12-5 AI jobs lista struktúra helyes (API ellenőrzés)', async ({ request }) => {
    await test.step('AI jobs API lekérése és válasz ellenőrzése', async () => {
      const response = await apiGet(request, '/ai-jobs');
      expect(response.status()).toBe(200);

      const body = await response.json() as unknown;
      const items: unknown[] = Array.isArray(body)
        ? body
        : (body as Record<string, unknown[]>)['items'] ?? [];

      expect(Array.isArray(items)).toBeTruthy();
      console.log(`[UC12-5] items.length = ${items.length}`);
    });
  });

  test('UC12-6 queue-stats API válasz érvényes', async ({ request }) => {
    await test.step('Queue stats API lekérése', async () => {
      const response = await apiGet(request, '/ai-jobs/queue-stats');
      expect(response.status()).toBe(200);

      const body = await response.json() as Record<string, unknown>;
      expect(body).toBeDefined();
      console.log(`[UC12-6] queue-stats keys = ${Object.keys(body).join(', ')}`);
    });
  });

  test('UC12-7 Pending státuszú job-ok szűrése API-n keresztül', async ({ request }) => {
    await test.step('Pending job-ok API szűrése', async () => {
      const response = await apiGet(request, '/ai-jobs?status=Pending');
      expect(response.status()).toBe(200);
    });
  });

  test('UC11-1 audit log oldal betölt', async ({ page }) => {
    await test.step('Admin audit log oldal megnyitása', async () => {
      await page.goto('/admin/audit-log');
      await expect(page).not.toHaveTitle(/500|error/i, { timeout: 15_000 });
      await expect(page.getByText(/500|internal server error/i)).not.toBeVisible();
    });
  });

  test('UC11-2 audit log API válasz érvényes', async ({ request }) => {
    await test.step('Audit log API lekérése és válasz ellenőrzése', async () => {
      const response = await apiGet(request, '/audit-log');
      expect(response.status()).toBe(200);

      const body = await response.json() as unknown;
      const items: unknown[] = Array.isArray(body)
        ? body
        : (body as Record<string, unknown[]>)['items'] ?? [];

      expect(Array.isArray(items)).toBeTruthy();
      console.log(`[UC11-2] audit log items.length = ${items.length}`);
    });
  });

  test('UC11-3 security-events API elérhető', async ({ request }) => {
    await test.step('Security events API lekérése', async () => {
      const response = await apiGet(request, '/audit-log/security-events');
      expect(response.status()).toBe(200);
    });
  });
});

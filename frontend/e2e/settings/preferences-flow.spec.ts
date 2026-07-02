import { test, expect } from '@playwright/test';
import { mockAuth } from '../helpers/mock-api';
import { adminUser } from '../fixtures/users';

test.describe('Beállítások — Személyes @smoke', () => {
  test('QA-B3-01 saját preferenciák mentése (golden path)', async ({ page }) => {
    await mockAuth(page, adminUser);
    let patchPayload: unknown;
    await page.route('**/api/v1/auth/me/preferences', async route => {
      patchPayload = route.request().postDataJSON();
      await route.fulfill({ status: 200 });
    });

    await page.goto('/settings/preferences');

    await page.locator('[data-testid="prefs-email-enabled"]').check();
    await page.locator('[data-testid="prefs-quiet-start"]').fill('22:00');
    await page.locator('[data-testid="prefs-quiet-end"]').fill('07:00');
    await page.locator('[data-testid="prefs-save"]').click();

    await expect.poll(() => patchPayload).toMatchObject({
      emailEnabled: true,
      quietHoursStart: '22:00',
      quietHoursEnd: '07:00',
    });
    await expect(page.locator('[data-testid="toast-message"]')).toContainText('Beállítások mentve.');
  });
});

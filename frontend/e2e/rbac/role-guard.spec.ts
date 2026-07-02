import { test, expect } from '@playwright/test';
import { mockAuth, mockJson } from '../helpers/mock-api';
import { adultUser } from '../fixtures/users';

const emptyDashboard = {
  upcomingDeadlines: [],
  overdueReminders: [],
  pendingSuggestions: { tasks: 0, deadlines: 0, tags: 0, topics: 0, total: 0 },
  recentDocuments: [],
  savedSearches: [],
};

test.describe('RBAC — roleGuard @smoke', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuth(page, adultUser);
    await mockJson(page, '**/api/v1/dashboard', emptyDashboard);
  });

  test('QA-B1-05 Adult nem érheti el a /family oldalt', async ({ page }) => {
    await page.goto('/family');

    await expect(page).toHaveURL(/\/$/);
    await expect(page.locator('[data-testid="toast-message"]')).toContainText(
      'Nincs jogosultságod ehhez az oldalhoz.'
    );
  });

  test('QA-J-RBAC-01 Adult nem érheti el az /admin/audit oldalt', async ({ page }) => {
    await page.goto('/admin/audit');

    await expect(page).toHaveURL(/\/$/);
    await expect(page.locator('[data-testid="toast-message"]')).toContainText(
      'Nincs jogosultságod ehhez az oldalhoz.'
    );
  });
});

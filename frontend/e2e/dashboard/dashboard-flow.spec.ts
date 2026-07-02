import { test, expect } from '@playwright/test';
import { mockAuth, mockJson } from '../helpers/mock-api';
import { adminUser } from '../fixtures/users';

const dashboardData = {
  upcomingDeadlines: [
    { id: '1', title: 'Gépjármű-biztosítás', dueDateUtc: '2026-08-01T00:00:00Z', status: 'Upcoming', category: 'Financial', origin: 'AiApproved', relatedFamilyMemberId: null, createdUtc: '2026-07-01T00:00:00Z' },
  ],
  overdueReminders: [
    { id: '2', title: 'Gázóra leolvasás', dueDateUtc: '2026-06-25T00:00:00Z', status: 'Overdue', category: 'Home', origin: 'Manual', relatedFamilyMemberId: null, createdUtc: '2026-06-01T00:00:00Z' },
  ],
  pendingSuggestions: { tasks: 2, deadlines: 1, tags: 3, topics: 0, total: 6 },
  recentDocuments: [
    { id: '3', title: 'Gázszámla 2026 június', originalFileName: 'gazszamla.pdf', mimeType: 'application/pdf', createdUtc: '2026-06-20T10:00:00Z' },
  ],
  savedSearches: [],
};

test.describe('Dashboard @smoke', () => {
  test('QA-L1-01 widget-ek megjelenítése mockolt adatokkal', async ({ page }) => {
    await mockAuth(page, adminUser);
    await mockJson(page, '**/api/v1/dashboard', dashboardData);

    await page.goto('/');

    await expect(page.getByText('Gépjármű-biztosítás')).toBeVisible();
    await expect(page.getByText('Gázóra leolvasás')).toBeVisible();
    await expect(page.getByText('Gázszámla 2026 június')).toBeVisible();
    await expect(page.getByText('Összesen')).toBeVisible();
    await expect(page.getByText('6', { exact: true })).toBeVisible();
  });

  test('QA-L1-02 üres állapotok', async ({ page }) => {
    await mockAuth(page, adminUser);
    await mockJson(page, '**/api/v1/dashboard', {
      upcomingDeadlines: [],
      overdueReminders: [],
      pendingSuggestions: { tasks: 0, deadlines: 0, tags: 0, topics: 0, total: 0 },
      recentDocuments: [],
      savedSearches: [],
    });

    await page.goto('/');

    await expect(page.getByText('Nincs közelgő határidő.')).toBeVisible();
    await expect(page.getByText('Nincs lejárt határidő.')).toBeVisible();
    await expect(page.getByText('Nincs jóváhagyásra váró javaslat.')).toBeVisible();
    await expect(page.getByText('Nincs dokumentum.')).toBeVisible();
  });
});

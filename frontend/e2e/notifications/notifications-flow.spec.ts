import { test, expect } from '@playwright/test';
import { mockAuth, mockJson } from '../helpers/mock-api';
import { adminUser } from '../fixtures/users';

const emptyFeed = { items: [], totalCount: 0, hasMore: false };

const emptyDashboard = {
  upcomingDeadlines: [],
  overdueReminders: [],
  pendingSuggestions: { tasks: 0, deadlines: 0, tags: 0, topics: 0, total: 0 },
  recentDocuments: [],
  savedSearches: [],
};

const digestBody =
  'Jó reggelt! Íme a mai áttekintés.\n\n' +
  '📅 Mai és holnapi emlékeztetők (1):\n' +
  '- 08:00 · Kutyasétáltatás\n\n' +
  '⏳ Közelgő határidők (7 nap, 1):\n' +
  '- 2026. 07. 15. · Gépjármű-biztosítás (Financial)\n\n' +
  '📄 Új dokumentumok az elmúlt 24 órában: 2';

function notification(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    id: 'eeeeeeee-0000-0000-0000-000000000001',
    type: 'DailyDigest',
    title: 'Napi összefoglaló – 2026. 07. 11.',
    body: digestBody,
    actionUrl: '/dashboard',
    readUtc: undefined,
    createdUtc: '2026-07-11T05:00:00Z',
    ...overrides,
  };
}

test.describe('Értesítések / napi összefoglaló @smoke', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuth(page, adminUser);
  });

  test('QA-N-01 üres állapot', async ({ page }) => {
    await page.route('**/api/v1/notifications?**', route => route.fulfill({ json: emptyFeed }));

    await page.goto('/notifications');

    await expect(page.locator('[data-testid="notifications-empty"]')).toBeVisible();
    await expect(page.getByText('Nincsenek értesítések')).toBeVisible();
  });

  test('QA-N-02 DailyDigest típusú értesítés listázása, ikon/címke és többsoros body', async ({ page }) => {
    await page.route('**/api/v1/notifications?**', route =>
      route.fulfill({ json: { items: [notification()], totalCount: 1, hasMore: false } })
    );

    await page.goto('/notifications');

    const item = page.locator('[data-testid="notification-item"]');
    await expect(item).toBeVisible();
    await expect(item).toHaveAttribute('data-type', 'DailyDigest');
    await expect(item.getByText('📋')).toBeVisible();
    await expect(item.getByText('Napi összefoglaló', { exact: true })).toBeVisible();
    await expect(item.getByText('Napi összefoglaló – 2026. 07. 11.')).toBeVisible();

    // A body pre-line-ban jelenik meg, a sorok/szakaszok mindegyike látszik
    await expect(item.getByText('📅 Mai és holnapi emlékeztetők (1):', { exact: false })).toBeVisible();
    await expect(item.getByText('⏳ Közelgő határidők (7 nap, 1):', { exact: false })).toBeVisible();
    await expect(item.getByText('📄 Új dokumentumok az elmúlt 24 órában: 2', { exact: false })).toBeVisible();
  });

  test('QA-N-03 ismeretlen type nem töri el az oldalt, alap ikon jelenik meg', async ({ page }) => {
    await page.route('**/api/v1/notifications?**', route =>
      route.fulfill({
        json: {
          items: [notification({ id: 'eeeeeeee-0000-0000-0000-000000000002', type: 'SomeFutureType', body: 'Egyszerű szöveg.' })],
          totalCount: 1,
          hasMore: false,
        },
      })
    );

    await page.goto('/notifications');

    const item = page.locator('[data-testid="notification-item"]');
    await expect(item).toBeVisible();
    await expect(item).toHaveAttribute('data-type', 'SomeFutureType');
    await expect(item.getByText('🔔')).toBeVisible();
    await expect(item.getByText('Értesítés', { exact: true })).toBeVisible();
  });

  test('QA-N-04 markRead: olvasottá válik a "Olvasott" gombra kattintva', async ({ page }) => {
    let markReadCalledWith: string | undefined;
    await page.route('**/api/v1/notifications?**', route =>
      route.fulfill({ json: { items: [notification()], totalCount: 1, hasMore: false } })
    );
    await page.route('**/api/v1/notifications/*/read', async route => {
      markReadCalledWith = route.request().url();
      await route.fulfill({ status: 200 });
    });

    await page.goto('/notifications');

    const item = page.locator('[data-testid="notification-item"]');
    await expect(page.locator('[data-testid="notification-unread-dot"]')).toBeVisible();
    await expect(page.locator('[data-testid="notification-mark-read"]')).toBeVisible();

    await page.locator('[data-testid="notification-mark-read"]').click();

    await expect.poll(() => markReadCalledWith).toContain('eeeeeeee-0000-0000-0000-000000000001');
    await expect(page.locator('[data-testid="notification-unread-dot"]')).toHaveCount(0);
    await expect(page.locator('[data-testid="notification-mark-read"]')).toHaveCount(0);
    await expect(item).toHaveAttribute('data-unread', 'false');
  });

  test('QA-N-05 actionUrl navigáció: a notification-re kattintva a dashboardra navigál', async ({ page }) => {
    let markReadCalled = false;
    await page.route('**/api/v1/notifications?**', route =>
      route.fulfill({ json: { items: [notification()], totalCount: 1, hasMore: false } })
    );
    await page.route('**/api/v1/notifications/*/read', async route => {
      markReadCalled = true;
      await route.fulfill({ status: 200 });
    });
    await mockJson(page, '**/api/v1/dashboard', emptyDashboard);

    await page.goto('/notifications');

    await page.locator('[data-testid="notification-item"]').click();

    // A digest ActionUrl-je a kontrakt szerint "/dashboard" -> a dashboard oldalra navigál
    await expect(page).toHaveURL(/\/(dashboard)?$/);
    await expect.poll(() => markReadCalled).toBe(true);
  });
});

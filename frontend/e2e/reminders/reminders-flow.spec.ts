import { test, expect } from '@playwright/test';
import { mockAuth } from '../helpers/mock-api';
import { adminUser } from '../fixtures/users';

const emptyGroups = { now: [], week: [], later: [], missed: [] };

function reminder(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    id: 'cccccccc-0000-0000-0000-000000000001',
    taskId: 'dddddddd-0000-0000-0000-000000000001',
    targetUserAccountId: adminUser.userAccountId,
    channel: 'InApp',
    status: 'Fired',
    triggerUtc: '2026-07-02T08:00:00Z',
    escalationLevel: 0,
    createdByUserAccountId: adminUser.userAccountId,
    createdUtc: '2026-07-01T08:00:00Z',
    updatedUtc: '2026-07-01T08:00:00Z',
    ...overrides,
  };
}

test.describe('Emlékeztetők @smoke', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuth(page, adminUser);
  });

  test('QA-G3-04 üres állapot', async ({ page }) => {
    await page.route('**/api/v1/reminders?**', route => route.fulfill({ json: emptyGroups }));

    await page.goto('/reminders');

    await expect(page.getByText('Nincsenek emlékeztetők')).toBeVisible();
  });

  test('QA-G3-01 emlékeztető nyugtázása', async ({ page }) => {
    let acknowledged = false;
    await page.route('**/api/v1/reminders?**', async route => {
      const groups = acknowledged ? emptyGroups : { ...emptyGroups, now: [reminder()] };
      await route.fulfill({ json: groups });
    });
    await page.route('**/api/v1/reminders/*/acknowledge', async route => {
      acknowledged = true;
      await route.fulfill({ status: 200 });
    });

    await page.goto('/reminders');
    await expect(page.locator('[data-testid="reminder-acknowledge"]')).toBeVisible();

    await page.locator('[data-testid="reminder-acknowledge"]').click();

    await expect(page.getByText('Nincsenek emlékeztetők')).toBeVisible();
  });

  test('QA-G3-02 emlékeztető halasztása', async ({ page }) => {
    await page.route('**/api/v1/reminders?**', route =>
      route.fulfill({ json: { ...emptyGroups, now: [reminder()] } })
    );
    let snoozePayload: unknown;
    await page.route('**/api/v1/reminders/*/snooze', async route => {
      snoozePayload = route.request().postDataJSON();
      await route.fulfill({ status: 200 });
    });

    await page.goto('/reminders');
    await page.locator('[data-testid="reminder-snooze"]').click();
    await page.getByText('1 óra', { exact: true }).click();

    await expect.poll(() => snoozePayload).toEqual({ snoozeMinutes: 60 });
  });

  test('QA-G3-03 emlékeztető mellőzése', async ({ page }) => {
    let skipCalled = false;
    await page.route('**/api/v1/reminders?**', async route => {
      const groups = skipCalled ? emptyGroups : { ...emptyGroups, now: [reminder()] };
      await route.fulfill({ json: groups });
    });
    await page.route('**/api/v1/reminders/*/skip', async route => {
      skipCalled = true;
      await route.fulfill({ status: 200 });
    });

    await page.goto('/reminders');
    await page.locator('[data-testid="reminder-skip"]').click();

    await expect(page.getByText('Nincsenek emlékeztetők')).toBeVisible();
  });
});

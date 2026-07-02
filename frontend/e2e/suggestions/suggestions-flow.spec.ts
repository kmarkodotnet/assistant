import { test, expect } from '@playwright/test';
import { mockAuth } from '../helpers/mock-api';
import { adminUser } from '../fixtures/users';

const emptyAggregate = { tasks: [], deadlines: [], tags: [], topics: [], totalCount: 0 };

const task1 = {
  id: 'eeeeeeee-0000-0000-0000-000000000001',
  title: 'Gázóra leolvasás bejelentése',
  dueDateUtc: null,
  status: 'Suggested',
  priority: 'Normal',
  origin: 'AiSuggested',
  assignedToFamilyMemberId: null,
  createdUtc: '2026-07-01T08:00:00Z',
};

const deadline1 = {
  id: 'ffffffff-0000-0000-0000-000000000001',
  title: 'Gépjármű-biztosítás megújítás',
  dueDateUtc: '2026-08-01T00:00:00Z',
  status: 'Suggested',
  category: 'Financial',
  origin: 'AiSuggested',
  relatedFamilyMemberId: null,
  createdUtc: '2026-07-01T08:00:00Z',
};

const tag1 = { documentId: 'aaaaaaaa-0000-0000-0000-000000000001', tagId: 'gggggggg-0000-0000-0000-000000000001', tagName: 'rezsi' };

test.describe('AI javaslatok @smoke', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuth(page, adminUser);
  });

  test('QA-F3-03 üres állapot', async ({ page }) => {
    await page.route('**/api/v1/suggestions', route => route.fulfill({ json: emptyAggregate }));

    await page.goto('/suggestions');

    await expect(page.getByText('Nincs jóváhagyásra váró javaslat.')).toBeVisible();
  });

  test('QA-F3-01 egyedi javaslat elfogadása', async ({ page }) => {
    const aggregate = { ...emptyAggregate, tasks: [task1], deadlines: [deadline1], totalCount: 2 };
    await page.route('**/api/v1/suggestions', route => route.fulfill({ json: aggregate }));
    let batchPayload: unknown;
    await page.route('**/api/v1/suggestions/batch', async route => {
      batchPayload = route.request().postDataJSON();
      await route.fulfill({ json: { approved: 1, rejected: 0, errors: [] } });
    });

    await page.goto('/suggestions');
    await expect(page.getByText(task1.title)).toBeVisible();

    await page.locator(`[data-testid="suggestion-approve-${task1.id}"]`).click();

    await expect.poll(() => batchPayload).toEqual({ approve: { tasks: [task1.id] } });
    await expect(page.locator('[data-testid="toast-message"]')).toContainText('1 elfogadva, 0 elutasítva.');
  });

  test('QA-F3-02 összes tag-javaslat elutasítása egyben', async ({ page }) => {
    const aggregate = { ...emptyAggregate, tags: [tag1], totalCount: 1 };
    await page.route('**/api/v1/suggestions', route => route.fulfill({ json: aggregate }));
    let batchPayload: unknown;
    await page.route('**/api/v1/suggestions/batch', async route => {
      batchPayload = route.request().postDataJSON();
      await route.fulfill({ json: { approved: 0, rejected: 1, errors: [] } });
    });

    await page.goto('/suggestions');
    await page.locator('[data-testid="suggestion-reject-all"]').click();

    await expect.poll(() => batchPayload).toEqual({
      reject: { tags: [{ documentId: tag1.documentId, tagId: tag1.tagId }] },
    });
  });
});

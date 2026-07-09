import { test, expect } from '@playwright/test';
import { mockAuth, mockJson } from '../helpers/mock-api';
import { adminUser } from '../fixtures/users';

const emptyDeadlineList: unknown[] = [];

function deadline(overrides: Record<string, unknown> = {}) {
  return {
    id: 'dead-0001-0000-0000-000000000001',
    title: 'Teszt határidő',
    status: 'Upcoming',
    category: 'Other',
    origin: 'Manual',
    relatedFamilyMemberId: null,
    dueDateUtc: '2026-08-01T00:00:00Z',
    createdUtc: '2026-07-01T08:00:00Z',
    ...overrides,
  };
}

const familyMembers: unknown[] = [];

test.describe('Határidők (Deadlines) @smoke', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuth(page, adminUser);
    // Mock family members list used by the dialog
    await page.route('**/api/v1/family/members', route =>
      route.fulfill({ json: familyMembers })
    );
  });

  // QA-D1-01: A határidők oldal betölt, szekciók láthatók
  test('QA-D1-01 határidők oldal betölt, szekciók láthatók', async ({ page }) => {
    await mockJson(page, '**/api/v1/deadlines**', emptyDeadlineList);

    await page.goto('/deadlines');

    // Three section headings must be visible in the 'all' filter view
    await expect(page.getByText('Közelgő')).toBeVisible();
    await expect(page.getByText('Lejárt')).toBeVisible();
    await expect(page.getByText(/Megoldva/)).toBeVisible();
  });

  // QA-D1-02: deadline-create-btn gombra kattintva megjelenik a dialog
  test('QA-D1-02 deadline-create-btn megnyitja a létrehozó dialogot', async ({ page }) => {
    await mockJson(page, '**/api/v1/deadlines**', emptyDeadlineList);

    await page.goto('/deadlines');

    await page.locator('[data-testid="deadline-create-btn"]').click();

    // Dialog heading visible
    await expect(page.getByText('Új határidő')).toBeVisible();
    // Title input visible
    await expect(page.locator('[data-testid="deadline-form-title"]')).toBeVisible();
  });

  // QA-D1-03: A dialog bezárható
  test('QA-D1-03 a dialog bezárható a Mégse gombbal', async ({ page }) => {
    await mockJson(page, '**/api/v1/deadlines**', emptyDeadlineList);

    await page.goto('/deadlines');

    await page.locator('[data-testid="deadline-create-btn"]').click();
    await expect(page.locator('[data-testid="deadline-form-title"]')).toBeVisible();

    // Click the Mégse (cancel) button inside the dialog
    await page.getByRole('button', { name: 'Mégse' }).click();

    // Dialog must be gone
    await expect(page.locator('[data-testid="deadline-form-title"]')).not.toBeVisible();
    // Remain on the deadlines page
    await expect(page.getByText('Határidők')).toBeVisible();
  });

  // QA-D1-04: A dialog bezárható az overlay-re kattintva
  test('QA-D1-04 a dialog bezárható az overlay-re kattintva', async ({ page }) => {
    await mockJson(page, '**/api/v1/deadlines**', emptyDeadlineList);

    await page.goto('/deadlines');

    await page.locator('[data-testid="deadline-create-btn"]').click();
    await expect(page.locator('[data-testid="deadline-form-title"]')).toBeVisible();

    // Click the backdrop overlay (the fixed inset-0 div outside the card)
    // The dialog container is the first fixed div; click at the edge
    await page.mouse.click(10, 10);

    await expect(page.locator('[data-testid="deadline-form-title"]')).not.toBeVisible();
  });

  // QA-D1-05: Üres állapot szöveg megjelenik
  test('QA-D1-05 üres állapot megjelenítése ha nincsenek határidők', async ({ page }) => {
    await mockJson(page, '**/api/v1/deadlines**', emptyDeadlineList);

    await page.goto('/deadlines');

    await expect(page.getByText('Nincsenek határidők')).toBeVisible();
  });

  // QA-D1-06: Közelgő határidő megjelenik a Közelgő szekcióban
  test('QA-D1-06 közelgő határidő kártyája látható', async ({ page }) => {
    await mockJson(page, '**/api/v1/deadlines**', [
      deadline({ title: 'Gépjármű-biztosítás megújítása', status: 'Upcoming' }),
    ]);

    await page.goto('/deadlines');

    await expect(page.getByText('Gépjármű-biztosítás megújítása')).toBeVisible();
  });

  // QA-D1-07: Kategória és státusz szűrők láthatók
  test('QA-D1-07 szűrők láthatók az oldalon', async ({ page }) => {
    await mockJson(page, '**/api/v1/deadlines**', emptyDeadlineList);

    await page.goto('/deadlines');

    await expect(page.locator('[data-testid="deadlines-filter-category"]')).toBeVisible();
    await expect(page.locator('[data-testid="deadlines-filter-status"]')).toBeVisible();
  });

  // QA-D1-08: Határidő létrehozása a dialógon keresztül
  test('QA-D1-08 határidő létrehozása dialógon keresztül', async ({ page }) => {
    const newDeadline = deadline({ id: 'dead-new', title: 'Iskolai beiratkozás' });

    const deadlines: unknown[] = [];
    await page.route('**/api/v1/deadlines', async route => {
      const method = route.request().method();
      if (method === 'GET') {
        await route.fulfill({ json: deadlines });
        return;
      }
      if (method === 'POST') {
        deadlines.push(newDeadline);
        await route.fulfill({ status: 201, json: newDeadline });
        return;
      }
      await route.continue();
    });

    await page.goto('/deadlines');

    await page.locator('[data-testid="deadline-create-btn"]').click();
    await expect(page.locator('[data-testid="deadline-form-title"]')).toBeVisible();

    await page.locator('[data-testid="deadline-form-title"]').fill('Iskolai beiratkozás');
    await page.locator('[data-testid="deadline-form-dueDate"]').fill('2026-09-01');
    await page.locator('[data-testid="deadline-form-submit"]').click();

    await expect(page.getByText('Iskolai beiratkozás')).toBeVisible();
  });
});

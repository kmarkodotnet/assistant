import { test, expect } from '@playwright/test';
import { mockAuth, mockJson } from '../helpers/mock-api';
import { adminUser } from '../fixtures/users';

const emptyTaskList: unknown[] = [];

function task(overrides: Record<string, unknown> = {}) {
  return {
    id: 'task-0001-0000-0000-000000000001',
    title: 'Teszt feladat',
    status: 'Open',
    priority: 'Normal',
    origin: 'Manual',
    assignedToFamilyMemberId: null,
    dueDateUtc: null,
    createdUtc: '2026-07-01T08:00:00Z',
    ...overrides,
  };
}

const familyMembers: unknown[] = [];

test.describe('Feladatok (Tasks) @smoke', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuth(page, adminUser);
    // Mock family members list used by the page and dialog
    await page.route('**/api/v1/family/members', route =>
      route.fulfill({ json: familyMembers })
    );
  });

  // QA-T1-01: A feladatok oldal betölt, kanban oszlopok láthatók
  test('QA-T1-01 feladatok oldal betölt, kanban oszlopok láthatók', async ({ page }) => {
    await mockJson(page, '**/api/v1/tasks**', emptyTaskList);

    await page.goto('/tasks');

    // Four kanban column headings must be visible
    await expect(page.getByText('Javasolt')).toBeVisible();
    await expect(page.getByText('Nyitott')).toBeVisible();
    await expect(page.getByText('Folyamatban')).toBeVisible();
    await expect(page.getByText('Kész')).toBeVisible();
  });

  // QA-T1-02: task-create-btn gombra kattintva megjelenik a dialog
  test('QA-T1-02 task-create-btn megnyitja a létrehozó dialogot', async ({ page }) => {
    await mockJson(page, '**/api/v1/tasks**', emptyTaskList);

    await page.goto('/tasks');

    await page.locator('[data-testid="task-create-btn"]').click();

    // Dialog title visible
    await expect(page.getByText('Új feladat')).toBeVisible();
    // Title input inside dialog is visible
    await expect(page.locator('[data-testid="task-title-input"]')).toBeVisible();
  });

  // QA-T1-03: task-title-input mezőbe írható, submit után a task megjelenik
  test('QA-T1-03 feladat létrehozása dialógon keresztül', async ({ page }) => {
    const newTask = task({ id: 'task-new', title: 'Új teszt feladat', status: 'Open' });

    // Initial list is empty, after POST return new task in list
    const tasks: unknown[] = [];
    await page.route('**/api/v1/tasks', async route => {
      const method = route.request().method();
      if (method === 'GET') {
        await route.fulfill({ json: tasks });
        return;
      }
      if (method === 'POST') {
        tasks.push(newTask);
        await route.fulfill({ status: 201, json: newTask });
        return;
      }
      await route.continue();
    });

    await page.goto('/tasks');

    await page.locator('[data-testid="task-create-btn"]').click();
    await expect(page.locator('[data-testid="task-title-input"]')).toBeVisible();

    await page.locator('[data-testid="task-title-input"]').fill('Új teszt feladat');
    await page.locator('[data-testid="task-form-submit"]').click();

    // After successful save the task title should appear on the board
    await expect(page.getByText('Új teszt feladat')).toBeVisible();
  });

  // QA-T1-04: task-cancel bezárja a dialogot
  test('QA-T1-04 task-cancel bezárja a dialogot', async ({ page }) => {
    await mockJson(page, '**/api/v1/tasks**', emptyTaskList);

    await page.goto('/tasks');

    await page.locator('[data-testid="task-create-btn"]').click();
    await expect(page.locator('[data-testid="task-title-input"]')).toBeVisible();

    await page.locator('[data-testid="task-cancel"]').click();

    // Dialog must no longer be visible
    await expect(page.locator('[data-testid="task-title-input"]')).not.toBeVisible();
    // Page heading still visible — we remain on the tasks page
    await expect(page.getByText('Feladatok')).toBeVisible();
  });

  // QA-T1-05: Szűrők megjelennek
  test('QA-T1-05 szűrők láthatók az oldalon', async ({ page }) => {
    await mockJson(page, '**/api/v1/tasks**', emptyTaskList);

    await page.goto('/tasks');

    await expect(page.locator('[data-testid="tasks-filter-member"]')).toBeVisible();
    await expect(page.locator('[data-testid="tasks-filter-priority"]')).toBeVisible();
  });

  // QA-T1-06: Üres állapot szöveg látható, ha nincsenek feladatok
  test('QA-T1-06 üres állapot szöveg megjelenítése', async ({ page }) => {
    await mockJson(page, '**/api/v1/tasks**', emptyTaskList);

    await page.goto('/tasks');

    await expect(page.getByText('Nincsenek feladatok')).toBeVisible();
  });

  // QA-T1-07: Feladatok megjelennek a kanban táblakon
  test('QA-T1-07 nyitott feladat megjelenik a Nyitott oszlopban', async ({ page }) => {
    await mockJson(page, '**/api/v1/tasks**', [task({ title: 'Bevásárlás', status: 'Open' })]);

    await page.goto('/tasks');

    await expect(page.getByText('Bevásárlás')).toBeVisible();
  });
});

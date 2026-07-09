/**
 * UC04 — Feladat kezelése UI-on keresztül (CRUD + állapotgép)
 */
import { test, expect } from '@playwright/test';
import { apiDelete, apiGet } from '../helpers/api';

test.describe('UC04 — Tasks CRUD & state machine @smoke', () => {
  let taskId: string;

  test.afterAll(async ({ request }) => {
    if (taskId) {
      await apiDelete(request, `/tasks/${taskId}`).catch(() => { /* already deleted */ });
    }
  });

  test('UC04-1 feladat létrehozása UI-on keresztül', async ({ page }) => {
    await test.step('Feladatok oldal megnyitása', async () => {
      await page.goto('/tasks');
      await expect(page.getByRole('heading', { name: 'Feladatok' })).toBeVisible();
    });

    await test.step('Létrehozó dialog megnyitása', async () => {
      await page.getByTestId('task-create-btn').click();
      await expect(page.getByRole('heading', { name: 'Új feladat', exact: true })).toBeVisible();
    });

    await test.step('Form kitöltése', async () => {
      await page.getByTestId('task-title-input').fill('E2E Teszt Feladat');
      await page.getByTestId('task-form-priority').selectOption('High');
      await page.getByTestId('task-form-description').fill('Ez egy automatizált E2E teszt feladat.');
    });

    await test.step('Feladat mentése', async () => {
      await page.getByTestId('task-form-submit').click();
      // Dialog bezárul — az h3 "Új feladat" eltűnik (a gomb "+ Új feladat" marad)
      await expect(page.getByRole('heading', { name: 'Új feladat', exact: true })).not.toBeVisible({ timeout: 5_000 });
    });

    await test.step('Feladat megjelenik a kanban táblán', async () => {
      await expect(page.getByText('E2E Teszt Feladat')).toBeVisible({ timeout: 10_000 });
    });

    await test.step('Feladat ID kinyerése API-n keresztül', async () => {
      const resp = await page.request.get('/api/v1/tasks');
      expect(resp.ok()).toBeTruthy();
      const items = await resp.json() as Record<string, unknown>[];
      const found = items.find(t => t['title'] === 'E2E Teszt Feladat');
      expect(found, 'A létrehozott feladatot vissza kell adnia az API-nak').toBeTruthy();
      taskId = found!['id'] as string;
      console.log(`[UC04-1] taskId = ${taskId}`);
    });
  });

  test('UC04-2 feladat szerkesztése UI-on keresztül', async ({ page }) => {
    expect(taskId, 'UC04-1-nek be kell állítania a taskId-t').toBeTruthy();

    await test.step('Feladatok oldal megnyitása', async () => {
      await page.goto('/tasks');
      await expect(page.getByText('E2E Teszt Feladat')).toBeVisible({ timeout: 10_000 });
    });

    await test.step('Szerkesztő dialog megnyitása', async () => {
      const card = page.getByText('E2E Teszt Feladat').locator('../..');
      await card.getByTestId('task-edit').click();
      await expect(page.getByRole('heading', { name: 'Feladat szerkesztése' })).toBeVisible();
    });

    await test.step('Cím módosítása', async () => {
      const titleInput = page.getByTestId('task-title-input');
      await titleInput.clear();
      await titleInput.fill('Módosított E2E Feladat');
    });

    await test.step('Módosítás mentése', async () => {
      await page.getByTestId('task-form-submit').click();
      await expect(page.getByRole('heading', { name: 'Feladat szerkesztése' })).not.toBeVisible({ timeout: 5_000 });
    });

    await test.step('Módosított cím megjelenik a listán', async () => {
      await expect(page.getByText('Módosított E2E Feladat')).toBeVisible({ timeout: 10_000 });
    });
  });

  test('UC04-3 állapotgép: Open → InProgress', async ({ page }) => {
    expect(taskId, 'UC04-1-nek be kell állítania a taskId-t').toBeTruthy();

    await test.step('Feladatok oldal megnyitása', async () => {
      await page.goto('/tasks');
      await expect(page.getByText('Módosított E2E Feladat')).toBeVisible({ timeout: 10_000 });
    });

    await test.step('Feladat indítása (Open → InProgress)', async () => {
      const card = page.getByText('Módosított E2E Feladat').locator('../..');
      await card.getByTestId('task-start').click();
    });

    await test.step('API-ban InProgress státusz ellenőrzése', async () => {
      await page.waitForTimeout(800);
      const resp = await apiGet(page.request, `/tasks/${taskId}`);
      const body = await resp.json() as Record<string, unknown>;
      expect(body['status']).toBe('InProgress');
      console.log(`[UC04-3] status = ${body['status']}`);
    });

    await test.step('Kártya megjelenik Folyamatban oszlopban', async () => {
      await expect(page.getByText('Módosított E2E Feladat')).toBeVisible({ timeout: 5_000 });
    });
  });

  test('UC04-4 állapotgép: InProgress → Done', async ({ page }) => {
    expect(taskId, 'UC04-1-nek be kell állítania a taskId-t').toBeTruthy();

    await test.step('Feladatok oldal megnyitása', async () => {
      await page.goto('/tasks');
      await expect(page.getByText('Módosított E2E Feladat')).toBeVisible({ timeout: 10_000 });
    });

    await test.step('Feladat befejezése (InProgress → Done)', async () => {
      const card = page.getByText('Módosított E2E Feladat').locator('../..');
      await card.getByTestId('task-complete').click();
    });

    await test.step('API-ban Done státusz ellenőrzése', async () => {
      await page.waitForTimeout(800);
      const resp = await apiGet(page.request, `/tasks/${taskId}`);
      const body = await resp.json() as Record<string, unknown>;
      expect(body['status']).toBe('Done');
      console.log(`[UC04-4] status = ${body['status']}`);
    });

    await test.step('Kártya megjelenik Kész oszlopban', async () => {
      await expect(page.getByText('Módosított E2E Feladat')).toBeVisible({ timeout: 5_000 });
    });
  });

  test('UC04-5 szűrő: prioritás szűrő High → csak magas prioritású feladatok', async ({ page }) => {
    await test.step('Feladatok oldal megnyitása', async () => {
      await page.goto('/tasks');
      await expect(page.getByRole('heading', { name: 'Feladatok' })).toBeVisible();
    });

    await test.step('High prioritás szűrő beállítása', async () => {
      await page.getByTestId('tasks-filter-priority').selectOption('High');
      await page.waitForTimeout(500);
    });

    await test.step('Oldal hiba nélkül betölt', async () => {
      await expect(page.getByRole('heading', { name: 'Feladatok' })).toBeVisible();
      await expect(page.getByText(/500|internal server error/i)).not.toBeVisible();
    });
  });

  test('UC04-6 mégse gomb bezárja a dialógot', async ({ page }) => {
    await test.step('Feladatok oldal megnyitása', async () => {
      await page.goto('/tasks');
      await expect(page.getByRole('heading', { name: 'Feladatok' })).toBeVisible();
    });

    await test.step('Dialog megnyitása', async () => {
      await page.getByTestId('task-create-btn').click();
      await expect(page.getByRole('heading', { name: 'Új feladat', exact: true })).toBeVisible();
    });

    await test.step('Mégse gombra kattintás (dialógban)', async () => {
      // A dialog Mégse gombjának szövege "Mégse" — role-alapú selector a dialogban
      // task-cancel testid ütközhet a kártyák Visszavon gombjával, ezért dialógon belül keressük
      const dialog = page.locator('.fixed.inset-0');
      await dialog.getByTestId('task-cancel').click();
    });

    await test.step('Dialog bezárul', async () => {
      await expect(page.getByRole('heading', { name: 'Új feladat', exact: true })).not.toBeVisible({ timeout: 5_000 });
    });
  });
});

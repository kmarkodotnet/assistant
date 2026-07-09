/**
 * UC03 — AI javaslat elfogadása / elutasítása UI-on keresztül
 *
 * A /suggestions oldal az AI pipeline által generált javaslatokat jeleníti meg.
 * Ha nincs javaslat: "Nincs jóváhagyásra váró javaslat." üzenet látható.
 * Ha van javaslat: suggestion-block komponens mutatja az Elfogadom/Elvetem gombokat.
 */
import { test, expect } from '@playwright/test';
import { apiGet } from '../helpers/api';

test.describe('UC03 — AI suggestions UI', () => {

  test('UC03-1 javaslatok oldal betölt @smoke', async ({ page }) => {
    await test.step('Javaslatok oldal megnyitása', async () => {
      await page.goto('/suggestions');
      await expect(page.getByRole('heading', { name: 'AI javaslatok' })).toBeVisible({ timeout: 15_000 });
    });

    await test.step('Oldal nem mutat 500-as hibát', async () => {
      await expect(page.getByText(/500|internal server error/i)).not.toBeVisible();
    });
  });

  test('UC03-2 üres állapot: nincs javaslat → tájékoztató üzenet', async ({ page }) => {
    await test.step('Javaslatok oldal megnyitása', async () => {
      await page.goto('/suggestions');
    });

    await test.step('Skeleton betöltés befejezésének megvárása', async () => {
      await expect(page.locator('ui-skeleton, [class*="skeleton"]')).not.toBeVisible({ timeout: 15_000 });
    });

    await test.step('Üres üzenet vagy suggestion-block megjelenik', async () => {
      const emptyMsg = page.getByText('Nincs jóváhagyásra váró javaslat.');
      const suggestionBlock = page.locator('ui-suggestion-block');
      const headingCount = page.getByText(/javaslat vár jóváhagyásra/);

      const emptyOrSuggestions = emptyMsg.or(suggestionBlock).or(headingCount);
      await expect(emptyOrSuggestions.first()).toBeVisible({ timeout: 15_000 });
    });
  });

  test('UC03-3 javaslatok számlálója megjelenik, ha van javaslat', async ({ page, request }) => {
    await test.step('Javaslatok ellenőrzése API-n keresztül', async () => {
      const resp = await apiGet(request, '/suggestions');
      if (!resp.ok()) {
        test.skip(true, 'Suggestions API nem elérhető');
        return;
      }
      const data = await resp.json() as Record<string, unknown>;
      const totalCount = data['totalCount'] as number;
      console.log(`[UC03-3] totalCount = ${totalCount}`);

      await page.goto('/suggestions');
      await expect(page.locator('ui-skeleton, [class*="skeleton"]')).not.toBeVisible({ timeout: 15_000 });

      if (totalCount === 0) {
        await expect(page.getByText('Nincs jóváhagyásra váró javaslat.')).toBeVisible();
      } else {
        await expect(page.getByText(`${totalCount} javaslat vár jóváhagyásra`)).toBeVisible();
      }
    });
  });

  test('UC03-4 feladat javaslat elfogadása (ha van)', async ({ page, request }) => {
    await test.step('Feladat javaslatok ellenőrzése API-n keresztül', async () => {
      const resp = await apiGet(request, '/suggestions');
      if (!resp.ok()) {
        test.skip(true, 'Suggestions API nem elérhető');
        return;
      }
      const data = await resp.json() as Record<string, unknown>;
      const tasks = data['tasks'] as Array<Record<string, unknown>>;

      if (!tasks || tasks.length === 0) {
        test.skip(true, 'Nincs feladat javaslat — teszt kihagyva');
        return;
      }

      const firstTaskId = tasks[0]['id'] as string;
      console.log(`[UC03-4] firstTaskId = ${firstTaskId}`);

      await page.goto('/suggestions');
      await expect(page.locator('ui-skeleton, [class*="skeleton"]')).not.toBeVisible({ timeout: 15_000 });

      const approveBtn = page.getByTestId(`suggestion-approve-${firstTaskId}`);
      await expect(approveBtn).toBeVisible({ timeout: 10_000 });
      await approveBtn.click();
    });

    await test.step('Elfogadás sikeres — oldal hiba nélkül frissül', async () => {
      await page.waitForTimeout(2_000);
      await expect(page.getByText(/500|internal server error/i)).not.toBeVisible();
    });
  });

  test('UC03-5 összes javaslat elfogadása (ha van)', async ({ page, request }) => {
    await test.step('Javaslatok ellenőrzése API-n keresztül', async () => {
      const resp = await apiGet(request, '/suggestions');
      if (!resp.ok()) {
        test.skip(true, 'Suggestions API nem elérhető');
        return;
      }
      const data = await resp.json() as Record<string, unknown>;
      const totalCount = data['totalCount'] as number;

      if (totalCount === 0) {
        test.skip(true, 'Nincs javaslat — teszt kihagyva');
        return;
      }

      await page.goto('/suggestions');
      await expect(page.locator('ui-skeleton, [class*="skeleton"]')).not.toBeVisible({ timeout: 15_000 });
    });

    await test.step('"Elfogadom mindet" gombra kattintás', async () => {
      const approveAllBtn = page.getByTestId('suggestion-approve-all').first();
      if (await approveAllBtn.isVisible()) {
        await approveAllBtn.click();
        await page.waitForTimeout(2_000);
        await expect(page.getByText(/500|internal server error/i)).not.toBeVisible();
      }
    });
  });

  test('UC03-6 javaslat elutasítása (ha van)', async ({ page, request }) => {
    await test.step('Határidő javaslatok ellenőrzése API-n keresztül', async () => {
      const resp = await apiGet(request, '/suggestions');
      if (!resp.ok()) {
        test.skip(true, 'Suggestions API nem elérhető');
        return;
      }
      const data = await resp.json() as Record<string, unknown>;
      const deadlines = data['deadlines'] as Array<Record<string, unknown>>;

      if (!deadlines || deadlines.length === 0) {
        test.skip(true, 'Nincs határidő javaslat — teszt kihagyva');
        return;
      }

      const firstDeadlineId = deadlines[0]['id'] as string;
      console.log(`[UC03-6] firstDeadlineId = ${firstDeadlineId}`);

      await page.goto('/suggestions');
      await expect(page.locator('ui-skeleton, [class*="skeleton"]')).not.toBeVisible({ timeout: 15_000 });

      const rejectBtn = page.getByTestId(`suggestion-reject-${firstDeadlineId}`);
      await expect(rejectBtn).toBeVisible({ timeout: 10_000 });
      await rejectBtn.click();
    });

    await test.step('Elutasítás sikeres — oldal hiba nélkül frissül', async () => {
      await page.waitForTimeout(2_000);
      await expect(page.getByText(/500|internal server error/i)).not.toBeVisible();
    });
  });
});

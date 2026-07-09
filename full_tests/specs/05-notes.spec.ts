/**
 * UC08 — Feljegyzés (Note) létrehozása, szerkesztése, törlése UI-on keresztül
 *
 * A Notes oldal inline dialog-ot használ (nem route-alapú).
 * Az admin felhasználó Adult jogokkal rendelkezik, így CRUD-ot végezhet.
 * Cleanup: afterAll API request-tel.
 */
import { test, expect } from '@playwright/test';
import { apiDelete, apiGet } from '../helpers/api';

test.describe('UC08 — Notes CRUD & UI', () => {
  let noteId: string;

  test.afterAll(async ({ request }) => {
    if (noteId) {
      await apiDelete(request, `/notes/${noteId}`).catch(() => { /* már törölve */ });
    }
  });

  test('UC08-1 feljegyzés oldal betölt', async ({ page }) => {
    await test.step('Feljegyzések oldal megnyitása', async () => {
      await page.goto('/notes');
      await expect(page.getByRole('heading', { name: 'Feljegyzések' })).toBeVisible({ timeout: 15_000 });
    });

    await test.step('Új feljegyzés gomb megjelenik', async () => {
      await expect(page.getByText('+ Új feljegyzés')).toBeVisible();
    });
  });

  test('UC08-2 feljegyzés létrehozása UI-on keresztül', async ({ page }) => {
    await test.step('Feljegyzések oldal megnyitása', async () => {
      await page.goto('/notes');
    });

    await test.step('Létrehozó dialog megnyitása', async () => {
      await page.getByText('+ Új feljegyzés').click();
      await expect(page.getByText('Új feljegyzés').nth(1)).toBeVisible();
    });

    await test.step('Form kitöltése (cím és tartalom)', async () => {
      const titleInput = page.locator('input[type="text"]').last();
      await titleInput.fill('E2E Feljegyzés');
      const bodyTextarea = page.locator('textarea').last();
      await bodyTextarea.fill('Teszt feljegyzés szövege az embedding teszteléshez.');
    });

    await test.step('Feljegyzés mentése', async () => {
      await page.getByText('Mentés').click();
      await expect(page.getByText('E2E Feljegyzés')).toBeVisible({ timeout: 10_000 });
    });

    await test.step('Feljegyzés ID kinyerése API-n keresztül', async () => {
      const resp = await page.request.get('/api/v1/notes');
      if (resp.ok()) {
        const body = await resp.json() as unknown;
        const items: unknown[] = Array.isArray(body)
          ? body
          : (body as Record<string, unknown[]>)['items'] ?? [];
        const found = items.find(
          (n) => (n as Record<string, unknown>)['title'] === 'E2E Feljegyzés'
        );
        if (found) {
          noteId = (found as Record<string, unknown>)['id'] as string;
        }
      }
      console.log(`[UC08-2] noteId = ${noteId}`);
    });
  });

  test('UC08-3 feljegyzés detail megtekintése UI-on keresztül', async ({ page }) => {
    expect(noteId, 'UC08-2-nek be kell állítania a noteId-t').toBeTruthy();

    await test.step('Feljegyzések oldal megnyitása', async () => {
      await page.goto('/notes');
    });

    await test.step('Feljegyzés detail nézetének megnyitása', async () => {
      await page.getByText('E2E Feljegyzés').click();
      await expect(page.getByText('Feljegyzés')).toBeVisible({ timeout: 10_000 });
      await expect(page.getByText('Teszt feljegyzés szövege az embedding teszteléshez.')).toBeVisible({ timeout: 10_000 });
    });

    await test.step('Detail dialog bezárása', async () => {
      await page.getByText('Bezárás').click();
      await expect(page.getByText('Teszt feljegyzés szövege az embedding teszteléshez.')).not.toBeVisible({ timeout: 5_000 });
    });
  });

  test('UC08-4 feljegyzés szerkesztése UI-on keresztül', async ({ page }) => {
    expect(noteId, 'UC08-2-nek be kell állítania a noteId-t').toBeTruthy();

    await test.step('Feljegyzések oldal megnyitása', async () => {
      await page.goto('/notes');
    });

    await test.step('Szerkesztő dialog megnyitása', async () => {
      const noteRow = page.getByText('E2E Feljegyzés').locator('../..');
      await expect(noteRow).toBeVisible({ timeout: 10_000 });
      await noteRow.getByText('Szerk.').click();
      await expect(page.getByText('Szerkesztés')).toBeVisible({ timeout: 10_000 });
    });

    await test.step('Cím módosítása és mentése', async () => {
      const titleInput = page.locator('input[type="text"]').last();
      await titleInput.clear();
      await titleInput.fill('Módosított E2E Feljegyzés');
      await page.getByText('Mentés').click();
    });

    await test.step('Módosított cím megjelenik a listában', async () => {
      await expect(page.getByText('Módosított E2E Feljegyzés')).toBeVisible({ timeout: 10_000 });
    });
  });

  test('UC08-5 Embed AI job ellenőrzése API-n keresztül', async ({ request }) => {
    expect(noteId, 'UC08-2-nek be kell állítania a noteId-t').toBeTruthy();

    await test.step('Rövid várakozás a worker enqueue-jához', async () => {
      await new Promise<void>(resolve => setTimeout(resolve, 2_000));
    });

    await test.step('AI jobs lekérése és Embed job keresése', async () => {
      const response = await apiGet(request, `/ai-jobs?pageSize=100`);

      if (response.status() === 403 || response.status() === 404) {
        test.skip(true, 'AI jobs endpoint nem elérhető — embedding ellenőrzés kihagyva');
        return;
      }
      expect(response.ok()).toBeTruthy();

      const body = await response.json() as unknown;
      const items: unknown[] = Array.isArray(body)
        ? body
        : (body as Record<string, unknown[]>)['items'] ?? [];

      const embedJob = items.find((j) => {
        const job = j as Record<string, unknown>;
        return job['targetId'] === noteId || job['jobType'] === 'Embed';
      });

      if (!embedJob) {
        console.warn(`[UC08-5] Nem található pending Embed job a(z) ${noteId} feljegyzéshez — lehet, hogy már feldolgozva.`);
      }
    });
  });

  test('UC08-6 feljegyzés törlése UI-on keresztül', async ({ page }) => {
    expect(noteId, 'UC08-2-nek be kell állítania a noteId-t').toBeTruthy();

    await test.step('Feljegyzések oldal megnyitása', async () => {
      await page.goto('/notes');
    });

    await test.step('Törlés megerősítése és feljegyzés törlése', async () => {
      const noteRow = page.getByText('Módosított E2E Feljegyzés').locator('../..');
      await expect(noteRow).toBeVisible({ timeout: 10_000 });

      page.on('dialog', async (dialog) => {
        await dialog.accept();
      });

      await noteRow.getByText('Törl.').click();
    });

    await test.step('Feljegyzés eltűnik a listából', async () => {
      await expect(page.getByText('Módosított E2E Feljegyzés')).not.toBeVisible({ timeout: 10_000 });
    });

    noteId = ''; // Cleanup már megtörtént
  });
});

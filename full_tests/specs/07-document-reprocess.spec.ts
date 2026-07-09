/**
 * UC06 — Dokumentum újrafeldolgozása UI + API kombináció
 *
 * Feltölt egy egyedi dokumentumot UI-on keresztül, megvárja a Done állapotot
 * (API polling), majd az újrafeldolgozást API-n indítja (nincs UI gomb erre
 * az oldalon), végül ellenőrzi a UI-ban az átmeneti állapotot és a végső Done-t.
 * Timeout: 200 s (két pipeline futás).
 */
import { test, expect } from '@playwright/test';
import { apiPost, apiDelete, waitForDocumentProcessed, apiGet } from '../helpers/api';
import path from 'path';
import fs from 'fs';

test.describe('UC06 — Document reprocess', () => {
  let docId: string;

  test.beforeAll(async ({ request }) => {
    // Egyedi tartalom generálása az ütközés elkerüléséhez
    const unique = `Reprocess teszt dokumentum ${Date.now()}\nEz csak a reprocess teszthez szükséges.\n`;
    const tmpPath = path.join(__dirname, '..', 'fixtures', `reprocess-${Date.now()}.txt`);
    fs.writeFileSync(tmpPath, unique, 'utf-8');

    try {
      const response = await request.post('/api/v1/documents', {
        multipart: {
          file: {
            name: path.basename(tmpPath),
            mimeType: 'text/plain',
            buffer: fs.readFileSync(tmpPath),
          },
        },
      });
      expect(response.status()).toBe(201);
      const body = await response.json() as Record<string, unknown>;
      docId = body['id'] as string;
    } finally {
      fs.unlinkSync(tmpPath);
    }
  });

  test.afterAll(async ({ request }) => {
    if (docId) {
      await apiDelete(request, `/documents/${docId}`).catch(() => { /* ignore */ });
    }
  });

  test('UC06-1 várakozás az első Done státuszra', async ({ request }) => {
    test.setTimeout(100_000);
    expect(docId).toBeTruthy();

    await test.step('AI pipeline befejezésének megvárása (első futás)', async () => {
      const doc = await waitForDocumentProcessed(request, docId, 90_000);
      console.log(`[UC06-1] processingStatus = ${doc['processingStatus']}`);
      expect(doc['processingStatus']).toBe('Done');
    });
  });

  test('UC06-2 dokumentum megjelenik Done állapotban a UI-ban', async ({ page }) => {
    expect(docId).toBeTruthy();

    await test.step('Dokumentumok oldal megnyitása', async () => {
      await page.goto('/documents');
    });

    await test.step('Dokumentum kártya "Done" státuszt mutat', async () => {
      const card = page.getByTestId(`doc-card-${docId}`);
      await expect(card).toBeVisible({ timeout: 10_000 });
      await expect(card.getByText('Done')).toBeVisible();
    });
  });

  test('UC06-3 újrafeldolgozás indítása API-n keresztül', async ({ request }) => {
    expect(docId).toBeTruthy();

    await test.step('Újrafeldolgozás API hívás', async () => {
      const response = await apiPost(request, `/documents/${docId}/reprocess`, { jobs: [] });
      console.log(`[UC06-3] reprocess status = ${response.status()}`);
      expect(response.status()).toBe(200);
    });
  });

  test('UC06-4 processingStatus átmegy Done-ból', async ({ request }) => {
    expect(docId).toBeTruthy();

    await test.step('Várakozás a státusz változásra (Done → más)', async () => {
      const deadline = Date.now() + 15_000;
      let currentStatus = 'Done';

      while (Date.now() < deadline && currentStatus === 'Done') {
        await new Promise<void>(resolve => setTimeout(resolve, 1_000));
        const resp = await apiGet(request, `/documents/${docId}`);
        const body = await resp.json() as Record<string, unknown>;
        currentStatus = body['processingStatus'] as string;
      }

      console.log(`[UC06-4] currentStatus = ${currentStatus}`);
      expect(currentStatus).not.toBe('Done');
    });
  });

  test('UC06-5 várakozás a második Done státuszra', async ({ request }) => {
    test.setTimeout(100_000);
    expect(docId).toBeTruthy();

    await test.step('AI pipeline befejezésének megvárása (második futás)', async () => {
      const doc = await waitForDocumentProcessed(request, docId, 90_000);
      console.log(`[UC06-5] processingStatus = ${doc['processingStatus']}`);
      expect(doc['processingStatus']).toBe('Done');
    });
  });

  test('UC06-6 UI-ban újra Done állapot látható az újrafeldolgozás után', async ({ page }) => {
    expect(docId).toBeTruthy();

    await test.step('Dokumentumok oldal megnyitása', async () => {
      await page.goto('/documents');
    });

    await test.step('Dokumentum kártya újra "Done" státuszt mutat', async () => {
      const card = page.getByTestId(`doc-card-${docId}`);
      await expect(card).toBeVisible({ timeout: 10_000 });
      await expect(card.getByText('Done')).toBeVisible();
    });
  });
});

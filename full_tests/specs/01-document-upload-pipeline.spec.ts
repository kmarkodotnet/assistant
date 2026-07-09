/**
 * UC01 — Dokumentum feltöltése UI-on keresztül + AI pipeline ellenőrzése
 * UC07 — Duplikált dokumentum észlelése (SHA256)
 *
 * Az AI pipeline (ExtractText → DetectLanguage → Summarize → Embed)
 * a worker által orkestrált, max. 90 mp-et engedélyezünk.
 * A feltöltés UI-driven: a dropzone file input-ját használjuk.
 * Pipeline-ellenőrzés: API polling (waitForDocumentProcessed),
 * majd visszanavigálás és UI-ban a "Done" státusz ellenőrzése.
 */
import { test, expect } from '@playwright/test';
import { waitForDocumentProcessed, apiDelete } from '../helpers/api';
import path from 'path';
import fs from 'fs';

const SAMPLE = path.join(__dirname, '..', 'fixtures', 'sample.txt');

test.describe('UC01 + UC07 — Document upload & AI pipeline @smoke', () => {
  let docId: string;

  test.afterAll(async ({ request }) => {
    if (docId) {
      await apiDelete(request, `/documents/${docId}`).catch(() => { /* ignore */ });
    }
  });

  test('UC01-1 dokumentum feltöltése dropzone-on keresztül', async ({ page }) => {
    await test.step('Dokumentumok oldal megnyitása', async () => {
      await page.goto('/documents');
    });

    await test.step('Feltöltési oldalra navigálás', async () => {
      await page.getByTestId('documents-upload-btn').click();
      await expect(page).toHaveURL(/\/documents\/upload/);
      await expect(page.getByTestId('documents-dropzone')).toBeVisible();
    });

    await test.step('Fájl kiválasztása a dropzone-on keresztül', async () => {
      await page.getByTestId('documents-file-input').setInputFiles(SAMPLE);
    });

    await test.step('Feltöltés sikeres — "Kész" státusz megjelenik', async () => {
      await expect(page.getByText('Kész').first()).toBeVisible({ timeout: 15_000 });
      await expect(page.getByText('sample.txt')).toBeVisible();
    });

    await test.step('Dokumentum ID kinyerése API-n keresztül', async () => {
      const resp = await page.request.get('/api/v1/documents?page=1&pageSize=1');
      if (resp.ok()) {
        const body = await resp.json() as Record<string, unknown>;
        const items = (Array.isArray(body) ? body : (body['items'] as unknown[])) ?? [];
        if (items.length > 0) {
          docId = (items[0] as Record<string, unknown>)['id'] as string;
        }
      }
      console.log(`[UC01-1] docId = ${docId}`);
    });
  });

  test('UC01-2 AI pipeline befejezése — processingStatus elér Done-t', async ({ request }) => {
    test.setTimeout(100_000);
    expect(docId, 'Az előző tesztnek be kell állítania a docId-t').toBeTruthy();

    await test.step('AI pipeline befejezésének megvárása (max 90 mp)', async () => {
      const doc = await waitForDocumentProcessed(request, docId, 90_000);
      console.log(`[UC01-2] processingStatus = ${doc['processingStatus']}`);
      expect(doc['processingStatus']).toBe('Done');
    });
  });

  test('UC01-3 UI-on a dokumentum "Done" státuszt mutat a listában', async ({ page }) => {
    expect(docId, 'Az előző tesztnek be kell állítania a docId-t').toBeTruthy();

    await test.step('Dokumentumok oldal megnyitása', async () => {
      await page.goto('/documents');
    });

    await test.step('Dokumentum kártya "Done" státuszt mutat', async () => {
      const card = page.getByTestId(`doc-card-${docId}`);
      await expect(card).toBeVisible({ timeout: 10_000 });
      await expect(card.getByText('Done')).toBeVisible();
    });
  });

  test('UC07 duplikált feltöltés → "Már létezik" jelzés a UI-ban', async ({ page }) => {
    expect(docId, 'UC01-1-nek be kell állítania a docId-t').toBeTruthy();

    await test.step('Feltöltési oldal megnyitása', async () => {
      await page.goto('/documents/upload');
      await expect(page.getByTestId('documents-dropzone')).toBeVisible();
    });

    await test.step('Ugyanazon fájl újbóli feltöltése', async () => {
      await page.getByTestId('documents-file-input').setInputFiles(SAMPLE);
    });

    await test.step('Duplikátum jelzés megjelenik', async () => {
      await expect(page.getByText('Már létezik')).toBeVisible({ timeout: 15_000 });
      await expect(page.getByText('Megnyitom a meglévőt →')).toBeVisible();
    });
  });
});

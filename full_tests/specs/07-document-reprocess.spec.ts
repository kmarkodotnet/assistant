/**
 * UC06 — Dokumentum újrafeldolgozása (reprocess → ExtractText job újraindítás)
 *
 * Uploads a fresh document, waits for Done, reprocesses it, then waits
 * for Done again. Timeout is extended to accommodate two pipeline runs.
 */
import { test, expect } from '@playwright/test';
import { apiGet, apiPost, apiDelete, waitForDocumentProcessed } from '../helpers/api';
import path from 'path';
import fs from 'fs';

const SAMPLE = path.join(__dirname, '..', 'fixtures', 'sample.txt');

test.describe('UC06 — Document reprocess', () => {
  let docId: string;

  test.beforeAll(async ({ request }) => {
    // Upload a fresh document with unique content so it won't clash with UC01
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
      await apiDelete(request, `/documents/${docId}`).catch(() => {/*ignore*/});
    }
  });

  test('UC06-1 wait for initial Done', async ({ request }) => {
    test.setTimeout(100_000);
    expect(docId).toBeTruthy();

    const doc = await waitForDocumentProcessed(request, docId, 90_000);
    expect(doc['processingStatus']).toBe('Done');
  });

  test('UC06-2 POST /documents/{id}/reprocess → 200', async ({ request }) => {
    expect(docId).toBeTruthy();

    const response = await apiPost(request, `/documents/${docId}/reprocess`, { jobs: [] });
    expect(response.status()).toBe(200);
  });

  test('UC06-3 processingStatus transitions away from Done after reprocess', async ({ request }) => {
    expect(docId).toBeTruthy();

    // Poll until status is no longer Done (may transition through Pending → Processing)
    const deadline = Date.now() + 15_000;
    let currentStatus = 'Done';

    while (Date.now() < deadline && currentStatus === 'Done') {
      await new Promise<void>(resolve => setTimeout(resolve, 1_000));
      const resp = await apiGet(request, `/documents/${docId}`);
      const body = await resp.json() as Record<string, unknown>;
      currentStatus = body['processingStatus'] as string;
    }

    // It should have moved away from Done
    expect(currentStatus).not.toBe('Done');
  });

  test('UC06-4 wait for second Done after reprocess', async ({ request }) => {
    test.setTimeout(100_000);
    expect(docId).toBeTruthy();

    const doc = await waitForDocumentProcessed(request, docId, 90_000);
    expect(doc['processingStatus']).toBe('Done');
  });
});

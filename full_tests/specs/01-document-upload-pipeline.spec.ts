/**
 * UC01 — Dokumentum feltöltése + AI pipeline
 * UC07 — Duplikált dokumentum észlelése (SHA256)
 *
 * Runs against the real stack. The AI pipeline (ExtractText → DetectLanguage →
 * Summarize → Embed) is orchestrated by the worker; allow up to 90 s.
 */
import { test, expect } from '@playwright/test';
import { apiGet, waitForDocumentProcessed } from '../helpers/api';
import path from 'path';
import fs from 'fs';

const SAMPLE = path.join(__dirname, '..', 'fixtures', 'sample.txt');

test.describe('UC01 + UC07 — Document upload & AI pipeline @smoke', () => {
  let docId: string;

  test.afterAll(async ({ request }) => {
    // Cleanup: delete the uploaded document
    if (docId) {
      await request.delete(`/api/v1/documents/${docId}`);
    }
  });

  test('UC01-1 POST /documents → 201 with document id', async ({ request }) => {
    const response = await request.post('/api/v1/documents', {
      multipart: {
        file: {
          name: 'sample.txt',
          mimeType: 'text/plain',
          buffer: fs.readFileSync(SAMPLE),
        },
      },
    });

    expect(response.status()).toBe(201);

    const body = await response.json() as Record<string, unknown>;
    expect(typeof body['id']).toBe('string');
    expect((body['id'] as string).length).toBeGreaterThan(0);

    docId = body['id'] as string;
  });

  test('UC01-2 AI pipeline completes — processingStatus reaches Done', async ({ request }) => {
    test.setTimeout(100_000); // 90 s pipeline + buffer

    expect(docId, 'Previous test must have set docId').toBeTruthy();

    const doc = await waitForDocumentProcessed(request, docId, 90_000);
    expect(doc['processingStatus']).toBe('Done');
  });

  test('UC01-3 GET /documents/{id}/text returns non-empty content', async ({ request }) => {
    expect(docId, 'Previous test must have set docId').toBeTruthy();

    const response = await apiGet(request, `/documents/${docId}/text`);
    expect(response.ok()).toBeTruthy();

    const body = await response.json() as Record<string, unknown>;
    const content = body['content'] as string | undefined;
    expect(content).toBeTruthy();
    expect((content ?? '').length).toBeGreaterThan(0);
  });

  test('UC07 duplicate upload → 409 Conflict', async ({ request }) => {
    expect(docId, 'Previous test must have set docId').toBeTruthy();

    // Upload the exact same bytes again
    const response = await request.post('/api/v1/documents', {
      multipart: {
        file: {
          name: 'sample.txt',
          mimeType: 'text/plain',
          buffer: fs.readFileSync(SAMPLE),
        },
      },
    });

    // Must be rejected as a duplicate
    expect(response.status()).toBe(409);
  });
});

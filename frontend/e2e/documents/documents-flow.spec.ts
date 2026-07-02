import { test, expect } from '@playwright/test';
import { mockAuth, mockJson } from '../helpers/mock-api';
import { adminUser } from '../fixtures/users';

const doc1 = {
  id: 'aaaaaaaa-0000-0000-0000-000000000001',
  title: 'Gázszámla 2026 június',
  originalFileName: 'gazszamla-2026-06.pdf',
  mimeType: 'application/pdf',
  sizeBytes: 245_000,
  sha256: 'a'.repeat(64),
  sourceType: 'Upload',
  isPrivate: false,
  processingStatus: 'Done',
  documentDate: '2026-06-15',
  relatedFamilyMemberId: null,
  createdByUserAccountId: adminUser.userAccountId,
  createdUtc: '2026-06-20T10:00:00Z',
  updatedUtc: '2026-06-20T10:05:00Z',
};

const doc2 = {
  ...doc1,
  id: 'aaaaaaaa-0000-0000-0000-000000000002',
  title: 'Orvosi lelet',
  originalFileName: 'orvosi-lelet.pdf',
  processingStatus: 'Extracting',
};

test.describe('Dokumentumok @smoke', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuth(page, adminUser);
  });

  test('QA-C2-01 üres dokumentumlista', async ({ page }) => {
    await mockJson(page, '**/api/v1/documents?**', { items: [], page: 1, pageSize: 50, totalCount: 0 });

    await page.goto('/documents');

    await expect(page.getByText('Nincsenek dokumentumok')).toBeVisible();
  });

  test('QA-C2-02 dokumentumlista tételekkel', async ({ page }) => {
    await mockJson(page, '**/api/v1/documents?**', { items: [doc1, doc2], page: 1, pageSize: 50, totalCount: 2 });

    await page.goto('/documents');

    await expect(page.locator('[data-testid="doc-card-title"]', { hasText: doc1.title })).toBeVisible();
    await expect(page.locator('[data-testid="doc-card-title"]', { hasText: doc2.title })).toBeVisible();
    await expect(page.getByText('Done')).toBeVisible();
    await expect(page.getByText('Extracting')).toBeVisible();
  });

  test('QA-C2-03 dokumentum törlése megerősítéssel', async ({ page }) => {
    const items = [doc1];
    await page.route('**/api/v1/documents?**', async route => {
      await route.fulfill({ json: { items, page: 1, pageSize: 50, totalCount: items.length } });
    });
    let deleteCalled = false;
    await page.route(`**/api/v1/documents/${doc1.id}`, async route => {
      if (route.request().method() === 'DELETE') {
        deleteCalled = true;
        items.length = 0;
        await route.fulfill({ status: 204 });
        return;
      }
      await route.continue();
    });

    await page.goto('/documents');
    await expect(page.locator('[data-testid="doc-card-title"]', { hasText: doc1.title })).toBeVisible();

    await page.locator('[data-testid="doc-card-delete"]').click();
    await expect(page.locator('[data-testid="confirm-dialog-overlay"]')).toBeVisible();
    await page.locator('[data-testid="confirm-ok"]').click();

    expect(deleteCalled).toBe(true);
    await expect(page.getByText('Nincsenek dokumentumok')).toBeVisible();
  });

  test('QA-C1-01 dokumentum feltöltése (golden path)', async ({ page }) => {
    await mockJson(page, '**/api/v1/documents?**', { items: [doc1], page: 1, pageSize: 50, totalCount: 1 });
    await page.route('**/api/v1/documents', async route => {
      if (route.request().method() === 'POST') {
        await route.fulfill({ status: 201, json: doc1 });
        return;
      }
      await route.continue();
    });

    await page.goto('/documents/upload');

    await page.setInputFiles('[data-testid="documents-file-input"]', {
      name: 'gazszamla-2026-06.pdf',
      mimeType: 'application/pdf',
      buffer: Buffer.from('%PDF-1.4 teszt tartalom'),
    });

    await expect(page.locator('[data-testid="upload-done-btn"]')).toBeVisible();
    await page.locator('[data-testid="upload-done-btn"]').click();

    await expect(page).toHaveURL(/\/documents$/);
  });

  test('QA-C3-01 dokumentum részletek tab-váltás', async ({ page }) => {
    const detail = { ...doc1, textSummary: { charCount: 1200, languageDetected: 'hu', isManuallyEdited: false, extractionMethod: 'PdfTextLayer' } };
    await mockJson(page, `**/api/v1/documents/${doc1.id}`, detail);

    await page.goto(`/documents/${doc1.id}`);

    await expect(page.locator('[data-testid="detail-title"]')).toHaveText(doc1.title);

    await page.locator('[data-testid="detail-tab-text"]').click();
    // QA-C4-BUG: a Szöveg tab jelenleg soha nem tölti be a kinyert szöveget,
    // lásd docs/qa/ui-test-scenarios.md — ez a jelenlegi (hibás) állapotot rögzíti.
    await expect(page.getByText('A szöveg kinyerése folyamatban van.')).toBeVisible();
  });
});

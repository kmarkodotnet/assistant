import { test, expect } from '@playwright/test';
import { mockAuth, mockJson } from '../helpers/mock-api';
import { adminUser } from '../fixtures/users';

const emptySavedSearches: unknown[] = [];

const searchResponse = {
  query: 'gázszámla',
  mode: 'Auto',
  hits: [
    {
      id: 'hit-0001',
      entityType: 'Document',
      entityId: 'doc-0001',
      title: 'Gázszámla 2026 június',
      snippet: 'A gázszámla összege 12 345 Ft...',
      confidence: 0.92,
      tags: [],
    },
  ],
  totalCount: 1,
  answer: null,
  answerConfidence: null,
  sources: [],
  modeUsed: 'Text',
  tookMs: 45,
};

test.describe('Keresés (Search) @smoke', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuth(page, adminUser);
    // Mock saved searches endpoint (loaded on page init)
    await page.route('**/api/v1/search/saved**', route =>
      route.fulfill({ json: emptySavedSearches })
    );
  });

  // QA-S1-01: A keresés oldal betölt, üres állapot szöveg látható
  test('QA-S1-01 keresés oldal betölt, üres állapot szöveg látható', async ({ page }) => {
    await page.goto('/search');

    await expect(page.getByText('Kérdezz bármit a dokumentumaidról')).toBeVisible();
    await expect(page.locator('[data-testid="search-input"]')).toBeVisible();
    await expect(page.locator('[data-testid="search-submit"]')).toBeVisible();
  });

  // QA-S1-02: search-input mezőbe írva a search-submit gomb aktívvá válik
  test('QA-S1-02 search-submit gomb aktívvá válik szöveg beírásakor', async ({ page }) => {
    await page.goto('/search');

    // Submit button starts disabled (empty input)
    await expect(page.locator('[data-testid="search-submit"]')).toBeDisabled();

    // Type a query
    await page.locator('[data-testid="search-input"]').fill('gázszámla');

    // Submit button becomes enabled
    await expect(page.locator('[data-testid="search-submit"]')).toBeEnabled();
  });

  // QA-S1-03: search-mode-select létezik és váltható
  test('QA-S1-03 search-mode-select létezik és módot lehet váltani', async ({ page }) => {
    await page.goto('/search');

    const modeSelect = page.locator('[data-testid="search-mode-select"]');
    await expect(modeSelect).toBeVisible();

    // Default value should be Auto
    await expect(modeSelect).toHaveValue('Auto');

    // Switch to Semantic mode
    await modeSelect.selectOption('Semantic');
    await expect(modeSelect).toHaveValue('Semantic');

    // Switch to Qa mode
    await modeSelect.selectOption('Qa');
    await expect(modeSelect).toHaveValue('Qa');

    // Switch to Text mode
    await modeSelect.selectOption('Text');
    await expect(modeSelect).toHaveValue('Text');

    // Switch to Filter mode
    await modeSelect.selectOption('Filter');
    await expect(modeSelect).toHaveValue('Filter');
  });

  // QA-S1-04: search-clear-history csak history esetén látható
  test('QA-S1-04 search-clear-history gomb csak előzmény esetén látható', async ({ page }) => {
    await page.route('**/api/v1/search', async route => {
      if (route.request().method() === 'POST') {
        await route.fulfill({ json: searchResponse });
        return;
      }
      await route.continue();
    });

    await page.goto('/search');

    // Before any search: clear-history button must NOT be present
    await expect(page.locator('[data-testid="search-clear-history"]')).not.toBeVisible();

    // Perform a search
    await page.locator('[data-testid="search-input"]').fill('gázszámla');
    await page.locator('[data-testid="search-submit"]').click();

    // After a search result appears, history exists → clear-history button must be visible
    await expect(page.getByText('Gázszámla 2026 június')).toBeVisible();
    await expect(page.locator('[data-testid="search-clear-history"]')).toBeVisible();

    // Click clear history
    await page.locator('[data-testid="search-clear-history"]').click();

    // History cleared → button disappears, empty state returns
    await expect(page.locator('[data-testid="search-clear-history"]')).not.toBeVisible();
    await expect(page.getByText('Kérdezz bármit a dokumentumaidról')).toBeVisible();
  });

  // QA-S1-05: Keresés végrehajtása és találat megjelenítése
  test('QA-S1-05 keresés végrehajtása találattal', async ({ page }) => {
    await page.route('**/api/v1/search', async route => {
      if (route.request().method() === 'POST') {
        await route.fulfill({ json: searchResponse });
        return;
      }
      await route.continue();
    });

    await page.goto('/search');

    await page.locator('[data-testid="search-input"]').fill('gázszámla');
    await page.locator('[data-testid="search-submit"]').click();

    // The search hit title should appear in the chat answer area
    await expect(page.getByText('Gázszámla 2026 június')).toBeVisible();
  });

  // QA-S1-06: Enter billentyű is elküldi a keresést
  test('QA-S1-06 Enter billentyűre keresés indul', async ({ page }) => {
    let searchCalled = false;
    await page.route('**/api/v1/search', async route => {
      if (route.request().method() === 'POST') {
        searchCalled = true;
        await route.fulfill({ json: searchResponse });
        return;
      }
      await route.continue();
    });

    await page.goto('/search');

    await page.locator('[data-testid="search-input"]').fill('gázszámla');
    await page.locator('[data-testid="search-input"]').press('Enter');

    await expect.poll(() => searchCalled).toBe(true);
  });

  // QA-S1-07: Shift+Enter NEM küldi el a keresést (új sor)
  test('QA-S1-07 Shift+Enter nem indít keresést', async ({ page }) => {
    let searchCalled = false;
    await page.route('**/api/v1/search', async route => {
      if (route.request().method() === 'POST') {
        searchCalled = true;
        await route.fulfill({ json: searchResponse });
        return;
      }
      await route.continue();
    });

    await page.goto('/search');

    await page.locator('[data-testid="search-input"]').fill('gázszámla');
    await page.locator('[data-testid="search-input"]').press('Shift+Enter');

    // Short wait — search must NOT have been called
    await page.waitForTimeout(300);
    expect(searchCalled).toBe(false);
  });
});

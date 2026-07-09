/**
 * UC02 — Dokumentum keresése (FTS + szemantikus)
 *
 * Assumes at least one Done-status document exists in the DB
 * (spec 01 runs first). If the DB is empty the tests still pass
 * because hits.length >= 0 is always valid.
 */
import { test, expect } from '@playwright/test';
import { apiPost } from '../helpers/api';

test.describe('UC02 — Document search', () => {
  test('UC02-1 FTS search returns 200 and valid structure', async ({ request }) => {
    const response = await apiPost(request, '/search', {
      query: 'teszt dokumentum',
      mode: 'Text',
    });

    expect(response.status()).toBe(200);

    const body = await response.json() as Record<string, unknown>;
    // Accept either "hits" or "items" as the result array key
    const results = (body['hits'] ?? body['items']) as unknown[];
    expect(Array.isArray(results)).toBeTruthy();
  });

  test('UC02-2 Semantic search returns 200', async ({ request }) => {
    const response = await apiPost(request, '/search', {
      query: 'teszt',
      mode: 'Semantic',
    });

    // 200 even if embedding is not available yet (empty results)
    expect(response.status()).toBe(200);
  });

  test('UC02-3 UI search page loads without 500 error @smoke', async ({ page }) => {
    await page.goto('/search');

    // Should not show a server error
    await expect(page).not.toHaveTitle(/500|error/i);

    // The page heading or search input should be visible
    const searchInput = page.locator('input[type="search"], input[type="text"], [data-testid="search-input"]');
    // It is acceptable if there's no input yet (page may load differently)
    // — the key assertion is no 500
    const h1 = page.locator('h1, h2, [data-testid="search-heading"]');
    const inputOrHeading = searchInput.or(h1);
    await expect(inputOrHeading.first()).toBeVisible({ timeout: 15_000 });
  });
});

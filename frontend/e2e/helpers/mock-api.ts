import type { Page } from '@playwright/test';
import type { MockCurrentUser } from '../fixtures/users';

/**
 * Mock az `/api/v1/auth/me` és a navbar-ban minden oldalon meghívott
 * olvasatlan-értesítés lekérdezésre. Valós Google OAuth-ot böngészős
 * Playwright-tesztből nem lehet végigvinni, ezért a bejelentkezett
 * állapotot API-szinten mockoljuk (lásd docs/qa/ui-test-scenarios.md 2.).
 */
export async function mockAuth(page: Page, user: MockCurrentUser): Promise<void> {
  await page.route('**/api/v1/auth/me', route => route.fulfill({ json: user }));
  await page.route('**/api/v1/notifications?**', route =>
    route.fulfill({ json: { items: [], totalCount: 0, hasMore: false } })
  );
}

/** Adott URL-mintára adott metódussal érkező kéréseket JSON válasszal old fel. */
export async function mockJson(
  page: Page,
  urlGlob: string,
  json: unknown,
  method = 'GET'
): Promise<void> {
  await page.route(urlGlob, async route => {
    if (route.request().method() !== method) {
      await route.continue();
      return;
    }
    await route.fulfill({ json });
  });
}

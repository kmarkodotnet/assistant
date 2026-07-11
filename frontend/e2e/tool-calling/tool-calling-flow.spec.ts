import { test, expect } from '@playwright/test';
import { mockAuth } from '../helpers/mock-api';
import { adminUser } from '../fixtures/users';

/**
 * E8. Természetes nyelvű parancsok (LLM tool-calling) — mvp-backlog.md E8,
 * api-design.md §16.1/§16.3, ai-pipeline.md §11.
 *
 * A backend/Ollama pipeline (ToolCallPlanner, HMAC proposalToken) itt
 * mockolt a `/api/v1/search` és `/api/v1/tool-calls/*` végpontokon —
 * ugyanaz a mintázat, mint `suggestions-flow.spec.ts` és
 * `reminders-flow.spec.ts` konvenciója (nincs élő docker-compose/Ollama
 * függőség az e2e futáshoz, csak API-mock).
 */

const emptySavedSearches: unknown[] = [];

function commandResponse(overrides: Record<string, unknown> = {}) {
  return {
    hits: [],
    totalCount: 0,
    modeUsed: 'Command',
    answer: null,
    toolCallProposal: null,
    ...overrides,
  };
}

function proposal(overrides: Record<string, unknown> = {}) {
  return {
    proposalToken: 'tok-warranty-0001',
    toolName: 'create_reminder',
    summary:
      'Létrehozzak egy emlékeztetőt a mosógép garanciájának lejárta előtt 3 nappal?',
    parameters: [
      { label: 'Termék', value: 'Mosógép (Bosch WAT28)' },
      { label: 'Lejárat', value: '2027-03-01' },
      { label: 'Emlékeztető', value: '2027-02-26 09:00 (3 nappal előbb)' },
      { label: 'Csatorna', value: 'Alkalmazáson belül' },
    ],
    warnings: [],
    expiresUtc: '2026-07-11T12:40:00Z',
    ...overrides,
  };
}

const INSTRUCTION = 'Emlékeztess 3 nappal a mosógép garancia lejárta előtt';

async function askInCommandMode(page: import('@playwright/test').Page, text: string) {
  await page.goto('/search');
  await page.locator('[data-testid="search-mode-select"]').selectOption('Command');
  await page.locator('[data-testid="search-input"]').fill(text);
  await page.locator('[data-testid="search-submit"]').click();
}

test.describe('Természetes nyelvű parancsok (LLM tool-calling) @smoke', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuth(page, adminUser);
    await page.route('**/api/v1/search/saved**', route =>
      route.fulfill({ json: emptySavedSearches })
    );
  });

  // QA-E8-01: Happy path — javaslat-kártya megjelenik, jóváhagyás → végrehajtás
  test('QA-E8-01 parancs jóváhagyása végrehajtja a tool-hívást', async ({ page }) => {
    await page.route('**/api/v1/search', async route => {
      if (route.request().method() !== 'POST') return route.continue();
      await route.fulfill({ json: commandResponse({ toolCallProposal: proposal() }) });
    });

    let confirmPayload: unknown;
    await page.route('**/api/v1/tool-calls/confirm', async route => {
      confirmPayload = route.request().postDataJSON();
      await route.fulfill({
        json: {
          executed: true,
          resultType: 'Reminder',
          resultId: '01910a0c-0000-0000-0000-000000000001',
          summary: 'Emlékeztető létrehozva 2027-02-26 09:00-ra.',
        },
      });
    });

    await askInCommandMode(page, INSTRUCTION);

    // Javaslat-kártya megjelenik a feloldott paraméterekkel
    await expect(page.getByText(proposal().summary)).toBeVisible();
    await expect(page.getByText('Mosógép (Bosch WAT28)')).toBeVisible();
    await expect(page.getByText('2027-03-01')).toBeVisible();
    const confirmBtn = page.locator('[data-testid="toolcall-confirm"]');
    const rejectBtn = page.locator('[data-testid="toolcall-reject"]');
    await expect(confirmBtn).toBeVisible();
    await expect(rejectBtn).toBeVisible();

    await confirmBtn.click();

    await expect.poll(() => confirmPayload).toEqual({ proposalToken: 'tok-warranty-0001' });

    // Sikeres végrehajtás visszaigazolása látszik
    await expect(page.locator('[data-testid="toolcall-result"]')).toContainText(
      'Emlékeztető létrehozva 2027-02-26 09:00-ra.'
    );

    // A jóváhagyás/elutasítás gombok eltűnnek
    await expect(confirmBtn).not.toBeVisible();
    await expect(rejectBtn).not.toBeVisible();
  });

  // QA-E8-02: Elutasítás — nincs végrehajtás, a kártya elutasított állapotba vált
  test('QA-E8-02 parancs elutasítása nem hajt végre semmit', async ({ page }) => {
    await page.route('**/api/v1/search', async route => {
      if (route.request().method() !== 'POST') return route.continue();
      await route.fulfill({
        json: commandResponse({
          toolCallProposal: proposal({ proposalToken: 'tok-warranty-0002' }),
        }),
      });
    });

    let confirmCalled = false;
    await page.route('**/api/v1/tool-calls/confirm', async route => {
      confirmCalled = true;
      await route.fulfill({ json: { executed: true, resultType: 'Reminder', resultId: 'x', summary: 'x' } });
    });

    let rejectPayload: unknown;
    await page.route('**/api/v1/tool-calls/reject', async route => {
      rejectPayload = route.request().postDataJSON();
      await route.fulfill({ status: 204 });
    });

    await askInCommandMode(page, INSTRUCTION);

    const confirmBtn = page.locator('[data-testid="toolcall-confirm"]');
    const rejectBtn = page.locator('[data-testid="toolcall-reject"]');
    await expect(rejectBtn).toBeVisible();

    await rejectBtn.click();

    await expect.poll(() => rejectPayload).toMatchObject({ proposalToken: 'tok-warranty-0002' });

    // Elutasított állapot — nincs végrehajtás-visszaigazolás
    await expect(page.locator('[data-testid="toolcall-result"]')).toContainText('Elutasítva');
    await expect(confirmBtn).not.toBeVisible();
    await expect(rejectBtn).not.toBeVisible();
    expect(confirmCalled).toBe(false);
  });

  // QA-E8-03: Nem értelmezhető utasítás Parancs módban — nincs javaslat-kártya,
  // nem jelenik meg üres kártya (ai-pipeline.md §11.3: action:"none" → Q&A visszaesés)
  test('QA-E8-03 nem-parancs szöveg Parancs módban nem hoz létre javaslat-kártyát', async ({ page }) => {
    await page.route('**/api/v1/search', async route => {
      if (route.request().method() !== 'POST') return route.continue();
      // ToolCallPlanner action:"none"-t ad -> toolCallProposal null, nincs answer/hits
      await route.fulfill({ json: commandResponse() });
    });

    await askInCommandMode(page, 'Mikor jár le a biztosításom?');

    // Nincs javaslat-kártya és nincs jóváhagyás/elutasítás gomb
    await expect(page.locator('[data-testid="toolcall-confirm"]')).toHaveCount(0);
    await expect(page.locator('[data-testid="toolcall-reject"]')).toHaveCount(0);
    await expect(page.locator('[data-testid="toolcall-result"]')).toHaveCount(0);

    // Normál üres-állapot visszaesés jelenik meg, nem crashel a UI
    await expect(page.getByText('Nem találtam eredményt')).toBeVisible();
  });

  // QA-E8-04: Dupla-klikk védelem — gyors dupla kattintás nem vált ki két végrehajtást
  test('QA-E8-04 dupla-klikk a Jóváhagyás gombon csak egyszer hajtja végre', async ({ page }) => {
    await page.route('**/api/v1/search', async route => {
      if (route.request().method() !== 'POST') return route.continue();
      await route.fulfill({
        json: commandResponse({ toolCallProposal: proposal({ proposalToken: 'tok-warranty-0004' }) }),
      });
    });

    let confirmCallCount = 0;
    await page.route('**/api/v1/tool-calls/confirm', async route => {
      confirmCallCount += 1;
      // Mesterséges késleltetés, hogy a valós hálózati latency alatti
      // dupla kattintást szimuláljuk.
      await new Promise(resolve => setTimeout(resolve, 300));
      await route.fulfill({
        json: {
          executed: true,
          resultType: 'Reminder',
          resultId: '01910a0c-0000-0000-0000-000000000004',
          summary: 'Emlékeztető létrehozva.',
        },
      });
    });

    await askInCommandMode(page, INSTRUCTION);
    await expect(page.locator('[data-testid="toolcall-confirm"]')).toBeVisible();

    // Két natív click esemény ugyanabban a szinkron JS taskban — ez szimulálja
    // a leggyorsabb lehetséges dupla-kattintást, mielőtt a CD a gombot
    // letiltaná/eltüntetné.
    await page.evaluate(() => {
      const btn = document.querySelector<HTMLButtonElement>('[data-testid="toolcall-confirm"]');
      btn?.click();
      btn?.click();
    });

    await expect(page.locator('[data-testid="toolcall-result"]')).toContainText(
      'Emlékeztető létrehozva.',
      { timeout: 5000 }
    );

    expect(confirmCallCount).toBe(1);
  });

  // QA-E8-05: Hibaeset — lejárt/érvénytelen token esetén hibaüzenet jelenik meg,
  // a UI nem crashel, a kártya visszaáll megerősítésre várakozó állapotba
  test('QA-E8-05 lejárt token esetén hibaüzenet jelenik meg, UI nem crashel', async ({ page }) => {
    await page.route('**/api/v1/search', async route => {
      if (route.request().method() !== 'POST') return route.continue();
      await route.fulfill({
        json: commandResponse({ toolCallProposal: proposal({ proposalToken: 'tok-warranty-0005' }) }),
      });
    });

    await page.route('**/api/v1/tool-calls/confirm', async route => {
      await route.fulfill({
        status: 401,
        json: { type: 'about:blank', title: 'A javaslat lejárt vagy érvénytelen.', status: 401 },
      });
    });

    await askInCommandMode(page, INSTRUCTION);

    const confirmBtn = page.locator('[data-testid="toolcall-confirm"]');
    await confirmBtn.click();

    // Hibaüzenet toast jelenik meg
    await expect(page.locator('[data-testid="toast-message"]').first()).toContainText(
      'Nem sikerült végrehajtani a parancsot.'
    );

    // A kártya visszaáll megerősítésre váró állapotba — nincs végrehajtva
    await expect(page.locator('[data-testid="toolcall-result"]')).toHaveCount(0);
    await expect(confirmBtn).toBeVisible();
    await expect(page.locator('[data-testid="toolcall-reject"]')).toBeVisible();
  });
});

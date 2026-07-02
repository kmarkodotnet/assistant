import { test, expect } from '@playwright/test';
import { mockAuth } from '../helpers/mock-api';
import { adminUser } from '../fixtures/users';

const member1 = {
  id: 'bbbbbbbb-0000-0000-0000-000000000001',
  displayName: 'Anna',
  fullName: 'Kovács Anna',
  relation: 'Spouse',
  birthDate: '1990-03-12',
  notes: null,
  hasUserAccount: true,
  rowVersion: 'v1',
  deletedUtc: null,
};

test.describe('Családtagok @smoke', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuth(page, adminUser);
  });

  test('QA-B1-01 családtag létrehozása (golden path)', async ({ page }) => {
    const members: unknown[] = [];
    await page.route('**/api/v1/family-members', async route => {
      if (route.request().method() === 'POST') {
        const body = route.request().postDataJSON() as { displayName: string; relation: string };
        expect(body.displayName).toBe('Anna');
        expect(body.relation).toBe('Spouse');
        members.push(member1);
        await route.fulfill({ status: 201, json: member1 });
        return;
      }
      await route.fulfill({ json: members });
    });

    await page.goto('/family');
    await expect(page.getByText('Nincsenek még családtagok')).toBeVisible();

    await page.locator('[data-testid="family-add-btn"]').click();
    await expect(page.locator('[data-testid="family-form-dialog"]')).toBeVisible();

    await page.locator('[data-testid="family-form-displayName"]').fill('Anna');
    await page.locator('[data-testid="family-form-relation"]').selectOption('Spouse');
    await page.locator('[data-testid="family-form-save"]').click();

    await expect(page.locator('[data-testid="family-form-dialog"]')).toBeHidden();
    await expect(page.locator('[data-testid="family-card-name"]')).toHaveText('Anna');
  });

  test('QA-B1-03 családtag szerkesztése', async ({ page }) => {
    let current = { ...member1 };
    await page.route('**/api/v1/family-members', async route => {
      if (route.request().method() === 'GET') {
        await route.fulfill({ json: [current] });
        return;
      }
      await route.continue();
    });
    await page.route(`**/api/v1/family-members/${member1.id}`, async route => {
      if (route.request().method() === 'PATCH') {
        current = { ...current, displayName: 'Anna Mária' };
        await route.fulfill({ json: current });
        return;
      }
      await route.continue();
    });

    await page.goto('/family');
    await expect(page.locator('[data-testid="family-card-name"]')).toHaveText('Anna');

    await page.locator('[data-testid="family-card-edit"]').click();
    await page.locator('[data-testid="family-form-displayName"]').fill('Anna Mária');
    await page.locator('[data-testid="family-form-save"]').click();

    await expect(page.locator('[data-testid="family-card-name"]')).toHaveText('Anna Mária');
  });

  test('QA-B1-04 családtag törlése megerősítéssel', async ({ page }) => {
    const members = [{ ...member1 }];
    await page.route('**/api/v1/family-members', async route => {
      if (route.request().method() === 'GET') {
        await route.fulfill({ json: members });
        return;
      }
      await route.continue();
    });
    await page.route(`**/api/v1/family-members/${member1.id}`, async route => {
      if (route.request().method() === 'DELETE') {
        members.length = 0;
        await route.fulfill({ status: 204 });
        return;
      }
      await route.continue();
    });

    await page.goto('/family');
    await expect(page.locator('[data-testid="family-card-name"]')).toBeVisible();

    await page.locator('[data-testid="family-card-delete"]').click();
    await page.locator('[data-testid="confirm-ok"]').click();

    await expect(page.getByText('Nincsenek még családtagok')).toBeVisible();
  });
});

import { test, expect } from '@playwright/test';

test.describe('Auth flow @smoke', () => {
  test('login page shows Google button', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByText('Bejelentkezés a Family OS-be')).toBeVisible();
    await expect(page.locator('[data-testid="login-google-btn"]')).toBeVisible();
  });

  test('unauthenticated redirect to login', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL(/.*login.*/);
  });
});

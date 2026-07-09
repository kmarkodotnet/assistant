import { defineConfig, devices } from '@playwright/test';
import path from 'path';

/**
 * Full-stack integration test config.
 * Runs against the real docker-compose stack at http://localhost.
 * Auth is handled via a dev-only test-login endpoint; storage states are
 * written by global-setup.ts before any test runs.
 *
 * Note: @playwright/test is already installed in frontend/node_modules.
 * Run tests from the repo root with:
 *   npx --prefix frontend playwright test --config full_tests/playwright.config.ts
 */

export const ADMIN_STORAGE = path.join(__dirname, '.auth', 'admin.json');
export const ADULT_STORAGE = path.join(__dirname, '.auth', 'adult.json');

export default defineConfig({
  testDir: './specs',
  globalSetup: './global-setup.ts',

  /* Serial execution — AI pipeline tests are time-sensitive */
  fullyParallel: false,
  workers: 1,

  /* Retry once on CI to absorb transient network blips */
  retries: process.env['CI'] ? 1 : 0,
  forbidOnly: !!process.env['CI'],

  /* AI pipeline can take up to 90 s */
  timeout: 60_000,

  reporter: [['html', { outputFolder: '../test-results/full-tests-report' }]],

  use: {
    baseURL: 'http://localhost',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    /* Default project uses admin storage state */
    storageState: ADMIN_STORAGE,
  },

  projects: [
    {
      name: 'full-stack-admin',
      use: {
        ...devices['Desktop Chrome'],
        storageState: ADMIN_STORAGE,
      },
    },
    {
      name: 'full-stack-adult',
      use: {
        ...devices['Desktop Chrome'],
        storageState: ADULT_STORAGE,
      },
      /* Only run tests that explicitly opt-in with the adult project */
      testMatch: /adult\.spec\.ts/,
    },
  ],
});

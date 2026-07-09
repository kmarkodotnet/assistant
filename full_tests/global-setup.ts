/**
 * Playwright global setup — runs once before all tests.
 *
 * Calls the dev-only POST /api/v1/auth/test-login endpoint to obtain
 * real auth cookies and saves them as Playwright storage states.
 * The stack must already be up (docker-compose up) before running tests.
 */
import { chromium, FullConfig } from '@playwright/test';
import path from 'path';
import fs from 'fs';

const BASE_URL = 'http://localhost';
const AUTH_DIR = path.join(__dirname, '.auth');

async function loginAndSave(
  config: FullConfig,
  email: string,
  role: string,
  displayName: string,
  storageFile: string
): Promise<void> {
  const browser = await chromium.launch();
  const context = await browser.newContext();
  const page = await context.newPage();

  const response = await page.request.post(`${BASE_URL}/api/v1/auth/test-login`, {
    data: { email, role, displayName },
    headers: { 'Content-Type': 'application/json' },
  });

  if (!response.ok()) {
    const body = await response.text();
    throw new Error(
      `test-login failed for ${email} (${response.status()}): ${body}\n` +
      'Make sure the stack is running and ASPNETCORE_ENVIRONMENT != Production.'
    );
  }

  await context.storageState({ path: storageFile });
  await browser.close();
  console.log(`[global-setup] Saved auth state for ${email} → ${storageFile}`);
}

export default async function globalSetup(config: FullConfig): Promise<void> {
  // Ensure the .auth directory exists
  if (!fs.existsSync(AUTH_DIR)) {
    fs.mkdirSync(AUTH_DIR, { recursive: true });
  }

  const adminFile = path.join(AUTH_DIR, 'admin.json');
  const adultFile = path.join(AUTH_DIR, 'adult.json');

  await loginAndSave(
    config,
    'e2e-admin@test.local',
    'Admin',
    'E2E Admin',
    adminFile
  );

  await loginAndSave(
    config,
    'e2e-adult@test.local',
    'Adult',
    'E2E Adult',
    adultFile
  );
}

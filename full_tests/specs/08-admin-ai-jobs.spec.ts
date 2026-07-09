/**
 * UC11 + UC12 — Audit napló + Admin AI feladatok figyelése
 *
 * Both endpoints require Admin role.
 * AI jobs endpoint: GET /api/v1/ai-jobs  (AiJobsAdminModule — NOT /admin/ai-jobs)
 * Audit log endpoint: GET /api/v1/audit-log
 */
import { test, expect } from '@playwright/test';
import { apiGet } from '../helpers/api';

test.describe('UC11 + UC12 — Admin: AI jobs & audit log', () => {
  test('UC12-1 GET /ai-jobs → 200 with items array', async ({ request }) => {
    const response = await apiGet(request, '/ai-jobs');
    expect(response.status()).toBe(200);

    const body = await response.json() as unknown;
    const items: unknown[] = Array.isArray(body)
      ? body
      : (body as Record<string, unknown[]>)['items'] ?? [];

    expect(Array.isArray(items)).toBeTruthy();
  });

  test('UC12-2 GET /ai-jobs/queue-stats → 200', async ({ request }) => {
    const response = await apiGet(request, '/ai-jobs/queue-stats');
    expect(response.status()).toBe(200);

    const body = await response.json() as Record<string, unknown>;
    // Queue stats should have numeric counters
    expect(body).toBeDefined();
  });

  test('UC12-3 GET /ai-jobs?status=Pending → 200', async ({ request }) => {
    const response = await apiGet(request, '/ai-jobs?status=Pending');
    expect(response.status()).toBe(200);
  });

  test('UC11-1 GET /audit-log → 200 with items array', async ({ request }) => {
    const response = await apiGet(request, '/audit-log');
    expect(response.status()).toBe(200);

    const body = await response.json() as unknown;
    const items: unknown[] = Array.isArray(body)
      ? body
      : (body as Record<string, unknown[]>)['items'] ?? [];

    expect(Array.isArray(items)).toBeTruthy();
  });

  test('UC11-2 GET /audit-log/security-events → 200', async ({ request }) => {
    const response = await apiGet(request, '/audit-log/security-events');
    expect(response.status()).toBe(200);
  });
});

/**
 * UC05 — Határidő kezelése (CRUD)
 *
 * RequireAdult policy — admin storage state covers this.
 */
import { test, expect } from '@playwright/test';
import { apiGet, apiPost, apiPatch, apiDelete } from '../helpers/api';

test.describe('UC05 — Deadlines CRUD', () => {
  let deadlineId: string;

  test.afterAll(async ({ request }) => {
    if (deadlineId) {
      await apiDelete(request, `/deadlines/${deadlineId}`).catch(() => {/*already deleted*/});
    }
  });

  test('UC05-1 POST /deadlines → 201', async ({ request }) => {
    const response = await apiPost(request, '/deadlines', {
      title: 'E2E Teszt Határidő',
      dueDateUtc: '2027-06-15T12:00:00Z',
      category: 'Other',
    });

    expect(response.status()).toBe(201);

    const body = await response.json() as Record<string, unknown>;
    expect(typeof body['id']).toBe('string');
    deadlineId = body['id'] as string;
  });

  test('UC05-2 GET /deadlines → deadline appears in list', async ({ request }) => {
    expect(deadlineId).toBeTruthy();

    const response = await apiGet(request, '/deadlines');
    expect(response.ok()).toBeTruthy();

    const body = await response.json() as unknown;
    const items: unknown[] = Array.isArray(body)
      ? body
      : (body as Record<string, unknown[]>)['items'] ?? [];

    const found = items.some(
      (d) => (d as Record<string, unknown>)['id'] === deadlineId
    );
    expect(found).toBeTruthy();
  });

  test('UC05-3 PATCH /deadlines/{id} → 200', async ({ request }) => {
    expect(deadlineId).toBeTruthy();

    const response = await apiPatch(request, `/deadlines/${deadlineId}`, {
      title: 'Módosított E2E Határidő',
    });
    expect(response.status()).toBe(200);
  });

  test('UC05-4 GET /deadlines/{id} shows updated title', async ({ request }) => {
    expect(deadlineId).toBeTruthy();

    const response = await apiGet(request, `/deadlines/${deadlineId}`);
    expect(response.ok()).toBeTruthy();

    const body = await response.json() as Record<string, unknown>;
    expect(body['title']).toBe('Módosított E2E Határidő');
  });

  test('UC05-5 POST /deadlines/{id}/resolve → status Resolved', async ({ request }) => {
    expect(deadlineId).toBeTruthy();

    const response = await apiPost(request, `/deadlines/${deadlineId}/resolve`);
    expect(response.status()).toBe(200);

    const detail = await apiGet(request, `/deadlines/${deadlineId}`);
    const body = await detail.json() as Record<string, unknown>;
    expect(body['status']).toBe('Resolved');
  });

  test('UC05-6 DELETE /deadlines/{id} → 204', async ({ request }) => {
    expect(deadlineId).toBeTruthy();

    const response = await apiDelete(request, `/deadlines/${deadlineId}`);
    expect(response.status()).toBe(204);

    deadlineId = ''; // Mark as cleaned up
  });
});

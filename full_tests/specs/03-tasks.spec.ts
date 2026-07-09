/**
 * UC04 — Feladat kezelése (CRUD + állapotgép)
 *
 * Uses the adult storage state (RequireAdult policy for CUD operations).
 * The admin user is also Adult-capable (Admin >= Adult), so the default
 * admin storageState is used here.
 */
import { test, expect } from '@playwright/test';
import { apiGet, apiPost, apiPatch, apiDelete } from '../helpers/api';

test.describe('UC04 — Tasks CRUD & state machine @smoke', () => {
  let taskId: string;

  test.afterAll(async ({ request }) => {
    // Best-effort cleanup
    if (taskId) {
      await apiDelete(request, `/tasks/${taskId}`).catch(() => {/* already deleted */});
    }
  });

  test('UC04-1 POST /tasks → 201', async ({ request }) => {
    const response = await apiPost(request, '/tasks', {
      title: 'E2E Teszt Feladat',
      priority: 'Normal',
    });

    expect(response.status()).toBe(201);

    const body = await response.json() as Record<string, unknown>;
    expect(typeof body['id']).toBe('string');
    taskId = body['id'] as string;
  });

  test('UC04-2 GET /tasks → task appears in list', async ({ request }) => {
    expect(taskId).toBeTruthy();

    const response = await apiGet(request, '/tasks');
    expect(response.ok()).toBeTruthy();

    const body = await response.json() as unknown;
    // API may return a plain array or a paged object
    const items: unknown[] = Array.isArray(body)
      ? body
      : (body as Record<string, unknown[]>)['items'] ?? [];

    const found = items.some(
      (t) => (t as Record<string, unknown>)['id'] === taskId
    );
    expect(found).toBeTruthy();
  });

  test('UC04-3 PATCH /tasks/{id} updates title → 200', async ({ request }) => {
    expect(taskId).toBeTruthy();

    const response = await apiPatch(request, `/tasks/${taskId}`, {
      title: 'Módosított E2E Feladat',
    });
    expect(response.status()).toBe(200);
  });

  test('UC04-4 GET /tasks/{id} shows updated title', async ({ request }) => {
    expect(taskId).toBeTruthy();

    const response = await apiGet(request, `/tasks/${taskId}`);
    expect(response.ok()).toBeTruthy();

    const body = await response.json() as Record<string, unknown>;
    expect(body['title']).toBe('Módosított E2E Feladat');
  });

  test('UC04-5 POST /tasks/{id}/start → status InProgress', async ({ request }) => {
    expect(taskId).toBeTruthy();

    const response = await apiPost(request, `/tasks/${taskId}/start`);
    expect(response.status()).toBe(200);

    const detail = await apiGet(request, `/tasks/${taskId}`);
    const body = await detail.json() as Record<string, unknown>;
    expect(body['status']).toBe('InProgress');
  });

  test('UC04-6 POST /tasks/{id}/complete → status Done', async ({ request }) => {
    expect(taskId).toBeTruthy();

    const response = await apiPost(request, `/tasks/${taskId}/complete`);
    expect(response.status()).toBe(200);

    const detail = await apiGet(request, `/tasks/${taskId}`);
    const body = await detail.json() as Record<string, unknown>;
    expect(body['status']).toBe('Done');
  });

  test('UC04-7 DELETE /tasks/{id} → 204, then GET → 404', async ({ request }) => {
    expect(taskId).toBeTruthy();

    const deleteResp = await apiDelete(request, `/tasks/${taskId}`);
    expect(deleteResp.status()).toBe(204);

    const getResp = await apiGet(request, `/tasks/${taskId}`);
    expect(getResp.status()).toBe(404);

    taskId = ''; // Mark as cleaned up
  });
});

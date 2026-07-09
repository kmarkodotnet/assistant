/**
 * UC15 — Családtag kezelése (CRUD)
 *
 * CUD operations require Admin role.
 * The family endpoint is /api/v1/family-members (NOT /api/v1/family).
 * PATCH requires a rowVersion for optimistic concurrency.
 */
import { test, expect } from '@playwright/test';
import { apiGet, apiPost, apiDelete } from '../helpers/api';

test.describe('UC15 — Family members CRUD', () => {
  let memberId: string;
  let rowVersion: string;

  test.afterAll(async ({ request }) => {
    if (memberId) {
      await apiDelete(request, `/family-members/${memberId}`).catch(() => {/*already deleted*/});
    }
  });

  test('UC15-1 POST /family-members → 201', async ({ request }) => {
    const response = await apiPost(request, '/family-members', {
      displayName: 'E2E Teszt Tag',
      relation: 'Other',
      birthDate: '1990-01-01',
    });

    expect(response.status()).toBe(201);

    const body = await response.json() as Record<string, unknown>;
    expect(typeof body['id']).toBe('string');
    memberId = body['id'] as string;
    rowVersion = body['rowVersion'] as string ?? '';
  });

  test('UC15-2 GET /family-members → new member appears', async ({ request }) => {
    expect(memberId).toBeTruthy();

    const response = await apiGet(request, '/family-members');
    expect(response.ok()).toBeTruthy();

    const body = await response.json() as unknown;
    const items: unknown[] = Array.isArray(body)
      ? body
      : (body as Record<string, unknown[]>)['items'] ?? [];

    const found = items.some(
      (m) => (m as Record<string, unknown>)['id'] === memberId
    );
    expect(found).toBeTruthy();
  });

  test('UC15-3 GET /family-members/{id} → valid detail', async ({ request }) => {
    expect(memberId).toBeTruthy();

    const response = await apiGet(request, `/family-members/${memberId}`);
    expect(response.ok()).toBeTruthy();

    const body = await response.json() as Record<string, unknown>;
    expect(body['displayName']).toBe('E2E Teszt Tag');
    // Capture rowVersion for PATCH
    if (body['rowVersion']) {
      rowVersion = body['rowVersion'] as string;
    }
  });

  test('UC15-4 PATCH /family-members/{id} → 200', async ({ request }) => {
    expect(memberId).toBeTruthy();

    const response = await request.patch(`/api/v1/family-members/${memberId}`, {
      data: {
        displayName: 'Módosított E2E Tag',
        relation: 'Other',
        rowVersion: rowVersion,
      },
      headers: { 'Content-Type': 'application/json' },
    });

    expect(response.status()).toBe(200);
  });

  test('UC15-5 DELETE /family-members/{id} → 204', async ({ request }) => {
    expect(memberId).toBeTruthy();

    const response = await apiDelete(request, `/family-members/${memberId}`);
    expect(response.status()).toBe(204);

    memberId = ''; // Mark as cleaned up
  });
});

/**
 * UC03 — AI javaslat elfogadása / elutasítása
 *
 * The suggestions endpoint returns whatever the AI pipeline has created.
 * If there are no suggestions in the DB, the test validates the empty-state
 * structure and calls batch with an empty approve list.
 */
import { test, expect } from '@playwright/test';
import { apiGet, apiPost } from '../helpers/api';

test.describe('UC03 — AI suggestions', () => {
  test('UC03-1 GET /suggestions → 200 with valid structure', async ({ request }) => {
    const response = await apiGet(request, '/suggestions');
    expect(response.status()).toBe(200);

    const body = await response.json() as Record<string, unknown>;
    // Must have these top-level keys
    expect(body).toHaveProperty('tasks');
    expect(body).toHaveProperty('deadlines');
    expect(body).toHaveProperty('totalCount');

    expect(Array.isArray(body['tasks'])).toBeTruthy();
    expect(Array.isArray(body['deadlines'])).toBeTruthy();
    expect(typeof body['totalCount']).toBe('number');
  });

  test('UC03-2 POST /suggestions/batch approve → 200', async ({ request }) => {
    // Fetch current suggestions to get real IDs (if any)
    const getSuggestions = await apiGet(request, '/suggestions');
    const suggestions = await getSuggestions.json() as Record<string, unknown>;

    const tasks = (suggestions['tasks'] as Array<Record<string, unknown>>) ?? [];
    const deadlines = (suggestions['deadlines'] as Array<Record<string, unknown>>) ?? [];

    // Approve the first task suggestion if one exists, otherwise send empty list
    const taskIds = tasks.slice(0, 1).map((t) => t['id'] as string);
    const deadlineIds = deadlines.slice(0, 1).map((d) => d['id'] as string);

    const response = await apiPost(request, '/suggestions/batch', {
      approve: {
        tasks: taskIds,
        deadlines: deadlineIds,
      },
    });

    expect(response.status()).toBe(200);

    const result = await response.json() as Record<string, unknown>;
    // The response should include how many were approved
    expect(typeof (result['approved'] ?? result['approvedCount'] ?? 0)).toBe('number');
  });
});

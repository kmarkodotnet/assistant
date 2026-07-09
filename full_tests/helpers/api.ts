/**
 * Thin wrappers around Playwright APIRequestContext.
 * All paths are relative to the configured baseURL (http://localhost).
 */
import type { APIRequestContext } from '@playwright/test';

const API_BASE = '/api/v1';

export function apiGet(request: APIRequestContext, path: string) {
  return request.get(`${API_BASE}${path}`);
}

export function apiPost(request: APIRequestContext, path: string, body?: unknown) {
  return request.post(`${API_BASE}${path}`, {
    data: body,
    headers: { 'Content-Type': 'application/json' },
  });
}

export function apiPatch(request: APIRequestContext, path: string, body?: unknown) {
  return request.patch(`${API_BASE}${path}`, {
    data: body,
    headers: { 'Content-Type': 'application/json' },
  });
}

export function apiDelete(request: APIRequestContext, path: string) {
  return request.delete(`${API_BASE}${path}`);
}

/**
 * Polls GET /api/v1/documents/{id} until processingStatus is 'Done' or 'Failed'.
 * Resolves with the final document JSON.
 * Throws if the timeout expires before a terminal status is reached.
 */
export async function waitForDocumentProcessed(
  request: APIRequestContext,
  docId: string,
  timeoutMs = 90_000
): Promise<Record<string, unknown>> {
  const intervalMs = 3_000;
  const deadline = Date.now() + timeoutMs;

  while (Date.now() < deadline) {
    const resp = await apiGet(request, `/documents/${docId}`);
    if (!resp.ok()) {
      throw new Error(`GET /documents/${docId} returned ${resp.status()}`);
    }
    const doc = await resp.json() as Record<string, unknown>;
    const status = doc['processingStatus'] as string | undefined;

    if (status === 'Done' || status === 'Failed') {
      return doc;
    }

    // Wait before next poll
    await new Promise<void>(resolve => setTimeout(resolve, intervalMs));
  }

  throw new Error(
    `Document ${docId} did not reach terminal processingStatus within ${timeoutMs}ms`
  );
}

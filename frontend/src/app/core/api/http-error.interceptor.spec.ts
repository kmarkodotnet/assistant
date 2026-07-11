import { describe, it, expect } from 'vitest';
import { isSessionAuth401 } from './http-error.interceptor';

// BUG-1: a tool-calls/confirm|reject 401-je a proposal-token állapotára
// vonatkozik (api-design.md §16.3.1), NEM a session lejártára — a globális
// interceptor ezekre nem navigálhat /login-ra.
describe('isSessionAuth401', () => {
  it('true for a regular API 401 (session expired)', () => {
    expect(isSessionAuth401({ url: '/api/v1/documents/123' })).toBe(true);
  });

  it('true for /api/v1/auth/me 401', () => {
    expect(isSessionAuth401({ url: '/api/v1/auth/me' })).toBe(true);
  });

  it('false for /api/v1/tool-calls/confirm 401 (expired proposal token)', () => {
    expect(isSessionAuth401({ url: '/api/v1/tool-calls/confirm' })).toBe(false);
  });

  it('false for /api/v1/tool-calls/reject 401', () => {
    expect(isSessionAuth401({ url: '/api/v1/tool-calls/reject' })).toBe(false);
  });

  it('false regardless of host/query-string prefix, as long as the path segment is present', () => {
    expect(isSessionAuth401({ url: 'https://api.example.com/api/v1/tool-calls/confirm?x=1' })).toBe(false);
  });
});

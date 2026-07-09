/**
 * Reusable test data factory helpers.
 * These call real API endpoints — the request context must carry a valid auth cookie
 * (provided by the storageState set up in global-setup.ts).
 */
import type { APIRequestContext } from '@playwright/test';
import { expect } from '@playwright/test';
import { apiPost } from './api';
import path from 'path';
import fs from 'fs';

const SAMPLE_TXT = path.join(__dirname, '..', 'fixtures', 'sample.txt');

/**
 * Uploads the sample.txt fixture as a multipart document.
 * Returns the new document's id.
 */
export async function uploadTextDoc(
  request: APIRequestContext,
  text?: string
): Promise<string> {
  let filePath = SAMPLE_TXT;

  // If custom text is supplied, write a temp file
  let tempFile: string | undefined;
  if (text !== undefined) {
    tempFile = path.join(
      __dirname,
      '..',
      'fixtures',
      `tmp-${Date.now()}.txt`
    );
    fs.writeFileSync(tempFile, text, 'utf-8');
    filePath = tempFile;
  }

  try {
    const response = await request.post('/api/v1/documents', {
      multipart: {
        file: {
          name: path.basename(filePath),
          mimeType: 'text/plain',
          buffer: fs.readFileSync(filePath),
        },
      },
    });

    expect(response.status()).toBe(201);
    const doc = await response.json() as Record<string, unknown>;
    return doc['id'] as string;
  } finally {
    if (tempFile && fs.existsSync(tempFile)) {
      fs.unlinkSync(tempFile);
    }
  }
}

/**
 * Creates a task via API. Returns the task object (with id).
 */
export async function createTask(
  request: APIRequestContext,
  title: string
): Promise<Record<string, unknown>> {
  const response = await apiPost(request, '/tasks', {
    title,
    priority: 'Normal',
  });
  expect(response.status()).toBe(201);
  return response.json() as Promise<Record<string, unknown>>;
}

/**
 * Creates a deadline via API. Returns the deadline object (with id).
 * dueDate should be an ISO 8601 UTC string, e.g. "2027-01-15T12:00:00Z"
 */
export async function createDeadline(
  request: APIRequestContext,
  title: string,
  dueDate: string
): Promise<Record<string, unknown>> {
  const response = await apiPost(request, '/deadlines', {
    title,
    dueDateUtc: dueDate,
    category: 'Other',
  });
  expect(response.status()).toBe(201);
  return response.json() as Promise<Record<string, unknown>>;
}

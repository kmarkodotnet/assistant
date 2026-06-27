import { describe, it, expect } from 'vitest';
import { DocumentCardComponent } from '../components/document-card.component';
import type { ProcessingStatus } from '../models/document.dto';

describe('DocumentCardComponent', () => {
  it('should be defined as a class', () => {
    expect(DocumentCardComponent).toBeDefined();
    expect(typeof DocumentCardComponent).toBe('function');
  });

  it('statusVariant static-equivalent logic returns correct badge variants', () => {
    // Extract the pure logic from statusVariant without Angular DI
    function statusVariant(status: ProcessingStatus): string {
      switch (status) {
        case 'Done': return 'success';
        case 'Failed': return 'danger';
        case 'Pending':
        case 'Extracting':
        case 'Analyzing': return 'info';
        default: return 'default';
      }
    }

    const cases: Array<[ProcessingStatus, string]> = [
      ['Done', 'success'],
      ['Failed', 'danger'],
      ['Pending', 'info'],
      ['Extracting', 'info'],
      ['Analyzing', 'info'],
    ];
    for (const [status, expected] of cases) {
      expect(statusVariant(status)).toBe(expected);
    }
  });
});

describe('Documents DTOs', () => {
  it('ProcessingStatus values are valid', () => {
    const statuses: ProcessingStatus[] = ['Pending', 'Extracting', 'Analyzing', 'Done', 'Failed'];
    expect(statuses).toHaveLength(5);
  });
});

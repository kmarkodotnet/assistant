import { describe, it, expect } from 'vitest';
import { HuDatePipe } from './hu-date.pipe';

describe('HuDatePipe', () => {
  const pipe = new HuDatePipe();

  it('should return empty string for null', () => {
    expect(pipe.transform(null)).toBe('');
  });

  it('should format date in Hungarian', () => {
    const result = pipe.transform('2026-06-27');
    expect(result).toContain('2026');
    expect(result).toContain('június');
  });
});

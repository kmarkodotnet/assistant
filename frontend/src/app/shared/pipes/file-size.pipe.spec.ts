import { describe, it, expect } from 'vitest';
import { FileSizePipe } from './file-size.pipe';

describe('FileSizePipe', () => {
  const pipe = new FileSizePipe();

  it('should return empty for null', () => {
    expect(pipe.transform(null)).toBe('');
  });

  it('should format bytes', () => {
    expect(pipe.transform(512)).toBe('512 B');
  });

  it('should format kilobytes', () => {
    expect(pipe.transform(1536)).toBe('1.5 KB');
  });

  it('should format megabytes', () => {
    expect(pipe.transform(2_097_152)).toBe('2.0 MB');
  });
});

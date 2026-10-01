import { describe, expect, it } from 'vitest';
import { resolveUploadEntityId } from './useFieldInput';

describe('resolveUploadEntityId', () => {
  it('keeps a positive record id for edit uploads', () => {
    expect(resolveUploadEntityId(12)).toBe(12);
    expect(resolveUploadEntityId('8')).toBe(8);
  });

  it('keeps 0 so a new record uploads as a temporary attachment', () => {
    expect(resolveUploadEntityId(0)).toBe(0);
    expect(resolveUploadEntityId('0')).toBe(0);
  });

  it('omits id when the form has no primary key', () => {
    expect(resolveUploadEntityId(undefined)).toBeUndefined();
    expect(resolveUploadEntityId('')).toBeUndefined();
    expect(resolveUploadEntityId('abc')).toBeUndefined();
  });
});

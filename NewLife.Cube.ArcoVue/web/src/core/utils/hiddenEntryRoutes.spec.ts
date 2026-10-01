import { describe, expect, it } from 'vitest';
import { HIDDEN_ENTRY_PATHS, isHiddenEntryPath } from './hiddenEntryRoutes';

describe('isHiddenEntryPath', () => {
  it('accepts the 12 whitelist paths ignoring case and slashes', () => {
    expect(HIDDEN_ENTRY_PATHS).toHaveLength(12);
    for (const p of HIDDEN_ENTRY_PATHS) {
      expect(isHiddenEntryPath(p)).toBe(true);
      expect(isHiddenEntryPath(`/${p}/`)).toBe(true);
      expect(isHiddenEntryPath(p.toLowerCase())).toBe(true);
    }
  });

  it('rejects nearby, shorter, and empty paths', () => {
    expect(isHiddenEntryPath('/Admin/User')).toBe(false);
    expect(isHiddenEntryPath('/Admin/UserOnline/Kick')).toBe(false);
    expect(isHiddenEntryPath('')).toBe(false);
    expect(isHiddenEntryPath('/')).toBe(false);
  });
});

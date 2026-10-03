import { describe, expect, it } from 'vitest';
import { canConfirmCloseAccount } from './closeAccount';

describe('canConfirmCloseAccount', () => {
  it('requires exact name match', () => {
    expect(canConfirmCloseAccount('admin', 'admin')).toBe(true);
    expect(canConfirmCloseAccount('Admin', 'admin')).toBe(false);
    expect(canConfirmCloseAccount('admin ', 'admin')).toBe(false);
    expect(canConfirmCloseAccount('admin', '')).toBe(false);
    expect(canConfirmCloseAccount('admin', null)).toBe(false);
  });
});

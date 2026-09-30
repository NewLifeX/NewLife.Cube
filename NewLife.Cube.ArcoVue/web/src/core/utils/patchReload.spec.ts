import { describe, expect, it } from 'vitest';
import { shouldReloadAfterPatch } from './patchReload';

describe('shouldReloadAfterPatch', () => {
  const sorts = [{ field: 'Name', desc: false }];
  const filter = { logic: 'all' as const, conditions: [{ field: 'Status', op: 'eq' as const, value: 1 }] };

  it('空字段名保守刷新', () => {
    expect(shouldReloadAfterPatch('', sorts, filter)).toBe(true);
    expect(shouldReloadAfterPatch('  ', sorts, filter)).toBe(true);
  });

  it('排序命中则刷新，大小写不敏感', () => {
    expect(shouldReloadAfterPatch('name', sorts, filter)).toBe(true);
  });

  it('筛选命中则刷新', () => {
    expect(shouldReloadAfterPatch('status', [], filter)).toBe(true);
  });

  it('都不命中则不刷新', () => {
    expect(shouldReloadAfterPatch('Enable', sorts, filter)).toBe(false);
  });
});

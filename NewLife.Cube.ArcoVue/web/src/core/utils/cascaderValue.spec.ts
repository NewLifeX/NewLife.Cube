import { describe, expect, it } from 'vitest';
import { leafFromCascaderChange } from './cascaderValue';

describe('leafFromCascaderChange', () => {
  it('数组取末段叶子（path-mode 选中路径）', () => {
    expect(leafFromCascaderChange([11, 21, 31])).toBe(31);
    expect(leafFromCascaderChange(['p', 'c', 'd'])).toBe('d');
  });

  it('空值/空数组归一为 undefined（清空）', () => {
    expect(leafFromCascaderChange(null)).toBeUndefined();
    expect(leafFromCascaderChange(undefined)).toBeUndefined();
    expect(leafFromCascaderChange('')).toBeUndefined();
    expect(leafFromCascaderChange([])).toBeUndefined();
  });

  it('标量当叶子（防御：path-mode 误关）', () => {
    expect(leafFromCascaderChange(31)).toBe(31);
    expect(leafFromCascaderChange('31')).toBe('31');
  });
});

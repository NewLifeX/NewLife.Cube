import { describe, expect, it } from 'vitest';
import {
  leafFromCascaderChange,
  isAreaLeaf,
  isEmptyAreaId,
  formatAreaPathLabel,
  pathFromCascaderOption,
} from './cascaderValue';

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

  it('AreaId=0 / "0" 视为未选', () => {
    expect(isEmptyAreaId(0)).toBe(true);
    expect(isEmptyAreaId('0')).toBe(true);
    expect(isEmptyAreaId(null)).toBe(true);
    expect(isEmptyAreaId(110000)).toBe(false);
    expect(leafFromCascaderChange(0)).toBeUndefined();
    expect(leafFromCascaderChange('0')).toBeUndefined();
  });
});

describe('formatAreaPathLabel', () => {
  const nameOf = (id: string | number) =>
    ({ 110000: '北京', 110100: '市辖区', 110102: '西城' })[String(id)];

  it('路径统一为名称，缺名时回落编码', () => {
    expect(formatAreaPathLabel([110000, 110100, 110102], nameOf)).toBe('北京 / 市辖区 / 西城');
    expect(formatAreaPathLabel([110000, 999], nameOf)).toBe('北京 / 999');
    expect(formatAreaPathLabel(110000, nameOf)).toBe('北京');
    expect(formatAreaPathLabel(0, nameOf)).toBe('');
  });
});

describe('isAreaLeaf / pathFromCascaderOption', () => {
  it('Level≥4 或 9 位编码为叶子', () => {
    expect(isAreaLeaf(110101001, 4)).toBe(true);
    expect(isAreaLeaf('110101001')).toBe(true);
    expect(isAreaLeaf(110101, 3)).toBe(false);
    expect(isAreaLeaf(110000, 1)).toBe(false);
  });

  it('option 槽双击取 pathValue', () => {
    expect(pathFromCascaderOption({ pathValue: [11, 21, 31], value: 31 })).toEqual([11, 21, 31]);
    expect(pathFromCascaderOption({ path: [{ value: 11 }, { value: 21 }], value: 21 })).toEqual([
      11, 21,
    ]);
    expect(pathFromCascaderOption({ value: 110000 })).toEqual([110000]);
    expect(pathFromCascaderOption(null)).toEqual([]);
  });
});

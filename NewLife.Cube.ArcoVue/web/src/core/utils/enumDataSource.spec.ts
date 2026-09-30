import { describe, expect, it } from 'vitest';
import { dataSourceToOptions } from './enumDataSource';

describe('dataSourceToOptions', () => {
  it('枚举同时有数字键与名称键时只保留数字键', () => {
    const opts = dataSourceToOptions({
      '0': '全部',
      全部: '全部',
      '1': '本部门及下级',
      本部门及下级: '本部门及下级',
    });
    expect(opts).toEqual([
      { value: '0', label: '全部' },
      { value: '1', label: '本部门及下级' },
    ]);
  });

  it('纯数字键（外键 ID→名称）原样保留', () => {
    const opts = dataSourceToOptions({ '12': '研发', '15': '市场' });
    expect(opts).toEqual([
      { value: '12', label: '研发' },
      { value: '15', label: '市场' },
    ]);
  });

  it('空对象返回空数组', () => {
    expect(dataSourceToOptions({})).toEqual([]);
    expect(dataSourceToOptions(undefined)).toEqual([]);
  });
});

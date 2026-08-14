import { describe, expect, it } from 'vitest';
import { collectCascaderIds, mergeAreaLabel } from './areaLabels';
import type { FieldMeta } from '../types/field';

function base(over: Partial<FieldMeta> = {}): FieldMeta {
  return { name: 'AreaId', typeName: 'Int32', itemType: 'area4', ...over };
}

describe('areaLabels', () => {
  it('mergeAreaLabel 写入缓存，空值忽略', () => {
    const cache: Record<string, string> = {};
    mergeAreaLabel(cache, 110101, '东城区');
    mergeAreaLabel(cache, null, '空');
    mergeAreaLabel(cache, 2, '');
    expect(cache).toEqual({ '110101': '东城区' });
  });

  it('collectCascaderIds 收集去重叶子 ID', () => {
    const fields = [
      base({ name: 'AreaId' }),
      base({ name: 'HomeId', itemType: 'cascader' }),
      base({ name: 'Name', typeName: 'String', itemType: undefined }),
    ];
    const rows = [
      { AreaId: 110101, HomeId: '110105', Name: 'a' },
      { AreaId: '110101', HomeId: 110106, Name: 'b' },
      { AreaId: '', HomeId: null, Name: 'c' },
    ];
    expect(collectCascaderIds(fields, rows)).toEqual(['110101', '110105', '110106']);
  });
});

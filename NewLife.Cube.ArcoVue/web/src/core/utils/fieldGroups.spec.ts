import { describe, expect, it } from 'vitest';
import {
  estimateDetailLabelWidth,
  groupFieldsByCategory,
  resolveCategoryTitle,
} from './fieldGroups';
import type { FieldMeta } from '@/core/types/field';

function f(name: string, category?: string, displayName?: string): FieldMeta {
  return { name, typeName: 'String', category, displayName };
}

describe('groupFieldsByCategory', () => {
  it('returns single untitled group when no category', () => {
    const g = groupFieldsByCategory([f('A'), f('B')]);
    expect(g).toHaveLength(1);
    expect(g[0].title).toBe('');
    expect(g[0].fields.map((x) => x.name)).toEqual(['A', 'B']);
  });

  it('renames 扩展 to 扩展属性 and empty to 默认属性', () => {
    const g = groupFieldsByCategory([
      f('Name', '基本'),
      f('Code', '基本'),
      f('Remark', '扩展'),
      f('Enable'),
    ]);
    expect(g.map((x) => x.title)).toEqual(['基本', '扩展属性', '默认属性']);
    expect(resolveCategoryTitle('扩展')).toBe('扩展属性');
    expect(resolveCategoryTitle('')).toBe('默认属性');
  });

  it('estimates unified label width from longest label', () => {
    const w = estimateDetailLabelWidth([
      f('a', undefined, '编号'),
      f('b', undefined, '值集编码'),
    ]);
    expect(w).toBeGreaterThanOrEqual(estimateDetailLabelWidth([f('a', undefined, '编号')]));
  });
});

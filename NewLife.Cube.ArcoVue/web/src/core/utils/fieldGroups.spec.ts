import { describe, expect, it } from 'vitest';
import {
  applyFormLayout,
  estimateDetailLabelWidth,
  groupFieldsByCategory,
  normalizeFormLayout,
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

describe('normalizeFormLayout (OSC-0013)', () => {
  it('drops unknown/duplicate field names and unknown categories', () => {
    const fields = [f('Name', '基本'), f('Code', '基本'), f('Remark', '扩展')];
    const norm = normalizeFormLayout(
      {
        order: ['Code', 'Code', 'Ghost', 'Name'],
        hidden: ['Remark', 'Ghost', 'Remark'],
        collapsedCategories: ['扩展', '不存在', '扩展'],
      },
      fields,
    );
    expect(norm.order).toEqual(['Code', 'Name']);
    expect(norm.hidden).toEqual(['Remark']);
    expect(norm.collapsedCategories).toEqual(['扩展']);
  });

  it('returns empty layout for null/invalid', () => {
    expect(normalizeFormLayout(null, [f('A')])).toEqual({
      order: [],
      hidden: [],
      collapsedCategories: [],
    });
  });
});

describe('applyFormLayout (OSC-0013)', () => {
  it('filters hidden, orders by layout, appends unknown at end, filters empty groups', () => {
    const groups = groupFieldsByCategory([
      f('A', '基本'),
      f('B', '基本'),
      f('C', '基本'),
      f('D', '扩展'),
    ]);
    const r = applyFormLayout(groups, {
      order: ['C', 'A'],
      hidden: ['B'],
      collapsedCategories: [],
    });
    expect(r.groups[0].fields.map((x) => x.name)).toEqual(['C', 'A']);
    expect(r.groups.map((g) => g.category)).toEqual(['基本', '扩展']);
  });

  it('collapsed returns categories present in groups only', () => {
    const groups = groupFieldsByCategory([f('A', '基本')]);
    const r = applyFormLayout(groups, {
      order: [],
      hidden: [],
      collapsedCategories: ['基本', '幽灵'],
    });
    expect(r.collapsed).toEqual(['基本']);
  });

  it('returns metadata order when layout is null', () => {
    const groups = groupFieldsByCategory([f('A', '基本'), f('B', '基本')]);
    const r = applyFormLayout(groups, null);
    expect(r.groups[0].fields.map((x) => x.name)).toEqual(['A', 'B']);
    expect(r.collapsed).toEqual([]);
  });
});

import { describe, expect, it } from 'vitest';
import { DEFAULT_CATEGORY, groupFieldsByCategory, mergeObjectModel } from './objectForm';
import type { FieldMeta } from '../types/field';

function base(name: string, over: Partial<FieldMeta> = {}): FieldMeta {
  return { name, typeName: 'String', ...over };
}

describe('objectForm', () => {
  it('空 Category 归入「基本」，组顺序=字段原序', () => {
    const fields = [
      base('Debug', { category: '' }),
      base('Name', { category: '基础' }),
      base('Enable', { category: '基础' }),
      base('Theme', { category: '' }),
      base('Port', { category: '网络' }),
    ];
    const groups = groupFieldsByCategory(fields);
    expect(groups.map((g) => g.category)).toEqual([DEFAULT_CATEGORY, '基础', '网络']);
    expect(groups[0].fields.map((f) => f.name)).toEqual(['Debug', 'Theme']);
    expect(groups[1].fields.map((f) => f.name)).toEqual(['Name', 'Enable']);
  });

  it('mergeObjectModel 只覆盖字段名键，保留未建模嵌套属性', () => {
    const original = { Debug: false, Name: 'x', Nested: { a: 1 }, Keep: 'k' };
    const form = { Debug: true, Name: 'y' };
    const out = mergeObjectModel(original, form);
    expect(out).toEqual({ Debug: true, Name: 'y', Nested: { a: 1 }, Keep: 'k' });
    // 原对象不被就地修改
    expect(original.Debug).toBe(false);
  });

  it('mergeObjectModel 大小写容错：表单 PascalCase 覆盖原对象 camelCase', () => {
    const original = { name: 'old', displayName: '旧', keep: 1 };
    const form = { Name: 'new', DisplayName: '新' };
    const out = mergeObjectModel(original, form);
    expect(out).toEqual({ name: 'new', displayName: '新', keep: 1 });
    expect(out).not.toHaveProperty('Name');
    expect(out).not.toHaveProperty('DisplayName');
  });
});

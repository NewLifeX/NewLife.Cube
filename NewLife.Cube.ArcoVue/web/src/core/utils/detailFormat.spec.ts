import { describe, expect, it } from 'vitest';
import {
  detailText,
  detailLabels,
  detailUrl,
  detailImage,
  detailFile,
  jsonPreview,
  isMultipleValueField,
} from './detailFormat';
import type { FieldMeta } from '../types/field';

const f = (partial: Partial<FieldMeta> & Pick<FieldMeta, 'name' | 'typeName'>): FieldMeta => ({
  ...partial,
  name: partial.name,
  typeName: partial.typeName,
});

describe('detailFormat', () => {
  it('translates dataSource labels and boolean', () => {
    const field = f({ name: 'Sex', typeName: 'Int32', dataSource: { '1': '男', '0': '女' } });
    expect(detailText(field, '1')).toBe('男');
    expect(detailText(field, 1)).toBe('男');
    expect(detailText(field, '9')).toBe('9');
    expect(detailText(field, null)).toBe('-');
    expect(detailText(f({ name: 'Enable', typeName: 'Boolean' }), true)).toBe('是');
    expect(detailText(f({ name: 'Enable', typeName: 'Boolean' }), '0')).toBe('否');
  });

  it('splits multi-select labels', () => {
    const field = f({
      name: 'Tags',
      typeName: 'String',
      multiple: true,
      itemType: 'MultipleSelect',
      dataSource: { a: '标签A', b: '标签B' },
    });
    expect(isMultipleValueField(field)).toBe(true);
    expect(detailLabels(field, 'a,b')).toEqual(['标签A', '标签B']);
    expect(detailLabels(field, ['a', 'x'])).toEqual(['标签A', 'x']);
    expect(detailText(field, 'a,b')).toBe('标签A、标签B');
  });

  it('summarizes JSON preview', () => {
    expect(jsonPreview({ a: 1 })).toBe('{"a":1}');
    expect(jsonPreview('short')).toBe('short');
    expect(jsonPreview('x'.repeat(300)).length).toBe(201);
    const field = f({ name: 'Ext', typeName: 'String', itemType: 'json' });
    expect(detailText(field, { a: 1 })).toBe('{"a":1}');
  });

  it('url / image / file safe links', () => {
    const url = f({ name: 'Site', typeName: 'String', itemType: 'url' });
    expect(detailUrl(url, 'https://newlifex.com')?.safe).toBe(true);
    expect(detailUrl(url, 'javascript:alert(1)')?.safe).toBe(false);
    expect(detailUrl(url, '') ).toBeNull();
    expect(detailUrl(f({ name: 'Name', typeName: 'String' }), 'http://x')).toBeNull();

    const img = f({ name: 'Cover', typeName: 'String', itemType: 'image' });
    expect(detailImage(img, 'https://a.com/b.png')?.safe).toBe(true);
    expect(detailImage(img, '/files/b.png')?.safe).toBe(false);

    const file = f({ name: 'Doc', typeName: 'String', itemType: 'file' });
    expect(detailFile(file, 'https://a.com/doc.pdf')?.text).toBe('doc.pdf');
    expect(detailFile(file, '/files/a.pdf')?.safe).toBe(false);
  });

  it('cascader leaf reads areaLabelCache; falls back to raw ID (OSC-2608139feb)', () => {
    const field = f({ name: 'AreaId', typeName: 'Int32', itemType: 'area4' });
    expect(detailText(field, 110101, { areaLabelCache: { '110101': '东城区' } })).toBe('东城区');
    expect(detailText(field, 999, { areaLabelCache: {} })).toBe('999');
  });

  it('LIST LOV without dataSource reads labelCache (OSC-2608139feb)', () => {
    const field = f({ name: 'DeptId', typeName: 'Int32', lovCode: 'List.Dept' });
    expect(detailText(field, 3, { labelCache: { 'List.Dept': { '3': '研发部' } } })).toBe('研发部');
    expect(detailText(field, 4, { labelCache: {} })).toBe('4');
  });
});

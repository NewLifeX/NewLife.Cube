import { describe, expect, it } from 'vitest';
import {
  resolveControl,
  resolveListControl,
  resolveSearchControl,
  serializeSubmitModel,
} from './fieldControl';
import type { FieldMeta } from '../types/field';

const base = (partial: Partial<FieldMeta> & Pick<FieldMeta, 'name' | 'typeName'>): FieldMeta => ({
  ...partial,
  name: partial.name,
  typeName: partial.typeName,
});

describe('fieldControl', () => {
  it('maps boolean / datetime / textarea / lovCode', () => {
    expect(resolveControl(base({ name: 'Enable', typeName: 'Boolean' }))).toBe('switch');
    expect(resolveControl(base({ name: 'CreateTime', typeName: 'DateTime' }))).toBe('datePicker');
    expect(resolveControl(base({ name: 'Remark', typeName: 'String', length: 500 }))).toBe('textarea');
    expect(resolveControl(base({ name: 'Kind', typeName: 'Int32', lovCode: 'Enum.Kind' }))).toBe('lov');
  });

  it('maps dataSource to select', () => {
    expect(
      resolveControl(
        base({ name: 'Sex', typeName: 'Int32', dataSource: { '1': '男', '0': '女' } }),
      ),
    ).toBe('select');
  });

  it('search / list controls', () => {
    expect(resolveSearchControl(base({ name: 'Age', typeName: 'Int32' }))).toBe('numberRange');
    expect(resolveListControl(base({ name: 'Enable', typeName: 'Boolean' }))).toBe('boolean');
    expect(
      resolveListControl(base({ name: 'Kind', typeName: 'Int32', lovCode: 'Enum.Kind' })),
    ).toBe('lov');
  });

  it('serializeSubmitModel joins multi-select arrays', () => {
    const fields = [
      base({ name: 'Tags', typeName: 'String', multiple: true, itemType: 'multipleSelect' }),
    ];
    expect(serializeSubmitModel({ Tags: ['a', 'b'], Name: 'x' }, fields)).toEqual({
      Tags: 'a,b',
      Name: 'x',
    });
  });
});

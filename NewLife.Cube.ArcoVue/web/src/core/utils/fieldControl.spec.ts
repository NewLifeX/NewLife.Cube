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

  it('search prefers dataSource over lovCode for readable labels', () => {
    expect(
      resolveSearchControl(
        base({
          name: 'Sex',
          typeName: 'Int32',
          lovCode: 'Enum.SexKinds',
          dataSource: { '0': '未知', '1': '男', '2': '女' },
        }),
      ),
    ).toBe('select');
    expect(
      resolveSearchControl(base({ name: 'RoleID', typeName: 'Int32', lovCode: 'Role' })),
    ).toBe('lov');
  });

  it('Cube.Vue getComponentBaseField: unknown typeName → select (enum)', () => {
    expect(resolveSearchControl(base({ name: 'Sex', typeName: 'SexKinds' }))).toBe('select');
    expect(resolveControl(base({ name: 'Sex', typeName: 'SexKinds' }))).toBe('select');
    expect(resolveListControl(base({ name: 'Sex', typeName: 'SexKinds' }))).toBe('select');
    // 系统类型仍走原映射
    expect(resolveSearchControl(base({ name: 'Name', typeName: 'String' }))).toBe('text');
    expect(resolveSearchControl(base({ name: 'Enable', typeName: 'Boolean' }))).toBe('switch');
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

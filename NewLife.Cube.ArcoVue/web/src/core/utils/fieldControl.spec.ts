import { describe, expect, it } from 'vitest';
import {
  isAuditField,
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

  it('multi-select itemType is case-insensitive (OSC-0009)', () => {
    const fields = [
      base({ name: 'Tags', typeName: 'String', itemType: 'MultipleSelect' }),
      base({ name: 'Flags', typeName: 'String', itemType: 'MULTIPLESELECT' }),
    ];
    expect(serializeSubmitModel({ Tags: ['a', 'b'], Flags: ['x', 'y'] }, fields)).toEqual({
      Tags: 'a,b',
      Flags: 'x,y',
    });
  });

  it('Int64/UInt64 keep precision beyond safe integer (OSC-0009)', () => {
    const i64 = base({ name: 'BigId', typeName: 'Int64' });
    const u64 = base({ name: 'BigU', typeName: 'UInt64' });
    // 安全整数 → number（保证后端 System.Text.Json 绑定成功）
    expect(serializeSubmitModel({ BigId: '123456789012' }, [i64])).toEqual({
      BigId: 123456789012,
    });
    // 超安全整数（雪花 ID 19 位）→ 保留字符串，避免 Number 精度丢失
    const snowflake = '2300000000000000001';
    expect(serializeSubmitModel({ BigId: snowflake }, [i64])).toEqual({ BigId: snowflake });
    expect(serializeSubmitModel({ BigU: snowflake }, [u64])).toEqual({ BigU: snowflake });
    expect(serializeSubmitModel({ BigId: 42 }, [i64])).toEqual({ BigId: 42 });
  });

  it('control prefers dataSource over auto Enum lovCode (OSC-0009)', () => {
    const f = base({
      name: 'Kind',
      typeName: 'Int32',
      lovCode: 'Enum.Kind',
      dataSource: { '1': 'A', '2': 'B' },
    });
    expect(resolveControl(f)).toBe('select');
    expect(resolveListControl(f)).toBe('select');
    expect(resolveSearchControl(f)).toBe('select');
    // 无 dataSource 的 LIST/枚举仍走 LovSelect
    expect(
      resolveControl(base({ name: 'DeptId', typeName: 'Int32', lovCode: 'List.Dept' })),
    ).toBe('lov');
  });

  it('isAuditField detects create/update audit fields (case-insensitive)', () => {
    expect(isAuditField(base({ name: 'CreateUser', typeName: 'String' }))).toBe(true);
    expect(isAuditField(base({ name: 'CreateUserID', typeName: 'Int32' }))).toBe(true);
    expect(isAuditField(base({ name: 'CreateIP', typeName: 'String' }))).toBe(true);
    expect(isAuditField(base({ name: 'CreateTime', typeName: 'DateTime' }))).toBe(true);
    expect(isAuditField(base({ name: 'UpdateUser', typeName: 'String' }))).toBe(true);
    expect(isAuditField(base({ name: 'UpdateUserID', typeName: 'Int32' }))).toBe(true);
    expect(isAuditField(base({ name: 'UpdateIP', typeName: 'String' }))).toBe(true);
    expect(isAuditField(base({ name: 'UpdateTime', typeName: 'DateTime' }))).toBe(true);
    expect(isAuditField(base({ name: 'createTime', typeName: 'DateTime' }))).toBe(true);
    expect(isAuditField(base({ name: 'Name', typeName: 'String' }))).toBe(false);
    expect(isAuditField(base({ name: 'LastLogin', typeName: 'DateTime' }))).toBe(false);
    expect(isAuditField(base({ name: 'Remark', typeName: 'String' }))).toBe(false);
  });
});

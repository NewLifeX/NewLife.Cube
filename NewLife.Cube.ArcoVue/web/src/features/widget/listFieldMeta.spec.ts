import { describe, expect, it } from 'vitest';
import { fieldLabelOf, mergeFieldMetas, unwrapPayload } from './listFieldMeta';
import type { FieldMeta } from '@/core/types/field';

describe('unwrapPayload', () => {
  it('unwraps ApiResponse layers', () => {
    expect(unwrapPayload({ code: 0, data: { list: [1] } })).toEqual({ list: [1] });
    expect(unwrapPayload({ code: 0, Data: [{ Name: 'Type' }] })).toEqual([{ Name: 'Type' }]);
  });
});

describe('mergeFieldMetas', () => {
  it('prefers Chinese displayName and non-String typeName', () => {
    const page: FieldMeta[] = [
      { name: 'Type', displayName: 'Type', typeName: 'String' },
      { name: 'IsSystem', displayName: 'IsSystem', typeName: 'Boolean' },
    ];
    const auto: FieldMeta[] = [
      { name: 'Type', displayName: '类型', typeName: 'RoleTypes' },
      { name: 'IsSystem', displayName: '系统', typeName: 'Boolean' },
    ];
    const merged = mergeFieldMetas(page, auto);
    const type = merged.find((f) => f.name === 'Type')!;
    const sys = merged.find((f) => f.name === 'IsSystem')!;
    expect(type.displayName).toBe('类型');
    expect(type.typeName).toBe('RoleTypes');
    expect(sys.displayName).toBe('系统');
    expect(sys.typeName).toBe('Boolean');
  });

  it('keeps dataSource from either side', () => {
    const a: FieldMeta[] = [
      { name: 'Type', displayName: '类型', typeName: 'RoleTypes', dataSource: { '1': '系统', '2': '普通' } },
    ];
    const b: FieldMeta[] = [{ name: 'Type', displayName: 'Type', typeName: 'RoleTypes' }];
    expect(mergeFieldMetas(b, a)[0].dataSource).toEqual({ '1': '系统', '2': '普通' });
  });
});

describe('fieldLabelOf', () => {
  it('returns Chinese displayName when present', () => {
    const metas: FieldMeta[] = [{ name: 'IsSystem', displayName: '系统', typeName: 'Boolean' }];
    expect(fieldLabelOf(metas, 'issystem')).toBe('系统');
    expect(fieldLabelOf(metas, 'Missing')).toBe('Missing');
  });
});

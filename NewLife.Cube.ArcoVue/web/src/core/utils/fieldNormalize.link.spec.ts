import { describe, expect, it } from 'vitest';
import type { DataField } from '@cube/api-core';
import { toFieldMeta } from './fieldNormalize';

describe('toFieldMeta link metadata', () => {
  it('保留 dataAction 与 hasTypeName=true', () => {
    const meta = toFieldMeta({
      name: 'Name',
      typeName: 'String',
      url: '/Admin/User?id={Id}',
      dataAction: undefined,
    } as DataField);
    expect(meta.hasTypeName).toBe(true);
    expect(meta.url).toBe('/Admin/User?id={Id}');
    expect(meta.dataAction).toBeUndefined();
  });

  it('合成列无 TypeName → hasTypeName=false，仍回落 typeName=String', () => {
    const meta = toFieldMeta({
      name: 'Log',
      url: '/Admin/Log?id={Id}',
      DataAction: 'action',
    } as DataField & { DataAction?: string });
    expect(meta.hasTypeName).toBe(false);
    expect(meta.typeName).toBe('String');
    expect(meta.dataAction).toBe('action');
  });

  it('空白 dataAction 视为无', () => {
    const meta = toFieldMeta({
      name: 'X',
      typeName: 'Int32',
      url: '/x',
      dataAction: '  ',
    } as DataField);
    expect(meta.dataAction).toBeUndefined();
    expect(meta.hasTypeName).toBe(true);
  });
});

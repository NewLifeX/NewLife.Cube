import { describe, expect, it } from 'vitest';
import type { FieldMeta } from '../types/field';
import {
  OPS_LINK_INLINE_MAX,
  classifyListLink,
  isDataListField,
  partitionListFields,
  toOpsCustomLink,
} from './listLinkFields';

function field(partial: Partial<FieldMeta> & { name: string }): FieldMeta {
  return {
    typeName: 'String',
    hasTypeName: false,
    ...partial,
  };
}

describe('classifyListLink', () => {
  it('url 空 → none', () => {
    expect(classifyListLink(field({ name: 'A' }))).toBe('none');
    expect(classifyListLink(field({ name: 'A', url: '  ' }))).toBe('none');
  });

  it('dataAction 非空 → opsAction（不论 hasTypeName）', () => {
    expect(
      classifyListLink(
        field({ name: 'Execute', url: '/x?id={Id}', dataAction: 'action', hasTypeName: true }),
      ),
    ).toBe('opsAction');
    expect(
      classifyListLink(
        field({ name: 'Execute', url: '/x?id={Id}', dataAction: 'action', hasTypeName: false }),
      ),
    ).toBe('opsAction');
  });

  it('url + hasTypeName → cell', () => {
    expect(
      classifyListLink(field({ name: 'Name', url: '/d?id={Id}', hasTypeName: true })),
    ).toBe('cell');
  });

  it('url + !hasTypeName → opsNav', () => {
    expect(
      classifyListLink(field({ name: 'Log', url: '/Admin/Log?id={Id}', hasTypeName: false })),
    ).toBe('opsNav');
  });
});

describe('partitionListFields', () => {
  it('拆分数据列与操作链接', () => {
    const fields = [
      field({ name: 'ID', typeName: 'Int32', hasTypeName: true }),
      field({ name: 'Name', url: '/u/{Id}', hasTypeName: true }),
      field({ name: 'Log', url: '/log?id={Id}', hasTypeName: false }),
      field({ name: 'Execute', url: '/run?id={Id}', dataAction: 'action', hasTypeName: false }),
    ];
    const { dataFields, opsLinks } = partitionListFields(fields);
    expect(dataFields.map((f) => f.name)).toEqual(['ID', 'Name']);
    expect(opsLinks.map((l) => l.name)).toEqual(['Log', 'Execute']);
    expect(opsLinks[1].dataAction).toBe('action');
    expect(isDataListField(fields[1])).toBe(true);
    expect(isDataListField(fields[2])).toBe(false);
  });

  it('toOpsCustomLink 仅 ops 种类', () => {
    expect(toOpsCustomLink(field({ name: 'Name', url: '/x', hasTypeName: true }))).toBeNull();
    expect(toOpsCustomLink(field({ name: 'Log', url: '/x', hasTypeName: false }))?.label).toBe(
      'Log',
    );
  });

  it('OPS_LINK_INLINE_MAX 为 2', () => {
    expect(OPS_LINK_INLINE_MAX).toBe(2);
  });
});

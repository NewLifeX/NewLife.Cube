import { describe, expect, it } from 'vitest';
import type { FieldMeta } from '@/core/types/field';
import { groupMissingLov } from './lovHydrate';

function field(partial: Partial<FieldMeta> & Pick<FieldMeta, 'name'>): FieldMeta {
  return { typeName: 'Int32', ...partial };
}

describe('groupMissingLov', () => {
  it('同一值集合并缺值，已有标签和非 lov 控件不进组', () => {
    const groups = groupMissingLov(
      [
        field({ name: 'RoleId', lovCode: 'List.Role' }),
        field({ name: 'OwnerId', lovCode: 'List.Role' }),
        field({ name: 'BackupId', lovCode: 'List.Role', dataSource: { '3': '管理员' } }),
        field({ name: 'AreaId', lovCode: 'List.Area', itemType: 'area' }),
        field({ name: 'Status', lovCode: 'Enum.Status' }),
        field({ name: 'Enable', lovCode: 'List.Enable', typeName: 'Boolean' }),
      ],
      { RoleId: 3, OwnerId: 3, BackupId: 3, AreaId: 1, Status: 1, Enable: true },
    );
    expect(groups).toEqual([
      {
        code: 'List.Role',
        values: ['3'],
        fields: [
          expect.objectContaining({ name: 'RoleId' }),
          expect.objectContaining({ name: 'OwnerId' }),
        ],
      },
    ]);
  });
});

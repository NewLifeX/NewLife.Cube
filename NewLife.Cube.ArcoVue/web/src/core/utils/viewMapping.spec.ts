import { describe, expect, it } from 'vitest';
import type { FieldMeta } from '@/core/types/field';
import {
  bucketKanban,
  canCreateViewKind,
  normalizeMapping,
  resolveViewPageSize,
  seedMapping,
} from './viewMapping';

function f(partial: Partial<FieldMeta> & { name: string }): FieldMeta {
  return {
    typeName: 'String',
    ...partial,
  };
}

describe('resolveViewPageSize', () => {
  it('uses pager size for table/card/tree', () => {
    expect(resolveViewPageSize('table', 30)).toBe(30);
    expect(resolveViewPageSize('card', 15)).toBe(15);
    expect(resolveViewPageSize('tree')).toBe(20);
  });

  it('clamps large views to 200–500', () => {
    expect(resolveViewPageSize('kanban')).toBe(200);
    expect(resolveViewPageSize('calendar', 20, 300)).toBe(300);
    expect(resolveViewPageSize('gantt', 20, 900)).toBe(500);
  });
});

describe('canCreateViewKind', () => {
  const fields = [
    f({ name: 'Name', typeName: 'String' }),
    f({ name: 'Status', typeName: 'Enum', dataSource: { a: 'A', b: 'B' } }),
    f({ name: 'Start', typeName: 'DateTime' }),
    f({ name: 'End', typeName: 'DateTime' }),
    f({ name: 'ParentId', typeName: 'Int32' }),
  ];

  it('allows table always; tree when Parent present', () => {
    expect(canCreateViewKind('table', fields, 'Admin/User').ok).toBe(true);
    expect(canCreateViewKind('tree', fields, 'Admin/User').ok).toBe(true);
    expect(
      canCreateViewKind(
        'tree',
        [f({ name: 'Name', typeName: 'String' })],
        'Admin/User',
      ).ok,
    ).toBe(false);
  });

  it('gates kanban/calendar/gantt by candidates', () => {
    expect(canCreateViewKind('kanban', fields, 'x').ok).toBe(true);
    expect(canCreateViewKind('calendar', fields, 'x').ok).toBe(true);
    expect(canCreateViewKind('gantt', fields, 'x').ok).toBe(true);
    expect(
      canCreateViewKind('gantt', [f({ name: 'Start', typeName: 'DateTime' })], 'x').ok,
    ).toBe(false);
  });
});

describe('normalizeMapping / seedMapping', () => {
  const fields = [
    f({ name: 'Name', typeName: 'String' }),
    f({ name: 'Status', typeName: 'Boolean' }),
    f({ name: 'Start', typeName: 'DateTime' }),
    f({ name: 'End', typeName: 'DateTime' }),
    f({ name: 'Photo', typeName: 'String', itemType: 'image' }),
  ];

  it('seeds card/kanban/calendar/gantt', () => {
    expect(seedMapping('card', fields)?.kind).toBe('card');
    expect(seedMapping('kanban', fields)).toMatchObject({
      kind: 'kanban',
      groupField: 'Status',
      titleField: 'Name',
    });
    expect(seedMapping('calendar', fields)).toMatchObject({
      kind: 'calendar',
      startField: 'Start',
    });
    expect(seedMapping('gantt', fields)).toMatchObject({
      kind: 'gantt',
      startField: 'Start',
      endField: 'End',
    });
  });

  it('drops illegal field names', () => {
    const m = normalizeMapping(
      'kanban',
      { kind: 'kanban', groupField: 'Nope', titleField: 'Name' },
      fields,
    );
    expect(m).toMatchObject({ kind: 'kanban', groupField: 'Status', titleField: 'Name' });
  });

  it('table has no mapping', () => {
    expect(normalizeMapping('table', { kind: 'card', titleField: 'Name' }, fields)).toBeUndefined();
  });
});

describe('bucketKanban', () => {
  it('orders by dataSource and appends 未分组', () => {
    const buckets = bucketKanban(
      [
        { Status: 'b', Name: '2' },
        { Status: 'a', Name: '1' },
        { Status: '', Name: 'x' },
        { Name: 'y' },
      ],
      'Status',
      { a: '甲', b: '乙' },
    );
    expect(buckets.map((b) => b.key)).toEqual(['a', 'b', '__ungrouped__']);
    expect(buckets[0].label).toBe('甲');
    expect(buckets[2].rows).toHaveLength(2);
  });

  it('同标签别名键只创建一个看板列，并归并到数字键', () => {
    const buckets = bucketKanban(
      [
        { Status: '1', Name: '管理员' },
        { Status: 'System', Name: '超级管理员' },
        { Status: '2', Name: '普通用户' },
      ],
      'Status',
      { '1': '系统', System: '系统', '2': '普通' },
    );
    expect(buckets.map((b) => b.key)).toEqual(['1', '2']);
    expect(buckets.map((b) => b.label)).toEqual(['系统', '普通']);
    expect(buckets[0].rows.map((r) => r.Name)).toEqual(['管理员', '超级管理员']);
  });

  it('字段名大小写容错：后端 camelCase 数据行按 PascalCase groupField 分组', () => {
    // FieldMeta.name 为 PascalCase（DataField.Name），数据行为 FastJson camelCase
    const buckets = bucketKanban(
      [
        { status: 'b', name: '2' },
        { status: 'a', name: '1' },
        { status: '', name: 'x' },
        { name: 'y' },
      ],
      'Status',
      { a: '甲', b: '乙' },
    );
    expect(buckets.map((b) => b.key)).toEqual(['a', 'b', '__ungrouped__']);
    expect(buckets[0].rows.map((r) => r.name)).toEqual(['1']);
    expect(buckets[1].rows.map((r) => r.name)).toEqual(['2']);
    expect(buckets[2].rows).toHaveLength(2);
  });

  it('无 dataSource 时按数据值自然分桶', () => {
    const buckets = bucketKanban(
      [
        { status: 'high', name: '1' },
        { status: 'low', name: '2' },
        { status: 'high', name: '3' },
      ],
      'Status',
    );
    expect(buckets.map((b) => b.key)).toEqual(['high', 'low']);
    expect(buckets[0].rows).toHaveLength(2);
  });
});

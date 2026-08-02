import { describe, expect, it } from 'vitest';
import type { FieldMeta } from '@/core/types/field';
import {
  bucketKanban,
  canCreateViewKind,
  isTableLikeViewKind,
  normalizeCardBodyColumns,
  normalizeCardFieldOrientation,
  normalizeCardLayout,
  normalizeMapping,
  resolveBatchDeleteState,
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

  it('clamps large views to 200–1000', () => {
    expect(resolveViewPageSize('kanban')).toBe(200);
    expect(resolveViewPageSize('calendar', 20, 300)).toBe(300);
    expect(resolveViewPageSize('gantt', 20, 900)).toBe(900);
    expect(resolveViewPageSize('gantt', 20, 2000)).toBe(1000);
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

  it('card keeps valid layout and falls back to standard otherwise', () => {
    expect(
      normalizeMapping('card', { kind: 'card', titleField: 'Name', layout: 'large' }, fields),
    ).toMatchObject({
      kind: 'card',
      layout: 'large',
      bodyColumns: 2,
      fieldOrientation: 'vertical',
    });
    expect(
      normalizeMapping('card', { kind: 'card', titleField: 'Name', layout: 'row' }, fields),
    ).toMatchObject({ kind: 'card', layout: 'row' });
    expect(
      normalizeMapping('card', { kind: 'card', titleField: 'Name' }, fields),
    ).toMatchObject({ kind: 'card', layout: 'standard' });
    expect(
      normalizeMapping('card', { kind: 'card', titleField: 'Name', layout: 'big' }, fields),
    ).toMatchObject({ kind: 'card', layout: 'standard' });
    expect(
      normalizeMapping('card', { kind: 'card', titleField: 'Name', layout: null }, fields),
    ).toMatchObject({ kind: 'card', layout: 'standard' });
    expect(
      normalizeMapping(
        'card',
        {
          kind: 'card',
          titleField: 'Name',
          layout: 'standard',
          bodyColumns: 3,
          fieldOrientation: 'horizontal',
        },
        fields,
      ),
    ).toMatchObject({ bodyColumns: 2, fieldOrientation: 'horizontal' });
    expect(
      normalizeMapping(
        'card',
        { kind: 'card', titleField: 'Name', layout: 'row', bodyColumns: 3 },
        fields,
      ),
    ).toMatchObject({ bodyColumns: 3 });
  });

  it('seeds card mapping with standard layout', () => {
    expect(seedMapping('card', fields)).toMatchObject({
      kind: 'card',
      titleField: 'Name',
      layout: 'standard',
      bodyColumns: 2,
      fieldOrientation: 'vertical',
    });
  });
});

describe('isTableLikeViewKind', () => {
  it('only table/tree are table-like', () => {
    expect(isTableLikeViewKind('table')).toBe(true);
    expect(isTableLikeViewKind('tree')).toBe(true);
    expect(isTableLikeViewKind('card')).toBe(false);
    expect(isTableLikeViewKind('kanban')).toBe(false);
    expect(isTableLikeViewKind('calendar')).toBe(false);
    expect(isTableLikeViewKind('gantt')).toBe(false);
  });
});

describe('normalizeCardLayout', () => {
  it('accepts only standard/large/row', () => {
    expect(normalizeCardLayout('standard')).toBe('standard');
    expect(normalizeCardLayout('large')).toBe('large');
    expect(normalizeCardLayout('row')).toBe('row');
    expect(normalizeCardLayout(undefined)).toBe('standard');
    expect(normalizeCardLayout(null)).toBe('standard');
    expect(normalizeCardLayout('')).toBe('standard');
    expect(normalizeCardLayout('big')).toBe('standard');
    expect(normalizeCardLayout(123)).toBe('standard');
    expect(normalizeCardLayout({})).toBe('standard');
  });
});

describe('normalizeCardBodyColumns', () => {
  it('falls back illegal values to 2 and clamps 3 on standard/large', () => {
    expect(normalizeCardBodyColumns(1, 'standard')).toBe(1);
    expect(normalizeCardBodyColumns(2, 'standard')).toBe(2);
    expect(normalizeCardBodyColumns(3, 'standard')).toBe(2);
    expect(normalizeCardBodyColumns(3, 'large')).toBe(2);
    expect(normalizeCardBodyColumns(3, 'row')).toBe(3);
    expect(normalizeCardBodyColumns(undefined, 'row')).toBe(2);
    expect(normalizeCardBodyColumns('3', 'row')).toBe(3);
  });
});

describe('normalizeCardFieldOrientation', () => {
  it('accepts horizontal otherwise vertical', () => {
    expect(normalizeCardFieldOrientation('horizontal')).toBe('horizontal');
    expect(normalizeCardFieldOrientation('vertical')).toBe('vertical');
    expect(normalizeCardFieldOrientation(undefined)).toBe('vertical');
    expect(normalizeCardFieldOrientation('side')).toBe('vertical');
  });
});

describe('resolveBatchDeleteState', () => {
  it('hidden outside table or without permission/allow', () => {
    for (const kind of ['tree', 'card', 'kanban', 'calendar', 'gantt'] as const) {
      expect(
        resolveBatchDeleteState({
          viewKind: kind,
          canDelete: true,
          allowDelete: true,
          selectedCount: 3,
        }),
      ).toEqual({ visible: false, disabled: true });
    }
    expect(
      resolveBatchDeleteState({
        viewKind: 'table',
        canDelete: false,
        allowDelete: true,
        selectedCount: 3,
      }),
    ).toEqual({ visible: false, disabled: true });
    expect(
      resolveBatchDeleteState({
        viewKind: 'table',
        canDelete: true,
        allowDelete: false,
        selectedCount: 3,
      }),
    ).toEqual({ visible: false, disabled: true });
  });

  it('visible but disabled without selection, enabled with selection', () => {
    expect(
      resolveBatchDeleteState({
        viewKind: 'table',
        canDelete: true,
        allowDelete: true,
        selectedCount: 0,
      }),
    ).toEqual({ visible: true, disabled: true });
    expect(
      resolveBatchDeleteState({
        viewKind: 'table',
        canDelete: true,
        allowDelete: true,
        selectedCount: 1,
      }),
    ).toEqual({ visible: true, disabled: false });
    expect(
      resolveBatchDeleteState({
        viewKind: 'table',
        canDelete: true,
        allowDelete: true,
        selectedCount: 5,
      }),
    ).toEqual({ visible: true, disabled: false });
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

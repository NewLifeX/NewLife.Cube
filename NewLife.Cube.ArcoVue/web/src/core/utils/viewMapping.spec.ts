import { describe, expect, it } from 'vitest';
import type { FieldMeta } from '@/core/types/field';
import type { GanttMapping } from './viewMapping';
import {
  bucketKanban,
  canCreateViewKind,
  defaultViewKindName,
  groupHeaderCell,
  groupRows,
  isGroupHeaderRow,
  isTableLikeViewKind,
  moveGroupField,
  nextGroupFieldNames,
  normalizeCardBodyColumns,
  normalizeCardFieldOrientation,
  normalizeCardLayout,
  normalizeDataSource,
  normalizeMapping,
  normalizePageSize,
  pushGroupField,
  removeGroupField,
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

describe('normalizePageSize', () => {
  it('accepts PAGE_SIZE_OPTIONS values only', () => {
    expect(normalizePageSize(20)).toBe(20);
    expect(normalizePageSize(100)).toBe(100);
    expect(normalizePageSize(1000)).toBe(1000);
  });

  it('normalizes invalid/negative/off-option/NaN to 0', () => {
    expect(normalizePageSize(0)).toBe(0);
    expect(normalizePageSize(-5)).toBe(0);
    expect(normalizePageSize(30)).toBe(0);
    expect(normalizePageSize(1001)).toBe(0);
    expect(normalizePageSize('50')).toBe(50);
    expect(normalizePageSize('abc')).toBe(0);
    expect(normalizePageSize(undefined)).toBe(0);
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
      titleField: 'Name',
      plannedStartField: 'Start',
      plannedEndField: 'End',
    });
    // 甘特新结构：实际/颜色/宽度缺省（OSC-0019）
    const ganttSeed = seedMapping('gantt', fields) as GanttMapping;
    expect(ganttSeed.actualStartField).toBeUndefined();
    expect(ganttSeed.actualEndField).toBeUndefined();
    expect(ganttSeed.barColor).toBeUndefined();
    expect(ganttSeed.tableWidth).toBeUndefined();
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

  it('gantt normalize: 旧数据 startField/endField 迁移为 planned，colorField 忽略（OSC-0019）', () => {
    const m = normalizeMapping(
      'gantt',
      {
        kind: 'gantt',
        titleField: 'Name',
        startField: 'Start',
        endField: 'End',
        colorField: 'Status',
      },
      fields,
    );
    expect(m).toMatchObject({
      kind: 'gantt',
      titleField: 'Name',
      plannedStartField: 'Start',
      plannedEndField: 'End',
    });
    // colorField 忽略（行为变更：按字段着色 → 固定色）
    expect((m as Record<string, unknown>)['colorField']).toBeUndefined();
    expect((m as Record<string, unknown>)['startField']).toBeUndefined();
    expect((m as Record<string, unknown>)['endField']).toBeUndefined();
  });

  it('gantt normalize: 新结构 round-trip（planned/actual/barColor/tableWidth）', () => {
    const m = normalizeMapping(
      'gantt',
      {
        kind: 'gantt',
        titleField: 'Name',
        plannedStartField: 'Start',
        plannedEndField: 'End',
        actualStartField: 'Start',
        actualEndField: 'End',
        barColor: '#FF6600',
        tableWidth: 520,
      },
      fields,
    );
    expect(m).toEqual({
      kind: 'gantt',
      titleField: 'Name',
      plannedStartField: 'Start',
      plannedEndField: 'End',
      actualStartField: 'Start',
      actualEndField: 'End',
      barColor: '#FF6600',
      tableWidth: 520,
    });
  });

  it('gantt normalize: 实际字段仅配一个视为未配置实际', () => {
    const m = normalizeMapping(
      'gantt',
      {
        kind: 'gantt',
        titleField: 'Name',
        plannedStartField: 'Start',
        plannedEndField: 'End',
        actualStartField: 'Start',
      },
      fields,
    );
    expect(m).toMatchObject({ plannedStartField: 'Start', plannedEndField: 'End' });
    expect((m as Record<string, unknown>)['actualStartField']).toBeUndefined();
    expect((m as Record<string, unknown>)['actualEndField']).toBeUndefined();
  });

  it('gantt normalize: barColor 非法 hex / tableWidth 越界非数均丢弃', () => {
    const m = normalizeMapping(
      'gantt',
      {
        kind: 'gantt',
        titleField: 'Name',
        plannedStartField: 'Start',
        plannedEndField: 'End',
        barColor: 'red',
        tableWidth: 9999,
      },
      fields,
    );
    expect((m as Record<string, unknown>)['barColor']).toBeUndefined();
    expect((m as Record<string, unknown>)['tableWidth']).toBe(640);

    const m2 = normalizeMapping(
      'gantt',
      {
        kind: 'gantt',
        titleField: 'Name',
        plannedStartField: 'Start',
        plannedEndField: 'End',
        barColor: '#12345',
        tableWidth: 'abc',
      },
      fields,
    );
    expect((m2 as Record<string, unknown>)['barColor']).toBeUndefined();
    expect((m2 as Record<string, unknown>)['tableWidth']).toBeUndefined();

    const m3 = normalizeMapping(
      'gantt',
      {
        kind: 'gantt',
        titleField: 'Name',
        plannedStartField: 'Start',
        plannedEndField: 'End',
        barColor: '#a1b2c3',
        tableWidth: 100,
      },
      fields,
    );
    expect((m3 as Record<string, unknown>)['barColor']).toBe('#a1b2c3');
    expect((m3 as Record<string, unknown>)['tableWidth']).toBe(280);
  });

  it('gantt normalize: 计划字段非法回落 seedMapping', () => {
    const m = normalizeMapping(
      'gantt',
      {
        kind: 'gantt',
        titleField: 'Name',
        plannedStartField: 'Nope',
        plannedEndField: 'Nope2',
      },
      fields,
    );
    expect(m).toMatchObject({
      kind: 'gantt',
      plannedStartField: 'Start',
      plannedEndField: 'End',
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

describe('normalizeDataSource', () => {
  it('数字键与名称键同标签时按 label 去重并优先数字键', () => {
    const { options } = normalizeDataSource({
      '0': '未知',
      '1': '男',
      '2': '女',
      未知: '未知',
      男: '男',
      女: '女',
    });
    expect(options).toEqual([
      { value: '0', label: '未知' },
      { value: '1', label: '男' },
      { value: '2', label: '女' },
    ]);
  });

  it('canonicalByKey 把名称键映射回数字键，供表单回显选中态', () => {
    const { canonicalByKey } = normalizeDataSource({
      '0': '未知',
      '1': '男',
      '2': '女',
      未知: '未知',
      男: '男',
      女: '女',
    });
    expect(canonicalByKey.get('男')).toBe('1');
    expect(canonicalByKey.get('女')).toBe('2');
    expect(canonicalByKey.get('1')).toBe('1');
  });

  it('纯名称键字典（无数字键）按原键去重保留', () => {
    const { options, canonicalByKey } = normalizeDataSource({ high: '高', low: '低' });
    expect(options).toEqual([
      { value: 'high', label: '高' },
      { value: 'low', label: '低' },
    ]);
    expect(canonicalByKey.get('high')).toBe('high');
  });
});

describe('groupRows (OSC-0015)', () => {
  const fields = [
    { name: 'Status', displayName: '状态', typeName: 'Int32', dataSource: { '1': '启用', '2': '停用' } },
    { name: 'Dept', displayName: '部门', typeName: 'String' },
  ];

  it('empty groupFields returns original records', () => {
    const rows = [{ name: 'a' }];
    expect(groupRows(rows, [], fields)).toBe(rows);
  });

  it('single-level grouping with dataSource label and count', () => {
    const rows = [
      { status: '1', name: 'a' },
      { status: '1', name: 'b' },
      { status: '2', name: 'c' },
    ];
    const groups = groupRows(rows, ['Status'], fields);
    expect(groups.map((g) => g.label)).toEqual(['启用', '停用']);
    expect(groups[0].count).toBe(2);
    expect(groups[0].children).toHaveLength(2);
    expect(groups[1].count).toBe(1);
  });

  it('multi-level grouping nests children and recomputes counts', () => {
    const rows = [
      { status: '1', dept: '甲', name: 'a' },
      { status: '1', dept: '乙', name: 'b' },
      { status: '1', dept: '乙', name: 'c' },
    ];
    const groups = groupRows(rows, ['Status', 'Dept'], fields);
    expect(groups).toHaveLength(1);
    expect(groups[0].count).toBe(3);
    const depts = groups[0].children as typeof groups;
    expect(depts.map((d) => d.label)).toEqual(['甲', '乙']);
    expect(depts[1].count).toBe(2);
  });

  it('empty value groups into 未分组 and unknown field too', () => {
    const rows = [
      { status: '', name: 'a' },
      { name: 'b' },
    ];
    const groups = groupRows(rows, ['Status'], fields);
    expect(groups[0].label).toBe('未分组');
    expect(groups[0].count).toBe(2);
    expect(groups[0].path).toBe('');
  });

  it('builds nested path for collapse keys', () => {
    const rows = [{ status: '1', dept: '甲', name: 'a' }];
    const groups = groupRows(rows, ['Status', 'Dept'], fields);
    expect(groups[0].path).toBe('1');
    expect((groups[0].children as typeof groups)[0].path).toBe('1::甲');
  });
});

describe('isGroupHeaderRow / groupHeaderCell (OSC-0015)', () => {
  it('识别组头节点', () => {
    expect(isGroupHeaderRow({ __group: true, __groupHeader: { label: '启用', path: '1' }, count: 2 })).toBe(true);
    expect(isGroupHeaderRow({ name: 'a' })).toBe(false);
    expect(isGroupHeaderRow({ __group: false })).toBe(false);
  });

  it('groupHeaderCell 输出 label (count)；非组头返回 null', () => {
    const node = {
      __group: true,
      __groupHeader: { label: '启用', path: '1' },
      label: '启用',
      count: 3,
    };
    expect(groupHeaderCell(node)).toBe('启用 (3)');
    expect(groupHeaderCell({ name: 'a' })).toBeNull();
  });
});

describe('分组草稿操作 (OSC-0015)', () => {
  it('pushGroupField 去重 + 上限 3', () => {
    expect(pushGroupField([], 'Dept')).toEqual(['Dept']);
    expect(pushGroupField(['Dept'], 'Dept')).toEqual(['Dept']);
    expect(pushGroupField(['A', 'B'], 'C')).toEqual(['A', 'B', 'C']);
    expect(pushGroupField(['A', 'B', 'C'], 'D')).toEqual(['A', 'B', 'C']);
    expect(pushGroupField(['A'], '')).toEqual(['A']);
  });

  it('moveGroupField 上移/下移，越界或非法返回原数组', () => {
    expect(moveGroupField(['A', 'B', 'C'], 1, -1)).toEqual(['B', 'A', 'C']);
    expect(moveGroupField(['A', 'B', 'C'], 1, 1)).toEqual(['A', 'C', 'B']);
    expect(moveGroupField(['A', 'B', 'C'], 0, -1)).toEqual(['A', 'B', 'C']);
    expect(moveGroupField(['A', 'B', 'C'], 2, 1)).toEqual(['A', 'B', 'C']);
    expect(moveGroupField(['A'], 0, 1)).toEqual(['A']);
  });

  it('removeGroupField 删除指定下标', () => {
    expect(removeGroupField(['A', 'B', 'C'], 1)).toEqual(['A', 'C']);
    expect(removeGroupField(['A'], 0)).toEqual([]);
    expect(removeGroupField(['A'], 5)).toEqual(['A']);
  });

  it('nextGroupFieldNames 排除已选并受上限约束', () => {
    expect(nextGroupFieldNames(['A', 'B', 'C'], ['A'])).toEqual(['B', 'C']);
    expect(nextGroupFieldNames(['A', 'B', 'C'], ['A', 'B', 'C'])).toEqual([]);
  });
});

describe('defaultViewKindName（保存视图为默认XX视图文案）', () => {
  it('六种视图类型映射到默认视图名称', () => {
    expect(defaultViewKindName('table')).toBe('列表');
    expect(defaultViewKindName('tree')).toBe('树状');
    expect(defaultViewKindName('card')).toBe('卡片');
    expect(defaultViewKindName('kanban')).toBe('看板');
    expect(defaultViewKindName('calendar')).toBe('日历');
    expect(defaultViewKindName('gantt')).toBe('甘特');
  });
});

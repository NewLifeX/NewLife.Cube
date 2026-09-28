import { describe, expect, it } from 'vitest';
import type { FieldMeta } from '@/core/types/field';
import { groupGanttRecords, isGanttGroupParent } from './ganttGroup';

const fields = [
  {
    name: 'Status',
    displayName: '状态',
    typeName: 'Enum',
    dataSource: { Open: '进行中', Done: '已完成' },
  },
] as FieldMeta[];

describe('groupGanttRecords', () => {
  it('无字段时扁平，没有合成父节点', () => {
    const rows = [{ id: 1, Status: 'Open' }, { id: 2, Status: 'Done' }];
    const out = groupGanttRecords(rows, '', fields);
    expect(out).toBe(rows);
    expect(out.some(isGanttGroupParent)).toBe(false);
  });

  it('两个枚举值得到两个父节点，标题用数据源 label', () => {
    const rows = [
      { id: 1, Status: 'Open' },
      { id: 2, Status: 'Done' },
      { id: 3, Status: 'Open' },
    ];
    const out = groupGanttRecords(rows, 'Status', fields);
    expect(out).toHaveLength(2);
    expect(out.every(isGanttGroupParent)).toBe(true);
    const parents = out.filter(isGanttGroupParent);
    expect(parents.map((p) => p.id)).toEqual(['__group:Open', '__group:Done']);
    expect(parents.map((p) => p.title)).toEqual(['进行中', '已完成']);
    expect(parents[0].children).toHaveLength(2);
    expect(parents[1].children).toEqual([rows[1]]);
  });

  it('空值归入未分组，父 id 以 __group: 开头', () => {
    const rows = [
      { id: 1, Status: 'Open' },
      { id: 2, Status: '' },
      { id: 3 },
    ];
    const out = groupGanttRecords(rows, 'Status', fields);
    const empty = out.filter(isGanttGroupParent).find((p) => p.title === '未分组');
    expect(empty).toBeTruthy();
    expect(empty!.id.startsWith('__group:')).toBe(true);
    expect(empty!.id).toBe('__group:');
    expect(empty!.children).toHaveLength(2);
  });
});

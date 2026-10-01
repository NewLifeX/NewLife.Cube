import { describe, expect, it } from 'vitest';
import { reloadWatchFields, shouldReloadAfterPatch, shouldReloadAfterWrite } from './patchReload';

describe('shouldReloadAfterPatch', () => {
  const sorts = [{ field: 'Name', desc: false }];
  const filter = { logic: 'all' as const, conditions: [{ field: 'Status', op: 'eq' as const, value: 1 }] };

  it('空字段名保守刷新', () => {
    expect(shouldReloadAfterPatch('', sorts, filter)).toBe(true);
    expect(shouldReloadAfterPatch('  ', sorts, filter)).toBe(true);
  });

  it('排序命中则刷新，大小写不敏感', () => {
    expect(shouldReloadAfterPatch('name', sorts, filter)).toBe(true);
  });

  it('筛选命中则刷新', () => {
    expect(shouldReloadAfterPatch('status', [], filter)).toBe(true);
  });

  it('都不命中则不刷新', () => {
    expect(shouldReloadAfterPatch('Enable', sorts, filter)).toBe(false);
  });
});

describe('shouldReloadAfterWrite', () => {
  const sorts = [{ field: 'Name', desc: false }];
  const filter = { logic: 'all' as const, conditions: [{ field: 'Status', op: 'eq' as const, value: 1 }] };

  it('没有字段名时刷新', () => {
    expect(shouldReloadAfterWrite([], sorts, filter, [])).toBe(true);
    expect(shouldReloadAfterWrite(['', '  '], sorts, filter, [])).toBe(true);
  });

  it('任一字段命中排序、筛选或观察字段则刷新', () => {
    expect(shouldReloadAfterWrite(['Remark', 'name'], sorts, filter, [])).toBe(true);
    expect(shouldReloadAfterWrite(['Remark', 'status'], [], filter, [])).toBe(true);
    expect(shouldReloadAfterWrite(['DeptId'], [], undefined, ['deptId'])).toBe(true);
  });

  it('都不命中则不刷新', () => {
    expect(shouldReloadAfterWrite(['Remark', 'Enable'], sorts, filter, ['GroupField'])).toBe(false);
  });
});

describe('reloadWatchFields', () => {
  it('看板只观察分组字段', () => {
    expect(reloadWatchFields('kanban', { groupField: 'DeptId' })).toEqual(['DeptId']);
  });

  it('日历只观察开始字段', () => {
    expect(reloadWatchFields('calendar', { startField: 'Start' })).toEqual(['Start']);
  });

  it('甘特观察计划、实际和分组', () => {
    expect(
      reloadWatchFields('gantt', {
        plannedStartField: 'PlanStart',
        plannedEndField: 'PlanEnd',
        actualStartField: 'ActualStart',
        actualEndField: ' ',
        groupField: 'Owner',
      }),
    ).toEqual(['PlanStart', 'PlanEnd', 'ActualStart', 'Owner']);
  });

  it('表格和卡片没有观察字段', () => {
    expect(reloadWatchFields('table', { groupField: 'DeptId' })).toEqual([]);
    expect(reloadWatchFields('card', null)).toEqual([]);
  });
});

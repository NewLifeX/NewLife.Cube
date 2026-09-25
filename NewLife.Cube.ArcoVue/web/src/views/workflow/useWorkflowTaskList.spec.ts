import { describe, expect, it, vi } from 'vitest';

vi.mock('@/api', () => ({
  default: {
    workflow: { todo: vi.fn(), done: vi.fn(), batchApprove: vi.fn(), reject: vi.fn() },
  },
}));

import { dueText, modeLabel, taskStatusMeta, taskTitle, entityLabel, summaryPlain, isHandleTask, rowActionsOf } from './useWorkflowTaskList';
import type { WorkflowTaskItem } from '@newlifex/api-core';

describe('modeLabel 节点模式', () => {
  it('or/and/sequence 中文', () => {
    expect(modeLabel('or')).toBe('或签');
    expect(modeLabel('AND')).toBe('会签');
    expect(modeLabel('sequence')).toBe('依次签');
    expect(modeLabel('')).toBe('');
    expect(modeLabel(undefined)).toBe('');
  });
});

describe('taskStatusMeta 任务状态', () => {
  it('后端大写状态映射', () => {
    expect(taskStatusMeta('Pending')).toEqual({ text: '待处理', color: 'orange' });
    expect(taskStatusMeta('Active')).toEqual({ text: '处理中', color: 'blue' });
    expect(taskStatusMeta('Done')).toEqual({ text: '已完成', color: 'green' });
    expect(taskStatusMeta('Rejected')).toEqual({ text: '已驳回', color: 'red' });
    expect(taskStatusMeta('Cancelled')).toEqual({ text: '已取消', color: 'gray' });
    expect(taskStatusMeta('Transferred')).toEqual({ text: '已转办', color: 'gray' });
  });
});

describe('dueText 截止倒计时', () => {
  const now = new Date('2026-09-03T12:00:00').getTime();

  it('无 DueTime / 非法 / 未设超时的 MinValue 返回 null', () => {
    expect(dueText(undefined, now)).toBeNull();
    expect(dueText('', now)).toBeNull();
    expect(dueText('not-a-date', now)).toBeNull();
    expect(dueText('0001-01-01 00:00:00', now)).toBeNull();
    expect(dueText('0001-01-01T00:00:00', now)).toBeNull();
  });

  it('未来显示剩余，过去显示已超时', () => {
    const future = new Date('2026-09-04T15:00:00').toISOString();
    expect(dueText(future, now)).toEqual({ text: '剩 1 天 3 时', overdue: false });
    const past = new Date('2026-09-03T08:30:00').toISOString();
    expect(dueText(past, now)).toEqual({ text: '已超时 3 时 30 分', overdue: true });
  });
});

describe('taskTitle 标题兜底', () => {
  it('有 title 用之，否则 typePath #instanceId', () => {
    expect(taskTitle({ title: '张三请假' } as WorkflowTaskItem)).toBe('张三请假');
    expect(taskTitle({ typePath: 'Admin/User', instanceId: 7 } as WorkflowTaskItem)).toBe(
      'Admin/User #7',
    );
  });
});

describe('entityLabel 实体友好名', () => {
  it('优先 typeName，否则路径末段', () => {
    expect(entityLabel('部门', 'Admin/Department')).toBe('部门');
    expect(entityLabel('', 'Admin/Department')).toBe('Department');
    expect(entityLabel(undefined, undefined)).toBe('—');
  });
});

describe('summaryPlain 列表摘要', () => {
  it('去 Markdown 并截断', () => {
    expect(summaryPlain('')).toBe('');
    expect(summaryPlain('**加急** 请审批')).toBe('加急 请审批');
    expect(summaryPlain('a'.repeat(100), 10)).toBe(`${'a'.repeat(10)}…`);
  });
});

describe('rowActionsOf 行操作矩阵（T5：办理节点只办不驳回）', () => {
  const todo = (t: Partial<WorkflowTaskItem>) => ({ id: '1', status: 'Pending', ...t }) as WorkflowTaskItem;

  it('审批行待办：同意/驳回/更多/进度', () => {
    const a = rowActionsOf(todo({ nodeType: 'oa.approve' }), 'todo');
    expect(a).toEqual({ canApprove: true, canReject: true, canHandle: false, canMore: true, canProgress: true });
  });

  it('办理行待办：仅「已办理」，不显示驳回', () => {
    const a = rowActionsOf(todo({ nodeType: 'oa.handle' }), 'todo');
    expect(a).toEqual({ canApprove: false, canReject: false, canHandle: true, canMore: true, canProgress: true });
  });

  it('已办列表与缺省 nodeType：不可操作，仅保留进度', () => {
    const a = rowActionsOf(todo({ nodeType: 'oa.handle' }), 'done');
    expect(a.canApprove).toBe(false);
    expect(a.canHandle).toBe(false);
    expect(a.canMore).toBe(false);
    expect(a.canProgress).toBe(true);
    expect(isHandleTask(todo({}))).toBe(false); // 缺省按审批处理
  });
});

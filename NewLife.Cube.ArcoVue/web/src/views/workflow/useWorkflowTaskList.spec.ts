import { describe, expect, it, vi } from 'vitest';

vi.mock('@/api', () => ({
  default: {
    workflow: { todo: vi.fn(), done: vi.fn(), batchApprove: vi.fn(), reject: vi.fn() },
  },
}));

import { dueText, modeLabel, taskStatusMeta, taskTitle } from './useWorkflowTaskList';
import type { WorkflowTaskItem } from '@cube/api-core';

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

  it('无 DueTime / 非法返回 null', () => {
    expect(dueText(undefined, now)).toBeNull();
    expect(dueText('', now)).toBeNull();
    expect(dueText('not-a-date', now)).toBeNull();
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

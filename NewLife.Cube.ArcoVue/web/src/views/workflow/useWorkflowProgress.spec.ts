import { describe, expect, it, vi } from 'vitest';

vi.mock('@/api', () => ({
  default: {
    workflow: { instance: vi.fn(), approve: vi.fn(), reject: vi.fn(), claim: vi.fn(), addSign: vi.fn(), transfer: vi.fn(), cc: vi.fn(), withdraw: vi.fn() },
  },
}));

import { actionLabel, instanceStatusMeta, pickMyTask } from './useWorkflowProgress';
import type { WorkflowInstanceDetail } from '@cube/api-core';

describe('instanceStatusMeta 实例状态展示', () => {
  it('后端大写状态映射', () => {
    expect(instanceStatusMeta('Running')).toEqual({ text: '审批中', color: 'orange' });
    expect(instanceStatusMeta('Approved')).toEqual({ text: '已通过', color: 'green' });
    expect(instanceStatusMeta('Rejected')).toEqual({ text: '已驳回', color: 'red' });
    expect(instanceStatusMeta('Withdrawn')).toEqual({ text: '已撤回', color: 'gray' });
    expect(instanceStatusMeta('Cancelled')).toEqual({ text: '已作废', color: 'gray' });
    expect(instanceStatusMeta(undefined)).toEqual({ text: '未知', color: 'gray' });
  });
});

describe('actionLabel 意见动作中文', () => {
  it('常见动作', () => {
    expect(actionLabel('start')).toBe('发起');
    expect(actionLabel('Approve')).toBe('同意');
    expect(actionLabel('reject')).toBe('驳回');
    expect(actionLabel('transfer')).toBe('转办');
    expect(actionLabel('cc')).toBe('知会');
    expect(actionLabel('addsign')).toBe('加签');
    expect(actionLabel('rollback')).toBe('回退');
    expect(actionLabel('withdraw')).toBe('撤回');
    expect(actionLabel('cancel')).toBe('作废');
    expect(actionLabel('')).toBe('处理');
  });
});

describe('pickMyTask 当前用户可办任务', () => {
  const base = {
    id: 1,
    typePath: 'Admin/User',
    status: 'Running',
    tasks: [
      { id: 11, status: 'Pending', visible: true, candidate: [2, 3], assigneeId: 0 },
      { id: 12, status: 'Active', visible: true, candidate: [], assigneeId: 5 },
    ],
  } as unknown as WorkflowInstanceDetail;

  it('候选命中（or 未认领）', () => {
    const t = pickMyTask(base, 3);
    expect(t?.id).toBe(11);
  });

  it('被指派命中（and/已认领）', () => {
    const t = pickMyTask(base, 5);
    expect(t?.id).toBe(12);
  });

  it('非候选返回 null', () => {
    expect(pickMyTask(base, 9)).toBeNull();
  });

  it('依次签未轮到 visible=false 不入选', () => {
    const d = {
      ...base,
      tasks: [
        { id: 21, status: 'Pending', visible: false, candidate: [3], assigneeId: 0 },
        { id: 22, status: 'Pending', visible: true, candidate: [8], assigneeId: 0 },
      ],
    } as unknown as WorkflowInstanceDetail;
    expect(pickMyTask(d, 3)).toBeNull();
    expect(pickMyTask(d, 8)?.id).toBe(22);
  });

  it('无详情 / 无 userId 返回 null', () => {
    expect(pickMyTask(null, 3)).toBeNull();
    expect(pickMyTask(base, undefined)).toBeNull();
  });
});

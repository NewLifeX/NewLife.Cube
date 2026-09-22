import { describe, expect, it, vi } from 'vitest';

vi.mock('@/api', () => ({
  default: { workflow: {}, automation: { entities: vi.fn() }, page: { getPage: vi.fn() } },
}));

import type { WorkflowInstanceDetail } from '@newlifex/api-core';
import { buildProgressFlow, rollbackTargets } from './wfProgressFlow';

const snapshot = JSON.stringify({
  version: 1,
  nodes: [
    { id: 'start', type: 'oa.start', data: {} },
    { id: 'n1', type: 'oa.approve', data: { name: '主管', mode: 'or' } },
    { id: 'end', type: 'oa.end', data: {} },
  ],
  edges: [
    { source: 'start', target: 'n1' },
    { source: 'n1', target: 'end' },
  ],
});

describe('buildProgressFlow', () => {
  it('发起 + 审批节点 + 当前用户待办高亮', () => {
    const d = {
      id: '1',
      typePath: 'Admin/User',
      status: 'Running',
      starterId: 9,
      createTime: '2026-09-01',
      graphSnapshot: snapshot,
      tasks: [{ id: '11', nodeId: 'n1', status: 'Pending', visible: true, candidate: [3], assigneeId: 0, mode: 'or' }],
      comments: [],
    } as unknown as WorkflowInstanceDetail;
    const nodes = buildProgressFlow(d, 3);
    expect(nodes[0].type).toBe('oa.start');
    const ap = nodes.find((n) => n.nodeId === 'n1');
    expect(ap?.title).toBe('主管');
    expect(ap?.current).toBe(true);
    expect(ap?.badges).toContain('或签');
  });
});

describe('rollbackTargets', () => {
  it('只收已办审批节点', () => {
    const d = {
      tasks: [
        { nodeId: 'n1', status: 'Done' },
        { nodeId: 'n2', status: 'Pending' },
      ],
      graphSnapshot: snapshot,
    } as unknown as WorkflowInstanceDetail;
    expect(rollbackTargets(d, 'n2').map((t) => t.nodeId)).toEqual(['n1']);
  });
});

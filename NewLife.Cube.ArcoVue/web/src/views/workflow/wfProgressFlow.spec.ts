import { describe, expect, it, vi } from 'vitest';

vi.mock('@/api', () => ({
  default: { workflow: {}, automation: { entities: vi.fn() }, page: { getPage: vi.fn() } },
}));

import type { WorkflowInstanceDetail } from '@newlifex/api-core';
import { buildProgressFlow, definitionPaint, opinionTimeline, previewKind, rollbackTargets, executionOrder, splitAttachments } from './wfProgressFlow';

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

const xorSnapshot = JSON.stringify({
  version: 1,
  nodes: [
    { id: 'start', type: 'oa.start', data: {} },
    {
      id: 'x1',
      type: 'oa.xor',
      data: { name: '金额分流', cases: [{ target: 'a1', filter: {} }], defaultTarget: 'b1' },
    },
    { id: 'a1', type: 'oa.approve', data: { name: '大额审批' } },
    { id: 'b1', type: 'oa.approve', data: { name: '普通审批' } },
    { id: 'end', type: 'oa.end', data: {} },
  ],
  edges: [
    { source: 'start', target: 'x1' },
    { source: 'x1', target: 'a1' },
    { source: 'x1', target: 'b1' },
    { source: 'a1', target: 'end' },
    { source: 'b1', target: 'end' },
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

describe('executionOrder 隐藏未命中分支（T5）', () => {
  const graph = JSON.parse(xorSnapshot);

  it('任务在「其他情况」支：只渲染该支，命中提示写明分支名', () => {
    const tasks = [{ id: '11', nodeId: 'b1', status: 'Done' } as never];
    const order = executionOrder(graph, tasks).map((n) => n.id);
    expect(order).toContain('b1');
    expect(order).not.toContain('a1');

    const d = {
      id: '5',
      status: 'Running',
      graphSnapshot: xorSnapshot,
      tasks: [{ id: '11', nodeId: 'b1', status: 'Done', assigneeId: 3 }],
      comments: [],
    } as unknown as WorkflowInstanceDetail;
    const nodes = buildProgressFlow(d, 3);
    const xor = nodes.find((n) => n.nodeId === 'x1');
    expect(xor?.hint).toBe('已走「普通审批」');
    expect(nodes.some((n) => n.nodeId === 'a1')).toBe(false);
  });

  it('任务在条件支：命中提示指向条件支', () => {
    const tasks = [{ id: '12', nodeId: 'a1', status: 'Done' } as never];
    const order = executionOrder(graph, tasks).map((n) => n.id);
    expect(order).toContain('a1');
    expect(order).not.toContain('b1');
  });
});

describe('splitAttachments 附件归类（T5）', () => {
  it('发起 / 意见 key / 任务 key 三分', () => {
    const atts = [
      { id: 1, key: '5', fileName: '申请单.pdf' },
      { id: 2, key: '5:c7', fileName: '意见.png' },
      { id: 3, key: '5:11', fileName: '旧附件.txt' },
    ] as unknown as WorkflowInstanceDetail['attachments'];
    const r = splitAttachments(atts, '5');
    expect(r.starter.map((a) => a.id)).toEqual(['1']);
    expect(r.byComment.get('7')?.map((a) => a.fileName)).toEqual(['意见.png']);
    expect(r.byTask.get('11')?.map((a) => a.fileName)).toEqual(['旧附件.txt']);
  });
});

describe('节点提示与意见附件（T5）', () => {
  it('自动跳过写在节点 hint；意见附件挂在办理人名下', () => {
    const d = {
      id: '5',
      status: 'Running',
      graphSnapshot: snapshot,
      tasks: [{ id: '21', nodeId: 'n1', status: 'Done', assigneeId: 3 }],
      comments: [{ id: '31', taskId: '21', content: '同意', createUser: '张三', createTime: '2026-09-02' }],
      attachments: [{ id: 2, key: '5:c31', fileName: '意见.png', url: '/f/2' }],
    } as unknown as WorkflowInstanceDetail;
    const nodes = buildProgressFlow(d, 3);
    const n1 = nodes.find((n) => n.nodeId === 'n1');
    expect(n1?.people[0]?.attachments?.[0]?.fileName).toBe('意见.png');

    const d2 = { ...d, comments: [{ id: '32', taskId: '21', content: '自动跳过', createUser: '系统' }] };
    const nodes2 = buildProgressFlow(d2 as unknown as WorkflowInstanceDetail, 3);
    expect(nodes2.find((n) => n.nodeId === 'n1')?.hint).toContain('自动跳过');
  });

  it('已转办状态写在节点 hint', () => {
    const d = {
      id: '5',
      status: 'Running',
      graphSnapshot: snapshot,
      tasks: [{ id: '22', nodeId: 'n1', status: 'Transferred', assigneeId: 3 }],
      comments: [],
    } as unknown as WorkflowInstanceDetail;
    const nodes = buildProgressFlow(d, 3);
    expect(nodes.find((n) => n.nodeId === 'n1')?.hint).toContain('已转办');
  });

  it('查看流程：已完成、当前、未处理三种颜色状态', () => {
    const d = {
      id: '5',
      status: 'Running',
      graphSnapshot: snapshot,
      startComment: '请批',
      tasks: [{ id: '11', nodeId: 'n1', status: 'Pending', assigneeId: 3 }],
      comments: [],
    } as unknown as WorkflowInstanceDetail;
    const paint = definitionPaint(d);
    expect(paint.find((n) => n.key === 'start')?.state).toBe('done');
    expect(paint.find((n) => n.key === 'n1')?.state).toBe('current');
    expect(paint.find((n) => n.key === 'end')?.state).toBe('waiting');
    expect(paint.find((n) => n.key === 'n1')?.assignees).toBe('人员：未指定');
    expect(paint.find((n) => n.key === 'start')?.assignees).toBe('');
    const events = opinionTimeline(d);
    expect(events[0]?.actionText).toBe('发起');
    expect(events.some((e) => e.actionText === '发起' && e.content === '请批')).toBe(true);
  });

  it('预览只认图片、pdf 和纯文本', () => {
    expect(previewKind('a.PNG')).toBe('image');
    expect(previewKind('a.pdf')).toBe('pdf');
    expect(previewKind('a.txt')).toBe('text');
    expect(previewKind('a.docx')).toBe('none');
  });
});

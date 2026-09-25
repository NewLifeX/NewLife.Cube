import { describe, expect, it, vi } from 'vitest';

vi.mock('@/api', () => ({
  default: { workflow: {}, automation: { entities: vi.fn() }, page: { getPage: vi.fn() } },
}));

import type { WfGraphData } from '@/core/types/workflow';
import {
  defaultNodeDataFor,
  flowDocToGraph,
  graphToFlowDoc,
  nextCaseBlockId,
  xorSplitBlocks,
} from './flowgramGraph';

const chainGraph: WfGraphData = {
  version: 1,
  nodes: [
    { id: 'start', type: 'oa.start', data: { name: '开始' } },
    { id: 'n1', type: 'oa.approve', data: { name: '审批', to: { kind: 'users', users: [1] } } },
    { id: 'cc1', type: 'oa.cc', data: { name: '知会' } },
    { id: 'end', type: 'oa.end', data: { name: '结束' } },
  ],
  edges: [
    { source: 'start', target: 'n1' },
    { source: 'n1', target: 'cc1' },
    { source: 'cc1', target: 'end' },
  ],
};

describe('graphToFlowDoc / flowDocToGraph 双向无损', () => {
  it('链序 ↔ 相邻成边，data 透传；start/end 映射画布内置类型', () => {
    const doc = graphToFlowDoc(chainGraph);
    expect(doc.nodes.map((n) => n.id)).toEqual(['start', 'n1', 'cc1', 'end']);
    // 画布内置 start/end 类型，中间节点沿用后端 oa.*
    expect(doc.nodes[0].type).toBe('start');
    expect(doc.nodes[doc.nodes.length - 1].type).toBe('end');
    expect(doc.nodes[1].type).toBe('oa.approve');
    expect((doc.nodes[1].data as Record<string, unknown>).name).toBe('审批');

    const back = flowDocToGraph(doc);
    expect(back.nodes).toEqual(chainGraph.nodes);
    expect(back.edges).toEqual(chainGraph.edges);
  });

  it('虚拟节点（非业务 type）被过滤', () => {
    const doc = {
      nodes: [
        { id: 'start', type: 'start', data: {} },
        { id: '__icon_1', type: '__block__', data: {} },
        { id: 'n1', type: 'oa.approve', data: { name: '审批' } },
        { id: '__order_1', type: '__block_icon__', data: {} },
        { id: 'end', type: 'end', data: {} },
      ],
    };
    const back = flowDocToGraph(doc);
    expect(back.nodes.map((n) => n.id)).toEqual(['start', 'n1', 'end']);
    expect(back.nodes.map((n) => n.type)).toEqual(['oa.start', 'oa.approve', 'oa.end']);
    expect(back.edges).toEqual([
      { source: 'start', target: 'n1' },
      { source: 'n1', target: 'end' },
    ]);
  });

  it('空/缺省安全', () => {
    expect(flowDocToGraph(null).nodes).toEqual([]);
    expect(graphToFlowDoc(null).nodes).toEqual([]);
  });

  it('xor 数据（cases/defaultTarget）透传无损', () => {
    const g: WfGraphData = {
      version: 1,
      nodes: [
        { id: 'start', type: 'oa.start', data: {} },
        { id: 'x', type: 'oa.xor', data: { name: '分流', cases: [{ target: 'n2' }], defaultTarget: 'end' } },
        { id: 'end', type: 'oa.end', data: {} },
      ],
      edges: [
        { source: 'start', target: 'x' },
        { source: 'x', target: 'end' },
      ],
    };
    const back = flowDocToGraph(graphToFlowDoc(g));
    expect(back.nodes.find((n) => n.id === 'x')?.data).toMatchObject({
      name: '分流',
      defaultTarget: 'end',
    });
    expect(back.edges).toEqual(
      expect.arrayContaining([
        { source: 'start', target: 'x' },
        { source: 'x', target: 'end' },
      ]),
    );
  });

  it('xor 画布为 条件N/其他情况 槽位，分支节点在 blocks 内；旧图（1 case）可打开', () => {
    const g: WfGraphData = {
      version: 1,
      nodes: [
        { id: 'start', type: 'oa.start', data: {} },
        { id: 'x', type: 'oa.xor', data: { cases: [{ target: 'n1' }], defaultTarget: 'end' } },
        { id: 'n1', type: 'oa.approve', data: { name: '主管' } },
        { id: 'end', type: 'oa.end', data: {} },
      ],
      edges: [
        { source: 'start', target: 'x' },
        { source: 'x', target: 'n1' },
        { source: 'x', target: 'end' },
        { source: 'n1', target: 'end' },
      ],
    };
    const doc = graphToFlowDoc(g);
    expect(doc.nodes.map((n) => n.id)).toEqual(['start', 'x', 'end']);
    const xor = doc.nodes[1];
    expect(xor.blocks).toHaveLength(2);
    expect(xor.blocks?.[0].data?.title).toBe('条件1');
    expect(xor.blocks?.[1].data?.title).toBe('其他情况');
    expect(xor.blocks?.[0].id).toBe('x__if');
    expect(xor.blocks?.[1].id).toBe('x__else');
    expect(xor.blocks?.[0].blocks?.map((n) => n.id)).toEqual(['n1']);
    expect(xor.blocks?.[1].blocks).toEqual([]);
    const back = flowDocToGraph(doc);
    expect(back.nodes.map((n) => n.id).sort()).toEqual(['end', 'n1', 'start', 'x']);
    expect(back.edges).toEqual(
      expect.arrayContaining([
        { source: 'x', target: 'n1' },
        { source: 'x', target: 'end' },
        { source: 'n1', target: 'end' },
      ]),
    );
    const x = back.nodes.find((n) => n.id === 'x')!;
    expect(x.data?.defaultTarget).toBe('end');
    expect((x.data?.cases as { target: string }[])[0].target).toBe('n1');
  });

  it('xor 多条件支：N 个条件槽 + 末支「其他情况」，cases/filter 保序透传', () => {
    const g: WfGraphData = {
      version: 1,
      nodes: [
        { id: 'start', type: 'oa.start', data: {} },
        {
          id: 'x',
          type: 'oa.xor',
          data: {
            name: '分流',
            cases: [
              { filter: { logic: 'all', conditions: [] }, target: 'n1' },
              { filter: { logic: 'all', conditions: [] }, target: 'n2' },
              { filter: { logic: 'all', conditions: [] }, target: 'n3' },
            ],
            defaultTarget: 'end',
          },
        },
        { id: 'n1', type: 'oa.approve', data: { name: 'A' } },
        { id: 'n2', type: 'oa.approve', data: { name: 'B' } },
        { id: 'n3', type: 'oa.approve', data: { name: 'C' } },
        { id: 'end', type: 'oa.end', data: {} },
      ],
      edges: [
        { source: 'start', target: 'x' },
        { source: 'x', target: 'n1' },
        { source: 'x', target: 'n2' },
        { source: 'x', target: 'n3' },
        { source: 'x', target: 'end' },
        { source: 'n1', target: 'end' },
        { source: 'n2', target: 'end' },
        { source: 'n3', target: 'end' },
      ],
    };
    const doc = graphToFlowDoc(g);
    const xor = doc.nodes.find((n) => n.id === 'x')!;
    expect(xor.blocks).toHaveLength(4);
    expect(xor.blocks?.map((b) => b.data?.title)).toEqual(['条件1', '条件2', '条件3', '其他情况']);
    expect(xor.blocks?.map((b) => b.id)).toEqual(['x__if', 'x__case1', 'x__case2', 'x__else']);

    const back = flowDocToGraph(doc);
    const x = back.nodes.find((n) => n.id === 'x')!;
    expect((x.data?.cases as { target: string }[]).map((c) => c.target)).toEqual(['n1', 'n2', 'n3']);
    expect(x.data?.defaultTarget).toBe('end');
  });
});

describe('分支卡片数据（画布条件卡片与添加条件）', () => {
  const xorGraph: WfGraphData = {
    version: 1,
    nodes: [
      { id: 'start', type: 'oa.start', data: {} },
      {
        id: 'x',
        type: 'oa.xor',
        data: {
          name: '分流',
          cases: [
            { filter: { logic: 'all', conditions: [{ field: 'Type', op: 'eq', value: '1' }] }, target: 'n1' },
            { filter: { logic: 'all', conditions: [] }, target: '' },
          ],
          defaultTarget: 'end',
        },
      },
      { id: 'n1', type: 'oa.approve', data: { name: 'A' } },
      { id: 'end', type: 'oa.end', data: {} },
    ],
    edges: [
      { source: 'start', target: 'x' },
      { source: 'x', target: 'n1' },
      { source: 'x', target: 'end' },
      { source: 'n1', target: 'end' },
    ],
  };

  it('分支块携带 filter（卡片摘要数据源），与 cases 一一对应', () => {
    const doc = graphToFlowDoc(xorGraph);
    const xor = doc.nodes.find((n) => n.id === 'x')!;
    expect(xor.blocks?.map((b) => b.data?.filter)).toEqual([
      { logic: 'all', conditions: [{ field: 'Type', op: 'eq', value: '1' }] },
      { logic: 'all', conditions: [] },
      undefined,
    ]);
  });

  it('块上 filter 为准（条件编辑后重建 cases 不错位）', () => {
    const doc = graphToFlowDoc(xorGraph);
    const xor = doc.nodes.find((n) => n.id === 'x')!;
    // 模拟画布内把条件2（空块）改成「Type 等于 9」
    xor.blocks![1].data = { ...xor.blocks![1].data, filter: { logic: 'all', conditions: [{ field: 'Type', op: 'eq', value: '9' }] } };
    const back = flowDocToGraph(doc);
    const x = back.nodes.find((n) => n.id === 'x')!;
    const cases = x.data?.cases as { filter: unknown; target: string }[];
    expect(cases[0].filter).toEqual({ logic: 'all', conditions: [{ field: 'Type', op: 'eq', value: '1' }] });
    expect(cases[1].filter).toEqual({ logic: 'all', conditions: [{ field: 'Type', op: 'eq', value: '9' }] });
  });

  it('块上「则去」缓存：块内无节点时 target 取块上值；无缓存则为空且不回退旧值', () => {
    const doc = graphToFlowDoc(xorGraph);
    const xor = doc.nodes.find((n) => n.id === 'x')!;
    xor.blocks![1].data = { ...xor.blocks![1].data, target: 'n2', filter: { logic: 'all', conditions: [] } };
    const back = flowDocToGraph(doc);
    const x = back.nodes.find((n) => n.id === 'x')!;
    const cases = x.data?.cases as { filter: unknown; target: string }[];
    expect(cases[1].target).toBe('n2');
    // 无缓存 + 块空：target 为空（不再用旧 cases 下标兜底，避免删分支后错位）
    const doc2 = graphToFlowDoc(xorGraph);
    const x2 = flowDocToGraph(doc2).nodes.find((n) => n.id === 'x')!;
    expect((x2.data?.cases as { target: string }[])[1].target).toBe('');
  });

  it('nextCaseBlockId：按现有 __if/__caseN 递增并避开缺口', () => {
    expect(nextCaseBlockId('x', ['x__if', 'x__else'])).toBe('x__case1');
    expect(nextCaseBlockId('x', ['x__if', 'x__case1', 'x__else'])).toBe('x__case2');
    expect(nextCaseBlockId('x', ['x__if', 'x__case2', 'x__else'])).toBe('x__case3');
  });

  it('xorSplitBlocks：filters 按序写入块，其他情况无 filter', () => {
    const blocks = xorSplitBlocks('x', 2, [{ a: 1 }, { b: 2 }]);
    expect(blocks.map((b) => b.id)).toEqual(['x__if', 'x__case1', 'x__else']);
    expect(blocks.map((b) => b.data?.filter)).toEqual([{ a: 1 }, { b: 2 }, undefined]);
  });
});

describe('defaultNodeDataFor 各类型默认 data', () => {
  it('approve 含 to/fields/timeout/加签回退', () => {
    const d = defaultNodeDataFor('oa.approve');
    expect(d.name).toBe('审批');
    expect(d.mode).toBe('or');
    expect(d.to).toEqual({ kind: 'users', users: [], roles: [], departments: [] });
    expect(d.fields).toEqual({ visible: ['*'], writable: [] });
    expect(d.timeoutAction).toBe('pass');
    expect(d.allowTransfer).toBe(true);
  });
  it('cc/xor/start/end 有默认 name', () => {
    expect(defaultNodeDataFor('oa.cc').name).toBe('知会');
    expect(defaultNodeDataFor('oa.xor').name).toBe('条件分流');
    expect(defaultNodeDataFor('oa.start').name).toBe('开始');
    expect(defaultNodeDataFor('oa.end').name).toBe('结束');
  });
});

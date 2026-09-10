import { describe, expect, it, vi } from 'vitest';

vi.mock('@/api', () => ({
  default: { workflow: {}, automation: { entities: vi.fn() }, page: { getPage: vi.fn() } },
}));

import {
  applyNodePatch,
  compileGraph,
  insertAfter,
  newDefaultGraph,
  parseGraph,
  removeNode,
  topoChain,
  validateGraph,
} from './useWorkflowDesigner';
import type { WfGraphData } from '@/core/types/workflow';

function toApprove(nodes: WfGraphData['nodes']) {
  return nodes.find((n) => n.type === 'oa.approve')!;
}

describe('newDefaultGraph 默认 Start→审批→End', () => {
  it('结构正确', () => {
    const g = newDefaultGraph();
    expect(g.version).toBe(1);
    expect(g.nodes.map((n) => n.type)).toEqual(['oa.start', 'oa.approve', 'oa.end']);
    expect(g.edges).toEqual([
      { source: 'start', target: 'n1' },
      { source: 'n1', target: 'end' },
    ]);
  });
});

describe('parseGraph 容错', () => {
  it('非法/空回落默认图', () => {
    expect(parseGraph(undefined).nodes.length).toBe(3);
    expect(parseGraph('not-json').nodes.length).toBe(3);
    expect(parseGraph('{"a":1}').nodes.length).toBe(3);
  });
  it('合法图保留结构', () => {
    const g = parseGraph('{"version":1,"nodes":[{"id":"start","type":"oa.start","data":{}}],"edges":[]}');
    expect(g.nodes[0].id).toBe('start');
    expect(g.version).toBe(1);
  });
});

describe('topoChain 沿唯一后继', () => {
  it('start→n1→n2→end', () => {
    const g = insertAfter(newDefaultGraph(), 'n1', 'oa.cc');
    const order = topoChain(g).map((n) => n.id);
    expect(order).toEqual(['start', 'n1', 'n2', 'end']);
  });
  it('无 start 时为空', () => {
    const g: WfGraphData = { version: 1, nodes: [{ id: 'a', type: 'oa.approve', data: {} }], edges: [] };
    expect(topoChain(g)).toEqual([]);
  });
});

describe('insertAfter / removeNode 链重接', () => {
  it('在审批后插入知会并重接', () => {
    let g = newDefaultGraph();
    g = insertAfter(g, 'n1', 'oa.cc');
    const cc = g.nodes.find((n) => n.type === 'oa.cc')!;
    expect(g.edges).toContainEqual({ source: 'n1', target: cc.id });
    expect(g.edges).toContainEqual({ source: cc.id, target: 'end' });
    expect(g.edges).not.toContainEqual({ source: 'n1', target: 'end' });
  });
  it('end 后不可插入', () => {
    const g = insertAfter(newDefaultGraph(), 'end', 'oa.approve');
    expect(g.nodes.length).toBe(3);
  });
  it('删除节点前驱接到后继', () => {
    let g = insertAfter(newDefaultGraph(), 'n1', 'oa.cc');
    const cc = g.nodes.find((n) => n.type === 'oa.cc')!;
    g = removeNode(g, cc.id);
    expect(g.nodes.find((n) => n.id === cc.id)).toBeUndefined();
    expect(g.edges).toContainEqual({ source: 'n1', target: 'end' });
  });
  it('start/end 禁删', () => {
    const g = removeNode(newDefaultGraph(), 'start');
    expect(g.nodes.length).toBe(3);
    const g2 = removeNode(newDefaultGraph(), 'end');
    expect(g2.nodes.length).toBe(3);
  });
});

describe('applyNodePatch / compileGraph', () => {
  it('补丁浅合并并序列化 version 1', () => {
    const g = applyNodePatch(newDefaultGraph(), 'n1', { name: '主管审批' });
    expect(toApprove(g.nodes).data.name).toBe('主管审批');
    const json = compileGraph(g);
    expect(JSON.parse(json).version).toBe(1);
    expect(json).toContain('主管审批');
  });
});

describe('validateGraph 快速校验', () => {
  it('缺审批人时报错', () => {
    expect(validateGraph(newDefaultGraph()).join('')).toContain('缺少审批人');
  });
  it('补上审批人后通过', () => {
    const g = applyNodePatch(newDefaultGraph(), 'n1', {
      to: { kind: 'users', users: [1], roles: [], departments: [] },
    });
    expect(validateGraph(g)).toEqual([]);
  });
});

describe('切换审批人类别清空旧 Id', () => {
  it('users → roles 后 ids 为空', () => {
    let g = applyNodePatch(newDefaultGraph(), 'n1', {
      to: { kind: 'users', users: [1], roles: [], departments: [] },
    });
    g = applyNodePatch(g, 'n1', {
      to: { kind: 'roles', users: [], roles: [], departments: [] },
    });
    const to = toApprove(g.nodes).data.to as { kind: string; roles: number[] };
    expect(to.kind).toBe('roles');
    expect(to.roles ?? []).toEqual([]);
  });
});

import type { WfGraphData, WfGraphNodeData } from '@/core/types/workflow';
import { wfRecipientToJson } from '@/core/types/workflow';
import { isXorBranchBlock, xorBranchBlockId, xorCaseList } from '../wfNodeCard';

export { isXorBranchBlock, xorBranchBlockId };

/**
 * 后端 GraphJson（扁平 nodes+edges）↔ FlowGram 固定布局文档（可嵌套 blocks）。
 * oa.xor 映射为 N 个条件支 + 末支「其他情况」（defaultTarget），对齐飞书条件判断画布（design §3/§4）。
 */

export interface FlowDocNode {
  id: string;
  type: string;
  data?: Record<string, unknown>;
  meta?: Record<string, unknown>;
  blocks?: FlowDocNode[];
}

export interface FlowDoc {
  version?: number;
  nodes: FlowDocNode[];
}

export const CANVAS_TYPE = {
  start: 'start',
  end: 'end',
} as const;

const BUSINESS_TYPES = new Set(['oa.start', 'oa.end', 'oa.approve', 'oa.handle', 'oa.cc', 'oa.xor', 'oa.parallel', 'start', 'end']);

export function isSplitNodeType(type: string): boolean {
  return type === 'oa.xor' || type === 'oa.parallel';
}

export function isBusinessNodeType(type: string): boolean {
  return BUSINESS_TYPES.has(type) || type.startsWith('oa.');
}

/** 插入条件分流时的空分支槽：caseCount 个条件支 + 末支「其他情况」；filters 按序写入块上（分支卡片摘要） */
export function xorSplitBlocks(xorId: string, caseCount = 1, filters?: unknown[]): FlowDocNode[] {
  const blocks: FlowDocNode[] = [];
  for (let i = 0; i < Math.max(0, caseCount); i++) {
    blocks.push({
      id: xorBranchBlockId(xorId, i),
      type: 'block',
      data: { title: `条件${i + 1}`, filter: filters?.[i] },
      blocks: [],
    });
  }
  blocks.push({ id: xorBranchBlockId(xorId, -1), type: 'block', data: { title: '其他情况' }, blocks: [] });
  return blocks;
}

/** 下一个条件分支块 id：扫描现有 __if/__caseN 取最大序号 +1（新块由画布插在「其他情况」之前） */
export function nextCaseBlockId(xorId: string, usedIds: Iterable<string>): string {
  let max = -1;
  for (const id of usedIds) {
    if (id === `${xorId}__if`) max = Math.max(max, 0);
    else if (id.startsWith(xorId)) {
      const m = /__case(\d+)$/.exec(id);
      if (m) max = Math.max(max, Number(m[1]));
    }
  }
  return xorBranchBlockId(xorId, max + 1);
}

export function toCanvasType(type: string): string {
  if (type === 'oa.start') return CANVAS_TYPE.start;
  if (type === 'oa.end') return CANVAS_TYPE.end;
  return type;
}

export function toOaType(type: string): string {
  if (type === CANVAS_TYPE.start) return 'oa.start';
  if (type === CANVAS_TYPE.end) return 'oa.end';
  return type;
}

export function defaultNodeDataFor(type: string): Record<string, unknown> {
  switch (type) {
    case 'oa.start':
      return { name: '开始' };
    case 'oa.approve':
      return {
        name: '审批',
        mode: 'or',
        to: wfRecipientToJson(undefined, [], 'users'),
        fields: { visible: ['*'], writable: [] },
        timeoutHours: 0,
        timeoutAction: 'pass',
        allowAddSign: true,
        allowRollback: true,
        allowTransfer: true,
        emptyPolicy: 'manager',
      };
    case 'oa.handle':
      return {
        name: '办理',
        mode: 'or',
        to: wfRecipientToJson(undefined, [], 'users'),
        emptyPolicy: 'manager',
      };
    case 'oa.cc':
      return { name: '知会', to: wfRecipientToJson(undefined, [], 'users') };
    case 'oa.xor':
      return { name: '条件分流', cases: [], defaultTarget: '' };
    case 'oa.parallel':
      return { name: '并行分支', cases: [], defaultTarget: '', joinTarget: '' };
    case 'oa.end':
      return { name: '结束' };
    default:
      return { name: '节点' };
  }
}

function succs(graph: WfGraphData, id: string): string[] {
  return (graph.edges ?? []).filter((e) => e.source === id).map((e) => e.target);
}

function reachable(graph: WfGraphData, start: string, banned: Set<string>): Set<string> {
  const hit = new Set<string>();
  const q = [start];
  while (q.length) {
    const id = q.pop()!;
    if (!id || hit.has(id) || banned.has(id)) continue;
    hit.add(id);
    for (const s of succs(graph, id)) q.push(s);
  }
  return hit;
}

/** 多分支汇合点：从首支沿链走，第一个出现在其它所有分支可达集的节点；兜底取第二条链起点 */
export function xorJoinId(graph: WfGraphData, xor: WfGraphNodeData): string | undefined {
  const cases = xorCaseList(xor.data).map((c) => c.target).filter(Boolean);
  const elseStart = String(xor.data?.defaultTarget ?? '').trim() || succs(graph, xor.id)[0];
  const unique = [...new Set([...cases, elseStart].filter(Boolean))];
  if (!unique.length) return undefined;
  if (unique.length === 1) return unique[0];
  const others = unique.slice(1).map((s) => reachable(graph, s, new Set([xor.id])));
  const seen = new Set<string>();
  let cur: string | undefined = unique[0];
  while (cur && !seen.has(cur)) {
    if (others.every((rs) => rs.has(cur!))) return cur;
    seen.add(cur);
    cur = succs(graph, cur)[0];
  }
  return unique[1];
}

function emitChain(
  graph: WfGraphData,
  startId: string | undefined,
  stopId: string | undefined,
  visited: Set<string>,
): FlowDocNode[] {
  const byId = new Map(graph.nodes.map((n) => [n.id, n]));
  const out: FlowDocNode[] = [];
  let cur = startId;
  while (cur && cur !== stopId && !visited.has(cur)) {
    const node = byId.get(cur);
    if (!node) break;
    visited.add(cur);
    if (isSplitNodeType(node.type)) {
      const join = xorJoinId(graph, node);
      const caseList = xorCaseList(node.data);
      const elseStart = String(node.data?.defaultTarget ?? '').trim();
      const blocks: FlowDocNode[] = [];
      // 保留原下标：target 为空的条件支也要占位，否则块序号与 cases 错位
      caseList.forEach((c, i) => {
        const start = String(c.target ?? '').trim();
        blocks.push({
          id: xorBranchBlockId(node.id, i),
          type: 'block',
          data: { title: `条件${i + 1}`, filter: c.filter, name: c.name },
          blocks: emitChain(graph, start && start !== join ? start : undefined, join, visited),
        });
      });
      blocks.push({
        id: xorBranchBlockId(node.id, -1),
        type: 'block',
        data: { title: '其他情况' },
        blocks: emitChain(graph, elseStart && elseStart !== join ? elseStart : undefined, join, visited),
      });
      out.push({ id: node.id, type: node.type, data: node.data ?? {}, blocks });
      cur = join;
      continue;
    }
    out.push({ id: node.id, type: toCanvasType(node.type), data: node.data ?? {} });
    cur = succs(graph, cur)[0];
  }
  return out;
}

export function graphToFlowDoc(graph: WfGraphData | null | undefined): FlowDoc {
  if (!graph?.nodes?.length) return { version: 1, nodes: [] };
  const start = graph.nodes.find((n) => n.type === 'oa.start') ?? graph.nodes[0];
  return { version: graph.version ?? 1, nodes: emitChain(graph, start.id, undefined, new Set()) };
}

function firstEntryId(n: FlowDocNode | undefined): string | undefined {
  if (!n) return undefined;
  const t = String(n.type);
  if (t === 'block') {
    const kids = n.blocks ?? [];
    return firstEntryId(kids[0]);
  }
  if (isBusinessNodeType(t)) return n.id;
  return firstEntryId((n.blocks ?? [])[0]);
}

function collectBusiness(nodes: FlowDocNode[] | undefined): WfGraphNodeData[] {
  const out: WfGraphNodeData[] = [];
  for (const n of nodes ?? []) {
    const t = String(n.type);
    if (t === 'block' || isXorBranchBlock(n.id)) {
      out.push(...collectBusiness(n.blocks));
      continue;
    }
    if (!isBusinessNodeType(t)) continue;
    out.push({ id: n.id, type: toOaType(t), data: n.data ?? {} });
    if (isSplitNodeType(t)) out.push(...collectBusiness(n.blocks));
  }
  return out;
}

function nextEntryId(nodes: FlowDocNode[], from: number, joinId: string | undefined): string | undefined {
  for (let j = from + 1; j < nodes.length; j++) {
    const id = firstEntryId(nodes[j]);
    if (id) return id;
  }
  return joinId;
}

function chainEdges(nodes: FlowDocNode[], joinId: string | undefined): { source: string; target: string }[] {
  const edges: { source: string; target: string }[] = [];
  for (let i = 0; i < nodes.length; i++) {
    const n = nodes[i];
    const nextId = nextEntryId(nodes, i, joinId);
    const t = String(n.type);
    if (t === 'block') {
      edges.push(...chainEdges(n.blocks ?? [], joinId));
      continue;
    }
    if (isSplitNodeType(t)) {
      for (const b of n.blocks ?? []) {
        const chain = b.blocks ?? [];
        const entry = firstEntryId(chain[0]);
        if (entry) edges.push({ source: n.id, target: entry });
        else if (nextId) edges.push({ source: n.id, target: nextId });
        edges.push(...chainEdges(chain, nextId));
      }
      continue;
    }
    if (isBusinessNodeType(t) && nextId && n.id !== nextId) edges.push({ source: n.id, target: nextId });
  }
  return edges;
}

function patchXorData(n: FlowDocNode, joinId: string | undefined): Record<string, unknown> {
  const data = { ...(n.data ?? {}) };
  const branchBlocks = (n.blocks ?? []).filter((b) => !b.type || String(b.type) === 'block');
  const caseBlocks = branchBlocks.slice(0, -1);
  const elseBlock = branchBlocks.length ? branchBlocks[branchBlocks.length - 1] : undefined;
  const caseTargets = caseBlocks.map((b) => firstEntryId(b.blocks?.[0]) || '');
  const elseEntry = firstEntryId(elseBlock?.blocks?.[0]);
  // filter/则去目标以块上数据为准（画布内条件编辑经 patchBranch 同步到块，不随分支增删错位）
  data.cases = caseTargets.map((t, i) => {
    const bd = caseBlocks[i]?.data as Record<string, unknown> | undefined;
    const cached = String(bd?.target ?? '').trim();
    return { filter: bd?.filter, target: t || cached || '', name: bd?.name };
  });
  data.defaultTarget = elseEntry || joinId || String(data.defaultTarget ?? '');
  if (String(n.type) === 'oa.parallel') data.joinTarget = joinId || String(data.joinTarget ?? '');
  return data;
}

function applyXorPatches(nodes: FlowDocNode[], joinId?: string): FlowDocNode[] {
  return nodes.map((n, i) => {
    const nextJoin = firstEntryId(nodes[i + 1]) || joinId;
    if (isSplitNodeType(String(n.type))) {
      return {
        ...n,
        data: patchXorData(n, nextJoin),
        blocks: (n.blocks ?? []).map((b) => ({
          ...b,
          blocks: applyXorPatches(b.blocks ?? [], nextJoin),
        })),
      };
    }
    if (String(n.type) === 'block') {
      return { ...n, blocks: applyXorPatches(n.blocks ?? [], joinId) };
    }
    return n;
  });
}

export function flowDocToGraph(doc: FlowDoc | null | undefined): WfGraphData {
  const patched = applyXorPatches(doc?.nodes ?? []);
  const nodes = collectBusiness(patched);
  const seen = new Set<string>();
  const uniq = nodes.filter((n) => (seen.has(n.id) ? false : (seen.add(n.id), true)));
  const edges = chainEdges(patched, undefined);
  return { version: doc?.version ?? 1, nodes: uniq, edges };
}

export function flowNodeTitle(type: string, data: Record<string, unknown> | undefined): string {
  const name = (data?.name as string) || '';
  const label =
    type === 'oa.start'
      ? '开始'
      : type === 'oa.end'
        ? '结束'
        : type === 'oa.cc'
          ? '知会'
          : type === 'oa.xor'
            ? '条件分流'
            : type === 'oa.parallel'
              ? '并行分支'
              : type === 'oa.approve'
              ? '审批'
              : type === 'oa.handle'
                ? '办理'
                : type;
  return name ? `${label} · ${name}` : label;
}

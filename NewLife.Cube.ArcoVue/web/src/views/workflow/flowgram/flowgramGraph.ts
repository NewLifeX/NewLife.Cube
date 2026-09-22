import type { WfGraphData, WfGraphNodeData } from '@/core/types/workflow';
import { wfRecipientToJson } from '@/core/types/workflow';
import { xorCaseList } from '../wfNodeCard';

/**
 * 后端 GraphJson（扁平 nodes+edges）↔ FlowGram 固定布局文档（可嵌套 blocks）。
 * oa.xor 映射为二分支：满足（首条 case）/ 不满足（defaultTarget），对齐飞书条件判断画布。
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

const BUSINESS_TYPES = new Set(['oa.start', 'oa.end', 'oa.approve', 'oa.cc', 'oa.xor', 'start', 'end']);

export function isBusinessNodeType(type: string): boolean {
  return BUSINESS_TYPES.has(type) || type.startsWith('oa.');
}

export function isXorBranchBlock(id: string): boolean {
  return id.endsWith('__if') || id.endsWith('__else');
}

export function xorIfBlockId(xorId: string): string {
  return `${xorId}__if`;
}

export function xorElseBlockId(xorId: string): string {
  return `${xorId}__else`;
}

/** 插入条件分流时的两个空分支槽 */
export function xorSplitBlocks(xorId: string): FlowDocNode[] {
  return [
    { id: xorIfBlockId(xorId), type: 'block', data: { title: '满足' }, blocks: [] },
    { id: xorElseBlockId(xorId), type: 'block', data: { title: '不满足' }, blocks: [] },
  ];
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
      };
    case 'oa.cc':
      return { name: '知会', to: wfRecipientToJson(undefined, [], 'users') };
    case 'oa.xor':
      return { name: '条件分流', cases: [], defaultTarget: '' };
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

/** 两分支汇合点：满足链上第一个也出现在不满足可达集中的节点 */
export function xorJoinId(graph: WfGraphData, xor: WfGraphNodeData): string | undefined {
  const cases = xorCaseList(xor.data);
  const ifStart = cases.map((c) => c.target).find(Boolean);
  const elseStart = String(xor.data?.defaultTarget ?? '').trim() || succs(graph, xor.id)[0];
  if (!ifStart) return elseStart;
  if (!elseStart || ifStart === elseStart) return ifStart;
  const rElse = reachable(graph, elseStart, new Set([xor.id]));
  const seen = new Set<string>();
  let cur: string | undefined = ifStart;
  while (cur && !seen.has(cur)) {
    if (rElse.has(cur)) return cur;
    seen.add(cur);
    cur = succs(graph, cur)[0];
  }
  return elseStart;
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
    if (node.type === 'oa.xor') {
      const join = xorJoinId(graph, node);
      const ifStart = xorCaseList(node.data).map((c) => c.target).find(Boolean);
      const elseStart = String(node.data?.defaultTarget ?? '').trim();
      out.push({
        id: node.id,
        type: 'oa.xor',
        data: node.data ?? {},
        blocks: [
          {
            id: xorIfBlockId(node.id),
            type: 'block',
            data: { title: '满足' },
            blocks: emitChain(graph, ifStart && ifStart !== join ? ifStart : undefined, join, visited),
          },
          {
            id: xorElseBlockId(node.id),
            type: 'block',
            data: { title: '不满足' },
            blocks: emitChain(graph, elseStart && elseStart !== join ? elseStart : undefined, join, visited),
          },
        ],
      });
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
    if (t === 'oa.xor') out.push(...collectBusiness(n.blocks));
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
    if (t === 'oa.xor') {
      const ifChain = n.blocks?.[0]?.blocks ?? [];
      const elseChain = n.blocks?.[1]?.blocks ?? [];
      const ifEntry = firstEntryId(ifChain[0]);
      const elseEntry = firstEntryId(elseChain[0]);
      if (ifEntry) edges.push({ source: n.id, target: ifEntry });
      else if (nextId) edges.push({ source: n.id, target: nextId });
      if (elseEntry) edges.push({ source: n.id, target: elseEntry });
      else if (nextId) edges.push({ source: n.id, target: nextId });
      edges.push(...chainEdges(ifChain, nextId));
      edges.push(...chainEdges(elseChain, nextId));
      continue;
    }
    if (isBusinessNodeType(t) && nextId && n.id !== nextId) edges.push({ source: n.id, target: nextId });
  }
  return edges;
}

function patchXorData(n: FlowDocNode, joinId: string | undefined): Record<string, unknown> {
  const data = { ...(n.data ?? {}) };
  const ifEntry = firstEntryId(n.blocks?.[0]?.blocks?.[0]);
  const elseEntry = firstEntryId(n.blocks?.[1]?.blocks?.[0]);
  const old = xorCaseList(data);
  const ifTarget = ifEntry || '';
  if (old.length) {
    data.cases = old.map((c, i) => (i === 0 ? { filter: c.filter, target: ifTarget || c.target } : c));
  } else if (ifTarget) {
    data.cases = [{ target: ifTarget }];
  } else {
    data.cases = [];
  }
  data.defaultTarget = elseEntry || joinId || String(data.defaultTarget ?? '');
  return data;
}

function applyXorPatches(nodes: FlowDocNode[], joinId?: string): FlowDocNode[] {
  return nodes.map((n, i) => {
    const nextJoin = firstEntryId(nodes[i + 1]) || joinId;
    if (String(n.type) === 'oa.xor') {
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
            : type === 'oa.approve'
              ? '审批'
              : type;
  return name ? `${label} · ${name}` : label;
}

import type { WfGraphData, WfGraphNodeData } from '@/core/types/workflow';
import { wfRecipientToJson } from '@/core/types/workflow';
import { topoChain } from '../useWorkflowDesigner';

/**
 * 后端 GraphJson（扁平 nodes+edges）↔ FlowGram 固定布局文档（顺序 nodes 树）双向转换。
 * 固定布局以节点顺序表达流向（与自由布局的边不同），V1 流程为单链（可含 xor 普通节点），
 * 转换无损：链序 ↔ 相邻成边；节点 data 原样透传（未知字段保留）。
 */

/** FlowGram 文档节点（与 FlowDocumentJSON 节点同构的最小视图） */
export interface FlowDocNode {
  id: string;
  type: string;
  data?: Record<string, unknown>;
  meta?: Record<string, unknown>;
  blocks?: FlowDocNode[];
}

/** FlowGram 文档（固定布局为顺序节点数组） */
export interface FlowDoc {
  version?: number;
  nodes: FlowDocNode[];
}

/**
 * 画布类型映射：FlowGram 固定布局内置 start/end 语义节点（type='start'/'end'），
 * 业务中间节点沿用后端 oa.*（oa.approve/oa.cc/oa.xor）。
 */
export const CANVAS_TYPE = {
  start: 'start',
  end: 'end',
} as const;

/** 业务节点类型集（含画布 start/end 映射后仍属流程业务节点） */
const BUSINESS_TYPES = new Set(['oa.start', 'oa.end', 'oa.approve', 'oa.cc', 'oa.xor', 'start', 'end']);

/** 是否画布业务节点（过滤 FlowGram 内部虚拟 icon/block 节点） */
export function isBusinessNodeType(type: string): boolean {
  return BUSINESS_TYPES.has(type) || type.startsWith('oa.');
}

/** 后端 oa.* → 画布类型 */
export function toCanvasType(type: string): string {
  if (type === 'oa.start') return CANVAS_TYPE.start;
  if (type === 'oa.end') return CANVAS_TYPE.end;
  return type;
}

/** 画布类型 → 后端 oa.*（未知透传） */
export function toOaType(type: string): string {
  if (type === CANVAS_TYPE.start) return 'oa.start';
  if (type === CANVAS_TYPE.end) return 'oa.end';
  return type;
}

/** 新插入节点默认 data（画布 Adder 插入时使用，字段与后端 WorkflowNode 对齐） */
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

/** 后端图 → FlowGram 文档：拓扑链序平铺（V1 单链）；start/end 映射为画布内置类型 */
export function graphToFlowDoc(graph: WfGraphData | null | undefined): FlowDoc {
  const order = graph ? topoChain(graph) : [];
  return {
    version: 1,
    nodes: order.map((n) => ({
      id: n.id,
      type: toCanvasType(n.type),
      data: n.data ?? {},
    })),
  };
}

/** FlowGram 文档 → 后端图：过滤虚拟节点后按序相邻成边 */
export function flowDocToGraph(doc: FlowDoc | null | undefined): WfGraphData {
  const business = (doc?.nodes ?? []).filter((n) => isBusinessNodeType(String(n.type)));
  const nodes: WfGraphNodeData[] = business.map((n) => ({
    id: n.id,
    type: toOaType(String(n.type)),
    data: n.data ?? {},
  }));
  const edges = nodes
    .slice(1)
    .map((n, i) => ({ source: nodes[i].id, target: n.id }));
  return { version: doc?.version ?? 1, nodes, edges };
}

/** 节点类型中文（画布卡片标题） */
export function flowNodeTitle(type: string, data: Record<string, unknown> | undefined): string {
  const name = (data?.name as string) || '';
  const label = type === 'oa.start' ? '开始' : type === 'oa.end' ? '结束' : type === 'oa.cc' ? '知会' : type === 'oa.xor' ? '条件分流' : type === 'oa.approve' ? '审批' : type;
  return name ? `${label} · ${name}` : label;
}

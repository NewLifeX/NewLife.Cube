/**
 * 实体自动化 GraphJson 编译 / 反编译 / 触发配置归一（OSC-260815fa86）。
 * 与后端 AutomationGraph.Compile 同一算法：Start → [Filter] → Action* → End。
 */
import type { ViewFilter } from './viewProfile';

export const AUTOMATION_ACTION_TYPES = [
  'notify',
  'updateRecord',
  'createRecord',
  'findRecords',
  'httpRequest',
  'delay',
  'runAutomation',
  'addComment',
  'aiText',
] as const;

/** 编辑器「添加动作」菜单（不含已下线入口） */
export const AUTOMATION_MENU_ACTION_TYPES = [
  'notify',
  'updateRecord',
  'createRecord',
  'findRecords',
  'httpRequest',
  'delay',
  'addComment',
  'aiText',
] as const;

/** 自身写入会再引爆自动化：不挂钩记录增删改（CronJob 已改为仅跳过心跳字段） */
export const AUTOMATION_SKIP_TYPE_NAMES = ['EntityAutomation', 'NotificationRecord', 'EntityComment'] as const;

/** 当前实体路径是否属于循环跳过类型 */
export function isAutomationSkipTypePath(typePath: string | null | undefined): boolean {
  const last = (typePath || '').replace(/^\/+|\/+$/g, '').split('/').pop() || '';
  return AUTOMATION_SKIP_TYPE_NAMES.some((x) => x.toLowerCase() === last.toLowerCase());
}

export const AUTOMATION_TRIGGER_KINDS = [
  'insert',
  'update',
  'delete',
  'insertOrUpdateIf',
  'fieldChange',
  'dateArrive',
  'schedule',
  'button',
  'webhook',
] as const;

const ACTION_SET = new Set<string>(AUTOMATION_ACTION_TYPES);

export type AutomationActionType = (typeof AUTOMATION_ACTION_TYPES)[number];
export type AutomationTriggerKind = (typeof AUTOMATION_TRIGGER_KINDS)[number];

export interface ActionDraft {
  type: string;
  data?: Record<string, unknown>;
}

export interface AutomationGraphNode {
  id: string;
  type: string;
  data?: Record<string, unknown>;
}

export interface AutomationGraphEdge {
  id: string;
  source: string;
  target: string;
}

export interface AutomationGraphJson {
  version: number;
  nodes: AutomationGraphNode[];
  edges: AutomationGraphEdge[];
}

export interface CompileAutomationInput {
  triggerKind?: string;
  filter?: ViewFilter | null;
  actions?: ActionDraft[] | null;
}

/** 表单 → 线性 GraphJson。非法动作剔除；超过 20 截断。 */
export function compileAutomationGraph(input: CompileAutomationInput): AutomationGraphJson {
  const kind = (input.triggerKind ?? '').trim().toLowerCase();
  const nodes: AutomationGraphNode[] = [];
  const edges: AutomationGraphEdge[] = [];
  let n = 0;
  const add = (type: string, data: Record<string, unknown> = {}) => {
    const id = `n${n}`;
    if (n > 0) {
      edges.push({ id: `e${n - 1}`, source: `n${n - 1}`, target: id });
    }
    nodes.push({ id, type, data });
    n += 1;
  };
  add('start', { triggerKind: kind });
  const conds = input.filter?.conditions ?? [];
  if (conds.length > 0) {
    add('filter', { filter: input.filter as unknown as Record<string, unknown> });
  }
  let count = 0;
  for (const a of input.actions ?? []) {
    const t = (a.type ?? '').trim();
    if (!ACTION_SET.has(t)) continue;
    if (count >= 20) break;
    const data = { ...(a.data ?? {}) };
    if (t === 'delay') data.minutes = clampDelayMinutes(data.minutes);
    add(t, data);
    count += 1;
  }
  add('end', {});
  return { version: 1, nodes, edges };
}

export interface ParseAutomationResult {
  triggerKind: string;
  filter: ViewFilter;
  actions: ActionDraft[];
  version: number;
  error?: string;
}

/** 线性链还原 filter 与 actions；分叉/环返回 error。 */
export function parseAutomationGraph(graph: AutomationGraphJson | null | undefined): ParseAutomationResult {
  const empty: ViewFilter = { logic: 'all', conditions: [] };
  if (!graph || !Array.isArray(graph.nodes)) {
    return { triggerKind: '', filter: empty, actions: [], version: 1, error: '请用表单重建' };
  }
  const version = graph.version ?? 1;
  const nodes = graph.nodes;
  const edges = graph.edges ?? [];
  const outgoing = new Map<string, string[]>();
  for (const e of edges) {
    if (!e?.source || !e?.target) continue;
    const list = outgoing.get(e.source) ?? [];
    list.push(e.target);
    outgoing.set(e.source, list);
  }
  for (const [, list] of outgoing) {
    if (list.length > 1) {
      return { triggerKind: '', filter: empty, actions: [], version, error: '请用表单重建' };
    }
  }
  const start = nodes.find((n) => (n.type ?? '').toLowerCase() === 'start');
  if (!start) {
    return { triggerKind: '', filter: empty, actions: [], version, error: '请用表单重建' };
  }
  const map = new Map(nodes.map((n) => [n.id, n]));
  const chain: AutomationGraphNode[] = [];
  const seen = new Set<string>();
  let cur = start.id;
  while (cur && map.has(cur)) {
    if (seen.has(cur)) {
      return { triggerKind: '', filter: empty, actions: [], version, error: '请用表单重建' };
    }
    seen.add(cur);
    const node = map.get(cur)!;
    chain.push(node);
    const next = outgoing.get(cur)?.[0];
    if (!next) break;
    cur = next;
  }
  const triggerKind = String(start.data?.triggerKind ?? '').trim().toLowerCase();
  let filter: ViewFilter = empty;
  const actions: ActionDraft[] = [];
  for (const node of chain) {
    const t = (node.type ?? '').trim();
    if (t === 'start' || t === 'end') continue;
    if (t === 'filter') {
      const f = node.data?.filter as ViewFilter | undefined;
      if (f && Array.isArray(f.conditions)) {
        filter = { logic: f.logic === 'any' ? 'any' : 'all', conditions: f.conditions };
      }
      continue;
    }
    actions.push({ type: t, data: (node.data as Record<string, unknown>) ?? {} });
  }
  const error = version > 1 ? '不支持的图版本' : undefined;
  return { triggerKind, filter, actions, version, error };
}

/** 触发配置归一：kind 小写、offset 夹紧、label 截断 12、watchFields 去空。 */
export function normalizeTriggerConfig(
  kind: string,
  cfg: Record<string, unknown> | null | undefined,
): Record<string, unknown> {
  const k = (kind ?? '').trim().toLowerCase();
  const o: Record<string, unknown> = { ...(cfg ?? {}) };
  if (k === 'button') {
    let label = String(o.label ?? '').trim();
    if (label.length > 12) label = label.slice(0, 12);
    if (!label) label = '运行';
    o.label = label;
    const rp = String(o.requirePermission ?? '');
    o.requirePermission = rp.toLowerCase() === 'update' ? 'update' : 'detail';
  }
  if (k === 'datearrive' || k === 'dateArrive') {
    let off = Number(o.offsetMinutes ?? 0);
    if (!Number.isFinite(off)) off = 0;
    if (off < -10080) off = -10080;
    if (off > 10080) off = 10080;
    o.offsetMinutes = Math.trunc(off);
    o.once = o.once !== false;
  }
  if (k === 'fieldchange' || k === 'fieldChange') {
    const arr = Array.isArray(o.watchFields) ? o.watchFields : [];
    const clean: string[] = [];
    for (const x of arr) {
      const s = String(x ?? '').trim();
      if (s && clean.length < 32) clean.push(s);
    }
    o.watchFields = clean;
  }
  return o;
}

/** delay 分钟：1–10080 */
export function clampDelayMinutes(n: unknown): number {
  let m = Number(n);
  if (!Number.isFinite(m)) m = 1;
  m = Math.trunc(m);
  if (m < 1) m = 1;
  if (m > 10080) m = 10080;
  return m;
}

const FOUND_TARGET_ACTIONS = new Set(['updateRecord', 'notify', 'addComment']);

/**
 * 检查「目标=查找结果」是否前置了 findRecords。
 * 返回有问题的动作下标与说明。
 */
export function validateFoundTargetChain(
  actions: ActionDraft[],
  labels?: Record<string, string>,
): { index: number; message: string }[] {
  const issues: { index: number; message: string }[] = [];
  let lastFind = -1;
  actions.forEach((a, i) => {
    const t = (a.type ?? '').trim();
    if (t === 'findRecords') {
      lastFind = i;
      return;
    }
    if (!FOUND_TARGET_ACTIONS.has(t)) return;
    const target = String((a.data as { target?: string } | undefined)?.target ?? 'current');
    if (target !== 'found') return;
    if (lastFind < 0) {
      const name = labels?.[t] ?? t;
      issues.push({
        index: i,
        message: `「${name}」目标为查找结果时，前面必须先有「查找记录」动作`,
      });
    }
  });
  return issues;
}

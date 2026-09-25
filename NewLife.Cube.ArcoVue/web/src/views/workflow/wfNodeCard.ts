import {
  WF_TO_KIND_LABEL,
  wfNodeTypeLabel,
  wfRecipientTo,
  wfToKindOf,
  type WfGraphData,
  type WfGraphNodeData,
  type WfToKind,
} from '@/core/types/workflow';
import { type FilterValueLabel, viewFilterHasRules, viewFilterSummary } from './wfFilterText';
import type { ViewFilter } from '@/core/utils/viewProfile';

/** 分流分支块 id 后缀：__if / __caseN / __else（画布块 id = splitId + 后缀） */
export function isXorBranchBlock(id: string): boolean {
  return id.endsWith('__if') || id.endsWith('__else') || /__case\d+$/.test(id);
}

/** 分支块 id：第 1 支 __if（兼容旧图）、后续 __case{i}、末支 __else（其他情况） */
export function xorBranchBlockId(xorId: string, index: number): string {
  if (index < 0) return `${xorId}__else`;
  return index === 0 ? `${xorId}__if` : `${xorId}__case${index}`;
}

/** 画布卡片展示（D3）：一眼看出谁批、什么方式、有没有配错 */
export interface WfNodeCardInfo {
  title: string;
  badges: string[];
  subtitle: string;
  warning: boolean;
  /** 标题行签核方式 */
  modeLabel?: string;
  /** 内容行接收人 */
  assignee?: string;
  /** 分流卡片：分支条数与其他情况 */
  branchLine?: string;
}

const MODE_BADGE: Record<string, string> = {
  or: '或签',
  and: '会签',
  sequence: '依次',
};

const TIMEOUT_ACTION: Record<string, string> = {
  pass: '自动通过',
  reject: '自动驳回',
  transfer: '自动转交',
};

/** 读取节点上缓存的接收人显示名（添加/编辑时写入 data.toLabels） */
export function recipientLabelsOf(data: Record<string, unknown> | undefined): string[] {
  const raw = data?.toLabels;
  if (!Array.isArray(raw)) return [];
  return raw.map(String).filter(Boolean);
}

/**
 * 接收人一行摘要（六种选人）：优先用 toLabels（如「管理员」「分管经理」），
 * 否则按类别回落（部门负责人/提交人自选/指定角色/未指定 等）。
 */
export function recipientSummary(to: unknown, labels?: string[]): string {
  const names = (labels ?? []).filter(Boolean);
  if (names.length === 1) return names[0]!;
  if (names.length > 1) {
    const head = names.slice(0, 2).join('、');
    return names.length > 2 ? `${head}等${names.length}人` : head;
  }
  const kind = wfToKindOf(to);
  if (kind === 'manager') return '部门负责人';
  if (kind === 'starterPick') return '提交人自选';
  if (kind === 'field') {
    const f = String((to as Record<string, unknown> | null)?.field ?? '').trim();
    return f || '未指定字段';
  }
  const r = wfRecipientTo(to);
  if (!r.ids.length) return '未指定';
  if (r.kind === 'roles') return r.ids.length === 1 ? '指定角色' : `角色 · ${r.ids.length}`;
  if (r.kind === 'departments') return r.ids.length === 1 ? '指定部门' : `部门 · ${r.ids.length}`;
  return r.ids.length === 1 ? '指定人员' : `${r.ids.length} 人`;
}

const ASSIGNEE_PREFIX: Record<WfToKind, string> = {
  users: '人员',
  roles: '角色',
  departments: '部门',
  manager: '管理人员',
  starterPick: '提交人自选',
  field: '表单人员',
};

/** 查看流程 / 设计器内容区：人员、角色、部门、管理人员 */
export function nodeAssigneeLine(data: Record<string, unknown> | undefined): string {
  const to = data?.to;
  const kind = wfToKindOf(to);
  const labels = recipientLabelsOf(data);
  if (kind === 'manager') return '管理人员：部门负责人';
  if (kind === 'starterPick') return '人员：提交人自选';
  const prefix = ASSIGNEE_PREFIX[kind];
  if (labels.length) return `${prefix}：${labels.join('、')}`;
  const who = recipientSummary(to);
  if (who === '未指定' || who === '未指定字段') return `${prefix}：未指定`;
  return `${prefix}：${who}`;
}

export function xorCaseList(
  data: Record<string, unknown> | undefined,
): { filter: unknown; target: string; name: string }[] {
  const raw = data?.cases;
  if (!Array.isArray(raw)) return [];
  return raw.map((c) => {
    const o = (c ?? {}) as Record<string, unknown>;
    return { filter: o.filter, target: String(o.target ?? ''), name: String(o.name ?? '') };
  });
}

/** 分流节点出边目标：默认分支优先，便于主链走 default */
export function xorTargetsOf(node: WfGraphNodeData): string[] {
  const def = String(node.data?.defaultTarget ?? '').trim();
  const cases = xorCaseList(node.data).map((c) => c.target).filter(Boolean);
  const out: string[] = [];
  if (def) out.push(def);
  for (const t of cases) if (!out.includes(t)) out.push(t);
  return out;
}

/**
 * 添加节点时生成卡片标题（设计 §4.1 人话格式）：
 * 「管理员 · 或签」「部门负责人 · 会签」「张三、李四等3人 · 依次」；知会/办理另行前缀。
 */
export function insertNodeTitle(
  type: 'oa.approve' | 'oa.handle' | 'oa.cc',
  mode: string,
  kind: WfToKind,
  labels: string[],
): string {
  const who = labels.length
    ? labels.length > 2
      ? `${labels.slice(0, 2).join('、')}等${labels.length}人`
      : labels.join('/')
    : WF_TO_KIND_LABEL[kind] || '未指定';
  if (type === 'oa.cc') return `知会·${who}`;
  if (type === 'oa.handle') return `办理·${who}`;
  const m = mode === 'and' ? '会签' : mode === 'sequence' ? '依次' : '或签';
  return `${who} · ${m}`;
}

export function nodeCardInfo(
  node: WfGraphNodeData,
  opts?: { hasStartFilter?: boolean; graph?: WfGraphData | null },
): WfNodeCardInfo {
  const name = String(node.data?.name ?? '').trim();
  const kind = wfNodeTypeLabel(node.type);
  const title = name && name !== kind ? name : kind;
  const badges: string[] = [];
  let subtitle = '';
  let warning = false;
  let modeLabel = '';
  let assignee = '';
  let branchLine = '';
  const labels = recipientLabelsOf(node.data);

  if (node.type === 'oa.start') {
    if (opts?.hasStartFilter) badges.push('有发起条件');
  } else if (node.type === 'oa.approve' || node.type === 'oa.handle') {
    const mode = String(node.data?.mode ?? 'or');
    const modeWord = MODE_BADGE[mode] || mode;
    const toKind = wfToKindOf(node.data?.to);
    if (node.type === 'oa.approve') badges.push(modeWord);
    if (node.type === 'oa.approve' || node.type === 'oa.handle') modeLabel = modeWord;
    assignee = nodeAssigneeLine(node.data);
    badges.push(WF_TO_KIND_LABEL[toKind] || '用户');
    const who = recipientSummary(node.data?.to, labels);
    if (who === '未指定' || who === '未指定字段') warning = true;
    subtitle = node.type === 'oa.approve' ? `${who} · ${modeWord}` : who;
    if (node.type === 'oa.approve') {
      const hours = Number(node.data?.timeoutHours ?? 0) || 0;
      if (hours > 0) {
        const act = TIMEOUT_ACTION[String(node.data?.timeoutAction ?? 'pass')] || '超时处理';
        subtitle = subtitle ? `${subtitle} · ${hours}h ${act}` : `${hours}h ${act}`;
      }
    }
  } else if (node.type === 'oa.cc') {
    badges.push('知会');
    const toKind = wfToKindOf(node.data?.to);
    badges.push(WF_TO_KIND_LABEL[toKind] || '用户');
    const who = recipientSummary(node.data?.to, labels);
    subtitle = who;
    assignee = nodeAssigneeLine(node.data);
    if (who === '未指定' || who === '未指定字段') warning = true;
  } else if (node.type === 'oa.xor' || node.type === 'oa.parallel') {
    const def = String(node.data?.defaultTarget ?? '').trim();
    const caseCount = xorCaseList(node.data).length;
    const total = caseCount + 1;
    badges.push(caseCount > 0 ? `${caseCount} 个条件` : node.type === 'oa.parallel' ? '并行分支' : '条件分流');
    if (!def) {
      warning = true;
      subtitle = '缺「其他情况」分支，无法发布';
      branchLine = `${total} 条分支 · 缺其他情况`;
    } else {
      const label = opts?.graph?.nodes.find((n) => n.id === def);
      subtitle = `其他情况 → ${String(label?.data?.name || def)}`;
      branchLine = `${total} 条分支 · 其他情况已接上`;
    }
  }

  return { title, badges, subtitle, warning, modeLabel, assignee, branchLine };
}

export function cardsOf(
  graph: WfGraphData | null | undefined,
  hasStartFilter = false,
): Record<string, WfNodeCardInfo> {
  const m: Record<string, WfNodeCardInfo> = {};
  for (const n of graph?.nodes ?? []) m[n.id] = nodeCardInfo(n, { hasStartFilter, graph });
  return m;
}

/** 画布分支卡片摘要（按块物理顺序：条件1…条件N、其他情况） */
export interface WfBranchInfo {
  /** 卡片标题：用户自定义条件名，未命名时“条件N”（其他情况固定） */
  title: string;
  subtitle: string;
  warning: boolean;
}

/** 每个分流节点的分支摘要表：splitId → 分支列表（与块物理顺序一致） */
export function branchInfosOf(
  graph: WfGraphData | null | undefined,
  fieldLabel?: (name: string) => string,
  valueLabel?: FilterValueLabel,
): Record<string, WfBranchInfo[]> {
  const m: Record<string, WfBranchInfo[]> = {};
  for (const n of graph?.nodes ?? []) {
    if (n.type !== 'oa.xor' && n.type !== 'oa.parallel') continue;
    const cases = xorCaseList(n.data);
    const list: WfBranchInfo[] = cases.map((c, i) => {
      const has = viewFilterHasRules(c.filter as ViewFilter | null | undefined);
      return {
        title: c.name.trim() || `条件${i + 1}`,
        subtitle: has ? viewFilterSummary(c.filter, fieldLabel, valueLabel) : n.type === 'oa.parallel' ? '无条件' : '未设置条件',
        warning: !c.target,
      };
    });
    const def = String(n.data?.defaultTarget ?? '').trim();
    const defName = def ? String(graph?.nodes.find((x) => x.id === def)?.data?.name || def) : '';
    list.push({
      title: '其他情况',
      subtitle: def ? `都不命中 → ${defName}` : '未指定默认节点',
      warning: !def,
    });
    m[n.id] = list;
  }
  return m;
}

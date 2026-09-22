import { wfNodeTypeLabel, wfRecipientTo, type WfGraphData, type WfGraphNodeData } from '@/core/types/workflow';

/** 画布卡片展示（D3）：一眼看出谁批、什么方式、有没有配错 */
export interface WfNodeCardInfo {
  title: string;
  badges: string[];
  subtitle: string;
  warning: boolean;
}

const MODE_BADGE: Record<string, string> = {
  or: '或签',
  and: '会签',
  sequence: '依次', // 历史图兼容展示；设计器已不再可选
};

const KIND_BADGE: Record<string, string> = {
  users: '用户',
  roles: '角色',
  departments: '部门',
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
 * 接收人一行摘要：优先用 toLabels（如「管理员」），否则回落类别粗摘要。
 */
export function recipientSummary(to: unknown, labels?: string[]): string {
  const names = (labels ?? []).filter(Boolean);
  if (names.length === 1) return names[0]!;
  if (names.length > 1) {
    const head = names.slice(0, 2).join('、');
    return names.length > 2 ? `${head}等${names.length}个` : head;
  }
  const r = wfRecipientTo(to);
  if (!r.ids.length) return '未指定';
  if (r.kind === 'roles') return r.ids.length === 1 ? '指定角色' : `角色 · ${r.ids.length}`;
  if (r.kind === 'departments') return r.ids.length === 1 ? '指定部门' : `部门 · ${r.ids.length}`;
  return r.ids.length === 1 ? '指定人员' : `${r.ids.length} 人`;
}

export function xorCaseList(data: Record<string, unknown> | undefined): { filter: unknown; target: string }[] {
  const raw = data?.cases;
  if (!Array.isArray(raw)) return [];
  return raw.map((c) => {
    const o = (c ?? {}) as Record<string, unknown>;
    return { filter: o.filter, target: String(o.target ?? '') };
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

/** 添加节点时生成卡片标题：或签·管理员 / 知会·张三 */
export function insertNodeTitle(
  type: 'oa.approve' | 'oa.cc',
  mode: string,
  kind: string,
  labels: string[],
): string {
  const who = labels.length ? labels.join('/') : KIND_BADGE[kind] || '未指定';
  if (type === 'oa.cc') return `知会·${who}`;
  const m = mode === 'and' ? '会签' : '或签';
  return `${m}·${who}`;
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
  const labels = recipientLabelsOf(node.data);

  if (node.type === 'oa.start') {
    if (opts?.hasStartFilter) badges.push('有发起条件');
  } else if (node.type === 'oa.approve') {
    const mode = String(node.data?.mode ?? 'or');
    badges.push(MODE_BADGE[mode] || mode);
    const rec = wfRecipientTo(node.data?.to);
    badges.push(KIND_BADGE[rec.kind] || '用户');
    const who = recipientSummary(node.data?.to, labels);
    if (who === '未指定') warning = true;
    subtitle = who;
    const hours = Number(node.data?.timeoutHours ?? 0) || 0;
    if (hours > 0) {
      const act = TIMEOUT_ACTION[String(node.data?.timeoutAction ?? 'pass')] || '超时处理';
      subtitle = subtitle ? `${subtitle} · ${hours}h ${act}` : `${hours}h ${act}`;
    }
  } else if (node.type === 'oa.cc') {
    badges.push('知会');
    const rec = wfRecipientTo(node.data?.to);
    badges.push(KIND_BADGE[rec.kind] || '用户');
    const who = recipientSummary(node.data?.to, labels);
    subtitle = who;
    if (who === '未指定') warning = true;
  } else if (node.type === 'oa.xor') {
    const def = String(node.data?.defaultTarget ?? '').trim();
    badges.push('满足 / 不满足');
    if (!def) {
      warning = true;
      subtitle = '缺「不满足」分支，无法发布';
    } else {
      const label = opts?.graph?.nodes.find((n) => n.id === def);
      subtitle = `不满足 → ${String(label?.data?.name || def)}`;
    }
  }

  return { title, badges, subtitle, warning };
}

export function cardsOf(
  graph: WfGraphData | null | undefined,
  hasStartFilter = false,
): Record<string, WfNodeCardInfo> {
  const m: Record<string, WfNodeCardInfo> = {};
  for (const n of graph?.nodes ?? []) m[n.id] = nodeCardInfo(n, { hasStartFilter, graph });
  return m;
}

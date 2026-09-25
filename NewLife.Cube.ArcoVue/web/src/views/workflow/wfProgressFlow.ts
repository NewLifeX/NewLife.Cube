import type { WorkflowInstanceDetail, WorkflowTaskItem } from '@newlifex/api-core';
import type { WfGraphData, WfGraphNodeData } from '@/core/types/workflow';
import { wfNodeTypeLabel } from '@/core/types/workflow';
import { parseGraph } from './useWorkflowDesigner';
import { nodeAssigneeLine, xorCaseList } from './wfNodeCard';
import { modeLabel, taskStatusMeta } from './useWorkflowTaskList';
import { viewFilterSummary } from './wfFilterText';

/** 意见附件（进度按意见列出；key=实例Id:c意见Id） */
export interface ProgressAttachment {
  id: string;
  fileName: string;
  url?: string;
}

export interface ProgressPerson {
  key: string;
  label: string;
  status: string;
  statusText: string;
  statusColor: string;
  comment?: string;
  time?: string;
  /** 本条意见/任务下挂的附件 */
  attachments?: ProgressAttachment[];
}

export interface ProgressFlowNode {
  key: string;
  nodeId: string;
  type: string;
  title: string;
  badges: string[];
  current: boolean;
  hint?: string;
  people: ProgressPerson[];
}

export interface RollbackTarget {
  nodeId: string;
  name: string;
}

function baseNodeId(nodeId: string | undefined): string {
  if (!nodeId) return '';
  const i = nodeId.indexOf('#');
  return i < 0 ? nodeId : nodeId.slice(0, i);
}

/** 解析图快照 JSON（实例 GraphSnapshot / 定义 PublishedGraphJson，或设计器 GraphJson） */
export function parseSnapshot(json: string | undefined): WfGraphData | null {
  if (!json) return null;
  const g = parseGraph(json);
  return g.nodes.length ? g : null;
}

function nodeTitle(node: WfGraphNodeData | undefined, fallbackId: string): string {
  if (!node) return fallbackId;
  const name = String(node.data?.name ?? '').trim();
  const kind = wfNodeTypeLabel(node.type);
  return name && name !== kind ? name : kind;
}

/** 附件归类：发起附件（key=实例Id）、按意见（实例Id:c意见Id）、按任务（实例Id:任务Id，历史兼容） */
export function splitAttachments(
  attachments: WorkflowInstanceDetail['attachments'],
  instanceId: string,
): {
  starter: ProgressAttachment[];
  byComment: Map<string, ProgressAttachment[]>;
  byTask: Map<string, ProgressAttachment[]>;
} {
  const starter: ProgressAttachment[] = [];
  const byComment = new Map<string, ProgressAttachment[]>();
  const byTask = new Map<string, ProgressAttachment[]>();
  const push = (m: Map<string, ProgressAttachment[]>, k: string, v: ProgressAttachment) => {
    const list = m.get(k);
    if (list) list.push(v);
    else m.set(k, [v]);
  };
  for (const a of attachments ?? []) {
    const item: ProgressAttachment = {
      id: String(a.id),
      fileName: String(a.fileName || a.title || a.id),
      url: a.url,
    };
    const key = String(a.key ?? '');
    if (!key || key === instanceId) {
      starter.push(item);
      continue;
    }
    const rest = key.startsWith(instanceId + ':') ? key.slice(instanceId.length + 1) : '';
    if (!rest) {
      starter.push(item);
      continue;
    }
    if (rest.startsWith('c')) push(byComment, rest.slice(1), item);
    else push(byTask, rest, item);
  }
  return { starter, byComment, byTask };
}

/** 可达节点集合（含自身；不含 xor 自身） */
function reachableIds(graph: WfGraphData, startId: string, banned: Set<string>): Set<string> {
  const hit = new Set<string>();
  const q = [startId];
  while (q.length) {
    const id = q.pop()!;
    if (!id || hit.has(id) || banned.has(id)) continue;
    hit.add(id);
    for (const e of graph.edges ?? []) if (e.source === id) q.push(e.target);
  }
  return hit;
}

/** XOR 命中分支（证据：分支链上出现过任务）；无证据返回末支「其他情况」且 visited=false */
export function xorHitBranch(
  graph: WfGraphData,
  xor: WfGraphNodeData,
  taskNodeIds: Set<string>,
): { id: string; visited: boolean } {
  const cases = xorCaseList(xor.data).map((c) => c.target).filter(Boolean);
  const def = String(xor.data?.defaultTarget ?? '').trim();
  const branches = [...cases, def].filter(Boolean);
  const banned = new Set([xor.id]);
  for (const t of branches) {
    if (taskNodeIds.has(t) || [...reachableIds(graph, t, banned)].some((id) => taskNodeIds.has(id))) {
      return { id: t, visited: true };
    }
  }
  return { id: def, visited: false };
}

/** 并行分支里实际进入的入口：链上出现过任务的分支；都没有则只留「其他情况」（且它不是汇合点） */
export function parallelEnteredArms(
  graph: WfGraphData,
  node: WfGraphNodeData,
  taskNodeIds: Set<string>,
): string[] {
  const join = String(node.data?.joinTarget ?? '').trim();
  const cases = xorCaseList(node.data).map((c) => c.target).filter((t) => t && t !== join);
  const banned = new Set([node.id, join].filter(Boolean));
  const entered = cases.filter(
    (t) => taskNodeIds.has(t) || [...reachableIds(graph, t, banned)].some((id) => taskNodeIds.has(id)),
  );
  if (entered.length) return entered;
  const def = String(node.data?.defaultTarget ?? '').trim();
  return def && def !== join ? [def] : [];
}

/** 执行路径（隐藏未命中分支）：普通节点沿首后继；XOR 走命中分支；并行画出进入的各支再汇合 */
export function executionOrder(graph: WfGraphData | null, tasks: WorkflowTaskItem[]): WfGraphNodeData[] {
  if (!graph) return [];
  const byId = new Map(graph.nodes.map((n) => [n.id, n] as const));
  const taskNodeIds = new Set(tasks.map((t) => baseNodeId(t.nodeId)).filter(Boolean));
  const visited = new Set<string>();
  const out: WfGraphNodeData[] = [];
  let cur: string | undefined = graph.nodes.find((n) => n.type === 'oa.start')?.id;
  while (cur && !visited.has(cur)) {
    visited.add(cur);
    const node = byId.get(cur);
    if (!node) break;
    out.push(node);
    if (node.type === 'oa.xor' || node.type === 'oa.parallel') {
      cur = node.type === 'oa.xor' ? xorHitBranch(graph, node, taskNodeIds).id : undefined;
      if (node.type === 'oa.parallel') {
        const join = String(node.data?.joinTarget ?? '').trim();
        for (const arm of parallelEnteredArms(graph, node, taskNodeIds)) {
          let step: string | undefined = arm;
          while (step && step !== join && !visited.has(step)) {
            visited.add(step);
            const child = byId.get(step);
            if (!child) break;
            out.push(child);
            step = (graph.edges ?? []).find((e) => e.source === step)?.target;
          }
        }
        cur = join || undefined;
        continue;
      }
      continue;
    }
    cur = (graph.edges ?? []).find((e) => e.source === cur)?.target;
  }
  return out;
}

function commentsOf(
  detail: WorkflowInstanceDetail,
  taskIds: Set<string>,
  alsoActions: string[] = [],
): { action: string; user: string; time?: string; content?: string }[] {
  const acts = new Set(alsoActions.map((a) => a.toLowerCase()));
  return (detail.comments ?? []).filter((c) => {
    const tid = c.taskId != null ? String(c.taskId) : '';
    if (tid && taskIds.has(tid)) return true;
    if (!tid && acts.has(String(c.action ?? '').toLowerCase())) return true;
    return false;
  }).map((c) => ({
    action: String(c.action ?? ''),
    user: c.createUser || '',
    time: c.createTime,
    content: c.content || undefined,
  }));
}

/** 任务 → 参与人（含意见与附件：优先按意见 key，兼容历史按任务 key） */
function personFromTask(
  t: WorkflowTaskItem,
  comments: WorkflowInstanceDetail['comments'],
  byComment: Map<string, ProgressAttachment[]>,
  byTask: Map<string, ProgressAttachment[]>,
): ProgressPerson[] {
  const st = taskStatusMeta(t.status);
  const related = (comments ?? []).filter((c) => String(c.taskId ?? '') === String(t.id));
  const last = related[related.length - 1];
  const lastAtts = last ? (byComment.get(String(last.id)) ?? []) : [];
  const taskAtts = byTask.get(String(t.id)) ?? [];
  const atts = [...lastAtts, ...taskAtts];
  const ids = t.assigneeId && t.assigneeId > 0 ? [t.assigneeId] : (t.candidate ?? []);
  if (!ids.length) {
    return [
      {
        key: `t${t.id}`,
        label: last?.createUser || '待认领',
        status: String(t.status ?? ''),
        statusText: st.text,
        statusColor: st.color,
        comment: last?.content || undefined,
        time: last?.createTime || t.finishTime || t.createTime,
        attachments: atts.length ? atts : undefined,
      },
    ];
  }
  return ids.map((uid) => {
    const hit = related[related.length - 1];
    const label = hit?.createUser || (t.assigneeId === uid ? hit?.createUser : '') || `用户 #${uid}`;
    const show = t.assigneeId === uid || ids.length === 1;
    return {
      key: `t${t.id}-${uid}`,
      label,
      status: String(t.status ?? ''),
      statusText: st.text,
      statusColor: st.color,
      comment: show ? hit?.content || undefined : undefined,
      time: show ? hit?.createTime || t.finishTime : t.createTime,
      attachments: show && atts.length ? atts : undefined,
    };
  });
}

/** 按节点组装进度（设计 §13.3 / D5）：任务 + 图快照，意见与附件挂在节点下；未命中分支不渲染 */
export function buildProgressFlow(detail: WorkflowInstanceDetail | null, userId?: number): ProgressFlowNode[] {
  if (!detail) return [];
  const graph = parseSnapshot(detail.graphSnapshot);
  const tasks = detail.tasks ?? [];
  const byNode = new Map<string, WorkflowTaskItem[]>();
  for (const t of tasks) {
    const id = t.nodeId || '';
    if (!id) continue;
    const list = byNode.get(id) ?? [];
    list.push(t);
    byNode.set(id, list);
  }
  const { byComment, byTask } = splitAttachments(detail.attachments, String(detail.id));
  const taskNodeIds = new Set(tasks.map((t) => baseNodeId(t.nodeId)).filter(Boolean));

  const order: WfGraphNodeData[] = graph
    ? executionOrder(graph, tasks)
    : [...new Set(tasks.map((t) => baseNodeId(t.nodeId)).filter(Boolean))].map((id) => ({
        id,
        type: 'oa.approve',
        data: { name: id },
      }));

  const extra = [...byNode.keys()].filter((id) => !order.some((n) => n.id === id || n.id === baseNodeId(id)));
  const nodes: ProgressFlowNode[] = [];

  nodes.push({
    key: 'start',
    nodeId: 'start',
    type: 'oa.start',
    title: '发起',
    badges: [],
    current: false,
    people: [
      {
        key: 'starter',
        label: detail.starterId ? `发起人 #${detail.starterId}` : '发起人',
        status: 'start',
        statusText: '已提交',
        statusColor: 'green',
        comment: detail.startComment || undefined,
        time: detail.createTime,
      },
    ],
  });

  const myPending = new Set(
    tasks
      .filter((t) => {
        const st = String(t.status ?? '').toLowerCase();
        if (!['pending', 'active'].includes(st)) return false;
        if (!t.visible && t.visible !== undefined) return false;
        return t.assigneeId === userId || (t.candidate ?? []).includes(userId ?? -1);
      })
      .map((t) => t.nodeId || ''),
  );

  for (const node of order) {
    if (node.type === 'oa.start' || node.type === 'oa.end') continue;
    if (node.type === 'oa.xor') {
      const cases = xorCaseList(node.data);
      const hit = graph ? xorHitBranch(graph, node, taskNodeIds) : { id: '', visited: false };
      const hitName = hit.id && graph ? graph.nodes.find((n) => n.id === hit.id) : undefined;
      nodes.push({
        key: node.id,
        nodeId: node.id,
        type: node.type,
        title: nodeTitle(node, node.id),
        badges: ['条件分流'],
        current: false,
        hint: hit.visited
          ? `已走「${nodeTitle(hitName, hit.id)}」`
          : cases.length
            ? `按条件分流（${cases.map((c) => viewFilterSummary(c.filter)).join('；')}）`
            : '按条件分流',
        people: [],
      });
      continue;
    }

    const related = tasks.filter((t) => t.nodeId === node.id || baseNodeId(t.nodeId) === node.id);
    const badges: string[] = [];
    if (node.type === 'oa.approve') badges.push(modeLabel(String(node.data?.mode ?? related[0]?.mode ?? '')));
    if (node.type === 'oa.handle') badges.push('办理');
    if (node.type === 'oa.cc') badges.push('知会');

    // 自动通过/自动跳过/已转办写在节点上（文案与引擎意见一致）
    const hints: string[] = [];
    for (const t of related) {
      if (String(t.status ?? '').toLowerCase() === 'transferred') hints.push('已转办');
    }
    for (const c of detail.comments ?? []) {
      const content = String(c.content ?? '').trim();
      if (content !== '自动跳过' && content !== '审批人为空，自动通过') continue;
      const tid = String(c.taskId ?? '');
      if (related.some((t) => String(t.id) === tid)) hints.push(content);
    }

    const people: ProgressPerson[] = [];
    if (node.type === 'oa.cc') {
      const cc = commentsOf(detail, new Set(), ['cc']);
      if (cc.length) {
        for (const c of cc) {
          people.push({
            key: `cc-${c.time}-${c.user}`,
            label: c.user || '知会',
            status: 'cc',
            statusText: '已送达',
            statusColor: 'purple',
            comment: c.content,
            time: c.time,
          });
        }
      } else {
        people.push({
          key: `cc-${node.id}`,
          label: '知会对象',
          status: related.length ? 'done' : 'pending',
          statusText: related.length ? '已送达' : '未到达',
          statusColor: related.length ? 'green' : 'gray',
        });
      }
    } else {
      for (const t of related) people.push(...personFromTask(t, detail.comments, byComment, byTask));
    }

    nodes.push({
      key: node.id,
      nodeId: node.id,
      type: node.type,
      title: nodeTitle(node, node.id),
      badges: badges.filter(Boolean),
      current: related.some((t) => myPending.has(t.nodeId || '')),
      hint: [...new Set(hints)].join('；') || undefined,
      people,
    });
  }

  for (const id of extra) {
    const related = byNode.get(id) ?? [];
    const first = related[0];
    nodes.push({
      key: id,
      nodeId: id,
      type: 'oa.approve',
      title: id.includes('#addsign') ? '前加签' : id.includes('#after') ? '后加签' : id,
      badges: [modeLabel(first?.mode)],
      current: related.some((t) => myPending.has(t.nodeId || '')),
      people: related.flatMap((t) => personFromTask(t, detail.comments, byComment, byTask)),
    });
  }

  const endStatus = String(detail.status ?? '').toLowerCase();
  if (endStatus && endStatus !== 'running') {
    nodes.push({
      key: 'end',
      nodeId: 'end',
      type: 'oa.end',
      title: '结束',
      badges: [],
      current: false,
      people: [
        {
          key: 'end',
          label: endStatus === 'approved' ? '已通过' : endStatus === 'rejected' ? '已驳回' : endStatus,
          status: detail.status ?? '',
          statusText: endStatus === 'approved' ? '办结' : '结束',
          statusColor: endStatus === 'approved' ? 'green' : endStatus === 'rejected' ? 'red' : 'gray',
          time: detail.finishTime,
        },
      ],
    });
  }

  return nodes;
}

export type DefNodeState = 'done' | 'current' | 'waiting';

export interface DefinitionPaintNode {
  key: string;
  title: string;
  badges: string[];
  state: DefNodeState;
  /** 审批 / 办理 / 知会的人员、角色、部门或管理人员 */
  assignees?: string;
}

const OPEN_STATUS = new Set(['pending', 'active']);

function assigneeLineOf(node: WfGraphNodeData): string {
  if (node.type !== 'oa.approve' && node.type !== 'oa.handle' && node.type !== 'oa.cc') return '';
  return nodeAssigneeLine(node.data);
}

/** 只读流程定义上色：已走完、当前、未处理 */
export function definitionPaint(detail: WorkflowInstanceDetail | null): DefinitionPaintNode[] {
  if (!detail) return [];
  const graph = parseSnapshot(detail.graphSnapshot);
  if (!graph) return [];
  const tasks = detail.tasks ?? [];
  const openIds = new Set(
    tasks.filter((t) => OPEN_STATUS.has(String(t.status ?? '').toLowerCase())).map((t) => baseNodeId(t.nodeId)),
  );
  const order = executionOrder(graph, tasks);
  const running = String(detail.status ?? '').toLowerCase() === 'running';
  const currentIdx = order.findIndex((n) => openIds.has(n.id));
  const indexOf = new Map(order.map((n, i) => [n.id, i] as const));

  return graph.nodes.map((node) => {
    const idx = indexOf.get(node.id);
    const onPath = idx != null;
    let state: DefNodeState = 'waiting';
    if (openIds.has(node.id)) state = 'current';
    else if (node.type === 'oa.start') state = 'done';
    else if (node.type === 'oa.end') state = running ? 'waiting' : 'done';
    else if (onPath && !running) state = 'done';
    else if (onPath && currentIdx >= 0 && idx < currentIdx) state = 'done';
    else if (
      tasks.some(
        (t) => baseNodeId(t.nodeId) === node.id && !OPEN_STATUS.has(String(t.status ?? '').toLowerCase()),
      )
    )
      state = 'done';
    const badges: string[] = [];
    if (node.type === 'oa.approve') badges.push(modeLabel(String(node.data?.mode ?? 'or')));
    if (node.type === 'oa.handle') badges.push('办理');
    if (node.type === 'oa.cc') badges.push('知会');
    if (node.type === 'oa.xor') badges.push('条件');
    if (node.type === 'oa.parallel') badges.push('并行');
    return {
      key: node.id,
      title: nodeTitle(node, node.id),
      badges,
      state,
      assignees: assigneeLineOf(node),
    };
  });
}

/** 尚未发起的流程定义：只读节点列表，全部视为未处理 */
export function definitionOutline(graphJson: string | null | undefined): DefinitionPaintNode[] {
  const graph = parseSnapshot(graphJson ?? undefined);
  if (!graph) return [];
  return graph.nodes.map((node) => {
    const badges: string[] = [];
    if (node.type === 'oa.approve') badges.push(modeLabel(String(node.data?.mode ?? 'or')));
    if (node.type === 'oa.handle') badges.push('办理');
    if (node.type === 'oa.cc') badges.push('知会');
    if (node.type === 'oa.xor') badges.push('条件');
    if (node.type === 'oa.parallel') badges.push('并行');
    return {
      key: node.id,
      title: nodeTitle(node, node.id),
      badges,
      state: 'waiting' as const,
      assignees: assigneeLineOf(node),
    };
  });
}

export interface OpinionEvent {
  key: string;
  actionText: string;
  user: string;
  time?: string;
  content?: string;
  nodeTitle?: string;
  attachments?: ProgressAttachment[];
}

/** 已提交与已审批的意见（不含未到达节点） */
export function opinionTimeline(detail: WorkflowInstanceDetail | null): OpinionEvent[] {
  if (!detail) return [];
  const graph = parseSnapshot(detail.graphSnapshot);
  const taskNode = new Map((detail.tasks ?? []).map((t) => [String(t.id), baseNodeId(t.nodeId)] as const));
  const { byComment } = splitAttachments(detail.attachments, String(detail.id));
  const events: OpinionEvent[] = [
    {
      key: 'start',
      actionText: '发起',
      user: detail.starterId ? `发起人 #${detail.starterId}` : '发起人',
      time: detail.createTime,
      content: detail.startComment || undefined,
    },
  ];
  for (const c of detail.comments ?? []) {
    if (String(c.action ?? '').toLowerCase() === 'start') continue;
    const nid = taskNode.get(String(c.taskId ?? ''));
    const node = nid ? graph?.nodes.find((n) => n.id === nid) : undefined;
    events.push({
      key: String(c.id),
      actionText: opinionActionText(c.action),
      user: c.createUser || '',
      time: c.createTime,
      content: c.content || undefined,
      nodeTitle: node ? nodeTitle(node, node.id) : undefined,
      attachments: byComment.get(String(c.id)),
    });
  }
  return events;
}

function opinionActionText(action: string | undefined): string {
  switch ((action ?? '').toLowerCase()) {
    case 'approve':
      return '同意';
    case 'reject':
      return '驳回';
    case 'complete':
      return '已办理';
    case 'transfer':
      return '转办';
    case 'cc':
      return '知会';
    case 'rollback':
      return '回退';
    case 'withdraw':
      return '撤回';
    case 'addsign':
    case 'add_sign':
      return '加签';
    default:
      return action || '处理';
  }
}

/** 浏览器能直接打开的附件：图片、pdf、纯文本 */
export function previewKind(fileName: string): 'image' | 'pdf' | 'text' | 'none' {
  const ext = fileName.split('.').pop()?.toLowerCase() ?? '';
  if (['png', 'jpg', 'jpeg', 'gif', 'webp', 'bmp'].includes(ext)) return 'image';
  if (ext === 'pdf') return 'pdf';
  if (['txt', 'md', 'csv', 'json', 'log'].includes(ext)) return 'text';
  return 'none';
}

export function rollbackTargets(detail: WorkflowInstanceDetail | null, currentNodeId?: string): RollbackTarget[] {
  if (!detail?.tasks) return [];
  const graph = parseSnapshot(detail.graphSnapshot);
  const seen = new Set<string>();
  const out: RollbackTarget[] = [];
  for (const t of detail.tasks) {
    if (String(t.status ?? '').toLowerCase() !== 'done') continue;
    const id = baseNodeId(t.nodeId);
    if (!id || id === baseNodeId(currentNodeId) || seen.has(id)) continue;
    const node = graph?.nodes.find((n) => n.id === id);
    if (node && node.type !== 'oa.approve') continue;
    seen.add(id);
    out.push({ nodeId: id, name: nodeTitle(node, id) });
  }
  return out;
}

export function nodeFlags(detail: WorkflowInstanceDetail | null, nodeId: string | undefined) {
  const graph = parseSnapshot(detail?.graphSnapshot);
  const node = graph?.nodes.find((n) => n.id === baseNodeId(nodeId));
  const data = node?.data ?? {};
  return {
    allowAddSign: data.allowAddSign !== false,
    allowRollback: data.allowRollback !== false,
    allowTransfer: data.allowTransfer !== false,
  };
}

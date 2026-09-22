import type { WorkflowInstanceDetail, WorkflowTaskItem } from '@newlifex/api-core';
import type { WfGraphData, WfGraphNodeData } from '@/core/types/workflow';
import { wfNodeTypeLabel } from '@/core/types/workflow';
import { parseGraph, topoChain } from './useWorkflowDesigner';
import { xorCaseList } from './wfNodeCard';
import { modeLabel, taskStatusMeta } from './useWorkflowTaskList';
import { viewFilterSummary } from './wfFilterText';

export interface ProgressPerson {
  key: string;
  label: string;
  status: string;
  statusText: string;
  statusColor: string;
  comment?: string;
  time?: string;
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

function parseSnapshot(json: string | undefined): WfGraphData | null {
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

function personFromTask(
  t: WorkflowTaskItem,
  comments: WorkflowInstanceDetail['comments'],
): ProgressPerson[] {
  const st = taskStatusMeta(t.status);
  const related = (comments ?? []).filter((c) => String(c.taskId ?? '') === String(t.id));
  const last = related[related.length - 1];
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
      },
    ];
  }
  return ids.map((uid) => {
    const hit = related[related.length - 1];
    const label = hit?.createUser || (t.assigneeId === uid ? hit?.createUser : '') || `用户 #${uid}`;
    return {
      key: `t${t.id}-${uid}`,
      label,
      status: String(t.status ?? ''),
      statusText: st.text,
      statusColor: st.color,
      comment: t.assigneeId === uid || ids.length === 1 ? hit?.content || undefined : undefined,
      time: t.assigneeId === uid || ids.length === 1 ? hit?.createTime || t.finishTime : t.createTime,
    };
  });
}

/** 按节点组装进度（设计 §13.3 / D5）：任务 + 图快照，意见挂在节点下 */
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

  const order: WfGraphNodeData[] = graph
    ? topoChain(graph)
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
      const visited = cases.find((c) =>
        tasks.some((t) => baseNodeId(t.nodeId) === c.target && String(t.status ?? '').toLowerCase() !== 'cancelled'),
      );
      const def = String(node.data?.defaultTarget ?? '');
      const hit = visited?.target || (tasks.some((t) => baseNodeId(t.nodeId) === def) ? def : '');
      const hitName = graph?.nodes.find((n) => n.id === hit);
      nodes.push({
        key: node.id,
        nodeId: node.id,
        type: node.type,
        title: nodeTitle(node, node.id),
        badges: ['条件分流'],
        current: false,
        hint: hit
          ? `已走「${nodeTitle(hitName, hit)}」`
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
    if (node.type === 'oa.cc') badges.push('知会');

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
      for (const t of related) people.push(...personFromTask(t, detail.comments));
    }

    nodes.push({
      key: node.id,
      nodeId: node.id,
      type: node.type,
      title: nodeTitle(node, node.id),
      badges: badges.filter(Boolean),
      current: related.some((t) => myPending.has(t.nodeId || '')),
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
      people: related.flatMap((t) => personFromTask(t, detail.comments)),
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

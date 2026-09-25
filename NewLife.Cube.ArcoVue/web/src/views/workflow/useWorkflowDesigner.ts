import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { FieldKind, type DataField, type WorkflowDefinitionItem, type WorkflowPhrase } from '@newlifex/api-core';
import { Message } from '@arco-design/web-vue';
import { createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import type { FieldMeta } from '@/core/types/field';
import type { WfGraphData, WfGraphNodeData, WfToKind } from '@/core/types/workflow';
import { isSearchToKind, wfRecipientTo, wfRecipientToJson, wfToJsonForKind, wfToKindOf } from '@/core/types/workflow';
import { toFieldMetas } from '@/core/utils/fieldNormalize';
import { ensureApiTypePath } from '@/core/utils/url';
import { emptyViewFilter, type ViewFilter } from '@/core/utils/viewProfile';
import { defaultNodeDataFor, flowDocToGraph, graphToFlowDoc, type FlowDoc } from './flowgram/flowgramGraph';
import { FlowGramDesigner, type FlowGramApi } from './flowgram/FlowGramDesigner';
import type { RecipientKind } from './recipient';
import { searchRecipients } from './recipient';
import { branchInfosOf, cardsOf, insertNodeTitle, recipientLabelsOf, xorCaseList } from './wfNodeCard';
import { parseViewFilterJson, stringifyViewFilter, viewFilterHasRules, filterValueLabelResolver } from './wfFilterText';

/**
 * 流程设计器（OSC-26090347f1 T8e-FlowGram）：画布由 FlowGram.AI 固定布局承担（结构增删/拖拽/缩放），
 * 运行时是 C# 引擎，浏览器只读写定义图（design §4）。<1024 只读提示（IA §3.4）。
 * 纯图函数（parseGraph/newDefaultGraph/insertAfter/removeNode/applyPatch/topoChain）可单测。
 */

/** 按类别解析接收人显示名（添加节点后画布卡片用） */
export async function resolveRecipientLabels(kind: RecipientKind, ids: number[]): Promise<string[]> {
  if (!ids.length) return [];
  try {
    const list = await searchRecipients(kind, '');
    const map = new Map(list.map((x) => [x.id, x.displayName || x.name || String(x.id)]));
    return ids.map((id) => map.get(id) || String(id));
  } catch {
    return ids.map(String);
  }
}

export function parseGraph(json: string | null | undefined): WfGraphData {
  if (json) {
    try {
      const obj = JSON.parse(json) as Partial<WfGraphData>;
      if (obj && Array.isArray(obj.nodes) && Array.isArray(obj.edges)) {
        // 保留根级未知字段（如 excludeStats），保存时原样写回
        return { ...obj, version: obj.version ?? 1, nodes: obj.nodes, edges: obj.edges };
      }
    } catch {
      /* 非法 JSON 回落默认图 */
    }
  }
  return newDefaultGraph();
}

/** 新定义默认图：Start → 审批 → End（IA §2 空态） */
export function newDefaultGraph(): WfGraphData {
  const nodes: WfGraphNodeData[] = [
    { id: 'start', type: 'oa.start', data: { name: '开始' } },
    {
      id: 'n1',
      type: 'oa.approve',
      data: {
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
      },
    },
    { id: 'end', type: 'oa.end', data: { name: '结束' } },
  ];
  return { version: 1, nodes, edges: [{ source: 'start', target: 'n1' }, { source: 'n1', target: 'end' }] };
}

/** 图节点下一跳映射 */
export function nextOf(graph: WfGraphData): Map<string, string> {
  const map = new Map<string, string>();
  for (const e of graph.edges ?? []) map.set(e.source, e.target);
  return map;
}

/** 主链拓扑序（从 start 沿唯一后继；环或分叉时截断防死循环） */
export function topoChain(graph: WfGraphData): WfGraphNodeData[] {
  const byId = new Map(graph.nodes.map((n) => [n.id, n]));
  const next = nextOf(graph);
  const order: WfGraphNodeData[] = [];
  let cur = graph.nodes.find((n) => n.type === 'oa.start')?.id;
  const seen = new Set<string>();
  while (cur && !seen.has(cur)) {
    seen.add(cur);
    const node = byId.get(cur);
    if (node) order.push(node);
    cur = next.get(cur);
  }
  return order;
}

/** XOR 出边按 cases + defaultTarget 重写（默认分支优先，主链走 default） */
export function compileXorEdges(graph: WfGraphData): WfGraphData {
  const xorIds = new Set(graph.nodes.filter((n) => n.type === 'oa.xor' || n.type === 'oa.parallel').map((n) => n.id));
  if (!xorIds.size) return graph;
  const idSet = new Set(graph.nodes.map((n) => n.id));
  const edges = (graph.edges ?? []).filter((e) => !xorIds.has(e.source));
  for (const n of graph.nodes) {
    if (n.type !== 'oa.xor' && n.type !== 'oa.parallel') continue;
    const def = String(n.data?.defaultTarget ?? '').trim();
    const targets: string[] = [];
    if (def && idSet.has(def)) targets.push(def);
    const cases = Array.isArray(n.data?.cases) ? (n.data!.cases as { target?: string }[]) : [];
    for (const c of cases) {
      const t = String(c?.target ?? '').trim();
      if (t && idSet.has(t) && !targets.includes(t)) targets.push(t);
    }
    for (const t of targets) edges.push({ source: n.id, target: t });
  }
  return { ...graph, edges };
}

/** 在指定节点后插入新节点（单后继重接 after→new→old）；禁止在 end 后插入 */
export function insertAfter(graph: WfGraphData, afterId: string, type: string): WfGraphData {
  const node = graph.nodes.find((n) => n.id === afterId);
  if (!node || node.type === 'oa.end') return graph;
  const id = nextNodeId(graph.nodes.map((n) => n.id));
  const oldTarget = firstTarget(graph, afterId);
  const data: Record<string, unknown> = { name: defaultName(type) };
  if (type === 'oa.approve') {
    data.mode = 'or';
    data.to = wfRecipientToJson(undefined, [], 'users');
    data.fields = { visible: ['*'], writable: [] };
    data.timeoutHours = 0;
    data.timeoutAction = 'pass';
    data.allowAddSign = true;
    data.allowRollback = true;
    data.allowTransfer = true;
    data.emptyPolicy = 'manager';
  }
  if (type === 'oa.handle') {
    data.mode = 'or';
    data.to = wfRecipientToJson(undefined, [], 'users');
    data.emptyPolicy = 'manager';
  }
  if (type === 'oa.cc') data.to = wfRecipientToJson(undefined, [], 'users');
  if (type === 'oa.xor' || type === 'oa.parallel') {
    data.cases = [];
    data.defaultTarget = oldTarget || '';
    if (type === 'oa.parallel') data.joinTarget = oldTarget || '';
  }
  const newNode: WfGraphNodeData = { id, type, data };
  // 移除 after→oldTarget 首边，改为 after→new + new→oldTarget；其它 from after 的出边保留
  const edges = (graph.edges ?? []).filter(
    (e) => !(e.source === afterId && e.target === oldTarget),
  );
  edges.push({ source: afterId, target: id });
  if (oldTarget) edges.push({ source: id, target: oldTarget });
  return { version: graph.version ?? 1, nodes: [...graph.nodes, newNode], edges };
}

/** 首个后继 id（无则 null） */
export function firstTarget(graph: WfGraphData, sourceId: string): string | null {
  const e = (graph.edges ?? []).find((x) => x.source === sourceId);
  return e?.target ?? null;
}

/** 下一可用节点 id（n1/n2…，跳过已用；特殊 start/end 固定名不可占用） */
function nextNodeId(used: string[]): string {
  let i = 1;
  const set = new Set(used);
  while (set.has(`n${i}`)) i++;
  return `n${i}`;
}

function defaultName(type: string): string {
  switch (type) {
    case 'oa.approve':
      return '审批';
    case 'oa.cc':
      return '知会';
    case 'oa.xor':
      return '条件分流';
    case 'oa.parallel':
      return '并行分支';
    case 'oa.start':
      return '开始';
    case 'oa.end':
      return '结束';
    default:
      return '节点';
  }
}

/** 删除节点：前驱接到原后继；start/end 禁删 */
export function removeNode(graph: WfGraphData, nodeId: string): WfGraphData {
  const node = graph.nodes.find((n) => n.id === nodeId);
  if (!node || node.type === 'oa.start' || node.type === 'oa.end') return graph;
  const pred = (graph.edges ?? []).find((e) => e.target === nodeId)?.source ?? null;
  const succ = firstTarget(graph, nodeId);
  const edges = (graph.edges ?? [])
    .filter((e) => e.source !== nodeId)
    .map((e) => (pred && succ && e.source === pred ? { ...e, target: succ } : e));
  // 若 pred 的其它边被上面覆盖后丢失原出边（多出边场景保留首条）
  if (pred && succ && !edges.some((e) => e.source === pred)) edges.push({ source: pred, target: succ });
  return { version: graph.version ?? 1, nodes: graph.nodes.filter((n) => n.id !== nodeId), edges };
}

/** 覆盖节点 data（浅合并 + 删除 undefined） */
export function applyNodePatch(graph: WfGraphData, nodeId: string, patch: Record<string, unknown>): WfGraphData {
  return {
    version: graph.version ?? 1,
    nodes: graph.nodes.map((n) => {
      if (n.id !== nodeId) return n;
      const next: Record<string, unknown> = { ...(n.data ?? {}) };
      for (const [k, v] of Object.entries(patch)) {
        if (v === undefined) delete next[k];
        else next[k] = v;
      }
      return { ...n, data: next };
    }),
    edges: graph.edges,
  };
}

/** 序列化为 GraphJson 文本（version 1） */
export function compileGraph(graph: WfGraphData): string {
  return JSON.stringify({ ...graph, version: graph.version ?? 1 });
}

/** 校验基本完整性（发布前 UI 快速提示；后端为最终权威） */
export function validateGraph(graph: WfGraphData): string[] {
  const errors: string[] = [];
  const starts = graph.nodes.filter((n) => n.type === 'oa.start').length;
  const ends = graph.nodes.filter((n) => n.type === 'oa.end').length;
  if (starts !== 1) errors.push('必须且仅有一个「开始」节点');
  if (ends !== 1) errors.push('必须且仅有一个「结束」节点');
  for (const n of graph.nodes) {
    const label = String((n.data?.name as string) || n.id);
    if (n.type === 'oa.approve') {
      // 六种选人按 kind 分支：仅 users/roles/departments 需 Id；manager 恒合法；
      // starterPick 由提交侧 picks 校验；field 由后端 ValidateTo 校验
      const toKind = wfToKindOf(n.data?.to);
      if (toKind === 'users' || toKind === 'roles' || toKind === 'departments') {
        const to = wfRecipientTo(n.data?.to);
        if (to.ids.length === 0) errors.push(`「${label}」缺少审批人`);
      }
      if (n.data?.timeoutAction === 'transfer' && Number(n.data?.timeoutHours) > 0) {
        const tt = wfRecipientTo(n.data?.timeoutTransferTo);
        if (!tt.ids.length) errors.push(`「${label}」超时转交未指定接收人`);
      }
    }
    if (n.type === 'oa.xor' || n.type === 'oa.parallel') {
      const def = String(n.data?.defaultTarget ?? '').trim();
      if (!def) errors.push(`「${label}」缺少默认分支`);
      else if (!graph.nodes.some((x) => x.id === def)) errors.push(`「${label}」默认分支目标不存在`);
    }
  }
  return errors;
}

/** 定义行可见字段选项（GetPage → GetFields → Automation/Meta 多源兜底） */
export async function loadWritableFields(typePath: string): Promise<{ name: string; label: string }[]> {
  const fields = await loadFilterFields(typePath);
  return fields.map((f) => ({ name: f.name, label: f.displayName || f.name }));
}

/**
 * 加载实体字段供「可写字段 / 条件分流 / 发起条件」使用。
 * 根因：流程 TypePath 为 `Admin/Department`，实体 GetPage 须 `/Admin/Department`，
 * 缺前导 `/` 时请求落到 `/apiAdmin/...` 失败，两侧都空。
 */
export async function loadFilterFields(typePath: string): Promise<FieldMeta[]> {
  const apiType = ensureApiTypePath(typePath);
  if (!apiType) return [];

  const mergeUnique = (lists: FieldMeta[][]): FieldMeta[] => {
    const map = new Map<string, FieldMeta>();
    for (const list of lists) {
      for (const f of list) {
        if (!f.name || f.primaryKey) continue;
        const key = f.name.toLowerCase();
        if (!map.has(key)) map.set(key, f);
      }
    }
    return [...map.values()];
  };

  const fromGetPage = async (): Promise<FieldMeta[]> => {
    try {
      const res = await cubeApi.page.getPage(apiType);
      const meta = ((res as { data?: unknown })?.data ?? res) as Record<string, unknown>;
      if (!meta || typeof meta !== 'object' || typeof meta === 'string') return [];
      const nested = meta.fields as { form?: { editForm?: unknown; addForm?: unknown }; list?: unknown } | undefined;
      return toFieldMetas(
        (meta.editForm ??
          nested?.form?.editForm ??
          meta.addForm ??
          nested?.form?.addForm ??
          meta.list ??
          nested?.list ??
          []) as DataField[],
      ).filter((f) => !!f.name);
    } catch {
      return [];
    }
  };

  const fromGetFields = async (kind: FieldKind): Promise<FieldMeta[]> => {
    try {
      const fb = await cubeApi.page.getFields(apiType, kind);
      return toFieldMetas((fb.data ?? []) as DataField[]).filter((f) => !!f.name);
    } catch {
      return [];
    }
  };

  const fromAutomationMeta = async (): Promise<FieldMeta[]> => {
    try {
      // Automation Meta 用 query typePath，存库形态 Admin/X 即可
      const bare = apiType.replace(/^\/+/, '');
      const res = await cubeApi.automation.meta(bare);
      const rows = (res.data ?? []) as Record<string, unknown>[];
      if (!Array.isArray(rows)) return [];
      return rows
        .map((row) => {
          const name = String(row.name ?? row.Name ?? '').trim();
          if (!name) return null;
          return {
            name,
            displayName: String(row.displayName ?? row.DisplayName ?? name).trim() || name,
            typeName: String(row.typeName ?? row.TypeName ?? 'String').trim() || 'String',
            primaryKey: Boolean(row.primaryKey ?? row.PrimaryKey),
            readOnly: Boolean(row.readOnly ?? row.ReadOnly),
            hasTypeName: true,
          } as FieldMeta;
        })
        .filter((f): f is FieldMeta => !!f);
    } catch {
      return [];
    }
  };

  const [page, edit, list, auto] = await Promise.all([
    fromGetPage(),
    fromGetFields(FieldKind.Edit),
    fromGetFields(FieldKind.List),
    fromAutomationMeta(),
  ]);
  return mergeUnique([page, edit, list, auto]);
}

/** 会签节点去掉 quorum，引擎按「全部通过」推进 */
export function stripAndQuorum(graph: WfGraphData): WfGraphData {
  return {
    ...graph,
    nodes: graph.nodes.map((n) => {
      if (n.type !== 'oa.approve' || String(n.data?.mode ?? '') !== 'and') return n;
      if (n.data?.quorum == null) return n;
      const data = { ...n.data };
      delete data.quorum;
      return { ...n, data };
    }),
  };
}

/** 设计器编排状态 */
export function useWorkflowDesigner() {
  const definitions = ref<WorkflowDefinitionItem[]>([]);
  const defsLoading = ref(false);
  /** 定义主键。后端 Int64 雪花 id 以字符串序列化（JSON number 会丢精度），全程字符串透传 */
  const currentId = ref<string | null>(null);
  const current = ref<WorkflowDefinitionItem | null>(null);
  const graph = ref<WfGraphData | null>(null);
  const selectedNodeId = ref<string | null>(null);
  /** 右侧抽屉：点空白是流程属性，点节点是节点属性 */
  const propertyPanel = ref<'none' | 'flow' | 'node'>('none');
  const saving = ref(false);
  const error = ref('');
  const writableFields = ref<{ name: string; label: string }[]>([]);
  const writableFieldsLoading = ref(false);
  const filterFields = ref<FieldMeta[]>([]);
  const phrases = ref<WorkflowPhrase[]>([]);
  const phrasesVisible = ref(false);
  const phraseDraft = ref('');
  const narrow = ref(false);
  /** 可发起实体选项（新建草稿 typePath 下拉） */
  const entityOptions = ref<{ value: string; label: string }[]>([]);
  /** React 画布挂载点（WorkflowDesignerPage 提供 div） */
  const canvasEl = ref<HTMLDivElement | null>(null);
  let reactRoot: Root | null = null;
  /** FlowGram 画布桥接 API（ref 回调在每次 render 更新） */
  const canvasApi: { current: FlowGramApi | null } = { current: null };

  /** 添加审批/办理/知会：插入前配置签核模式与接收人 */
  const insertVisible = ref(false);
  const insertAfterId = ref<string | null>(null);
  const insertType = ref<'oa.approve' | 'oa.handle' | 'oa.cc'>('oa.approve');
  const insertMode = ref('or');
  const insertToKind = ref<WfToKind>('users');
  const insertToIds = ref<number[]>([]);
  const pendingInsertDraft = ref<Record<string, unknown> | null>(null);

  /** id → 卡片名称（label 权威来源：Vue graph） */
  function labelsOf(g: WfGraphData | null): Record<string, string> {
    const m: Record<string, string> = {};
    for (const n of g?.nodes ?? []) {
      const name = (n.data?.name as string) ?? '';
      if (name) m[n.id] = name;
    }
    return m;
  }

  const hasStartFilter = computed(() => viewFilterHasRules(parseViewFilterJson(current.value?.startFilter)));
  const cardInfos = computed(() => cardsOf(graph.value, hasStartFilter.value));
  /** 分支卡片摘要（画布块按物理顺序查，字段/枚举值均取友好名） */
  const branchInfos = computed(() =>
    branchInfosOf(
      graph.value,
      (name) => filterFields.value.find((f) => f.name === name)?.displayName || name,
      filterValueLabelResolver(filterFields.value),
    ),
  );
  const graphErrors = computed(() => (graph.value ? validateGraph(graph.value) : []));

  /** 渲染/更新 React 画布（labels/readonly/doc 变更时调用；挂载前 no-op） */
  function renderCanvas() {
    if (!reactRoot) return;
    const g = graph.value;
    reactRoot.render(
      createElement(FlowGramDesigner, {
        doc: graphToFlowDoc(g),
        readonly: !canEdit.value,
        labels: labelsOf(g),
        cards: cardInfos.value,
        branchInfos: branchInfos.value,
        selectedId: selectedNodeId.value || undefined,
        ref: (api: FlowGramApi | null) => {
          canvasApi.current = api;
        },
        onSelectNode: (id: string) => {
          if (id) {
            selectedNodeId.value = id;
            propertyPanel.value = 'node';
          } else {
            selectedNodeId.value = null;
            propertyPanel.value = 'flow';
          }
          renderCanvas();
        },
        onInsertAfter: (fromId: string, type: string) => {
          beginInsert(fromId, type);
        },
        onDocChanged: (d: FlowDoc) => {
          graphChangedFromCanvas(d);
        },
      }),
    );
  }

  /** React 画布结构/数据变更 → 镜像重建 Vue graph（保存/发布/属性面板读取唯一数据源） */
  function graphChangedFromCanvas(doc: FlowDoc) {
    const prev = graph.value;
    const prevById = new Map((prev?.nodes ?? []).map((n) => [n.id, n] as const));
    let addedId: string | null = null;
    const raw = flowDocToGraph(doc);
    const nodes = raw.nodes.map((n) => {
      const old = prevById.get(n.id);
      if (old && (!n.data || Object.keys(n.data).length === 0)) return { ...n, data: old.data };
      if (!old) addedId = n.id;
      if (!n.data || Object.keys(n.data).length === 0) n.data = defaultNodeDataFor(n.type);
      if (!old && pendingInsertDraft.value) {
        n.data = { ...n.data, ...pendingInsertDraft.value };
        pendingInsertDraft.value = null;
      }
      if (!old && (n.type === 'oa.xor' || n.type === 'oa.parallel')) {
        const next = raw.edges.find((e) => e.source === n.id)?.target || '';
        if (next && !n.data.defaultTarget) n.data = { ...n.data, defaultTarget: next };
      }
      return n;
    });
    const nextIds = new Set(nodes.map((n) => n.id));
    for (const n of prev?.nodes ?? []) {
      if (nextIds.has(n.id)) continue;
      // 仅保留仍被画布现存节点 target 引用的旧节点；已删分流节点自身不再被"自我引用"复活
      const referenced = nodes.some((x) => xorTargetIds(x).includes(n.id));
      if (referenced) {
        nodes.push(n);
        nextIds.add(n.id);
      }
    }
    graph.value = compileXorEdges({ version: raw.version, nodes, edges: raw.edges });
    if (addedId) {
      selectedNodeId.value = addedId;
      propertyPanel.value = 'node';
    }
    renderCanvas();
  }

  function xorTargetIds(n: WfGraphNodeData): string[] {
    if (n.type !== 'oa.xor' && n.type !== 'oa.parallel') return [];
    const def = String(n.data?.defaultTarget ?? '').trim();
    const cases = Array.isArray(n.data?.cases) ? (n.data!.cases as { target?: string }[]) : [];
    return [def, ...cases.map((c) => String(c?.target ?? '').trim())].filter(Boolean);
  }

  /** 在 canvasEl 上创建 React root（组件挂载后调用） */
  function ensureCanvas() {
    if (canvasEl.value && !reactRoot) {
      reactRoot = createRoot(canvasEl.value);
      renderCanvas();
    }
  }

  /**
   * 画布挂载 div 位于模板 `v-if="!graph"` 的 v-else 分支内：初始 graph=null 时不存在，
   * onMounted 时 canvasEl 为空会跳过 ensureCanvas；打开定义后 div 才出现。
   * 因此 graph/canvasEl 就绪后再次尝试挂载（flush post：等 Vue patch 填 ref）。
   */
  watch([graph, canvasEl], () => ensureCanvas(), { flush: 'post' });

  /** 可配置实体（automation.entities update 权限）；label 优先菜单中文显示名 */
  async function loadEntities() {
    try {
      const res = await cubeApi.automation.entities('update');
      const rows = (res.data ?? []) as { typePath?: string; displayName?: string; name?: string }[];
      entityOptions.value = rows
        .filter((r) => !!r.typePath)
        .map((r) => ({ value: r.typePath as string, label: r.displayName || r.name || (r.typePath as string) }));
    } catch {
      entityOptions.value = [];
    }
  }

  const selectedNode = computed(
    () => graph.value?.nodes.find((n) => n.id === selectedNodeId.value) ?? null,
  );
  const chain = computed(() => (graph.value ? topoChain(graph.value) : []));
  const canEdit = computed(() => !narrow.value && !!current.value);

  /** 选中节点当前接收人（规范化，六种选人） */
  const selectedToKind = computed<WfToKind>({
    get: () => wfToKindOf(selectedNode.value?.data?.to),
    set: (kind: WfToKind) => {
      // 切换选人类型时清空已选，避免旧 Id 落到新类别上
      patchSelected({ to: wfToJsonForKind(kind, []), toLabels: [] });
    },
  });

  /** 合并 to 上的附加字段（scope/multiple/field/fieldAs），保留原 kind 与 Id */
  function patchToExtras(extra: Record<string, unknown>) {
    const node = selectedNode.value;
    if (!node) return;
    const to = (node.data?.to ?? {}) as Record<string, unknown>;
    const kind = wfToKindOf(to);
    const ids = wfRecipientTo(to).ids;
    patchSelected({ to: wfToJsonForKind(kind, ids, { ...to, ...extra }) });
  }

  const selectedToIds = computed({
    get: () => wfRecipientTo(selectedNode.value?.data?.to).ids,
    set: (ids: number[]) => {
      if (!isSearchToKind(selectedToKind.value)) return;
      const kind = selectedToKind.value;
      const prev = recipientLabelsOf(selectedNode.value?.data);
      patchSelected({
        to: wfRecipientToJson(selectedNode.value?.data?.to, ids, kind),
      });
      void (async () => {
        const resolved = await resolveRecipientLabels(kind as RecipientKind, ids);
        const cur = selectedNode.value;
        if (!cur) return;
        const still = wfRecipientTo(cur.data?.to);
        if (still.kind !== kind || still.ids.join(',') !== ids.join(',')) return;
        const live = recipientLabelsOf(cur.data);
        const labels = ids.map((id, i) => {
          const name = (resolved[i] ?? '').trim();
          if (name && name !== String(id)) return name;
          const kept = (live[i] ?? '').trim();
          if (kept && kept !== String(id)) return kept;
          const old = (prev[i] ?? '').trim();
          if (old && old !== String(id)) return old;
          return name || kept || String(id);
        });
        patchSelected({ toLabels: labels });
      })();
    },
  });

  /** 选人框里的显示名，和 Id 一起写到卡片 */
  const selectedToLabels = computed({
    get: () => recipientLabelsOf(selectedNode.value?.data),
    set: (labels: string[]) => {
      const ids = wfRecipientTo(selectedNode.value?.data?.to).ids;
      const next = ids.map((id, i) => {
        const name = (labels[i] ?? '').trim();
        return name || String(id);
      });
      patchSelected({ toLabels: next });
    },
  });

  /** 提交人自选：可选范围与是否多选 */
  const selectedStarterPickScope = computed({
    get: () => String((selectedNode.value?.data?.to as Record<string, unknown> | undefined)?.scope ?? 'all'),
    set: (v: string) => patchToExtras({ scope: v }),
  });
  const selectedStarterPickMultiple = computed({
    get: () => (selectedNode.value?.data?.to as Record<string, unknown> | undefined)?.multiple === true,
    set: (v: boolean) => patchToExtras({ multiple: v }),
  });

  /** 表单人员：字段名与取值方式（user=用户字段；manager=部门字段取负责人） */
  const selectedFieldName = computed({
    get: () => String((selectedNode.value?.data?.to as Record<string, unknown> | undefined)?.field ?? ''),
    set: (v: string) => {
      const label = filterFields.value.find((f) => f.name === v)?.displayName ?? v;
      patchToExtras({ field: v });
      if (v) patchSelected({ toLabels: [label] });
    },
  });
  const selectedFieldAs = computed({
    get: () => String((selectedNode.value?.data?.to as Record<string, unknown> | undefined)?.fieldAs ?? 'user'),
    set: (v: string) => patchToExtras({ fieldAs: v }),
  });

  /** 空人策略：缺省（停住通知）/ pass（自动通过）/ manager（转部门负责人）/ user（指定人） */
  const selectedEmptyPolicy = computed({
    get: () => String(selectedNode.value?.data?.emptyPolicy ?? ''),
    set: (v: string) => patchSelected({ emptyPolicy: v === 'default' || !v ? undefined : v }),
  });
  const selectedEmptyUserIds = computed({
    get: () => {
      const id = Number(selectedNode.value?.data?.emptyUserId ?? 0) || 0;
      return id > 0 ? [id] : [];
    },
    set: (ids: number[]) => patchSelected({ emptyUserId: ids[0] ?? undefined }),
  });

  /** 审批节点「不计入效率」 */
  const selectedExcludeStats = computed({
    get: () => selectedNode.value?.data?.excludeStats === true,
    set: (v: boolean) => patchSelected({ excludeStats: v ? true : undefined }),
  });

  /** 流程根「不纳入效率统计」 */
  const rootExcludeStats = computed({
    get: () => graph.value?.excludeStats === true,
    set: (v: boolean) => {
      if (!graph.value) return;
      setGraph({ ...graph.value, excludeStats: v ? true : undefined });
    },
  });

  /** 通用字符串属性（name/mode/...） */
  function stringProp(key: string) {
    return computed({
      get: () => String(selectedNode.value?.data?.[key] ?? ''),
      set: (v: string) => patchSelected({ [key]: v }),
    });
  }
  /** 通用数值属性 */
  function numProp(key: string) {
    return computed({
      get: () => Number(selectedNode.value?.data?.[key] ?? 0) || 0,
      set: (v: number) => patchSelected({ [key]: v }),
    });
  }
  /** 可写字段（data.fields.writable） */
  const selectedWritable = computed({
    get: () => {
      const f = selectedNode.value?.data?.fields as { writable?: unknown } | undefined;
      return Array.isArray(f?.writable) ? (f.writable as unknown[]).map(String) : [];
    },
    set: (names: string[]) => {
      const fields = { visible: ['*'], writable: names };
      patchSelected({ fields });
    },
  });
  const selectedName = stringProp('name');
  const selectedMode = stringProp('mode');
  const selectedTimeoutHours = numProp('timeoutHours');
  const selectedTimeoutAction = stringProp('timeoutAction');
  const selectedAddSign = boolDefaultTrue('allowAddSign');
  const selectedRollback = boolDefaultTrue('allowRollback');
  const selectedTransfer = boolDefaultTrue('allowTransfer');
  /** 或签 / 会签 / 依次审批。会签仍是全部通过 */
  function setApproveMode(mode: string) {
    const m = mode === 'and' ? 'and' : mode === 'sequence' ? 'sequence' : 'or';
    patchSelected({ mode: m, quorum: undefined });
  }
  const selectedTimeoutToKind = computed<RecipientKind>({
    get: () => wfRecipientTo(selectedNode.value?.data?.timeoutTransferTo).kind as RecipientKind,
    set: (kind: RecipientKind) => patchSelected({ timeoutTransferTo: wfRecipientToJson(undefined, [], kind) }),
  });
  const selectedTimeoutToIds = computed({
    get: () => wfRecipientTo(selectedNode.value?.data?.timeoutTransferTo).ids,
    set: (ids: number[]) =>
      patchSelected({
        timeoutTransferTo: wfRecipientToJson(selectedNode.value?.data?.timeoutTransferTo, ids, selectedTimeoutToKind.value),
      }),
  });
  const selectedDefaultTarget = stringProp('defaultTarget');
  const nodeOptions = computed(() =>
    (graph.value?.nodes ?? [])
      .filter((n) => n.type !== 'oa.start' && n.id !== selectedNodeId.value)
      .map((n) => ({
        value: n.id,
        label: `${String(n.data?.name || wfNodeTypeLabelSafe(n.type))}（${n.id}）`,
      })),
  );
  const lockPolicy = computed({
    get: () => current.value?.lockPolicy || 'full',
    set: (v: string) => {
      if (current.value) current.value.lockPolicy = v;
    },
  });
  const startFilter = computed({
    get: () => parseViewFilterJson(current.value?.startFilter),
    set: (v: ViewFilter) => {
      if (current.value) current.value.startFilter = stringifyViewFilter(v);
    },
  });
  /** 是否审批/办理/知会节点（展示接收人属性区） */
  const selectedHasTo = computed(
    () =>
      !!selectedNode.value && ['oa.approve', 'oa.handle', 'oa.cc'].includes(selectedNode.value.type),
  );

  function boolDefaultTrue(key: string) {
    return computed({
      get: () => selectedNode.value?.data?.[key] !== false,
      set: (v: boolean) => patchSelected({ [key]: v }),
    });
  }
  function wfNodeTypeLabelSafe(type: string) {
    return type === 'oa.approve' ? '审批' : type === 'oa.cc' ? '知会' : type === 'oa.xor' ? '条件分流' : type === 'oa.parallel' ? '并行分支' : type === 'oa.end' ? '结束' : type === 'oa.handle' ? '办理' : type;
  }

  /** 画布分支卡片选中：__if / __caseN / __else 后缀（块 id 即选中的 selectedNodeId） */
  const BRANCH_SUFFIX = /__(?:if|else|case\d+)$/;
  const selectedBranch = computed(() => {
    const id = selectedNodeId.value ?? '';
    if (!id || !BRANCH_SUFFIX.test(id)) return null;
    const splitId = id.replace(BRANCH_SUFFIX, '');
    const split = graph.value?.nodes.find((n) => n.id === splitId) ?? null;
    if (!split || (split.type !== 'oa.xor' && split.type !== 'oa.parallel')) return null;
    const cases = xorCaseList(split.data);
    const isElse = id.endsWith('__else');
    const m = /__case(\d+)$/.exec(id);
    const index = isElse ? cases.length : m ? Number(m[1]) : 0;
    return { blockId: id, splitId, split, cases, index, isElse };
  });

  /** 分支补丁：改指定 split 节点的 cases / defaultTarget（不经 selectedNode） */
  function patchBranchData(
    splitId: string,
    patch: { cases?: { filter?: unknown; target?: string; name?: string }[]; defaultTarget?: string },
  ) {
    const g = graph.value;
    if (!g) return;
    setGraph(applyNodePatch(g, splitId, patch));
    renderCanvas();
  }

  /** 分支补丁：只改指定 split 的第 index 个条件 */
  function patchBranchCase(splitId: string, index: number, patch: { filter?: unknown; target?: string; name?: string }) {
    const g = graph.value;
    if (!g) return;
    const split = g.nodes.find((n) => n.id === splitId);
    if (!split) return;
    const cases = xorCaseList(split.data).map((c, i) => (i === index ? { ...c, ...patch } : c));
    setGraph(applyNodePatch(g, splitId, { cases }));
    renderCanvas();
  }

  /** 分支面板：条件过滤器（case 专属；其他情况无） */
  const selectedBranchFilter = computed<ViewFilter>({
    get: () => {
      const b = selectedBranch.value;
      if (!b || b.isElse) return emptyViewFilter();
      return (b.cases[b.index]?.filter as ViewFilter) ?? emptyViewFilter();
    },
    set: (v: ViewFilter) => {
      const b = selectedBranch.value;
      if (!b || b.isElse) return;
      patchBranchCase(b.splitId, b.index, { filter: v });
      // 同步到画布块（下一次结构镜像携带，避免删除其他分支时回退）
      canvasApi.current?.patchBranch(b.blockId, { filter: v });
    },
  });

  /** 分支面板：条件名称（case 专属；留空则卡片回落「条件N」；其他情况无） */
  const selectedBranchName = computed<string>({
    get: () => {
      const b = selectedBranch.value;
      if (!b || b.isElse) return '';
      return String(b.cases[b.index]?.name ?? '');
    },
    set: (v: string) => {
      const b = selectedBranch.value;
      if (!b || b.isElse) return;
      const name = String(v ?? '').trim();
      patchBranchCase(b.splitId, b.index, { name });
      // 同步到画布块（下一次结构镜像携带，避免分支增删后丢失）
      canvasApi.current?.patchBranch(b.blockId, { name });
    },
  });

  /** 删除当前条件分支（其他情况不可删；画布可用时走 FlowGram 删除块） */
  function removeSelectedBranch() {
    const b = selectedBranch.value;
    if (!b || b.isElse) return;
    if (canvasApi.current) {
      canvasApi.current.removeBranch(b.blockId);
    } else if (graph.value) {
      patchBranchData(b.splitId, { cases: b.cases.filter((_, i) => i !== b.index) });
    }
    selectedNodeId.value = null;
    propertyPanel.value = 'none';
  }

  function onResize() {
    narrow.value = window.innerWidth < 1024;
  }
  const route = useRoute();
  onMounted(async () => {
    onResize();
    window.addEventListener('resize', onResize);
    ensureCanvas();
    void loadEntities();
    void loadPhrases();
    await loadDefinitions();
    // 直达：/Cube/Workflow/Designer?id=<定义Id>（菜单 URL 直达设计器）；id 保持字符串透传
    const qid = route.query.id;
    if (qid) {
      const sid = String(qid);
      if (sid && definitions.value.some((d) => String(d.id) === sid)) void openDefinition(sid);
    }
  });
  onBeforeUnmount(() => {
    window.removeEventListener('resize', onResize);
    reactRoot?.unmount();
    reactRoot = null;
  });

  async function loadDefinitions() {
    defsLoading.value = true;
    try {
      const res = await cubeApi.workflow.definitions({});
      definitions.value = res.data ?? [];
    } catch (err) {
      definitions.value = [];
      Message.error(formatApiError(err, '加载流程定义失败'));
    } finally {
      defsLoading.value = false;
    }
  }

  async function openDefinition(id: number | string | null) {
    // 后端 Int64 雪花主键以字符串序列化（避免 JSON number 精度丢失），比较/透传一律按字符串
    const sid = id == null ? '' : String(id);
    currentId.value = sid || null;
    current.value = definitions.value.find((d) => String(d.id) === sid) ?? null;
    if (current.value) {
      graph.value = compileXorEdges(parseGraph(current.value.graphJson));
      selectedNodeId.value = null;
      writableFieldsLoading.value = true;
      try {
        filterFields.value = await loadFilterFields(current.value.typePath || '');
        writableFields.value = filterFields.value.map((f) => ({ name: f.name, label: f.displayName || f.name }));
      } finally {
        writableFieldsLoading.value = false;
      }
    } else {
      graph.value = null;
      writableFields.value = [];
      filterFields.value = [];
    }
    // 同步画布：labels/readonly 更新 + 数据装载（切换定义）
    renderCanvas();
    canvasApi.current?.load(graphToFlowDoc(graph.value));
  }

  async function createDefinition(typePath: string, name: string): Promise<string | null> {
    if (!typePath || !name) {
      Message.warning('请填写实体与名称');
      return null;
    }
    try {
      const res = await cubeApi.workflow.createDefinition({
        typePath,
        name,
        enable: true,
        lockPolicy: 'full',
        graphJson: compileGraph(newDefaultGraph()),
      });
      const d = res.data as WorkflowDefinitionItem;
      await loadDefinitions();
      await openDefinition(d.id);
      Message.success('已创建草稿');
      return String(d.id);
    } catch (err) {
      Message.error(formatApiError(err, '创建失败'));
      return null;
    }
  }

  function setGraph(next: WfGraphData | null) {
    graph.value = next ? compileXorEdges(next) : next;
  }

  /** 属性补丁写入选中节点；名称/分流/接收人变化同步画布卡片 */
  function patchSelected(patch: Record<string, unknown>) {
    if (!graph.value || !selectedNodeId.value) return;
    let next = applyNodePatch(graph.value, selectedNodeId.value, patch);
    if ('cases' in patch || 'defaultTarget' in patch) next = compileXorEdges(next);
    setGraph(next);
    if ('name' in patch || 'mode' in patch || 'to' in patch || 'toLabels' in patch || 'cases' in patch || 'defaultTarget' in patch || 'timeoutHours' in patch) {
      renderCanvas();
    }
  }

  /** 在选中节点后插入（优先 FlowGram 画布操作，其历史回调镜像回 Vue；无画布回退纯图插入） */
  function addNodeAfter(type: string) {
    const sel = selectedNodeId.value;
    if (!sel) return;
    if (canvasApi.current) {
      canvasApi.current.addAfter(sel, type);
      return;
    }
    if (!graph.value) return;
    let next = insertAfter(graph.value, sel, type);
    const added = next.nodes.find((n) => !graph.value!.nodes.some((o) => o.id === n.id));
    if (added && pendingInsertDraft.value) {
      next = applyNodePatch(next, added.id, pendingInsertDraft.value);
      pendingInsertDraft.value = null;
    }
    setGraph(next);
    selectedNodeId.value = added?.id ?? sel;
    renderCanvas();
  }

  /** 「+」菜单：条件直接插入；审批/办理/知会先弹窗配置签核模式与接收人 */
  function beginInsert(fromId: string, type: string) {
    selectedNodeId.value = fromId;
    if (type === 'oa.xor' || type === 'oa.parallel') {
      addNodeAfter(type);
      return;
    }
    if (type !== 'oa.approve' && type !== 'oa.handle' && type !== 'oa.cc') {
      addNodeAfter(type);
      return;
    }
    insertAfterId.value = fromId;
    insertType.value = type;
    insertMode.value = 'or';
    insertToKind.value = 'users';
    insertToIds.value = [];
    insertVisible.value = true;
  }

  function cancelInsert() {
    insertVisible.value = false;
    insertAfterId.value = null;
    pendingInsertDraft.value = null;
  }

  async function confirmInsert(): Promise<boolean> {
    if (!insertAfterId.value) return false;
    const kind = insertToKind.value;
    if (isSearchToKind(kind) && !insertToIds.value.length) {
      Message.warning(insertType.value === 'oa.cc' ? '请选择知会对象' : '请选择审批人');
      return false;
    }
    const mode = insertMode.value === 'and' ? 'and' : insertMode.value === 'sequence' ? 'sequence' : 'or';
    const ids = isSearchToKind(kind) ? [...insertToIds.value] : [];
    const labels = isSearchToKind(kind) ? await resolveRecipientLabels(kind as RecipientKind, ids) : [];
    const draft: Record<string, unknown> = {
      to: wfToJsonForKind(kind, ids),
      toLabels: labels,
      name: insertNodeTitle(insertType.value, mode, kind, labels),
    };
    if (insertType.value === 'oa.approve') {
      draft.mode = mode;
      if (mode === 'and') draft.quorum = undefined;
    }
    if (insertType.value === 'oa.handle') {
      // 办理节点仅 or/and（sequence 发布会被拒），依次归一为或签
      draft.mode = mode === 'and' ? 'and' : 'or';
    }
    pendingInsertDraft.value = draft;
    selectedNodeId.value = insertAfterId.value;
    insertVisible.value = false;
    addNodeAfter(insertType.value);
    insertAfterId.value = null;
    return true;
  }

  function removeSelected() {
    const sel = selectedNodeId.value;
    if (!sel) return;
    if (canvasApi.current) {
      canvasApi.current.removeNode(sel);
      return;
    }
    if (!graph.value) return;
    setGraph(removeNode(graph.value, sel));
    selectedNodeId.value = null;
    renderCanvas();
  }

  function selectNode(id: string | null) {
    selectedNodeId.value = id;
    propertyPanel.value = id ? 'node' : 'none';
    // 同步画布选中高亮（分支卡片也要激活态）
    renderCanvas();
  }

  function openFlowProperties() {
    selectedNodeId.value = null;
    propertyPanel.value = 'flow';
  }

  async function save(): Promise<boolean> {
    const def = current.value;
    if (!def || !graph.value) return false;
    saving.value = true;
    try {
      const g = stripAndQuorum(compileXorEdges(graph.value));
      await cubeApi.workflow.updateDefinition(def.id, {
        typePath: def.typePath,
        name: def.name,
        enable: def.enable,
        lockPolicy: def.lockPolicy || 'full',
        startFilter: def.startFilter || '{}',
        graphJson: compileGraph(g),
      });
      await loadDefinitions();
      await openDefinition(def.id);
      Message.success('草稿已保存');
      return true;
    } catch (err) {
      Message.error(formatApiError(err, '保存失败'));
      return false;
    } finally {
      saving.value = false;
    }
  }

  async function publish(): Promise<boolean> {
    const def = current.value;
    if (!def) return false;
    const g = graph.value ? stripAndQuorum(compileXorEdges(graph.value)) : null;
    const errors = g ? validateGraph(g) : [];
    if (errors.length) {
      Message.warning(errors.join('；'));
      return false;
    }
    // 发布前先把会签 quorum 清掉并落库，避免旧图仍带比例
    if (g && graph.value) {
      setGraph(g);
      saving.value = true;
      try {
        await cubeApi.workflow.updateDefinition(def.id, {
          typePath: def.typePath,
          name: def.name,
          enable: def.enable,
          lockPolicy: def.lockPolicy || 'full',
          startFilter: def.startFilter || '{}',
          graphJson: compileGraph(g),
        });
      } catch (err) {
        Message.error(formatApiError(err, '发布前保存失败'));
        saving.value = false;
        return false;
      }
    }
    saving.value = true;
    try {
      const res = await cubeApi.workflow.publishDefinition(def.id);
      const msg = (res as unknown as { message?: string })?.message || '已发布';
      Message.success(msg);
      await loadDefinitions();
      await openDefinition(def.id);
      return true;
    } catch (err) {
      Message.error(formatApiError(err, '发布失败'));
      return false;
    } finally {
      saving.value = false;
    }
  }

  async function loadPhrases() {
    try {
      const res = await cubeApi.workflow.phrases();
      phrases.value = res.data ?? [];
    } catch {
      phrases.value = [];
    }
  }

  function addPhrase() {
    const t = phraseDraft.value.trim();
    if (!t) return;
    if (phrases.value.some((p) => p.text === t)) {
      phraseDraft.value = '';
      return;
    }
    phrases.value = [...phrases.value, { id: Date.now(), text: t }];
    phraseDraft.value = '';
  }

  function removePhrase(id: number | string) {
    phrases.value = phrases.value.filter((p) => String(p.id) !== String(id));
  }

  /** 常用语就地改名（仅本地；落库走 savePhrases） */
  function updatePhrase(id: number | string, text: string) {
    const t = text.trim();
    if (!t) return;
    phrases.value = phrases.value.map((p) => (String(p.id) === String(id) ? { ...p, text: t } : p));
  }

  /** 常用语整体保存（就地管理：增/改/删后立即落库）；成功后重载，保持 id 与服务端一致 */
  async function savePhrases(): Promise<boolean> {
    try {
      await cubeApi.workflow.savePhrases(phrases.value.map((p) => p.text).filter(Boolean));
      await loadPhrases();
      return true;
    } catch (err) {
      Message.error(formatApiError(err, '保存常用语失败'));
      return false;
    }
  }

  return {
    definitions,
    defsLoading,
    currentId,
    current,
    graph,
    selectedNodeId,
    propertyPanel,
    selectedNode,
    chain,
    canEdit,
    narrow,
    saving,
    error,
    writableFields,
    writableFieldsLoading,
    filterFields,
    entityOptions,
    canvasEl,
    graphErrors,
    lockPolicy,
    startFilter,
    selectedToKind,
    selectedToIds,
    selectedToLabels,
    selectedStarterPickScope,
    selectedStarterPickMultiple,
    selectedFieldName,
    selectedFieldAs,
    selectedEmptyPolicy,
    selectedEmptyUserIds,
    selectedExcludeStats,
    rootExcludeStats,
    selectedName,
    selectedMode,
    selectedTimeoutHours,
    selectedTimeoutAction,
    selectedTimeoutToKind,
    selectedTimeoutToIds,
    selectedAddSign,
    selectedRollback,
    selectedTransfer,
    selectedWritable,
    selectedHasTo,
    selectedBranch,
    selectedBranchName,
    selectedBranchFilter,
    removeSelectedBranch,
    selectedDefaultTarget,
    nodeOptions,
    phrases,
    phrasesVisible,
    phraseDraft,
    addPhrase,
    removePhrase,
    updatePhrase,
    savePhrases,
    loadDefinitions,
    openDefinition,
    openFlowProperties,
    createDefinition,
    addNodeAfter,
    beginInsert,
    confirmInsert,
    cancelInsert,
    insertVisible,
    insertType,
    insertMode,
    insertToKind,
    insertToIds,
    removeSelected,
    patchSelected,
    setApproveMode,
    selectNode,
    save,
    publish,
  };
}

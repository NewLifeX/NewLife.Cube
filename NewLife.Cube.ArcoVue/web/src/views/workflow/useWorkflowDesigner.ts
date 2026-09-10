import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import type { WorkflowDefinitionItem } from '@cube/api-core';
import { Message } from '@arco-design/web-vue';
import { createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import type { WfGraphData, WfGraphNodeData } from '@/core/types/workflow';
import { wfRecipientTo, wfRecipientToJson } from '@/core/types/workflow';
import { defaultNodeDataFor, flowDocToGraph, graphToFlowDoc, type FlowDoc } from './flowgram/flowgramGraph';
import { FlowGramDesigner, type FlowGramApi } from './flowgram/FlowGramDesigner';
import type { RecipientKind } from './recipient';

/**
 * 流程设计器（OSC-26090347f1 T8e-FlowGram）：画布由 FlowGram.AI 固定布局承担（结构增删/拖拽/缩放），
 * 运行时是 C# 引擎，浏览器只读写定义图（design §4）。<1024 只读提示（IA §3.4）。
 * 纯图函数（parseGraph/newDefaultGraph/insertAfter/removeNode/applyPatch/topoChain）可单测。
 */

export function parseGraph(json: string | null | undefined): WfGraphData {
  if (json) {
    try {
      const obj = JSON.parse(json) as Partial<WfGraphData>;
      if (obj && Array.isArray(obj.nodes) && Array.isArray(obj.edges)) {
        return { version: obj.version ?? 1, nodes: obj.nodes, edges: obj.edges };
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

/** 在指定节点后插入新节点（单后继重接 after→new→old）；禁止在 end 后插入 */
export function insertAfter(graph: WfGraphData, afterId: string, type: string): WfGraphData {
  const node = graph.nodes.find((n) => n.id === afterId);
  if (!node || node.type === 'oa.end') return graph;
  const id = nextNodeId(graph.nodes.map((n) => n.id));
  const data: Record<string, unknown> = { name: defaultName(type) };
  if (type === 'oa.approve') {
    data.mode = 'or';
    data.to = wfRecipientToJson(undefined, [], 'users');
    data.fields = { visible: ['*'], writable: [] };
    data.timeoutHours = 0;
    data.timeoutAction = 'pass';
    data.allowAddSign = true;
    data.allowRollback = true;
  }
  if (type === 'oa.cc') data.to = wfRecipientToJson(undefined, [], 'users');
  const newNode: WfGraphNodeData = { id, type, data };
  const oldTarget = firstTarget(graph, afterId);
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
    if (n.type === 'oa.approve') {
      const to = wfRecipientTo(n.data?.to);
      if (to.ids.length === 0) errors.push(`「${(n.data?.name as string) || n.id}」缺少审批人`);
    }
  }
  return errors;
}

/** 定义行可见字段选项（GetPage.editForm 字段名） */
export async function loadWritableFields(typePath: string): Promise<{ name: string; label: string }[]> {
  try {
    const res = await cubeApi.page.getPage(typePath);
    const meta = ((res as { data?: unknown })?.data ?? res) as Record<string, unknown>;
    const list = (meta.editForm ?? meta.addForm ?? []) as { name?: string; displayName?: string }[];
    return list
      .filter((f) => !!f.name)
      .map((f) => ({ name: f.name as string, label: (f.displayName as string) || (f.name as string) }));
  } catch {
    return [];
  }
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
  const saving = ref(false);
  const error = ref('');
  const writableFields = ref<{ name: string; label: string }[]>([]);
  const narrow = ref(false);
  /** 可发起实体选项（新建草稿 typePath 下拉） */
  const entityOptions = ref<{ value: string; label: string }[]>([]);
  /** React 画布挂载点（WorkflowDesignerPage 提供 div） */
  const canvasEl = ref<HTMLDivElement | null>(null);
  let reactRoot: Root | null = null;
  /** FlowGram 画布桥接 API（ref 回调在每次 render 更新） */
  const canvasApi: { current: FlowGramApi | null } = { current: null };

  /** id → 卡片名称（label 权威来源：Vue graph） */
  function labelsOf(g: WfGraphData | null): Record<string, string> {
    const m: Record<string, string> = {};
    for (const n of g?.nodes ?? []) {
      const name = (n.data?.name as string) ?? '';
      if (name) m[n.id] = name;
    }
    return m;
  }

  /** 渲染/更新 React 画布（labels/readonly/doc 变更时调用；挂载前 no-op） */
  function renderCanvas() {
    if (!reactRoot) return;
    const g = graph.value;
    reactRoot.render(
      createElement(FlowGramDesigner, {
        doc: graphToFlowDoc(g),
        readonly: !canEdit.value,
        labels: labelsOf(g),
        ref: (api: FlowGramApi | null) => {
          canvasApi.current = api;
        },
        onSelectNode: (id: string) => {
          selectedNodeId.value = id;
        },
        onDocChanged: (d: FlowDoc) => {
          graphChangedFromCanvas(d);
        },
      }),
    );
  }

  /** React 画布结构/数据变更 → 镜像重建 Vue graph（保存/发布/属性面板读取唯一数据源） */
  function graphChangedFromCanvas(doc: FlowDoc) {
    const prevById = new Map((graph.value?.nodes ?? []).map((n) => [n.id, n] as const));
    let addedId: string | null = null;
    const raw = flowDocToGraph(doc);
    const nodes = raw.nodes.map((n) => {
      const old = prevById.get(n.id);
      // 已存在节点 data 以 Vue 镜像为准（FlowGram 不承载业务 data 语义）
      if (old && (!n.data || Object.keys(n.data).length === 0)) return { ...n, data: old.data };
      if (!old) addedId = n.id;
      if (!n.data || Object.keys(n.data).length === 0) n.data = defaultNodeDataFor(n.type);
      return n;
    });
    graph.value = { version: raw.version, nodes, edges: raw.edges };
    if (addedId) selectedNodeId.value = addedId;
    renderCanvas();
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

  /** 可配置实体（automation.entities update 权限） */
  async function loadEntities() {
    try {
      const res = await cubeApi.automation.entities('update');
      const rows = (res.data ?? []) as { typePath?: string; name?: string }[];
      entityOptions.value = rows
        .filter((r) => !!r.typePath)
        .map((r) => ({ value: r.typePath as string, label: (r.name as string) || (r.typePath as string) }));
    } catch {
      entityOptions.value = [];
    }
  }

  const selectedNode = computed(
    () => graph.value?.nodes.find((n) => n.id === selectedNodeId.value) ?? null,
  );
  const chain = computed(() => (graph.value ? topoChain(graph.value) : []));
  const canEdit = computed(() => !narrow.value && !!current.value);

  /** 选中节点当前接收人（规范化） */
  const selectedToKind = computed<RecipientKind>({
    get: () => wfRecipientTo(selectedNode.value?.data?.to).kind as RecipientKind,
    set: (kind: RecipientKind) => {
      // 切换用户/角色/部门时清空已选，避免旧 Id 落到新类别上导致下拉无法选
      patchSelected({ to: wfRecipientToJson(undefined, [], kind) });
    },
  });
  const selectedToIds = computed({
    get: () => wfRecipientTo(selectedNode.value?.data?.to).ids,
    set: (ids: number[]) => {
      patchSelected({
        to: wfRecipientToJson(selectedNode.value?.data?.to, ids, selectedToKind.value),
      });
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
  /** 通用布尔属性 */
  function boolProp(key: string) {
    return computed({
      get: () => selectedNode.value?.data?.[key] === true,
      set: (v: boolean) => patchSelected({ [key]: v }),
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
  const selectedAddSign = boolProp('allowAddSign');
  const selectedRollback = boolProp('allowRollback');
  /** 是否审批/知会节点（展示接收人/超时属性区） */
  const selectedHasTo = computed(
    () => !!selectedNode.value && ['oa.approve', 'oa.cc'].includes(selectedNode.value.type),
  );

  function onResize() {
    narrow.value = window.innerWidth < 1024;
  }
  const route = useRoute();
  onMounted(async () => {
    onResize();
    window.addEventListener('resize', onResize);
    ensureCanvas();
    void loadEntities();
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
      graph.value = parseGraph(current.value.graphJson);
      selectedNodeId.value = null;
      writableFields.value = await loadWritableFields(current.value.typePath || '');
    } else {
      graph.value = null;
      writableFields.value = [];
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
    graph.value = next;
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
    const next = insertAfter(graph.value, sel, type);
    setGraph(next);
    const added = [...next.nodes].sort((a, b) => b.id.localeCompare(a.id))[0];
    selectedNodeId.value = added?.id ?? sel;
    renderCanvas();
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

  /** 属性补丁写入选中节点；name 变化同步画布卡片标签 */
  function patchSelected(patch: Record<string, unknown>) {
    if (!graph.value || !selectedNodeId.value) return;
    setGraph(applyNodePatch(graph.value, selectedNodeId.value, patch));
    if ('name' in patch) renderCanvas();
  }

  function selectNode(id: string | null) {
    selectedNodeId.value = id;
  }

  async function save(): Promise<boolean> {
    const def = current.value;
    if (!def || !graph.value) return false;
    saving.value = true;
    try {
      await cubeApi.workflow.updateDefinition(def.id, {
        typePath: def.typePath,
        name: def.name,
        enable: def.enable,
        lockPolicy: def.lockPolicy,
        graphJson: compileGraph(graph.value),
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
    const errors = graph.value ? validateGraph(graph.value) : [];
    if (errors.length) {
      Message.warning(errors.join('；'));
      return false;
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

  return {
    definitions,
    defsLoading,
    currentId,
    current,
    graph,
    selectedNodeId,
    selectedNode,
    chain,
    canEdit,
    narrow,
    saving,
    error,
    writableFields,
    entityOptions,
    canvasEl,
    selectedToKind,
    selectedToIds,
    selectedName,
    selectedMode,
    selectedTimeoutHours,
    selectedTimeoutAction,
    selectedAddSign,
    selectedRollback,
    selectedWritable,
    selectedHasTo,
    loadDefinitions,
    openDefinition,
    createDefinition,
    addNodeAfter,
    removeSelected,
    patchSelected,
    selectNode,
    save,
    publish,
  };
}

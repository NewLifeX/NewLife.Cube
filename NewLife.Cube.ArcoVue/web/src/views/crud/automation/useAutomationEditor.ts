import { computed, reactive, ref, watch } from 'vue';
import { Message, Modal } from '@arco-design/web-vue';
import type { AutomationRunItem } from '@cube/api-core';
import cubeApi from '@/api';
import type { FieldMeta } from '@/core/types/field';
import {
  AUTOMATION_ACTION_TYPES,
  AUTOMATION_MENU_ACTION_TYPES,
  AUTOMATION_TRIGGER_KINDS,
  clampDelayMinutes,
  compileAutomationGraph,
  normalizeTriggerConfig,
  parseAutomationGraph,
  validateFoundTargetChain,
  type ActionDraft,
} from '@/core/utils/automationGraph';
import {
  FILTER_OP_LABELS,
  FILTER_OPS_BY_KIND,
  draftToFilter,
  filterToDraftRows,
  newFilterDraftRow,
  opNeedsValue,
  resolveFieldFilterKind,
  type FilterDraftRow,
} from '@/core/utils/filterBuilder';
import type { ViewFilter, ViewFilterOp } from '@/core/utils/viewProfile';
import { formatApiError } from '@/core/utils/apiError';
import { formatDateTime } from '@/core/utils/datetime';

interface EditorProps {
  typePath: string;
  fields: FieldMeta[];
  editId: number | 'new';
}

interface EditorEmits {
  saved: [];
  back: [];
  footerChange: [payload: { saving: boolean; summary: string }];
}

type Emit = <K extends keyof EditorEmits>(event: K, ...args: EditorEmits[K]) => void;

export const TRIGGER_OPTIONS: { value: (typeof AUTOMATION_TRIGGER_KINDS)[number]; label: string }[] = [
  { value: 'insert', label: '添加新记录时' },
  { value: 'update', label: '修改记录时' },
  { value: 'delete', label: '删除记录时' },
  { value: 'insertOrUpdateIf', label: '新增或修改时' },
  { value: 'fieldChange', label: '字段变更时' },
  { value: 'dateArrive', label: '到达记录中的时间' },
  { value: 'schedule', label: '定时触发' },
  { value: 'button', label: '点击按钮时' },
  { value: 'webhook', label: '收到 Webhook 时' },
];

export const ACTION_OPTIONS: {
  value: (typeof AUTOMATION_MENU_ACTION_TYPES)[number];
  label: string;
  icon: string;
}[] = [
  { value: 'notify', label: '发送通知', icon: 'message' },
  { value: 'updateRecord', label: '修改记录', icon: 'edit' },
  { value: 'createRecord', label: '创建记录', icon: 'file-addition' },
  { value: 'findRecords', label: '查找记录', icon: 'search' },
  { value: 'httpRequest', label: '发送 HTTP 请求', icon: 'link' },
  { value: 'delay', label: '延时', icon: 'timer' },
  { value: 'addComment', label: '添加评论', icon: 'comments' },
  { value: 'aiText', label: 'AI 文本', icon: 'font-size' },
];

function emptyAction(type: string): ActionDraft {
  const data: Record<string, unknown> =
    type === 'notify'
      ? { channel: 'InApp', to: { kind: 'users', users: [], roles: [], departments: [] }, title: '', body: '' }
      : type === 'updateRecord'
        ? { target: 'current', fields: [{ name: '', value: '' }] }
        : type === 'createRecord'
          ? { typePath: '', fields: [{ name: '', value: '' }] }
          : type === 'findRecords'
            ? { typePath: '', limit: 20, filter: { logic: 'all', conditions: [] } }
            : type === 'httpRequest'
              ? { method: 'POST', url: '', body: '', headers: {} }
              : type === 'delay'
                ? { minutes: 1 }
                : type === 'runAutomation'
                  ? { automationId: 0 }
                  : type === 'addComment'
                    ? { target: 'current', content: '' }
                    : type === 'aiText'
                      ? { prompt: '', outputField: '' }
                      : {};
  return { type, data };
}

/** 归一动作 data：去掉未实现的 updateRecord.created；补齐 notify 接收人结构 */
function normalizeActionDraft(a: ActionDraft): ActionDraft {
  const data: Record<string, unknown> = { ...(a.data ?? {}) };
  if (a.type === 'updateRecord') {
    if (String(data.target ?? '') === 'created') data.target = 'current';
    if (!Array.isArray(data.fields)) data.fields = [{ name: '', value: '' }];
  }
  if (a.type === 'notify') {
    const to = (data.to && typeof data.to === 'object' ? { ...(data.to as object) } : {}) as Record<
      string,
      unknown
    >;
    const asArr = (v: unknown) =>
      (Array.isArray(v) ? v : [])
        .map((x) => Number(x))
        .filter((n) => Number.isFinite(n) && n > 0);
    let kind = String(to.kind ?? '').toLowerCase();
    if (kind === 'user') kind = 'users';
    if (kind === 'role') kind = 'roles';
    if (kind === 'department' || kind === 'dept') kind = 'departments';
    if (!['users', 'roles', 'departments'].includes(kind)) {
      const u = asArr(to.users).length;
      const r = asArr(to.roles).length;
      const d = asArr(to.departments).length;
      kind = r && !u && !d ? 'roles' : d && !u && !r ? 'departments' : 'users';
    }
    if (to.mode === 'userId' && Number(to.userId) > 0 && kind === 'users') {
      to.users = [Number(to.userId)];
    }
    to.kind = kind;
    to.users = kind === 'users' ? asArr(to.users) : [];
    to.roles = kind === 'roles' ? asArr(to.roles) : [];
    to.departments = kind === 'departments' ? asArr(to.departments) : [];
    data.to = to;
  }
  if (a.type === 'addComment' && !data.target) data.target = 'current';
  if (a.type === 'findRecords' && !data.filter) data.filter = { logic: 'all', conditions: [] };
  return { type: a.type, data };
}

export function useAutomationEditor(props: EditorProps, emit: Emit) {
  const mainTab = ref<'edit' | 'runs'>('edit');
  const saving = ref(false);
  const loading = ref(false);
  const parseError = ref('');
  const name = ref('未命名自动化');
  const enable = ref(true);
  const priority = ref(100);
  const version = ref(1);
  const triggerKind = ref<(typeof AUTOMATION_TRIGGER_KINDS)[number]>('insert');
  const triggerConfig = reactive({
    watchFields: [] as string[],
    field: '',
    offsetMinutes: 0,
    once: true,
    cron: '',
    label: '运行',
    requirePermission: 'detail',
    requireSignature: false,
  });
  const hookToken = ref('');
  const filterLogic = ref<'all' | 'any'>('all');
  const filterRows = ref<FilterDraftRow[]>([]);
  const actions = ref<ActionDraft[]>([]);
  const runsLoading = ref(false);
  const runs = ref<(AutomationRunItem & { timeText?: string })[]>([]);

  const isNew = computed(() => props.editId === 'new');
  const canViewRuns = computed(() => !isNew.value);
  const hookUrl = computed(() =>
    hookToken.value ? `${window.location.origin}/Cube/Automation/Hook/${hookToken.value}` : '',
  );

  const summaryText = computed(() => {
    const trig = TRIGGER_OPTIONS.find((x) => x.value === triggerKind.value)?.label || triggerKind.value;
    const act = actions.value[0]
      ? ACTION_OPTIONS.find((x) => x.value === actions.value[0].type)?.label || actions.value[0].type
      : '指定操作';
    const cond =
      filterRows.value.length > 0
        ? filterLogic.value === 'any'
          ? '且满足任一条件时'
          : '且同时满足条件时'
        : '';
    return `当「${trig}」${cond}，就执行「${act}」${actions.value.length > 1 ? `等 ${actions.value.length} 个动作` : ''}`;
  });

  const actionLabelMap = computed(() => {
    const m: Record<string, string> = {};
    for (const o of ACTION_OPTIONS) m[o.value] = o.label;
    m.runAutomation = '运行自动化';
    return m;
  });

  const foundWarnings = computed(() => {
    const issues = validateFoundTargetChain(actions.value, actionLabelMap.value);
    const map: Record<number, string> = {};
    for (const i of issues) map[i.index] = i.message;
    return map;
  });

  function foundWarningAt(i: number): string {
    return foundWarnings.value[i] || '';
  }

  const runColumns = [
    { title: '时间', dataIndex: 'timeText', width: 160 },
    { title: '触发', dataIndex: 'triggerKind', width: 100 },
    { title: '状态', dataIndex: 'status', width: 90 },
    { title: '记录', dataIndex: 'recordKey', width: 90 },
    { title: '错误', dataIndex: 'error' },
  ];

  function resetDraft() {
    mainTab.value = 'edit';
    parseError.value = '';
    name.value = '未命名自动化';
    enable.value = true;
    priority.value = 100;
    version.value = 1;
    triggerKind.value = 'insert';
    triggerConfig.watchFields = [];
    triggerConfig.field = '';
    triggerConfig.offsetMinutes = 0;
    triggerConfig.once = true;
    triggerConfig.cron = '';
    triggerConfig.label = '运行';
    triggerConfig.requirePermission = 'detail';
    triggerConfig.requireSignature = false;
    hookToken.value = '';
    filterLogic.value = 'all';
    filterRows.value = [];
    actions.value = [emptyAction('notify')];
    runs.value = [];
  }

  async function load() {
    resetDraft();
    if (isNew.value) return;
    loading.value = true;
    try {
      const res = await cubeApi.automation.get(props.editId as number);
      const d = res.data;
      if (!d) return;
      name.value = d.name;
      enable.value = d.enable;
      priority.value = d.priority ?? 100;
      version.value = d.version;
      triggerKind.value = (d.triggerKind as (typeof AUTOMATION_TRIGGER_KINDS)[number]) || 'insert';
      hookToken.value = d.hookToken || '';
      try {
        const cfg = JSON.parse(d.triggerConfig || '{}') as Record<string, unknown>;
        Object.assign(triggerConfig, normalizeTriggerConfig(triggerKind.value, cfg));
      } catch {
        /* keep defaults */
      }
      try {
        const g = JSON.parse(d.graphJson || '{}');
        const parsed = parseAutomationGraph(g);
        parseError.value = parsed.error || '';
        filterLogic.value = parsed.filter.logic;
        filterRows.value = filterToDraftRows(parsed.filter);
        actions.value = parsed.actions.length
          ? parsed.actions.map(normalizeActionDraft)
          : [emptyAction('notify')];
      } catch {
        parseError.value = '请用表单重建';
      }
    } catch (err) {
      Message.error(formatApiError(err, '加载失败'));
    } finally {
      loading.value = false;
    }
  }

  async function loadRuns() {
    if (isNew.value) {
      runs.value = [];
      return;
    }
    runsLoading.value = true;
    try {
      const res = await cubeApi.automation.runs({
        typePath: props.typePath.replace(/^\/+/, ''),
        automationId: props.editId as number,
        pageIndex: 1,
        pageSize: 50,
      });
      runs.value = (res.data ?? []).map((r) => ({
        ...r,
        timeText: formatDateTime(r.createTime),
      }));
    } catch {
      runs.value = [];
    } finally {
      runsLoading.value = false;
    }
  }

  watch(
    () => props.editId,
    () => {
      void load();
    },
    { immediate: true },
  );

  watch(mainTab, (tab) => {
    if (tab === 'runs') void loadRuns();
  });

  function addFilterRow() {
    filterRows.value.push(newFilterDraftRow());
  }
  function removeFilterRow(i: number) {
    filterRows.value.splice(i, 1);
  }
  function onFilterField(row: FilterDraftRow) {
    const ops = opsOf(row.cond.field);
    if (!ops.includes(row.cond.op)) row.cond.op = ops[0] ?? 'eq';
    row.cond.value = undefined;
  }

  /** 条件区字段：优先父组件传入，空则拉 Meta 兜底（避免 listFields 未就绪时整块消失） */
  const metaFields = ref<FieldMeta[]>([]);
  const conditionFields = computed(() => {
    const fromProps = (props.fields ?? []).filter((f) => !!f?.name);
    if (fromProps.length) return fromProps;
    return metaFields.value;
  });

  async function ensureConditionFields() {
    if ((props.fields ?? []).length > 0) return;
    const tp = (props.typePath || '').replace(/^\/+/, '');
    if (!tp) return;
    try {
      const res = await cubeApi.automation.meta(tp, { kind: 'search' });
      const list = res.data ?? [];
      if (list.length) {
        metaFields.value = list.map((f) => ({
          name: f.name,
          displayName: f.displayName,
          typeName: f.typeName || 'String',
          primaryKey: f.primaryKey,
          readOnly: f.readOnly,
        }));
        return;
      }
      const all = await cubeApi.automation.meta(tp, { kind: 'all' });
      metaFields.value = (all.data ?? []).map((f) => ({
        name: f.name,
        displayName: f.displayName,
        typeName: f.typeName || 'String',
        primaryKey: f.primaryKey,
        readOnly: f.readOnly,
      }));
    } catch {
      metaFields.value = [];
    }
  }

  watch(
    () => [props.typePath, props.fields?.length] as const,
    () => {
      void ensureConditionFields();
    },
    { immediate: true },
  );

  function opsOf(fieldName: string): ViewFilterOp[] {
    const f =
      conditionFields.value.find((x) => x.name === fieldName) ||
      props.fields.find((x) => x.name === fieldName);
    if (!f) return ['eq', 'neq', 'contains', 'isNull', 'notNull'];
    return [...FILTER_OPS_BY_KIND[resolveFieldFilterKind(f)]];
  }

  /** 字段变更触发：勾选同步 watchFields，嵌套「变更为」写入 filter */
  const isFieldChangeTrigger = computed(() => triggerKind.value === 'fieldChange');

  const conditionSectionTitle = computed(() =>
    isFieldChangeTrigger.value ? '关注以下任一字段的变更' : '同时满足以下条件',
  );

  const conditionNestLabel = computed(() =>
    isFieldChangeTrigger.value ? '变更为' : '满足',
  );

  const showFilterLogic = computed(() => !isFieldChangeTrigger.value);

  /** 条件区始终展示（字段未就绪时显示空态 + 添加入口） */
  const showConditionList = computed(() => true);

  function findFilterRow(fieldName: string): FilterDraftRow | undefined {
    return filterRows.value.find((r) => r.cond.field === fieldName);
  }

  function ensureFilterRow(fieldName: string): FilterDraftRow {
    let row = findFilterRow(fieldName);
    if (row) return row;
    const ops = opsOf(fieldName);
    row = { cond: { field: fieldName, op: ops[0] ?? 'eq', value: undefined } };
    filterRows.value.push(row);
    return row;
  }

  function isConditionFieldChecked(fieldName: string): boolean {
    if (isFieldChangeTrigger.value) {
      return (triggerConfig.watchFields || []).includes(fieldName) || !!findFilterRow(fieldName);
    }
    return !!findFilterRow(fieldName);
  }

  function setConditionFieldChecked(fieldName: string, checked: boolean) {
    if (checked) {
      if (isFieldChangeTrigger.value && !triggerConfig.watchFields.includes(fieldName)) {
        triggerConfig.watchFields = [...triggerConfig.watchFields, fieldName];
      }
      ensureFilterRow(fieldName);
      return;
    }
    if (isFieldChangeTrigger.value) {
      triggerConfig.watchFields = triggerConfig.watchFields.filter((x) => x !== fieldName);
    }
    const i = filterRows.value.findIndex((r) => r.cond.field === fieldName);
    if (i >= 0) filterRows.value.splice(i, 1);
  }

  function selectAllConditionFields() {
    for (const f of conditionFields.value) {
      if (f.name) setConditionFieldChecked(f.name, true);
    }
  }

  function clearAllConditionFields() {
    if (isFieldChangeTrigger.value) triggerConfig.watchFields = [];
    filterRows.value = [];
  }

  function conditionRowOf(fieldName: string): FilterDraftRow {
    return ensureFilterRow(fieldName);
  }

  /** 添加一条条件：优先勾选下一个未选字段并展开；否则追加自由行 */
  function addConditionField() {
    const next = conditionFields.value.find((f) => f.name && !isConditionFieldChecked(f.name));
    if (next?.name) {
      setConditionFieldChecked(next.name, true);
      return;
    }
    addFilterRow();
  }

  /** 不在勾选列表中的自由条件行（兼容旧数据 / 字段列表外条件） */
  const orphanFilterRows = computed(() => {
    const names = new Set(conditionFields.value.map((f) => f.name));
    return filterRows.value
      .map((row, index) => ({ row, index }))
      .filter(({ row }) => !row.cond.field || !names.has(row.cond.field));
  });

  function addAction(type: string) {
    if (actions.value.length >= 20) {
      Message.warning('动作不能超过 20 个');
      return;
    }
    const draft = emptyAction(type);
    if (type === 'findRecords') {
      draft.data = {
        ...(draft.data ?? {}),
        typePath: props.typePath.replace(/^\/+/, ''),
      };
    }
    actions.value.push(draft);
  }
  function removeAction(i: number) {
    actions.value.splice(i, 1);
  }
  function moveAction(i: number, dir: -1 | 1) {
    const j = i + dir;
    if (j < 0 || j >= actions.value.length) return;
    const arr = actions.value.slice();
    const t = arr[i];
    arr[i] = arr[j];
    arr[j] = t;
    actions.value = arr;
  }

  function actionLabel(type: string) {
    return ACTION_OPTIONS.find((x) => x.value === type)?.label ?? type;
  }

  async function copyToken() {
    if (!hookToken.value) return;
    try {
      await navigator.clipboard.writeText(hookToken.value);
      Message.success('已复制');
    } catch {
      Message.error('复制失败');
    }
  }

  function regenHook() {
    Modal.confirm({
      title: '重新生成令牌？',
      content: '旧 Webhook 地址将立即失效',
      onOk: async () => {
        await save({ regen: true });
      },
    });
  }

  async function save(opts?: { regen?: boolean; forceEnable?: boolean; stay?: boolean }) {
    const nm = name.value.trim();
    if (nm.length < 1 || nm.length > 50) {
      Message.error('名称长度须为 1–50');
      return;
    }
    if (parseError.value === '请用表单重建') {
      Message.error('当前图无法保存，请用表单重建');
      return;
    }
    const chainIssues = validateFoundTargetChain(actions.value, actionLabelMap.value);
    if (chainIssues.length) {
      Message.error(chainIssues[0].message);
      return;
    }
    const filter: ViewFilter = draftToFilter(filterLogic.value, filterRows.value);
    const act = actions.value
      .filter((a) => AUTOMATION_ACTION_TYPES.includes(a.type as (typeof AUTOMATION_ACTION_TYPES)[number]))
      .slice(0, 20)
      .map((a) => {
        const normalized = normalizeActionDraft(a);
        const data = { ...(normalized.data ?? {}) };
        if (normalized.type === 'delay') data.minutes = clampDelayMinutes(data.minutes);
        // 字段赋值去掉空 name
        if (Array.isArray(data.fields)) {
          data.fields = (data.fields as { name?: string; value?: string }[])
            .filter((f) => (f.name || '').trim())
            .slice(0, 32);
        }
        return { type: normalized.type, data };
      });
    const cfg = normalizeTriggerConfig(triggerKind.value, { ...triggerConfig });
    compileAutomationGraph({ triggerKind: triggerKind.value, filter, actions: act });
    if (opts?.forceEnable) enable.value = true;
    saving.value = true;
    try {
      const body = {
        id: isNew.value ? undefined : (props.editId as number),
        typePath: props.typePath.replace(/^\/+/, ''),
        name: nm,
        enable: enable.value,
        priority: priority.value,
        triggerKind: triggerKind.value,
        triggerConfig: cfg,
        version: version.value,
        filter,
        actions: act,
        regenHook: !!opts?.regen,
      };
      const res = isNew.value
        ? await cubeApi.automation.create(body)
        : await cubeApi.automation.update(body);
      if (res.data?.hookToken) hookToken.value = res.data.hookToken;
      if (res.data?.version) version.value = res.data.version;
      Message.success(opts?.forceEnable ? '已保存并启用' : '已保存');
      if (!opts?.stay) emit('saved');
    } catch (err) {
      Message.error(formatApiError(err, '保存失败'));
    } finally {
      saving.value = false;
    }
  }

  function saveOnly() {
    if (isNew.value) enable.value = false;
    return save();
  }

  function saveAndEnable() {
    return save({ forceEnable: true });
  }

  /** 运行日志条目展示辅助（SFC 薄脚本，全部集中于此） */
  function runTitle(r: { name?: string | null }) {
    const name = (r.name || '').trim() || '未命名流程';
    return `流程 「${name}」`;
  }

  /** 详情去掉「流程「名」」前缀，避免与标题重复 */
  function runDetailBody(r: { detail?: string | null; name?: string | null; status?: string }) {
    const d = (r.detail || '').trim();
    if (!d) return runStatusLabel(r.status);
    const m = d.match(/^流程\s*「[^」]+」\s*(.*)$/s);
    const body = (m?.[1] || d).trim();
    return body || runStatusLabel(r.status);
  }

  function runStatusTone(status?: string) {
    const s = (status || '').toLowerCase();
    if (s === 'succeeded') return 'ok';
    if (s === 'failed') return 'fail';
    if (s === 'running' || s === 'queued') return 'run';
    if (s === 'waiting') return 'wait';
    return 'muted';
  }

  function runStatusIcon(status?: string) {
    const tone = runStatusTone(status);
    if (tone === 'ok') return 'check';
    if (tone === 'fail') return 'close';
    if (tone === 'run') return 'refresh';
    if (tone === 'wait') return 'time';
    return 'info';
  }

  function runDotColor(status?: string) {
    const tone = runStatusTone(status);
    if (tone === 'ok') return 'rgb(var(--success-6))';
    if (tone === 'fail') return 'rgb(var(--danger-6))';
    if (tone === 'run') return 'rgb(var(--primary-6))';
    if (tone === 'wait') return 'rgb(var(--warning-6))';
    return 'var(--color-text-3)';
  }

  function runStatusLabel(status?: string) {
    const s = (status || '').toLowerCase();
    if (s === 'succeeded') return '执行成功';
    if (s === 'failed') return '执行失败';
    if (s === 'running') return '执行中';
    if (s === 'queued') return '排队中';
    if (s === 'waiting') return '等待中';
    return status || '未知状态';
  }

  // 保存中 / 摘要变化时同步给抽屉底栏（原在 SFC 内，迁入以满足薄脚本门禁）
  watch(
    [saving, summaryText],
    () => {
      emit('footerChange', { saving: saving.value, summary: summaryText.value });
    },
    { immediate: true },
  );

  return {
    mainTab,
    saving,
    loading,
    parseError,
    name,
    enable,
    priority,
    triggerKind,
    triggerConfig,
    hookToken,
    hookUrl,
    filterLogic,
    filterRows,
    actions,
    isNew,
    canViewRuns,
    summaryText,
    runs,
    runsLoading,
    runColumns,
    FILTER_OP_LABELS,
    opNeedsValue,
    opsOf,
    addFilterRow,
    removeFilterRow,
    onFilterField,
    isFieldChangeTrigger,
    conditionSectionTitle,
    conditionNestLabel,
    showFilterLogic,
    showConditionList,
    conditionFields,
    isConditionFieldChecked,
    setConditionFieldChecked,
    selectAllConditionFields,
    clearAllConditionFields,
    conditionRowOf,
    addConditionField,
    orphanFilterRows,
    addAction,
    removeAction,
    moveAction,
    actionLabel,
    foundWarningAt,
    copyToken,
    regenHook,
    saveOnly,
    saveAndEnable,
    runTitle,
    runDetailBody,
    runStatusTone,
    runStatusIcon,
    runDotColor,
    runStatusLabel,
    TRIGGER_OPTIONS,
    ACTION_OPTIONS,
  };
}

/** 供抽屉 footer 同步摘要与 loading */
export type AutomationEditorFooterPayload = { saving: boolean; summary: string };

/**
 * 效率页（design §6）：时间窗 + 六维聚合 + 点行下钻 + 月/节点行展开最慢 10 条。
 * 三个汇总数与分组无关（后端算）；页面单层不换页，点标签回到上一级聚合。
 */
import { computed, ref, watch } from 'vue';
import type {
  WorkflowEfficiencyResult,
  WorkflowEfficiencyRow,
  WorkflowEfficiencySlowItem,
} from '@newlifex/api-core';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import type { WfId } from './useWorkflowProgress';

/** 聚合维度（design §6.3） */
export type EffGroupBy = 'process' | 'department' | 'user' | 'node' | 'year' | 'month';

/** 时间窗：近 7/30/90 天或按年份 */
export type EffWindow = 'days7' | 'days30' | 'days90' | 'year';

/** 筛选标签键 */
export type EffTagKey = 'definitionId' | 'departmentId' | 'userId' | 'year';

/** 表上方筛选标签 */
export interface EffTag {
  key: EffTagKey;
  text: string;
}

/** 耗时文案（design §6.4）：不足 1 小时「N 分钟」；1~48 小时「N.N 小时」；达 48 小时「N.N 天」 */
export function formatDuration(hours?: number | null): string {
  if (hours == null || !Number.isFinite(hours)) return '—';
  if (hours < 1) return `${Math.round(hours * 60)} 分钟`;
  if (hours < 48) return `${hours.toFixed(1)} 小时`;
  return `${(hours / 24).toFixed(1)} 天`;
}

/** 无行，或每行样本都是 0（按年固定三行）时不画表 */
export function rowsLookEmpty(rows: { count?: number }[]): boolean {
  return rows.length === 0 || rows.every((r) => (r.count ?? 0) === 0);
}

/** 比例文案（后端给 0~1；无样本 null → 「—」） */
export function formatRate(rate?: number | null): string {
  if (rate == null || !Number.isFinite(rate)) return '—';
  return `${Math.round(rate * 100)}%`;
}

/** 点行下钻（design §6.3）；month/node 行返回 null（就地展开，不改聚合） */
export function drillOf(
  current: EffGroupBy,
  rowKey: string,
  definitionId?: string,
): { groupBy: EffGroupBy; patch: Partial<Record<EffTagKey, string>> } | null {
  switch (current) {
    case 'process':
      return { groupBy: 'node', patch: { definitionId: rowKey } };
    case 'department':
      return { groupBy: 'user', patch: { departmentId: rowKey } };
    case 'year':
      return { groupBy: 'month', patch: { year: rowKey } };
    case 'user':
      return definitionId
        ? { groupBy: 'node', patch: { userId: rowKey } }
        : { groupBy: 'process', patch: { userId: rowKey } };
    default:
      return null;
  }
}

/** 去掉某标签后的聚合（回到上一级） */
export function removeTagOf(key: EffTagKey, definitionId?: string): EffGroupBy {
  switch (key) {
    case 'definitionId':
      return 'process';
    case 'departmentId':
      return 'department';
    case 'userId':
      return definitionId ? 'node' : 'process';
    case 'year':
      return 'year';
  }
}

/** 请求参数（与后端 GET /Workflow/Efficiency 对齐） */
export interface EffQuery {
  groupBy: EffGroupBy;
  days?: number;
  year?: string;
  definitionId?: string;
  departmentId?: string;
  userId?: string;
  nodeId?: string;
  month?: string;
}

/** 组装查询（groupBy=year 固定三年、忽略时间窗；yearFilter 优先于时间窗） */
export function buildQuery(o: {
  groupBy: EffGroupBy;
  window: EffWindow;
  yearValue: string;
  yearFilter?: string;
  definitionId?: string;
  departmentId?: string;
  userId?: string;
  nodeId?: string;
  month?: string;
}): EffQuery {
  const q: EffQuery = { groupBy: o.groupBy };
  if (o.groupBy !== 'year') {
    if (o.yearFilter) q.year = o.yearFilter;
    else if (o.window === 'year') q.year = o.yearValue;
    else q.days = o.window === 'days7' ? 7 : o.window === 'days90' ? 90 : 30;
  }
  if (o.definitionId) q.definitionId = o.definitionId;
  if (o.departmentId) q.departmentId = o.departmentId;
  if (o.userId) q.userId = o.userId;
  if (o.nodeId) q.nodeId = o.nodeId;
  if (o.month) q.month = o.month;
  return q;
}

/** 效率页状态与请求 */
export function useWorkflowEfficiency() {
  const nowYear = new Date().getFullYear();
  /** 时间窗（按年份时时提供今年 / 前年 / 前两年三选） */
  const window = ref<EffWindow>('days30');
  const yearValue = ref(String(nowYear));
  const yearChoices = [String(nowYear), String(nowYear - 1), String(nowYear - 2)];

  const groupBy = ref<EffGroupBy>('process');
  const definitionId = ref('');
  const departmentId = ref('');
  const userId = ref('');
  const yearFilter = ref('');
  const labelMap = ref<Partial<Record<EffTagKey, string>>>({});

  const loading = ref(false);
  const error = ref('');
  const data = ref<WorkflowEfficiencyResult | null>(null);

  /** 已展开行（月/节点）与最慢记录缓存 */
  const expandedKeys = ref<(string | number)[]>([]);
  const slowMap = ref<Record<string, WorkflowEfficiencySlowItem[]>>({});
  const slowLoading = ref('');

  /** 进度抽屉（点最慢记录的标题打开） */
  const progressVisible = ref(false);
  const progressInstanceId = ref<WfId | null>(null);

  const baseOf = () => ({
    window: window.value,
    yearValue: yearValue.value,
    yearFilter: yearFilter.value || undefined,
    definitionId: definitionId.value || undefined,
    departmentId: departmentId.value || undefined,
    userId: userId.value || undefined,
  });

  const query = computed<EffQuery>(() => buildQuery({ groupBy: groupBy.value, ...baseOf() }));

  const rows = computed(() => data.value?.rows ?? []);
  const summary = computed(() => ({
    avgText: formatDuration(data.value?.avgHours),
    rateText: formatRate(data.value?.completionRate),
    overdueOpen: data.value?.overdueOpen ?? 0,
    truncated: !!data.value?.truncated,
  }));
  const isEmpty = computed(() => !loading.value && !!data.value && rowsLookEmpty(rows.value));

  /** 未选流程时「节点」不可点（后端 400） */
  const canPickNode = computed(() => !!definitionId.value);
  /** 仅月、节点两行可展开最慢 10 条 */
  const canExpandRows = computed(() => groupBy.value === 'month' || groupBy.value === 'node');

  async function load() {
    loading.value = true;
    error.value = '';
    try {
      const res = await cubeApi.workflow.efficiency(query.value);
      data.value = res.data ?? null;
    } catch (err) {
      data.value = null;
      error.value = formatApiError(err, '效率统计加载失败');
    } finally {
      loading.value = false;
    }
  }

  watch(
    query,
    () => {
      expandedKeys.value = [];
      slowMap.value = {};
      void load();
    },
    { immediate: true },
  );

  /** 改时间窗时清掉年下钻标签（否则时间窗改了也不生效） */
  watch(window, () => {
    yearFilter.value = '';
  });

  /** 点行：下钻筛选；月/节点行就地展开最慢 10 条 */
  function pickRow(record: WorkflowEfficiencyRow) {
    if (canExpandRows.value) {
      expandedKeys.value = expandedKeys.value.includes(record.key) ? [] : [record.key];
      if (expandedKeys.value.length) void loadSlow(record.key);
      return;
    }
    const d = drillOf(groupBy.value, record.key, definitionId.value || undefined);
    if (!d) return;
    const k = Object.keys(d.patch)[0] as EffTagKey | undefined;
    if (k) labelMap.value = { ...labelMap.value, [k]: record.title };
    if (d.patch.definitionId !== undefined) definitionId.value = d.patch.definitionId;
    if (d.patch.departmentId !== undefined) departmentId.value = d.patch.departmentId;
    if (d.patch.userId !== undefined) userId.value = d.patch.userId;
    if (d.patch.year !== undefined) yearFilter.value = d.patch.year;
    groupBy.value = d.groupBy;
  }

  /** 展开行：再请求一次拿 slow（月行带 month、节点行带 nodeId） */
  async function loadSlow(key: string) {
    if (slowMap.value[key]) return;
    slowLoading.value = key;
    try {
      const q =
        groupBy.value === 'node'
          ? buildQuery({ ...baseOf(), groupBy: 'node', nodeId: key })
          : buildQuery({ ...baseOf(), groupBy: 'month', month: key });
      const res = await cubeApi.workflow.efficiency(q);
      slowMap.value = { ...slowMap.value, [key]: res.data?.slow ?? [] };
    } catch (err) {
      Message.error(formatApiError(err, '最慢记录加载失败'));
    } finally {
      slowLoading.value = '';
    }
  }

  const slowOf = (key: string) => slowMap.value[key] ?? [];

  /** 表格展开按钮触发（v-model:expanded-keys 已更新） */
  function onExpand(rowKey: string | number, record: WorkflowEfficiencyRow) {
    if (expandedKeys.value.map(String).includes(String(rowKey))) void loadSlow(record.key);
  }

  /** 手动切换聚合；「节点」需先选流程 */
  function pickGroupBy(g: EffGroupBy) {
    if (g === 'node' && !canPickNode.value) return;
    groupBy.value = g;
  }

  const tags = computed<EffTag[]>(() => {
    const out: EffTag[] = [];
    if (definitionId.value)
      out.push({ key: 'definitionId', text: labelMap.value.definitionId || '已选流程' });
    if (departmentId.value)
      out.push({ key: 'departmentId', text: labelMap.value.departmentId || '已选部门' });
    if (userId.value) out.push({ key: 'userId', text: labelMap.value.userId || '已选人员' });
    if (yearFilter.value)
      out.push({ key: 'year', text: labelMap.value.year || `${yearFilter.value}年` });
    return out;
  });

  /** 点标签：去掉该条件并回到上一级聚合 */
  function removeTag(key: EffTagKey) {
    if (key === 'definitionId') definitionId.value = '';
    else if (key === 'departmentId') departmentId.value = '';
    else if (key === 'userId') userId.value = '';
    else yearFilter.value = '';
    groupBy.value = removeTagOf(key, definitionId.value || undefined);
  }

  /** 最慢记录标题：打开进度抽屉 */
  function openProgress(instanceId: string) {
    progressInstanceId.value = instanceId;
    progressVisible.value = true;
  }

  return {
    window,
    yearValue,
    yearChoices,
    groupBy,
    canPickNode,
    tags,
    rows,
    summary,
    isEmpty,
    canExpandRows,
    expandedKeys,
    slowOf,
    slowLoading,
    loading,
    error,
    pickRow,
    onExpand,
    pickGroupBy,
    removeTag,
    openProgress,
    progressVisible,
    progressInstanceId,
    reload: load,
  };
}

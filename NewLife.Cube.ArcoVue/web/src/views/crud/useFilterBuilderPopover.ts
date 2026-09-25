import { computed, reactive, ref, watch } from 'vue';
import { hostRefField, isHostRefValue } from '@newlifex/api-core';
import type { FieldMeta } from '@/core/types/field';
import { normalizeFilter, type ViewFilter, type ViewFilterOp } from '@/core/utils/viewProfile';
import {
  resolveFieldFilterKind,
  FILTER_OPS_BY_KIND,
  FILTER_OP_LABELS,
  opNeedsValue,
  newFilterDraftRow,
  resetCondForField,
  draftToFilter,
  filterToDraftRows,
  type FilterDraftRow,
} from '@/core/utils/filterBuilder';
import { normalizeDataSource } from '@/core/utils/viewMapping';
import cubeApi from '@/api';

/** FilterBuilderPopover 组件 props 类型（与 FilterBuilderPopover.vue defineProps 泛型逐字一致） */
interface FilterBuilderPopoverProps {
  /** 弹层是否可见（由父级管理，与分组弹层互斥） */
  visible: boolean;
  /** 筛选候选字段（当前视图可见字段） */
  fields: FieldMeta[];
  /** 当前筛选方案（父级 viewProfile.getFilter） */
  modelValue: ViewFilter;
  /** 是否有命名视图可保存 */
  canSave: boolean;
  /** 宿主字段候选（OSC-260903e2a4）：非空时条件行值来源可切「值 / 宿主」；缺省不显示 */
  hostFields?: FieldMeta[];
  /** 是否显示「保存条件到此视图」按钮（OSC-260903e2a4）；部件配置传 false，默认 true */
  showSaveView?: boolean;
}

/** FilterBuilderPopover 组件 emits 类型（与 FilterBuilderPopover.vue defineEmits 泛型逐字一致） */
interface FilterBuilderPopoverEmits {
  'update:visible': [boolean];
  apply: [ViewFilter];
  save: [ViewFilter];
}

type FilterBuilderPopoverEmit = <K extends keyof FilterBuilderPopoverEmits>(event: K, ...args: FilterBuilderPopoverEmits[K]) => void;

/** FilterBuilderPopover 组件全部业务 TS：筛选条件草稿、字段候选与用户选项懒加载（自 FilterBuilderPopover.vue script setup 原样搬移） */
export function useFilterBuilderPopover(props: FilterBuilderPopoverProps, emit: FilterBuilderPopoverEmit) {
  function condFieldOf(name: string): FieldMeta | undefined {
    return props.fields.find((f) => f.name === name);
  }

  /** 字段筛选类别（枚举/字符/人员/数字/日期） */
  function kindOfName(name: string): ReturnType<typeof resolveFieldFilterKind> {
    const f = condFieldOf(name);
    return f ? resolveFieldFilterKind(f) : 'string';
  }

  /** 该行当前字段可用的操作符 */
  function opsOf(row: FilterDraftRow): readonly ViewFilterOp[] {
    return row.cond.field ? FILTER_OPS_BY_KIND[kindOfName(row.cond.field)] : ['eq'];
  }

  /** 枚举/值集下拉选项（dataSource 物化；无则空） */
  function enumOptionsOf(row: FilterDraftRow): { value: string; label: string }[] {
    const f = row.cond.field ? condFieldOf(row.cond.field) : undefined;
    if (f?.dataSource && Object.keys(f.dataSource).length) {
      return normalizeDataSource(f.dataSource).options;
    }
    return [];
  }

  /** 人员字段值控件需要用户实体下拉：懒加载 /Admin/User 前 500 条 */
  const userOptions = ref<{ value: string; label: string }[]>([]);
  const userLoading = ref(false);
  async function ensureUserOptions() {
    if (userOptions.value.length || userLoading.value) return;
    userLoading.value = true;
    try {
      const res = await cubeApi.page.getList('/Admin/User', { pageIndex: 0, pageSize: 500 });
      const rows = (res.data as Record<string, unknown>[]) || [];
      userOptions.value = rows.map((u) => {
        const id = u.id ?? u.Id;
        const name = u.name ?? u.Name ?? u.account ?? u.Account;
        return { value: String(id), label: String(name ?? id) };
      });
    } catch {
      /* 忽略：下拉保持空 */
    } finally {
      userLoading.value = false;
    }
  }

  /** 查询字段候选（OSC-260830a1b2）：优先主键字段排前，便于先按索引字段筛选；索引信息未在字段元数据下发时保持后端原序 */
  const fieldCandidates = computed(() => {
    const list = props.fields.filter((f) => !!f.name);
    const pk = list.filter((f) => f.primaryKey);
    const rest = list.filter((f) => !f.primaryKey);
    return [...pk, ...rest];
  });

  const draft = reactive<{ logic: 'all' | 'any'; rows: FilterDraftRow[] }>({
    logic: 'all',
    rows: [],
  });

  function syncDraftFromProps() {
    const f = normalizeFilter(props.modelValue);
    draft.logic = f.logic;
    draft.rows = filterToDraftRows(f);
    // 打开时若无条件，自动添加第一个字段为查询条件（OSC-260830a1b2）
    if (draft.rows.length === 0) {
      const first = fieldCandidates.value[0];
      if (first) {
        const row = newFilterDraftRow();
        row.cond.field = first.name;
        const kind = resolveFieldFilterKind(first);
        row.cond.op = FILTER_OPS_BY_KIND[kind][0];
        draft.rows.push(row);
      }
    }
  }

  function addCond() {
    draft.rows.push(newFilterDraftRow());
  }

  function removeCond(i: number) {
    draft.rows.splice(i, 1);
  }

  function onFieldChange(row: FilterDraftRow) {
    // 字段切换：op 重置为该类别默认（eq）并清空值
    resetCondForField(row.cond, kindOfName(row.cond.field));
    // 人员字段首次使用时懒加载用户下拉
    if (row.cond.field && kindOfName(row.cond.field) === 'person') void ensureUserOptions();
  }

  function onOpChange(row: FilterDraftRow) {
    // 操作符切换后清空旧值（如 isNull ↔ eq 之间的值残留）
    row.cond.value = undefined;
    row.cond.value2 = undefined;
  }

  /** 值控件统一写回（各控件类型 emit 的 value 直接落到条件值） */
  function onCondValue(row: FilterDraftRow, v: unknown) {
    row.cond.value = v;
  }

  // ---- 宿主字段引用（OSC-260903e2a4）：条件值可引用宿主页字段，存 { $host: 字段名 } ----
  const hasHost = computed(() => (props.hostFields ?? []).some((f) => !!f?.name));
  /** 宿主字段下拉选项（去重保序，中文 displayName 优先） */
  function hostOptions(): { value: string; label: string }[] {
    const seen = new Set<string>();
    const out: { value: string; label: string }[] = [];
    for (const f of props.hostFields ?? []) {
      if (!f?.name || seen.has(f.name)) continue;
      seen.add(f.name);
      out.push({ value: f.name, label: f.displayName || f.name });
    }
    return out;
  }
  /** 该行值来源：host=宿主字段引用（值为 $host 对象） */
  function valueModeOf(row: FilterDraftRow): 'fixed' | 'host' {
    return isHostRefValue(row.cond.value) ? 'host' : 'fixed';
  }
  /** 该行是否可切宿主引用：有宿主候选、已选字段、且操作符需要值 */
  function canHostValue(row: FilterDraftRow): boolean {
    return (
      hasHost.value &&
      !!row.cond.field &&
      opNeedsValue(row.cond.op)
    );
  }
  /** 切换值来源：宿主=保留当前宿主字段或取第一个候选；固定=清空值 */
  function onValueMode(row: FilterDraftRow, mode: unknown) {
    if (mode === 'host') {
      if (!isHostRefValue(row.cond.value)) {
        const first = (props.hostFields ?? []).find((f) => !!f?.name)?.name;
        row.cond.value = first ? { $host: first } : undefined;
      }
    } else {
      row.cond.value = undefined;
      row.cond.value2 = undefined;
    }
  }
  /** 宿主字段选择：写回 $host 对象 */
  function onHostValue(row: FilterDraftRow, v: unknown) {
    const field = String(v ?? '').trim();
    row.cond.value = field ? { $host: field } : undefined;
  }
  /** 当前宿主引用字段名（模板绑定用） */
  function hostFieldOf(row: FilterDraftRow): string {
    return hostRefField(row.cond.value) ?? '';
  }

  function resetDraft() {
    draft.logic = 'all';
    draft.rows = [];
  }

  function toFilter(): ViewFilter {
    return draftToFilter(draft.logic, draft.rows);
  }

  function emitApply() {
    emit('apply', toFilter());
    close();
  }

  function emitSave() {
    emit('save', toFilter());
  }

  function close() {
    emit('update:visible', false);
  }

  function onVisibleChange(v: boolean) {
    if (v) syncDraftFromProps();
    emit('update:visible', v);
  }

  // 父级直接关闭（互斥切换）时同步内部
  watch(
    () => props.visible,
    (v) => {
      if (v) syncDraftFromProps();
    },
  );

  return {
    FILTER_OP_LABELS,
    opNeedsValue,
    onVisibleChange,
    draft,
    fieldCandidates,
    opsOf,
    kindOfName,
    userLoading,
    userOptions,
    onCondValue,
    enumOptionsOf,
    condFieldOf,
    hasHost,
    hostOptions,
    valueModeOf,
    canHostValue,
    onValueMode,
    onHostValue,
    hostFieldOf,
    showSaveView: props.showSaveView !== false,
    removeCond,
    addCond,
    onFieldChange,
    onOpChange,
    resetDraft,
    emitSave,
    close,
    emitApply,
    syncDraftFromProps,
  };
}

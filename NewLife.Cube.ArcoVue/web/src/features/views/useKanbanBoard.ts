import { computed, reactive, watch } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import type { ColumnPref } from '@/core/utils/viewProfile';
import { bucketKanban, type KanbanMapping } from '@/core/utils/viewMapping';
import { getValueByKey } from '@/core/utils/url';
import { buildCardBodyFields, cardExcludeKeys, resolveImageUrl } from './cardHelpers';
import type { ViewFormatRule } from '@/core/utils/viewProfile';
import { resolveCardTitleFormat, resolveRowSideColor } from '@/core/utils/viewFormat';

/** KanbanBoard 组件 props 类型（与 KanbanBoard.vue defineProps 泛型逐字一致） */
interface KanbanBoardProps {
  records: Record<string, unknown>[];
  columns: ColumnPref[];
  fields: FieldMeta[];
  mapping?: KanbanMapping | null;
  rowKey: string;
  height?: number;
  canViewDetail: boolean;
  canEdit: boolean;
  canDelete: boolean;
  typePath?: string;
  formatCell?: (field: FieldMeta, record: Record<string, unknown>) => string;
  formatRules?: ViewFormatRule[];
}

/* ---------------- 滚动懒加载（每列先渲染 100 条，列内滚动到底动态追加） ---------------- */
/** 初始渲染条数与滚动追加步长 */
const INITIAL_VISIBLE = 100;
const LOAD_STEP = 100;

/** KanbanBoard 组件全部业务 TS：分桶列构建与列内滚动懒加载（自 KanbanBoard.vue script setup 原样搬移） */
export function useKanbanBoard(props: KanbanBoardProps) {
  const columns = computed(() => {
    if (!props.mapping?.groupField) return [];
    const field = props.fields.find((f) => f.name === props.mapping!.groupField);
    return bucketKanban(props.records, props.mapping.groupField, field?.dataSource);
  });

  /** 每列已渲染条数（key = 列 key；列头 count 仍显示总数 col.rows.length） */
  const colVisible = reactive<Record<string, number>>({});

  function initColVisibility(cols: typeof columns.value) {
    for (const k of Object.keys(colVisible)) delete colVisible[k];
    for (const col of cols) colVisible[col.key] = INITIAL_VISIBLE;
  }

  // 分组变化（数据/分组字段变更）→ 重置各列懒加载计数
  watch(
    () => columns.value,
    (cols) => {
      initColVisibility(cols);
    },
    { immediate: true },
  );

  function onColScroll(key: string, e: Event) {
    const el = e.currentTarget as HTMLElement;
    // 接近列底部（剩余不足 200px）时追加下一批
    if (el.scrollTop + el.clientHeight >= el.scrollHeight - 200) {
      const col = columns.value.find((c) => c.key === key);
      if (!col) return;
      const cur = colVisible[key] ?? INITIAL_VISIBLE;
      if (cur < col.rows.length) colVisible[key] = Math.min(col.rows.length, cur + LOAD_STEP);
    }
  }

  const exclude = computed(() =>
    props.mapping ? cardExcludeKeys(props.mapping) : [],
  );

  function rowKeyOf(row: Record<string, unknown>, idx: number) {
    const v = getValueByKey(row, props.rowKey);
    return v == null || v === '' ? idx : String(v);
  }

  function titleOf(row: Record<string, unknown>) {
    const key = props.mapping?.titleField;
    if (!key) return '-';
    const field = props.fields.find((f) => f.name === key);
    if (field && props.formatCell) return props.formatCell(field, row);
    const raw = getValueByKey(row, key);
    return raw == null || raw === '' ? '-' : String(raw);
  }

  function bodyOf(row: Record<string, unknown>) {
    return buildCardBodyFields(
      row,
      props.columns,
      props.fields,
      exclude.value,
      props.formatCell,
    );
  }

  function titleFormatColorOf(row: Record<string, unknown>) {
    return resolveCardTitleFormat(row, props.formatRules || [], props.fields)?.color;
  }

  function titleFormatBoldOf(row: Record<string, unknown>) {
    return !!resolveCardTitleFormat(row, props.formatRules || [], props.fields)?.bold;
  }

  function sideFormatColorOf(row: Record<string, unknown>) {
    return resolveRowSideColor(row, props.formatRules || [], props.fields);
  }

  return {
    columns,
    INITIAL_VISIBLE,
    colVisible,
    onColScroll,
    rowKeyOf,
    titleOf,
    bodyOf,
    titleFormatColorOf,
    titleFormatBoldOf,
    sideFormatColorOf,
    resolveImageUrl,
  };
}

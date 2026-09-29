import { computed, reactive, ref, watch } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import type { ColumnPref } from '@/core/utils/viewProfile';
import { bucketKanban, type KanbanMapping } from '@/core/utils/viewMapping';
import { wfRowEditLocked, wfRowWritable } from '@/core/types/workflow';
import {
  dragStartedFromControl,
  kanbanCardDraggable,
  kanbanDropAllowed,
  kanbanPatchValue,
} from './kanbanMove';
import { getValueByKey } from '@/core/utils/url';
import { resolveCellLabel } from '@/core/utils/fieldBadge';
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
  compact?: boolean;
  /** 分组字段在编辑表单且当前用户可更新 */
  canDragGroup?: boolean;
  /** 分组字段必填时，不能放到「未分组」 */
  groupRequired?: boolean;
  /** 当前命名视图。折叠按实体 + 视图 + 分组字段记住 */
  viewId?: string;
}

/* ---------------- 滚动懒加载（每列先渲染 100 条，列内滚动到底动态追加） ---------------- */
/** 初始渲染条数与滚动追加步长 */
const INITIAL_VISIBLE = 100;
const LOAD_STEP = 100;

/** 折叠集合切换：同一 key 两次回到原态（OSC-260926c2b8 列折叠纯函数） */
export function toggleCollapsed(keys: readonly string[], key: string): string[] {
  return keys.includes(key) ? keys.filter((k) => k !== key) : [...keys, key];
}

/** 折叠记在 sessionStorage，刷新和重新进入看板仍在；不写入 ViewProfile */
export function kanbanCollapsedStorageKey(typePath: string, viewId: string, groupField: string): string {
  return `cube.kanban.collapsed:${encodeURIComponent(typePath)}:${encodeURIComponent(viewId)}:${encodeURIComponent(groupField)}`;
}

/** 读出已折叠列 key。坏数据当全部展开 */
export function readKanbanCollapsed(storage: Storage, key: string): string[] {
  try {
    const raw = storage.getItem(key);
    if (!raw) return [];
    const parsed = JSON.parse(raw) as unknown;
    if (!Array.isArray(parsed)) return [];
    return parsed.filter((item): item is string => typeof item === 'string' && item.length > 0);
  } catch {
    return [];
  }
}

/** 全部展开时删掉记录，避免留下空数组 */
export function writeKanbanCollapsed(storage: Storage, key: string, keys: readonly string[]) {
  if (!keys.length) storage.removeItem(key);
  else storage.setItem(key, JSON.stringify(keys));
}

export interface KanbanMovePayload {
  row: Record<string, unknown>;
  field: string;
  value: unknown;
}

/** KanbanBoard 组件全部业务 TS：分桶列构建、列内滚动懒加载与跨列拖放 */
export function useKanbanBoard(
  props: KanbanBoardProps,
  emit: (event: 'move', payload: KanbanMovePayload) => void,
) {
  const columns = computed(() => {
    if (!props.mapping?.groupField) return [];
    const gf = props.mapping.groupField;
    const field =
      props.fields.find((f) => f.name === gf) ||
      props.fields.find((f) => (f.name || '').toLowerCase() === gf.toLowerCase());
    return bucketKanban(props.records, gf, field?.dataSource);
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

  /** 已折叠列。按实体、视图、分组字段记在 sessionStorage，刷新后仍收起 */
  const collapsedKeys = ref<string[]>([]);

  function collapsedStorageKey() {
    const field = props.mapping?.groupField || '';
    if (props.compact || !field) return '';
    return kanbanCollapsedStorageKey(props.typePath || '', props.viewId || '', field);
  }

  function sessionStore(): Storage | null {
    try {
      return typeof sessionStorage === 'undefined' ? null : sessionStorage;
    } catch {
      return null;
    }
  }

  function restoreCollapsed() {
    const key = collapsedStorageKey();
    const store = sessionStore();
    collapsedKeys.value = key && store ? readKanbanCollapsed(store, key) : [];
  }

  function persistCollapsed() {
    const key = collapsedStorageKey();
    const store = sessionStore();
    if (!key || !store) return;
    writeKanbanCollapsed(store, key, collapsedKeys.value);
  }

  watch(collapsedStorageKey, () => restoreCollapsed(), { immediate: true });

  /** 列是否折叠 */
  function isColumnCollapsed(key: string): boolean {
    return collapsedKeys.value.includes(key);
  }

  /** 列头点击折叠/展开；工作台迷你看板（compact）不参与 */
  function toggleColumn(key: string) {
    if (props.compact) return;
    collapsedKeys.value = toggleCollapsed(collapsedKeys.value, key);
    persistCollapsed();
  }

  const dragging = ref<{ row: Record<string, unknown>; fromKey: string } | null>(null);
  const dropKey = ref('');
  /** 本次按下是否落在按钮或操作区。dragstart 的 target 是可拖元素本身，不能再靠它判断 */
  const pressFromControl = ref(false);

  function groupFieldMeta() {
    const name = props.mapping?.groupField || '';
    return (
      props.fields.find((field) => field.name === name) ||
      props.fields.find((field) => (field.name || '').toLowerCase() === name.toLowerCase())
    );
  }

  function cardDraggable(row: Record<string, unknown>) {
    return kanbanCardDraggable({
      compact: !!props.compact,
      canDragGroup: !!props.canDragGroup,
      groupField: props.mapping?.groupField || '',
      locked: wfRowEditLocked(row),
      writable: wfRowWritable(row),
    });
  }

  function onCardPointerDown(event: MouseEvent) {
    pressFromControl.value = dragStartedFromControl(event.target);
  }

  function onCardDragStart(fromKey: string, row: Record<string, unknown>, event: DragEvent) {
    if (pressFromControl.value || dragStartedFromControl(event.target) || !cardDraggable(row)) {
      event.preventDefault();
      return;
    }
    dragging.value = { row, fromKey };
    event.dataTransfer?.setData('text/plain', fromKey);
    if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move';
  }

  function onCardDragEnd() {
    dragging.value = null;
    dropKey.value = '';
  }

  function isDraggingCard(row: Record<string, unknown>) {
    return dragging.value?.row === row;
  }

  function onColDragOver(toKey: string, event: DragEvent) {
    const drag = dragging.value;
    if (!drag || isColumnCollapsed(toKey)) return;
    if (kanbanDropAllowed(drag.fromKey, toKey, !!props.groupRequired) !== 'move') return;
    event.preventDefault();
    if (event.dataTransfer) event.dataTransfer.dropEffect = 'move';
    dropKey.value = toKey;
  }

  function onColDragLeave(toKey: string, event: DragEvent) {
    const current = event.currentTarget as HTMLElement | null;
    const next = event.relatedTarget as Node | null;
    if (current && next && current.contains(next)) return;
    if (dropKey.value === toKey) dropKey.value = '';
  }

  function onColDrop(toKey: string, event: DragEvent) {
    event.preventDefault();
    const drag = dragging.value;
    const field = props.mapping?.groupField || '';
    dragging.value = null;
    dropKey.value = '';
    if (!drag || !field || isColumnCollapsed(toKey)) return;
    if (kanbanDropAllowed(drag.fromKey, toKey, !!props.groupRequired) !== 'move') return;
    emit('move', {
      row: drag.row,
      field: groupFieldMeta()?.name || field,
      value: kanbanPatchValue(groupFieldMeta()?.typeName, toKey),
    });
  }

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
    const field =
      props.fields.find((f) => f.name === key) ||
      props.fields.find((f) => (f.name || '').toLowerCase() === key.toLowerCase());
    if (field && props.formatCell) return props.formatCell(field, row);
    const raw = getValueByKey(row, key);
    if (raw == null || raw === '') return '-';
    if (field) return resolveCellLabel(field, raw) || String(raw);
    return String(raw);
  }

  function bodyOf(row: Record<string, unknown>) {
    // compact 数据看板：按 props.columns（配置的显示字段）渲染正文；未配置则为空
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
    collapsedKeys,
    isColumnCollapsed,
    toggleColumn,
    dropKey,
    cardDraggable,
    onCardPointerDown,
    onCardDragStart,
    onCardDragEnd,
    isDraggingCard,
    onColDragOver,
    onColDragLeave,
    onColDrop,
  };
}

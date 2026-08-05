<template>
  <div ref="hostRef" class="cube-list-table" :style="{ height: height + 'px' }"></div>
</template>

<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { ListTable } from '@visactor/vtable';
import type { ColumnPref } from '@/core/utils/viewProfile';
import { frozenLeftCount } from '@/core/utils/viewProfile';
import { BADGE_BORDER_RADIUS, BADGE_PADDING } from '@/core/utils/fieldBadge';
import { getValueByKey } from '@/core/utils/url';
import {
  buildOpsParts,
  formatOpsLabel,
  resolveOpsActionByRatio,
  type OpsAction,
} from '@/core/utils/opsAction';

export interface ListTableColumnDef {
  pref: ColumnPref;
  title: string;
  format?: (row: Record<string, unknown>) => string;
  /** 状态/枚举徽章列 */
  badge?: boolean;
  badgeOf?: (row: Record<string, unknown>) => {
    label: string;
    buttonColor: string;
    buttonBorderColor: string;
    textColor: string;
  } | null;
  /** 启用/Enable 徽标：可点击切换启用/禁用（悬停显示 pointer） */
  enableToggle?: boolean;
}

const props = withDefaults(
  defineProps<{
    records: Record<string, unknown>[];
    columns: ListTableColumnDef[];
    rowKey: string;
    selectedKeys?: (string | number)[];
    showCheckbox?: boolean;
    height?: number;
    canEdit?: boolean;
    canDelete?: boolean;
    canViewDetail?: boolean;
    showExpand?: boolean;
    enableSort?: boolean;
    /** 服务端排序状态；用于表头升/降序图标（不走 VTable 内部排序） */
    sortState?: { field: string; desc: boolean } | null;
    /** 树视图：启用 VTable hierarchy（行含 children） */
    hierarchy?: boolean;
  }>(),
  {
    selectedKeys: () => [],
    showCheckbox: false,
    height: 480,
    canEdit: false,
    canDelete: false,
    canViewDetail: true,
    showExpand: false,
    enableSort: true,
    sortState: null,
    hierarchy: false,
  },
);

const emit = defineEmits<{
  rowClick: [row: Record<string, unknown>];
  rowDblClick: [row: Record<string, unknown>];
  selectionChange: [keys: (string | number)[]];
  columnsChange: [cols: ColumnPref[]];
  sortChange: [payload: { field: string; desc: boolean } | null];
  action: [payload: { action: 'detail' | 'edit' | 'delete'; row: Record<string, unknown> }];
  toggleEnable: [row: Record<string, unknown>];
}>();

const hostRef = ref<HTMLElement | null>(null);
let table: InstanceType<typeof ListTable> | null = null;
let applying = false;
let ro: ResizeObserver | null = null;

function rowId(row: Record<string, unknown>): string {
  // 字段名大小写容错（rowKey 为 FieldMeta.name PascalCase，数据行 key 为 camelCase）
  const v = getValueByKey(row, props.rowKey);
  return v == null ? '' : String(v);
}

function opsFlags() {
  return {
    canViewDetail: props.canViewDetail,
    canEdit: props.canEdit,
    canDelete: props.canDelete,
  };
}

function opsLabel(): string {
  return formatOpsLabel(buildOpsParts(opsFlags()));
}

function opsColumnWidth(): number {
  const n = buildOpsParts(opsFlags()).length;
  if (n <= 0) return 80;
  return Math.min(220, Math.max(72, n * 56));
}

/** 按点击在操作列内的横向位置解析动作 */
function resolveOpsClick(args: {
  col?: number;
  row?: number;
  event?: MouseEvent | PointerEvent | TouchEvent;
}): OpsAction | null {
  const flags = opsFlags();
  const parts = buildOpsParts(flags);
  if (!parts.length) return null;
  if (parts.length === 1 || !table || args.col == null || args.row == null) {
    return resolveOpsActionByRatio(0, flags);
  }
  try {
    const rect = table.getCellRect(args.col, args.row) as {
      left?: number;
      width?: number;
    };
    const ev = args.event as MouseEvent | undefined;
    if (rect && typeof rect.left === 'number' && typeof rect.width === 'number' && ev?.clientX != null) {
      const ratio = (ev.clientX - rect.left) / Math.max(1, rect.width);
      return resolveOpsActionByRatio(ratio, flags);
    }
  } catch {
    /* fall through */
  }
  return resolveOpsActionByRatio(0, flags);
}

function leadingCount(): number {
  return (props.showCheckbox ? 1 : 0) + (props.showExpand ? 1 : 0);
}

function buildColumns(): any[] {
  const cols: any[] = [];
  if (props.showExpand) {
    cols.push({
      field: '__expand',
      title: '',
      width: 40,
      dragHeader: false,
      sort: false,
      showSort: false,
      fieldFormat: () => '›',
    });
  }
  if (props.showCheckbox) {
    // VTable 要求 cellType/headerType，误用 type 会把勾选图标渲染成截断文本（如 "f..."）
    cols.push({
      field: '__checked',
      title: '',
      width: 48,
      headerType: 'checkbox',
      cellType: 'checkbox',
      dragHeader: false,
      sort: false,
      showSort: false,
    });
  }

  for (const c of props.columns.filter((x) => x.pref.visible)) {
    if (c.badge && c.badgeOf) {
      cols.push({
        field: c.pref.key,
        title: c.title,
        width: c.pref.width || 80,
        // 服务端排序：只显示图标，禁用 VTable 内部排序（见 sort_click）
        sort: false,
        showSort: props.enableSort,
        dragHeader: true,
        cellType: 'button',
        disable: true,
        fieldFormat: (rec: Record<string, unknown>) => {
          const b = c.badgeOf?.(rec);
          if (b) return b.label;
          return c.format ? c.format(rec) : '-';
        },
        style: (args: { table?: any; col?: number; row?: number }) => {
          const record = args.table?.getRecordByCell?.(args.col, args.row) as
            | Record<string, unknown>
            | undefined;
          const badge = record ? c.badgeOf?.(record) : null;
          const buttonStyle = badge
            ? {
                buttonColor: badge.buttonColor,
                buttonBorderColor: badge.buttonBorderColor,
                buttonBorderRadius: BADGE_BORDER_RADIUS,
                buttonPadding: BADGE_PADDING,
                buttonDisableColor: badge.buttonColor,
                buttonDisableBorderColor: badge.buttonBorderColor,
                buttonTextDisableColor: badge.textColor,
              }
            : {
                buttonColor: '#F2F3F5',
                buttonBorderColor: '#F2F3F5',
                buttonBorderRadius: BADGE_BORDER_RADIUS,
                buttonPadding: BADGE_PADDING,
                buttonDisableColor: '#F2F3F5',
                buttonDisableBorderColor: '#F2F3F5',
                buttonTextDisableColor: '#4E5969',
              };
          return {
            textAlign: 'center',
            color: badge?.textColor || '#4b5563',
            // 仅「启用/Enable」徽标可点击（pointer）；其它状态/枚举/值集徽标悬停不变（default）
            cursor: c.enableToggle ? 'pointer' : 'default',
            buttonStyle,
          };
        },
      });
      continue;
    }
    cols.push({
      field: c.pref.key,
      title: c.title,
      width: c.pref.width || 140,
      sort: false,
      showSort: props.enableSort,
      dragHeader: true,
      disableColumnResize: false,
      fieldFormat: (rec: Record<string, unknown>) => {
        if (c.format) return c.format(rec);
        const v = rec[c.pref.key];
        return v == null || v === '' ? '-' : String(v);
      },
    });
  }

  if (props.canViewDetail || props.canEdit || props.canDelete) {
    cols.push({
      field: '__ops',
      title: '操作',
      width: opsColumnWidth(),
      dragHeader: false,
      sort: false,
      showSort: false,
      disableColumnResize: true,
      fieldFormat: () => opsLabel(),
      style: {
        color: '#165DFF',
        cursor: 'pointer',
        textAlign: 'center',
      },
    });
  }

  return cols;
}

function frozenCount(): number {
  return leadingCount() + frozenLeftCount(props.columns.map((c) => c.pref));
}

function withChecks(records: Record<string, unknown>[]) {
  const selected = new Set((props.selectedKeys || []).map(String));
  return records.map((r) => ({
    ...r,
    __checked: selected.has(rowId(r)),
  }));
}

function buildOption(): any {
  const cols = buildColumns();
  if (props.hierarchy && cols.length) {
    const firstData = cols.find((c: { field?: string }) => c.field && c.field !== '__check' && c.field !== '__ops' && c.field !== '__expand');
    if (firstData) (firstData as { tree?: boolean }).tree = true;
  }
  const sortState = props.sortState?.field
    ? { field: props.sortState.field, order: props.sortState.desc ? 'desc' : 'asc' }
    : null;

  return {
    records: withChecks(props.records),
    columns: cols,
    frozenColCount: frozenCount(),
    rightFrozenColCount: props.canViewDetail || props.canEdit || props.canDelete ? 1 : 0,
    widthMode: 'standard',
    columnResizeMode: 'all',
    hover: { highlightMode: 'row', disableHeaderHover: true },
    // 禁用单元格选中框；勾选列负责多选。Hover 用整行高亮
    select: { highlightMode: 'row', disableSelect: true, disableHeaderSelect: true },
    tooltip: { isShowOverflowTextTooltip: true },
    // 服务端排序：图标状态由 sortState 驱动，数据不走 VTable 内部排序
    sortState,
    ...(props.hierarchy
      ? // VTable 的 hierarchyExpandLevel>1 时根节点才默认展开；设为 2 使树视图默认显示第一层子节点
        { hierarchyExpandLevel: 2, hierarchyIndent: 16 }
      : {}),
    // 默认表头与数据行区分；字体规范待 Harness「组件/场景」体系落地
    theme: {
      // 默认 cellBorderClipDirection=top-left 会裁掉底/右边；行分隔须用顶边
      // borderLineWidth: [上, 右, 下, 左] — 仅行分隔，无列分隔
      defaultStyle: {
        borderColor: '#E5E6EB',
        borderLineWidth: [1, 0, 0, 0],
      },
      headerStyle: {
        bgColor: '#F2F3F5',
        color: '#4E5969',
        fontWeight: 500,
        fontSize: 13,
        borderColor: '#E5E6EB',
        borderLineWidth: [1, 0, 0, 0],
      },
      bodyStyle: {
        bgColor: '#FFFFFF',
        color: '#1D2129',
        fontWeight: 400,
        fontSize: 13,
        borderColor: '#E5E6EB',
        borderLineWidth: [1, 0, 0, 0],
        hover: {
          cellBgColor: '#F7F8FA',
          inlineRowBgColor: '#F7F8FA',
        },
      },
      frameStyle: {
        borderColor: 'transparent',
        borderLineWidth: 0,
      },
      selectionStyle: {
        cellBorderLineWidth: 0,
        cellBgColor: 'transparent',
      },
    },
  };
}

function syncFromTable(colWidths?: number[]) {
  if (!table || applying) return;
  const define = (table.columns || []) as Array<{ field?: string | number; width?: number }>;
  const next: ColumnPref[] = props.columns.map((c) => ({ ...c.pref }));
  const byKey = new Map(next.map((c) => [c.key, c]));
  const orderedVisible: ColumnPref[] = [];

  define.forEach((col, idx) => {
    const field = String(col.field ?? '');
    if (!field || field === '__checked' || field === '__ops' || field === '__expand') return;
    const pref = byKey.get(field);
    if (!pref) return;
    pref.visible = true;
    const fromEvent = colWidths?.[idx];
    const w =
      typeof fromEvent === 'number' && fromEvent > 0
        ? fromEvent
        : typeof col.width === 'number'
          ? col.width
          : undefined;
    if (typeof w === 'number' && w > 0) pref.width = Math.round(w);
    orderedVisible.push(pref);
  });
  const hidden = next.filter((c) => !orderedVisible.some((v) => v.key === c.key));
  for (const h of hidden) h.visible = false;

  // 拖宽/拖表头只同步宽与顺序；frozen 由配置抽屉维护，避免被表头冻结数改写
  emit('columnsChange', [...orderedVisible, ...hidden]);
}

function fieldKey(field: unknown): string {
  if (Array.isArray(field)) return String(field[0] ?? '');
  return field == null ? '' : String(field);
}

function bindEvents() {
  if (!table) return;

  function resolveBodyRow(args: { row?: number; cellLocation?: string }) {
    if (args.cellLocation === 'columnHeader') return null;
    const headerLevels = table!.columnHeaderLevelCount || 1;
    return props.records[(args.row ?? 0) - headerLevels] || null;
  }

  table.on('click_cell', ((args: any) => {
    const row = resolveBodyRow(args);
    if (!row) return;
    const field = fieldKey(args.field);
    if (field === '__checked') return;
    // 单击仅处理操作列 / 展开列；数据行单击不打开详情
    if (field === '__expand') {
      if (props.canViewDetail) emit('action', { action: 'detail', row });
      return;
    }
    if (field === '__ops') {
      const action = resolveOpsClick(args);
      if (action) emit('action', { action, row });
      return;
    }
    // 「启用/Enable」徽标：直接切换启用/禁用（须父级授权 Update）
    const colDef = props.columns.find((c) => c.pref.key === field);
    if (colDef?.enableToggle && colDef.badge) {
      emit('toggleEnable', row);
      return;
    }
    emit('rowClick', row);
  }) as any);

  table.on('dblclick_cell', ((args: any) => {
    const row = resolveBodyRow(args);
    if (!row) return;
    const field = fieldKey(args.field);
    if (field === '__checked' || field === '__ops' || field === '__expand') return;
    if (props.canViewDetail) emit('rowDblClick', row);
  }) as any);

  table.on('checkbox_state_change', (() => {
    // VTable 勾选状态在 stateManager，不一定回写 records.__checked
    const records = (table!.records || []) as Record<string, unknown>[];
    let states: unknown;
    try {
      states = table!.getCheckboxState('__checked');
    } catch {
      states = undefined;
    }
    const out: (string | number)[] = [];
    for (let i = 0; i < records.length; i++) {
      const rec = records[i];
      const checked =
        Array.isArray(states) && i < states.length
          ? !!states[i]
          : !!(rec as { __checked?: unknown })?.__checked;
      if (!checked) continue;
      const id = rowId(rec);
      if (id) out.push(/^\d+$/.test(id) ? Number(id) : id);
    }
    emit('selectionChange', out);
  }) as any);

  table.on(
    'resize_column_end',
    ((args: { col?: number; colWidths?: number[] }) => {
      syncFromTable(args?.colWidths);
    }) as any,
  );
  table.on('change_header_position', (() => syncFromTable()) as any);

  table.on(
    'sort_click',
    ((args: any) => {
      if (!props.enableSort) return false;
      const field = fieldKey(args.field);
      if (!field || field === '__ops' || field === '__checked' || field === '__expand') return false;
      // return false 会跳过 VTable 内部状态推进，必须按业务 sortState 自行循环：无→升→降→无
      const cur = props.sortState;
      let next: { field: string; desc: boolean } | null;
      if (!cur || cur.field !== field) next = { field, desc: false };
      else if (!cur.desc) next = { field, desc: true };
      else next = null;
      emit('sortChange', next);
      return false;
    }) as any,
  );
}

function mountTable() {
  if (!hostRef.value) return;
  table?.release();
  applying = true;
  table = new ListTable(hostRef.value, buildOption());
  applying = false;
  bindEvents();
  if (!ro && typeof ResizeObserver !== 'undefined') {
    ro = new ResizeObserver(() => {
      try {
        table?.resize?.();
      } catch {
        /* ignore */
      }
    });
    ro.observe(hostRef.value);
  }
  void nextTick(() => {
    try {
      table?.resize?.();
    } catch {
      /* ignore */
    }
  });
}

function refreshOption() {
  if (!table) {
    mountTable();
    return;
  }
  applying = true;
  table.updateOption(buildOption());
  applying = false;
}

onMounted(mountTable);
onBeforeUnmount(() => {
  ro?.disconnect();
  ro = null;
  table?.release();
  table = null;
});

// 注意：不要把 selectedKeys 放进全量 refresh 依赖——勾选后回写会 updateOption，冲掉 VTable 勾选态
watch(
  () => [
    props.records,
    props.columns,
    props.showCheckbox,
    props.canEdit,
    props.canDelete,
    props.canViewDetail,
    props.showExpand,
    props.enableSort,
    props.sortState?.field,
    props.sortState?.desc,
    props.hierarchy,
  ],
  () => refreshOption(),
  { deep: true },
);

/** 父级清空选择时同步勾选 UI（不整表 refresh，避免打断勾选交互） */
watch(
  () => props.selectedKeys,
  (keys) => {
    if (!table || applying) return;
    if ((keys?.length ?? 0) > 0) return;
    applying = true;
    try {
      table.setRecords?.(withChecks(props.records), { sortState: null });
      if (props.sortState?.field) {
        table.updateSortState?.(
          { field: props.sortState.field, order: props.sortState.desc ? 'desc' : 'asc' },
          false,
        );
      }
    } catch {
      refreshOption();
    } finally {
      applying = false;
    }
  },
);
</script>

<style scoped>
.cube-list-table {
  width: 100%;
  max-width: 100%;
  min-width: 0;
  min-height: 320px;
  border: none;
  overflow: hidden;
  background: var(--color-bg-2);
  box-sizing: border-box;
}
</style>

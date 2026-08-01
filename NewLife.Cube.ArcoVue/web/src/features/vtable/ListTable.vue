<template>
  <div ref="hostRef" class="cube-list-table" :style="{ height: height + 'px' }"></div>
</template>

<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { ListTable } from '@visactor/vtable';
import type { ColumnPref } from '@/core/utils/entityViewProfile';
import { frozenLeftCount } from '@/core/utils/entityViewProfile';
import { BADGE_BORDER_RADIUS, BADGE_PADDING } from '@/core/utils/fieldBadge';

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
  },
);

const emit = defineEmits<{
  rowClick: [row: Record<string, unknown>];
  rowDblClick: [row: Record<string, unknown>];
  selectionChange: [keys: (string | number)[]];
  columnsChange: [cols: ColumnPref[]];
  sortChange: [payload: { field: string; desc: boolean } | null];
  action: [payload: { action: 'detail' | 'edit' | 'delete'; row: Record<string, unknown> }];
}>();

const hostRef = ref<HTMLElement | null>(null);
let table: InstanceType<typeof ListTable> | null = null;
let applying = false;
let ro: ResizeObserver | null = null;

function rowId(row: Record<string, unknown>): string {
  const v = row[props.rowKey];
  return v == null ? '' : String(v);
}

function opsLabel(): string {
  const parts: string[] = [];
  if (props.canViewDetail) parts.push('详情');
  if (props.canEdit) parts.push('编辑');
  if (props.canDelete) parts.push('删除');
  return parts.length ? parts.join(' · ') : '-';
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
      fieldFormat: () => '›',
    });
  }
  if (props.showCheckbox) {
    cols.push({
      type: 'checkbox',
      field: '__checked',
      width: 48,
    });
  }

  for (const c of props.columns.filter((x) => x.pref.visible)) {
    if (c.badge && c.badgeOf) {
      cols.push({
        field: c.pref.key,
        title: c.title,
        width: c.pref.width || 80,
        sort: props.enableSort,
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
      sort: props.enableSort,
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
      width: 168,
      dragHeader: false,
      sort: false,
      disableColumnResize: true,
      fieldFormat: () => opsLabel(),
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
  return {
    records: withChecks(props.records),
    columns: buildColumns(),
    frozenColCount: frozenCount(),
    rightFrozenColCount: props.canViewDetail || props.canEdit || props.canDelete ? 1 : 0,
    widthMode: 'standard',
    columnResizeMode: 'all',
    hover: { highlightMode: 'row', disableHeaderHover: true },
    // 禁用单元格选中框；勾选列负责多选。Hover 用整行高亮
    select: { highlightMode: 'row', disableSelect: true, disableHeaderSelect: true },
    tooltip: { isShowOverflowTextTooltip: true },
    // 默认表头与数据行区分；字体规范待 Harness「组件/场景」体系落地
    theme: {
      defaultStyle: {
        borderColor: '#E5E6EB',
        borderLineWidth: 1,
      },
      headerStyle: {
        bgColor: '#F2F3F5',
        color: '#4E5969',
        fontWeight: 500,
        fontSize: 13,
        borderColor: '#E5E6EB',
        borderLineWidth: 1,
      },
      bodyStyle: {
        bgColor: '#FFFFFF',
        color: '#1D2129',
        fontWeight: 400,
        fontSize: 13,
        borderColor: '#E5E6EB',
        borderLineWidth: 1,
        hover: {
          cellBgColor: '#F7F8FA',
          inlineRowBgColor: '#F7F8FA',
        },
      },
      frameStyle: {
        borderColor: '#E5E6EB',
        borderLineWidth: 1,
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

  // 冻结入口已禁用：保留偏好中的 frozen，避免拖宽时被表头冻结数改写
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
    if (field === '__expand' || field === '__ops') {
      if (props.canViewDetail) emit('action', { action: 'detail', row });
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
    const records = (table!.records || []) as Record<string, unknown>[];
    const out: (string | number)[] = [];
    for (const rec of records) {
      if (rec.__checked) {
        const id = rowId(rec);
        if (id) out.push(/^\d+$/.test(id) ? Number(id) : id);
      }
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
      if (args.order === 'normal') emit('sortChange', null);
      else emit('sortChange', { field, desc: args.order === 'desc' });
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

watch(
  () => [
    props.records,
    props.columns,
    props.showCheckbox,
    props.selectedKeys,
    props.canEdit,
    props.canDelete,
    props.canViewDetail,
    props.showExpand,
    props.enableSort,
  ],
  () => refreshOption(),
  { deep: true },
);
</script>

<style scoped>
.cube-list-table {
  width: 100%;
  max-width: 100%;
  min-width: 0;
  min-height: 320px;
  border: 1px solid var(--color-border);
  border-radius: 4px;
  overflow: hidden;
  background: var(--color-bg-2);
  box-sizing: border-box;
}
</style>

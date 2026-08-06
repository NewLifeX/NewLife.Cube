<template>
  <!-- hover 表头时显示各列边界分隔线，辅助定位列宽拖拽区（VTable 原生分隔线仅拖动时显示） -->
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
    /** 分组视图（OSC-0015）：records 含 __groupHeader 组头节点行，组头跨列显示并浅色区分 */
    grouped?: boolean;
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
    grouped: false,
  },
);

const emit = defineEmits<{
  rowClick: [row: Record<string, unknown>];
  rowDblClick: [row: Record<string, unknown>];
  selectionChange: [keys: (string | number)[]];
  columnsChange: [cols: ColumnPref[]];
  sortChange: [payload: { field: string; desc: boolean } | null];
  action: [payload: { action: 'detail' | 'edit' | 'delete'; row: Record<string, unknown> }];
  toggleEnable: [row: Record<string, unknown>, field: string];
}>();

const hostRef = ref<HTMLElement | null>(null);
let table: InstanceType<typeof ListTable> | null = null;
let applying = false;
let ro: ResizeObserver | null = null;
let sepRaf = 0;
/** 分隔线层由 JS 动态创建：VTable 构造时会清空宿主容器，模板子元素会被删除 */
let sepEl: HTMLElement | null = null;

/** 确保分隔线层存在（VTable 创建/重建后调用） */
function ensureSeparatorLayer(): HTMLElement | null {
  const host = hostRef.value;
  if (!host) return null;
  let el = host.querySelector<HTMLElement>('.cube-table-separators');
  if (!el) {
    el = document.createElement('div');
    el.className = 'cube-table-separators';
    el.setAttribute('aria-hidden', 'true');
    host.appendChild(el);
  }
  sepEl = el;
  return el;
}

/** 清除表头分隔线 */
function clearHeaderSeparators() {
  const el = sepEl;
  if (!el) return;
  el.classList.remove('show');
  el.replaceChildren();
}

/**
 * hover 表头时渲染各列边界分隔线（OSC-0014）。
 * 仅当指针位于表头区域时显示；列边界取 VTable getCellRelativeRect（相对容器视口坐标，含冻结/滚动）。
 */
function updateHeaderSeparators(ev: MouseEvent) {
  const host = hostRef.value;
  const sep = sepEl;
  const t = table;
  if (!host || !sep || !t) return;
  const hostRect = host.getBoundingClientRect();
  const x = ev.clientX - hostRect.left;
  const y = ev.clientY - hostRect.top;
  if (x < 0 || y < 0) {
    clearHeaderSeparators();
    return;
  }
  // 表头区域判定：y 小于表头总高度（表头冻结，不随垂直滚动移动）
  const headerLevels = t.columnHeaderLevelCount || 1;
  let headerBottom = 0;
  try {
    headerBottom = t.getRowsHeight(0, headerLevels - 1);
  } catch {
    /* ignore */
  }
  if (y > headerBottom) {
    clearHeaderSeparators();
    return;
  }
  // 收集各列右边界 x（相对容器视口）
  const bounds: number[] = [];
  const colCount = t.colCount;
  for (let c = 0; c < colCount; c++) {
    try {
      const r = t.getCellRelativeRect(c, 0) as { left?: number; width?: number };
      if (r && typeof r.left === 'number' && typeof r.width === 'number' && r.width > 0) {
        const right = r.left + r.width;
        if (right > 0 && right <= host.clientWidth + 1) bounds.push(Math.round(right));
      }
    } catch {
      /* ignore */
    }
  }
  const uniq = [...new Set(bounds)];
  if (!uniq.length) {
    clearHeaderSeparators();
    return;
  }
  // 分隔线仅在表头区域显示（高度=表头总高），不贯穿数据区
  const headerHeight = headerBottom;
  // 复用已有节点，避免高频 mousemove 频繁重建 DOM
  let nodes = sep.children;
  for (let i = 0; i < uniq.length; i++) {
    let div = nodes[i] as HTMLElement | undefined;
    if (!div) {
      div = document.createElement('div');
      div.className = 'sep';
      sep.appendChild(div);
    }
    div.style.left = uniq[i] - 1 + 'px';
    div.style.height = headerHeight + 'px';
  }
  while (sep.children.length > uniq.length) sep.removeChild(sep.lastChild!);
  sep.classList.add('show');
}

function onHostMouseMove(ev: MouseEvent) {
  cancelAnimationFrame(sepRaf);
  sepRaf = requestAnimationFrame(() => updateHeaderSeparators(ev));
}

function onHostMouseLeave() {
  cancelAnimationFrame(sepRaf);
  clearHeaderSeparators();
}

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
  /** 数据列计数（用于组头行跨列显示：仅首数据列显示 label） */
  let dataColCount = 0;
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
    const isFirstDataCol = dataColCount === 0;
    dataColCount += 1;
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
        // 勿用列级 disable:true——VTable 会强制渲染禁用态（not-allowed 光标），且覆盖下方 style.cursor；
        // 徽标是否可点由 click_cell 按 enableToggle 分发（非 Enable 徽标点击仍走 rowClick，不启停）
        fieldFormat: (rec: Record<string, unknown>) => {
          const gh = groupHeaderFormat(rec, isFirstDataCol);
          if (gh !== undefined) return gh;
          const b = c.badgeOf?.(rec);
          if (b) return b.label;
          return c.format ? c.format(rec) : '-';
        },
        style: (args: { table?: any; col?: number; row?: number }) => {
          const record = args.table?.getRecordByCell?.(args.col, args.row) as
            | Record<string, unknown>
            | undefined;
          const ghs = groupHeaderStyle(record);
          if (ghs) return ghs;
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
            // 仅「启用/Enable」徽标可点击（pointer）；其它状态/枚举/值集徽标正常指示（default）
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
        const gh = groupHeaderFormat(rec, isFirstDataCol);
        if (gh !== undefined) return gh;
        if (c.format) return c.format(rec);
        const v = rec[c.pref.key];
        return v == null || v === '' ? '-' : String(v);
      },
      style: (args: { table?: any; col?: number; row?: number }) => {
        const record = args.table?.getRecordByCell?.(args.col, args.row) as
          | Record<string, unknown>
          | undefined;
        return groupHeaderStyle(record) ?? undefined;
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
    // 组头行不参与勾选
    __checked: r.__groupHeader ? false : selected.has(rowId(r)),
  }));
}

/** 组头行显示文本（首数据列）或空串（其余列）；count 取组头节点自身字段 */
function groupHeaderFormat(rec: Record<string, unknown> | undefined, isFirstDataCol: boolean): string | undefined {
  const gh = rec?.__groupHeader as { label?: string } | undefined;
  if (!gh) return undefined;
  return isFirstDataCol ? `📁 ${gh.label ?? ''} (${rec?.count ?? 0})` : '';
}

/** 组头行背景浅色 + 加粗，与普通行区分 */
function groupHeaderStyle(rec: Record<string, unknown> | undefined): Record<string, unknown> | null {
  if (!rec?.__groupHeader) return null;
  return { bgColor: '#F7F8FA', color: '#1D2129', fontWeight: 600 };
}

function buildOption(): any {
  const cols = buildColumns();
  if (props.hierarchy && cols.length) {
    const firstData = cols.find((c: { field?: string }) => c.field && c.field !== '__check' && c.field !== '__ops' && c.field !== '__expand');
    if (firstData) (firstData as { tree?: boolean }).tree = true;
  }
  // 分组视图：组头节点行含 children，同样以 hierarchy 渲染；默认展开一级
  if (props.grouped) {
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
    ...(props.hierarchy || props.grouped
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
    if (row.__groupHeader) return; // 组头行不响应点击
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
    // Boolean 徽标（Enable 及任意 Boolean 字段）：可点击切换（须父级授权 Update）；携带字段名供切换对应字段
    const colDef = props.columns.find((c) => c.pref.key === field);
    if (colDef?.enableToggle && colDef.badge) {
      emit('toggleEnable', row, colDef.pref.key);
      return;
    }
    emit('rowClick', row);
  }) as any);

  table.on('dblclick_cell', ((args: any) => {
    const row = resolveBodyRow(args);
    if (!row) return;
    if (row.__groupHeader) return; // 组头行不响应双击
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
      if ((rec as { __groupHeader?: unknown })?.__groupHeader) continue; // 组头行不参与勾选
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
  // VTable 构造会清空宿主容器（innerHTML=''），必须在创建后重建分隔线层
  ensureSeparatorLayer();
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

onMounted(() => {
  mountTable();
  // 捕获阶段监听，避免 VTable 内部 stopPropagation 吞掉 mousemove
  hostRef.value?.addEventListener('mousemove', onHostMouseMove, true);
  hostRef.value?.addEventListener('mouseleave', onHostMouseLeave, true);
});
onBeforeUnmount(() => {
  ro?.disconnect();
  ro = null;
  cancelAnimationFrame(sepRaf);
  hostRef.value?.removeEventListener('mousemove', onHostMouseMove, true);
  hostRef.value?.removeEventListener('mouseleave', onHostMouseLeave, true);
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
      props.grouped,
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
  position: relative;
  width: 100%;
  max-width: 100%;
  min-width: 0;
  min-height: 320px;
  border: none;
  overflow: hidden;
  background: var(--color-bg-2);
  box-sizing: border-box;
}

/* hover 表头时的列边界分隔线层：JS 动态创建（无 scoped 属性），需 :deep 匹配；不拦截鼠标，浮于 VTable canvas 之上 */
.cube-list-table :deep(.cube-table-separators) {
  position: absolute;
  inset: 0;
  pointer-events: none;
  z-index: 10;
  display: none;
}
.cube-list-table :deep(.cube-table-separators.show) {
  display: block;
}
.cube-list-table :deep(.cube-table-separators .sep) {
  position: absolute;
  top: 0;
  width: 2px;
  /* 高度由 JS 设为表头高度（仅表头区域显示，不贯穿数据区） */
  height: 100%;
  /*
   * 颜色跟随当前主题的 Secondary 色系（light/dark 自动切换）。
   * 用 --color-secondary-hover 而非 --color-secondary：亮色主题下 secondary=#F2F3F5 与表头背景同色几乎不可见，
   * hover 档更深一档，保证分隔线可辨识。
   */
  background: var(--color-secondary-hover);
  border-left: 1px solid var(--color-secondary-hover);
  box-sizing: border-box;
}
</style>

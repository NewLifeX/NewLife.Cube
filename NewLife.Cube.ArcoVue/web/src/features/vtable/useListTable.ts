import { nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { ListTable, TYPES, register } from '@visactor/vtable';
import { createGroup, createText } from '@visactor/vtable/es/vrender';
import { appendWfStatusMark, wfMarkKind } from './wfStatusMark';
import type { FieldMeta } from '@/core/types/field';
import type { ColumnPref, ViewFormatRule } from '@/core/utils/viewProfile';
import { frozenLeftCount, frozenRightCount } from '@/core/utils/viewProfile';
import {
  buildFormatIndex,
  resolveCellFormat,
  resolveRowFormat,
  resolveRowSideColor,
  ROW_SIDE_WIDTH_PX,
  type RowFormatHit,
} from '@/core/utils/viewFormat';
import { wfRowCanStart, wfRowEditLocked, wfRowInstanceId, wfRowStatus } from '@/core/types/workflow';
import { BADGE_BORDER_RADIUS, BADGE_PADDING } from '@/core/utils/fieldBadge';
import { getValueByKey } from '@/core/utils/url';
import { colorFromSnapshot, readThemeSnapshot, themeColor } from '@/core/utils/themeColor';
import { customFreezeSides, freezeLineHeight, freezeLineXs } from './freezeLines';
import { toVTableSortOption, vtableSortKey } from './vtableSortState';
import {
  buildOpsPartsWithLinks,
  opsActionColor,
  OPS_ACTION_LABELS,
  OPS_LINK_COLOR,
  type OpsAction,
  type OpsAutomationButton,
  type OpsCustomLink,
} from '@/core/utils/opsAction';
import { OPS_LINK_INLINE_MAX } from '@/core/utils/listLinkFields';
import { isIamRowActionDisabled } from '@/core/utils/iamGuards';
import { wfStatusBadge } from '@/views/crud/useWorkflowList';
import {
  isDateTimeBucketType,
  timeBucketOf,
  stripTimeBucketLabel,
  timeSortValue,
} from '@/core/utils/timeBucket';

/** 列头排序图标与工具栏排序弹层同一套：升序 alphabetical-sorting，降序 alphabetical-sorting-two */
const SORT_ASC_PATHS = ['M36 4V43.5', 'M7 28H23L7 44H23', 'M7 20L15.2759 4L23 20', 'M44 36L36 44L28 36'];
const SORT_DESC_PATHS = ['M36 4V43.5', 'M7 4H23L7 20H23', 'M7 44L15.2759 28L23 44', 'M44 36L36 44L28 36'];

function sortHeaderSvg(desc: boolean, color: string, opacity: string): string {
  const paths = desc ? SORT_DESC_PATHS : SORT_ASC_PATHS;
  const body = paths
    .map(
      (d) =>
        `<path d="${d}" stroke="${color}" stroke-opacity="${opacity}" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"/>`,
    )
    .join('');
  return `<svg width="14" height="14" viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">${body}</svg>`;
}

function sortHeaderIcon(name: string, mode: 'asc' | 'desc' | 'normal') {
  const icon = {
    type: 'svg' as const,
    width: 14,
    height: 14,
    funcType: TYPES.IconFuncTypeEnum.sort,
    name,
    positionType: TYPES.IconPosition.contentRight,
    marginLeft: 3,
    cursor: 'pointer',
    svg: '',
  };
  Object.defineProperty(icon, 'svg', {
    enumerable: true,
    configurable: true,
    get() {
      const active = mode !== 'normal';
      const color = active
        ? themeColor('--primary-6', 'rgb(22, 93, 255)')
        : themeColor('--color-text-3', '#86909C');
      return sortHeaderSvg(mode === 'desc', color, active ? '1' : '0.45');
    },
  });
  return icon;
}

register.icon('sort_upward', sortHeaderIcon('sort_upward', 'asc'));
register.icon('sort_downward', sortHeaderIcon('sort_downward', 'desc'));
register.icon('sort_normal', sortHeaderIcon('sort_normal', 'normal'));

/** OA 审批行操作动作 key（OSC-26090347f1）：由 ListTable @action 上抛，DefaultList 消费 */
export const WF_ROW_ACTION = {
  submit: 'wfSubmit',
  progress: 'wfProgress',
} as const;

/** 行 workflow 提交是否可点（非审批中/已通过且行级可发起） */
export function wfRowSubmitEnabled(row: Record<string, unknown>): boolean {
  const s = wfRowStatus(row);
  return s !== 'running' && s !== 'approved' && wfRowCanStart(row);
}

/** 行 workflow 提交是否渲染 */
export function wfRowSubmitVisible(row: Record<string, unknown>): boolean {
  return wfRowSubmitEnabled(row);
}

/** 行 workflow 进度是否渲染（有实例） */
export function wfRowProgressVisible(row: Record<string, unknown>): boolean {
  return wfRowInstanceId(row) !== '';
}

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
  /** 字段挂 Url：单元格可点导航（OSC-2608178bdb） */
  cellLink?: { url: string; target?: string };
}

/** ListTable 组件 props 类型（与 ListTable.vue defineProps 泛型逐字一致） */
interface ListTableProps {
  records: Record<string, unknown>[];
  columns: ListTableColumnDef[];
  rowKey: string;
  selectedKeys?: (string | number)[];
  showCheckbox?: boolean;
  height?: number;
  canEdit?: boolean;
  canDelete?: boolean;
  canViewDetail?: boolean;
  /** 魔方设置 EnableTableDoubleClick；false 时禁用双击进详情 */
  enableTableDoubleClick?: boolean;
  showExpand?: boolean;
  enableSort?: boolean;
  /** 行操作列额外按钮（自动化 button 规则，最多 3 个） */
  automationButtons?: OpsAutomationButton[];
  /** GetPage 合成 Url/dataAction 自定义链接（OSC-2608178bdb） */
  opsCustomLinks?: OpsCustomLink[];
  /** OA 审批行操作（OSC-26090347f1）：类型启用时渲染 提交/进度；行级按 __wf* 覆盖再隐藏 */
  workflowButtons?: { submit?: boolean; progress?: boolean };
  /** 服务端排序状态（可多列）；用于表头升/降序图标，不走 VTable 内部排序 */
  sortState?: { field: string; desc: boolean }[] | { field: string; desc: boolean } | null;
  /** 树视图：启用 VTable hierarchy（行含 children） */
  hierarchy?: boolean;
  /** 分组视图（OSC-0015）：records 含 __groupHeader 组头节点行，组头跨列显示并浅色区分 */
  grouped?: boolean;
  groupFields?: string[];
  groupLabelOf?: (field: string, value: unknown) => string | undefined;
  formatRules?: ViewFormatRule[];
  formatFields?: FieldMeta[];
  typePath?: string;
}

/** ListTable 组件 emits 类型（与 ListTable.vue defineEmits 泛型逐字一致） */
interface ListTableEmits {
  rowClick: [row: Record<string, unknown>];
  rowDblClick: [row: Record<string, unknown>];
  selectionChange: [keys: (string | number)[]];
  columnsChange: [cols: ColumnPref[]];
  sortChange: [payload: { field: string; desc: boolean } | null];
  action: [payload: {
    action: string;
    row: Record<string, unknown>;
    clientX?: number;
    clientY?: number;
  }];
  /** 单元格字段挂链接点击 */
  cellLink: [payload: { url: string; target?: string; row: Record<string, unknown> }];
  toggleEnable: [row: Record<string, unknown>, field: string];
  /** 滚动接近底部（剩余不足 200px）时触发，供父级增量加载更多行（列表/树懒加载） */
  scrollBottom: [];
}

type ListTableEmit = <K extends keyof ListTableEmits>(event: K, ...args: ListTableEmits[K]) => void;

/** ListTable 组件全部业务 TS：状态、列构建、VTable 挂载与事件绑定（自 ListTable.vue script setup 原样搬移） */
export function useListTable(props: ListTableProps, emit: ListTableEmit) {
  const hostRef = ref<HTMLElement | null>(null);
  let table: InstanceType<typeof ListTable> | null = null;
  let applying = false;
  let ro: ResizeObserver | null = null;
  let sepRaf = 0;
  /** 主题（外观/主色）变化监听：VTable canvas 色值需在主题变更后重建读取 */
  let themeObserver: MutationObserver | null = null;
  /** 主题刷新防抖计时器：body 属性变化可能一次操作多次触发，合并为一次全量重建 */
  let themeRefreshTimer = 0;
  /** 最近一次 setRecords 时间：setRecords 会触发 scroll 事件，需忽略以避免误报滚动到底导致增量追加循环 */
  let lastSetRecordsAt = 0;
  /** 分隔线层由 JS 动态创建：VTable 构造时会清空宿主容器，模板子元素会被删除 */
  let sepEl: HTMLElement | null = null;
  const themeVars = [
    '--primary-6',
    '--primary-5',
    '--danger-6',
    '--danger-5',
    '--link-6',
    '--link-5',
    '--color-text-1',
    '--color-text-2',
    '--color-text-3',
    '--color-fill-1',
    '--color-fill-2',
    '--color-fill-3',
    '--color-bg-2',
    '--color-border-2',
    '--color-border-3',
  ];
  let themeSnap: Record<string, string> = {};
  function paintColor(name: string, fallback: string): string {
    return colorFromSnapshot(themeSnap, name, fallback);
  }
  let formatByRow = new Map<string, RowFormatHit>();
  let lastChecked: Record<string, unknown>[] | null = null;
  let lastCheckedSrc: Record<string, unknown>[] | null = null;
  let lastCheckedKeys = '';

  function syncFormatIndex(records: Record<string, unknown>[]) {
    const rules = props.formatRules || [];
    const columns = (props.columns || []).map((c) => c.pref?.key).filter((k): k is string => !!k);
    formatByRow = rules.length
      ? buildFormatIndex(records, rules, props.formatFields || [], rowId, columns)
      : new Map();
  }

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

  function ensureFreezeLineLayer(): HTMLElement | null {
    const host = hostRef.value;
    if (!host) return null;
    let el = host.querySelector<HTMLElement>('.cube-table-freeze-lines');
    if (!el) {
      el = document.createElement('div');
      el.className = 'cube-table-freeze-lines';
      el.setAttribute('aria-hidden', 'true');
      const left = document.createElement('div');
      left.className = 'freeze-line freeze-line--left';
      const right = document.createElement('div');
      right.className = 'freeze-line freeze-line--right';
      el.append(left, right);
      host.appendChild(el);
    }
    return el;
  }

  function updateFreezeLines() {
    const host = hostRef.value;
    const layer = ensureFreezeLineLayer();
    const t = table;
    if (!host || !layer || !t) return;
    const prefs = props.columns.map((c) => c.pref);
    const sides = customFreezeSides(prefs);
    const leftEl = layer.querySelector<HTMLElement>('.freeze-line--left');
    const rightEl = layer.querySelector<HTMLElement>('.freeze-line--right');
    if (!sides.left && !sides.right) {
      leftEl?.classList.remove('is-on');
      rightEl?.classList.remove('is-on');
      return;
    }
    let frozenBlockRight = 0;
    let rightFrozenBlockLeft = 0;
    try {
      frozenBlockRight = t.getFrozenColsWidth?.() ?? 0;
    } catch {
      /* ignore */
    }
    try {
      const tw = t.tableNoFrameWidth ?? 0;
      const rw = t.getRightFrozenColsWidth?.() ?? 0;
      const tx = typeof t.tableX === 'number' ? t.tableX : 0;
      if (rw > 0) rightFrozenBlockLeft = tx + tw - rw;
    } catch {
      /* ignore */
    }
    const xs = freezeLineXs({
      showLeft: sides.left,
      showRight: sides.right,
      frozenBlockRight,
      rightFrozenBlockLeft,
    });
    let contentH = 0;
    try {
      const rows = t.rowCount ?? 0;
      if (rows > 0) contentH = t.getRowsHeight(0, rows - 1);
      if (!(contentH > 0)) contentH = t.getAllRowsHeight?.() ?? 0;
    } catch {
      /* ignore */
    }
    const h = freezeLineHeight(contentH, host.clientHeight);
    if (leftEl) {
      if (xs.left != null && h > 0) {
        leftEl.style.left = `${xs.left - 1}px`;
        leftEl.style.height = `${h}px`;
        leftEl.classList.add('is-on');
      } else {
        leftEl.classList.remove('is-on');
      }
    }
    if (rightEl) {
      if (xs.right != null && h > 0) {
        rightEl.style.left = `${xs.right}px`;
        rightEl.style.height = `${h}px`;
        rightEl.classList.add('is-on');
      } else {
        rightEl.classList.remove('is-on');
      }
    }
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

  function opsBundle() {
    const bundle = buildOpsPartsWithLinks({
      canViewDetail: props.canViewDetail,
      canEdit: props.canEdit,
      canDelete: props.canDelete,
      automationButtons: props.automationButtons,
      opsLinks: props.opsCustomLinks,
      inlineMax: OPS_LINK_INLINE_MAX,
    });
    // OA 审批行操作（OSC-26090347f1）：类型启用时作为操作列直出按钮（行级可见性渲染时按 __wf* 过滤）
    const wf = props.workflowButtons;
    if (wf?.submit) bundle.parts.push(WF_ROW_ACTION.submit);
    if (wf?.progress) bundle.parts.push(WF_ROW_ACTION.progress);
    return bundle;
  }

  function opsLabel(action: string, row?: Record<string, unknown>): string {
    if (action === WF_ROW_ACTION.submit) return '提交';
    // 审批未结束：操作栏用「审批」，打开编辑抽屉；已结束仍是「进度」
    if (action === WF_ROW_ACTION.progress)
      return row && wfRowStatus(row) === 'running' ? '审批' : '进度';
    if (action === 'more') return '更多';
    if (action.startsWith('auto:')) {
      const id = action.slice(5);
      const b = (props.automationButtons ?? []).find((x) => String(x.id) === id);
      return b?.name || '运行';
    }
    if (action.startsWith('link:')) {
      const name = action.slice(5);
      const l = (props.opsCustomLinks ?? []).find((x) => x.name === name);
      return l?.label || name;
    }
    return OPS_ACTION_LABELS[action as OpsAction] ?? action;
  }

  const OPS_ITEM_GAP = 12;
  const OPS_CELL_PAD = 8;

  function opsLabelWidth(label: string): number {
    let w = 0;
    for (const ch of label) w += ch.charCodeAt(0) > 255 ? 13 : 8;
    return Math.ceil(w);
  }

  /** 按文案估算操作列宽：项间距 12、左右内边距 8 */
  function opsColumnWidth(): number {
    const parts = opsBundle().parts;
    if (!parts.length) return 72;
    const content = parts.reduce((sum, action, i) => {
      return sum + opsLabelWidth(opsLabel(action)) + (i ? OPS_ITEM_GAP : 0);
    }, 0);
    return Math.max(72, content + OPS_CELL_PAD * 2);
  }

  /**
   * 操作列独立链接（customLayout）：每个动作一个可点击链接文本，取代按横向位置命中动作。
   * 组头（树）/组标题（groupBy）行不渲染操作。链接用主题主色，hover 变色 + 下划线。
   */
  function renderOpsLayout(args: any) {
    const t = table;
    if (!t || !hostRef.value) return undefined;
    const { col, row } = args;
    const rect = (args.rect as { width?: number; height?: number } | undefined) ??
      (t.getCellRect(col, row) as { width: number; height: number });
    const record = t.getCellOriginRecord?.(col, row) as
      | Record<string, unknown>
      | undefined;
    // 组头（树视图）/组标题（groupBy 分组）行不渲染操作
    if (
      !record ||
      (record as { vtableMerge?: unknown }).vtableMerge ||
      (record as { __groupHeader?: unknown }).__groupHeader
    ) {
      return undefined;
    }
    const parts = opsBundle().parts;
    if (!parts.length) return undefined;

    const container = createGroup({
      height: rect.height,
      width: rect.width,
      display: 'flex',
      flexDirection: 'row',
      flexWrap: 'nowrap',
      alignItems: 'center',
      // 勿实心填充：整行填色走单元格 bgColor，否则会挡住 hover/选择态底色
      fill: false,
    });

    const visibleParts = parts.filter((action) => {
      // OA 审批行操作：提交仅可发起行（非审批中/已通过且行级可发起）；进度仅已有实例行（IA §4）
      if (action === WF_ROW_ACTION.submit) return wfRowSubmitVisible(record);
      if (action === WF_ROW_ACTION.progress) return wfRowProgressVisible(record);
      // 审批中：隐藏编辑/删除（全视图一致；列表操作列在此过滤）
      if ((action === 'edit' || action === 'delete') && wfRowEditLocked(record)) return false;
      if (action === 'delete' && isIamRowActionDisabled(props.typePath, record, 'delete'))
        return false;
      return true;
    });
    visibleParts.forEach((action, i) => {
      const isLast = i === visibleParts.length - 1;
      // 链接配色：详情/编辑=主色、删除=警示色、其余系统自定义=链接色（需求 OSC）
      const color =
        action === 'more' || action.startsWith('link:')
          ? OPS_LINK_COLOR
          : opsActionColor(action);
      const link = createText({
        text: opsLabel(action, record),
        fontSize: 13,
        fontFamily: 'sans-serif',
        fill: paintColor(color.token, color.fallback),
        cursor: 'pointer',
        // 仅首项左内边距、项间距 6、末项右内边距（原先左右各 10 会叠成 20 间隙）
        boundsPadding: [0, isLast ? OPS_CELL_PAD : OPS_ITEM_GAP, 0, i === 0 ? OPS_CELL_PAD : 0],
      });
      link.states = {
        hover: {
          fill: paintColor(color.hoverToken, color.hoverFallback),
          underline: 1,
        },
      };
      link.addEventListener('mouseenter', () => {
        link.addState('hover', true, false);
        t.scenegraph?.updateNextFrame?.();
      });
      link.addEventListener('mouseleave', () => {
        link.removeState('hover', false);
        t.scenegraph?.updateNextFrame?.();
      });
      link.addEventListener('click', (evt: any) => {
        const pe = evt?.nativeEvent ?? evt?.event ?? evt;
        emit('action', {
          action,
          row: record,
          clientX: typeof pe?.clientX === 'number' ? pe.clientX : undefined,
          clientY: typeof pe?.clientY === 'number' ? pe.clientY : undefined,
        });
      });
      container.add(link);
    });

    return { rootContainer: container, renderDefault: false };
  }

  /** 审批状态列：仅图标徽标；hover Tooltip 显示状态文案 */
  function renderWfStatusLayout(args: any) {
    const t = table;
    if (!t || !hostRef.value) return undefined;
    const { col, row } = args;
    const rect = (args.rect as { width?: number; height?: number } | undefined) ??
      (t.getCellRect(col, row) as { width: number; height: number });
    const record = t.getCellOriginRecord?.(col, row) as Record<string, unknown> | undefined;
    if (
      !record ||
      (record as { vtableMerge?: unknown }).vtableMerge ||
      (record as { __groupHeader?: unknown }).__groupHeader
    ) {
      return undefined;
    }
    const badge = wfStatusBadge(wfRowStatus(record));
    const size = 22;
    const container = createGroup({
      height: rect.height,
      width: rect.width,
      display: 'flex',
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'center',
      fill: false,
    });
    const pill = createGroup({
      height: size,
      width: size,
      cornerRadius: BADGE_BORDER_RADIUS,
      fill: badge.bgColor,
      clip: true,
      cursor: 'default',
    });
    appendWfStatusMark(pill, wfMarkKind(wfRowStatus(record)), size, badge.textColor);
    const tip = badge.tooltip || badge.text;
    const showTip = () => {
      try {
        const cellRect = t.getVisibleCellRangeRelativeRect?.({ col, row }) ?? t.getCellRect(col, row);
        t.showTooltip?.(col, row, {
          content: tip,
          referencePosition: { rect: cellRect, placement: TYPES.Placement.top },
          style: {
            bgColor: '#1d2129',
            color: '#fff',
            fontSize: 12,
            padding: [6, 8, 6, 8],
            arrowMark: true,
          },
          disappearDelay: 120,
        });
      } catch {
        /* ignore */
      }
    };
    pill.addEventListener('mouseenter', showTip);
    container.add(pill);
    return { rootContainer: container, renderDefault: false };
  }

  function leadingCount(): number {
    // 分组模式（groupBy + rowSeriesNumber checkbox）：不使用前置 checkbox/expand 数据列
    if (props.groupFields?.length) return 0;
    // 审批状态列插在 chrome 与业务列之间，随左侧冻结一起钉住
    return (props.showCheckbox ? 1 : 0) + (props.showExpand ? 1 : 0) + (props.workflowButtons ? 1 : 0);
  }

  function buildColumns(): any[] {
    const cols: any[] = [];
    /** 数据列计数（用于组头行跨列显示：仅首数据列显示 label） */
    let dataColCount = 0;
    // 分组模式（groupBy + rowSeriesNumber checkbox）：不添加前置 expand/checkbox 数据列
    const groupedMode = !!props.groupFields?.length;
    if (props.showExpand && !groupedMode) {
      cols.push({
        field: '__expand',
        title: '',
        width: 40,
        minWidth: 40,
        maxWidth: 40,
        dragHeader: false,
        sort: false,
        showSort: false,
        fieldFormat: () => '›',
        style: (args: { table?: any; col?: number; row?: number }) => {
          if (!isFormatBodyRow(args)) return undefined;
          const record = recordOf(args);
          return {
            ...rowChromeFillPatch(record),
            ...sideBarPatch(record),
          };
        },
      });
    }
    if (props.showCheckbox && !groupedMode) {
      // VTable 要求 cellType/headerType，误用 type 会把勾选图标渲染成截断文本（如 "f..."）
      cols.push({
        field: '__checked',
        title: '',
        width: 48,
        minWidth: 48,
        maxWidth: 48,
        headerType: 'checkbox',
        cellType: 'checkbox',
        dragHeader: false,
        sort: false,
        showSort: false,
        style: (args: { table?: any; col?: number; row?: number }) => {
          if (!isFormatBodyRow(args)) return undefined;
          const record = recordOf(args);
          const fill = rowChromeFillPatch(record);
          // 有 expand 时侧边画在最左 expand 列
          const side = props.showExpand ? undefined : sideBarPatch(record);
          if (!fill && !side) return undefined;
          return { ...fill, ...side };
        },
      });
    }

    // OA 审批状态列（IA §1）：徽标 + Tooltip（图标/色板按状态）
    if (props.workflowButtons) {
      const isFirstWfCol = dataColCount === 0;
      dataColCount += 1;
      cols.push({
        field: '__wfStatus',
        title: '审批',
        width: 64,
        minWidth: 64,
        maxWidth: 64,
        dragHeader: false,
        sort: false,
        showSort: false,
        headerStyle: {
          textAlign: 'center',
          padding: [8, 4, 8, 4],
        },
        fieldFormat: (rec: Record<string, unknown>) => {
          const gh = groupHeaderFormat(rec, isFirstWfCol);
          if (gh !== undefined) return gh;
          return wfStatusBadge(wfRowStatus(rec)).text;
        },
        style: (args: { table?: any; col?: number; row?: number }) => {
          const record = args.table?.getRecordByCell?.(args.col, args.row) as
            | Record<string, unknown>
            | undefined;
          const ghs = groupHeaderStyle(record);
          if (ghs) return ghs;
          return { textAlign: 'center', ...rowChromeFillPatch(record) };
        },
        customLayout: renderWfStatusLayout,
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
                  // 非徽标字段的普通按钮底色/文字跟随主题（VTable canvas 读 Arco token）
                  buttonColor: paintColor('--color-fill-2', '#F2F3F5'),
                  buttonBorderColor: paintColor('--color-fill-2', '#F2F3F5'),
                  buttonBorderRadius: BADGE_BORDER_RADIUS,
                  buttonPadding: BADGE_PADDING,
                  buttonDisableColor: paintColor('--color-fill-2', '#F2F3F5'),
                  buttonDisableBorderColor: paintColor('--color-fill-2', '#F2F3F5'),
                  buttonTextDisableColor: paintColor('--color-text-2', '#4E5969'),
                };
            return {
              textAlign: 'center',
              color: badge?.textColor || paintColor('--color-text-1', '#4b5563'),
              cursor: c.enableToggle ? 'pointer' : 'default',
              buttonStyle,
              ...(isFormatBodyRow(args) ? cellBgPatch(record, c.pref.key) : {}),
              ...(isFormatBodyRow(args) && isFirstDataCol && !chromeHasLeftBar()
                ? sideBarPatch(record)
                : {}),
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
          const ghs = groupHeaderStyle(record);
          if (ghs) return ghs;
          const fmt =
            isFormatBodyRow(args)
              ? {
                  ...cellBgPatch(record, c.pref.key),
                  ...(isFirstDataCol && !chromeHasLeftBar() ? sideBarPatch(record) : {}),
                }
              : undefined;
          if (c.cellLink) {
            return {
              color: paintColor(OPS_LINK_COLOR.token, '#165DFF'),
              cursor: 'pointer',
              textDecoration: 'underline',
              ...fmt,
            };
          }
          return fmt;
        },
      });
    }

    if (opsBundle().parts.length) {
      const opsW = opsColumnWidth();
      cols.push({
        field: '__ops',
        title: '操作',
        width: opsW,
        minWidth: opsW,
        maxWidth: opsW,
        dragHeader: false,
        sort: false,
        showSort: false,
        disableColumnResize: true,
        customLayout: renderOpsLayout,
        headerStyle: {
          textAlign: 'left',
          padding: [8, 8, 8, 8],
        },
        style: (args: { table?: any; col?: number; row?: number }) => {
          const base = { textAlign: 'left' as const };
          if (!isFormatBodyRow(args)) return base;
          return { ...base, ...rowChromeFillPatch(recordOf(args)) };
        },
      });
    }

    return cols;
  }

  function frozenCount(): number {
    return leadingCount() + frozenLeftCount(props.columns.map((c) => c.pref));
  }

  function rightFrozenCount(): number {
    const ops = opsBundle().parts.length ? 1 : 0;
    return ops + frozenRightCount(props.columns.map((c) => c.pref));
  }

  function withChecks(records: Record<string, unknown>[]) {
    const key = `${props.groupFields?.length ? 'g' : 't'}\u0000${(props.selectedKeys || []).map(String).join('\u0001')}`;
    if (lastChecked && lastCheckedSrc === records && lastCheckedKeys === key) return lastChecked;
    const selected = new Set((props.selectedKeys || []).map(String));
    // 分组模式（OSC-0015 重构：VTable 原生 groupBy + rowSeriesNumber checkbox）：
    // 勾选初值写回记录 `_vtable_rowSeries_number` 字段，供 VTable 初始化行号列 checkbox
    const next = props.groupFields?.length
      ? records.map((r) => ({ ...r, _vtable_rowSeries_number: selected.has(rowId(r)) }))
      : records.map((r) => ({
          ...r,
          // 组头行不参与勾选：渲染为禁用态（VTable checkbox 列支持 {checked, disable} 对象值）
          __checked: r.__groupHeader ? { checked: false, disable: true } : selected.has(rowId(r)),
        }));
    lastChecked = next;
    lastCheckedSrc = records;
    lastCheckedKeys = key;
    return next;
  }

  /** 组头行显示文本（首数据列）或空串（其余列）；count 取组头节点自身字段 */
  function groupHeaderFormat(rec: Record<string, unknown> | undefined, isFirstDataCol: boolean): string | undefined {
    const gh = rec?.__groupHeader as { label?: string } | undefined;
    if (!gh) return undefined;
    return isFirstDataCol ? `📁 ${gh.label ?? ''} (${rec?.count ?? 0})` : '';
  }

  /** 组头行背景浅色 + 加粗，与普通行区分（tree 模式组头；groupBy 组标题由 groupTitleStyle 定制） */
  function groupHeaderStyle(rec: Record<string, unknown> | undefined): Record<string, unknown> | null {
    if (!rec?.__groupHeader) return null;
    return {
      bgColor: paintColor('--color-fill-1', '#F7F8FA'),
      color: paintColor('--color-text-1', '#1D2129'),
      fontWeight: 600,
    };
  }

  function isFormatBodyRow(args: { table?: any; col?: number; row?: number }): boolean {
    const t = args.table;
    if (!t) return true;
    const header = t.columnHeaderLevelCount || 1;
    if (typeof args.row === 'number' && args.row < header) return false;
    try {
      const lv = t.getGroupTitleLevel?.(args.col, args.row);
      if (lv != null && lv >= 0) return false;
    } catch {
      /* ignore */
    }
    return true;
  }

  function recordOf(args: { table?: any; col?: number; row?: number }): Record<string, unknown> | undefined {
    return args.table?.getRecordByCell?.(args.col, args.row) as Record<string, unknown> | undefined;
  }

  function chromeHasLeftBar(): boolean {
    if (props.groupFields?.length) return true;
    return !!props.showCheckbox || !!props.showExpand;
  }

  function sideBarPatch(record: Record<string, unknown> | undefined): Record<string, unknown> | undefined {
    if (!record || record.__groupHeader) return undefined;
    const rules = props.formatRules || [];
    const id = rowId(record);
    const hit = rules.length && id ? formatByRow.get(id) : undefined;
    const color = hit
      ? hit.side
      : rules.length
        ? resolveRowSideColor(record, rules, props.formatFields || [])
        : undefined;
    if (!color) return undefined;
    const edge = paintColor('--color-border-2', '#E5E6EB');
    return {
      borderLineWidth: [1, 0, 0, ROW_SIDE_WIDTH_PX],
      borderColor: [edge, edge, edge, color],
    };
  }

  function cellBgPatch(record: Record<string, unknown> | undefined, columnField: string): Record<string, unknown> | undefined {
    if (!record || record.__groupHeader) return undefined;
    const rules = props.formatRules || [];
    if (!rules.length) return undefined;
    const id = rowId(record);
    const hit = id ? formatByRow.get(id) : undefined;
    const fmt = hit
      ? hit.cells[columnField.toLowerCase()]
      : resolveCellFormat(record, columnField, rules, props.formatFields || []);
    if (!fmt) return undefined;
    return {
      ...(fmt.color ? { bgColor: fmt.color } : {}),
      ...(fmt.bold ? { fontWeight: 700 } : {}),
    };
  }

  /** 整行填色铺到勾选 / 展开 / 操作等 chrome 列（单元格/整列规则不涂这些列） */
  function rowChromeFillPatch(record: Record<string, unknown> | undefined): Record<string, unknown> | undefined {
    if (!record || record.__groupHeader) return undefined;
    const rules = props.formatRules || [];
    if (!rules.length) return undefined;
    const id = rowId(record);
    const hit = id ? formatByRow.get(id) : undefined;
    const fmt = hit ? hit.row : resolveRowFormat(record, rules, props.formatFields || []);
    if (!fmt) return undefined;
    return {
      ...(fmt.color ? { bgColor: fmt.color } : {}),
      ...(fmt.bold ? { fontWeight: 700 } : {}),
    };
  }

  /** groupBy 组标题行文本：`📁 字段名 在 label (count)`；label 按分组字段 dataSource 翻译（OSC-0015）；日期时间字段显示时间桶标签 */
  function groupTitleFormat(
    record: Record<string, unknown> | undefined,
    col?: number,
    row?: number,
    t?: { getGroupTitleLevel?: (c?: number, r?: number) => number | undefined },
  ): string {
    const level = (t?.getGroupTitleLevel?.(col, row) as number | undefined) ?? 0;
    const field = props.groupFields?.[level];
    const value = (record as { vtableMergeName?: unknown })?.vtableMergeName;
    let label: string | undefined;
    if (field && isTimeGroupField(field)) {
      // 时间分桶：value 为“${序号}·${标签}”，去掉前缀显示
      label = stripTimeBucketLabel(value);
    } else if (field && props.groupLabelOf) {
      label = props.groupLabelOf(field, value);
    }
    const valueLabel = label ?? (value == null ? '未分组' : String(value));
    // 仅日期时间字段分组采用“字段名 在 值”格式；普通字段保持“值 (N)”
    const prefix = field && isTimeGroupField(field) ? `${groupFieldLabel(field)} 在 ` : '';
    return `📁 ${prefix}${valueLabel} (${(record as { children?: unknown[] })?.children?.length ?? 0})`;
  }

  /** groupBy 字段名与数据字段名匹配：视图分组字段为 PascalCase（FieldMeta.name），数据行字段为 camelCase */
  function toDataField(field: string): string {
    if (!field) return field;
    // 与后端 System.Text.Json 默认 camelCase 输出对齐（.NET JsonNamingPolicy.CamelCase）：
    // Type→type、ParentID→parentID、ID→id、URL→url；仅首字母小写无法处理全大写缩写字段
    const chars = field.split('');
    if (chars.length > 0 && isUpperChar(chars[0])) {
      for (let i = 0; i < chars.length; i++) {
        // 首字符后紧跟小写：只小写首字符即可（ParentID→parentID）
        if (i === 1 && !isUpperChar(chars[i])) break;
        const hasNext = i + 1 < chars.length;
        // 大写组后跟小写：停止（保持该大写后的原样）
        if (i > 0 && hasNext && !isUpperChar(chars[i + 1])) break;
        chars[i] = chars[i].toLowerCase();
      }
    }
    return chars.join('');
  }

  function isUpperChar(c: string): boolean {
    return c >= 'A' && c <= 'Z';
  }

  /** 分组字段是否为可做时间分桶的日期时间类型（OSC-260830a1b2 之后：日期时间分组） */
  function isTimeGroupField(field: string): boolean {
    const fm = (props.formatFields || []).find((f) => f.name === field);
    return !!fm && isDateTimeBucketType(fm.typeName);
  }

  /** 分组字段显示名（组标题“字段名 在 值”用）；缺省回落字段名 */
  function groupFieldLabel(field: string): string {
    const fm = (props.formatFields || []).find((f) => f.name === field);
    return fm?.displayName || field;
  }

  /** 时间分桶临时字段名（camelCase，与 VTable 分组数据字段命名一致） */
  function timeBucketField(field: string): string {
    return `__timeBucket_${toDataField(field)}`;
  }

  /**
   * 为每条记录注入时间分桶临时字段（仅当分组含日期时间字段时）。
   * 桶键为“${序号}·${标签}”，带序号前缀；无效/空值归入“未明确时间”桶（键 “7·未明确时间”，排在最后）。
   * 随后按第一个时间分组字段降序（近到远）稳定排序 records：
   * VTable 分组组顺序取决于 records 中组键值首次出现顺序（GroupConfig.sort 不生效），
   * 预排序保证时间桶组间由近到远、组内记录也按时间近到远。
   */
  function withTimeBucket(records: Record<string, unknown>[]): Record<string, unknown>[] {
    const fields = (props.groupFields || []).filter((f) => isTimeGroupField(f));
    if (!fields.length) return records;
    const now = new Date();
    const decorated = records.map((r) => {
      const out = { ...r };
      for (const f of fields) {
        out[timeBucketField(f)] = timeBucketOf(r[toDataField(f)], now);
      }
      return out;
    });
    const dataField = toDataField(fields[0]);
    // 稳定排序：近到远（最新在前）；无时间值排最后
    return decorated.sort((a, b) => timeSortValue(b[dataField]) - timeSortValue(a[dataField]));
  }

  function buildOption(): any {
    themeSnap = readThemeSnapshot(themeVars);
    syncFormatIndex((props.records || []) as Record<string, unknown>[]);
    const cols = buildColumns();
    const groupedMode = !!props.groupFields?.length;
    // 树视图仍用 VTable hierarchy；分组视图不再设 tree（VTable 会把 checkbox 列自动设为 tree 列导致渲染异常），
    // 改用 VTable 原生 groupBy + rowSeriesNumber checkbox（OSC-0015 重构，参考官方 list-table-group-checkbox demo）
    if (!groupedMode && props.hierarchy && cols.length) {
      const firstData = cols.find((c: { field?: string }) => c.field && c.field !== '__checked' && c.field !== '__ops' && c.field !== '__expand');
      if (firstData) (firstData as { tree?: boolean }).tree = true;
    }
    // 多列排序：每一列都要有图标。multipleSort 让 VTable 保留数组，而不是只留最后一列。
    // 无排序必须是 []：null 会被包成 [null] 并在建表时抛错。
    const sortState = toVTableSortOption(props.sortState);

    return {
      records: withChecks(withTimeBucket(props.records)),
      columns: cols,
      frozenColCount: frozenCount(),
      rightFrozenColCount: Math.min(rightFrozenCount(), Math.max(0, cols.length - frozenCount())),
      ...(groupedMode
        ? {
            // 官方分组复选框方案：checkbox 置于 rowSeriesNumber（每行最前面），
            // groupConfig.titleCheckbox 让组标题行左侧显示 checkbox，enableCheckboxCascade 级联同步子行
            groupConfig: {
              // groupBy 需与数据字段名匹配（camelCase）；日期时间字段改用时间分桶临时字段（带序号前缀键），
              // 其余字段沿用 camelCase 字段名。注：VTable 组顺序由 records 中组键值首次出现顺序决定
              // （GroupConfig.sort 不生效），时间近到远由 withTimeBucket 预排序 records 保证
              groupBy: (props.groupFields || []).map((f) => ({
                key: isTimeGroupField(f) ? timeBucketField(f) : toDataField(f),
                sort: 'asc',
              })),
              titleCheckbox: true,
              titleFieldFormat: groupTitleFormat,
            },
            rowSeriesNumber: {
              width: 48,
              minWidth: 48,
              maxWidth: 48,
              sort: false,
              format: () => '',
              cellType: 'checkbox',
              headerType: 'checkbox',
              style: (args: { table?: any; col?: number; row?: number }) => {
                if (!isFormatBodyRow(args)) return undefined;
                const record = recordOf(args);
                return {
                  ...rowChromeFillPatch(record),
                  ...sideBarPatch(record),
                };
              },
            },
            enableCheckboxCascade: true,
          }
        : {}),
      widthMode: 'standard',
      // 表宽有余量时摊给数据列；操作/勾选等 chrome 列用 maxWidth 钉死
      autoFillWidth: true,
      columnResizeMode: 'all',
      // 官方异步大数据建议（visactor 性能指南 async_data）：
      // 双击列间隔线自动计算列宽会请求/计算全部数据 → 禁用；右键表头组织全部选中 cell 信息同理
      resize: { disableDblclickAutoResizeColWidth: true },
      eventOptions: { contextmenuReturnAllSelectedCells: false },
      // 关闭浏览器滚动链/回弹（纯滚动行为优化，不影响功能）
      overscrollBehavior: 'none',
      hover: { highlightMode: 'row', disableHeaderHover: true },
      // 禁用单元格选中框；勾选列负责多选。Hover 用整行高亮
      select: { highlightMode: 'row', disableSelect: true, disableHeaderSelect: true },
      tooltip: { isShowOverflowTextTooltip: true },
      // 服务端排序：图标由 sortState 驱动。列上 sort:false，setRecords 不会改数据顺序
      multipleSort: true,
      sortState,
      ...(props.hierarchy || props.grouped
        ? // VTable 的 hierarchyExpandLevel>1 时根节点才默认展开；设为 2 使树视图默认显示第一层子节点
          { hierarchyExpandLevel: 2, hierarchyIndent: 16 }
        : {}),
      // 默认表头与数据行区分；颜色经 themeColor 读取 Arco 语义 token（canvas 不支持 CSS 变量，随亮/暗主题与用户主色）
      theme: {
        // VTable DEFAULT 主题 underlay 为 #FFF：空数据区/表体空白会透白；须跟 body 底色
        underlayBackgroundColor: paintColor('--color-bg-2', '#FFFFFF'),
        // 默认 cellBorderClipDirection=top-left 会裁掉底/右边；行分隔须用顶边
        // borderLineWidth: [上, 右, 下, 左] — 仅行分隔，无列分隔
        // DEFAULT.defaultStyle.bgColor=#ECF1F5，未覆盖时暗色下仍呈浅蓝灰
        defaultStyle: {
          bgColor: paintColor('--color-bg-2', '#FFFFFF'),
          color: paintColor('--color-text-1', '#1D2129'),
          borderColor: paintColor('--color-border-2', '#E5E6EB'),
          borderLineWidth: [1, 0, 0, 0],
        },
        // 功能图标（展开/折叠箭头、排序等）颜色：VTable 默认 #141414 深黑，暗色下深底不可见
        // 读取 Arco 文字色 token，随亮/暗主题自动切换
        functionalIconsStyle: {
          expand_color: paintColor('--color-text-3', '#86909C'),
          collapse_color: paintColor('--color-text-3', '#86909C'),
          sort_color: paintColor('--color-text-3', '#86909C'),
        },
        // 勾选框：VRender 默认 defaultFill 为白底，暗色表头上会整块发白；未选透明填充 + 描边，选中跟 Arco 主色
        checkboxStyle: {
          defaultFill: 'transparent',
          defaultStroke: paintColor('--color-border-3', '#C9CDD4'),
          disableFill: paintColor('--color-fill-2', '#F2F3F5'),
          checkedFill: paintColor('--primary-6', '22, 93, 255'),
          checkedStroke: paintColor('--primary-6', '22, 93, 255'),
          disableCheckedFill: paintColor('--color-fill-3', '#E5E6EB'),
          disableCheckedStroke: paintColor('--color-border-2', '#E5E6EB'),
        },
        headerStyle: {
          bgColor: paintColor('--color-fill-2', '#F2F3F5'),
          color: paintColor('--color-text-2', '#4E5969'),
          fontWeight: 500,
          fontSize: 13,
          borderColor: paintColor('--color-border-2', '#E5E6EB'),
          borderLineWidth: [1, 0, 0, 0],
        },
        // 分组标题行样式（groupBy）：浅灰底 + 加粗，与普通行区分（OSC-0015）
        groupTitleStyle: {
          bgColor: paintColor('--color-fill-1', '#F7F8FA'),
          color: paintColor('--color-text-1', '#1D2129'),
          fontWeight: 600,
          fontSize: 13,
          borderColor: paintColor('--color-border-2', '#E5E6EB'),
          borderLineWidth: [1, 0, 0, 0],
        },
        bodyStyle: {
          bgColor: paintColor('--color-bg-2', '#FFFFFF'),
          color: paintColor('--color-text-1', '#1D2129'),
          fontWeight: 400,
          fontSize: 13,
          borderColor: paintColor('--color-border-2', '#E5E6EB'),
          borderLineWidth: [1, 0, 0, 0],
          hover: {
            cellBgColor: paintColor('--color-fill-1', '#F7F8FA'),
            inlineRowBgColor: paintColor('--color-fill-1', '#F7F8FA'),
          },
        },
        // 右冻结操作列与表体共用 hover，避免整行填色后操作区选择态发花
        rightFrozenStyle: {
          bgColor: paintColor('--color-bg-2', '#FFFFFF'),
          color: paintColor('--color-text-1', '#1D2129'),
          fontWeight: 400,
          fontSize: 13,
          borderColor: paintColor('--color-border-2', '#E5E6EB'),
          borderLineWidth: [1, 0, 0, 0],
          hover: {
            cellBgColor: paintColor('--color-fill-1', '#F7F8FA'),
            inlineRowBgColor: paintColor('--color-fill-1', '#F7F8FA'),
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
        // 关掉 VTable 默认冻结阴影；自定义示意线仅在用户钉了左/右冻结时由 overlay 绘制
        frozenColumnLine: {
          shadow: {
            width: 0,
            startColor: 'rgba(0,0,0,0)',
            endColor: 'rgba(0,0,0,0)',
            visible: 'scrolling',
          },
        },
        // 滚动条：DEFAULT 浅色轨在暗色下刺眼
        scrollStyle: {
          scrollRailColor: paintColor('--color-fill-2', '#F2F3F5'),
          scrollSliderColor: paintColor('--color-fill-3', '#E5E6EB'),
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

    // 分组模式（groupBy + rowSeriesNumber checkbox）：VTable 实际渲染列 = rowSeriesNumber(48) + 数据列 + __ops，
    // 而 table.columns 定义数组不含 rowSeriesNumber；colWidths / getColWidth 按实际列索引（含该列）取值，
    // 因此数据列的实际列索引 = columns 数组索引 + 1，否则列宽整体错位 1 位（拖动一列导致其它列宽度乱变）
    const offset = props.groupFields?.length ? 1 : 0;

    define.forEach((col, idx) => {
      const field = String(col.field ?? '');
      if (!field || field === '__checked' || field === '__ops' || field === '__expand') return;
      const pref = byKey.get(field);
      if (!pref) return;
      pref.visible = true;
      const realIdx = idx + offset;
      const fromEvent = colWidths?.[realIdx];
      let w =
        typeof fromEvent === 'number' && fromEvent > 0
          ? fromEvent
          : typeof col.width === 'number'
            ? col.width
            : undefined;
      // 兜底：直接从实际列读取最新宽度（不依赖事件参数结构，含 rowSeriesNumber 偏移）
      if (!(typeof w === 'number' && w > 0)) {
        try {
          const cw = table!.getColWidth(realIdx);
          if (typeof cw === 'number' && cw > 0) w = cw;
        } catch {
          /* ignore */
        }
      }
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

    function resolveBodyRow(args: { col?: number; row?: number; cellLocation?: string }) {
      if (args.cellLocation === 'columnHeader') return null;
      // groupBy 分组模式：展示行含组标题行，与 props.records 错位，须用 getCellOriginRecord 取原始记录
      try {
        const rec = table!.getCellOriginRecord?.(args.col ?? 0, args.row ?? 0) as
          | Record<string, unknown>
          | null
          | undefined;
        if (rec && typeof rec === 'object' && !(rec as { vtableMerge?: unknown }).vtableMerge) {
          return rec;
        }
        return null;
      } catch {
        /* fall through */
      }
      const headerLevels = table!.columnHeaderLevelCount || 1;
      return props.records[(args.row ?? 0) - headerLevels] || null;
    }

    table.on('click_cell', ((args: any) => {
      const row = resolveBodyRow(args);
      if (!row) return;
      if (row.__groupHeader || row.vtableMerge) return; // 组头/组标题行不响应点击
      const field = fieldKey(args.field);
      if (field === '__checked' || field === '_vtable_rowSeries_number') return;
      // 单击仅处理操作列 / 展开列；数据行单击不打开详情
      if (field === '__expand') {
        if (props.canViewDetail) emit('action', { action: 'detail', row });
        return;
      }
      if (field === '__ops') {
        // 操作列改为 customLayout 独立链接：动作由各链接元素 click 事件触发，此处不按位置命中
        return;
      }
      // Boolean 徽标（Enable 及任意 Boolean 字段）：可点击切换（须父级授权 Update）；携带字段名供切换对应字段
      const colDef = props.columns.find((c) => c.pref.key === field);
      if (colDef?.enableToggle && colDef.badge) {
        emit('toggleEnable', row, colDef.pref.key);
        return;
      }
      if (colDef?.cellLink) {
        emit('cellLink', {
          url: colDef.cellLink.url,
          target: colDef.cellLink.target,
          row,
        });
        return;
      }
      emit('rowClick', row);
    }) as any);

    table.on('dblclick_cell', ((args: any) => {
      const row = resolveBodyRow(args);
      if (!row) return;
      if (row.__groupHeader || row.vtableMerge) return; // 组头/组标题行不响应双击
      const field = fieldKey(args.field);
      if (
        field === '__checked' ||
        field === '_vtable_rowSeries_number' ||
        field === '__ops' ||
        field === '__expand'
      )
        return;
      if (props.canViewDetail && props.enableTableDoubleClick !== false) emit('rowDblClick', row);
    }) as any);

    table.on('checkbox_state_change', (() => {
      // 延后到宏任务读取：VTable 内部级联监听（bindGroupTitleCheckboxChange/bindGroupCheckboxTreeChange）在
      // setTimeout(0) 中注册，晚于本监听。若同步读取会拿到级联前的旧状态并 emit 空 → 父级清空 selectedKeys →
      // 触发 setRecords 重置全部勾选态，导致分组/树级联失效（OSC-0015 分组后勾选框不可用根因）。
      // groupBy 下 getCheckboxState 返回按原始 record 索引的稀疏数组，改用遍历展示行读取状态。
      setTimeout(() => {
        if (!table) return;
        const out: (string | number)[] = [];
        const headerLevels = table.columnHeaderLevelCount || 1;
        for (let row = headerLevels; row < table.rowCount; row++) {
          let rec: Record<string, unknown> | null = null;
          try {
            rec = table.getCellOriginRecord(0, row) as Record<string, unknown> | null;
          } catch {
            rec = null;
          }
          if (
            !rec ||
            (rec as { vtableMerge?: unknown }).vtableMerge ||
            (rec as { __groupHeader?: unknown }).__groupHeader
          ) {
            continue; // 组标题/组头行不参与勾选
          }
          let checked = false;
          try {
            checked = !!table.getCellCheckboxState(0, row);
          } catch {
            checked = false;
          }
          if (!checked) continue;
          const id = rowId(rec);
          if (id) out.push(/^\d+$/.test(id) ? Number(id) : id);
        }
        emit('selectionChange', out);
      }, 0);
    }) as any);

    table.on(
      'resize_column_end',
      ((args: { col?: number; colWidths?: number[] }) => {
        syncFromTable(args?.colWidths);
        updateFreezeLines();
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
        // 多列时表头点击仍替换成这一列（清掉第 2、3 列），与工具栏「应用」写入的 sorts 区分
        const sorts = Array.isArray(props.sortState)
          ? props.sortState
          : props.sortState?.field
            ? [props.sortState]
            : [];
        const sole = sorts.length === 1 ? sorts[0] : null;
        let next: { field: string; desc: boolean } | null;
        if (!sole || sole.field !== field) next = { field, desc: false };
        else if (!sole.desc) next = { field, desc: true };
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
    try {
      table = new ListTable(hostRef.value, buildOption());
    } catch {
      table = null;
      applying = false;
      return;
    }
    // VTable 构造会清空宿主容器（innerHTML=''），必须在创建后重建分隔线层
    ensureSeparatorLayer();
    updateFreezeLines();
    applying = false;
    bindEvents();
    if (!ro && typeof ResizeObserver !== 'undefined') {
      ro = new ResizeObserver(() => {
        try {
          table?.resize?.();
        } catch {
          /* ignore */
        }
        updateFreezeLines();
      });
      ro.observe(hostRef.value);
    }
    void nextTick(() => {
      try {
        table?.resize?.();
      } catch {
        /* ignore */
      }
      updateFreezeLines();
    });
  }

  function refreshOption() {
    if (!table) {
      mountTable();
      return;
    }
    applying = true;
    try {
      table.updateOption(buildOption());
    } finally {
      applying = false;
    }
    ensureSeparatorLayer();
    updateFreezeLines();
  }

  /** 滚动接近底部（剩余不足 200px）时上报，父级据此增量加载更多行（列表/树懒加载，避免千条一次性传 VTable） */
  function onTableScroll(e: unknown) {
    const t = table;
    if (!t) return;
    // setRecords 会触发 scroll 事件（保留滚动位置），忽略其后的短窗口以避免增量追加循环
    if (Date.now() - lastSetRecordsAt < 200) return;
    const ev = e as { scrollTop?: number } | null;
    const top = ev?.scrollTop ?? t.getScrollTop();
    const total = t.getAllRowsHeight();
    const viewH = t.canvas?.height ?? hostRef.value?.clientHeight ?? 0;
    if (total > viewH && top + viewH >= total - 200) emit('scrollBottom');
  }

  onMounted(() => {
    mountTable();
    table?.on('scroll', onTableScroll);
    // 捕获阶段监听，避免 VTable 内部 stopPropagation 吞掉 mousemove
    hostRef.value?.addEventListener('mousemove', onHostMouseMove, true);
    hostRef.value?.addEventListener('mouseleave', onHostMouseLeave, true);
    // 主题（外观/主色）变化时重建表格：VTable canvas 颜色在 buildOption 时快照，需重新读取 Arco token
    if (typeof MutationObserver !== 'undefined') {
      themeObserver = new MutationObserver(() => {
        // 防抖合并：body 属性变化（loading/布局等）可能频繁触发，只重建一次。
        // 重建期间断开观察，避免 refreshOption 写回 body 样式后再触发自己。
        clearTimeout(themeRefreshTimer);
        themeRefreshTimer = window.setTimeout(() => {
          const obs = themeObserver;
          obs?.disconnect();
          try {
            refreshOption();
          } catch {
            /* ignore */
          } finally {
            if (obs && themeObserver === obs) {
              obs.observe(document.body, {
                attributes: true,
                attributeFilter: ['style', 'arco-theme'],
              });
            }
          }
        }, 120);
      });
      themeObserver.observe(document.body, {
        attributes: true,
        attributeFilter: ['style', 'arco-theme'],
      });
    }
  });
  onBeforeUnmount(() => {
    themeObserver?.disconnect();
    themeObserver = null;
    clearTimeout(themeRefreshTimer);
    ro?.disconnect();
    ro = null;
    cancelAnimationFrame(sepRaf);
    hostRef.value?.removeEventListener('mousemove', onHostMouseMove, true);
    hostRef.value?.removeEventListener('mouseleave', onHostMouseLeave, true);
    table?.off('scroll', onTableScroll);
    table?.release();
    table = null;
  });

  // 注意：不要把 selectedKeys 放进全量 refresh 依赖——勾选后回写会 updateOption，冲掉 VTable 勾选态
  // （非 deep：records 为整体替换（翻页/加载新数组），引用变化即可触发；deep 会对千条记录全量深度遍历拖慢更新）
  /** 仅更新数据（setRecords）而非全量 updateOption：翻页/换数据时避免重建 columns/布局，
   *  千条数据从 ~850ms 降至 setRecords 的数据替换开销（性能优化，不影响功能）。
   *  注意：数据更新时必须经 withTimeBucket 注入时间分桶字段并近到远排序，
   *  否则刷新/查询/重置后日期时间分组在 setRecords 路径下失效（与 buildOption 保持一致）。 */
  function applyRecords() {
    if (!table) {
      mountTable();
      return;
    }
    syncFormatIndex((props.records || []) as Record<string, unknown>[]);
    applying = true;
    lastSetRecordsAt = Date.now();
    try {
      // 必须把 sortState 放进 setRecords：随后的 updateSortState(executeSort:false) 只改内部状态、不重绘表头。
      // 各列 sort:false，这里不会按图标再排一次数据，行序保持服务端 Order By。
      // 空排序传 []，避免 multipleSort 把 null 包成 [null] 后建表抛错。
      table.setRecords?.(withChecks(withTimeBucket(props.records)), {
        sortState: toVTableSortOption(props.sortState),
      });
    } catch {
      try {
        refreshOption();
      } catch {
        /* 重建失败不抛出渲染周期，否则列表 loading 无法收尾 */
      }
    } finally {
      applying = false;
      updateFreezeLines();
    }
  }

  // 数据或排序变化：仅 setRecords（保留滚动位置，只替换数据不重建配置）。
  // 排序必须单独监听：请求失败或行序未变时 records 引用不变，表头图标仍要跟上 sorts。
  // 用字符串签名比较，避免 toVTableSortOption 每次返回新数组导致 setRecords 循环。
  watch([() => props.records, () => vtableSortKey(props.sortState)], () => applyRecords());

  // 配置/交互能力/层级/分组变化：全量重建（updateOption）；sortState 由 applyRecords 写入 setRecords
  watch(
    () => [
      props.columns,
      props.showCheckbox,
      props.canEdit,
      props.canDelete,
      props.canViewDetail,
      props.showExpand,
      props.enableSort,
      props.automationButtons,
      props.opsCustomLinks,
      props.hierarchy,
      props.grouped,
      props.groupFields,
      props.formatRules,
      props.formatFields,
      props.typePath,
    ],
    () => refreshOption(),
  );

  /** 父级清空选择时同步勾选 UI（不整表 refresh，避免打断勾选交互） */
  watch(
    () => props.selectedKeys,
    (keys) => {
      if (!table || applying) return;
      if ((keys?.length ?? 0) > 0) return;
      applyRecords();
    },
  );

  return { hostRef };
}

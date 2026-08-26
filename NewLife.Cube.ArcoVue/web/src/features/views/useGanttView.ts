import { onBeforeUnmount, onMounted, ref, watch } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import type { GanttMapping } from '@/core/utils/viewMapping';
import { getValueByKey } from '@/core/utils/url';
import { themeColor } from '@/core/utils/themeColor';
import {
  ganttMappingSignature,
  ganttRecordsSignature,
  nearlySame,
} from './ganttLayout';

/** GanttView 组件 props 类型（与 GanttView.vue defineProps 泛型逐字一致） */
interface GanttViewProps {
  records: Record<string, unknown>[];
  fields: FieldMeta[];
  mapping?: GanttMapping | null;
  rowKey: string;
  height?: number;
  /** 缩放级别（zoomScale.levels 下标 0~4：年/年月/月·日/周·日/日·时），由父级工具栏控制，默认月·日 */
  zoomLevel?: number;
}

/** GanttView 组件 emits 类型（与 GanttView.vue defineEmits 泛型逐字一致） */
interface GanttViewEmits {
  detail: [row: Record<string, unknown>];
  /** 拖拽左侧表格宽度后上报新 mapping（tableWidth 随 mapping 持久化，OSC-0019） */
  'mapping-change': [mapping: GanttMapping];
}

type GanttViewEmit = <K extends keyof GanttViewEmits>(event: K, ...args: GanttViewEmits[K]) => void;

/** GanttView 组件全部业务 TS：VTable Gantt 构建、缩放级别、宽度/尺寸监听（自 GanttView.vue script setup 原样搬移） */
export function useGanttView(props: GanttViewProps, emit: GanttViewEmit) {
  const host = ref<HTMLElement | null>(null);
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  let gantt: any = null;
  /** 表格宽度轮询句柄（VTable Gantt 无宽度调整事件，轮询 taskTableWidth 兜底） */
  let widthTimer = 0;
  /** 拖拽停止防抖：连续变化重置计时，停止 300ms 后上报 */
  let widthDebounce = 0;
  /** 已上报的表格宽度（重建/上报后重置，避免循环） */
  let lastWidth = 380;
  /** 容器尺寸监听：窗口缩放/侧栏折叠等导致可视区变化时重建（需求 2：时间轴填满） */
  let resizeObserver: ResizeObserver | null = null;
  let resizeDebounce = 0;
  /** 宿主上次记录的尺寸：ResizeObserver 回调先对比尺寸，真正变化才重建（VTable 缩放等内部布局变化也会触发 RO，避免无谓重建） */
  let lastHostW = 0;
  let lastHostH = 0;
  /** 异步 mount 世代：重叠调用只让最后一次落地，避免 import 竞态反复拆建 */
  let mountGen = 0;
  /** 已应用到宿主的高度，仅差值超阈值才改尺寸 */
  let lastAppliedHeight = 0;
  /** 缩放级别应用兜底重试标记（正常同步一次成功；极端未就绪时延迟单次重试） */
  let zoomApplyTimer = 0;
  let zoomApplyRetry = 0;
  /** 等级切换/时间轴重绘等待中（等级改变重绘 canvas 时显示等待图标） */
  const zooming = ref(false);
  /** 时间轴列宽（与 timelineHeader.colWidth 保持一致） */
  const TIMELINE_COL_WIDTH = 60;
  /** 甘特图外层框架边框宽度（outerFrameStyle.borderLineWidth 上下合计） */
  const GANTT_FRAME_BORDER = 2;
  /** 时间轴最小跨度（天）：至少覆盖 1 年（366 天含闰年余量），保证整年可查看 */
  const MIN_TIMELINE_DAYS = 366;

  function toDateStr(raw: unknown): string {
    if (raw == null || raw === '') return '';
    const d = raw instanceof Date ? raw : new Date(String(raw));
    if (Number.isNaN(d.getTime())) return '';
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  /** 字段显示名：用于左侧任务列表列标题（计划开始/计划结束） */
  function fieldLabel(name: string): string {
    const f = props.fields.find((x) => x.name === name);
    return f?.displayName?.trim() || f?.name || name;
  }

  /**
   * 行级预处理（OSC-0019）：
   * - 实际有值（actualStartField/actualEndField 均配置且行值非空）→ 主条用实际，基线用计划
   * - 无实际 → 主条回退计划（与基线 overlap 完全重合，视觉单条）
   * - 计划起止均为空的行过滤（现状逻辑保留）
   */
  function buildRecords() {
    const m = props.mapping;
    if (!m) return [];
    return props.records
      .map((row, idx) => {
        const rawId = getValueByKey(row, props.rowKey);
        const id = rawId == null || rawId === '' ? idx : rawId;
        const titleRaw = getValueByKey(row, m.titleField);
        const plannedStart = toDateStr(getValueByKey(row, m.plannedStartField));
        const plannedEnd = toDateStr(getValueByKey(row, m.plannedEndField));
        const hasActual =
          !!m.actualStartField &&
          !!m.actualEndField &&
          toDateStr(getValueByKey(row, m.actualStartField)) !== '' &&
          toDateStr(getValueByKey(row, m.actualEndField)) !== '';
        return {
          __row: row,
          id,
          title: titleRaw == null || titleRaw === '' ? '(无标题)' : String(titleRaw),
          // 主条：有实际用实际，否则回退计划
          __actualStart: hasActual ? toDateStr(getValueByKey(row, m.actualStartField!)) : plannedStart,
          __actualEnd: hasActual ? toDateStr(getValueByKey(row, m.actualEndField!)) : plannedEnd,
          // 基线（计划）
          __plannedStart: plannedStart,
          __plannedEnd: plannedEnd,
        };
      })
      .filter((r) => r.__plannedStart && r.__plannedEnd);
  }

  /** 表格宽度轮询（VTable Gantt 无 resize_table_width 事件，design §4.3 兜底）：
   *  拖拽中 taskTableWidth 连续变化，防抖 300ms 后上报一次，避免频繁重建 */
  function startWidthWatch() {
    stopWidthWatch();
    widthTimer = window.setInterval(() => {
      const raw = gantt?.taskTableWidth as number | undefined;
      if (typeof raw !== 'number' || !Number.isFinite(raw)) return;
      const w = Math.round(raw);
      if (nearlySame(w, lastWidth)) return;
      lastWidth = w;
      if (widthDebounce) window.clearTimeout(widthDebounce);
      widthDebounce = window.setTimeout(() => {
        if (!props.mapping) return;
        const tableWidth = Math.min(640, Math.max(280, w));
        if (nearlySame(tableWidth, props.mapping.tableWidth ?? 380)) return;
        emit('mapping-change', { ...props.mapping, tableWidth });
      }, 300);
    }, 300);
  }

  function stopWidthWatch() {
    if (widthTimer) window.clearInterval(widthTimer);
    if (widthDebounce) window.clearTimeout(widthDebounce);
    widthTimer = 0;
    widthDebounce = 0;
  }

  /**
   * 计算时间轴范围（minDate/maxDate）使时间轴区满足两个约束（需求）：
   * - 时间跨度至少 1 年（MIN_TIMELINE_DAYS），保证整年可查看
   * - 数据日期跨度不足时，以数据中点向两端扩展，时间轴铺满可视区、无右侧留白
   * 数据跨度足够时返回空对象，交给 VTable 按数据范围自适应（更宽时可原生横向滚动切换时间）
   */
  function computeTimelineRange(
    records: Array<{
      __plannedStart: string;
      __plannedEnd: string;
      __actualStart: string;
      __actualEnd: string;
    }>,
  ): { minDate?: string; maxDate?: string } {
    if (!host.value) return {};
    const hostW = host.value.clientWidth || 0;
    const tableW = props.mapping?.tableWidth ?? 380;
    const sceneW = Math.max(0, hostW - tableW - GANTT_FRAME_BORDER);
    // 至少 1 年，同时满足可视区填满（两者取大）
    const minDays = Math.max(MIN_TIMELINE_DAYS, Math.ceil(sceneW / TIMELINE_COL_WIDTH));

    const times: number[] = [];
    for (const r of records) {
      for (const v of [r.__plannedStart, r.__plannedEnd, r.__actualStart, r.__actualEnd]) {
        const t = new Date(v).getTime();
        if (Number.isFinite(t)) times.push(t);
      }
    }
    if (!times.length) return {};
    const minT = Math.min(...times);
    const maxT = Math.max(...times);
    const spanDays = Math.ceil((maxT - minT) / 86400000) + 1;
    if (spanDays >= minDays) return {};

    const extend = minDays - spanDays;
    const half = Math.floor(extend / 2);
    return {
      minDate: toDateStr(new Date(minT - half * 86400000)),
      maxDate: toDateStr(new Date(maxT + (extend - half) * 86400000)),
    };
  }

  /** 容器尺寸监听：可视区宽度变化（窗口缩放/侧栏折叠/拖拽表格宽度）时重建甘特图 */
  function startResizeWatch() {
    stopResizeWatch();
    if (!host.value || typeof ResizeObserver === 'undefined') return;
    resizeObserver = new ResizeObserver(() => {
      if (resizeDebounce) window.clearTimeout(resizeDebounce);
      resizeDebounce = window.setTimeout(() => {
        // 仅在宿主尺寸真正变化时重建；VTable 缩放/时间轴总宽变化等内部布局变化也会触发 RO，
        // 用尺寸对比拦截，避免无谓重建造成“级别来回跳”的循环（跳动根因修复）
        if (!host.value) return;
        const w = host.value.offsetWidth;
        const h = host.value.offsetHeight;
        if (nearlySame(w, lastHostW) && nearlySame(h, lastHostH)) return;
        lastHostW = w;
        lastHostH = h;
        void mountGantt();
      }, 200);
    });
    resizeObserver.observe(host.value);
  }

  function stopResizeWatch() {
    resizeObserver?.disconnect();
    resizeObserver = null;
    if (resizeDebounce) window.clearTimeout(resizeDebounce);
    resizeDebounce = 0;
  }

  /** 时间轴刻度文字样式（粗粒度主行/细粒度子行区分） */
  const mainScaleStyle = {
    fontSize: 12,
    color: themeColor('--color-text-2', '#4E5969'),
    textAlign: 'center' as const,
  };
  const subScaleStyle = {
    fontSize: 11,
    color: themeColor('--color-text-3', '#86909C'),
    textAlign: 'center' as const,
  };

  /**
   * 智能缩放级别（zoomScale.levels，从粗到细）：
   * 级别0 年（宏观整年）→ 级别1 年月 → 级别2 月日（日常计划）→ 级别3 周日 → 级别4 日时（最细）
   * 配合 min/maxMillisecondsPerPixel 约束缩放极限；由父级工具栏缩放按钮控制级别
   */
  function buildZoomLevels(): Array<Array<Record<string, unknown>>> {
    const fmtYear = (d: { startDate: Date }) => `${d.startDate.getFullYear()}`;
    const fmtYearMonth = (d: { startDate: Date }) => `${d.startDate.getFullYear()}-${d.startDate.getMonth() + 1}`;
    const fmtMonth = (d: { startDate: Date }) => `${d.startDate.getMonth() + 1}月`;
    const fmtMonthDay = (d: { startDate: Date }) => `${d.startDate.getMonth() + 1}/${d.startDate.getDate()}`;
    const fmtDay = (d: { startDate: Date }) => `${d.startDate.getDate()}日`;
    const fmtHour = (d: { startDate: Date }) =>
      `${String(d.startDate.getHours()).padStart(2, '0')}:00`;

    return [
      // 级别0：年（最粗，整年视图）
      [{ unit: 'year', step: 1, format: fmtYear, style: mainScaleStyle }],
      // 级别1：年-月
      [
        { unit: 'year', step: 1, format: fmtYear, style: mainScaleStyle },
        { unit: 'month', step: 1, format: fmtMonth, style: subScaleStyle },
      ],
      // 级别2：月-日（日常计划）
      [
        { unit: 'month', step: 1, format: fmtYearMonth, style: mainScaleStyle },
        { unit: 'day', step: 1, format: fmtDay, style: subScaleStyle },
      ],
      // 级别3：周-日
      [
        { unit: 'week', step: 1, format: fmtMonthDay, style: mainScaleStyle },
        { unit: 'day', step: 1, format: fmtDay, style: subScaleStyle },
      ],
      // 级别4：日-时（最细；hour step 12 控制列数：3 年跨度约 2190 列，step 6 时翻倍为 4380）
      [
        { unit: 'day', step: 1, format: fmtMonthDay, style: mainScaleStyle },
        { unit: 'hour', step: 12, format: fmtHour, style: subScaleStyle },
      ],
    ];
  }

  /**
   * 应用缩放级别：创建实例后立即设置（或父级工具栏切换后重新应用）。
   * 等级切换需重绘时间轴 canvas（不同等级列数差异大、可能耗时），先显示等待遮罩并让浏览器渲染一帧，
   * 再执行同步 setZoomPosition；完成后隐藏遮罩（给用户重绘反馈）。
   * ZoomScaleManager 在 Gantt 构造函数中已完整初始化，同步 setZoomPosition 与实例创建同一渲染帧，
   * 消除“先渲染自动初始级别（年）、再延迟跳变到目标级别”的两段式跳动。
   * 仅在极端未就绪时延迟单次重试兜底。
   *
   * <param name="level">缩放级别（zoomScale.levels 下标 0~4）</param>
   */
  function applyZoomLevel(level: number) {
    if (!gantt?.zoomScaleManager) return;
    // 已处于目标级别：跳过，避免无谓的 setZoomPosition 触发整表重绘
    if (gantt.zoomScaleManager.getCurrentLevel?.() === level) return;
    zooming.value = true;
    // 延迟 20ms 执行同步重绘：先让 Vue 渲染等待遮罩并由浏览器绘制一帧，重绘耗时期间用户看到等待反馈。
    // 用 setTimeout 而非 requestAnimationFrame——后台标签/受限环境下 rAF 可能不触发，导致遮罩无法复位
    if (zoomApplyTimer) window.clearTimeout(zoomApplyTimer);
    zoomApplyTimer = window.setTimeout(() => {
      zoomApplyTimer = 0;
      if (doSetZoomLevel(level)) {
        zoomApplyRetry = 0;
        zooming.value = false;
        return;
      }
      // 兜底：ZoomScaleManager 未就绪时延迟单次重试（不再 5 次循环）
      if (zoomApplyRetry >= 1) {
        zoomApplyRetry = 0;
        zooming.value = false;
        return;
      }
      zoomApplyRetry++;
      applyZoomLevel(level);
    }, 20);
  }

  /** 执行 setZoomPosition（同步重绘），返回是否成功 */
  function doSetZoomLevel(level: number): boolean {
    try {
      return !!gantt?.zoomScaleManager?.setZoomPosition?.({ levelNum: level });
    } catch {
      return false;
    }
  }

  /** 甘特图初始定位计时器（mountGantt 中延迟到缩放级别重绘完成后执行） */
  let firstTaskScrollTimer = 0;

  /**
   * 初始定位：滚动时间轴使第一条任务条紧贴左侧表格区、落在可视区域内。
   * 数据跨度被 computeTimelineRange 扩展后，第一条任务条的起点不一定在时间轴最左端，
   * 通过 getXByTime 求起点 x 像素并 setScrollLeft，让用户打开甘特图即看到第一条任务。
   */
  function scrollToFirstTask() {
    if (!gantt) return;
    const list = buildRecords();
    if (!list.length) return;
    const startStr = list[0].__actualStart || list[0].__plannedStart;
    if (!startStr) return;
    const t = new Date(startStr).getTime();
    if (!Number.isFinite(t)) return;
    const x = gantt.getXByTime?.(t);
    if (typeof x !== 'number' || !Number.isFinite(x) || x <= 0) return;
    gantt.stateManager?.setScrollLeft?.(x);
  }

  async function mountGantt() {
    const gen = ++mountGen;
    stopWidthWatch();
    stopResizeWatch();
    if (zoomApplyTimer) window.clearTimeout(zoomApplyTimer);
    zoomApplyTimer = 0;
    zooming.value = false;
    if (!host.value || !props.mapping?.plannedStartField || !props.mapping?.plannedEndField) return;
    const { Gantt } = await import('@visactor/vtable-gantt');
    if (gen !== mountGen || !host.value) return;
    gantt?.release?.();
    gantt = null;
    host.value.innerHTML = '';
    const records = buildRecords();
    if (!records.length) return;

    lastWidth = props.mapping.tableWidth ?? 380;
    lastAppliedHeight = props.height ?? 520;
    // 记录当前宿主尺寸，供 ResizeObserver 尺寸变化判定（避免缩放等内部布局变化误触发重建）
    lastHostW = host.value.offsetWidth;
    lastHostH = host.value.offsetHeight;
    const barColor = props.mapping.barColor ?? themeColor('--primary-6', '22, 93, 255');
    // 时间轴范围：数据跨度不足可视区时扩展 minDate/maxDate 填满（需求 2）
    const { minDate, maxDate } = computeTimelineRange(records);

    // VisActor 类型与运行时 option 不完全对齐，宽松传入
    const option: Record<string, unknown> = {
      records,
      ...(minDate ? { minDate } : {}),
      ...(maxDate ? { maxDate } : {}),
      taskListTable: {
        columns: [
          { field: 'title', title: '标题', width: 160 },
          { field: '__plannedStart', title: fieldLabel(props.mapping.plannedStartField), width: 110 },
          { field: '__plannedEnd', title: fieldLabel(props.mapping.plannedEndField), width: 110 },
        ],
        tableWidth: props.mapping.tableWidth ?? 380,
        minTableWidth: 280,
        maxTableWidth: 640,
        // 只读列表：关闭悬停高亮计算，减少逐帧命中/高亮开销（千条数据下收益明显）
        hover: {
          disableHover: true,
          disableHeaderHover: true,
        },
        // 表头/正文样式与列表视图（ListTable）一致：浅灰表头底 + 次要文字色 + 500 字重
        theme: {
          underlayBackgroundColor: themeColor('--color-bg-2', '#FFFFFF'),
          defaultStyle: {
            bgColor: themeColor('--color-bg-2', '#FFFFFF'),
            color: themeColor('--color-text-1', '#1D2129'),
            borderColor: themeColor('--color-border-2', '#E5E6EB'),
            borderLineWidth: [1, 0, 0, 0],
          },
          headerStyle: {
            bgColor: themeColor('--color-fill-2', '#F2F3F5'),
            color: themeColor('--color-text-2', '#4E5969'),
            fontWeight: 500,
            fontSize: 13,
            borderColor: themeColor('--color-border-2', '#E5E6EB'),
            borderLineWidth: [1, 0, 0, 0],
          },
          bodyStyle: {
            bgColor: themeColor('--color-bg-2', '#FFFFFF'),
            color: themeColor('--color-text-1', '#1D2129'),
            fontWeight: 400,
            fontSize: 13,
            borderColor: themeColor('--color-border-2', '#E5E6EB'),
            borderLineWidth: [1, 0, 0, 0],
            hover: {
              cellBgColor: themeColor('--color-fill-1', '#F7F8FA'),
              inlineRowBgColor: themeColor('--color-fill-1', '#F7F8FA'),
            },
          },
        },
      },
      taskBar: {
        // 主条 = 实际（无实际回退计划），基线 = 计划
        startDateField: '__actualStart',
        endDateField: '__actualEnd',
        baselineStartDateField: '__plannedStart',
        baselineEndDateField: '__plannedEnd',
        baselinePosition: 'overlap',
        baselineStyle: {
          width: 18,
          barColor: 'rgba(134,144,156,0.55)',
          borderLineWidth: 0,
        },
        // 固定任务条颜色：mapping.barColor 缺省主题主色（OSC-0019）
        barStyle: {
          width: 18,
          barColor,
          completedBarColor: barColor,
          borderColor: barColor,
          borderLineWidth: 0,
        },
        // 只读：不引入数据编辑
        moveable: false,
        resizable: false,
        scheduleCreatable: false,
        progressAdjustable: false,
        // 任务条超出可视区时左右边缘显示定位图标（OSC-0019）
        locateIcon: true,
      },
      timelineHeader: {
        colWidth: 60,
        backgroundColor: themeColor('--color-fill-2', '#F2F3F5'),
        horizontalLine: { lineWidth: 1, lineColor: themeColor('--color-border-2', '#E5E6EB') },
        verticalLine: { lineWidth: 1, lineColor: themeColor('--color-border-2', '#E5E6EB') },
        scales: [
          {
            unit: 'day',
            step: 1,
            format(date: { dateIndex: number }) {
              return String(date.dateIndex);
            },
            style: {
              fontSize: 12,
              color: themeColor('--color-text-2', '#4E5969'),
              textAlign: 'center',
              backgroundColor: themeColor('--color-fill-2', '#F2F3F5'),
            },
          },
        ],
        // 智能缩放（需求）：5 级时间刻度（年/月/周/日/时）自动切换，级别由父级工具栏选项框控制
        // （VTable 内部 Ctrl+滚轮缩放无法关闭，已在宿主捕获阶段拦截）
        zoomScale: {
          enabled: true,
          // 最细 1 小时/像素；最粗 3 天/像素（可宏观查看多年，初始级别落在月/日粒度便于阅读任务条）
          minMillisecondsPerPixel: 3600000,
          maxMillisecondsPerPixel: 259200000,
          levels: buildZoomLevels(),
        },
      },
      frame: {
        outerFrameStyle: {
          borderLineWidth: 1,
          borderColor: themeColor('--color-border-2', '#E5E6EB'),
          cornerRadius: 6,
        },
        // 左侧表格宽度拖拽 + 拖拽高亮线（OSC-0019）
        verticalSplitLineMoveable: true,
        verticalSplitLineHighlight: {
          lineColor: themeColor('--primary-6', '22, 93, 255'),
          lineWidth: 2,
        },
      },
      grid: {
        verticalLine: { lineWidth: 1, lineColor: themeColor('--color-border-2', '#E5E6EB') },
        horizontalLine: { lineWidth: 1, lineColor: themeColor('--color-border-2', '#E5E6EB') },
        // 性能：竖线按 day 粒度绘制（默认跟随最细刻度 hour，列数是 day 的 4 倍，缩放/重绘开销大）
        verticalLineDependenceOnTimeScale: 'day',
      },
      headerRowHeight: 36,
      rowHeight: 36,
      overscrollBehavior: 'none',
    };
    gantt = new Gantt(host.value, option as never);
    // 创建后立即同步套上目标等级：Gantt 构造函数会先落到自动初始级别（年），
    // 若再延迟 setZoomPosition，每次重建都会「年→月·日」闪一帧。工具栏切级仍走 applyZoomLevel 遮罩。
    doSetZoomLevel(props.zoomLevel ?? 2);
    startWidthWatch();
    startResizeWatch();
    lastHostW = host.value.offsetWidth;
    lastHostH = host.value.offsetHeight;
    // 初始定位：缩放已同步完成，稍候一帧再滚到首条任务
    if (firstTaskScrollTimer) window.clearTimeout(firstTaskScrollTimer);
    firstTaskScrollTimer = window.setTimeout(scrollToFirstTask, 40);

    gantt.on?.('click_cell', (args: { originData?: { __row?: Record<string, unknown> } }) => {
      const row = args?.originData?.__row;
      if (row) emit('detail', row);
    });
    gantt.on?.('click_task_bar', (args: { record?: { __row?: Record<string, unknown> } }) => {
      const row = args?.record?.__row;
      if (row) emit('detail', row);
    });
  }

  /**
   * 拦截 Ctrl+滚轮：VTable Gantt 内部对 ctrlKey 滚轮强制缩放且无配置可关闭，
   * 在捕获阶段拦截并阻止，缩放统一由父级工具栏选项框控制（需求 5）。
   *
   * <param name="e">滚轮事件</param>
   */
  function onWheelCapture(e: WheelEvent) {
    if (e.ctrlKey) {
      e.preventDefault();
      e.stopPropagation();
    }
  }

  function applyHostSize(height: number) {
    if (!host.value) return;
    host.value.style.height = `${height}px`;
    lastAppliedHeight = height;
    try {
      gantt?._updateSize?.();
    } catch {
      /* 无 _updateSize 时保持当前画布，避免为 1～2px 测高抖动整表重建 */
    }
    lastHostW = host.value.offsetWidth;
    lastHostH = host.value.offsetHeight;
  }

  onMounted(() => {
    // 捕获阶段拦截 Ctrl+滚轮，避免 VTable 内部缩放（Capture 先于 VTable 内部 wheel 监听）
    host.value?.addEventListener('wheel', onWheelCapture, true);
    void mountGantt();
  });

  watch(
    () => ({
      rec: ganttRecordsSignature(props.records, props.mapping, props.rowKey),
      map: ganttMappingSignature(props.mapping),
      h: props.height ?? 520,
    }),
    (next, prev) => {
      if (!prev) return;
      if (next.rec === prev.rec && next.map === prev.map) {
        if (nearlySame(next.h, lastAppliedHeight || prev.h)) return;
        applyHostSize(next.h);
        return;
      }
      void mountGantt();
    },
  );

  // 父级工具栏缩放按钮（−/+）切换 → 重新应用缩放级别
  watch(
    () => props.zoomLevel,
    (level) => {
      if (typeof level === 'number') applyZoomLevel(level);
    },
  );

  onBeforeUnmount(() => {
    mountGen += 1;
    host.value?.removeEventListener('wheel', onWheelCapture, true);
    stopWidthWatch();
    stopResizeWatch();
    if (zoomApplyTimer) window.clearTimeout(zoomApplyTimer);
    zoomApplyTimer = 0;
    if (firstTaskScrollTimer) window.clearTimeout(firstTaskScrollTimer);
    firstTaskScrollTimer = 0;
    gantt?.release?.();
    gantt = null;
  });

  return {
    host,
    zooming,
  };
}

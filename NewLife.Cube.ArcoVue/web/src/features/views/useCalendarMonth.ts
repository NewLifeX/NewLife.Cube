import { computed, nextTick, ref, watch } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import type { CalendarMapping } from '@/core/utils/viewMapping';
import { resolveCellBadge } from '@/core/utils/fieldBadge';
import { getValueByKey } from '@/core/utils/url';

/** 日历模式：日 / 周 / 月（工具栏分段切换；OSC-260926c2b8 补记） */
export type CalendarViewMode = 'day' | 'week' | 'month';

/** 时间轴每小时后高 48px（CalendarMonth.vue 的时段线按同值绘制） */
export const CALENDAR_HOUR_HEIGHT = 48;

/** 周标签（下标＝`getDay()`：0=周日）；周视图列序为 周一…周日（周日最后） */
export const CALENDAR_WEEK_LABELS = ['周日', '周一', '周二', '周三', '周四', '周五', '周六'];

/** CalendarMonth 组件 props 类型（与 CalendarMonth.vue defineProps 泛型逐字一致） */
interface CalendarMonthProps {
  records: Record<string, unknown>[];
  fields: FieldMeta[];
  mapping?: CalendarMapping | null;
  rowKey: string;
  height?: number;
  /** 有新增权时本月空白格可点击新建（OSC-260926c2b8） */
  canAdd?: boolean;
  /** 月导航游标（受控，来自列表页工具栏）。缺省回落组件内部今天 */
  cursor?: Date;
  /** 视图模式（受控，来自列表页工具栏分段）。缺省月 */
  mode?: CalendarViewMode;
}

type CalEvent = {
  key: string;
  title: string;
  color: string;
  row: Record<string, unknown>;
  start: Date;
  end: Date;
};

/** 日历空白日新建门禁（OSC-260926c2b8）：有新增权 + 本月格 + 已配置开始字段三项同时成立 */
export function canCreateOnCalendarCell(canAdd: boolean | undefined, inMonth: boolean, hasStart: boolean): boolean {
  return !!canAdd && inMonth && hasStart;
}

/** 本地日期格式化 YYYY-MM-DD（壁钟拼接，避免 toISOString 的时区漂移） */
export function formatCalendarDate(d: Date): string {
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
}

/** 导航游标位移：日 ±1 天、周 ±7 天、月 ±1 月（保持日号，沿用 setMonth 语义） */
export function shiftCalendarCursor(cursor: Date, delta: number, mode: CalendarViewMode = 'month'): Date {
  const d = new Date(cursor);
  if (mode === 'day') {
    d.setDate(d.getDate() + delta);
    return d;
  }
  if (mode === 'week') {
    d.setDate(d.getDate() + delta * 7);
    return d;
  }
  d.setMonth(d.getMonth() + delta);
  return d;
}

/** 当前周起始日（周一），清零时分 */
export function startOfCalendarWeek(cursor: Date): Date {
  const d = new Date(cursor.getFullYear(), cursor.getMonth(), cursor.getDate());
  d.setDate(d.getDate() - ((d.getDay() + 6) % 7));
  return d;
}

/** 导航标题：月「2026年9月」；周「2026年9月28日 – 10月4日」；日「2026年9月29日 今天/周二」 */
export function calendarRangeLabel(mode: CalendarViewMode, cursor: Date, today: Date = new Date()): string {
  const y = cursor.getFullYear();
  const m = cursor.getMonth() + 1;
  const d = cursor.getDate();
  if (mode === 'month') return `${y}年${m}月`;
  if (mode === 'day') {
    const isToday =
      y === today.getFullYear() && cursor.getMonth() === today.getMonth() && d === today.getDate();
    return `${y}年${m}月${d}日 ${isToday ? '今天' : CALENDAR_WEEK_LABELS[cursor.getDay()]}`;
  }
  const start = startOfCalendarWeek(cursor);
  const end = new Date(start);
  end.setDate(start.getDate() + 6);
  const tail =
    end.getFullYear() === start.getFullYear()
      ? `${end.getMonth() + 1}月${end.getDate()}日`
      : `${end.getFullYear()}年${end.getMonth() + 1}月${end.getDate()}日`;
  return `${start.getFullYear()}年${start.getMonth() + 1}月${start.getDate()}日 – ${tail}`;
}

/** 单日时间轴布局块：top/height 为像素（按 hourHeight 折算），left/width 为百分比（重叠并列） */
export interface TimeSpanLayout {
  top: number;
  height: number;
  leftPct: number;
  widthPct: number;
}

/**
 * 把（可跨天）事件时段布局到某一天的 24 小时时间轴：
 * 裁到当日边界（结束日计入：月末视图同日也展示）；重叠事件贪心分列（先释放的列优先）；零时长按 +1 分钟参与分列并保证最小高度。
 */
export function layoutDaySpans(
  spans: { start: Date; end: Date }[],
  dayStart: Date,
  hourHeight: number,
  minHeight = 20,
): TimeSpanLayout[] {
  const dayStartTs = dayStart.getTime();
  const dayEndTs = dayStartTs + 86_400_000;
  const clipped = spans.map((s, i) => {
    const rawStart = s.start.getTime();
    const startTs = Math.max(rawStart, dayStartTs);
    let endTs = Math.min(Math.max(s.end.getTime(), rawStart), dayEndTs);
    const visible = startTs < dayEndTs && endTs >= dayStartTs;
    if (visible && endTs <= startTs) endTs = Math.min(startTs + 60_000, dayEndTs); // 零时长：占 1 分钟参与分列
    return { i, startTs, endTs, visible };
  });
  const result: TimeSpanLayout[] = spans.map(() => ({ top: 0, height: 0, leftPct: 0, widthPct: 100 }));
  const visible = clipped.filter((c) => c.visible).sort((a, b) => a.startTs - b.startTs || a.endTs - b.endTs);

  let cluster: { item: (typeof visible)[number]; col: number }[] = [];
  let colEnds: number[] = [];
  let clusterMaxEnd = -Infinity;
  const closeCluster = () => {
    if (!cluster.length) return;
    const colCount = Math.max(1, colEnds.length);
    const widthPct = 100 / colCount;
    for (const { item, col } of cluster) {
      result[item.i] = {
        top: ((item.startTs - dayStartTs) / 3_600_000) * hourHeight,
        height: Math.max(minHeight, ((item.endTs - item.startTs) / 3_600_000) * hourHeight),
        leftPct: col * widthPct,
        widthPct,
      };
    }
    cluster = [];
    colEnds = [];
    clusterMaxEnd = -Infinity;
  };
  for (const c of visible) {
    if (cluster.length && c.startTs > clusterMaxEnd) closeCluster();
    let col = colEnds.findIndex((end) => end <= c.startTs);
    if (col < 0) {
      colEnds.push(c.endTs);
      col = colEnds.length - 1;
    } else {
      colEnds[col] = c.endTs;
    }
    cluster.push({ item: c, col });
    clusterMaxEnd = Math.max(clusterMaxEnd, c.endTs);
  }
  closeCluster();
  return result;
}

/** 日/周时间轴点击空白：按点击位置取整点小时（0~23 夹取） */
export function timeGridCreatePayload(
  dateText: string,
  offsetY: number,
  hourHeight: number,
): { date: string; hour: number } {
  const hour = Math.min(23, Math.max(0, Math.floor((offsetY || 0) / hourHeight)));
  return { date: dateText, hour };
}

/** CalendarMonth 组件全部业务 TS：月历网格/事件按天索引/事件文字颜色（自 CalendarMonth.vue script setup 原样搬移；emits 仅模板 $emit 使用） */
export function useCalendarMonth(props: CalendarMonthProps) {
  /** 月网格表头：周一为一周第一天（周日最后） */
  const weekLabels = ['一', '二', '三', '四', '五', '六', '日'];
  /** 未传入受控游标时的兜底（独立使用场景） */
  const fallbackCursor = ref(new Date());
  const cursor = computed(() => props.cursor ?? fallbackCursor.value);

  const year = computed(() => cursor.value.getFullYear());
  const month = computed(() => cursor.value.getMonth());

  function parseDate(raw: unknown): Date | null {
    if (raw == null || raw === '') return null;
    const d = raw instanceof Date ? raw : new Date(String(raw));
    return Number.isNaN(d.getTime()) ? null : d;
  }

  function dayKey(d: Date): string {
    return `${d.getFullYear()}-${d.getMonth()}-${d.getDate()}`;
  }

  /** 事件标签文字颜色：按背景亮度选深/浅字（浅色背景（徽标浅色/主题浅色阶）用深字，深背景用白字） */
  function eventTextColor(bg: string): string {
    let r = 255;
    let g = 255;
    let b = 255;
    const hex = /#([0-9a-fA-F]{6})/.exec(bg);
    const rgb = /rgba?\(\s*(\d+)[^,]*,\s*(\d+)[^,]*,\s*(\d+)/.exec(bg);
    if (hex) {
      r = parseInt(hex[1].slice(0, 2), 16);
      g = parseInt(hex[1].slice(2, 4), 16);
      b = parseInt(hex[1].slice(4, 6), 16);
    } else if (rgb) {
      r = Number(rgb[1]);
      g = Number(rgb[2]);
      b = Number(rgb[3]);
    }
    const yiq = (r * 299 + g * 587 + b * 114) / 1000;
    return yiq >= 160 ? 'var(--color-text-1)' : '#fff';
  }

  const events = computed<CalEvent[]>(() => {
    const m = props.mapping;
    if (!m?.startField) return [];
    const colorField = m.colorField
      ? props.fields.find((f) => f.name === m.colorField)
      : undefined;
    const out: CalEvent[] = [];
    props.records.forEach((row, idx) => {
      const start = parseDate(getValueByKey(row, m.startField));
      if (!start) return;
      const end = m.endField
        ? parseDate(getValueByKey(row, m.endField)) || start
        : start;
      const titleRaw = getValueByKey(row, m.titleField);
      const title = titleRaw == null || titleRaw === '' ? '(无标题)' : String(titleRaw);
      let color = 'rgb(var(--primary-6))';
      if (colorField) {
        const badge = resolveCellBadge(colorField, getValueByKey(row, colorField.name));
        if (badge) color = badge.textColor;
      }
      const id = getValueByKey(row, props.rowKey);
      out.push({
        key: id != null && id !== '' ? String(id) : `e-${idx}`,
        title,
        color,
        row,
        start,
        end: end < start ? start : end,
      });
    });
    return out;
  });

  const cells = computed(() => {
    const y = year.value;
    const m = month.value;
    const first = new Date(y, m, 1);
    // 周一为一周第一天（周日最后）：补齐格数按周一六日序
    const startPad = (first.getDay() + 6) % 7;
    const today = new Date();
    const todayKey = dayKey(today);

    // 事件按天索引（性能）：仅构建当月日历网格窗口 [gridStart, gridEnd] 的天→事件 Map，
    // 每格 O(1) 查询，替代原每格 events.filter + 每事件多次 new Date（1000 事件约 12.6 万次 Date 分配）
    const gridStart = new Date(y, m, 1 - startPad);
    const gridEnd = new Date(y, m, 42 - startPad);
    const byDay = new Map<string, CalEvent[]>();
    for (const ev of events.value) {
      const s = new Date(ev.start.getFullYear(), ev.start.getMonth(), ev.start.getDate());
      const e = new Date(ev.end.getFullYear(), ev.end.getMonth(), ev.end.getDate());
      // 与网格窗口无交集的事件跳过；遍历仅限窗口交集内天数（事件一般很短）
      if (e < gridStart || s > gridEnd) continue;
      const dStart = s < gridStart ? new Date(gridStart) : s;
      const dEnd = e > gridEnd ? gridEnd : e;
      for (let d = new Date(dStart); d <= dEnd; d.setDate(d.getDate() + 1)) {
        const key = dayKey(d);
        const arr = byDay.get(key);
        if (arr) arr.push(ev);
        else byDay.set(key, [ev]);
      }
    }

    const result: {
      day: number;
      inMonth: boolean;
      isToday: boolean;
      events: CalEvent[];
      /** 日键（弹层受控与去重共用） */
      key: string;
      /** 该格自然日（YYYY-MM-DD，含非本月补齐格） */
      dateText: string;
      /** 有新增权且为本月格时可点击新建 */
      canCreate: boolean;
    }[] = [];
    const hasStart = !!props.mapping?.startField;
    for (let i = 0; i < 42; i++) {
      const d = new Date(gridStart);
      d.setDate(gridStart.getDate() + i);
      const key = dayKey(d);
      const inMonth = d.getMonth() === m;
      result.push({
        day: d.getDate(),
        inMonth,
        isToday: key === todayKey,
        events: byDay.get(key) ?? [],
        key,
        dateText: formatCalendarDate(d),
        canCreate: canCreateOnCalendarCell(props.canAdd, inMonth, hasStart),
      });
    }
    return result;
  });

  /* ---------------- 日 / 周 模式：时间轴网格（OSC-260926c2b8 补记） ---------------- */

  /** 模式：缺省月（受控，来自工具栏分段） */
  const mode = computed<CalendarViewMode>(() => props.mode ?? 'month');
  /** 时间轴每小时高度（像素） */
  const hourHeight = CALENDAR_HOUR_HEIGHT;
  /** 时间轴整天高度 */
  const dayHeight = hourHeight * 24;
  /** 时刻刻度（00:00–23:00） */
  const hourTicks = Array.from({ length: 24 }, (_, h) => `${String(h).padStart(2, '0')}:00`);

  /** 当前模式的日期范围：周 7 天（周一起，周日最后）/ 日 1 天 / 月 空（用 42 格网格） */
  const rangeDays = computed<Date[]>(() => {
    const c = cursor.value;
    if (mode.value === 'week') {
      const start = startOfCalendarWeek(c);
      return Array.from({ length: 7 }, (_, i) => {
        const d = new Date(start);
        d.setDate(start.getDate() + i);
        return d;
      });
    }
    if (mode.value === 'day') return [new Date(c.getFullYear(), c.getMonth(), c.getDate())];
    return [];
  });

  function formatTime(d: Date): string {
    const p = (n: number) => String(n).padStart(2, '0');
    return `${p(d.getHours())}:${p(d.getMinutes())}`;
  }

  /** 周 / 日列：每列裁天后的定位事件块与「今天」标记（与月网格同日也展示结束日一致） */
  const timeColumns = computed(() => {
    const todayKey = dayKey(new Date());
    return rangeDays.value.map((day) => {
      const dayStartTs = day.getTime();
      const dayEndTs = dayStartTs + 86_400_000;
      const dayEvents = events.value.filter(
        (ev) => ev.start.getTime() < dayEndTs && Math.max(ev.end.getTime(), ev.start.getTime()) >= dayStartTs,
      );
      const layouts = layoutDaySpans(
        dayEvents.map((ev) => ({ start: ev.start, end: ev.end })),
        day,
        hourHeight,
      );
      return {
        key: dayKey(day),
        dateText: formatCalendarDate(day),
        canCreate: canCreateOnCalendarCell(props.canAdd, true, !!props.mapping?.startField),
        isToday: dayKey(day) === todayKey,
        weekLabel: CALENDAR_WEEK_LABELS[day.getDay()],
        dateLabel: `${day.getMonth() + 1}月${day.getDate()}日`,
        blocks: dayEvents.map((ev, i) => ({
          key: ev.key,
          top: layouts[i].top,
          height: layouts[i].height,
          leftPct: layouts[i].leftPct,
          widthPct: layouts[i].widthPct,
          color: ev.color,
          textColor: eventTextColor(ev.color),
          title: ev.title,
          timeText: formatTime(ev.start),
          row: ev.row,
        })),
      };
    });
  });

  /** 今天在范围内时的「当前时刻」线（像素）；否则 null */
  const nowTop = computed(() => {
    if (mode.value === 'month') return null;
    const now = new Date();
    if (!rangeDays.value.some((d) => dayKey(d) === dayKey(now))) return null;
    return ((now.getHours() * 60 + now.getMinutes()) / 60) * hourHeight;
  });

  /** 时间轴空白点击：事件坐标转整点载荷（模板 $emit('create', …) 消费） */
  function onTimeGridClick(dateText: string, event: MouseEvent) {
    return timeGridCreatePayload(dateText, event?.offsetY ?? 0, hourHeight);
  }

  /** 时间轴滚动容器（模板 ref 绑定）；挂载/切模式/切日期后滚到 08:00，或更早的首个事件 */
  const gridEl = ref<HTMLElement | null>(null);
  function scrollTimeGrid() {
    const el = gridEl.value;
    if (!el || mode.value === 'month') return;
    let earliest = Infinity;
    for (const col of timeColumns.value) {
      for (const b of col.blocks) earliest = Math.min(earliest, b.top);
    }
    el.scrollTop = earliest < 8 * hourHeight ? Math.max(0, earliest - 8) : 8 * hourHeight;
  }
  watch([mode, () => cursor.value.getTime()], () => nextTick(scrollTimeGrid), { immediate: true });

  /** +N 弹层展开的日键（受控；点条目或关闭后置空）（OSC-260926c2b8） */
  const moreKey = ref<string | null>(null);

  /** +N 弹层点条目：关闭弹层并返回行（模板 $emit('detail', ...) 消费） */
  function pickMoreEvent(row: Record<string, unknown>) {
    moreKey.value = null;
    return row;
  }

  return {
    weekLabels,
    year,
    month,
    eventTextColor,
    cells,
    hourTicks,
    hourHeight,
    dayHeight,
    timeColumns,
    nowTop,
    gridEl,
    onTimeGridClick,
    moreKey,
    pickMoreEvent,
  };
}

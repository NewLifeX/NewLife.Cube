import { computed, ref } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import type { CalendarMapping } from '@/core/utils/viewMapping';
import { resolveCellBadge } from '@/core/utils/fieldBadge';
import { getValueByKey } from '@/core/utils/url';

/** CalendarMonth 组件 props 类型（与 CalendarMonth.vue defineProps 泛型逐字一致） */
interface CalendarMonthProps {
  records: Record<string, unknown>[];
  fields: FieldMeta[];
  mapping?: CalendarMapping | null;
  rowKey: string;
  height?: number;
}

type CalEvent = {
  key: string;
  title: string;
  color: string;
  row: Record<string, unknown>;
  start: Date;
  end: Date;
};

/** CalendarMonth 组件全部业务 TS：月历网格/事件按天索引/事件文字颜色（自 CalendarMonth.vue script setup 原样搬移；emits 仅模板 $emit 使用） */
export function useCalendarMonth(props: CalendarMonthProps) {
  const weekLabels = ['日', '一', '二', '三', '四', '五', '六'];
  const cursor = ref(new Date());

  const year = computed(() => cursor.value.getFullYear());
  const month = computed(() => cursor.value.getMonth());

  function shiftMonth(delta: number) {
    const d = new Date(cursor.value);
    d.setMonth(d.getMonth() + delta);
    cursor.value = d;
  }

  function goToday() {
    cursor.value = new Date();
  }

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
    const startPad = first.getDay();
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
    }[] = [];
    for (let i = 0; i < 42; i++) {
      const d = new Date(gridStart);
      d.setDate(gridStart.getDate() + i);
      const key = dayKey(d);
      result.push({
        day: d.getDate(),
        inMonth: d.getMonth() === m,
        isToday: key === todayKey,
        events: byDay.get(key) ?? [],
      });
    }
    return result;
  });

  return {
    weekLabels,
    year,
    month,
    shiftMonth,
    goToday,
    eventTextColor,
    cells,
  };
}

<template>
  <div class="calendar-month" :style="{ minHeight: height + 'px' }">
    <div class="cal-toolbar">
      <a-button size="small" @click="shiftMonth(-1)">上一月</a-button>
      <span class="cal-title">{{ year }}年{{ month + 1 }}月</span>
      <a-button size="small" @click="shiftMonth(1)">下一月</a-button>
      <a-button size="small" type="text" @click="goToday">今天</a-button>
    </div>
    <div class="cal-weekhead">
      <div v-for="d in weekLabels" :key="d">{{ d }}</div>
    </div>
    <div class="cal-grid">
      <div
        v-for="(cell, idx) in cells"
        :key="idx"
        class="cal-cell"
        :class="{ muted: !cell.inMonth, today: cell.isToday }"
      >
        <div class="cal-day">{{ cell.day }}</div>
        <button
          v-for="ev in cell.events.slice(0, 3)"
          :key="ev.key"
          type="button"
          class="cal-event"
          :style="{ background: ev.color, color: eventTextColor(ev.color) }"
          :title="ev.title"
          @click="$emit('detail', ev.row)"
        >
          {{ ev.title }}
        </button>
        <div v-if="cell.events.length > 3" class="cal-more">+{{ cell.events.length - 3 }}</div>
      </div>
    </div>
    <a-alert
      v-if="!mapping?.startField"
      type="warning"
      style="margin-top: 8px"
    >
      请在自定义配置中设置日历开始日期字段
    </a-alert>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import type { CalendarMapping } from '@/core/utils/viewMapping';
import { resolveCellBadge } from '@/core/utils/fieldBadge';
import { getValueByKey } from '@/core/utils/url';

const props = withDefaults(
  defineProps<{
    records: Record<string, unknown>[];
    fields: FieldMeta[];
    mapping?: CalendarMapping | null;
    rowKey: string;
    height?: number;
  }>(),
  { height: 520 },
);

defineEmits<{
  detail: [row: Record<string, unknown>];
}>();

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

type CalEvent = {
  key: string;
  title: string;
  color: string;
  row: Record<string, unknown>;
  start: Date;
  end: Date;
};

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
  const result: {
    day: number;
    inMonth: boolean;
    isToday: boolean;
    events: CalEvent[];
  }[] = [];

  const gridStart = new Date(y, m, 1 - startPad);
  for (let i = 0; i < 42; i++) {
    const d = new Date(gridStart);
    d.setDate(gridStart.getDate() + i);
    const inMonth = d.getMonth() === m;
    const key = dayKey(d);
    const dayEvents = events.value.filter((ev) => {
      const s = new Date(ev.start.getFullYear(), ev.start.getMonth(), ev.start.getDate());
      const e = new Date(ev.end.getFullYear(), ev.end.getMonth(), ev.end.getDate());
      const cur = new Date(d.getFullYear(), d.getMonth(), d.getDate());
      return cur >= s && cur <= e;
    });
    result.push({
      day: d.getDate(),
      inMonth,
      isToday: key === todayKey,
      events: dayEvents,
    });
  }
  return result;
});
</script>

<style scoped>
.calendar-month {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.cal-toolbar {
  display: flex;
  align-items: center;
  gap: 8px;
}
.cal-title {
  font-weight: 500;
  min-width: 120px;
  text-align: center;
}
.cal-weekhead,
.cal-grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
}
.cal-weekhead {
  font-size: 12px;
  color: var(--color-text-3);
  text-align: center;
  padding: 4px 0;
}
.cal-cell {
  min-height: 88px;
  border: 1px solid var(--color-border-1);
  padding: 4px;
  background: var(--color-bg-2);
}
.cal-cell.muted {
  opacity: 0.45;
}
.cal-cell.today {
  background: var(--color-primary-light-1);
}
.cal-day {
  font-size: 12px;
  margin-bottom: 2px;
}
.cal-event {
  display: block;
  width: 100%;
  border: none;
  border-radius: 3px;
  color: #fff;
  font-size: 11px;
  padding: 1px 4px;
  margin-bottom: 2px;
  text-align: left;
  cursor: pointer;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.cal-more {
  font-size: 11px;
  color: var(--color-text-3);
}
</style>

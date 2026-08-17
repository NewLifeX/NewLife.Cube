<template>
  <div class="calendar-month" :style="{ minHeight: height + 'px' }">
    <div
      v-if="!mapping?.startField"
      class="view-empty-wrap"
      :style="{ minHeight: (height || 240) + 'px' }"
    >
      <a-alert type="warning">请在自定义配置中设置日历开始日期字段</a-alert>
    </div>
    <template v-else>
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
    </template>
  </div>
</template>

<script setup lang="ts">
import type { FieldMeta } from '@/core/types/field';
import type { CalendarMapping } from '@/core/utils/viewMapping';
import { useCalendarMonth } from './useCalendarMonth';

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

const {
  weekLabels,
  year,
  month,
  shiftMonth,
  goToday,
  eventTextColor,
  cells,
} = useCalendarMonth(props);
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

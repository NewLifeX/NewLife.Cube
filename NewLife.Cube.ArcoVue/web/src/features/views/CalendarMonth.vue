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
          :class="{ muted: !cell.inMonth, today: cell.isToday, 'can-create': cell.canCreate }"
          @click="cell.canCreate && $emit('create', { date: cell.dateText })"
        >
          <div class="cal-day">{{ cell.day }}</div>
          <button
            v-for="ev in cell.events.slice(0, 3)"
            :key="ev.key"
            type="button"
            class="cal-event"
            :style="{ background: ev.color, color: eventTextColor(ev.color) }"
            :title="ev.title"
            @click.stop="$emit('detail', ev.row)"
          >
            {{ ev.title }}
          </button>
          <a-popover
            v-if="cell.events.length > 3"
            trigger="click"
            position="top"
            :popup-visible="moreKey === cell.key"
            @popup-visible-change="(v: boolean) => (moreKey = v ? cell.key : null)"
          >
            <template #content>
              <div class="cal-more-panel">
                <button
                  v-for="ev in cell.events"
                  :key="ev.key"
                  type="button"
                  class="cal-more-item"
                  :title="ev.title"
                  @click="$emit('detail', pickMoreEvent(ev.row))"
                >
                  {{ ev.title }}
                </button>
              </div>
            </template>
            <button type="button" class="cal-more" @click.stop>+{{ cell.events.length - 3 }}</button>
          </a-popover>
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
    /** 有新增权时本月空白格可点击新建（OSC-260926c2b8） */
    canAdd?: boolean;
  }>(),
  { height: 520, canAdd: false },
);

defineEmits<{
  detail: [row: Record<string, unknown>];
  create: [payload: { date: string }];
}>();

const {
  weekLabels,
  year,
  month,
  shiftMonth,
  goToday,
  eventTextColor,
  cells,
  moreKey,
  pickMoreEvent,
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
.cal-cell.can-create {
  cursor: pointer;
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
  background: none;
  border: none;
  padding: 0 4px;
  cursor: pointer;
}
.cal-more-panel {
  display: flex;
  flex-direction: column;
  gap: 2px;
  max-width: 280px;
  max-height: 240px;
  overflow-y: auto;
}
.cal-more-item {
  display: block;
  width: 100%;
  border: none;
  background: none;
  border-radius: 3px;
  font-size: 12px;
  padding: 4px 8px;
  text-align: left;
  cursor: pointer;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.cal-more-item:hover {
  background: var(--color-fill-2);
}
</style>

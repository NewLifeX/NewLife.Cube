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
      <!-- 月：42 格网格（原有） -->
      <template v-if="mode === 'month'">
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

      <!-- 日 / 周：时间轴网格（时刻列 + 事件块按开始时间定位，重叠并列） -->
      <div v-else class="cal-time-wrap" :style="{ height: (height || 520) + 'px' }">
        <div ref="gridEl" class="cal-time-scroll">
          <!-- 表头在滚动容器内 sticky：与列共用同一宽度（避免滚动条压缩内容区导致错位） -->
          <div class="cal-time-head">
            <div class="cal-axis-head">GMT+8</div>
            <div
              v-for="col in timeColumns"
              :key="col.key"
              class="cal-time-head-cell"
              :class="{ today: col.isToday }"
            >
              <span class="cal-time-week">{{ col.weekLabel }}</span>
              <span class="cal-time-date">{{ col.dateLabel }}</span>
            </div>
          </div>
          <div class="cal-time-body" :style="{ height: dayHeight + 'px' }">
            <div class="cal-axis">
              <div
                v-for="tick in hourTicks"
                :key="tick"
                class="cal-axis-tick"
                :style="{ height: hourHeight + 'px' }"
              >
                {{ tick }}
              </div>
            </div>
            <div
              v-for="col in timeColumns"
              :key="col.key"
              class="cal-time-col"
              :class="{ today: col.isToday, 'can-create': col.canCreate }"
              @click="col.canCreate && $emit('create', onTimeGridClick(col.dateText, $event))"
            >
              <button
                v-for="block in col.blocks"
                :key="block.key"
                type="button"
                class="cal-time-block"
                :style="{
                  top: block.top + 'px',
                  height: block.height + 'px',
                  left: block.leftPct + '%',
                  width: 'calc(' + block.widthPct + '% - 2px)',
                  background: block.color,
                  color: block.textColor,
                }"
                :title="block.title"
                @click.stop="$emit('detail', block.row)"
              >
                <span class="cal-time-block-time">{{ block.timeText }}</span>{{ block.title }}
              </button>
            </div>
            <div v-if="nowTop !== null" class="cal-now-line" :style="{ top: nowTop + 'px' }"></div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import type { FieldMeta } from '@/core/types/field';
import type { CalendarMapping } from '@/core/utils/viewMapping';
import { useCalendarMonth, type CalendarViewMode } from './useCalendarMonth';

const props = withDefaults(
  defineProps<{
    records: Record<string, unknown>[];
    fields: FieldMeta[];
    mapping?: CalendarMapping | null;
    rowKey: string;
    height?: number;
    /** 有新增权时本月空白格可点击新建（OSC-260926c2b8） */
    canAdd?: boolean;
    /** 月导航游标（受控，来自列表页工具栏）。缺省回落组件内部今天 */
    cursor?: Date;
    /** 视图模式：日 / 周 / 月（受控；缺省月）（OSC-260926c2b8 补记） */
    mode?: CalendarViewMode;
  }>(),
  { height: 520, canAdd: false },
);

defineEmits<{
  detail: [row: Record<string, unknown>];
  create: [payload: { date: string; hour?: number }];
}>();

const {
  weekLabels,
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
} = useCalendarMonth(props);
</script>

<style scoped>
.calendar-month {
  display: flex;
  flex-direction: column;
  gap: 8px;
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
/* 日 / 周：时间轴网格（OSC-260926c2b8 补记） */
.cal-time-wrap {
  display: flex;
  flex-direction: column;
  border: 1px solid var(--color-border-1);
  border-radius: 4px;
  overflow: hidden;
  background: var(--color-bg-2);
}
/* 表头与内容同滚动容器：sticky 置顶，宽度随滚动条同步（对齐列边界） */
.cal-time-head {
  display: flex;
  position: sticky;
  top: 0;
  z-index: 3;
  border-bottom: 1px solid var(--color-border-2);
  background: var(--color-bg-2);
}
.cal-axis-head {
  flex: 0 0 56px;
  width: 56px;
  display: flex;
  align-items: flex-end;
  justify-content: center;
  padding-bottom: 4px;
  font-size: 11px;
  color: var(--color-text-3);
  border-right: 1px solid var(--color-border-1);
}
.cal-time-head-cell {
  flex: 1;
  min-width: 0;
  padding: 6px 4px;
  text-align: center;
  display: flex;
  flex-direction: column;
  gap: 2px;
  border-right: 1px solid var(--color-border-1);
}
.cal-time-head-cell:last-child {
  border-right: none;
}
.cal-time-week {
  font-size: 12px;
  color: var(--color-text-3);
}
.cal-time-date {
  font-size: 13px;
  color: var(--color-text-1);
}
.cal-time-head-cell.today .cal-time-date {
  color: rgb(var(--primary-6));
  font-weight: 600;
}
.cal-time-scroll {
  flex: 1;
  overflow-y: auto;
}
.cal-time-body {
  position: relative;
  display: flex;
}
.cal-axis {
  flex: 0 0 56px;
  width: 56px;
  border-right: 1px solid var(--color-border-1);
}
.cal-axis-tick {
  box-sizing: border-box;
  font-size: 11px;
  color: var(--color-text-3);
  text-align: right;
  padding-right: 6px;
  border-top: 1px solid var(--color-border-1);
}
/* 时段线：与 CALENDAR_HOUR_HEIGHT=48 保持一致 */
.cal-time-col {
  flex: 1;
  min-width: 0;
  position: relative;
  border-right: 1px solid var(--color-border-1);
  background-image: repeating-linear-gradient(
    to bottom,
    var(--color-border-1) 0,
    var(--color-border-1) 1px,
    transparent 1px,
    transparent 48px
  );
}
.cal-time-col:last-child {
  border-right: none;
}
.cal-time-col.today {
  background-color: rgba(var(--primary-6), 0.03);
}
.cal-time-col.can-create {
  cursor: pointer;
}
.cal-time-block {
  position: absolute;
  box-sizing: border-box;
  border: none;
  border-radius: 3px;
  padding: 1px 4px;
  font-size: 11px;
  line-height: 1.3;
  text-align: left;
  overflow: hidden;
  cursor: pointer;
  z-index: 1;
}
.cal-time-block-time {
  margin-right: 4px;
  opacity: 0.85;
}
.cal-now-line {
  position: absolute;
  left: 56px;
  right: 0;
  border-top: 1px dashed rgb(var(--danger-6));
  pointer-events: none;
  z-index: 2;
}
</style>

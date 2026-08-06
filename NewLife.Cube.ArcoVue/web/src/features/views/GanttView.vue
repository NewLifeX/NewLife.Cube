<template>
  <div class="gantt-view">
    <div ref="host" class="gantt-host" :style="{ height: height + 'px' }" />
    <a-alert v-if="!mapping?.startField || !mapping?.endField" type="warning" style="margin-top: 8px">
      请在自定义配置中设置甘特开始/结束日期字段
    </a-alert>
    <a-empty v-else-if="!records.length" description="暂无数据" />
  </div>
</template>

<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import type { GanttMapping } from '@/core/utils/viewMapping';
import { resolveCellBadge } from '@/core/utils/fieldBadge';
import { getValueByKey } from '@/core/utils/url';
import { themeColor } from '@/core/utils/themeColor';

const props = withDefaults(
  defineProps<{
    records: Record<string, unknown>[];
    fields: FieldMeta[];
    mapping?: GanttMapping | null;
    rowKey: string;
    height?: number;
  }>(),
  { height: 520 },
);

const emit = defineEmits<{
  detail: [row: Record<string, unknown>];
}>();

const host = ref<HTMLElement | null>(null);
// eslint-disable-next-line @typescript-eslint/no-explicit-any
let gantt: any = null;

function toDateStr(raw: unknown): string {
  if (raw == null || raw === '') return '';
  const d = raw instanceof Date ? raw : new Date(String(raw));
  if (Number.isNaN(d.getTime())) return '';
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

function buildRecords() {
  const m = props.mapping;
  if (!m) return [];
  const colorField = m.colorField
    ? props.fields.find((f) => f.name === m.colorField)
    : undefined;
  return props.records.map((row, idx) => {
    const rawId = getValueByKey(row, props.rowKey);
    const id = rawId == null || rawId === '' ? idx : rawId;
    const titleRaw = getValueByKey(row, m.titleField);
    let barColor = themeColor('--primary-6', '22, 93, 255');
    if (colorField) {
      const badge = resolveCellBadge(colorField, getValueByKey(row, colorField.name));
      if (badge) barColor = badge.textColor;
    }
    return {
      __row: row,
      id,
      title: titleRaw == null || titleRaw === '' ? '(无标题)' : String(titleRaw),
      start: toDateStr(getValueByKey(row, m.startField)),
      end: toDateStr(getValueByKey(row, m.endField)),
      barColor,
    };
  }).filter((r) => r.start && r.end);
}

async function mountGantt() {
  if (!host.value || !props.mapping?.startField || !props.mapping?.endField) return;
  const { Gantt } = await import('@visactor/vtable-gantt');
  gantt?.release?.();
  gantt = null;
  host.value.innerHTML = '';
  const records = buildRecords();
  if (!records.length) return;

  // VisActor 类型与运行时 option 不完全对齐，宽松传入
  const option: Record<string, unknown> = {
    records,
    taskListTable: {
      columns: [
        { field: 'title', title: '标题', width: 160 },
        { field: 'start', title: '开始', width: 110 },
        { field: 'end', title: '结束', width: 110 },
      ],
      tableWidth: 380,
      minTableWidth: 280,
      maxTableWidth: 480,
    },
    taskBar: {
      startDateField: 'start',
      endDateField: 'end',
      moveable: false,
      resizable: false,
      scheduleCreatable: false,
      barStyle: {
        width: 18,
        barColor: themeColor('--primary-6', '22, 93, 255'),
        completedBarColor: themeColor('--primary-6', '22, 93, 255'),
        borderColor: themeColor('--primary-6', '22, 93, 255'),
        borderLineWidth: 0,
      },
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
    },
    frame: {
      outerFrameStyle: {
        borderLineWidth: 1,
        borderColor: themeColor('--color-border-2', '#E5E6EB'),
        cornerRadius: 6,
      },
    },
    grid: {
      verticalLine: { lineWidth: 1, lineColor: themeColor('--color-border-2', '#E5E6EB') },
      horizontalLine: { lineWidth: 1, lineColor: themeColor('--color-border-2', '#E5E6EB') },
    },
    headerRowHeight: 36,
    rowHeight: 36,
    overscrollBehavior: 'none',
  };
  gantt = new Gantt(host.value, option as never);

  gantt.on?.('click_cell', (args: { originData?: { __row?: Record<string, unknown> } }) => {
    const row = args?.originData?.__row;
    if (row) emit('detail', row);
  });
  gantt.on?.('click_task_bar', (args: { record?: { __row?: Record<string, unknown> } }) => {
    const row = args?.record?.__row;
    if (row) emit('detail', row);
  });
}

onMounted(() => {
  void mountGantt();
});

watch(
  () => [props.records, props.mapping, props.height] as const,
  () => {
    void mountGantt();
  },
  { deep: true },
);

onBeforeUnmount(() => {
  gantt?.release?.();
  gantt = null;
});
</script>

<style scoped>
.gantt-view {
  width: 100%;
}
.gantt-host {
  width: 100%;
  min-height: 240px;
}
</style>

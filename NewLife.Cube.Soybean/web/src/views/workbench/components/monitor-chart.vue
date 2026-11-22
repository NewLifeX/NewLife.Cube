<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue';
import type { MonitorPoint } from '@newlifex/page-logic';
import {
  MONITOR_POLL_INTERVAL,
  appendMonitorPoint,
  buildMonitorAxis,
  buildMonitorChart,
  createWorkbenchLoader
} from '@newlifex/page-logic';
import { cubeApi } from '@/api/cube';
import { $t } from '@/locales';

defineOptions({ name: 'MonitorChart' });

const CHART_WIDTH = 600;
const CHART_HEIGHT = 160;
const GRID_LINES = [0.25, 0.5, 0.75].map(ratio => Math.round(CHART_HEIGHT * ratio));

const loader = createWorkbenchLoader(cubeApi);
const points = ref<MonitorPoint[]>([]);
let timer: number | undefined;
let disposed = false;
let polling = false;

const latest = computed(() => points.value.at(-1));
const chart = computed(() => buildMonitorChart(points.value, CHART_WIDTH, CHART_HEIGHT));
const axisTicks = computed(() => buildMonitorAxis(points.value, 5));
const cpuText = computed(() => (latest.value ? `${latest.value.cpu.toFixed(1)}%` : '--'));
const memText = computed(() => (latest.value ? `${latest.value.memory.toFixed(1)}%` : '--'));
const usageText = computed(() => {
  const point = latest.value;
  if (!point?.memUsed || !point?.memTotal) return '';
  return `${(point.memUsed / 1024).toFixed(1)} / ${(point.memTotal / 1024).toFixed(1)} GB`;
});

async function poll() {
  if (polling) return;
  polling = true;

  try {
    const point = await loader.pollMonitor();
    if (!disposed && point) points.value = appendMonitorPoint(points.value, point);
  } finally {
    polling = false;
  }
}

onMounted(() => {
  poll();
  timer = window.setInterval(poll, MONITOR_POLL_INTERVAL);
});

onUnmounted(() => {
  disposed = true;
  if (timer) window.clearInterval(timer);
});
</script>

<template>
  <div class="monitor">
    <div class="monitor__legend">
      <span class="monitor__item">
        <i class="monitor__dot monitor__dot--cpu" />
        {{ $t('page.workbench.cpu') }}
        <b>{{ cpuText }}</b>
      </span>
      <span class="monitor__item">
        <i class="monitor__dot monitor__dot--mem" />
        {{ $t('page.workbench.memory') }}
        <b>{{ memText }}</b>
      </span>
      <span v-if="usageText" class="monitor__usage">{{ usageText }}</span>
    </div>

    <div class="monitor__body">
      <svg class="monitor__chart" :viewBox="`0 0 ${CHART_WIDTH} ${CHART_HEIGHT}`" preserveAspectRatio="none">
        <line
          v-for="y in GRID_LINES"
          :key="y"
          class="monitor__grid"
          x1="0"
          :y1="y"
          :x2="CHART_WIDTH"
          :y2="y"
          vector-effect="non-scaling-stroke"
        />
        <polyline
          v-if="chart.memPoints"
          class="monitor__line monitor__line--mem"
          :points="chart.memPoints"
          fill="none"
          vector-effect="non-scaling-stroke"
        />
        <polyline
          v-if="chart.cpuPoints"
          class="monitor__line monitor__line--cpu"
          :points="chart.cpuPoints"
          fill="none"
          vector-effect="non-scaling-stroke"
        />
      </svg>
      <div v-if="axisTicks.length > 1" class="monitor__axis">
        <span
          v-for="tick in axisTicks"
          :key="`${tick.label}-${tick.percent}`"
          class="monitor__tick"
          :style="{ left: `${tick.percent}%` }"
        >
          {{ tick.label }}
        </span>
      </div>
      <div v-if="!latest" class="monitor__waiting">{{ $t('page.workbench.monitorWaiting') }}</div>
    </div>
  </div>
</template>

<style scoped>
.monitor {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.monitor__legend {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 16px;
  color: var(--el-text-color-regular);
  font-size: 13px;
}

.monitor__item {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.monitor__item b {
  color: var(--el-text-color-primary);
  font-weight: 600;
  font-variant-numeric: tabular-nums;
}

.monitor__dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}

.monitor__dot--cpu {
  background: #409eff;
}

.monitor__dot--mem {
  background: #67c23a;
}

.monitor__usage {
  margin-left: auto;
  color: var(--el-text-color-secondary);
  font-variant-numeric: tabular-nums;
}

.monitor__body {
  position: relative;
}

.monitor__chart {
  display: block;
  width: 100%;
  height: 160px;
  border-radius: 4px;
  background: var(--el-fill-color-lighter);
}

.monitor__grid {
  stroke: var(--el-border-color-lighter);
  stroke-width: 1;
  stroke-dasharray: 4 4;
}

.monitor__line {
  stroke-width: 2;
  stroke-linecap: round;
  stroke-linejoin: round;
}

.monitor__line--cpu {
  stroke: #409eff;
}

.monitor__line--mem {
  stroke: #67c23a;
}

/* X 轴时间刻度：按百分比与折线采样点对齐，两端的标签向内收避免溢出 */
.monitor__axis {
  position: relative;
  height: 16px;
  margin-top: 4px;
}

.monitor__tick {
  position: absolute;
  top: 0;
  transform: translateX(-50%);
  color: var(--el-text-color-secondary);
  font-size: 11px;
  line-height: 16px;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

.monitor__tick:first-child {
  transform: translateX(0);
}

.monitor__tick:last-child {
  transform: translateX(-100%);
}

.monitor__waiting {
  position: absolute;
  inset: 0 0 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--el-text-color-secondary);
  font-size: 13px;
}
</style>

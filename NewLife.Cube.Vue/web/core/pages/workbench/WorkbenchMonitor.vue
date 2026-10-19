<script setup lang="ts">
/**
 * 工作台性能监控卡
 *
 * 消费 `Admin/Index/MonitorData` 的累计快照，用轻量 SVG 绘制 CPU/内存两条曲线。
 * 不引入 ECharts，避免为了两张 0-100 曲线增加首屏运行时。
 */
import { computed } from 'vue';
import type { MonitorPoint } from '../../types/workbench';
import { buildSparkPoints, formatMemory, formatPercent } from './chart';

interface Props {
  /** 累计监控点，按时间升序 */
  points?: MonitorPoint[];
}

const props = withDefaults(defineProps<Props>(), { points: () => [] });

const CHART_WIDTH = 600;
const CHART_HEIGHT = 160;
const GRID_LINES = [25, 50, 75];

const latest = computed(() => props.points[props.points.length - 1] ?? null);
const cpuPoints = computed(() => buildSparkPoints(props.points.map((item) => item.cpu), CHART_WIDTH, CHART_HEIGHT));
const memoryPoints = computed(() =>
  buildSparkPoints(
    props.points.map((item) => item.memory),
    CHART_WIDTH,
    CHART_HEIGHT,
  ),
);
</script>

<template>
  <div class="wb-monitor" data-cy="workbench-monitor">
    <div class="wb-monitor-legend">
      <span class="wb-legend-item cpu">
        <i class="wb-dot"></i>
        CPU
        <b>{{ formatPercent(latest?.cpu ?? 0) }}</b>
      </span>
      <span class="wb-legend-item memory">
        <i class="wb-dot"></i>
        内存
        <b>{{ formatPercent(latest?.memory ?? 0) }}</b>
      </span>
      <span class="wb-monitor-memory">{{ formatMemory(latest?.memUsed, latest?.memTotal) }}</span>
    </div>

    <div v-if="points.length" class="wb-monitor-chart">
      <svg viewBox="0 0 600 160" preserveAspectRatio="none" role="img" aria-label="CPU 与内存使用率曲线">
        <line
          v-for="ratio in GRID_LINES"
          :key="ratio"
          class="wb-grid-line"
          x1="0"
          :y1="(CHART_HEIGHT * ratio) / 100"
          x2="600"
          :y2="(CHART_HEIGHT * ratio) / 100"
        />
        <polyline class="wb-line cpu" :points="cpuPoints" />
        <polyline class="wb-line memory" :points="memoryPoints" />
      </svg>
    </div>
    <el-empty v-else :image-size="48" description="等待监控数据…" />
  </div>
</template>

<style lang="scss" scoped>
.wb-monitor {
  min-height: 180px;
}

.wb-monitor-legend {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 16px;
  margin-bottom: 12px;
  color: var(--el-text-color-secondary);
}

.wb-legend-item {
  display: inline-flex;
  align-items: center;
  gap: 6px;

  b {
    color: var(--el-text-color-primary);
    font-variant-numeric: tabular-nums;
  }

  &.cpu .wb-dot {
    background: var(--el-color-primary);
  }

  &.memory .wb-dot {
    background: var(--el-color-success);
  }
}

.wb-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}

.wb-monitor-memory {
  margin-left: auto;
  font-variant-numeric: tabular-nums;
}

.wb-monitor-chart {
  height: 180px;
  border-radius: var(--el-border-radius-base);
  background: var(--el-fill-color-lighter);
  overflow: hidden;
}

.wb-monitor-chart svg {
  display: block;
  width: 100%;
  height: 100%;
}

.wb-grid-line {
  stroke: var(--el-border-color-lighter);
  stroke-width: 1;
  vector-effect: non-scaling-stroke;
}

.wb-line {
  fill: none;
  stroke-width: 2;
  stroke-linejoin: round;
  stroke-linecap: round;
  vector-effect: non-scaling-stroke;

  &.cpu {
    stroke: var(--el-color-primary);
  }

  &.memory {
    stroke: var(--el-color-success);
  }
}
</style>

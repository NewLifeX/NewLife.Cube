<script setup lang="ts">
/**
 * 指标列表（默认列表页的合并工具栏与独立指标行共用）
 *
 * 只负责把 `metrics` 数组或 `#metrics` 插槽渲染成「图标 + 名称 + 值 + 环比」，
 * 行高、右侧控件与边框由宿主决定。
 */
import type { CubeEngineContext } from '../../engine/types';
import type { CubeTableMetric } from './types';

const props = withDefaults(
  defineProps<{
    ctx: CubeEngineContext;
    metrics?: CubeTableMetric[];
  }>(),
  { metrics: () => [] },
);
</script>

<template>
  <div class="ctm-metrics">
    <slot name="metrics" :ctx="ctx" :metrics="props.metrics">
      <template v-for="(item, index) in props.metrics" :key="item.label">
        <div v-if="index > 0" class="ctm-divider"></div>
        <div class="ctm-item">
          <div v-if="item.icon" class="ctm-icon" :style="{ background: item.bg, color: item.color }">
            <el-icon :size="14"><component :is="item.icon" /></el-icon>
          </div>
          <span class="ctm-label">{{ item.label }}</span>
          <span class="ctm-value">{{ item.value }}</span>
          <span v-if="item.change" class="ctm-change" :class="item.changeType">{{ item.change }}</span>
        </div>
      </template>
    </slot>
  </div>
</template>

<style scoped>
.ctm-metrics {
  min-width: 0;
  display: flex;
  align-items: center;
}
.ctm-item {
  display: flex;
  align-items: center;
  white-space: nowrap;
}
.ctm-divider {
  width: 1px;
  height: 20px;
  margin: 0 18px;
  flex-shrink: 0;
  background: var(--el-border-color-lighter);
}
.ctm-icon {
  width: 28px;
  height: 28px;
  margin-right: 8px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
}
.ctm-label {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}
.ctm-value {
  margin-left: 6px;
  font-size: 15px;
  font-weight: 600;
  line-height: 20px;
  color: var(--el-text-color-primary);
}
.ctm-change {
  margin-left: 6px;
  font-size: 11px;
  line-height: 18px;
}
.ctm-change.up {
  color: var(--el-color-success);
}
.ctm-change.warning {
  color: var(--el-color-warning);
}
.ctm-change.down {
  color: var(--el-color-danger);
}
</style>

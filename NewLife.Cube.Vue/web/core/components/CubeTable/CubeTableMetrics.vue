<script setup lang="ts">
/**
 * CubeTable 指标行（表格上方那一行，对应 MES ListPage 的 `.table-toolbar`）
 *
 * 左侧是统计指标（`metrics` 数组或 `#metrics` 插槽整体替换），
 * 右侧依次是业务追加控件、内置「设置列」、刷新。
 * 整行没有任何内容时不渲染，避免表格上面多一条空带。
 */
import { computed, useSlots } from 'vue';
import { Refresh } from '@element-plus/icons-vue';
import type { CubeEngineContext } from '../../engine/types';
import type { CubeTableMetric } from './types';

const props = withDefaults(
  defineProps<{
    ctx: CubeEngineContext;
    /** 统计指标（左侧） */
    metrics?: CubeTableMetric[];
    /** 是否显示刷新 */
    showRefresh?: boolean;
    /** 是否显示「设置列」入口（内容由宿主经 `#column-setting` 提供） */
    showColumnSetting?: boolean;
  }>(),
  { metrics: () => [], showRefresh: true, showColumnSetting: false },
);

const slots = useSlots();
const metricList = computed(() => props.metrics ?? []);
const visible = computed(
  () =>
    Boolean(slots.metrics || slots.extra || slots['column-setting']) ||
    metricList.value.length > 0 ||
    props.showRefresh ||
    props.showColumnSetting,
);
</script>

<template>
  <div v-if="visible" class="ctm">
    <div class="ctm-metrics">
      <slot name="metrics" :ctx="ctx" :metrics="metricList">
        <template v-for="(item, index) in metricList" :key="item.label">
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

    <div class="ctm-controls">
      <slot name="extra" :ctx="ctx" />
      <slot name="column-setting" />
      <el-button v-if="showRefresh" :icon="Refresh" circle title="刷新" @click="ctx.refresh()" />
    </div>
  </div>
</template>

<style scoped>
.ctm {
  flex-shrink: 0;
  min-height: 56px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}
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
.ctm-controls {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}
</style>

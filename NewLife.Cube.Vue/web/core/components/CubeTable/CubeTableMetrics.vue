<script setup lang="ts">
/**
 * CubeTable 独立指标行（默认列表页已合并进 `CubeTableToolbar`）
 *
 * 单独取用时：左侧统计指标（`metrics` 数组或 `#metrics` 插槽整体替换），
 * 右侧依次是业务追加控件、内置「设置列」、刷新。
 * 整行没有任何内容时不渲染，避免表格上面多一条空带。
 */
import { computed, useSlots } from 'vue';
import { useI18n } from 'vue-i18n';
import { Refresh } from '@element-plus/icons-vue';
import CubeTableMetricList from './CubeTableMetricList.vue';
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
const { t } = useI18n();
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
    <CubeTableMetricList :ctx="ctx" :metrics="metricList">
      <template v-if="slots.metrics" #metrics="scope">
        <slot name="metrics" v-bind="scope" />
      </template>
    </CubeTableMetricList>

    <div class="ctm-controls">
      <slot name="extra" :ctx="ctx" />
      <slot name="column-setting" />
      <el-button v-if="showRefresh" :icon="Refresh" circle :title="t('grid.refresh')" @click="ctx.refresh()" />
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
.ctm-controls {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}
</style>

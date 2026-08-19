<template>
  <a-modal
    :visible="visible"
    title="配置固定图表（ECharts option）"
    width="680"
    :footer="false"
    @cancel="emit('update:visible', false)"
  >
    <div class="coe">
      <div class="coe-tip">
        以 JSON 编辑一张 ECharts option；运行时数据（dataset.source / series[].data）保存时自动剔除，随当前列表行实时填充。
      </div>
      <a-textarea
        :model-value="text"
        :auto-size="{ minRows: 8, maxRows: 14 }"
        placeholder='{ "xAxis": { "type": "category" }, "yAxis": { "type": "value" }, "series": [{ "type": "bar" }] }'
        class="coe-input"
        @update:model-value="onInput"
      />
      <div v-if="error" class="coe-error">
        <a-typography-text type="danger">{{ error }}</a-typography-text>
      </div>
      <div v-if="text.trim()" class="coe-preview">
        <div class="coe-preview-label">预览（当前列表行）</div>
        <div ref="previewRef" class="coe-preview-chart" />
      </div>
    </div>
    <template #footer>
      <a-space>
        <a-button @click="clear">清除</a-button>
        <a-button @click="emit('update:visible', false)">取消</a-button>
        <a-button type="primary" @click="save">保存</a-button>
      </a-space>
    </template>
  </a-modal>
</template>

<script setup lang="ts">
import { useChartOptionEditor } from './useChartOptionEditor';

defineOptions({ name: 'ChartOptionEditor' });

const props = defineProps<{
  visible: boolean;
  chartOption: unknown;
  rows: Record<string, unknown>[];
}>();

const emit = defineEmits<{
  'update:visible': [boolean];
  save: [option: unknown];
  clear: [];
}>();

const { text, error, previewRef, onTextChange, save, clear } = useChartOptionEditor(props, emit);

function onInput(v: string) {
  text.value = v;
  onTextChange();
}
</script>

<style scoped>
.coe-tip {
  font-size: var(--cube-font-size-meta);
  color: var(--color-text-3);
  margin-bottom: 8px;
}
.coe-input {
  font-family: var(--font-family);
}
.coe-error {
  margin-top: 8px;
}
.coe-preview {
  margin-top: 12px;
}
.coe-preview-label {
  font-size: var(--cube-font-size-meta);
  color: var(--color-text-3);
  margin-bottom: 4px;
}
.coe-preview-chart {
  height: 240px;
  border: 1px solid var(--color-border-2);
  border-radius: 4px;
}
</style>

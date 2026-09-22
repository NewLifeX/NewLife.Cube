<script setup lang="ts">
/**
 * 流程条件编辑：按钮显示人话摘要，点开复用列表页 FilterBuilder（发起条件 / XOR 分支）。
 */
import { computed, ref } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import type { ViewFilter } from '@/core/utils/viewProfile';
import FilterBuilderPopover from '@/views/crud/FilterBuilderPopover.vue';
import { emptyViewFilter } from '@/core/utils/viewProfile';
import { parseViewFilterJson, viewFilterSummary } from './wfFilterText';

const props = defineProps<{
  modelValue?: unknown;
  fields: FieldMeta[];
  disabled?: boolean;
  emptyText?: string;
}>();

const emit = defineEmits<{ (e: 'update:modelValue', v: ViewFilter): void }>();

const visible = ref(false);
const filter = computed(() => parseViewFilterJson(props.modelValue));
const summary = computed(() => {
  const s = viewFilterSummary(filter.value, (name) => props.fields.find((f) => f.name === name)?.displayName || name);
  return s === '不限制' ? props.emptyText || '不限制，点击设置' : s;
});

function onApply(v: ViewFilter) {
  emit('update:modelValue', v ?? emptyViewFilter());
  visible.value = false;
}
</script>

<template>
  <FilterBuilderPopover
    :visible="visible"
    :fields="fields"
    :model-value="filter"
    :can-save="false"
    :show-save-view="false"
    @update:visible="(v: boolean) => (visible = v)"
    @apply="onApply"
  >
    <a-button size="mini" :disabled="disabled" @click="visible = true">{{ summary }}</a-button>
  </FilterBuilderPopover>
</template>

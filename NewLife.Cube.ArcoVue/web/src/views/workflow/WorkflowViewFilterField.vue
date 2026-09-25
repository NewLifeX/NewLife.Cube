<script setup lang="ts">
/**
 * 流程条件编辑：按钮显示人话摘要，点开复用列表页 FilterBuilder（发起条件 / XOR 分支）。
 * inline 模式（工作流节点属性抽屉）：不弹层，直接在容器内铺开条件构建面板。
 */
import { computed, ref } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import type { ViewFilter } from '@/core/utils/viewProfile';
import FilterBuilderPopover from '@/views/crud/FilterBuilderPopover.vue';
import FilterBuilderPanel from '@/views/crud/FilterBuilderPanel.vue';
import { emptyViewFilter } from '@/core/utils/viewProfile';
import { filterValueLabelResolver, parseViewFilterJson, viewFilterSummary } from './wfFilterText';

const props = defineProps<{
  modelValue?: unknown;
  fields: FieldMeta[];
  disabled?: boolean;
  emptyText?: string;
  /** 内嵌模式：不显示摘要按钮与弹层，直接把条件构建面板铺在容器内（节点属性抽屉用） */
  inline?: boolean;
}>();

const emit = defineEmits<{ (e: 'update:modelValue', v: ViewFilter): void }>();

const visible = ref(false);
const filter = computed(() => parseViewFilterJson(props.modelValue));
const summary = computed(() => {
  const s = viewFilterSummary(
    filter.value,
    (name) => props.fields.find((f) => f.name === name)?.displayName || name,
    filterValueLabelResolver(props.fields),
  );
  return s === '不限制' ? props.emptyText || '不限制，点击设置' : s;
});

function onApply(v: ViewFilter) {
  emit('update:modelValue', v ?? emptyViewFilter());
  visible.value = false;
}
</script>

<template>
  <FilterBuilderPanel
    v-if="inline"
    inline
    :fields="fields"
    :model-value="filter"
    :can-save="false"
    :show-save-view="false"
    @apply="onApply"
  />
  <FilterBuilderPopover
    v-else
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

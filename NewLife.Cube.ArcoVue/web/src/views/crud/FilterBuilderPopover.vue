<template>
  <a-popover
    :popup-visible="visible"
    position="bottom"
    trigger="click"
    class="filter-builder-popover"
    @popup-visible-change="(v: boolean) => emit('update:visible', v)"
  >
    <template #content>
      <FilterBuilderPanel
        :fields="fields"
        :model-value="modelValue"
        :can-save="canSave"
        :host-fields="hostFields"
        :show-save-view="showSaveView"
        :visible="visible"
        @apply="(v: ViewFilter) => emit('apply', v)"
        @save="(v: ViewFilter) => emit('save', v)"
        @update:visible="(v: boolean) => emit('update:visible', v)"
      />
    </template>

    <template #default>
      <slot />
    </template>
  </a-popover>
</template>

<script setup lang="ts">
import type { FieldMeta } from '@/core/types/field';
import type { ViewFilter } from '@/core/utils/viewProfile';
import FilterBuilderPanel from './FilterBuilderPanel.vue';

defineProps<{
  /** 弹层是否可见（由父级管理，与分组弹层互斥） */
  visible: boolean;
  /** 筛选候选字段（当前视图可见字段） */
  fields: FieldMeta[];
  /** 当前筛选方案（父级 viewProfile.getFilter） */
  modelValue: ViewFilter;
  /** 是否有命名视图可保存 */
  canSave: boolean;
  /** 宿主字段候选（OSC-260903e2a4）：非空时条件行值来源可切「值 / 宿主」；缺省不显示 */
  hostFields?: FieldMeta[];
  /** 是否显示「保存条件到此视图」按钮（OSC-260903e2a4）；部件配置传 false，默认 true */
  showSaveView?: boolean;
}>();

const emit = defineEmits<{
  'update:visible': [boolean];
  apply: [ViewFilter];
  save: [ViewFilter];
}>();

</script>

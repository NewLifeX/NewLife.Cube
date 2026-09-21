<template>
  <a-form-item :label="label">
    <div class="wd-filter-wrap">
      <FilterBuilderPopover
        :visible="visible"
        :fields="fields"
        :model-value="modelValue"
        :can-save="false"
        :host-fields="hostFields"
        :show-save-view="false"
        @update:visible="(v: boolean) => emit('update:visible', v)"
        @apply="(f: ViewFilter) => emit('apply', f)"
      >
        <a-button size="mini">
          <icon-park type="filter" />
          {{ count ? `${label}（${count}）` : label }}
        </a-button>
      </FilterBuilderPopover>
      <a-button v-if="count" size="mini" status="danger" @click="emit('clear')">清除</a-button>
    </div>
    <template #extra>
      <span class="wd-hint">{{ hint }}</span>
    </template>
  </a-form-item>
</template>

<script setup lang="ts">
/**
 * 部件条件编辑器表单项（OSC-260920）：
 * 迷你图表标签为「数据范围」并上移到图表模板下，其余实体部件仍为「查询条件」；
 * 底层同为 query.extraFilter，故抽成同一组件避免两处标记漂移。
 */
import type { FieldMeta } from '@/core/types/field';
import type { ViewFilter } from '@/core/utils/viewProfile';
import FilterBuilderPopover from '@/views/crud/FilterBuilderPopover.vue';

defineProps<{
  /** 标签文案：迷你图表=数据范围；其它实体部件=查询条件 */
  label: string;
  /** 已配置条件数（0 时不显示清除按钮） */
  count: number;
  /** 条件编辑器当前方案 */
  modelValue: ViewFilter;
  /** 源实体字段候选 */
  fields: FieldMeta[];
  /** 宿主字段候选（$host 引用；工作台为空数组） */
  hostFields: FieldMeta[];
  /** 弹层显隐（由父级持有，保持单一状态源） */
  visible: boolean;
  /** 底部提示文案 */
  hint: string;
}>();

const emit = defineEmits<{
  'update:visible': [boolean];
  apply: [ViewFilter];
  clear: [];
}>();
</script>

<style scoped>
.wd-filter-wrap {
  display: flex;
  align-items: center;
  gap: 8px;
}
.wd-hint {
  font-size: 12px;
  color: var(--color-text-3);
}
</style>

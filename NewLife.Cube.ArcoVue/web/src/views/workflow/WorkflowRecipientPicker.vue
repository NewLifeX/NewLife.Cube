<script setup lang="ts">
/**
 * 审批接收人选择（OSC-26090347f1 T8c）：用户/角色/部门 单选/多选 + 远端搜索。
 * 业务逻辑在 useRecipientPicker.ts（SFC 构薄门禁），UI 对齐自动化动作卡片。
 */
import type { RecipientKind } from './recipient';
import { RECIPIENT_KIND_LABEL, useRecipientPicker } from './useRecipientPicker';

const props = withDefaults(
  defineProps<{
    /** 当前类别 */
    kind?: RecipientKind;
    /** 已选 Id */
    modelValue?: number[];
    /** 多选（转办固定单选） */
    multiple?: boolean;
    placeholder?: string;
  }>(),
  { kind: 'users', modelValue: () => [], multiple: true, placeholder: '' },
);

const emit = defineEmits<{
  (e: 'update:kind', kind: RecipientKind): void;
  (e: 'update:modelValue', ids: number[]): void;
}>();

const { kind, selectValue, options, loading, doSearch, onKindChange, onUpdate } =
  useRecipientPicker(props, emit);

defineExpose({ doSearch });
</script>

<template>
  <div class="wf-recipient">
    <a-radio-group
      :model-value="kind"
      type="button"
      size="small"
      class="wf-recipient__kind"
      @change="onKindChange"
    >
      <a-radio v-for="(label, k) in RECIPIENT_KIND_LABEL" :key="k" :value="k">
        {{ label }}
      </a-radio>
    </a-radio-group>
    <a-select
      :key="kind + (multiple ? '-m' : '-s')"
      :model-value="selectValue"
      :multiple="multiple"
      allow-clear
      allow-search
      :filter-option="false"
      :loading="loading"
      :placeholder="placeholder || `选择${RECIPIENT_KIND_LABEL[kind]}`"
      size="small"
      @search="doSearch"
      @focus="() => doSearch('')"
      @popup-visible-change="(v: boolean) => v && doSearch('')"
      @update:model-value="onUpdate"
    >
      <a-option
        v-for="o in options"
        :key="o.id"
        :value="o.id"
        :label="o.displayName || o.name || String(o.id)"
      >
        {{ o.displayName || o.name || o.id }}
      </a-option>
    </a-select>
  </div>
</template>

<style scoped>
.wf-recipient {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.wf-recipient__kind {
  width: fit-content;
}
</style>

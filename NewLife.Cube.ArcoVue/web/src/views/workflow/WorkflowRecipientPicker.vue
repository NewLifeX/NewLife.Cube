<script setup lang="ts">
/**
 * 审批接收人选择（OSC-26090347f1 T8c）：用户/角色/部门 单选/多选 + 远端搜索。
 * 业务逻辑在 useRecipientPicker.ts（SFC 构薄门禁），UI 对齐自动化动作卡片。
 */
import type { RecipientKind } from './recipient';
import { RECIPIENT_KIND_LABEL, useRecipientPicker } from './useRecipientPicker';
import { computed } from 'vue';

const props = withDefaults(
  defineProps<{
    /** 当前类别 */
    kind?: RecipientKind;
    /** 已选 Id */
    modelValue?: number[];
    /** 与 Id 对齐的显示名 */
    labels?: string[];
    /** 多选（转办、指定某人固定单选） */
    multiple?: boolean;
    /** 为 false 时不画类别按钮。设计器已用六种下拉选过类别 */
    showKind?: boolean;
    placeholder?: string;
    /** 下拉挂载点（放进待办弹层内，避免点选项关掉弹层） */
    popupContainer?: HTMLElement | string;
  }>(),
  { kind: 'users', modelValue: () => [], labels: () => [], multiple: true, showKind: true, placeholder: '', popupContainer: undefined },
);

const emit = defineEmits<{
  (e: 'update:kind', kind: RecipientKind): void;
  (e: 'update:modelValue', ids: number[]): void;
  (e: 'update:labels', labels: string[]): void;
}>();

const { kind, selectValue, options, loading, doSearch, onKindChange, onUpdate } =
  useRecipientPicker(props, emit);

/**
 * 下拉选项合并已选：远端 options 里没有的已选值，按 labels（与 modelValue 对齐）补占位选项，
 * 使选中标签显示友好名而非 id（节点属性 / 转办 / 表单等所有宿主共用）。
 */
const mergedOptions = computed(() => {
  const list = options.value.map((o) => ({ id: o.id, label: o.displayName || o.name || String(o.id) }));
  const ids = props.modelValue ?? [];
  const labels = props.labels ?? [];
  ids.forEach((id, i) => {
    if (!list.some((o) => o.id === id)) list.push({ id, label: labels[i] || String(id) });
  });
  return list;
});

defineExpose({ doSearch });
</script>

<template>
  <div class="wf-recipient">
    <a-radio-group
      v-if="showKind"
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
      style="width: 100%"
      @search="doSearch"
      @focus="() => doSearch('')"
      @popup-visible-change="(v: boolean) => v && doSearch('')"
      @update:model-value="onUpdate"
      :popup-container="popupContainer"
    >
      <a-option v-for="o in mergedOptions" :key="o.id" :value="o.id" :label="o.label">
        {{ o.label }}
      </a-option>
    </a-select>
  </div>
</template>

<style scoped>
.wf-recipient {
  display: flex;
  flex-direction: column;
  gap: 8px;
  width: 100%;
}
.wf-recipient__kind {
  width: fit-content;
}
</style>

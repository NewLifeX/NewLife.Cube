<script setup lang="ts">
/**
 * 设计器六种选人（OSC-260922201a design §3.1/§4.1）：指定成员/角色/部门（搜索），
 * 部门负责人/提交人自选/表单人员（无 Id，带 scope/multiple/field/fieldAs）。
 * 薄 .vue：逻辑在 useAssigneePicker；用户/角色/部门搜索复用 WorkflowRecipientPicker。
 */
import type { WfToKind } from '@/core/types/workflow';
import WorkflowRecipientPicker from './WorkflowRecipientPicker.vue';
import { ASSIGNEE_KIND_ORDER, STARTER_SCOPE_OPTIONS, useAssigneePicker } from './useAssigneePicker';

const props = withDefaults(
  defineProps<{
    kind?: WfToKind;
    modelValue?: number[];
    labels?: string[];
    scope?: string;
    starterMultiple?: boolean;
    multiSelect?: boolean;
    field?: string;
    fieldAs?: string;
    fields?: { name: string; label: string }[];
  }>(),
  {
    kind: 'users',
    modelValue: () => [],
    labels: () => [],
    scope: 'all',
    starterMultiple: false,
    multiSelect: true,
    field: '',
    fieldAs: 'user',
    fields: () => [],
  },
);

const emit = defineEmits<{
  (e: 'update:kind', kind: WfToKind): void;
  (e: 'update:modelValue', ids: number[]): void;
  (e: 'update:labels', labels: string[]): void;
  (e: 'update:scope', scope: string): void;
  (e: 'update:starterMultiple', multiple: boolean): void;
  (e: 'update:field', field: string): void;
  (e: 'update:fieldAs', fieldAs: string): void;
}>();

const {
  currentKind,
  isSearchKind,
  kindLabel,
  kindHint,
  fieldOptions,
  searchKind,
  onKindChange,
  onIds,
  onLabels,
  onScope,
  onStarterMultiple,
  onField,
  onFieldAs,
} = useAssigneePicker(props, emit);
</script>

<template>
  <div class="wf-assignee">
    <a-select
      :model-value="currentKind"
      size="small"
      class="wf-assignee__select"
      @change="onKindChange"
    >
      <a-option v-for="k in ASSIGNEE_KIND_ORDER" :key="k" :value="k">
        {{ kindLabel(k) }}
      </a-option>
    </a-select>

    <WorkflowRecipientPicker
      v-if="isSearchKind"
      :kind="searchKind"
      :show-kind="false"
      :model-value="modelValue"
      :labels="labels"
      :multiple="multiSelect === true"
      @update:model-value="onIds"
      @update:labels="onLabels"
    />

    <div v-if="kind === 'starterPick'" class="wf-assignee__row">
      <a-select
        :model-value="scope ?? 'all'"
        :options="STARTER_SCOPE_OPTIONS"
        size="small"
        style="width: 130px"
        @update:model-value="onScope"
      />
      <a-checkbox :model-value="starterMultiple === true" @change="onStarterMultiple">允许多人</a-checkbox>
    </div>

    <div v-else-if="kind === 'field'" class="wf-assignee__row">
      <a-select
        :model-value="field ?? ''"
        :options="fieldOptions"
        size="small"
        allow-search
        placeholder="选择表单字段"
        style="width: 180px"
        @update:model-value="onField"
      />
      <a-radio-group
        :model-value="fieldAs ?? 'user'"
        type="button"
        size="mini"
        @change="onFieldAs"
      >
        <a-radio value="user">用户字段</a-radio>
        <a-radio value="manager">部门字段取负责人</a-radio>
      </a-radio-group>
    </div>

    <a-typography-text
      v-if="kind !== 'starterPick'"
      class="wf-assignee__hint"
      type="secondary"
      style="font-size: 12px"
    >
      {{ kindHint }}
    </a-typography-text>
  </div>
</template>

<style scoped>
.wf-assignee {
  display: flex;
  flex-direction: column;
  gap: 8px;
  width: 100%;
}
.wf-assignee__select,
.wf-assignee :deep(.arco-select),
.wf-assignee :deep(.wf-recipient),
.wf-assignee :deep(.wf-recipient .arco-select) {
  width: 100%;
}
.wf-assignee__row {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}
.wf-assignee__hint {
  line-height: 1.5;
}
</style>

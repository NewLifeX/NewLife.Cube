<template>
  <div class="lov-select">
    <a-select
      v-if="mode === 'enum'"
      :model-value="modelValue"
      :placeholder="placeholder"
      :disabled="disabled"
      :multiple="multiple"
      allow-clear
      style="width: 100%"
      @update:model-value="onUpdate"
    >
      <a-option v-for="opt in options" :key="String(opt.value)" :value="opt.value" :label="opt.label">
        {{ opt.label }}
      </a-option>
    </a-select>

    <!-- LIST 单选：下拉直接展示首页数据（角色列表等），“更多”按钮打开高级表格；
         输入关键字触发远程搜索（OSC-0015 5.6） -->
    <a-input-group v-else-if="!multiple" class="lov-select--single">
      <a-select
        :model-value="singleSelectValue"
        :placeholder="placeholder"
        :disabled="disabled"
        :loading="loadingOptions"
        allow-clear
        filterable
        :filter-option="false"
        style="flex: 1; min-width: 0"
        @search="onRemoteSearch"
        @dropdown-visible-change="onDropdownVisible"
        @update:model-value="onInlineSelect"
      >
        <a-option
          v-for="opt in inlineOptions"
          :key="String(opt.value)"
          :value="opt.value"
          :label="opt.label"
        >
          {{ opt.label }}
        </a-option>
      </a-select>
      <a-button :disabled="disabled" @click="tableVisible = true">更多</a-button>
    </a-input-group>

    <!-- LIST 多选：已选标签 + 选择/清除（弹窗内勾选确认） -->
    <div v-else class="lov-select--multi">
      <a-space v-if="selectedLabels.length" wrap :size="4">
        <a-tag
          v-for="(lb, i) in selectedLabels"
          :key="`${lb}-${i}`"
          closable
          @close="onRemoveLabel(i)"
        >
          {{ lb }}
        </a-tag>
      </a-space>
      <a-button :disabled="disabled" @click="tableVisible = true">选择</a-button>
      <a-button v-if="hasValue" :disabled="disabled" @click="onClear">清除</a-button>
    </div>

    <LovSelectTable
      v-model:visible="tableVisible"
      :lov-code="code"
      :meta="listMeta"
      :multiple="multiple"
      :model-value="modelValue"
      @confirm="onConfirm"
    />
  </div>
</template>

<script setup lang="ts">
import LovSelectTable from './LovSelectTable.vue';
import { useLovSelect } from './useLovSelect';

const props = withDefaults(
  defineProps<{
    code: string;
    modelValue?: string | number | Array<string | number> | null;
    placeholder?: string;
    disabled?: boolean;
    multiple?: boolean;
  }>(),
  { placeholder: '请选择', disabled: false, multiple: false },
);

const emit = defineEmits<{
  'update:modelValue': [value: string | number | Array<string | number> | undefined];
}>();

const {
  mode,
  options,
  inlineOptions,
  loadingOptions,
  listMeta,
  tableVisible,
  hasValue,
  singleSelectValue,
  selectedLabels,
  onRemoteSearch,
  onDropdownVisible,
  onUpdate,
  onInlineSelect,
  onConfirm,
  onClear,
  onRemoveLabel,
} = useLovSelect(props, emit);
</script>

<style scoped>
.lov-select--multi {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  width: 100%;
}
.lov-select--multi .arco-space {
  flex: 1;
  min-width: 0;
}
</style>

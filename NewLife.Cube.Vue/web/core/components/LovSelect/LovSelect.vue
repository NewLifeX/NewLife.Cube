<template>
  <div class="lov-select">
    <!-- 元数据加载中 -->
    <el-select
      v-if="loading && !resolvedType"
      disabled
      loading
      placeholder="加载中..."
      style="width: 100%"
    />

    <!-- 列表型 ↓ 触发按钮，弹窗交给 LovSelectTable -->
    <template v-if="resolvedType === 'LIST'">
      <el-input
        :model-value="displayText"
        :placeholder="placeholder"
        :disabled="disabled"
        readonly
        style="width: 100%"
        @click="openDialog"
      >
        <template #append>
          <el-button :disabled="disabled" @click="openDialog">
            <el-icon><Search /></el-icon>
          </el-button>
        </template>
      </el-input>

      <LovSelectTable
        v-model:dialog-visible="dialogVisible"
        :lov-code="code"
        :lov-meta="listMeta"
        :inline-enums="metaInlineEnums"
        :translate-cache="translateCache"
        :multiple="multiple"
        :model-value="modelValue"
        @select="handleTableSelect"
        @confirm="handleTableMultiConfirm"
      />
    </template>

    <!-- 枚举型 · 多选 -->
    <el-select
      v-else-if="resolvedType === 'ENUM' && multiple"
      v-model="selectedValues"
      multiple
      collapse-tags
      collapse-tags-tooltip
      :placeholder="placeholder"
      :clearable="clearable"
      :disabled="disabled"
      :loading="loading"
      style="width: 100%"
      @change="handleMultiChange"
    >
      <el-option v-for="opt in options" :key="opt.value" :label="opt.label" :value="opt.value" />
    </el-select>

    <!-- 枚举型 · 单选 -->
    <el-select
      v-else
      v-model="selectedValue"
      :placeholder="placeholder"
      :clearable="clearable"
      :disabled="disabled"
      :loading="loading"
      style="width: 100%"
      @change="handleEnumChange"
    >
      <el-option v-for="opt in options" :key="opt.value" :label="opt.label" :value="opt.value" />
    </el-select>
  </div>
</template>

<script setup lang="ts">
// 显式声明组件名，确保 Vue DevTools 显示为 LovSelect（不依赖目录/文件名推断）
defineOptions({ name: 'LovSelect' });
import { toRef, onMounted } from 'vue';
import { Search } from '@element-plus/icons-vue';
import LovSelectTable from '../LovSelectTable/index.vue';
import { useLovSelect } from './useLovSelect';

const props = withDefaults(
  defineProps<{
    code: string;
    type?: string;
    modelValue?: string | number | string[];
    placeholder?: string;
    clearable?: boolean;
    disabled?: boolean;
    size?: 'large' | 'default' | 'small';
    /** 是否为多选（multipleSelect 场景），emit string[] */
    multiple?: boolean;
  }>(),
  {
    placeholder: '请选择',
    clearable: true,
    disabled: false,
    multiple: false,
  },
);

const emit = defineEmits<{
  (e: 'update:modelValue', value: string | number | string[] | undefined): void;
  (e: 'change', value: string | number | string[] | undefined): void;
}>();

// ── 逻辑/事件全部委托给 useLovSelect；本组件只做「绑定模板 + emit 转发」的薄壳 ──
const {
  loading,
  resolvedType,
  options,
  selectedValue,
  selectedValues,
  dialogVisible,
  displayText,
  listMeta,
  metaInlineEnums,
  translateCache,
  loadMeta,
  onEnumChange,
  onEnumMultiChange,
  onTableSelect,
  onTableMultiConfirm,
  openDialog,
} = useLovSelect({
  code: toRef(props, 'code'),
  modelValue: toRef(props, 'modelValue'),
  multiple: toRef(props, 'multiple'),
});

// hook 不含生命周期，组件在挂载后显式加载元数据
onMounted(() => loadMeta());

function emitChange(value: string | number | string[] | undefined) {
  emit('update:modelValue', value);
  emit('change', value);
}

function handleEnumChange(val: string | number | undefined) {
  emitChange(onEnumChange(val));
}

function handleMultiChange(val: string[]) {
  emitChange(onEnumMultiChange(val));
}

function handleTableSelect(row: Record<string, unknown>) {
  emitChange(onTableSelect(row));
}

function handleTableMultiConfirm(vals: string[]) {
  emitChange(onTableMultiConfirm(vals));
}
</script>

<style scoped>
.lov-select {
  width: 100%;
}
.lov-select :deep(.el-input-group__append) {
  cursor: pointer;
}
</style>

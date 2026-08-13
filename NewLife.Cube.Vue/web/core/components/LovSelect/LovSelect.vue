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

    <!-- 列表型（单选/多选统一）：el-select + suffix-icon 搜索入口，点击打开弹窗 -->
    <el-select
      ref="listSelectRef"
      v-else-if="resolvedType === 'LIST'"
      :model-value="listSelectValue"
      :multiple="multiple"
      :collapse-tags="multiple"
      :collapse-tags-tooltip="multiple"
      :max-collapse-tags="3"
      :placeholder="placeholder"
      :clearable="clearable"
      :disabled="disabled"
      :loading="loading"
      :suffix-icon="Search"
      style="width: 100%"
      @visible-change="handleListVisibleChange"
      @remove-tag="handleListMultiRemove"
      @clear="handleListClear"
    >
      <el-option v-for="tag in listTags" :key="tag.value" :label="tag.label" :value="tag.value" />
    </el-select>

    <!-- 弹窗（单选/多选共用，单双合并一个组件）
         注意：:model-value 传的是「内部同步值 listSelectValue」，而非外部原始 modelValue。
         外部 modelValue 由 useLovSelect 的 watch 同步进 selectedValue/selectedValues（见 syncFromModelValue），
         listSelectValue 由这两个内部变量派生，从而 LovSelectTable 只读父组件已同步好的内部状态，
         不直接消费外部传入值（避免回流/回显错位）。 -->
    <LovSelectTable
      v-if="resolvedType === 'LIST'"
      v-model:dialog-visible="dialogVisible"
      :lov-code="code"
      :lov-meta="listMeta"
      :inline-enums="metaInlineEnums"
      :translate-cache="translateCache"
      :multiple="multiple"
      :model-value="listSelectValue"
      @select="handleTableSelect"
      @confirm="handleTableMultiConfirm"
    />

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
import { toRef, computed, onMounted, ref, nextTick } from 'vue';
import { Search } from '@element-plus/icons-vue';
import LovSelectTable from '../LovSelectTable/index.vue';
import { useLovSelect } from './useLovSelect';
import { getSelectedLabel } from '@newlifex/cube-vue/core/components/LovSelect/lovStore';

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

/** LIST el-select 实例（用于拦截下拉展开） */
const listSelectRef = ref<InstanceType<(typeof import('element-plus'))['ElSelect']> | null>(null);

/** LIST 单选时用 selectedValue，多选时用 selectedValues */
const listSelectValue = computed(() =>
  props.multiple ? selectedValues.value : selectedValue.value,
);

/** LIST 标签列表（从 lovStore 解析 label，单选/多选共用） */
const listTags = computed(() => {
  // 响应式依赖：LIST meta 加载完成后重算，确保下方 translateCache 已填充 inlineEnums
  void listMeta.value;
  const code = props.code;
  const fill = (v: string | number | undefined): string => {
    if (v == null || v === '') return '';
    let label = getSelectedLabel(code, v);
    // 兜底：lovStore labelCache 未命中（如关闭态外部流入、未打开弹窗拉取列表），
    // 回退 meta 随附的 inlineEnums（applyMeta 已写入 translateCache），避免回显原始数字 id
    const key = `${code}:${v}`;
    if (label === String(v) && translateCache.has(key)) {
      label = translateCache.get(key)!;
    }
    return label;
  };
  if (props.multiple) {
    return selectedValues.value.map((v) => ({ value: v, label: fill(v) }));
  }
  const v = selectedValue.value;
  return v != null && v !== '' ? [{ value: v, label: fill(v) }] : [];
});

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

/** LIST 多选：删除单个标签 */
function handleListMultiRemove(removedValue: unknown) {
  const newVal = selectedValues.value.filter((v) => v !== removedValue);
  selectedValues.value = newVal;
  emitChange(newVal);
}

/** LIST 清空所有 */
function handleListClear() {
  if (props.multiple) {
    selectedValues.value = [];
    emitChange([]);
  } else {
    selectedValue.value = undefined;
    emitChange(undefined);
  }
}

/** LIST 拦截下拉展开，改为打开弹窗选择 */
function handleListVisibleChange(visible: boolean) {
  if (visible) {
    const selectInstance = listSelectRef.value as unknown as { blur?: () => void } | null;
    nextTick(() => selectInstance?.blur?.());
    openDialog();
  }
}
</script>

<style scoped>
.lov-select {
  width: 100%;
}
/* LIST：suffix 搜索图标可点击 */
.lov-select :deep(.el-select__suffix) {
  cursor: pointer;
}
.lov-select :deep(.el-select__suffix .el-icon) {
  width: 14px;
  height: 14px;
  color: var(--el-text-color-secondary);
}
</style>

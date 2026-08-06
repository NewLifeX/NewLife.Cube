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
import { computed, reactive, ref, watch, onBeforeUnmount } from 'vue';
import {
  fetchBatchLabel,
  fetchLovListData,
  fetchLovMeta,
  resolveLovType,
} from '@/core/utils/lov-api';
import type { LovEnumOption, LovListMeta } from '@/core/types/lov';
import LovSelectTable from './LovSelectTable.vue';

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

const mode = ref<'enum' | 'list'>('enum');
const options = ref<LovEnumOption[]>([]);
const inlineOptions = ref<LovEnumOption[]>([]);
const loadingOptions = ref(false);
const listMeta = ref<LovListMeta | null>(null);
const tableVisible = ref(false);
/** value → label 缓存；LIST 历史值回显与已选标签展示共用 */
const labelCache = reactive<Record<string, string>>({});

const values = computed<(string | number)[]>(() => {
  if (props.modelValue == null || props.modelValue === '') return [];
  return Array.isArray(props.modelValue) ? props.modelValue : [props.modelValue];
});

const hasValue = computed(() => values.value.length > 0);

/** LIST 单选下拉值（字符串键与 option 对齐；提交时由 normalizeSubmitValue 还原数值） */
const singleSelectValue = computed(() => {
  if (!values.value.length) return undefined;
  return String(values.value[0]);
});

const selectedLabels = computed(() =>
  values.value.map((v) => labelCache[String(v)] ?? String(v)),
);

async function loadMeta() {
  if (!props.code) return;
  const inferred = resolveLovType(props.code);
  try {
    const meta = await fetchLovMeta(props.code);
    const item = meta.meta?.find((m) => m.lovCode === props.code) ?? meta.meta?.[0];
    if (item?.type === 'LIST' || inferred === 'LIST') {
      mode.value = 'list';
      listMeta.value = item?.type === 'LIST' ? (item as LovListMeta) : null;
      // 单选时预加载首页数据，直接以下拉形式展示角色等列表
      if (!props.multiple) await loadInlineOptions(item as LovListMeta | null);
    } else {
      mode.value = 'enum';
      listMeta.value = null;
      const inline = meta.inlineEnums?.[props.code];
      options.value = item?.type === 'ENUM' ? item.options : (inline ?? []);
    }
  } catch {
    mode.value = inferred === 'LIST' ? 'list' : 'enum';
    listMeta.value = null;
  }
  // 历史值回显：即使 Meta 失败也尝试反查标签
  ensureLabels(values.value);
}

/** 按 Meta 的 valueField/labelField 映射 ListData 行为下拉选项；keyword 非空时携带 q 远程搜索（OSC-0015 5.6） */
async function loadInlineOptions(meta: LovListMeta | null, keyword = '') {
  const seq = ++loadSeq;
  const valueField = (meta?.valueField || 'id').trim();
  const labelField = (meta?.labelField || 'name').trim();
  loadingOptions.value = true;
  const params: Record<string, unknown> = {};
  if (keyword) params.q = keyword;
  try {
    const res = await fetchLovListData({
      lovCode: props.code,
      params,
      pageSize: 200,
      pageNum: 1,
    });
    if (seq !== loadSeq) return; // 过期响应丢弃
    const rows = Array.isArray(res?.data) ? res.data : [];
    inlineOptions.value = rows
      .map((r) => {
        const row = r as Record<string, unknown>;
        const val = row[valueField] ?? row[valueField.charAt(0).toLowerCase() + valueField.slice(1)];
        const lbl = row[labelField] ?? row[labelField.charAt(0).toLowerCase() + labelField.slice(1)];
        if (val == null) return null;
        const value = String(val);
        const label = String(lbl ?? val);
        labelCache[value] = label;
        return { value, label } as LovEnumOption;
      })
      .filter((x): x is LovEnumOption => x != null);
  } catch {
    if (seq !== loadSeq) return;
    inlineOptions.value = [];
  } finally {
    if (seq === loadSeq) loadingOptions.value = false;
  }
}

/** 远程搜索防抖计时器（OSC-0015 5.6：300ms） */
let searchTimer: ReturnType<typeof setTimeout> | null = null;
/** 加载序号：防抖/下拉打开并发请求时丢弃过期响应，避免慢响应覆盖新结果 */
let loadSeq = 0;

/** 下拉输入关键字 → 防抖 300ms 后携带 q 远程搜索 */
function onRemoteSearch(keyword: string) {
  if (searchTimer) clearTimeout(searchTimer);
  searchTimer = setTimeout(() => {
    searchTimer = null;
    void loadInlineOptions(listMeta.value, keyword.trim());
  }, 300);
}

/** 下拉打开：加载首页数据（输入框清空态；搜索由 @search 独立驱动） */
function onDropdownVisible(open: boolean) {
  if (!open) return;
  void loadInlineOptions(listMeta.value, '');
}

/** 对缺失标签的 value 调用 BatchLabel 权威反查（LIST 值集） */
async function ensureLabels(vals: (string | number)[]) {
  const missing = vals
    .map(String)
    .filter((v) => v && labelCache[v] == null);
  if (!missing.length) return;
  try {
    const map = await fetchBatchLabel({ lovCode: props.code, values: missing });
    for (const [k, v] of Object.entries(map)) {
      if (v != null) labelCache[k] = v;
    }
  } catch {
    /* 保留原始值降级显示 */
  }
}

function onUpdate(v: unknown) {
  emit('update:modelValue', v as string | number | Array<string | number> | undefined);
}

/** LIST 单选下拉选择：emit 字符串值；提交时由 normalizeSubmitValue 还原数值型字段 */
function onInlineSelect(v: unknown) {
  if (v == null || v === '') {
    emit('update:modelValue', undefined);
    return;
  }
  emit('update:modelValue', String(v));
}

function onConfirm(vals: unknown[]) {
  if (props.multiple) {
    emit('update:modelValue', vals as (string | number)[]);
  } else {
    emit('update:modelValue', (vals[0] as string | number) ?? undefined);
  }
  ensureLabels(vals as (string | number)[]);
  // 表格确认后回填 inline 选项，保证下拉也能展示新选值
  if (!props.multiple && vals.length) {
    const v = vals[0];
    const key = String(v);
    if (!inlineOptions.value.some((o) => o.value === key)) {
      inlineOptions.value = [
        { value: key, label: labelCache[key] ?? key },
        ...inlineOptions.value,
      ];
    }
  }
  tableVisible.value = false;
}

function onClear() {
  emit('update:modelValue', props.multiple ? [] : undefined);
}

function onRemoveLabel(index: number) {
  const next = [...values.value];
  next.splice(index, 1);
  emit('update:modelValue', next);
}

watch(() => props.code, loadMeta, { immediate: true });
watch(
  () => props.modelValue,
  (v) => {
    if (v == null || v === '') return;
    ensureLabels(Array.isArray(v) ? v : [v]);
  },
  { deep: true },
);

onBeforeUnmount(() => {
  // 卸载清理防抖计时器，避免卸载后仍发起远程搜索写已销毁组件
  if (searchTimer) {
    clearTimeout(searchTimer);
    searchTimer = null;
  }
  loadSeq++;
});
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

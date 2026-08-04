<template>
  <div class="lov-select">
    <a-select
      v-if="mode === 'enum'"
      :model-value="modelValue"
      :placeholder="placeholder"
      :disabled="disabled"
      :multiple="multiple"
      allow-clear
      allow-search
      style="width: 100%"
      @update:model-value="onUpdate"
    >
      <a-option v-for="opt in options" :key="String(opt.value)" :value="opt.value" :label="opt.label">
        {{ opt.label }}
      </a-option>
    </a-select>

    <!-- LIST 单选：只读展示权威 label + 选择/清除 -->
    <a-input-group v-else-if="!multiple">
      <a-input
        :model-value="displayLabel"
        readonly
        :placeholder="placeholder"
        :disabled="disabled"
      />
      <a-button :disabled="disabled" @click="tableVisible = true">选择</a-button>
      <a-button v-if="hasValue" :disabled="disabled" @click="onClear">清除</a-button>
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
import { computed, reactive, ref, watch, onMounted } from 'vue';
import {
  fetchBatchLabel,
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
const listMeta = ref<LovListMeta | null>(null);
const tableVisible = ref(false);
/** value → label 缓存；LIST 历史值回显与已选标签展示共用 */
const labelCache = reactive<Record<string, string>>({});

const values = computed<(string | number)[]>(() => {
  if (props.modelValue == null || props.modelValue === '') return [];
  return Array.isArray(props.modelValue) ? props.modelValue : [props.modelValue];
});

const hasValue = computed(() => values.value.length > 0);

const displayLabel = computed(() => {
  if (!values.value.length) return '';
  const v = values.value[0];
  return labelCache[String(v)] ?? String(v);
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

function onConfirm(vals: unknown[]) {
  if (props.multiple) {
    emit('update:modelValue', vals as (string | number)[]);
  } else {
    emit('update:modelValue', (vals[0] as string | number) ?? undefined);
  }
  ensureLabels(vals as (string | number)[]);
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
onMounted(loadMeta);
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

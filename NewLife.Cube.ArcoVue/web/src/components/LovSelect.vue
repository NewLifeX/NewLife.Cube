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
    <a-input-group v-else>
      <a-input :model-value="displayLabel" readonly :placeholder="placeholder" :disabled="disabled" />
      <a-button :disabled="disabled" @click="tableVisible = true">选择</a-button>
      <a-button v-if="modelValue != null && modelValue !== ''" :disabled="disabled" @click="onUpdate(multiple ? [] : undefined)">
        清除
      </a-button>
    </a-input-group>
    <LovSelectTable
      v-model:visible="tableVisible"
      :lov-code="code"
      @select="onListSelect"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue';
import { fetchLovMeta, resolveLovType } from '@/core/utils/lov-api';
import type { LovEnumOption } from '@/core/types/lov';
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
const displayLabel = ref('');
const tableVisible = ref(false);

async function loadMeta() {
  if (!props.code) return;
  const inferred = resolveLovType(props.code);
  try {
    const meta = await fetchLovMeta(props.code);
    const item = meta.meta?.find((m) => m.lovCode === props.code) ?? meta.meta?.[0];
    if (item?.type === 'LIST' || inferred === 'LIST') {
      mode.value = 'list';
    } else {
      mode.value = 'enum';
      const inline = meta.inlineEnums?.[props.code];
      options.value = item?.type === 'ENUM' ? item.options : (inline ?? []);
    }
  } catch {
    mode.value = inferred === 'LIST' ? 'list' : 'enum';
  }
}

function onUpdate(v: unknown) {
  emit('update:modelValue', v as string | number | Array<string | number> | undefined);
}

function onListSelect(row: Record<string, unknown>) {
  const value = (row.value ?? row.Value ?? row.id ?? row.Id) as string | number;
  const label = String(row.label ?? row.Label ?? row.name ?? row.Name ?? value);
  displayLabel.value = label;
  if (props.multiple) {
    const cur = Array.isArray(props.modelValue) ? [...props.modelValue] : [];
    if (!cur.includes(value)) cur.push(value);
    emit('update:modelValue', cur);
  } else {
    emit('update:modelValue', value);
  }
  tableVisible.value = false;
}

watch(() => props.code, loadMeta, { immediate: true });
watch(
  () => props.modelValue,
  (v) => {
    if (mode.value === 'enum' && v != null) {
      const opt = options.value.find((o) => String(o.value) === String(v));
      displayLabel.value = opt?.label ?? String(v ?? '');
    } else if (v == null || v === '') {
      displayLabel.value = '';
    }
  },
);
onMounted(loadMeta);
</script>

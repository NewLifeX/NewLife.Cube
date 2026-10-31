<template>
  <!-- 地区级联（ItemType=area/area1~4）：按层级渲染多级下拉，提交最后一级编号 -->
  <div v-if="wt === 'area'" class="field-area">
    <v-select
      v-for="(levelOptions, level) in areaOptions"
      :key="level"
      :model-value="areaPath[level] ?? null"
      :items="levelOptions"
      item-title="name"
      item-value="id"
      :label="level === 0 ? '省份' : '下级地区'"
      variant="outlined"
      density="comfortable"
      clearable
      hide-details
      class="mb-1"
      @update:model-value="(v: any) => onAreaChange(level, v)"
    />
  </div>

  <!-- 时长（ItemType=TimeSpan）：时:分:秒，对外仍是原始存储值 -->
  <div v-else-if="wt === 'timeSpan'" class="field-timespan">
    <v-text-field
      v-for="(part, index) in spanPartsList"
      :key="index"
      :model-value="part.value"
      :label="part.placeholder"
      type="number"
      :min="0"
      :max="part.max"
      :step="part.precision === 3 ? 0.001 : 1"
      variant="outlined"
      density="comfortable"
      hide-details
      class="field-timespan__part"
      @update:model-value="(v: any) => onSpanChange(index, v)"
    />
  </div>

  <v-switch
    v-else-if="wt === 'switch'"
    :model-value="!!value"
    :label="label"
    color="primary"
    hide-details
    @update:model-value="(v: any) => emit('update:value', v)"
  />

  <v-select
    v-else-if="isSelect"
    :model-value="selectValue"
    :items="options"
    item-title="label"
    item-value="value"
    :label="label"
    :multiple="isMulti"
    :chips="isMulti"
    variant="outlined"
    density="comfortable"
    clearable
    hide-details
    @update:model-value="(v: any) => emit('update:value', normalizeSelectOut(v))"
  />

  <v-text-field
    v-else-if="wt === 'number' || wt === 'mapInput'"
    :model-value="value ?? ''"
    :label="label"
    type="number"
    :step="precisionOf > 0 ? 10 ** -precisionOf : 1"
    variant="outlined"
    density="comfortable"
    hide-details
    @update:model-value="(v: any) => emit('update:value', v === '' || v == null ? '' : Number(v))"
  />

  <v-text-field
    v-else-if="wt === 'datetime' || wt === 'date' || wt === 'time'"
    :model-value="value ?? ''"
    :label="label"
    :type="nativeDateType"
    variant="outlined"
    density="comfortable"
    hide-details
    @update:model-value="(v: any) => emit('update:value', v ?? '')"
  />

  <v-textarea
    v-else-if="isTextarea"
    :model-value="value ?? ''"
    :label="label"
    variant="outlined"
    density="comfortable"
    rows="3"
    auto-grow
    hide-details
    @update:model-value="(v: any) => emit('update:value', v)"
  />

  <v-text-field
    v-else-if="wt === 'password'"
    :model-value="value ?? ''"
    :label="label"
    type="password"
    variant="outlined"
    density="comfortable"
    hide-details
    @update:model-value="(v: any) => emit('update:value', v)"
  />

  <v-text-field
    v-else-if="wt === 'color'"
    :model-value="value ?? ''"
    label="颜色值（如 #1867c0）"
    variant="outlined"
    density="comfortable"
    hide-details
    @update:model-value="(v: any) => emit('update:value', v)"
  >
    <template #prepend-inner>
      <span class="field-color-dot" :style="{ background: value || '#000000' }" />
    </template>
  </v-text-field>

  <!-- 图片 / 附件：字段值即地址，给出预览与地址输入（上传由业务页处理） -->
  <div v-else-if="wt === 'image'" class="field-image">
    <img v-if="value" :src="value" alt="" class="field-image__preview" />
    <v-text-field
      :model-value="value ?? ''"
      label="图片地址"
      variant="outlined"
      density="comfortable"
      hide-details
      @update:model-value="(v: any) => emit('update:value', v)"
    />
  </div>

  <v-text-field
    v-else-if="wt === 'file'"
    :model-value="value ?? ''"
    label="附件地址"
    variant="outlined"
    density="comfortable"
    hide-details
    @update:model-value="(v: any) => emit('update:value', v)"
  />

  <span v-else-if="wt === 'readonly'" class="text-body-1">{{ value ?? '-' }}</span>

  <v-text-field
    v-else
    :model-value="value ?? ''"
    :label="label"
    :type="inputType"
    variant="outlined"
    density="comfortable"
    clearable
    hide-details
    @update:model-value="(v: any) => emit('update:value', v)"
  />
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import type { AreaOption, DataField, TimeSpanParts, TimeSpanUnit } from '@newlifex/cube';
import {
  createAreaCascade,
  joinTimeSpan,
  resolveWidget,
  splitTimeSpan,
  timeSpanUnitOfDescription,
} from '@newlifex/cube';
import {
  VTextField,
  VTextarea,
  VSwitch,
  VSelect,
} from 'vuetify/components';
import cubeApi from '@/api';

const props = defineProps<{
  field: DataField;
  value?: any;
  disabled?: boolean;
}>();

const emit = defineEmits<{
  (e: 'update:value', v: any): void;
}>();

const mapping = computed(() => resolveWidget(props.field));
const wt = computed(() => mapping.value.widget);
const label = computed(() => props.field.displayName || props.field.name);

const isSelect = computed(() => ['select', 'lov', 'lovMulti', 'mapSelect'].includes(wt.value));
const isMulti = computed(() => wt.value === 'lovMulti' || !!mapping.value.props?.multiple);
const isTextarea = computed(() => ['textarea', 'html', 'markdown', 'code', 'json'].includes(wt.value));

const nativeDateType = computed(() => {
  if (wt.value === 'datetime') return 'datetime-local';
  if (wt.value === 'time') return 'time';
  return 'date';
});

const inputType = computed(() => {
  switch (wt.value) {
    case 'email':
      return 'email';
    case 'phone':
      return 'tel';
    case 'url':
      return 'url';
    default:
      return 'text';
  }
});

const precisionOf = computed(() => Number(mapping.value.props?.precision ?? props.field.scale ?? 0) || 0);

const options = computed(() => {
  const ds = (mapping.value.props?.dataSource ?? props.field.dataSource) as Record<string, string> | undefined;
  if (!ds) return [] as { value: string; label: string }[];
  return Object.entries(ds).map(([value, text]) => ({ value, label: text }));
});

function toMultiArray(v: any): string[] {
  if (v == null || v === '') return [];
  if (Array.isArray(v)) return v.map(String);
  const text = String(v).trim();
  if (!text) return [];
  try {
    const parsed = JSON.parse(text);
    if (Array.isArray(parsed)) return parsed.map(String);
  } catch {
    /* 非 JSON，按逗号串处理 */
  }
  return text.split(',').filter(Boolean);
}

const selectValue = computed(() => (isMulti.value ? toMultiArray(props.value) : (props.value ?? null)));

function normalizeSelectOut(v: any): any {
  if (!isMulti.value) return v ?? '';
  const array = Array.isArray(v) ? v : toMultiArray(v);
  return array.join(',');
}

// ── 时长 ──
const spanUnit = computed<TimeSpanUnit>(
  () => (mapping.value.props?.unit as TimeSpanUnit) ?? timeSpanUnitOfDescription(props.field.description),
);
const spanParts = ref<TimeSpanParts>(splitTimeSpan(props.value, spanUnit.value));
watch(
  () => props.value,
  (v) => {
    spanParts.value = splitTimeSpan(v, spanUnit.value);
  },
);

const spanPartsList = computed(() => [
  { value: spanParts.value.hours, max: undefined, precision: 0, placeholder: '时' },
  { value: spanParts.value.minutes, max: 59, precision: 0, placeholder: '分' },
  { value: spanParts.value.seconds, max: 59, precision: spanUnit.value === 'milliseconds' ? 3 : 0, placeholder: '秒' },
]);

const SPAN_KEYS: (keyof TimeSpanParts)[] = ['hours', 'minutes', 'seconds'];

function onSpanChange(index: number, v: any) {
  const next = { ...spanParts.value, [SPAN_KEYS[index]]: Number(v ?? 0) || 0 };
  spanParts.value = next;
  emit('update:value', joinTimeSpan(next, spanUnit.value));
}

// ── 地区级联 ──
const areaOptions = ref<AreaOption[][]>([]);
const areaPath = ref<number[]>([]);
let areaCascade: ReturnType<typeof createAreaCascade> | null = null;

function applyArea(state: { options: AreaOption[][]; path: number[] }) {
  areaOptions.value = state.options;
  areaPath.value = state.path;
}

async function initArea(value: any) {
  areaCascade ??= createAreaCascade(cubeApi.area, Number(mapping.value.props?.areaLevel ?? 3) || 3);
  const n = Number(value ?? 0);
  applyArea(await areaCascade.init(Number.isFinite(n) ? n : 0));
}

async function onAreaChange(level: number, v: any) {
  if (!areaCascade) return;
  const state = await areaCascade.selectLevel(level, v === '' || v == null ? null : Number(v));
  applyArea(state);
  emit('update:value', state.value ?? '');
}

onMounted(() => {
  if (wt.value === 'area') void initArea(props.value);
});

watch(
  () => props.value,
  (v) => {
    if (wt.value !== 'area' || !areaCascade) return;
    const current = areaCascade.snapshot.value;
    if (Number(v ?? 0) !== Number(current ?? 0)) void initArea(v);
  },
);
</script>

<style scoped>
.field-timespan {
  display: flex;
  gap: 6px;
}

.field-timespan__part {
  max-width: 96px;
}

.field-image {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
}

.field-image__preview {
  max-width: 72px;
  max-height: 72px;
  border-radius: 4px;
  border: 1px solid rgba(0, 0, 0, 0.24);
}

.field-color-dot {
  display: inline-block;
  width: 14px;
  height: 14px;
  margin-top: 12px;
  border-radius: 3px;
  border: 1px solid rgba(0, 0, 0, 0.24);
}
</style>

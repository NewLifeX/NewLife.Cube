<template>
  <!-- 地区级联（ItemType=area/area1~4）：按层级渲染多级下拉，提交最后一级编号 -->
  <div v-if="widget === 'area'" class="field-area">
    <a-select
      v-for="(levelOptions, level) in areaOptions"
      :key="level"
      :model-value="areaPath[level] ?? ''"
      :allow-clear="true"
      :disabled="disabled"
      :placeholder="level === 0 ? '请选择省份' : '请选择下级'"
      style="width: 100%; margin-bottom: 4px"
      @change="(v: any) => onAreaChange(level, v)"
    >
      <a-option v-for="opt in levelOptions" :key="opt.id" :value="opt.id" :label="opt.name" />
    </a-select>
  </div>

  <!-- 时长（ItemType=TimeSpan）：时:分:秒，对外仍是原始存储值 -->
  <div v-else-if="widget === 'timeSpan'" class="field-timespan">
    <a-input-number
      v-for="(part, index) in spanPartsList"
      :key="index"
      :model-value="part.value"
      :min="0"
      :max="part.max"
      :precision="part.precision"
      :disabled="disabled"
      :placeholder="part.placeholder"
      style="width: 88px"
      @change="(v: any) => onSpanChange(index, v)"
    />
  </div>

  <a-switch
    v-else-if="widget === 'switch'"
    :model-value="!!value"
    :disabled="disabled"
    @change="(v: any) => emit('update:value', v)"
  />

  <a-select
    v-else-if="isSelectWidget"
    :model-value="selectValue"
    :multiple="isMulti"
    :disabled="disabled"
    :allow-clear="true"
    :placeholder="`请选择${label}`"
    style="width: 100%"
    @change="(v: any) => emit('update:value', normalizeSelectOut(v))"
  >
    <a-option v-for="opt in options" :key="opt.value" :value="opt.value" :label="opt.label" />
  </a-select>

  <a-input-number
    v-else-if="widget === 'number' || widget === 'mapInput'"
    :model-value="toNumber(value)"
    :precision="widget === 'number' ? precisionOf : 0"
    :disabled="disabled"
    :placeholder="`请输入${label}`"
    style="width: 100%"
    @change="(v: any) => emit('update:value', v ?? '')"
  />

  <a-date-picker
    v-else-if="widget === 'datetime' || widget === 'date'"
    :model-value="value"
    :show-time="widget === 'datetime'"
    :disabled="disabled"
    :placeholder="`请选择${label}`"
    :allow-clear="true"
    style="width: 100%"
    @change="(v: any) => emit('update:value', v ?? '')"
  />

  <a-time-picker
    v-else-if="widget === 'time'"
    :model-value="value"
    :disabled="disabled"
    :placeholder="`请选择${label}`"
    :allow-clear="true"
    style="width: 100%"
    @change="(v: any) => emit('update:value', v ?? '')"
  />

  <a-textarea
    v-else-if="isTextareaWidget"
    :model-value="value ?? ''"
    :disabled="disabled"
    :placeholder="`请输入${label}`"
    :auto-size="{ minRows: 3 }"
    @change="(v: string) => emit('update:value', v)"
  />

  <a-input-password
    v-else-if="widget === 'password'"
    :model-value="value ?? ''"
    :disabled="disabled"
    :placeholder="`请输入${label}`"
    allow-clear
    @change="(v: string) => emit('update:value', v)"
  />

  <a-input
    v-else-if="widget === 'color'"
    :model-value="value ?? ''"
    :disabled="disabled"
    placeholder="请输入颜色值，如 #1677ff"
    allow-clear
    @change="(v: string) => emit('update:value', v)"
  >
    <template #prefix>
      <span class="field-color-dot" :style="{ background: value || '#000000' }" />
    </template>
  </a-input>

  <!-- 图片 / 附件：字段值即地址，给出预览与地址输入（上传由业务页处理） -->
  <div v-else-if="widget === 'image'" class="field-image">
    <img v-if="value" :src="value" alt="" class="field-image__preview" />
    <a-input
      :model-value="value ?? ''"
      :disabled="disabled"
      placeholder="图片地址"
      allow-clear
      @change="(v: string) => emit('update:value', v)"
    />
  </div>

  <a-input
    v-else-if="widget === 'file'"
    :model-value="value ?? ''"
    :disabled="disabled"
    placeholder="附件地址"
    allow-clear
    @change="(v: string) => emit('update:value', v)"
  />

  <span v-else-if="widget === 'readonly'" class="field-readonly">{{ value ?? '-' }}</span>

  <a-input
    v-else
    :model-value="value ?? ''"
    :disabled="disabled"
    :type="inputType"
    :placeholder="`请输入${label}`"
    allow-clear
    @change="(v: string) => emit('update:value', v)"
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
const widget = computed(() => mapping.value.widget);
const label = computed(() => props.field.displayName || props.field.name);

const isSelectWidget = computed(() => ['select', 'lov', 'lovMulti', 'mapSelect'].includes(widget.value));
const isMulti = computed(() => widget.value === 'lovMulti' || !!mapping.value.props?.multiple);
const isTextareaWidget = computed(() => ['textarea', 'html', 'markdown', 'code', 'json'].includes(widget.value));

/** 输入框原生类型：邮箱/电话/网址用浏览器校验，其余文本 */
const inputType = computed(() => {
  switch (widget.value) {
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

/** 数值精度：percent/money 沿用字段 scale */
const precisionOf = computed(() => Number(mapping.value.props?.precision ?? props.field.scale ?? 0) || 0);

function toNumber(v: any): number | undefined {
  if (v === null || v === undefined || v === '') return undefined;
  const n = Number(v);
  return Number.isNaN(n) ? undefined : n;
}

/** 下拉选项：后端 dataSource（引擎已随 props 透传） */
const options = computed(() => {
  const ds = (mapping.value.props?.dataSource ?? props.field.dataSource) as Record<string, string> | undefined;
  if (!ds) return [] as { value: string; label: string }[];
  return Object.entries(ds).map(([value, text]) => ({ value, label: text }));
});

/** 多选值归一：数组 / 逗号串 / JSON 串 */
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

const selectValue = computed(() => (isMulti.value ? toMultiArray(props.value) : (props.value ?? '')));

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
  applyArea(await areaCascade.init(toNumber(value) ?? 0));
}

async function onAreaChange(level: number, v: any) {
  if (!areaCascade) return;
  const state = await areaCascade.selectLevel(level, v === '' || v == null ? null : Number(v));
  applyArea(state);
  emit('update:value', state.value ?? '');
}

onMounted(() => {
  if (widget.value === 'area') void initArea(props.value);
});

watch(
  () => props.value,
  (v) => {
    if (widget.value !== 'area' || !areaCascade) return;
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

.field-color-dot {
  display: inline-block;
  width: 14px;
  height: 14px;
  border-radius: 3px;
  border: 1px solid var(--color-border-2);
}

.field-readonly {
  color: var(--color-text-3);
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
  border: 1px solid var(--color-border-2);
}
</style>

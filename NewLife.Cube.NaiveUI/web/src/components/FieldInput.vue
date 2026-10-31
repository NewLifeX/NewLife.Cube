<template>
  <!-- 地区级联（ItemType=area/area1~4）：按层级渲染多级下拉，提交最后一级编号 -->
  <div v-if="widget === 'area'" class="field-area">
    <n-select
      v-for="(levelOptions, level) in areaOptions"
      :key="level"
      :value="areaPath[level] ?? null"
      :options="levelOptions.map((o) => ({ label: o.name, value: o.id }))"
      :placeholder="level === 0 ? '请选择省份' : '请选择下级'"
      clearable
      style="width: 100%; margin-bottom: 4px"
      @update:value="(v: any) => onAreaChange(level, v)"
    />
  </div>

  <!-- 时长（ItemType=TimeSpan）：时:分:秒，对外仍是原始存储值 -->
  <div v-else-if="widget === 'timeSpan'" class="field-timespan">
    <n-input-number
      v-for="(part, index) in spanPartsList"
      :key="index"
      :value="part.value"
      :min="0"
      :max="part.max"
      :precision="part.precision"
      :placeholder="part.placeholder"
      style="width: 88px"
      @update:value="(v: any) => onSpanChange(index, v)"
    />
  </div>

  <n-switch v-else-if="widget === 'switch'" :value="!!value" @update:value="(v: any) => emit('update:value', v)" />

  <n-select
    v-else-if="isSelect"
    :value="selectValue"
    :options="options"
    :multiple="isMulti"
    clearable
    filterable
    style="width: 100%"
    :placeholder="`请选择${label}`"
    @update:value="(v: any) => emit('update:value', normalizeSelectOut(v))"
  />

  <n-input-number
    v-else-if="widget === 'number' || widget === 'mapInput'"
    :value="toNumber(value)"
    :precision="widget === 'number' ? precisionOf : 0"
    :placeholder="`请输入${label}`"
    style="width: 100%"
    @update:value="(v: any) => emit('update:value', v ?? '')"
  />

  <n-date-picker
    v-else-if="widget === 'datetime'"
    :value="toTimestamp(value)"
    type="datetime"
    clearable
    style="width: 100%"
    @update:value="(v: any) => emit('update:value', v ? new Date(v).toISOString() : '')"
  />

  <n-date-picker
    v-else-if="widget === 'date'"
    :value="toTimestamp(value)"
    type="date"
    clearable
    style="width: 100%"
    @update:value="(v: any) => emit('update:value', v ? new Date(v).toISOString().split('T')[0] : '')"
  />

  <n-time-picker
    v-else-if="widget === 'time'"
    :value="value || null"
    clearable
    style="width: 100%"
    @update:value="(v: any) => emit('update:value', v ?? '')"
  />

  <n-input
    v-else-if="isTextarea"
    :value="value ?? ''"
    type="textarea"
    :autosize="{ minRows: 3, maxRows: 8 }"
    :placeholder="`请输入${label}`"
    @update:value="(v: any) => emit('update:value', v)"
  />

  <n-input
    v-else-if="widget === 'password'"
    :value="value ?? ''"
    type="password"
    show-password-on="click"
    :placeholder="`请输入${label}`"
    @update:value="(v: any) => emit('update:value', v)"
  />

  <n-upload
    v-else-if="widget === 'file'"
    :show-file-list="true"
    action="/Admin/File/UploadLayui"
    :max="1"
    @finish="onUploadFinish"
  >
    <n-button>选择文件</n-button>
  </n-upload>

  <div v-else-if="widget === 'image'" class="field-image">
    <img v-if="value" :src="value" alt="" style="max-width: 120px; max-height: 120px; border-radius: 4px" />
    <n-upload :show-file-list="false" action="/Admin/File/UploadLayui" @finish="onUploadFinish">
      <n-button size="small">上传图片</n-button>
    </n-upload>
  </div>

  <n-input
    v-else-if="widget === 'color'"
    :value="value ?? ''"
    placeholder="请输入颜色值，如 #18a058"
    @update:value="(v: any) => emit('update:value', v)"
  >
    <template #prefix>
      <span class="field-color-dot" :style="{ background: value || '#000000' }" />
    </template>
  </n-input>

  <span v-else-if="widget === 'readonly'" class="field-readonly">{{ value ?? '-' }}</span>

  <n-input
    v-else
    :value="value ?? ''"
    :type="inputType"
    :maxlength="field.length && field.length > 0 ? field.length : undefined"
    :placeholder="`请输入${label}`"
    clearable
    @update:value="(v: any) => emit('update:value', v)"
  />
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import type { AreaOption, FieldMapping, TimeSpanParts, TimeSpanUnit } from '@newlifex/cube';
import {
  createAreaCascade,
  joinTimeSpan,
  splitTimeSpan,
  timeSpanUnitOfDescription,
} from '@newlifex/cube';
import { NInput, NInputNumber, NSelect, NSwitch, NDatePicker, NTimePicker, NUpload, NButton } from 'naive-ui';
import api from '@/api';

const props = defineProps<{
  value: any;
  mapping: FieldMapping;
}>();

const emit = defineEmits<{
  (e: 'update:value', val: any): void;
}>();

const widget = computed(() => props.mapping.widget);
const field = computed(() => props.mapping.field);
const label = computed(() => field.value.displayName || field.value.name);

const isSelect = computed(() => ['select', 'lov', 'lovMulti', 'mapSelect'].includes(widget.value));
const isMulti = computed(() => widget.value === 'lovMulti' || !!props.mapping.props?.multiple);
const isTextarea = computed(() => ['textarea', 'html', 'markdown', 'code', 'json'].includes(widget.value));

const inputType = computed<'text' | 'password' | 'textarea'>(() => {
  switch (widget.value) {
    case 'password':
      return 'password';
    case 'textarea':
      return 'textarea';
    default:
      return 'text';
  }
});

const precisionOf = computed(() => Number(props.mapping.props?.precision ?? field.value.scale ?? 0) || 0);

function toNumber(v: any): number | null {
  if (v === null || v === undefined || v === '') return null;
  const n = Number(v);
  return Number.isNaN(n) ? null : n;
}

function toTimestamp(v: any): number | null {
  if (!v) return null;
  const t = new Date(v).getTime();
  return Number.isNaN(t) ? null : t;
}

/** 下拉选项：后端 dataSource（引擎已随 props 透传） */
const options = computed(() => {
  const ds = (props.mapping.props?.dataSource ?? field.value.dataSource) as Record<string, string> | undefined;
  if (!ds) return [] as { label: string; value: string }[];
  return Object.entries(ds).map(([value, text]) => ({ label: text, value }));
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

/** 上传完成后回写文件地址 */
function onUploadFinish({ event }: any) {
  try {
    const res = JSON.parse((event?.target as XMLHttpRequest)?.response);
    if (res?.data?.url) emit('update:value', res.data.url);
  } catch {
    /* 非标准响应，忽略 */
  }
}

// ── 时长 ──
const spanUnit = computed<TimeSpanUnit>(
  () => (props.mapping.props?.unit as TimeSpanUnit) ?? timeSpanUnitOfDescription(field.value.description),
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
  areaCascade ??= createAreaCascade(api.area, Number(props.mapping.props?.areaLevel ?? 3) || 3);
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
.field-readonly {
  color: rgba(0, 0, 0, 0.65);
}

.field-image {
  display: flex;
  align-items: flex-end;
  gap: 8px;
}

.field-timespan {
  display: flex;
  gap: 6px;
}

.field-color-dot {
  display: inline-block;
  width: 14px;
  height: 14px;
  border-radius: 3px;
  border: 1px solid rgba(0, 0, 0, 0.24);
}
</style>

<script setup lang="ts">
/**
 * 表单内容渲染组件（字段 → 控件 唯一分发点）
 *
 * 入参 `fields` 为后端下发的 `FieldMeta[]`，内部统一调用
 * `web/core/utils/fieldControl.ts` 的 `resolveControl` 解析控件类型，
 * 不再维护本地 TYPE_TO_*_TYPE 映射。
 *
 * v-model 绑定整个表单对象；任一控件变更时聚合 emit 整个对象（沿用既有行为）。
 */
import { computed } from 'vue';
import type { FieldMeta, ControlType } from '../../types/field';
import { resolveControl, isFullWidthControl, resolveNumberPrecision, resolveNumberStep } from '../../utils/fieldControl';
import LovSelect from '../../components/LovSelect.vue';
import Uploader from '../../components/Uploader.vue';
import JsonEditor from '../../components/JsonEditor.vue';
import RichEditor from '../../components/RichEditor.vue';
import ColorPicker from '../../components/ColorPicker.vue';
import IconSelector from '../../components/IconSelector.vue';

interface Props {
  title?: string;
  fields?: FieldMeta[];
  modelValue?: Record<string, unknown>;
  /** 接口前缀（动态类型，由路由计算得，如 /Device/DeviceProfile），透传给 Uploader 推导上传地址 */
  apiPrefix?: string;
}

const props = withDefaults(defineProps<Props>(), {
  title: '基本信息',
  fields: () => [],
  modelValue: () => ({}),
  apiPrefix: '',
});

const emit = defineEmits<{
  'update:modelValue': [val: Record<string, unknown>];
}>();

/** 每个字段解析出的控件类型 */
function controlOf(field: FieldMeta): ControlType {
  return resolveControl(field);
}

function isRequired(field: FieldMeta): boolean {
  return !field.nullable && !field.primaryKey && controlOf(field) !== 'readonly';
}

function getValue(key: string): unknown {
  return props.modelValue?.[key];
}

function toNumber(val: unknown): number | undefined {
  if (val === null || val === undefined || val === '') return undefined;
  const n = Number(val);
  return Number.isNaN(n) ? undefined : n;
}

function updateField(key: string, value: unknown) {
  emit('update:modelValue', { ...props.modelValue, [key]: value });
}

/** 数值控件统一回写 number */
function updateNumber(key: string, value: number | undefined) {
  updateField(key, value === undefined ? '' : value);
}

/**
 * 多选值归一为 string[]，兼容以下来源：
 * - 数组（表单编辑中 LovSelect 直接回写的 string[]）
 * - 逗号分隔字符串（后端 String 列存储格式，如 "1,2"）
 * - JSON 字符串（如 '["1","2"]'）
 */
function toMultiArray(val: unknown): string[] {
  if (val == null) return [];
  if (Array.isArray(val)) return val.filter((x) => x != null).map(String);
  const s = String(val).trim();
  if (!s) return [];
  try {
    const parsed = JSON.parse(s);
    if (Array.isArray(parsed)) return parsed.filter((x: unknown) => x != null).map(String);
  } catch {
    /* 非 JSON，按逗号拆分 */
  }
  return s
    .split(',')
    .map((x) => x.trim())
    .filter((x) => x.length > 0);
}
</script>

<template>
  <div class="form-content">
    <div class="fmc-header">
      <h2 class="fmc-title">{{ title }}</h2>
    </div>
    <div class="fmc-body">
      <div class="fmc-grid">
        <div
          v-for="field in fields"
          :key="field.name"
          class="fmc-field"
          :class="{ 'fmc-field--full': isFullWidthControl(controlOf(field)) }"
        >
          <label :for="`fmc-${field.name}`" class="fmc-label">
            {{ field.displayName || field.name }}
            <span v-if="isRequired(field)" class="fmc-required" aria-hidden="true">*</span>
          </label>

          <!-- 大文本 -->
          <el-input
            v-if="controlOf(field) === 'textarea'"
            :id="`fmc-${field.name}`"
            type="textarea"
            :rows="3"
            class="fmc-textarea"
            :placeholder="field.description || '请输入...'"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <!-- 数值 -->
          <el-input-number
            v-else-if="controlOf(field) === 'inputNumber'"
            :id="`fmc-${field.name}`"
            class="fmc-input-number"
            :controls="true"
            :precision="resolveNumberPrecision(field)"
            :step="resolveNumberStep(field)"
            :placeholder="field.description || '请输入数值'"
            :model-value="toNumber(getValue(field.name))"
            @update:model-value="(v: number | undefined) => updateNumber(field.name, v)"
          />

          <!-- 布尔开关 -->
          <div v-else-if="controlOf(field) === 'switch'" class="fmc-switch-wrapper">
            <el-switch
              :model-value="Boolean(getValue(field.name))"
              @change="(v: string | number | boolean) => updateField(field.name, v)"
            />
          </div>

          <!-- 日期时间 -->
          <el-date-picker
            v-else-if="controlOf(field) === 'datePicker'"
            class="fmc-date-picker"
            type="datetime"
            :placeholder="field.description || '选择日期时间'"
            format="YYYY-MM-DD HH:mm:ss"
            value-format="YYYY-MM-DDTHH:mm:ss"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <!-- 时间 -->
          <el-time-picker
            v-else-if="controlOf(field) === 'timePicker'"
            class="fmc-date-picker"
            :placeholder="field.description || '选择时间'"
            format="HH:mm:ss"
            value-format="HH:mm:ss"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <!-- 值集单选 -->
          <LovSelect
            v-else-if="controlOf(field) === 'lov'"
            :code="field.lovCode || ''"
            :model-value="(getValue(field.name) as string | number | undefined)"
            :placeholder="field.description || '请选择'"
            @update:model-value="(v: string | number | string[] | undefined) => updateField(field.name, v)"
          />

          <!-- 值集多选 -->
          <LovSelect
            v-else-if="controlOf(field) === 'lovMulti'"
            :code="field.lovCode || ''"
            :multiple="true"
            :model-value="toMultiArray(getValue(field.name))"
            :placeholder="field.description || '请选择（多选）'"
            @update:model-value="(v: string | number | string[] | undefined) => updateField(field.name, v)"
          />

          <!-- 文件上传 -->
          <Uploader
            v-else-if="controlOf(field) === 'upload'"
            kind="file"
            :api-prefix="apiPrefix"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <!-- 图片上传 -->
          <Uploader
            v-else-if="controlOf(field) === 'image'"
            kind="image"
            :api-prefix="apiPrefix"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <!-- Json 编辑器 -->
          <JsonEditor
            v-else-if="controlOf(field) === 'json'"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <!-- 富文本 HTML -->
          <RichEditor
            v-else-if="controlOf(field) === 'richHtml'"
            mode="html"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <!-- 富文本 Markdown -->
          <RichEditor
            v-else-if="controlOf(field) === 'richMarkdown'"
            mode="markdown"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <!-- 颜色选择器 -->
          <ColorPicker
            v-else-if="controlOf(field) === 'color'"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <!-- 图标选择器 -->
          <IconSelector
            v-else-if="controlOf(field) === 'icon'"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <!-- 邮箱 / 手机 / 网址（带格式校验） -->
          <el-input
            v-else-if="controlOf(field) === 'email' || controlOf(field) === 'tel' || controlOf(field) === 'url'"
            :id="`fmc-${field.name}`"
            class="fmc-input"
            :type="controlOf(field) === 'email' ? 'email' : controlOf(field) === 'tel' ? 'tel' : 'url'"
            :placeholder="field.description || '请输入...'"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <!-- 只读（Guid 等） -->
          <el-input
            v-else-if="controlOf(field) === 'readonly'"
            :id="`fmc-${field.name}`"
            class="fmc-input"
            disabled
            :model-value="String(getValue(field.name) ?? '')"
          />

          <!-- 默认：普通文本输入 -->
          <el-input
            v-else
            :id="`fmc-${field.name}`"
            class="fmc-input"
            :placeholder="field.description || '请输入...'"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.form-content {
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-light);
  border-radius: var(--el-border-radius-base);
  box-shadow: var(--el-box-shadow-light);
  overflow: hidden;
}

.fmc-header {
  padding: 14px 18px;
  border-bottom: 1px solid var(--el-border-color-light);
}

.fmc-title {
  font-family: 'Libre Baskerville', Georgia, serif;
  font-size: 14px;
  font-weight: 700;
  color: var(--el-text-color-primary);
  margin: 0;
}

.fmc-body {
  padding: 20px;
}

.fmc-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px 24px;
}

.fmc-field {
  display: flex;
  flex-direction: column;

  &--full {
    grid-column: 1 / -1;
  }
}

.fmc-label {
  display: block;
  font-family: 'Fira Sans', system-ui, sans-serif;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-bottom: 5px;
  font-weight: 500;
}

.fmc-required {
  color: var(--el-color-danger);
  margin-left: 2px;
}

/**
 * 表单输入控件统一样式。
 *
 * 注意：只覆盖「视觉」属性（字体、颜色），不碰布局（width/height/padding/border），
 * 避免与 Element Plus 内置样式冲突造成多余空间或溢出。
 */

/** 文本输入（el-input 根元素） */
.fmc-input {
  font-family: 'Fira Sans', system-ui, sans-serif;
  font-size: 13px;
  color: var(--el-text-color-primary);
}

/** 多行文本域（独立类，避免影响普通输入） */
.fmc-textarea {
  font-family: 'Fira Sans', system-ui, sans-serif;
  font-size: 13px;
  color: var(--el-text-color-primary);
  max-height: 120px;
  resize: vertical;
}

.fmc-input-number {
  width: 100%;
}

.fmc-switch-wrapper {
  display: flex;
  align-items: center;
  padding-top: 2px;
}

.fmc-date-picker {
  width: 100%;
}
</style>

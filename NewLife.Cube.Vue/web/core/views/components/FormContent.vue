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
import { computed, defineAsyncComponent, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { QuestionFilled } from '@element-plus/icons-vue';
import type { FieldMeta, ControlType } from '../../types/field';
import {
  areaLevel,
  enumOptionValue,
  isFullWidthControl,
  isMoneyItemType,
  isPercentItemType,
  resolveControl,
  resolveNumberPrecision,
  resolveNumberStep,
} from '../../utils/fieldControl';
import { resolveFieldRenderer } from '../../engine/fieldRenderers';
import { toPascalAndCamel } from '../../utils/url';
import LovSelect from '../../components/LovSelect/index.vue';
import AreaSelect from '../../components/AreaSelect/index.vue';
import Uploader from '../../components/Uploader.vue';
// 重型编辑器改为异步组件，仅在表单确有 json / 富文本字段时才按需加载，
// 避免把 @wangeditor / md-editor-v3 / vanilla-jsoneditor 全部打进主表单 chunk。
const JsonEditor = defineAsyncComponent(() => import('../../components/JsonEditor.vue'));
const RichEditor = defineAsyncComponent(() => import('../../components/RichEditor.vue'));
import ColorPicker from '../../components/ColorPicker.vue';
import IconSelector from '../../components/IconSelector.vue';

interface Props {
  title?: string;
  fields?: FieldMeta[];
  modelValue?: Record<string, unknown>;
  /** 接口前缀（动态类型，由路由计算得，如 /Device/DeviceProfile），透传给 Uploader 推导上传地址 */
  apiPrefix?: string;
  /** 栅格列数，默认 2。在弹窗/抽屉中可根据字段数量动态调整 */
  columns?: number;
  /** 是否禁用所有表单控件（查看详情模式只读） */
  disabled?: boolean;
  /** 分组方式：auto=有多个分类时按分类分区，category=强制按分类，none=平铺，函数=自定义分组 */
  groupBy?: 'auto' | 'category' | 'none' | ((field: FieldMeta) => string);
  /** 分组呈现：tabs=分类 Tab（默认），sections=标题分区 */
  groupDisplay?: 'tabs' | 'sections';
  /** 字段说明呈现：tooltip=Label 旁问号提示（默认），placeholder=沿用输入框占位，none=不展示 */
  descriptionMode?: 'tooltip' | 'placeholder' | 'none';
}

const props = withDefaults(defineProps<Props>(), {
  title: undefined,
  fields: () => [],
  modelValue: () => ({}),
  apiPrefix: '',
  columns: 2,
  groupBy: 'auto',
  groupDisplay: 'tabs',
  descriptionMode: 'tooltip',
});

const { t } = useI18n();

/** 未指定标题时的默认表单标题 */
const titleText = computed(() => props.title || t('form.basicInfo'));

/** 无分类字段的兜底分组名（sections 模式） */
const defaultGroup = computed(() => t('form.otherGroup'));

/** 无分类字段的 Tab 文案（tabs 模式，对齐 MVC 的「默认选项」） */
const defaultTabGroup = computed(() => t('form.defaultGroup'));

/** 空分类在 el-tabs 内部的占位 name，避免空字符串被当作未设置 */
const DEFAULT_GROUP_KEY = '__default_group__';

const useTabs = computed(() => props.groupDisplay === 'tabs');

const activeGroupKey = ref('');

/** preset 行级显隐：hidden 与 visibleWhen 共同决定字段是否出现在当前模型下 */
const visibleFields = computed(() =>
  props.fields.filter(
    (field) => !field.hidden && (!field.visibleWhen || field.visibleWhen(props.modelValue ?? {})),
  ),
);

/** 按 Category 分组；没有分类时保持一个默认分组，模板结构不变 */
const groups = computed(() => {
  const fields = visibleFields.value;
  const hasCategory = fields.some((f) => !!f.category?.trim());
  if (props.groupBy === 'none' || (props.groupBy === 'auto' && !hasCategory)) {
    return [{ key: '', title: '', fields }];
  }

  const map = new Map<string, typeof fields>();
  for (const field of fields) {
    const key =
      (typeof props.groupBy === 'function' ? props.groupBy(field) : field.category)?.trim() || '';
    const list = map.get(key) ?? [];
    list.push(field);
    map.set(key, list);
  }
  return [...map.entries()].map(([key, fields]) => ({
    key,
    // 分区模式沿用「其他」，Tab 模式在 groupTabTitle 里换成「默认选项」
    title: key || defaultGroup.value,
    fields,
  }));
});

/** 空分类分组的 Tab name（不能为空字符串） */
const tabNameOf = (group: { key: string }) => group.key || DEFAULT_GROUP_KEY;

/** Tab 文案：对齐 MVC 规则，空分类用「默认选项」，短分类名补「选项」 */
function groupTabTitle(group: { key: string }): string {
  const name = group.key.trim();
  if (!name) return defaultTabGroup.value;
  return name.length < 4 ? t('form.groupTabShort', { name }) : name;
}

/** 当前激活分组；找不到时回退第一组 */
const activeGroup = computed(
  () => groups.value.find((group) => tabNameOf(group) === activeGroupKey.value) ?? groups.value[0],
);

/** 实际渲染的分组：Tab 模式只渲染当前分类，分区模式渲染全部分组 */
const displayGroups = computed(() =>
  useTabs.value ? (activeGroup.value ? [activeGroup.value] : []) : groups.value,
);

// 字段异步加载或显隐变化后，保证激活 Tab 始终有效
watch(
  () => groups.value.map(tabNameOf),
  (keys) => {
    if (!keys.includes(activeGroupKey.value)) activeGroupKey.value = keys[0] ?? '';
  },
  { immediate: true },
);

/** 字段说明：tooltip 模式展示在 Label 旁，placeholder/none 模式不展示 */
function descriptionOf(field: FieldMeta): string {
  return props.descriptionMode === 'tooltip' ? (field.description ?? '').trim() : '';
}

/** 输入框占位：placeholder 模式沿用旧行为（描述优先），其余模式使用通用提示 */
function hintOf(field: FieldMeta, key: string): string {
  return props.descriptionMode === 'placeholder' ? field.description || t(key) : t(key);
}

const emit = defineEmits<{
  'update:modelValue': [val: Record<string, unknown>];
}>();

/** 每个字段解析出的控件类型 */
function controlOf(field: FieldMeta): ControlType {
  return resolveControl(field);
}

/** 后端下发 view 名时优先交给已注册的字段渲染器，未注册则回退内置控件 */
const fieldRendererOf = (field: FieldMeta) => resolveFieldRenderer(field);

/** 纯日期字段（ItemType=date）只选日期，不带时分秒 */
function isDateOnly(field: FieldMeta): boolean {
  return (field.itemType ?? '').trim().toLowerCase() === 'date';
}

/** 数值控件的单位提示 */
function numberPrefix(field: FieldMeta): string {
  return isMoneyItemType(field.itemType) ? '¥' : '';
}

function numberSuffix(field: FieldMeta): string {
  return isPercentItemType(field.itemType) ? '%' : '';
}

function isRequired(field: FieldMeta): boolean {
  // preset 的 requiredWhen 优先于静态必填判定
  if (field.requiredWhen) return field.requiredWhen(props.modelValue ?? {});
  // Boolean 开关恒有值（默认 false），与 fieldControl.isRequiredField 口径一致，避免星号与校验漂移
  if (field.typeName === 'Boolean') return false;
  return !field.nullable && !field.primaryKey && controlOf(field) !== 'readonly';
}

/**
 * 解析模型中的实际键。后端字段元数据 name 为 PascalCase（如 Name/Enable），
 * 而行数据（列表行 / Detail）为 camelCase（如 name/enable），直接按字段名精确取值会取不到 → 编辑弹窗回填为空。
 * 此处优先精确命中，其次翻转首字母（PascalCase ↔ camelCase），再兼容 ID/UUID 全大写场景。
 */
function resolveKey(key: string): string {
  if (props.modelValue && key in props.modelValue) return key;
  const flipped = toPascalAndCamel(key);
  if (props.modelValue && flipped !== key && flipped in props.modelValue) return flipped;
  if (props.modelValue && key === key.toUpperCase() && key !== key.toLowerCase()) {
    const lowerKey = key.toLowerCase();
    if (lowerKey in props.modelValue) return lowerKey;
  }
  if (props.modelValue && key === key.toLowerCase() && /[a-z]/.test(key)) {
    const upperKey = key.toUpperCase();
    if (upperKey in props.modelValue) return upperKey;
  }
  return key;
}

function getValue(key: string): unknown {
  return props.modelValue?.[resolveKey(key)];
}

/** 字段级校验错误：字段名 → 提示文案 */
const errors = ref<Record<string, string>>({});

/** 判断值是否为空（null/undefined/空白字符串/空数组） */
function isEmptyValue(value: unknown): boolean {
  if (value === null || value === undefined) return true;
  if (typeof value === 'string') return value.trim() === '';
  if (Array.isArray(value)) return value.length === 0;
  return false;
}

/**
 * 校验必填字段
 *
 * 表单提交前由父组件通过 `defineExpose` 调用；返回 false 时父组件应中止提交。
 * 仅校验当前可见字段，禁用（详情）模式直接通过。
 *
 * @returns 是否全部通过
 */
function validate(): boolean {
  const next: Record<string, string> = {};
  if (!props.disabled) {
    for (const field of visibleFields.value) {
      if (!isRequired(field)) continue;
      if (isEmptyValue(getValue(field.name))) {
        next[field.name] = t('form.fieldRequired', { field: field.displayName || field.name });
      }
    }
  }
  errors.value = next;
  return Object.keys(next).length === 0;
}

/** 清除单个字段的错误提示（用户修改后立即消失） */
function clearError(name: string): void {
  if (!errors.value[name]) return;
  const next = { ...errors.value };
  delete next[name];
  errors.value = next;
}

defineExpose({ validate });

/** 模板内类型收窄：不在模板里写 `as A | B`（竖线会被 vue/no-deprecated-filter 误判为过滤器） */
function asScalar(val: unknown): string | number | undefined {
  return val as string | number | undefined;
}

/** 地区级联的取值类型收窄 */
function asAreaValue(val: unknown): number | string | null {
  return val as number | string | null;
}

function toNumber(val: unknown): number | undefined {
  if (val === null || val === undefined || val === '') return undefined;
  const n = Number(val);
  return Number.isNaN(n) ? undefined : n;
}

/** 回写使用模型实际存在的键（保持行数据 camelCase），避免同字段大小写双键共存 */
function updateField(key: string, value: unknown) {
  clearError(key);
  emit('update:modelValue', { ...props.modelValue, [resolveKey(key)]: value });
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
    <!-- 分类 Tab：替换「基本信息」卡片标题，单分类时整个头部隐藏 -->
    <div v-if="useTabs && groups.length > 1" class="fmc-header fmc-header--tabs">
      <el-tabs v-model="activeGroupKey" class="fmc-group-tabs">
        <el-tab-pane
          v-for="group in groups"
          :key="group.key"
          :name="tabNameOf(group)"
          :label="groupTabTitle(group)"
        />
      </el-tabs>
    </div>
    <!-- 分区模式保留原来的卡片标题 -->
    <div v-else-if="!useTabs" class="fmc-header">
      <h2 class="fmc-title">{{ titleText }}</h2>
    </div>
    <div class="fmc-body">
      <section v-for="group in displayGroups" :key="group.key" class="fmc-group">
        <h3 v-if="!useTabs && group.title" class="fmc-group-title">{{ group.title }}</h3>
        <div class="fmc-grid" :style="{ gridTemplateColumns: `repeat(${props.columns}, 1fr)` }">
          <div
            v-for="field in group.fields"
            :key="field.name"
            class="fmc-field"
            :class="{ 'fmc-field--full': isFullWidthControl(controlOf(field)) }"
          >
          <label :for="`fmc-${field.name}`" class="fmc-label">
            {{ field.displayName || field.name }}
            <!-- 查看（禁用）模式下不展示必填星号：详情只读无填写语义 -->
            <span v-if="!props.disabled && isRequired(field)" class="fmc-required" aria-hidden="true">*</span>
            <!-- 字段说明：完整描述放 Tooltip，避免长文本挤占两列表单 -->
            <el-tooltip
              v-if="descriptionOf(field)"
              :content="descriptionOf(field)"
              placement="top"
              :show-after="200"
            >
              <el-icon class="fmc-help" :aria-label="descriptionOf(field)" tabindex="0">
                <QuestionFilled />
              </el-icon>
            </el-tooltip>
          </label>

          <!-- 字段级渲染器：后端声明 view，前端按名注册；未注册时回退内置控件 -->
          <component
            v-if="fieldRendererOf(field)"
            :is="fieldRendererOf(field)?.component"
            v-bind="fieldRendererOf(field)?.props"
            :field="field"
            :value="getValue(field.name)"
            :model="modelValue"
            :disabled="props.disabled"
            @update:value="(v: unknown) => updateField(field.name, v)"
          />

          <!-- 大文本 -->
          <el-input
            v-else-if="controlOf(field) === 'textarea'"
            :id="`fmc-${field.name}`"
            type="textarea"
            :rows="3"
            class="fmc-textarea"
            :disabled="props.disabled"
            :placeholder="hintOf(field, 'form.placeholderInput')"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <!-- 数值 -->
          <div v-else-if="controlOf(field) === 'inputNumber'" class="fmc-number">
            <span v-if="numberPrefix(field)" class="fmc-unit">{{ numberPrefix(field) }}</span>
            <el-input-number
              :id="`fmc-${field.name}`"
              class="fmc-input-number"
              :controls="true"
              :disabled="props.disabled"
              :precision="resolveNumberPrecision(field)"
              :step="resolveNumberStep(field)"
              :placeholder="hintOf(field, 'form.placeholderNumber')"
              :model-value="toNumber(getValue(field.name))"
              @update:model-value="(v: number | undefined) => updateNumber(field.name, v)"
            />
            <span v-if="numberSuffix(field)" class="fmc-unit">{{ numberSuffix(field) }}</span>
          </div>

          <!-- 枚举 / 字典下拉（后端下发 dataSource） -->
          <el-select
            v-else-if="controlOf(field) === 'select'"
            class="fmc-select"
            :placeholder="hintOf(field, 'form.placeholderSelect')"
            :model-value="asScalar(getValue(field.name))"
            @update:model-value="(v: string | number) => updateField(field.name, v)"
          >
            <el-option
              v-for="(label, key) in field.dataSource || {}"
              :key="key"
              :label="label"
              :value="enumOptionValue(key)"
            />
          </el-select>

          <!-- 布尔开关 -->
          <div v-else-if="controlOf(field) === 'switch'" class="fmc-switch-wrapper">
            <el-switch
              :disabled="props.disabled"
              :model-value="Boolean(getValue(field.name))"
              @change="(v: string | number | boolean) => updateField(field.name, v)"
            />
          </div>

          <!-- 日期时间。ItemType=date 时只选日期 -->
          <el-date-picker
            v-else-if="controlOf(field) === 'datePicker'"
            class="fmc-date-picker"
            :type="isDateOnly(field) ? 'date' : 'datetime'"
            :disabled="props.disabled"
            :placeholder="isDateOnly(field) ? t('form.placeholderDate') : t('form.placeholderDateTime')"
            :format="isDateOnly(field) ? 'YYYY-MM-DD' : 'YYYY-MM-DD HH:mm:ss'"
            :value-format="isDateOnly(field) ? 'YYYY-MM-DD' : 'YYYY-MM-DDTHH:mm:ss'"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <!-- 时间 -->
          <el-time-picker
            v-else-if="controlOf(field) === 'timePicker'"
            class="fmc-date-picker"
            :disabled="props.disabled"
            :placeholder="hintOf(field, 'form.placeholderTime')"
            format="HH:mm:ss"
            value-format="HH:mm:ss"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <!-- 地区级联（ItemType=area1~4） -->
          <AreaSelect
            v-else-if="controlOf(field) === 'area'"
            :model-value="asAreaValue(getValue(field.name))"
            :level="areaLevel(field)"
            :disabled="props.disabled"
            :placeholder="hintOf(field, 'form.placeholderArea')"
            @update:model-value="(v: number | undefined) => updateField(field.name, v)"
          />

          <!-- 值集单选 -->
          <LovSelect
            v-else-if="controlOf(field) === 'lov'"
            :code="field.lovCode || ''"
            :disabled="props.disabled"
            :parent="field.parentField ? (getValue(field.parentField) as string | number | undefined) : undefined"
            :model-value="(getValue(field.name) as string | number | undefined)"
            :placeholder="hintOf(field, 'form.placeholderSelect')"
            @update:model-value="(v: string | number | Array<string | number> | undefined) => updateField(field.name, v)"
          />

          <!-- 值集多选 -->
          <LovSelect
            v-else-if="controlOf(field) === 'lovMulti'"
            :code="field.lovCode || ''"
            :multiple="true"
            :disabled="props.disabled"
            :parent="field.parentField ? (getValue(field.parentField) as string | number | undefined) : undefined"
            :model-value="toMultiArray(getValue(field.name))"
            :placeholder="hintOf(field, 'form.placeholderMultiSelect')"
            @update:model-value="(v: string | number | Array<string | number> | undefined) => updateField(field.name, v)"
          />

          <!-- 文件上传 -->
          <Uploader
            v-else-if="controlOf(field) === 'upload'"
            kind="file"
            :api-prefix="apiPrefix"
            :disabled="props.disabled"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <!-- 图片上传 -->
          <Uploader
            v-else-if="controlOf(field) === 'image'"
            kind="image"
            :api-prefix="apiPrefix"
            :disabled="props.disabled"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <!-- Json 编辑器 -->
          <JsonEditor
            v-else-if="controlOf(field) === 'json'"
            :disabled="props.disabled"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <!-- 富文本 HTML -->
          <RichEditor
            v-else-if="controlOf(field) === 'richHtml'"
            mode="html"
            :disabled="props.disabled"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <!-- 富文本 Markdown -->
          <RichEditor
            v-else-if="controlOf(field) === 'richMarkdown'"
            mode="markdown"
            :disabled="props.disabled"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <!-- 颜色选择器 -->
          <ColorPicker
            v-else-if="controlOf(field) === 'color'"
            :disabled="props.disabled"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <!-- 图标选择器 -->
          <IconSelector
            v-else-if="controlOf(field) === 'icon'"
            :disabled="props.disabled"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <!-- 邮箱 / 手机 / 网址（带格式校验） -->
          <el-input
            v-else-if="controlOf(field) === 'email' || controlOf(field) === 'tel' || controlOf(field) === 'url'"
            :id="`fmc-${field.name}`"
            class="fmc-input"
            :disabled="props.disabled"
            :type="controlOf(field) === 'email' ? 'email' : controlOf(field) === 'tel' ? 'tel' : 'url'"
            :placeholder="hintOf(field, 'form.placeholderInput')"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <!-- 密码（ItemType=password，对齐 MVC _Form_String 的遮蔽输入） -->
          <el-input
            v-else-if="controlOf(field) === 'password'"
            :id="`fmc-${field.name}`"
            class="fmc-input"
            :disabled="props.disabled"
            type="password"
            show-password
            autocomplete="new-password"
            :placeholder="hintOf(field, 'form.placeholderInput')"
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
            :disabled="props.disabled"
            :placeholder="hintOf(field, 'form.placeholderInput')"
            :model-value="String(getValue(field.name) ?? '')"
            @update:model-value="(v: string) => updateField(field.name, v)"
          />

          <div v-if="errors[field.name]" class="fmc-error" role="alert">{{ errors[field.name] }}</div>
          </div>
        </div>
      </section>
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

/* 分类 Tab 模式：Tab 条就是卡片头部，隐藏多余的下边框线 */
.fmc-header--tabs {
  padding: 6px 18px 0;
}

.fmc-group-tabs :deep(.el-tabs__header) {
  margin: 0;
}

.fmc-group-tabs :deep(.el-tabs__nav-wrap::after) {
  display: none;
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

.fmc-group + .fmc-group {
  margin-top: 22px;
  padding-top: 18px;
  border-top: 1px solid var(--el-border-color-lighter);
}

.fmc-group-title {
  font-size: 13px;
  font-weight: 600;
  color: var(--el-text-color-primary);
  margin: 0 0 14px;
}

.fmc-grid {
  display: grid;
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

.fmc-error {
  margin-top: 4px;
  color: var(--el-color-danger);
  font-size: 12px;
  line-height: 1.4;
}

/* 字段说明问号：完整描述走 Tooltip，不占用两列表单的横向空间 */
.fmc-help {
  margin-left: 4px;
  color: var(--el-text-color-placeholder);
  font-size: 14px;
  vertical-align: -3px;
  cursor: help;
  outline: none;

  &:hover,
  &:focus-visible {
    color: var(--el-color-primary);
  }
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

.fmc-number {
  display: flex;
  align-items: center;
  gap: 6px;
}

.fmc-number .fmc-input-number {
  flex: 1;
  width: 100%;
}

.fmc-unit {
  flex-shrink: 0;
  font-size: 12px;
  color: var(--el-text-color-secondary);
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

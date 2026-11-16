<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import type { PageMeta } from '@newlifex/api-core';
import type { FieldMapping } from '@newlifex/field-mapping';
import { resolveWidgets } from '@newlifex/field-mapping';
import { groupFieldsByCategory } from '@newlifex/page-logic';
import { cubeApi } from '@/api/cube';
import { $t } from '@/locales';
import { getRowValue } from '../shared';

defineOptions({ name: 'SettingsForm' });

interface Props {
  /** 控制器名，如 Core、Cube */
  controller: string;
  /** 当前配置页的 /meta 载荷 */
  meta?: PageMeta;
}

const props = defineProps<Props>();

const apiPrefix = computed(() => `/Admin/${props.controller}`);
const fields = computed<FieldMapping[]>(() => {
  const form = props.meta?.editForm ?? props.meta?.addForm ?? [];
  return resolveWidgets(form.filter(field => !field.primaryKey && !field.readOnly));
});
const groups = computed(() => groupFieldsByCategory(fields.value, item => item.field.category));
const activeTab = ref('0');

const loading = ref(false);
const saving = ref(false);
const loaded = ref(false);
const error = ref(false);
// 控件类型由字段元数据决定（文本/数字/布尔/数组），统一用宽松类型承载
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const model = ref<Record<string, any>>({});

const wideWidgets = new Set(['textarea', 'html', 'markdown', 'json', 'code']);

function fieldLabel(item: FieldMapping) {
  return item.field.displayName || item.field.header || item.field.name;
}

function tabLabel(category: string) {
  return category || $t('page.form.basicInfo');
}

function isWide(item: FieldMapping) {
  return wideWidgets.has(item.widget);
}

/** 后端枚举允许空字符串表示「自动/默认」时，空串是合法选项，不能当作未选中 */
function hasEmptyOption(item: FieldMapping) {
  return !!item.field.dataSource && Object.prototype.hasOwnProperty.call(item.field.dataSource, '');
}

function emptyValuesOf(item: FieldMapping) {
  return hasEmptyOption(item) ? [null, undefined] : undefined;
}

function optionsOf(item: FieldMapping) {
  if (!item.field.dataSource) return [];
  return Object.entries(item.field.dataSource).map(([value, text]) => ({ value, label: String(text) }));
}

function isDropdown(item: FieldMapping) {
  return ['select', 'lov', 'lovMulti'].includes(item.widget) && optionsOf(item).length > 0;
}

/** 记录值 → 控件值 */
function normalizeForControl(item: FieldMapping, raw: unknown): unknown {
  if (raw === null || raw === undefined) {
    if (item.widget === 'switch') return false;
    if (item.widget === 'number') return undefined;
    return '';
  }
  switch (item.widget) {
    case 'switch':
      return Boolean(raw);
    case 'number': {
      const num = Number(raw);
      return Number.isNaN(num) ? undefined : num;
    }
    case 'date':
      return String(raw).slice(0, 10);
    case 'datetime':
      return String(raw).slice(0, 19).replace(' ', 'T');
    case 'time': {
      const text = String(raw);
      return text.length > 8 ? text.slice(-8) : text;
    }
    case 'select':
    case 'lov':
      return String(raw);
    default:
      return String(raw);
  }
}

/** 控件值 → 提交值 */
function serializeValue(item: FieldMapping, value: unknown): unknown {
  if (item.widget === 'switch') return value === true;
  if (item.widget === 'number') return typeof value === 'number' ? value : undefined;
  if (item.widget === 'select' || item.widget === 'lov') {
    if (value === undefined || value === null || value === '') {
      // 有空值选项时显式提交空串（如界面语言=自动），否则视为未修改
      return hasEmptyOption(item) ? '' : undefined;
    }
    // 枚举与数值字典需按数字提交，字符串字段保持原样
    const typeName = (item.field.typeName || '').toLowerCase();
    return typeName !== 'string' && /^-?\d+$/.test(String(value)) ? Number(value) : String(value);
  }
  if (value === undefined || value === null) return undefined;
  return value;
}

async function load() {
  loading.value = true;
  error.value = false;
  loaded.value = false;
  try {
    const res = await cubeApi.client.get(apiPrefix.value);
    const data = (res.data?.data ?? res.data ?? {}) as Record<string, unknown>;
    const next: Record<string, unknown> = {};
    for (const item of fields.value) {
      next[item.field.name] = normalizeForControl(item, getRowValue(data, item.field.name));
    }
    model.value = next;
    loaded.value = true;
  } catch {
    error.value = true;
  } finally {
    loading.value = false;
  }
}

async function save() {
  saving.value = true;
  try {
    const payload: Record<string, unknown> = {};
    for (const item of fields.value) {
      const value = serializeValue(item, model.value[item.field.name]);
      if (value !== undefined) payload[item.field.name] = value;
    }
    await cubeApi.client.put(apiPrefix.value, payload);
    ElMessage.success($t('settingsCenter.saveSuccess'));
  } catch {
    // 请求层已提示错误
  } finally {
    saving.value = false;
  }
}

onMounted(load);
</script>

<template>
  <ElCard v-loading="loading" shadow="never" class="settings-form-card">
    <div v-if="error" class="settings-form__unavailable">{{ $t('settingsCenter.unavailable') }}</div>
    <template v-else-if="loaded">
      <ElForm :model="model" label-width="150px" label-position="right" class="settings-form">
        <ElTabs v-model="activeTab" :class="{ 'settings-form__tabs--plain': groups.length <= 1 }">
          <ElTabPane
            v-for="(group, groupIndex) in groups"
            :key="group.category"
            :label="tabLabel(group.category)"
            :name="String(groupIndex)"
          >
            <div class="settings-form__grid">
              <ElFormItem
                v-for="item in group.fields"
                :key="item.field.name"
                :label="fieldLabel(item)"
                :class="{ 'settings-form__item--wide': isWide(item) }"
              >
                <ElInput
                  v-if="item.widget === 'password'"
                  v-model="model[item.field.name]"
                  type="password"
                  show-password
                  autocomplete="new-password"
                  :placeholder="item.field.description"
                />
                <ElInput
                  v-else-if="isWide(item)"
                  v-model="model[item.field.name]"
                  type="textarea"
                  :rows="3"
                  :placeholder="item.field.description"
                />
                <ElInputNumber
                  v-else-if="item.widget === 'number'"
                  v-model="model[item.field.name]"
                  class="w-full"
                  controls-position="right"
                />
                <ElSwitch v-else-if="item.widget === 'switch'" v-model="model[item.field.name]" />
                <ElDatePicker
                  v-else-if="item.widget === 'date'"
                  v-model="model[item.field.name]"
                  type="date"
                  value-format="YYYY-MM-DD"
                  class="w-full"
                />
                <ElDatePicker
                  v-else-if="item.widget === 'datetime'"
                  v-model="model[item.field.name]"
                  type="datetime"
                  value-format="YYYY-MM-DDTHH:mm:ss"
                  class="w-full"
                />
                <ElTimePicker
                  v-else-if="item.widget === 'time'"
                  v-model="model[item.field.name]"
                  value-format="HH:mm:ss"
                  class="w-full"
                />
                <ElSelect
                  v-else-if="isDropdown(item)"
                  v-model="model[item.field.name]"
                  :empty-values="emptyValuesOf(item)"
                  filterable
                  clearable
                  class="w-full"
                >
                  <ElOption
                    v-for="option in optionsOf(item)"
                    :key="option.value"
                    :label="option.label"
                    :value="option.value"
                  />
                </ElSelect>
                <ElInput
                  v-else
                  v-model="model[item.field.name]"
                  :placeholder="item.field.description || item.field.name"
                />
              </ElFormItem>
            </div>
          </ElTabPane>
        </ElTabs>
      </ElForm>

      <div class="settings-form__footer">
        <ElButton :disabled="saving" @click="load">{{ $t('common.reset') }}</ElButton>
        <ElButton type="primary" :loading="saving" @click="save">{{ $t('settingsCenter.save') }}</ElButton>
      </div>
    </template>
  </ElCard>
</template>

<style scoped>
.settings-form-card {
  min-height: 240px;
}

.settings-form {
  max-width: 1080px;
}

.settings-form__tabs--plain :deep(.el-tabs__header) {
  display: none;
}

.settings-form__grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0 24px;
}

@media (max-width: 900px) {
  .settings-form__grid {
    grid-template-columns: minmax(0, 1fr);
  }
}

.settings-form__item--wide {
  grid-column: 1 / -1;
}

.settings-form__footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px solid var(--el-border-color-lighter);
}

.settings-form__unavailable {
  padding: 48px 0;
  color: var(--el-text-color-secondary);
  text-align: center;
}
</style>

<script setup lang="ts">
import { ref, reactive, computed } from 'vue';
import { ArrowDown, ArrowUp, Search } from '@element-plus/icons-vue';

interface SelectOption {
  value: string | number;
  label: string;
}

type FieldType =
  | 'text'
  | 'select'
  | 'multi-select'
  | 'number-range'
  | 'date-range';

interface SearchField {
  key: string;
  label: string;
  type: FieldType;
  placeholder?: string;
  options?: SelectOption[];
  span?: 1 | 2 | 3 | 4;
}

interface Props {
  fields?: SearchField[];
  visibleRows?: number;
}

const props = withDefaults(defineProps<Props>(), {
  fields: () =>
    [
      { key: 'keyword', label: '关键词', type: 'text' },
      {
        key: 'status',
        label: '状态',
        type: 'select',
        options: [
          { value: '', label: '全部' },
          { value: 'active', label: '正常' },
          { value: 'disabled', label: '禁用' },
        ],
      },
    ] satisfies SearchField[],
  visibleRows: 1,
});

const emit = defineEmits<{
  search: [params: Record<string, unknown>];
  reset: [];
}>();

const COLS = 4;

const collapsed = ref(true);
const formData = reactive<Record<string, unknown>>({});

const visibleFields = computed(() => {
  if (!collapsed.value) return props.fields;
  const limit = props.visibleRows * COLS;
  let used = 0;
  const result: SearchField[] = [];
  for (const f of props.fields) {
    const span = f.span ?? 1;
    if (used + span > limit) break;
    result.push(f);
    used += span;
  }
  return result;
});

const hasMore = computed(() => {
  const limit = props.visibleRows * COLS;
  let used = 0;
  for (const f of props.fields) {
    used += f.span ?? 1;
    if (used > limit) return true;
  }
  return false;
});

function handleSearch() {
  emit('search', { ...formData });
}

function handleReset() {
  for (const key in formData) {
    delete formData[key];
  }
  emit('reset');
}

function toggleCollapse() {
  collapsed.value = !collapsed.value;
}
</script>

<template>
  <div class="list-search-bar">
    <div class="lsb-body">
      <div class="lsb-grid">
        <template v-for="field in visibleFields" :key="field.key">
          <div
            class="lsb-field"
            :style="field.span && field.span > 1 ? { gridColumn: `span ${field.span}` } : undefined"
          >
            <label class="lsb-label">{{ field.label }}</label>
            <el-input
              v-if="field.type === 'text'"
              v-model="formData[field.key] as string"
              :placeholder="field.placeholder ?? `请输入${field.label}`"
              clearable
              class="lsb-input"
              @keyup.enter="handleSearch"
            />
            <el-select
              v-else-if="field.type === 'select'"
              v-model="formData[field.key]"
              :placeholder="field.placeholder ?? `请选择`"
              clearable
              class="lsb-input"
            >
              <el-option
                v-for="opt in field.options"
                :key="opt.value"
                :label="opt.label"
                :value="opt.value"
              />
            </el-select>
            <el-select
              v-else-if="field.type === 'multi-select'"
              v-model="formData[field.key]"
              :placeholder="field.placeholder ?? `请选择`"
              clearable
              multiple
              collapse-tags
              collapse-tags-tooltip
              class="lsb-input"
            >
              <el-option
                v-for="opt in field.options"
                :key="opt.value"
                :label="opt.label"
                :value="opt.value"
              />
            </el-select>
            <div v-else-if="field.type === 'number-range'" class="lsb-range">
              <el-input-number
                v-model="formData[`${field.key}_min`] as number"
                placeholder="最小值"
                :controls="false"
                class="lsb-range-num"
              />
              <span class="lsb-range-sep">~</span>
              <el-input-number
                v-model="formData[`${field.key}_max`] as number"
                placeholder="最大值"
                :controls="false"
                class="lsb-range-num"
              />
            </div>
            <el-date-picker
              v-else-if="field.type === 'date-range'"
              v-model="formData[field.key]"
              type="daterange"
              range-separator="~"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
              value-format="YYYY-MM-DD"
              class="lsb-input lsb-datepicker"
            />
          </div>
        </template>
      </div>
      <div class="lsb-actions">
        <el-button v-if="hasMore" class="lsb-btn lsb-btn--ghost" @click="toggleCollapse">
          <el-icon><ArrowUp v-if="!collapsed" /><ArrowDown v-else /></el-icon>
          {{ collapsed ? '更多' : '收起' }}
        </el-button>
        <el-button class="lsb-btn lsb-btn--secondary" @click="handleReset">重置</el-button>
        <el-button type="primary" class="lsb-btn lsb-btn--primary" @click="handleSearch">
          <el-icon><Search /></el-icon>查询
        </el-button>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.list-search-bar {
  width: 100%;
}

.lsb-body {
  display: flex;
  align-items: flex-start;
  gap: 16px;
}

.lsb-grid {
  flex: 1;
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 12px 16px;
  min-width: 0;
}

.lsb-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-width: 0;
}

.lsb-label {
  font-family: var(--el-font-family);
  font-size: 12px;
  font-weight: 500;
  line-height: 1.3;
  color: var(--el-text-color-secondary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.lsb-input {
  width: 100%;

  :deep(.el-input__wrapper),
  :deep(.el-select__wrapper) {
    min-height: 36px;
    border-radius: var(--el-border-radius-base);
    background: var(--el-bg-color);
    border: 1px solid var(--el-border-color-light);
    box-shadow: none;
    transition:
      border-color 0.15s ease,
      background 0.15s ease,
      box-shadow 0.15s ease;
  }

  :deep(.el-input__wrapper.is-focus),
  :deep(.el-select__wrapper.is-focused) {
    background: var(--el-fill-color-blank);
    border-color: var(--el-color-primary);
    box-shadow:
      0 0 0 1px var(--el-color-primary) inset,
      0 0 0 3px var(--el-color-primary-light-8);
  }

  :deep(.el-input__inner),
  :deep(.el-select__selected-item) {
    font-family: var(--el-font-family);
    font-size: 13px;
    color: var(--el-text-color-primary);
  }

  :deep(.el-input__inner::placeholder) {
    color: var(--el-text-color-placeholder);
  }
}

.lsb-datepicker {
  :deep(.el-date-editor) {
    width: 100%;
  }

  :deep(.el-input__wrapper) {
    min-height: 36px;
    border-radius: var(--el-border-radius-base);
    background: var(--el-bg-color);
    border: 1px solid var(--el-border-color-light);
  }

  :deep(.el-input__wrapper.is-focus) {
    border-color: var(--el-color-primary);
    box-shadow:
      0 0 0 1px var(--el-color-primary) inset,
      0 0 0 3px var(--el-color-primary-light-8);
  }
}

.lsb-range {
  display: flex;
  align-items: center;
  gap: 8px;
}

.lsb-range-num {
  flex: 1;
  min-width: 0;

  :deep(.el-input__wrapper) {
    min-height: 36px;
    border-radius: var(--el-border-radius-base);
    background: var(--el-bg-color);
    border: 1px solid var(--el-border-color-light);
  }

  :deep(.el-input__wrapper.is-focus) {
    background: var(--el-fill-color-blank);
    border-color: var(--el-color-primary);
    box-shadow:
      0 0 0 1px var(--el-color-primary) inset,
      0 0 0 3px var(--el-color-primary-light-8);
  }

  :deep(.el-input__inner) {
    font-family: var(--el-font-family-mono);
    font-size: 13px;
    color: var(--el-text-color-primary);
    text-align: center;
  }
}

.lsb-range-sep {
  flex-shrink: 0;
  font-size: 13px;
  font-weight: 500;
  color: var(--el-text-color-secondary);
  line-height: 1;
}

.lsb-actions {
  flex-shrink: 0;
  display: flex;
  flex-direction: row;
  align-items: center;
  gap: 8px;
  padding-top: 22px;
}

.lsb-btn {
  min-height: 36px;
  padding: 0 16px;
  border-radius: var(--el-border-radius-base);
  font-family: var(--el-font-family);
  font-size: 13px;
  font-weight: 500;
  transition: all 0.15s ease;

  &--primary {
    --el-button-bg-color: var(--el-color-primary);
    --el-button-border-color: var(--el-color-primary);
    --el-button-hover-bg-color: var(--el-color-primary-dark-2);
    --el-button-hover-border-color: var(--el-color-primary-dark-2);
    --el-button-active-bg-color: var(--el-color-primary-dark-2);
    --el-button-active-border-color: var(--el-color-primary-dark-2);
    --el-button-text-color: var(--el-color-white);
  }

  &--secondary {
    --el-button-bg-color: transparent;
    --el-button-border-color: var(--el-border-color-light);
    --el-button-text-color: var(--el-text-color-regular);
    --el-button-hover-bg-color: var(--el-color-primary-light-9);
    --el-button-hover-border-color: var(--el-color-primary);
    --el-button-hover-text-color: var(--el-color-primary);
    --el-button-active-bg-color: var(--el-color-primary-light-8);
    --el-button-active-border-color: var(--el-color-primary);
    --el-button-active-text-color: var(--el-color-primary);
    margin-left: 0;
  }

  &--ghost {
    --el-button-bg-color: transparent;
    --el-button-border-color: var(--el-border-color-lighter);
    --el-button-text-color: var(--el-text-color-secondary);
    --el-button-hover-bg-color: var(--el-bg-color);
    --el-button-hover-border-color: var(--el-border-color);
    --el-button-hover-text-color: var(--el-text-color-regular);
    margin-left: 0;
  }
}

@media (max-width: 900px) {
  .lsb-body {
    flex-direction: column;
  }

  .lsb-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .lsb-actions {
    padding-top: 0;
    align-self: flex-end;
  }
}

@media (max-width: 560px) {
  .lsb-grid {
    grid-template-columns: 1fr;
  }
}
</style>

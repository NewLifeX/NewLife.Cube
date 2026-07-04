<script setup lang="ts">
import { ref, reactive, computed } from 'vue';
import { ArrowDown, ArrowUp, Search } from '@element-plus/icons-vue';

interface SelectOption {
  value: string | number;
  label: string;
}

/** 筛选字段类型 */
type FieldType =
  | 'text' // 普通文本输入
  | 'select' // 单选下拉
  | 'multi-select' // 多选下拉（标签式）
  | 'number-range' // 数字范围：最小值 ~ 最大值
  | 'date-range'; // 日期范围：开始 ~ 结束

interface SearchField {
  key: string;
  label: string;
  type: FieldType;
  placeholder?: string;
  options?: SelectOption[];
  /** 占用的列数，默认 1，最大 4 */
  span?: 1 | 2 | 3 | 4;
}

interface Props {
  fields?: SearchField[];
  /** 折叠状态下显示的行数，默认 1 */
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

/** 折叠时最多显示的字段数（按 span 累计列数，不超过 visibleRows * COLS） */
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
      <!-- 字段网格 -->
      <div class="lsb-grid">
        <template v-for="field in visibleFields" :key="field.key">
          <div
            class="lsb-field"
            :style="field.span && field.span > 1 ? { gridColumn: `span ${field.span}` } : undefined"
          >
            <label class="lsb-label">{{ field.label }}</label>

            <!-- 文本输入 -->
            <el-input
              v-if="field.type === 'text'"
              v-model="formData[field.key] as string"
              :placeholder="field.placeholder ?? `请输入${field.label}`"
              clearable
              class="lsb-input"
              @keyup.enter="handleSearch"
            />

            <!-- 单选下拉 -->
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

            <!-- 多选下拉 -->
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

            <!-- 数字范围 -->
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

            <!-- 日期范围 -->
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

      <!-- 操作按钮区（固定右侧，与第一行对齐） -->
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
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-light);
  border-radius: var(--el-border-radius-base);
  box-shadow: var(--el-box-shadow-light);
  padding: 12px 16px;
}

/* ── 主体：字段网格 + 右侧操作按钮 ── */
.lsb-body {
  display: flex;
  align-items: flex-start;
  gap: 12px;
}

/* ── 4列字段网格 ── */
.lsb-grid {
  flex: 1;
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 8px 12px;
  min-width: 0;
}

/* ── 单个字段 ── */
.lsb-field {
  display: flex;
  flex-direction: column;
  gap: 4px;
  min-width: 0;
}

.lsb-label {
  font-family: 'Fira Sans', system-ui, sans-serif;
  font-size: 12px;
  font-weight: 500;
  line-height: 1.2;
  color: var(--el-text-color-secondary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* ── 通用输入控件 ── */
.lsb-input {
  width: 100%;

  :deep(.el-input__wrapper),
  :deep(.el-select__wrapper) {
    min-height: 34px;
    border-radius: 6px;
    background: var(--el-bg-color);
    box-shadow: 0 0 0 1px var(--el-border-color-light) inset;
    transition:
      box-shadow 0.15s ease,
      background 0.15s ease;
  }

  :deep(.el-input__wrapper.is-focus),
  :deep(.el-select__wrapper.is-focused) {
    background: var(--el-fill-color-blank);
    box-shadow:
      0 0 0 1px var(--el-color-primary) inset,
      0 0 0 3px rgba(29, 112, 64, 0.08);
  }

  :deep(.el-input__inner),
  :deep(.el-select__selected-item) {
    font-family: 'Fira Sans', system-ui, sans-serif;
    font-size: 13px;
    color: var(--el-text-color-primary);
  }
}

.lsb-datepicker {
  :deep(.el-date-editor) {
    width: 100%;
  }
}

/* ── 数字范围 ── */
.lsb-range {
  display: flex;
  align-items: center;
  gap: 6px;
}

.lsb-range-num {
  flex: 1;
  min-width: 0;

  :deep(.el-input__wrapper) {
    min-height: 34px;
    border-radius: 6px;
    background: var(--el-bg-color);
    box-shadow: 0 0 0 1px var(--el-border-color-light) inset;
  }

  :deep(.el-input__wrapper.is-focus) {
    background: var(--el-fill-color-blank);
    box-shadow:
      0 0 0 1px var(--el-color-primary) inset,
      0 0 0 3px rgba(29, 112, 64, 0.08);
  }

  :deep(.el-input__inner) {
    font-family: 'JetBrains Mono', 'Fira Code', monospace;
    font-size: 13px;
    color: var(--el-text-color-primary);
    text-align: center;
  }
}

.lsb-range-sep {
  flex-shrink: 0;
  font-size: 13px;
  color: var(--el-text-color-secondary);
  line-height: 1;
}

/* ── 操作按钮（右侧竖排或横排） ── */
.lsb-actions {
  flex-shrink: 0;
  display: flex;
  flex-direction: row;
  align-items: center;
  gap: 8px;
  padding-top: 20px; /* 与 label 高度对齐 */
}

.lsb-btn {
  min-height: 34px;
  padding: 0 14px;
  border-radius: 6px;
  font-family: 'Fira Sans', system-ui, sans-serif;
  font-size: 13px;
  font-weight: 500;

  &--primary {
    --el-button-bg-color: var(--el-color-primary);
    --el-button-border-color: var(--el-color-primary);
    --el-button-hover-bg-color: var(--el-color-primary-dark-2);
    --el-button-hover-border-color: var(--el-color-primary-dark-2);
    --el-button-active-bg-color: var(--el-color-primary);
    --el-button-active-border-color: var(--el-color-primary);
    --el-button-text-color: var(--el-color-white);
  }

  &--secondary {
    --el-button-bg-color: var(--el-fill-color-blank);
    --el-button-border-color: var(--el-border-color-light);
    --el-button-text-color: var(--el-text-color-regular);
    --el-button-hover-bg-color: var(--el-color-primary-light-9);
    --el-button-hover-border-color: var(--el-color-primary-light-8);
    --el-button-hover-text-color: var(--el-color-primary);
    --el-button-active-bg-color: var(--el-color-primary-light-8);
    --el-button-active-border-color: var(--el-color-primary-light-8);
    --el-button-active-text-color: var(--el-color-primary);
    margin-left: 0;
  }

  &--ghost {
    --el-button-bg-color: transparent;
    --el-button-border-color: var(--el-border-color-light);
    --el-button-text-color: var(--el-text-color-regular);
    --el-button-hover-bg-color: var(--el-bg-color);
    --el-button-hover-border-color: var(--el-color-primary-light-8);
    --el-button-hover-text-color: var(--el-color-primary);
    margin-left: 0;
  }
}

/* ── 响应式：小屏收为单列 ── */
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

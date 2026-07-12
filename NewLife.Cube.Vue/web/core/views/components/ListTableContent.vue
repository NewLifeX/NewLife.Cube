<script setup lang="ts">
/**
 * 列表表格内容组件
 *
 * 入参 `fields` 为后端下发的 `FieldMeta[]`，每列按 `resolveListControl` 解析渲染类型，
 * 渲染色块 / 图标 / 缩略图 / Json 折叠 / 富文本摘要 / 开关 / 时间 / LOV 翻译标签等
 * （不再仅渲染纯文本）。
 *
 * 兼容旧版 `columns` 入参（仅文本渲染）。
 */
import { ref, reactive, computed, watch } from 'vue';
import { getValueByKey, resolveAssetUrl } from '@newlifex/cube-vue/core/utils/url';
import { resolveListControl } from '@newlifex/cube-vue/core/utils/fieldControl';
import { fetchBatchLabel } from '@newlifex/cube-vue/core/utils/lov-api';
import * as ElementPlusIconsVue from '@element-plus/icons-vue';
import type { FieldMeta, ListControlType } from '@newlifex/cube-vue/core/types/field';

interface Column {
  key: string;
  label: string;
  width?: string;
  align?: 'left' | 'center' | 'right';
  mono?: boolean;
  render?: (row: Record<string, unknown>) => string;
}

interface InternalColumn {
  key: string;
  label: string;
  width?: string;
  align?: 'left' | 'center' | 'right';
  mono?: boolean;
  control: ListControlType;
  field?: FieldMeta;
}

interface Props {
  fields?: FieldMeta[];
  columns?: Column[];
  data?: Record<string, unknown>[];
  loading?: boolean;
  selectable?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  fields: () => [],
  columns: () => [],
  data: () => [],
  loading: false,
  selectable: false,
});

const emit = defineEmits<{
  edit: [row: Record<string, unknown>];
  delete: [row: Record<string, unknown>];
}>();

/** 计算列宽：主键窄列、数值右对齐、图片/文件窄列 */
function columnWidth(field: FieldMeta, control: ListControlType): string | undefined {
  if (field.primaryKey) return '80px';
  if (control === 'image' || control === 'file') return '100px';
  if (control === 'color' || control === 'icon') return '90px';
  if (control === 'boolean') return '90px';
  return undefined;
}

function columnAlign(field: FieldMeta, control: ListControlType): 'left' | 'center' | 'right' {
  if (control === 'number') return 'right';
  if (control === 'boolean' || control === 'color' || control === 'icon' || control === 'image') {
    return 'center';
  }
  if (field.primaryKey) return 'center';
  return 'left';
}

/** 归一为内部列结构 */
const renderColumns = computed<InternalColumn[]>(() => {
  const srcFields = props.fields ?? [];
  if (srcFields.length > 0) {
    return srcFields.map((f) => {
      const control = resolveListControl(f);
      return {
        key: f.name,
        label: f.displayName || f.name,
        width: columnWidth(f, control),
        align: columnAlign(f, control),
        mono: f.primaryKey,
        control,
        field: f,
      };
    });
  }
  const srcCols = props.columns ?? [];
  if (srcCols.length > 0) {
    return srcCols.map((c) => ({
      key: c.key,
      label: c.label,
      width: c.width,
      align: c.align ?? 'left',
      mono: c.mono,
      control: 'text' as ListControlType,
    }));
  }
  return [
    { key: 'id', label: 'ID', width: '80px', align: 'center', mono: true, control: 'readonly' },
    { key: 'name', label: '名称', control: 'text' },
    { key: 'status', label: '状态', control: 'text' },
  ];
});

// ── LOV 翻译（BatchLabel）──
const lovLabels = reactive<Record<string, string>>({});

/** 多值归一：数组 / 逗号串 / 单值 → string[] */
function normalizeMulti(raw: unknown): string[] {
  if (raw == null || raw === '') return [];
  if (Array.isArray(raw)) return raw.map((v) => String(v));
  if (typeof raw === 'string') {
    // 兼容 "1,2" 或 JSON 数组串
    const s = raw.trim();
    if (s.startsWith('[')) {
      try {
        const arr = JSON.parse(s);
        if (Array.isArray(arr)) return arr.map((v) => String(v));
      } catch {
        /* ignore */
      }
    }
    return s.split(',').map((x) => x.trim()).filter(Boolean);
  }
  return [String(raw)];
}

/** 拉取所有 LOV 列的翻译字典 */
async function loadLovLabels() {
  const tasks: Promise<void>[] = [];
  for (const col of renderColumns.value) {
    if (col.control !== 'lov' || !col.field?.lovCode) continue;
    const lovCode = col.field.lovCode;
    const values = new Set<string>();
    for (const row of props.data ?? []) {
      const raw = getValueByKey(row, col.key);
      for (const v of normalizeMulti(raw)) values.add(v);
    }
    const uncached = [...values].filter((v) => !(lovCode + ':' + v in lovLabels));
    if (uncached.length === 0) continue;
    tasks.push(
      (async () => {
        try {
          const map = await fetchBatchLabel({ lovCode, values: uncached });
          for (const [k, label] of Object.entries(map)) lovLabels[lovCode + ':' + k] = label;
        } catch (e) {
          console.error('ListTableContent: BatchLabel 失败', e);
        }
      })(),
    );
  }
  await Promise.all(tasks);
}

watch([() => props.data, () => props.fields], loadLovLabels, { immediate: true });

function getLovLabel(lovCode: string, value: unknown): string {
  const key = lovCode + ':' + String(value);
  return lovLabels[key] ?? String(value ?? '');
}

// ── 单元格取值辅助 ──
function getCellText(row: Record<string, unknown>, col: InternalColumn): string {
  const raw = getValueByKey(row, col.key);
  if (raw == null) return '';
  if (col.control === 'lov' && col.field?.lovCode) {
    return normalizeMulti(raw)
      .map((v) => getLovLabel(col.field!.lovCode!, v))
      .join('、');
  }
  return String(raw);
}

const iconCache = ref<Record<string, unknown>>({});
function resolveIcon(name: string): unknown {
  if (!name) return null;
  if (!iconCache.value[name]) {
    iconCache.value[name] = (ElementPlusIconsVue as Record<string, unknown>)[name] ?? null;
  }
  return iconCache.value[name];
}

function getIconName(col: InternalColumn, row: Record<string, unknown>): string {
  return String(getValueByKey(row, col.key) ?? '');
}

/** Json / 富文本 摘要截断 */
function truncate(text: string, len = 40): string {
  if (!text) return '';
  const clean = text.replace(/\s+/g, ' ').trim();
  return clean.length > len ? clean.slice(0, len) + '…' : clean;
}

function getCellRaw(row: Record<string, unknown>, col: InternalColumn): string {
  const raw = getValueByKey(row, col.key);
  return raw == null ? '' : String(raw);
}
</script>

<template>
  <div class="list-table-content">
    <el-table
      :data="data"
      class="ltc-table"
      v-loading="loading"
      empty-text=""
      header-row-class-name="ltc-header-row"
      stripe
      border
    >
      <el-table-column
        v-if="selectable"
        type="selection"
        :width="46"
        align="center"
      />

      <el-table-column
        v-for="col in renderColumns"
        :key="col.key"
        :prop="col.key"
        :label="col.label"
        :width="col.width"
        :align="col.align ?? 'left'"
      >
        <template #header>
          <el-tooltip :content="col.label" placement="top" :show-after="500">
            <span class="ltc-header-text">{{ col.label }}</span>
          </el-tooltip>
        </template>

        <template #default="scope">
          <!-- 颜色色块 -->
          <template v-if="col.control === 'color'">
            <span
              class="ltc-color"
              :style="{ background: getCellRaw(scope.row, col) || 'transparent' }"
              :title="getCellRaw(scope.row, col)"
            ></span>
          </template>

          <!-- 图标预览 -->
          <template v-else-if="col.control === 'icon'">
            <el-icon v-if="resolveIcon(getIconName(col, scope.row))" class="ltc-icon">
              <component :is="resolveIcon(getIconName(col, scope.row))" />
            </el-icon>
            <span v-else class="ltc-cell-text">{{ getCellRaw(scope.row, col) }}</span>
          </template>

          <!-- 图片缩略图 -->
          <template v-else-if="col.control === 'image'">
            <el-image
              v-if="getCellRaw(scope.row, col)"
              :src="resolveAssetUrl(getCellRaw(scope.row, col))"
              fit="cover"
              class="ltc-thumb"
              :preview-src-list="[resolveAssetUrl(getCellRaw(scope.row, col))]"
              preview-teleported
            />
            <span v-else class="ltc-cell-text">—</span>
          </template>

          <!-- 文件链接 -->
          <template v-else-if="col.control === 'file'">
            <el-link
              v-if="getCellRaw(scope.row, col)"
              :href="resolveAssetUrl(getCellRaw(scope.row, col))"
              target="_blank"
              type="primary"
              :underline="true"
            >
              查看文件
            </el-link>
            <span v-else class="ltc-cell-text">—</span>
          </template>

          <!-- 布尔开关 -->
          <template v-else-if="col.control === 'boolean'">
            <el-switch :model-value="Boolean(getValueByKey(scope.row, col.key))" disabled />
          </template>

          <!-- Json 折叠 -->
          <template v-else-if="col.control === 'json'">
            <el-popover placement="left" :width="360" trigger="click">
              <template #reference>
                <span class="ltc-cell-text ltc-json">{{ truncate(getCellRaw(scope.row, col)) }}</span>
              </template>
              <pre class="ltc-pre">{{ getCellRaw(scope.row, col) }}</pre>
            </el-popover>
          </template>

          <!-- 富文本摘要（html / markdown） -->
          <template v-else-if="col.control === 'html'">
            <el-popover placement="left" :width="360" trigger="click">
              <template #reference>
                <span class="ltc-cell-text ltc-json">{{ truncate(getCellRaw(scope.row, col)) }}</span>
              </template>
              <div
                v-if="col.field?.itemType === 'html'"
                class="ltc-html"
                v-html="getCellRaw(scope.row, col)"
              ></div>
              <pre v-else class="ltc-pre">{{ getCellRaw(scope.row, col) }}</pre>
            </el-popover>
          </template>

          <!-- LOV 翻译（含多选标签） -->
          <template v-else-if="col.control === 'lov' && col.field?.lovCode">
            <template v-if="normalizeMulti(getValueByKey(scope.row, col.key)).length > 1">
              <el-tag
                v-for="v in normalizeMulti(getValueByKey(scope.row, col.key))"
                :key="v"
                class="ltc-tag"
                effect="plain"
                round
              >
                {{ getLovLabel(col.field.lovCode, v) }}
              </el-tag>
            </template>
            <span v-else class="ltc-cell-text">{{ getCellText(scope.row, col) }}</span>
          </template>

          <!-- 只读文本（Guid 等） -->
          <template v-else-if="col.control === 'readonly'">
            <span class="ltc-cell-text ltc-cell-text--mono">{{ getCellRaw(scope.row, col) }}</span>
          </template>

          <!-- 数值 -->
          <template v-else-if="col.control === 'number'">
            <span class="ltc-cell-text ltc-cell-text--num">{{ getCellRaw(scope.row, col) }}</span>
          </template>

          <!-- 默认文本（含 url 链接） -->
          <template v-else>
            <el-link
              v-if="col.field?.itemType === 'url' && getCellRaw(scope.row, col)"
              :href="getCellRaw(scope.row, col)"
              target="_blank"
              type="primary"
              :underline="true"
            >
              {{ getCellRaw(scope.row, col) }}
            </el-link>
            <span v-else class="ltc-cell-text">{{ getCellText(scope.row, col) }}</span>
          </template>
        </template>
      </el-table-column>

      <el-table-column label="操作" width="120" fixed="right" align="center">
        <template #header>
          <el-tooltip content="操作" placement="top" :show-after="500">
            <span class="ltc-header-text">操作</span>
          </el-tooltip>
        </template>
        <template #default="scope">
          <div class="ltc-actions">
            <button class="ltc-action-btn ltc-action-btn--edit" @click="emit('edit', scope.row)">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" width="14" height="14">
                <path d="M17 3a2.85 2.83 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5Z"></path>
                <path d="m15 5 4 4"></path>
              </svg>
              <span>编辑</span>
            </button>
            <button class="ltc-action-btn ltc-action-btn--delete" @click="emit('delete', scope.row)">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" width="14" height="14">
                <path d="M3 6h18"></path>
                <path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6"></path>
                <path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"></path>
              </svg>
              <span>删除</span>
            </button>
          </div>
        </template>
      </el-table-column>
    </el-table>
  </div>
</template>

<style lang="scss" scoped>
.list-table-content {
  width: 100%;
}

.ltc-table {
  :deep(.el-table__inner-wrapper::before) {
    display: none;
  }

  :deep(.el-table__header-wrapper th.el-table__cell) {
    background: var(--el-fill-color-lighter);
    border-bottom: 2px solid var(--el-border-color-light);
    padding: 0;
  }

  :deep(.el-table__header-wrapper .cell) {
    padding: 14px 16px;
    font-family: var(--el-font-family);
    font-size: 12px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    color: var(--el-text-color-secondary);
  }

  :deep(.el-table__body-wrapper td.el-table__cell) {
    padding: 0;
    border-bottom: 1px solid var(--el-border-color-lighter);
    transition: background 0.15s ease;
  }

  :deep(.el-table__body tr:hover > td.el-table__cell) {
    background: var(--el-color-primary-light-9);
  }

  :deep(.el-table__body tr.el-table__row--striped > td.el-table__cell) {
    background: var(--el-fill-color-lighter);
  }

  :deep(.el-table__body tr.el-table__row--striped:hover > td.el-table__cell) {
    background: var(--el-color-primary-light-9);
  }

  :deep(.el-table__body .cell) {
    padding: 14px 16px;
    line-height: 1.5;
  }

  :deep(.el-checkbox__input.is-checked .el-checkbox__inner),
  :deep(.el-checkbox__input.is-indeterminate .el-checkbox__inner) {
    background-color: var(--el-color-primary);
    border-color: var(--el-color-primary);
  }

  :deep(.el-checkbox__inner:hover) {
    border-color: var(--el-color-primary);
  }

  :deep(.el-table__fixed-right) {
    border-left: 1px solid var(--el-border-color-light);
  }

  :deep(.el-table__fixed-right::before) {
    background-color: transparent;
  }
}

.ltc-header-text {
  display: inline-block;
  max-width: 100%;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  vertical-align: bottom;
}

.ltc-cell-text {
  font-family: var(--el-font-family);
  font-size: 13px;
  color: var(--el-text-color-primary);

  &--mono {
    font-family: var(--el-font-family-mono);
    font-size: 12px;
    font-weight: 500;
  }

  &--num {
    font-family: var(--el-font-family-mono);
  }
}

.ltc-color {
  display: inline-block;
  width: 22px;
  height: 22px;
  border-radius: 4px;
  border: 1px solid var(--el-border-color);
  vertical-align: middle;
}

.ltc-icon {
  font-size: 18px;
  color: var(--el-text-color-regular);
  vertical-align: middle;
}

.ltc-thumb {
  width: 48px;
  height: 48px;
  border-radius: 4px;
}

.ltc-json {
  cursor: pointer;
  color: var(--el-color-primary);
}

.ltc-pre {
  max-height: 300px;
  overflow: auto;
  font-family: var(--el-font-family-mono);
  font-size: 12px;
  white-space: pre-wrap;
  word-break: break-all;
  margin: 0;
}

.ltc-html {
  max-height: 300px;
  overflow: auto;
  font-size: 13px;
}

.ltc-tag {
  margin: 2px;
}

.ltc-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

.ltc-action-btn {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 4px 8px;
  background: transparent;
  border: none;
  border-radius: 4px;
  font-size: 12px;
  cursor: pointer;
  transition: all 0.15s ease;

  &--edit {
    color: var(--el-text-color-secondary);

    &:hover {
      background: var(--el-color-primary-light-9);
      color: var(--el-color-primary);
    }
  }

  &--delete {
    color: var(--el-text-color-secondary);

    &:hover {
      background: var(--el-color-danger-light-9);
      color: var(--el-color-danger);
    }
  }
}
</style>

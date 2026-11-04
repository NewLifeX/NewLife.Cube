<script setup lang="ts">
/**
 * 字典参数（Admin/Parameter）—— 左类别导航 + 右参数表（方案 B）
 *
 * 列表 / CRUD 仍由 Cube 引擎驱动；本页补：类别索引、按类筛选、紧凑搜索与列呈现 preset。
 */
import { computed, nextTick, onMounted, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { Search } from '@element-plus/icons-vue';
import CubeTable from '@newlifex/cube-vue/core/components/CubeTable/CubeTable.vue';
import ListPageHeader from '@newlifex/cube-vue/core/views/components/ListPageHeader.vue';
import { createDefaultCubeEngineDeps, useCubeEngine } from '@newlifex/cube-vue/core/engine/useCubeEngine';
import { registerPagePreset } from '@newlifex/cube-vue/core/engine/pagePreset';
import { dictLabel } from '@newlifex/cube-vue/core/components/CubeTable/cubeTable.logic';
import type { FieldMeta } from '@newlifex/cube-vue/core/types/field';

const PARAMETER_ROUTE = '/Admin/Parameter';

registerPagePreset(PARAMETER_ROUTE, {
  list: {
    remove: ['Category'],
    patch: {
      Id: { hidden: true },
      ID: { hidden: true },
      Remark: { hidden: true },
      UpdateUser: { hidden: true },
      UpdateTime: { width: 168 },
    },
    order: ['Name', 'Value', 'LongValue', 'Kind', 'Enable', 'UserID', 'UpdateTime'],
  },
  search: { remove: ['Category', 'UserID'] },
  form: { columns: 1 },
});

const route = useRoute();
const { t } = useI18n();
const ctx = useCubeEngine({ routePath: route.path });

const VALUE_PREVIEW_LEN = 48;
const CATEGORY_PAGE_SIZE = 500;

interface CategoryItem {
  name: string;
  count: number;
}

const selectedCategory = ref<string | null>(null);
const systemOnly = ref(false);
const categoryFilter = ref('');
const categoryLoading = ref(false);
const categories = ref<CategoryItem[]>([]);
const categoryTotal = ref(0);

const categorySearchKey = computed(() => {
  const hit = ctx.searchFields.value.find((f) => f.name.toLowerCase() === 'category');
  return hit?.name ?? 'Category';
});

const filteredCategories = computed(() => {
  const q = categoryFilter.value.trim().toLowerCase();
  if (!q) return categories.value;
  return categories.value.filter((item) => item.name.toLowerCase().includes(q));
});

const panelTitle = computed(() => {
  if (selectedCategory.value) {
    return t('parameterAdmin.panelCategory', { name: selectedCategory.value });
  }
  return t('parameterAdmin.panelAll');
});

const panelCountHint = computed(() => {
  if (selectedCategory.value) {
    return t('parameterAdmin.panelCountFiltered', { count: ctx.total.value });
  }
  return t('parameterAdmin.panelCountAll', { count: categoryTotal.value || ctx.total.value });
});

function kindTagType(kind: unknown): '' | 'success' | 'warning' | 'info' | 'danger' {
  const n = Number(kind);
  if (Number.isNaN(n)) return 'info';
  if (n === 0) return 'info';
  if (n === 3) return 'success';
  if (n === 9 || n === 14) return 'warning';
  if (n === 21 || n === 22) return 'warning';
  return '';
}

function previewText(raw: unknown, max = VALUE_PREVIEW_LEN): string {
  if (raw === null || raw === undefined) return '';
  const s = String(raw).replace(/\s+/g, ' ').trim();
  if (!s) return '';
  return s.length > max ? `${s.slice(0, max)}…` : s;
}

function rowUserId(row: Record<string, unknown>): number {
  const v = row.userID ?? row.UserID ?? row.userId;
  return Number(v ?? 0);
}

function rowCategory(row: Record<string, unknown>): string {
  const v = row.category ?? row.Category;
  const s = v === null || v === undefined ? '' : String(v).trim();
  return s || t('parameterAdmin.uncategorized');
}

function pickStoredValue(row: Record<string, unknown>): string {
  const long = row.longValue ?? row.LongValue;
  if (long !== null && long !== undefined && String(long).trim() !== '') {
    return String(long);
  }
  const val = row.value ?? row.Value;
  return val !== null && val !== undefined ? String(val) : '';
}

/** 与引擎 `readListResponse` 同口径，避免索引请求解析成空列表 */
function extractListPayload(res: unknown): { rows: Record<string, unknown>[]; total: number } {
  let body = res;
  if (body && typeof body === 'object' && 'data' in body && !Array.isArray((body as { data?: unknown }).data)) {
    const inner = (body as { data?: unknown }).data;
    if (inner && typeof inner === 'object' && ('data' in inner || 'page' in inner)) {
      body = inner;
    }
  }
  if (Array.isArray(body)) {
    return { rows: body as Record<string, unknown>[], total: body.length };
  }
  const record = (body ?? {}) as {
    data?: unknown;
    page?: { totalCount?: number; total?: number };
  };
  const rows = Array.isArray(record.data)
    ? (record.data as Record<string, unknown>[])
    : record.data && typeof record.data === 'object'
      ? [record.data as Record<string, unknown>]
      : [];
  const total = record.page?.totalCount ?? record.page?.total ?? rows.length;
  return { rows, total };
}

const engineHttp = createDefaultCubeEngineDeps().http;

async function loadCategoryIndex(): Promise<void> {
  const api = ctx.type.value;
  if (!api) return;
  categoryLoading.value = true;
  try {
    const allRows: Record<string, unknown>[] = [];
    let pageIndex = 1;
    let total = Number.POSITIVE_INFINITY;

    while ((pageIndex - 1) * CATEGORY_PAGE_SIZE < total) {
      const res = await engineHttp({
        url: api,
        method: 'get',
        params: { pageIndex, pageSize: CATEGORY_PAGE_SIZE },
      });
      const chunk = extractListPayload(res);
      if (!chunk.rows.length) break;
      allRows.push(...chunk.rows);
      total = chunk.total;
      pageIndex += 1;
      if (pageIndex > 40) break;
    }

    mergeCategoriesFromRows(allRows, true);
    categoryTotal.value = allRows.length || total;
  } finally {
    categoryLoading.value = false;
  }
}

function applySystemFilter(): void {
  if (systemOnly.value) {
    ctx.searchModel.UserID_min = 0;
    ctx.searchModel.UserID_max = 0;
  } else {
    ctx.searchModel.UserID_min = undefined;
    ctx.searchModel.UserID_max = undefined;
  }
}

async function selectCategory(name: string | null): Promise<void> {
  selectedCategory.value = name;
  if (name) {
    ctx.searchModel[categorySearchKey.value] = name;
  } else {
    ctx.searchModel[categorySearchKey.value] = undefined;
  }
  await ctx.search();
}

async function resetSearchBar(): Promise<void> {
  systemOnly.value = false;
  ctx.searchModel.UserID_min = undefined;
  ctx.searchModel.UserID_max = undefined;
  await ctx.resetSearch();
  if (selectedCategory.value) {
    ctx.searchModel[categorySearchKey.value] = selectedCategory.value;
    await ctx.search();
  }
}

function seedSystemUserOnForm(): void {
  for (const key of ['UserID', 'userID', 'UserId', 'userId']) {
    ctx.formModel[key] = 0;
  }
}

/** 新增默认值：种类普通(0)、默认启用，避免只填名称/数值仍被校验拦住 */
function seedCreateDefaultsOnForm(): void {
  seedSystemUserOnForm();
  for (const key of ['Kind', 'kind']) ctx.formModel[key] = 0;
  for (const key of ['Enable', 'enable']) ctx.formModel[key] = true;
}

function handleAdd(): void {
  ctx.openCreate();
  void nextTick(() => {
    seedCreateDefaultsOnForm();
    const cat = selectedCategory.value;
    if (!cat) return;
    const key = categorySearchKey.value;
    ctx.formModel[key] = cat;
    const lower = key.charAt(0).toLowerCase() + key.slice(1);
    if (lower !== key) ctx.formModel[lower] = cat;
  });
}

async function copyValue(text: string) {
  if (!text) return;
  try {
    if (navigator.clipboard?.writeText) {
      await navigator.clipboard.writeText(text);
    } else {
      const ta = document.createElement('textarea');
      ta.value = text;
      ta.style.position = 'fixed';
      ta.style.opacity = '0';
      document.body.appendChild(ta);
      ta.select();
      document.execCommand('copy');
      document.body.removeChild(ta);
    }
    ElMessage.success(t('parameterAdmin.copySuccess'));
  } catch {
    ElMessage.error(t('ssoAccessToken.copyFailed'));
  }
}

function mergeCategoriesFromRows(rows: Record<string, unknown>[], replaceCounts: boolean): void {
  const map = replaceCounts
    ? new Map<string, number>()
    : new Map(categories.value.map((c) => [c.name, c.count]));
  for (const row of rows) {
    const name = rowCategory(row);
    if (replaceCounts) {
      map.set(name, (map.get(name) ?? 0) + 1);
    } else if (!map.has(name)) {
      map.set(name, 1);
    }
  }
  if (!map.size) return;
  categories.value = [...map.entries()]
    .map(([name, count]) => ({ name, count }))
    .sort((a, b) => a.name.localeCompare(b.name, undefined, { sensitivity: 'base' }));
}

watch(
  () => ctx.list.value,
  (rows) => {
    if (rows.length) mergeCategoriesFromRows(rows, false);
  },
  { deep: true },
);

watch(
  () => ctx.schemaReady.value && !ctx.loading.value,
  (ready) => {
    if (ready) void loadCategoryIndex();
  },
  { immediate: true },
);

/** 侧栏 + 自定义搜索区会改变表格可用宽度，需触发 el-table 重新 layout，否则固定操作列会漂到中间 */
function nudgeTableLayout(): void {
  void nextTick(() => {
    window.dispatchEvent(new Event('resize'));
  });
}

onMounted(() => {
  nudgeTableLayout();
});

watch(
  () => [selectedCategory.value, ctx.loading.value] as const,
  () => nudgeTableLayout(),
);
</script>

<template>
  <div class="parameter-page">
    <ListPageHeader :subtitle="t('parameterAdmin.subtitle')" />

    <div class="parameter-body">
      <aside class="parameter-nav" aria-label="parameter categories">
        <div class="parameter-nav-head">
          <span class="parameter-nav-title">{{ t('parameterAdmin.categoryNav') }}</span>
          <span class="parameter-nav-meta">{{ t('parameterAdmin.categoryTotal', { count: categories.length }) }}</span>
        </div>
        <el-input
          v-model="categoryFilter"
          class="parameter-nav-filter"
          clearable
          size="small"
          :placeholder="t('parameterAdmin.filterCategory')"
          :prefix-icon="Search"
        />
        <div class="parameter-nav-context" aria-live="polite">
          <span class="parameter-panel-title">{{ panelTitle }}</span>
          <span class="parameter-panel-sub">{{ panelCountHint }}</span>
        </div>
        <div v-loading="categoryLoading" class="parameter-nav-list">
          <button
            type="button"
            class="parameter-nav-item"
            :class="{ active: selectedCategory === null }"
            @click="selectCategory(null)"
          >
            <span class="parameter-nav-label">{{ t('parameterAdmin.allCategories') }}</span>
            <span class="parameter-nav-count">{{ categoryTotal || ctx.total.value }}</span>
          </button>
          <button
            v-for="item in filteredCategories"
            :key="item.name"
            type="button"
            class="parameter-nav-item"
            :class="{ active: selectedCategory === item.name }"
            @click="selectCategory(item.name)"
          >
            <span class="parameter-nav-label" :title="item.name">{{ item.name }}</span>
            <span class="parameter-nav-count">{{ item.count }}</span>
          </button>
        </div>
      </aside>

      <div class="parameter-main">
        <CubeTable
          :context="ctx"
          class="parameter-table"
          :show-add="false"
          columns-storage-key="parameter-admin-b"
        >
          <template #search="{ ctx: searchCtx }">
            <div class="parameter-search-stack">
              <div class="parameter-search">
                <el-input
                  v-if="searchCtx.hasKeyword"
                  :model-value="searchCtx.keyword.value"
                  class="parameter-search-keyword"
                  clearable
                  :placeholder="t('parameterAdmin.searchPlaceholder')"
                  :prefix-icon="Search"
                  @update:model-value="(v: string) => (searchCtx.keyword.value = v)"
                  @keyup.enter="searchCtx.search()"
                  @clear="searchCtx.search()"
                />
                <el-checkbox v-model="systemOnly" @change="applySystemFilter">
                  {{ t('parameterAdmin.systemOnly') }}
                </el-checkbox>
                <div class="parameter-search-actions">
                  <el-button @click="resetSearchBar">{{ t('button.reset') }}</el-button>
                  <el-button type="primary" @click="searchCtx.search()">{{ t('button.search') }}</el-button>
                </div>
              </div>
            </div>
          </template>

          <template #actions>
            <el-button type="primary" @click="handleAdd">
              {{ selectedCategory ? t('parameterAdmin.addInCategory') : t('engine.add') }}
            </el-button>
          </template>

          <template #col-Kind="{ column, value }">
            <el-tag :type="kindTagType(value)" size="small" effect="light">
              {{ dictLabel(column.field as FieldMeta, value) || value }}
            </el-tag>
          </template>

          <template #col-Name="{ value }">
            <code class="param-key">{{ value || '—' }}</code>
          </template>

          <template #col-UserID="{ row, value }">
            <el-tag v-if="rowUserId(row as Record<string, unknown>) === 0" size="small" type="primary" effect="plain">
              {{ t('parameterAdmin.systemScope') }}
            </el-tag>
            <span v-else class="param-user">
              {{ t('parameterAdmin.userScope', { id: value ?? rowUserId(row as Record<string, unknown>) }) }}
            </span>
          </template>

          <template #col-Enable="{ value }">
            <el-tag size="small" :type="value ? 'success' : 'info'" effect="light">
              {{ value ? t('common.yes') : t('common.no') }}
            </el-tag>
          </template>

          <template #col-Value="{ row, value }">
            <div class="param-value-cell">
              <el-tooltip
                v-if="pickStoredValue(row as Record<string, unknown>)"
                :content="pickStoredValue(row as Record<string, unknown>)"
                placement="top-start"
                :show-after="400"
              >
                <span class="param-value-text">{{ previewText(value) || '—' }}</span>
              </el-tooltip>
              <span v-else class="param-value-text muted">—</span>
              <el-button
                v-if="value !== null && value !== undefined && String(value).trim() !== ''"
                link
                type="primary"
                size="small"
                class="param-copy"
                @click.stop="copyValue(String(value))"
              >
                {{ t('parameterAdmin.copyValue') }}
              </el-button>
            </div>
          </template>

          <template #col-LongValue="{ row, value }">
            <div class="param-value-cell">
              <el-tooltip
                v-if="pickStoredValue(row as Record<string, unknown>)"
                :content="pickStoredValue(row as Record<string, unknown>)"
                placement="top-start"
                :show-after="400"
              >
                <span class="param-value-text">{{ previewText(value, 64) || '—' }}</span>
              </el-tooltip>
              <span v-else class="param-value-text muted">—</span>
              <el-button
                v-if="value !== null && value !== undefined && String(value).trim() !== ''"
                link
                type="primary"
                size="small"
                class="param-copy"
                @click.stop="copyValue(String(value))"
              >
                {{ t('parameterAdmin.copyValue') }}
              </el-button>
            </div>
          </template>
        </CubeTable>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.parameter-page {
  height: 100%;
  box-sizing: border-box;
  display: flex;
  flex-direction: column;
  padding: 12px;
  min-height: 0;
  overflow: hidden;
}

.parameter-body {
  flex: 1;
  min-height: 0;
  display: flex;
  gap: 12px;
}

.parameter-nav {
  width: 220px;
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  min-height: 0;
  padding: 10px 10px 8px;
  border-radius: 8px;
  border: 1px solid var(--el-border-color-lighter);
  background: var(--el-bg-color-overlay);
  box-shadow: var(--el-box-shadow-light);
}

.parameter-nav-head {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 8px;
}

.parameter-nav-title {
  font-size: 13px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.parameter-nav-meta {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.parameter-nav-filter {
  margin-bottom: 8px;
}

.parameter-nav-list {
  flex: 1;
  min-height: 0;
  overflow: auto;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.parameter-nav-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  width: 100%;
  padding: 8px 10px;
  border: none;
  border-radius: 6px;
  background: transparent;
  cursor: pointer;
  text-align: left;
  font: inherit;
  color: var(--el-text-color-regular);
  transition: background 0.15s ease;

  &:hover {
    background: var(--el-fill-color-light);
  }

  &.active {
    background: var(--el-color-primary-light-9);
    color: var(--el-color-primary);
    font-weight: 500;
  }
}

.parameter-nav-label {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 13px;
}

.parameter-nav-count {
  flex-shrink: 0;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  font-variant-numeric: tabular-nums;

  .active & {
    color: var(--el-color-primary);
  }
}

.parameter-main {
  flex: 1;
  min-width: 0;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

.parameter-table {
  flex: 1;
  min-height: 0;
  padding: 0;

  :deep(.cube-table) {
    height: 100%;
    padding: 0;
  }

  :deep(.ct-table-wrap) {
    width: 100%;
  }

  :deep(.ct-table-wrap .el-table) {
    width: 100% !important;
  }
}

.parameter-search-stack {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.parameter-search {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px;
  padding: 10px 12px;
  border-radius: 8px;
  border: 1px solid var(--el-border-color-lighter);
  background: var(--el-bg-color-overlay);
  box-shadow: var(--el-box-shadow-light);
}

.parameter-nav-context {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding: 8px 10px;
  margin-bottom: 6px;
  border-radius: 6px;
  border: 1px solid var(--el-border-color-lighter);
  background: var(--el-fill-color-blank);
}

.parameter-nav-context .parameter-panel-title {
  font-size: 13px;
  font-weight: 600;
  color: var(--el-text-color-primary);
  line-height: 1.35;
}

.parameter-nav-context .parameter-panel-sub {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.parameter-search-keyword {
  width: min(360px, 100%);
}

.parameter-search-actions {
  margin-left: auto;
  display: flex;
  gap: 8px;
}

.param-key {
  font-family: var(--el-font-family-monospace, ui-monospace, monospace);
  font-size: 12px;
  padding: 2px 6px;
  border-radius: 4px;
  background: var(--el-fill-color-light);
  color: var(--el-text-color-primary);
  word-break: break-all;
}

.param-user {
  font-size: 13px;
  color: var(--el-text-color-secondary);
}

.param-value-cell {
  display: flex;
  align-items: center;
  gap: 6px;
  max-width: 100%;
}

.param-value-text {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 13px;

  &.muted {
    color: var(--el-text-color-placeholder);
  }
}

.param-copy {
  flex-shrink: 0;
  padding: 0 4px;
}
</style>

<template>
  <el-dialog
    :model-value="dialogVisible"
    @update:model-value="$emit('update:dialogVisible', $event)"
    :title="'选择 ' + (lovMeta?.name || lovCode)"
    width="680px"
    top="5vh"
    append-to-body
    :close-on-click-modal="false"
  >
    <!-- 搜索栏 -->
    <div v-if="searchFields.length > 0" class="lst-search-bar">
      <template v-for="field in searchFields" :key="field.field">
        <el-input
          v-if="field.componentType === 'input'"
          v-model="searchParams[field.field]"
          :placeholder="field.title"
          size="small"
          clearable
          style="width: 160px"
        />
        <LovSelect
          v-else-if="(field.componentType === 'select' || field.componentType === 'lov') && field.refLovCode"
          :code="field.refLovCode"
          v-model="searchParams[field.field]"
          :placeholder="field.title"
          size="small"
          clearable
        />
        <el-select
          v-else-if="field.componentType === 'select'"
          v-model="searchParams[field.field]"
          :placeholder="field.title"
          size="small"
          clearable
          style="width: 160px"
        >
          <el-option label="-" value="" />
        </el-select>
        <el-date-picker
          v-else-if="field.componentType === 'datepicker'"
          v-model="searchParams[field.field]"
          :placeholder="field.title"
          size="small"
          style="width: 160px"
        />
      </template>
      <el-button type="primary" size="small" @click="searchData">搜索</el-button>
      <el-button size="small" @click="resetSearch">重置</el-button>
    </div>

    <!-- 数据表格 -->
    <el-table
      :data="tableData"
      v-loading="tableLoading"
      stripe
      :highlight-current-row="!multiple"
      :row-class-name="rowClassName"
      style="width: 100%"
      @row-click="selectRow"
    >
      <el-table-column
        v-for="col in tableColumns"
        :key="col.field"
        :prop="col.field"
        :label="col.title"
        :width="col.width || undefined"
        :align="col.align || 'left'"
      >
        <template #default="{ row }">
          {{ getTranslatedText(row, col) }}
        </template>
      </el-table-column>
    </el-table>

    <!-- 分页 -->
    <div v-if="listConfig?.pageable" class="lst-pagination">
      <el-pagination
        v-model:current-page="currentPage"
        v-model:page-size="pageSize"
        :total="total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @current-change="fetchListData"
        @size-change="(s: number) => { pageSize = s; fetchListData() }"
      />
    </div>

    <template #footer>
      <el-button @click="$emit('update:dialogVisible', false)">取消</el-button>
      <el-button v-if="multiple" type="primary" @click="confirmMulti">确定</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { getConfig } from '@newlifex/cube-vue/core/configure';
import LovSelect from './LovSelect.vue';
import { fetchLovListData, fetchBatchLabel } from '@newlifex/cube-vue/core/utils/lov-api';
import type {
  LovEnumOption,
  LovListMeta,
  LovSearchField,
  LovTableColumn,
} from '@newlifex/cube-vue/core/types/lov';

const props = defineProps<{
  dialogVisible: boolean;
  lovCode: string;
  lovMeta: LovListMeta | null;
  inlineEnums: Record<string, LovEnumOption[]>;
  translateCache: Map<string, string>;
  /** 是否多选（multipleSelect 场景），确定后 emit string[] */
  multiple?: boolean;
}>();

const emit = defineEmits<{
  (e: 'update:dialogVisible', value: boolean): void;
  (e: 'select', row: Record<string, unknown>): void;
  (e: 'confirm', values: string[]): void;
}>();

/** 多选模式下的选中值集合 */
const selectedValues = ref<string[]>([]);

// ── 从 meta 中读取配置 ──
const listConfig = props.lovMeta?.listConfig || null;
const searchFields = ref<LovSearchField[]>(props.lovMeta?.searchFields || []);
const tableColumns = ref<LovTableColumn[]>(props.lovMeta?.tableColumns || []);

// ── 状态 ──
const tableLoading = ref(false);
const searchParams = ref<Record<string, string | number | undefined>>({});
const tableData = ref<Record<string, unknown>[]>([]);
const currentPage = ref(1);
const pageSize = ref(20);
const total = ref(0);

// ── 当 meta 变化时刷新配置 ──
watch(() => props.lovMeta, (meta) => {
  if (!meta) return;
  searchFields.value = meta.searchFields || [];
  tableColumns.value = meta.tableColumns || [];
});

// ── 弹窗打开时首次加载数据 ──
watch(() => props.dialogVisible, (visible) => {
  if (visible) {
    searchParams.value = {};
    selectedValues.value = [];
    currentPage.value = 1;
    fetchListData();
  }
});

function searchData() {
  currentPage.value = 1;
  fetchListData();
}

function resetSearch() {
  searchParams.value = {};
  currentPage.value = 1;
  fetchListData();
}

async function fetchListData() {
  tableLoading.value = true;
  try {
    const result = await fetchLovListData({
      lovCode: props.lovCode,
      params: searchParams.value,
      pageNum: currentPage.value,
      pageSize: pageSize.value,
    });
    tableData.value = result.data || [];
    total.value = result.total || 0;

    // 批量翻译列表列中被引用的 List.xxx 值
    if (tableColumns.value.length > 0) {
      const batchMap = new Map<string, string[]>();
      for (const col of tableColumns.value) {
        if (!col.refLovCode) continue;
        if (props.inlineEnums[col.refLovCode]) continue;

        const values = tableData.value
          .map((r) => r[col.field])
          .filter((v) => v != null && v !== '')
          .map(String);
        if (values.length > 0) {
          batchMap.set(col.refLovCode, [...new Set(values)]);
        }
      }

      for (const [batchLovCode, values] of batchMap) {
        const uncached = values.filter(v => !props.translateCache.has(`${batchLovCode}:${v}`));
        if (uncached.length === 0) continue;
        try {
          const labelResult = await fetchBatchLabel({ lovCode: batchLovCode, values: uncached });
          for (const [v, label] of Object.entries(labelResult)) {
            props.translateCache.set(`${batchLovCode}:${v}`, label);
          }
        } catch (e) {
          console.error('LovSelectTable: 批量翻译失败', e);
        }
      }
    }
  } catch (err) {
    console.error('LovSelectTable: 获取列表数据失败', err);
  } finally {
    tableLoading.value = false;
  }
}

function getTranslatedText(row: Record<string, unknown>, col: LovTableColumn): string {
  if (!col.refLovCode) return String(row[col.field] ?? '-');

  const value = row[col.field];
  if (value == null) return '-';

  const cacheKey = `${col.refLovCode}:${value}`;

  const cached = props.translateCache.get(cacheKey);
  if (cached) return cached;

  if (props.inlineEnums[col.refLovCode]) {
    const map = new Map(
      props.inlineEnums[col.refLovCode].map((e) => [String(e.value), e.label])
    );
    return map.get(String(value)) ?? String(value);
  }

  return String(value);
}

function selectRow(row: Record<string, unknown>) {
  if (props.multiple) {
    const valueField = props.lovMeta?.valueField || 'id';
    const val = String(row[valueField]);
    const idx = selectedValues.value.indexOf(val);
    if (idx >= 0) selectedValues.value.splice(idx, 1);
    else selectedValues.value.push(val);
    return;
  }
  emit('select', row);
}

/** 确认多选结果 */
function confirmMulti() {
  emit('confirm', [...selectedValues.value]);
}

/** 多选行高亮 */
function rowClassName(row: { row: Record<string, unknown> }): string {
  if (!props.multiple) return '';
  const valueField = props.lovMeta?.valueField || 'id';
  const val = String(row.row[valueField]);
  return selectedValues.value.includes(val) ? 'lst-row--selected' : '';
}
</script>

<style scoped>
.lst-search-bar {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 16px;
  align-items: center;
}
.lst-pagination {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}

:deep(.lst-row--selected) {
  background: var(--el-color-primary-light-9) !important;
}
</style>

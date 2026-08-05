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
          v-else-if="
            (field.componentType === 'select' || field.componentType === 'lov') && field.refLovCode
          "
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
      ref="tableRef"
      :data="tableData"
      v-loading="tableLoading"
      stripe
      :row-key="getRowKey"
      :highlight-current-row="!multiple"
      :row-class-name="rowClassName"
      style="width: 100%"
      @row-click="selectRow"
      @selection-change="onSelectionChange"
    >
      <!-- 左侧勾选列：多选=复选框，单选=单选框，一眼区分交互模式 -->
      <el-table-column v-if="multiple" type="selection" width="48" :reserve-selection="true" />
      <el-table-column v-else key="radio" width="48" align="center" label="">
        <template #default="{ row }">
          <el-radio v-model="currentValue" :value="getRowValue(row)" @change="onRadioSelect(row)">
            <span />
          </el-radio>
        </template>
      </el-table-column>

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
        @size-change="
          (s: number) => {
            pageSize = s;
            fetchListData();
          }
        "
      />
    </div>

    <template #footer>
      <div class="lst-footer">
        <span class="lst-selected-count">已选 {{ selectedCountText }}</span>
        <span class="lst-footer-buttons">
          <el-button @click="$emit('update:dialogVisible', false)">取消</el-button>
          <el-button v-if="multiple" type="primary" @click="confirmMulti">确定</el-button>
        </span>
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
// 显式声明组件名，确保 Vue DevTools 显示为 LovSelectTable（不依赖目录/文件名推断）
defineOptions({ name: 'LovSelectTable' });
import { ref, watch, computed, nextTick, defineAsyncComponent } from 'vue';
// 用异步组件打破与 LovSelect 的循环依赖：LovSelect 静态 import 本组件用于 LIST 弹窗，
// 本组件又需要 LovSelect 作为嵌套 lov 搜索控件。若双方都静态 import，会形成循环依赖，
// 导致 Vite eager glob 在收集 story 时漏掉「先加载一方」的 stories 导出，使 LovSelect 的 CT story 无法注册。
const LovSelect = defineAsyncComponent(() => import('../LovSelect/index.vue'));
import {
  fetchLovListData,
  fetchLovListDataDirect,
  fetchBatchLabel,
  shouldDirectRequest,
} from '@newlifex/cube-vue/core/utils/lov-api';
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
  /** 当前已选值（单选为单个值，多选为逗号分隔字符串或 string[]），弹窗打开时用于回显已选 */
  modelValue?: string | number | string[] | undefined;
}>();

const emit = defineEmits<{
  (e: 'update:dialogVisible', value: boolean): void;
  (e: 'select', row: Record<string, unknown>): void;
  (e: 'confirm', values: string[]): void;
}>();

/** el-table 实例引用（用于回显多选勾选状态） */
const tableRef = ref<unknown>(null);

/** 多选模式下的选中值集合（已选统计与高亮的唯一权威来源，任何路径都不得用"当前页勾选子集"裁剪它） */
const selectedValues = ref<string[]>([]);

/**
 * 回显 / 翻页重放勾选期间的守卫标志。
 * restoreSelection 用 toggleRowSelection 重放勾选时，el-table 会触发 selection-change，
 * 但此时它只上报"本次 toggle 的当前页行"，若据此覆盖会裁掉跨页/回显已选 → 见 README 根因 C2/C3。
 * 重放期间置 true，onSelectionChange 直接 return，重放结束后置 false。
 */
const restoringSelection = ref(false);

/** 单选模式下已选值（用于弹窗打开时高亮已选行） */
const currentValue = ref<string | number | undefined>('');

/** 将已存储值归一为 string[]（多选逗号分隔字符串 / 数组 / 单选单值） */
function toValueArray(val?: string | number | string[] | null): string[] {
  if (val == null) return [];
  if (Array.isArray(val)) return val.map(String);
  if (typeof val === 'number') return [String(val)];
  return String(val)
    .split(',')
    .map((s) => s.trim())
    .filter((s) => s !== '');
}

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

/** 弹窗底部「已选统计」文本（右下角展示） */
const selectedCountText = computed(() => {
  if (props.multiple) return `${selectedValues.value.length} 项`;
  return currentValue.value != null && String(currentValue.value) !== '' ? '1 项' : '0 项';
});

/** 行唯一键（reserve-selection 跨页保留勾选所需） */
function getRowKey(row: Record<string, unknown>): string | number {
  return row[props.lovMeta?.valueField || 'id'] as string | number;
}

/** 取行的值字段（单选 radio 的 value） */
function getRowValue(row: Record<string, unknown>): string | number {
  return row[props.lovMeta?.valueField || 'id'] as string | number;
}

// ── 当 meta 变化时刷新配置 ──
watch(
  () => props.lovMeta,
  (meta) => {
    if (!meta) return;
    searchFields.value = meta.searchFields || [];
    tableColumns.value = meta.tableColumns || [];
  },
);

// ── 弹窗打开时首次加载数据，并回显已选值 ──
watch(
  () => props.dialogVisible,
  (visible) => {
    if (visible) {
      searchParams.value = {};
      if (props.multiple) {
        // 多选：从已存储值恢复选中集合（支持逗号分隔字符串或 string[]）
        selectedValues.value = toValueArray(props.modelValue);
        currentValue.value = undefined;
      } else {
        // 单选：记录已选值用于高亮当前行
        selectedValues.value = [];
        currentValue.value = props.modelValue as string | number | undefined;
      }
      currentPage.value = 1;
      fetchListData();
    }
  },
);

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
    const listConfig = props.lovMeta?.listConfig || null;

    // 请求方式决策：requestUrl 以 / 开头（同源同应用）→ 强制前端直连；否则按 proxyRequest 决定
    const direct = shouldDirectRequest(listConfig);

    let result: { data: Record<string, unknown>[]; total: number };
    if (direct) {
      // 前端直连数据源（不经后端代理）
      result = await fetchLovListDataDirect(listConfig!, {
        lovCode: props.lovCode,
        params: searchParams.value,
        pageNum: currentPage.value,
        pageSize: pageSize.value,
      });
    } else {
      // 后端 /Admin/Lov/ListData 代理转发
      result = await fetchLovListData({
        lovCode: props.lovCode,
        params: searchParams.value,
        pageNum: currentPage.value,
        pageSize: pageSize.value,
      });
    }
    tableData.value = result.data || [];
    total.value = result.total || 0;

    // 多选回显：根据已存储值勾选当前页行（reserve-selection 会跨页保留）
    if (props.multiple) {
      await nextTick();
      restoreSelection();
    }

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
        const uncached = values.filter((v) => !props.translateCache.has(`${batchLovCode}:${v}`));
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
    const map = new Map(props.inlineEnums[col.refLovCode].map((e) => [String(e.value), e.label]));
    return map.get(String(value)) ?? String(value);
  }

  return String(value);
}

function selectRow(row: Record<string, unknown>) {
  if (props.multiple) return; // 多选走左侧勾选框，行点击不再直接切换
  emit('select', row);
}

/** 单选：点击左侧单选框即选中并关闭弹窗 */
function onRadioSelect(row: Record<string, unknown>) {
  if (props.multiple) return;
  emit('select', row);
}

/** 多选：勾选框变化回调，同步选中集合（非重放期间以 el-table 全量选择为准，reserve-selection 保证跨页全量） */
function onSelectionChange(vals: Record<string, unknown>[]) {
  // 重放（回显/翻页）期间 el-table 仅上报本次 toggle 的当前页行，若覆盖会裁掉跨页/回显已选 → 见 README 根因 C2/C3
  if (restoringSelection.value) return;
  const valueField = props.lovMeta?.valueField || 'id';
  selectedValues.value = vals.map((r) => String(r[valueField]));
}

/**
 * 多选回显：让"勾选视图"与权威集合 selectedValues 对齐（命中则勾选、未命中则取消）。
 * 注意：本函数只 toggle 勾选框，绝不回写 selectedValues —— 否则会触发"用当前页勾选子集反写权威集合"的裁剪 bug。
 * 未命中行的 toggle(false) 同时清掉 reserve-selection 可能残留的旧勾选。
 */
function restoreSelection() {
  if (!props.multiple || !tableRef.value) return;
  const valueField = props.lovMeta?.valueField || 'id';
  const selected = new Set(selectedValues.value);
  const elTable = tableRef.value as {
    toggleRowSelection?: (row: unknown, selected?: boolean) => void;
  };
  const toggle = elTable.toggleRowSelection;
  if (typeof toggle !== 'function') return;
  restoringSelection.value = true; // 抑制 selection-change 覆盖权威集合
  try {
    for (const row of tableData.value) {
      const key = String(row[valueField]);
      toggle(row, selected.has(key));
    }
  } finally {
    restoringSelection.value = false;
  }
}

/** 确认多选结果 */
function confirmMulti() {
  emit('confirm', [...selectedValues.value]);
}

/** 行高亮：多选高亮已勾选行；单选高亮已选行（回显） */
function rowClassName(row: { row: Record<string, unknown> }): string {
  const valueField = props.lovMeta?.valueField || 'id';
  const val = String(row.row[valueField]);
  if (props.multiple) {
    return selectedValues.value.includes(val) ? 'lst-row--selected' : '';
  }
  return currentValue.value != null &&
    String(currentValue.value) !== '' &&
    val === String(currentValue.value)
    ? 'lst-row--selected'
    : '';
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
.lst-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
}
.lst-selected-count {
  color: var(--el-text-color-secondary);
  font-size: 12px;
}
.lst-footer-buttons {
  display: flex;
  gap: 8px;
}

:deep(.lst-row--selected) {
  background: var(--el-color-primary-light-9) !important;
}
</style>

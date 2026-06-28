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
      highlight-current-row
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
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { getConfig } from 'cube-front/core/configure';
import LovSelect from './LovSelect.vue';

const props = defineProps<{
  dialogVisible: boolean;
  lovCode: string;
  lovMeta: any;
  inlineEnums: Record<string, any[]>;
  translateCache: Map<string, string>;
}>();

const emit = defineEmits<{
  (e: 'update:dialogVisible', value: boolean): void;
  (e: 'select', row: any): void;
}>();

// ── 从 meta 中读取配置 ──
const listConfig = props.lovMeta?.listConfig || null;
const searchFields = ref<any[]>(props.lovMeta?.searchFields || []);
const tableColumns = ref<any[]>(props.lovMeta?.tableColumns || []);

// ── 状态 ──
const tableLoading = ref(false);
const searchParams = ref<Record<string, any>>({});
const tableData = ref<any[]>([]);
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
    const cfg = getConfig();
    const baseUrl = cfg.request?.baseUrl || '';
    const res = await fetch(`${baseUrl}/Admin/Lov/ListData`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        lovCode: props.lovCode,
        params: searchParams.value,
        pageNum: currentPage.value,
        pageSize: pageSize.value,
      }),
    });
    const json = await res.json();
    tableData.value = json.data || [];
    total.value = json.total || 0;

    // 批量翻译列表列中被引用的 List.xxx 值
    if (tableColumns.value.length > 0) {
      const batchMap = new Map<string, string[]>();
      for (const col of tableColumns.value) {
        if (!col.refLovCode) continue;
        if (props.inlineEnums[col.refLovCode]) continue;

        const values = tableData.value
          .map((r: any) => r[col.field])
          .filter((v: any) => v != null && v !== '')
          .map(String);
        if (values.length > 0) {
          batchMap.set(col.refLovCode, [...new Set(values)]);
        }
      }

      for (const [batchLovCode, values] of batchMap) {
        const uncached = values.filter(v => !props.translateCache.has(`${batchLovCode}:${v}`));
        if (uncached.length === 0) continue;
        try {
          const labelCfg = getConfig();
          const labelBaseUrl = labelCfg.request?.baseUrl || '';
          const labelRes = await fetch(`${labelBaseUrl}/Admin/Lov/BatchLabel`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ lovCode: batchLovCode, values: uncached }),
          });
          const labelJson = await labelRes.json();
          for (const [v, label] of Object.entries(labelJson)) {
            props.translateCache.set(`${batchLovCode}:${v}`, label as string);
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

function getTranslatedText(row: any, col: any): string {
  if (!col.refLovCode) return row[col.field] ?? '-';

  const value = row[col.field];
  if (value == null) return '-';

  const cacheKey = `${col.refLovCode}:${value}`;

  const cached = props.translateCache.get(cacheKey);
  if (cached) return cached;

  if (props.inlineEnums[col.refLovCode]) {
    const map = new Map(
      props.inlineEnums[col.refLovCode].map((e: any) => [String(e.value), e.label])
    );
    return map.get(String(value)) ?? String(value);
  }

  return String(value);
}

function selectRow(row: any) {
  emit('select', row);
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
</style>

import { nextTick } from 'vue';
import { Message } from '@arco-design/web-vue';
import { FieldKind, type PageSetting } from '@cube/api-core';
import cubeApi from '@/api';
import { toFieldMetas } from '@/core/utils/fieldNormalize';
import { isTenantField, resolveListControl } from '@/core/utils/fieldControl';
import { getValueByKey } from '@/core/utils/url';
import { formatApiError } from '@/core/utils/apiError';
import {
  enrichFieldsWithEnumDataSource,
  enrichFieldsWithLookup,
  fetchBatchLabel,
} from '@/core/utils/lov-api';
import { collectCascaderIds, mergeAreaLabel } from '@/core/utils/areaLabels';
import { buildSortPayload, applyChartData } from '@/core/utils/viewProfile';
import { normalizePageSize } from '@/core/utils/viewMapping';
import { buildViewFilterParam, cleanSearchParams, matchesViewFilter } from '@/core/utils/searchFilters';
import { useTenantStore } from '@/stores/tenant';
import type { ListContext } from './listContext';
import type { FieldMeta } from '@/core/types/field';

/**
 * DefaultList 查询领域（OSC-260813c3e9）：查询 / 分页 / LOV 水合 / 预定义查询 / 图表加载。
 */
export function useListQuery(ctx: ListContext) {
  const tenantStore = useTenantStore();

  function withoutTenant(fields: FieldMeta[]): FieldMeta[] {
    if (tenantStore.enableTenant) return fields;
    return fields.filter((f) => !isTenantField(f));
  }
  const {
    typePath,
    listFields,
    pageSetting,
    pkField,
    searchFields,
    addFields,
    editFields,
    detailFields,
    masterTimeName,
    masterTimeDisplayName,
    enableKey,
    tableData,
    tableDataRaw,
    loading,
    selectedKeys,
    statData,
    labelCache,
    areaLabelCache,
    pagination,
    searchForm,
    searchTouched,
    activeSort,
    activeViewKind,
    effectivePageSize,
    effectiveSearch,
    insight,
    chartData,
    chartLoading,
    chartError,
    chartSeq,
    isLargePageView,
    viewFilter,
    filterFields,
    searchKeys,
    treeRows,
    tableVisibleCount,
    TABLE_INITIAL_VISIBLE,
    TABLE_LOAD_STEP,
    evpStore,
    measureTableHeight,
  } = ctx;

  /** 列表/树滚动接近底部：追加下一批行（懒加载，避免千条一次性传 VTable） */
  function onTableScrollBottom() {
    const rows = treeRows.value;
    if (tableVisibleCount.value < rows.length) {
      tableVisibleCount.value = Math.min(rows.length, tableVisibleCount.value + TABLE_LOAD_STEP);
    }
  }

  async function hydrateLovLabels(rows: Record<string, unknown>[]) {
    // 仅对仍无 dataSource 的 LIST/其它 LOV 走 BatchLabel；Enum 已在 enrich 阶段灌入
    const lovFields = listFields.value.filter(
      (f) =>
        f.lovCode &&
        resolveListControl(f) === 'lov' &&
        !(f.dataSource && Object.keys(f.dataSource).length),
    );
    for (const f of lovFields) {
      const code = f.lovCode!;
      const values = [
        ...new Set(
          rows
            .map((r) => getValueByKey(r, f.name))
            .filter((v) => v != null && v !== '')
            .map(String),
        ),
      ];
      if (!values.length) continue;
      try {
        const map = await fetchBatchLabel({ lovCode: code, values });
        labelCache[code] = { ...(labelCache[code] || {}), ...map };
        // 回写到字段，后续行/徽章不再重复请求
        f.dataSource = { ...(f.dataSource || {}), ...map };
        // 回写同名 lov 字段到详情/编辑/添加分区，抽屉内直接命中 dataSource（OSC-2608139feb）
        for (const pf of [...detailFields.value, ...editFields.value, ...addFields.value]) {
          if (pf.lovCode === code) pf.dataSource = { ...(pf.dataSource || {}), ...map };
        }
      } catch {
        /* ignore */
      }
    }
  }

  /** 地区/级联叶子批量补标签（OSC-2608139feb）：去重后逐 ID getDetail，单 ID 失败忽略不阻断列表 */
  async function hydrateAreaLabels(rows: Record<string, unknown>[]) {
    const ids = collectCascaderIds(listFields.value, rows);
    for (const id of ids) {
      if (areaLabelCache[String(id)]) continue;
      try {
        const res = await cubeApi.page.getDetail<Record<string, unknown>>('/Cube/Area', id);
        const data = (res as unknown as { data?: Record<string, unknown> })?.data ?? res;
        if (data && typeof data === 'object') {
          const rec = data as Record<string, unknown>;
          const name = (rec.name ?? rec.Name) as unknown;
          mergeAreaLabel(areaLabelCache, id, name);
        }
      } catch {
        /* ignore */
      }
    }
  }

  async function loadFields() {
    const page = await cubeApi.page.getPage(typePath.value);
    // 开发代理未覆盖业务 Area 时，Vite 会返回 index.html 字符串，导致 list 为空
    if (!page || typeof page !== 'object' || Array.isArray(page)) {
      Message.error('GetPage 响应无效：请确认开发代理已转发业务 Area（如 /School）到后端');
      listFields.value = [];
      return;
    }
    const meta = (page.data || {}) as Record<string, unknown>;
    if (typeof meta === 'string' || !meta || Array.isArray(meta)) {
      Message.error('GetPage 未返回 JSON（常为代理未命中）。请重启 pnpm dev 后再试');
      listFields.value = [];
      return;
    }
    pageSetting.value =
      (meta.setting as PageSetting | undefined) ??
      (meta.pageSetting as PageSetting | undefined) ??
      null;
    // 主时间字段与关键字开关（OSC-0016）：setting 透传搜索面板固定控件
    masterTimeName.value = pageSetting.value?.masterTimeName ?? null;
    masterTimeDisplayName.value = pageSetting.value?.masterTimeDisplayName ?? null;
    enableKey.value = pageSetting.value?.enableKey !== false;
    let list = toFieldMetas(
      (meta.list as never) || ((meta.fields as { list?: never })?.list),
    ).filter((f) => !!f.name);
    // GetPage.list 异常为空时回落 GetFields(List)
    if (!list.length) {
      try {
        const fb = await cubeApi.page.getFields(typePath.value, FieldKind.List);
        list = toFieldMetas(fb.data).filter((f) => !!f.name);
      } catch {
        /* ignore */
      }
    }
    const nested = meta.fields as
      | { list?: unknown; search?: unknown; form?: { addForm?: unknown; editForm?: unknown; detail?: unknown } }
      | undefined;
    let search = toFieldMetas((meta.search || nested?.search) as never).filter(
      (f) => !!f.name && !f.primaryKey && f.typeName !== 'Guid',
    );
    let add = toFieldMetas((meta.addForm || nested?.form?.addForm) as never).filter(
      (f) => !!f.name,
    );
    let edit = toFieldMetas((meta.editForm || nested?.form?.editForm) as never).filter(
      (f) => !!f.name,
    );
    let detail = toFieldMetas((meta.detail || nested?.form?.detail) as never).filter(
      (f) => !!f.name,
    );
    // 各分区缺失时按 ViewKind 走 GetFields 兜底，保证表单/搜索有权威元数据（OSC-0009）
    if (!search.length) {
      try {
        const fb = await cubeApi.page.getFields(typePath.value, FieldKind.Search);
        search = toFieldMetas(fb.data).filter(
          (f) => !!f.name && !f.primaryKey && f.typeName !== 'Guid',
        );
      } catch {
        /* ignore */
      }
    }
    if (!add.length) {
      try {
        const fb = await cubeApi.page.getFields(typePath.value, FieldKind.Add);
        add = toFieldMetas(fb.data).filter((f) => !!f.name);
      } catch {
        /* ignore */
      }
    }
    if (!edit.length) {
      try {
        const fb = await cubeApi.page.getFields(typePath.value, FieldKind.Edit);
        edit = toFieldMetas(fb.data).filter((f) => !!f.name);
      } catch {
        /* ignore */
      }
    }
    if (!detail.length) {
      try {
        const fb = await cubeApi.page.getFields(typePath.value, FieldKind.Detail);
        detail = toFieldMetas(fb.data).filter((f) => !!f.name);
      } catch {
        /* ignore */
      }
    }
    // 一次 Meta 灌入 Enum dataSource；再按 Cube.Vue Lookup 补未知 typeName 枚举
    const allFields = [...list, ...search, ...add, ...edit, ...detail];
    await enrichFieldsWithEnumDataSource(allFields);
    await enrichFieldsWithLookup(allFields);
    // 多租户关闭：列表/搜索/表单/多维映射均不再暴露租户字段
    listFields.value = withoutTenant(list);
    searchFields.value = withoutTenant(search);
    addFields.value = withoutTenant(add);
    editFields.value = withoutTenant(edit);
    detailFields.value = withoutTenant(detail);
    const pk = listFields.value.find((f) => f.primaryKey);
    pkField.value = pk?.name || 'id';
  }

  async function loadData(skipFetch = false) {
    // 翻页/重载后以当前页选择为准，避免旧主键残留导致批量删除误用
    selectedKeys.value = [];
    loading.value = true;
    // 数据重载 → 列表/树增量渲染从头开始（前 100 条），滚动再追加
    tableVisibleCount.value = TABLE_INITIAL_VISIBLE;
    try {
      const sort = buildSortPayload(activeSort.value);
      const pageSize = effectivePageSize.value;
      // 甘特图现可翻页（pageIndex 随分页器 current），看板/日历仍固定第一页大加载
      const pageIndex =
        isLargePageView.value && activeViewKind.value !== 'gantt' ? 0 : pagination.current - 1;
      let rows: Record<string, unknown>[];
      if (skipFetch && tableDataRaw.value.length) {
        // 复用已加载原始数据（视图切换/纯前端筛选变化：搜索、排序、加载量未变，避免重复请求后端）
        rows = tableDataRaw.value;
      } else {
        // 视图筛选下推（OSC-260819e483 P2）：有条件才传 viewFilter，后端 SearchData 可下推时服务端过滤；
        // 无法下推时忽略服务端过滤，本页仍由下方 matchesViewFilter 复核（翻页不完整为已知限制）
        const vf = buildViewFilterParam(viewFilter.value);
        const res = await cubeApi.page.getList(typePath.value, {
          pageIndex,
          pageSize,
          ...sort,
          ...effectiveSearch.value,
          ...(vf ? { viewFilter: vf } : {}),
        });
        rows = (res.data as Record<string, unknown>[]) || [];
        tableDataRaw.value = rows;
        statData.value = (res.stat as Record<string, unknown>) ?? null;
        if (res.page) pagination.total = res.page.totalCount || 0;
      }
      // 筛选构建器客户端复核（OSC-0015）：业务重写 Search 的控制器（如 Department.Search
      // 仅处理 id/parentId/enable/visible）与树控制器可能不应用通用等值过滤，对已加载数据
      // 兜底过滤保证筛选生效；普通控制器后端已过滤时此处幂等。同时覆盖 any 多条件 OR 降级。
      if (viewFilter.value.conditions.length) {
        const rawCount = rows.length;
        tableData.value = rows.filter((r) =>
          matchesViewFilter(r, viewFilter.value, filterFields.value),
        );
        // 本页已加载全部数据且后端未按筛选过滤（发生删减）时，纠正 total 反映过滤结果
        if (
          tableData.value.length !== rawCount &&
          pagination.total > 0 &&
          rawCount >= pagination.total
        ) {
          pagination.total = tableData.value.length;
        }
      } else {
        tableData.value = rows;
      }
      if (!skipFetch) {
        await hydrateLovLabels(tableData.value);
        await hydrateAreaLabels(tableData.value);
      }
    } finally {
      loading.value = false;
      // 洞察图表与列表同源（同一 effectiveSearch），随列表刷新；竞态由 chartSeq 保护
      void loadChart();
      // 数据/分页器渲染完成后重测表格高度（default/fill 模式填满可视区，分页器保持可见）
      nextTick(measureTableHeight);
    }
  }

  /** 加载固定图表（OSC-0012 + OSC-260819e483 P5）：showChart 时带有效搜索请求 GetChartData；过期响应丢弃。
   *  渲染优先级：开发者 GetChartData 非空数组 → 开发者图；否则用户 chartOption → applyChartData 当前列表行；否则空态 */
  async function loadChart() {
    if (!insight.value.showChart) {
      chartData.value = [];
      chartError.value = '';
      chartLoading.value = false;
      return;
    }
    const seq = ++chartSeq.value;
    chartLoading.value = true;
    chartError.value = '';
    try {
      const vf = buildViewFilterParam(viewFilter.value);
      const res = await cubeApi.page.getChartData(typePath.value, {
        ...effectiveSearch.value,
        ...(vf ? { viewFilter: vf } : {}),
      });
      if (seq !== chartSeq.value) return;
      const dev = Array.isArray(res.data) ? res.data : [];
      if (dev.length) {
        // 开发者图优先（不改 GetChartData 签名；子类非空数组仍优先于用户 option）
        chartData.value = dev;
      } else if (insight.value.chartOption !== undefined) {
        // 用户 option：applyChartData 当前列表行（数据随当前 GetList，含 search/viewFilter）
        chartData.value = [applyChartData(insight.value.chartOption, tableData.value)];
      } else {
        chartData.value = [];
      }
    } catch (err) {
      if (seq !== chartSeq.value) return;
      chartData.value = [];
      chartError.value = formatApiError(err, '图表加载失败');
    } finally {
      if (seq === chartSeq.value) chartLoading.value = false;
    }
  }

  /** 将基准/已保存条件回填到搜索表单（视图切换、初始加载时调用） */
  function applySearchToForm(params: Record<string, unknown>) {
    Object.keys(searchForm).forEach((k) => delete searchForm[k]);
    Object.assign(searchForm, params);
  }

  function handleSearch() {
    // 显式搜索后有效条件取自表单（OSC-0012）
    searchTouched.value = true;
    pagination.current = 1;
    loadData();
  }

  function handleReset() {
    Object.keys(searchForm).forEach((k) => delete searchForm[k]);
    // 重置查询参数：一并清除当前应用的预定义查询标记（与删除的「清空查询参数」合并）
    evpStore.clearActiveQuery(typePath.value);
    searchTouched.value = true;
    pagination.current = 1;
    loadData();
  }

  /** 应用预定义查询（OSC-0016）：整体回填 searchForm（无残留键）→ 执行 → activeQueryId 由 store 设置 */
  function handleApplyQuery(id: string) {
    const params = evpStore.applyQuery(typePath.value, id);
    if (!params) return;
    applySearchToForm(params);
    searchTouched.value = true;
    pagination.current = 1;
    loadData();
  }

  /** 保存当前查询为预定义（OSC-0016）：store 新增条目并指向，随后自动执行一次查询 */
  function handleSaveQuery(name: string) {
    const params = cleanSearchParams({ ...searchForm }, searchKeys.value);
    evpStore.saveQueryAs(typePath.value, name, params);
    Message.success('已保存为预定义查询');
    searchTouched.value = true;
    pagination.current = 1;
    loadData();
  }

  /** 重命名当前查询（OSC-0016） */
  function handleRenameQuery(id: string, name: string) {
    evpStore.renameQuery(typePath.value, id, name);
    Message.success('已重命名');
  }

  /** 删除预定义查询（OSC-0016）：删除后当前表单参数保留 */
  function handleDeleteQuery(id: string) {
    evpStore.deleteQuery(typePath.value, id);
    Message.success('已删除');
  }

  function onPageChange(page: number) {
    pagination.current = page;
    loadData();
  }

  function onPageSizeChange(size: number) {
    pagination.pageSize = size;
    // 页面级 PageSize：普通视图与甘特图（现可翻页）保存到当前 typePath，不再写全局 workspace
    if (!isLargePageView.value || activeViewKind.value === 'gantt') {
      evpStore.setPageSize(typePath.value, normalizePageSize(size), true);
    }
    pagination.current = 1;
    loadData();
  }

  return {
    hydrateLovLabels,
    hydrateAreaLabels,
    loadFields,
    loadData,
    loadChart,
    applySearchToForm,
    handleSearch,
    handleReset,
    handleApplyQuery,
    handleSaveQuery,
    handleRenameQuery,
    handleDeleteQuery,
    onPageChange,
    onPageSizeChange,
    onTableScrollBottom,
  };
}

export type ListQuery = ReturnType<typeof useListQuery>;

/**
 * Cube 页面引擎纯工厂（业务逻辑单一出口）
 *
 * 设计依据：`docs/architecture/cube-engine.md` §4、`docs/decisions/0005-cube-engine-context.md`。
 * 本文件**不含生命周期与 UI 依赖**：状态、取数、校验、提交都是可直接调用的普通函数，
 * 外部世界（元数据接口、HTTP、提示通道）全部经 `deps` 注入，单测不需要真实后端与配置。
 * 生命周期（`onMounted` / `watch(route.path)` / `provide`）在 `useCubeEngine.ts`，
 * 渲染在 `core/components/CubeTable/`，三者职责互不重叠。
 *
 * 两种驱动模式：
 *   - **元数据驱动**（默认页面主路径）：给 `routePath`，引擎拉 `GET {type}/GetPage` 归一字段；
 *   - **配置驱动**（业务整页 / 嵌入复用）：给 `apiPrefix` + `fields`，引擎跳过 `GetPage`，
 *     直接消费前端字段定义 —— 这是业务页「不写 Section 也能定制整页」的合法出口。
 *
 * 数据层用 `DataSet`（对齐 `TODO.md` 的 P0「集成 DataSet 到默认视图」），
 * 但对草案 §4.2 做了三处契约修正，照抄草案会回归：
 *   1. 更新走 `PUT {type}`、删除走 `DELETE {type}?id=` —— 后端
 *      `NewLife.Cube/Common/EntityController.cs:201` 与 `:24` 都是集合路由，
 *      草案的路径式 `{type}/{id}` 命中不到路由（405）。`core/views/index.vue:266-268`
 *      的注释与 `@newlifex/api-core` 的 `page.remove`（api.ts:324）都是既有佐证。
 *   2. `DataSet.read` 会把 `pageIndex/pageSize` 合并进 transport 回调的 `params`，
 *      草案的 `read: () => ({ params: {...searchModel} })` 把它们丢了，
 *      表现是「翻页后请求仍是第 1 页」。这里改为只交查询条件、分页交数据层。
 *   3. 必填判定复用 `fieldControl.isRequiredField()`，不往共享契约 `FieldMeta`
 *      里加草案设想的 `required` 字段（避免出现两套「必填」口径）。
 */
import { computed, reactive, ref } from 'vue';
import type { AxiosInstance } from 'axios';
import { DataSet } from '../dataset/data-set/DataSet';
import { routeToApiPrefix } from '../utils/url';
import { normalizeOverrideFields, normalizePageMeta } from '../utils/fieldMeta';
import { serializeSubmitModel } from '../utils/fieldControl';
import {
  buildDownloadName,
  buildExportParams,
  buildQueryParams,
  cleanRowForUpdate,
  dateStamp,
  entityNameFromPrefix,
  findMissingRequiredField,
  inferBlobExtension,
  isZeroRowImport,
  parseImportResult,
  resolveRowId,
} from './logic';
import type {
  CubeEngineContext,
  CubeEngineDeps,
  CubeEngineOptions,
  CubeFormMode,
  CubePageMeta,
  ImportResult,
  NormalizedPageFields,
  PageSetting,
} from './types';

/** 未提供任何字段组时的归一空集合（保证视图可无条件消费） */
const EMPTY_FIELDS: NormalizedPageFields = {
  setting: {},
  listFields: [],
  searchFields: [],
  addFields: [],
  editFields: [],
  detailFields: [],
};

/** 解析实体前缀：显式 `apiPrefix` 优先，其次由路由推导（支持多段路由） */
function resolveType(options: CubeEngineOptions): string {
  const explicit = options.apiPrefix?.trim();
  if (explicit) return explicit;
  const routePath = options.routePath?.trim();
  if (routePath) return routeToApiPrefix(routePath);
  return '';
}

/** 从各层错误形态里取可展示的文案（后端业务体、axios 响应、原生 Error） */
function extractMessage(err: unknown): string | null {
  const e = err as {
    message?: string;
    data?: { message?: string };
    response?: { data?: { message?: string } };
  };
  return e?.response?.data?.message ?? e?.data?.message ?? e?.message ?? null;
}

/** 清空 reactive 模型的顶层键（不替换对象引用，保持组件侧的引用稳定） */
function clearModel(model: Record<string, unknown>): void {
  for (const key of Object.keys(model)) delete model[key];
}

/** 详情里的 `'true'/'false'` 字符串需还原为布尔，否则开关控件回显成假 */
function normalizeDetailBooleans(
  source: Record<string, unknown>,
  meta: NormalizedPageFields,
): Record<string, unknown> {
  // 键名大小写不敏感比对：详情回包是 camelCase，元数据字段名是 PascalCase
  const booleanNames = new Set<string>();
  for (const list of [meta.editFields, meta.addFields, meta.detailFields]) {
    for (const field of list) if (field.typeName === 'Boolean') booleanNames.add(field.name.toLowerCase());
  }
  const out: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(source)) {
    const isBoolean = booleanNames.has(key.toLowerCase());
    out[key] = isBoolean && typeof value === 'string' ? value.toLowerCase() === 'true' : value;
  }
  return out;
}

/**
 * 创建页面引擎上下文。
 *
 * @param options 驱动模式与初始配置（`routePath` / `apiPrefix` / `fields` / `pageSize` …）
 * @param deps    外部依赖（元数据、HTTP，以及可选的详情 / 导入导出 / 批量删除覆盖）
 */
export function createCubeEngine(
  options: CubeEngineOptions,
  deps: CubeEngineDeps,
): CubeEngineContext {
  const type = ref(resolveType(options));

  const schemaReady = ref(false);
  const loading = ref(false);
  const submitting = ref(false);
  const error = ref<string | null>(null);
  const keyword = ref('');
  const selected = ref<Record<string, unknown>[]>([]);
  const pendingBlob = ref<{ blob: Blob; name: string } | null>(null);
  const pageSetting = ref<PageSetting>({});
  const dialogVisible = ref(false);
  const formLoading = ref(false);
  const formMode = ref<CubeFormMode>('add');
  const formId = ref<number | string | null>(null);

  const fields = ref<NormalizedPageFields>(EMPTY_FIELDS);
  const searchModel = reactive<Record<string, unknown>>({});
  const formModel = reactive<Record<string, unknown>>({});

  /** 主键字段名（来自列表元数据，回退 `id`）；行数据实际键名由 `resolveRowId` 容错 */
  const idKey = computed(() => fields.value.listFields.find((f) => f.primaryKey)?.name ?? 'id');

  const apiPrefix = computed(() => type.value);

  /** 数据层：`DataSet` 承担列表与分页（`dataKey='data'`、`totalCountKey='page.totalCount'`） */
  const dataSet = new DataSet<Record<string, unknown>, Record<string, unknown>>({
    axios: deps.http as unknown as AxiosInstance,
    idField: 'id',
    pageSize: options.pageSize ?? 20,
    autoQuery: false,
    transport: {
      // params 已含 DataSet 合并的 pageIndex/pageSize，这里只补 url
      read: (props) => ({ url: type.value, method: 'get', params: props.params }),
      create: (props) => ({ url: type.value, method: 'post', data: props.data }),
      update: (props) => ({ url: type.value, method: 'put', data: props.data }),
      destroy: (props) => ({
        url: type.value,
        method: 'delete',
        params: { id: resolveRowId((props.data ?? {}) as Record<string, unknown>, idKey.value) },
      }),
    },
  });

  const list = computed(() => dataSet.data);
  const total = computed(() => dataSet.totalCount);
  const page = computed(() => dataSet.currentPage);
  const pageSize = computed(() => dataSet.pageSize);
  const listFields = computed(() => fields.value.listFields);
  const searchFields = computed(() => fields.value.searchFields);
  const formFields = computed(() =>
    formMode.value === 'add' ? fields.value.addFields : fields.value.editFields,
  );
  const dialogTitle = computed(() =>
    formMode.value === 'add' ? '新增' : formMode.value === 'edit' ? '编辑' : '查看',
  );

  /** 当前查询条件（关键字 + 搜索模型），列表与导出共用同一口径 */
  function currentQuery(): Record<string, unknown> {
    return buildQueryParams({
      keyword: keyword.value,
      filters: searchModel,
      keywordEnabled: options.keyword !== false,
    });
  }

  /** 统一包裹取数：维护 loading、收敛错误、避免未捕获 rejection */
  async function runLoad(action: () => Promise<unknown>): Promise<void> {
    if (!type.value) {
      error.value = 'CubeEngine: 未提供 routePath 或 apiPrefix，无法定位实体接口';
      return;
    }
    loading.value = true;
    error.value = null;
    try {
      await action();
    } catch (err) {
      error.value = extractMessage(err) ?? '请求失败';
    } finally {
      loading.value = false;
    }
  }

  const load = () => runLoad(() => dataSet.read(currentQuery()));

  /** 应用前端字段覆盖（配置驱动），或拉取后端元数据（元数据驱动） */
  async function applyFields(): Promise<void> {
    if (options.fields) {
      const override = options.fields;
      const listO = override.listFields ?? [];
      const addO = override.addFields ?? normalizeOverrideFields(listO, 'form');
      const setting = override.setting ?? {};
      fields.value = {
        setting,
        listFields: listO,
        searchFields: override.searchFields ?? normalizeOverrideFields(listO, 'search'),
        addFields: addO,
        editFields: override.editFields ?? addO,
        detailFields: override.detailFields ?? addO,
      };
      pageSetting.value = setting;
      return;
    }
    if (!type.value) return;
    const meta = (await deps.getPage(type.value)) as CubePageMeta | null | undefined;
    const normalized = normalizePageMeta(meta ?? undefined);
    fields.value = normalized;
    pageSetting.value = normalized.setting;
  }

  /**
   * 初始化搜索模型：一律 `undefined` 表示「不筛选」。
   * Boolean 也必须是 undefined —— 若默认 false，首查会莫名带上「否」条件，
   * 与「打开页面先看全量」的预期相悖。
   */
  function resetSearchModel(): void {
    clearModel(searchModel);
    for (const field of fields.value.searchFields) searchModel[field.name] = undefined;
  }

  /** 表单模型初值：Boolean 默认 false（开关需要确定态），其余 undefined */
  function resetFormModel(): void {
    clearModel(formModel);
    for (const field of fields.value.addFields) {
      formModel[field.name] = field.typeName === 'Boolean' ? false : undefined;
    }
  }

  async function init(): Promise<void> {
    error.value = null;
    try {
      await applyFields();
      resetSearchModel();
      resetFormModel();
      schemaReady.value = true;
      if (options.autoQuery !== false) await load();
    } catch (err) {
      schemaReady.value = false;
      error.value = extractMessage(err) ?? '初始化失败';
    }
  }

  async function reload(next?: { routePath?: string; apiPrefix?: string }): Promise<void> {
    if (next?.apiPrefix) options.apiPrefix = next.apiPrefix;
    else if (next?.routePath) options.routePath = next.routePath;
    type.value = resolveType(options);
    dataSet.currentPage = 1;
    keyword.value = '';
    selected.value = [];
    await init();
  }

  async function search(): Promise<void> {
    dataSet.currentPage = 1;
    await load();
  }

  async function searchByKeyword(value: string): Promise<void> {
    keyword.value = value;
    await search();
  }

  async function resetSearch(): Promise<void> {
    keyword.value = '';
    resetSearchModel();
    await search();
  }

  async function changePage(next: number): Promise<void> {
    dataSet.currentPage = next;
    await load();
  }

  async function changePageSize(size: number): Promise<void> {
    dataSet.pageSize = size;
    // 换每页条数后原页码失去意义，必须回第 1 页
    dataSet.currentPage = 1;
    await load();
  }

  async function setFilter(name: string, value: unknown): Promise<void> {
    searchModel[name] = value;
    await search();
  }

  function setSelected(rows: Record<string, unknown>[]): void {
    selected.value = rows ?? [];
  }

  function openCreate(): void {
    formMode.value = 'add';
    formId.value = null;
    resetFormModel();
    // 新增没有回源过程，直接可编辑
    formLoading.value = false;
    dialogVisible.value = true;
  }

  async function fetchDetail(prefix: string, id: number | string): Promise<Record<string, unknown>> {
    const res = (await deps.http.get(`${prefix}/Detail`, { params: { id } })) as {
      data?: Record<string, unknown>;
    };
    return res?.data ?? (res as Record<string, unknown>) ?? {};
  }

  /**
   * 打开编辑 / 查看：先取详情回显。
   * 列表行是「显示态投影」（LOV 已翻译成标签、可能未下发全部列），
   * 直接铺进表单会把标签当值提交，故一律回源 `GET {type}/Detail?id=`。
   *
   * ⚠️ 模型键名跟随**详情回包**（camelCase），不预先铺一层元数据字段名（PascalCase）：
   * 两套大小写键并存时，必填校验会读到那份 `undefined` 的别名键，表现为
   * 「详情明明有值却提示不能为空」。口径与 `openListFormDialog.ts:173-222` 一致：
   * 只有新增模式才按元数据名铺默认值。
   */
  async function openForm(mode: 'edit' | 'view', row: Record<string, unknown>): Promise<void> {
    const id = resolveRowId(row, idKey.value);
    formMode.value = mode;
    formId.value = id ?? null;
    clearModel(formModel);
    formLoading.value = true;
    dialogVisible.value = true;
    if (id === undefined) {
      formLoading.value = false;
      error.value = `CubeEngine: 行数据缺少主键（${idKey.value}），无法${mode === 'edit' ? '编辑' : '查看'}`;
      return;
    }
    try {
      const raw = deps.getDetail ? await deps.getDetail(type.value, id) : await fetchDetail(type.value, id);
      // 详情回包期间若弹窗已被关掉（用户手快），就不要再把值灌回一个已关闭的模型
      if (!dialogVisible.value || formMode.value !== mode) return;
      const merged = normalizeDetailBooleans(raw ?? {}, fields.value);
      Object.assign(formModel, merged);
      // 详情接口可能不返回主键，而 PUT 需要它定位记录
      if (resolveRowId(formModel, idKey.value) === undefined) formModel[idKey.value] = id;
    } catch (err) {
      error.value = extractMessage(err) ?? '详情加载失败';
    } finally {
      formLoading.value = false;
    }
  }

  function closeDialog(): void {
    dialogVisible.value = false;
  }

  async function submitForm(): Promise<boolean> {
    if (formMode.value === 'view') {
      closeDialog();
      return true;
    }
    const missing = findMissingRequiredField(formModel, formFields.value);
    if (missing) {
      deps.notify?.warning?.(`${missing.displayName ?? missing.name}不能为空`);
      return false;
    }
    const isEdit = formId.value !== null;
    submitting.value = true;
    error.value = null;
    try {
      let data = serializeSubmitModel({ ...formModel }, formFields.value);
      if (isEdit) {
        // 清洗 PascalCase 重复键（列表/详情回包会同时带 enable 与 Enable）
        data = cleanRowForUpdate(data);
        const realKey =
          Object.keys(data).find((k) => k.toLowerCase() === String(idKey.value).toLowerCase()) ??
          idKey.value;
        data[realKey] = formId.value;
        await dataSet.remoteUpdate(data);
      } else {
        await dataSet.create(data);
      }
      deps.notify?.success?.(isEdit ? '更新成功' : '新增成功');
      closeDialog();
      await load();
      return true;
    } catch (err) {
      // 字段级 / 业务错误已由请求层统一弹出，这里只保留引擎内可见的失败态
      error.value = extractMessage(err) ?? '保存失败';
      return false;
    } finally {
      submitting.value = false;
    }
  }

  async function remove(row: Record<string, unknown>): Promise<boolean> {
    if (resolveRowId(row, idKey.value) === undefined) {
      deps.notify?.error?.(`行数据缺少主键（${idKey.value}）`);
      return false;
    }
    submitting.value = true;
    try {
      await dataSet.destroy(row);
      deps.notify?.success?.('删除成功');
      // 删掉当前页最后一条时回退一页，避免停在空页
      if (list.value.length <= 1 && dataSet.currentPage > 1) dataSet.currentPage -= 1;
      await load();
      selected.value = [];
      return true;
    } catch (err) {
      error.value = extractMessage(err) ?? '删除失败';
      return false;
    } finally {
      submitting.value = false;
    }
  }

  async function removeSelected(): Promise<boolean> {
    const ids = selected.value
      .map((row) => resolveRowId(row, idKey.value))
      .filter((id): id is number | string => id !== undefined);
    if (!ids.length) {
      if (!selected.value.length) deps.notify?.warning?.('请先勾选要删除的数据');
      else deps.notify?.error?.('选中行缺少主键，无法删除');
      return false;
    }
    submitting.value = true;
    try {
      if (deps.deleteSelect && !options.batchDeleteSequential) {
        await deps.deleteSelect(type.value, ids, { commaJoin: options.batchDeleteCommaJoin });
      } else {
        // 逐条降级：后端无 DeleteSelect 端点，或显式要求顺序删除时使用
        await Promise.all(
          ids.map((id) => deps.http({ url: type.value, method: 'delete', params: { id } })),
        );
      }
      deps.notify?.success?.(`已删除 ${ids.length} 条`);
      selected.value = [];
      await load();
      return true;
    } catch (err) {
      error.value = extractMessage(err) ?? '批量删除失败';
      return false;
    } finally {
      submitting.value = false;
    }
  }

  /** 导出：引擎只拿 blob 与文件名，`<a download>` 的 DOM 操作留在视图层 */
  async function exportFile(format = 'xlsx'): Promise<string | null> {
    const params = buildExportParams({
      format,
      keyword: keyword.value,
      filters: searchModel,
      keywordEnabled: options.keyword !== false,
    });
    try {
      const blob = deps.exportFile
        ? await deps.exportFile(type.value, params)
        : await fetchBlob(`${type.value}/ExportFile`, params);
      const name = buildDownloadName(
        entityNameFromPrefix(type.value),
        dateStamp(),
        inferBlobExtension(blob.type),
      );
      pendingBlob.value = { blob, name };
      deps.notify?.success?.('导出成功');
      return name;
    } catch (err) {
      error.value = extractMessage(err) ?? '导出失败';
      deps.notify?.error?.(error.value);
      return null;
    }
  }

  async function downloadTemplate(): Promise<string | null> {
    try {
      const blob = deps.downloadTemplate
        ? await deps.downloadTemplate(type.value)
        : await fetchBlob(`${type.value}/DownloadTemplate`, {});
      const name = buildDownloadName(
        `${entityNameFromPrefix(type.value)}导入模板`,
        dateStamp(),
        inferBlobExtension(blob.type),
      );
      pendingBlob.value = { blob, name };
      return name;
    } catch (err) {
      const message = '下载导入模板失败，请确认后端已实现 DownloadTemplate 接口';
      error.value = extractMessage(err) ?? message;
      deps.notify?.error?.(message);
      return null;
    }
  }

  async function fetchBlob(url: string, params: Record<string, unknown>): Promise<Blob> {
    const res = await deps.http.get(url, { responseType: 'blob', params });
    if (res instanceof Blob) return res;
    const body = res as { data?: unknown };
    if (body?.data instanceof Blob) return body.data;
    throw new Error('导出响应不是文件流');
  }

  async function importFile(file: File): Promise<ImportResult | null> {
    try {
      const res = deps.importFile ? await deps.importFile(type.value, file) : await postImport(file);
      const result = parseImportResult(res);
      if (!result.ok) {
        error.value = result.message ?? '导入失败';
        return result;
      }
      // 0 行必须降级为警示：否则「提示导入成功但列表没多一条」会直接误导使用者
      if (isZeroRowImport(result.message)) {
        deps.notify?.warning?.(`${result.message ?? ''}，未导入任何数据，请检查表头是否与导入模板一致`);
      } else {
        deps.notify?.success?.(result.message ?? '导入成功');
      }
      await load();
      return result;
    } catch (err) {
      error.value = extractMessage(err) ?? '导入失败';
      return null;
    }
  }

  async function postImport(file: File): Promise<unknown> {
    const formData = new FormData();
    formData.append('file', file);
    return deps.http.post(`${type.value}/ImportFile`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  }

  return {
    schemaReady,
    loading,
    submitting,
    error,
    list,
    total,
    page,
    pageSize,
    keyword,
    selected,
    pendingBlob,
    searchModel,
    formModel,
    apiPrefix,
    pageSetting,
    listFields,
    searchFields,
    formFields,
    dialogVisible,
    formLoading,
    formMode,
    formId,
    dialogTitle,
    init,
    reload,
    retry: init,
    search,
    searchByKeyword,
    resetSearch,
    changePage,
    changePageSize,
    refresh: load,
    setSelected,
    openCreate,
    openEdit: (row) => openForm('edit', row),
    openView: (row) => openForm('view', row),
    closeDialog,
    submitForm,
    remove,
    removeSelected,
    exportFile,
    downloadTemplate,
    importFile,
    setFilter,
  };
}

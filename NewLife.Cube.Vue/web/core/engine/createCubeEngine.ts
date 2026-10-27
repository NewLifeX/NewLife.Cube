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
 *   - **元数据驱动**（默认页面主路径）：给 `routePath`，引擎拉 `GET {type}/meta` 归一字段；
 *   - **配置驱动**（业务整页 / 嵌入复用）：给 `apiPrefix` + `fields`，引擎跳过 `meta`，
 *     直接消费前端字段定义 —— 这是业务页「不写 Section 也能定制整页」的合法出口。
 *
 * 数据访问**只经注入的 `deps.http`**，不依赖 `core/dataset/DataSet`：
 * `DataSet` 在自身模块作用域 `import axios from '../../utils/request'`，
 * 一旦被引擎牵进来，纯逻辑单测与 CT gallery 就得连带真实请求链与配置虚拟模块
 * （gallery 把 `@newlifex/cube-vue/core/utils/request` 别名成桩，但相对路径 import 不受别名约束）。
 * 引擎要的只是「列表状态 + 分页 + 四个动词」，自己持有反而更小更可测。
 * 注：这与 ADR 0005 草案里「数据层以 DataSet 为底座」的选型不同，属有意偏离。
 *
 * 对草案 §4.2 的三处契约修正（照抄草案会回归）：
 *   1. 更新走 `PUT {type}`、删除走 `DELETE {type}?id=` —— 后端
 *      `NewLife.Cube/Common/EntityController.cs:201` 与 `:24` 都是集合路由，
 *      草案的路径式 `{type}/{id}` 命中不到路由（405）。`core/views/index.vue:266-268`
 *      的注释与 `@newlifex/api-core` 的 `page.remove`（api.ts:324）都是既有佐证。
 *   2. 分页参数由引擎自己带：`pageIndex`（1-based）+ `pageSize` 每次请求现取当前页码，
 *      不依赖调用方回传 —— 草案的 transport 把合并进来的分页参数丢了，
 *      表现是「翻页后请求仍是第 1 页」。
 *   3. 必填判定复用 `fieldControl.isRequiredField()`，不往共享契约 `FieldMeta`
 *      里加草案设想的 `required` 字段（避免出现两套「必填」口径）。
 */
import { computed, reactive, ref } from 'vue';
import { routeToApiPrefix } from '../utils/url';
import { normalizeOverrideFields, normalizePageMeta } from '../utils/fieldMeta';
import { applyPagePreset, getPagePreset, type PagePreset } from './pagePreset';
import { serializeSubmitModel } from '../utils/fieldControl';
import i18n from '../i18n';
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
  BackendToolbarItem,
  CubeEngineContext,
  CubeEngineDeps,
  CubeEngineOptions,
  CubeFormMode,
  CubePageMeta,
  ImportResult,
  NormalizedPageFields,
  PageSetting,
} from './types';

/** 引擎文案翻译：引擎不在组件上下文内，直接走全局 i18n 实例 */
function t(key: string, named?: Record<string, unknown>): string {
  return named ? i18n.global.t(key, named) : i18n.global.t(key);
}

/** 未提供任何字段组时的归一空集合（保证视图可无条件消费） */
const EMPTY_FIELDS: NormalizedPageFields = {
  setting: {},
  listFields: [],
  searchFields: [],
  addFields: [],
  editFields: [],
  detailFields: [],
  ui: {},
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

/**
 * Cube 分页回包 → `{ rows, total, stat }`。
 *
 * 宽容三种既有形状（口径与 `core/views/index.vue:307-316` 一致，避免换引擎就换个解析）：
 * `{ data: [...], page: { totalCount } }`、裸数组、`data` 为单对象。
 * `stat` 是后端 `RetrieveState` 产出的汇总实体（如访问统计的合计行），没有则为 null。
 */
function readListResponse(res: unknown): {
  rows: Record<string, unknown>[];
  total: number;
  stat: Record<string, unknown> | null;
} {
  if (Array.isArray(res)) {
    return { rows: res as Record<string, unknown>[], total: res.length, stat: null };
  }
  const body = (res ?? {}) as {
    data?: unknown;
    page?: { totalCount?: number; total?: number };
    stat?: unknown;
  };
  const rows = Array.isArray(body.data)
    ? (body.data as Record<string, unknown>[])
    : body.data && typeof body.data === 'object'
      ? [body.data as Record<string, unknown>]
      : [];
  const total = body.page?.totalCount ?? body.page?.total ?? rows.length;
  const stat = body.stat && typeof body.stat === 'object' ? (body.stat as Record<string, unknown>) : null;
  return { rows, total, stat };
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
  /** 后端下发的页面 UI 扩展（meta.ui）。核心不解释，原样交给 preset / 渲染器 */
  const ui = ref<Record<string, unknown>>({});
  /** 当前页面生效的呈现配置 */
  const pagePreset = ref<PagePreset | null>(options.preset ?? null);
  const dialogVisible = ref(false);
  const formLoading = ref(false);
  const formMode = ref<CubeFormMode>('add');
  const formId = ref<number | string | null>(null);

  const fields = ref<NormalizedPageFields>(EMPTY_FIELDS);
  /** 后端声明的工具栏自定义按钮 */
  const toolbarItems = ref<BackendToolbarItem[]>([]);
  const searchModel = reactive<Record<string, unknown>>({});
  const formModel = reactive<Record<string, unknown>>({});

  /** 主键字段名（来自列表元数据，回退 `id`）；行数据实际键名由 `resolveRowId` 容错 */
  const idKey = computed(() => fields.value.listFields.find((f) => f.primaryKey)?.name ?? 'id');

  const apiPrefix = computed(() => type.value);

  /**
   * 列表状态由引擎自持（不再经 `DataSet`）：
   * `dataKey='data'` / `totalCountKey='page.totalCount'` 是 Cube 分页回包的既有形状，
   * 与 `@newlifex/api-core` 的 `PageResult` 对齐。
   */
  const listRows = ref<Record<string, unknown>[]>([]);
  const totalRef = ref(0);
  /** 后端 `stat` 汇总行（`Pager.RetrieveState` 产出，如访问统计的合计），无则 null */
  const statRow = ref<Record<string, unknown> | null>(null);
  /** 控制器没有实体元数据（接口型控制器，`GET /meta` 404） */
  const metaMissing = ref(false);
  const pageIndex = ref(1);
  const sizePerPage = ref(options.pageSize ?? 20);

  const list = computed(() => listRows.value);
  const total = computed(() => totalRef.value);
  const page = computed(() => pageIndex.value);
  const pageSize = computed(() => sizePerPage.value);
  const listFields = computed(() => fields.value.listFields);
  const searchFields = computed(() => fields.value.searchFields);
  const hasKeyword = computed(() => fields.value.keyword != null);
  // 元数据就绪前无法判断页面是否有 keyword 项，保持旧行为发送 q；就绪后只按 search 声明决定
  const keywordEnabled = computed(() => options.keyword !== false && (!schemaReady.value || hasKeyword.value));
  const multiKeys = computed(() =>
    searchFields.value.filter((f) => f.multiple || f.itemType === 'multipleSelect').map((f) => f.name),
  );
  const formFields = computed(() =>
    formMode.value === 'add' ? fields.value.addFields : fields.value.editFields,
  );
  const dialogTitle = computed(() =>
    formMode.value === 'add' ? t('engine.add') : formMode.value === 'edit' ? t('engine.edit') : t('engine.view'),
  );

  /** 当前查询条件（关键字 + 搜索模型 + 页面附加条件），列表与导出共用同一口径 */
  function currentQuery(): Record<string, unknown> {
    return buildQueryParams({
      keyword: keyword.value,
      filters: { ...options.extraQuery?.(), ...searchModel },
      keywordEnabled: keywordEnabled.value,
      multiKeys: multiKeys.value,
    });
  }

  /** 统一包裹取数：维护 loading、收敛错误、避免未捕获 rejection */
  async function runLoad(action: () => Promise<unknown>): Promise<void> {
    if (!type.value) {
      error.value = t('engine.missingRoutePath');
      return;
    }
    loading.value = true;
    error.value = null;
    try {
      await action();
    } catch (err) {
      error.value = extractMessage(err) ?? t('engine.requestFailed');
      // 失败必须出声：否则用户只看到一片空表格，分不清「没数据」还是「没取到」
      console.error(t('engine.listLoadFailed'), err);
      deps.notify?.error?.(error.value);
    } finally {
      loading.value = false;
    }
  }

  const load = () =>
    runLoad(async () => {
      const res = await deps.http({
        url: type.value,
        method: 'get',
        params: {
          ...currentQuery(),
          pageIndex: pageIndex.value,
          pageSize: sizePerPage.value,
        },
      });
      const next = readListResponse(res);
      listRows.value = next.rows;
      totalRef.value = next.total;
      statRow.value = next.stat;
    });

  /** 解析当前页面生效的 preset：显式 options.preset 优先，其次按路由路径注册表 */
  function resolvePreset(): PagePreset | null {
    return options.preset ?? getPagePreset(options.routePath) ?? null;
  }

  /** 应用前端字段覆盖（配置驱动），或拉取后端元数据（元数据驱动） */
  async function applyFields(): Promise<void> {
    const preset = resolvePreset();
    pagePreset.value = preset;

    if (options.fields) {
      const override = options.fields;
      const listO = override.listFields ?? [];
      const addO = override.addFields ?? normalizeOverrideFields(listO, 'form');
      const normalized = applyPagePreset(
        {
          setting: override.setting ?? {},
          listFields: listO,
          searchFields: override.searchFields ?? normalizeOverrideFields(listO, 'search'),
          addFields: addO,
          editFields: override.editFields ?? addO,
          detailFields: override.detailFields ?? addO,
          ui: override.ui ?? {},
        },
        preset,
      );
      fields.value = normalized;
      pageSetting.value = normalized.setting;
      ui.value = normalized.ui;
      return;
    }
    if (!type.value) return;
    const meta = (await deps.getPage(type.value)) as CubePageMeta | null | undefined;

    // 接口型控制器没有元数据：标记出来，由列表页渲染说明，不走后面的取数
    if (!meta) {
      metaMissing.value = true;
      return;
    }

    const normalized = applyPagePreset(normalizePageMeta(meta), preset);
    fields.value = normalized;
    pageSetting.value = normalized.setting;
    ui.value = normalized.ui;
    toolbarItems.value = meta?.toolbar ?? [];

    // 树形列表：分页会把父节点裁掉导致树不完整，改为一次取足够多的行
    if (normalized.setting.tree && sizePerPage.value < 1000) sizePerPage.value = 1000;
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
    metaMissing.value = false;
    try {
      await applyFields();
      resetSearchModel();
      resetFormModel();
      schemaReady.value = true;
      // 没有实体元数据的控制器没有列表可查，直接停在说明状态
      if (metaMissing.value) return;
      if (options.autoQuery !== false) await load();
    } catch (err) {
      schemaReady.value = false;
      error.value = extractMessage(err) ?? t('engine.initFailed');
    }
  }

  async function reload(next?: { routePath?: string; apiPrefix?: string }): Promise<void> {
    if (next?.apiPrefix) options.apiPrefix = next.apiPrefix;
    else if (next?.routePath) options.routePath = next.routePath;
    type.value = resolveType(options);
    pageIndex.value = 1;
    keyword.value = '';
    selected.value = [];
    await init();
  }

  async function search(): Promise<void> {
    pageIndex.value = 1;
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
    pageIndex.value = next;
    await load();
  }

  async function changePageSize(size: number): Promise<void> {
    sizePerPage.value = size;
    // 换每页条数后原页码失去意义，必须回第 1 页
    pageIndex.value = 1;
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
    const res = (await deps.http.get(`${prefix}/${encodeURIComponent(String(id))}`)) as {
      data?: Record<string, unknown>;
    };
    return res?.data ?? (res as Record<string, unknown>) ?? {};
  }

  /**
   * 打开编辑 / 查看：先取详情回显。
   * 列表行是「显示态投影」（LOV 已翻译成标签、可能未下发全部列），
   * 直接铺进表单会把标签当值提交，故一律回源 `GET {type}/{id}`。
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
      error.value = t('engine.rowMissingKey', { key: idKey.value, action: mode === 'edit' ? t('engine.edit') : t('engine.view') });
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
      error.value = extractMessage(err) ?? t('engine.detailLoadFailed');
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
      deps.notify?.warning?.(t('engine.required', { field: missing.displayName ?? missing.name }));
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
        await deps.http({ url: `${type.value}/${encodeURIComponent(String(formId.value))}`, method: 'put', data });
      } else {
        await deps.http({ url: type.value, method: 'post', data });
      }
      deps.notify?.success?.(isEdit ? t('engine.updateSuccess') : t('engine.createSuccess'));
      closeDialog();
      await load();
      return true;
    } catch (err) {
      // 字段级 / 业务错误已由请求层统一弹出，这里只保留引擎内可见的失败态
      error.value = extractMessage(err) ?? t('engine.saveFailed');
      return false;
    } finally {
      submitting.value = false;
    }
  }

  async function remove(row: Record<string, unknown>): Promise<boolean> {
    if (resolveRowId(row, idKey.value) === undefined) {
      deps.notify?.error?.(t('engine.rowMissingKeyShort', { key: idKey.value }));
      return false;
    }
    submitting.value = true;
    try {
      await deps.http({ url: `${type.value}/${encodeURIComponent(String(resolveRowId(row, idKey.value)))}`, method: 'delete' });
      deps.notify?.success?.(t('engine.deleteSuccess'));
      // 删掉当前页最后一条时回退一页，避免停在空页
      if (list.value.length <= 1 && pageIndex.value > 1) pageIndex.value -= 1;
      await load();
      selected.value = [];
      return true;
    } catch (err) {
      error.value = extractMessage(err) ?? t('engine.deleteFailed');
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
      if (!selected.value.length) deps.notify?.warning?.(t('engine.selectFirst'));
      else deps.notify?.error?.(t('engine.selectMissingKey'));
      return false;
    }
    submitting.value = true;
    try {
      if (deps.deleteSelect && !options.batchDeleteSequential) {
        await deps.deleteSelect(type.value, ids);
      } else {
        // 逐条降级：显式要求顺序删除时使用
        await Promise.all(
          ids.map((id) => deps.http({ url: `${type.value}/${encodeURIComponent(String(id))}`, method: 'delete' })),
        );
      }
      deps.notify?.success?.(t('engine.deleted', { count: ids.length }));
      selected.value = [];
      await load();
      return true;
    } catch (err) {
      error.value = extractMessage(err) ?? t('engine.batchDeleteFailed');
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
      keywordEnabled: keywordEnabled.value,
      multiKeys: multiKeys.value,
    });
    try {
      const blob = deps.exportFile
        ? await deps.exportFile(type.value, params)
        : await fetchBlob(`${type.value}/export`, params);
      const name = buildDownloadName(
        entityNameFromPrefix(type.value),
        dateStamp(),
        inferBlobExtension(blob.type),
      );
      pendingBlob.value = { blob, name };
      deps.notify?.success?.(t('engine.exportSuccess'));
      return name;
    } catch (err) {
      error.value = extractMessage(err) ?? t('engine.exportFailed');
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
        t('engine.importTemplate', { name: entityNameFromPrefix(type.value) }),
        dateStamp(),
        inferBlobExtension(blob.type),
      );
      pendingBlob.value = { blob, name };
      return name;
    } catch (err) {
      const message = t('engine.templateDownloadFailed');
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
    throw new Error(t('engine.notFileStream'));
  }

  async function importFile(file: File): Promise<ImportResult | null> {
    try {
      const res = deps.importFile ? await deps.importFile(type.value, file) : await postImport(file);
      const result = parseImportResult(res);
      if (!result.ok) {
        error.value = result.message ?? t('engine.importFailed');
        return result;
      }
      // 0 行必须降级为警示：否则「提示导入成功但列表没多一条」会直接误导使用者
      if (isZeroRowImport(result.message)) {
        deps.notify?.warning?.(t('engine.importNothing', { message: result.message ?? '' }));
      } else {
        deps.notify?.success?.(result.message ?? t('engine.importSuccess'));
      }
      await load();
      return result;
    } catch (err) {
      error.value = extractMessage(err) ?? t('engine.importFailed');
      return null;
    }
  }

  async function postImport(file: File): Promise<unknown> {
    const formData = new FormData();
    formData.append('file', file);
    return deps.http.post(`${type.value}/import`, formData, {
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
    stat: computed(() => statRow.value),
    metaMissing: computed(() => metaMissing.value),
    page,
    pageSize,
    keyword,
    hasKeyword,
    selected,
    pendingBlob,
    searchModel,
    formModel,
    apiPrefix,
    pageSetting,
    ui: computed(() => ui.value),
    preset: pagePreset,
    listFields,
    searchFields,
    toolbar: computed(() => toolbarItems.value),
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

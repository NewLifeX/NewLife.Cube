/**
 * Cube 页面引擎类型契约（core/engine）
 *
 * 定位：把 `core/views/index.vue` 里散落的「拉元数据 → 归一字段 → 初始化模型 → 自动首查 →
 * 分页/搜索/增删改」状态与行为收敛成一个可单测的上下文对象（见
 * `docs/architecture/cube-engine.md` §3/§4、`docs/decisions/0005-cube-engine-context.md`）。
 *
 * 本文件只放类型，不含运行时代码，供纯工厂 `createCubeEngine` 与薄 composable
 * `useCubeEngine` 共享，业务侧也可 `import type` 后自行构造 `deps`。
 *
 * ⚠️ 与草案的两处**契约修正**（草案写于后端路由修复之前，照抄会回归）：
 *   1. 更新/删除都是**集合路由**，不是路径式 id：
 *      后端 `NewLife.Cube/Common/EntityController.cs` 只有
 *      `[HttpPut("/api/[area]/[controller]")]`（L201）与
 *      `[HttpDelete("/api/[area]/[controller]")]`（L24，id 走 QueryString）。
 *      草案 §4.2 的 `PUT {type}/{id}`、`DELETE {type}/{id}` 命中不到路由，返回 405
 *      （`core/views/index.vue:266-268` 的注释就是当初踩坑后写的，
 *      `@newlifex/api-core` 的 `page.remove` 亦为 `params:{id}`，见 api.ts:324-325）。
 *   2. 必填判定不新增 `FieldMeta.required`：`core/utils/fieldControl.ts:214-218`
 *      的 `isRequiredField()` 已是全项目唯一口径（由 nullable/primaryKey/控件类型推导），
 *      引擎不再往共享契约里加同义字段，避免出现两套「必填」。
 */
import type { Ref, ComputedRef } from 'vue';
import type { FieldMeta } from '../types/field';

/**
 * 后端 `meta` 下发的页面运行时开关。
 * 字段口径与 `core/views/index.vue` 的 `PageMeta.setting` 保持一致。
 */
export interface PageSetting {
  /** 导航视图形态 */
  navView?: string;
  /** 是否显示标题栏 */
  enableNavbar?: boolean;
  /** 是否显示工具栏 */
  enableToolbar?: boolean;
  /** 是否允许新增。页面能力，不代表当前用户权限 */
  enableAdd?: boolean;
  /** 是否允许勾选（批量操作）。页面能力，不代表当前用户权限 */
  enableSelect?: boolean;
  /** 是否显示页脚 */
  enableFooter?: boolean;
  /** 整页只读（隐藏增删改入口） */
  isReadOnly?: boolean;
  /** 双击行进入编辑 */
  enableTableDoubleClick?: boolean;
  /** 是否按主键排序 */
  orderByKey?: boolean;
  /** 删除是否需要二次确认 */
  doubleDelete?: boolean;
  /** 当前页面菜单编号，用于和用户权限串匹配 */
  menuId?: number;
  /** 页面动作所需权限位。有效权限由前端结合用户信息计算 */
  requiredPermissions?: RequiredPermissions;
}

/** 页面动作所需权限位 */
export interface RequiredPermissions {
  add?: number;
  update?: number;
  delete?: number;
}

/**
 * 后端 `meta` 下发的原始字段（NewLife.Cube `DataField` 序列化结构，camelCase）。
 *
 * 相比 `core/views/index.vue:53-67` 的内联声明，补齐 `required` 与 `precision/scale`
 * 等渲染必需信息（后端有则用，无则前端推导）。
 */
export interface BackendField {
  /** 属性名（PascalCase，如 `Name`） */
  name: string;
  /** 显示名 */
  displayName?: string;
  /** 描述（用作表单占位提示） */
  description?: string;
  /** CLR 类型名 */
  typeName: string;
  /** 后端 ItemType（image / file / json / html / singleSelect …） */
  itemType?: string;
  /** 字符串长度（>=300 归一为大文本） */
  length?: number;
  /** 数值总位数（BindColumn.Precision） */
  precision?: number;
  /** 小数位数（BindColumn.Scale） */
  scale?: number;
  /** 字段分类，用于表单分组 */
  category?: string;
  /** 是否显示。false 时仍作为可用列下发给表格设置 */
  visible?: boolean;
  /** 是否允许空 */
  nullable?: boolean;
  /** 是否主键 */
  primaryKey?: boolean;
  /** 是否只读 */
  readOnly?: boolean;
  /** 映射字段 */
  mapField?: string;
  /** 值集编码 */
  lovCode?: string;
  /** 是否多选 */
  multiple?: boolean;
  /** 后端显式声明必填（可选；缺省时由 `isRequiredField` 推导） */
  required?: boolean;
  /** 渲染器名（对应控制器 View/ItemView/GroupView） */
  view?: string;
  /** 扩展属性，核心不解释 */
  props?: Record<string, unknown>;
  /** 是否非实体列 */
  virtual?: boolean;
  /** 搜索项类型：keyword 由关键字框统一处理 */
  kind?: 'keyword' | 'range';
  /** 区间结束参数名（kind=range） */
  end?: string;
  /** 区间对应的实体字段名（kind=range） */
  field?: string;
  /** 内联枚举字典（如 { "1": "小时", "2": "班次" }），有则免一次 LOV 请求 */
  dataSource?: Record<string, string>;
  /**
   * 级联父字段名（后端对带 `[Parent]` 的枚举子字典下发，如 OrderType → "Section"）。
   * 表单侧由 `FormContent` 转成 `LovSelect` 的 `:parent`，实现「父变过滤子」。
   */
  parentField?: string;
}

/** `GET {type}/meta` 的页面元数据（六组独立字段数组） */
export interface CubePageMeta {
  setting?: PageSetting;
  entity?: { name?: string; displayName?: string; primaryKey?: string };
  list?: BackendField[];
  addForm?: BackendField[];
  editForm?: BackendField[];
  detail?: BackendField[];
  search?: BackendField[];
  /** 工具栏自定义按钮（内置按钮由 setting 控制） */
  toolbar?: BackendToolbarItem[];
}

/**
 * 后端下发的工具栏按钮（`meta.toolbar[]`）。
 * 对齐 MVC 的 `_List_Toolbar_Custom` / `_List_Toolbar_Batch`：
 * `url`/`action` 对应 `data-url`，`include` 对应 `data-fields`，`method` 对应 `data-method`。
 */
export interface BackendToolbarItem {
  name: string;
  displayName?: string;
  icon?: string;
  type?: string;
  kind: 'link' | 'request' | 'upload' | 'custom';
  url?: string;
  action?: string;
  method?: string;
  include?: string[];
  appendQuery?: boolean;
  target?: string;
  accept?: string;
  confirm?: string;
  requiresSelection?: boolean;
  renderer?: string;
}

/** 元数据归一后的字段集合（引擎与视图共用） */
export interface NormalizedPageFields {
  setting: PageSetting;
  listFields: FieldMeta[];
  searchFields: FieldMeta[];
  /** 关键字查询项；为空表示该页面没有关键字搜索 */
  keyword?: FieldMeta;
  addFields: FieldMeta[];
  editFields: FieldMeta[];
  detailFields: FieldMeta[];
}

/** 引擎工厂入参 */
export interface CubeEngineOptions {
  /**
   * 页面路由路径（如 `/device/device-profile`）。
   * 元数据驱动主路径：由 `routeToApiPrefix()` 推导 `apiPrefix`。
   * 与 `apiPrefix` 二选一；两者都缺省时引擎无法定位实体，`init()` 会置错。
   */
  routePath?: string;
  /**
   * 显式实体前缀（如 `/api/Equipments/Equipment`，或不含 `/api` 的
   * `/Equipments/Equipment` —— 请求层 `resolveRequestUrl` 会去重补全）。
   * 优先于 `routePath`，用于嵌入组件、整页自管、或后端未按路由命名场景。
   */
  apiPrefix?: string;
  /**
   * 字段覆盖：提供任一数组即**跳过 `meta` 请求**，直接用前端配置的字段
   * （配置驱动模式，等价于业务页自持 `columns` / `formFields` 的用法）。
   * 未提供的组回退空数组，`setting` 回退 `{}`。
   */
  fields?: Partial<NormalizedPageFields>;
  /** 初始每页条数（默认 20，与 `core/views/index.vue:181` 一致） */
  pageSize?: number;
  /** 是否 `init()` 后自动首查（默认 true） */
  autoQuery?: boolean;
  /** 关键字搜索开关：true 时把 `keyword` 作为 `q` 随列表/导出请求发出（默认 true） */
  keyword?: boolean;
  /**
   * 惰性附加查询条件：每次列表/导出取参时合并（对齐 MES ListPage 的 `queryParams`）。
   * 用于页面级过滤（如看板选中的产线、路由 query 带的状态），不必写进 `searchModel`。
   */
  extraQuery?: () => Record<string, unknown>;
  /** 删除是否需要 `ElMessageBox` 之外的二次确认由视图层决定；此处仅声明后端口径 */
  doubleDelete?: boolean;
  /** 批量删除降级：置 true 时引擎改为逐条 `DELETE {type}/{id}`，不再走 `ids=` 批量端点 */
  batchDeleteSequential?: boolean;
}

/** 引擎外部依赖（全部可注入，便于 Vitest 隔离测试） */
export interface CubeEngineDeps {
  /**
   * 拉取页面元数据。默认实现已把 `ApiResponse<PageMeta>` 解包为 `CubePageMeta`
   * （事实校正：`cubeApi.page.getPage()` 返回 ApiResponse，须取 `.data`，见草案 §0 注 1）。
   */
  getPage: (type: string) => Promise<CubePageMeta | null | undefined>;
  /**
   * HTTP 实例。默认 `core/utils/request`（其返回值已是业务体 `ApiResponse`，
   * 列表按 `{ data, page: { totalCount } }` 解包，blob 场景返回值本身就是文件流）。
   * 不要传 `cubeAxios` / `client`，那是未解包的 `AxiosResponse`，解包口径会错一位。
   */
  http: CubeHttpLike;
  /**
   * 取详情。默认实现走 `GET {type}/{id}`（与 `openListFormDialog` 编辑回显同源），
   * 返回已解包的实体对象。
   */
  getDetail?: (type: string, id: number | string) => Promise<Record<string, unknown>>;
  /** 列表导出。默认实现走 `GET {type}/export` 取 blob */
  exportFile?: (type: string, params: Record<string, unknown>) => Promise<Blob>;
  /**
   * 导入。默认实现走 `POST {type}/import`（multipart）。
   * 返回**原始响应体**（`ApiResponse` 形态），由引擎统一解析 code / message / 行数。
   */
  importFile?: (type: string, file: File) => Promise<unknown>;
  /** 下载导入模板。默认实现走 `GET {type}/DownloadTemplate`（后端自定义 Action，可缺省） */
  downloadTemplate?: (type: string) => Promise<Blob>;
  /** 批量删除。默认实现走 `DELETE {type}?ids=1,2,3`（后端 EntityController.DeleteAll） */
  deleteSelect?: (type: string, keys: (number | string)[]) => Promise<unknown>;
  /**
   * LOV 值集查询（可选注入，用于引擎侧预取标签）。
   * 默认**不启用**：列表/搜索/表单的 LOV 取值已由 `LovSelect` 与
   * `core/components/LovSelect/lovStore.ts` 的会话级缓存承担，引擎再造一份缓存会出现
   * 两套真相；仅在自管渲染时才需注入本项。
   */
  lookup?: (codes: string) => Promise<Record<string, Array<{ label: string; value: unknown }>>>;
  /** 消息通道（默认 element-plus 的 ElMessage）；抽象出来便于单测断言提示 */
  notify?: CubeNotifier;
}

/** 引擎需要的最小 HTTP 形态（可调用 + get/post/put/delete） */
export type CubeHttpLike = ((config: {
  url: string;
  method?: string;
  params?: Record<string, unknown>;
  data?: unknown;
}) => Promise<unknown>) & {
  get: (url: string, config?: Record<string, unknown>) => Promise<unknown>;
  post: (url: string, data?: unknown, config?: Record<string, unknown>) => Promise<unknown>;
  put: (url: string, data?: unknown, config?: Record<string, unknown>) => Promise<unknown>;
  delete: (url: string, config?: Record<string, unknown>) => Promise<unknown>;
};

/** 轻提示通道（供引擎在删除/导入导出等边界给反馈） */
export interface CubeNotifier {
  success?: (message: string) => void;
  warning?: (message: string) => void;
  error?: (message: string) => void;
  info?: (message: string) => void;
}

/** 导入结果（后端 `import` 返回 `ApiResponse`，`message` 内含行数口径） */
export interface ImportResult {
  /** 是否业务成功（`code === 0`） */
  ok: boolean;
  /** 后端返回的结果文案 */
  message?: string;
  /** 实际落库行数（由 `message` 解析，无法解析时为 null） */
  rows: number | null;
  /** 原始响应 */
  raw?: unknown;
}

/** 表单弹窗模式 */
export type CubeFormMode = 'add' | 'edit' | 'view';

/**
 * 引擎暴露给视图的上下文 —— 状态、模型、配置、行为的单一出口。
 *
 * `Ref` / `ComputedRef` 不会自动解包（上下文是普通对象而非 reactive），
 * 内置子组件建议 `toRefs(ctx)` 后在模板使用，避免 `ctx.list.value` 噪声（草案 §8.7）。
 */
export interface CubeEngineContext {
  // ── 状态 ──
  /** 元数据是否已就绪（配置驱动模式下 `init()` 后即 true） */
  schemaReady: Ref<boolean>;
  /** 列表取数中 */
  loading: Ref<boolean>;
  /** 提交/删除等写操作进行中 */
  submitting: Ref<boolean>;
  /** 初始化或请求失败的文案（避免未捕获 rejection 后页面空白） */
  error: Ref<string | null>;
  /** 当前列表数据 */
  list: ComputedRef<Record<string, unknown>[]>;
  /** 总条数 */
  total: ComputedRef<number>;
  /** 当前页码（1-based，与后端 `pageIndex` 口径一致） */
  page: ComputedRef<number>;
  /** 每页条数 */
  pageSize: ComputedRef<number>;
  /** 关键字（后端 `q`） */
  keyword: Ref<string>;
  /** 当前页面是否存在关键字查询项 `kind=keyword` */
  hasKeyword: ComputedRef<boolean>;
  /** 当前勾选行（批量操作用） */
  selected: Ref<Record<string, unknown>[]>;
  /**
   * 待下载文件（导出 / 导入模板的结果容器）。
   * 引擎不碰 DOM：视图层取出后创建 `<a download>` 触发下载并置空。
   */
  pendingBlob: Ref<{ blob: Blob; name: string } | null>;

  // ── 模型 ──
  /** 搜索模型：键为字段名（含 `_min` / `_max` 派生键），值原样随请求发出 */
  searchModel: Record<string, unknown>;
  /** 表单模型 */
  formModel: Record<string, unknown>;

  // ── 配置（归一后） ──
  /** 实体请求前缀（已解析，含 `/api` 与否取决于入参） */
  apiPrefix: ComputedRef<string>;
  /** 页面运行时开关 */
  pageSetting: Ref<PageSetting>;
  /** 列表字段（控件类型由 `resolveListControl` 在组件内解析） */
  listFields: ComputedRef<FieldMeta[]>;
  /** 搜索字段（已过滤主键与 Guid） */
  searchFields: ComputedRef<FieldMeta[]>;
  /** 工具栏自定义按钮 */
  toolbar: ComputedRef<BackendToolbarItem[]>;
  /** 表单字段（按 `formMode` 取 addForm / editForm） */
  formFields: ComputedRef<FieldMeta[]>;

  // ── 表单弹窗状态 ──
  /** 弹窗是否可见 */
  dialogVisible: Ref<boolean>;
  /**
   * 编辑/查看的详情回源中。
   * 弹窗必须等它结束再渲染表单：否则用户已经在新建默认值上敲了字，
   * 详情回包一到就把输入覆盖掉（静默丢数据，且很难复现）。
   */
  formLoading: Ref<boolean>;
  /** 弹窗模式 */
  formMode: Ref<CubeFormMode>;
  /** 当前编辑目标主键（edit/view 必填） */
  formId: Ref<number | string | null>;
  /** 弹窗标题（可由视图覆盖） */
  dialogTitle: ComputedRef<string>;

  // ── 行为 ──
  /** 初始化：拉元数据（或应用字段覆盖）→ 建模 → 可选首查 */
  init: () => Promise<void>;
  /** 切换实体后重初始化（同组件实例复用） */
  reload: (options?: { routePath?: string; apiPrefix?: string }) => Promise<void>;
  /** 失败重试 */
  retry: () => Promise<void>;
  /** 按当前条件查询（回第 1 页由调用方决定，搜索区调用时内部已处理） */
  search: () => Promise<void>;
  /** 关键字变化并查询 */
  searchByKeyword: (value: string) => Promise<void>;
  /** 清空搜索模型并回第 1 页查询 */
  resetSearch: () => Promise<void>;
  /** 翻页 */
  changePage: (page: number) => Promise<void>;
  /** 改每页条数（回第 1 页） */
  changePageSize: (size: number) => Promise<void>;
  /** 保持当前页刷新 */
  refresh: () => Promise<void>;
  /** 设置勾选行（由表格 selection-change 驱动） */
  setSelected: (rows: Record<string, unknown>[]) => void;
  /** 打开新增弹窗 */
  openCreate: () => void;
  /** 打开编辑弹窗（先取详情回显，列表行是「显示态投影」不可直接作表单初值） */
  openEdit: (row: Record<string, unknown>) => Promise<void>;
  /** 打开查看弹窗（只读） */
  openView: (row: Record<string, unknown>) => Promise<void>;
  /** 关闭弹窗 */
  closeDialog: () => void;
  /** 提交表单（含必填校验与多选序列化）；成功返回 true */
  submitForm: () => Promise<boolean>;
  /** 删除单条（不含确认弹窗，确认由视图层负责） */
  remove: (row: Record<string, unknown>) => Promise<boolean>;
  /** 批量删除当前勾选行 */
  removeSelected: () => Promise<boolean>;
  /** 导出（返回文件名，失败返回 null） */
  exportFile: (format?: string) => Promise<string | null>;
  /** 下载导入模板（后端未实现该 Action 时返回 null 并提示） */
  downloadTemplate: () => Promise<string | null>;
  /** 导入文件，返回后端结果（0 行由视图层降级为警示） */
  importFile: (file: File) => Promise<ImportResult | null>;
  /** 供插槽/自定义组件使用：手动把某个字段写入 searchModel 并查询 */
  setFilter: (name: string, value: unknown) => Promise<void>;
}

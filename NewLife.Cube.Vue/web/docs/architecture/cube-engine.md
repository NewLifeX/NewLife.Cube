# Cube 引擎与 CubeTable 组件方案（基于 cube-vue 实际架构）

> **状态：草案 v3（方向调整为「CubeTable 组件 + 插槽自定义」，放弃 Section 覆盖）。** 本文是需求/设计草案，落地前需经代码评审确认。当前代码事实以 `core/`、`apps/`、`configs/` 为准。
>
> **一句话定位**：把 `core/views/index.vue` 里“拉元数据 → 初始化模型 → 自动首查 → 分页/搜索/增删改”的散落逻辑，抽成 `core/engine/` 中**单一、可单测、通过 `provide/inject` 暴露**的页面引擎 `CubeEngine`；再做一个**单一集成组件 `CubeTable`**——它整合搜索、工具栏、表格、分页、表单弹窗，内置按最佳实践拆分的结构化子组件，通过**具名插槽**开放每个区域的自定义覆盖。默认 `index.vue` 只写一行 `<CubeTable />`；需要定制（如自定义搜索渲染）就在对应插槽里写，**不再有 Section 覆盖文件**。

---

## 0. 与原始方案、与本项目事实的差异

原始方案（后端配置优先、前端上下文全自动驱动）方向正确，但其中多数能力**本项目已经具备**，且若干假设与实现不符。本方案是**演进/收敛**，不是另起炉灶。

| 维度 | 原始方案假设 | 本项目实际 | 本方案处理 |
| --- | --- | --- | --- |
| 后端配置 | 下发 `CubeSchema`（单数组 + `inSearch/inTable/inForm` 标志） | `GetPage` 已下发 `PageMeta`：`setting/list/addForm/editForm/detail/search` 六组独立 `BackendField[]`，前端归一为 `FieldMeta[]` | **复用** `PageMeta` + `FieldMeta`；“六数组分离”比单数组加标志更强，不重造 `CubeFieldConfig` |
| 配置接口 | `/api/{moduleCode}/schema` | `GET {type}/GetPage`（`type` 由 `routeToApiPrefix(route.path)` 推导为 `/Area/Controller`） | 改用 `GetPage`；模块标识用 `routeToApiPrefix`，与 `DefaultEntity.vue` / `index.vue` 一致 |
| 路由推导 | `route.meta.moduleCode` | 菜单驱动动态路由，`DefaultEntity.vue` 按 `menuStore.flatMenus` 匹配 `route.path` 解析页面 | 引擎以 `route.path` 推导 `type`，与现有约定一致 |
| 数据访问 | 直接 `import request` 手写 CRUD | 已存在 `usePageApi`（`@newlifex/api-core`）与 `DataSet` 类，且 `index.vue` 仍用裸 `request` 自建逻辑 | **数据层用 `DataSet`**（对齐 `TODO.md` P0）；元数据/LOV 用 `usePageApi`；收敛 `index.vue` 裸请求 |
| 生命周期 | 塞进 `provideCubeContext()` | `onMounted` 仅在 `setup` 同步调用才有效 | 拆为**纯工厂 `createCubeEngine(deps)`**（无生命周期、可单测）+ 薄 composable `useCubeEngine()`（`onMounted(() => ctx.init())`） |
| UI 接入（v2 旧方案） | 所有 Section 直接 `useCubeContext()` | Section 当前是受控 `props/emit` 组件，覆盖机制依赖此契约 | v2：引擎只在页面根消费并适配回 `props/emit`；叶子 Section 保持受控 |
| **UI 接入（v3 本次）** | —— | Section 覆盖机制（`ListSearchBar` 等按文件名覆盖）分散、需多文件、覆盖契约隐式 | **放弃 Section 覆盖**：改为单个 `CubeTable` 集成组件 + **具名插槽**做区域自定义；内置结构化子组件（搜索/工具栏/表格/分页/表单弹窗），每个子组件仍以 `ctx` 为 prop（受控、可测） |
| “0 行 JS 页面” | 靠 `CubeProvider` | `DefaultEntity.vue` 已是 catch-all | 默认 `index.vue` = `<CubeTable />`；定制在插槽内完成 |

> **评审中发现的两处关键事实（已据以修订，v3 仍成立）**：
> 1. `usePageApi.getPage()` 返回 `ApiResponse<PageMeta>`，须经 `.data` 取 `PageMeta`（不是直接返回 `PageMeta`）。
> 2. 项目 `TODO.md` 已把“**集成 DataSet 到默认视图**”列为 P0/高优先级——这直接决定了引擎数据层应选 `DataSet`，而非 `usePageApi` 的 CRUD 方法（后者删除端点为 `?id=` 查询式，与 `index.vue` 当前可用的路径式 `DELETE /{id}` 不一致，贸然切换会破坏删除）。

> **v3 新增方向性决定**：**废除 Section 覆盖机制**。原 `decisions/0004` 与 `reference/route-conventions.md` 描述的“按文件名覆盖 `ListSearchBar` 等 Section”路径，统一收敛为 **`CubeTable` + 插槽**。理由见 §5 与 `decisions/0005`。

---

## 1. 目标与定位

1. **统一数据访问面**：标准 CRUD 的数据与状态收敛到 `DataSet`；元数据与 LOV 走 `usePageApi`。消除 `index.vue` 里裸 `request` 的重复拼接（与 `TODO.md` P0 一致）。
2. **可单测的引擎**：核心逻辑全部落在纯工厂 `createCubeEngine(deps)` 中，依赖（元数据获取、HTTP、路由路径）通过 `deps` 注入，可用 Vitest 隔离测试（符合 `standards/testing-standard.md`）。
3. **业务逻辑与 UI 分离**：引擎持有状态、数据、行为（领域逻辑）；`CubeTable` 及其内置子组件只渲染、只消费上下文，不取数。
4. **单一集成组件 + 插槽覆盖**：把搜索/工具栏/表格/分页/表单弹窗整合进 `CubeTable`，用**具名插槽**取代 Section 覆盖文件——定制更内聚、更易追踪、无散落文件。
5. **渐进式演进**：不一次性重写；分阶段抽引擎 → 做 `CubeTable` → 让默认 `index.vue` 切换为 `<CubeTable />` → 试点迁移 → 移除旧 Section 机制，每阶段可独立验证（对齐 `decisions/0004` 与 `guides/ai-iteration.md`）。

---

## 2. 现状与边界

### 2.1 已有能力（直接复用）

| 能力 | 位置 | 说明 |
| --- | --- | --- |
| 元数据契约 `GetPage` | `core/views/index.vue` `fetchPageMeta()` | 返回 `PageMeta`（setting/list/addForm/editForm/detail/search） |
| 路由→API 前缀 | `core/utils/url.ts` `routeToApiPrefix(path)` | `/device/device-profile` → `/Device/DeviceProfile`（支持多段） |
| 字段归一 + 控件解析 | `core/types/field.ts`、`core/utils/fieldControl.ts` | `FieldMeta` / `ControlType` / `SearchControlType` / `ListControlType` 是全项目唯一真理源 |
| 数据访问门面 | `core/composables/useCubeApi.ts` | `usePageApi().getPage/getList/.../lookup`；`getPage` 返回 `ApiResponse<PageMeta>` |
| 响应式数据集 | `core/dataset/data-set/DataSet.ts` | `read/create/remoteUpdate/destroy`、分页、`dataKey='data'`/`totalCountKey='page.totalCount'`（与 `ApiResponse` 默认对齐），`autoQuery` |
| 页面解析 | `core/pages/DefaultEntity.vue` | 按菜单匹配解析默认列表页 / 覆盖页 / 404 |
| Section 覆盖（**将被废除**） | `core/composables/useSections.ts` | `ListSearchBar/ListTableContent/ListToolbar/ListPagination/...` 可按路由文件名覆盖 |

### 2.2 当前缺口（本方案要补的）

- `index.vue` 自己用裸 `request` 拼 `GetPage`、列表、删除（约 470 行），**没有统一数据访问层**；与 `usePageApi`/`DataSet` 三套并存（`TODO.md` 已标注为高风险）。
- 列表/搜索/表单/分页逻辑全挤在一个文件里，**无法单测**状态流转。
- 没有“页面引擎”概念；业务应用想复用默认取数只能复制或整页覆盖。
- **定制方式碎片化**：Section 覆盖需新建同名文件、靠 `useSections` 隐式解析，定制点分散、不易追踪；本方案用插槽内聚替代。

---

## 3. 契约（复用并扩展现有类型）

**不重新定义字段契约。** 引擎消费现有 `FieldMeta` 与 `GetPage` 六数组，并补齐缺失的 `required` 与内联 `dataSource`。

```ts
// core/engine/types.ts
import type { Ref, ComputedRef } from 'vue'
import type { FieldMeta } from '@newlifex/cube-vue/core/types/field'

/** 从 index.vue 提升的页面设置（运行时开关） */
export interface PageSetting {
  navView?: string
  enableNavbar?: boolean
  enableToolbar?: boolean
  enableAdd?: boolean
  enableKey?: boolean
  enableSelect?: boolean
  enableFooter?: boolean
  isReadOnly?: boolean
  enableTableDoubleClick?: boolean
  orderByKey?: boolean
  doubleDelete?: boolean
}

/** 后端 GetPage 原始字段（从 index.vue 提升为共享类型，并补齐 required/dataSource/mapField） */
export interface BackendField {
  name: string
  displayName?: string
  description?: string
  typeName: string
  itemType?: string
  length?: number
  nullable?: boolean
  primaryKey?: boolean
  readOnly?: boolean
  mapField?: string
  lovCode?: string
  multiple?: boolean
  required?: boolean                 // 新增：前端校验来源
  dataSource?: Record<string, string> // 新增：内联枚举，免一次网络请求
}

/** 引擎暴露给视图根的上下文（业务逻辑的单一出口） */
export interface CubeEngineContext {
  // —— 状态 ——
  schemaReady: Ref<boolean>
  loading: Ref<boolean>
  error: Ref<string | null>          // 初始化/请求失败态，避免未捕获 rejection
  list: ComputedRef<Record<string, unknown>[]>
  total: Ref<number>
  page: Ref<number>
  pageSize: Ref<number>

  // —— 模型（reactive） ——
  searchModel: Record<string, unknown>
  formModel: Record<string, unknown>

  // —— 配置（归一后） ——
  pageSetting: Ref<PageSetting | null>
  searchFields: ComputedRef<FieldMeta[]>   // 注意：是 FieldMeta[]，控件类型由组件内 resolve* 解析
  listFields: ComputedRef<FieldMeta[]>
  formFields: ComputedRef<FieldMeta[]>      // 按 formMode 取 addForm / editForm
  lovOptions: Ref<Record<string, Array<{ label: string; value: unknown }>>>

  // —— 表单弹窗状态 ——
  dialogVisible: Ref<boolean>
  formMode: Ref<'add' | 'edit'>

  // —— 行为 ——
  init: () => Promise<void>
  reload: (routePath?: string) => Promise<void>  // 路由变化重初始化
  search: () => Promise<void>
  resetSearch: () => void
  changePage: (p: number) => void
  changePageSize: (s: number) => void
  refresh: () => Promise<void>
  openCreate: () => void
  openEdit: (row: Record<string, unknown>) => void
  submitForm: () => Promise<void>
  remove: (row: Record<string, unknown>) => Promise<void>
  exportFile: () => Promise<void>
  importFile: (file: File) => Promise<void>
  openChart: () => Promise<void>
  retry: () => Promise<void>
}
```

> **为什么不引入 `SearchFieldMeta` 到引擎？** `SearchFieldMeta` 要求必有 `searchType`，而控件解析（`resolveSearchControl`）本就在列表搜索组件内完成。引擎只提供 `FieldMeta[]`，控件类型留给组件解析，避免重复 `fieldControl.ts` 的真理逻辑。

> **方案术语 ↔ 本项目实现（对照用户整合方案「文档一：Cube 上下文与泛型组件封装规范」的命名）**：
> | 方案草案命名 | 本项目类型 / API | 说明 |
> | --- | --- | --- |
> | `CubeFieldConfig` | `FieldMeta`（`core/types/field.ts`）/ `BackendField`（`core/engine/types.ts`） | 字段契约唯一真理源，不重造 |
> | `CubeSchema` | `CubePageMeta`（`setting/list/addForm/editForm/detail/search` 六数组） | 来自后端 `GetPage` 元数据，前端归一 |
> | `CubeContext` | `CubeEngineContext`（`core/engine/types.ts`） | 引擎暴露给视图根的上下文（状态/模型/行为单一出口） |
> | `provideCubeContext()` | `createCubeEngine(deps)`（纯工厂）+ `useCubeEngine()`（setup/provide） | 生命周期拆出，可单测 |
> | `useCubeContext()` | `useCubeContext()`（`inject` 消费，本项目保留） | 仅作消费端注入，与方案草案语义一致，非弃用名 |

---

## 4. 引擎设计（纯工厂 + 薄 composable，数据层基于 DataSet）

### 4.1 依赖与默认实现

```ts
// core/engine/types.ts（补充 deps 接口）
import type { AxiosInstance } from 'axios'
export interface CubePageMeta {
  setting?: PageSetting
  list?: BackendField[]
  addForm?: BackendField[]
  editForm?: BackendField[]
  detail?: BackendField[]
  search?: BackendField[]
}
export interface CubeEngineDeps {
  /** 拉取页面元数据（默认实现已解包 ApiResponse，返回 CubePageMeta） */
  getPage: (type: string) => Promise<CubePageMeta>
  /** LOV 值集查询（默认 usePageApi.lookup） */
  lookup?: (codes: string) => Promise<Record<string, Array<{ label: string; value: unknown }>>>
  /** DataSet 使用的 HTTP 实例（默认 core/utils/request，返回 ApiResponse） */
  http: AxiosInstance
  /** 当前路由路径（推导 type 与调试标签） */
  routePath: string
}
```

### 4.2 纯工厂 `createCubeEngine`（可单测核心）

```ts
// core/engine/createCubeEngine.ts
import { reactive, ref, computed } from 'vue'
import { DataSet } from '@newlifex/cube-vue/core/dataset/data-set/DataSet'
import { routeToApiPrefix, getValueByKey } from '@newlifex/cube-vue/core/utils/url'
import { toFieldMeta, backendFieldsToFormFields } from '@newlifex/cube-vue/core/utils/fieldMeta'
import type { BackendField, CubeEngineContext, CubeEngineDeps, PageSetting } from './types'

export interface CreateCubeEngineOptions {
  routePath: string
  autoQuery?: boolean
}

export function createCubeEngine(options: CreateCubeEngineOptions, deps: CubeEngineDeps): CubeEngineContext {
  const type = routeToApiPrefix(options.routePath)   // 支持多段路由（/A/B/C）
  const { getPage, lookup, http } = deps

  const schemaReady = ref(false)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const total = ref(0)
  const pageSetting = ref<PageSetting | null>(null)
  const lovOptions = ref<Record<string, Array<{ label: string; value: unknown }>>>({})
  const formMode = ref<'add' | 'edit'>('add')
  const dialogVisible = ref(false)

  const rawSearch = ref<ReturnType<typeof toFieldMeta>[]>([])
  const rawList = ref<ReturnType<typeof toFieldMeta>[]>([])
  const addFields = ref<ReturnType<typeof toFieldMeta>[]>([])
  const editFields = ref<ReturnType<typeof toFieldMeta>[]>([])

  const searchModel = reactive<Record<string, unknown>>({})
  const formModel = reactive<Record<string, unknown>>({})

  // —— 数据层：DataSet 为底座，传输用完整 apiPrefix（避免 usePageApi 仅两段截断 + 删除端点差异）——
  const dataSet = new DataSet<Record<string, unknown>, Record<string, unknown>>({
    axios: http,
    idField: 'id',
    pageSize: 20,
    autoQuery: false,
    transport: {
      // DataSet.read 会自动合并 pageIndex/pageSize 到 params；此处只给 searchModel
      read: () => ({ url: type, method: 'get', params: { ...searchModel } }),
      create: ({ data }) => ({ url: type, method: 'post', data }),
      // 路径式 DELETE/PUT，与 index.vue 当前可用行为一致（不用 usePageApi 的 ?id= 查询式）
      update: ({ data }) => ({ url: `${type}/${getValueByKey(data, 'id')}`, method: 'put', data }),
      destroy: ({ data }) => ({ url: `${type}/${getValueByKey(data, 'id')}`, method: 'delete' }),
    },
  })

  const list = computed(() => dataSet.data)
  const page = computed(() => dataSet.currentPage)
  const pageSize = computed(() => dataSet.pageSize)

  const searchFields = computed(() => rawSearch.value)
  const listFields = computed(() => rawList.value)
  const formFields = computed(() => (formMode.value === 'edit' ? editFields.value : addFields.value))

  async function loadLov(fields: BackendField[]) {
    // 内联 dataSource 直填，免请求
    fields.forEach(f => { if (f.dataSource) lovOptions.value[f.lovCode!] = Object.entries(f.dataSource).map(([value, label]) => ({ value, label })) })
    const codes = [...new Set(fields.filter(f => f.lovCode && !f.dataSource).map(f => f.lovCode!))]
    if (codes.length && lookup) {
      const res = await lookup(codes.join(','))
      Object.entries(res).forEach(([code, opts]) => { lovOptions.value[code] = opts })
    }
  }

  async function init() {
    error.value = null
    try {
      const meta = await getPage(type)           // 默认实现已解包 ApiResponse
      pageSetting.value = meta.setting ?? {}
      rawSearch.value = (meta.search ?? []).filter(f => !f.primaryKey && f.typeName !== 'Guid').map(toFieldMeta)
      rawList.value = (meta.list ?? []).map(toFieldMeta)
      addFields.value = backendFieldsToFormFields(meta.addForm ?? [])
      editFields.value = backendFieldsToFormFields(meta.editForm ?? meta.addForm ?? [])
      await loadLov([...rawSearch.value, ...rawList.value, ...addFields.value, ...editFields.value])
      // 搜索模型：全部 undefined = “不筛选”（Boolean 也默认不筛选，避免首查只显“否”）
      rawSearch.value.forEach(f => { searchModel[f.name] = undefined })
      // 表单模型：Boolean 新增默认 false，其余 undefined
      addFields.value.forEach(f => { formModel[f.name] = f.typeName === 'Boolean' ? false : undefined })
      schemaReady.value = true
      if (options.autoQuery !== false) await search()
    } catch (e) {
      error.value = (e as Error)?.message || '初始化失败'
      schemaReady.value = false
    }
  }

  const search = () => dataSet.read()
  const resetSearch = () => { Object.keys(searchModel).forEach(k => { searchModel[k] = undefined }); dataSet.currentPage = 1; dataSet.read() }
  const changePage = (p: number) => { dataSet.currentPage = p; dataSet.read() }
  const changePageSize = (s: number) => { dataSet.pageSize = s; dataSet.currentPage = 1; dataSet.read() }
  const refresh = () => dataSet.read()

  function openCreate() { formMode.value = 'add'; dialogVisible.value = true; Object.keys(formModel).forEach(k => { formModel[k] = undefined }); addFields.value.forEach(f => { if (f.typeName === 'Boolean') formModel[f.name] = false }) }
  function openEdit(row: Record<string, unknown>) { formMode.value = 'edit'; dialogVisible.value = true; Object.assign(formModel, row) }

  async function submitForm() {
    const id = getValueByKey(formModel, 'id')   // 主键大小写容错（id/ID/Id）
    if (id != null) await dataSet.remoteUpdate(formModel)
    else await dataSet.create(formModel)
    dialogVisible.value = false
    await dataSet.read()
  }
  async function remove(row: Record<string, unknown>) { await dataSet.destroy(row); await dataSet.read() }

  async function exportFile() {
    const blob = await http.get(`${type}/ExportFile`, { responseType: 'blob' })
    downloadBlob(blob as Blob, `${type.split('/').pop()}_${Date.now()}.bin`)
  }
  async function importFile(file: File) {
    const fd = new FormData(); fd.append('file', file)
    await http.post(`${type}/ImportFile`, fd, { headers: { 'Content-Type': 'multipart/form-data' } })
    await dataSet.read()
  }
  async function openChart() {
    const res: any = await http.get(`${type}/GetChartData`)
    chartData.value = Array.isArray(res?.data) ? res.data : []
    chartVisible.value = true
  }

  const reload = async (rp?: string) => { if (rp) type = routeToApiPrefix(rp); await init() }
  const retry = () => init()

  return {
    schemaReady, loading, error, list, total, page, pageSize,
    searchModel, formModel, pageSetting, searchFields, listFields, formFields, lovOptions,
    dialogVisible, formMode,
    init, reload, search, resetSearch, changePage, changePageSize, refresh,
    openCreate, openEdit, submitForm, remove, exportFile, importFile, openChart, retry,
  }
}
```

### 4.3 薄 composable `useCubeEngine`（生命周期 + provide）

```ts
// core/engine/useCubeEngine.ts
import { provide, inject, onMounted, watch, type InjectionKey } from 'vue'
import { useRoute } from 'vue-router'
import { usePageApi } from '@newlifex/cube-vue/core/composables/useCubeApi'
import request from '@newlifex/cube-vue/core/utils/request'
import { createCubeEngine, type CubeEngineContext, type CubePageMeta } from './createCubeEngine'

export const CubeEngineKey: InjectionKey<CubeEngineContext> = Symbol('CubeEngine')

export function useCubeEngine(options: { routePath: string; autoQuery?: boolean }): CubeEngineContext {
  const route = useRoute()
  const api = usePageApi(...splitType(routeToApiPrefix(options.routePath))) // usePageApi 仅用于元数据/LOV
  const ctx = createCubeEngine(
    { routePath: options.routePath, autoQuery: options.autoQuery },
    {
      // 默认 getPage 解包 ApiResponse<PageMeta> → CubePageMeta
      getPage: async (type: string) => (await api.getPage()) as unknown as CubePageMeta, // 实际 .data
      lookup: (codes: string) => api.lookup(codes),
      http: request,                 // DataSet 用裸 request（返回 ApiResponse，匹配其 dataKey 默认值）
      routePath: options.routePath,
    },
  )
  provide(CubeEngineKey, ctx)
  onMounted(() => ctx.init())
  // 路由变化（同组件实例复用）时重新初始化
  watch(() => route.path, () => ctx.reload())
  return ctx
}

export function useCubeContext(): CubeEngineContext {
  const ctx = inject(CubeEngineKey)
  if (!ctx) throw new Error('useCubeContext 必须在 useCubeEngine 之后使用')
  return ctx
}
```

> **设计要点**：纯工厂只接收已构造好的 `deps`（无模块级 `usePageApi` 调用、无生命周期），可在 Vitest 无组件环境直接测试；`usePageApi` 仅在 composable 内调用，`route.path` 通过 `deps.routePath` 注入，避开 `useRoute` 的 setup 限制。

> **命名澄清（避免与方案草案混淆）**：页面根用 `useCubeEngine()` 完成「工厂调用 + `provide(CubeEngineKey)` + `onMounted(init)` + `watch(route.path)`」；子组件 / 插槽内用 `useCubeContext()` 做 `inject` 消费。两者角色不同、不可互替。`useCubeContext()` 是本项目的**合法消费端 API**，与方案草案的 `useCubeContext`（消费语义）一致，并非弃用名。

---

## 5. 视图集成：单一 `CubeTable` 组件 + 具名插槽（废除 Section 覆盖）

**v3 核心决策：放弃 Section 覆盖机制，改用单个 `CubeTable` 集成组件 + 具名插槽自定义。** 理由：

- **内聚**：一次定制无需新建同名文件、无需理解 `useSections` 的隐式解析；覆盖点就在使用处。
- **可追踪**：插槽是显式作用域，Vue DevTools/编辑器一眼可见“此处被定制”，而 Section 覆盖靠文件名约定，易漏。
- **可测试**：`CubeTable` 内置子组件仍以 `ctx` 为 prop（受控），可独立单测；插槽覆盖可在组件树内用 mock `ctx` 验证。
- **DX 更好**：业务应用从“复制 Section 文件”变为“在 `<CubeTable>` 里写插槽”，心智负担更低。

### 5.1 CubeTable 的结构拆分（最佳实践）

`CubeTable` 不做巨石单文件，而是**集成宿主 + 结构化子组件**，每个子组件职责单一、受控、可测：

```
core/components/CubeTable/
├── CubeTable.vue              # 集成宿主：解析 ctx（外部 prop → 注入 → 自动创建）+ 布局 + 插槽分发
├── CubeTableSearch.vue        # 搜索区：el-form + 动态字段（input/select/date 等经 resolveSearchControl 渲染）
├── CubeTableToolbar.vue       # 工具栏：新增/刷新/导入/导出 + #extra 插槽（追加按钮）
├── CubeTableGrid.vue          # 表格：动态列 + 索引列 + 操作列 + #table-row-actions / #col-[prop] 插槽
├── CubeTablePagination.vue    # 分页：绑定 ctx.page/pageSize
├── CubeTableFormDialog.vue    # 新增/编辑弹窗：绑定 ctx.dialogVisible/formModel/submitForm
├── types.ts                   # 插槽作用域类型（CubeTableSlotProps 等）
└── __tests__/                 # 各子组件单测 + CubeTable 组合渲染测试
```

> 内置子组件**全部以 `ctx` 为 prop 接收上下文**（不 `inject`），保持与现有 `ListSearchBar` 等一致的“受控可测”特性，但脱离 Section 覆盖机器。表单弹窗作为 `CubeTable` 内的声明式组件，**在 provider 组件树内**，故可直接消费 `ctx`（含 `dialogVisible`/`formModel`），不再需要命令式 `openListFormDialog` 的 `Teleport` 取数难题。

### 5.2 CubeTable 上下文解析（外部指定 / 自动获取）

```vue
<!-- core/components/CubeTable/CubeTable.vue（骨架） -->
<script setup lang="ts">
import { inject, useRoute } from 'vue'
import { useCubeEngine, CubeEngineKey, type CubeEngineContext } from '@newlifex/cube-vue/core/engine/useCubeEngine'
import CubeTableSearch from './CubeTableSearch.vue'
import CubeTableToolbar from './CubeTableToolbar.vue'
import CubeTableGrid from './CubeTableGrid.vue'
import CubeTablePagination from './CubeTablePagination.vue'
import CubeTableFormDialog from './CubeTableFormDialog.vue'

const props = withDefaults(defineProps<{
  /** 外部指定上下文（嵌入场景 / 自定义引擎）。传入则不再自动创建 */
  context?: CubeEngineContext
  /** 无外部 context 且无注入时，是否自动按当前路由创建引擎（默认 true） */
  autoEngine?: boolean
}>(), { autoEngine: true })

const route = useRoute()
// 三级解析：外部 prop → 已注入（父级 useCubeEngine）→ 自动创建（页面根语义）
const injected = inject(CubeEngineKey, null)
const ctx: CubeEngineContext = props.context
  ?? injected
  ?? (props.autoEngine
        ? useCubeEngine({ routePath: route.path })
        : (() => { throw new Error('CubeTable: 无 context 且无注入，请传入 context 或开启 autoEngine') })())
</script>

<template>
  <div class="cube-table">
    <slot name="header" :ctx="ctx" />

    <slot name="search" :ctx="ctx">
      <CubeTableSearch :ctx="ctx" />
    </slot>

    <slot name="toolbar" :ctx="ctx">
      <CubeTableToolbar :ctx="ctx">
        <template #extra><slot name="toolbar-extra" :ctx="ctx" /></template>
      </CubeTableToolbar>
    </slot>

    <slot name="table" :ctx="ctx">
      <CubeTableGrid :ctx="ctx">
        <template #row-actions="slotProps"><slot name="table-row-actions" v-bind="slotProps" /></template>
        <template #cell="slotProps"><slot :name="`col-${slotProps.field.name}`" v-bind="slotProps" /></template>
      </CubeTableGrid>
    </slot>

    <slot name="pagination" :ctx="ctx">
      <CubeTablePagination :ctx="ctx" />
    </slot>

    <slot name="footer" :ctx="ctx" />

    <slot name="form" :ctx="ctx">
      <CubeTableFormDialog :ctx="ctx" />
    </slot>
  </div>
</template>
```

> **设计要点**：
> - “外部指定上下文”通过 `context` prop：适合把 `CubeTable` 嵌入仪表盘、或自定义页面自带引擎（`const ctx = useCubeEngine({ routePath })` 后传入）。
> - “无上下文自动获取”通过 `inject(CubeEngineKey)` 或 `useCubeEngine()`：默认 `index.vue = <CubeTable />` 即走自动创建路径，渲染完整 CRUD。
> - `.value` 噪声：内置子组件建议 `const { list, loading } = toRefs(props.ctx)` 解包后再在模板使用，避免 `ctx.list.value` 写法。

### 5.3 插槽 API（每个区域均可定制）

| 插槽 | 作用域 `{ ctx }` | 默认渲染 | 用途 |
| --- | --- | --- | --- |
| `header` | `ctx` | 无 | 表格上方横幅（提示/告警/筛选标签） |
| `search` | `ctx` | `CubeTableSearch` | 整块搜索区替换（如自定义搜索条件渲染） |
| `toolbar` | `ctx` | `CubeTableToolbar` | 整块工具栏替换 |
| `toolbar-extra` | `ctx` | 无 | 在默认工具栏**内**追加按钮（最小侵入） |
| `table` | `ctx` | `CubeTableGrid` | 整块表格替换 |
| `table-row-actions` | `{ row, ctx }` | 内置 编辑/删除 | 表格操作列的按钮自定义（方案规范槽名，取代早期 `#row-actions`） |
| `col-[prop]` | `{ row, field, value }` | 内置 文本/tag/image | 按字段名动态定制某列单元格，如 `#col-Status` / `#col-Kind`（取代早期单一兜底槽 `#cell`） |
| `pagination` | `ctx` | `CubeTablePagination` | 整块分页替换 |
| `footer` | `ctx` | 无 | 表格下方区域 |
| `form` | `ctx` | `CubeTableFormDialog` | 整块新增/编辑弹窗替换 |

### 5.4 默认 index.vue（极简）

```vue
<!-- core/views/index.vue（改造后） -->
<template>
  <!-- 默认渲染完整 CRUD；引擎由 CubeTable 自动创建（三级解析之“自动创建”） -->
  <CubeTable />
</template>
```

### 5.5 定制示例（插槽覆盖，不再有 Section 文件）

```vue
<!-- apps/<app>/src/views/<route>/index.vue -->
<template>
  <CubeTable>
    <!-- 自定义搜索区渲染 -->
    <template #search="{ ctx }">
      <MySearchBar :ctx="ctx" />
    </template>

    <!-- 在默认工具栏内追加按钮（最小侵入） -->
    <template #toolbar-extra="{ ctx }">
      <el-button @click="ctx.openChart()">图表</el-button>
    </template>

    <!-- 自定义行操作 -->
    <template #table-row-actions="{ row, ctx }">
      <el-button link type="primary" @click="ctx.openEdit(row)">编辑</el-button>
      <el-button link type="danger"  @click="ctx.remove(row)">删除</el-button>
    </template>

    <!-- 自定义某字段单元格（如状态列中文映射） -->
    <template #col-Status="{ row, field, value }">
      <el-tag v-if="field.name === 'Status'">{{ value ? '启用' : '禁用' }}</el-tag>
      <span v-else>{{ value }}</span>
    </template>
  </CubeTable>
</template>
```

> 定制组件 `MySearchBar` 等有三种取数方式，任选其一：(a) 通过插槽作用域 `{ ctx }` 直接拿到上下文；(b) 调用 `useCubeContext()`（父级 `CubeTable` 已 `provide`）；(c) 由父级 `:ctx` 显式传入。三者等价，推荐插槽作用域或 `:ctx` 以保持显式依赖。

### 5.6 整页覆盖如何获得引擎

业务应用在 `apps/<app>/src/views/<path>/index.vue` 的**整页覆盖**仍是独立组件，不经过默认 `core/views/index.vue`。它若想复用 `CubeTable`：
- 最简：直接 `<CubeTable />`（自动按当前路由创建引擎）。
- 需自定义引擎（不同路由/预载数据）：在自身 `setup` 里 `const ctx = useCubeEngine({ routePath })`，再 `<CubeTable :context="ctx" />`。
- 完全自管、不用 `CubeTable` 也可，但不得使用 `useCubeContext()`（无 provider）。

此约定需同步更新 `reference/route-conventions.md`（见 §8.4）。

---

## 6. 可测试性（贴合 `standards/testing-standard.md`）

**事实校正**：项目**已有**测试基建——`core/__tests__/*.spec.ts`（`fieldControl`、`url`、`usePasswordRules`、`parameter-kind`、`LovSelectTable`、`LoginForm.password-rules`、`FormContent.fieldtypes`）以及 `e2e/fieldtypes-crud.spec.ts`、`e2e/parameter-section.spec.ts`、`ListFormDialog.spec.ts`。因此 P3 是**扩展既有 e2e 回归**，而非从零搭建。

测试对象 = 纯工厂 `createCubeEngine(deps)`。注入假 `getPage`/`lookup`/`http`，不依赖真实 `usePageApi` 与配置。

```ts
// core/engine/__tests__/createCubeEngine.spec.ts
import { describe, it, expect, vi } from 'vitest'
import { createCubeEngine } from '../createCubeEngine'

const meta = {
  setting: { enableAdd: true },
  list: [{ name: 'Name', displayName: '名称', typeName: 'String' }],
  search: [{ name: 'Name', displayName: '名称', typeName: 'String' }],
  addForm: [{ name: 'Name', displayName: '名称', typeName: 'String', required: true }],
  editForm: [{ name: 'Id', displayName: '编号', typeName: 'Int32', primaryKey: true, readOnly: true },
             { name: 'Name', displayName: '名称', typeName: 'String' }],
  detail: [],
}
function makeDeps(overrides: any = {}) {
  const http = { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() } as any
  http.get.mockImplementation((url: string) => {
    if (url.endsWith('/GetPage')) return Promise.resolve({ data: meta })        // 模拟已解包的 getPage
    if (url.endsWith('/ExportFile')) return Promise.resolve(new Blob())
    if (url.endsWith('/GetChartData')) return Promise.resolve({ data: [] })
    return Promise.resolve({ data: [{ id: 1, name: 'admin' }], page: { totalCount: 1 } }) // 列表
  })
  return { getPage: vi.fn().mockResolvedValue(meta), lookup: vi.fn().mockResolvedValue({}), http, routePath: '/device/device-profile', ...overrides }
}

describe('CubeEngine 纯工厂', () => {
  it('init 自动拉取 GetPage 并首查', async () => {
    const d = makeDeps(); const ctx = createCubeEngine({ routePath: d.routePath }, d)
    expect(ctx.schemaReady.value).toBe(false)
    await ctx.init()
    expect(ctx.schemaReady.value).toBe(true)
    expect(d.getPage).toHaveBeenCalledTimes(1)
    expect(ctx.list.value).toEqual([{ id: 1, name: 'admin' }])
    expect(ctx.total.value).toBe(1)
    expect(ctx.searchModel).toHaveProperty('Name')
    expect(ctx.searchModel.Name).toBeUndefined()  // Boolean 之外也默认不筛选
  })

  it('编辑模式应取 editForm（含只读主键），而非 addForm', async () => {
    const d = makeDeps(); const ctx = createCubeEngine({ routePath: d.routePath }, d)
    await ctx.init(); ctx.openEdit({ id: 5, name: 'x' })
    expect(ctx.formFields.value.some(f => f.name === 'Id')).toBe(true)
    expect(ctx.formFields.value.find(f => f.name === 'Id')?.readOnly).toBe(true)
  })

  it('init 失败应设置 error 且不触发 search', async () => {
    const d = makeDeps({ getPage: vi.fn().mockRejectedValue(new Error('boom')) })
    const ctx = createCubeEngine({ routePath: d.routePath }, d)
    await ctx.init()
    expect(ctx.error.value).toBe('boom')
    expect(ctx.schemaReady.value).toBe(false)
    expect(d.http.get).not.toHaveBeenCalledWith('/device/device-profile', expect.anything())
  })

  it('remove 走路径式 DELETE', async () => {
    const d = makeDeps(); const ctx = createCubeEngine({ routePath: d.routePath }, d)
    await ctx.init(); await ctx.remove({ id: 9 })
    expect(d.http.delete).toHaveBeenCalledWith('/Device/DeviceProfile/9')
  })

  it('resetSearch 清空模型并回首页', async () => {
    const d = makeDeps(); const ctx = createCubeEngine({ routePath: d.routePath }, d)
    await ctx.init(); ctx.searchModel.Name = 'x'; await ctx.resetSearch()
    expect(ctx.searchModel.Name).toBeUndefined(); expect(ctx.page.value).toBe(1)
  })
})
```

命令（与规范一致）：`pnpm run test:unit`、`pnpm run test:e2e`、`pnpm run type-check`、`pnpm run lint:eslint`。

测试矩阵应覆盖：搜索参数过滤（空串/null/undefined 丢弃）、`changePage/changePageSize`、LOV 缓存、`autoQuery:false` 不首查、`init` 失败分支、删除端点、导出/导入调用链；以及 `CubeTable` 组合渲染（传 mock `ctx` prop，断言默认子组件渲染、插槽覆盖生效、`table-row-actions`/`col-[prop]` 作用域正确）。

---

## 7. 渐进式落地路线（对齐 ADR 0004 与 ai-iteration）

| 阶段 | 内容 | 验收 | 风险 |
| --- | --- | --- | --- |
| **P0** | 本方案文档 + ADR 0005（评审通过）；更新 `route-conventions.md` 标记 Section 覆盖为遗留、新增 CubeTable 插槽说明 | 评审通过 | — |
| **P1** | 提取 `core/utils/fieldMeta.ts`（`BackendField` + `toFieldMeta` 含 `required/mapField/dataSource` + `backendFieldsToFormFields`）；新增 `core/engine/types.ts`、`createCubeEngine.ts`（DataSet 底座）+ 单测（**不碰 `index.vue`**） | `test:unit` 通过；类型检查通过 | 低：纯新增 |
| **P2** | 新增 `useCubeEngine`/`useCubeContext` + `CubeEngineKey`、错误态/重试、路由 `watch` 重载 | 类型检查通过 | 低 |
| **P3** | 新增 `core/components/CubeTable/`（`CubeTable` + 5 个内置子组件 + `types.ts` + 单测）。`core/views/index.vue` 切换为 `<CubeTable />`（默认走自动创建）。**保留对存量 Section 覆盖的兼容桥**：`index.vue` 用路由 meta/开关决定走 `CubeTable` 还是旧 Section 布局，直至 P6 移除。扩展既有 `e2e/fieldtypes-crud.spec.ts` 覆盖 `CubeTable` 默认 CRUD | 默认 CRUD 仍可用（菜单直达 + 首查 + 分页 + 增删改 + 导出/导入/图表）；`test:e2e` 绿；存量 Section 覆盖页面不回归 | 中：需回归，靠兼容桥 + 扩展 e2e 兜底 |
| **P4** | 试点迁移 1–2 个 `apps/<app>` 页面：将其 Section 覆盖文件改写为 `CubeTable` 插槽；验证插槽覆盖（search/toolbar-extra/table-row-actions/col-[prop]）生效 | 试点页面 CRUD 闭环 + 定制点正确 | 中 |
| **P5** | 推动其余业务应用从 Section 覆盖迁移到 `CubeTable` 插槽；更新 `docs/reference/route-conventions.md` 与示例 | 迁移完成率达标 | 中 |
| **P6** | 移除兼容桥与旧 Section 机制：删除 `useSections.ts`、`core/views/components/ListSearchBar.vue` 等旧 Section 组件、`openListFormDialog` 命令式弹窗（由 `CubeTableFormDialog` 取代）；回流 `TODO.md`/路线图（勾掉“集成 DataSet”条目） | 文档同步；无遗留 Section 引用 | 低（前序阶段已验证） |

每阶段先跑 `test:unit / type-check / lint`，P3/P4 还需扩展后的 Playwright 覆盖“菜单直达 + 刷新 + CRUD 成功/空态/失败”。

---

## 8. 风险与开放问题（已收敛）

1. **数据层选型**：引擎数据层用 `DataSet`（对齐 `TODO.md` P0），元数据/LOV 用 `usePageApi`。`DataSet` 现仅被 `CbTable.vue` 以**类型**引用（非实例化），引擎是其首个规模化落地场景，边界清晰。
2. **删除端点契约**：`usePageApi.remove` 为 `?id=` 查询式，与 `index.vue` 当前可用的路径式 `DELETE /{id}` 不一致。引擎经 `DataSet` 传输统一用**路径式**，避免回归；若后端确为查询式，再单独验证切换。
3. **表单弹窗纳入上下文**：弹窗开关/校验/提交反馈由 `CubeTableFormDialog` 声明式绑定 `ctx.dialogVisible/formModel/submitForm`，弹窗位于 `CubeTable` 组件树内（在 `provide` 之下），可直接消费 `ctx`，不再需要命令式 `openListFormDialog` 的 `Teleport` 取数补偿。
4. **Section 覆盖废除的兼容（最大新增风险）**：存量业务应用靠 `ListSearchBar` 等 Section 文件定制。P3 用“兼容桥”（`index.vue` 按路由 meta 选择 `CubeTable`/旧布局）避免一次性破坏；P4–P6 逐步迁移并删除旧机制。过渡期间两套并存，需 e2e 双覆盖。
5. **校验来源**：`FieldMeta` 新增 `required`（来自 `DataField.required`），表单渲染（`CubeTableFormDialog`）据此显示必填与前端校验；后端 `fieldErrors` 仍由 `usePageApi.onFieldError` 兜底。
6. **i18n / 主题**：字段标签来自服务端 `GetPage`，引擎不翻译（既有约束）；组件层渲染，引擎不持有任何 `--el-*` / 布局类 token（符合 [ADR 0003](../decisions/0003-element-plus-tailwind-design-system.md) 与 [UI 规范](../standards/ui-spec.md)）。
7. **`.value` 书写**：`useCubeContext()` 返回普通对象，嵌套 `Ref` 不会自动解包。推荐内置子组件用 `toRefs(props.ctx)` 解包后在模板使用，避免 `ctx.list.value` 噪声；直接操作 `ctx.searchModel`（reactive）无需 `.value`。
8. **分页基数**：沿用现有 1-based（`pageIndex` 默认 1），与 `index.vue`/`DataSet` 一致；迁移前确认后端非 0-based。
9. **CubeTable 上下文三重解析的歧义**：若自定义页面既 `useCubeEngine()` 提供、又 `<CubeTable />`（无 prop），`CubeTable` 会 `inject` 到该 provider 而不会重复创建（三级解析第二优先）。仅当“自管引擎但不想提供”时才需显式传 `:context` 并关闭 `autoEngine`，文档需明确此约定。

---

## 9. 综合评审结论（多子代理审查 + v3 方向调整）

本方案经四个方向并行审查（架构贴合、可测试性与渐进演进、后端契约/API 契合、UI 分离与 DX），据此修订；并在 v3 据用户决策进一步调整：

- **架构/技术正确性**：修正了 `searchFields` 类型（应为 `FieldMeta[]` 而非 `SearchFieldMeta[]`）、补 `PageSetting`/`BackendField` 类型、`addForm`/`editForm` 按 mode 区分、导出/图表/删除确认/刷新/翻页动作补齐、主键用 `getValueByKey`、LOV 预拉取与缓存。
- **可测试性/演进**：将 `deps` 设为唯一数据源（工厂零副作用、无需配置 stub）；修正 `getPage` 需 `.data` 的事实；P3 增加兼容桥 + 对等测试；明确项目**已有**测试基建，P3 扩展而非新建 e2e；测试矩阵补齐失败分支与参数过滤。
- **后端契约**：修正 `getPage` 返回 `ApiResponse<PageMeta>` 须解包；修正 Boolean 搜索默认应为 `undefined`（非 `false`）；明确删除端点差异并改用路径式（经 DataSet）；补 `required`/`dataSource` 契约与校验来源。
- **UI 分离/DX（v2→v3 再反转）**：v2 曾决定“引擎只在页面根消费、叶子 Section 保持受控”。**v3 据用户决策改为：废除 Section 覆盖，改用单一 `CubeTable` 集成组件 + 具名插槽**。理由：定制内聚、可追踪、可测、DX 更好。内置子组件仍以 `ctx` 为 prop（受控可测），彻底脱离 Section 覆盖机器；表单弹窗随之改为声明式绑定 `ctx`，消除命令式取数难题。
- **数据层**：据 `TODO.md` 与 api-core 源码事实，引擎数据层用“`DataSet` 底座 + `usePageApi` 元数据/LOV”，既对齐既定方向，又化解删除端点与多段路由两处回归风险。

> 另：本方案与 `decisions/0004`（控制器优先 + 渐进覆盖）中“Section 覆盖”路径存在冲突。`decisions/0005`（本 ADR）明确**以 CubeTable 插槽取代 Section 覆盖**为后续方向，`0004` 的相关段落应在 P0 阶段标注“遗留/将被取代”。

# ADR 0005：抽取统一页面引擎（DataSet 底座）+ CubeTable 组件（插槽自定义，废除 Section 覆盖）

**状态：草案（v3 — 视图集成层改为 CubeTable + 插槽，废除 Section 覆盖）**

## 背景

`core/views/index.vue`（约 470 行）自行用裸 `request` 拼接 `GetPage`、列表查询与删除，承载了“拉元数据 → 初始化模型 → 自动首查 → 分页/搜索/增删改”的全部逻辑。与之并存的数据访问能力还有 `usePageApi`（`@newlifex/api-core`）与 `DataSet` 类，三者职责重叠。项目 `TODO.md` 已把“**集成 DataSet 到默认视图**”列为 P0/高优先级——即既定方向是用 `DataSet` 替换手动 request+ref 模式。

结果是：默认 CRUD 的状态流转无法单测；业务应用若想复用默认取数只能复制或整页覆盖。项目已具备后端驱动所需的一切基础（`GetPage` 元数据、`routeToApiPrefix`、`FieldMeta`/`ControlType` 契约、`usePageApi`、`DataSet`），缺的是**一个统一、可测试、通过上下文暴露的页面引擎**，以及一个**统一、可插槽定制的视图集成组件**。

既有 `decisions/0004` 提倡的“Section 覆盖”（按文件名覆盖 `ListSearchBar` 等）在实践中暴露问题：定制点分散到多个同名文件、靠 `useSections` 隐式解析、不易追踪与测试。故 v3 决定**以单个 `CubeTable` 组件 + 具名插槽取代 Section 覆盖**。

## 决策

- 从 `index.vue` 抽取纯工厂 `createCubeEngine(deps)`：输入 `route.path`（推导 `/Area/Controller`，支持多段），依赖（元数据获取、LOV、HTTP）通过 `deps` 注入；负责 GetPage 归一、搜索/表单模型初始化、列表/分页/增删改，不含任何生命周期。
- **数据层以 `DataSet` 为底座**（对齐 `TODO.md` P0）：`DataSet` 的 `read/create/remoteUpdate/destroy` 经完整 `apiPrefix` 传输，删除用**路径式** `DELETE /{id}`（与 `index.vue` 当前可用行为一致，规避 `usePageApi.remove` 的 `?id=` 查询式差异）。元数据与 LOV 走 `usePageApi`（`getPage` 返回 `ApiResponse<PageMeta>`，引擎解包 `.data`；`lookup` 预拉取并缓存）。
- 新增薄 composable `useCubeEngine()`：在 `setup` 内调用工厂、`provide(CubeEngineKey, ctx)`、`onMounted(() => ctx.init())`、并对 `route.path` `watch` 触发 `reload()`；提供 `useCubeContext()` 供整页覆盖等场景消费。
- **视图集成层 = 单一 `CubeTable` 组件 + 具名插槽（v3 新增，取代 Section 覆盖）**：
  - `CubeTable` 整合搜索、工具栏（操作按钮）、表格、分页、新增/编辑弹窗，内置按职责拆分的受控子组件（`CubeTableSearch`/`CubeTableToolbar`/`CubeTableGrid`/`CubeTablePagination`/`CubeTableFormDialog`），每个子组件以 `ctx` 为 prop（受控、可测）。
  - **上下文三级解析**：`CubeTable` 优先用 `context` prop（外部指定）→ 其次 `inject(CubeEngineKey)`（父级已 `useCubeEngine`）→ 最后按当前路由自动 `useCubeEngine()`（页面根语义）。满足“外部指定上下文 / 无上下文自动获取”双重要求。
  - **插槽 API**：每个区域（`search`/`toolbar`/`table`/`pagination`/`form`）均可整块替换；并提供 `header`/`footer` 横幅、`toolbar-extra`（默认工具栏内追加按钮）、`table-row-actions`（行操作）、`col-[prop]`（按字段名动态单元格）等手术式插槽。
  - **废除 Section 覆盖**：不再鼓励/支持按文件名覆盖 `ListSearchBar` 等；业务定制一律在 `<CubeTable>` 的插槽内完成。兼容期内由 `index.vue` 的路由 meta 开关在 `CubeTable` 与旧 Section 布局间切换，最终移除旧机制。
- 复用现有 `FieldMeta`/`ControlType` 与 `GetPage` 六数组结构；`FieldMeta` 扩充 `required` 与内联 `dataSource`（来自 `DataField`），以支撑前端必填校验与免请求枚举。
- 渐进式迁移：先抽引擎+单测（不碰 `index.vue`）→ 做 `CubeTable` → 默认 `index.vue` 切 `<CubeTable />`（兼容桥保留旧 Section 页面）→ 试点迁移 → 移除旧 Section 机制；每阶段可独立验证。

## 理由

- 纯工厂 + 注入依赖使“引擎逻辑”可在 Vitest 中无组件隔离测试（满足 `standards/testing-standard.md`）；项目已有 `core/__tests__/*.spec.ts` 与 `e2e/fieldtypes-crud.spec.ts` 等基建，P3 是扩展而非新建。
- 数据层选 `DataSet` 既对齐 `TODO.md` 既定方向，又化解删除端点与多段路由两处回归风险。
- **`CubeTable` + 插槽优于 Section 覆盖**：定制内聚（一处写完，无需散落同名文件）、可追踪（显式作用域，DevTools/编辑器可见）、可测试（内置子组件仍受控）、DX 更好（从“复制 Section 文件”变为“写插槽”）。
- 内置子组件以 `ctx` 为 prop 保持受控可测；表单弹窗随 `CubeTable` 声明式绑定 `ctx.dialogVisible/formModel/submitForm`，消除命令式 `openListFormDialog` 的 `Teleport` 取数难题。
- 复用而非重建，避免与 `routeToApiPrefix`/`FieldMeta` 体系冲突，迁移成本最低。

## 后果

- `index.vue` 从“逻辑宿主 + Section 适配”降为“`<CubeTable />` 一行外壳”；取数/状态/CRUD 集中到 `core/engine/`，视图集成集中到 `core/components/CubeTable/`，均可单测。
- 业务应用定制从“建 Section 覆盖文件”改为“在 `<CubeTable>` 插槽内写”，整页覆盖仍可优先于默认渲染且须自行调用 `useCubeEngine()` 或传 `:context`。
- 标准实体默认 CRUD 与业务覆盖路径不变；表单弹窗改为声明式、绑定 `ctx`，不再命令式。
- **与 ADR 0004 的冲突**：`0004` 的“Section 覆盖”路径被本 ADR 取代；`0004` 相关段落应在 P0 标注“遗留/将被取代”，并在 `reference/route-conventions.md` 同步更新。
- 演进详细设计与分阶段验收见 [架构文档](../architecture/cube-engine.md)。

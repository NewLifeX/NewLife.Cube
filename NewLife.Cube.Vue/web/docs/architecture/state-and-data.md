# 状态、请求与元数据页面

## 状态边界

Pinia 状态集中在 `core/stores/`：

| Store  | 职责                                             |
| ------ | ------------------------------------------------ |
| `user` | 当前用户与认证相关状态                           |
| `menu` | 菜单树、平铺菜单、当前激活菜单和动态路由注册状态 |
| `tabs` | 已打开页面标签                                   |

页面局部状态留在组件或 composable；跨页面、跨布局共享的状态才进入 Pinia。

## 请求层

`core/utils/request.ts` 是 Axios 入口。它负责：

- 根据 `getConfig().request.baseUrl` 拼接相对 API 地址；
- 注入认证信息与额外请求头；
- 处理 401、未授权、网络错误和响应拦截；
- 保留标准 API 响应对象，并在失败响应时发出通知、抛出错误；
- 二进制下载与非标准响应直接透传。

标准响应和分页辅助工具定义在 `core/utils/response.ts`。详见 [API 契约](../standards/api-contract.md)。

## 默认页面

> **目标架构（ADR 0005）**：默认列表页 `core/views/index.vue` 仅渲染 `<CubeTable />`，由 `CubeEngine`（`core/engine/createCubeEngine.ts` 纯工厂 + `useCubeEngine` composable）按当前路由自动创建上下文，并驱动搜索/工具栏/表格/分页/新增编辑弹窗。字段元数据来自后端 `GetPage`（六数组 `setting/list/addForm/editForm/detail/search`），归一为 `FieldMeta[]`。业务定制一律用 `CubeTable` 具名插槽（见 `reference/route-conventions.md` 与 `guides/customize-page.md`）。

> **当前遗留实现**：过渡期内 `core/views/index.vue` 仍是「`GetPage` + `useSections` 的 Section 组合 + 命令式 `openListFormDialog`」的旧实现（约 470 行）。按 `architecture/cube-engine.md` §7 的 P0–P6 路线，旧实现将逐步被 `CubeTable` + 引擎取代；存量 Section 覆盖页面由兼容桥保留至 P6 移除。详见 `decisions/0005`。

catch-all 解析入口 `core/pages/DefaultEntity.vue` 依据菜单匹配页面路径，渲染应用覆盖页或框架默认列表页，未匹配时回退 `PageNotFound`。

## 数据容器

`core/dataset/data-set/DataSet.ts` 提供 `DataSet`（read/create/remoteUpdate/destroy + 分页）。它是 `CubeEngine` 的数据层底座（`architecture/cube-engine.md` §4），适合具有查询、分页、加载状态和表格展示的实体集合。业务页优先复用现有页面/数据能力，而不是重复封装 Axios 请求和分页状态。

## 国际化与通知

- `core/i18n/` 提供 i18n 实例；全局初始化在 `initApp()`。
- `core/components/Notification.ts` 是统一通知入口；请求层与响应包装器通过它显示错误和反馈。

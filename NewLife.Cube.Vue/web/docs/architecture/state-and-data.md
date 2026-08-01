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

`core/pages/DefaultEntity.vue` 是 catch-all 页面。它依据当前菜单匹配页面路径，并渲染：

- 应用注册的 `DefaultListPage` 覆盖；或
- 框架 `core/views/index.vue` 默认列表页；或
- 未匹配菜单时的 `PageNotFound`。

默认列表/表单页面从后端 `EntityController` 元数据取得字段、搜索和编辑配置，再由 `core/views/components/` 的 Section 组合为可操作界面。

## 数据容器

`core/dataset/` 提供 `DataSet`。它适合具有查询、分页、加载状态和表格展示的实体集合。业务页优先复用现有页面/数据能力，而不是重复封装 Axios 请求和分页状态。

## 国际化与通知

- `core/i18n/` 提供 i18n 实例；全局初始化在 `initApp()`。
- `core/components/Notification.ts` 是统一通知入口；请求层与响应包装器通过它显示错误和反馈。

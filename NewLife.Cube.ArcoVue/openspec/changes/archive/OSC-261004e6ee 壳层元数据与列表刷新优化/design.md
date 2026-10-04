# OSC-261004e6ee Design — 壳层元数据与列表刷新优化

## 0. 适用框架与官方资料

| 场景 | 框架 | 资料 |
| --- | --- | --- |
| 全局状态、缓存和失效 | Pinia + Vue 3 | https://pinia.vuejs.org/core-concepts/actions.html |
| 壳层与列表生命周期 | Vue 3 Composition API | https://vuejs.org/guide/essentials/lifecycle.html |
| 单元测试 | Vitest | https://vitest.dev/guide/mocking.html |

页面 SFC 不新增业务逻辑；缓存、请求去重和刷新调度位于 store/composable 或纯函数。

## 1. 文件级改动地图

| 文件 | 改动 | 保留 |
| --- | --- | --- |
| `web/src/stores/app.ts` | 为 LoginConfig、AI 配置增加成功缓存、in-flight Promise、`force` 刷新与失效；为 Inbox/Workflow 动态刷新增加 in-flight Promise | 现有字段名称、错误回退、顶栏调用 API |
| `web/src/stores/tenant.ts` | `load(force?)` 成功缓存与并发去重；`switchTo` 强制加载；`clear` 清理缓存/Promise | `applyResult`、租户 header/sessionStorage 语义 |
| `web/src/stores/userProfile.ts` | `loadFromServer(force?)` 并发去重；重置会话时清理引用 | 本地偏好先渲染、保存防抖与错误回退 |
| `web/src/layouts/useShellAuth.ts` | 仅在 store 新接口需要时传递明确的 force/失效意图 | 菜单、Profile 与租户初始化职责 |
| `web/src/layouts/useShellToolbar.ts` | 不删除顶栏初始化；依赖 store 对租户与动态角标去重 | 未读/待办可见性和角标语义 |
| `web/src/views/crud/useDefaultList.ts` | 明确初始化与普通刷新调用边界；若抽取调度 helper，则只由 composable 调用 | `bootstrap` 的首次加载/`typePath` 切换顺序、`loadData` 的查询语义 |
| `web/src/views/crud/useListViews.ts` | 将临时筛选/查询应用结果切换为视图级；计算切换前后请求签名，仅安全时复用行数据 | 分组、排序、填色的当前交互与视图切换入口 |
| `web/src/views/crud/useListQuery.ts` 或 `core/utils/listRequestSignature.ts` | 生成可比较的规范化服务端请求签名，并在允许复用时保留行集来源签名 | `loadData(true)` 的本地重绘、LOV/地区标签与图表刷新 |
| `web/src/stores/viewProfile.ts` / `core/utils/viewProfile.ts` | 以视图 ID 存取临时筛选和已应用查询状态，兼容既有实体级 sessionStorage 数据 | `NamedView` 的 `sort`/`sorts`/`filter`/`group`/`format` 线缆兼容 |
| `web/src/views/crud/DefaultList.vue` 及相关工具栏组件 | 仅在发现刷新事件未绑定 `loadData` 时修正绑定 | 现有按钮、权限、分页与多维视图交互 |
| `web/src/stores/*.spec.ts` | 新增/扩展 store 的缓存、并发、失败和失效测试 | 既有 store 测试模式 |
| `web/src/views/crud/*.spec.ts` 或 `core/utils/*Refresh*.spec.ts` | 锁定普通刷新不调用 bootstrap、实体切换调用 bootstrap 的边界 | 不强制挂载完整 SFC |
| `ArcoVue企业中后台迁移方案.md` | 补性能度量口径与壳层缓存策略 | 多维视图产品边界 |
| `Doc/功能清单.md` | 追加或更新 SPA 性能优化事实条目 | 既有编码与状态矩阵 |

## 2. 缓存与刷新状态模型

### 2.1 稳定元数据

每个资源单独维护下列状态，不引入全局通用缓存 Map，避免资源失效边界混淆：

```ts
type StableRequestState<T> = {
  loaded: boolean;
  request: Promise<T> | null;
};
```

成功值仍保存在现有公开 store state（例如 `loginConfig`、`aiConfig`、`items`、`prefs`）；`loaded` 表示该值可复用。`request` 只用于并发去重，完成（成功或失败）后必须置空。

| 资源 | 成功缓存条件 | `force=true` | 失效 |
| --- | --- | --- | --- |
| LoginConfig | 本 store 已成功加载 | 跳过成功缓存、重取 | 登出/身份切换 |
| AI Config | 本 store 已成功加载 | 跳过成功缓存、重取 | 登出/身份切换 |
| UserProfile | `loaded=true` | 跳过成功缓存、重取 | `resetSession` |
| Tenants | `loaded=true` | 跳过成功缓存、重取 | `clear`、切租户成功后 |

规则顺序：若 `force=false` 且已成功加载，立即返回已存在 state；否则若 `request` 非空，返回该 Promise；否则创建请求。失败不可置 `loaded=true`，但保持当前既有的 UI 回退状态。

### 2.2 动态角标

`refreshInboxUnread()` 和 `refreshWorkflowMeta()` 各自维护 `request` Promise：

| 输入 | 进行中 Promise | 新请求 | state 结果 |
| --- | --- | --- | --- |
| 同时第二次刷新 | 返回同一 Promise | 否 | 第一次响应写入 |
| 第一次完成后显式刷新 | 无 | 是 | 新响应覆盖旧值 |
| 请求失败 | Promise 清空 | 下次可重试 | 保持既有回退（0/disabled） |

不得新增 `loaded` 以复用动态角标旧数据。

## 3. 列表生命周期矩阵

`bootstrap()` 和 `loadData()` 是唯一刷新入口；普通刷新不得间接调用 `bootstrap()`。

| 触发源 | `loadFields` | `loadProfile` | `loadData` | 说明 |
| --- |:---:|:---:|:---:| --- |
| 首次挂载 | 是 | 是 | 是 | 维持现有初始化顺序 |
| `typePath` 改变 | 是 | 是 | 是 | 新实体需新元数据与视图配置 |
| 工具栏“刷新” | 否 | 否 | 是 | 保留 LOV/地区水合和洞察图加载 |
| 分页、搜索、筛选、排序 | 否 | 否 | 是 | 以当前参数重新查询 |
| 仅切换本地多维视图且允许复用原始行 | 否 | 否 | `loadData(true)` | 保留现有 skipFetch 策略 |
| 切换租户后的页面重载 | 是 | 是 | 是 | 浏览器刷新与 store 失效后重新初始化 |

`loadData()` 完成后仍调用既有 `hydrateLovLabels`、`hydrateAreaLabels` 和 `loadChart`；本号不得以减少请求为由跳过数据一致性步骤。

### 3.1 多维视图状态隔离

每个 `NamedView` 已在线缆中持有 `sort`/`sorts`、`filter`、`group`、`format`。本号要求运行态也遵守同一隔离边界：

| 状态 | 当前实现事实 | 本号目标 |
| --- | --- | --- |
| 分组 | `NamedView.group`，按 activeViewId 更新 | 保持视图级隔离 |
| 排序 | `NamedView.sort` / `sorts`，按 activeViewId 更新 | 保持视图级隔离 |
| 填色 | `NamedView.format`，按 activeViewId 更新 | 保持视图级隔离 |
| 已保存筛选 | `NamedView.filter`，按 activeViewId 更新 | 保持视图级隔离 |
| 未保存当前筛选 | 当前 `cube:lastQuery:{typePath}` 是实体级，会在切换视图时重新应用 | 改为 `typePath + viewId` 视图级会话状态；旧实体级值首次读取时仅迁入当时活动视图，随后删除旧键 |
| 已应用预定义查询 | 方案目录和 active id 当前为实体级，切换视图会再次应用 | 方案目录仍为实体级；应用状态和结果改为当前视图级，切换视图读取新视图自己的状态 |

筛选的保存与应用规则：

1. 用户在视图 A 应用但未保存的筛选，只影响视图 A；切到 B 后显示 B 的筛选状态。
2. 用户将筛选保存到视图 A，只更新 A 的 `NamedView.filter`；B 保持原有 `filter`。
3. 预定义查询可从任一视图选择，但选择后只成为该视图的当前查询状态；不修改其它视图。
4. “清除所有实体查询”若保留现有产品语义，必须显式作为跨视图命令；普通“清除筛选”只清当前视图。

### 3.2 原始行复用的正确性门槛

`loadData(true)` 不发送 API，只能用于“服务端理论上会返回同一行集”的场景。实现应建立规范化 `ListRequestSignature`，写入最近一次真实 `GetList` 成功返回的原始行旁：

```ts
type ListRequestSignature = {
  typePath: string;
  tenantCode: string;
  pageIndex: number;
  pageSize: number;
  sorts: string;
  search: string;
  viewFilter: string;
  calendarWindow: string;
};
```

- 对象键排序后序列化；空条件统一为同一值，避免等价对象因引用不同误判。
- 切换后的目标签名严格等于 `tableDataRaw` 的来源签名，才允许 `loadData(true)`。
- 仅分组、填色、列显示、卡片/表格等渲染差异不进入签名，可本地重绘。
- 筛选、关键字、排序、分页、日历窗口、租户或实体变化任一不同，必须 `loadData()` 获取服务端正确分页行集；不得只对当前页 `tableDataRaw` 使用 `matchesViewFilter` 作为替代。

## 4. 核心文档影响

| 文档 | 影响 |
| --- | --- |
| `ArcoVue企业中后台迁移方案.md` | 增加前端性能量测口径：Network 总资源数与 Fetch/XHR API 数分离；说明壳层稳定元数据缓存/动态角标去重策略。 |
| `Doc/功能清单.md` | 更新或新增 SPA 性能优化条目，说明壳层元数据会话缓存、动态请求去重和列表刷新边界。 |

## 5. 测试设计

1. app store：同一 tick 两次 `fetchLoginConfig` / `fetchAiConfig` 只调用 API 一次；成功后第三次不请求；失败后下一次会重试；Inbox/Workflow 同时调用一次、完成后再次调用会请求。
2. tenant store：两次并发 `load()` 只调用一次；`switchTo` 后强制第二次加载；`clear()` 后下一次 `load()` 重新请求；失败后重试请求。
3. userProfile store：两次并发 `loadFromServer()` 只请求一次；`resetSession()` 后下一次重新请求；失败不锁死。
4. 列表刷新：普通刷新调度只调用 `loadData`；`typePath` 改变只调用 `bootstrap`；`bootstrap` 首次加载顺序仍为 fields → profile → data。
5. 多视图隔离：A/B 两视图分别配置分组、排序、筛选、填色，切换后逐项恢复各自配置；A 的未保存筛选和预定义查询应用不影响 B。
6. 复用门槛：仅分组/填色/渲染类型不同且签名相等时不请求；任一筛选、排序、分页、关键字、日历窗口、租户不同则发出新 `GetList`，结果以服务端返回为准。
7. 回归：`vue-tsc -b` 通过。手工在浏览器清空 Network、勾选 Fetch/XHR 后进入已登录实体页并点击刷新，确认不重复 `GetPage`/`ViewProfile`/`ViewProfileTemplate`，且动态角标操作后数值更新。

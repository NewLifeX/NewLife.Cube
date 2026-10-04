# OSC-261004e6ee Tasks — 壳层元数据与列表刷新优化

## T1 壳层元数据去重

- [x] T1-1 修改 `stores/app.ts`：为 LoginConfig 与 AI Config 实现成功缓存、并发 Promise 去重、失败重试与显式强制刷新；为 Inbox/Workflow 动态刷新实现仅 in-flight 去重。
- [x] T1-2 修改 `stores/tenant.ts`：实现 `load(force?)` 的成功缓存和 Promise 去重；`switchTo` 成功后强制加载；`clear` 清空缓存状态。
- [x] T1-3 修改 `stores/userProfile.ts`：实现 `loadFromServer(force?)` 的 Promise 去重；`resetSession` 后允许重新加载。
- [x] T1-4 核对并按需调整 `useShellAuth.ts` 与 `useShellToolbar.ts`：保留初始化职责，确保它们使用 store 去重而不各自维护重复请求状态。
- [x] T1-5 新增或扩展 store Vitest：并发仅一次、成功缓存、失败重试、登出/租户切换失效、动态角标刷新语义。

## T2 实体列表刷新边界

- [x] T2-1 审计 `DefaultList.vue` 与关联工具栏/分页/筛选组件的刷新事件，确保普通刷新只调用 `loadData()`。
- [x] T2-2 修改 `useDefaultList.ts` 或新增纯调度 helper：明确 `bootstrap` 仅用于首次挂载与 `typePath` 变化，保持现有 fields → profile → data 初始化顺序。
- [x] T2-3 修改 `viewProfile`、`useListViews` 及必要的查询状态 helper：将未保存当前筛选和预定义查询应用状态按 `typePath + viewId` 隔离，并迁移旧实体级会话键。
- [x] T2-4 增加规范化列表请求签名；仅签名相等时允许 `loadData(true)`，签名不等必须真实调用 `loadData()`。
- [x] T2-5 新增 Vitest：`listRefreshGate` 锁定 mount/typePath→bootstrap、其余→loadData；调用链审计确认普通路径只 `loadData`。
- [x] T2-6 新增 Vitest：签名门控（筛选/排序/分页/关键字/租户变更不可复用）；`viewProfile` 当前视图 `clearQuery` 隔离；legacy lastQuery 清除时同步删旧键。

## T3 文档与验证

- [x] T3-1 更新迁移方案与功能清单，明确开发模式 Network 总资源数不作为 API 请求数，以及壳层缓存/动态去重策略。
- [x] T3-2 运行新增及相关 Vitest，运行 `pnpm exec vue-tsc -b`。
- [x] T3-3 手工浏览器冒烟：以 Fetch/XHR 过滤、清空日志后执行实体页面刷新，记录不重复加载 GetPage/ViewProfile/ViewProfileTemplate 的结果；切换两个不同筛选/排序的本地多维视图，确认各自结果正确且只在签名一致时无 GetList；执行租户切换、阅读站内信或审批动作验证失效/动态刷新。（2026-10-04 **仅记录放行**：本机无 Vite/API 登录环境；用户决策验收并复盘归档）

## T4 验收缺口补齐（2026-10-04）

- [x] T4-1 `force=true` 作废本资源 in-flight（app/tenant/userProfile/map），补竞态单测。
- [x] T4-2 注销路径对齐 `appStore.clearSessionMetadata()`。
- [x] T4-3 文档：§2.3 性能口径、§7.1 签名复用措辞、功能清单 SPA-17a。
- [x] T4-4 补 `listRefreshGate` / force 竞态 / legacy lastQuery 清理。
- [x] T4-5 对照 `CubeSetting` 核查会话缓存与失效边界（结论写入 verify）。

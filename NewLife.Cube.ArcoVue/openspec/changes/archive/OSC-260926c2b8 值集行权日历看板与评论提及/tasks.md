# Tasks

> 执行记录（2026-09-27）：T1–T13、T15、T16、T17 完成；T14 手工冒烟因本机无运行环境（浏览器 + 后端服务），留待验收阶段执行。测试/构建明细见 `verify.md`。

## A. 值集行权

- [x] T1 抽出 `TenantScopeHelper.GetFilter`，`CreateWhere` 的租户分支改为调用它；补一条「Enforce 无上下文得到 1=0」的现有行为回归。
- [x] T2 新增 `LovEntityGuard`：目标菜单 Detail、租户表达式、`DataScopeHelper.GetFilter/CanAccess`，不写 `DataScopeContext.Current`。
- [x] T3 `FetchEntityList` 在关键字条件之后 AND Guard 表达式；无 Detail 返回 403。
- [x] T4 实体 `BatchLabel` 按主键 + `CanAccess` + 租户判定，不可见键省略；`Enum.*` 与外部 HTTP 不进入 Guard。
- [x] T5 `XUnitTest/LovEntityGuardTests.cs` 覆盖 design §1.2 的菜单、租户、行权、枚举四类。（枚举类：Guard 仅接受 IEntityFactory 且不误伤普通实体，「Enum.* 不经 Guard」由 LovController 分支保证；枚举翻译行为由既有 `LovRegistryTests` 回归）

## B. 日历与看板

- [x] T6 `calendarDay.ts`（或 `useCalendarMonth` 导出的纯函数）实现 `canCreateOnCalendarCell`；`calendarDay.spec.ts` 覆盖有权/无权、本月/非本月、无 startField。
- [x] T7 `CalendarMonth.vue`：`+N` 按钮 + `a-popover` 列出当日事件；空白日按矩阵发 `create`；薄 script。
- [x] T8 `DefaultList.vue` 传入 `canAdd`；`useRecordNav.openAdd` 路径上写入 `startField` 本地 0 点，不写 `endField`。
- [x] T9 `toggleCollapsed` + `kanbanCollapse.spec.ts`；折叠写入当前视图 `mapping.collapsedColumns`；`KanbanBoard.vue` 列头切换；`compact` 不折叠也不写配置。折叠改为向上收起（列宽不变、标题横排）。拖动中卡片完全不透明。
- [x] T18 `kanbanMove.ts` + spec：可拖、同列、必填未分组、布尔提交值。
- [x] T19 `KanbanBoard` 原生拖放；折叠列与按钮不接收/不发起拖动；emit `move`。
- [x] T20 `onKanbanMove`：乐观更新、`patchFields` / 审批补丁、`fail>0` 与异常回滚；`DefaultList` 传入 `canDragGroup`。
- [x] T21 回写迁移方案 §7.4 / §10.4 #18 与竞品报告看板行。
- [x] T22 日历月导航（上一月/下一月/今天）移入工具栏「添加记录」之后（`listContext.calendarCursor` + `DefaultList` 工具栏渲染；`CalendarMonth` 月游标改受控）；日历固定一次加载上限 1000 条。
- [x] T23 看板退出大视图恢复分页器（`isLargePageViewKind` 去掉 kanban，`pageIndex/pageSize` 随分页器）；底部不再提示「一次最多加载 1000 条」。
- [x] T24 日历 日/周/月 模式：`layoutDaySpans` 时间轴布局 + `CalendarMonth` 周/日网格 + 工具栏分段；导航简化为「今天 ‹ › 标题」；`shiftCalendarCursor` 按模式位移（含 spec）。
- [x] T25 日/周模式点击时间轴空白新建：`timeGridCreatePayload` 取整点 + 列 `canCreate`；`onCreateFromCalendar` 支持 `hour`（date 字段忽略）；点事件块不触发。
- [x] T26 日历导航移至工具栏右侧、「关键字」输入框前（与甘特缩放同位）。
- [x] T27 周起始改周一：周视图列序 周一…周日；月网格表头与补齐同步；`startOfCalendarWeek`/标题/用例更新。
- [x] T28 修复周/日时间轴表头与内容区错位：表头移入滚动容器并 sticky（与列同宽）。

## C. 评论提及

- [x] T10 `commentMention.ts` + spec：去重、去掉自己、最多 20、删除标签时去掉正文中第一次 `@显示名`。
- [x] T11 `api.ts` 的 `comment.post` 增加可选 `mentionUserIds`。
- [x] T12 `useRecordDrawer` 顶层与回复分别提交；`RecordDrawer.vue` 用已注册图标 `people`，选人走 `getList('/Admin/User')`。

## D. 抽屉与表单体验（会话续补）

- [x] T29 修复记录抽屉（新增/编辑/详情/只读实体分支）内容超高不能滚动：`arco-drawer-body` 保持 `overflow:hidden`（标签栏固定），无标签分支表单/详情直接子级自滚动；各 Tab pane（编辑/详情/历史/评论）自滚动。
- [x] T30 讨论页签输入框改白卡承载（`comment-box` 加 `bg-2`/边框/内边距，默认样式输入框可见，与编辑表单输入框观感一致）；顺带还原 OSC-0009 补丁拼坏的 `.detail-field` 样式块（悬空 `gap` 声明、`.detail-image/.detail-json` 规则归位到 `.detail-field__value` 前）。
- [x] T31 验收补齐（用户决策：先补齐）：① 值集越权端到端（受限账号 403 / BatchLabel `{}` / admin 200 对照；授权已回滚、账号已删）；② 日历预填可见性核查（模型值正确；实体无表单内 DateTime，登记为演示约束）。

## 收尾

- [x] T13 跑新增 XUnit 与 Vitest；`dotnet build` 与 `vue-tsc` 无错误。（明细见 verify.md「执行记录」）
- [x] T14 手工冒烟记入 verify.md：值集越权、日历 `+N` 与空白日、看板折叠刷新仍在、提及请求体。（2026-09-29 验收阶段完成：三层值集拒绝矩阵、`+N` 弹层 292 条、空白日预填模型值、折叠刷新仍在、`mentionUserIds:[5,4]`；明细见 verify.md「验收记录」）
- [x] T15 回写迁移方案 §7.4 / §10.4 #17 #18 / BE-D2 / BE-D3 与竞品报告对应行，标明本号。
- [x] T16 收尾门禁修复（代码审查 F1/F2/F3 + 实现审计 🟡1/🟡2/🟡4）：`FetchRemoteList` 的 entity: 分支补菜单裁决；`TenantScopeHelper.Resolve` 加请求级缓存；实体 `BatchLabel` 改单次主键集合查询 + `CanAccess`；看板随视图切换重建。修复后回归全绿（明细见 verify.md）。
- [x] T17 G5 补控制器级自动化：`NewLife.Cube.Tests/Services/LovControllerGuardTests.cs` 7 例（ListData 403 / 行权范围过滤 / Enforce 无租户空集 / BatchLabel 省略与非法键静默 / Enum 翻译），独立连接名 `LovCtrlGuard` 隔离，7/7 通过。后续项（不阻塞）：🟡3 `GetTenantField` 自定义实现防御与 `WidgetQueryService` 租户助手同步评估。

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
- [x] T9 `toggleCollapsed` + `kanbanCollapse.spec.ts`；折叠记在 `sessionStorage`（实体 + 视图 + 分组字段），刷新后仍收起；`KanbanBoard.vue` 列头切换；`compact` 无折叠钮也不读写记录。折叠改为向上收起（列宽不变、标题横排）。拖动中卡片透明度 0.4。
- [x] T18 `kanbanMove.ts` + spec：可拖、同列、必填未分组、布尔提交值。
- [x] T19 `KanbanBoard` 原生拖放；折叠列与按钮不接收/不发起拖动；emit `move`。
- [x] T20 `onKanbanMove`：乐观更新、`patchFields` / 审批补丁、`fail>0` 与异常回滚；`DefaultList` 传入 `canDragGroup`。
- [x] T21 回写迁移方案 §7.4 / §10.4 #18 与竞品报告看板行。

## C. 评论提及

- [x] T10 `commentMention.ts` + spec：去重、去掉自己、最多 20、删除标签时去掉正文中第一次 `@显示名`。
- [x] T11 `api.ts` 的 `comment.post` 增加可选 `mentionUserIds`。
- [x] T12 `useRecordDrawer` 顶层与回复分别提交；`RecordDrawer.vue` 用已注册图标 `people`，选人走 `getList('/Admin/User')`。

## 收尾

- [x] T13 跑新增 XUnit 与 Vitest；`dotnet build` 与 `vue-tsc` 无错误。（明细见 verify.md「执行记录」）
- [ ] T14 手工冒烟记入 verify.md：值集越权、日历 `+N` 与空白日、看板折叠刷新仍在、提及请求体。（本机无运行环境，留待 `验收 OSC-260926c2b8` 阶段执行）
- [x] T15 回写迁移方案 §7.4 / §10.4 #17 #18 / BE-D2 / BE-D3 与竞品报告对应行，标明本号。
- [x] T16 收尾门禁修复（代码审查 F1/F2/F3 + 实现审计 🟡1/🟡2/🟡4）：`FetchRemoteList` 的 entity: 分支补菜单裁决；`TenantScopeHelper.Resolve` 加请求级缓存；实体 `BatchLabel` 改单次主键集合查询 + `CanAccess`；看板随视图切换重建。修复后回归全绿（明细见 verify.md）。
- [x] T17 G5 补控制器级自动化：`NewLife.Cube.Tests/Services/LovControllerGuardTests.cs` 7 例（ListData 403 / 行权范围过滤 / Enforce 无租户空集 / BatchLabel 省略与非法键静默 / Enum 翻译），独立连接名 `LovCtrlGuard` 隔离，7/7 通过。后续项（不阻塞）：🟡3 `GetTenantField` 自定义实现防御与 `WidgetQueryService` 租户助手同步评估。

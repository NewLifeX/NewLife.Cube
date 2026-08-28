# OSC-26082815a1 — 首页自定义工作台

## 1. 目标愿景

登录后的 `/home` 成为可配置的「我的工作台」：管理员与普通用户各自看到 CubeNC 预定义部件的角色种子，并可在平台目录内增删排序；配置按 **用户 > 主角色 > 系统默认** 整份读取，与运维监控页分离。

- 目标 1：`GET /Cube/Workbench` 按当前用户解析出 `{ source, roleId, config }`；个人 `UserProfile.HomeJson` 有效则整份采用，否则主角色 Parameter `Workbench.Role`，再否则系统种子（系统角色 / 普通用户两套）。
- 目标 2：工作台 Catalog 的 **named 可用部件 = CubeNC `Widgets/System` 13 个预定义 Widget 的 SPA 对等实现**（`ICubeWidget`，不引用 Razor `IWidget`）；`AdminOnly` 与 MVC 一致；另补 `Inbox` 覆盖迁移方案待办槽。
- 目标 3：ArcoVue `/home` 挂 `WidgetHost`（`surface=workbench`）渲染种子与用户编排；`/Admin/Index` 仍为 DefaultHome 监控页。用户可恢复默认；管理员可写角色模板。
- 目标 4：洞察槽 Catalog/PUT/Host **仅** `metricCard` / `miniChart`（禁用 `miniKanban` / `dataList` / `dataCard`）；工作台 **开放** 数据看板 / 数据列表 / 数据卡片，并允许从 Sources 钉已授权实体的指标卡/迷你图（次于 named 目录）。

## 2. 为何做

迁移方案 §8.5.2 已拍板首页工作台分层，OSC-2608280e9e 把 Host 做成可复用运行时但 **明确不做首页**。今日 `/home` 与 `/Admin/Index` 都是进程/程序集监控页，普通用户没有「门厅」。CubeNC Razor 工作台已有 13 个预定义部件与管理员/个人两套默认，数据逻辑可平移，UI 必须用 Arco + 既有 Widget 协议重做，禁止搬 `WidgetManager` / cshtml。

## 3. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | **一个 OSC**。`tasks.md` 分 P1→P6。前端不得在 P1 API 未就绪时接线；可先写纯函数/Vitest。 |
| 2 | 皮肤主实现 **ArcoVue**；契约落 **Cube WebAPI + `@cube/api-core`**。Cube.Vue / NaiveUI **不改 UI**。CubeNC `Widgets/IWidget` **不引用、不改、不删除**。 |
| 3 | 角色层 **不新建 `RoleWorkspace` 表**。`Parameter(UserID=0, Category=Workbench.Role, Name={roleId})` 的 `LongValue` 存角色 JSON。用户层 `UserProfile` **只加列 `HomeJson`**，不塞进 `WorkspaceJson`。 |
| 4 | named 目录以 CubeNC 13 件为准（见 §4 对照表）+ `Inbox`。Greeting 为页面横幅（非 Catalog 项），始终渲染。 |
| 5 | 工作台 `layout.w` ∈ `{2,3,4,6,8,12}`（对齐 MVC Cols=2 的 6 KPI 一行与 Monitor Cols=8）。洞察槽校验仍只允许 `{3,4,6,12}`。 |
| 6 | 上限工作台 **16** 张 / 64KiB；洞察槽仍 12。整份读取，不做 widget-id 合并。`PUT ""` 清除个人域并继承。 |
| 7 | 不做整页画布、自由 x/y、第三方市场、租户层工作台、把 `Role.Ex6` 当 JSON、用 `UserId=0` UserProfile 假扮角色。 |
| 8 | 流程引擎待办未引入模块则不注册 `flowTasks`；本号待办 = 站内信 `Inbox`。 |

## 4. CubeNC → SPA named 对照（可用 Widget）

数据逻辑从 CubeNC `Widgets/System/*.cs` **复制进** `NewLife.Cube/Widgets/Workbench/`，实现 `ICubeWidget`。`GetData` 契约改为可 JSON 序列化的匿名/字典（禁止返回 Razor HTML / CubeNC `ECharts` 类型）。

| CubeNC Name | Title | MVC Type | AdminOnly | SPA kind | 默认 w | 默认种子 |
| --- | --- | --- | --- | --- | --- | --- |
| UserCount | 用户总数 | Kpi | 是 | metricCard | 2 | 仅 admin |
| TodayLogin | 今日登录 | Kpi | 是 | metricCard | 2 | 仅 admin |
| OnlineCount | 在线用户 | Kpi | 是 | metricCard | 2 | 仅 admin |
| Log24h | 24h日志 | Kpi | 是 | metricCard | 2 | 仅 admin |
| Error24h | 24h异常 | Kpi | 是 | metricCard | 2 | 仅 admin |
| CpuRate | CPU使用率 | Kpi | 是 | metricCard | 2 | 仅 admin |
| MyLogins | 我的登录 | Kpi | 否 | metricCard | 3 | 仅 member |
| MyDays | 注册天数 | Kpi | 否 | metricCard | 3 | 仅 member |
| QuickLink | 快捷入口 | Content | 否 | quickLinks | 4 | 双方 |
| Profile | 个人信息 | Content | 否 | profile | 4 | 双方 |
| SysInfo | 系统信息 | Kv | 是 | kvList | 4 | 仅 admin |
| LoginLog | 登录与在线 | Content | 是 | loginLog | 4 | 仅 admin |
| Monitor | 性能监控 | Chart | 是 | monitorChart | 8 | 仅 admin（可从目录移除） |
| — | 站内信 | — | 否 | inbox | 6 | 双方（CubeNC 无对等，补 §8.5.2 待办） |

`Surfaces = "workbench"`（不进洞察槽 Catalog）。`Permission` 空。`Color` 与 MVC 一致：UserCount=blue、TodayLogin=green、OnlineCount=cyan、Log24h=grey、Error24h=red、CpuRate=orange、MyLogins=green、MyDays=blue。

系统默认种子（代码常量，不入库）：

- **admin**（任一 `IsSystem` 角色）：6 张系统 KPI → Monitor(w=8)+QuickLink(w=4) → Inbox(w=6)+SysInfo(w=6) → LoginLog(w=6)+Profile(w=6)。
- **member**：MyLogins(w=3)+MyDays(w=3)+Inbox(w=6) → QuickLink(w=6)+Profile(w=6)。

Catalog 对当前用户不可见的 named 不得出现在「添加部件」；已保存实例若后来无权限 → 锁卡，不 403 整页。

## 5. 做什么（按阶段）

**P1 后端存储与解析**：Cube.xml `HomeJson` → xcode；`WorkbenchResolver`；`WorkbenchRoleStore`（Parameter）；`GET/PUT /Cube/Workbench` 与 `…/Role/{roleId}`；`DashboardJson.TryNormalize(..., surface)`。

**P2 named 实现**：13+Inbox 的 `ICubeWidget`；Catalog `?surface=`；KPI/内容 JSON 契约单测。

**P3 api-core**：Workbench API、HomeJson 线缆、`parseWorkbenchJson`（w 含 2/8、上限 16）。

**P4 ArcoVue 工作台页**：`/home` → Workbench + Host；横幅；新 kind 渲染器；ConfigDrawer 工作台模式（先 named 再实体）；Monitor 5s 轮询；恢复默认；角色模板管理页（管理员）。

**P5 洞察槽隔离**：Catalog/PUT/Host insight 仍禁 miniKanban 与 workbench-only named。

**P6 文档**：§8.5.2、功能清单 DASH-2、核心接口、前端对接指南、web README、e2e `/home` 断言改工作台。

## 6. 不做什么

- 不把 CubeNC `WidgetManager` / `Dashboard.cshtml` / Parameter `Widget.Layout` 拖拽墙接到 SPA。
- 不改 `GetPage` / `Search` / `GetChartData` 签名。
- 不改 CubeNC 预定义 Widget 源码（Razor 工作台继续独立）。
- 不做多套门户可见范围、发布审批、移动双端画布。
- 不把 Process/Assembly 表嵌进工作台（仍在 `/Admin/Index`）。
- 不把外观设置 PUT 写入 `HomeJson`。

## 7. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-2608280e9e | `ICubeWidget`、`WidgetHost`、`/Cube/Widget/{Catalog,Query,Data}`、`surface` 预留 |
| OSC-0002 / OSC-0004 | UserProfile 表与壳偏好 API |
| 迁移方案 §5.1 / §8.5.2 | 用户>主角色>系统；与监控页分离 |
| CubeNC Widgets/System | **数据语义与 AdminOnly 对照表**，非引用 |

## 8. 测试范围

| 类型 | 是否做 | 说明 |
|------|--------|------|
| XUnit（NewLife.Cube.Tests） | 是 | Resolver 三层、Parameter 角色读写、AdminOnly Catalog、HomeJson 空串继承、insight PUT 仍拒 miniKanban、workbench PUT 允许 w=2/8 |
| Vitest（api-core + arco-vue） | 是 | parse w 集合、种子 admin/member、mergeWorkspace 仍不含 HomeJson、pageKind /home 与 Admin/Index 分离 |
| 构建 | 是 | Cube + CubeNC + api-core + arco-vue |
| 手工 | 是 | 管理员/普通用户默认墙、添加/删除/恢复、改角色模板不覆盖已个性化、Monitor 曲线跳动 |
| Cube.Vue/NaiveUI/CubeNC Widget 改代码 | 否 | |

硬门禁：本 OSC 新增单测全过 + 构建无错误。

## 9. 成功标准

- [ ] P1–P6 在 verify 中均有可判定 AC。
- [ ] 系统角色未个性化打开 `/home` 见到 6 张系统 KPI + Monitor；普通用户见到 MyLogins/MyDays，看不到 UserCount/Monitor。
- [ ] 个性化后改角色 Parameter 不覆盖；恢复默认回落角色或系统种子。
- [ ] `/Admin/Index` 仍展示系统信息/进程/程序集；`/home` 不再出现进程模块表。
- [ ] 洞察槽行为与 e9e 一致（含禁 miniKanban）。

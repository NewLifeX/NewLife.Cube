# OSC-2608280e9e — 页面仪表盘 Widget 协议

## 1. 目标愿景

实体列表洞察槽成为可配置的平台 Widget 运行时：用户绑定已授权实体，用指标卡、受限迷你图表和只读迷你看板看数；同一套实例契约与 Host 可被后续首页工作台直接复用。

- 目标 1：`ViewProfile.DashboardJson` 实体级持久化 Widget 实例（个人 > 全局模板 > 空）；不跟 NamedView 走。
- 目标 2：WebAPI `POST /Cube/Widget/Query` 对已授权 `typePath` 做 count/sum/avg/min/max/group，走 `DataPermission` + 租户 Where；无法翻译的筛选返回 400，禁止当前页假聚合。
- 目标 3：ArcoVue DefaultList 洞察槽可增删/排序平台部件（≤12），绑定 Sources 中有 Detail 权的实体；筛选构建器变化触发重查，不绑 SearchDrawer。
- 目标 4：C# `ICubeWidget` + 前端 `registerWidget` 可注册新类型；未知 kind 占位不崩槽。旧 `NamedView.insight` 只读迁移后不再作为写入主路径。

## 2. 为何做

迁移方案 §8.5.3 要把 e483 单图上限演进为页面级小仪表盘；现码 `GetList.stat` 几乎只有 UserStat 才有值，`viewFilter` 下推还会冲掉 Stat，筛选构建器 `skipFetch` 使统计不随筛选更新。跨实体指标若继续打 GetList，会系统性造假或打爆列表接口。

推荐方案乙：把 Widget 收成「渲染器 kind + 数据提供方 + 布局」协议，聚合走带鉴权的只读 Query，实体页与后续工作台共用 Host。方案甲（前端编排 GetList）明确不做主线。

## 3. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | **一个 OSC**，`tasks.md` 分 P1→P5。P3 前端不得在 P1 API 未就绪时接线；可先写纯函数/Vitest。不另拆后端号。 |
| 2 | 皮肤主实现 **ArcoVue**；契约落 **Cube WebAPI + `@cube/api-core`**。Cube.Vue / NaiveUI **不改 UI**。CubeNC Razor `Widgets/IWidget` **不引用、不迁移**。 |
| 3 | 第一期平台 kind：`metricCard`、`miniChart`（sparkline/line/bar/pie 模板）；**洞察槽暂不交付 `miniKanban`**（代码/Catalog/PUT 禁用，留给工作台 OSC）。禁止用户粘贴自由 ECharts option 作为新编主路径。 |
| 4 | 代码可注册：C# `ICubeWidget` + Vue `registerWidget`。禁止用户上传、第三方市场、整页画布、自由 x/y 拖拽。洞察槽仅 12 栅格自动流 + `w/order`。 |
| 5 | 用户可在洞察槽内增删/排序；源表必须当前用户 **Detail**。跨实体只许声明 `linkFilter`，禁止隐式 JOIN / SQL / 脚本。 |
| 6 | DashboardJson **整份**读取（个人非空则整份采用，含空 `widgets:[]`）；null/缺省继承模板。与首页工作台「已个性化不覆盖」同构，不做 widget-id 级合并。 |
| 7 | 筛选联动绑 **筛选构建器** `viewFilter`，不绑 SearchDrawer。同源 AND；跨实体无 mapping 则该部件不吃宿主筛选并角标提示。 |
| 8 | `GetList` / `GetChartData` / `OnGetChartData` **不改签名**。不全局打开 `RetrieveState`。 |
| 9 | 本号 **不做**：首页工作台、角色槽位、待办聚合、快捷入口 named 样例（Catalog 扫描能力要有，InboxUnread 等 named 实现留给工作台 OSC）、迷你表格、多图自由 option、字段级 ACL、查询收口退役 SearchDrawer。 |

## 4. 做什么（按阶段）

**P1 后端协议**：Cube.xml `DashboardJson` → xcode；`UpsertForUser` / 模板读写；`GET/POST /Cube/Widget/{Sources,Catalog,Query,Data}`；`ICubeWidget` 扫描。WebAPI only。

**P2 api-core**：`WidgetInstance` / `DashboardConfig` / `ViewProfileModel.dashboardJson`；`createWidgetApi`。

**P3 ArcoVue 运行时**：`WidgetHost` / `WidgetGrid` / 三渲染器 / `WidgetConfigDrawer` / `registerWidget`；DefaultList 洞察槽接线；筛选联动。

**P4 旧 insight 迁移**：无 DashboardJson 时从 `NamedView.insight` 合成只读部件；首次保存 DashboardJson 后不再写 insight。

**P5 文档**：迁移方案 §8.5.3、功能清单、核心接口架构、前端对接指南、web README。

## 5. 不做什么

- 不把 CubeNC Razor WidgetManager 搬进 SPA。
- 不新增整页画布、第三方注册表、用户脚本公式、迷你表格。
- 不改 `GetPage` / `Search` / `CreateWhere` / `GetChartData` 签名。
- 不把 `GET /Cube/Automation/Entities` 的默认 update 权当选表源（另提供 Sources=Detail）。
- 不修列表 `skipFetch` 假筛选（§8.5.4 另号）；Widget Query 独立请求，不走 skipFetch。
- 不实现首页 `UserProfile.workspace.widgets`。
- 不改 Cube.Vue `ListChartDialog`。

## 6. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-0002 | ViewProfile 表与 API |
| OSC-0014 | 全局模板 UserId=0；个人 > 模板 |
| OSC-0015 / OSC-260819e483 | `ViewFilterDto` + `AutomationFilter.TryBuildWhere` |
| OSC-260815fa86 | `EntityPageRegistry`、`AutomationAuth.HasPermission` |
| 迁移方案 §8.2 / §8.5.3 | 固定容器；洞察槽内平台部件；不做市场/画布 |

## 7. 测试范围

| 类型 | 是否做 | 说明 |
|------|--------|------|
| XUnit（NewLife.Cube.Tests） | 是 | Sources 鉴权、Query 聚合/拒绝 SQL/跨实体无 mapping、DashboardJson 校验、Catalog AdminOnly |
| Vitest（api-core + arco-vue） | 是 | schema 归一、linkFilter、未知 kind、legacy 合成、序列化 |
| 构建 | 是 | NewLife.Cube + NewLife.CubeNC + @cube/api-core + arco-vue |
| 手工 | 是 | 洞察槽增删部件、跨实体指标、筛选联动、无权限锁卡、旧 insight 刷新仍在 |
| Cube.Vue/NaiveUI 改代码 | 否 | 无新参时 GetList/GetChartData 与今日一致 |

硬门禁：本 OSC 新增单测全过 + 构建无错误。

## 8. 成功标准

- [ ] P1–P5 在 verify 中均有可判定 AC。
- [ ] 无 DashboardJson 的实体页与今日双关隐藏行为兼容（或仅只读迁移出合成卡）。
- [ ] 用户可为当前实体配置指标卡/迷你图/迷你看板，并可引用有 Detail 权的其它实体。
- [ ] Query 不返回行实体（aggregate 模式）；无权限不 403 整页。
- [ ] `WidgetHost` 不依赖 DefaultList 私有状态，工作台可另挂同一组件。

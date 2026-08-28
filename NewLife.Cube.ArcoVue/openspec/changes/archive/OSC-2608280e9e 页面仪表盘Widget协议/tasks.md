# OSC-2608280e9e Tasks

状态：Implementing（验收缺口补齐中）。P1–P6 主路径已完成；V1 为验收发现缺口。

硬约束：不改 `GetPage`/`Search`/`GetChartData` 签名；不引用 CubeNC `Widgets/IWidget`；不做首页工作台。

---

## 阶段 P1 — Cube 后端协议

- [x] P1.1 `Entity/Cube.xml` ViewProfile 增加 `DashboardJson`（String, Length=-1）。按 xcode 指令生成 `视图配置.cs` / `ViewProfileModel.cs`，禁止手写骨架
- [x] P1.2 `视图配置.Biz.cs`：`UpsertForUser` 在 `model.DashboardJson != null` 时拷贝；`SaveGlobalTemplate(typePath, viewsJson, filtersJson, dashboardJson)` 增加第三域（null 不覆盖，空壳 widgets=[] 或空串清除）；`DeleteGlobalTemplate` / `DeleteGlobalFormJson` 的 hasContent/hasOther 计入 DashboardJson
- [x] P1.3 `CubeController` GET ViewProfile：个人 DashboardJson 空白则填模板值；PUT 走 `DashboardJson` 校验器（design §3.1 / §4.4）；Template GET/PUT 接受 `dashboardJson`（仅管理员）
- [x] P1.4 新建 `NewLife.Cube/Widgets/`：`ICubeWidget`、`CubeWidgetAttribute`、`WidgetContext`、`CubeWidgetManager` 扫描；`DashboardJson` 解析/校验静态类
- [x] P1.5 新建 `Controllers/WidgetController.cs` `[Route("Cube/Widget")]`：Sources / Catalog / Query / Data；登录校验抄 `AutomationController`
- [x] P1.6 `WidgetQueryService`：按 design §4.2 组装 Where（租户 + DataPermission + extraFilter + 同源/跨实体 mapping）；aggregate 不返回 rows；无法翻译 400；无 Detail 403；sql/script/join 键 400
- [x] P1.7 XUnit `NewLife.Cube.Tests/Osc260828WidgetTests.cs`：Sources Detail、Query count、非法 field、跨实体无 mapping 不 AND、重复 id、>12、>64KB、空串继承、无 Detail 403。过滤器 `FullyQualifiedName~Osc260828`。测试实体须 `BindColumn`+`SetItem`（手写 auto-property 无脏数据）
- [x] P1.7b `DashboardJson` 写出 widgets 时 `JsonNode.Parse` 克隆，避免 “node already has a parent”（net6/7 无 DeepClone）
- [x] P1.8 `dotnet build NewLife.Cube` + `NewLife.CubeNC` 无错误（Link 实体编译）

## 阶段 P2 — api-core

- [x] P2.1 `types.ts` `ViewProfileModel.dashboardJson`
- [x] P2.2 `widget.ts`：类型 + `parseDashboardJson` / `serializeDashboardJson`（与后端校验对齐的前端子集：version、widgets 长度、id 唯一、w/order 归一）
- [x] P2.3 `createWidgetApi` + `cube.ts` 挂载 `widget.sources|catalog|query|data`
- [x] P2.4 api-core 单测：非法 version、重复 id、未知键保留、空 widgets
- [x] P2.5 `pnpm --filter @cube/api-core test` 通过

## 阶段 P3 — ArcoVue Widget 运行时

- [x] P3.1 `features/widget/registry.ts`：`registerWidget` / `getWidget`；`main.ts` 注册 metricCard、miniChart、miniKanban
- [x] P3.2 `WidgetGrid.vue` + `useWidgetGrid.ts`：12 列自动流，`w`→span，无拖拽。薄 SFC
- [x] P3.3 `MetricCardWidget` / `MiniChartWidget` / `MiniKanbanWidget` + 各自 `useXxx.ts`。迷你图用 `echartsTheme.initEcharts` + 平台模板，禁止把用户 option 当新编。看板 `KanbanBoard` 增加 `compact` 默认 false
- [x] P3.4 `UnknownWidget` / `LockedWidget`；`useWidgetQuery.ts` 并行请求，403→锁卡，未知 kind 不发 Query
- [x] P3.5 `WidgetHost.vue` + `useWidgetHost.ts`：只经 `WidgetSurfaceContext` inject/props，禁止 import `useListQuery`
- [x] P3.6 `WidgetConfigDrawer.vue` + use：kind → 源（Sources，「当前实体」置顶）→ 字段/图表模板 → 预览。`linkFilter` 跨实体必填提示。上限 12
- [x] P3.7 `viewProfile` store + `viewProfile.ts`：dashboard 解析/保存；PUT 带 `dashboardJson`；空串清除；与 viewsJson 分域防抖
- [x] P3.8 `DefaultList.vue` / `InsightPanel.vue`：挂 Host；空配置高度 0 + 添加入口；`onFilterApply` 通知 Host 重查。不传 SearchDrawer 条件
- [x] P3.9 Vitest：serialize order、未知 kind、同源 AND 标志、跨实体无 mapping 角标逻辑、compact 看板不发 edit。`.vue` 无业务 TS
- [x] P3.10 `pnpm --filter @cube/arco-vue test` + `build` 无错误
- [x] P3.11 会话补录：Widget 悬停操作组（编辑/复制/删除）与标题对齐；跨实体「未联动」角标（`WidgetLinkBadge`）；配置器支持跨实体复制部件
- [x] P3.12 会话补录：个人 DashboardJson 空白时回落全局模板；管理员保存个人时同步模板（便于分享页继承）

## 阶段 P4 — 旧 insight 只读迁移

- [x] P4.1 `synthesizeLegacyDashboard(insight, statData, hasDeveloperChart)`：仅 dashboard 未配置时调用（design §7）
- [x] P4.2 `legacyChart` 可删/升级；禁止 ConfigDrawer 新建该 kind；PUT 拒绝 legacyChart
- [x] P4.3 停止 `updateInsight` 作为仪表盘主写；ViewConfigDrawer 双开关移除或改为「打开页面仪表盘」
- [x] P4.4 GetChartData 仅当合成开发者图/legacy 仍需要时调用；纯 Query 卡不依赖 GetList.stat
- [x] P4.5 Vitest：双关不合成；showStat 空 stat → 一张 count；保存 dashboard 后不再合成

## 阶段 P5 — 文档

- [x] P5.1 迁移方案 §8.5.3 改为 Widget 协议 + DashboardJson；§3.1 图表行；§10.4 #13 指向本号
- [x] P5.2 `Doc/功能清单.md` SPA-7/SPA-15；新增 `DASH-1`
- [x] P5.3 `Doc/Api/核心接口架构.md`：`dashboardJson` + `/Cube/Widget/*`
- [x] P5.4 `Doc/Api/前端对接指南.md` + `NewLife.Cube.ArcoVue/web/README.md`（`registerWidget`）
- [x] P5.5 竞品分析 §3.1 仪表盘行注明本号（验收前可仍写进行中）

## 阶段 P6 — 会话补录：视图分享（embed + 短令牌）

> 验收期补录：洞察槽配置可随分享链接只读打开；属本号衍生，不另拆 OSC。

- [x] P6.1 `POST …/Share`（`ReadOnlyEntityController`）：签发 Url 锁定 `UserToken`（最长 1 年）；返回 token/expire/path
- [x] P6.2 `LoadToken` 接受非 JWT 短令牌；`TryLoadShareUserToken` + `IsShareRequestAllowed`（实体路径 + Widget/ViewProfile/MenuTree 等白名单）
- [x] P6.3 ArcoVue：`ShareViewPopover`、`embedMode`、`EmbedLayout`（视口高度可滚）、路由 persist 短令牌；embed 隐藏分享按钮与「自动化流程」/「表单布局」；跳过 Automation 按钮探测
- [x] P6.4 api-core：`page.share`（若包内已挂载）
- [x] P6.5 验收期：`ShareViewPopover.vue` 去掉 `watch`/业务 ref，逻辑进 `useShareViewPopover`（sfcThin）

## 阶段 V1 — 验收缺口补齐（2026-08-28）

- [x] V1.1 **P0** `WidgetQueryService.BuildWhere`：租户模式 AND `TenantId`（Tenant 实体用 `Id`）；无效租户 fail-closed；AdminBackend 不加；与 `CreateWhere` 同等
- [x] V1.2 **P1** `timeField`：SQLite/MySQL/SQLServer/PostgreSQL 按日历日分桶（最近 buckets≤24）；其它方言仍 400「不支持时间分桶」；补 XUnit
- [x] V1.3 **P1** `mode=list` 行投影：仅主键 + 非敏感标量字段（排除 Password/Secret/Salt/二进制）；禁止全工厂字段
- [x] V1.4 **P1** 空仪表盘且 `canEdit`：洞察槽显示「添加部件」入口（高度可点，非永久 0）
- [x] V1.5 **P1** `shouldQueryWidget` 允许 `provider=named` 走 `widget.data()`（HostTypePath 填入 WidgetContext；Data API 透传）
- [x] V1.6 **P1 文档** 洞察槽暂缓 miniKanban（留给工作台）：同步 proposal 备注 / DASH-1 / §8.5.3 / web README / design 锁定表；Catalog/PUT/Host 维持禁看板
- [x] V1.7 重跑：`Osc260828` **10 pass**；api-core build；arco-vue **713 pass**；Cube build 0 error

## 冒烟（验收阶段）

- [x] S.1 Admin/User 洞察槽添加一张 count 指标卡 → 刷新仍在；值随筛选构建器变化（不随 SearchDrawer）
- [x] S.2 引用有权的另一实体做 sum/miniChart；无权实体不在 Sources；手改 JSON 保存 400
- [x] S.3 迷你看板洞察槽暂缓（文档对齐）；compact 代码保留给工作台
- [x] S.4 旧 showStat/showChart 页在未保存 dashboard 前仍能看见合成卡；保存后 insight 不再写入
- [x] S.5 无部件且可编辑显示「添加部件」；满 12 不能再添加
- [x] S.6 Cube.Vue 列表图表按钮 / GetChartData 行为与今日一致（回归，不改代码）
- [x] S.7 分享链接（embed=1&token）：列表+页面仪表盘有数据；无系统导航；无「自动化流程」；可滚动见分页器；短令牌 401 已消除

## 收尾

- [x] C.1 本 OSC 新增单测全过
- [x] C.2 构建 NewLife.Cube + NewLife.CubeNC + api-core + arco-vue 无错误
- [x] C.3 `verify.md` 勾选后方可 Validating / Done

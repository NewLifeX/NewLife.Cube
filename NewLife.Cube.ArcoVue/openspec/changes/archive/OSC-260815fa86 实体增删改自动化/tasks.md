# OSC-260815fa86 Tasks

> 进入 Implementing 后勾选。顺序：T1 模型 → T2 拦截/队列 → T3 图与动作 → T4 API → T5 api-core → T6 前端编译函数 → T7 配置 UI → T8 列表入口与按钮/历史 → T9 Cron → T10 Webhook → T11 测试构建文档。

不得用「按需/适配/优化/适当」替代下列路径与断言。

## T1 实体与生成

- [x] 1.1 `Cube.xml` 按 design §2.1 增加 `EntityAutomation`（默认 ConnName=Cube）；列名、索引、DataScale 与 xml 一致。~~曾含 `AutomationRun` ConnName=Log~~ → **2026-08-19 已从 xml 删除，禁止加回**（现行见 design 文首修订）。
- [x] 1.2 xcode 生成；Biz 实现 `FindAllByTypePath`、`FindByHookToken`、`EnsureHookToken`（保存走 Controller 编译，不另开 SaveCompiled 入口）。
- [x] 1.3 `NewLife.CubeNC.csproj` Link 实体 cs、Biz、Model（与 EntityComment 相同模式）。

## T2 拦截器与入队

- [x] 2.1 `AutomationPersistence` 包装 `IEntityPersistence`：Update 前复制 Dirtys；Insert/Update/Delete **成功后** `AutomationRun.Enqueue`。跳过 design 跳过类型表。
- [x] 2.2 `CubeService.UseCube` 在 `EntityFactory.InitAll` 之后 `AutomationHost.Register`；后续工厂补挂。
- [x] 2.3 `AutomationTrigger.OnPersisted` 实现 §3.3 矩阵；租户匹配；批量>50 延期；debounce 3s；`AutomationScope` 深度。
- [x] 2.4 `AutomationWorker` HostedService 消费 `queued`；DI 注册。

## T3 图编译与执行器

- [x] 3.1 `AutomationGraph.Compile/Validate/Parse`：线性链、version、未知 type 拒绝保存；执行器 fail-closed 防旧脏数据（含 approval）。
- [x] 3.2 `AutomationFilter.Match` 对齐 `matchesViewFilter`（含 all/any、eq 数组、contains 大小写、isNull、gt/gte/lt/lte、after/before）。
- [x] 3.3 `AutomationExecutor` + `AutomationActions`：notify/update/create/find/http/delay/runAutomation/addComment/aiText/end。found 连续段语义见 design §4.4。
- [x] 3.4 模板替换白名单；http 仅 http(s)、15s、64KB；notify 渠道失败不中止。

## T4 Cube API

- [x] 4.1 新建 `AutomationController`，路由 `Cube/Automation`，Hook 匿名。权限函数 `CanConfigure/CanViewRuns/CanPressButton`。
- [x] 4.2 GET 列表隐藏 token；GET 详情可配置者可见 token；POST/PUT 用 filter+actions 编译，Version 冲突 409。
- [x] 4.3 GET Runs、POST Run、GET Meta、GET Entities、GET Recipients、Inbox*。

## T5 api-core

- [x] 5.1 `createAutomationApi`：list/get/create/update/remove/runs/run/meta/entities/recipients/inbox*（`AUTOMATION_HOOK_PATH` 常量）。
- [x] 5.2 `createCubeApi` 增加 `automation`；`index.ts` 导出。
- [x] 5.3 `api.spec.ts` 至少 4 个 URL 断言（list GET 带 typePath、POST body、Run POST、Runs GET）。

## T6 前端图工具（Vitest）

- [x] 6.1 `automationGraph.ts`：`compileAutomationGraph`、`parseAutomationGraph`、`normalizeTriggerConfig`、`AUTOMATION_MENU_ACTION_TYPES`、`validateFoundTargetChain`。
- [x] 6.2 `automationGraph.spec.ts` ≥8 例（无 filter、有 filter、非法 action、分叉 parse 失败、version、button、fieldChange 空数组、delay 上限、found 链路）。

## T7 配置 UI

- [x] 7.1 `AutomationDrawer.vue` + `useAutomationDrawer.ts`：列表、Enable 开关、删除确认、创建。
- [x] 7.2 `AutomationEditor.vue` + `useAutomationEditor.ts`：飞书双栏（触发+字段条件卡片 | 动作）；条件复用 FilterBuilder 操作符矩阵，不改 Popover 列表行为。
- [x] 7.3 动作卡片：`AutomationActionCard` 头栏 `⋯` 菜单（上移/下移/删除+图标）+ 右侧收起；添加下拉 8 种（不含 runAutomation）；Webhook token 复制/重生；notify 接收人三选一；found 链路校验。
- [x] 7.4 图标登记（lightning/remind/more 等）；SFC 薄脚本。
- [x] 7.5 壳站内通知：`ShellToolbar` remind + `InboxDrawer`（无 footer）。

## T8 列表入口、行按钮、记录 Tab

- [x] 8.1 `DefaultList.vue` list-topbar：搜索与高级之间插入「自动化」，`v-if="flags.canUpdate"`，顺序见 design §7.1。
- [x] 8.2 `opsAction.ts` + `useListTable.ts`：最多 3 个 `auto:{id}`；点击 POST Run。
- [x] 8.3 `opsAction.spec.ts` 覆盖 0/3/4 个按钮。
- [x] 8.4 运行日志在流程编辑器 Tab（系统 Log）；**不**在 RecordDrawer 增加自动化 Tab。

## T9 定时与日期到达、延时续跑

- [x] 9.1 `EntityAutomationJob` Cron `0 * * * * ?`：扫描 schedule（Cron 到期）、dateArrive（字段+offset、once 查重）、Status=waiting 且 ResumeAt≤now 的 Run 续跑。
- [x] 9.2 Job 被 `ScanJobs` 发现；默认 Enable=true。

## T10 Webhook

- [x] 10.1 `POST /Cube/Automation/Hook/{token}`：404 无效 token；签名失败 401；429 限流 60/min；body 进入 context.webhook。
- [x] 10.2 XUnit：错误 token、签名、限流（`AutomationHookRate` 测 61 次）。

## T11 测试、构建、文档

- [x] 11.1 `NewLife.Cube.Tests/Osc260815AutomationTests.cs`：Filter 同构、深度、debounce、Dirtys fieldChange、租户、approval 脏图执行失败、Compile 拒绝环。
- [x] 11.2 跑 design §10 全部命令，0 failed / 0 error。
- [x] 11.3 回写 `web/README.md`、`Doc/功能清单.md`（追加 DATA/SPA 行）、`Doc/Api/核心接口架构.md`、迁移方案（自动化 ≠ FlowGram 运行时）。
- [x] 11.4 手工冒烟：Admin/User（及 Area）顶栏「搜索」与「高级」之间可见「自动化」（2026-08-16 浏览器确认）。完整 insert→InApp→Run 成功链路未在本环境点完创建抽屉点击被壳层遮罩拦截），以单测 + 入口 AC 为准。

## T12 验收缺口补齐（openspec-verify 2026-08-16）

- [x] 12.1 **P0.1（历史，已废止）** 2026-08-16 曾将 `AutomationRun` 落入 `Cube.xml`（ConnName=Log）。**后续实现禁止按本条把表加回。**
- [x] 12.2 其余 P0/P1/P2：found 连续段语义 + 空 found 跳过；findRecords SQL 下推/分页扫描；Filter 与 matchesViewFilter 对齐；runAutomation 自引用保存拒绝；httpRequest SSRF；Hook 先校验再限流 + 字典淘汰；废弃 target=created；Worker WrapAll 补挂工厂；批量 names/values 路径 After；debounce 查内存 queued/running；角色/部门展开租户裁剪；api-core recipients/entities/inbox URL 单测。
- [x] 12.3 **2026-08-19 现行**：删除 `Cube.xml` `AutomationRun` 表与 `自动化运行.*` / CubeNC Link；队列改为 `Automation/AutomationRun.cs` 内存 POCO；`GET /Runs` 与 dateArrive `once` 改读系统 Log（`AutomationFlowLog`）。禁止再 xcode 生成该表。

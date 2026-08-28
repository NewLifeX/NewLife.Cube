# OSC-26082815a1 Tasks

状态：Implementing（P1–P6 已勾选，冒烟 S.1–S.6 留给验收）。硬约束：不引用 CubeNC `IWidget`；不改 GetPage/GetChartData 签名；不新建 RoleWorkspace 表；洞察槽行为与 e9e 一致。

---

## 阶段 P1 — 存储与解析 API

- [x] P1.1 `Entity/Cube.xml` UserProfile 增加 `HomeJson`（String, Length=-1）。按 xcode 指令生成实体/Model，禁止手写骨架
- [x] P1.2 `用户呈现配置.Biz.cs`：`UpsertForUser` 仅当 `model.HomeJson != null` 拷贝（空串写入清空）；`ToModel` 输出 HomeJson
- [x] P1.3 `DashboardJson.TryNormalize` 增加 `surface`（默认 insight）。workbench：max 16、w∈{2,3,4,6,8,12}、允许 miniKanban。insight 分支行为与今日单测一致
- [x] P1.4 `WorkbenchRoleStore`：`Category=Workbench.Role`，UserID=0，JSON 只写 `LongValue`；Get/Save/Delete
- [x] P1.5 `WorkbenchSeeds`：admin / member 实例列表，稳定 id `seed-{Name}`，对照 proposal §4
- [x] P1.6 `WorkbenchResolver.Resolve(IUser)`：design §2 / §8.1
- [x] P1.7 `WorkbenchController`：GET/PUT `/Cube/Workbench`、GET/PUT `/Cube/Workbench/Role/{roleId}`；非系统角色写 Role → 403；角色不存在 → 404
- [x] P1.8 CubeController UserProfile 线缆包含 `homeJson`；null 不覆盖
- [x] P1.9 XUnit `Osc26082815a1WorkbenchTests`：§8.1 四格、Parameter UserID=0、空串继承、insight 仍拒 miniKanban、workbench 接受 w=2。过滤器 `FullyQualifiedName~Osc26082815a1`
- [x] P1.10 `dotnet build NewLife.Cube` + `NewLife.CubeNC` 无错误

## 阶段 P2 — CubeNC 对等 named

- [x] P2.1 `Widgets/Workbench/` 13 个 class + `InboxWidget`，`CubeWidgetAttribute` 填 Name/Title/Kind/Cols/AdminOnly/Surfaces=workbench/Color
- [x] P2.2 GetData 契约 design §4；QuickLink 服务端过滤 adminOnly 链接；SysInfo 无 HTML；Monitor 只返回当前点
- [x] P2.3 `CubeWidgetManager.CatalogFor(user, surface)`；`WidgetController.Catalog` 读 `surface` query，默认 insight
- [x] P2.4 XUnit：普通用户 Catalog workbench 无 UserCount/Monitor/SysInfo/LoginLog，有 MyLogins/MyDays/QuickLink/Profile/Inbox；系统角色有 13+Inbox。Data 对不可见 named 404
- [x] P2.5 构建无错误

## 阶段 P3 — api-core

- [x] P3.1 `UserProfileModel.homeJson`
- [x] P3.2 `parseDashboardJson(raw, surface)` / serialize 按 surface；workbench w=8 合法
- [x] P3.3 `createWorkbenchApi`：get / put / getRole / putRole；挂 `cubeApi.workbench`
- [x] P3.4 Catalog 请求带 `surface`
- [x] P3.5 Vitest：非法 w 归一、空 widgets、version；`pnpm --filter @cube/api-core test`

## 阶段 P4 — ArcoVue 工作台

- [x] P4.1 `views/home/index.vue` 改挂 Workbench；DynamicPage `home` 仍 DefaultHome
- [x] P4.2 `Workbench.vue` + `useWorkbench.ts`：横幅、GET 解析、editing、恢复默认 PUT `""`、Host inject `surface=workbench` hostFilter=null canEdit=editing
- [x] P4.3 注册 quickLinks / profile / kvList / loginLog / monitorChart / inbox 薄 SFC + useXxx
- [x] P4.4 MetricCard 展示 trend；clickUrl 回落 Data.url
- [x] P4.5 Monitor 5s 轮询、12 点缓冲、卸载 clearInterval
- [x] P4.6 ConfigDrawer workbench：Tab 平台 named 一键添加；Tab 实体指标（无 linkFilter）；上限 16
- [x] P4.7 Host insight 仍滤 miniKanban；workbench 不过滤；上限按 surface
- [x] P4.8 管理员 `WorkbenchRole` 页：选角色、预览、PUT Role；菜单仅系统角色
- [x] P4.9 `userProfile.ts` appearance payload **不含** homeJson；单测 mergeWorkspace 丢 home
- [x] P4.10 IconPark 新 type 注册 + spec
- [x] P4.11 Vitest：种子形状、surface 上限、trend 读取。`.vue` 无业务 TS
- [x] P4.12 `pnpm --filter @cube/arco-vue test` + `build` 无错误
- [x] P4.13 更新 `e2e/object-home.spec.ts`：`/home` 工作台；`/Admin/Index` 系统信息

## 阶段 P5 — 洞察槽隔离回归

- [x] P5.1 现有 Osc260828 Widget 单测全过（insight PUT 拒看板、上限 12）
- [x] P5.2 Catalog 默认 surface 与旧客户端无 query 时 kinds 不含 miniKanban、named 不含 UserCount

## 阶段 P6 — 文档

- [x] P6.1 迁移方案 §8.5.2 写 HomeJson + Parameter、CubeNC 对照、与监控页分离
- [x] P6.2 `Doc/功能清单.md` DASH-2；SPA-7 home 行
- [x] P6.3 `Doc/Api/核心接口架构.md`、`前端对接指南.md`、`web/README.md`
- [x] P6.4 竞品分析仪表盘/工作台行注本号（可选）

## 会话小任务补录（执行中发现，非原计划）

- [x] P7.1 `WorkbenchSeeds` 改为 `Lazy<String>`，避免静态字段初始化顺序导致 NRE
- [x] P7.2 UserCount 单测改为先插入用户再读 `User.Meta.Count`
- [x] P7.3 vue-tsc：`useQuickLinksWidget` 的 `context` 导入改为同目录；`ChartItem` 补索引签名以兼容 `WidgetQueryResult.items`
- [x] P7.4 迁移方案 §5.1 由 `workspace` 改为 `HomeJson` + Parameter；差距表 #5 划掉；`Admin/Index` 路由注释改为监控页

## 会话小任务补录（P8 — 实体列表/卡片与验收缺口，非原 P1–P6）

- [x] P8.1 新增 `dataList` / `dataCard`；`miniKanban` 展示名「数据看板」；ConfigDrawer「实体部件」
- [x] P8.2 Catalog：workbench 五 kind；insight 仅 metricCard + miniChart
- [x] P8.3 `DashboardJson` + api-core：insight PUT 拒看板/列表/卡片；list 类 provider=`entity.list`
- [x] P8.4 Host / ConfigDrawer insight 过滤三 kind；workbench 可添加
- [x] P8.5 拉取数量 10/20/30/50/100/300/全部(-1)；「全部」性能提示；Query -1 不截断、其余封顶 300
- [x] P8.6 DataList：紧凑斑马纹、默认最多 7 行、`rotateDataListWindow` 每秒循环
- [x] P8.7 DataCard：单行 RecordCard + 左右切换
- [x] P8.8 工作台横幅图标按钮：`setting-config` / `check` / `undo`；登记 IconPark
- [x] P8.9 角色模板：空 widgets 保存改为清除模板（避免空墙阻断系统种子）；「清除模板」按钮
- [x] P8.10 XUnit：用户压角色、空串 HomeJson 回落角色

---

## 冒烟（验收阶段）

- [ ] S.1 系统管理员 `/home`：6 KPI + Monitor；无进程模块表
- [ ] S.2 普通用户 `/home`：MyLogins/MyDays；无 UserCount/Monitor
- [ ] S.3 添加 Inbox 或实体指标卡，刷新仍在；恢复默认后角色/系统种子回来
- [ ] S.4 改角色模板，已个性化用户布局不变；空保存不阻断系统种子
- [ ] S.5 `/Admin/Index` 系统信息/刷新仍可用
- [ ] S.6 洞察槽添加部件仅指标卡/迷你图表（无看板/列表/卡片）
- [ ] S.7 `/home` 可添加数据列表/卡片/看板，刷新仍在
- [ ] S.8 数据列表多于 7 行时每秒窗口循环
- [ ] S.9 拉取数量含「全部」与性能提示
- [ ] S.10 横幅为图标按钮（自定义/恢复默认）

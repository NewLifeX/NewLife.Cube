# OSC-2610011ff2 — 隐藏功能入口

## 1. 目标愿景

MVC 里不进侧栏、靠用户中心和设置「更多」打开的管理页，在 ArcoVue 里用固定地址就能打开，并用通用列表或对象页呈现。

- 目标 1：白名单内的隐藏菜单 URL 已登录即可打开，不依赖 `visible===true` 才注册路由。
- 目标 2：账号中心提供在线会话、令牌、登录日志、OAuth 日志入口；对象设置左侧「更多」提供短信、邮件、OAuth、访问规则入口。侧栏仍不展示 `visible===false` 的项。
- 目标 3：`/Admin/UserStat` 在列表洞察区以固定图表部件展示 `GetChartData` 的前 2 张图（整行、高度压缩；标题取自原图）；无数据时只留列表。其后仍可添加自定义部件。
- 目标 4：`/Cube/Workflow/Efficiency` 在菜单未播种时也能直达。

## 2. 为何做

[`buildLeafRoutes`](../../../web/src/core/utils/menuRoutes.ts) 跳过 `visible===false`。API 上用户在线、用户统计、用户令牌、用户链接、OAuth 配置/日志、访问规则、短信、邮件、字典参数、租户用户、应用日志均为隐藏菜单。用户列表上的单元格链接指向这些地址时，路由未注册。MVC 用 `_User_Nav` 与 `_Object_Nav` 进入；Cube.Vue 写成静态路由。效率页已有组件，但不在 `oaLeafRoutes` 里。

## 3. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | 白名单（Pascal 路径，大小写不敏感）只注册这些叶子，全部走现有 `DynamicPage`：`/Admin/UserOnline`、`/Admin/UserStat`、`/Admin/UserToken`、`/Admin/UserConnect`、`/Admin/OAuthConfig`、`/Admin/OAuthLog`、`/Admin/AccessRule`、`/Admin/SmsConfig`、`/Admin/MailConfig`、`/Admin/Parameter`、`/Admin/TenantUser`、`/Cube/AppLog`。 |
| 2 | 侧栏、顶栏、混合布局继续按 `visible!==false` 画菜单。白名单只解决「知道地址或从入口点进去」。 |
| 3 | 账号中心新页签「关联」：在线会话、令牌、登录日志、OAuth 日志。查询带当前用户 `id`（`userInfo.id`）。`id` 缺失时按钮禁用。用户统计不带用户条件，链到 `/Admin/UserStat`。 |
| 4 | 对象页左侧在自动发现的对象项之后追加「更多」：短信设置、邮件设置、OAuth 设置、访问规则。菜单树（含 `visible=false`）里没有对应 `url` 时不显示该项。点击 `router.push`，不把它们探测成对象页。 |
| 5 | 用户统计：`typePath` 为 `Admin/UserStat` 时，列表加载后请求 `GET /api/Admin/UserStat/GetChartData`（沿用当前列表查询参数）。返回数组非空则经 `mergeDeveloperCharts` 注入洞察区最多 2 张固定 `legacyChart`（整行压缩、无设置抽屉）；空数组或失败不挡列表，失败用警告。 |
| 6 | `oaLeafRoutes` 增加 `Efficiency` / 标题「效率」。组件仍由 `resolvePageComponent` 指向 `WorkflowEfficiencyPage.vue`。 |
| 7 | 不读写 `Menu`（Menus）实体的 `Visible`。白名单页保持库里现有可见性；要显示或隐藏侧栏项，由管理员在菜单管理里自行修改。本号不新增启动时覆盖 `Visible` 的代码，也不改这些控制器的 `[Menu]` 可见参数或 `LastUpdate`（避免 `MenuHelper` 按特性把 `Visible` 写回实体）。 |

## 4. 做什么

1. `router/index.ts` 为白名单与效率页增加静态子路由。
2. 账号中心「关联」页签；对象页「更多」链接。
3. 用户统计页内图表，复用现有图表 option 归一函数（若仍写在弹窗 composable 内则抽到 `core/utils`）。
4. 单测：白名单路径表、关联链接查询拼装、图表「有数据/空/失败」分支。

## 5. 不做什么

- 不把全部 `visible=false` 菜单自动注册（避免把服务控制器指南卡和设计器以外的内部 URL 铺开）。
- 不把隐藏项画进侧栏。
- 不修改 Menus 实体各菜单项的 `Visible`（含启动覆盖、菜单扫描回写、改 `[Menu]` 可见参数）。侧栏是否出现某项，沿用库中现值，由管理员按需改。
- 不新写用户在线、令牌等专用表格；有 `GetPage` 的走 `DefaultList`，短信/邮件/OAuth/参数同理。
- 不做菜单上升/下降、地区地图、工作台部件全局页、文案外置。
- 不实现 `NotificationRecord` 专用页（API 宿主无该控制器；站内信仍走抽屉）。

## 6. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-2608139feb | `DynamicPage` 实体/对象探测 |
| OSC-26090347f1 / OSC-260922201a | 效率页组件已存在，本号只补静态路由 |
| OSC-261001909b | 账号关联与租户「成员」链接依赖本号路由；本号不实现那些链接本身 |

## 7. 测试范围

触及 `web/`；箱线升序顺带改 `UserStatController.OnGetChartData`（演示数据排序，不改 `[Menu]`）。执行期跑本号 Vitest 与 `vue-tsc -b`。验收期新增单测全过，`vue-tsc -b` 与 `dotnet build NewLife.Cube -f net10.0` 无错误。

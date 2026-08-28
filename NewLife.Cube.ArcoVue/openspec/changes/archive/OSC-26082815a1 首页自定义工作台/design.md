# OSC-26082815a1 Design — 首页自定义工作台

适用前端：Arco Design Vue（https://arco.design/vue/docs/start）。迷你图 / Monitor 用已接入 ECharts 壳（`web/src/core/utils/echartsTheme.ts`）。不涉及 VTable 主表、不涉及 FlowGram。`.vue` 薄 script，业务进同目录 `useXxx.ts` / `core/utils`。

图标：IconPark，新图标须先在 https://iconpark.oceanengine.com/official 确认存在再写入 `iconRegistry.ts` + `iconComponents.ts`。CubeNC FontAwesome → IconPark：`fa-users`→`peoples`，`fa-sign-in`→`login`（若库无则 `enter`），`fa-user-circle`/`fa-user-circle-o`→`user`，`fa-file-text-o`→`file-text`，`fa-exclamation-triangle`→`attention`，`fa-tachometer`→`dashboard`，`fa-calendar`→`calendar`，`fa-th-large`→`application-menu`，`fa-server`→`computer`，`fa-line-chart`→`chart-line`，Inbox→`remind`。实施时以站点存在性为准，单测校验 type。

## 1. 冻结与加法

| 符号 | 冻结 |
|------|------|
| `GetPage` / `Search` / `CreateWhere` / `GetChartData` | 不改签名 |
| CubeNC `IWidget` / `WidgetManager` / `Widgets/System/*` | **不引用、不改** |
| `UserProfile.LayoutJson` / `ThemeJson` / `WorkspaceJson` 语义 | 不把首页布局写入 WorkspaceJson |
| 洞察槽 `DashboardJson` 上限 12、`w∈{3,4,6,12}`、禁 miniKanban | 保持 |
| Cube.Vue / NaiveUI | 不改 UI |

加法：`UserProfile.HomeJson` 列；`WorkbenchResolver` + Parameter 角色行；`/Cube/Workbench*`；13+Inbox `ICubeWidget`；ArcoVue `views/home/Workbench*` 与若干 kind 渲染器。

## 2. 三层模型

| 层 | 职责 | 存贮 |
|----|------|------|
| Surface | `workbench` 本号；`insight` 不动 | — |
| Instance | 配置 JSON，不含查询结果 | 用户 `HomeJson`；角色 Parameter.LongValue |
| Runtime | Host 拉 Data/Query、锁卡 | 无持久化 |
| Chrome | 欢迎横幅 | 不入库，每次按当前用户渲染 |

读取（整份，与 e9e 同构）：

1. `HomeJson` 非空且解析成功（含 `widgets:[]`）→ `source=user`。
2. 否则主角色 `RoleId` 的 Parameter 行有效 → `source=role`。
3. 否则系统种子：`user.Roles.Any(r => r.IsSystem)` → admin 种子，否则 member 种子 → `source=system`。
4. 附加 `RoleIds` 不参与。角色 Id 无效或 JSON 坏 → 当无角色配置。

「有效」：UTF-8 JSON object、`version===1`、`widgets` 为数组。解析失败当未配置（GET 不 500）。

## 3. 实例 schema

复用 `@cube/api-core` `DashboardConfig` / `WidgetInstance`。工作台归一化与洞察槽的差异只允许下列分支（`DashboardJson.TryNormalize` 增加 `surface` 参数，默认 `insight` 保持旧行为）：

| 规则 | insight | workbench |
|------|---------|-----------|
| max widgets | 12 | 16 |
| `layout.w` | 3,4,6,12（非法→3） | **2,3,4,6,8,12**（非法→3） |
| miniKanban PUT | 400 | 允许，`provider=entity.list` |
| `legacyChart` PUT | 400 | 400 |
| named 缺 widgetName | 400 | 400 |
| 未知 kind | 允许保存（占位） | 同左 |
| 字节上限 | 64KiB | 64KiB |

`HomeJson` / 角色 LongValue 根形状：

```ts
{ version: 1, widgets: WidgetInstance[] }
```

named 实例最小形（KPI 例）：

```ts
{
  id: string,                    // 种子用稳定 id：`seed-{Name}`；用户新增 ulid
  kind: 'metricCard',
  title: string,                 // 默认 Catalog.Title
  layout: { w, h?: 1, order },
  source: { provider: 'named', widgetName: 'UserCount' },
  query: {},
  style: { icon, color, clickUrl?: string }
}
```

内容类 `kind` 与 `widgetName` 必须同时匹配对照表；PUT 时若 kind 与 Catalog 声明不一致 → 以 Catalog.Kind 覆盖 kind（不 400，避免旧草稿）。

剥离 `data`/`value`/`items`/`rows` 后再保存。未知顶层键保留。

空配置：`PUT /Cube/Workbench` body `{ homeJson: "" }` 或 `homeJson: null` 且 query `clear=1` → 列写成 null/空串，下次继承。显式 `{version:1,widgets:[]}` 表示用户清空墙，**不**继承（横幅仍在，Host 显示添加入口）。

## 4. named GetData 契约（WebAPI JSON）

前端 `normalizeQueryResult` / 各 `useXxx` 必须兼容 PascalCase 与 camelCase。

### 4.1 KPI（metricCard + named）

返回 `{ value: string, trend: string, url: string }`。

| Name | value 算法（与 CubeNC 一致） | url |
|------|------------------------------|-----|
| UserCount | `User.Meta.Count` 千分位 | `/Admin/User` |
| TodayLogin | `UserStat.FindByDate(Today)?.Logins ?? 0` | `/Admin/Log?dtStart=今日&act=登录` |
| OnlineCount | `UserOnline.FindCount()` | `/Admin/UserOnline` |
| Log24h | `Log.FindCount` 雪花 Id 近 24h | `/Admin/Log?dtStart=-24h` |
| Error24h | 同上且 `Success==false` | 同上 `&success=false` |
| CpuRate | `MachineInfo.CpuRate*100` 一位小数+`%`；trend=`内存 {mem}%` | `/Admin/Index`（监控页，不是 Main 外链） |
| MyLogins | `ManageProvider.User.Logins` | `/Admin/User/Info` |
| MyDays | `(Now-RegisterTime).Days`，RegisterTime.Year≤2000 → 0 | `""` |

MetricCard：主值=`value`；次文案=`trend`（e9e 未接线，本号补）；点击=`style.clickUrl` 否则 payload `url`。

### 4.2 QuickLink → kind `quickLinks`

返回 `{ links: { name, url, icon, adminOnly }[] }`。服务端按当前用户是否系统角色过滤 `adminOnly`。链接表与 CubeNC 六条一致（个人中心 / 系统设置 / 用户管理 / 审计日志 / 在线用户 / 定时作业）。前端 IconPark 映射 `icon`（fa-*）。点击 `router.push(url)`。无用户 pin 第一期（实例不存 pins）。

### 4.3 Profile → kind `profile`

返回 `{ name, displayName, roleNames, online, logins, lastLogin, lastLoginIP, registerTime }`。只读描述列表。`displayName` 空则 `name`。

### 4.4 SysInfo → kind `kvList`

返回 `{ items: { label, value, href? }[] }`。键与 CubeNC 字典一致；**禁止 HTML**。原「更多信息」改为 `label=更多信息, value=查看完整服务器信息, href=/Admin/Index`。

### 4.5 LoginLog → kind `loginLog`

返回 `{ logins: { createTime, userName, action, createIP }[], onlines: { name, createTime, oAuthProvider }[] }`。条数各 ≤10，算法同 CubeNC。两栏列表。

### 4.6 Monitor → kind `monitorChart`

返回 `{ time: string, cpu: number, mem: number }` **当前一点**（0–100）。**禁止**返回 CubeNC `ECharts` 对象。前端 `useMonitorChartWidget`：5s 调 `widget.data('Monitor')`，环形缓冲 12 点；卸载停表。无点时画当前一点水平线。AdminOnly。

### 4.7 Inbox → kind `inbox`

返回 `{ unread: number, items: { id, title, createTime }[] }`。`NotificationRecord.Search` 当前用户 InApp，未读优先，limit 8。点击打开已有 Inbox 抽屉（`appStore.openInboxDrawer`），不新开路由。无权限/表空 → 空列表 + unread=0，不报错。

## 5. HTTP

新建 `Controllers/WorkbenchController.cs` `[Route("Cube/Workbench")]`，鉴权抄 `WidgetController`（登录）。

| Action | 方法 | 权限 | 成功 |
|--------|------|------|------|
| `""` | GET | 登录 | `{ source, roleId, config }`；`config` 已归一化；`roleId` 为主角色，无则 0 |
| `""` | PUT | 登录 | body `{ homeJson: string }`；`""` 清除；非法 400。只写当前用户 HomeJson |
| `Role/{roleId}` | GET | 系统角色 | `{ roleId, config }`；无行则 `config=null` |
| `Role/{roleId}` | PUT | 系统角色 | body `{ homeJson }`；写 Parameter LongValue；角色不存在 404；非法 400 |
| Widget `Catalog` | GET | 登录 | 增加 query `surface=insight\|workbench`，默认 insight（兼容 e9e） |

`GET Catalog?surface=workbench`：

- `kinds`：metricCard、miniChart、**miniKanban**（defaultW 3/6/6）。
- `named`：`CubeWidgetManager.CatalogFor(user)` 且 `Surfaces` 含 `workbench`（逗号拆分，忽略大小写）。insight 默认仍剔除 miniKanban，且 named 仅 `Surfaces` 含 `insight` 或空（本号新 named 只标 workbench，故不进洞察槽）。

`POST Query` / `GET Data` 不改路径。Data 对 AdminOnly named：`Visible` 失败 → 404（Host 锁卡/未知，不跳登录）。

## 6. 后端文件地图

| 文件 | 计划 | 不动 |
|------|------|------|
| `Entity/Cube.xml` UserProfile | 增列 `HomeJson` String Length=-1，说明「首页工作台。JSON：version+widgets」 | WorkspaceJson 语义 |
| `Entity/用户呈现配置.cs` + `Models/UserProfileModel.cs` | **xcode 生成** | 手写骨架 |
| `Entity/用户呈现配置.Biz.cs` | `UpsertForUser`：`HomeJson != null` 才拷贝（空串允许写成清空）；`ToModel` 带出 | 外观三列 |
| `Controllers/CubeController` UserProfile GET/PUT | 线缆增加 `homeJson`；外观客户端不传则 null 不覆盖 | layout/theme/workspace |
| **新建** `Widgets/WorkbenchJson.cs` 或扩展 `DashboardJson.TryNormalize(..., String surface)` | surface 分支见 §3 | insight 默认参数 |
| **新建** `Widgets/WorkbenchRoleStore.cs` | Category 常量 `Workbench.Role`；Get/Save/Clear；LongValue only | Widget.Layout |
| **新建** `Widgets/WorkbenchResolver.cs` | §2 读取；种子 `WorkbenchSeeds.Admin/Member` | — |
| **新建** `Widgets/WorkbenchSeeds.cs` | 固定 id `seed-UserCount` 等 | — |
| **新建** `Widgets/Workbench/*Widget.cs` | 14 个 class，`[CubeWidget("UserCount","用户总数", Kind="metricCard", Cols=2, AdminOnly=true, Surfaces="workbench", Color="blue")]` | 不放 CubeNC |
| **新建** `Controllers/WorkbenchController.cs` | §5 | 不放 CubeNC |
| `Widgets/CubeWidgetManager.cs` | `CatalogFor(user, surface)`；`Visible` 增加 Surfaces 过滤可选 | 无 surface 时保持旧「全部可见 named」（insight 调用方传 insight） |
| `Controllers/WidgetController.Catalog` | 读 `surface` query | Sources/Query 签名 |
| `NewLife.Cube.Tests/Osc26082815a1WorkbenchTests.cs` | Resolver/种子/AdminOnly/w=2/角色 Parameter | — |

禁止 `using NewLife.CubeNC`。

## 7. 前端文件地图

| 文件 | 计划 | 不动 |
|------|------|------|
| `packages/api-core/src/widget.ts` | `parseDashboardJson(raw, surface?)`；Workbench w 集合；`createWorkbenchApi` | insight 默认 |
| `packages/api-core/src/types.ts` | `UserProfileModel.homeJson?` | workspaceJson |
| `web/src/views/home/index.vue` | 改为挂 `Workbench.vue` | — |
| **新建** `views/home/Workbench.vue` + `useWorkbench.ts` | GET Workbench；inject Host；横幅；自定义/恢复 | 不读 userProfile.workspace 当布局 |
| `views/dynamic/DynamicPage.vue` | `pageKind==='home'` **仍 DefaultHome**（监控） | 探测逻辑 |
| `features/widget/registry.ts` | 注册 quickLinks/profile/kvList/loginLog/monitorChart/inbox | metricCard 已有 |
| **新建** 六个薄 SFC + `useXxx.ts` | 见 §4 | — |
| `features/widget/useMetricCardWidget.ts` | 次文案 trend；click 回落 payload.url | sparkline |
| `features/widget/useWidgetConfigDrawer.ts` | `surface=workbench`：第一步「平台部件」列表（named 卡片）+ 「实体指标」三种 kind | insight 三步 |
| `features/widget/useWidgetHost.ts` | workbench 允许 miniKanban；上限 16 | insight 过滤看板 |
| `DashboardJson` 前端 serialize | surface 传入 | — |
| `core/utils/userProfile.ts` | **禁止** mergeWorkspace 读 HomeJson；appearance payload 不含 homeJson | aiFab |
| **新建** `views/settings/WorkbenchRole.vue` 或挂角色抽屉 | 管理员选 RoleId，GET/PUT Role | 普通用户不可见 |
| `web/e2e/object-home.spec.ts` | `/home` 断言工作台（问候或 KPI）；`/Admin/Index` 仍系统信息 | — |
| `iconRegistry` / `iconComponents` | 本号新 type | 已有映射 |

`/home` 静态路由已存在，不必改 `pageKind`。StartPage 已是 `/home`。

## 8. 条件矩阵

### 8.1 GET /Cube/Workbench

| HomeJson | 角色 Parameter | 系统角色? | source | config |
|----------|----------------|-----------|--------|--------|
| 合法非空（含空数组） | * | * | user | 个人 |
| null/空白/非法 | 合法非空 | * | role | 角色 |
| null/空白/非法 | 无/非法 | 是 | system | admin 种子 |
| null/空白/非法 | 无/非法 | 否 | system | member 种子 |
| 未登录 | | | 401 | — |

### 8.2 Catalog named 可见

| AdminOnly | 用户 IsSystem | 出现在 workbench Catalog |
|-----------|---------------|--------------------------|
| true | 是 | 是 |
| true | 否 | 否 |
| false | * | 是 |

已钉在 config 里但 Catalog 不可见 → 仍渲染，Data 404 → LockedWidget「无权限」。

### 8.3 PUT 权限

| API | 系统角色 | 普通用户 |
|-----|----------|----------|
| PUT /Workbench | 写自己 HomeJson | 写自己 HomeJson |
| PUT /Workbench/Role/{id} | 200 | 403 |
| GET Role/{id} | 200 | 403 |

### 8.4 表面隔离

| 操作 | insight | workbench |
|------|---------|-----------|
| 添加 miniKanban | 抽屉不列；PUT 400 | 可 |
| 添加 UserCount named | Catalog 无 | Catalog 有（管理员） |
| 筛选联动 hostFilter | 有 | 恒为 null，不展示未联动角标 |

## 9. UI（Workaround 页）

DOM 自上而下：

1. **横幅**（非 widget）：左问候「{时段}好，{displayName}」+ 当天日期；右「自定义工作台」主按钮、「恢复默认」次按钮（`source==='user'` 才显示恢复，否则禁用）。时段：5–11 上午、11–13 中午、13–18 下午、其余晚上。无 displayName 用 login name。
2. **WidgetHost** `canEdit` 在自定义模式 true，否则 false。进入自定义：按钮切换 `editing`，卡片出现 e9e 操作组；再次点击「完成」退出。
3. 空墙且可编辑：虚线「添加部件」。
4. 内容区 `<800px`：所有卡 span 12（与 e9e 同）。

配置抽屉 workbench：宽 480、`placement=right`。Tab「平台部件」= named 网格（图标+标题+Admin 标记）；点一项立即 append 默认实例并关闭（已达 16 则 Message.warning）。Tab「实体指标」= 现有 kind→Sources→字段，无 host linkFilter（工作台无宿主实体，隐藏联动表）。

角色管理：仅系统角色，入口放外观设置旁「角色工作台」或 `/settings/workbench-role`。左角色列表（主数据 `GET /Admin/Role` 已有则复用），右只读预览 + 保存。保存写 PUT Role。提示「已个性化用户不会被覆盖」。

Monitor 卡 min-height h=4（320px）。KPI h=1。

不做：画布拖拽、嵌套、用户上传 Vue、把监控进程表放进工作台。

## 10. 核心文档影响

- 迁移方案 §8.5.2：实现口径（HomeJson + Parameter，非新表）；named=CubeNC 对照。
- `Doc/功能清单.md`：新增 DASH-2 首页工作台；SPA-7 home 行改为 Workbench。
- `Doc/Api/核心接口架构.md` + `前端对接指南.md`：`/Cube/Workbench`、`homeJson`、Catalog surface。
- `web/README.md`：surface=workbench、新 kind。
- 竞品分析：仪表盘行可注「首页工作台本号」。

## 11. 测试设计

XUnit 过滤器 `FullyQualifiedName~Osc26082815a1`：

- Resolver 四格真值表（§8.1）各 1 case。
- 角色 Save 后 GET 命中 LongValue；UserID 必须为 0。
- Catalog workbench 普通用户不含 UserCount/Monitor，含 MyLogins。
- TryNormalize workbench 接受 w=2 与 miniKanban；insight 仍拒。
- HomeJson 空串 GET 不把空数组当 user source。
- KPI UserCount 数值与 `User.Meta.Count` 一致（可 mock 或空库 0）。

Vitest：

- `parseDashboardJson` workbench w=8 保留、w=5→3。
- `mergeWorkspace` 丢弃 home 键（防回归）。
- 种子 admin widgets 含 `seed-Monitor`，member 不含。
- MetricCard 读 `Trend`/`trend`。

构建：`dotnet build NewLife.Cube`、`NewLife.CubeNC`；`pnpm --filter @cube/api-core test`；`pnpm --filter @cube/arco-vue test`。

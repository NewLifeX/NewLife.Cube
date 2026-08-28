# OSC-26082815a1 Verify

> 状态：**Validating**（checklist: passed）  
> 时间：2026-08-29T01:25:00+08:00  
> 触发：验收并复盘 15a1；用户授权缺口自动补齐；冒烟浏览器项仅记录。

## 验收编排

1. implementation-audit  
2. code-review  
3. doc-sync  

### 会话小任务补录

- P8.1–P8.10 已入 `tasks.md`（实体列表/卡片、拉取数量、自动轮播、横幅图标、角色空墙修复、用户>角色交叉单测）。
- 本轮验收自动补齐：角色模板空 widgets → 清除；XUnit 用户压角色 / 空串回落角色；DataList `watch` 迁出以满足 sfcThin。

### implementation-audit（三级工作台重点）

| 层 | 保存 | 展示优先级 |
| --- | --- | --- |
| 用户 | `PUT /Cube/Workbench` → `UserProfile.HomeJson`；`""` 清除并继承 | **最高**：`IsConfigured(HomeJson)` 即 `source=user`（含显式 `widgets:[]`） |
| 角色 | `PUT /Cube/Workbench/Role/{id}` → `Parameter(UserID=0, Category=Workbench.Role).LongValue`；仅系统角色可写；`""` Clear | 用户未配置时命中 → `source=role` |
| 系统 | 代码常量 `WorkbenchSeeds` Admin/Member，不入库 | 用户与角色皆未配置 → `source=system` |

- 空串 HomeJson → 不视为 user，回落角色或系统（XUnit：`EmptyHomeJson_Inherits` / `EmptyHomeJson_FallsToRole`）。
- 用户合法 JSON 压过角色（XUnit：`UserBeatsRole`）。
- 角色页空部件保存改为清除模板，避免空墙阻断系统种子（P8.9）。
- 愿景 1–4 ✅（目标 4 已扩写：insight 仅 metricCard/miniChart）。

### code-review

- 🔴 无  
- 🟡 Catalog HTTP 未单独打断言 insight kinds 长度=2（逻辑在 `WidgetController.Catalog`，DashboardJson/api-core 已拒三 kind）  
- 🟡 角色列表 `pageSize:100` 可能截断  
- 🟢 sfcThin：DataList 自动轮播已进 `useDataListWidget`  
- 🟢 未引用 CubeNC `IWidget`；无 RoleWorkspace 表  

### 愿景对照与缺口决策

- P0：无  
- P1：角色空墙 → **已自动补齐**（P8.9）  
- P2：浏览器冒烟 S.1–S.10 → **仅记录不补齐**（无浏览器工具；用户授权夜间自动执行代码缺口）  

### 测试与构建门禁（验收实测）

| 命令 | 结果 |
|------|------|
| `dotnet test … --filter FullyQualifiedName~Osc260828` | **22 passed**（含 15a1 新增交叉用例） |
| `dotnet build NewLife.Cube` | 0 error |
| `dotnet build NewLife.CubeNC` | 0 error |
| `pnpm --filter @cube/api-core test` | Vitest + node:test 全过 |
| `pnpm --filter @cube/arco-vue test` | **753 passed** |

## 冻结（全程必须保持）

- [x] `GetPage` / `Search` / `GetChartData` 未改签名  
- [x] 未引用、未修改 CubeNC `IWidget` / `Widgets/System/*`  
- [x] 无 `RoleWorkspace` 表；角色 JSON 仅 Parameter `Workbench.Role` + LongValue  
- [x] `WorkspaceJson` 仍只含 defaultView/pageSize/aiFab/aiPanel；外观 PUT 不带 homeJson  
- [x] 洞察槽：上限 12；无 miniKanban / dataList / dataCard；无 workbench-only named  
- [x] 无整页画布、无第三方市场、无租户工作台  

## 愿景对照

- [x] 目标 1：GET 用户 > 主角色 > 系统；空串继承；显式空数组 `source=user`  
- [x] 目标 2：13 named + Inbox；AdminOnly；GetData 无 HTML/无 ECharts option  
- [x] 目标 3：`/home`=Workbench；`/Admin/Index`=DefaultHome；恢复默认；管理员写角色  
- [x] 目标 4：insight 仅指标卡/迷你图表；workbench 开放看板/列表/卡片  

## 冒烟

- [ ] S.1–S.10 见 tasks.md（浏览器；本环境未手测，仅记录）

## 暂缓

- CubeNC Razor 工作台视觉再改  
- 流程待办 `flowTasks`、多门户可见范围  
- 角色页预载系统种子可视化（清除后预览仍为空壳，不影响用户 GET）  

# OSC-2610011ff2 Verify — 隐藏功能入口

## AC-1 路由

- [x] AC1.1 未把菜单 visible 改为 true 时，直接打开 `/Admin/UserToken` 进入通用列表，而不是 404。
- [x] AC1.2 侧栏不出现「用户令牌」（菜单仍为 visible false）。
- [x] AC1.3 `/Cube/Workflow/Efficiency` 在无菜单项时仍渲染效率页。
- [x] AC1.4 `/Admin/UserOnline/Kick` 不被视为白名单页。

## AC-2 入口

- [x] AC2.1 账号中心「关联」中，当前用户 id 为 3 时令牌链接为 `/Admin/UserToken?userId=3`。id 缺失时该按钮禁用。用户统计链接无 userId。
- [x] AC2.2 对象设置左侧在菜单树含隐藏的短信 url 时显示「短信设置」；菜单完全没有该 url 时不显示。

## AC-3 用户统计

- [x] AC3.1 GetChartData 返回至少 1 项时，洞察区渲染最多 2 张固定 `legacyChart`（整行 w=12、高度压缩；箱线/K 线略增高）。部件标题取自原图表 title，图内不再画 title；不接设置/升级/删除。工具栏上方不再放独立图。
- [x] AC3.2 返回空数组时不出现开发者图，列表仍在。
- [x] AC3.3 请求失败时列表仍在，并有警告。
- [x] AC3.4 boxplot 五项升序后绘制箱体（不再塌成横线）；前端 `prepareFixedChartOption` 与后端 `OnGetChartData` 双保险。

## AC-4 回归

- [x] AC4.1 `/Admin/User` 等可见菜单列表不渲染用户统计图表区。
- [x] AC4.2 `buildLeafRoutes` 仍跳过 `visible===false`。
- [x] AC4.3 本号 diff 不含对 `Menu.Visible` 的赋值或 `Save`，白名单控制器的 `[Menu]` 可见参数与 `LastUpdate` 与改前一致。库中某项仍为不可见时侧栏不出现；管理员把该项改为可见后，侧栏随菜单接口显示，无需再发版。

## 命令

```powershell
cd NewLife.Cube.ArcoVue/web
pnpm exec vitest run src/core/utils/hiddenEntryRoutes.spec.ts src/core/utils/accountLinks.spec.ts src/core/utils/objectMoreLinks.spec.ts src/core/utils/chartOptions.spec.ts
pnpm exec vue-tsc -b
dotnet build NewLife.Cube/NewLife.Cube.csproj -f net10.0
```

预期：列出的 spec 全部通过；vue-tsc 无错误；dotnet 0 错误（本号触及 `UserStatController` 箱线升序）。

## 执行记录（2026-10-01）

```powershell
cd NewLife.Cube.ArcoVue/web
pnpm exec vitest run src/core/utils/hiddenEntryRoutes.spec.ts src/core/utils/accountLinks.spec.ts src/core/utils/objectMoreLinks.spec.ts src/core/utils/chartOptions.spec.ts
pnpm exec vue-tsc -b
```

结果：4 个 spec、12 条测试通过；`vue-tsc -b` 退出码 0。

浏览器（已登录，`http://localhost:5183`）：

- 账号中心「关联」五条入口可见。刷新后 `userInfo` 为空时先禁用，补拉用户信息后令牌链到 `/Admin/UserToken?userId=1`，列表打开，侧栏无「令牌」。
- `/Admin/UserStat` 洞察区出现图，工具栏旁无图，列表仍在，页签标题为「用户统计」。
- 魔方设置左侧「更多」含短信、邮件、OAuth、访问规则（菜单 `visible=false`）。点「短信设置」进入 `/Admin/SmsConfig` 通用列表。
- `/Cube/Workflow/Efficiency` 打开效率页。
- 未改 Menus.`Visible`。

## 验收阶段记录（openspec-verify，2026-10-01）

> 触发：验收并复盘 1ff2 。

### 会话小任务补录

- 洞察区固定整行部件（替代半宽与工具栏上方面板）；无设置/升级/删除。
- 空白图：`a-spin` 块级 + `chartEl`/ResizeObserver。
- boxplot：前后端五数升序 + tall 高度。已写入 tasks T3 子条目；`status` 注明「会话小任务已补录」。

### 实现审计

对照 proposal 目标 1–4 与「不改 Menus.Visible」：

| 目标 | 结论 | 证据 |
| --- | --- | --- |
| 1 白名单直达 | 达成 | `HIDDEN_ENTRY_ROUTES` 12 条；`router/index.ts` 静态 `DynamicPage`；`[Menu(0,false)]` 未改 |
| 2 账号关联 + 对象更多 | 达成 | `accountLinks` / `objectMoreLinks` + 页签/左侧「更多」 |
| 3 UserStat 前 2 张图 | 达成（布局按执行期决策） | `useUserStatChart` → `mergeDeveloperCharts`；整行 w=12 压缩；非半宽 |
| 4 Efficiency 直达 | 达成 | `oaLeafRoutes` + `Efficiency` |

无 P0/P1 实现缺口。`Doc/功能清单.md` 本号 design 声明不改。

### 代码审查

无 🔴。注意：固定图运行时注入、不入仪表盘持久化（`persistableDashboard` 过滤 `dev-chart-*`）。boxplot 乱序已在前后端归一。

### 文档同步

- 迁移方案 B3 已含白名单与效率页静态路由句。
- proposal/design/verify AC3 与执行期用户决策对齐：洞察区固定整行部件（非半宽、非工具栏上方）。

### 测试与构建（验收重跑）

```text
vitest: 4 files / 19 tests passed（hiddenEntry 2 + accountLinks 3 + objectMore 2 + chartOptions 12）
vue-tsc -b: exit 0
dotnet build NewLife.Cube -f net10.0: 0 error（3 既有 XML warning）
```

### 缺口清单

无。P2 风险：浏览器 AC 以执行期冒烟 + 代码路径为主，本验收未再逐条点选。

### 验收结论

**通过**（checklist: passed）。可复盘 OSC-2610011ff2。

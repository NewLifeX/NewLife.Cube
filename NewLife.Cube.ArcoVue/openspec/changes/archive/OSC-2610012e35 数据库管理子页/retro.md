# Retro

> 复盘 2026-10-03 | 状态 Done
> 验收决策：P2 缺口 4 条；用户选择补齐 1（纯表可达）/2（Tooltip E2E）/3（清理 dbDiff），第 4（字典显示表名）仅记录

## 概述

ArcoVue 为 `/Admin/Db` 补数据库管理子页：卡片「更多」菜单（实体/压缩）；实体抽屉（显示名=描述首句、备注、省略+Tooltip、数字右对齐、720 宽）与数据字典下钻（实体模型 + 纯表双数据源，列对齐 CubeNC `Db/Entities.cshtml`）。执行期历经多轮会话修订：抽屉空白根治（`#columns`）、描述友好名、表/差异出口移除、列规范化、宽度对齐、验收缺口补齐。

## 目标达成（对照 proposal §1）

| 目标 | 结论 | 证据 |
| --- | --- | --- |
| 1 五个 JSON 动作 + 连接名白名单 | 达成 | `DbController`（显式路由）；`Osc2610012e35DbTests` 18/18 |
| 2 实体/压缩入口、实体抽屉、数据字典下钻（含纯表）、压缩确认 | 达成 | E2E 2/2；截图（列表/字典/纯表/Tooltip） |
| 3 权限裁剪（Detail/Update，备份保持现状） | 达成 | `getDbActionPermissions`；`dbPage.spec` |
| 4 空态与不嵌 HTML | 达成 | 「无数据/无字段/无实体模型」标识；无 Razor 嵌入 |

## 测试与构建

- `dotnet build NewLife.Cube -f net10.0`：0 错误；`dotnet build CubeDemo`（发布重启 Bin/CubeDemo）：0 错误。
- `Osc2610012e35DbTests`：18/18（非法连接 ×6、显式路由 ×5、ShowTables 描述字段、ShowEntityFields ×2、ShowTableFields ×2、Compact 路由）。
- web：`vue-tsc -b` 0；vitest dbPage 2 + useDbPage 4（描述拆分/合并纯表/备注占位）。
- api-core：72/72（含 5 个 Db 封装 URL 断言）。
- E2E `admin-db-drawer.spec.ts`：2/2（菜单缺失、列头、右对齐 CSS、省略渲染、Tooltip hover、纯表流程、宽度 720）。

## 实际完成范围

计划内 T1–T3；验收缺口补齐 T4-1/2/3。会话小任务（自 `Accepted` 起）：描述列展示、显示名/备注拆分、表出口移除、差异出口移除、数据字典下钻、列规范化（省略/对齐/表头）、抽屉宽度 720、纯表合并可选（验收缺口 1）——均已补录 `tasks.md` 与 `verify.md` AC。

## 过程中的坑

- **`a-table` 子列必须放 `#columns` 插槽**：默认插槽被 Arco 当裸 `<table>` 内容渲染，列不注册→表格 0 列空売（"抽屉全空白"）；改加载/空态分支无效，因为数据分支恒被选中。
- **空白类缺陷先取证**：Playwright `admin/admin` 会话可在真实环境 Dump DOM（`<table>` 仅注释节点）并快速定位。
- **友好名回落链**：数据库注释 → 实体 [Description] → 技术名；SQLite 无注释时全依赖实体模型。
- **UI 出口删除有连锁**：删「表」后纯表不可达（验收缺口）；删「差异」后 `flattenDiff` 与 api-core `dbDiff` 成死代码；后端 REST 动作（ModelDiff/ShowTables）保留。
- **抽屉遮罩拦截页面按钮**：E2E 切换前先 `Escape` 关闭（`escToClose` 默认开）。
- **SFC 纯净度**：展示辅助也要进 composable（验收代码审查修正，补 spec）。
- **后端重建重启流程**：`dotnet build CubeDemo`（输出直写 Bin\CubeDemo）→ 停旧进程 → 从 `Bin\CubeDemo` 启动（相对 `..\Data` 才解析到 Bin\Data）。

## 风险与后续

- P2（仅记录）：字典工具栏未展示 `tableName`；如需可补「显示名（表名）」格式。
- 观察项：`sqlite_sequence` 等系统表会以纯表行出现（备注「无实体模型（仅数据表）」可区分）。

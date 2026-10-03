# OSC-2610012e35 Verify — 数据库管理子页

## AC-1 接口

- [x] AC1.1 连接名不在配置中时五个动作 code 非 0，响应体不是 HTML。（`Osc2610012e35DbTests`）
- [x] AC1.2 ShowTables / ShowEntities / ShowEntityFields / ModelDiff 的 data 含 design §2 的数组字段。无差异时 tables 为空数组。（实现审阅 + `ShowTables` SQLite 测试）
- [x] AC1.3 Compact 成功 message 为「压缩完成」。异常时 code 为 1 且 message 非空。（实现审阅）
- [x] AC1.4 无 Detail 调用前四个被拒绝；无 Update 调用 Compact 被拒绝（沿用 EntityAuthorize，测试可反射特性）。（实现审阅）
- [x] AC1.5 ShowEntityFields 未知实体 code 非 0；Membership/User 返回字段架构（含 AI 主键标记）。（`ShowEntityFields_*`，2026-10-03 修订）
- [x] AC1.6 ShowTableFields 未知表 code 非 0；已建表返回字段架构（含主键标记）。（`ShowTableFields_*`，2026-10-03 验收缺口 1 补齐）

## AC-2 页面

- [x] AC2.1 卡片「更多」菜单提供实体、压缩（表、差异出口已移除，2026-10-03 修订）。（实现审阅 + `admin-db-drawer.spec.ts`）
- [x] AC2.2 实体抽屉列出显示名、表名、行数（右对齐）、备注（显示名=描述首句、备注=末列、单行省略渲染）；无实体显示「无数据」。（`dbPage.spec.ts` + `admin-db-drawer.spec.ts`）
- [x] AC2.3 压缩有确认；取消不发请求。（实现审阅；请求仅在 `Modal.confirm.onOk` 中发起）
- [x] AC2.4 权限对象只有 Update 时不显示实体，显示压缩。无权限对象时都显示。（`dbPage.spec.ts`）
- [x] AC2.5 点击显示名进入数据字典（同抽屉，标题=实体中文名，显示名首列、「名称」为字段名、数字列右对齐）；可返回实体列表。（`admin-db-drawer.spec.ts`）
- [x] AC2.6 两个抽屉宽度与实体对象宽抽屉一致（`RECORD_DRAWER_WIDE` 720）。（`admin-db-drawer.spec.ts`，2026-10-03 修订）
- [x] AC2.7 实体列表合并无实体模型的纯表行（备注「无实体模型（仅数据表）」），点击进入字段字典（数据源=数据库架构）；可返回。（`mergeDbEntityRows` spec + `admin-db-drawer.spec.ts`，2026-10-03 验收缺口 1 补齐）
- [x] AC2.8 字典「类型」列截断单元格 hover 弹出 Tooltip 显示完整内容。（`admin-db-drawer.spec.ts`，2026-10-03 验收缺口 2 补齐）
- [x] AC2.9 api-core 已清理 `dbDiff` 死方法（`dbTables` 因纯表合并复用而保留）。（`api.spec.ts`，2026-10-03 验收缺口 3 补齐）

## AC-3 回归

- [x] AC3.1 备份、备份并压缩、下载架构仍可用。（实现审阅；既有调用保持不变）
- [x] AC3.2 CubeNC 的 Razor `Db/Index.cshtml` 未被本号删除或改成 JSON。（变更审阅）

## 验收编排（openspec-verify，2026-10-03）

| 步骤 | 结果 |
| --- | --- |
| 实现审计 | 对照 proposal §1 四条目标与 design/tasks/verify：全部达成，无 P0/P1 缺口 |
| 代码审查 | 按 NewLife 规范复核本次 diff；修正 1 处 SFC 纯净度问题（`.vue` 内展示辅助 → `useDbPage.ts` 导出 + spec），修正后 vue-tsc 0 |
| 文档同步 | 修正 proposal 目标 1/3 与 §4、design §1/§3/§4 遗留（四个按钮/差异/flattenDiff）、迁移方案数据库页口径、功能清单 SYS-4 |

### 目标愿景对照

| 目标 | 结论 | 证据 |
| --- | --- | --- |
| 1 五个 JSON 动作 + 连接名白名单 | 达成 | `DbController` 五动作（显式路由）；后端 18/18 |
| 2 实体/压缩入口、实体抽屉、数据字典下钻（含纯表）、压缩确认 | 达成 | `index.vue`/`useDbPage`；E2E 2/2；截图 |
| 3 权限裁剪（Detail/Update，备份保持现状） | 达成 | `getDbActionPermissions`；`dbPage.spec` AC2.4 |
| 4 空态与不嵌 HTML | 达成 | 「无数据/无字段/无实体模型」空态与标识；无 Razor 嵌入 |

### 缺口处置（用户决策，2026-10-03）

- 补齐 1（纯表可达）：`ShowTableFields` + `mergeDbEntityRows` + `openDictionary` 双数据源，已复核（后端 18/18、E2E 含纯表流程）。
- 补齐 2（Tooltip E2E）：字典「类型」列 hover 断言（`AccessActionKinds`），已复核。
- 补齐 3（清理死方法）：api-core 删除 `dbDiff`；`dbTables` 因复用保留，已复核（api-core 72/72）。
- 仅记录 4：字典工具栏未显示表名（`ShowEntityFields`/`ShowTableFields` 返回 `tableName`，UI 未展示）——用户选择仅记录，见「风险」。

### 风险（仅记录）

- P2：数据字典工具栏未展示 `tableName`（技术表名），如需可后续在工具栏补「显示名（表名）」格式。
- 观察项：`sqlite_sequence` 等系统表会作为纯表行出现在实体列表末尾（备注「无实体模型（仅数据表）」可区分）。

## 命令

```powershell
dotnet test "NewLife.Cube.Tests/NewLife.Cube.Tests.csproj" --filter Osc2610012e35
dotnet build NewLife.Cube/NewLife.Cube.csproj -f net10.0
cd NewLife.Cube.ArcoVue/web
pnpm exec vitest run src/core/utils/dbPage.spec.ts
pnpm exec vue-tsc -b
cd ../../packages/api-core
pnpm exec vitest run src/api.spec.ts
```

预期：过滤测试、dbPage spec、api.spec 通过；build 与 vue-tsc 无错误。csproj 路径以仓库实际文件名为准。

## 执行记录

- `dotnet test NewLife.Cube.Tests/NewLife.Cube.Tests.csproj --filter Osc2610012e35`：9/9 通过（含显式 API 路由回归）。
- `dotnet build NewLife.Cube/NewLife.Cube.csproj -f net10.0`：0 错误。
- `pnpm exec vitest run src/core/utils/dbPage.spec.ts`：2/2 通过；`pnpm exec vitest run src/api.spec.ts`：40/40 通过。
- `pnpm exec vue-tsc -b`：通过。
- 2026-10-03 修订：抽屉空白根因修复（#columns）+ description；后端 14/14、vue-tsc 0、vitest 4/4、api-core 72/72、E2E 2/2。表清单移除、名称/备注拆分、数据字典下钻（`ShowEntityFields`）均于真机截图验证。
- 2026-10-03 二次修订：差异出口移除、列规范化（显示名/字段名、省略+Tooltip、右对齐）；vue-tsc 0、vitest 3/3、E2E 2/2（含右对齐与省略断言）；截图含 Tooltip 悬浮验证。
- 2026-10-03 三次修订：抽屉宽度 → `RECORD_DRAWER_WIDE`（720）；vue-tsc 0、E2E 2/2（含宽度断言）。
- 2026-10-03 验收：三步编排完成；门禁重跑：build 0 错误、后端 14/14、vue-tsc 0、vitest 4/4、api-core 72/72、E2E 2/2。
- 2026-10-03 缺口补齐复核：`ShowTableFields`（未知表/已建表）+ 纯表合并行 + Tooltip hover + `dbDiff` 清理；重跑：build 0 错误、后端 18/18、vue-tsc 0、vitest 6/6、api-core 72/72、E2E 2/2（含纯表与 Tooltip 断言）；截图 `db-list-pure.png`/`db-dict-pure.png`。

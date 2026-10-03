# OSC-2610012e35 — 数据库管理子页

## 1. 目标愿景

数据库卡片除备份与下载架构外，能查看实体列表与数据字典，并压缩当前连接。（2026-10-03 修订：表、差异出口移除，ModelDiff 接口保留）

- 目标 1：API `DbController` 提供 `ShowTables`、`ShowEntities`、`ShowEntityFields`、`ModelDiff`、`Compact` 的 JSON，连接名必须是已配置连接，否则 `code` 非 0（不返回 HTML）。
- 目标 2：ArcoVue 数据库卡片每个连接增加「实体」「压缩」（收纳于「更多」菜单）；实体打开右侧抽屉并下钻数据字典；压缩须确认。（2026-10-03 二次修订：表、差异出口移除）
- 目标 3：无 Detail 权限不显示实体入口（含数据字典；表、差异出口已移除）；无 Update 权限不显示压缩。备份按钮的显隐保持现状。
- 目标 4：抽屉无数据时显示「无数据」，不把 HTML 视图嵌进 SPA。

## 2. 为何做

MVC [`Db/Index.cshtml`](../../../../NewLife.CubeNC/Areas/Admin/Views/Db/Index.cshtml) 有表清单、实体清单、模型差异和压缩。这些动作在 CubeNC `DbController`，返回 Razor。SPA 使用的 [`NewLife.Cube` DbController](../../../../NewLife.Cube/Areas/Admin/Controllers/DbController.cs) 只有备份、备份并压缩、下载架构。ArcoVue 卡片因此也无法做这四项。

## 3. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | 在 `NewLife.Cube` 的 `DbController` 增加五个动作（含 `ShowEntityFields` 数据字典），算法对齐 CubeNC（连接名白名单、`CompactDatabase`、表行数、实体行数、库有而实体无的列、字段架构表）。返回 `Json(0, null, data)`，不 `return View`。 |
| 2 | `ShowTables` data：`{ name, tables: [{ name, tableName, description, count }] }`。`ShowEntities` data：`{ name, entities: [{ name, tableName, description, count }] }`。`description` 为展示友好名（数据库注释优先、回落实体模型 `[Description]`，可空）。`ShowEntityFields` data：`{ name, type, tableName, fields: [{ name, displayName, type, length, precision, scale, key, nullable, description }] }`（对齐 CubeNC `Db/Entities.cshtml`）。`ModelDiff` data：`{ name, tables: [{ name, tableName, displayName, hasEntityModel, columns: [{ name, columnName, dataType }] }] }`。不返回 XML 片段。（2026-10-03 补 `description` 与 `ShowEntityFields`） |
| 3 | `Compact` 成功 `Json(0, "压缩完成")`，失败 `Json(1, 异常消息)`，不重定向到 Index HTML。权限：表/实体/差异 = Detail；压缩 = Update。 |
| 4 | 前端抽屉宽与实体对象抽屉一致（`RECORD_DRAWER_WIDE` 720），从右侧打开。实体用 `a-table`：显示名（描述首句，点击进入数据字典）、表名、行数（右对齐）、备注（描述余下部分，末列；单行省略+Tooltip）；数据字典同抽屉展示、可返回；表、差异出口移除。 |
| 5 | 压缩确认文案：「确认压缩数据库 {name}？SQLite 将执行 VACUUM。」进行中卡片按钮 loading，禁止连点。 |
| 6 | 非法连接名、空列表、接口 `code!==0` 均在抽屉或卡片上用 `a-alert` 展示 `message`，不关闭已打开的抽屉（压缩失败保持卡片）。 |

## 4. 做什么

1. 将 CubeNC 四段查询抽到 `NewLife.Cube` 的 JSON 动作（可私有方法，不新建公共服务，除非 CubeNC 与 API 必须共享同一类型——本号不要求改 CubeNC）。
2. `useDbPage` 增加 `dbEntities` / `dbEntityFields` / `dbCompact` 等调用；`index.vue` 只绑定菜单与抽屉可见性。
3. api-core 如无现成方法，在 page API 增加五个薄封装（`dbTables` / `dbEntities` / `dbEntityFields` / `dbDiff` / `dbCompact`）。
4. 后端测试：非法连接名拒绝；合法名在 SQLite 测试库上 `ShowTables` 返回数组（允许 0 行）。不要求断言生产库差异内容。
5. 前端测试：按钮权限真值、描述拆分（`splitDbDescription`）；diff 行展平随差异出口移除而删。

## 5. 不做什么

- 不在浏览器执行 SQL，不提供改表、删表、还原备份。
- 不把 CubeNC Razor 页删掉或改成 JSON（MVC 站点保持 View）。
- 不实现 Session / Cache 监控子页。
- 不做模型差异的「应用 XML 到实体」写回。

## 6. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-2608139feb | 数据库卡片页已存在，本号只加动作 |
| CubeNC `DbController` | 行为对照，不共用 Razor 模型类型 |

## 7. 测试范围

触及 `NewLife.Cube` 与 `web/`（及 `packages/api-core` 若加封装）。执行期跑本号 XUnit 与 Vitest。验收期新增单测全过；`dotnet build NewLife.Cube -f net10.0` 与 `vue-tsc -b` 无错误。

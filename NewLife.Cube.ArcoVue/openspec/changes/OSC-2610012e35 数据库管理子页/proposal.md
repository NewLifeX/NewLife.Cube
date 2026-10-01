# OSC-2610012e35 — 数据库管理子页

## 1. 目标愿景

数据库卡片除备份与下载架构外，能查看表、查看实体、查看模型差异，并压缩当前连接。

- 目标 1：API `DbController` 提供 `ShowTables`、`ShowEntities`、`ModelDiff`、`Compact` 的 JSON，连接名必须是已配置连接，否则 400。
- 目标 2：ArcoVue 数据库卡片每个连接增加「表」「实体」「差异」「压缩」。前三个打开右侧抽屉；压缩须确认。
- 目标 3：无 Detail 权限不显示前三个；无 Update 权限不显示压缩。备份按钮的显隐保持现状。
- 目标 4：抽屉无数据时显示「无数据」，不把 HTML 视图嵌进 SPA。

## 2. 为何做

MVC [`Db/Index.cshtml`](../../../../NewLife.CubeNC/Areas/Admin/Views/Db/Index.cshtml) 有表清单、实体清单、模型差异和压缩。这些动作在 CubeNC `DbController`，返回 Razor。SPA 使用的 [`NewLife.Cube` DbController](../../../../NewLife.Cube/Areas/Admin/Controllers/DbController.cs) 只有备份、备份并压缩、下载架构。ArcoVue 卡片因此也无法做这四项。

## 3. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | 在 `NewLife.Cube` 的 `DbController` 增加四个动作，算法对齐 CubeNC（连接名白名单、`CompactDatabase`、表行数、实体行数、库有而实体无的列）。返回 `Json(0, null, data)`，不 `return View`。 |
| 2 | `ShowTables` data：`{ name, tables: [{ name, tableName, count }] }`。`ShowEntities` data：`{ name, entities: [{ name, tableName, count }] }`。`ModelDiff` data：`{ name, tables: [{ name, tableName, displayName, hasEntityModel, columns: [{ name, columnName, dataType }] }] }`。不返回 XML 片段。 |
| 3 | `Compact` 成功 `Json(0, "压缩完成")`，失败 `Json(1, 异常消息)`，不重定向到 Index HTML。权限：表/实体/差异 = Detail；压缩 = Update。 |
| 4 | 前端抽屉宽与记录抽屉相同（`recordDrawerWidth`），从右侧打开。表与实体用 `a-table`：名称、表名、行数。差异用表名分组，下列出多出的列名与类型；`hasEntityModel=false` 时分组标题加「无实体」。 |
| 5 | 压缩确认文案：「确认压缩数据库 {name}？SQLite 将执行 VACUUM。」进行中卡片按钮 loading，禁止连点。 |
| 6 | 非法连接名、空列表、接口 `code!==0` 均在抽屉或卡片上用 `a-alert` 展示 `message`，不关闭已打开的抽屉（压缩失败保持卡片）。 |

## 4. 做什么

1. 将 CubeNC 四段查询抽到 `NewLife.Cube` 的 JSON 动作（可私有方法，不新建公共服务，除非 CubeNC 与 API 必须共享同一类型——本号不要求改 CubeNC）。
2. `useDbPage` 增加四个调用；`index.vue` 只绑定按钮与抽屉可见性。
3. api-core 如无现成方法，在 page API 增加四个薄封装。
4. 后端测试：非法连接名拒绝；合法名在 SQLite 测试库上 `ShowTables` 返回数组（允许 0 行）。不要求断言生产库差异内容。
5. 前端测试：按钮权限真值、diff 行展平。

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

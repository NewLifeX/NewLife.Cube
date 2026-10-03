# OSC-2610012e35 Design — 数据库管理子页

## 0. 适用框架与官方资料

| 场景 | 框架 | 资料 |
| --- | --- | --- |
| 卡片按钮、抽屉、表格、确认 | Arco Design Vue | https://arco.design/vue/docs/start （Drawer、Table、Modal） |
| 抽屉宽度 | 现有 `RECORD_DRAWER_WIDE` | 与实体对象 详情/编辑/添加 宽抽屉一致（720），`placement=right`（2026-10-03 修订） |
| SFC | README | `index.vue` 只绑定 `useDbPage` |

## 1. 文件级改动地图

| 文件 | 改动 | 保留 |
| --- | --- | --- |
| `NewLife.Cube/Areas/Admin/Controllers/DbController.cs` | 五个 JSON 动作（含 ShowEntityFields 数据字典） | 现有 Index / Backup / BackupAndCompress / Download |
| `NewLife.Cube.Tests/Osc2610012e35DbTests.cs` | 新建。非法连接名；内存或测试 SQLite 上 ShowTables 返回 code 0 | — |
| `packages/api-core/src/api.ts` | `createPageApi` 增加 `dbTables` / `dbEntities` / `dbEntityFields` / `dbDiff` / `dbCompact`（表、差异方法为接口保留，UI 未用） | 现有 backup |
| `packages/api-core/src/api.spec.ts` | 五条 URL 与方法 | — |
| `web/src/views/admin/db/useDbPage.ts` | 打开抽屉、压缩确认、权限 | 备份与下载架构 |
| `web/src/views/admin/db/index.vue` | 「更多」菜单（实体/压缩）与一个抽屉 | 卡片布局 |
| `web/src/core/utils/dbPage.ts` | 纯函数：权限、描述拆分（diff 行展平已随差异出口移除） | — |
| `web/src/core/utils/dbPage.spec.ts` | 新建 | — |

CubeNC 的 `DbController` 与 cshtml 不改。

## 2. JSON

连接名不在 `DAL.ConnStrs` 时五个动作都 `Json(1, "非法操作！")`（或抛验证异常后由全局中间件变成非 0，实现选一种并在测试里锁定）。不要返回 HTML。

`ShowTables`：

```json
{ "name": "Membership", "tables": [{ "name": "User", "tableName": "User", "description": "用户", "count": 1 }] }
```

`ShowEntities`：`entities[]` 字段 `name`、`tableName`、`description`、`count`（表不存在时 count 为 null）。

`description`（2026-10-03 修订）：展示友好名。数据库表注释优先；为空时回落实体模型 `[Description]`（`EntityFactory.GetTables(name, false)` 的 `DataTable.Description`）；两者皆空为 null，前端「名称」列回落技术名，「表名」列保留技术名。

`ShowEntityFields`（2026-10-03 新增）：参数 `name` + `type`（实体类名），返回 `{ name, type, tableName, fields: [{ name, displayName, type, length, precision, scale, key, nullable, description }] }`。`key` 取值 `AI`（自增）/`PK`（主键）/`UQ`（单列唯一索引）/null；`description` 去掉 `DisplayName` 前缀与首部「。，」（对齐 CubeNC `Db/Entities.cshtml` 字段架构表）；未知实体 `Json(1, "实体不存在！")`。

`ModelDiff`：`tables[]` 含 `name`、`tableName`、`displayName`、`hasEntityModel`、`columns[]`（`name`、`columnName`、`dataType`）。无差异时 `tables: []`。

`Compact`：成功 message「压缩完成」；捕获异常 message 为异常文本，code 为 1。

权限特性与 CubeNC 一致：前三个 `PermissionFlags.Detail`，压缩 `PermissionFlags.Update`。

## 3. 卡片按钮

在现有「下载架构」之后（2026-10-03 修订：收纳进「更多」下拉；表、差异出口移除，ModelDiff 接口保留）：

| 按钮 | 权限 | 行为 |
| --- | --- | --- |
| 实体 | Detail | 抽屉标题「实体 · {name}」 |
| 压缩 | Update | 确认后调用 Compact，成功 Message 并刷新卡片列表 |

实体抽屉（2026-10-03 修订）：列 = 显示名（描述首句，点击进入数据字典）/ 表名 / 行数（右对齐）/ 备注（描述首个「。」之后余下部分，最后一列）；各列单行省略（`:ellipsis="true" tooltip`，仅溢出时提示完整内容）；表格启用横向滚动。数据字典复用同抽屉：标题「数据字典 · {实体中文名}」（回落技术名），工具栏右侧「{type} · 共 N 个字段」+「← 返回实体列表」，列对齐 CubeNC `Db/Entities.cshtml` 字段架构定义：显示名、字段名、类型、长度（右对齐）、精度（右对齐）、主键（AI/PK/UQ）、允许空（N=不允许空）、备注；表头不折行。

`getMenuPermission('Admin/Db')` 无键时入口都显示（与当前备份在无键时的策略对齐：无键则 `canBackup` 现逻辑若为「有键才备份」，压缩跟随 Update 键；无权限对象时显示压缩。实现时读 `useDbPage` 的 `canBackup`：压缩使用同一套「无键则允许、有键则看 Update」）。实体（含数据字典）看 Detail，无键则允许。

（2026-10-03 二次修订：差异出口与 `flattenDiff` 已移除；`ModelDiff` JSON 接口保留供 API 消费。）

## 4. 核心文档影响

| 文档 | 影响 |
| --- | --- |
| `ArcoVue企业中后台迁移方案.md` | 修改：数据库页能力口径（实体/数据字典/压缩；表、差异出口移除） |
| `Doc/Api/核心接口架构.md` | 修改：补 `Admin/Db/ShowTables|ShowEntities|ShowEntityFields|ModelDiff|Compact` |
| `Doc/功能清单.md` | 同步 SYS-4 行（实体/数据字典/压缩口径） |
| `web/README.md` | 无 |

## 5. 测试设计

后端：连接名 `../x` 或空串 → code 非 0。测试库连接名（与现有 Cube 测试相同的 SQLite 名）`ShowTables` code 0 且 `tables` 为数组；`ShowEntityFields` 未知实体 code 非 0，Membership/User 返回字段架构（含 AI 主键标记）。

前端：`flattenDiff` 把两列展成两行；`splitDbDescription` 按首个「。」拆分名称/备注；无 Detail 键集合 `{ Update: true }` 时查看动作 false、压缩 true。

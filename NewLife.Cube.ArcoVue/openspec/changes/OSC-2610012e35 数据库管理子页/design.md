# OSC-2610012e35 Design — 数据库管理子页

## 0. 适用框架与官方资料

| 场景 | 框架 | 资料 |
| --- | --- | --- |
| 卡片按钮、抽屉、表格、确认 | Arco Design Vue | https://arco.design/vue/docs/start （Drawer、Table、Modal） |
| 抽屉宽度 | 现有 `recordDrawerWidth` | 与记录抽屉一致，`placement=right` |
| SFC | README | `index.vue` 只绑定 `useDbPage` |

## 1. 文件级改动地图

| 文件 | 改动 | 保留 |
| --- | --- | --- |
| `NewLife.Cube/Areas/Admin/Controllers/DbController.cs` | 四个 JSON 动作 | 现有 Index / Backup / BackupAndCompress / Download |
| `NewLife.Cube.Tests/Osc2610012e35DbTests.cs` | 新建。非法连接名；内存或测试 SQLite 上 ShowTables 返回 code 0 | — |
| `packages/api-core/src/api.ts` | `createPageApi` 增加 `dbTables` / `dbEntities` / `dbDiff` / `dbCompact` | 现有 backup |
| `packages/api-core/src/api.spec.ts` | 四条 URL 与方法 | — |
| `web/src/views/admin/db/useDbPage.ts` | 打开抽屉、压缩确认、权限 | 备份与下载架构 |
| `web/src/views/admin/db/index.vue` | 四个按钮与一个抽屉 | 卡片布局 |
| `web/src/core/utils/dbPage.ts` | 纯函数：权限、diff 行展平 | — |
| `web/src/core/utils/dbPage.spec.ts` | 新建 | — |

CubeNC 的 `DbController` 与 cshtml 不改。

## 2. JSON

连接名不在 `DAL.ConnStrs` 时四个动作都 `Json(1, "非法操作！")`（或抛验证异常后由全局中间件变成非 0，实现选一种并在测试里锁定）。不要返回 HTML。

`ShowTables`：

```json
{ "name": "Membership", "tables": [{ "name": "User", "tableName": "User", "count": 1 }] }
```

`ShowEntities`：`entities[]` 字段 `name`、`tableName`、`count`（表不存在时 count 为 null）。

`ModelDiff`：`tables[]` 含 `name`、`tableName`、`displayName`、`hasEntityModel`、`columns[]`（`name`、`columnName`、`dataType`）。无差异时 `tables: []`。

`Compact`：成功 message「压缩完成」；捕获异常 message 为异常文本，code 为 1。

权限特性与 CubeNC 一致：前三个 `PermissionFlags.Detail`，压缩 `PermissionFlags.Update`。

## 3. 卡片按钮

在现有「下载架构」之后：

| 按钮 | 权限 | 行为 |
| --- | --- | --- |
| 表 | Detail | 抽屉标题「表 · {name}」 |
| 实体 | Detail | 抽屉标题「实体 · {name}」 |
| 差异 | Detail | 抽屉标题「差异 · {name}」 |
| 压缩 | Update | 确认后调用 Compact，成功 Message 并刷新卡片列表 |

`getMenuPermission('Admin/Db')` 无键时四个按钮都显示（与当前备份在无键时的策略对齐：无键则 `canBackup` 现逻辑若为「有键才备份」，压缩跟随 Update 键；无权限对象时显示压缩。实现时读 `useDbPage` 的 `canBackup`：压缩使用同一套「无键则允许、有键则看 Update」）。表/实体/差异看 Detail，无键则允许。

差异表列：表名、有无实体、列名、类型。一个表多列则多行（`flattenDiff`）。无差异：抽屉内「无差异」。

## 4. 核心文档影响

| 文档 | 影响 |
| --- | --- |
| `ArcoVue企业中后台迁移方案.md` | 修改：数据库页四项能力 |
| `Doc/Api/核心接口架构.md` | 修改：补 `Admin/Db/ShowTables|ShowEntities|ModelDiff|Compact` |
| `Doc/功能清单.md` | 若已有数据库条目则补测试列；没有则新增一行 DB-1 |
| `web/README.md` | 无 |

## 5. 测试设计

后端：连接名 `../x` 或空串 → code 非 0。测试库连接名（与现有 Cube 测试相同的 SQLite 名）`ShowTables` code 0 且 `tables` 为数组。

前端：`flattenDiff` 把两列展成两行；无 Detail 键集合 `{ Update: true }` 时表按钮 false、压缩 true。

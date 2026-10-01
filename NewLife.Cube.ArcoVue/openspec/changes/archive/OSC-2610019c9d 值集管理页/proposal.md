# OSC-2610019c9d — 值集管理页

## 1. 目标愿景

管理员从菜单「值集」进入可维护手工名值定义的页面，而不是「无法识别页面类型」。

- 目标 1：`/Admin/Lov` 无论菜单是否已注册动态路由，都打开值集列表，可按关键字筛选、新增、编辑、删除手工定义。
- 目标 2：ENUM 定义可维护枚举项；LIST 定义可维护数据源地址、取值/显示字段、搜索字段与表格列；保存走现有 `GetConfig` / `SaveConfig`。
- 目标 3：无 Insert / Update / Delete 权限时对应按钮不可用；接口失败时列表区给出警告，不白屏。
- 目标 4：表单里已有的值集选择（Meta / ListData / BatchLabel）行为不变。

## 2. 为何做

[`LovController`](../../../../NewLife.Cube/Areas/Admin/Controllers/LovController.cs) 不是实体控制器，没有 `GetPage`。ArcoVue `DynamicPage` 探测失败后显示空状态。Cube.Vue 已有 [`lov/index.vue`](../../../../NewLife.Cube.Vue/web/apps/cube-admin/src/views/admin/lov/index.vue) 与 `config.vue`。API 的列表、详情、增删改和 `GetConfig` / `SaveConfig` 已存在，缺的是 Arco 页面。

## 3. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | 页面只管理 `LovStore` 手工定义。代码枚举与 `[LovList]` 不在本列表出现（与控制器 Index 一致）。 |
| 2 | 列表数据：`GET /api/Admin/Lov`。详情：`GET /api/Admin/Lov/Detail?id=`。新增：`POST /api/Admin/Lov`。更新：`PUT /api/Admin/Lov`。删除：`DELETE /api/Admin/Lov?id=`。配置：`GET /api/Admin/Lov/GetConfig?id=`、`POST /api/Admin/Lov/SaveConfig`。 |
| 3 | 定义字段：`lovCode`、`name`、`type`（仅 `ENUM` / `LIST`）、`valueField`、`labelField`、`enabled`、`remark`。`lovCode` 新增后不可改。 |
| 4 | 配置抽屉：`type=ENUM` 编辑 `enumItems`（value、label、sort、enabled）；`type=LIST` 三 Tab 对齐 Cube.Vue（listConfig 全字段 + searchFields + tableColumns 及编辑弹层）。保存为全量替换，与 `SaveConfig` 现语义一致。 |
| 5 | 路由：`router/index.ts` 增加静态子路由 `Admin/Lov`；`pageKind` 将 `admin/lov` 标为 `custom`，`DynamicPage` 挂值集页。菜单动态注册与静态路由命中同一页。 |
| 6 | 权限：用 `userStore.getMenuPermission('Admin/Lov')`。无键或含 Detail 可看列表；无 Insert 隐藏新增；无 Update 隐藏编辑与配置保存；无 Delete 隐藏删除。 |

## 4. 做什么

1. 新增值集列表页与配置抽屉（薄 `.vue` + composable）。
2. 静态路由与 `pageKind=custom` 短路，避免再落入 unknown。
3. Vitest 覆盖列表行映射、权限按钮、配置体组装。
4. 文档：迁移方案功能对照补一行「值集管理页」。

## 5. 不做什么

- 不改 `Meta` / `ListData` / `BatchLabel` 的解析与行权。
- 不把代码枚举导入为手工定义，不提供「从枚举同步」按钮。
- 不重做通用 `DefaultList`，本页不调用 `GetPage`。
- 不做值集市场、导入导出、版本历史。

## 6. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-260926c2b8 | 值集 `entity:` 行权已在 `ListData`；本号不改该路径 |
| OSC-2608139feb | `DynamicPage` / `pageKind` 的 custom 短路模式（Db/File）照此扩展 |
| Cube.Vue `lov/config.vue` | 只对照字段，不复制 Element Plus 组件 |

## 7. 测试范围

触及 `web/` 与样例种子（`LovSampleSeeds` + `LovController.Index` / `UseCube` 幂等调用）。执行期跑本号 Vitest 与 `LovSampleSeedsTests`，并 `vue-tsc -b`。不改 Meta / ListData / BatchLabel。验收期本号新增单测全部通过，`pnpm exec vue-tsc -b` 与 `dotnet build NewLife.Cube` 无错误。

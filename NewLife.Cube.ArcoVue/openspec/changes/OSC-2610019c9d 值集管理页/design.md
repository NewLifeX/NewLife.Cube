# OSC-2610019c9d Design — 值集管理页

## 0. 适用框架与官方资料

| 场景 | 框架 | 资料 |
| --- | --- | --- |
| 列表、抽屉、表单、表格、空状态 | Arco Design Vue | https://arco.design/vue/docs/start （Table、Drawer、Form、Modal、Empty） |
| 页面结构 | 本仓库 SFC 规则 | `openspec/README.md`「SFC 职责分离」 |

`.vue` 只做模板与 `useXxx()` 绑定。请求与校验在 composable。

## 1. 文件级改动地图

| 文件 | 改动 | 保留 |
| --- | --- | --- |
| `web/src/views/admin/lov/index.vue` | 新建。工具栏、表格、定义抽屉、配置抽屉的模板 | 无业务 ref |
| `web/src/views/admin/lov/useLovPage.ts` | 列表加载、关键字、定义保存、删除、权限 | — |
| `web/src/views/admin/lov/LovConfigDrawer.vue` | 新建。ENUM 表或 LIST 表单 | 无请求 |
| `web/src/views/admin/lov/useLovConfig.ts` | GetConfig / 组装 SaveConfig / 校验 | — |
| `web/src/core/utils/lovAdmin.ts` | 新建纯函数：行映射、权限、配置体 | — |
| `web/src/core/utils/lovAdmin.spec.ts` | 新建 | — |
| `web/src/core/utils/pageKind.ts` | `CUSTOM_TYPES` 加入 `admin/lov` | Db/File/服务页规则 |
| `web/src/views/dynamic/DynamicPage.vue` | `isLovPage` 时挂值集页 | 其它 kind |
| `web/src/views/dynamic/useDynamicPage.ts` | `isLovPage`：规范化路径 `admin/lov` | — |
| `web/src/router/index.ts` | 静态子路由 `Admin/Lov`，组件 `DynamicPage`，`meta.title=值集`，`meta.typePath=Admin/Lov` | 现有静态路由 |

不改 `LovController`、`Meta`、`ListData`、`BatchLabel`、`FieldInput`。

## 2. 权限与按钮

`perms = getMenuPermission('Admin/Lov')`。无任何键视为可看（与数据库页「有菜单即可看」一致）。有键时：

| 条件 | 新增 | 编辑 / 配置保存 | 删除 |
| --- | --- | --- | --- |
| 无权限对象或空对象 | 显示 | 显示 | 显示 |
| 有键且无 Insert | 隐藏 | — | — |
| 有键且无 Update | — | 隐藏 | — |
| 有键且无 Delete | — | — | 隐藏 |
| 有键且无 Detail | 整页 `a-alert`「无权查看值集」，不请求列表 | | |

## 3. 列表与定义抽屉

列：编码、名称、类型、启用、操作。类型只显示 `ENUM` 或 `LIST`。关键字过滤编码与名称（前端对已加载行过滤；请求仍带 `Q`，与 Index 的 `p["Q"]` 一致）。

定义抽屉 `placement=right`，宽 `recordDrawerWidth`。新增：编码可编辑，必填，仅字母数字点与下划线，最长 64。编辑：编码只读。类型必选 ENUM 或 LIST。名称必填，最长 40。保存失败展示 `message`，抽屉不关。

删除用 `Modal.confirm`，文案「确认删除值集 {lovCode}？」。

## 4. 配置抽屉

仅编辑已保存的定义（有 lovCode）。打开时 `GetConfig`。

| type | 可见区域 | 保存 body |
| --- | --- | --- |
| ENUM | 枚举表：value、label、sort；可增删行 | `{ id, enumItems }` |
| LIST | requestUrl、valueField、labelField；搜索字段表；表格列表 | `{ id, listConfig, searchFields, tableColumns }` |
| 其它或空 | `a-empty`「该类型不能在此配置」 | 保存按钮禁用 |

ENUM 行：value、label 必填，空行在提交前剔除。LIST 的 requestUrl 允许空（保存后由后端按现逻辑处理）；不在前端发代理请求试拉。

## 5. 空与失败

| 输入 | 界面 |
| --- | --- |
| 列表 `[]` | 表格空「暂无手工值集」 |
| 列表请求抛错 | `a-alert` warning，表格空 |
| GetConfig 失败 | 抽屉内 alert，不展示半份旧数据 |
| SaveConfig `code!==0` | Message.error，抽屉保持 |

## 6. 核心文档影响

| 文档 | 影响 |
| --- | --- |
| `ArcoVue企业中后台迁移方案.md` | 修改：功能对照补「值集管理页 /Admin/Lov」 |
| `web/README.md` | 无 |
| `Doc/功能清单.md` | 无（不新增后端能力） |
| `Doc/Api/核心接口架构.md` | 无 |

## 7. 测试设计

`lovAdmin.spec.ts`：

- 权限空对象 → 三个按钮都允许。
- 仅有 Detail → 新增/编辑/删除均为否。
- ENUM 配置剔除空 value 行，保留 0 与 false 不在本表（value 为字符串）。
- LIST body 含 requestUrl 与列数组。
- 类型 `AUTO` → `canSaveConfig=false`。

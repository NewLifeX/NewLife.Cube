# OSC-2608139feb — 表单值集级联与通用对象主页

## 1. 为何做

ArcoVue 实体页已能零配置渲染列表与抽屉表单，但添加/编辑/详情对 **状态 / 枚举 / 值集 / 地区级联** 仍有断裂：级联选择因 `path-mode` 与取值形状不一致而清空、懒加载无 `load-more`、详情与列表常显示原始 ID。系统管理与魔方管理一批实体因此无法正确编辑。

同时，菜单中的 **ObjectController 配置页**（`/Admin/Cube`、`/Admin/Sys`、`/Admin/Core`、`/Admin/XCode`）与 **首页 / `/Admin/Index`** 没有对应前端：一律掉进 `DefaultList`，而 WebAPI 契约是 `GET/PUT /api/{Area}/{Controller}` + `GetFields`，以及 `GET /api/Admin/Index/Main` 等仪表盘接口。Cube.Vue 用手工页补齐；ArcoVue 需要 **通用探测 + 元数据表单**，使任意 ObjectController 子类无需手写页面。

## 2. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | **一次做完**：通用 Object/Config 表单页（Cube/Sys/Core/XCode 及未来同类控制器共用）+ 主页对接 `/Admin/Index/Main`，并对标 Cube.Vue 仪表盘（ServerVarList / ProcessList / AssemblyList / 刷新 / MemoryFree / Restart）。 |
| 2 | **验收深度 2C**：Playwright E2E 覆盖系统管理与魔方管理主要实体的添加/编辑/详情路径，以及主页与至少两个 Object 配置页；同时补 Vitest/XUnit。 |
| 3 | 字段事实源仍是后端 `GetPage` / `GetFields` / `PrepareForApi`。前端不按字段名猜业务关系。 |
| 4 | 静态 `dataSource` / `dataSourceMap` 优先本地下拉；LIST / 无字典的 `lovCode` 走 LovSelect；`itemType` 为 `area`/`area4`/`cascader` 走地区级联，提交叶子 ID。 |
| 5 | Object 页 **不** 走 `GetPage`。探测失败 `GetPage` 后改用 `GET {type}` + `GetFields` + `PUT {type}`。禁止为 Cube/Sys/Core/XCode 各写一份手工表单。 |
| 6 | `/home` 与菜单 `/Admin/Index` 共用同一仪表盘实现。`Admin/Db`、`Admin/File` 本号一并实现专用页；`Admin/Star`（`ConfigController<StarSetting>`，继承 ObjectController）复用通用 DefaultObject，不写手工表单。 |
| 7 | 不改 Cube.Vue 前端；后端仅允许：`ObjectController.GetFields` 调用 `PrepareForApi`，级联/标签所需的既有 Area/LOV API 消费，`IndexController` 仪表盘 JSON 化（`Main` 增加 AJAX 分支、搬运 MVC 既有 ServerVarList/ProcessList/AssemblyList、MemoryFree 改 JSON 返回），以及 `FileController` 的 SPA 最小修复（`Index` 返回列表 JSON、动作返回 JSON 而非 Redirect；均不新造业务接口）。 |

## 3. 做什么

1. 修复 Cascader：`path-mode` 与 onChange 取值对齐、绑定 `load-more`、编辑回显路径。
2. 详情/列表地区名与 LIST LOV 标签：`hydrateAreaLabels` + `areaLabelCache` 接入 `formatFieldValue` / `detailText`；详情分区也走 BatchLabel。
3. 枚举类 `typeName` 提交按数字键强制为 number（与 `Enum` 一致）。
4. `DynamicPage` 按 `detectPageKind` 分发：`entity` → DefaultList，`object` → DefaultObject，`home` → DefaultHome。
5. 通用 DefaultObject：按 Category 分 Tab，复用 FieldInput / enrich / 提交归一化。
6. DefaultHome：消费 IndexController 的 Main / ServerVarList / ProcessList / AssemblyList，并提供刷新、MemoryFree、Restart。
7. api-core 补对象 GET 与首页接口封装。
8. 新增 `Admin/Db` 专用页：数据库列表 + 备份/备份并压缩/下载架构，消费后端既有 `GET /api/Admin/Db` 与 Backup 动作。
9. 新增 `Admin/File` 专用页：目录导航 + 上传/下载/压缩/解压/复制粘贴/移动/删除；后端 `FileController` 最小修复后对接。
10. `Admin/Star` 经探测落入 DefaultObject 并验证保存，不写手工表单。
11. Playwright E2E + 单测 + 构建门禁；实体验收清单写入 verify。
12. 魔方设置优化（DefaultObject 配置中心）：外围底部常驻保存面板（保存按钮右侧）、展示区主题外壳；description 经 `a-form-item` 的 `tooltip`（Arco 官方 label 问号气泡）展示；左列表右配置，菜单树自动发现 ObjectController 配置页注入左侧列表；对象的 Category（如魔方设置的 通用/用户登录/界面配置/AI/系统功能）作为子菜单管理并自动全部展开；右侧按 Category 分组不折叠，分组框占满面板，每个配置项占一行居中排版、宽 6/12（全宽控件 12/12）。

## 4. 不做什么

- 不为 `Admin/Star` 写手工表单（复用 DefaultObject）；不实现 `Admin/Db`、`Admin/File` 之外的其它 Admin 定制页。
- 不把 Cube.Vue `admin/cube/index.vue` 等手工表单搬到 ArcoVue；`Admin/Db`、`Admin/File` 按 ArcoVue 自身交互重写，不做字段文案级 1:1 复制。
- 不引入低代码画布、跨实体公式、非 Area 的通用任意 URL 级联数据源（`itemType=cascader` 仍走 `/Cube/Area`）。
- 不改 `Int64AsString` 契约；不在详情用未净化 `v-html`。
- 不把 E2E 失败归因于「环境未起」而不写可重复命令与默认账号。

## 5. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-0003 | Done：DynamicPage / DefaultList / FieldInput |
| OSC-0008 | Done：提交归一化 / RecordDrawer |
| OSC-0009 | Done：dataSource 优先、BatchLabel、CascaderField 初版、detailFormat |
| OSC-260813c3e9 | 进行中/已落地：SFC 薄脚本；本号新增 `.vue` 必须同目录 `useXxx.ts` |
| Cube WebAPI | `ObjectController`、`IndexController`、`ReadOnlyEntityController.GetPage` |

## 6. 测试范围

| 类型 | 是否做 | 说明 |
| --- | --- | --- |
| Vitest | 是 | detectPageKind、cascader onChange、area/detail 标签、enum-like 提交、Object 字段分组 |
| Playwright E2E | 是 | 登录后逐实体打开添加/编辑/详情；主页四块数据；Cube+Sys 对象页保存控件 |
| XUnit / dotnet build | 是 | ObjectController.GetFields 物化 DataSourceMap（能单测则测，否则 build 验证） |
| 构建 | 是 | `pnpm --filter @cube/arco-vue test`、`pnpm --filter @cube/arco-vue build`、`dotnet build NewLife.Cube` |

## 7. 成功标准

- [ ] User 等实体的添加/编辑/详情：布尔开关、枚举/值集下拉显示可读标签，地区级联可选中并回显路径，提交后列表/详情不再只显示原始 ID。
- [ ] `/home` 与 `/Admin/Index` 展示 Main 真实字段，并可刷新 ServerVar / Process / Assembly；MemoryFree、Restart 有确认后调用对应接口。
- [ ] `/Admin/Cube`、`/Admin/Sys`、`/Admin/Core`、`/Admin/XCode` 均由同一 DefaultObject 渲染并可 PUT 保存，无各自手工表单。
- [ ] `/Admin/Db` 展示数据库列表并可备份/备份并压缩/下载架构；`/Admin/File` 可导航目录并执行上传/下载/压缩/解压/复制粘贴/删除；`/Admin/Star` 由 DefaultObject 渲染并可 PUT 保存。
- [ ] Playwright 对本号列出的实体与对象/主页/Db/File 路径可重复执行；本号新增单测全过；web 与 NewLife.Cube 构建无错误。

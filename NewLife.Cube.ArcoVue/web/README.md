# NewLife.Cube.ArcoVue 前端源码

基于 **Vue 3 + Arco Design Vue + Vite** 的魔方管理后台前端。

## 技术栈

- Vue 3
- Arco Design Vue（字节跳动开源组件库）
- Vite 6
- Pinia 3
- TypeScript
- pnpm

## 开发

```bash
pnpm install
pnpm dev
```

默认开发端口 **5183**。Vite 代理见 `devProxy.ts`：

- 固定前缀：`/Admin`、`/Auth`、`/Mfa`、`/Cube`、`/Sso`、`/api`
- **业务 Area 通配**：`/^/[A-Z]…/`（如 `/School/Class/GetPage`）
- 浏览器 HTML 导航 `bypass` 回 SPA；XHR/fetch 转发后端

改代理后需**重启** `pnpm dev`。

单元测试：

```bash
pnpm test
```

嵌入宿主（如 CubeDemo）需启用本皮肤：

```csharp
// app.UseVue(builder.Environment);
app.UseArcoVue(builder.Environment);
```

勿与 `UseVue` / `UseReact` 等同时启用。仓库演示默认仍可为 `UseVue`；本地联调 ArcoVue 时再切换。

## 构建

构建产物输出到 `../wwwroot/`，嵌入 .NET 程序集作为静态资源：

```bash
pnpm build
```

## 壳与 UserProfile

- 布局由 `userProfile.layout.mode`（`side` / `top` / `mix`）经 `layouts/RootLayout.vue` 动态切换。
- 主题 / 密度写入 CSS 变量与 `arco-theme`，持久化到 `GET/PUT /Cube/UserProfile`。
- **侧栏 header 间隙（OSC-0017）**：side 布局 `.layout-header` 显式 `height: 60px` + `border-bottom` + `gap`，面包屑与右上角按钮垂直居中、顶部有合理间隙；top/mix 布局无回归。
- **主题主色治理**：`theme/tokens.ts` 按主色（`--cube-primary`）生成 Arco `--primary-1~10` 色阶与 `--color-primary-light-1~4`（亮色 1 浅→10 深；暗色方向反转、浅色阶为主色半透明），`applyTheme` 写入 `body` 覆盖 Arco 默认，使按钮/链接/选中态等 Arco 组件主色跟随用户主题。VTable / Gantt 为 canvas 渲染不支持 CSS 变量，经 `core/utils/themeColor.ts` 读取 Arco 语义 token（`--color-text-*`/`--color-fill-*`/`--color-border-*`/`--color-bg-*`）并监听 body 主题变化重建表格。
- 外观设置：`/settings/appearance`；顶栏提供主题、密度、设置入口。
- **主题预置色（OSC-0017）**：「外观设置」主色为 13 个 Arco 官方品牌色预置色板（`core/utils/presetColors.ts`，官方中文命名，默认「极客蓝」`#165DFF` 高亮）+ 自定义色区；选预置色即写 `primaryColor` 并持久化。
- **统一图标体系（OSC-0017）**：全项目图标统一为 IconPark（`@icon-park/vue-next`），`main.ts` 注册全局 `<icon-park type>` 聚合组件；图标组件**按需引入**于 `core/utils/iconComponents.ts`（仅打包用到的图标，避免全量 `install` 膨胀 bundle）；业务图标名集中注册于 `core/utils/iconRegistry.ts`（视图类型 / 外观 / 字段类型 `fieldIcon` / 菜单 `menuIcon`（fa-xxx 映射 + 名称关键词 + 默认三态兜底）），不残留 Arco 图标。
- CRUD 页面不读取壳偏好 store（契约隔离）。

## 列表与 ViewProfile

- 默认列表支持多视图：`table` / `tree`（VTable）、`card` / `kanban` / `calendar` / `gantt`（`features/views/*`）。
- Tab 工具条（`ViewTabsToolbar`）切换 / 新建 / 配置；选中 Tab 显示主题浅色底纹 + 底部主题主色滑动指示器（切换视图时平滑滑动过渡）；新建 / 重命名视图使用居中 Arco Modal（跟随主题，替代原生 `prompt`）；映射存 `ViewsJson` 的 `NamedView.mapping`。
- 列布局与命名视图经 `GET/PUT/DELETE /Cube/ViewProfile` 持久化。
- 看板/日历/甘特使用较大 pageSize（约 200–500）；看板不拖拽写回。
- **筛选记忆（OSC-0012）**：搜索条件按命名视图保存到 `ViewProfile.filtersJson`，仅显式点击「保存到此视图」写入；有效条件优先级为 URL 参数 > 已保存筛选 > 空条件，URL 不自动写回；可独立「清除默认筛选」。
- **查询洞察面板（OSC-0012）**：统计标签 / 一张固定图表与列表共用同一搜索条件（命名视图配置抽屉可独立开关统计与图表，无数据 / 无权限 / 失败均非阻塞降级）；`QueryInsightPanel` 已更名 `InsightPanel` 并**暂隐藏不渲染**（搜索改由 `SearchDrawer` 抽屉承载，见 OSC-0016），待简易图表看板设计时再启用。
- **页面级 PageSize（OSC-0012）**：每页条数按 `typePath` 保存到 `ViewProfile.pageSize`（仅接受 20/50/100/200/500/1000），切换实体互不影响；未配置时回落旧全局 `workspace.pageSize` 种子，分页器变更不再写全局偏好。
- **受限表单布局（OSC-0013）**：`ViewProfile.formJson` 按「新增 / 编辑 / 详情」三模式独立保存字段顺序、显隐与 Category 折叠；列表顶栏「表单布局」入口可分别配置并恢复当前模式默认。仅展示偏好——字段权限、必填、校验与提交载荷仍由 GetPage / `prepareSubmitPayload` 权威决定，隐藏字段不能绕过。
- **筛选构建器（OSC-0015）**：工具栏「筛选」打开 Popover 弹层（非 Drawer），条件行竖排增删、AND/OR 切换；**纯前端过滤**——条件不并入后端请求，对已加载数据本地过滤、翻页继续过滤；操作符按字段类别开放：枚举/值集（等于/不等于/为空/不为空）、字符（+包含/不包含）、人员（创建者/更新者等，等于/不等于 + 用户实体下拉）、数字（+大于/大于或等于/小于/小于或等于）、日期时间（晚于/早于）；条件组保存到 `NamedView.filter`（`ViewsJson`）随视图自动应用（应用即持久化，刷新/下次打开保留）；本页全量加载且发生删减时纠正分页 total。筛选按钮在有条件下显示主色底纹 + 右上角当前主题 Primary 色数字徽标（条件数），点击徽标一键清除。
- **多级分组（OSC-0015）**：工具栏「分组」打开 Popover，按 `listFields` 可分组字段有序增删（最多 3 个）并上移/下移，保存到 `NamedView.group`；table 视图分组采用 **VTable 原生 `groupConfig.groupBy`**（官方 list-table-group-checkbox 方案，组标题行显示 `📁 label (count)`，dataSource 翻译），**勾选 checkbox 置于 rowSeriesNumber（每行最前面）**，组标题行 checkbox 与组内子行级联勾选/取消（`titleCheckbox` + `enableCheckboxCascade`），表头全选/取消；分组按钮带底纹与右上角当前主题 Primary 色数字徽标（分组字段数），点击徽标清除分组；树视图不允许分组操作。筛选/分组弹层互斥展开。
- **搜索面板一行折叠（OSC-0015）**：搜索字段容器默认一行，溢出时底部显示「展开更多 N」/「收起」（`offsetHeight > clientHeight` 判定），字段增删后重置折叠态；**已随 OSC-0016 面板重构改为右侧搜索抽屉**（折叠逻辑随 `InsightPanel` 暂隐藏）。
- **LOV LIST 远程搜索（OSC-0015）**：LIST 单选下拉支持输入关键字远程搜索（防抖 300ms，携带 `q` 参数调 `/Admin/Lov/ListData`），空输入回首页；ENUM 与「更多」高级表格入口不变。
- **通用查询与预定义查询（OSC-0016）**：搜索栏为**右侧抽屉**（`SearchDrawer`，标题「高级搜索」，宽 300 = 外观设置抽屉 480 的一半 + 60，**无关闭按钮**、**无底部确定/取消**、点击界面其它区域关闭），**每个查询条件占一行**——关键字 Q 作为第一个条件，其余按 GetPage `Search` 列表顺序依次排列（主时间范围 `dtStart/dtEnd` 单独一行）；**查询组合按钮放抽屉右上角**（文字在前、向下箭头在右），「查询 ▾」下拉集中承载执行查询、重置与预定义查询管理（保存为预定义/应用/重命名/删除/重置查询参数）（「保存到此视图 / 清除默认筛选」菜单项已移除）；日期/数值/时间搜索字段为**单值等值控件**（提交字段名=值，后端 Equal 命中，不再产生 `_min/_max`）；Map 外键字段自动出候选（小表内联 `dataSourceMap` 下拉 / 大表 `Entity.` 值集远程搜索）；预定义查询为**实体级个人配置**存 `ViewProfile.queriesJson`（不随模板），刷新恢复。
- 列表页顶部工具栏（筛选/搜索，table 另有分组）右侧「高级」菜单承载当前实体的导入/导出/批量删除；排序不设工具栏按钮，由列表/树视图标题栏（表头）排序图标承担（受自定义配置「工具栏/排序」开关控制）；表格默认左侧勾选 + 表头全选，批量删除受删除权限、视图允许删除与选中行共同门禁（OSC-0007）。
- **图标化与模板入口移除（OSC-0017）**：视图 Tab 类型文字改为 IconPark 类型图标（tooltip 显示类型名）；工具栏「筛选/分组/搜索/高级」及高级菜单项（导入/导出/批量删除/表单布局）带图标；详情抽屉字段标签前按字段类型显示图标（`fieldIcon`）；导航菜单（side/top/mix）显示统一图标（`menuIcon` fa 映射 / 关键词 / 默认三态兜底）；「高级/管理模板」入口与 `TemplateManageDrawer.vue` 已移除（视图模板发布仍走视图 Tab「存为默认XX视图」）。
- 卡片视图支持**标准 / 偏大 / 整行**三种布局（`NamedView.mapping.layout`），并可配置正文字段列数与横/竖排版；整行布局窄屏自动回退纵向，多行/长文本字段自动占满整行。
- 列表/树/卡片/看板视图中 `Enable` 字段徽标可点击，直接调用后端 `EnableSelect/DisableSelect`（`GET {type}/EnableSelect?keys=`）启停；非 Enable 的状态/枚举/值集徽标悬停光标不变（OSC-0009）。
- 状态/枚举/值集字段在列表/树/卡片/看板渲染为徽标，宽度按文案自适应；卡片视图高度统一为全量对象最高者（`min-height` 下发），操作区固定左下（OSC-0009）。
- 表格行**双击**或操作列 / 卡片左下按钮打开右侧详情抽屉。

## 记录抽屉（表单 / 历史 / 评论）

- 新增/编辑提交时，枚举/Lov 值按字段元数据自动归一化为 `number`（对齐 MVC 版 System.Text.Json 绑定）；可空 String 空值提交 `""`（OSC-0008）；Int64/UInt64 超过安全整数范围时保留字符串避免精度丢失（OSC-0009）。
- 字段分区（`GetPage` list/addForm/editForm/detail/search）由 `resolveFieldsForKind` 统一回退，详情展示与回填同源；分区缺失时按依赖顺序兜底（OSC-0009）。
- 静态 `dataSourceMap`（枚举/状态/布尔）优先本地下拉；LIST 值集按 Meta 的 `valueField` / `labelField` / `tableColumns` / `searchFields` 工作，历史值标签由后端 `BatchLabel` 权威反查（OSC-0009）。
- 详情按字段类型富渲染：字典/多选标签、Boolean、时间、URL 安全链接、图片缩略图、文件下载链接、JSON 摘要；HTML/Markdown 仅以纯文本输出（OSC-0009）。
- 保存失败时后端 `FieldErrors` 优先映射到对应表单字段；无法映射时保留全局提示（OSC-0009）。
- 搜索控件与主表单同源（`resolveSearchControl`/`resolveControl` 一致优先 dataSource）；日期/时间/数值按 itemType 推断单值 `date`/`datetime`/`time`/`number`（提交字段名=值，OSC-0016）；搜索框值集直显首页数据 +「更多」打开高级表格（OSC-0009）。
- 地区级联：`ItemType=area4`/`area`/`cascader` 字段渲染 Arco Cascader，懒加载 `/Cube/Area` 子级并回溯路径回显（OSC-0009）。
- 通用字段校验：手机/电话/邮件/邮箱/网址按字段元数据自动格式校验，空值不触发（OSC-0009）。
- 日期/时间：按 itemType 推断 date/datetime/time 组件，壁钟时间（wall-clock）解析避免 UTC `Z` 串时区漂移（OSC-0009）。
- 历史 Tab：分页（20/页）+ 操作类型筛选（新增/更新/删除）+ 时间/操作人/成功失败徽章/换行（OSC-0008）。
- 评论 Tab：顶层 + 一层回复 + 删除本人评论，消费 `/Cube/EntityComment`（OSC-0008）。

## 目录结构

```
web/
├── src/
│   ├── api/          # API 调用层（复用 @cube/api-core）
│   ├── components/   # 组件（含 TagsView）
│   ├── layouts/      # RootLayout + side/top/mix
│   ├── theme/        # 主题 token / 密度 CSS
│   ├── router/       # 路由配置
│   ├── stores/       # Pinia（含 userProfile、tagsView）
│   ├── views/        # 页面视图（含 settings/appearance）
│   ├── App.vue
│   └── main.ts
├── index.html
├── vite.config.ts
├── tsconfig.json
└── package.json
```

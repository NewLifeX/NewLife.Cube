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
- 外观设置：`/settings/appearance`；顶栏提供主题、密度、设置入口。
- CRUD 页面不读取壳偏好 store（契约隔离）。

## 列表与 ViewProfile

- 默认列表支持多视图：`table` / `tree`（VTable）、`card` / `kanban` / `calendar` / `gantt`（`features/views/*`）。
- Tab 工具条（`ViewTabsToolbar`）切换 / 新建 / 配置；映射存 `ViewsJson` 的 `NamedView.mapping`。
- 列布局与命名视图经 `GET/PUT/DELETE /Cube/ViewProfile` 持久化。
- 看板/日历/甘特使用较大 pageSize（约 200–500）；看板不拖拽写回。
- 列表页顶部工具栏（筛选/搜索，table/tree 另有分组/排序）右侧「高级」菜单承载当前实体的导入/导出/批量删除；表格默认左侧勾选 + 表头全选，批量删除受删除权限、视图允许删除与选中行共同门禁（OSC-0007）。
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
- 搜索控件与主表单同源（`resolveSearchControl`/`resolveControl` 一致优先 dataSource）；日期/时间按 itemType 推断 `dateRange`/`datetimeRange`/`timeRange`；搜索框值集直显首页数据 +「更多」打开高级表格（OSC-0009）。
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

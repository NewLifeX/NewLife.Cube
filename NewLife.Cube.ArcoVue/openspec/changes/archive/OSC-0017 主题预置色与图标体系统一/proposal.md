# OSC-0017 — 主题预置色与图标体系统一

## 1. 为何做

ArcoVue 主题与图标呈现存在 5 处缺口：

1. **主题主色无官方预置**：「外观设置」主色仅有任意颜色选择器，未提供 [arco.design 风格配置平台](https://arco.design/) 主页预置的 13 个官方品牌色（官方中文命名：浪漫红/晚秋红/日暮黄/明黄金/柠檬黄/青柠绿/极光绿/青春绿/碧涛蓝/极客蓝/贵族紫/浪漫紫/法式洋红），用户难以一键选用规范色。
2. **图标使用分散且无规范**：前端散用 Arco 图标组件约 17 个（`@arco-design/web-vue/es/icon`，与 IconPark **同源同命名**），无统一图标注册表与资源规范；后端菜单 `Icon` 为 Font Awesome 类名（`fa-xxx`）已透传到 `MenuItem.icon` 但前端不渲染，导航菜单无图标。
3. **多维视图图标缺失**：视图 Tab 用类型文字（表格/树状/卡片/看板/日历/甘特图）；工具栏「筛选/分组/搜索/高级」为纯文本按钮、高级菜单项无图标；详情抽屉字段标签前无类型图标，视觉识别效率低。
4. **右上角工具栏臃肿**：主题切换为文字按钮（亮色/暗色/跟随系统循环），另有独立「外观设置」文字按钮，与用户下拉入口重复。
5. **「管理模板」冗余入口**：高级菜单「管理模板」打开的 `TemplateManageDrawer` 功能已整合至视图 Tab「存为默认XX视图」等处（视图模板发布）；搜索模板管理不再需要独立入口，属冗余入口需移除。
6. **侧栏布局 header 顶部无间隙**：布局模式为「侧栏」时，Arco `a-layout-header` 无默认高度（仅 `flex:0 0 auto`，高度靠内容撑），header 内折叠按钮+面包屑+`ShellToolbar` 紧贴顶部、视觉顶住界面；且 `side.vue` header 缺少 top/mix 布局的 `border-bottom`，三布局视觉不一致。

本号引入 **IconPark 官方图标资源**（`@icon-park/vue-next`，Vue 3 包）作为统一图标体系；在「外观设置」主题下预置 13 个官方品牌色；统一多维视图/工具栏/详情/右上角/导航菜单的图标呈现；移除冗余「管理模板」入口。

## 2. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | 13 预置色 = Arco 官方 13 个**品牌色**（不含中性灰 gray），使用官方中文命名；默认主题色「极客蓝」`#165DFF` 在色板中高亮。 |
| 2 | 图标体系：安装 **`@icon-park/vue-next`**（Vue 3 包；`@icon-park/vue` 为 Vue 2 版不可用），全局 `install(app, 'icon')` 后统一用 `<icon-park type="...">` 渲染；建立图标名注册表 `iconRegistry.ts` 作为唯一事实源。 |
| 3 | 视图 Tab 类型文字替换为**仅图标**（tooltip 显示类型名）；视图名称文字保留。 |
| 4 | 「高级/管理模板」菜单项移除，`TemplateManageDrawer.vue` 删除；搜索模板发布/清除能力一并移除，不再提供独立入口（视图模板发布仍走视图 Tab「存为默认XX视图」）。 |
| 5 | 右上角：主题按钮改为**图标按钮**（light=sun / dark=moon / system=computer，点击循环切换）；独立「外观设置」按钮删除；**完整外观设置入口保留在用户下拉菜单**（保留「外观设置」doption）。 |
| 6 | 导航菜单图标：后端 `fa-xxx` → IconPark 的**内置映射表**（覆盖 Cube 内置控制器常见 fa 图标）+ 按菜单名关键词兜底 + 默认图标兜底。 |
| 7 | 菜单图标映射**不改后端**：`MenuItem.icon` 继续存 `fa-xxx`，前端 `menuIcon(item)` 负责解析。 |
| 8 | 详情抽屉字段图标：按 `FieldMeta.typeName` / `itemType` 映射（String/数值/Boolean/DateTime/Enum/Map/Image/File/URL/Email/Mobile 等），图标置于字段标签前。 |
| 9 | 需求 6「自定义实体列表/表单设计方案」**另立 OSC-0018**，本号不涉及。 |
| 10 | 仅 ArcoVue 前端；Cube.Vue / NaiveUI 等其他 SPA 不在本号范围。 |
| 11 | 侧栏布局 header 间隙修复：`side.vue` `.layout-header` 显式设置高度（与 top/mix 视觉一致）并补 `border-bottom`/`gap`，使面包屑与右上角按钮垂直居中、顶部有合理间隙；仅改 side 布局，top/mix 不回归。 |

## 3. 做什么

### 主题预置色
- 新增 `web/src/core/utils/presetColors.ts`：`PRESET_THEME_COLORS`（13 项 `{ key, name, color }`，官方中文命名 + hex）。
- `AppearanceDrawer.vue` / `appearance.vue`：主色表单项改为「预置色板」swatch-grid（复用 `ViewConfigDrawer` 推荐色板的交互模式）+ 自定义色区（保留 color-picker / hex input）；选择预置色 → `patchTheme({ primaryColor })`；当前主题色在色板中高亮。

### 统一图标体系
- `web/package.json` 新增依赖 `@icon-park/vue-next`。
- `web/src/main.ts`：`import { install } from '@icon-park/vue-next/es/all'; install(app, 'icon')` 全局注册（`<icon-park type="...">`）。
- 新增 `web/src/core/utils/iconRegistry.ts`（唯一事实源）：
  - `VIEW_KIND_ICONS: Record<ViewKind, string>`（6 视图 → IconPark type）
  - `APPEARANCE_ICONS: Record<Appearance, string>`（light/dark/system → sun/moon/computer）
  - `fieldIcon(field: FieldMeta): string`（字段类型 → IconPark type）
  - `FA_ICON_MAP: Record<string, string>`（fa-xxx → IconPark type）+ `menuIcon(item: MenuItem): string`（映射表命中 → 名称关键词兜底 → 默认图标）
- 现有 Arco 图标（`@arco-design/web-vue/es/icon`）全量替换为 `<icon-park :type>`（约 12 文件 / 17 图标，见 design §2.2 清单）。

### 多维视图图标
- `ViewTabsToolbar.vue`：视图 Tab 类型文字 → `<icon-park :type="VIEW_KIND_ICONS[v.view]" />`（仅图标）+ tooltip（类型名）。
- `DefaultList.vue` 工具栏：「筛选/分组/搜索」按钮加图标；「高级」按钮与菜单项（导入/导出/批量删除/表单布局）加图标。
- 删除「高级/管理模板」菜单项、`templateDrawerVisible` 状态、`TemplateManageDrawer` import 与渲染及关联 handler；删除 `TemplateManageDrawer.vue` 文件。
- `RecordDrawer.vue`：详情字段标签（`detail-field__label`）前按 `fieldIcon(field)` 渲染类型图标。

### 右上角
- `ShellToolbar.vue`：主题文字按钮 → 图标按钮（`APPEARANCE_ICONS` + tooltip 显示当前外观名）；删除独立「外观设置」按钮；用户下拉保留「外观设置」doption（完整外观设置唯一入口）。

### 侧栏布局 header 间隙
- `side.vue`：`.layout-header` 显式设置高度（如 60px，与 top/mix 视觉一致）、补 `border-bottom` 与 `gap`，使面包屑与 `ShellToolbar` 垂直居中、顶部有合理间隙（根因：Arco `a-layout-header` 无默认高度，高度靠内容撑）。

### 导航菜单
- `SidebarMenuNodes.vue`（side/top/mix 三布局共用）：菜单项前渲染 `menuIcon(item)` 图标。

## 4. 不做什么

- 不改后端菜单 `Icon` 存储格式与控制器 `[Menu(Icon = "...")]` 取值（fa-xxx 保留，前端负责映射）。
- 不引入 IconPark SVG 素材入库 / 字体文件（统一用 npm 包组件）。
- 不改变主题持久化契约（`ThemePrefs.primaryColor` 仍为 hex 字符串，双通道持久化不变）。
- 不做需求 6（自定义实体列表/表单设计）——另立 OSC-0018。
- 不改 `ViewConfigDrawer` 的背景推荐色板（与本号主题主色预置无关，保持独立）。
- 不改 Cube.Vue / NaiveUI 等其他前端皮肤。

## 5. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-0005/0006 | Done：6 视图 + 命名视图 Tab（ViewTabsToolbar / VIEW_KIND_LABEL 基线） |
| OSC-0013 | Done：RecordDrawer 详情抽屉 / 字段分组 / FormJson |
| OSC-0016 | Done：工具栏 / 搜索抽屉 / ViewTabsToolbar 现状 |
| `@icon-park/vue-next` | 新增前端依赖（Vue 3 图标包，与 Arco 图标同源同命名） |

## 6. 测试范围

| 类型 | 是否做 | 说明 |
| --- | --- | --- |
| ArcoVue Vitest | 是 | `presetColors` 13 色完整/唯一/含默认；`iconRegistry` 各映射（VIEW_KIND_ICONS 6 视图、fieldIcon 各字段类型、FA_ICON_MAP + 名称兜底 + 默认兜底、APPEARANCE_ICONS）；AppearanceDrawer 选预置色写 primaryColor；ShellToolbar 主题图标切换 |
| 构建 | 是 | `npm.cmd --prefix NewLife.Cube.ArcoVue/web run build`（vue-tsc + vite）通过，`wwwroot` 重新生成 |
| 手工冒烟 | 是 | 外观设置 13 色切换即时生效并持久化；视图 Tab 图标 + tooltip；工具栏/详情字段图标；右上角主题图标循环切换；side/top/mix 导航菜单图标；「管理模板」入口消失 |

## 7. 成功标准

- [ ] 「外观设置」主题下展示 13 个官方品牌色预置色板（官方中文命名），选择即切换主色并持久化；自定义色仍可用。
- [ ] 全项目图标统一为 IconPark（`@icon-park/vue-next` 全局 `<icon-park type>` 渲染），`web/src` 无残留 `@arco-design/web-vue/es/icon` 引用。
- [ ] 视图 Tab 以类型图标替代文字（tooltip 显示类型名）；工具栏筛选/分组/搜索/高级（含菜单项）带图标；详情抽屉字段标签前按字段类型显示图标。
- [ ] 「高级/管理模板」菜单项与 `TemplateManageDrawer.vue` 已删除，`grep TemplateManageDrawer` 为 0 命中。
- [ ] 右上角为主题图标按钮（sun/moon/computer 循环切换），无独立「外观设置」按钮；用户下拉保留「外观设置」入口且可打开抽屉。
- [ ] 布局模式「侧栏」下，header 面包屑与右上角按钮垂直居中、顶部有合理间隙，不顶住界面；top/mix 布局无回归。
- [ ] 侧栏/顶栏/混合导航菜单项显示统一图标（fa 命中 / 关键词兜底 / 默认兜底三态均可见）。
- [ ] `openspec/README.md` 已登记 IconPark 官方图标资源；`web/README.md` 等事实文档最小同步。
- [ ] 本 OSC 新增单测全部通过，相关构建无错误。

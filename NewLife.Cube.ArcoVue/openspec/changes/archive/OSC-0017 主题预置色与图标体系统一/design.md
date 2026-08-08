# OSC-0017 Design — 主题预置色与图标体系统一

## 0. 适用框架与官方资料

| 场景 | 框架 | 官方资料 |
| --- | --- | --- |
| 图标（全局 `<icon-park type>` 组件） | IconPark `@icon-park/vue-next` | [官方图标库](https://iconpark.oceanengine.com/official)（图标名与分类，2658 图标）· [GitHub bytedance/IconPark `packages/vue-next/README.md`](https://github.com/bytedance/IconPark)（Install / 全局 install / Props） |
| 主题预置色 | Arco Design 官方色板 | [调色板 Palette](https://arco.design/palette)（官方中文命名与 hex）；权威色值见源码 `arco-design-vue/packages/web-vue/components/style/color/colors.less` |
| 组件（Drawer/Radio/Tooltip/Dropdown/Doption/ColorPicker/Slider） | Arco Design Vue | https://arco.design/vue/docs/start（实现前必须查阅对应组件官方 API，不得凭印象补造 props/emits） |

**已核实的关键事实**：

- Arco 图标与 IconPark **同源同命名**（`IconSearch` = IconPark `search`），但组件来源不同：前者 `@arco-design/web-vue/es/icon`，后者 `@icon-park/vue-next`。
- Vue 3 包名为 **`@icon-park/vue-next`**（`@icon-park/vue` 是 Vue 2 版）。两种用法：
  1. 按需组件：`import { Home } from '@icon-park/vue-next'` → `<home theme="filled" />`（tree-shakeable）。
  2. **全局组件（本号采用）**：`import { install } from '@icon-park/vue-next/es/all'; install(app, 'icon')` → `<icon-people type="people" />`，`type` 为 kebab-case 图标名；对无效 `type` 抛 `Error: ${type} is not a valid icon type name`。
- 全局 `<icon-park>` 组件 props：`type`（kebab-case）、`theme`（`'outline' | 'filled' | 'two-tone' | 'multi-color'`，默认 `outline`）、`size`（number|string，默认 `'1em'`）、`fill`（string|string[]，默认 `'currentColor'`）、`spin`（boolean）、`strokeWidth`（默认 4）、`strokeLinecap` / `strokeLinejoin`。
- 13 品牌色权威色值（`colors.less`）：见 §3.1。

## 1. 目标与契约边界

在不改后端、不改主题持久化契约的前提下，完成主题预置色与统一图标体系：

- **后端零改动**：`MenuItem.icon` 继续承载 `fa-xxx`，前端 `menuIcon(item)` 解析；不新增接口。
- **图标渲染统一入口**：所有新增/替换图标一律走全局 `<icon-park :type>` 组件，图标名集中注册于 `iconRegistry.ts`，禁止散落硬编码图标组件。
- **主题契约不变**：`ThemePrefs.primaryColor` 仍为 hex 字符串；预置色只是快速选择，最终仍写 `primaryColor`，`tokens.ts`/`applyTheme.ts` 不感知差异。
- **管理模板移除是纯前端减负**：视图模板发布已有「存为默认XX视图」（`ViewTabsToolbar` saveAsDefault → `DefaultList.onSaveAsDefault`）；搜索模板管理随抽屉一并移除，不新增替代入口。

**与既有机制的职责分离**：

| 机制 | 归属 | 本号关系 |
| --- | --- | --- |
| 主题主色/预置色 | `userProfile store` + `presetColors.ts` | 预置色写入 `primaryColor`，复用现有 patchTheme/持久化 |
| 视图 Tab / 命名视图 | `ViewTabsToolbar` / `VIEW_KIND_LABEL` | 仅把类型文字换成图标（tooltip 仍显示文字） |
| 工具栏 / 高级菜单 | `DefaultList.vue` | 加图标 + 删「管理模板」 |
| 详情字段展示 | `RecordDrawer.vue` / `fieldGroups.ts` | 字段标签前加类型图标 |
| 导航菜单图标 | 后端 `fa-xxx` → 前端 `menuIcon` | 纯前端映射 |
| 图标资源规范 | `openspec/README.md` | 登记 IconPark 为官方图标资源 |

## 2. 文件级改动地图

### 2.1 主题预置色

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `web/src/core/utils/presetColors.ts`（新增） | `interface PresetThemeColor { key; name; color }` + `export const PRESET_THEME_COLORS`（13 项，官方中文命名 + hex，§3.1） | — |
| `web/src/views/settings/AppearanceDrawer.vue` | 「主色」表单项改为：预置色板 swatch-grid（13 色，当前 `form.theme.primaryColor` 高亮 + check 标记）在上 + 自定义色区（`a-color-picker` hide-trigger 或保留 hex input）在下；点击预置色 → `onThemeChange()` | 布局/密度/圆角/字号表单项、保存/恢复默认、footer 逻辑 |
| `web/src/views/settings/appearance.vue` | 同上（页面形态） | 同上 |

### 2.2 统一图标体系

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `web/package.json` | `dependencies` 新增 `"@icon-park/vue-next": "^1.4.2"`（npm latest） | 其余依赖 |
| `web/src/main.ts` | 新增 `import { install } from '@icon-park/vue-next/es/all';` + `install(app, 'icon')`；**执行期降级**：全量 install 使主包膨胀约 387KB gzip（实测主包 682→324KB gzip），改为**按需引入**——`main.ts` 自定义 `<icon-park>` 动态组件（`defineComponent` + 按 `type` 查 `ICON_COMPONENTS` 渲染），不再全量 install | App 创建、路由、store 注册 |
| `web/src/core/utils/iconComponents.ts`（新增） | 按需具名 `import { Table, Tree, ... } from '@icon-park/vue-next'`，登记 `ICON_COMPONENTS: Record<string, Component>`（kebab-case → 组件）+ `FALLBACK_ICON`；作为图标组件唯一引入点（design §10 降级落地） | — |
| `web/src/core/utils/iconRegistry.ts`（新增） | §3.2/§3.3 全部映射与纯函数（仅图标名字符串，不 import 组件） | — |
| 现有 Arco 图标文件（替换清单见下） | `@arco-design/web-vue/es/icon` 引入与模板标签替换为 `<icon-park :type>` | 各文件业务逻辑 |

**现有 Arco 图标替换清单**（T4 用 `grep -r "@arco-design/web-vue/es/icon"` 复核，以命中为准）：

| 文件 | 现状 Arco 图标 | 替换 IconPark type（提案） |
| --- | --- | --- |
| `features/search/QueryComboButton.vue` | IconSearch/IconDown/IconRefresh/IconUp/IconCheck/IconDelete/IconSave/IconEdit | search/down/refresh/up/check/delete/save/edit |
| `layouts/mix.vue` / `layouts/side.vue` | IconMenuFold / IconMenuUnfold | menu-fold / menu-unfold |
| `views/crud/DefaultList.vue` | IconDown | down |
| `views/crud/FilterBuilderPopover.vue` | IconClose | close |
| `views/crud/FormContent.vue` | IconDown | down |
| `views/crud/FormLayoutDrawer.vue` | IconDragDotVertical / IconEye / IconEyeInvisible / IconInfoCircle | drag / preview-open / preview-close / info |
| `views/crud/GroupPopover.vue` | IconUp / IconDown / IconClose | up / down / close |
| `views/crud/NamedViewsToolbar.vue` | IconCheck / IconDown | check / down |
| `views/crud/RecordDrawer.vue` | IconUp / IconDown | up / down |
| `views/crud/ViewConfigDrawer.vue` | IconCheck / IconDown / IconDragDotVertical / IconEye / IconEyeInvisible / IconInfoCircle / IconPushpin | check / down / drag / preview-open / preview-close / info / pin |
| `views/crud/ViewTabsToolbar.vue` | IconMoreVertical | more |
| `views/settings/*`（如有） | — | — |

> 图标名以 IconPark 官方站点与 `IconType` 联合类型为准；上表为**提案**，实现时逐项用 `type in IconMap` 校验，无效名按 §4.3 回退。

### 2.3 多维视图图标 + 管理模板删除

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `web/src/views/crud/ViewTabsToolbar.vue` | `view-tab-kind` 的 `kindLabel(v.view)` 文字替换为 `<icon-park :type="VIEW_KIND_ICONS[v.view]" />`，外层 `a-tooltip :content="VIEW_KIND_LABEL[v.view]"` | 视图名称、菜单（重命名/配置/复制/删除/存为默认/恢复默认）、`view-tab-indicator` 滑动指示器 |
| `web/src/views/crud/DefaultList.vue` | ①「筛选」按钮文字前加 `<icon-park type="filter">`；②「分组」按钮文字前加 `<icon-park type="group">`（或 sort）；③「搜索」按钮文字前加 `<icon-park type="search">`；④「高级」按钮文字前加 `<icon-park type="more">`；⑤高级菜单项加图标：导入 `import` / 导出 `export` / 批量删除 `delete` / 表单布局 `layout`；⑥**删除**「管理模板」`a-doption`、`templateDrawerVisible`、`TemplateManageDrawer` import 与渲染、`onSaveAsDefault` 以外的模板相关 handler | 添加记录、徽标计数、筛选/分组弹层逻辑、导出格式子菜单、批量删除门禁 |
| `web/src/views/crud/TemplateManageDrawer.vue` | **删除文件**（视图/搜索模板管理入口整体移除） | — |
| `web/src/views/crud/RecordDrawer.vue` | 详情字段 `<div class="detail-field__label">` 内、显示名前渲染 `<icon-park :type="fieldIcon(field)" class="detail-field__icon" />` | 字段值渲染（图片/链接/文件/JSON）、分组折叠、`detailLabelStyle` |

### 2.4 右上角

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `web/src/layouts/ShellToolbar.vue` | ①主题文字按钮改为图标按钮：`<a-tooltip :content="appearanceLabel"><a-button type="text" size="small" @click="cycleAppearance"><icon-park :type="APPEARANCE_ICONS[theme.appearance]" /></a-button></a-tooltip>`；②删除独立「外观设置」`a-button`；③用户下拉保留「外观设置」doption 与「退出登录」 | 用户头像/名称、`cycleAppearance` 循环逻辑、`goAppearance` |

### 2.5 导航菜单

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `web/src/layouts/SidebarMenuNodes.vue` | ①`a-sub-menu` 标题前渲染 `menuIcon(item)`（递归子项同样）；②`a-menu-item` 内容前渲染 `menuIcon(item)` | `visible(item)`、`normalize(url)`、递归结构 |

> side/top/mix 三布局均通过 `SidebarMenuNodes` 渲染菜单，一处改动三布局生效。

### 2.6 侧栏布局 header 顶部间隙（执行期用户补充）

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `web/src/layouts/side.vue` | `.layout-header`：显式 `height`（如 60px，与 top/mix 布局视觉一致）+ 补 `border-bottom: 1px solid var(--color-border)` 与 `gap: 16px`，使折叠按钮+面包屑（`.layout-header__left`）与 `ShellToolbar` 垂直居中、顶部有合理间隙 | 布局结构、折叠逻辑、sider 样式 |

**根因**：Arco `a-layout-header` 默认仅 `flex: 0 0 auto; box-sizing: border-box; margin: 0`（**无 height/padding，高度完全由内容撑开**，已核实 `@arco-design/web-vue/es/layout/style/index.css`）。side 布局 header 内容为矮型控件（text 按钮+面包屑+头像），撑出高度后内容紧贴顶部；而 top/mix 布局 header 有品牌文字+水平菜单撑高且带 `border-bottom`，视觉正常。修复以显式高度统一三布局 header 视觉。

## 3. 图标资源规范与色板

### 3.1 13 官方品牌色（`presetColors.ts` 内容）

色值来自 `arco-design-vue` `colors.less` 权威定义（`-6` 即主色）：

| key | name（官方中文） | color |
| --- | --- | --- |
| red | 浪漫红 | `#F53F3F` |
| orangered | 晚秋红 | `#F77234` |
| orange | 日暮黄 | `#FF7D00` |
| gold | 明黄金 | `#F7BA1E` |
| yellow | 柠檬黄 | `#FADC19` |
| lime | 青柠绿 | `#9FDB1D` |
| green | 极光绿 | `#00B42A` |
| cyan | 青春绿 | `#14C9C9` |
| blue | 碧涛蓝 | `#3491FA` |
| arcoblue | 极客蓝 | `#165DFF`（**默认主题色**） |
| purple | 贵族紫 | `#722ED1` |
| pinkpurple | 浪漫紫 | `#D91AD9` |
| magenta | 法式洋红 | `#F5319D` |

> 中性灰 `gray #86909C` 不作为品牌色预置（用户已确认）；自定义色区可自由选任意色。

### 3.2 图标资源规范（列入 openspec）

- **资源**：IconPark 官方图标库（`https://iconpark.oceanengine.com/official`），npm 包 `@icon-park/vue-next`（Vue 3）。
- **渲染**：全局 `<icon-park :type="kebab-case名" />`；`size` 默认 `1em`（跟随字号），`fill` 默认 `currentColor`（跟随文字色），需要时用 `theme`/`spin`。
- **注册表**：所有业务图标名集中注册于 `web/src/core/utils/iconRegistry.ts`；新图标必须先经 IconPark 站点确认存在再注册。
- **openspec 登记**：`openspec/README.md`「前端框架与官方文档」表新增「图标」行（见 §9）。

### 3.3 图标映射表（`iconRegistry.ts`）

```ts
// 视图类型 → 图标（6 视图）
export const VIEW_KIND_ICONS: Record<ViewKind, string> = {
  table: 'table', tree: 'tree', card: 'pic', kanban: 'board', calendar: 'calendar', gantt: 'timeline',
};

// 外观 → 图标（右上角主题按钮）
export const APPEARANCE_ICONS: Record<Appearance, string> = {
  light: 'sun', dark: 'moon', system: 'computer',
};

// fa-xxx → IconPark（覆盖 Cube 内置控制器常见菜单图标）
export const FA_ICON_MAP: Record<string, string> = {
  'fa-user': 'people', 'fa-users': 'people-two', 'fa-user-plus': 'add-user',
  'fa-user-circle': 'user', 'fa-user-secret': 'incognito-mode',
  'fa-table': 'table', 'fa-list': 'list', 'fa-navicon': 'menu',
  'fa-wrench': 'tools', 'fa-cog': 'setting', 'fa-gear': 'setting',
  'fa-database': 'database', 'fa-history': 'history', 'fa-clock-o': 'clock',
  'fa-tasks': 'checklist', 'fa-star': 'star', 'fa-home': 'home',
  'fa-file': 'file', 'fa-file-text': 'file-text', 'fa-search': 'search',
  'fa-desktop': 'computer', 'fa-tachometer': 'dashboard', 'fa-area-chart': 'chart-line',
  'fa-shopping-cart': 'shopping-bag', 'fa-bomb': 'bomb',
  // 其它内置菜单（无 fa- 前缀的也纳入）
  list: 'list', grid: 'grid',
};

// 名称关键词兜底（menuIcon 未命中 FA_ICON_MAP 时）
const MENU_KEYWORD_FALLBACK: Array<[RegExp, string]> = [
  [/用户|成员|账户|账号|个人/, 'people'],
  [/角色|权限|授权/, 'permissions'],
  [/菜单|导航/, 'menu'],
  [/日志|审计|历史/, 'history'],
  [/设置|配置|参数|系统/, 'setting'],
  [/数据|数据库|模型|表/, 'database'],
  [/文件|附件|上传/, 'file'],
  [/统计|报表|图表|分析/, 'chart-line'],
  [/任务|计划|调度|定时/, 'timer'],
  [/流程|审批|工作流/, 'send'],
  [/消息|通知|提醒/, 'message'],
  [/订单|交易|支付/, 'shopping-bag'],
  [/部门|组织|机构/, 'building'],
  [/客户|联系人/, 'user'],
  [/商品|产品|物料/, 'box'],
  [/仓库|库存/, 'inbox'],
];

export const DEFAULT_MENU_ICON = 'app';
export function menuIcon(item: { icon?: string; displayName?: string; name: string }): string;
```

> `FA_ICON_MAP` / `MENU_KEYWORD_FALLBACK` 具体图标名以实现时 IconPark `IconType` 校验为准（§4.3 回退），此处为提案基线。

## 4. 条件矩阵

### 4.1 字段类型 → 图标（`fieldIcon(field)`）

判定优先级：`itemType` 特殊字段 > `typeName` 常规类型 > 默认。

| 条件 | 结果图标（提案） | 说明 |
| --- | --- | --- |
| `itemType == 'image'` | `pic` | 图片字段 |
| `itemType == 'file'` | `file` | 附件字段 |
| `itemType == 'url'` | `link` | 链接字段 |
| `itemType == 'mail'` / `'email'` | `email` | 邮箱字段 |
| `itemType == 'mobile'` / `'phone'` | `phone` | 手机字段 |
| `typeName == 'Boolean'` | `switch` | 开关 |
| `typeName` ∈ DateTime/Date/TimeSpan | `time` | 日期/时间 |
| `typeName` ∈ Int16/Int32/Int64/Double/Decimal/Single | `number` | 数值 |
| `typeName == 'Enum'` | `tag` | 枚举 |
| `field.mapField` 非空 | `link` | Map 外键 |
| `primaryKey` | `key` | 主键 |
| `typeName == 'Guid'` | `key` | 唯一标识 |
| 其它 String / 默认 | `font-size` | 文本/兜底 |

### 4.2 菜单图标解析矩阵（`menuIcon(item)`）

| `item.icon` | 处理 | 结果 |
| --- | --- | --- |
| 命中 `FA_ICON_MAP` | 直接取映射 | 映射图标 |
| 未命中（含无 `fa-` 前缀） | 按 `displayName || name` 匹配 `MENU_KEYWORD_FALLBACK` | 关键词图标 |
| 关键词也未命中 | 返回 `DEFAULT_MENU_ICON` | 默认图标（app） |

### 4.3 无效图标名回退（实现期校验）

- 全局 `<icon-park>` 对无效 `type` 抛异常 → `iconRegistry` 中每个注册图标名必须经 `IconType` 校验（单测断言 + 冒烟逐项渲染）。
- 命名冲突/不存在时：业务图标优先换 IconPark 语义相近图标；`menuIcon`/`fieldIcon` 的默认分支保证不因映射缺失而抛异常（`fieldIcon` 兜底 `font-size`，`menuIcon` 兜底 `app`）。

## 5. UI 规格

### 5.1 外观设置主色区

```
┌─ 主色 ─────────────────────────────┐
│  ┌────┐ ┌────┐ ┌────┐ …（13 色 swatch-grid，圆形/圆角方块）│
│  │浪漫红│ │晚秋红│ │日暮黄│          │
│  └────┘ └────┘ └────┘              │
│  （选中：主题主色描边 + check 角标；当前主题色高亮）│
│  自定义  [ a-color-picker ]  #165DFF│
└────────────────────────────────────┘
```

- swatch：28px 圆角方块，`title` 显示官方中文名；hover 描边主题色；选中 `icon-check` 白色角标。
- 点击预置色 → `form.theme.primaryColor = c.color` + `onThemeChange()`（与现有 input 行为一致，走 `patchTheme` + debounce 持久化）。

### 5.2 视图 Tab

```
[icon] 视图名   ← 仅类型图标 + 视图名称；tooltip 显示「表格/树状/卡片/看板/日历/甘特图」
```

- 图标 `size=14px` 与视图名字号匹配；图标颜色跟随 Tab 文字色（`fill=currentColor`）。
- `view-tab-indicator` 滑动指示器逻辑不变（基于 Tab 宽度测量，图标替换不影响）。

### 5.3 右上角主题图标按钮

```
[主题图标按钮] [用户头像 ▾]
  ↑ tooltip「亮色 / 暗色 / 跟随系统」（当前外观名）
```

- `type` 按 `APPEARANCE_ICONS[appearance]`：亮色 sun / 暗色 moon / 跟随系统 computer。
- 点击循环 `light → dark → system`（沿用 `cycleAppearance`）。

### 5.4 导航菜单项

```
[icon] 菜单名     ← a-sub-menu 标题 / a-menu-item 内容统一前缀图标
```

- 图标 `size=14px`，`fill=currentColor`；子菜单标题图标与叶子菜单图标一致规格。

## 6. 状态与唯一来源

| 状态 | 唯一来源 | 说明 |
| --- | --- | --- |
| 主题主色 | `userProfile store.theme.primaryColor` | 预置色只是写入该 hex，持久化链路不变 |
| 预置色数据 | `presetColors.ts` `PRESET_THEME_COLORS` | 只读常量 |
| 视图类型图标 | `iconRegistry.ts` `VIEW_KIND_ICONS` | 只读常量 |
| 字段类型图标 | `iconRegistry.ts` `fieldIcon(field)` | 纯函数 |
| 菜单图标 | `iconRegistry.ts` `menuIcon(item)` | 纯函数 |
| 图标资源 | `@icon-park/vue-next` 包 + `iconRegistry.ts` 注册表 | openspec 登记 |

## 7. 删除清单

- `DefaultList.vue`：`a-doption`「管理模板」、`templateDrawerVisible` ref、`TemplateManageDrawer` import 与模板渲染、`onTemplateChanged` 等关联 handler（若有独立函数一并删除）。
- `TemplateManageDrawer.vue`：**整文件删除**（含视图/搜索模板发布/清除/删除整个模板全部能力）。
- 收尾校验：`grep -r "TemplateManageDrawer" web/src packages/**` 为 0 命中；`grep -r "@arco-design/web-vue/es/icon" web/src` 为 0 命中。

## 8. 测试设计

### 8.1 前端 Vitest（新增/更新 spec）

- `presetColors.spec.ts`：`PRESET_THEME_COLORS.length === 13`；key 唯一；色值非空且为 hex；含 `arcoblue #165DFF`；官方名非空。
- `iconRegistry.spec.ts`：
  - `VIEW_KIND_ICONS` 覆盖 6 个 ViewKind 且值非空。
  - `APPEARANCE_ICONS` 覆盖 light/dark/system。
  - `fieldIcon`：itemType（image/file/url/mail/mobile）、typeName（Boolean/DateTime/Int32/Double/Enum/Guid/String/默认）各分支正确；Map（mapField 非空）→ link；primaryKey → key。
  - `menuIcon`：FA_ICON_MAP 命中（如 `fa-user`）、关键词兜底（如「日志」→ history）、默认兜底（未知名）三态；`icon` 为空走关键词/默认。
  - 所有注册图标名在 IconPark `IconType` 联合类型中有效（若可在单测中静态校验则断言，否则冒烟兜底）。
- `AppearanceDrawer` 相关（或抽纯函数）：选预置色 → `patchTheme` 的 `primaryColor` 正确。
- `ShellToolbar` 相关（如有现成 spec）：主题图标随 appearance 变化；点击循环切换。

### 8.2 构建

- `npm.cmd --prefix NewLife.Cube.ArcoVue/web run test`（全量 Vitest 通过）。
- `npm.cmd --prefix NewLife.Cube.ArcoVue/web run build`（vue-tsc + vite 通过，`wwwroot` 重新生成）。

### 8.3 手工冒烟（verify.md）

- 外观设置：13 色板渲染、选色即时生效、刷新持久化、自定义色仍可用。
- 视图 Tab：6 视图图标正确 + tooltip；切换视图图标随类型变化。
- 工具栏/高级菜单：筛选/分组/搜索/高级及菜单项图标可见；「管理模板」消失。
- 详情抽屉：抽查 String/数值/DateTime/Boolean/Map/Image 字段标签前图标正确。
- 右上角：主题图标随外观变化、循环切换正常；无独立「外观设置」按钮；用户下拉「外观设置」打开抽屉。
- 导航：side/top/mix 菜单图标（含管理员菜单 fa 命中、业务菜单关键词/默认兜底）。
- 布局：布局模式「侧栏」下面包屑与右上角按钮垂直居中、顶部有合理间隙不顶住界面；「顶栏/混合」布局 header 无回归。

## 9. 核心文档影响

| 文档 | 影响 |
| --- | --- |
| `NewLife.Cube.ArcoVue/openspec/README.md` | 「前端框架与官方文档」表新增一行：`图标 | IconPark 官方图标库（@icon-park/vue-next） | https://iconpark.oceanengine.com/official · GitHub bytedance/IconPark（vue-next README）`；并在表下注明「图标名以 IconPark `IconType` 为准，统一注册于 `iconRegistry.ts`」 |
| `NewLife.Cube.ArcoVue/web/README.md` | 登记 OSC-0017 能力（主题 13 预置色 / 统一 IconPark 图标体系 / 管理模板入口移除） |
| `Doc/功能清单.md` | SPA 主题/图标相关条目增补 OSC-0017 状态 |

## 10. 风险

| 风险 | 缓解 |
| --- | --- |
| IconPark 图标名拼写/版本差异导致 `type` 无效 | 全局组件无效 type 抛异常可快速暴露；`iconRegistry` 集中注册 + 单测断言 + 冒烟逐项渲染 |
| 全局 `install(app, 'icon')` 全量图标进入 bundle，体积增大 | 若 `vite build` 产物明显膨胀（对比基线），降级为按需 `import { Xxx } from '@icon-park/vue-next'` 局部引入（仅在 build 超预期时执行，默认全局方案） |
| 替换 Arco 图标造成交互回归 | 逐文件替换 + 冒烟覆盖菜单/下拉/抽屉/弹层关键交互；替换前后 Vitest 全量对比 |
| 菜单图标映射遗漏（新业务 fa 图标未覆盖） | 关键词兜底 + 默认图标保证不空；映射表可增量扩充 |
| 预置色与自定义色视觉混淆 | 预置色板 + 自定义区分区展示，选中态明确（check + 主题色描边） |
| `ViewTabsToolbar` 指示器宽度测量受图标影响 | 图标与文字同尺寸、不换行；冒烟验证指示器滑动正常 |
| 侧栏 header 高度显式化与 top/mix 靠内容撑高视觉不一致 | 高度取值与 top/mix 实际渲染高度在浏览器核对（量取）；三布局 header 视觉高度差异在冒烟中确认可接受 |

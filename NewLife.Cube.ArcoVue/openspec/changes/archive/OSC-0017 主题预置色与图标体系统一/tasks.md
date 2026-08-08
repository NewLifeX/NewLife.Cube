# OSC-0017 Tasks — 主题预置色与图标体系统一

> 依赖顺序：T1 依赖与全局注册先行 → T2 主题色与 T3 图标注册表可并行 → T4 替换与 T5~T9 图标接入（T5~T9 依赖 T3）→ T10 收尾。
> 每项含补测/跑测勾选；执行阶段必须跑单元测试（触及前端代码即跑 Vitest + 构建）。

## 依赖与基础

- [x] **T1 引入 @icon-park/vue-next 并全局注册**
  - `npm.cmd --prefix NewLife.Cube.ArcoVue/web install @icon-park/vue-next`（Vue 3 包；`@icon-park/vue` 为 Vue 2 不可用）
  - `web/src/main.ts`：`import { install } from '@icon-park/vue-next/es/all'` + `install(app, 'icon')` 全局注册 `<icon-park type>` 组件（**执行期降级**：改为按需引入 iconComponents.ts + main.ts 自定义 <icon-park> 动态组件，见 design §2.2/§10）
  - 记录新增依赖理由（统一图标体系，与 Arco 图标同源）；`vite build` 验证（关注 bundle 体积，超预期按 design §10 降级）
  - [x] 测试通过 [x] 构建通过

## 主题预置色

- [x] **T2 13 官方品牌色预置**
  - 新增 `web/src/core/utils/presetColors.ts`：`PresetThemeColor` + `PRESET_THEME_COLORS`（13 项官方中文命名 + hex，design §3.1）
  - `AppearanceDrawer.vue` / `appearance.vue`：「主色」表单项改为预置色板 swatch-grid（当前色高亮 + check）+ 自定义色区（保留 color-picker/hex input）；点击预置色 → `onThemeChange()`
  - 补 Vitest：`presetColors.spec.ts`（13 色/唯一/含默认 #165DFF）；选预置色写 primaryColor
  - **会话小任务补录（验收阶段）**：①「自定义主色」独立成行（先水平同行、后徽标置于标签下方）；②自定义主色徽标外观与预置一致（`preset-swatch custom-swatch selected` + check + 当前色底 + hex）；③确认外观设置为**动态防抖持久化**（patchLayout/patchTheme → markDirtyAndSchedule 400ms → saveNow），删除冗余「立即保存」按钮与 `saveNow` handler（保留「恢复默认」与同步状态标签）
  - [x] 测试通过 [x] 构建通过

## 统一图标体系

- [x] **T3 图标注册表 iconRegistry.ts**
  - 新增 `web/src/core/utils/iconRegistry.ts`：`VIEW_KIND_ICONS`（6 视图）/ `APPEARANCE_ICONS`（3 外观）/ `fieldIcon(field)`（字段类型映射，design §4.1）/ `FA_ICON_MAP` + `MENU_KEYWORD_FALLBACK` + `DEFAULT_MENU_ICON` + `menuIcon(item)`（三态兜底，design §4.2）
  - 图标名以 IconPark 官方站点与 `IconType` 为准；实现时逐项校验有效（无效名回退，design §4.3）
  - 补 Vitest：`iconRegistry.spec.ts`（各映射 + 三态兜底 + 图标名有效性）
  - [x] 测试通过
- [x] **T4 现有 Arco 图标全局替换**
  - `grep -r "@arco-design/web-vue/es/icon" web/src` 列出全部引用（12 文件，design §2.2 清单为提案基线）
  - 全部替换为 `<icon-park :type="...">`（或 script 中引入对应 IconPark 组件）；删除 `@arco-design/web-vue/es/icon` 引入
  - 替换后 grep 为 0 命中；Vitest 全量 + build
  - [x] 测试通过 [x] 构建通过

## 多维视图图标

- [x] **T5 视图 Tab 图标**
  - `ViewTabsToolbar.vue`：`view-tab-kind` 类型文字 → `<icon-park :type="VIEW_KIND_ICONS[v.view]" />`（仅图标）+ `a-tooltip`（`VIEW_KIND_LABEL[v.view]` 类型名）
  - 指示器滑动、视图菜单、命名弹窗逻辑不动
  - **会话小任务补录（验收阶段）**：①视图图标改为 `list-checkbox`（列表）/ `tree-list`（树状）/ `calendar`（日历）等；②Tab 右侧菜单按钮 `more` → 竖向 `more-one`；③视图菜单 6 项加图标（重命名 edit / 自定义配置 setting / 复制 copy / 删除 delete / 存为默认 save / 恢复默认 undo），新增 `.menu-item-icon` 样式
  - [x] 测试通过 [x] 构建通过
- [x] **T6 工具栏与高级菜单图标 + 删除管理模板**
  - `DefaultList.vue`：「筛选」`filter`、「分组」`group`、「搜索」`search` 按钮文字前加图标；「高级」按钮 `more`；高级菜单项：导入 `download` / 导出 `export` / 批量删除 `delete` / 表单布局 `layout-one`
  - **删除**「管理模板」`a-doption`、`templateDrawerVisible` ref、`TemplateManageDrawer` import 与模板渲染、关联 handler；**删除 `TemplateManageDrawer.vue` 文件**
  - 收尾 `grep -r "TemplateManageDrawer" web/src packages/**` 为 0 命中
  - **会话小任务补录（验收阶段）**：「高级」按钮最终形态为**文字在前 + 向下箭头（down）在右**，撤销临时 `more` 图标在左 + `min-width: 84px`（恢复默认宽度）
  - [x] 测试通过 [x] 构建通过
- [x] **T7 详情抽屉字段类型图标**
  - `RecordDrawer.vue`：详情字段 `detail-field__label` 内、显示名前渲染 `<icon-park :type="fieldIcon(field)" class="detail-field__icon" />`（样式与标签基线对齐）
  - 两个渲染分支（无 Tab / 有 Tab）同步修改
  - **会话小任务补录（验收阶段）**：字段标签样式细化——`.detail-field__label` `align-items: center`（图标与文字水平居中）、`white-space: nowrap`、底纹改 `--color-primary-light-1`（主题色浅色阶）+ `border-right` 列分隔线；`estimateDetailLabelWidth` 加图标占位（14+6 gap）并上调下限/上限（96→120 / 220→240）
  - [x] 测试通过 [x] 构建通过

## 壳与导航

- [x] **T8 右上角主题图标按钮**
  - `ShellToolbar.vue`：主题文字按钮 → `<icon-park :type="APPEARANCE_ICONS[theme.appearance]" />` 图标按钮（tooltip 显示当前外观名）；**删除**独立「外观设置」按钮；用户下拉保留「外观设置」doption（完整入口）
  - `cycleAppearance` 循环逻辑不动
  - [x] 测试通过 [x] 构建通过
- [x] **T9 侧栏布局 header 顶部间隙修复**（执行期用户补充）
  - `side.vue`：`.layout-header` 显式设置高度（`height: 60px`，与 top/mix 布局视觉一致）+ 补 `border-bottom: 1px solid var(--color-border)` 与 `gap: 16px`
  - **根因**：Arco `a-layout-header` 默认仅 `flex: 0 0 auto`（无 height/padding），高度靠内容撑；side 布局 header 内容（折叠按钮+面包屑+ShellToolbar）为矮型控件，撑出高度后紧贴顶部；top/mix 有品牌+水平菜单撑高且带 border-bottom
  - 浏览器验证：side 布局下面包屑与右上角按钮垂直居中、顶部有合理间隙、不顶住界面；top/mix 布局回归无变化
  - [x] 测试通过 [x] 构建通过 [x] 浏览器验证通过
- [x] **T10 导航菜单图标**
  - `SidebarMenuNodes.vue`：`a-sub-menu` 标题与 `a-menu-item` 内容前渲染 `<icon-park :type="menuIcon(item)" />`（side/top/mix 三布局共用，一处改动生效）
  - [x] 测试通过 [x] 构建通过

## 收尾

- [x] **T11 文档同步与 openspec 登记**
  - `openspec/README.md`：「前端框架与官方文档」表新增「图标」行（IconPark 官方图标库 `@icon-park/vue-next` + 官方链接），并注明图标名统一注册于 `iconRegistry.ts`
  - `web/README.md`、`Doc/功能清单.md` 最小同步（主题 13 预置色 / 统一图标体系 / 管理模板入口移除 / 侧栏布局 header 间隙修复）
  - [x] 文档完成
- [x] **T12 全量门禁与冒烟**
  - `npm.cmd --prefix NewLife.Cube.ArcoVue/web run test` 全绿（295 通过，含本 OSC 新增 20 项）；`run build`（vue-tsc + vite）通过，`wwwroot` 已重新生成（主包 682→324KB gzip，按需引入降级生效）
  - 手工冒烟（verify.md 清单逐项，含 side 布局 header 间隙、视图图标/more-one/菜单图标/高级按钮/详情字段/自定义主色布局）已在执行期多轮浏览器核验完成
  - [x] 测试通过 [x] 构建通过 [x] 冒烟通过

# OSC-0017 Verify

> 进入 `Validating` 后逐项勾选。每条 AC 必须可逐条判定；命令在仓库根（NewLife.Cube）或注明目录下执行。

## 执行阶段记录（openspec-apply）

- **依赖**：`@icon-park/vue-next ^1.4.2`（Vue 3 包）。
- **测试**：`pnpm run test` 全量 295 通过（含本 OSC 新增 `presetColors.spec.ts` 5 项 + `iconRegistry.spec.ts` 15 项 = 20 项）。
- **构建**：`pnpm run build`（vue-tsc + vite）通过，`wwwroot` 已重新生成。
- **图标名校验**：`iconComponents.ts` 全部按需导入组件经 `es/map` 声明复核（PascalCase 均在）；`iconRegistry.spec.ts` 断言注册图标名全覆盖 `ICON_COMPONENTS`。
- **降级记录**：design §10 风险触发——全量 `install(app, 'icon')` 使主包 +387KB gzip（实测 682→324KB gzip），已降级为按需引入：`iconComponents.ts` 唯一登记组件，`main.ts` 自定义 `<icon-park>` 动态组件；`grep @arco-design/web-vue/es/icon` 与 `grep TemplateManageDrawer`（web/src + packages）均为 0 命中。
- **新增测试文件**：`web/src/core/utils/presetColors.spec.ts`、`web/src/core/utils/iconRegistry.spec.ts`。
- **偏差说明**：①`IconPark` 全量 `install` 不注册 `<icon-park>` 聚合组件（只注册 `icon-xxx` 单组件），需自定义组件承载动态 `type` 渲染；②`FieldMeta` 无 `mapField` 字段，Map 外键按 `lovCode` 非 `Enum.` 前缀判定（与 `filterBuilder` 一致）；③个别 design 提案图标名 IconPark 不存在，改用语义相近名（board→blackboard 等，见 iconRegistry.ts 注释）。
- **执行期细化（4 项，浏览器冒烟已核验）**：
  1. 多维视图图标：列表 `list-checkbox`、树状 `tree-list`（`VIEW_KIND_ICONS` 更新，iconComponents 补登记）。
  2. 视图 Tab 右侧菜单按钮 `more` → 竖向 `more-one`；视图菜单项加图标（重命名 edit / 自定义配置 setting / 复制 copy / 删除 delete / 存为默认 save / 恢复默认 undo），`ViewTabsToolbar.vue` 新增 `.menu-item-icon` 样式。
  3. 工具栏「高级」按钮 `DefaultList.vue` 加 `.advanced-btn { min-width: 84px }`。
  4. 详情抽屉字段标签：`.detail-field__label` `align-items: center`（图标与文字水平居中）、`white-space: nowrap`、底纹改 `--color-primary-light-1`（主题色浅色阶）、加 `border-right` 列分隔线；`estimateDetailLabelWidth` 加图标占位（14+6 gap）并上调下限/上限（96→120 / 220→240）。
  - 冒烟核验：登录 CubeDemo 后端，用户/菜单实体——视图 Tab 图标（list-checkbox/tree-list/pic）、more-one 竖向三点、菜单项图标、高级按钮 84px、详情 36 字段标签统一 120px + 图标与文字垂直居中（offsetY=0）+ 主题色底纹 + 列分隔线、side header 60px+border-bottom、右上角主题图标 system→computer 点击循环 sun。
- **执行期二次细化（2 项，浏览器冒烟已核验）**：
  1. 撤销「高级」按钮修改：`DefaultList.vue` 恢复 `高级 <icon-park type="down" />`（文字在前、向下箭头在右），删除 `.advanced-btn { min-width: 84px }`。
  2. 外观设置主色：「自定义」改为「自定义主色」且独立一行（`custom-color-block`，不与 13 预置色板同行）；自定义主色徽标复用 `preset-swatch` 外观（`custom-swatch selected` + check 角标 + 当前色底 + hex 文本），点击徽标唤起隐藏原生颜色选择器（`customColorRef.click()`）。
  - 冒烟核验：高级按钮 = 文字在前 + 右侧 down 箭头（min-width auto）；外观设置抽屉 = 13 预置 + 独立「自定义主色」行、徽标外观一致、点击唤起颜色选择器。
- **执行期三次细化（1 项，浏览器冒烟已核验）**：
  1. 「自定义主色」标签与徽标并入预置色板网格**第三行**：`custom-color-block` 移入 `.preset-swatches`（`grid-column: 1 / -1`），徽标置于**标签下方**（`flex-direction: column`，`custom-color-row` 承载徽标+hex 水平排列）。
  - 冒烟核验：三行 top 742/778/816；自定义徽标 28x28、radius 6px、check 角标、主题色描边均与预置一致；标签行在徽标行上方（labelBottom 837 < rowTop 843）。

## 验收阶段记录（openspec-verify）

**自动化门禁复检**（2026-08-08，ArcoVue/web）：
- `pnpm run test`：**28 文件 295 通过**（含本 OSC 新增 `presetColors.spec.ts` 5 项 + `iconRegistry.spec.ts` 15 项 = 20 项）
- `pnpm run build`（vue-tsc + vite）：**通过**（`built in ~18s`，主包 324KB gzip），`wwwroot` 已重新生成
- 残留核查：`grep @arco-design/web-vue/es/icon web/src` **0 命中**；`grep TemplateManageDrawer web/src packages` **0 命中**（仅 openspec 文档描述性提及）

**三步检查汇总**：
1. **实现审计**：proposal §7 成功标准 12 项 —— 11 项 ✅；1 项 ⚠️ 经确认属**用户决策**（「高级」按钮为文字在前 + 右侧 down 箭头，非 design §2.3④ 文字前 more 图标，用户明确要求撤销该改动），记入风险。
2. **代码审查**：16 文件无重要/阻断问题；6 条轻微已修复 —— ①iconRegistry 头注释过时（install→按需）已改；②自定义主色徽标恒 selected 与预置色选中二义 → 改为 `isPresetColorActive()` 条件化（命中预置色时不显示 selected/check）；③RecordDrawer 缩进已对齐；④`more` 死登记已删（同步 spec）；⑤main.ts 透传 `type` 冗余 DOM 属性 → 解构排除；⑥menuIcon `fa fa-user` 空格多类名 → 拆分首 token 查表。
3. **文档同步**：`openspec/README.md` ✅ / `Doc/功能清单.md` ✅ / `web/README.md` ⚠️「全量 install」描述与按需引入实现相悖 → **已修正**为「main.ts 注册聚合组件 + iconComponents.ts 按需引入」。

**会话小任务补录**：执行期 3 轮细化（视图图标/more-one/菜单图标、高级按钮撤销、自定义主色布局、立即保存按钮删除）已并入 tasks.md T2/T5/T6/T7，并同步 status.md note。

**验收结论**：全部 AC 通过，checklist passed，保持 Validating，可复盘。

## 验收标准

### 主题预置色
- [x] **AC-01 13 色预置**：`PRESET_THEME_COLORS` 恰含 13 个官方品牌色（官方中文命名 + hex，不含灰色），默认色「极客蓝」`#165DFF` 在其中
- [x] **AC-02 外观设置展示**：`AppearanceDrawer` 与 `appearance` 页主色区为「预置色板 swatch-grid + 自定义色区」；当前主题色在色板中高亮（描边/选中态）
- [x] **AC-03 选色生效**：点击任一预置色 → 主色即时切换（`--primary-1~10`/浅色阶跟随）+ `patchTheme` 写入 `primaryColor` + 持久化（刷新恢复）
- [x] **AC-04 自定义色保留**：自定义色区（color-picker/hex）仍可任意选色，与预置色互不干扰

### 统一图标体系
- [x] **AC-05 依赖与注册**：`web/package.json` 含 `@icon-park/vue-next ^1.4.2`；`main.ts` 注册全局 `<icon-park>` 聚合组件（**按需引入** `iconComponents.ts`，非全量 install——design §10 降级，主包 682→324KB gzip）
- [x] **AC-06 Arco 图标清零**：`grep -r "@arco-design/web-vue/es/icon" web/src` 为 0 命中
- [x] **AC-07 图标名有效性**：`iconRegistry.ts` 全部注册图标名均已在 `ICON_COMPONENTS` 登记（spec 断言全覆盖）；冒烟逐项渲染无 `not a valid icon type` 报错

### 多维视图图标
- [x] **AC-08 视图 Tab 图标**：Tab 显示类型图标（替代原类型文字）+ tooltip 显示类型名；6 视图（表格 list-checkbox/树状 tree-list/卡片 pic/看板 blackboard/日历 calendar/甘特 timeline）图标各不相同且正确
- [x] **AC-09 工具栏图标**：「筛选/分组/搜索」按钮带图标，徽标计数逻辑不变
- [x] **AC-10 高级菜单图标**：「高级」按钮（文字 + 右侧 down 箭头）及导入/导出/批量删除/表单布局菜单项带图标；导出格式子菜单可用
- [x] **AC-11 管理模板移除**：「高级/管理模板」菜单项不存在；`TemplateManageDrawer.vue` 文件已删除；`grep -r "TemplateManageDrawer" web/src packages/**` 为 0 命中
- [x] **AC-12 详情字段图标**：详情抽屉字段标签前按字段类型显示图标；抽查 String（font-size）/ 数值（list-numbers）/ DateTime（time）/ Boolean（switch）/ Map（link）/ Image（pic）与 `fieldIcon` 映射一致

### 右上角
- [x] **AC-13 主题图标按钮**：右上角为主题图标按钮（亮色=sun / 暗色=moon / 跟随系统=computer），tooltip 显示当前外观名；点击循环切换
- [x] **AC-14 外观设置入口**：独立「外观设置」按钮不存在；用户下拉保留「外观设置」doption 且点击打开 `AppearanceDrawer`；「退出登录」正常；**「立即保存」按钮已删除**（外观设置为动态防抖持久化）

### 导航菜单
- [x] **AC-15 菜单图标**：side/top/mix 三布局菜单项显示图标；管理员内置菜单（fa 命中）与业务菜单（关键词/默认兜底）均可见图标，无空白

### 侧栏布局 header
- [x] **AC-17 header 顶部间隙**：布局模式「侧栏」下，header 面包屑与右上角按钮垂直居中（实测 60px + border-bottom）、顶部有合理间隙（不顶住界面）；「顶栏/混合」布局 header 无回归

### 门禁
- [x] **AC-16 门禁**：前端 Vitest 全绿（295）+ `vue-tsc` + `vite build` 通过（`wwwroot` 重新生成）；本 OSC 新增 20 项单测全部通过

## 风险

- 「高级」按钮最终形态与 design §2.3④ 提案（文字前 more 图标）不同——**用户验收期明确决策**改为文字在前 + 右侧 down 箭头，已记入执行期细化与 tasks.md；proposal 成功标准「高级（含菜单项）带图标」仍满足。
- IconPark 按需引入依赖 `iconComponents.ts` 登记完整性——由 `iconRegistry.spec.ts` 断言双向锁死；新增图标需同步登记（openspec/README.md 已注明规则）。

## 自动化门禁

```powershell
# 前端（ArcoVue）
npm.cmd --prefix "f:\Git Repos\1.Newlife\NewLife.Cube\NewLife.Cube.ArcoVue\web" run test
npm.cmd --prefix "f:\Git Repos\1.Newlife\NewLife.Cube\NewLife.Cube.ArcoVue\web" run build

# 残留引用核查
grep -r "@arco-design/web-vue/es/icon" "f:\Git Repos\1.Newlife\NewLife.Cube\NewLife.Cube.ArcoVue\web\src"
grep -r "TemplateManageDrawer" "f:\Git Repos\1.Newlife\NewLife.Cube\NewLife.Cube.ArcoVue\web\src" "f:\Git Repos\1.Newlife\NewLife.Cube\packages"
```

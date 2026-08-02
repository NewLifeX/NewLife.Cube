# OSC-0007 Tasks

> 仅在状态进入 `Implementing` 后勾选；每项完成后先跑对应测试，再推进下一项。

## T1 工具栏与图表入口

- [x] 1.1 在 `web/src/views/crud/DefaultList.vue` 删除搜索区和列表顶栏的两个“图表”按钮。
- [x] 1.2 保留 `ListChartModal`、`chartVisible`、`chartList`、`openChart()`、`getChartData` 调用及组件 import；`openChart` 以 `void openChart` 显式引用避免 noUnusedLocals。
- [x] 1.3 删除独立 `.page-tools` 结构与样式，改由列表顶栏右侧「高级」下拉承载导入、导出、批量删除。
- [x] 1.4 在 `viewMapping.ts` 实现 `resolveBatchDeleteState` / `isTableLikeViewKind`，补 Vitest（viewMapping.spec）。

## T2 表格选择与删除门禁

- [x] 2.1 表格视图向 `ListTable` 固定传入 `show-checkbox="activeViewKind === 'table'"`；树和其他视图维持现有行为。
- [x] 2.2 复用 `ListTable` 既有 `checkbox_state_change` → `selectionChange` 回写 `selectedKeys`，表头全选沿用 VTable 机制。
- [x] 2.3 按条件矩阵以 `batchDeleteState` 控制批量删除菜单项的显示与 `disabled`；`handleBatchDelete` 增加可见/可用防御检查；删除成功后清空选择并 reload。
- [x] 2.4 视图切换/创建/删除/重置及实体路由切换后清空 `selectedKeys`。

## T3 配置抽屉精简

- [x] 3.1 `ViewConfigDrawer.vue` 将「顶部栏」文案替换为「工具栏」。
- [x] 3.2 删除“允许添加记录”“按钮文字”“自定义按钮”表单控件；`DefaultList.vue` 移除自定义按钮运行入口，新增记录按钮改用 `flags.canAdd`。
- [x] 3.3 表格/树保留分组和排序配置；卡片/看板/日历/甘特隐藏（`isTableLikeViewKind`）。
- [x] 3.4 旧 `chrome.allowAdd`/`addButtonText`/`customButton` 保留在类型与 normalizeChrome 中，运行时不再渲染；保存走对象扩展保留未知键。

## T4 卡片布局与字体 Token

- [x] 4.1 `viewMapping.ts` 扩展 `CardMapping.layout`、`normalizeCardLayout`、`seedMapping`/`normalizeMapping` 归一化。
- [x] 4.2 `ViewConfigDrawer.vue` 卡片区增加布局单选（标准/偏大/整行），经 `update:mapping` 写入 `NamedView.mapping`。
- [x] 4.3 `CardList.vue` 按 layout 应用 `card-list--standard/large/row` 栅格；`RecordCard.vue` 按 layout 放置图片并渲染整行字段（窄屏回退）。
- [x] 4.4 `cardHelpers.ts` 输出 `CardBodyField.fullRow` 与 `isCardBodyFieldFullRow`；补 Vitest（cardHelpers.spec）。
- [x] 4.5 `theme/tokens.ts` 新增 5 个语义字体 CSS 变量；本号触及组件已改用。

## T5 验证与文档

- [x] 5.1 执行 `pnpm test`：18 files / 108 tests passed。
- [x] 5.2 执行 `pnpm build`：vue-tsc + vite 无错误。
- [x] 5.3 自动化门禁通过；浏览器手工冒烟见 verify.md（待真实环境执行）。
- [x] 5.4 回写迁移方案（ViewMapping.layout、差距表#7、§13 OSC-0007）、对接指南、web README。

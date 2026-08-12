# ADR 0003：Element Plus 管外观，Tailwind 管页面布局

**状态：已采纳**

## 背景

页面一致性会因未定义的颜色、间距、组件状态和布局方式而分裂。项目已使用 Element Plus、Tailwind 和可切换主题。

## 决策

- Element Plus 负责交互组件的外观与状态。
- Tailwind 负责页面级 `flex`、`grid`、间距和响应式布局。
- `src/theme/tailwind.css` 只把 Tailwind 语义类映射至 `--el-*`，不创建第三套色彩 token。
- 页面级布局不与 `el-row`/`el-col` 并用；后者仅用于 `el-form` 内的字段网格。

## 后果

- UI 规范以 [ui-spec.md](../standards/ui-spec.md) 为准。
- 新页面不能硬编码主题色或引入第二套图标、色彩 token 和页面级栅格。
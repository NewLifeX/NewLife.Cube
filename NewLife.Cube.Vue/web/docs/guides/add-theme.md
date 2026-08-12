# 新增主题

## 规则

主题只改变 Element Plus 语义颜色；页面布局和组件代码不得为某个主题写特例。完整 UI 约束见 [ui-spec.md](../standards/ui-spec.md)。

## 步骤

1. 在 `core/themes/<name>.css` 新建主题文件，覆盖 `--el-color-primary` 及其亮度变体、`--el-color-success`、`--el-color-warning`、`--el-color-danger`、`--el-color-info`。
2. 在 `core/themes/index.ts` 的 `THEME_CSS_FILES` 注册异步导入。
3. 在 `core/composables/useTheme.ts` 扩展 `ThemeFamily` 并在 `THEME_GROUPS` 添加 light/dark 选项。
4. 切换新主题，检查 Element Plus 按钮、表格、输入框、分页、错误状态与 Tailwind 语义类是否同步变化。
5. 若新增的是可复用的主题策略而非单个主题值，补 ADR 和 UI 规范；不要创建 `--app-*`、`--tn` 等平行 token 层。

## 验收

- [ ] `bg-primary`、`text-primary`、`border-primary` 随主题变化。
- [ ] `bg-bg-page`、`text-text-primary`、`border-border` 在浅色/深色均可读。
- [ ] 页面没有硬编码主题颜色。
- [ ] Element Plus 官方暗色变量仍在 `core/initApp.ts` 导入。

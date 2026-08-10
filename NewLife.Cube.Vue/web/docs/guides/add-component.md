# 新增共享组件

## 先选择归属

| 情况               | 位置                                              |
| ------------------ | ------------------------------------------------- |
| 仅当前业务路径使用 | `apps/<app>/src/views/<path>/`，优先 Section 覆盖 |
| 多个业务应用共享   | `core/components/`                                |
| 默认列表/表单能力  | `core/views/components/` 或 `core/views/`         |
| 页面级布局         | `core/layouts/`，同时遵守布局技能                 |

不要为已有 `CbTable`、`LovSelect`、`Uploader`、`RichEditor` 等能力新建功能相同的组件；先查 [组件目录](../reference/component-catalog.md)。

## 实施步骤

1. 用 [UI 规范](../standards/ui-spec.md) 确定组件的外观、反馈和可访问性。
2. 定义显式 props、emits 和 TypeScript 类型；不要通过全局对象隐式传递数据。
3. 主题相关样式使用 `--el-*` 或 Tailwind 语义类；页面级排版用 Tailwind。
4. 为异常、空数据、禁用和加载状态确定行为。
5. 为复杂逻辑或公共组件添加 Vitest 测试。
6. 把组件加入 [component-catalog.md](../reference/component-catalog.md)；若新增通用规则或新扩展机制，更新标准或 ADR。

# 架构决策记录（ADR）

ADR 记录仍会影响后续工作的关键取舍。每篇 ADR 固定包含：背景、决策、理由、后果和状态。

| 编号                                                        | 状态   | 决策                                                |
| ----------------------------------------------------------- | ------ | --------------------------------------------------- |
| [0001](./0001-core-first-application-model.md)              | 已采纳 | 以 `core/` 为框架引擎，`apps/` 为业务应用边界       |
| [0002](./0002-menu-driven-routing-and-section-overrides.md) | 已采纳 | 菜单驱动动态路由，业务页面与 Section 覆盖按约定发现 |
| [0003](./0003-element-plus-tailwind-design-system.md)       | 已采纳 | Element Plus 管外观，Tailwind 管页面布局            |
| [0004](./0004-controller-first-crud-and-progressive-override.md) | 已采纳 | 控制器优先提供 CRUD，业务差异按覆盖层级递进 |
# 覆盖默认页面区块

默认 CRUD 页面由框架 `core/views/` 组合而成。只需替换局部功能时，使用 Section 覆盖，避免复制列表或表单整页。

## 步骤

1. 确认目标菜单路径，例如 `/admin/user`。
2. 在业务应用创建对应目录：`apps/<app>/src/views/admin/user/`。
3. 创建首字母大写、名称位于 `SectionKeyMap` 的 `.vue` 文件。
4. 启动 Vite；`cubeFront()` 插件会扫描该文件并生成虚拟 Section 模块。
5. 打开目标菜单路径，验证覆盖生效且其他 Section 仍使用默认实现。

示例：

```text
apps/cube-admin/src/views/admin/user/
├─ index.vue                 # 可选：完整自定义页面
├─ ListSearchBar.vue         # 覆盖搜索区
├─ ListToolbar.vue           # 覆盖工具栏
└─ FormContent.vue           # 覆盖表单字段区
```

## 可用名称

权威名单在 `core/composables/useSections.ts` 的 `SectionKeyMap`。当前包括：

- `DefaultListPage`、`PageNotFound`
- `ListPageHeader`、`ListSearchBar`、`ListToolbar`、`ListTableContent`、`ListPagination`、`ListPageFooter`
- `FormPageHeader`、`FormContent`、`FormActions`

仅 PascalCase 文件会被扫描；`index.vue`、`form.vue` 等小写文件不是 Section 覆盖。

## 真实案例：参数管理（Parameter）

`apps/cube-admin/src/views/admin/parameter/` 演示了从整页复制到 Section 覆盖的迁移：

**迁移前**：`apps/cube-admin/src/views/admin/parameter/index.vue`（~300 行，手写完整 CRUD）
**迁移后**：删除 `index.vue`，仅保留 `ListTableContent.vue`（~100 行，仅定制 kind 列渲染）

```text
apps/cube-admin/src/views/admin/parameter/
└─ ListTableContent.vue      # Section 覆盖：将 kind（Int32）渲染为标签
```

这样做的收益：
- 搜索栏、工具栏、分页、页头、新增/编辑弹窗等全部由默认引擎提供
- 默认引擎自动处理数据加载、分页、CRUD 操作
- 覆盖文件职责单一，仅解决"kind 列显示为标签"这一个业务需求
- 后续升级默认引擎时，parameter 页面自动受益

## 何时改 core

当新能力应服务所有应用的默认页面时，修改 `core/views/` 或共享组件；当它只适用于一个业务路径时，用应用层 Section。两者取舍见 [ADR 0001](../decisions/0001-core-first-application-model.md) 与 [ADR 0002](../decisions/0002-menu-driven-routing-and-section-overrides.md)。

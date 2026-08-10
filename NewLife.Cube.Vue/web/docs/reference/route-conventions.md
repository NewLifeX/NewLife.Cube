# 路由与视图约定参考

| 项目         | 当前约定                                       |
| ------------ | ---------------------------------------------- |
| 框架入口     | `index.html` 加载 `/core/main.ts`              |
| 静态路由     | `core/routes/index.ts`                         |
| 动态菜单路由 | `core/utils/menuRoutes.ts`                     |
| 微应用清单   | `configs/microAppConfig.json`                  |
| 应用路由导出 | `apps/<app>/src/main.ts` 导出 `routes`         |
| 业务页面     | `apps/<app>/src/views/**/index.vue`            |
| 列表页渲染   | `core/views/index.vue`（目标：ADR 0005 P3 落地后渲染单个 `<CubeTable />` 并自动创建引擎；**当前为旧 Section 实现**） |
| 列表页定制   | 在 `<CubeTable>` 内用**具名插槽**覆盖（见下）  |
| Section 覆盖 | ⚠️ **遗留机制，将被废除**（见 `decisions/0005` 与 `architecture/cube-engine.md`） |
| 动态路径风格 | `router.routeNamingStyle`：`pascal` 或 `kebab` |

动态菜单路径优先解析业务应用视图；未匹配则使用 `core/views/index.vue` 默认列表页。已存在的应用级路由不会被菜单动态注册覆盖。

## 列表页定制：CubeTable + 插槽（取代 Section 覆盖）

默认列表页 `core/views/index.vue` 仅渲染 `<CubeTable />`，由引擎按当前路由自动创建上下文并渲染搜索/工具栏/表格/分页/表单弹窗。`CubeTable` 整合这些区域，并开放**具名插槽**供业务应用定制，**不再使用 Section 覆盖文件**。

- 整块替换：`#search` / `#toolbar` / `#table` / `#pagination` / `#form`
- 手术式附加：`#header` / `#footer`（横幅）、`#toolbar-extra`（默认工具栏内追加按钮）
- 表格内：`#table-row-actions="{ row, ctx }"`（行操作按钮）、`#col-[prop]="{ row, field, value }"`（按字段名动态单元格渲染，如 `#col-Status`）

示例（自定义搜索区与行操作）：

```vue
<template>
  <CubeTable>
    <template #search="{ ctx }"><MySearchBar :ctx="ctx" /></template>
    <template #toolbar-extra="{ ctx }"><el-button @click="ctx.openChart()">图表</el-button></template>
    <template #table-row-actions="{ row, ctx }">
      <el-button link type="primary" @click="ctx.openEdit(row)">编辑</el-button>
      <el-button link type="danger"  @click="ctx.remove(row)">删除</el-button>
    </template>
  </CubeTable>
</template>
```

`CubeTable` 的上下文三级解析：外部 `context` prop → 已注入的 `useCubeEngine` → 按当前路由自动创建。定制组件可取数于：插槽作用域 `{ ctx }`、`useCubeContext()`（父级已 provide）、或父级 `:ctx` 显式传入。完整插槽 API 与内置子组件拆分见 `architecture/cube-engine.md` §5。

> **遗留 Section 覆盖（将被废除）**：原 `core/composables/useSections.ts` 的 `SectionKeyMap` 按文件名覆盖（`ListSearchBar`、`ListToolbar`、`ListTableContent`、`FormContent`、`FormActions`）。新页面一律用 CubeTable 插槽；存量 Section 覆盖页面在迁移完成前仍可用（由 `index.vue` 兼容桥按路由 meta 切换），最终随 `decisions/0005` 的 P6 移除。

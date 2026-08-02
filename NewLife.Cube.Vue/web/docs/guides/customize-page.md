# 覆盖默认页面区块（CubeTable 具名插槽）

默认 CRUD 页面由框架 `core/views/index.vue` 渲染单个 `<CubeTable />` 组合而成。`CubeTable` 整合搜索、工具栏、表格、分页、新增/编辑弹窗，并开放**具名插槽**供业务应用定制。只需替换局部功能时，用插槽覆盖，避免复制列表或表单整页。

> **Section 覆盖（旧机制）已废除**：原 `ListSearchBar.vue` / `ListToolbar.vue` / `FormContent.vue` 等按 `SectionKeyMap` 文件名覆盖的方式，自 `decisions/0005` 起不再鼓励；存量 Section 覆盖页面在迁移完成前仍可用（由 `index.vue` 兼容桥按路由 meta 切换），最终随 P6 移除。新页面一律用下方插槽。

## 步骤（插槽定制）

1. 确认目标菜单路径，例如 `/admin/user`。
2. 在业务应用创建对应目录（如需放定制组件）：`apps/<app>/src/views/admin/user/`。
3. 在目标路由的 `index.vue` 中用 `<CubeTable>` 包裹插槽，写定制内容。无需新建同名文件、无需理解隐式解析。
4. 打开目标菜单路径，验证插槽覆盖生效且其他区域仍使用默认实现。

示例（自定义搜索区、工具栏追加按钮、行操作、单元格渲染）：

```vue
<!-- apps/cube-admin/src/views/admin/user/index.vue -->
<template>
  <CubeTable>
    <!-- 整块替换搜索区 -->
    <template #search="{ ctx }">
      <MySearchBar :ctx="ctx" />
    </template>

    <!-- 在默认工具栏内追加按钮（最小侵入） -->
    <template #toolbar-extra="{ ctx }">
      <el-button @click="ctx.openChart()">图表</el-button>
    </template>

    <!-- 自定义行操作（方案规范槽名 #table-row-actions） -->
    <template #table-row-actions="{ row, ctx }">
      <el-button link type="primary" @click="ctx.openEdit(row)">编辑</el-button>
      <el-button link type="danger"  @click="ctx.remove(row)">删除</el-button>
    </template>

    <!-- 自定义某字段单元格（按字段名动态槽 #col-[prop]，如状态列 #col-Status） -->
    <template #col-Status="{ row, field, value }">
      <el-tag v-if="field.name === 'Status'">{{ value ? '启用' : '禁用' }}</el-tag>
      <span v-else>{{ value }}</span>
    </template>
  </CubeTable>
</template>
```

## 插槽 API（每个区域均可定制）

| 插槽 | 作用域 `{ ctx }` | 默认渲染 | 用途 |
| --- | --- | --- | --- |
| `header` | `ctx` | 无 | 表格上方横幅（提示/告警/筛选标签） |
| `search` | `ctx` | `CubeTableSearch` | 整块搜索区替换 |
| `toolbar` | `ctx` | `CubeTableToolbar` | 整块工具栏替换 |
| `toolbar-extra` | `ctx` | 无 | 默认工具栏**内**追加按钮（最小侵入） |
| `table` | `ctx` | `CubeTableGrid` | 整块表格替换 |
| `table-row-actions` | `{ row, ctx }` | 内置 编辑/删除 | 表格操作列的按钮自定义（方案规范槽名，取代早期 `#row-actions`） |
| `col-[prop]` | `{ row, field, value }` | 内置 文本/tag/image | 按字段名动态定制某列单元格，如 `#col-Status` / `#col-Kind`（取代早期单一兜底槽 `#cell`） |
| `pagination` | `ctx` | `CubeTablePagination` | 整块分页替换 |
| `footer` | `ctx` | 无 | 表格下方区域 |
| `form` | `ctx` | `CubeTableFormDialog` | 整块新增/编辑弹窗替换 |

完整插槽与内置子组件拆分见 `architecture/cube-engine.md` §5；上下文三级解析（`context` prop → `inject` → 自动创建）见 `reference/route-conventions.md`。

## 真实案例：参数管理（Parameter）的 kind 列渲染

目标：把 `kind`（Int32）渲染为标签。

**旧方式（Section 覆盖，遗留）**：`apps/cube-admin/src/views/admin/parameter/ListTableContent.vue` 覆盖整个表格内容区，仅定制 kind 列。

**新方式（插槽，推荐）**：在 `apps/cube-admin/src/views/admin/parameter/index.vue` 用 `#col-Kind` 插槽只定制该单元格，其余表格能力（搜索/工具栏/分页/弹窗）全部由 `CubeTable` 默认提供：

```vue
<template>
  <CubeTable>
    <template #col-Kind="{ row, field, value }">
      <el-tag v-if="field.name === 'Kind'">{{ kindLabel(value) }}</el-tag>
      <span v-else>{{ value }}</span>
    </template>
  </CubeTable>
</template>
```

收益：定制点内聚在一处、显式可见；默认引擎自动处理数据加载、分页、CRUD；后续升级默认引擎时自动受益。

## 何时改 core

当新能力应服务所有应用的默认页面时，修改 `core/components/CubeTable/` 或 `core/engine/`；当它只适用于一个业务路径时，用应用层插槽。两者取舍见 `decisions/0001` 与 `decisions/0002`。

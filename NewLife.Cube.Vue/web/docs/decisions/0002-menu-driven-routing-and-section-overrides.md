# ADR 0002：菜单驱动动态路由，业务页面与 Section 覆盖按约定发现

**状态：已采纳（路由发现部分仍有效；「Section 覆盖」定制路径已被 ADR 0005 取代，见下方遗留标注）**

## 背景

后端菜单决定用户可访问的业务路径；同时，业务应用需要在不修改框架默认模板的前提下替换局部页面区块。

## 决策

- 路由守卫加载菜单后，由 `registerMenuRoutes()` 注册叶子菜单的动态路由。
- 动态路由按 `apps/*/src/views/**/index.vue` 解析业务页面，未命中时回退框架默认页面。
- `cubeFront()` Vite 插件扫描业务应用 `src/views/` 中 PascalCase 的 `.vue` 文件，并通过虚拟模块注册为 Section 覆盖。
- 应用级预注册路由优先于菜单动态路由；同一路径不会被动态注册覆盖。

## 后果

- 新增标准 CRUD 时应先检查菜单和默认页面是否足够，不应先手写路由。
- 只需改搜索栏、工具栏、表格、表单字段等局部时，使用 Section 覆盖而不是复制整页。
- 页面路径、视图目录和 Section 文件名必须遵守约定，具体步骤见 [新增页面](../guides/add-page.md) 与 [覆盖页面](../guides/customize-page.md)。

> **遗留标注（2026-08-02）**：本 ADR 中「Section 覆盖」作为默认 CRUD 的局部定制手段，自 `decisions/0005` 起被 **`CubeTable` 具名插槽** 取代。路由发现（`apps/*/src/views/**/index.vue` 解析、菜单动态注册）仍然有效；但**新页面一律用 CubeTable 插槽定制，不再新建 Section 覆盖文件**。存量 Section 覆盖页面在迁移完成前保留（兼容桥），最终随 `architecture/cube-engine.md` §7 的 P6 移除。最新定制方式见 [customize-page.md](../guides/customize-page.md) 与 [route-conventions.md](../reference/route-conventions.md)。
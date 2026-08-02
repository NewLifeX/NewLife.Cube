# Vue、TypeScript 与样式约定

> 团队规范总纲见 [前端可测试渐进式开发与测试规范](../standards/frontend-testable-development.md)。下文「关注点分离」为其强制细则。

## 关注点分离（强制）

- **视图只渲染，逻辑只计算，基础设施只通信**。严禁在 `index.vue` 的 `<template>` 与 `<script setup>` 中编写业务逻辑（数据结构转换、提交拼装、取数、复杂 `filter/map`）。
- **`index.vue` 的 `<script setup>` 不允许超过 50 行**，不允许出现 `new Date()`、`fetch()`、直接 `request` 取数或复杂条件拼接。
- 任何数据转换、复杂计算、提交前拼装，必须提取到 **`*.logic.ts`** 作为纯函数导出（页面级放 `apps/<app>/src/views/<area>/<entity>/<entity>.logic.ts`；跨页面共享逻辑进 `core/engine`）。纯函数禁止依赖 Vue/DOM，便于 Vitest 单测。
- 页面定制一律用 `CubeTable` 具名插槽（见 [customize-page.md](../guides/customize-page.md)）；不再新建 Section 覆盖文件。

## 代码边界

- 默认模板和共享能力修改在 `core/`（引擎在 `core/engine/`、集成组件在 `core/components/CubeTable/`）；业务页面和定制组件修改在 `apps/<app>/`。
- 根 `src/` 是遗留壳，不是当前默认模板的实现位置。
- 优先扩展已有 composable、业务组件和 `CubeTable` 插槽；不要为局部页面复制框架逻辑。

## Vue 与 TypeScript

- 使用 Vue 3 Composition API 与 `<script setup lang="ts">`。
- Props、emits、请求结果和跨模块数据必须有显式 TypeScript 类型。
- 类型导入使用 `import type`。
- **命名分层约定（避免歧义，组件文件夹与文件分开规定）：**
  - **组件文件夹**：大驼峰 **PascalCase**，与组件名一致。例：`core/components/LovSelectTable/`、`core/components/CubeTable/`（内含 `CubeTable.vue`、`CubeTableSearch.vue`、`CubeTableToolbar.vue`、`CubeTableGrid.vue`、`CubeTablePagination.vue`、`CubeTableFormDialog.vue`）。现役代码目录即采用此风格，新增组件目录必须沿用。
  - **组件文件**：大驼峰 **PascalCase** 的 `.vue`（`CubeTable.vue`、`LovSelectTable.vue`）。
  - **组件名声明**：组件必须使用 `defineOptions({ name: '...' })` 显式声明组件名，组件名应与文件名（不含扩展名）一致，确保 Vue DevTools 正确显示组件层级。
  - **业务视图目录**（`apps/<app>/src/views/<area>/<entity>/`）：跟随 `router.routeNamingStyle`（`pascal` 或 `kebab`），解析兼容 PascalCase / kebab-case / 小写；入口文件固定为 `index.vue`。
  - **纯逻辑 / 工具文件**：小驼峰 `*.logic.ts`、`utils/*.ts`（`user.logic.ts`、`transform.ts`）；**不使用横线**。
  - **测试文件**：固定后缀 `*.spec.ts` / `*.test.ts`，基名跟随被测对象（现役 `LovSelectTable.test.ts`）；**不使用横线**。
- 自动发现的 Section 文件首字母大写，且名称必须存在于 `SectionKeyMap`。

## 状态与副作用

- 组件局部状态留在组件/composable；跨页面共享状态使用 Pinia store。
- 网络请求统一走 `core/utils/request.ts`；不要在页面中另建 Axios 实例。
- 标准 API 响应、分页和错误处理遵守 [api-contract.md](./api-contract.md)。
- 异步代码使用 `async`/`await`，边界处处理异常；不要吞掉错误后继续假装成功。

## 样式

- 页面级排版用 Tailwind；交互组件优先 Element Plus。
- 样式必须遵守 [ui-spec.md](./ui-spec.md)，主题相关值通过 `--el-*` 或映射的 Tailwind 语义类获取。
- 组件私有样式只处理 Tailwind/Element Plus 无法表达的局部结构，不写主题色硬编码。
- 不添加新的全局 token、全局类或覆盖规则，除非先有 ADR 说明其共享价值。

## 格式与检查

在 `web/` 执行：

```powershell
pnpm run type-check
pnpm run lint:eslint
pnpm run test:unit
```

全量 `check` 会顺序执行类型检查和 ESLint；提交前至少运行与改动范围匹配的命令。

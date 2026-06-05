# 页面覆盖系统设计文档

> **文档版本**：v1.0（实施中）  
> **适用范围**：框架开发者、业务应用开发者  
> **创建日期**：2026-05-03

---

## 1. 设计目标

本系统借鉴 **ASP.NET Core MVC** 的 View / Section / Layout 机制，为 Vue 3 + TypeScript 前端框架提供一套**约定优先、可局部覆盖**的页面组件体系。

### 1.1 核心目标

| 目标     | 说明                                                       |
| -------- | ---------------------------------------------------------- |
| 开箱即用 | 新项目零配置即可得到完整的列表页、表单页、布局             |
| 局部覆盖 | 业务方可替换任意 Section 组件，无需修改框架代码            |
| 层级清晰 | 覆盖优先级明确：页面级 > 应用级 > 框架默认                 |
| Vue 风格 | 机制基于 `provide/inject` + 具名 `slot`，符合 Vue 生态惯例 |

### 1.2 类比关系

```
ASP.NET Core MVC          →    本框架
─────────────────────────────────────────────────
_Layout.cshtml            →    MainLayout / TopMenu
@RenderBody()             →    <router-view>
@RenderSection("Search")  →    inject(SearchBarKey)
@section Search { ... }   →    provide(SearchBarKey, MyComp)
Partial View              →    Section 组件（可独立替换）
```

---

## 2. 核心概念

### 2.1 Section（页面分区）

Section 是页面的命名功能区块。框架为每种页面类型预定义若干 Section，每个 Section 都有默认实现，业务方可按需替换。

#### 列表页 Section

```
┌─────────────────────────────────┐
│          PageHeader             │  标题、面包屑
├─────────────────────────────────┤
│          SearchBar              │  搜索条件筛选区
├─────────────────────────────────┤
│         TableToolbar            │  新建按钮、批量操作、导出、刷新等
├─────────────────────────────────┤
│         TableContent            │  数据表格主体
├─────────────────────────────────┤
│          Pagination             │  分页控件
├─────────────────────────────────┤
│          PageFooter             │  页面底部（可选）
└─────────────────────────────────┘
```

#### 表单页 Section

```
┌─────────────────────────────────┐
│          PageHeader             │  标题、面包屑
├─────────────────────────────────┤
│         FormContent             │  表单字段区
├─────────────────────────────────┤
│         FormActions             │  提交、取消、重置按钮区
└─────────────────────────────────┘
```

### 2.2 SectionKey

每个 Section 对应一个 InjectionKey，用于 `provide/inject` 链路：

```typescript
// core/composables/useSections.ts
import type { InjectionKey, Component } from 'vue';

export const PageHeaderKey:    InjectionKey<Component> = Symbol('PageHeader');
export const SearchBarKey:     InjectionKey<Component> = Symbol('SearchBar');
export const TableToolbarKey:  InjectionKey<Component> = Symbol('TableToolbar');
export const TableContentKey:  InjectionKey<Component> = Symbol('TableContent');
export const PaginationKey:    InjectionKey<Component> = Symbol('Pagination');
export const PageFooterKey:    InjectionKey<Component> = Symbol('PageFooter');

// 表单页
export const FormContentKey:   InjectionKey<Component> = Symbol('FormContent');
export const FormActionsKey:   InjectionKey<Component> = Symbol('FormActions');
```

### 2.3 三层解析优先级

```
┌─────────────────────────────────────────────────────┐
│  页面级（最高优先级）                                  │
│  setup() 内 provide(SearchBarKey, UserSearchBar)     │
├─────────────────────────────────────────────────────┤
│  应用级                                              │
│  initApp 回调中 app.provide(SearchBarKey, AppSearch)│
├─────────────────────────────────────────────────────┤
│  框架默认（最低优先级）                                │
│  core/components/defaults/SearchBar.vue             │
└─────────────────────────────────────────────────────┘
```

Vue 的 `inject` 天然沿组件树向上查找，最近的 `provide` 胜出，与上述优先级完全吻合。

---

## 3. 覆盖机制设计

### 3.1 方案对比

| 方案                   | 机制                              | 优点                       | 缺点                       |
| ---------------------- | --------------------------------- | -------------------------- | -------------------------- |
| **A：SectionKey 注入** | `provide/inject` 传递 Component   | 跨层级覆盖、应用级统一替换 | 需要显式 import SectionKey |
| **B：具名 Slot**       | `<DefaultListPage #search="...">` | Vue 原生写法，IDE 友好     | 只能在直接父组件中使用     |
| **C：A + B 混合**      | 两者并存，Slot 优先于 inject      | 灵活性最高                 | 实现略复杂                 |

**推荐方案：C（A + B 混合）**

- SectionKey 注入作为**主机制**，支持应用级统一配置和跨层覆盖
- 具名 Slot 作为**便捷方式**，适合单页面快速定制，无需 import Key
- Slot 内容的优先级高于 inject 结果

### 3.2 DefaultListPage 解析逻辑（伪代码）

```vue
<!-- core/pages/DefaultListPage.vue -->
<script setup lang="ts">
import { inject } from 'vue';
import {
  SearchBarKey, TableContentKey, PaginationKey,
  PageHeaderKey, TableToolbarKey, PageFooterKey
} from '@/composables/useSections';

// 框架默认组件
import DefaultPageHeader   from '@/components/defaults/PageHeader.vue';
import DefaultSearchBar    from '@/components/defaults/SearchBar.vue';
import DefaultTableToolbar from '@/components/defaults/TableToolbar.vue';
import DefaultTableContent from '@/components/defaults/TableContent.vue';
import DefaultPagination   from '@/components/defaults/Pagination.vue';
import DefaultPageFooter   from '@/components/defaults/PageFooter.vue';

// inject 回退到框架默认
const PageHeaderComp   = inject(PageHeaderKey,   DefaultPageHeader);
const SearchBarComp    = inject(SearchBarKey,    DefaultSearchBar);
const TableToolbarComp = inject(TableToolbarKey, DefaultTableToolbar);
const TableContentComp = inject(TableContentKey, DefaultTableContent);
const PaginationComp   = inject(PaginationKey,   DefaultPagination);
const PageFooterComp   = inject(PageFooterKey,   DefaultPageFooter);
</script>

<template>
  <!-- Slot 优先于 inject 结果 -->
  <slot name="header">
    <component :is="PageHeaderComp" v-bind="headerProps" />
  </slot>
  <slot name="search">
    <component :is="SearchBarComp" v-bind="searchProps" />
  </slot>
  <slot name="toolbar">
    <component :is="TableToolbarComp" v-bind="toolbarProps" />
  </slot>
  <slot name="table">
    <component :is="TableContentComp" v-bind="tableProps" />
  </slot>
  <slot name="pagination">
    <component :is="PaginationComp" v-bind="paginationProps" />
  </slot>
  <slot name="footer">
    <component :is="PageFooterComp" />
  </slot>
</template>
```

### 3.3 文件约定（推荐项目结构）

```
apps/my-app/src/pages/UserList/
├── index.vue          ← 页面入口，使用 DefaultListPage 模板
├── SearchBar.vue      ← 覆盖默认搜索栏（可选）
├── TableContent.vue   ← 覆盖默认表格（可选）
└── PageHeader.vue     ← 覆盖默认页头（可选）
```

> **约定说明**：文件名与 SectionKey 名称一一对应。框架未来可提供自动扫描插件，在构建时自动 `provide` 同目录下发现的 Section 组件（类似 Nuxt 的文件路由约定），彻底消除手动 `provide` 的样板代码。

---

## 4. 框架默认组件

### 4.1 目录结构（待实现）

```
core/
├── components/
│   ├── CbTable.vue              ← 已有：DataSet 表格
│   ├── CubeSearch.vue           ← 已有：搜索组件
│   ├── CubePager.vue            ← 已有：分页组件
│   ├── CubeListPager.vue        ← 已有：列表+分页组合
│   ├── CubeListToolbarSearch.vue← 已有：工具栏搜索
│   └── defaults/                ← 待实现：Section 默认组件
│       ├── PageHeader.vue       ← 默认页头（标题 + 新建按钮）
│       ├── SearchBar.vue        ← 默认搜索（封装 CubeSearch）
│       ├── TableToolbar.vue     ← 默认工具栏（封装 CubeListToolbarSearch）
│       ├── TableContent.vue     ← 默认表格（封装 CbTable）
│       ├── Pagination.vue       ← 默认分页（封装 CubePager）
│       ├── PageFooter.vue       ← 默认底部
│       ├── FormContent.vue      ← 默认表单内容
│       └── FormActions.vue      ← 默认表单操作
└── pages/
    ├── DefaultEntity.vue        ← 已有：列表+表单切换（待重构）
    ├── DefaultListPage.vue      ← 待实现：默认列表页
    ├── DefaultFormPage.vue      ← 待实现：默认表单页
    ├── PageLogin.tsx            ← 已有
    ├── PageHome.vue             ← 已有
    └── PageNotFound.vue         ← 已有
```

### 4.2 各默认组件职责说明

| 组件               | 依赖现有组件            | 主要 Props             | 说明                                     |
| ------------------ | ----------------------- | ---------------------- | ---------------------------------------- |
| `PageHeader.vue`   | —                       | `title`                | 显示页面标题与面包屑导航                 |
| `SearchBar.vue`    | `CubeSearch`            | `dataset`              | 从 DataSet 读取查询字段定义并渲染        |
| `TableToolbar.vue` | `CubeListToolbarSearch` | `dataset`              | 新建按钮、批量操作、导出、刷新等操作入口 |
| `TableContent.vue` | `CbTable`               | `dataset`              | 从 DataSet 读取列定义和数据渲染表格      |
| `Pagination.vue`   | `CubePager`             | `dataset`              | 从 DataSet 读取分页状态                  |
| `PageFooter.vue`   | —                       | —                      | 可选底部区域，默认不渲染                 |
| `FormContent.vue`  | —                       | `dataset`, `fields`    | 表单字段自动渲染                         |
| `FormActions.vue`  | —                       | `onSubmit`, `onCancel` | 提交/取消/重置按钮                       |

---

## 5. 使用示例

### 5.1 零配置：直接使用框架默认

```vue
<!-- apps/my-app/src/pages/UserList/index.vue -->
<script setup lang="ts">
import { useDataSet } from '@core/dataset';
import DefaultListPage from '@core/pages/DefaultListPage.vue';

const dataSet = useDataSet({ api: '/api/users' });
</script>

<template>
  <!-- 所有 Section 均使用框架默认，零配置 -->
  <DefaultListPage :dataset="dataSet" title="用户管理" />
</template>
```

### 5.2 页面级覆盖：SectionKey 注入

```vue
<!-- apps/my-app/src/pages/UserList/index.vue -->
<script setup lang="ts">
import { provide } from 'vue';
import { useDataSet } from '@core/dataset';
import { SearchBarKey, TableContentKey } from '@core/composables/useSections';
import DefaultListPage from '@core/pages/DefaultListPage.vue';
import UserSearchBar from './SearchBar.vue';
import UserTable from './TableContent.vue';

const dataSet = useDataSet({ api: '/api/users' });

// 页面级覆盖：仅影响当前页面
provide(SearchBarKey, UserSearchBar);
provide(TableContentKey, UserTable);
</script>

<template>
  <DefaultListPage :dataset="dataSet" title="用户管理" />
</template>
```

### 5.3 页面级覆盖：具名 Slot（快捷方式）

```vue
<!-- apps/my-app/src/pages/UserList/index.vue -->
<script setup lang="ts">
import { useDataSet } from '@core/dataset';
import DefaultListPage from '@core/pages/DefaultListPage.vue';
import UserSearchBar from './SearchBar.vue';

const dataSet = useDataSet({ api: '/api/users' });
</script>

<template>
  <DefaultListPage :dataset="dataSet" title="用户管理">
    <!-- 仅替换搜索栏，其他 Section 仍使用框架默认 -->
    <template #search>
      <UserSearchBar :dataset="dataSet" />
    </template>
  </DefaultListPage>
</template>
```

### 5.4 应用级覆盖：统一替换某 Section

```typescript
// apps/my-app/src/main.ts
import { initApp } from '@core/initApp';
import { SearchBarKey } from '@core/composables/useSections';
import AppSearchBar from './components/AppSearchBar.vue';

initApp((app) => {
  // 应用级覆盖：该应用所有列表页都使用 AppSearchBar
  app.provide(SearchBarKey, AppSearchBar);
});
```

---

## 6. 与现有框架的集成点

### 6.1 DataSet 集成

`DataSet`（`core/dataset/`）是列表页的数据层核心，封装了：
- 列表数据和列定义
- 分页状态（当前页、总数、每页条数）
- 查询参数和查询逻辑

`DefaultListPage` 接收 `dataSet` prop 并向下透传给各 Section 组件，各默认 Section 组件通过 `dataSet` 完成自渲染，无需业务层手动绑定数据。

```typescript
// DefaultListPage 的 Props 定义示意
interface DefaultListPageProps {
  dataset: DataSet;       // 核心数据层
  title?: string;         // 页面标题
}
```

### 6.2 LayoutKey 对齐

现有 `core/composables/useProvideInject.ts` 已实现 `LayoutKey` 的 provide/inject 机制。SectionKey 体系遵循完全相同的模式，确保一致性：

```typescript
// 现有（已实现）
export const LayoutKey: InjectionKey<Component> = Symbol('Layout');

// 新增（待实现），命名和用法保持一致
export const SearchBarKey: InjectionKey<Component> = Symbol('SearchBar');
```

---

## 7. 实现路线图

### Phase 1：基础框架（优先级：高）

- [x] 在 `core/composables/useSections.ts` 中定义全部 SectionKey 常量
- [x] 实现 `useSections` composable（封装 inject + 默认回退逻辑）
- [x] 实现 `core/pages/DefaultListPage.vue`（基于 inject + slot 双机制）
- [x] 实现 `core/pages/DefaultFormPage.vue`

> **实现说明（2026-05-04）**：所有默认 Section 组件均按 Boreal Admin 设计风格（`docs/design.md`）从零实现，不依赖现有的 CbTable、CubeSearch 等封装组件，保持样式一致性和独立性。

### Phase 2：默认 Section 组件（优先级：中）

- [ ] `core/components/defaults/PageHeader.vue`
- [ ] `core/components/defaults/SearchBar.vue`（封装 CubeSearch）
- [ ] `core/components/defaults/TableToolbar.vue`（封装 CubeListToolbarSearch）
- [ ] `core/components/defaults/TableContent.vue`（封装 CbTable）
- [ ] `core/components/defaults/Pagination.vue`（封装 CubePager）
- [ ] `core/components/defaults/FormContent.vue`
- [ ] `core/components/defaults/FormActions.vue`

### Phase 3：完善与扩展（优先级：低）

- [ ] 实现构建时自动扫描 Section 文件的 Vite 插件（消除手动 provide 样板代码）
- [ ] 补充单元测试（覆盖优先级链路）
- [ ] 完善开发者文档和使用示例

---

## 8. 设计决策记录

### ADR-001：为什么选择 provide/inject 而非 Vuex/Pinia

- Section 组件替换是**配置行为**，而非状态管理，不应引入状态管理库
- `provide/inject` 天然支持树形作用域，与覆盖优先级语义完全一致
- 无需额外依赖，框架保持轻量

### ADR-002：为什么同时支持 Slot

- Slot 是 Vue 模板中最直观的内容插入方式，对业务开发者友好
- 单页面快速定制时，Slot 无需 import SectionKey，减少认知负担
- Slot 和 provide/inject 互为补充，不冲突

### ADR-003：SectionKey 使用 Symbol 而非 String

- Symbol 可避免命名冲突（多个框架共存时）
- TypeScript `InjectionKey<T>` 泛型提供类型安全，inject 结果类型自动推断
- 运行时无法伪造，安全性更高

---

*本文档为设计草稿，实现细节可能随开发进展调整。*

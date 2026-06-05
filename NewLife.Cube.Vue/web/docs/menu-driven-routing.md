# 菜单驱动路由注册设计文档

> **文档版本**：v0.2（设计完善中）  
> **适用范围**：框架开发者、业务应用开发者  
> **创建日期**：2026-05-04

---

## 1. 设计背景与目标

### 1.1 背景

本框架定位为 Vue 3 + TypeScript 的**约定优先前端框架**，设计思想类比 **ASP.NET Core MVC**：路由不需要手动一一声明，而是由框架根据约定自动推导。

在 ASP.NET Core MVC 中，控制器方法（Action）自动映射为路由，无需每个接口都在 `Startup` 里手动注册。本框架将这一思想引入前端：**菜单数据即路由声明**——后端返回的菜单树已经包含了所有业务路径，框架据此自动注册路由，无需前端开发者逐一 `addRoute`。

### 1.2 类比关系

```
ASP.NET Core MVC                    →    本框架
──────────────────────────────────────────────────────────
路由约定（Convention-based routing） →    菜单驱动路由注册
Controller / Action 自动发现        →    菜单叶子节点自动注册
DefaultController（兜底逻辑）       →    DefaultListPage（框架默认页）
Global Filter（全局拦截）           →    router.beforeEach（路由守卫）
Area / Module                       →    菜单树父节点（分组）
```

### 1.3 核心目标

| 目标         | 说明                                                             |
| ------------ | ---------------------------------------------------------------- |
| 零配置路由   | 新业务路径只需在后端菜单中配置，无需前端改动路由文件             |
| 开箱即用页面 | 菜单叶子节点自动使用 `DefaultListPage`，无需每个路由都指定组件   |
| 灵活覆盖     | 业务应用可在 `initApp` 或页面组件中覆盖特定路径的组件            |
| 刷新稳定     | 页面刷新后路由依然可用，不出现白屏或 404                         |
| 优先级明确   | 应用级预注册 > 框架动态注册（菜单） > catch-all 兜底，行为可预期 |

---

## 2. 方案对比

在设计过程中评估了三种实现路径：

| 方案                                | 机制                                                                      | 优点                                               | 缺点                                                     | 结论         |
| ----------------------------------- | ------------------------------------------------------------------------- | -------------------------------------------------- | -------------------------------------------------------- | ------------ |
| **方案 1**：catch-all 延迟检查      | 访问未知路由时在 `DefaultEntity` 组件内部检查菜单，命中后动态注册并重定向 | 实现最简单，不需要改动路由守卫结构                 | 首次访问产生双重导航，history 留下多余记录，用户体验较差 | 不推荐       |
| **方案 2**：菜单加载后批量注册      | 菜单数据加载完成后，在路由守卫中批量 `addRoute` 全部叶子节点              | 路由表干净，导航直接命中目标路由，无冗余跳转       | 刷新页面时需等待菜单加载，需要 `/loading` 中转页处理     | 推荐         |
| **方案 3**：方案 2 + 组件解析优先级 | 同方案 2，额外支持应用级预注册覆盖，内置表单页路由路径约定                | 最灵活，支持多层覆盖，与 Section Override 系统协同 | 设计稍复杂，需制定路由类型约定（列表 vs 表单）           | **最优选择** |

**本文采用方案 3**。

---

## 3. 推荐方案详细设计

### 3.1 核心流程

#### 正常登录流程

```
用户登录
    │
    ▼
router.beforeEach 触发
    │
    ├─ 无 token → 重定向 /login
    │
    ▼
有 token，检查是否有菜单数据
    │
    ├─ 无菜单 → fetchMenuAsync() → registerMenuRoutes(router, flatMenus)
    │                                      │
    │               ┌──────────────────────┘
    │               ▼
    │           遍历菜单叶子节点，跳过已注册路径
    │           resolvePageComponent(path) 确定组件
    │           router.addRoute({ path, component })
    │
    ├─ 有菜单（已注册） → 直接放行
    │
    ▼
检查 to.name === 'DefaultEntity'（命中了 catch-all）
    │
    ├─ 是 → next({ ...to, replace: true })  重新导航，命中新注册路由
    │
    └─ 否 → next()
```

#### 刷新页面流程

```
页面刷新
    │
    ▼
Vue Router 初始化，静态路由可用，动态路由尚未注册
    │
    ▼
router.beforeEach 触发（目标路由可能命中 catch-all）
    │
    ▼
发现 !menuStore.hasMenus
    │
    ▼
重定向到 /loading（保存 redirect 参数）
    │
    ▼
Loading 页面等待 fetchMenuAsync() + registerMenuRoutes()
    │
    ▼
注册完成 → 重定向回原始路径（命中新注册路由）
```

### 3.2 路由组件解析优先级

```
┌──────────────────────────────────────────────────────────┐
│  优先级 1（最高）：应用级预注册路由                        │
│  apps/my-app/src/main.ts 中 initApp 回调里                │
│  router.addRoute({ path: '/user', component: UserList })  │
├──────────────────────────────────────────────────────────┤
│  优先级 2：框架菜单动态注册                               │
│  菜单叶子节点 → resolvePageComponent(path)                │
│  → DefaultListPage 或 DefaultFormPage                    │
├──────────────────────────────────────────────────────────┤
│  优先级 3（最低）：框架 catch-all 兜底                    │
│  path: '/:pathMatch(.*)*' → DefaultEntity                │
│  （显示 404 提示或框架兜底内容）                          │
└──────────────────────────────────────────────────────────┘
```

**实现依据**：Vue Router 的 `addRoute` 在路由表中按注册顺序排列。当应用级路由**先于**框架动态注册时，其优先级更高；catch-all 始终在静态路由中最后匹配。框架在注册菜单路由时跳过已存在路径，确保应用级路由不被覆盖。

### 3.3 `registerMenuRoutes` 伪代码

```typescript
// core/utils/menuRoutes.ts

import type { Router } from 'vue-router';
import type { FlatMenuItem } from '@/stores/menu';

/**
 * 根据路径约定推断页面组件。
 *
 * 约定规则（无需后端改造，纯前端约定）：
 *   - /create、/new、/add 结尾 → DefaultFormPage（新建表单）
 *   - /edit/:id、/update/:id  → DefaultFormPage（编辑表单）
 *   - 其余路径               → DefaultListPage（列表页）
 */
function resolvePageComponent(path: string) {
  if (/\/(create|new|add)$/.test(path) || /\/(edit|update)(\/|$)/.test(path)) {
    return () => import('@/pages/DefaultFormPage.vue');
  }
  return () => import('@/pages/DefaultListPage.vue');
}

/**
 * 将菜单叶子节点批量注册为动态路由。
 *
 * @param router   Vue Router 实例
 * @param menus    已拍平的菜单列表（来自 menuStore.flatMenus）
 */
export function registerMenuRoutes(router: Router, menus: FlatMenuItem[]): void {
  // 获取当前已注册路径集合，避免重复注册（应用级预注册路由受到保护）
  const existingPaths = new Set(router.getRoutes().map(r => r.path));

  // 筛选叶子节点：在 flatMenus 中，没有其他菜单以该 id 为 parentId 的即为叶子
  const parentIds = new Set(menus.map(m => m.parentId).filter(Boolean));
  const leafMenus = menus.filter(m => !parentIds.has(m.id) && m.path);

  for (const menu of leafMenus) {
    if (existingPaths.has(menu.path)) {
      // 应用已预注册该路径，跳过，保持应用级路由优先
      continue;
    }

    router.addRoute({
      path: menu.path,
      name: `menu-${menu.id}`,                   // 避免与静态路由名冲突
      component: resolvePageComponent(menu.path),
      meta: {
        auth: true,
        menuId: menu.id,
        title: menu.title,
      },
    });
  }
}
```

### 3.4 路由守卫修改点

```typescript
// core/router/index.ts（修改部分）

import { registerMenuRoutes } from '@/utils/menuRoutes';

router.beforeEach(async (to, from, next) => {
  // 1. Token 检查（现有逻辑保持不变）
  // ...

  // 2. 用户信息（现有逻辑保持不变）
  // ...

  // 3. 菜单加载 + 动态路由注册
  if (!menuStore.hasMenus) {
    await menuStore.fetchMenuAsync();
    // ← 新增：菜单加载完成后批量注册动态路由
    registerMenuRoutes(router, menuStore.flatMenus);
  }

  // 4. 设置当前激活菜单（现有逻辑保持不变）
  if (menuStore.hasMenus) {
    menuStore.setActiveMenuByPath(to.path);
  }

  // 5. ← 新增：若当前导航命中了 catch-all，需重新导航以命中新注册的路由
  if (to.name === 'DefaultEntity' && menuStore.hasMenus) {
    return next({ ...to, replace: true });
  }

  next();
});
```

### 3.5 路径约定详解

| 路径示例           | 匹配规则          | 解析结果          |
| ------------------ | ----------------- | ----------------- |
| `/user`            | 默认              | `DefaultListPage` |
| `/user/role`       | 默认              | `DefaultListPage` |
| `/user/create`     | 以 `/create` 结尾 | `DefaultFormPage` |
| `/user/new`        | 以 `/new` 结尾    | `DefaultFormPage` |
| `/user/edit/123`   | 含 `/edit/`       | `DefaultFormPage` |
| `/user/update/123` | 含 `/update/`     | `DefaultFormPage` |
| `/order/add`       | 以 `/add` 结尾    | `DefaultFormPage` |

> **扩展说明**：若未来需要后端显式声明页面类型，可在 `FlatMenuItem` 中增加 `pageType?: 'list' | 'form' | 'detail'` 字段，`resolvePageComponent` 优先读取该字段，路径约定作为回退。

---

## 4. 目录结构

### 4.1 新增文件

```
core/
└── utils/
    └── menuRoutes.ts    ← 新增：registerMenuRoutes 工具函数
```

### 4.2 修改文件

```
core/
├── router/
│   └── index.ts         ← 修改：菜单加载后调用 registerMenuRoutes，处理 catch-all 重导航
└── routes/
    └── index.ts         ← 可选修改：catch-all 的 DefaultEntity 组件是否保留（建议保留作为 404 兜底）
```

### 4.3 完整目录上下文

```
core/
├── utils/
│   ├── menuRoutes.ts    ← 新增
│   ├── api-helpers.ts
│   ├── request.ts
│   └── ...
├── router/
│   └── index.ts         ← 修改
├── routes/
│   └── index.ts         ← 可选修改
├── pages/
│   ├── DefaultListPage.vue   ← 现有，菜单路由默认组件
│   ├── DefaultFormPage.vue   ← 现有，表单路由默认组件
│   └── DefaultEntity.vue     ← 现有，catch-all 兜底（保留）
└── stores/
    └── menu.ts               ← 现有，提供 flatMenus 数据
```

---

## 5. 使用示例

### 5.1 零配置（开箱即用）

后端菜单配置了 `/user` 路径后，前端**无需任何改动**，刷新即可通过 `/user` 访问一个基于 `DefaultListPage` 的页面。

```
后端新增菜单项：{ path: '/order', title: '订单管理', ... }
                            ↓
框架自动注册路由：router.addRoute({ path: '/order', component: DefaultListPage })
                            ↓
用户访问 /order → 看到默认列表页（含默认搜索栏、表格、分页）
```

`DefaultListPage` 会自动调用接口获取数据并展示，全程不需要业务方写一行路由代码。

### 5.2 应用级覆盖（自定义路由组件）

若某个路径需要完全自定义的页面组件，在 `initApp` 回调中**提前**注册即可。框架动态注册时会检测到该路径已存在，自动跳过。

```typescript
// apps/my-app/src/main.ts

import { initApp } from '@cube/core';
import UserListPage from './pages/UserListPage.vue';

initApp({
  setup(app, router) {
    // 在框架注册菜单路由前，提前注册自定义组件
    router.addRoute({
      path: '/user',
      name: 'UserList',
      component: UserListPage,
      meta: { auth: true },
    });
  },
});
```

### 5.3 页面级覆盖（结合 Section Override 系统）

若只需替换 `DefaultListPage` 的某个 Section（如搜索栏），无需自定义整个页面，直接使用 Section Override 系统：

```vue
<!-- apps/my-app/src/pages/OrderList.vue -->
<script setup lang="ts">
import { provide } from 'vue';
import { SearchBarKey, TableContentKey } from '@cube/core/composables/useSections';
import OrderSearchBar from './OrderSearchBar.vue';
import OrderTable from './OrderTable.vue';

// 覆盖搜索栏和表格，其余 Section 使用框架默认
provide(SearchBarKey, OrderSearchBar);
provide(TableContentKey, OrderTable);
</script>

<template>
  <!-- DefaultListPage 会通过 inject 自动使用上面 provide 的组件 -->
  <DefaultListPage />
</template>
```

也可直接使用具名 Slot（更简洁，适合简单覆盖）：

```vue
<template>
  <DefaultListPage>
    <template #search>
      <OrderSearchBar />
    </template>
  </DefaultListPage>
</template>
```

---

## 6. 实施清单（Phase 1）

以下为具体实施步骤，按顺序执行：

```
1.  创建 core/utils/menuRoutes.ts
    - 实现 resolvePageComponent(path: string) 函数
    - 实现 registerMenuRoutes(router, menus) 函数
    - 编写单元测试（vitest）覆盖路径约定的各种情况

2.  修改 core/router/index.ts
    - 在 fetchMenuAsync() 调用之后，加入 registerMenuRoutes(router, menuStore.flatMenus)
    - 在 next() 之前，加入 catch-all 检测逻辑（to.name === 'DefaultEntity'）

3.  验证刷新场景
    - 确认 /loading 页面在菜单未加载时正确中转
    - 确认菜单加载完成后可重定向回原始路径并命中新注册路由

4.  验证应用级覆盖
    - 在测试应用的 initApp 中预注册一个自定义组件路由
    - 确认框架动态注册跳过该路径
    - 确认访问该路径时使用的是应用级组件

5.  验证路径约定
    - 访问 /user/create，确认加载 DefaultFormPage
    - 访问 /user，确认加载 DefaultListPage
    - 访问 /user/edit/1，确认加载 DefaultFormPage

6.  更新 FlatMenuItem 类型定义（可选）
    - 在 core/stores/menu.ts 中为 FlatMenuItem 添加 pageType?: 'list' | 'form' | 'detail'
    - 在 resolvePageComponent 中优先读取该字段

7.  更新 core/types/index.ts
    - 导出 registerMenuRoutes 供外部按需使用

8.  补充文档示例代码到 docs/menu-driven-routing.md（本文档）
```

---

## 7. 未来规划（Phase 2）

### 7.1 文件路由自动扫描（类 Nuxt 文件路由）

Phase 2 将引入**文件扫描自动注册**机制，彻底消除手动 `addRoute` 的样板代码：

```
apps/my-app/src/pages/
├── user/
│   ├── index.vue          → 路由 /user          （列表页）
│   ├── create.vue         → 路由 /user/create    （新建表单）
│   └── edit/
│       └── [id].vue       → 路由 /user/edit/:id  （编辑表单）
└── order/
    └── index.vue          → 路由 /order
```

框架提供 Vite 插件，在构建时扫描 `pages/` 目录，自动生成路由配置并注入，与菜单驱动路由协同工作：**文件路由的优先级等同于"应用级预注册"**，高于框架菜单动态注册。

### 7.2 路由元信息自动推断

从菜单数据自动填充路由的 `meta` 字段：

```typescript
meta: {
  title: menu.title,      // 用于面包屑、页面标题
  icon: menu.icon,        // 用于 Tab 页图标
  menuId: menu.id,        // 用于激活菜单高亮
  keepAlive: true,        // 可从菜单配置中读取
}
```

### 7.3 动态路由热更新

菜单数据变更（如权限调整）后，支持在不刷新页面的情况下动态移除或添加路由，实现权限的实时生效。

---

## 8. 约定式 Section 组件自动发现

### 8.1 功能目标

在第 5.3 节的"页面级 Section 覆盖"方案中，业务开发者需要在 `index.vue` 里手动 `provide` 覆盖组件。  
本功能通过**文件系统约定**自动完成 `provide`，彻底消除 Section 覆盖的样板代码：

> **约定**：页面文件夹下存在与 SectionKey 同名的组件文件时，框架自动将其注入为对应 Section 的覆盖组件，无需手动 `provide`。

```
apps/my-app/src/pages/
├── user/
│   ├── index.vue         ← 页面入口（可省略，路由由菜单驱动自动注册）
│   ├── SearchBar.vue     ← 自动覆盖 SearchBarKey → 替换默认搜索栏
│   └── TableContent.vue  ← 自动覆盖 TableContentKey → 替换默认表格
└── order/
    └── FormContent.vue   ← 自动覆盖 FormContentKey → 替换默认表单内容
```

### 8.2 关键约束与设计决策

Vite 在**构建时**静态分析 `import.meta.glob`，不支持运行时动态路径拼接。因此 glob 调用必须写在**应用代码**中，框架提供工具函数 `registerPageSections`，应用传入 glob 结果即可。

文件夹路径（相对于 `pages/`）直接映射为路由路径：

| glob key                             | 路由路径      | Section 名     |
| ------------------------------------ | ------------- | -------------- |
| `./pages/user/SearchBar.vue`         | `/user`       | `SearchBar`    |
| `./pages/user/TableContent.vue`      | `/user`       | `TableContent` |
| `./pages/order/list/FormContent.vue` | `/order/list` | `FormContent`  |

只处理**大写字母开头**（PascalCase）的文件，排除 `index.vue` 等入口文件。

### 8.3 实现方案

#### Step 1：App 启动注册（应用侧）

```typescript
// apps/my-app/src/main.ts
import { initApp } from '@core/initApp';
import { registerPageSections } from '@core/utils/pageSections';

const pageModules = import.meta.glob('./pages/**/*.vue');

initApp((app) => {
  registerPageSections(app, pageModules);
});
```

#### Step 2：工具函数（框架侧，新增 `core/utils/pageSections.ts`）

```typescript
import type { App } from 'vue';
import { defineAsyncComponent } from 'vue';
import { PageSectionRegistryKey, SectionKeyMap } from '@core/composables/useSections';

type GlobModule = Record<string, () => Promise<{ default: unknown }>>;
type SectionRegistry = Record<string, Record<string, () => Promise<{ default: unknown }>>>;

function parseGlobKey(key: string): { routePath: string; sectionName: string } | null {
  const match = key.match(/^\.\/pages\/(.+)\/([A-Z][A-Za-z]+)\.vue$/);
  if (!match) return null;
  const [, folderPath, sectionName] = match;
  return { routePath: '/' + folderPath, sectionName };
}

export function registerPageSections(app: App, modules: GlobModule): void {
  const registry: SectionRegistry = {};

  for (const [key, loader] of Object.entries(modules)) {
    const parsed = parseGlobKey(key);
    if (!parsed) continue;
    const { routePath, sectionName } = parsed;
    if (!SectionKeyMap[sectionName]) continue;
    registry[routePath] ??= {};
    registry[routePath][sectionName] = loader;
  }

  app.provide(PageSectionRegistryKey, registry);
}
```

#### Step 3：新增 `SectionKeyMap` 和 `PageSectionRegistryKey`（`core/composables/useSections.ts`）

```typescript
/** Section 名称 → InjectionKey 映射表，用于约定式自动发现 */
export const SectionKeyMap: Record<string, InjectionKey<Component>> = {
  PageHeader:   PageHeaderKey,
  SearchBar:    SearchBarKey,
  TableToolbar: TableToolbarKey,
  TableContent: TableContentKey,
  Pagination:   PaginationKey,
  PageFooter:   PageFooterKey,
  FormContent:  FormContentKey,
  FormActions:  FormActionsKey,
};

/** 全局页面 Section 注册表的 InjectionKey */
export const PageSectionRegistryKey: InjectionKey<
  Record<string, Record<string, () => Promise<{ default: unknown }>>>
> = Symbol('PageSectionRegistry');
```

#### Step 4：`DefaultListPage` / `DefaultFormPage` 读取注册表

在 `<script setup>` 中（在 inject 调用之前执行）：

```typescript
import { inject, provide, defineAsyncComponent, useRoute } from 'vue';
import type { Component } from 'vue';
import { PageSectionRegistryKey, SectionKeyMap } from '@core/composables/useSections';

const route = useRoute();
const registry = inject(PageSectionRegistryKey, {} as Record<string, Record<string, () => Promise<{ default: unknown }>>>);
const pageOverrides = registry[route.path] ?? {};

// 约定式覆盖：优先级低于手动 provide，但高于应用级 provide
for (const [name, loader] of Object.entries(pageOverrides)) {
  const key = SectionKeyMap[name];
  if (key) {
    provide(key, defineAsyncComponent(loader as () => Promise<{ default: Component }>));
  }
}
// inject 在此之后执行，可读取到上面的 provide
```

### 8.4 完整优先级链

```
┌──────────────────────────────────────────────────────────────┐
│  优先级 1（最高）：具名 Slot                                   │
│  <DefaultListPage><template #search>...</template></...>     │
├──────────────────────────────────────────────────────────────┤
│  优先级 2：页面内手动 provide                                 │
│  index.vue setup() 中 provide(SearchBarKey, MyComp)          │
├──────────────────────────────────────────────────────────────┤
│  优先级 3：约定式自动发现（本章）                             │
│  pages/user/SearchBar.vue → 自动 provide 给 /user 路由       │
├──────────────────────────────────────────────────────────────┤
│  优先级 4：应用级 provide                                     │
│  initApp 回调中 app.provide(SearchBarKey, AppSearchBar)      │
├──────────────────────────────────────────────────────────────┤
│  优先级 5（最低）：框架默认                                   │
│  inject(SearchBarKey, DefaultSearchBar)                      │
└──────────────────────────────────────────────────────────────┘
```

### 8.5 SectionKey 与文件名对应表

| 文件名             | 对应 SectionKey   | 适用页面类型    |
| ------------------ | ----------------- | --------------- |
| `PageHeader.vue`   | `PageHeaderKey`   | 列表页 + 表单页 |
| `SearchBar.vue`    | `SearchBarKey`    | 列表页          |
| `TableToolbar.vue` | `TableToolbarKey` | 列表页          |
| `TableContent.vue` | `TableContentKey` | 列表页          |
| `Pagination.vue`   | `PaginationKey`   | 列表页          |
| `PageFooter.vue`   | `PageFooterKey`   | 列表页 + 表单页 |
| `FormContent.vue`  | `FormContentKey`  | 表单页          |
| `FormActions.vue`  | `FormActionsKey`  | 表单页          |

### 8.6 实施清单（Phase 2）

```
1.  修改 core/composables/useSections.ts
    - 新增 SectionKeyMap（Section 名 → InjectionKey 映射）
    - 新增 PageSectionRegistryKey（全局注册表 InjectionKey）

2.  创建 core/utils/pageSections.ts
    - 实现 parseGlobKey(key) 解析函数
    - 实现 registerPageSections(app, modules) 注册函数

3.  修改 core/pages/DefaultListPage.vue
    - 在 <script setup> inject 之前读取注册表并执行约定式 provide

4.  修改 core/pages/DefaultFormPage.vue
    - 同上

5.  在示例应用（apps/cube-admin/src/main.ts）中接入
    - 添加 import.meta.glob('./pages/**/*.vue')
    - 调用 registerPageSections(app, pageModules)

6.  验证约定式覆盖与优先级
```

---

## 附录：相关设计文档

| 文档                                  | 说明                                          |
| ------------------------------------- | --------------------------------------------- |
| `docs/page-override-system-design.md` | Section Override 系统设计（与本文协同工作）   |
| `docs/menu-driven-routing.md`         | 菜单驱动路由与约定式 Section 发现设计（本文） |
| `docs/design.md`                      | Boreal Admin 设计系统文档                     |
| `docs/project-specification.md`       | 项目规范与编码约定                            |

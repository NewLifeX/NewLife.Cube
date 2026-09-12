---
name: cube-layout
description: |
  在 @newlifex/cube-vue 框架中新增、注册或切换页面布局。
  当用户说"新增布局"、"自定义布局"、"修改布局"、"切换布局"、"使用 XXX 布局"时使用。
  包含布局组件创建、框架注册、应用使用的完整流程。
---

# @newlifex/cube-vue 布局系统

## 什么时候用

当需要以下场景时使用：
- 新增自定义页面布局
- 修改框架默认布局
- 切换不同页面使用不同布局
- 为特定路由指定布局

## 布局使用方式

### 方式一：框架默认布局（开箱即用）

框架内置了 CyberLayout（赛博风格布局），默认已注册并使用，无需任何配置：

```
@newlifex/cube-vue/core/layouts/CyberLayout/  ← 默认使用
@newlifex/cube-vue/core/layouts/MainLayout/   ← 备用布局
@newlifex/cube-vue/core/layouts/TopMenu/      ← 备用布局
```

### 方式二：应用层注册并使用自定义布局

在项目 `src/main.ts` 中调用 `registerLayout` 注册自定义布局，并设为当前布局：

```typescript
// src/main.ts
import { initApp } from '@newlifex/cube-vue/core/initApp';
import '@newlifex/cube-vue/core/global.css';
import { registerLayout } from '@newlifex/cube-vue/core/composables/useLayout';
import MainLayout from '@newlifex/cube-vue/core/layouts/MainLayout';

registerLayout({
  id: 'main',
  label: '主布局',
  icon: '⊟',
  description: '侧边栏 + 内容区布局',
  component: MainLayout,
}, true);

initApp();
```

> **关键**：`registerLayout(option, setAsCurrent)` 是注册布局的唯一入口。
> - `setAsCurrent = true`：立即切换为该布局（写入 `localStorage`）
> - `setAsCurrent = false`（默认）：仅注册，不切换
>
> **不要**使用 `provide(app, LayoutKey, ...)` —— 那是旧机制，`RootLayout` 不读取它。

---

## CSS Token 规范

> **重要**：布局样式**必须**使用 Element Plus CSS token（`--el-*`）或 Cube Layout token（`--cube-layout-*`），**禁止硬编码色值、自定义 CSS 变量或第三方 token 体系**。

核心规则：
- ✅ 必须使用 `--el-*`（Element Plus 官方 Token），通过 `var(--xxx)` 引用
- ✅ 可使用 `--cube-layout-*`（框架布局保留 Token），不私占、不改写其语义
- ✅ 布局样式优先用 Tailwind 工具类编排（如 `flex items-center gap-2 bg-[var(--el-color-primary)]`）；需要取色 / 语义时直接用 `--el-*` 或框架保留的 `--cube-layout-*` token，**禁止自定义任何 CSS 变量**（包括 `--{布局名}-*` 这类命名）
- ❌ 禁止硬编码色值（如 `#1a2b3c`、`rgb(...)`、`rgba(...)`）
- ❌ 禁止自定义任何 CSS 变量（不得新增 `--cube-layout-xxx`，也不得新增 `--{布局名}-*` 之类自定义 token）
- ❌ 禁止在组件 `scoped style` 中覆盖 `--el-*` 变量

> 📖 完整 Token 表、三层架构规范、示例对照：[references/css-token-spec.md](references/css-token-spec.md)

---

## 新增自定义布局

> **注意**：首先判断在哪个项目新增布局：
> - **@newlifex/cube-vue 框架项目**：布局将作为内置布局，默认被所有应用使用
> - **用户自己的项目（如 NewLife.Cube.Vue）**：布局仅当前应用可用

### 场景 A：在用户项目中新增布局

在项目 `src/layouts/` 下创建布局目录，编写组件后在 `src/main.ts` 中调用 `registerLayout` 注册。

### 场景 B：在 @newlifex/cube-vue 框架中新增布局

在 `@newlifex/cube-vue/core/layouts/` 下创建布局目录，在 `core/main.ts` 中注册。

> 📖 两种场景的完整步骤、代码模板和组件实现：[references/layout-implementation-guide.md](references/layout-implementation-guide.md)

---

## 框架内置布局

| 布局 ID    | 路径                                   | 特点                               |
| ---------- | -------------------------------------- | ---------------------------------- |
| `cyber`    | `@newlifex/cube-vue/core/layouts/CyberLayout/` | 深色科技风格 + 霓虹发光 + 主题切换 |
| `main`     | `@newlifex/cube-vue/core/layouts/MainLayout/`  | 侧边栏 + 内容区，Element Plus 风格 |
| `top-menu` | `@newlifex/cube-vue/core/layouts/TopMenu/`     | 顶部导航栏 + 内容区                |

## 布局插槽与结构约定

`RootLayout` 是统一外壳，**只向布局组件注入两个插槽**：

| 插槽名    | 说明                 | 是否必须 |
| --------- | -------------------- | -------- |
| `default` | 主内容区（页面内容，由 RootLayout 用 keep-alive + transition 包裹） | 是 |
| `tabs`    | 标签页视图（声明后 RootLayout 注入内置 TabsView；不声明则不显示） | 否（可选） |

> 重要纠正：旧文档写 `sidebar` / `header` 是插槽——那是错的。`RootLayout` 不会向布局注入 `sidebar`/`header` 插槽。布局组件必须自己渲染侧边栏与顶栏（参照 `CyberLayout/index.vue` 直接 import 框架组件 `LogoBrand`/`SearchBar`/`MenuItem`/`UserProfile`/`ThemeSwitcher`/`ModeSwitcher`/`LayoutSwitcher`/`NotificationBell` 并 `useMenuStore()` 拉取菜单）。不要把侧边栏/顶栏写成等待 RootLayout 注入的空 `<slot name="sidebar"/>`，否则运行时不渲染、无菜单、无法导航。

布局组件标准骨架：

```vue
<template>
  <div class="my-layout">
    <aside class="side">
      <LogoBrand />
      <SearchBar />
      <ElScrollbar><MenuItem :menu="menu" /></ElScrollbar>
      <UserProfile />
      <ThemeSwitcher /><ModeSwitcher /><LayoutSwitcher /><NotificationBell />
    </aside>

    <slot name="tabs" />

    <main class="content"><slot /></main>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useMenuStore } from '@newlifex/cube-vue/core/stores/menu';
import { ElScrollbar } from 'element-plus';
import LogoBrand from '@newlifex/cube-vue/core/components/LogoBrand.vue';
import SearchBar from '@newlifex/cube-vue/core/components/SearchBar.vue';
import MenuItem from '@newlifex/cube-vue/core/components/MenuItem.vue';
import UserProfile from '@newlifex/cube-vue/core/components/UserProfile.vue';
import ThemeSwitcher from '@newlifex/cube-vue/core/components/ThemeSwitcher.vue';
import ModeSwitcher from '@newlifex/cube-vue/core/components/ModeSwitcher.vue';
import LayoutSwitcher from '@newlifex/cube-vue/core/components/LayoutSwitcher.vue';
import NotificationBell from '@newlifex/cube-vue/core/components/NotificationBell.vue';

const menu = computed(() => useMenuStore().menu);
</script>
```

布局专属装饰层（如星空背景、悬浮导航坞）可作为布局内部 UI 自由添加，不影响插槽约定。

## 运行时切换布局

```typescript
import { useLayout } from '@newlifex/cube-vue/core/composables/useLayout';
const { currentLayout, setLayout, availableLayouts } = useLayout();
setLayout('main-layout');
```

## 路由级别指定布局

> 框架路由由后端菜单自动生成，**不需要**手写 `vue-router` 的 `routes` 数组。布局的切换通过 `registerLayout(option, true)` 或运行时 `setLayout(id)`（LayoutSwitcher 组件）完成，而非在路由 `meta` 里指定。

若确有"某路由用特定布局"的强需求，可结合运行时 `setLayout` 在页面 `onMounted` 中切换，但常规项目不推荐。

## 场景判断速查

| 需求           | 在哪里新增                 | 是否需要 registerLayout                         |
| -------------- | -------------------------- | ----------------------------------------------- |
| 仅当前项目使用 | 用户项目 `src/layouts/`    | 是，`registerLayout(option, true)` 切换为当前   |
| 所有应用共用   | @newlifex/cube-vue `core/layouts/` | 是，在 `core/main.ts` 中注册                    |
| 设为框架默认   | @newlifex/cube-vue `core/layouts/` | 是，`registerLayout(option, true)` 设为当前布局 |

> **核心规则**：无论哪种场景，都必须调用 `registerLayout` 才能让布局生效。

---

## 红线 / 禁止自行发挥

> 以下为历史踩坑固化的强制约束，**落实时严格照办，禁止凭记忆或"想当然"自行发挥**：

1. **`registerLayout` 参数必须是 `label` + `icon`，禁止用 `name`**：框架 `LayoutOption` 以 `label`（展示名）+ `icon`（图标字符/组件）识别布局，`name` 不是合法字段，`vue-tsc` 会失败且布局无法注册。也不要再用旧版 `provide(app, LayoutKey, ...)`——`RootLayout` 不读取它。
2. **`RootLayout` 只注入 `tabs` 与 `default` 两个插槽**：`sidebar` / `header` **不是插槽**。布局组件必须**自己渲染**侧边栏与顶栏（import 框架组件 `LogoBrand`/`SearchBar`/`MenuItem`/`UserProfile`/`ThemeSwitcher`/`ModeSwitcher`/`LayoutSwitcher`/`NotificationBell` 并 `useMenuStore()` 拉菜单）。禁止把侧边栏/顶栏写成等待注入的空 `<slot name="sidebar"/>`——那会导致运行时不渲染、无菜单、无法导航。
3. **CSS Token 规范（硬约束）**：
   - ✅ 必须用 `--el-*`（Element Plus 官方 Token），经 `var(--xxx)` 引用；
   - ✅ 布局样式优先用 Tailwind 工具类（`flex` / `p-4` / `bg-[var(--el-color-primary)]` 等）编排；取色 / 语义只用 `--el-*` 或框架保留的 `--cube-layout-*` token，**不得新增任何自定义 CSS 变量**（含 `--{布局名}-*`）；
   - ❌ 禁止硬编码色值（`#1a2b3c`、`rgb(...)`、`rgba(...)`、`hsla(...)`）；
   - ❌ 禁止自定义任何 CSS 变量（不得新增 `--cube-layout-xxx`，也不得新增 `--{布局名}-*` 之类自定义 token）；
   - ❌ 禁止在 `scoped style` 中覆盖 `--el-*` 变量。
4. **不要手写路由 `routes` 为布局而设**：框架路由由后端菜单自动生成，布局切换靠 `registerLayout(option, true)` 或运行时 `setLayout(id)`，不在路由 `meta` 里指定布局。
5. **多布局共存时每个都要 `registerLayout`**：无论新增到用户项目还是框架 `core/layouts/`，都必须调用 `registerLayout` 才能生效，漏注册等于没加。

## 参考文件

| 文件 | 内容 |
| --- | --- |
| [references/css-token-spec.md](references/css-token-spec.md) | CSS Token 完整表、三层架构、命名规范、示例对照 |
| [references/layout-implementation-guide.md](references/layout-implementation-guide.md) | 布局实现完整流程、框架组件清单、菜单对接、踩坑记录、验证清单 |

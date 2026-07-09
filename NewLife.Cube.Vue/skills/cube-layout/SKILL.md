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

// 注册布局并立即切换为当前布局（第二个参数 setAsCurrent = true）
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
> - `setAsCurrent = false`（默认）：仅注册，不切换（可通过 `setLayout(id)` 或布局切换器后续切换）
>
> **不要**使用 `provide(app, LayoutKey, ...)` —— 那是旧机制，`RootLayout` 不读取它。

---

## CSS Token 规范

> **重要**：布局样式**必须**使用 Element Plus CSS token（`--el-*`）或 Cube Layout token（`--cube-layout-*`），**禁止硬编码色值、自定义 CSS 变量或第三方 token 体系**。
> 框架 `global.css` 和 `core/cube-layout-vars.css` 提供了完整的 Token 体系，布局组件直接引用即可。

### 双 Token 架构

```
┌───────────────────────────────────────────────┐
│  --el-* 体系（Element Plus 语义 Token）        │
│  --el-bg-color, --el-text-color-primary        │  ← 页面背景/文字/边框/填充
│  --el-color-primary, --el-border-color         │  ← 主色/边框
├───────────────────────────────────────────────┤
│  --cube-layout-* 体系（布局结构 Token）         │
│  --cube-layout-sidebar-width                  │  ← 侧边栏宽度
│  --cube-layout-menu-item-color                │  ← 菜单项颜色
│  --cube-layout-nav-height                     │  ← 导航栏高度
│  --cube-layout-tabsview-*                     │  ← 标签页颜色
└───────────────────────────────────────────────┘
```

### Element Plus 语义 Token（`--el-*`）

| 变量                          | 用途                     |
| ----------------------------- | ------------------------ |
| `--el-bg-color`               | 页面主体背景色           |
| `--el-bg-color-overlay`       | 弹窗/卡片/浮层面板背景   |
| `--el-fill-color`             | 填充色（输入框背景）     |
| `--el-fill-color-light`       | 浅填充色（搜索栏背景）   |
| `--el-text-color-primary`     | 一级文字色（标题/正文）  |
| `--el-text-color-regular`     | 二级文字色（次要信息）   |
| `--el-text-color-secondary`   | 三级文字色（提示文字）   |
| `--el-text-color-placeholder` | 占位符文字色             |
| `--el-border-color`           | 常规边框色               |
| `--el-border-color-light`     | 浅边框色（分割线）       |
| `--el-color-primary`          | 主色（按钮/链接/激活态） |
| `--el-color-success`          | 成功色                   |
| `--el-color-warning`          | 警告色                   |
| `--el-color-danger`           | 危险色（错误/删除）      |
| `--el-border-radius-base`     | 基础圆角（卡片/弹窗）    |
| `--el-border-radius-small`    | 小圆角（按钮/输入框）    |
| `--el-box-shadow-light`       | 浅阴影（卡片）           |
| `--el-box-shadow`             | 常规阴影（下拉面板）     |

### Cube Layout 布局结构 Token（`--cube-layout-*`）

| 变量                                     | 用途           | 默认值                            |
| ---------------------------------------- | -------------- | --------------------------------- |
| `--cube-layout-sidebar-width`            | 侧边栏展开宽度 | `220px`                           |
| `--cube-layout-sidebar-collapsed-width`  | 侧边栏折叠宽度 | `64px`                            |
| `--cube-layout-nav-height`               | 导航栏高度     | `52px`                            |
| `--cube-layout-content-padding`          | 内容区内边距   | `24px`                            |
| `--cube-layout-sidebar-bg`               | 侧边栏背景     | `var(--el-bg-color-overlay)`      |
| `--cube-layout-sidebar-border-color`     | 侧边栏边框色   | `var(--el-border-color)`          |
| `--cube-layout-menu-item-color`          | 菜单项文字色   | `var(--el-text-color-regular)`    |
| `--cube-layout-menu-item-hover-bg`       | 菜单项悬浮背景 | `var(--el-color-primary-light-9)` |
| `--cube-layout-menu-item-active-color`   | 菜单项激活色   | `var(--el-color-primary)`         |
| `--cube-layout-nav-bg`                   | 导航栏背景     | `var(--el-bg-color-overlay)`      |
| `--cube-layout-nav-border-color`         | 导航栏边框色   | `var(--el-border-color)`          |
| `--cube-layout-breadcrumb-item-color`    | 面包屑文字色   | `var(--el-text-color-secondary)`  |
| `--cube-layout-breadcrumb-current-color` | 面包屑当前色   | `var(--el-text-color-primary)`    |
| `--cube-layout-tabsview-bg`              | 标签页栏背景   | `var(--el-bg-color-overlay)`      |

> 完整 `--cube-layout-*` 变量清单见 `@newlifex/cube-vue/core/cube-layout-vars.css`

### 使用规则

| 规则                                                                                              | 说明                                           |
| ------------------------------------------------------------------------------------------------- | ---------------------------------------------- |
| ✅ **必须**使用 `--el-*` 或 `--cube-layout-*`                                                      | `background: var(--el-bg-color-overlay)`       |
| ✅ **必须**使用 `var(--xxx)` 引用                                                                  | `color: var(--el-text-color-primary)`          |
| ✅ 布局专属尺寸变量以 `--cube-layout-` 前缀                                                        | `--cube-layout-sidebar-width`                  |
| ✅ 带合理 fallback                                                                                 | `var(--cube-layout-nav-height, 60px)`          |
| ❌ **禁止**使用旧版自定义 token（`--bg-*`、`--text-*`、`--accent-*`、`--sidebar-*`、`--navbar-*`） | 已废弃，不再支持                               |
| ❌ **禁止**硬编码色值                                                                              | 不得出现 `#fff`、`#1e293b`、`rgba(x,x,x,x)` 等 |
| ❌ **禁止**在组件 `scoped style` 中覆盖 `--el-*` 变量                                              | 全局变量在各布局 `variables.css` 中统一管理    |

### 布局专属变量定义方式

**推荐**：在布局的 `styles/variables.css` 中使用两层覆盖：

```css
/* styles/variables.css — 同时覆盖 --el-* 和 --cube-layout-* */
:root,
[data-theme="aurora"] {
  /* 1) 覆盖 Element Plus 通用 Token */
  --el-color-primary: #2563eb;
  --el-bg-color: #f8fafc;
  --el-bg-color-overlay: #ffffff;
  --el-text-color-primary: #1e293b;
  --el-text-color-regular: #475569;
  --el-border-color: #e2e8f0;

  /* 2) 覆盖 Cube Layout 布局 Token */
  --cube-layout-sidebar-width: 220px;
  --cube-layout-sidebar-collapsed-width: 64px;
  --cube-layout-nav-height: 60px;
  --cube-layout-sidebar-bg: var(--el-bg-color-overlay);
  --cube-layout-menu-item-active-bg: linear-gradient(135deg, rgba(37,99,235,0.1), rgba(16,185,129,0.1));
}

[data-theme="aurora-dark"] {
  /* 深色模式只覆盖差异化值 */
  --el-bg-color: #0f172a;
  --el-bg-color-overlay: #1e293b;
  --el-text-color-primary: #f1f5f9;
  ...
}
```

### 示例对照

```scss
/* ❌ 错误：硬编码 */
.sidebar { background: #1e1e1e; color: #e8eaed; }

/* ❌ 错误：使用已废弃的自定义 token */
.sidebar { background: var(--bg-secondary); color: var(--text-primary); }

/* ✅ 正确：使用 Element Plus token */
.sidebar { background: var(--el-bg-color-overlay); color: var(--el-text-color-primary); }

/* ✅ 正确：使用 Cube Layout token */
.sidebar { background: var(--cube-layout-sidebar-bg); }
```

---

## 新增自定义布局

> **注意**：首先判断在哪个项目新增布局：
> - **@newlifex/cube-vue 框架项目**：布局将作为内置布局，默认被所有应用使用
> - **用户自己的项目（如 NewLife.Cube.Vue）**：布局仅当前应用可用

---

### 场景 A：在用户项目中新增布局

适用于需要为当前项目定制独特外观的情况。

#### 步骤 1：创建布局组件

在项目 `src/layouts/` 下创建布局目录：

```
NewLife.Cube.Vue/src/layouts/
└── CustomLayout/
    ├── index.vue           # 主布局入口
    ├── Sidebar.vue         # 侧边栏（可选）
    └── Navbar.vue          # 顶部导航栏（可选）
```

#### 步骤 2：编写布局组件

**src/layouts/CustomLayout/index.vue**：

```vue
<template>
  <div class="custom-layout">
    <aside class="layout-sidebar">
      <slot name="sidebar">
        <div class="logo">自定义Logo</div>
      </slot>
    </aside>

    <main class="layout-main">
      <header class="layout-navbar">
        <slot name="header">
          <h1>{{ title }}</h1>
        </slot>
      </header>

      <div class="layout-content">
        <slot></slot>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  title?: string;
}>();
</script>

<style scoped lang="scss">
.custom-layout {
  display: flex;
  min-height: 100vh;
  background: var(--bg-primary);
  color: var(--text-primary);
}

.layout-sidebar {
  width: var(--custom-sidebar-width, 260px);
  flex-shrink: 0;
  background: var(--bg-secondary);
  border-right: 1px solid var(--border-default);
}

.layout-main {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.layout-navbar {
  height: 64px;
  padding: 0 24px;
  background: var(--navbar-bg, var(--bg-secondary));
  border-bottom: 1px solid var(--navbar-border, var(--border-subtle));
  color: var(--navbar-text, var(--text-secondary));
}

.layout-content {
  flex: 1;
  padding: 24px;
}
</style>
```

#### 步骤 3：在应用中使用

**src/main.ts**：

```typescript
import { initApp } from '@newlifex/cube-vue/core/initApp';
import '@newlifex/cube-vue/core/global.css';
import { registerLayout } from '@newlifex/cube-vue/core/composables/useLayout';
import CustomLayout from './layouts/CustomLayout/index.vue';

// 注册自定义布局并立即切换为当前布局
registerLayout({
  id: 'custom',
  label: '自定义布局',
  icon: '◉',
  description: '自定义布局描述',
  component: CustomLayout,
}, true);

initApp();
```

---

### 场景 B：在 @newlifex/cube-vue 框架中新增布局

适用于需要让布局成为框架内置布局，所有应用默认使用的情况。

#### 步骤 1：创建布局组件

在 `@newlifex/cube-vue/core/layouts/` 下创建布局目录：

```
@newlifex/cube-vue/core/layouts/
└── YourLayout/
    ├── index.vue           # 主布局入口
    ├── Sidebar.vue         # 侧边栏（可选）
    ├── Navbar.vue          # 顶部导航栏（可选）
    └── styles/
        ├── variables.scss  # CSS 变量（可选）
        └── theme.scss      # 主题样式（可选）
```

#### 步骤 2：编写布局组件

**@newlifex/cube-vue/core/layouts/YourLayout/index.vue**：

```vue
<template>
  <div class="your-layout">
    <aside class="layout-sidebar">
      <slot name="sidebar">
        <div class="logo">Logo</div>
      </slot>
    </aside>

    <main class="layout-main">
      <header class="layout-navbar">
        <slot name="header">
          <h1>{{ title }}</h1>
        </slot>
      </header>

      <div class="layout-content">
        <slot></slot>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  title?: string;
}>();
</script>

<style lang="scss" scoped>
.your-layout {
  display: flex;
  min-height: 100vh;
  background: var(--bg-primary);
  color: var(--text-primary);
}

.layout-sidebar {
  width: 260px;
  flex-shrink: 0;
  background: var(--bg-secondary);
  border-right: 1px solid var(--border-default);
}

.layout-main {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.layout-navbar {
  height: 64px;
  padding: 0 24px;
  background: var(--navbar-bg, var(--bg-secondary));
  border-bottom: 1px solid var(--navbar-border, var(--border-subtle));
  color: var(--navbar-text, var(--text-secondary));
}

.layout-content {
  flex: 1;
  padding: 24px;
}
</style>
```

#### 步骤 3：在框架中注册布局

编辑 `@newlifex/cube-vue/core/main.ts`：

```typescript
import { initApp } from './initApp';
import YourLayout from './layouts/YourLayout/index.vue';
import { registerLayout } from './composables/useLayout';

// 注册新布局
registerLayout({
  id: 'your-layout',      // 唯一标识
  label: '你的布局',       // 显示名称
  icon: '◉',             // 图标
  description: '布局描述',
  component: YourLayout,
});

initApp();
```

#### 步骤 4：设为默认布局（可选）

在 `registerLayout` 时设置第二个参数 `setAsCurrent = true`：

```typescript
registerLayout({
  id: 'your-layout',
  label: '你的布局',
  icon: '◉',
  description: '布局描述',
  component: YourLayout,
}, true);  // setAsCurrent = true，立即切换为该布局
```

> **注意**：`registerLayout` 的第二个参数是 `setAsCurrent`（布尔值），不是 `isDefault`。
> - `true`：立即切换为该布局（写入 `localStorage`）
> - `false`（默认）：仅注册到列表，不切换

## 应用层使用布局

### 覆盖框架默认布局

```typescript
// src/main.ts
import { initApp } from '@newlifex/cube-vue/core/initApp';
import '@newlifex/cube-vue/core/global.css';
import { registerLayout } from '@newlifex/cube-vue/core/composables/useLayout';
import CustomLayout from './layouts/CustomLayout/index.vue';

// 注册自定义布局并立即切换
registerLayout({
  id: 'custom',
  label: '自定义布局',
  icon: '◉',
  description: '自定义布局描述',
  component: CustomLayout,
}, true);

initApp();
```

### 运行时切换布局

使用 `useLayout` 组合函数：

```typescript
import { useLayout } from '@newlifex/cube-vue/core/composables/useLayout';

const { currentLayout, setLayout, availableLayouts } = useLayout();

// 切换到指定布局
setLayout('main-layout');

// 获取当前布局
console.log(currentLayout.value);
```

### 路由级别指定布局

在路由 meta 中指定布局 ID：

```typescript
const routes = [
  {
    path: '/dashboard',
    component: () => import('./views/Dashboard.vue'),
    meta: {
      layout: 'your-layout',
    },
  },
];
```

## 框架内置布局

| 布局 ID    | 路径                                   | 特点                               |
| ---------- | -------------------------------------- | ---------------------------------- |
| `cyber`    | `@newlifex/cube-vue/core/layouts/CyberLayout/` | 深色科技风格 + 霓虹发光 + 主题切换 |
| `main`     | `@newlifex/cube-vue/core/layouts/MainLayout/`  | 侧边栏 + 内容区，Element Plus 风格 |
| `top-menu` | `@newlifex/cube-vue/core/layouts/TopMenu/`     | 顶部导航栏 + 内容区                |

### CyberLayout 赛博风格

CyberLayout 是框架默认布局，特点：
- 默认深色主题（`#0a0e14`）
- 绿色（`#4ade80`）/ 青色（`#22d3ee`）霓虹发光
- 260px 固定侧边栏
- 64px 顶部导航栏
- 内置主题切换按钮（明/暗）

CSS 变量（可在项目覆盖）：

```scss
:root {
  --cyber-bg-primary: #0a0e14;
  --cyber-accent-green: #4ade80;
  --cyber-accent-cyan: #22d3ee;
  --cyber-sidebar-width: 260px;
  --cyber-nav-height: 64px;
}

[data-theme="light"] {
  --cyber-bg-primary: #f5f7fa;
  --cyber-accent-green: #16a34a;
  --cyber-accent-cyan: #0891b2;
}
```

> **提示**：自定义布局的主题变量命名建议以布局名做前缀（如 `--aurora-xxx`），而非 `--cyber-xxx`，避免与 CyberLayout 冲突。具体规则见上方「CSS Token 规范」章节。

## 布局插槽

| 插槽名    | 说明                 |
| --------- | -------------------- |
| `default` | 主内容区（页面内容） |
| `sidebar` | 侧边栏内容           |
| `header`  | 顶部导航内容         |

## 验证布局

1. 确保布局文件路径正确（框架 vs 用户项目）
2. 运行 `pnpm dev`
3. 检查布局是否正确渲染
4. 测试主题切换（如适用）
5. 测试响应式效果

## 布局功能实现流程

> **重要**：新增布局必须实现以下全部功能，才能成为生产级布局。
> 仅实现 UI 骨架（静态 HTML）属于"玩具"，不能上线。

### 功能清单与实现流程

#### 1. 注册布局

在 `src/main.ts` 中调用 `registerLayout` 注册布局：

```typescript
// src/main.ts
import { initApp } from '@newlifex/cube-vue/core/initApp';
import '@newlifex/cube-vue/core/global.css';
import { registerLayout } from '@newlifex/cube-vue/core/composables/useLayout';
import AuroraLayout from './layouts/AuroraLayout/index.vue';

registerLayout({
  id: 'aurora',
  label: '极光蓝绿',
  icon: '◉',
  description: '极光蓝绿风格布局',
  component: AuroraLayout,
}, true);  // true = 立即切换为当前布局

initApp();
```

#### 2. 布局入口组件（index.vue）

```vue
<template>
  <div class="aurora-layout">
    <Sidebar />
    <main class="aurora-main">
      <Navbar />
      <div class="aurora-content">
        <slot></slot>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import Sidebar from './Sidebar.vue';
import Navbar from './Navbar.vue';
</script>

<style>
@import './styles/variables.css';
</style>
```

#### 3. 侧边栏（Sidebar.vue）— 对接 menuStore + 使用 MenuItem + LogoBrand

**必须功能**：
- Logo：`<LogoBrand />` 自动读取配置中的 logo 和 system title
- 搜索框（可选）：`<SearchBar mode="box" />` 实时搜索菜单
- 从 `menuStore.treeMenus` 获取树形菜单
- 使用 `<MenuItem>` 组件渲染菜单（自动处理递归、图标、高亮、跳转）
- 加载/空状态：显示"加载菜单中..."或"暂无菜单数据"
- 底部用户区域（可选）：`<UserProfile variant="sidebar" dropup>` 含退出登录

**推荐实现**（使用 MenuItem 组件）：

```vue
<script setup lang="ts">
import { computed } from 'vue';
import { storeToRefs } from 'pinia';
import { useMenuStore } from '@newlifex/cube-vue/core/stores/menu';
import LogoBrand from '@newlifex/cube-vue/core/components/LogoBrand.vue';
import SearchBar from '@newlifex/cube-vue/core/components/SearchBar.vue';
import MenuItem from '@newlifex/cube-vue/core/components/MenuItem.vue';
import UserProfile from '@newlifex/cube-vue/core/components/UserProfile.vue';

const menuStore = useMenuStore();
const { treeMenus, activeMenu, loading, hasMenus } = storeToRefs(menuStore);
const menuGroups = computed(() => treeMenus.value ?? []);
</script>

<template>
  <aside class="sidebar">
    <!-- Logo（自动读取配置） -->
    <div class="sidebar-logo"><LogoBrand /></div>

    <!-- 搜索框 -->
    <div class="sidebar-search"><SearchBar mode="box" placeholder="搜索菜单..." /></div>

    <!-- 菜单（MenuItem 自动处理递归/图标/高亮/跳转） -->
    <nav class="sidebar-menu">
      <template v-for="group in menuGroups" :key="group.id">
        <div v-if="group.children?.length" class="menu-group">
          <div class="menu-group-title">{{ group.title || group.name }}</div>
          <MenuItem v-for="item in group.children" :key="item.id" :menu="item" :activeMenu="activeMenu" />
        </div>
      </template>
      <div v-if="!hasMenus" class="menu-empty">{{ loading ? '加载菜单中...' : '暂无菜单数据' }}</div>
    </nav>

    <!-- 底部用户区域 -->
    <div class="sidebar-user">
      <UserProfile variant="sidebar" dropup>
        <template #extra-options>
          <ModeSwitcher />
          <ThemeSwitcher />
          <LayoutSwitcher />
          <NotificationBell />
        </template>
      </UserProfile>
    </div>
  </aside>
</template>
```

`MenuItem` 自动处理了菜单的所有交互（展开/折叠、递归、图标、高亮、跳转），无需手写。

**仅当需要自定义交互时**才手写菜单（不推荐）：

#### 4. 顶部导航（Navbar.vue）— 对接 userStore + menuStore + 框架组件

**必须功能**：
- 页面标题：从 `menuStore.activeMenu.title` 获取
- 用户信息：`<UserProfile variant="navbar" />` 自动对接 `userStore.userInfo`
- 布局切换器：`<LayoutSwitcher />`
- 主题切换器：`<ThemeSwitcher />`
- 模式切换器：`<ModeSwitcher />`
- 通知按钮：`<NotificationBell />`
- 搜索框（可选）：`<SearchBar mode="icon" />`

**完整示例**：

```vue
<script setup lang="ts">
import { computed } from 'vue';
import { useMenuStore } from '@newlifex/cube-vue/core/stores/menu';
import LayoutSwitcher from '@newlifex/cube-vue/core/components/LayoutSwitcher.vue';
import ThemeSwitcher from '@newlifex/cube-vue/core/components/ThemeSwitcher.vue';
import ModeSwitcher from '@newlifex/cube-vue/core/components/ModeSwitcher.vue';
import NotificationBell from '@newlifex/cube-vue/core/components/NotificationBell.vue';
import UserProfile from '@newlifex/cube-vue/core/components/UserProfile.vue';
import SearchBar from '@newlifex/cube-vue/core/components/SearchBar.vue';

const menuStore = useMenuStore();
const pageTitle = computed(() => menuStore.activeMenu?.title || menuStore.activeMenu?.name || '仪表盘');
</script>

<template>
  <header class="navbar">
    <!-- 左侧标题 -->
    <div class="navbar-left">
      <h2>{{ pageTitle }}</h2>
    </div>

    <!-- 右侧工具栏 -->
    <div class="navbar-right">
      <SearchBar mode="icon" />
      <ModeSwitcher />
      <ThemeSwitcher />
      <LayoutSwitcher />
      <NotificationBell />
      <UserProfile variant="navbar" />
    </div>
  </header>
</template>
```

#### 5. 面包屑（可选，在 Navbar 或独立组件中实现）

基于 `activeMenu.parentMenu` 反向指针链向上递推：

```typescript
const breadcrumbPath = computed(() => {
  if (!activeMenu.value) return [];
  const path = [];
  let current = activeMenu.value;
  while (current) {
    path.unshift({ name: current.name, title: current.title });
    current = current.parentMenu;
  }
  return path;
});
```

#### 6. 主题变量（variables.css）

基于框架 CSS Token 定义布局专属变量：

```css
:root,
[data-theme='aurora'] {
  /* 布局尺寸 */
  --aurora-sidebar-width: 220px;
  --aurora-nav-height:    60px;
  --aurora-content-padding: 24px;

  /* 布局颜色（基于 Layer 1 派生） */
  --aurora-bg:             var(--bg-primary);
  --aurora-sidebar-bg:     var(--bg-secondary);
  --aurora-nav-bg:         var(--bg-secondary);
  --aurora-text-primary:   var(--text-primary);
  --aurora-text-secondary: var(--text-secondary);
  --aurora-text-muted:     var(--text-muted);
  --aurora-border:         var(--border-default);
  --aurora-sidebar-border: var(--border-default);

  /* 品牌渐变 */
  --aurora-logo-gradient:  linear-gradient(135deg, var(--blue-600), var(--green-500));
  --aurora-primary-gradient: linear-gradient(135deg, var(--blue-600), var(--green-500));

  /* 圆角 */
  --aurora-radius-sm: 8px;
  --aurora-radius-md: 12px;

  /* 动画 */
  --aurora-transition: all 0.2s ease;
}
```

#### 7. CSS Token 使用规范

| 规则                                | 说明                                  |
| ----------------------------------- | ------------------------------------- |
| ✅ 必须引用 Layer 1 语义变量         | `background: var(--bg-primary)`       |
| ✅ 布局专属变量以 `--{布局名}-` 前缀 | `--aurora-sidebar-width`              |
| ✅ 基于 Layer 1 派生布局颜色         | `--aurora-bg: var(--bg-primary)`      |
| ❌ 禁止硬编码色值                    | 除非是设计原语（Layer 0）且需注释说明 |

### 功能完整性检查清单

| 功能           | 组件/方式                                  | 数据来源                     | 说明                      |
| -------------- | ------------------------------------------ | ---------------------------- | ------------------------- |
| 布局注册       | `registerLayout()`                         | `useLayout()`                | `main.ts` 中调用          |
| Logo/标题      | `<LogoBrand />`                            | `config.base.logo` + `title` | 自动读取配置              |
| 菜单动态数据   | `<MenuItem />`                             | `menuStore.treeMenus`        | 递归渲染，非静态数据      |
| 菜单图标       | `MenuItem` 内置                            | `menu.icon` → Element Plus   | 支持 EP 图标 + 默认兜底   |
| 活跃菜单高亮   | `MenuItem` 内置                            | `menuStore.activeMenu`       | 自动高亮当前项 + 祖先     |
| 菜单搜索       | `<SearchBar mode="box" />`                 | `menuStore.flatMenus`        | 模糊搜索 + 面包屑 + 跳转  |
| 用户信息       | `<UserProfile variant="navbar" />`         | `userStore.userInfo`         | 头像/用户名/退出登录      |
| 页面标题       | `menuStore.activeMenu.title`               | `menuStore`                  | 面包屑或标题显示          |
| 布局切换器     | `<LayoutSwitcher />`                       | `useLayout()`                | 多布局时自动显示          |
| 主题切换器     | `<ThemeSwitcher />`                        | `useTheme()`                 | 主题选择下拉              |
| 模式切换器     | `<ModeSwitcher />`                         | `useTheme().toggleMode()`    | 明/暗模式切换             |
| 通知按钮       | `<NotificationBell />`                     | 无                           | UI 占位，可扩展           |
| 用户头像下拉   | `<UserProfile variant="sidebar" dropup />` | `userStore`                  | 侧边栏底部 + 额外选项插槽 |
| 面包屑导航     | 手动基于 `parentMenu` 链                   | `activeMenu.parentMenu`      | 沿反向指针递推            |
| CSS Token 变量 | `variables.css`                            | Layer 1 派生                 | `--aurora-xxx` 命名前缀   |

> **关键**：所有展示用户/菜单/Logo 的组件都已对接真实 Store 或配置，**无需手动获取数据**。

### 关键框架组件清单

| 组件               | 路径                                   | 对接数据                      | 用途                                    |
| ------------------ | -------------------------------------- | ----------------------------- | --------------------------------------- |
| `LogoBrand`        | `core/components/LogoBrand.vue`        | `config.base.logo` + `.title` | Logo 图片 + 系统标题                    |
| `MenuItem`         | `core/components/MenuItem.vue`         | `menuStore`                   | 树形菜单（递归/图标/高亮/跳转）         |
| `SearchBar`        | `core/components/SearchBar.vue`        | `menuStore.flatMenus`         | 菜单搜索（关键词→结果→跳转）            |
| `LayoutSwitcher`   | `core/components/LayoutSwitcher.vue`   | `useLayout()`                 | 布局切换下拉                            |
| `ThemeSwitcher`    | `core/components/ThemeSwitcher.vue`    | `useTheme()`                  | 主题选择下拉                            |
| `ModeSwitcher`     | `core/components/ModeSwitcher.vue`     | `useTheme()`                  | 明/暗模式切换                           |
| `NotificationBell` | `core/components/NotificationBell.vue` | 无                            | 通知铃铛                                |
| `UserProfile`      | `core/components/UserProfile.vue`      | `userStore.userInfo`          | 用户头像下拉（navbar/sidebar 两种变体） |
| `UserAvatar`       | `core/components/UserAvatar.vue`       | `userStore.userInfo`          | 用户头像首字母（轻量无下拉）            |
| `ActionButton`     | `core/components/ActionButton.vue`     | 无                            | 通用图标按钮基类                        |
| `SwitcherDropdown` | `core/components/SwitcherDropdown.vue` | 无                            | 通用下拉切换器                          |

### 关键框架 Store

| Store          | 路径                                      | 用途             |
| -------------- | ----------------------------------------- | ---------------- |
| `useMenuStore` | `@newlifex/cube-vue/core/stores/menu.ts`          | 菜单树、活跃菜单 |
| `useUserStore` | `@newlifex/cube-vue/core/stores/user.ts`          | 用户信息、登出   |
| `useTheme`     | `@newlifex/cube-vue/core/composables/useTheme.ts` | 主题切换         |

## 场景判断速查

| 需求           | 在哪里新增                 | 是否需要 registerLayout                         |
| -------------- | -------------------------- | ----------------------------------------------- |
| 仅当前项目使用 | 用户项目 `src/layouts/`    | 是，`registerLayout(option, true)` 切换为当前   |
| 所有应用共用   | @newlifex/cube-vue `core/layouts/` | 是，在 `core/main.ts` 中注册                    |
| 设为框架默认   | @newlifex/cube-vue `core/layouts/` | 是，`registerLayout(option, true)` 设为当前布局 |

> **核心规则**：无论哪种场景，都必须调用 `registerLayout` 才能让布局生效。
> `RootLayout.vue` 只读取 `useLayout` 的 `currentComponent`，不读取 `provide(LayoutKey)`。

---

# 布局实现完整流程总结

> 本文档以 **AuroraLayout（极光蓝绿）** 的实际实现过程为例，完整记录从零到生产可用的全部步骤、复用的框架组件、菜单对接方式、踩坑记录与最佳实践。
> 后续新增布局时，按此流程执行即可避免常见错误。

---

## 一、整体流程概览

```
需求分析 → 研究框架 → 设计 CSS Token → 创建目录结构
  → 实现 index.vue → 实现 Sidebar（对接 menuStore）
  → 实现 Navbar（对接 userStore + 框架组件）
  → 注册布局（registerLayout）→ 构建验证 → 浏览器验证
```

| 阶段                | 产出                                       | 耗时参考 |
| ------------------- | ------------------------------------------ | -------- |
| 需求分析 + 研究框架 | 确定复用哪些框架组件/Store                 | 30 分钟  |
| CSS Token 设计      | `variables.css`                            | 20 分钟  |
| 组件实现            | `index.vue` + `Sidebar.vue` + `Navbar.vue` | 1-2 小时 |
| 注册 + 验证         | `main.ts` 修改 + 构建 + 浏览器检查         | 30 分钟  |

---

## 二、复用的框架组件清单

以下组件均来自 `@newlifex/cube-vue/core/components/`，**无需自行实现**，直接 import 使用即可。

### 可复用的框架内置组件

以下组件均来自 `@newlifex/cube-vue/core/components/`，**无需自行实现**，直接 import 使用即可。
所有组件都已对接真实数据（menuStore / userStore / config），不是静态 UI。

| 组件               | 路径                                   | 对接数据                           | 用途                                                                    |
| ------------------ | -------------------------------------- | ---------------------------------- | ----------------------------------------------------------------------- |
| `LogoBrand`        | `core/components/LogoBrand.vue`        | `getConfig().base.logo` + `.title` | Logo 图片 + 系统标题，props: `collapsed`                                |
| `MenuItem`         | `core/components/MenuItem.vue`         | `menuStore` 递归渲染               | 树形菜单项，支持 Element Plus 图标，props: `menu` `depth` `activeMenu`  |
| `SearchBar`        | `core/components/SearchBar.vue`        | `menuStore.flatMenus` 模糊搜索     | 菜单搜索框（`mode="box"`）/ 图标按钮（`mode="icon"`）                   |
| `UserProfile`      | `core/components/UserProfile.vue`      | `userStore.userInfo`               | 用户头像 + 下拉菜单（个人资料/退出登录），variant: `navbar` / `sidebar` |
| `UserAvatar`       | `core/components/UserAvatar.vue`       | `userStore.userInfo`               | 仅用户头像首字母 + 名称，props: `size` `showName`                       |
| `LayoutSwitcher`   | `core/components/LayoutSwitcher.vue`   | `useLayout()`                      | 布局切换下拉（仅多布局时显示）                                          |
| `ThemeSwitcher`    | `core/components/ThemeSwitcher.vue`    | `useTheme()`                       | 主题选择下拉                                                            |
| `ModeSwitcher`     | `core/components/ModeSwitcher.vue`     | `useTheme().toggleMode()`          | 明/暗模式切换按钮                                                       |
| `NotificationBell` | `core/components/NotificationBell.vue` | 无（UI 占位）                      | 通知铃铛按钮                                                            |
| `ActionButton`     | `core/components/ActionButton.vue`     | 无                                 | 通用图标按钮基类                                                        |

### 2.1 LogoBrand — Logo 和系统标题（自动读取配置）

```vue
<script setup lang="ts">
import LogoBrand from '@newlifex/cube-vue/core/components/LogoBrand.vue';
</script>

<template>
  <!-- 折叠时隐藏标题 -->
  <LogoBrand :collapsed="false" />
</template>
```

| 特性     | 说明                                                                      |
| -------- | ------------------------------------------------------------------------- |
| 数据来源 | `getConfig().base.logo` — logo 图片 URL/路径                              |
|          | `getConfig().base.title` — 系统标题文字                                   |
| 配置位置 | `configs/config.ts` → `base: { logo: '/favicon.svg', title: '系统名称' }` |
| 图片处理 | 自动计算宽高比，正方形图片隐藏标题避免重复                                |
| Props    | `collapsed?: boolean` — 折叠模式隐藏标题                                  |
| 无需传参 | 不传 `collapsed` 时默认展开显示                                           |

### 2.2 MenuItem — 树形菜单组件（动态数据 + 图标）

```vue
<script setup lang="ts">
import MenuItem from '@newlifex/cube-vue/core/components/MenuItem.vue';
import { useMenuStore } from '@newlifex/cube-vue/core/stores/menu';
import { storeToRefs } from 'pinia';

const menuStore = useMenuStore();
const { activeMenu } = storeToRefs(menuStore);
</script>

<template>
  <MenuItem v-for="item in topMenus" :key="item.id" :menu="item" :activeMenu="activeMenu" />
</template>
```

| 特性      | 说明                                                                                       |
| --------- | ------------------------------------------------------------------------------------------ |
| 数据来源  | `props.menu` — `TreeMenuItem` 对象                                                         |
| 递归渲染  | 自动递归 `menu.children`，每层自动缩进                                                     |
| 图标支持  | **Element Plus 图标**（优先）→ **内联 SVG**（fallback）→ **默认 Menu 图标**                |
| 图标匹配  | icon 字段值（如 `"User"`）→ `@element-plus/icons-vue` 中查找 PascalCase 匹配               |
| 展开/折叠 | 有子菜单时点击切换展开                                                                     |
| 激活高亮  | 根据 `activeMenu` 自动判断当前项和祖先高亮                                                 |
| 点击跳转  | 叶子节点调用 `openMenuTab()` + `menuStore.setActiveMenu()`                                 |
| Props     | `menu: TreeMenuItem` — 菜单项数据                                                          |
|           | `depth?: number` — 缩进层级（默认 0）                                                      |
|           | `activeMenu?: TreeMenuItem` — 当前活跃菜单                                                 |
| CSS 变量  | `--radius-sm` `--sidebar-item-hover` `--sidebar-item-active` `--accent` `--text-secondary` |

### 2.3 SearchBar — 菜单搜索框（对接 menuStore 真实菜单）

```vue
<script setup lang="ts">
import SearchBar from '@newlifex/cube-vue/core/components/SearchBar.vue';
</script>

<template>
  <!-- 搜索框模式：输入文字弹出搜索结果下拉 -->
  <SearchBar mode="box" placeholder="搜索菜单..." />

  <!-- 图标模式：仅显示搜索图标按钮 -->
  <SearchBar mode="icon" />
</template>
```

| 特性     | 说明                                                                 |
| -------- | -------------------------------------------------------------------- |
| 数据来源 | `menuStore.flatMenus` — 所有平铺菜单（含无路径的父级）               |
| 匹配规则 | 按 `name` 和 `title` 模糊匹配，取前 10 条                            |
| 结果展示 | 显示匹配菜单名 + 面包屑路径（`父级 › 当前`）                         |
| 高亮     | 匹配字符用 `<mark>` 标签高亮                                         |
| 点击行为 | 调用 `openMenuTab()` 跳转 + `menuStore.setActiveMenu()` 更新活跃状态 |
| Props    | `mode: 'icon' \| 'box'` — 显示模式                                   |
|          | `placeholder?: string` — 输入框占位文字                              |

### 2.4 UserProfile — 用户头像下拉（真实用户数据）

```vue
<script setup lang="ts">
import UserProfile from '@newlifex/cube-vue/core/components/UserProfile.vue';
</script>

<template>
  <!-- 顶部栏模式：下拉向下 -->
  <UserProfile variant="navbar" />

  <!-- 侧边栏底部模式：下拉向上 -->
  <UserProfile variant="sidebar" dropup />
</template>
```

| 特性         | 说明                                                                  |
| ------------ | --------------------------------------------------------------------- |
| **数据来源** | `userStore.userInfo.displayName` → 用户名                             |
|              | `userStore.userInfo.id` → 头像 URL `${baseUrl}/Cube/Avatar/${userId}` |
| **真实可用** | ✅ 登录后自动填充，无需手动获取                                        |
| 头像降级     | 加载失败时显示用户名首字母大写                                        |
| 内置操作     | 「个人资料」→ `/profile`；「退出登录」→ `userStore.logout()`          |
| Slots        | `#extra-options` — 在用户信息卡之前插入自定义内容                     |
| Props        | `variant: 'navbar' \| 'sidebar'` — 显示变体                           |
|              | `dropup: boolean` — 是否向上弹出（sidebar 模式用）                    |

> ⚠️ 注意：组件名为 `UserProfile`，**不是** `NavbarUserProfile`。CyberLayout 的 Navbar 中引用 `NavbarUserProfile` 是旧代码遗留问题，实际该文件不存在，新布局应直接使用 `UserProfile`。

### 2.5 LayoutSwitcher — 布局切换下拉

```vue
<script setup lang="ts">
import LayoutSwitcher from '@newlifex/cube-vue/core/components/LayoutSwitcher.vue';
</script>

<template>
  <LayoutSwitcher />
</template>
```

| 特性     | 说明                                                     |
| -------- | -------------------------------------------------------- |
| 自动隐藏 | 仅注册了 1 个布局时不显示（`v-if="layouts.length > 1"`） |
| 数据来源 | 读取 `useLayout().layouts` 和 `currentLayoutId`          |
| 切换逻辑 | 内部调用 `useLayout().setLayout(layoutId)`               |
| Props    | `onChange?: (layoutId: string) => void` — 自定义切换回调 |
| 无需传参 | 默认行为即可满足绝大多数场景                             |

### 2.6 ThemeSwitcher — 主题选择下拉

```vue
<script setup lang="ts">
import ThemeSwitcher from '@newlifex/cube-vue/core/components/ThemeSwitcher.vue';
</script>

<template>
  <ThemeSwitcher />
</template>
```

| 特性     | 说明                                                                |
| -------- | ------------------------------------------------------------------- |
| 主题来源 | 读取框架已注册的主题列表                                            |
| 切换逻辑 | 修改 `document.documentElement.setAttribute('data-theme', themeId)` |
| Props    | 无必填参数，开箱即用                                                |

### 2.7 ModeSwitcher — 明暗模式切换

```vue
<script setup lang="ts">
import ModeSwitcher from '@newlifex/cube-vue/core/components/ModeSwitcher.vue';
</script>

<template>
  <ModeSwitcher />
</template>
```

| 特性  | 说明                            |
| ----- | ------------------------------- |
| 模式  | `light` / `dark` / 跟随系统     |
| 存储  | 写入 `localStorage`，刷新后保持 |
| Props | 无必填参数                      |

### 2.8 NotificationBell — 通知铃铛

```vue
<script setup lang="ts">
import NotificationBell from '@newlifex/cube-vue/core/components/NotificationBell.vue';
</script>

<template>
  <NotificationBell show-label @click="handleClick" />
</template>
```

| 特性  | 说明                                                  |
| ----- | ----------------------------------------------------- |
| 图标  | 铃铛 SVG，内置样式                                    |
| Props | `showLabel?: boolean`（默认 false）、`label?: string` |
| 事件  | `@click` — 点击时触发                                 |
| 扩展  | 后续可接入真实通知 API，当前仅 UI 占位                |

### 2.9 UserAvatar — 用户头像首字母（轻量级）

```vue
<script setup lang="ts">
import UserAvatar from '@newlifex/cube-vue/core/components/UserAvatar.vue';
</script>

<template>
  <UserAvatar size="small" :showName="true" variant="default" />
</template>
```

| 特性               | 说明                                                    |
| ------------------ | ------------------------------------------------------- |
| 数据来源           | `userStore.userInfo.displayName` 首字母 + 全名          |
| Props              | `size: 'small' \| 'medium' \| 'large'`（28/32/40px）    |
|                    | `showName?: boolean`                                    |
|                    | `variant: 'default' \| 'green'`                         |
|                    | `onClick?: () => void`                                  |
| 对比 `UserProfile` | `UserAvatar` 轻量无下拉，`UserProfile` 包含完整下拉菜单 |

---

## 三、菜单对接方式（Sidebar + Navbar）

### 3.1 使用的 Store

```typescript
import { useMenuStore, type TreeMenuItem } from '@newlifex/cube-vue/core/stores/menu';
import { useUserStore } from '@newlifex/cube-vue/core/stores/user';
```

| Store          | 关键字段     | 类型                   | 说明                                |
| -------------- | ------------ | ---------------------- | ----------------------------------- |
| `useMenuStore` | `treeMenus`  | `TreeMenuItem[]`       | 树形菜单列表（顶层节点 = 菜单分组） |
| `useMenuStore` | `activeMenu` | `TreeMenuItem \| null` | 当前选中的菜单项                    |
| `useMenuStore` | `loading`    | `boolean`              | 菜单是否正在加载                    |
| `useMenuStore` | `hasMenus`   | `boolean`              | 是否有菜单数据                      |
| `useUserStore` | `userInfo`   | `UserInfo \| null`     | 当前用户信息                        |

### 3.2 TreeMenuItem 数据结构

```typescript
interface TreeMenuItem {
  id: string;           // 菜单项唯一标识
  name: string;         // 路由名称（用于 router-link 的 name）
  title: string;        // 显示标题
  path: string;         // 路由路径（用于 router-link 的 to）
  icon?: string;        // 图标（emoji 或图标类名）
  parentId?: string;    // 父菜单 ID
  parentMenu?: TreeMenuItem;  // 父菜单对象引用（面包屑用）
  children?: TreeMenuItem[]; // 子菜单
}
```

### 3.3 Sidebar 菜单渲染模式

**数据来源**：`menuStore.treeMenus`（顶层节点作为分组，`children` 作为组内菜单项）

**推荐使用 `MenuItem` 组件**（无需手写菜单逻辑）：

```vue
<script setup lang="ts">
import { storeToRefs } from 'pinia';
import { useMenuStore } from '@newlifex/cube-vue/core/stores/menu';
import MenuItem from '@newlifex/cube-vue/core/components/MenuItem.vue';

const menuStore = useMenuStore();
const { treeMenus, activeMenu } = storeToRefs(menuStore);
const menuGroups = computed(() => treeMenus.value ?? []);
</script>

<template>
  <nav>
    <template v-for="group in menuGroups" :key="group.id">
      <div v-if="group.children?.length" class="menu-group">
        <div class="menu-group-title">{{ group.title || group.name }}</div>
        <MenuItem
          v-for="item in group.children"
          :key="item.id"
          :menu="item"
          :activeMenu="activeMenu"
        />
      </div>
    </template>
    <div v-if="!hasMenus" class="menu-empty">
      {{ loading ? '加载菜单中...' : '暂无菜单数据' }}
    </div>
  </nav>
</template>
```

`MenuItem` 自动处理：
- **递归渲染**子菜单（含缩进）
- **展开/折叠**切换
- **激活高亮**（当前项 + 祖先链路）
- **图标渲染**（Element Plus → SVG → 默认 Menu 图标）
- **路由跳转**（调 `openMenuTab()` + `setActiveMenu()`）

如果不需要分组标题或需要自定义交互（如分组折叠），可参考 CyberLayout 的直接使用 `MenuItem` 方式：

```vue
<MenuItem v-for="menu in menuList" :key="menu.id" :menu="menu" :activeMenu="activeMenu" />
```

**手写菜单（不推荐，仅当需要特殊交互时）**：

### 3.4 Navbar 面包屑（基于 parentMenu 链）

```typescript
// Navbar.vue
const activeMenu = computed(() => menuStore.activeMenu);

const breadcrumbPath = computed(() => {
  const menu = activeMenu.value;
  if (!menu) return [];
  const path: Array<{ name: string; title?: string }> = [];
  let current: TreeMenuItem | undefined = menu;
  while (current) {
    path.unshift({ name: current.name, title: current.title });
    current = current.parentMenu; // 沿 parentMenu 反向指针向上递推
  }
  return path;
});
```

```vue
<template>
  <nav class="breadcrumb">
    <template v-for="(item, idx) in breadcrumbPath" :key="idx">
      <span v-if="idx < breadcrumbPath.length - 1" class="bc-item">{{ item.title }}</span>
      <span v-if="idx < breadcrumbPath.length - 1" class="bc-sep">›</span>
      <span v-else class="bc-current">{{ item.title }}</span>
    </template>
  </nav>
</template>
```

> **关键点**：`parentMenu` 是框架在构建菜单树时自动填充的反向指针，无需手动维护。

---

## 四、CSS Token 三层架构（完整规范）

### Layer 0 — 设计原语（框架定义，布局不应重复定义）

```css
/* 框架全局定义，布局直接引用 */
--blue-600: #2563eb;
--green-500: #22c55e;
--gray-50: #f9fafb;
--gray-100: #f3f4f6;
--gray-200: #e5e7eb;
--gray-300: #d1d5db;
--gray-400: #9ca3af;
--gray-500: #6b7280;
--gray-600: #4b5563;
--gray-700: #374151;
--gray-800: #1f2937;
--gray-900: #111827;
/* ... 更多原语 */
```

### Layer 1 — 语义变量（框架定义，布局直接引用）

```css
--bg-primary:      #ffffff;     /* 亮色主背景 */
--bg-secondary:    #f9fafb;     /* 亮色次背景 */
--text-primary:    #111827;
--text-secondary:  #6b7280;
--text-muted:      #9ca3af;
--border-default:  #e5e7eb;
```

### Layer 2 — 布局专属变量（在 `variables.css` 中定义）

```css
/* layouts/AuroraLayout/styles/variables.css */
:root,
[data-theme='aurora'] {
  /* 尺寸 */
  --aurora-sidebar-width: 220px;
  --aurora-nav-height:    60px;
  --aurora-content-padding: 24px;

  /* 颜色（基于 Layer 1 派生，禁止硬编码） */
  --aurora-bg:             var(--bg-primary);
  --aurora-sidebar-bg:     var(--bg-secondary);
  --aurora-nav-bg:         var(--bg-secondary);
  --aurora-text-primary:   var(--text-primary);
  --aurora-text-secondary: var(--text-secondary);
  --aurora-text-muted:     var(--text-muted);
  --aurora-border:         var(--border-default);
  --aurora-sidebar-border: var(--border-default);

  /* 品牌渐变 */
  --aurora-logo-gradient:  linear-gradient(135deg, var(--blue-600), var(--green-500));
  --aurora-primary-gradient: linear-gradient(135deg, var(--blue-600), var(--green-500));

  /* 圆角 */
  --aurora-radius-sm: 8px;
  --aurora-radius-md: 12px;

  /* 动画 */
  --aurora-transition: all 0.2s ease;
}
```

### 命名规范

| 规则                                            | 示例                            |
| ----------------------------------------------- | ------------------------------- |
| 布局专属变量必须以布局名做前缀                  | `--aurora-sidebar-width`        |
| 禁止使用框架布局的前缀                          | 不要用 `--cyber-xxx`，避免冲突  |
| 颜色优先引用 Layer 1 语义变量                   | `background: var(--bg-primary)` |
| 禁止硬编码色值（除非是 Layer 0 原语且注释说明） | —                               |

---

## 五、目录结构规范

```
src/layouts/{LayoutName}/
├── index.vue              # 布局入口（组合 Sidebar + Navbar + slot）
├── Sidebar.vue            # 侧边栏（必须对接 menuStore）
├── Navbar.vue             # 顶部导航（必须对接 userStore + 框架组件）
├── styles/
│   └── variables.css      # 布局专属 CSS Token（Layer 2）
└── README.md              # 可选：布局说明文档
```

> **注意**：`variables.css` 使用 `@import` 导入（非 scoped），确保变量定义在 `:root` 作用域，供所有布局子组件引用。

---

## 六、注册布局（main.ts）

```typescript
// src/main.ts
import { initApp } from '@newlifex/cube-vue/core/initApp';
import '@newlifex/cube-vue/core/global.css';
import { registerLayout } from '@newlifex/cube-vue/core/composables/useLayout';
import AuroraLayout from './layouts/AuroraLayout/index.vue';

registerLayout({
  id: 'aurora',
  label: '极光蓝绿',
  icon: '◉',
  description: '极光蓝绿风格布局',
  component: AuroraLayout,
}, true);  // true = 立即切换为当前布局

initApp();
```

| 参数              | 说明                                          |
| ----------------- | --------------------------------------------- |
| `id`              | 布局唯一标识，用于 `data-theme` 属性          |
| `label`           | 布局切换器中显示的中文名称                    |
| `icon`            | 布局切换器中显示的图标（emoji 或 SVG）        |
| `description`     | 布局描述（可选）                              |
| `component`       | 布局 Vue 组件                                 |
| 第二个参数 `true` | 注册后立即设为当前布局（首次注册建议传 true） |

> ⚠️ **踩坑记录**：早期尝试通过 `provide(LayoutKey, ...)` 注册布局，但 `RootLayout.vue` 只读取 `useLayout().currentComponent`（模块级状态），不读取 injected 值。正确方式是通过 `registerLayout()` 函数修改 `registeredLayouts` 数组。

---

## 七、各组件职责与代码结构

### 7.1 index.vue — 布局入口

**职责**：组合 Sidebar + Navbar + 内容区，导入 CSS 变量。

```vue
<script setup lang="ts">
import Sidebar from './Sidebar.vue';
import Navbar from './Navbar.vue';
</script>

<template>
  <div class="aurora-layout">
    <Sidebar />
    <main class="aurora-main">
      <Navbar />
      <div class="aurora-content">
        <slot></slot>
      </div>
    </main>
  </div>
</template>

<style>
@import './styles/variables.css';  /* 非 scoped，定义 :root 变量 */
</style>

<style scoped lang="scss">
.aurora-layout { display: flex; min-height: 100vh; }
.aurora-main   { margin-left: var(--aurora-sidebar-width); flex: 1; }
.aurora-content { flex: 1; padding: var(--aurora-content-padding); }
</style>
```

### 7.2 Sidebar.vue — 动态侧边栏

**职责**：展示 Logo + 搜索框 + 动态菜单树 + 活跃状态高亮。

**关键逻辑**：
- Logo：使用 `<LogoBrand />`，自动从 `configs/config.ts` 的 `base.logo` 和 `base.title` 读取
- 搜索框（可选）：使用 `<SearchBar mode="box" placeholder="搜索菜单..." />`，支持菜单模糊搜索
- 菜单：使用 `<MenuItem>` 组件（`menuStore.treeMenus` 动态数据），自动处理递归渲染、图标、高亮、跳转
- 底部用户区域（可选）：使用 `<UserProfile variant="sidebar" dropup>`，含头像/用户名/退出登录
- 加载中/无数据时显示友好提示

### 7.3 Navbar.vue — 顶部导航栏

**职责**：面包屑 + 布局/主题/模式切换 + 通知 + 用户头像下拉。

**关键逻辑**：
- 面包屑：沿 `activeMenu.parentMenu` 链向上递推
- 右侧工具栏：`LayoutSwitcher` + `ThemeSwitcher` + `ModeSwitcher` + `NotificationBell` + `UserProfile`
- 不直接读取 `userStore`，用户信息由 `UserProfile` 组件内部处理

---

## 八、常见踩坑与解决方案

| 坑                           | 表现                       | 原因                                         | 解决方案                                                           |
| ---------------------------- | -------------------------- | -------------------------------------------- | ------------------------------------------------------------------ |
| `provide(LayoutKey)` 不生效  | 布局切换器显示但切换无反应 | `RootLayout.vue` 不读取 injected 值          | 使用 `registerLayout()` 函数                                       |
| `NavbarUserProfile` 导入失败 | 编译错误：模块不存在       | 组件实际名为 `UserProfile.vue`               | 改为 `import UserProfile from '.../UserProfile.vue'`               |
| 侧边栏菜单不更新             | 路由切换后高亮状态不变     | 使用了静态数据而非 store                     | 使用 `<MenuItem>` 组件或改为 `computed(() => menuStore.treeMenus)` |
| CSS 变量不生效               | 样式无变化                 | `variables.css` 用了 `scoped` style          | `@import` 必须在非 scoped 的 `<style>` 中                          |
| 布局切换器不显示             | 只看到一个布局选项         | 只注册了一个布局                             | 注册多个布局后自动显示                                             |
| 面包屑为空                   | 导航栏无面包屑             | `activeMenu` 为 null                         | 确保路由跳转后 menuStore 已更新 activeMenu                         |
| 手写图标/菜单/用户信息       | 需要自行处理各种边缘情况   | 未使用框架内置组件                           | 使用 `MenuItem`/`LogoBrand`/`UserProfile` 等现成组件               |
| 用户头像不显示               | 头像图片 404 或加载失败    | `baseUrl` 未正确配置                         | `UserProfile` 自动拼接 `${baseUrl}/Cube/Avatar/${userId}`          |
| 菜单图标不显示               | 只有文字无图标             | 未导入 Element Plus icons 或 icon 字段不匹配 | `MenuItem` 已内置 EP 图标解析，设置正确的 icon 字段值即可          |

---

## 九、验证清单

构建通过后，按以下步骤在浏览器中验证：

- [ ] 布局正确渲染（侧边栏 + 顶部栏 + 内容区）
- [ ] 侧边栏 Logo 显示配置中的 logo 图片和系统标题
- [ ] 侧边栏搜索框可搜索菜单并跳转
- [ ] 侧边栏显示动态菜单（从后端加载，非静态数据）
- [ ] 点击菜单项后高亮状态正确切换
- [ ] 菜单图标正常显示（Element Plus 图标/默认 Menu 图标）
- [ ] 路由跳转正常
- [ ] 面包屑随路由变化正确更新
- [ ] 右上角显示当前登录用户的头像/名称
- [ ] 用户头像下拉菜单有「个人资料」和「退出登录」
- [ ] 布局切换器显示（注册多个布局时）
- [ ] 主题切换器可切换主题
- [ ] 明暗模式切换正常
- [ ] 通知铃铛可点击
- [ ] 响应式：窗口缩小时侧边栏行为正常（如折叠）
- [ ] 刷新页面后布局/主题/模式状态保持

---

## 十、从原型 HTML 到生产组件的转换要点

如果已有设计原型（HTML/CSS），按以下步骤转换：

1. **提取设计 Token**：从原型中提取颜色、间距、圆角等，映射到 CSS 变量
2. **保留三层架构**：原型色值 → Layer 0 原语 / Layer 1 语义 / Layer 2 布局专属
3. **拆分组件**：原型 HTML → `index.vue`（骨架）+ `Sidebar.vue` + `Navbar.vue`
4. **替换静态数据**：原型中的示例菜单 → `menuStore.treeMenus`
5. **接入框架组件**：原型中的按钮/图标 → `LayoutSwitcher` / `ThemeSwitcher` 等
6. **保留交互逻辑**：折叠/展开/下拉等交互，参考 CyberLayout 的实现方式

---
---
name: cube-layout
description: |
  在 cube-front 框架中新增、注册或切换页面布局。
  当用户说"新增布局"、"自定义布局"、"修改布局"、"切换布局"、"使用 XXX 布局"时使用。
  包含布局组件创建、框架注册、应用使用的完整流程。
---

# Cube-Front 布局系统

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
cube-front/core/layouts/CyberLayout/  ← 默认使用
cube-front/core/layouts/MainLayout/   ← 备用布局
cube-front/core/layouts/TopMenu/      ← 备用布局
```

### 方式二：应用层注册并使用自定义布局

在项目 `src/main.ts` 中调用 `registerLayout` 注册自定义布局，并设为当前布局：

```typescript
// src/main.ts
import { initApp } from 'cube-front/core/initApp';
import 'cube-front/core/global.css';
import { registerLayout } from 'cube-front/core/composables/useLayout';
import MainLayout from 'cube-front/core/layouts/MainLayout';

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

> **重要**：布局样式**必须**使用 CSS 自定义属性（Token 变量），**禁止硬编码色值**。
> 框架 `global.css` 提供了完整的三层 Token 体系，布局组件直接引用即可。

### 三层 Token 架构

```
┌──────────────────────────────────────────┐
│  Layer 0: Primitive Tokens（设计原语）    │
│  --green-400, --blue-500, --amber-300... │  ← 不随主题变化
├──────────────────────────────────────────┤
│  Layer 1: Semantic Tokens（语义别名）     │
│  --bg-primary, --text-primary, --accent  │  ← 随主题变化
├──────────────────────────────────────────┤
│  Layer 2: Component Tokens（组件级变量）  │
│  --sidebar-bg, --navbar-text, --card-bg  │  ← 布局自己定义
└──────────────────────────────────────────┘
```

### Layer 0：设计原语（不随主题变化）

| 变量          | 值                  | 用途       |
| ------------- | ------------------- | ---------- |
| `--green-400` | `#4ade80`           | 绿色系主色 |
| `--green-600` | `#16a34a`           | 绿色强调   |
| `--blue-500`  | `#3b82f6`           | 蓝色       |
| `--blue-600`  | `#2563eb`           | 蓝色强调   |
| `--amber-400` | `#fbbf24`           | 警告色     |
| `--rose-400`  | `#fb7185`           | 错误色     |
| `--cyan-400`  | `#22d3ee`           | 青色信息色 |
| `--radius-sm` | `8px`               | 小圆角     |
| `--radius-md` | `12px`              | 中圆角     |
| `--shadow-md` | `0 4px 6px...`      | 阴影       |
| `--ease`      | `cubic-bezier(...)` | 动画曲线   |

### Layer 1：语义别名（随主题变化）

| 类别 | 变量                                                                      | 用途          |
| ---- | ------------------------------------------------------------------------- | ------------- |
| 背景 | `--bg-primary` `--bg-secondary` `--bg-card` `--bg-elevated`               | 页面/卡片背景 |
| 边框 | `--border-subtle` `--border-default` `--border-emphasis`                  | 边框色        |
| 文字 | `--text-primary` `--text-secondary` `--text-muted` `--text-inverse`       | 文字色        |
| 强调 | `--accent` `--accent-hover` `--accent-secondary` `--accent-muted`         | 品牌强调色    |
| 语义 | `--color-success` `--color-warning` `--color-error` `--color-info`        | 状态色        |
| 别名 | `--bg` `--bd` `--card` `--t1` `--t2` `--t3` `--ac` `--ac-l` `--ok` `--er` | 简写兼容      |

### Layer 1 导航栏专用变量（随主题变化）

| 变量                  | 用途           |
| --------------------- | -------------- |
| `--navbar-bg`         | 导航栏背景     |
| `--navbar-border`     | 导航栏边框     |
| `--navbar-text`       | 导航栏文字     |
| `--navbar-text-hover` | 导航栏悬停文字 |
| `--navbar-text-muted` | 导航栏次要文字 |
| `--navbar-hover-bg`   | 导航栏悬停背景 |
| `--navbar-active-bg`  | 导航栏激活背景 |

### 使用规则

| 规则                                                | 说明                                                          |
| --------------------------------------------------- | ------------------------------------------------------------- |
| ✅ **必须**引用 Layer 1 语义变量                     | `background: var(--bg-primary)` 而非 `#1e1e1e`                |
| ✅ **必须**使用 `var(--xxx)` 引用                    | `color: var(--text-primary)`                                  |
| ✅ 布局专属变量（Layer 2）以 `--{布局名}-` 前缀命名  | `--aurora-sidebar-width`、`--aurora-logo-glow`                |
| ✅ 带合理 fallback                                   | `var(--sidebar-bg, var(--bg-secondary))` 仅在旧布局兼容时使用 |
| ❌ **禁止**在布局样式中硬编码色值                    | 除非是设计原语定义（Layer 0），且需加注释说明                 |
| ❌ **禁止**在组件 `scoped style` 中覆盖 Layer 1 变量 | 全局变量在 `global.css` 中统一管理                            |

### 布局专属变量（Layer 2）定义方式

**推荐**：在布局的 `styles/variables.css` 中定义，基于 Layer 1 派生：

```css
/* styles/variables.css */
:root {
  /* 布局尺寸 */
  --aurora-sidebar-width: 220px;
  --aurora-nav-height: 60px;

  /* 布局颜色（基于 Layer 1 派生） */
  --aurora-bg: var(--bg-primary);
  --aurora-sidebar-bg: var(--bg-secondary);
  --aurora-nav-bg: var(--bg-secondary);
  --aurora-text-primary: var(--text-primary);
  --aurora-text-secondary: var(--text-secondary);
  --aurora-text-muted: var(--text-muted);
  --aurora-border: var(--border-default);
  --aurora-sidebar-border: var(--border-default);

  /* 品牌渐变 */
  --aurora-logo-gradient: linear-gradient(135deg, var(--blue-600), var(--green-500));
  --aurora-primary-gradient: linear-gradient(135deg, var(--blue-600), var(--green-500));

  /* 圆角 */
  --aurora-radius-sm: 8px;
  --aurora-radius-md: 12px;

  /* 动画 */
  --aurora-transition: all 0.2s ease;
}
```

### 示例对照

```scss
/* ❌ 错误：硬编码 */
.sidebar { background: #1e1e1e; color: #e8eaed; }

/* ✅ 正确：引用 token */
.sidebar { background: var(--bg-secondary); color: var(--text-primary); }

/* ✅ 正确：布局专属变量 */
.sidebar { background: var(--aurora-sidebar-bg, var(--bg-secondary)); }
```

---

## 新增自定义布局

> **注意**：首先判断在哪个项目新增布局：
> - **cube-front 框架项目**：布局将作为内置布局，默认被所有应用使用
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
import { initApp } from 'cube-front/core/initApp';
import 'cube-front/core/global.css';
import { registerLayout } from 'cube-front/core/composables/useLayout';
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

### 场景 B：在 cube-front 框架中新增布局

适用于需要让布局成为框架内置布局，所有应用默认使用的情况。

#### 步骤 1：创建布局组件

在 `cube-front/core/layouts/` 下创建布局目录：

```
cube-front/core/layouts/
└── YourLayout/
    ├── index.vue           # 主布局入口
    ├── Sidebar.vue         # 侧边栏（可选）
    ├── Navbar.vue          # 顶部导航栏（可选）
    └── styles/
        ├── variables.scss  # CSS 变量（可选）
        └── theme.scss      # 主题样式（可选）
```

#### 步骤 2：编写布局组件

**cube-front/core/layouts/YourLayout/index.vue**：

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

编辑 `cube-front/core/main.ts`：

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
import { initApp } from 'cube-front/core/initApp';
import 'cube-front/core/global.css';
import { registerLayout } from 'cube-front/core/composables/useLayout';
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
import { useLayout } from 'cube-front/core/composables/useLayout';

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
| `cyber`    | `cube-front/core/layouts/CyberLayout/` | 深色科技风格 + 霓虹发光 + 主题切换 |
| `main`     | `cube-front/core/layouts/MainLayout/`  | 侧边栏 + 内容区，Element Plus 风格 |
| `top-menu` | `cube-front/core/layouts/TopMenu/`     | 顶部导航栏 + 内容区                |

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
import { initApp } from 'cube-front/core/initApp';
import 'cube-front/core/global.css';
import { registerLayout } from 'cube-front/core/composables/useLayout';
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

#### 3. 侧边栏（Sidebar.vue）— 必须对接 menuStore

**必须功能**：
- 从 `useMenuStore()` 获取 `treeMenus`（树形菜单）
- 从 `useMenuStore()` 获取 `activeMenu`（当前活跃菜单）
- 菜单项高亮（`activeMenu.id === item.id`）
- 路由跳转（`<router-link>` 或 `router.push`）
- 折叠/展开状态（可选）

**错误示例**（静态数据 = 玩具）：
```typescript
// ❌ 静态数据，无法动态更新
const menuGroups = [{ title: '系统', items: [{ path: '/', label: '首页' }] }];
```

**正确示例**（对接 store + Element Plus 图标）：
```typescript
// ✅ 动态数据 + Element Plus 图标
import { computed, h } from 'vue';
import { useMenuStore, type TreeMenuItem } from 'cube-front/core/stores/menu';
import * as ElementPlusIcons from '@element-plus/icons-vue';

const menuStore = useMenuStore();
const treeMenus = computed(() => menuStore.treeMenus ?? []);
const activeMenu = computed(() => menuStore.activeMenu);

/** 将 icon 字段（如 "User" / "setting" / "menu-fold"）解析为 Element Plus 图标组件 */
function resolveMenuIcon(iconName?: string) {
  if (!iconName) return null;
  // 精确匹配（PascalCase，如 "User"、"Setting"）
  const key = iconName as keyof typeof ElementPlusIcons;
  const comp = ElementPlusIcons[key];
  if (comp) return comp;
  // 兼容小写/横线/下划线格式（如 "user" / "setting" / "menu-fold"）
  const pascal = iconName
    .split(/[-_]/)
    .map((s) => s.charAt(0).toUpperCase() + s.slice(1))
    .join('');
  return (ElementPlusIcons as Record<string, unknown>)[pascal] ?? null;
}

/** 渲染图标：优先 Element Plus 图标，降级显示 emoji 占位 */
function renderMenuIcon(iconName?: string) {
  const iconComp = resolveMenuIcon(iconName);
  if (iconComp) return h(iconComp as any, { width: 16, height: 16 });
  return iconName || '📄';
}
```

```vue
<template>
  <nav>
    <template v-for="group in menuGroups" :key="group.id">
      <div v-if="group.children?.length" class="menu-group">
        <div class="menu-group-title">{{ group.title || group.name }}</div>
        <router-link
          v-for="item in group.children"
          :key="item.id"
          :to="item.path"
          class="menu-item"
          :class="{ active: isActive(item) }"
        >
          <!-- Element Plus 图标渲染 -->
          <span class="menu-icon"><component :is="renderMenuIcon(item.icon)" /></span>
          <span class="menu-label">{{ item.title || item.name }}</span>
        </router-link>
      </div>
    </template>
    <div v-if="!hasMenus" class="menu-empty">
      {{ loading ? '加载菜单中...' : '暂无菜单数据' }}
    </div>
  </nav>
</template>
```

> **图标来源**：菜单数据中的 `icon` 字段由后端返回，通常为 Element Plus 图标名称（如 `"User"`、`"Setting"`、`"MenuFold"`）。
> 框架通过 `elSvg()` 全局注册了所有图标（前缀 `ele-`），但动态渲染场景下直接 `import * as ElementPlusIcons` 更可靠。
> 降级策略：找不到对应图标时显示原始 icon 值或 emoji 占位。

**菜单渲染参考**（CyberLayout Sidebar 模式）：
- 展开模式（220px）：完整菜单树 + 分组折叠
- 折叠模式（64px）：每组一个图标 + 飞出面板
- 飞出菜单：鼠标悬停触发，200ms 延迟关闭

#### 4. 顶部导航（Navbar.vue）— 必须对接 userStore + menuStore

**必须功能**：
- 用户信息：从 `useUserStore().userInfo` 获取（`displayName`、`avatar`）
- 页面标题：从 `useMenuStore().activeMenu` 获取（`title` 或 `name`）
- 搜索框：可先 UI 占位，后续对接搜索 API
- 通知按钮：可先 UI 占位，后续对接通知系统
- 布局切换器：`<LayoutSwitcher />` 组件
- 主题切换器：`<ThemeSwitcher />` 组件
- 模式切换器：`<ModeSwitcher />` 组件

**正确示例**：
```typescript
import { useUserStore } from 'cube-front/core/stores/user';
import { useMenuStore } from 'cube-front/core/stores/menu';
import LayoutSwitcher from 'cube-front/core/components/LayoutSwitcher.vue';
import ThemeSwitcher from 'cube-front/core/components/ThemeSwitcher.vue';
import ModeSwitcher from 'cube-front/core/components/ModeSwitcher.vue';
import NotificationBell from 'cube-front/core/components/NotificationBell.vue';
import NavbarUserProfile from 'cube-front/core/components/NavbarUserProfile.vue';

const userStore = useUserStore();
const menuStore = useMenuStore();

const currentUser = computed(() => userStore.userInfo);
const userName = computed(() => currentUser.value?.displayName || '管理员');
const pageTitle = computed(() => menuStore.activeMenu?.title || '仪表盘');
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

| 功能           | 状态 | 说明                                     |
| -------------- | ---- | ---------------------------------------- |
| 布局注册       | ⬜    | `registerLayout` 在 `main.ts` 中调用     |
| 菜单动态数据   | ⬜    | 对接 `menuStore.treeMenus`，非静态数据   |
| 活跃菜单高亮   | ⬜    | 对接 `menuStore.activeMenu`              |
| 用户信息显示   | ⬜    | 对接 `userStore.userInfo`                |
| 页面标题       | ⬜    | 对接 `menuStore.activeMenu.title`        |
| 布局切换器     | ⬜    | `<LayoutSwitcher />` 组件                |
| 主题切换器     | ⬜    | `<ThemeSwitcher />` 组件                 |
| 模式切换器     | ⬜    | `<ModeSwitcher />` 组件                  |
| 通知按钮       | ⬜    | `<NotificationBell />` 组件              |
| 用户头像下拉   | ⬜    | `<UserProfile variant="navbar" />` 组件  |
| 面包屑导航     | ⬜    | 基于 `parentMenu` 链                     |
| CSS Token 变量 | ⬜    | 所有颜色引用 `var(--xxx)`                |
| 主题变量定义   | ⬜    | `--aurora-xxx` 在 `variables.css` 中定义 |

### 关键框架组件清单

| 组件               | 路径                                              | 用途                                               |
| ------------------ | ------------------------------------------------- | -------------------------------------------------- |
| `LayoutSwitcher`   | `cube-front/core/components/LayoutSwitcher.vue`   | 布局切换下拉                                       |
| `ThemeSwitcher`    | `cube-front/core/components/ThemeSwitcher.vue`    | 主题选择下拉                                       |
| `ModeSwitcher`     | `cube-front/core/components/ModeSwitcher.vue`     | 明/暗模式切换                                      |
| `NotificationBell` | `cube-front/core/components/NotificationBell.vue` | 通知铃铛                                           |
| `UserProfile`      | `cube-front/core/components/UserProfile.vue`      | 用户头像下拉（支持 `navbar` / `sidebar` 两种变体） |
| `SwitcherDropdown` | `cube-front/core/components/SwitcherDropdown.vue` | 通用下拉切换器                                     |

### 关键框架 Store

| Store          | 路径                                      | 用途             |
| -------------- | ----------------------------------------- | ---------------- |
| `useMenuStore` | `cube-front/core/stores/menu.ts`          | 菜单树、活跃菜单 |
| `useUserStore` | `cube-front/core/stores/user.ts`          | 用户信息、登出   |
| `useTheme`     | `cube-front/core/composables/useTheme.ts` | 主题切换         |

## 场景判断速查

| 需求           | 在哪里新增                 | 是否需要 registerLayout                         |
| -------------- | -------------------------- | ----------------------------------------------- |
| 仅当前项目使用 | 用户项目 `src/layouts/`    | 是，`registerLayout(option, true)` 切换为当前   |
| 所有应用共用   | cube-front `core/layouts/` | 是，在 `core/main.ts` 中注册                    |
| 设为框架默认   | cube-front `core/layouts/` | 是，`registerLayout(option, true)` 设为当前布局 |

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

以下组件均来自 `cube-front/core/components/`，**无需自行实现**，直接 import 使用即可。

### 2.1 LayoutSwitcher — 布局切换下拉

```vue
<script setup lang="ts">
import LayoutSwitcher from 'cube-front/core/components/LayoutSwitcher.vue';
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

### 2.2 ThemeSwitcher — 主题选择下拉

```vue
<script setup lang="ts">
import ThemeSwitcher from 'cube-front/core/components/ThemeSwitcher.vue';
</script>

<template>
  <ThemeSwitcher />
</template>
```

| 特性     | 说明                                                                |
| -------- | ------------------------------------------------------------------- |
| 主题来源 | 读取框架已注册的主题列表（Cyber、Aurora 等）                        |
| 切换逻辑 | 修改 `document.documentElement.setAttribute('data-theme', themeId)` |
| Props    | 无必填参数，开箱即用                                                |

### 2.3 ModeSwitcher — 明暗模式切换

```vue
<script setup lang="ts">
import ModeSwitcher from 'cube-front/core/components/ModeSwitcher.vue';
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

### 2.4 NotificationBell — 通知铃铛

```vue
<script setup lang="ts">
import NotificationBell from 'cube-front/core/components/NotificationBell.vue';
</script>

<template>
  <NotificationBell @click="handleNotificationClick" />
</template>
```

| 特性  | 说明                                                  |
| ----- | ----------------------------------------------------- |
| 图标  | 铃铛 SVG，内置样式                                    |
| Props | `showLabel?: boolean`（默认 false）、`label?: string` |
| 事件  | `@click` — 点击时触发                                 |
| 扩展  | 后续可接入真实通知 API，当前仅 UI 占位                |

### 2.5 UserProfile — 用户头像下拉（**注意：不是 NavbarUserProfile**）

```vue
<script setup lang="ts">
import UserProfile from 'cube-front/core/components/UserProfile.vue';
</script>

<template>
  <!-- 顶部栏模式：下拉向下 -->
  <UserProfile variant="navbar" />

  <!-- 侧边栏底部模式：下拉向上 -->
  <UserProfile variant="sidebar" :dropup="true" />
</template>
```

| 特性       | 说明                                                                                      |
| ---------- | ----------------------------------------------------------------------------------------- |
| **组件名** | `UserProfile.vue`（**不是** `NavbarUserProfile.vue`，后者不存在）                         |
| variant    | `'navbar'`（顶部栏，下拉向下）\| `'sidebar'`（侧边栏底部，下拉向上）                      |
| dropup     | `boolean`，sidebar 模式时传 `true`，下拉面板向上弹出                                      |
| 头像 URL   | 自动拼接 `${baseUrl}/Cube/Avatar/${userId}`                                               |
| 降级显示   | 头像加载失败时显示用户名首字母大写                                                        |
| 内置操作   | 点击头像 → 下拉菜单 → 「个人资料」跳转 `/profile` + 「退出登录」调用 `userStore.logout()` |
| Slots      | `#extra-options` — 可在用户信息卡之前插入自定义选项                                       |

> ⚠️ **踩坑记录**：早期文档误写为 `NavbarUserProfile`，实际组件名为 `UserProfile`，通过 `variant` 区分使用场景。

---

## 三、菜单对接方式（Sidebar + Navbar）

### 3.1 使用的 Store

```typescript
import { useMenuStore, type TreeMenuItem } from 'cube-front/core/stores/menu';
import { useUserStore } from 'cube-front/core/stores/user';
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

```typescript
// Sidebar.vue
const menuStore = useMenuStore();
const treeMenus = computed(() => menuStore.treeMenus ?? []);
const activeMenu = computed(() => menuStore.activeMenu);
const menuGroups = computed(() => treeMenus.value); // 顶层节点 = 分组

function isActive(item: TreeMenuItem): boolean {
  const active = activeMenu.value;
  if (!active) return false;
  // 精确匹配：当前菜单项本身，或其父分组
  return active.id === item.id || active.parentId === item.id;
}
```

```vue
<template>
  <nav>
    <template v-for="group in menuGroups" :key="group.id">
      <div v-if="group.children?.length" class="menu-group">
        <div class="menu-group-title">{{ group.title || group.name }}</div>
        <router-link
          v-for="item in group.children"
          :key="item.id"
          :to="item.path"
          class="menu-item"
          :class="{ active: isActive(item) }"
        >
          <span class="menu-icon">{{ item.icon || '📄' }}</span>
          <span class="menu-label">{{ item.title || item.name }}</span>
        </router-link>
      </div>
    </template>
    <!-- 无数据时显示加载/空状态 -->
    <div v-if="!hasMenus" class="menu-empty">
      {{ loading ? '加载菜单中...' : '暂无菜单数据' }}
    </div>
  </nav>
</template>
```

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
import { initApp } from 'cube-front/core/initApp';
import 'cube-front/core/global.css';
import { registerLayout } from 'cube-front/core/composables/useLayout';
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

**职责**：展示 Logo + 动态菜单树 + 活跃状态高亮。

**关键逻辑**：
- 从 `menuStore.treeMenus` 读取菜单分组
- 从 `menuStore.activeMenu` 判断当前活跃项
- 使用 `<router-link :to="item.path">` 实现路由跳转
- 加载中/无数据时显示友好提示

### 7.3 Navbar.vue — 顶部导航栏

**职责**：面包屑 + 布局/主题/模式切换 + 通知 + 用户头像下拉。

**关键逻辑**：
- 面包屑：沿 `activeMenu.parentMenu` 链向上递推
- 右侧工具栏：`LayoutSwitcher` + `ThemeSwitcher` + `ModeSwitcher` + `NotificationBell` + `UserProfile`
- 不直接读取 `userStore`，用户信息由 `UserProfile` 组件内部处理

---

## 八、常见踩坑与解决方案

| 坑                           | 表现                       | 原因                                | 解决方案                                             |
| ---------------------------- | -------------------------- | ----------------------------------- | ---------------------------------------------------- |
| `provide(LayoutKey)` 不生效  | 布局切换器显示但切换无反应 | `RootLayout.vue` 不读取 injected 值 | 使用 `registerLayout()` 函数                         |
| `NavbarUserProfile` 导入失败 | 编译错误：模块不存在       | 组件实际名为 `UserProfile.vue`      | 改为 `import UserProfile from '.../UserProfile.vue'` |
| 侧边栏菜单不更新             | 路由切换后高亮状态不变     | 使用了静态数据而非 store            | 改为 `computed(() => menuStore.treeMenus)`           |
| CSS 变量不生效               | 样式无变化                 | `variables.css` 用了 `scoped` style | `@import` 必须在非 scoped 的 `<style>` 中            |
| 布局切换器不显示             | 只看到一个布局选项         | 只注册了一个布局                    | 注册多个布局后自动显示                               |
| 面包屑为空                   | 导航栏无面包屑             | `activeMenu` 为 null                | 确保路由跳转后 menuStore 已更新 activeMenu           |

---

## 九、验证清单

构建通过后，按以下步骤在浏览器中验证：

- [ ] 布局正确渲染（侧边栏 + 顶部栏 + 内容区）
- [ ] 侧边栏显示动态菜单（从后端加载，非静态数据）
- [ ] 点击菜单项后高亮状态正确切换
- [ ] 路由跳转正常（`<router-link>` 生效）
- [ ] 面包屑随路由变化正确更新
- [ ] 布局切换器显示（注册多个布局时）
- [ ] 主题切换器可切换主题
- [ ] 明暗模式切换正常
- [ ] 通知铃铛可点击
- [ ] 用户头像下拉正常显示，退出登录功能正常
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

## 十一、 AuroraLayout 实际文件参考

| 文件                                                              | 行数    | 说明       |
| ----------------------------------------------------------------- | ------- | ---------- |
| `SmartMES.Frontend/src/layouts/AuroraLayout/index.vue`            | ~45 行  | 布局入口   |
| `SmartMES.Frontend/src/layouts/AuroraLayout/Sidebar.vue`          | ~80 行  | 动态侧边栏 |
| `SmartMES.Frontend/src/layouts/AuroraLayout/Navbar.vue`           | ~100 行 | 顶部导航   |
| `SmartMES.Frontend/src/layouts/AuroraLayout/styles/variables.css` | ~60 行  | 主题变量   |
| `SmartMES.Frontend/src/main.ts`                                   | +5 行   | 注册布局   |

> 以上文件均为实际可运行的生产代码，可直接作为新增布局的参考模板。

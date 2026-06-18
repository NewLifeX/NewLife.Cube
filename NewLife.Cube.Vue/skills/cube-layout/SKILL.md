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

### 方式二：应用层覆盖布局

在项目 `src/main.ts` 中覆盖默认布局：

```typescript
// src/main.ts
import { initApp } from 'cube-front/core/initApp';
import 'cube-front/core/global.css';
import { LayoutKey } from 'cube-front/core/composables/useProvideInject';
import MainLayout from 'cube-front/core/layouts/MainLayout';

initApp((app, { provide }) => {
  provide(app, LayoutKey, MainLayout, { override: true });
});
```

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
}

.layout-sidebar {
  width: 260px;
  flex-shrink: 0;
  background: #1e1e1e;
}

.layout-main {
  flex: 1;
}

.layout-navbar {
  height: 64px;
  padding: 0 24px;
  background: #2d2d2d;
}

.layout-content {
  padding: 24px;
}
</style>
```

#### 步骤 3：在应用中使用

**src/main.ts**：

```typescript
import { initApp } from 'cube-front/core/initApp';
import 'cube-front/core/global.css';
import { LayoutKey } from 'cube-front/core/composables/useProvideInject';
import CustomLayout from './layouts/CustomLayout/index.vue';

initApp((app, { provide }) => {
  provide(app, LayoutKey, CustomLayout, { override: true });
});
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
}

.layout-sidebar {
  width: 260px;
  flex-shrink: 0;
}

.layout-main {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.layout-navbar {
  height: 64px;
  padding: 0 24px;
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

**方案 A**：在 `registerLayout` 时设置 `isDefault: true`

```typescript
registerLayout({
  id: 'your-layout',
  label: '你的布局',
  component: YourLayout,
  isDefault: true,  // 设为默认
});
```

**方案 B**：修改 `cube-front/core/initApp.ts`

```typescript
// 修改导入
import YourLayout from './layouts/YourLayout/index.vue';

// 修改默认值
if (!hasProvided(LayoutKey)) {
  appProvide(app, LayoutKey, YourLayout, { track: true });
}
```

## 应用层使用布局

### 覆盖框架默认布局

```typescript
// src/main.ts
import { initApp } from 'cube-front/core/initApp';
import 'cube-front/core/global.css';
import { LayoutKey } from 'cube-front/core/composables/useProvideInject';
import CustomLayout from './layouts/CustomLayout';  // 本项目路径

initApp((app, { provide }) => {
  provide(app, LayoutKey, CustomLayout, { override: true });
});
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

## 场景判断速查

| 需求           | 在哪里新增                 | 是否需要 registerLayout      |
| -------------- | -------------------------- | ---------------------------- |
| 仅当前项目使用 | 用户项目 `src/layouts/`    | 否                           |
| 所有应用共用   | cube-front `core/layouts/` | 是                           |
| 设为框架默认   | cube-front `core/layouts/` | 是，且设置 `isDefault: true` |
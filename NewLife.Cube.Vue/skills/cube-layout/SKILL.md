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
- ✅ 必须使用 `--el-*` 或 `--cube-layout-*`，通过 `var(--xxx)` 引用
- ✅ 布局专属变量以 `--{布局名}-` 前缀，基于 Layer 1 语义变量派生
- ❌ 禁止硬编码色值、禁止使用已废弃的 `--bg-*`/`--text-*` 等自定义 token
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

## 布局插槽

| 插槽名    | 说明                 |
| --------- | -------------------- |
| `default` | 主内容区（页面内容） |
| `sidebar` | 侧边栏内容           |
| `header`  | 顶部导航内容         |

## 运行时切换布局

```typescript
import { useLayout } from '@newlifex/cube-vue/core/composables/useLayout';
const { currentLayout, setLayout, availableLayouts } = useLayout();
setLayout('main-layout');
```

## 路由级别指定布局

```typescript
const routes = [{
  path: '/dashboard',
  component: () => import('./views/Dashboard.vue'),
  meta: { layout: 'your-layout' },
}];
```

## 场景判断速查

| 需求           | 在哪里新增                 | 是否需要 registerLayout                         |
| -------------- | -------------------------- | ----------------------------------------------- |
| 仅当前项目使用 | 用户项目 `src/layouts/`    | 是，`registerLayout(option, true)` 切换为当前   |
| 所有应用共用   | @newlifex/cube-vue `core/layouts/` | 是，在 `core/main.ts` 中注册                    |
| 设为框架默认   | @newlifex/cube-vue `core/layouts/` | 是，`registerLayout(option, true)` 设为当前布局 |

> **核心规则**：无论哪种场景，都必须调用 `registerLayout` 才能让布局生效。

---

## 参考文件

| 文件 | 内容 |
| --- | --- |
| [references/css-token-spec.md](references/css-token-spec.md) | CSS Token 完整表、三层架构、命名规范、示例对照 |
| [references/layout-implementation-guide.md](references/layout-implementation-guide.md) | 布局实现完整流程、框架组件清单、菜单对接、踩坑记录、验证清单 |

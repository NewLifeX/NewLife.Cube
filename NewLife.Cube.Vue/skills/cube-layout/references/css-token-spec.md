# CSS Token 规范详细参考

> 从 cube-layout SKILL.md 拆出的 CSS Token 详细规范。布局样式**必须**使用 Element Plus CSS token（`--el-*`）或 Cube Layout token（`--cube-layout-*`），**禁止硬编码色值、自定义 CSS 变量或第三方 token 体系**。

## 双 Token 架构

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

## Element Plus 语义 Token（`--el-*`）

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

## Cube Layout 布局结构 Token（`--cube-layout-*`）

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

## 使用规则

| 规则                                                                                              | 说明                                           |
| ------------------------------------------------------------------------------------------------- | ---------------------------------------------- |
| ✅ **必须**使用 `--el-*` 或 `--cube-layout-*`                                                      | `background: var(--el-bg-color-overlay)`       |
| ✅ **必须**使用 `var(--xxx)` 引用                                                                  | `color: var(--el-text-color-primary)`          |
| ✅ 布局专属尺寸变量以 `--cube-layout-` 前缀                                                        | `--cube-layout-sidebar-width`                  |
| ✅ 带合理 fallback                                                                                 | `var(--cube-layout-nav-height, 60px)`          |
| ❌ **禁止**使用旧版自定义 token（`--bg-*`、`--text-*`、`--accent-*`、`--sidebar-*`、`--navbar-*`） | 已废弃，不再支持                               |
| ❌ **禁止**硬编码色值                                                                              | 不得出现 `#fff`、`#1e293b`、`rgba(x,x,x,x)` 等 |
| ❌ **禁止**在组件 `scoped style` 中覆盖 `--el-*` 变量                                              | 全局变量在各布局 `variables.css` 中统一管理    |

## 布局专属变量定义方式

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

## 示例对照

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

## CSS Token 三层架构

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

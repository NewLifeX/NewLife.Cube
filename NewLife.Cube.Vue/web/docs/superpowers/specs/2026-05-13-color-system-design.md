# @newlifex/cube-vue 颜色系统设计规格

> **状态：** 已实现
> **日期：** 2026-05-13
> **作者：** Claude Code

## 目标

重构 @newlifex/cube-vue 前端框架的颜色系统，建立分层 Token 架构，实现：
- 布局与颜色解耦
- 主题与模式组合切换
- Element Plus 组件颜色与主题系统统一

---

## 架构概览

### 核心概念

| 概念 | 定义 | 示例 |
|------|------|------|
| **布局 (Layout)** | 只定义结构，不含颜色 | CyberLayout、TopMenuLayout |
| **主题 (Theme)** | 定义品牌色和语义 | cyber（深林绿）、forest（森林绿） |
| **模式 (Mode)** | 控制明暗 | dark（深色）、light（浅色） |

### 组合方式

```
布局 (CyberLayout) + 主题 (cyber) + 模式 (dark) = cyber-dark
布局 (CyberLayout) + 主题 (forest) + 模式 (light) = forest-light
```

---

## Token 分层架构

```
┌─────────────────────────────────────────────────────┐
│  Layer 0: Primitives（设计原语）                     │
│  不随主题变化，是所有主题的基础色彩                   │
│  --green-400: #4ade80                               │
│  --cyan-400: #22d3ee                                │
│  --radius-sm: 8px                                   │
├─────────────────────────────────────────────────────┤
│  Layer 1: Semantic（语义别名）                       │
│  主题通过覆盖这些变量来改变整体外观                   │
│  --bg-primary, --accent, --text-primary             │
├─────────────────────────────────────────────────────┤
│  Layer 2: Component（组件变量）                      │
│  基于语义变量的组件级封装                            │
│  --navbar-bg, --sidebar-bg, --btn-primary-bg        │
├─────────────────────────────────────────────────────┤
│  Layer 3: Element Plus（EP 覆盖）                   │
│  覆盖 EP 内部 CSS 变量                               │
│  --el-color-primary                                 │
├─────────────────────────────────────────────────────┤
│  Layer 4: Layout（布局变量）                         │
│  仅在特定布局组件中生效                              │
│  --layout-nav-height                                │
└─────────────────────────────────────────────────────┘
```

---

## 变量命名规范

### Layer 0: Primitives

```css
:root {
  /* 绿色系 */
  --green-300: #86efac;
  --green-400: #4ade80;
  --green-500: #22c55e;
  --green-600: #16a34a;
  --green-700: #15803d;

  /* 青色系 */
  --cyan-300: #67e8f9;
  --cyan-400: #22d3ee;
  --cyan-500: #06b6d4;
  --cyan-600: #0891b2;

  /* 琥珀/警告系 */
  --amber-400: #fbbf24;
  --amber-600: #d97706;

  /* 玫瑰/错误系 */
  --rose-400: #fb7185;
  --rose-500: #f43f5e;

  /* 紫罗兰系 */
  --violet-300: #c4b5fd;
  --violet-400: #a78bfa;

  /* 蓝色系 */
  --blue-400: #60a5fa;
  --blue-500: #3b82f6;
  --blue-600: #2563eb;

  /* 布局尺寸 */
  --radius-sm: 8px;
  --radius-md: 12px;
  --radius-lg: 16px;
  --radius-xl: 20px;

  /* 动画 */
  --ease: cubic-bezier(0.4, 0, 0.2, 1);
  --ease-spring: cubic-bezier(0.34, 1.56, 0.64, 1);

  /* 模糊 */
  --glass-blur: blur(14px);
  --glass-blur-sm: blur(8px);
}
```

### Layer 1: Semantic（主题覆盖）

```css
/* Cyber Dark（默认） */
:root,
[data-theme='cyber-dark'] {
  --bg-primary: #0a0e14;
  --bg-secondary: #111820;
  --bg-tertiary: #1a222d;
  --bg-card: #141b24;

  --border-subtle: rgba(74, 222, 128, 0.12);
  --border-default: rgba(74, 222, 128, 0.2);
  --border-emphasis: rgba(74, 222, 128, 0.3);

  --text-primary: #e8eaed;
  --text-secondary: #8b949e;
  --text-muted: #5c6370;

  --accent: var(--green-400);
  --accent-hover: var(--green-300);
  --accent-secondary: var(--cyan-400);
  --accent-muted: rgba(74, 222, 128, 0.1);
  --accent-soft: rgba(74, 222, 128, 0.15);

  --color-success: var(--green-400);
  --color-warning: var(--amber-400);
  --color-error: var(--rose-400);
  --color-info: var(--cyan-400);
}

/* Forest Dark */
[data-theme='forest-dark'] {
  --bg-primary: #0f1f18;
  --bg-secondary: #1a3328;
  --bg-tertiary: #243d32;
  --bg-card: #152a20;

  --border-subtle: rgba(29, 112, 64, 0.15);
  --border-default: rgba(29, 112, 64, 0.25);
  --border-emphasis: rgba(29, 112, 64, 0.4);

  --accent: #1d7040;
  --accent-hover: #2a9d57;
  --accent-secondary: var(--blue-500);
  --accent-muted: rgba(29, 112, 64, 0.12);
  --accent-soft: rgba(29, 112, 64, 0.2);
}
```

### Layer 2: Component

```css
:root,
[data-theme] {
  /* 按钮 */
  --btn-primary-bg: var(--accent);
  --btn-primary-color: var(--text-inverse);
  --btn-secondary-bg: transparent;
  --btn-secondary-color: var(--accent);

  /* 导航栏 */
  --navbar-bg: var(--bg-secondary);
  --navbar-border: var(--border-subtle);
  --navbar-text: var(--text-primary);

  /* 侧边栏 */
  --sidebar-bg: var(--bg-secondary);
  --sidebar-border: var(--border-subtle);
  --sidebar-item-hover: var(--accent-muted);
  --sidebar-item-active: var(--accent-soft);
}
```

### Layer 3: Element Plus

```css
:root,
[data-theme='cyber-dark'],
[data-theme='forest-dark'] {
  --el-color-primary: var(--accent);
  --el-color-primary-light-3: color-mix(in srgb, var(--accent) 60%, white);
  --el-color-primary-light-5: color-mix(in srgb, var(--accent) 40%, white);

  --el-bg-color: var(--bg-primary);
  --el-text-color-primary: var(--text-primary);
  --el-text-color-regular: var(--text-secondary);
  --el-border-color: var(--border-subtle);

  --el-color-success: var(--color-success);
  --el-color-warning: var(--color-warning);
  --el-color-danger: var(--color-error);
  --el-color-info: var(--color-info);
}
```

### Layer 4: Layout

```css
.cyber-layout {
  --layout-nav-height: 64px;
  --layout-sidebar-width: 260px;
  --layout-content-min-width: 1200px;
}
```

---

## 主题定义

### 主题列表

| Theme ID | 主题 | 模式 | 主色 |
|----------|------|------|------|
| `cyber-dark` | Cyber 赛博 | 深色 | #4ade80 |
| `cyber-light` | Cyber 赛博 | 浅色 | #16a34a |
| `forest-dark` | Forest 森林绿 | 深色 | #1d7040 |
| `forest-light` | Forest 森林绿 | 浅色 | #1d7040 |

### 颜色对比

| 主题 | 背景色 | 强调色 | 文字色 |
|------|--------|--------|--------|
| cyber-dark | #0a0e14 | #4ade80 | #e8eaed |
| cyber-light | #f5f7fa | #16a34a | #1a222d |
| forest-dark | #0f1f18 | #1d7040 | #e8eaed |
| forest-light | #f4f5f1 | #1d7040 | #1a222d |

---

## 文件变更记录

### 新增文件

| 文件 | 描述 |
|------|------|
| `docs/superpowers/specs/2026-05-13-color-system-design.md` | 本规格文档 |

### 修改文件

| 文件 | 变更 |
|------|------|
| `core/global.css` | 完整重构，建立分层架构 |
| `core/composables/useTheme.ts` | 新增 ThemeFamily、ThemeMode 类型，支持组合切换 |
| `core/layouts/CyberLayout/index.vue` | 移除 `--cyber-*`，使用语义变量 |
| `core/layouts/CyberLayout/Navbar/index.vue` | 使用 `--navbar-*`、`--text-*` 变量 |
| `core/layouts/CyberLayout/Sidebar/index.vue` | 使用 `--sidebar-*`、`--accent` 变量 |
| `core/components/MenuItem.vue` | 使用 `--sidebar-*`、 `--accent` 变量 |
| `core/components/SwitcherDropdown.vue` | 使用 `--bg-*`、`--accent` 变量 |
| `core/components/ThemeSwitcher.vue` | 更新类型导入 |

---

## API 参考

### useTheme Composable

```typescript
import { useTheme, type ThemeId, type ThemeFamily, type ThemeMode } from '@newlifex/cube-vue/core/composables/useTheme';

const {
  currentTheme,      // 当前主题选项 { id, label, icon, description, family, mode }
  currentGroup,      // 当前主题分组 { id, label, icon, variants }
  currentThemeId,    // 当前主题 ID (ref)
  themeGroups,       // 所有主题分组
  themes,            // 所有主题选项（扁平化）
  setTheme,          // (id: ThemeId) => void
  setThemeByGroupAndMode,  // (family: ThemeFamily, mode: ThemeMode) => void
  toggleMode,        // () => void，切换当前主题的暗/亮模式
  switchTheme,       // (family: ThemeFamily) => void，切换主题家族
} = useTheme();
```

### 切换示例

```typescript
// 切换到 Cyber 深色
setTheme('cyber-dark');

// 切换到 Forest 浅色
setTheme('forest-light');

// 切换到同主题的亮色
toggleMode();

// 切换到 Forest 主题（保持当前模式）
switchTheme('forest');
```

---

## 迁移指南

### 旧变量 → 新变量对照

| 旧变量 | 新变量 |
|--------|--------|
| `--cyber-bg-primary` | `--bg-primary` |
| `--cyber-bg-secondary` | `--bg-secondary` |
| `--cyber-bg-tertiary` | `--bg-tertiary` |
| `--cyber-accent-green` | `--accent` |
| `--cyber-accent-cyan` | `--accent-secondary` |
| `--cyber-text-primary` | `--text-primary` |
| `--cyber-text-secondary` | `--text-secondary` |
| `--cyber-border-subtle` | `--border-subtle` |

### 组件中使用

```vue
<style lang="scss" scoped>
.my-component {
  background: var(--bg-card);
  color: var(--text-primary);
  border: 1px solid var(--border-subtle);

  &.active {
    background: var(--accent-muted);
    color: var(--accent);
  }
}
</style>
```

---

## 兼容性

- 所有旧变量通过别名保留，确保平滑迁移
- `data-theme` 属性使用新 ID（`cyber-dark` 而非 `cyber`）
- 默认主题（无属性）为 `cyber-dark`

---

## 验证清单

- [x] Cyber Dark 主题显示正确
- [x] Cyber Light 主题切换正常
- [x] Forest Dark 主题颜色正确
- [x] Forest Light 主题颜色正确
- [x] Element Plus 按钮颜色与主题一致
- [x] Element Plus 表格、输入框颜色正确
- [x] 主题切换后 EP 组件正确响应
- [x] 布局组件使用语义变量
- [x] 无 `--cyber-*` 硬编码（组件中）

---

## 后续扩展

如需新增主题：

1. 在 `global.css` 添加 `[data-theme='{family}-{mode}']` 块
2. 在 `useTheme.ts` 添加 `ThemeId` 类型和 `THEME_GROUPS` 项
3. 覆盖 Layer 1-3 变量

不需要修改任何布局或组件代码。
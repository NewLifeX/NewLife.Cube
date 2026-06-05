# CUBE 多主题系统设计指南

## 架构概览

本系统采用 **CSS 自定义属性（Custom Properties）+ `data-theme` 属性** 驱动的主题切换方案，与 Element Plus 的内置主题系统无缝衔接。

```
切换逻辑
useTheme.ts  →  document.documentElement.setAttribute('data-theme', id)
                                   ↓
global.css  →  [data-theme='xxx'] { --var: value; }
                                   ↓
组件 SCSS / 内联样式  →  var(--var)
```

---

## Token 分层结构

### §1 全局固定 Token（`core/global.css` `:root`）

与主题颜色无关的布局/动效值，**所有主题共用，不重复覆盖**：

| Token                  | 说明                  |
| ---------------------- | --------------------- |
| `--radius-sm/md/lg/xl` | 圆角大小              |
| `--r`                  | 圆角别名（8px）       |
| `--ease`               | 过渡曲线              |
| `--glass-blur`         | 毛玻璃模糊量          |
| `--card`               | 纯白基础色（#ffffff） |

### §2 主题色 Token（各主题选择器下）

每个主题必须完整定义以下分组：

| 分组         | Token 示例                                     | 说明                                         |
| ------------ | ---------------------------------------------- | -------------------------------------------- |
| **EP 覆盖**  | `--el-color-primary`                           | 覆盖 Element Plus 主题色（默认主题无需覆盖） |
| **导航栏**   | `--tn`, `--tn-b`, `--tn-t`, `--tn-ac`          | Topnav 背景/边框/文字/激活色                 |
| **页面基础** | `--bg`, `--bd`, `--card`                       | 背景/边框                                    |
| **强调色**   | `--ac`, `--ac-l`, `--ac-b`                     | 页面级强调色及浅色变体                       |
| **文字层级** | `--t1`, `--t2`, `--t3`                         | 主/次/辅助文字色                             |
| **语义色**   | `--ok`, `--wn`, `--er`, `--in`                 | 成功/警告/错误/信息色及对应浅色              |
| **阴影**     | `--sh`, `--shm`, `--shadow-*`                  | 带颜色调性的阴影                             |
| **滚动条**   | `--scrollbar-thumb`, `--scrollbar-thumb-hover` | 滚动条颜色                                   |

---

## 现有主题

### 默认主题（`data-theme="default"` 或无属性）

- **选择器**：`:root, [data-theme='default']`
- **EP 主题色**：使用 Element Plus 内置蓝色 `#409eff`，**不覆盖** `--el-color-primary`
- **导航栏**：深海军蓝 `#1e3a8a`
- **页面背景**：米白 `#faf8f5`
- **强调色**：蓝色 `#2563eb`

### 森林绿主题（`data-theme="forest"`）

- **选择器**：`[data-theme='forest']`
- **EP 主题色**：覆盖为 `#1d7040`（Boreal 森林绿）
- **导航栏**：深森林绿 `#1a3328`
- **页面背景**：自然绿白 `#f4f5f1`
- **强调色**：绿色 `#1d7040`

---

## 新增主题步骤

### 第一步：在 `global.css` 添加主题 Token 块

复制以下模板，放在 `§3` 之后：

```css
/* ============================================================
   §N — 主题名称（data-theme="your-theme-id"）
   ============================================================ */
[data-theme='your-theme-id'] {
  /* ── Element Plus 主题色覆盖（如需） ── */
  --el-color-primary: #xxxxxx;
  --el-color-primary-light-3: #xxxxxx;
  --el-color-primary-light-5: #xxxxxx;
  --el-color-primary-light-7: #xxxxxx;
  --el-color-primary-light-8: #xxxxxx;
  --el-color-primary-light-9: #xxxxxx;
  --el-color-primary-dark-2: #xxxxxx;

  /* ── 顶部导航 ── */
  --tn: #xxxxxx;   /* 导航栏背景 */
  --tn-b: #xxxxxx; /* 导航栏边框/强调 */
  --tn-t: #xxxxxx; /* 导航栏文字 */
  --tn-ac: #xxxxxx; /* 导航栏激活色 */

  /* ── 页面基础 ── */
  --bg: #xxxxxx;
  --bd: #xxxxxx;

  /* ── 强调色 ── */
  --ac: #xxxxxx;
  --ac-l: #xxxxxx;
  --ac-b: #xxxxxx;

  /* ── 文字层级 ── */
  --t1: #xxxxxx;
  --t2: #xxxxxx;
  --t3: #xxxxxx;

  /* ── 语义色（通常保持不变） ── */
  --ok: #16a34a; --okl: #dcfce7;
  --wn: #d97706; --wnl: #fef3c7;
  --er: #dc2626; --erl: #fee2e2;
  --in: #2563eb; --inl: #dbeafe;

  /* ── 兼容长名 token ── */
  --color-primary: #xxxxxx;
  --color-primary-hover: #xxxxxx;
  --color-primary-light: #xxxxxx;
  --color-primary-bg: #xxxxxx;
  --color-primary-glow: rgba(r, g, b, 0.12);
  --gradient-primary: linear-gradient(135deg, #dark 0%, #light 100%);
  --gradient-subtle: linear-gradient(135deg, #bg 0%, #bgwarm 50%, #bg 100%);

  --color-bg-page: #xxxxxx;
  --color-bg-warm: #xxxxxx;
  --color-bg-hover: #xxxxxx;
  --color-bg-active: #xxxxxx;
  --color-bg-table-header: #xxxxxx;
  --color-bg-table-stripe: rgba(r, g, b, 0.4);

  --color-text-primary: #xxxxxx;
  --color-text-secondary: #xxxxxx;
  --color-text-tertiary: #xxxxxx;
  --color-text-placeholder: #xxxxxx;

  --color-border: #xxxxxx;
  --color-border-light: #xxxxxx;
  --color-border-hover: #xxxxxx;

  --shadow-xs: 0 1px 2px rgba(r, g, b, 0.03);
  --shadow-sm: 0 2px 8px rgba(r, g, b, 0.04);
  --shadow-md: 0 4px 16px rgba(r, g, b, 0.05);
  --shadow-lg: 0 8px 32px rgba(r, g, b, 0.06);
  --sh: 0 1px 3px rgba(r, g, b, 0.06), 0 1px 2px rgba(r, g, b, 0.04);
  --shm: 0 4px 18px rgba(r, g, b, 0.09);

  --scrollbar-thumb: #xxxxxx;
  --scrollbar-thumb-hover: #xxxxxx;
}
```

### 第二步：在 `useTheme.ts` 添加主题选项

```typescript
// core/composables/useTheme.ts
export type ThemeId = 'default' | 'forest' | 'your-theme-id';

export const THEMES: ThemeOption[] = [
  // ...现有主题...
  {
    id: 'your-theme-id',
    label: '主题名称',
    icon: '🎨',
    description: '主题描述',
  },
];
```

### 第三步：验证

切换到新主题后，检查以下页面要素是否正确响应：
- [ ] 顶部导航栏背景色和文字色
- [ ] 页面背景色
- [ ] 主要按钮颜色（gradient-primary）
- [ ] 表格表头背景
- [ ] 输入框 focus 状态颜色
- [ ] El-Tag、El-Pagination 主色
- [ ] 滚动条颜色

---

## 注意事项

1. **Element Plus 默认主题**：`[data-theme='default']` **不覆盖** `--el-color-primary`，让 EP 使用内置蓝色。其他自定义主题需完整覆盖所有 `--el-color-primary-*` 变量。

2. **CSS 变量继承**：主题 Token 覆盖后，`§1` 中的固定 Token（`--radius-*`、`--ease` 等）仍然有效，无需重复声明。

3. **EP 组件覆盖**：`global.css` 中对 EP 组件（`.el-button--primary`、`.el-table` 等）的样式覆盖已全部使用 CSS 变量，会自动跟随主题变化。若新增 EP 组件覆盖，**禁止硬编码颜色值**，必须使用对应的 CSS 变量。

4. **SCSS 组件样式**：业务组件的 `<style lang="scss">` 中，**不允许**使用 SCSS 变量（`$primary` 等）来定义与主题相关的颜色，应使用 `var(--ac)`、`var(--tn)` 等 CSS 变量。

5. **LocalStorage 持久化**：当前主题 ID 保存在 `localStorage['cube-theme']`，页面刷新后自动恢复。

---

## 文件索引

| 文件                                    | 职责                                                            |
| --------------------------------------- | --------------------------------------------------------------- |
| `core/global.css`                       | 全部 CSS Token 定义（§1 固定 + §2 默认 + §3 森林绿 + 未来主题） |
| `core/composables/useTheme.ts`          | 主题状态管理，`THEMES` 数组，`setTheme()`                       |
| `core/layouts/TopMenu/Topnav/index.vue` | 顶部导航，主题切换图标按钮 + 下拉菜单                           |

---

## Element Plus 主题定制

### 官方文档

- 主题文档: https://element-plus.org/zh-CN/guide/theming.html
- CSS 变量: https://element-plus.org/zh-CN/component/var.html
- GitHub: https://github.com/element-plus/element-plus

### Element Plus CSS 变量参考

Element Plus 使用 CSS 自定义属性实现主题定制，核心变量如下：

```css
/* 主色调 */
--el-color-primary: #409eff;
--el-color-primary-light-3: #79bbff;
--el-color-primary-light-5: #a0cfff;
--el-color-primary-light-7: #c6e2ff;
--el-color-primary-light-8: #d9ecff;
--el-color-primary-light-9: #ecf5ff;
--el-color-primary-dark-2: #337ecc;

/* 文字颜色 */
--el-text-color-primary: #303133;
--el-text-color-regular: #606266;
--el-text-color-secondary: #909399;
--el-text-color-placeholder: #a8abb2;

/* 边框颜色 */
--el-border-color: #dcdfe6;
--el-border-color-light: #e4e7ed;
--el-border-color-lighter: #ebeef5;

/* 填充颜色 */
--el-fill-color: #f0f2f5;
--el-fill-color-light: #f5f7fa;
--el-fill-color-lighter: #fafafa;
--el-fill-color-blank: #ffffff;

/* 背景颜色 */
--el-bg-color: #ffffff;
--el-bg-color-page: #f2f3f5;
--el-bg-color-overlay: #ffffff;

/* 遮罩色 */
--el-mask-color: rgba(255, 255, 255, 0.9);
--el-mask-color-extra-light: rgba(255, 255, 255, 0.3);

/* 阴影 */
--el-box-shadow: 0px 12px 32px 4px rgba(0, 0, 0, 0.04);
--el-box-shadow-light: 0px 0px 12px rgba(0, 0, 0, 0.12);
--el-box-shadow-lighter: 0px 0px 6px rgba(0, 0, 0, 0.12);

/* 禁用状态 */
--el-disabled-bg-color: #f5f7fa;
--el-disabled-text-color: #a8abb2;
--el-disabled-border-color: #e4e7ed;

/* 语义色 */
--el-color-success: #67c23a;
--el-color-warning: #e6a23c;
--el-color-danger: #f56c6c;
--el-color-info: #909399;
```

### 深色主题实现

```css
/* 方式一：data-theme 属性 */
[data-theme='cyber-dark'],
[data-theme='forest-dark'] {
  --el-color-primary: var(--accent);
  --el-bg-color: var(--bg-primary);
  --el-text-color-primary: var(--text-primary);
  /* ... */
}

/* 方式二：使用 color-mix 自动计算变体 */
[data-theme] {
  --el-color-primary-light-3: color-mix(in srgb, var(--accent) 60%, white);
  --el-color-primary-light-5: color-mix(in srgb, var(--accent) 40%, white);
  --el-color-primary-light-7: color-mix(in srgb, var(--accent) 20%, white);
  --el-color-primary-light-9: color-mix(in srgb, var(--accent) 5%, white);
  --el-color-primary-dark-2: color-mix(in srgb, var(--accent) 80%, black);
}
```

### cube-front EP 集成

```css
/* 深色主题统一处理 */
[data-theme='cyber-dark'],
[data-theme='forest-dark'] {
  --el-color-primary: var(--accent);
  --el-bg-color: var(--bg-primary);
  --el-bg-color-page: var(--bg-primary);
  --el-bg-color-overlay: var(--bg-elevated);
  --el-text-color-primary: var(--text-primary);
  --el-text-color-regular: var(--text-secondary);
  --el-text-color-secondary: var(--text-muted);
  --el-border-color: var(--border-subtle);
  --el-fill-color: var(--bg-secondary);
  --el-fill-color-blank: var(--bg-card);
}

/* 浅色主题统一处理 */
[data-theme='cyber-light'],
[data-theme='forest-light'] {
  --el-color-primary: var(--accent);
  --el-bg-color: var(--bg-secondary);
  --el-bg-color-page: var(--bg-primary);
  --el-bg-color-overlay: var(--bg-elevated);
  --el-mask-color: rgba(0, 0, 0, 0.5);
  --el-mask-color-extra-light: rgba(0, 0, 0, 0.1);
}
```

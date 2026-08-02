# 布局实现完整指南

> 从 cube-layout SKILL.md 拆出的布局实现详细流程。包含功能清单、框架组件对接、菜单对接方式、踩坑记录和验证清单。

## 整体流程概览

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

## 功能清单与实现流程

> **重要**：新增布局必须实现以下全部功能，才能成为生产级布局。仅实现 UI 骨架（静态 HTML）属于"玩具"，不能上线。

### 1. 注册布局

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

### 2. 布局入口组件（index.vue）

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

### 3. 侧边栏（Sidebar.vue）— 对接 menuStore

**必须功能**：
- Logo：`<LogoBrand />` 自动读取配置中的 logo 和 system title
- 搜索框（可选）：`<SearchBar mode="box" />` 实时搜索菜单
- 从 `menuStore.treeMenus` 获取树形菜单
- 使用 `<MenuItem>` 组件渲染菜单
- 加载/空状态提示
- 底部用户区域（可选）：`<UserProfile variant="sidebar" dropup>`

**推荐实现**：

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
    <div class="sidebar-logo"><LogoBrand /></div>
    <div class="sidebar-search"><SearchBar mode="box" placeholder="搜索菜单..." /></div>
    <nav class="sidebar-menu">
      <template v-for="group in menuGroups" :key="group.id">
        <div v-if="group.children?.length" class="menu-group">
          <div class="menu-group-title">{{ group.title || group.name }}</div>
          <MenuItem v-for="item in group.children" :key="item.id" :menu="item" :activeMenu="activeMenu" />
        </div>
      </template>
      <div v-if="!hasMenus" class="menu-empty">{{ loading ? '加载菜单中...' : '暂无菜单数据' }}</div>
    </nav>
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

### 4. 顶部导航（Navbar.vue）

**必须功能**：
- 页面标题：从 `menuStore.activeMenu.title` 获取
- 用户信息：`<UserProfile variant="navbar" />`
- 布局切换器：`<LayoutSwitcher />`
- 主题切换器：`<ThemeSwitcher />`
- 模式切换器：`<ModeSwitcher />`
- 通知按钮：`<NotificationBell />`

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
    <div class="navbar-left"><h2>{{ pageTitle }}</h2></div>
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

### 5. 面包屑（基于 parentMenu 链）

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

## 关键框架组件清单

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

## 关键框架 Store

| Store          | 路径                                      | 用途             |
| -------------- | ----------------------------------------- | ---------------- |
| `useMenuStore` | `@newlifex/cube-vue/core/stores/menu.ts`          | 菜单树、活跃菜单 |
| `useUserStore` | `@newlifex/cube-vue/core/stores/user.ts`          | 用户信息、登出   |
| `useTheme`     | `@newlifex/cube-vue/core/composables/useTheme.ts` | 主题切换         |

## 菜单对接方式

### 使用的 Store

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

### TreeMenuItem 数据结构

```typescript
interface TreeMenuItem {
  id: string;
  name: string;
  title: string;
  path: string;
  icon?: string;
  parentId?: string;
  parentMenu?: TreeMenuItem;
  children?: TreeMenuItem[];
}
```

## 功能完整性检查清单

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

## 目录结构规范

```
src/layouts/{LayoutName}/
├── index.vue              # 布局入口（组合 Sidebar + Navbar + slot）
├── Sidebar.vue            # 侧边栏（必须对接 menuStore）
├── Navbar.vue             # 顶部导航（必须对接 userStore + 框架组件）
├── styles/
│   └── variables.css      # 布局专属 CSS Token（Layer 2）
└── README.md              # 可选：布局说明文档
```

## 常见踩坑与解决方案

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

## 验证清单

构建通过后，按以下步骤在浏览器中验证：

- [ ] 布局正确渲染（侧边栏 + 顶部栏 + 内容区）
- [ ] 侧边栏 Logo 显示配置中的 logo 图片和系统标题
- [ ] 侧边栏搜索框可搜索菜单并跳转
- [ ] 侧边栏显示动态菜单（从后端加载，非静态数据）
- [ ] 点击菜单项后高亮状态正确切换
- [ ] 菜单图标正常显示
- [ ] 路由跳转正常
- [ ] 面包屑随路由变化正确更新
- [ ] 右上角显示当前登录用户的头像/名称
- [ ] 用户头像下拉菜单有「个人资料」和「退出登录」
- [ ] 布局切换器显示（注册多个布局时）
- [ ] 主题切换器可切换主题
- [ ] 明暗模式切换正常
- [ ] 通知铃铛可点击
- [ ] 响应式：窗口缩小时侧边栏行为正常
- [ ] 刷新页面后布局/主题/模式状态保持

## 从原型 HTML 到生产组件的转换要点

1. **提取设计 Token**：从原型中提取颜色、间距、圆角等，映射到 CSS 变量
2. **保留三层架构**：原型色值 → Layer 0 原语 / Layer 1 语义 / Layer 2 布局专属
3. **拆分组件**：原型 HTML → `index.vue`（骨架）+ `Sidebar.vue` + `Navbar.vue`
4. **替换静态数据**：原型中的示例菜单 → `menuStore.treeMenus`
5. **接入框架组件**：原型中的按钮/图标 → `LayoutSwitcher` / `ThemeSwitcher` 等
6. **保留交互逻辑**：折叠/展开/下拉等交互，参考 CyberLayout 的实现方式

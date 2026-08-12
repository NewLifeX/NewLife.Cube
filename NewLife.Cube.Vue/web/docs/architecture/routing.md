# 路由、菜单与页面发现

## 静态路由

`core/routes/index.ts` 提供 `/`、`/login`、`/unauthorized`、`/loading` 和 catch-all 路由。catch-all 指向 `core/pages/DefaultEntity.vue`，用于在未命中显式业务路由时按菜单解析默认页面。

## 初始化与守卫

`core/router/index.ts` 创建 Router，并在导航前依次：

1. 等待 `initAppRoutes()` 完成微应用路由加载；未完成时转入 `/loading`。
2. 处理 URL hash token。
3. 对受保护路由检查 access token；无 token 时跳转登录页。
4. 拉取用户信息和菜单。
5. 以菜单叶子节点调用 `registerMenuRoutes()` 注册动态路由。
6. 更新当前激活菜单；当前路径刚被注册时使用 `router.replace()` 重新命中。

## 微应用路由

`configs/microAppConfig.json` 定义微应用；`core/plugin/index.ts` 生成 `virtual:@newlifex/cube-vue-micro-apps`；`core/microAppRouter.ts` 导入每个应用的 `src/main.ts` 并注册其导出的 `routes`。

应用路由使用 `router.addRoute()` 注册。应用显式注册的相同路径优先，菜单动态注册不会覆盖已有路径。

## 菜单动态路由

`core/stores/menu.ts` 拉取菜单并转换为平铺/树形结构。`core/utils/menuRoutes.ts` 选择叶子菜单，为每个路径注册路由。

业务视图解析顺序：

1. `apps/*/src/views/**/index.vue` 中与菜单路径匹配的视图。
2. `core/apps/*/src/views/**/index.vue` 中的同类视图。
3. 框架 `core/views/index.vue` 后备默认列表页。

解析器兼容 PascalCase、kebab-case 和小写目录；同一候选下优先工作区业务应用和 kebab-case 路径。路由命名风格由 `router.routeNamingStyle`（`pascal` 或 `kebab`）控制。

## Section 覆盖

`cubeFront()` 插件扫描子应用 `src/views/` 中首字母大写的 `.vue` 文件，生成 `virtual:@newlifex/cube-vue-sections`。`initApp()` 将其注册为 `PageSectionRegistryKey`。

文件约定：

```text
apps/<app>/src/views/<route-path>/<SectionName>.vue
```

例如 `apps/cube-admin/src/views/admin/user/ListSearchBar.vue` 会注册到 `/admin/user`。可用 Section 名称在 `core/composables/useSections.ts` 的 `SectionKeyMap` 中定义。

完整取舍见 [ADR 0002](../decisions/0002-menu-driven-routing-and-section-overrides.md)；操作步骤见 [新增页面](../guides/add-page.md) 与 [覆盖页面](../guides/customize-page.md)。

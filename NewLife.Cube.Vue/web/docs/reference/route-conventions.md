# 路由与视图约定参考

| 项目         | 当前约定                                       |
| ------------ | ---------------------------------------------- |
| 框架入口     | `index.html` 加载 `/core/main.ts`              |
| 静态路由     | `core/routes/index.ts`                         |
| 动态菜单路由 | `core/utils/menuRoutes.ts`                     |
| 微应用清单   | `configs/microAppConfig.json`                  |
| 应用路由导出 | `apps/<app>/src/main.ts` 导出 `routes`         |
| 业务页面     | `apps/<app>/src/views/**/index.vue`            |
| Section 覆盖 | 同目录下首字母大写的 `<SectionName>.vue`       |
| 动态路径风格 | `router.routeNamingStyle`：`pascal` 或 `kebab` |

动态菜单路径优先解析业务应用视图；未匹配则使用 `core/views/index.vue` 默认列表页。已存在的应用级路由不会被菜单动态注册覆盖。

允许自动发现的 Section 名称以 `core/composables/useSections.ts` 的 `SectionKeyMap` 为准，例如 `ListSearchBar`、`ListToolbar`、`ListTableContent`、`FormContent` 和 `FormActions`。

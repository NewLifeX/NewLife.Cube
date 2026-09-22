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

应用路由经 `core/router/routeOverride.ts` 的 `addFrameworkRoute()` 收口注册。应用声明的相同路径优先，菜单动态注册不会覆盖已有路径；业务应用要用 `initApp({ externalRoutes })` 而非裸 `router.addRoute()`（见下节）。

## 菜单动态路由

`core/stores/menu.ts` 拉取菜单并转换为平铺/树形结构。`core/utils/menuRoutes.ts` 选择叶子菜单，为每个路径注册路由。

业务视图解析顺序：

1. `apps/*/src/views/**/index.vue` 中与菜单路径匹配的视图。
2. `core/apps/*/src/views/**/index.vue` 中的同类视图。
3. 框架 `core/views/index.vue` 后备默认列表页。

解析器兼容 PascalCase、kebab-case 和小写目录；同一候选下优先工作区业务应用和 kebab-case 路径。路由命名风格由 `router.routeNamingStyle`（`pascal` 或 `kebab`）控制。

## 路由优先级（外部路由优先）

vue-router 的匹配是「matchers 数组中首个命中者胜出」，同形态 path 按注册先后排列，**先注册者胜**；只有同 name 时后者才会顶掉前者。框架静态路由在 `createRouter()` 阶段就已注册，业务应用无论如何都晚于它，因此裸调 `router.addRoute()` 无法覆盖框架路由（典型症状：业务声明的 `/` 首页重定向变成死路由，`resolve('/')` 仍命中框架的 `home`）。

`core/router/routeOverride.ts` 把「谁能覆盖谁」变成显式声明：

| 角色 | 入口 | 行为 |
| --- | --- | --- |
| 业务外部路由 | `initApp({ externalRoutes })` / `addExternalRoute()` | 登记声明表（按 name 与归一化 path 双索引）→ 清理框架同 name / 同形态 path 的已登记项 → 注册 |
| 框架侧所有来源 | `addFrameworkRoute()` | 注册前查声明表，命中则跳过并告警 |

框架侧三个注册点已全部收口：`core/router/index.ts` 的静态表、`core/microAppRouter.ts` 的微应用路由、`core/utils/menuRoutes.ts` 的菜单动态路由。**新增框架侧注册点必须复用 `addFrameworkRoute()`**，否则外部声明无法阻止晚注册的框架路由抢占同形态路径。

判定同形态时用 `normalizeRoutePath()`（`/X/:id`、`/X/:name`、`/X/:id(\d+)`、catch-all 归一化为同一 key），大小写敏感。

`addExternalRoute()` 返回的撤销函数会**同时撤销声明**（只撤路由不撤声明会让框架永久让位，表现为「框架页面莫名不可达」）；同理 `addFrameworkRoute()` 返回的撤销函数会同步清理记录表，避免长期运行后残留失效记录。

外部路由在 `app.use(router)` 之前落盘（`initApp` 内部处理），先于首次导航解析。覆盖操作只告警不拦截：覆盖 `/login` `/loading` `/unauthorized` 等安全关键路径、覆盖 catch-all、缺字符串 name、既无 component 也无 redirect 时输出 `[路由优先级]` 开头的 `console.warn`。

单元测试：`core/__tests__/routeOverride.spec.ts`。

## Section 覆盖

`cubeFront()` 插件扫描子应用 `src/views/` 中首字母大写的 `.vue` 文件，生成 `virtual:@newlifex/cube-vue-sections`。`initApp()` 将其注册为 `PageSectionRegistryKey`。

文件约定：

```text
apps/<app>/src/views/<route-path>/<SectionName>.vue
```

例如 `apps/cube-admin/src/views/admin/user/ListSearchBar.vue` 会注册到 `/admin/user`。可用 Section 名称在 `core/composables/useSections.ts` 的 `SectionKeyMap` 中定义。

完整取舍见 [ADR 0002](../decisions/0002-menu-driven-routing-and-section-overrides.md)；操作步骤见 [新增页面](../guides/add-page.md) 与 [覆盖页面](../guides/customize-page.md)。

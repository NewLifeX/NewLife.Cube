# NewLife.Cube.ArcoVue 前端源码

基于 **Vue 3 + Arco Design Vue + Vite** 的魔方管理后台前端。

## 技术栈

- Vue 3
- Arco Design Vue（字节跳动开源组件库）
- Vite 6
- Pinia 3
- TypeScript
- pnpm

## 开发

```bash
pnpm install
pnpm dev
```

默认开发端口 **5183**。Vite 代理见 `devProxy.ts`：

- 固定前缀：`/Admin`、`/Auth`、`/Mfa`、`/Cube`、`/Sso`、`/api`
- **业务 Area 通配**：`/^/[A-Z]…/`（如 `/School/Class/GetPage`）
- 浏览器 HTML 导航 `bypass` 回 SPA；XHR/fetch 转发后端

改代理后需**重启** `pnpm dev`。

单元测试：

```bash
pnpm test
```

嵌入宿主（如 CubeDemo）需启用本皮肤：

```csharp
// app.UseVue(builder.Environment);
app.UseArcoVue(builder.Environment);
```

勿与 `UseVue` / `UseReact` 等同时启用。仓库演示默认仍可为 `UseVue`；本地联调 ArcoVue 时再切换。

## 构建

构建产物输出到 `../wwwroot/`，嵌入 .NET 程序集作为静态资源：

```bash
pnpm build
```

## 壳与 UserProfile

- 布局由 `userProfile.layout.mode`（`side` / `top` / `mix`）经 `layouts/RootLayout.vue` 动态切换。
- 主题 / 密度写入 CSS 变量与 `arco-theme`，持久化到 `GET/PUT /Cube/UserProfile`。
- 外观设置：`/settings/appearance`；顶栏提供主题、密度、设置入口。
- CRUD 页面不读取壳偏好 store（契约隔离）。

## 列表与 EntityViewProfile

- 默认列表主表为 **VisActor VTable**（`features/vtable/ListTable.vue`）。
- 列布局 / 多命名视图（仅 table）经 `GET/PUT/DELETE /Cube/EntityViewProfile` 持久化。
- 工具条：命名视图切换 +「配置」抽屉；数据行**双击**或操作列打开右侧详情抽屉。

## 目录结构

```
web/
├── src/
│   ├── api/          # API 调用层（复用 @cube/api-core）
│   ├── components/   # 组件（含 TagsView）
│   ├── layouts/      # RootLayout + side/top/mix
│   ├── theme/        # 主题 token / 密度 CSS
│   ├── router/       # 路由配置
│   ├── stores/       # Pinia（含 userProfile、tagsView）
│   ├── views/        # 页面视图（含 settings/appearance）
│   ├── App.vue
│   └── main.ts
├── index.html
├── vite.config.ts
├── tsconfig.json
└── package.json
```

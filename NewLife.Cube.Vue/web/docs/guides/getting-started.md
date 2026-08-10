# 开始开发

## 前置条件

- Node.js >= 24
- pnpm >= 9
- 后端 API 可访问（本地默认由 `VITE_API_URL` 指向）

## 安装与启动

在 `web/` 目录执行：

```powershell
pnpm install
pnpm dev
```

开发服务器默认使用端口 `5187`。根 `index.html` 实际加载 `/core/main.ts`，因此修改默认模板应从 `core/` 开始，而不是根 `src/`。

## 常用检查

```powershell
pnpm run type-check
pnpm run lint:eslint
pnpm run test:unit
pnpm build
```

构建产物写入 `../wwwroot`。开发 API 地址配置在 `.env.development` 的 `VITE_API_URL`；请求不经 Vite proxy。配置优先级与运行时覆盖见 [build-and-runtime.md](../architecture/build-and-runtime.md)。

## 第一次定位代码

| 目标              | 从哪里开始                                                |
| ----------------- | --------------------------------------------------------- |
| 默认列表/表单行为 | `core/views/`、`core/pages/DefaultEntity.vue`             |
| 路由与菜单        | `core/router/`、`core/utils/menuRoutes.ts`                |
| 业务应用页面      | `apps/<app>/src/views/`                                   |
| 主题与布局        | `core/themes/`、`core/layouts/`、`src/theme/tailwind.css` |
| 请求与响应        | `core/utils/request.ts`、`core/utils/response.ts`         |

继续前先读 [架构概览](../architecture/overview.md)；具体任务从 [guides/](./) 选择。

# 构建与发布

## 本地构建

在 `web/` 目录执行：

```powershell
pnpm build
```

Vite 默认将产物写入 `../wwwroot`，并生成 source map。发布前建议顺序执行：

```powershell
pnpm run type-check
pnpm run lint:eslint
pnpm run test:unit
pnpm build
```

## 运行时配置

构建时读取 `configs/config.production.ts`。`cubeFront()` 插件会把该文件中的 `BUILD_*` 占位符注入 HTML，部署环境可写入 `window._CUBE_CONFIG_` 覆盖运行时配置。

运行时覆盖只用于公开配置，例如 API 基地址；不得把密钥、令牌或私有凭据打入前端资源。

## 微应用清单

发布前确认 `configs/microAppConfig.json` 只包含需要发布的应用，且每个应用包路径可解析并导出 `routes`。错误清单会导致微应用路由初始化失败，用户被转到 `/loading`。

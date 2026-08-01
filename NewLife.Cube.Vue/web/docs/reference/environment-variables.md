# 环境变量参考

| 文件               | 用途                   | 当前变量                                                      |
| ------------------ | ---------------------- | ------------------------------------------------------------- |
| `.env`             | 通用 Vite 环境设置     | `VITE_PORT`、`VITE_OPEN`、`VITE_OPEN_CDN`、`VITE_PUBLIC_PATH` |
| `.env.development` | 本地开发后端和资源地址 | `VITE_API_URL`、`VITE_IMG_BASE_URL`                           |
| `.env.production`  | 生产构建环境变量       | 以文件实际内容为准                                            |

## 开发 API 地址

`configs/config.development.ts` 将 `request.baseUrl` 设为 `import.meta.env.VITE_API_URL || ''`。请求层会将非绝对 API 路径拼到这个基地址；不要依赖历史文档中的 Vite proxy。

默认 Playwright E2E 后端地址在 `playwright.config.ts` 的 `PLAYWRIGHT_API_URL` 默认值；需要联调其他后端时，同时检查它和 `VITE_API_URL`。

## 运行时覆盖

生产环境可以通过 `window._CUBE_CONFIG_` 覆盖 `configs/config.production.ts` 的值，优先级高于静态配置。部署层只能覆盖配置，不应向前端注入密钥或其他敏感信息。

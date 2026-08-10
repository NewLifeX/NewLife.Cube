# 构建与运行时

## Vite

根 `vite.config.ts` 配置 Vue、Vue JSX、自动导入、Element Plus Resolver、开发工具和 `cubeFront()` 插件。默认构建输出到 `../wwwroot`，开发服务器端口为 `5187`。

`cubeFront()` 生成三个运行时虚拟模块：

| 虚拟模块                                | 来源                           | 用途                     |
| --------------------------------------- | ------------------------------ | ------------------------ |
| `virtual:@newlifex/cube-vue-micro-apps` | `configs/microAppConfig.json`  | 导入并注册微应用路由     |
| `virtual:@newlifex/cube-vue-config`     | `configs/config.ts` 与环境配置 | 提供合并前配置和当前环境 |
| `virtual:@newlifex/cube-vue-sections`   | 子应用 `src/views/`            | 自动注册 Section 覆盖    |

## 配置优先级

`core/configure/index.ts` 的 `getConfig()` 按从低到高合并：

1. `core/configure/defaultConfig/`
2. `configs/config.ts`
3. `configs/config.<mode>.ts`
4. 浏览器 `window._CUBE_CONFIG_`

生产构建会从环境配置中提取 `BUILD_*` 占位符并注入 HTML，供部署环境覆盖。

## 环境变量

开发时用 `.env.development` 的 `VITE_API_URL` 指向后端。前端请求直连该地址，根 Vite 配置没有启用 API proxy。完整变量说明见 [environment-variables.md](../reference/environment-variables.md)。

## 应用边界

`apps/` 内的应用由 `configs/microAppConfig.json` 决定是否参与运行。应用 `src/main.ts` 必须导出 `routes`，由微应用路由中心异步加载。

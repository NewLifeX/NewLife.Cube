# 排障手册

| 现象                        | 首先检查                                                 | 可能原因                                                       |
| --------------------------- | -------------------------------------------------------- | -------------------------------------------------------------- |
| 页面停在 `/loading`         | 浏览器控制台与 `configs/microAppConfig.json`             | 微应用路由尚未初始化、应用模块导入失败                         |
| 有菜单但访问到默认/404 页面 | `core/utils/menuRoutes.ts`、业务 `views/**/index.vue`    | 菜单路径与视图目录不匹配，或应用未在清单注册                   |
| Section 覆盖未生效          | 文件名、目录、`SectionKeyMap`                            | 文件不是 PascalCase、名称未注册、路径不对应菜单路由            |
| 请求地址错误                | `.env.development`、`configs/config.*.ts`、`getConfig()` | `VITE_API_URL` 或环境配置未生效                                |
| 反复跳登录/未授权           | `core/utils/request.ts`、token、用户信息接口             | 401、登录地址配置或后端会话失效                                |
| 构建失败                    | `package.json` 脚本与具体缺失模块                        | 先执行 `pnpm run type-check` 定位类型问题，再执行 `pnpm build` |
| 单测启动异常                | `vitest.config.unit.ts`                                  | 测试应使用最小 Vitest 配置，避免依赖全量 Vite 插件             |

排障时优先收集：当前 URL、菜单路径、控制台首个异常、请求 URL/状态码、`traceId` 和正在使用的环境配置。不要只凭 UI 现象修改路由或请求层。

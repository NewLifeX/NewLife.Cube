# 配置参考

配置类型在 `core/configure/types.d.ts`，默认值在 `core/configure/defaultConfig/`，最终读取入口是 `getConfig()`。

| 顶层键    | 用途                                     | 常见来源                   |
| --------- | ---------------------------------------- | -------------------------- |
| `base`    | 标题、Logo、页脚、环境标记               | `configs/config.ts`        |
| `request` | `baseUrl`、超时、请求头、请求/响应拦截器 | `configs/config.<mode>.ts` |
| `auth`    | token key、登录地址、重新登录行为        | 默认配置或应用配置         |
| `menu`    | 菜单请求、字段映射、树形结构             | 默认配置或应用配置         |
| `user`    | 当前用户请求配置                         | 默认配置或应用配置         |
| `router`  | 路径命名风格                             | 默认配置或应用配置         |
| `ui`      | 布局与主题偏好                           | 默认配置或应用配置         |

配置按默认、通用、环境、运行时四层覆盖。具体合并顺序见 [build-and-runtime.md](../architecture/build-and-runtime.md)。

新增配置前先判断它属于已有顶层域；不要创建平行的全局配置对象。涉及部署替换时优先使用 `BUILD_*` 占位符或 `window._CUBE_CONFIG_`，不要在构建产物中硬编码环境地址。

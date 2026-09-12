# @newlifex/api-core

魔方前端公共 API 调用层，框架无关。

- axios 封装：统一请求/响应拦截、401 跳转、错误处理
- Token 管理：存储、刷新、过期处理
- 密码安全：RSA 加密、Challenge-Response 流程
- 服务路径与类型定义（`CubeApi` / `DataField` / `UserInfo` 等）

```ts
import { createCubeApi } from '@newlifex/api-core';

const api = createCubeApi({ baseURL: '/', tokenStorage: localStorage });
```

## 安装

```bash
pnpm add @newlifex/api-core
```

框架无关，配合 `@newlifex/auth-logic` / `@newlifex/page-logic` 使用。

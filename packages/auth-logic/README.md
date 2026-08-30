# @newlifex/auth-logic

魔方前端认证业务逻辑，框架无关核心 + Pinia/Zustand 适配器。

- 登录 / 登出 / 注册 / 激活 / 找回密码
- 用户信息、菜单、权限
- 验证码、Challenge-Response 密码加密
- 适配器：`@newlifex/auth-logic/pinia`、`@newlifex/auth-logic/zustand`

```ts
import { createZustandAuthStore } from '@newlifex/auth-logic/zustand';

const store = createZustandAuthStore(api);
await store.login({ name: 'admin', password: '...' });
```

## 安装

```bash
pnpm add @newlifex/auth-logic
```

依赖 `@newlifex/api-core`；`pinia` / `zustand` 为可选 peer 依赖。

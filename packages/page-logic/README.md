# @newlifex/page-logic

魔方前端列表页业务编排逻辑，框架无关核心 + Pinia/Zustand 适配器。

- 列表页状态编排：字段 / 数据 / 分页 / 排序 / 搜索
- CRUD 操作封装
- 适配器：`@newlifex/page-logic/pinia`、`@newlifex/page-logic/zustand`

```ts
import { createPageStore } from '@newlifex/page-logic/zustand';

const store = createPageStore(api, { entity: 'User' });
await store.load();
```

## 安装

```bash
pnpm add @newlifex/page-logic
```

依赖 `@newlifex/api-core` / `@newlifex/field-mapping` / `@newlifex/page-utils`；`pinia` / `zustand` 为可选 peer 依赖。

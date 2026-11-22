# @newlifex/field-mapping

魔方前端字段映射模块：`DataField` 元数据到 UI 组件的映射规则引擎。

- 字段类型 → 控件类型（文本框 / 下拉 / 日期 / 字典 / LOV 等）
- 字段元数据归一化（`toCamelCase` 等）
- 框架无关，各皮肤在其基础上映射到具体组件库

```ts
import { resolveWidget, type DataField } from '@newlifex/field-mapping';

const widget = resolveWidget(field);
```

## 安装

```bash
pnpm add @newlifex/field-mapping
```

依赖 `@newlifex/api-core`。

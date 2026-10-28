# @newlifex/cube

魔方（NewLife.Cube）前端公共逻辑，框架无关。各前端皮肤（Vue / React / Angular / Svelte）共用同一套实现，避免每个皮肤各写一份。

| 能力 | 内容 |
|------|------|
| API 调用层 | axios 封装、统一请求/响应拦截、401 跳转、Token 管理、密码 RSA 加密（Challenge-Response） |
| 字段映射 | 后端 `DataField` 元数据 → 抽象控件类型、列宽、对齐方式 |
| 页面工具 | 权限码（`Auth`）、菜单查找、URL 变量替换、导出链接 |
| 认证逻辑 | 登录 / 登出 / 注册 / 激活 / 找回密码 / MFA，框架无关核心 + Pinia/Zustand 适配器 |
| 列表页编排 | 字段加载、分页查询、CRUD、导入导出、页签、工作台视图模型 |
| 类型定义 | `CubeApi` / `DataField` / `UserInfo` / `PageMeta` 等 |

## 安装

```bash
pnpm add @newlifex/cube
```

`pinia`、`vue`、`zustand` 都是**可选** peer 依赖，只有用到对应适配器入口时才需要安装。

## 使用

```ts
// 主入口：框架无关
import { createCubeApi, resolveWidgets, Auth, AuthLogic, PageLogic } from '@newlifex/cube';

const api = createCubeApi({ baseURL: '/', tokenStorage: localStorage });
```

```ts
// Vue 系（需要 pinia + vue）
import { createPiniaAuthStore, usePageLogic } from '@newlifex/cube/pinia';

export const useUserStore = createPiniaAuthStore(api);
```

```ts
// React 系（需要 zustand）
import { createZustandAuthStore, createPageStore } from '@newlifex/cube/zustand';

export const useUserStore = createZustandAuthStore(api);
```

## 入口

| 入口 | 用途 | 需要的 peer 依赖 |
|------|------|------------------|
| `@newlifex/cube` | 框架无关核心 | 无 |
| `@newlifex/cube/pinia` | Vue 皮肤适配器 | `pinia`、`vue` |
| `@newlifex/cube/zustand` | React 皮肤适配器 | `zustand` |

## 许可

MIT

---
name: cube-add-app
description: |
  在 cube-front 微前端架构中新增一个子应用/模块。
  当用户说"新增应用"、"创建子应用"、"添加新模块"、"新建页面"、"创建新页面"时使用。
  注意：新增的应用放在调用技能的目录（项目根目录 apps/），而不是 cube-front 目录。
---

# Cube-Front 新增应用

## 什么时候用

当用户需要新增一个子应用或新页面时使用。

## 最小步骤（新增应用只需4步）

1. **新建测试页面**：`{项目根目录}/apps/<app-name>/src/views/Test/index.vue`
2. **导出路由**：在 `apps/<app-name>/src/routes.ts` 添加路由导出
3. **新建 main.ts**：`apps/<app-name>/src/main.ts`，内容为 `export { default as routes } from './routes';`
4. **注册应用**：在 `{前端项目}/configs/microAppConfig.json` 添加配置

## 内置模板

### 1. 页面组件 (views/Test/index.vue)

```vue
<template>
  <div class="test-page">
    <h1>{{ title }}</h1>
    <p>这是 {{ name }} 页面</p>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';

const title = ref('测试页面');
const name = 'Test';
</script>

<style scoped>
.test-page {
  padding: 20px;
}
</style>
```

### 2. 路由导出 (apps/<app-name>/src/routes.ts)

```typescript
import type { RouteRecordRaw } from 'vue-router';

const routes: RouteRecordRaw[] = [
  {
    path: '/test',
    name: 'Test',
    component: () => import('./views/Test/index.vue'),
    meta: { title: '测试页面' },
  },
];

export default routes;
```


### 3. main.ts (apps/<app-name>/src/main.ts)

```typescript
export { default as routes } from './routes';
```

### 4. 微应用配置 (configs/microAppConfig.json)

```json
{
  "name": "<app-name>",
  "packageName": "/apps/<app-name>"
}
```

## 新增页面

在已有应用中新增页面只需2步：

1. **创建页面**：`apps/<app-name>/src/views/<Module>/index.vue`
2. **注册路由**：在 `apps/<app-name>/src/routes.ts` 添加路由

```typescript
{
  path: '/xxx',
  name: 'Xxx',
  component: () => import('./views/Xxx/index.vue'),
  meta: { title: '页面标题' },
},
```


## 常用工具

```typescript
import { getAccessToken } from 'cube-front/core/utils/token';
import { useUserStore } from 'cube-front/core/stores/user';

const token = getAccessToken();
const userStore = useUserStore();
```

## 验证

1. **重新启动应用**：`pnpm dev`，因为新增应用需要重新构建虚拟模块
2. **访问测试路由**：`/<app-name>/test`
3. **页面验证**：
   - 页面正常渲染 → 成功
   - 跳转到登录页 → 登录后重新访问 `/<app-name>/test`，确认页面正常显示
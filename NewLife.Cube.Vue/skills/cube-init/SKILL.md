---
name: cube-init
description: |
  初始化一个新的前端项目，使用 cube-front 框架。
  当用户说"初始化项目"、"创建新项目"、"搭建前端项目"、"使用 cube-front 初始化"时使用此技能。
  自动配置布局、状态管理(Pinia)、路由、API请求库、多语言支持、容器化部署等核心功能，开箱即用。
---

# Cube-Front 项目初始化

## 什么时候用

当用户需要创建一个新的前端项目，或在现有项目中引入 cube-front 框架时使用。

## 初始化步骤

### 1. 检查依赖

确保目标目录已安装/配置：
- `cube-front` workspace 依赖已添加（路径如 `../../cube-front`）
- `pnpm` 包管理器

### 2. 创建项目入口文件

在项目 `src/` 目录下创建 `main.ts`：

```typescript
// src/main.ts
import { initApp } from 'cube-front/core/initApp';
import 'cube-front/core/global.css';

initApp();
```

### 3. 创建 index.html

```html
<!DOCTYPE html>
<html lang="zh-CN">
  <head>
    <meta charset="UTF-8" />
    <link rel="icon" type="image/svg+xml" href="/vite.svg" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>应用名称</title>
  </head>
  <body>
    <div id="app"></div>
    <script type="module" src="/src/main.ts"></script>
  </body>
</html>
```

### 4. 创建配置文件

在项目根目录创建 `configs/` 目录：

**configs/config.ts** - 通用配置：
```typescript
import type { EnvConfig } from 'cube-front/core/configure/types';

export const config: EnvConfig = {
  base: {
    title: '应用名称',
    footer: '版权所有 © 2026',
  },
  ui: {
    theme: {
      primaryColor: '#1890ff',
    },
  },
  auth: {
    oauthUrl: '/login',
    getUserInfoAxiosConfig: {
      url: '/Admin/User/Info',
      method: 'GET',
    },
  },
};
```

**configs/config.development.ts** - 开发环境配置：
```typescript
import type { EnvConfig } from 'cube-front/core/configure/types';

export const config: EnvConfig = {
  request: {
    baseUrl: import.meta.env.VITE_APP_BASE_API || 'http://localhost:5000',
    timeout: 30000,
  },
};
```

**configs/config.test.ts** - 测试环境配置：
```typescript
import type { EnvConfig } from 'cube-front/core/configure/types';

export const config: EnvConfig = {
  request: {
    baseUrl: import.meta.env.VITE_APP_BASE_API || '/api',
    timeout: 30000,
  },
};
```

**configs/config.production.ts** - 生产环境配置（支持容器部署替换）：
```typescript
import type { EnvConfig } from 'cube-front/core/configure/types';

export const config: EnvConfig = {
  request: {
    // 如果需要在部署时通过环境变量动态替换，解开该注释
    // vite 插件会将该配置注入到 html 文件，替换打包后的 html 的占位符即可
    // 如果是容器的话，通过脚本 docker/entrypoint.sh 进行替换
    baseUrl: '${BUILD_REQUEST_BASE_URL}',

    // baseUrl: import.meta.env.VITE_APP_BASE_API || '/api',
    timeout: 30000,
  },
};
```

**configs/microAppConfig.json** - 微应用配置（空数组即可）：
```json
[]
```

### 5. 创建环境变量文件

**注意**：`VITE_APP_` 前缀的环境变量会被 Vite 自动加载。

**`.env`** - 通用环境变量：
```bash
# 通用环境变量
VITE_APP_TITLE=应用名称
```

**`.env.development`** - 开发环境变量（Vite dev 默认加载）：
```bash
# 开发环境变量
VITE_APP_ENV=development
VITE_APP_TITLE=应用名称 - 开发环境
VITE_APP_BASE_API=http://localhost:5000
```

**`.env.test`** - 测试环境变量（需 `vite --mode test`）：
```bash
# 测试环境变量
VITE_APP_ENV=test
VITE_APP_TITLE=应用名称 - 测试环境
VITE_APP_BASE_API=http://test-api.example.com
```

**`.env.production`** - 生产环境变量（Vite build 默认加载）：
```bash
# 生产环境变量
VITE_APP_ENV=production
VITE_APP_TITLE=应用名称
# 生产环境 API URL 由容器部署时通过 BUILD_REQUEST_BASE_URL 注入
VITE_APP_BASE_API=/api
```

### 6. 配置 Vite

**vite.config.ts**：
```typescript
import { defineConfig, loadEnv } from 'vite';
import vue from '@vitejs/plugin-vue';
import { resolve } from 'path';
import vitePluginCubeFront from 'cube-front/core/plugin';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');

  return {
    plugins: [
      vue(),
      vitePluginCubeFront(),
    ],
    resolve: {
      alias: {
        '@': resolve(__dirname, 'src'),
        'cube-front': resolve(__dirname, '../../cube-front'),
      },
      extensions: ['.mjs', '.js', '.ts', '.jsx', '.tsx', '.json', '.d.ts'],
    },
    server: {
      port: 3000,
      proxy: {
        '/api': {
          target: env.VITE_APP_API_URL || 'http://localhost:5000',
          changeOrigin: true,
          secure: true,
          ws: true,
        },
      },
    },
    build: {
      outDir: 'dist',
      sourcemap: !env.VITE_APP_ENV === 'prod',
    },
  };
});
```

### 7. 配置 TypeScript

**tsconfig.json**：
```json
{
  "compilerOptions": {
    "target": "ES2020",
    "useDefineForClassFields": true,
    "module": "ESNext",
    "lib": ["ES2020", "DOM", "DOM.Iterable"],
    "skipLibCheck": true,
    "moduleResolution": "bundler",
    "allowImportingTsExtensions": true,
    "resolveJsonModule": true,
    "isolatedModules": true,
    "noEmit": true,
    "jsx": "preserve",
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noFallthroughCasesInSwitch": true,
    "baseUrl": ".",
    "paths": {
      "@/*": ["./src/*"]
    }
  },
  "include": ["src/**/*.ts", "src/**/*.d.ts", "src/**/*.tsx", "src/**/*.vue"],
  "references": [{ "path": "./tsconfig.node.json" }]
}
```

**tsconfig.node.json**：
```json
{
  "compilerOptions": {
    "composite": true,
    "skipLibCheck": true,
    "module": "ESNext",
    "moduleResolution": "bundler",
    "allowSyntheticDefaultImports": true,
    "strict": true
  },
  "include": ["vite.config.ts"]
}
```

### 8. 配置 package.json

**package.json** scripts：
```json
{
  "scripts": {
    "dev": "vite",
    "dev:test": "vite --mode test",
    "build": "vite build",
    "build:test": "vite build --mode test",
    "preview": "vite preview",
    "lint": "eslint . --ext .vue,.js,.jsx,.cjs,.mjs --fix"
  }
}
```

**Vite 默认模式规则**：
- `vite` → mode=development，自动加载 `.env` + `.env.development`
- `vite build` → mode=production，自动加载 `.env` + `.env.production`
- `vite --mode test` → mode=test，需手动指定

### 9. 创建 Docker 配置

在项目根目录创建 `docker/` 目录：

**docker/Dockerfile**：
```dockerfile
# 构建阶段
FROM node:20-alpine AS builder

WORKDIR /app

# 安装 pnpm
RUN npm install -g pnpm

# 安装依赖
COPY package.json pnpm-lock.yaml* ./
RUN pnpm install --frozen-lockfile --prod

# 复制源代码
COPY . .

# 构建（支持环境变量）
ARG VITE_APP_BASE_API
ENV VITE_APP_BASE_API=$VITE_APP_BASE_API

RUN pnpm run build

# 生产阶段
FROM nginx:alpine

# 复制构建产物
COPY --from=builder /app/dist /usr/share/nginx/html

# 复制入口点脚本
COPY docker/enterpoint.sh /docker-entrypoint.d/enterpoint.sh
RUN chmod +x /docker-entrypoint.d/enterpoint.sh

# 暴露端口
EXPOSE 80

# 设置入口点
CMD ["nginx", "-g", "daemon off;"]
```

**docker/enterpoint.sh**：
```bash
#!/bin/bash
set +e
set -x

echo "🚀 Frontend Container Starting..."

# 替换 BUILD_ 开头的占位符（vite 插件注入到 index.html 的内联脚本）
# sed 找不到匹配时不会报错，直接执行即可
sed -i "s|BUILD_REQUEST_BASE_URL|${BUILD_REQUEST_BASE_URL:-/}|g" /usr/share/nginx/html/index.html

# 可以在这里添加更多 BUILD_ 变量替换...

echo "✅ 配置完成"
echo "🎉 启动 Nginx..."

# 执行原始入口点命令
exec "$@"
```

### 10. 配置 package.json（完整示例）

```json
{
  "name": "your-app",
  "version": "1.0.0",
  "type": "module",
  "packageManager": "pnpm@9.15.4",
  "scripts": {
    "dev": "vite",
    "dev:test": "vite --mode test",
    "build": "vite build",
    "build:test": "vite build --mode test",
    "preview": "vite preview",
    "lint": "eslint . --ext .vue,.js,.jsx,.cjs,.mjs --fix"
  },
  "dependencies": {
    "cube-front": "workspace:../../cube-front",
    "vue": "^3.5.0",
    "vue-router": "^4.5.0",
    "pinia": "^3.0.0",
    "axios": "^1.8.0",
    "element-plus": "^2.9.0",
    "@element-plus/icons-vue": "^2.3.1",
    "@microsoft/signalr": "^8.0.0"
  },
  "devDependencies": {
    "@vitejs/plugin-vue": "^5.2.0",
    "vite": "^6.2.0",
    "sass": "^1.85.0",
    "eslint": "^9.20.0",
    "eslint-plugin-vue": "^10.0.0"
  }
}
```

## 配置优先级（从低到高）

```
defaultConfig → configs/config.ts → configs/config.{env}.ts → window._CUBE_CONFIG_ → BUILD_ 占位符
```

## BUILD_ 占位符机制

用于生产环境容器部署时动态注入配置：

1. 在 `configs/config.production.ts` 中使用 `baseUrl: '${BUILD_REQUEST_BASE_URL}'`
2. Vite 构建时会自动将 `BUILD_XXX` 占位符注入到 `dist/index.html`
3. 容器启动时，`docker/enterpoint.sh` 通过 sed 替换占位符

生成的 html 内联脚本格式：
```html
<script>
let cubeConfig = window._CUBE_CONFIG_ || (window._CUBE_CONFIG_={});
let request = cubeConfig["request"] || (cubeConfig["request"]={});
request["baseUrl"] = "BUILD_REQUEST_BASE_URL";
</script>
```

## 框架提供的能力

初始化完成后，框架自动提供：

| 功能 | 说明 | 如何使用 |
|------|------|----------|
| **布局系统** | MainLayout 主布局，侧边栏+内容区 | 通过 `LayoutKey` 依赖注入自定义 |
| **状态管理** | UserStore 用户状态，MenuStore 菜单状态 | `useUserStore()`, `useMenuStore()` |
| **路由系统** | 动态路由，微前端支持 | 通过后端菜单动态生成 |
| **API请求** | 带 Token、401处理、错误提示的 Axios 封装 | `import request from 'cube-front/core/utils/request'` |
| **国际化** | Vue I18n，支持动态切换 | `intl.get('key').d('默认值')` |
| **页面覆盖** | Section 机制，可覆盖框架组件 | 在 `views/` 下创建大写开头的 Vue 文件 |
| **BUILD_ 配置注入** | 生产构建时自动注入到 html | 在 config.production.ts 使用 `${BUILD_XXX}` |

## 验证初始化成功

1. **开发环境验证**：
   - `pnpm dev` 启动
   - 访问 `http://localhost:3000`
   - 检查登录页面正常渲染

2. **生产构建验证**：
   - `pnpm build` 成功
   - 检查 `dist/index.html` 是否包含 BUILD_ 占位符脚本
   - 容器构建：`docker build -t your-app .`

## 多语言配置（可选）

默认中文，如需多语言在 `src/i18n/` 下配置：
- `src/i18n/index.ts` - I18n 实例
- `src/i18n/locales/` - 语言文件目录
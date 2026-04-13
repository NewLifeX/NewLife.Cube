# NewLife.Cube.NaiveUI 前端源码

基于 **Vue 3 + Naive UI + Vite** 的魔方管理后台前端。

## 技术栈

- Vue 3 (Composition API + `<script setup>`)
- Naive UI（暗黑主题原生支持）
- Vite 6
- TypeScript
- pnpm

## 开发

```bash
pnpm install
pnpm dev
```

## 构建

构建产物输出到 `../wwwroot/`，嵌入 .NET 程序集作为静态资源：

```bash
pnpm build
```

## 目录结构

```
web/
├── src/
│   ├── api/          # API 调用层（复用 @cube/api-core）
│   ├── components/   # 公共组件
│   ├── layouts/      # 布局组件
│   ├── router/       # 路由配置
│   ├── stores/       # Pinia 状态管理
│   ├── views/        # 页面视图
│   ├── App.vue
│   └── main.ts
├── index.html
├── vite.config.ts
├── tsconfig.json
└── package.json
```

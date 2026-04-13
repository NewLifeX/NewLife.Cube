# NewLife.Cube.Vuetify 前端源码

基于 **Vue 3 + Vuetify 3 + Vite** 的魔方管理后台前端。

## 技术栈

- Vue 3
- Vuetify 3（Material Design 组件库）
- Vite 6
- Pinia 3
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
│   ├── components/   # 组件
│   ├── layouts/      # 布局组件
│   ├── plugins/      # Vuetify 插件配置
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

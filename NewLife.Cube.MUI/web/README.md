# NewLife.Cube.MUI 前端源码

基于 **React 18 + Material UI 6 + Vite** 的魔方管理后台前端。

## 技术栈

- React 18
- Material UI 6 (MUI)（Google Material Design 风格）
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
│   ├── router/       # 路由配置（React Router）
│   ├── stores/       # Zustand 状态管理
│   ├── pages/        # 页面视图
│   ├── App.tsx
│   └── main.tsx
├── index.html
├── vite.config.ts
├── tsconfig.json
└── package.json
```

# NewLife.Cube.Shadcn 前端源码

基于 **React 18 + Shadcn UI + Tailwind CSS + Vite** 的魔方管理后台前端。

## 技术栈

- React 18
- Shadcn UI（基于 Radix UI 的无头组件 + Tailwind CSS）
- Tailwind CSS 4
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
│   ├── components/   # Shadcn UI 组件 + 自定义组件
│   ├── layouts/      # 布局组件
│   ├── router/       # 路由配置（React Router）
│   ├── stores/       # Zustand 状态管理
│   ├── pages/        # 页面视图
│   ├── lib/          # 工具函数（cn 等 Shadcn 约定）
│   ├── App.tsx
│   └── main.tsx
├── index.html
├── vite.config.ts
├── tailwind.config.ts
├── tsconfig.json
└── package.json
```

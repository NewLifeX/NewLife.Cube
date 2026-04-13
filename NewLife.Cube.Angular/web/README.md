# NewLife.Cube.Angular 前端源码

基于 **Angular 19 + NG-ZORRO (Ant Design for Angular)** 的魔方管理后台前端。

## 技术栈

- Angular 19
- NG-ZORRO（Ant Design Angular 组件库）
- TypeScript
- Angular CLI / esbuild

## 开发

```bash
npm install
ng serve
```

## 构建

构建产物输出到 `../wwwroot/`，嵌入 .NET 程序集作为静态资源：

```bash
ng build --output-path ../wwwroot
```

## 目录结构

```
web/
├── src/
│   ├── app/
│   │   ├── api/          # API 调用层（复用 @cube/api-core）
│   │   ├── components/   # 共享组件
│   │   ├── layouts/      # 布局组件
│   │   ├── pages/        # 页面组件
│   │   ├── services/     # 服务
│   │   ├── stores/       # 状态管理
│   │   ├── app.component.ts
│   │   ├── app.config.ts
│   │   └── app.routes.ts
│   ├── index.html
│   ├── main.ts
│   └── styles.css
├── angular.json
├── tsconfig.json
└── package.json
```

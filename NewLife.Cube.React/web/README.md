# NewLife.Cube.React

魔方（NewLife.Cube）第三代 WebAPI 前端的 **React 皮肤**。基于 **React 18 + Ant Design 5 + Zustand + Vite**，由后端字段元数据（`GetPage`）驱动，动态渲染通用列表页 / 表单页，对齐 Vue 皮肤功能。

> 本项目为**全新重构**（原 UMI Max 版本已废弃），依赖公共前端库 `packages/`（`@cube/*`）。

---

## 技术栈

| 层 | 选型 |
|----|------|
| 构建 | Vite 6（产物输出 `../wwwroot`，由 .NET 项目嵌入程序集） |
| UI | Ant Design 5 + @ant-design/icons |
| 状态 | Zustand 5（登录态 / 菜单 / 多标签 / 主题） |
| 路由 | react-router v6（静态路由 + catch-all `*` → 动态实体页） |
| 数据 | axios（`@cube/api-core`）+ `@cube/page-logic` 页面 Store |
| 图表 | ECharts 5（懒加载） |
| AI | `marked` + `dompurify` + mermaid（CDN 懒加载） |
| 测试 | Vitest 3 + RTL（单测）；Playwright 1.54（E2E） |

---

## 目录结构

```
web/
├─ src/
│  ├─ configure/        # 配置系统（默认配置 + window._CUBE_CONFIG_ 运行时覆盖）
│  ├─ api/              # 全局 CubeApi 客户端（token 事件 / 统一错误提示）
│  ├─ stores/           # user / menu / tabs / theme 各 Zustand store
│  ├─ router/           # 静态路由表 + RouteMeta
│  ├─ layouts/          # RootLayout / MainLayout（Sider 菜单、Header、多标签、Footer）
│  ├─ pages/            # Login / Register / Activate / ForgotPassword / ProfileSecurity / Home / DefaultEntity / NotFound
│  ├─ views/            # list（通用列表页）+ form（表单弹窗 / 表单页）
│  ├─ components/       # field 控件族、ai 助手、公共组件
│  ├─ hooks/            # usePageStore / useLoginConfig / useLookup / useLovOptions
│  ├─ utils/            # fieldControl / url / passwordRules / icon / menuItems
│  └─ types/            # FieldMeta / ControlType 等共享类型
├─ e2e/                 # Playwright E2E（auth.setup 会话复用 + login/list 场景）
├─ tests/               # Vitest 环境与全局 mock
├─ Doc/                 # 功能清单.md + 架构设计.md
└─ package.json
```

---

## 本地开发

前置：先启动后端 API（如 `CubeDemo`，默认 `http://localhost:5050`），`/api` 与 `/Ai` 由 Vite 代理转发（剥掉 `/api` 前缀）。

```bash
# 安装依赖（仓库根执行一次即可）
pnpm install

# 启动开发服务器（端口 5188）
pnpm dev
```

## 构建

```bash
pnpm build
```

构建产物输出到 `../wwwroot/`，由 .NET 项目 `NewLife.Cube.React.csproj` 嵌入程序集，通过 `app.UseReact(env)` 以 SPA 回退方式提供。

## 测试

```bash
# 单元测试（Vitest，53 个）
pnpm test:unit

# E2E 测试（Playwright，13 个；需后端运行于 5050 端口）
pnpm test:e2e
```

---

## 文档

- 功能清单：`web/Doc/功能清单.md`（55/55 完成，含验收记录）
- 架构设计：`web/Doc/架构设计.md`
- 需求参考：公共 `Doc/需求文档.md`（SPA 章节）、`Doc/Api/魔方前端内置需求.md`

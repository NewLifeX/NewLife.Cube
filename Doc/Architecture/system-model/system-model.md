# 魔方 NewLife.Cube 系统模型（C4）

- 产出技能：`system-modeler` + `c4model`
- 视图状态：**current-state（现状）**，2026-09-01，分支 `x_master`（HEAD `6282baa7`）
- 图源码：[`newlife-cube.dsl`](./newlife-cube.dsl)（Structurizr DSL，可在 Qoder Canvas 以 DSL 格式预览）

## 目的与读者

回答"魔方是什么、由哪些可部署/可发布单元组成、边界在哪"。
读者：框架维护者、准备通过 NuGet/npm 集成的开发者。

## 阅读顺序

1. `landscape` —— 人、系统、外部依赖总览。
2. `context` —— 魔方系统边界与外部关系。
3. `containers` —— 双核心库、主题包、演示宿主、测试工程的容器划分。
4. `components-core` —— WebAPI 核心库内部 9 个组件。
5. `演示与开发机` 部署视图 —— 三个宿主进程的单机运行形态。

## 关键架构结论（证据见 evidence 文档）

1. **双形态同源核心**：`NewLife.Cube`（WebAPI/前后端分离，NuGet: NewLife.Cube）与
   `NewLife.CubeNC`（MVC/Razor，NuGet: NewLife.Cube.Core，AssemblyName 同为 NewLife.Cube）。
   两者通过 csproj `<Compile Include>` **双向链接源码**——这是本仓库最重要的结构特征，
   也是最大的耦合点（改一处共享文件同时影响两个包）。
2. **主题二分法**：7 个 MVC Razor 主题（引用 CubeNC）+ 9 个 SPA 皮肤（独立前端工程，
   引用 NewLife.Cube，Vite 产物写入 wwwroot 随 NuGet 分发）。
3. **SSO 双角色**：魔方既是 OAuth2 客户端（14 个第三方 provider），也是 OAuth2 服务端；
   `CubeSSO` 是服务端角色的独立部署宿主。
4. **数据访问委托 XCode**：仓库内无直接 SQL，数据库由宿主连接串 `provider=` 后缀决定，
   演示默认 SQLite 三库（Membership / Cube / Log）。
5. **多租户为请求级中间件**：`DataScopeMiddleware` + `TenantContext`（AsyncLocal），
   仓库内不存在名为 `TenantProvider` 的类。

## 假设与不确定项（图中已标注）

- `CubeSSO → 第三方 OAuth 级联登录`：标注 `inferred`（服务端端点齐全，但未找到直接调用证据）。
- `NewLife.CubeST`：仅剩 bin/obj 残留，标注为已废弃，未纳入模型。
- `NewLife.Cube.React.csproj` 被 `魔方3.sln` 与 CubeDemo.csproj 引用但磁盘缺失（陈旧引用）；
  React 已独立仓库维护，模型中不单独列出。
- 支持的数据库全集未验证（仅见 sqlite 在用、mysql 示例）。

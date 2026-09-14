# 部署与发布拓扑（deployment-topology）

- 产出技能：`deployment-topology-analyzer` + `graphviz`
- 视图状态：**current-state**，2026-09-01，分支 `x_master`
- 图源码：[`deployment-topology.dot`](./deployment-topology.dot)

## 回答的问题

"魔方在哪里运行、如何发布、运行时依赖什么？"

## 运行时宿主（仓库内可见的部署单元）

| 宿主 | 端口证据 | 部署方式证据 |
| --- | --- | --- |
| CubeDemo（WebAPI 演示） | `CubeDemo\Properties\launchSettings.json` → https://localhost:7116；appsettings 注释 8080/8081 | 直接 `dotnet run`；注册星尘 |
| CubeDemoNC（MVC 演示） | `CubeDemoNC\appsettings.json:9` → http://*:7080; https://*:7081 | `bootstrap.sh`：`nohup dotnet cube.dll --urls http://* -basepath ../`（Linux 手工部署） |
| CubeSSO（SSO 服务端） | `CubeSSO\Properties\launchSettings.json` → :8080 | Kestrel 或 `CubeSSO\Dockerfile`（aspnet 基础镜像，EXPOSE 80，卷 `/cube`，含 Docker 启动配置） |

关键认知：**魔方是框架不是服务**——仓库内没有魔方自身的生产部署拓扑，生产形态由集成方
决定；仓库提供的是"可嵌入的 NuGet 包 + 三个可运行的参考宿主"。

## 数据层

- 演示默认 **SQLite 三库**（`CubeDemo\appsettings.json:11-19`）：Membership / Cube / Log，
  文件位于运行目录 `Data/`；`Migration=On` 自动建表。
- 换库机制：连接串尾部 `provider=` 后缀（XCode DAL 约定），注释中给出 `provider=mysql`
  示例；CubeDemo 引用 `NewLife.MySql`。数据库支持全集未在仓库内验证。
- **Redis 可选**：仅 CubeDemoNC 演示（`Startup.cs:34-57`，缓存/DataProtection/事件总线），默认注释关闭。

## 外部服务

- **星尘 Stardust**：三个宿主启动时 `AddStardust(null)` + `RegisterService(...)`（服务注册 + 配置中心）。
  服务端地址仅注释证据（`http://star.newlifex.com:6600`，low 置信）。
- 日志：XLog 文件日志落 `Log/`（`CubeDemoNC\Program.cs` 的 `logging.AddXLog()`）。

## 发布管道（构建期拓扑）

| 管道 | 触发 | 产物 |
| --- | --- | --- |
| `.github\workflows\test.yml` | 全分支 | 构建+测试 `魔方.sln`（注意：不含 NewLife.Cube.Tests） |
| `.github\workflows\publish.yml` | tag `v*` | dotnet pack ×3 → nuget.org + GitHub Packages |
| `.github\workflows\publish-beta.yml` | master 路径过滤 | `yyyy.MMdd-betaHHmm` 预发布包 |
| `.github\workflows\publish-npm.yml` | — | npm 前端包（@newlifex/*） |

前端构建链：pnpm workspace → 各 SPA 皮肤 Vite 构建（`outDir: ../wwwroot`）→ 产物以
EmbeddedResource 嵌入 .NET 项目随 NuGet 分发；Vue 另有独立 nginx:alpine Docker 镜像
（`NewLife.Cube.Vue\web\docker\Dockerfile`）。

## 不确定项

- CubeDemo 生产端口（8080/8081 仅在注释中）。
- 星尘服务端生产地址。
- CubeSSO 与演示站是否共用 Membership 库（图中标注 `inferred`）。
- E2E 编排脚本 `scripts\e2e-mvc.cs`（发布 CubeSSO → dotnet test E2EMvcTest）仅用于本地/脚本触发，
  其在 CI 中的挂载点未核实。

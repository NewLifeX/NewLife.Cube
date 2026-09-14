# 依赖与变更影响分析（module-dependencies）

- 产出技能：`dependency-impact-analyzer` + `graphviz`
- 视图状态：**current-state**，2026-09-01，分支 `x_master`
- 图源码：[`module-dependencies.dot`](./module-dependencies.dot)（Graphviz DOT，可在 Qoder Canvas 以 DOT 格式预览）

## 回答的问题

"谁依赖谁？改动会波及哪里？"

## 依赖方向（全部为编译期引用，证据：各 `.csproj`）

- 主题/皮肤 → 核心库（单向）：7 个 MVC 主题 → CubeNC；9 个 SPA 皮肤、Swagger → Cube。
- 宿主 → 核心库 + 主题：CubeDemo → Cube+Swagger+Vue+ArcoVue；CubeDemoNC → CubeNC+7 主题；CubeSSO → CubeNC。
- 测试 → 核心库：NewLife.Cube.Tests → Cube；E2EMvcTest/Test/XUnitTest → CubeNC。
- 核心库 → 外部 NuGet：NewLife.Core / XCode / IP / AI / Office / Stardust.Extensions。

## 关键风险点（按波及面排序）

1. **双向共享源码（红边）**：`NewLife.Cube.csproj:89/:95/:105/:123` 的双向
   `<Compile Include>` 使两个核心库共享 Jobs/Modules/Services/ViewModels/WebMiddleware
   与 AI/Common/Entity/OAuth 源码。改动共享文件 = 同时改变两个 NuGet 包，
   波及全部 16 个主题包与 3 个宿主。这是**最大的隐性耦合**，且方向不对称：
   Cube→NC 与 NC→Cube 链接的文件集不同，逐文件归属需查 csproj。
2. **XCode 基类下沉**：ManageProvider 基类、PermissionFlags、Entity DAL 在
   NewLife.XCode NuGet 包内（仓库外）。升级 XCode 版本（当前 12.1）是隐性破坏面。
3. **测试工程脱离 sln**：NewLife.Cube.Tests 不在 `魔方.sln`/`魔方3.sln` 中，
   本地"生成解决方案"不会编译它；其 CI 触发路径未核实 → 回归保护可能存在缺口。
4. **陈旧引用**：`NewLife.Cube.React.csproj` 被 `魔方3.sln` 与 CubeDemo.csproj
   引用但磁盘上不存在（React 已独立仓库）→ 构建依赖悬空引用，建议清理。
5. **无集中版本管理**：`Directory.Build.props` 仅含 NuGetAudit，版本号散落各
   csproj（核心 6.14、演示 6.8 + 日期后缀），多包发版易漂移。

## 变更影响速查

| 如果你要改… | 必须同时验证… |
| --- | --- |
| 共享源码文件（Services/Modules 等） | 两个核心包 + 全部宿主构建；两条 [`../flows/`](../flows/) 流程回归 |
| `Common/` 实体控制器基类 | 所有继承它的业务方控制器；权限特性行为 |
| `Membership/` 令牌逻辑 | 登录链 + OAuth 回调 + SSO 服务端三处令牌颁发路径 |
| 连接串/数据访问约定 | 演示站三库（Membership/Cube/Log）+ XCode Migration 行为 |
| 任一主题包 | 仅对应核心包形态（MVC 主题不影响 WebAPI 形态，反之亦然） |

## 不确定项

- 共享源码的逐文件归属与双向裁剪规则（各侧 `Compile Remove`）未完全核对。
- 支持数据库全集未验证（影响"换库"类变更的影响面评估）。

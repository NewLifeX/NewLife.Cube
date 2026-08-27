# NewLife.Cube.Tests

NewLife.Cube 的自动化测试工程（xUnit，net10.0）。本文档记录测试基座的约定，**新增测试前请先阅读**，避免重新踩进全局状态和并发的坑。

## 测试分层定位

本工程是**服务层集成测试**：手工构造 `HttpContext`，直接调用服务（`UserService.Login` / `Provider.TryLogin`）与实体 API，不起 Web 主机、不走 HTTP 管道。

- 验证对象：登录管线、JWT 签发/校验、租户上下文切换、实体仓储行为。
- 控制器路由、过滤器、`ApiResponse` 包装等 HTTP 层行为，不属于本工程职责。

## 并发与运行配置

- `xunit.runner.json` 已关闭集合级并行（`parallelizeTestCollections: false`），全工程串行执行。这是兜底防线：**不要为了加速而重新打开并行**，除非所有测试都已满足下文的进程级状态隔离要求。
- `[Collection]` 声明保留，作为隔离契约的文档化表达。

## 进程级静态状态契约

以下全局状态会被测试读写，**谁改动谁恢复**（`try/finally` 或夹具 `Dispose`）：

| 状态 | 说明 |
|---|---|
| `ManageProvider.Provider` / `ManageProvider2.Context` | 管理提供者装配。`ManageProvider2.Context` 为 internal，经 `InternalsVisibleTo` 直接赋值，**禁止反射注入** |
| `CubeSetting.Current` | `EnableTenant`、`TenantEnforceMode`、`TenantQueryPolicy`、`JwtSecret` 等开关 |
| `TenantContext.Current` | 当前租户上下文，测试结束必须清回 `null` |
| `DAL.ConnStrs` / DAL 实例缓存 | 连接串与连接映射，见下节 |

恢复范围可参考 `TenantAuthFixture.Dispose`：只恢复最易泄漏的开关与上下文；`ManageProvider` 装配保持不动（其它测试类可能依赖当前装配）。

## SQLite 数据策略（二选一）

**模式 A：集合级共享库 + 种子一次 + 唯一键隔离**（租户测试在用）

- `ICollectionFixture` 中建一次库、种子一次，整个集合内不再重建。
- 测试间靠唯一用户名/租户编码隔离，**不做逐测试清表**（清表与查询并发时必然偶发失败）。
- 集合声明 `DisableParallelization = true`。

**模式 B：每测试独立临时库**（`OAuthLogRetentionTests` 在用）

- 每测试用 `Guid` 命名独立临时 SQLite 文件，天然无冲突。
- **必须**在 `finally` 恢复全局连接串：`MapTo` 类映射串按 XCode 约定直写 `DAL.ConnStrs`；实体连接串用 `DAL.AddConnStr`（连接串变化时会自动 `Reset` 对应缓存实例）。

两个已知陷阱：

1. **`MapTo` 用 `TryAdd` 安装**：若连接名键已被别的连接串占住，映射会静默安装失败。覆写连接串的测试必须保证恢复干净。
2. **`DAL.Reset()` 是实例方法**：强制某连接名重新解析用 `DAL.Create("Cube").Reset()`，它同时清空表结构检查缓存。

## 测试接缝

主程序集通过 `InternalsVisibleTo("NewLife.Cube.Tests", PublicKey=...)` 开放 internal 成员。本工程用 `..\Doc\newlife.snk` 与主程序集同密钥签名，因此可用但**不得**对外发布。新增对 internal 成员的测试时直接引用，不要用反射。

## 现有测试集合

| 集合 | 测试类 | 基座 |
|---|---|---|
| `TenantAuth`（串行） | `TenantAuthReproTests`、`TenantChecklistIntegrationTests` | 共享 SQLite 夹具 `TenantAuthFixture` |
| （无） | `OAuthLogRetentionTests` | 每测试独立临时库 + 连接串恢复 |
| （无） | `MenuHelperCheckVisibleTests` | 纯静态方法，无全局状态 |

## 新增测试检查清单

- [ ] 是否触碰上表的进程级状态？是 → 必须有恢复逻辑
- [ ] 需要数据库？选模式 A（加入现有集合）或模式 B（独立临时库 + 恢复连接串），不要新造第三种
- [ ] 需要 internal 成员？直接用，不要反射
- [ ] 断言业务结果而非仅"无异常"；失败路径也要断言

# OSC-2608273d95 Retro

> 复盘时间：2026-09-14T22:35:00+08:00
> 触发：验收并复盘 3d95 变更（用户裁定「补齐 G1–G4 后再复盘」+「AC 判据按锁定包改写，不升包」）

## 结果摘要

| 维度 | 结果 |
|------|------|
| AC 通过率 | 25 条 AC 中 22 条已判定通过（单测/冒烟/审阅）；AC-04/07/12 判据按锁定包改写后通过；AC-08、AC-22 保留冒烟（代码路径已由 XCode 回归与 `Osc260903WidgetQueryTests` 覆盖） |
| 自动化门禁 | 双栈 build **0 警告 0 错误**；WebAPI 行权+新增族 **79/0**；MVC 族 **36/0**；XCode `DataScope` 只读回归 **92/0**；前端 Vitest **848**（本号未改前端） |
| 真机冒烟 | 隔离实例（5099 + 独立 SQLite）：四档行集 **1 / 6 / 7 / 1 / 10** 行、日志不扩权（15 行全自己）、越权详情 `非法访问数据[2]`、他人 Password `***`、导出 6 / 1 行 |
| 缺口 | P0 = 0；P1 ×4（G1 导入 / G2 AI 记录上下文 / G3 App 密钥令牌 / G4 内置部件）**已补齐并验证**；G9 判据已改写；G5–G8/G10 按裁定仅记录 |

## 范围回顾

| 维度 | 计划 | 实际 |
|------|------|------|
| 行权事实源 | Role.DataScope + 接口自动过滤 | Role.DataScope 四档；接口层**显式接线**（`GetDataScopeExpression`/`CanAccess`/`ValidPermission` + 本轮新增 `CubeDataScope`/`ValidImport`），非隐式自动过滤 |
| DataPermission | 拆除仅本人覆盖 | 已拆（矩阵内 7 控制器 + CubeNC 同款）；`Department` 有惉保留 `ManagerID` 特性（AC-07 判据按此修正） |
| 字段 | IFieldScope 脱敏 + GetPage 标记 | 已交付（双栈共享 `ReadOnlyEntityControllerScope.cs`，列表/详情/导出/5 个令牌出口）；`DataField.sensitive` 契约入库 |
| 字段矩阵 ACL | 另号 | 仍另号（未动） |
| （计划外）导入 | — | 本轮补齐：`ValidImport` 逐行归属/范围校验 + 包内跨实体数据集页面权限 |
| （计划外）AI 与部件 | — | 本轮补齐：`CubeTools` 记录上下文走 `FindData`+脱敏；两套内置部件接入 `GetFilter` |

## 实际完成范围

- **接口层行权**：`GetDataScopeContext()`（真实用户上下文、按请求缓存、**不写** `DataScopeContext.Current`）、`GetDataScopeExpression()`、`CanAccess()`、`ValidPermission()`（新增防伪造归属）；列表 `builder ∧ dataScope ∧ viewExp`、详情越权抛「非法访问数据」。
- **双栈脱敏**：共享 `Common/ReadOnlyEntityControllerScope.cs` + `NewLife.CubeNC.csproj` Link；MVC 侧列表/详情/5 个令牌出口接入；导出统一走 `ExportDataMasked`。
- **页外出口收口（本轮补齐）**：新增共享 `Common/CubeDataScope.cs`（`Create`/`GetCurrent`/`GetFilter`/`Merge`/`HasOwner`/`CanAccess`/`IsForgedOwner`，双栈 Link）。
  - G1 导入：`OnImport` 前置 `ValidImport`（归属不得为他人 / 已声明归属须在范围 / 合并类按主键不得覆盖不可见行）+ `ImportZip` 跨实体数据集页面权限 + `FindImportTarget`（主键未声明不查库）。
  - G2 AI 记录上下文：`CubeTools<TEntity>` 新增可选 `findRecord` 委托 + `FindRecord(id)`（越权记日志后降级），控制器注入 `FindRecordMasked` = `FindData` + `MaskSensitiveFields`。
  - G3 App 密钥令牌：`ValidToken` 支持 `userId`/`user` 绑定用户（绑定后与页面同一套行权），未绑定时显式 `XTrace` 声明系统级。
  - G4 内置部件：用户总数 / 在线 / 24h 日志 / 24h 异常 / 登录与在线明细（MVC 与 WebAPI 两套）一律经 `GetFilter`。
- **测试**：`DataScopeRowPermissionTests`、`CubeDataScopeTests`（新 14 例）、`ImportRowPermissionTests`（新 11 例）、`DataPermissionArchitectureTests`、`UserScopeQueryBypassGuardTests`、`DataScopeMenuOverrideTests`、`WidgetDataTests`（新增 4 例）、CubeNC `DataScopeDecouplingTests`/`DataScopeSensitiveMvcTests`。
- **文档**：`Doc/PERM-数据权限.md`（生效范围表按导入/令牌/AI/部件口径重写）、`Doc/Api/字段元数据.md`（`sensitive` 契约）、`Doc/DATA-控制器扩展.md`、`ChangeLog.md`（v6.15.2026.0914）、迁移方案 §8.6.1b。

## 做得好

- **验收驱动补齐**：验收不是「打勾放行」，而是把三步检查发现的 4 个旁路（导入/AI/令牌/部件）在**同一号内**补齐，避免「宣称全覆盖、实际有旁路」的长期债。
- **真机证据闭环**：每档行集都对照服务端 SQL 与日志，并用 SQL 证伪「本人恒可见」假设，定位到**包版本差异**而非代码缺陷。
- **共享文件纪律**：`CubeDataScope.cs` 与 `ReadOnlyEntityControllerScope.cs` 同走 Link，双栈行为一致；补 Link 后 MVC 一次编译通过。
- **可测性设计**：`CubeTools` 用**可选委托**扩展（4 参数构造保持兼容，旧子类/旧测试零改动）；`ValidImport`/`FindImportTarget`/`ValidImportEntity` 全 virtual，可桩化且派生类可放开。
- **文档按实现改写**：把「导入走 FindData」「令牌与页面一致」两处不实表述改成实现口径，并新增 ChangeLog 升级需知。

## 偏离与原因（记录）

| # | 偏离 | 原因 |
| --- | --- | --- |
| D1 | AC-04/AC-07/AC-12 判据改写而非改代码 | 判据引用了 XCode `ac108a773`（2026-09-11「本人数据始终可访问」）语义，而本号锁定包 `12.2.2026.901`（2026-09-01）无此行为；用户裁定不升包 → 按包内语义改写判据 |
| D2 | G3 无自动化单测 | App 密钥分支需 `App` 表数据与 CubeNC 令牌端点宿主；与本号 R5 同口径（CubeNC 侧缺可直接引用的测试工程），靠代码审阅 + 文档对齐 |
| D3 | G1 违规「拒绝整次导入」而非静默跳过行 | 安全优先且语义可预期（导入内容 ⊆ 用户可见范围）；已写入 ChangeLog 升级需知，并留 `ValidImport` virtual 供派生类放宽 |
| D4 | G5–G8/G10 未做 | 用户裁定为「仅记录」；已逐条留在 `tasks.md` 与 `verify.md` 缺口清单，留后续号 |
| D5 | 同文件区混入他号变更 | 本号交付后同期落地 `995a6f57`（Map 外键候选 dataSource）与 `04ef627d`（布尔不下发 dataSource），同在 `GetPage`/`GetFields` 链上但不属本号；已在 verify 登记「后续改动需连跑本号用例族」 |

## 教训

（详条已追加至 `openspec/harness/lessons.md`）

- **验收判据必须钉到锁定包版本**：本地 XCode 仓库 HEAD 可能领先 NuGet 锁定包，按源码写 AC 会得到「实现不符」的假结论。
- **Link 文件是隐形依赖**：`Widgets/System/*.cs`、`AI/CubeTools.cs`、`Common/*` 被 `NewLife.CubeNC.csproj` 链接编译，新增共享类型必须同步 csproj。
- **批量写库是行权旁路高发区**：`BatchInsert/BatchUpsert/Merge` 不走逐行 `Valid`，导入必须前置显式校验；合并类还需按主键查已存在行。
- **页外出口要有一份助手**：部件/AI/导入等出口各自直查实体，行权必漏；统一 `CubeDataScope` 后新增出口只需一行接入。
- **`AdminOnly` ≠ 行权**：系统角色也可能持有非「全部」的 DataScope，部件统计仍会放大。
- **测试注入范围的做法**：单测无 `ManageProvider.User` 时 `GetFilter(factory, null)` 会回落 `DataScopeContext.Current`，部件类测试设 `Current` 最省事。
- **手写测试实体没有 `_` 访问器**：断言表达式要用生成实体（如 `User._.ID`）。

## 遗留与后续

- **G5**：菜单级 DataScope 覆盖未接线，但文档/迁移方案已宣称「角色/菜单四档」→ 接线 `Create(user, menu)` 或降级宣传。
- **G6/G7/G8**：部件投影改 `IFieldScope`；`MaskSensitiveList` 返回值收口；`***` 往返写回忽略。
- **G10**：文档/台账漂移（`PERM-数据权限.md:138` `Trance` 错字、`README.md:392` TFM、`Doc/功能清单.md` 单测清单）。
- **冒烟未覆盖**：AC-08（Role 保存）、AC-22（Widget Query count，代码路径已由 `Osc260903WidgetQueryTests` 覆盖）。
- **XCode 升级回看**：若日后升到含 `ac108a773` 的包，AC-04/07/12 与自定义范围「本人数据始终可访问」需重新核对（行为会变宽）。
- **G1 边界**：`FindImportTarget` 只按主键定位已存在行，`Merge` 走唯一索引匹配时仍可能漏检（已在代码注释与 tasks 登记）。

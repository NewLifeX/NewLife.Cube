# OSC-2608273d95 合并后重审计（audit-merged）

> 审计时间：2026-09-12
> 审计基线：`NewLife.Cube` @ `ArcoVue` 分支 `264a001d`（已合并 `origin/master`）+ 引用包 `NewLife.XCode 12.2.2026.901`
> 审计范围：只读核对（Cube / CubeNC / ArcoVue / XCode 工作区源码），不修改任何代码
> 结论：**T1 全部由上游完成**、**T2 的 XCode 部分完成**；Cube/前端条目仍开放；**原方案路线与合并后架构冲突**，已决策改走「接口层行权」（见 §4）。

---

## 1. 头号发现：合并带入「数据权限去耦」架构，与原方案路线冲突 🔴

合并中 master 改写了 4 个关键文件（`git diff 9f1305bb..HEAD`）：

| 文件 | 变更 | 含义 |
| --- | --- | --- |
| `XUnitTest/DataScopeDecouplingTests.cs` | 新增 +99 | 三条守护断言 |
| `Doc/PERM-数据权限.md` | 99+/241− | 重写为「实体层 ❌ / 接口层 ✅ 唯一执行点」 |
| `NewLife.CubeNC/WebMiddleware/DataScopeMiddleware.cs` | +23/−10 | 注入**宿主系统态**上下文使拦截器休眠 |
| `NewLife.Cube/Common/ReadOnlyEntityController2.cs` | +12/−7 | 固化「权限表达式不得被 viewFilter 放大」 |

`DataScopeMiddleware` 现状（注释原文）：

> 宿主数据权限声明：实体层以"系统身份"运行，数据权限统一由接口层负责。XCode 数据权限拦截器会在上下文缺失时以 `ManageProvider.User` 兜底创建，**若此处不注入，请求内一切业务代码（SSO/服务/自定义 Action）都会被隐式收窄**。

实现：`if (DataScopeContext.Current == null) DataScopeContext.Current = CreateHostScope(user)`，其中 `CreateHostScope` 返回 `DataScopes.全部`（`:84-95`）。

三条守护测试：

1. `DataScopeDecouplingTests.Entities_DoNotRegisterDataScopeInterceptor` — 断言 `UserConnect/UserToken/OAuthLog/NotificationRecord` **不得**挂 `DataScopeInterceptor`
2. `DataScopeDecouplingTests.CrossUserQuery_NotFilteredByEntityLayer` — `仅本人(UserId=111)` 上下文下 `UserConnect.FindByProviderAndOpenID` **必须仍查到 222 的行**（注释：「严禁在此加数据范围」）
3. `DataScopeDecouplingTests.Controllers_HaveExpectedDataPermission` — **钉死** 11 个控制器当前 `[DataPermission]` 表达式

与原方案的冲突点：原 T2「给 User/Department/Log 及 Cube 个人数据实体挂 `DataScopeInterceptor`」、T3「CreateWhere 以 `DataScopeContext.Current` 合并 GetFilter」、§2「改 XCode 行为」——全部与上述架构/测试/文档对立。

**性质判定**：不是合并残留 bug，而是 master 的有意决策（`PERM-数据权限.md §9.6` 记录了「实体级过滤导致业务代码被隐式收窄、同字段条件叠加、菜单数据范围配置引发全站失效」三类事故后回退）。

---

## 2. T1 / T2（XCode 侧）：上游已完成 ✅

Cube 现引用 `NewLife.XCode 12.2.2026.901`，其 `ChangeLog.md` 的「数据权限（DataScope）增强」逐条对应原 `NewLife.XCode-Issue.md`：

| 原 Issue | 现状 | 证据锚点 |
| --- | --- | --- |
| ① `Role.Valid` 把 `DataScope==0`（全部）改写为「本部门」 | ✅ 仅 Insert 且未脏时填默认；显式 0 保留 | `XCode/Membership/角色.Biz.cs:134-144` |
| ② `DataScopeInterceptor.OnValid` 失败仍 `return true` | ✅ catch 分支 `return false` | `XCode/Membership/DataScopeModule.cs:52-81` |
| ③ User/Department/Log 未挂载 | ✅ 已挂载 + `IDataScopeFieldProvider` | `用户.Biz.cs:44`、`部门.Biz.cs:12`、`日志.Biz.cs:8` |
| ④ 部门缓存键缺 deptId | ✅ `DataScope:{userId}:{deptId}:{scope}`，`ClearCache` 兼容新旧键 | `DataScopeContext.cs:127-143` |
| ⑤ 纯用户/纯部门实体过滤 | ✅ `IUserScope` 只按用户（不 join 扩权）；`IDepartmentScope` 走 `BuildDepartmentScopeFilter`；空部门 `Equal(-1)` | `DataScopeContext.cs:365-406, 447-453` |
| ⑥ 菜单 `DataScope` 默认 | ✅ Insert 默认 `-1`（继承角色） | `菜单.Biz.cs:42-43` |

⇒ 原 G-A2（升级包会破坏内部读取）风险**已消除**；T1、T2 的 XCode 条目可在 tasks 勾选，且**本号不需要改 XCode 仓库**。
**新增约束**：Cube 侧只允许使用 ≤ `12.2.2026.901` 的 XCode API。

XCode 现成可用 API（本号接线全部复用，不另造第二套行权）：

- `DataScopeContext.Create(IUser? user, IMenu? menu = null)`：系统角色→全部+ViewSensitive；多角色取 `Min(DataScope)`；无角色→仅本人；带部门缓存
- `DataScopeHelper.GetFilter(IEntityFactory factory, DataScopeContext? context = null)` / `GetFilter<TEntity>()`
- `DataScopeHelper.CanAccess(...)`（`IDataScope` / `IUserScope` / `IDepartmentScope` 重载）
- `IFieldScope` + `FieldScopeHelper.MaskSensitiveFields(IEntity, DataScopeContext?, maskValue)`
- `IUserScope`（`Int32 UserId`）/ `IDepartmentScope`（`Int32 DepartmentId`）/ `IDataScope` / `IDataScopeFieldProvider`

---

## 3. Cube / 前端侧：相对原方案仍开放（锚点已核实）

| 原任务 | 状态 | 证据 |
| --- | --- | --- |
| T3 `CreateWhere` 合 `GetFilter` | ❌ | `ReadOnlyEntityController2.cs:290-293` 只读 `[DataPermissionAttribute]`；全 Cube **零处** `GetFilter` 调用 |
| T3 `FindData` / `ValidPermission` | ❌ | `:274-281` 走 CreateWhere；`:699` `ValidPermission` 默认 `true` |
| T3 `DataField.Sensitive` + 脱敏 | ❌ | `DataField` 无 `Sensitive`；`PrepareFieldsForApi`（`ReadOnlyEntityController.cs:340`）无脱敏；迁移方案 §8.6 亦记「`MaskSensitiveFields` 存在，控制器**从未调用**」 |
| T3 Widget Query | ⚠️ 半套 | `WidgetQueryService.cs:257` 只 AND `DataPermissionAttribute` |
| T4 拆 `[DataPermission]` | ❌ | 双栈 11 控制器 + CubeNC `NotificationRecordController` 全在，且被 `DataScopeDecouplingTests` 钉死 |
| T4 WebAPI RoleController 对齐 | ❌ | 仅 CubeNC 有 `DataDepartmentIds` DataSource（`RoleController.cs:28/32`） |
| T5 ArcoVue 行权 UI | ❌ | `web/src` **零处** `DataScope │ DataDepartmentIds`；`dataScopeForm.ts`、`rejectSensitiveColumns` 不存在；`iamGuards.shouldShowSelfOnlyUserAlert` 已存在（可复用） |
| T6 文档 | ⚠️ 方向需改 | `PERM-数据权限.md` 已被 master 重写，不能再按原任务「重写」，应增量补四档矩阵与接线说明 |
| 双栈共享前提 | ✅ | `NewLife.CubeNC.csproj` `<Compile Include="..\NewLife.Cube\Common\ReadOnlyEntityController2.cs">`（文件位于 WebAPI 项目下，非 NC 目录） |

Cube 侧当前**零** `IUserScope / IDataScope` 引用 ⇒ 给实体补接口不会触发既有分支行为改变（只需自查）。
待映射字段（已核实）：`UserToken.UserID`、`UserOnline.UserID`、`UserConnect.UserID`、`OAuthLog.UserId`、`NotificationRecord.UserId`（另有 `TenantId`）。

---

## 4. 路线决策：A（接口层行权）✅ 已选定

| 方案 | 核心思路 | 优点 | 代价 |
| --- | --- | --- | --- |
| **A（采纳）** | 实体层保持纯净、宿主系统态注入不动、**不改 XCode**；在控制器层用真实用户上下文调用 `DataScopeHelper.GetFilter/CanAccess/FieldScopeHelper` | 与合并后架构一致；不触碰 SSO/服务层；`DataScopeDecouplingTests` 前两条继续守护；XCode 零改动 | 需在控制器层构造用户上下文；`[DataPermission]` 策略与第三条守护测试需同步修订 |
| B（弃用） | 宿主上下文改真实用户态 + 实体挂拦截器 | 一次接线全宿主自动过滤 | 推翻 `DataScopeDecouplingTests` 全部三条与 `PERM-数据权限.md §9.6`；重演 master 记录的隐式收窄事故；且 XCode 12.2 已在内置实体挂拦截器，「必须保留宿主系统态注入」成为硬约束 |

**路线 A 的关键约束（设计实现必须遵守）**

1. `DataScopeContext.Current` 保持宿主系统态语义，**不得**在请求管线里改写为真实用户态（否则 XCode 内置拦截器复活 → 隐式收窄）。
2. 行权上下文在控制器层**显式构造**并**按请求传递**（不用 `Current`），例如 `DataScopeContext.Create(user as IUser)`；建议按请求缓存到 `HttpContext.Items` 以避免重复构造。
3. 目标实体通过**接口**声明参与行权：XCode 内置（User/Log/Department）已就绪；Cube 个人数据实体补 `IUserScope` + `IDataScopeFieldProvider`（**只加接口，不挂拦截器**），使 `GetFilter/CanAccess` 能识别字段。
4. `DataScopeDecouplingTests` 第 1、2 条**保持不动**（实体层仍纯净）；第 3 条需随「拆仅本人特性」同步改写。

---

## 5. 修订后的差距清单（G 重编号）

| 编号 | 缺口 | 落点 | 优先级 |
| --- | --- | --- | --- |
| G1 | `CreateWhere` 未合并 `GetFilter`（四档不生效的根因） | `NewLife.Cube/Common/ReadOnlyEntityController2.cs` | P0 |
| G2 | `FindData`/`ValidPermission` 无 `CanAccess`（详情/写入越权） | 同上 | P0 |
| G3 | Cube 5 个个人数据实体未实现 `IUserScope` + FieldProvider | `Entity/{用户令牌,用户在线,用户链接,OAuth日志,通知记录}.Biz.cs` | P0 |
| G4 | 仅本人 `[DataPermission]` 压制四档（7 控制器 × 双栈 + NC NotificationRecord） | `Areas/Admin/Controllers/*` | P0 |
| G5 | `DataScopeDecouplingTests.Controllers_HaveExpectedDataPermission` 需同步修订 | `XUnitTest/DataScopeDecouplingTests.cs` | P0 |
| G6 | `DataField.Sensitive` + `PrepareFieldsForApi` 打标 + 列表/详情/导出 `MaskSensitiveFields` | `ViewModels/DataField.cs`、`Common/ReadOnlyEntityController*.cs` | P0 |
| G7 | `WidgetQueryService` 只 AND `DataPermissionAttribute`，未合 `GetFilter` | `Widgets/WidgetQueryService.cs` | P1 |
| G8 | WebAPI `RoleController` 缺 `DataDepartmentIds` DataSource | `Areas/Admin/Controllers/RoleController.cs` | P1 |
| G9 | ArcoVue 无 `dataScopeForm.ts` / `rejectSensitiveColumns` | `web/src/core/utils/*` + Role 表单 + 列表 | P1 |
| G10 | `PERM-数据权限.md` 需增量补「四档矩阵 + 接口层接线」（**不是**回到实体层行权） | `Doc/PERM-数据权限.md` | P1 |
| G11 | 功能清单 PERM-6 / 迁移方案 §8.6 / 竞品分析 / web README 事实回写 | 多处 | P2 |
| G12 | 菜单级 `DataScope` 覆盖未接（XCode 已支持 `Create(user, menu)`，Cube 侧缺菜单上下文） | 另号/待定 | P2（本号不做） |

---

## 6. 本号显式不做（保持既有决策）

- 不改 XCode 仓库任何文件；不升级/降级 `NewLife.XCode` 包版本。
- 不改 `DataScopeContext.Current` 的宿主系统态语义；不给任何实体挂 `DataScopeInterceptor`。
- 不改租户 `ITenantScope` 语义；不改 `viewFilter` 与权限的 AND 顺序。
- 不给 Menu/Role/File 加行权；不改 `LovController.ListData`；不做字段矩阵 ACL、不裁剪导出列集（`AllFields` 暂留）。
- 不把菜单级 `DataScope` 覆盖纳入本号（G12 另号）。

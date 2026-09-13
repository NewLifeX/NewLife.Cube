# OSC-2608273d95 Design — 角色数据范围行权

> **路线（2026-09-12 重审计后）：A — 接口层行权**。依据 `audit-merged.md`：合并后的 master 明确「实体层不承担行权」，`DataScopeMiddleware` 注入宿主系统态上下文使拦截器休眠，`XUnitTest/DataScopeDecouplingTests` 守护实体层纯净。
> 本设计因此：**不改 XCode 仓库**、**不给任何实体挂 `DataScopeInterceptor`**、**不改写 `DataScopeContext.Current`**；行权在控制器层显式构造上下文后调用 XCode 现成 API（`DataScopeHelper.GetFilter/CanAccess`、`FieldScopeHelper.MaskSensitiveFields`）。

适用前端：Arco Design Vue（https://arco.design/vue/docs/start）。表单显隐用现有 FieldInput / DefaultForm 字段元数据，不新增设计系统。不涉及 VisActor 新配置、不涉及 FlowGram。`.vue` 薄 script；显隐与 sensitive 列逻辑进 `core/utils` 纯函数 + spec。

## 0. 状态唯一来源

| 状态 | 来源 | 禁止 |
| --- | --- | --- |
| 行权范围 | 控制器层显式构造的 `DataScopeContext`（`DataScopeContext.Create(user, menu = null)`：`IsSystem`→全部，否则 `Min(DataScope)`）。**不**读写 `DataScopeContext.Current`（其语义固定为宿主系统态：`DataScopes.全部`） | 前端按角色名猜；用 `DataPermission` 表达式替代 DataScope；把 `Current` 改成真实用户态 |
| 可访问部门 | `AccessibleDepartmentIds`（null=不限制；空数组=部门维度无权限） | 前端传部门 ID 列表当授权 |
| 租户 | 现有 `CreateWhere` 租户分支 | 用 DataScope 表达租户 |
| 敏感字段 | 实体 `IFieldScope.GetSensitiveFields()` + 上下文 `ViewSensitive` + 是否本人 | ViewProfile 藏列当 ACL |
| 菜单覆盖 | 菜单列 `DataScope`：`<0` 继承角色；`>=0` 覆盖为该枚举值 | 把枚举默认 0（全部）当成「未配置」而不改库默认 -1 |

`DataScopeContext.IsSystem` 保持「`DataScope==全部`」含义（含非系统角色显式选全部）。`Role.IsSystem` 仍在 Create 时强制全部。

## 1. 行权矩阵（穷尽）

上下文已创建。实体实现的接口见列。`全部` 含 `Role.IsSystem`。

### 1.1 `IDataScope`（User：UserId=ID，DepartmentId=DepartmentID）

| DataScope | 列表/导出 GetFilter | CanAccess / 详情 | 写入 OnValid |
| --- | --- | --- | --- |
| 全部 | 无行过滤 | 真 | 过 |
| 仅本人 | `ID = 当前用户` | 实体 UserId==当前 | 只能写自己 |
| 本部门 | `DepartmentID = 当前部门`（部门≤0 则恒假 `DepartmentID=-1`） | 部门∈{当前部门} | 部门必须∈可访问集 |
| 本部门及下级 | `DepartmentID IN (自身+子孙)` | 同上 | 同上 |
| 自定义 | `DepartmentID IN (各角色 DataDepartmentIds 并集)`；并集空则恒假 | 同上 | 同上 |

### 1.2 仅 `IUserScope`（Log / UserToken / UserOnline / UserConnect / OAuthLog / NotificationRecord）

| DataScope | GetFilter | CanAccess |
| --- | --- | --- |
| 全部 | 无 | 真 |
| 仅本人 / 本部门 / 本部门及下级 / 自定义 | **一律** 归属用户字段 = 当前用户 | 归属用户==当前 |

禁止为「本部门」去 join `User` 放大日志/令牌。

### 1.3 仅 `IDepartmentScope`（Department：DepartmentId 映射为 `ID`）

| DataScope | GetFilter | CanAccess |
| --- | --- | --- |
| 全部 | 无 | 真 |
| 仅本人 | `ID = 当前用户.DepartmentID`（无部门则恒假） | 实体 ID==当前部门 |
| 本部门 | 同仅本人（本表一行=一个部门） | 同左 |
| 本部门及下级 | `ID IN (自身+子孙)` | ID∈集合 |
| 自定义 | `ID IN DataDepartmentIds 并集`；空则恒假 | 同左 |

原缺口「`GetAccessibleDepartmentIds` 在『仅本人』返回空数组使部门列表变化为 `= -1`」**已由 XCode 12.2.2026.0901 修复**（`BuildDepartmentScopeFilter`）——本号不重复修，仅回归验证。

### 1.4 未实现三接口

GetFilter=null，CanAccess=真（就行权而言）。菜单权限与租户仍有效。

### 1.5 无上下文 / 匿名

`DataScopeContext.Current==null` 且 `ManageProvider.User==null`：GetFilter=null（与 XCode 拦截器无上下文时不校验一致）。Cube 列表接口仍走 `EntityAuthorize`，未登录进不了 SearchData。禁止在匿名 GetPage 元数据里下发行权 SQL。

### 1.6 与租户、viewFilter

`SearchData` 合并顺序（不得颠倒）：

1. `CreateWhere`：租户字符串 +（可选）剩余 `DataPermission` 额外 AND  
2. `DataScopeHelper.GetFilter` AND  
3. `viewFilter` AND（OSC-260819e483：logic=any **只 OR 筛选条件**，不得 OR 进 1/2）

### 1.7 现有 DataPermission 拆除后对照

| 控制器 | 今日 | 本号 |
| --- | --- | --- |
| User | 非系统 `ID={#userId}` | 按 1.1 |
| Log | `CreateUserID={#userId}` | 按 1.2 |
| UserToken / Online / Connect / OAuthLog | `UserID`/`UserId`={#userId} | 按 1.2 |
| NotificationRecord | `UserId={#userId}` | 按 1.2 |
| Department | 无（注释掉的 ManagerID） | 按 1.3 |
| Role / Menu / File / Cube 配置 | 无 | 仍无行权 |

`DataPermissionAttribute` 保留为「额外 AND」，但**矩阵内 7 个控制器必须去掉仅本人表达式**（否则本部门被压成仅本人）。`CreateWhere` 合并顺序：租户 → `GetFilter` →（若特性仍在且未被 `IsSystem`/`att.Valid(roles)` 放行）`att.Expression`。

**合并后新增约束**：`XUnitTest/DataScopeDecouplingTests.Controllers_HaveExpectedDataPermission` 钉死了上述 11 个控制器的表达式，拆特性时必须**同步修订该测试的矩阵**；同文件 `Entities_DoNotRegisterDataScopeInterceptor` 与 `CrossUserQuery_NotFilteredByEntityLayer` 必须**保持不动**（实体层继续纯净）。

## 2. XCode 现状（已就绪，本号不改 XCode）

Cube 引用 `NewLife.XCode 12.2.2026.901`，其 ChangeLog「数据权限（DataScope）增强」已交付原 Issue ①–⑥，本号只需回归验证、**不改 XCode 仓库**：

| 原 Issue | 现状与锚点（XCode 源码） |
| --- | --- |
| `Role.Valid` 把「全部」改写 | 仅 Insert 且未脏时填默认；显式 0 保留 — `XCode/Membership/角色.Biz.cs:134-144` |
| `OnValid` 失败放行 | catch 分支 `return false` — `XCode/Membership/DataScopeModule.cs:52-81` |
| User/Department/Log 未挂载 | 已挂载 + `IDataScopeFieldProvider` — `用户.Biz.cs:44`、`部门.Biz.cs:12`、`日志.Biz.cs:8` |
| 缓存键缺部门 | `DataScope:{userId}:{deptId}:{scope}`，`ClearCache` 兼容新旧键 — `DataScopeContext.cs:127-143` |
| 纯用户/纯部门实体过滤 | `IUserScope` 只按用户（不 join 扩权）；`IDepartmentScope` 走 `BuildDepartmentScopeFilter` — `DataScopeContext.cs:365-406,447-453` |
| 菜单 `DataScope` 默认 | Insert 默认 `-1`（继承角色）— `菜单.Biz.cs:42-43` |

**硬约束（合并后架构）**：`DataScopeMiddleware` 以 `CreateHostScope(user)`（`DataScopes.全部`）注入宿主系统态，使 XCode 内置拦截器休眠，避免 SSO/服务层被隐式收窄；`XUnitTest/DataScopeDecouplingTests` 第 1、2 条守护该行为。本号**不得**改写 `DataScopeContext.Current`、不得给实体挂 `DataScopeInterceptor`。

本号复用的 XCode 现成 API（不另造第二套行权）：

- `DataScopeContext.Create(IUser? user, IMenu? menu = null)`
- `DataScopeHelper.GetFilter(IEntityFactory factory, DataScopeContext? context = null)` / `GetFilter<TEntity>()`
- `DataScopeHelper.CanAccess(...)`（`IDataScope` / `IUserScope` / `IDepartmentScope` 重载）
- `IFieldScope.GetSensitiveFields()` + `FieldScopeHelper.MaskSensitiveFields(IEntity, DataScopeContext?, maskValue)`

## 3. 实体参与行权（只加接口，不挂拦截器）

行权判定由 `GetFilter(factory, ctx)` / `CanAccess(entity, ctx)` 按**实体实现的接口**分派，因此只需让目标实体声明接口，无需拦截器。

| 实体 | 仓库 | 需补的接口 | FieldProvider 映射 |
| --- | --- | --- | --- |
| User | XCode | 已有 `IDataScope` + `IFieldScope` + `IDataScopeFieldProvider` | 已有 `ID` / `DepartmentID` |
| Department | XCode | 已有 `IDepartmentScope` + `IDataScopeFieldProvider` | 已有 `_.ID` |
| Log | XCode | 已有 `IUserScope` + `IDataScopeFieldProvider` | 已有 `_.CreateUserID` |
| UserToken / UserOnline / UserConnect | Cube `Entity/*.Biz.cs` | **新增** `IUserScope` + `IDataScopeFieldProvider` | `GetUserField() => _.UserID`；显式 `Int32 IUserScope.UserId { get => UserID; set => UserID = value; }` |
| OAuthLog | Cube | **新增** `IUserScope` + `IDataScopeFieldProvider` | `GetUserField() => _.UserId`（属性名即 `UserId`，隐式实现） |
| NotificationRecord | Cube | **新增** `IUserScope` + `IDataScopeFieldProvider` | `GetUserField() => _.UserId`（实体另有 `TenantId`，租户链路不变） |

- 只声明接口，**不**写 `Meta.Interceptors.Add<DataScopeInterceptor>()`（守护测试 `Entities_DoNotRegisterDataScopeInterceptor` 必须保持绿）。
- Cube 侧当前零 `IUserScope/IDataScope` 引用，新增接口不影响既有分支；仅需自查 `is IUserScope` 判定处（`FieldScopeHelper` 的本人判定正是期望行为）。
- 未实现三接口的实体（Role/Menu/File/配置表等）：`GetFilter` 返回 null，行权不介入（菜单权限与租户照旧）。

## 4. Cube 控制器接线

### 4.0 行权上下文构造（路线 A 核心）

- 在控制器层构造 `DataScopeContext.Create(ManageProvider.User as IUser)`，**不写** `DataScopeContext.Current`（后者固定宿主系统态）。
- 封装 `protected DataScopeContext? GetDataScopeContext()`：按请求缓存到 `HttpContext.Items`（同请求多次调用只构造一次；`Create` 内部部门列表有 5 分钟缓存、角色取自用户实体缓存，开销可控）。
- 未登录/无用户 → `null`（`GetFilter` 返回 null，行权不介入；列表接口仍由 `EntityAuthorize` 拦截）。
- 菜单级覆盖 `Create(user, menu)` 本号**不接**（G12 另号）；库默认 `Menu.DataScope = -1` 已等价「继承角色」。

### 4.1 `CreateWhere` / `SearchData` / `FindData` / `ValidPermission`

`NewLife.Cube/Common/ReadOnlyEntityController2.cs` —— **CubeNC 已通过 `<Compile Include>` Link 同一文件，只改一处**（其余控制器两栈各改）。

- 抽出 `protected Expression? GetDataScopeExpression()` → `DataScopeHelper.GetFilter(Factory, GetDataScopeContext())`。
- `SearchData`：把 GetFilter **AND** 进 `p.State`（与 viewFilter 同一 try 保护；解析失败时保留行权表达式、放弃 viewFilter；`logic=any` 不得 OR 进行权）。
- `FindData`：查出后 `!CanAccess(entity)` → 抛现有 `非法访问数据`。
- `ValidPermission`：默认改为 `CanAccess`（无接口为真）；现有 `UserTokenController.ValidPermission` 等 override 改为先 `base.ValidPermission(...)` 再业务规则。
- `CanAccess`：按 `IDataScope`/`IUserScope`/`IDepartmentScope` 重载分派（`DataScopeHelper.CanAccess`）；都不是则 true。

列表 `Index` / 详情 / 导出：返回前对实体调 `FieldScopeHelper.MaskSensitiveFields(entity, GetDataScopeContext())`。导出必须先走与列表相同的 `SearchData` 行集（禁止另开无 State 的 `FindAll`），列仍可用 `Factory.AllFields`（BE-E1 另号）。

### 4.2 Widget Query（WebAPI）

`WidgetQueryService` 现会 AND `DataPermissionAttribute` 表达式（`:257`）。本号在其 Where 上追加 **`DataScopeHelper.GetFilter(fact, ctx)`**（聚合与列表同源，`ctx` 亦由当前用户构造）。禁止只滤 DefaultList 而 Query 看全表。

CubeNC 无 Widget 控制器则跳过此文件。

### 4.3 拆除仅本人特性（含守护测试同步）

两栈删除 User、Log、UserToken、UserOnline、UserConnect、OAuthLog 上的 `[DataPermission(null, "...")]`；`NotificationRecord` **仅 CubeNC 控制器**有该特性。Department 保持无特性。

**同步修订** `XUnitTest/DataScopeDecouplingTests.Controllers_HaveExpectedDataPermission`：矩阵内 7 个控制器改为断言「无 `[DataPermission]`（行权由 DataScope GetFilter 表达）」；未纳入本号的 Parameter / Attachment / PrincipalAgent 保留原表达式断言。同文件 `Entities_DoNotRegisterDataScopeInterceptor` 与 `CrossUserQuery_NotFilteredByEntityLayer` **不得改动**。

### 4.4 Role 表单

WebAPI `RoleController` 对齐 CubeNC：`AddFormFields`/`EditFormFields` 的 `DataDepartmentIds` 设 `DataSource` = 启用部门 ID→Name。列表可保留 DataScope 列。

### 4.5 GetPage 敏感标记

`DataField` 增 `Boolean Sensitive`（JSON camelCase `sensitive`）。其中的上下文即 §4.0 的 `GetDataScopeContext()`（真实用户角色合并 `ViewSensitive`）；**不得**用 `DataScopeContext.Current`（宿主态 `ViewSensitive=false`，会把所有人的敏感字段一起脱敏）。`PrepareFieldsForApi`：若 `Factory.EntityType` 可赋为 `IFieldScope`，实例 `GetSensitiveFields()` 命中的字段：

- `context.ViewSensitive` 或（当前用户为本人且实体是单行上下文）→ 不标 sensitive、不强制藏列。GetPage 无当前行，列表字段：仅当 `ViewSensitive` 为真时 `Sensitive=false`；否则 `Sensitive=true` 且列表/详情字段对敏感列 `DataField` 可同时 `ShowIn` 保持，由前端藏列；**序列化脱敏仍以后端 Mask 为准**。
- 禁止把 password 哈希下发给非授权用户（Mask 为 `***`）。

本人看自己：列表含多人时 Mask 按行（`CanViewSensitiveFields` / UserId==当前）。`MaskSensitiveFields` 已有「ViewSensitive 或本人」逻辑；对 User 的 IUserScope.UserId=ID 成立。

## 5. ArcoVue

| 文件 | 改动 | 保留 |
| --- | --- | --- |
| `web/src/core/utils/dataScopeForm.ts`（新建） | `isCustomDataScope(v)`：4 或 `'自定义'`；`shouldShowDataDepartmentIds(model)` | 不在 vue 写分支 |
| Role 表单字段显隐 | `DefaultForm` / `FieldInput`：`Admin/Role` + 字段 DataDepartmentIds 且非自定义 → 不渲染（**实际接入点**：`useFormContent.visibleFields` 一处 filter，判定在 `dataScopeForm.ts`，`.vue` 零分支） | 不改 Permission 树 |
| `iamGuards.ts` | `shouldShowSelfOnlyUserAlert`：增加「仅当可判定为仅本人」：`rows.length===1 && total∈{0,1,NaN} && id==自己` **保持**；本部门多人时自然不触发。文案不变 | 不根据角色名显示 |
| 列表列 | 字段 `sensitive===true` 不进可见列（纯函数 `rejectSensitiveColumns`） | 不把藏列当授权 |
| `*.spec.ts` | 覆盖显隐与 sensitive | |

不改 GetPage 匿名；不展示 Where 表达式。UI 细节见 `ui/information-architecture.md`。

## 6. 文件级改动地图

### 6.1 NewLife.XCode（**零改动**）

| 项 | 结论 |
| --- | --- |
| `XCode/Membership/DataScopeContext.cs` / `DataScopeModule.cs` / `角色.Biz.cs` / `用户.Biz.cs` / `部门.Biz.cs` / `日志.Biz.cs` | 原计划修改项已由上游 12.2.2026.0901 交付（锚点见 §2），本号**不改** |
| 包版本 | 保持 `12.2.2026.901`，不升不降；只用该版本已有 API |
| 回归 | `XUnitTest.XCode/Membership/DataScopeTests.cs` 仅作只读回归门禁（不修改该仓库） |

### 6.2 NewLife.Cube / CubeNC

| 文件 | 改 |
| --- | --- |
| `Common/ReadOnlyEntityController2.cs` | SearchData/FindData/ValidPermission/Mask；**Link 双栈一份** |
| `Common/ReadOnlyEntityController.cs` | Index/详情/导出返回前 Mask；PrepareFieldsForApi 标 Sensitive |
| `ViewModels/DataField.cs` | `Sensitive` |
| `Widgets/WidgetQueryService.cs` | GetFilter AND（WebAPI） |
| `Areas/Admin/Controllers/UserController.cs` | 去 DataPermission（Cube + CubeNC） |
| `Areas/Admin/Controllers/LogController.cs` | 同上两栈 |
| `Areas/Admin/Controllers/UserTokenController.cs` | 同上两栈 |
| `Areas/Admin/Controllers/UserOnlineController.cs` | 同上两栈 |
| `Areas/Admin/Controllers/UserConnectController.cs` | 同上两栈 |
| `Areas/Admin/Controllers/OAuthLogController.cs` | 同上两栈 |
| `NewLife.CubeNC/Areas/Admin/Controllers/NotificationRecordController.cs` | 去 DataPermission（仅 NC） |
| `Areas/Admin/Controllers/RoleController.cs`（WebAPI） | DataDepartmentIds DataSource；对齐 CubeNC |
| `Entity/{用户令牌,用户在线,用户链接,OAuth日志,通知记录}.Biz.cs` | **只加** `IUserScope` + `IDataScopeFieldProvider`（不挂拦截器） |
| `XUnitTest/DataScopeDecouplingTests.cs` | 仅 `Controllers_HaveExpectedDataPermission` 按 §4.3 同步；实体层两条不动 |
| `Doc/PERM-数据权限.md` | **增量**补「DataScope 四档 + 接口层接线」章节（不回到实体层行权） |
| `Doc/功能清单.md` | PERM-6 改为 DataScope 行权，注明本号 |

### 6.3 ArcoVue

见 §5。同步 `web/README.md` custom/数据权限一句；`竞品分析报告.md` § 行权限一句（事实：行权在服务端 DataScope）。

### 6.4 禁止改

- `EntityAuthorize` 菜单位检查、GetPage AllowAnonymous。
- OSC-0018 五件套、Cube.Vue 源码、`LovController` 值集旁路。
- 用 DataPermission 三字段构造器「实现」文档幻想 API。

### 6.5 与 §8.6 对照

| Issue | 本号任务 | 状态 |
| --- | --- | --- |
| BE-A1 Role.Valid 0=全部 | 上游完成（§2） | ✅ 不改 XCode |
| BE-A2 OnValid 失败不放行 | 上游完成（§2） | ✅ 不改 XCode |
| BE-A3 实体接口（**不挂拦截器**） | T3 | 本号 |
| BE-A4 部门仅本人≡本部门 | 上游完成 | ✅ 不改 XCode |
| BE-A5 缓存键 deptId | 上游完成 | ✅ 不改 XCode |
| BE-A6 CreateWhere 合 GetFilter；拆仅本人特性 | T4/T5 | 本号 |
| BE-A7 详情/导出/PATCH/Widget | T4/T5；值集另号 | 本号 |
| BE-A8 PERM 文档 + PERM-6 | T8 | 本号 |
| BE-B1 IFieldScope 脱敏 + GetPage sensitive | T6 | 本号 |
| BE-B2 / D2 / E1 | 明确不做 | — |
| 菜单级 DataScope 覆盖 | 另号（G12） | 不做 |

## 7. 字段权限地基（本号边界）

本号交付：

- 请求内 `DataScopeContext.ViewSensitive`
- 行 Mask + GetPage `sensitive`
- 后续 OSC 可在同一上下文上做「角色字段白名单」，读取 `Current` 与字段名，**不必再发明行权**。

本号不交付：角色编辑器里的字段矩阵、按字段禁用编辑器。

## 8. 核心文档影响

| 文档 | 动作 |
| --- | --- |
| `Doc/PERM-数据权限.md` | **增量**：保留合并后 §9.6「实体层不承担行权」结论，新增「DataScope 四档矩阵 + 接口层 GetFilter 接线」章节；修正 `DataScopeType` 等旧描述 |
| `Doc/功能清单.md` PERM-6 | 后端✅ 对齐 DataScope；ArcoVue 无独立 ACL UI |
| `ArcoVue企业中后台迁移方案.md` | §8.6 BE-A1～A8/B1 标本号；行权事实源 DataScope |
| `NewLife.Cube.ArcoVue/竞品分析报告.md` | 行权限：服务端 DataScope，不再写「仅 DataPermission 仅本人」为终态 |
| `NewLife.Cube.ArcoVue/web/README.md` | 一句：行权服务端 DataScope，前端不筛选当授权 |
| `Doc/Api/核心接口架构.md` | 无新路径；GetPage `sensitive` 字段说明 |
| `NewLife.Cube.ArcoVue/web/docs/**` | 无强制；若文档仍写 DataPermission 仅本人则改一句 |

## 9. 测试设计

| 用例 | 期望 | 位置 |
| --- | --- | --- |
| GetFilter User 本部门 / 仅本人 | 语义同 §1.1（DepartmentID=当前 / ID=当前） | XCode DataScopeTests（只读回归） |
| GetFilter Log 本部门 | CreateUserID=当前（不扩权） | XCode（只读回归） |
| GetFilter Department 仅本人 | ID=当前部门，不是 -1 | XCode（只读回归） |
| Role Insert 未设 / Update=0 | 普通默认本部门；显式全部保存后仍为 0 | XCode（只读回归） |
| CreateWhere（Cube） | 本部门角色列表含同部门其他用户、不含外部门 | 新增 Cube XUnit |
| 无接口实体 | `GetFilter` 为 null，Role/Menu 列表不空 | 新增 Cube XUnit |
| FindData / ValidPermission | 仅本人直打他人 User id → 非法访问/拒绝写入 | 新增 Cube XUnit |
| Cube 实体接口映射 | UserToken/Online/Connect 走 `_.UserID`；OAuthLog/NotificationRecord 走 `_.UserId` | 新增 Cube XUnit |
| MaskSensitiveFields 上下文 | 用 `GetDataScopeContext()`：无 ViewSensitive 他人 Password=`***`、本人不遮蔽；不得用宿主态 | 新增 Cube XUnit |
| 实体层无拦截器 | `DataScopeDecouplingTests` 前两条保持绿 | 既有守护（不改） |
| Vitest | 自定义(4) 才显示 DataDepartmentIds；`sensitive===true` 列被 reject | 新增 Vitest |

执行期：`dotnet test`（XCode DataScopeTests 只做回归、不改该仓库；Cube 本号新增测试）+ `pnpm --filter @newlifex/cube-arco-vue test`；构建 `NewLife.Cube`、`NewLife.CubeNC`、`@newlifex/cube-arco-vue`。

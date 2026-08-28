# OSC-2608273d95 Design — 角色数据范围行权

适用前端：Arco Design Vue（https://arco.design/vue/docs/start）。表单显隐用现有 FieldInput / DefaultForm 字段元数据，不新增设计系统。不涉及 VisActor 新配置、不涉及 FlowGram。`.vue` 薄 script；显隐与 sensitive 列逻辑进 `core/utils` 纯函数 + spec。

## 0. 状态唯一来源

| 状态 | 来源 | 禁止 |
| --- | --- | --- |
| 行权范围 | `DataScopeContext.Current.DataScope`（`Create` 自 `user.Roles`：`IsSystem`→全部，否则 `Min(DataScope)`；菜单 `DataScope>=0` 可覆盖） | 前端按角色名猜；用 `DataPermission` 表达式替代 DataScope |
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

今日 `GetAccessibleDepartmentIds` 在「仅本人」返回空数组，`BuildDepartmentFilter` 变成 `= -1`（部门列表空白）。**必须**按上表改为「仅本人 ≡ 本部门」。

### 1.4 未实现三接口

GetFilter=null，CanAccess=真（就行权而言）。菜单权限与租户仍有效。

### 1.5 无上下文 / 匿名

`DataScopeContext.Current==null` 且 `ManageProvider.User==null`：GetFilter=null（与今日拦截器一致）。Cube 列表接口仍走 `EntityAuthorize`，未登录进不了 SearchData。禁止在匿名 GetPage 元数据里下发行权 SQL。

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

`DataPermissionAttribute` 保留。`CreateWhere`：若特性仍在，**在 DataScope 之后**额外 AND `att.Expression`；`IsSystem` 或 `att.Valid(roles)` 时跳过该额外句，**不**跳过 DataScope（系统角色 Create 已是全部，GetFilter 本就为 null）。

## 2. XCode 行为修正

### 2.1 `Role.Valid`（`XCode/Membership/角色.Biz.cs`）

删除 `if (DataScope == 0) { 按 Type 改写 }`。

仅当 **Insert** 且 `DataScope` **不在脏数据中**（调用方未赋值）时，按 Type 写入默认：系统→全部，名称含「高级」的普通→本部门及下级，其余普通→本部门，租户→本部门及下级，其它→仅本人。

Update 时 0 就是「全部」，原样保存。

### 2.2 部门缓存键

`GetCachedDepartmentIds`：`DataScope:{userId}:{deptId}:{scope}`。`ClearCache(userId)` 无法枚举所有 deptId 时改为对该 userId 前缀删除，或 Clear 该用户时 `MemoryCache` 按前缀；最小实现：ClearCache(userId) 仍循环 scope 0–4 **且** 同时 Remove 不含 dept 的旧键（兼容），并文档约定改部门后调 `ClearCache(userId)`。

### 2.3 `GetFilter` / `CanAccess`

- `IUserScope`（非 IDataScope）：`全部` 已在入口返回 null；其余一律用户字段 Equal(当前)。**不要**对 IUserScope 走部门 IN。
- `IDepartmentScope`（非 IDataScope）：`仅本人` 与 `本部门` 都用 `deptField.Equal(context.DepartmentId)`（部门≤0 → Equal(-1)）。
- `IDataScope`：保持「仅本人走用户字段；部门类走 AccessibleDepartmentIds」；仅本人且无用户字段时退化为当前部门（已有）。
- `CanAccess(IDepartmentScope)`：`仅本人` 改为 `entity.DepartmentId == context.DepartmentId`，不要对空数组返回 false 导致本部门用户看不见自己的部门行。

### 2.4 `DataScopeInterceptor.OnValid`

`InvalidOperationException`：写审计日志后 **`return false`**（或继续抛出，由 XCode Valid 失败路径拒绝写入）。禁止 catch 后 `return true`。

无上下文：保持今日「不校验」（后台任务/种子数据）。列表过滤无上下文也不加条件。

### 2.5 菜单覆盖

`SetMenu`：`(Int32)menu.DataScope >= 0` 才覆盖。库默认 -1。不要给共享枚举增加「继承」项以免角色下拉出现。角色表单 DataScope 数据源仅为 0–4。

## 3. 实体挂载（接口 + 拦截器）

均在实体静态构造 `Meta.Interceptors.Add<DataScopeInterceptor>()`（可重复 Add 则只加一次）。显式实现 `IUserScope.UserId` 当 CLR 属性名不是 `UserId`。

| 实体 | 仓库 | 接口 | GetUserField / GetDepartmentField |
| --- | --- | --- | --- |
| User | XCode | 已有 IDataScope + FieldProvider | 已有 ID / DepartmentID |
| Department | XCode | 新增 IDepartmentScope + FieldProvider | Dept=`_.ID`；User=null |
| Log | XCode | IUserScope + FieldProvider | User=`_.CreateUserID` |
| UserToken / UserOnline / UserConnect | Cube Entity | IUserScope + FieldProvider | User=`_.UserID` |
| OAuthLog | Cube Entity | 同上 | User=`_.UserId`（以实体字段名为准） |
| NotificationRecord | Cube Entity | 同上 | User=`_.UserId` |

`IUserScope` 要求可写 `UserId`：显式接口映射到实际列，避免改表。

## 4. Cube 控制器接线

文件（WebAPI 与 CubeNC **各改对应副本**；共享的 `ReadOnlyEntityController2` 若已 Link 则只改一处）：

### 4.1 `CreateWhere` / `SearchData` / `FindData` / `ValidPermission`

`NewLife.Cube/Common/ReadOnlyEntityController2.cs`（CubeNC 同源 Link 则不双改）。

- 抽出 `protected Expression GetDataScopeExpression()` → `DataScopeHelper.GetFilter(Factory)`。
- `SearchData`：在现有 builder / viewFilter 逻辑上，把 GetFilter **AND** 进 `p.State`（builder 为 null 则 State=filter；与 viewFilter 同一 try 保护，解析失败时保留行权表达式、放弃 viewFilter）。
- `FindData`：查出后 `!CanAccess(entity)` 或 builder.Eval 失败 → 抛现有 `非法访问数据`。
- `ValidPermission`：默认改为 `CanAccess`（无接口为真）；现有 UserToken 等 override 改为先 `base.ValidPermission` 再业务规则。

`CanAccess` 分发：IDataScope / IUserScope / IDepartmentScope 重载；都不是则 true。

列表 `Index` / 导出：在返回前对 `IEnumerable<TEntity>` 调 `FieldScopeHelper.MaskSensitiveFields`。详情同样。

### 4.2 拆除特性

两栈删除这些行上的 `[DataPermission(null, "...")]`：User、Log、UserToken、UserOnline、UserConnect、OAuthLog、NotificationRecord。Department 保持无特性。

### 4.3 Role 表单

WebAPI `RoleController` 对齐 CubeNC：`AddFormFields`/`EditFormFields` 的 `DataDepartmentIds` 设 `DataSource` = 启用部门 ID→Name。列表可保留 DataScope 列。

### 4.4 GetPage 敏感标记

`DataField` 增 `Boolean Sensitive`（JSON camelCase `sensitive`）。`PrepareFieldsForApi`：若 `Factory.EntityType` 可赋为 `IFieldScope`，实例 `GetSensitiveFields()` 命中的字段：

- `context.ViewSensitive` 或（当前用户为本人且实体是单行上下文）→ 不标 sensitive、不强制藏列。GetPage 无当前行，列表字段：仅当 `ViewSensitive` 为真时 `Sensitive=false`；否则 `Sensitive=true` 且列表/详情字段对敏感列 `DataField` 可同时 `ShowIn` 保持，由前端藏列；**序列化脱敏仍以后端 Mask 为准**。
- 禁止把 password 哈希下发给非授权用户（Mask 为 `***`）。

本人看自己：列表含多人时 Mask 按行（`CanViewSensitiveFields` / UserId==当前）。`MaskSensitiveFields` 已有「ViewSensitive 或本人」逻辑；对 User 的 IUserScope.UserId=ID 成立。

## 5. ArcoVue

| 文件 | 改动 | 保留 |
| --- | --- | --- |
| `web/src/core/utils/dataScopeForm.ts`（新建） | `isCustomDataScope(v)`：4 或 `'自定义'`；`shouldShowDataDepartmentIds(model)` | 不在 vue 写分支 |
| Role 表单字段显隐 | DefaultForm / FieldInput：`Admin/Role` + 字段 DataDepartmentIds 且非自定义 → 不渲染 | 不改 Permission 树 |
| `iamGuards.ts` | `shouldShowSelfOnlyUserAlert`：增加「仅当可判定为仅本人」：`rows.length===1 && total∈{0,1,NaN} && id==自己` **保持**；本部门多人时自然不触发。文案不变 | 不根据角色名显示 |
| 列表列 | 字段 `sensitive===true` 不进可见列（纯函数 `rejectSensitiveColumns`） | 不把藏列当授权 |
| `*.spec.ts` | 覆盖显隐与 sensitive | |

不改 GetPage 匿名；不展示 Where 表达式。

## 6. 文件级改动地图

### 6.1 NewLife.XCode（必改）

| 文件 | 改 | 不动 |
| --- | --- | --- |
| `XCode/Membership/DataScopeContext.cs` | 缓存键；ClearCache | Create 的 IsSystem/Min 语义 |
| `XCode/Membership/DataScopeModule.cs` | OnValid 失败不放行 | OnInit 接口检测 |
| `XCode/Membership/角色.Biz.cs` | Valid 默认值；RoleDataScopeExtensions 若与 CanAccess 冲突则对齐 1.3 | Permission 位 |
| `XCode/Membership/用户.Biz.cs` | 加 DataScopeInterceptor | IDataScope 映射 |
| `XCode/Membership/部门.Biz.cs` | 接口 + 拦截器 + FieldProvider | 租户拦截器 |
| `XCode/Membership/日志.Biz.cs` | 接口 + 拦截器 + FieldProvider | InsertOnly |
| `XUnitTest.XCode/Membership/DataScopeTests.cs` | 补矩阵与 OnValid false | |

### 6.2 NewLife.Cube / CubeNC

| 文件 | 改 |
| --- | --- |
| `Common/ReadOnlyEntityController2.cs` | SearchData/FindData/ValidPermission/Mask |
| `Common/ReadOnlyEntityController.cs` | Index/详情返回前 Mask；PrepareFieldsForApi 标 Sensitive |
| `ViewModels/DataField.cs` | `Sensitive` |
| `Areas/Admin/Controllers/UserController.cs` 等 | 去 DataPermission（两栈） |
| `Areas/Admin/Controllers/RoleController.cs`（WebAPI） | DataDepartmentIds DataSource |
| `Entity/*用户令牌|在线|链接|OAuth日志|通知记录*.Biz.cs` | 接口 + 拦截器 |
| `Doc/PERM-数据权限.md` | 重写为 DataScope 矩阵 |
| `Doc/功能清单.md` | PERM-6 改为 DataScope 行权，注明本号 |

### 6.3 ArcoVue

见 §5。同步 `web/README.md` custom/数据权限一句；`竞品分析报告.md` § 行权限一句（事实：行权在服务端 DataScope）。

### 6.4 禁止改

- `EntityAuthorize` 菜单位检查、GetPage AllowAnonymous。
- OSC-0018 五件套、Cube.Vue 源码。
- 用 DataPermission 三字段构造器「实现」文档幻想 API。

## 7. 字段权限地基（本号边界）

本号交付：

- 请求内 `DataScopeContext.ViewSensitive`
- 行 Mask + GetPage `sensitive`
- 后续 OSC 可在同一上下文上做「角色字段白名单」，读取 `Current` 与字段名，**不必再发明行权**。

本号不交付：角色编辑器里的字段矩阵、按字段禁用编辑器。

## 8. 核心文档影响

| 文档 | 动作 |
| --- | --- |
| `Doc/PERM-数据权限.md` | 删除 DataScopeType / 错误特性签名 / 用户级 DataScope 覆盖；改为本节矩阵 |
| `Doc/功能清单.md` PERM-6 | 后端✅ 对齐 DataScope；ArcoVue 无独立 ACL UI |
| `ArcoVue企业中后台迁移方案.md` | 行权事实源改为 DataScope；DataPermission 降为可选额外 AND |
| `web/README.md` | 一句：行权服务端 DataScope，前端不筛选当授权 |

## 9. 测试设计

| 用例 | 期望 |
| --- | --- |
| GetFilter User 本部门 | DepartmentID=当前；不含外部门 |
| GetFilter User 仅本人 | ID=当前 |
| GetFilter Log 本部门 | CreateUserID=当前（不扩权） |
| GetFilter Department 仅本人 | ID=当前部门，不是 -1 |
| Role Insert 未设 DataScope | 普通默认本部门；显式 全部=0 保存后仍为 0 |
| Role Update DataScope=0 | 仍为全部 |
| Interceptor OnValid 改他人用户（仅本人） | false / 不落库 |
| CreateWhere 无接口实体 | GetFilter null |
| Mask User 列表无 ViewSensitive | 他人 Password=`***`；自己明文哈希按今日（或仍 *** 若 List 本无 Password 列——以 ListFields 为准） |
| Vitest 自定义才显示 DataDepartmentIds | DataScope 2 隐藏、4 显示 |

执行期：`dotnet test` XCode DataScopeTests + Cube 本号测试；`pnpm --filter @cube/arco-vue test` 相关 spec。

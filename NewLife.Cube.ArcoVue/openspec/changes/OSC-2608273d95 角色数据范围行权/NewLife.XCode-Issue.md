# 角色 DataScope 行权无法落地：Valid 把「全部」当未设置、拦截器失败放行、内置实体未挂载

> 本文供粘贴到 [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) Issue。  
> Cube / ArcoVue 侧对应规划：`OSC-2608273d95`（行权接线、拆除仅本人 `DataPermission`、字段脱敏地基）。**下列问题必须在 XCode 修，Cube 无法用 partial 跨程序集补接口，也无法挡住 `Role.Valid` 把 0 改写成「本部门」。**

---

## 标题（建议）

`[Membership] DataScope 无法作为行权事实源：Role.Valid 吞掉「全部」、DataScopeInterceptor 校验失败仍 return true、User/Department/Log 未挂载`

## 标签（建议）

`bug` `enhancement` `Membership` `security`

---

## 背景

XCode 已具备较完整的数据范围模型：

- 枚举 `DataScopes`：`全部=0` / `本部门及下级=1` / `本部门=2` / `仅本人=3` / `自定义=4`
- 角色字段 `Role.DataScope`、`DataDepartmentIds`、`ViewSensitive`
- `DataScopeContext.Create`、`DataScopeHelper.GetFilter` / `CanAccess`、`DataScopeInterceptor`

NewLife.Cube（WebAPI + MVC）+ ArcoVue 希望：**行级权限唯一跟角色 `DataScope` 走**（多角色取 `Min`，`IsSystem` 仍等于全部）。列表/详情/导出/写入与 ORM 查询应使用同一套 `GetFilter` / `CanAccess`。

当前 Cube 只能用 `[DataPermission(null, "ID={#userId}")]` 做「非系统角色仅本人」，**完全忽略** `Role.DataScope`。要把行权接到 `GetFilter`，XCode 侧有几处硬伤，导致「改角色数据范围」在运行时不成立，或一挂拦截器写入反而被放行。

消费方约定（已拍板，供实现时对齐，不必在 XCode 再讨论产品）：

1. 实现 `IDataScope` / `IUserScope` / `IDepartmentScope` 的实体自动按 DataScope 过滤；未实现的实体不加行权。
2. 仅 `IUserScope`、没有部门列的表（如日志）：`本部门/自定义` **不要** join 用户表放大成同事数据，仍按归属用户过滤。
3. 普通角色允许把 `DataScope` 存成 **全部（0）**，与枚举注释一致。
4. `IFieldScope` / `ViewSensitive` 的脱敏由 Cube 调现有 `MaskSensitiveFields`，XCode 只需保证接口与上下文正确。

---

## 问题 1（P0）：`Role.Valid` 把 `DataScope==0` 当成「未设置」

**文件：** `XCode/Membership/角色.Biz.cs`（约 133–142 行）

```csharp
if (DataScope == 0)
{
    DataScope = Type switch
    {
        RoleTypes.系统 => DataScopes.全部,
        RoleTypes.普通 => Name.Contains("高级") ? DataScopes.本部门及下级 : DataScopes.本部门,
        RoleTypes.租户 => DataScopes.本部门及下级,
        _ => DataScopes.仅本人,
    };
}
```

`DataScopes.全部` 的值就是 **0**。因此：

| 操作 | 实际结果 |
| --- | --- |
| 普通角色在界面选择「全部」并保存 | Insert/Update 必经 `Valid`，0 被改写成「本部门」或「本部门及下级」 |
| 系统角色未填范围 | 会被写成全部（碰巧符合） |
| 想区分「未赋值」和「显式全部」 | 当前无法区分 |

Cube 的 `RoleController.Valid` 无法覆盖：`entity.Insert/Update` 仍会再跑实体 `Valid`，二次保存同样被改写。

**期望：**

- **Insert** 且调用方 **未给 DataScope 赋值**（不在脏数据中）时，再按 `Type` 填默认值。
- **Update** 或 Insert 已显式赋值时，`0` 就是「全部」，原样落库。
- 不要用 `DataScope == 0` 作为未设置哨兵。

---

## 问题 2（P0）：`DataScopeInterceptor.OnValid` 失败后仍然放行

**文件：** `XCode/Membership/DataScopeModule.cs`（约 52–79 行）

`ValidInsert` / `ValidUpdateOrDelete` 在无权时 `throw new InvalidOperationException(...)`。`OnValid` 将其 catch 后写日志，然后 **`return true`**。

效果：一旦给实体挂上该拦截器，越权修改/删除会记一条失败日志，**写入仍成功**。这比不挂拦截器更危险。

**期望：** catch 后 `return false`，或不要 catch、让 Valid 失败路径拒绝保存。无 `DataScopeContext` 时保持今日「不校验」（种子数据/后台任务），可维持。

---

## 问题 3（P0）：内置实体未实现接口或未注册拦截器，`GetFilter` 对它们无效

`DataScopeHelper.GetFilter(factory)` 仅当实体类型实现 `IDataScope` / `IUserScope` / `IDepartmentScope` 时才生成条件。Cube **不能** 给 XCode 程序集里的类型补 `partial` 接口。

| 实体 | 现状 | 期望 |
| --- | --- | --- |
| `User` | 已实现 `IDataScope` + `IDataScopeFieldProvider`（UserId→ID，DepartmentId→DepartmentID），**未** `Add<DataScopeInterceptor>()` | 静态构造增加 `DataScopeInterceptor`（在修了问题 2 之后） |
| `Department` | 仅 `ITenantScope`，无部门行权接口 | 实现 `IDepartmentScope`（`DepartmentId` 映射为 `ID`）+ `IDataScopeFieldProvider.GetDepartmentField() => _.ID` + 拦截器 |
| `Log` | 无 scope 接口；归属列是 `CreateUserID` | 实现 `IUserScope`（显式映射到 `CreateUserID`）+ `GetUserField() => _.CreateUserID` + 拦截器 |

仅 `IUserScope` 的 `GetFilter` 在非「全部」时按用户字段过滤即可（日志不要按部门扩权），与问题 5 一致。

---

## 问题 4（P1）：仅 `IDepartmentScope` +「仅本人」变成空集

`GetAccessibleDepartmentIds` 在 `仅本人` 时不往集合里加部门，返回空数组。`BuildDepartmentFilter` 把空数组当成无权限：`deptField.Equal(-1)`。

`CanAccess(IDepartmentScope)`：`deptIds.Length==0` 时 `Contains` 为 false，本部门用户 **看不到自己的部门行**。

对「部门表一行 = 一个部门」：

| DataScope | 期望过滤 |
| --- | --- |
| 全部 | 无 |
| 仅本人 / 本部门 | `ID = 当前用户.DepartmentID`（无部门则恒假） |
| 本部门及下级 | `ID IN (自身+子孙)` |
| 自定义 | `ID IN DataDepartmentIds`（空则恒假） |

`IDataScope` 的「仅本人走 UserId」请保持，不要改 User 列表语义。

---

## 问题 5（P1，请保持/写进注释）：仅 `IUserScope` 不要按部门放大

`GetFilter` 对纯 `IUserScope`（非 `IDataScope`）在非全部时使用 `userField.Equal(context.UserId)`。这是日志/令牌类表想要的行为，请 **保持** 并在 XML 注释中写明：没有部门列时，`本部门/自定义` 不会变成同事可见。

---

## 问题 6（P1）：部门缓存键未包含 `DepartmentID`

**文件：** `DataScopeContext.GetCachedDepartmentIds`

```csharp
var key = $"DataScope:{userId}:{(Int32)scope}";
```

用户调岗后 5 分钟内仍用旧部门列表。`ClearCache(userId)` 也只删 `DataScope:{userId}:0..4`，与带 deptId 的新键对不上。

**期望：** key 含 `userId + deptId + scope`；`ClearCache(userId)` 能清掉该用户全部相关键（含旧格式，避免升级后脏缓存）。

---

## 问题 7（P2）：菜单 `DataScope` 默认值与枚举 0 冲突

`菜单.cs` 注释与列默认是 **-1 = 使用角色默认**，CLR 属性类型却是 `DataScopes`（默认 0 = 全部）。`DataScopeContext.SetMenu` 在 `menu.DataScope >= 0` 时用菜单值覆盖角色。

若历史数据或 `new Menu()` 得到 0，会把所有角色行权覆盖成「全部」。

**期望：**

- 比较与存储继续用「`< 0` 继承角色，`>= 0` 覆盖」；
- 保证新建菜单默认是 -1（或等价哨兵），不要让未配置菜单变成「全部」；
- 不必把「继承」加进角色下拉用的枚举项（避免角色表单出现 -1）。

---

## 建议改动清单（XCode）

1. `角色.Biz.cs`：仅 Insert 且 DataScope 未脏时写默认值。  
2. `DataScopeModule.cs`：`OnValid` 失败返回 false / 继续抛出。  
3. `DataScopeContext.cs`：缓存键与 ClearCache。  
4. `DataScopeHelper`：`IDepartmentScope` 仅本人 ≡ 本部门；纯 `IUserScope` 注释保持按用户。  
5. `用户.Biz.cs` / `部门.Biz.cs` / `日志.Biz.cs`：接口 + `DataScopeInterceptor`（2 修好后再挂）。  
6. `XUnitTest.XCode/Membership/DataScopeTests.cs`：覆盖下列验收。

Cube 将自行：在 `CreateWhere` 合并 `GetFilter`、拆掉仅本人 `DataPermission`、`MaskSensitiveFields`、给 **Cube 程序集内** 的 UserToken 等挂 `IUserScope`。不要求 XCode 改 Cube。

---

## 验收用例（XCode 单测即可）

1. 普通角色 `DataScope = 全部`，`Update()` 后读回仍为 `全部`。  
2. 普通角色 Insert 且未赋 DataScope，默认不是「未设置残留」，而是本部门或现有 Type 规则。  
3. 上下文为仅本人，拦截器 `OnValid(Update)` 改他人 `IDataScope` 数据 → **false 或不落库**。  
4. `GetFilter(typeof(User))`：本部门 → `DepartmentID = 当前部门`；仅本人 → `ID = 当前用户`。  
5. `GetFilter(typeof(Log))`（挂上 IUserScope 后）：本部门仍 `CreateUserID = 当前用户`。  
6. `GetFilter(typeof(Department))`：仅本人 → `ID = 当前部门`，不是 `ID = -1`。  
7. 同一 userId、换 `DepartmentID` 后 `AccessibleDepartmentIds` 立即按新部门算（或 ClearCache 后必新）。

---

## 环境

- 消费方：NewLife.Cube（Api 版 + CubeNC）、NewLife.Cube.ArcoVue  
- 规划号：OSC-2608273d95  
- 不要求 XCode 依赖 Cube 或前端

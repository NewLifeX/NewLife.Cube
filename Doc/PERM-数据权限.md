# 第9章 数据权限

> 本章介绍魔方的数据权限系统，实现数据级别的访问控制。
> 
> 数据权限在菜单权限基础上，进一步限制用户能够操作的数据范围。

---

## 9.1 数据权限概述

### 什么是数据权限

数据权限是在功能权限（菜单/操作权限）基础上的进一步细化：

| 权限类型 | 控制对象 | 示例 |
|---------|---------|------|
| 功能权限 | 能做什么操作 | 能否访问用户管理、能否删除数据 |
| 数据权限 | 能操作哪些数据 | 只能看到本部门数据、只能编辑自己创建的数据 |

### 应用场景

- **多部门系统**：各部门只能看到本部门数据
- **多租户系统**：租户之间数据完全隔离
- **分级管理**：上级可以看到下级数据
- **创建者限制**：只能操作自己创建的数据

---

## 9.2 DataPermissionAttribute 特性

### 基本用法

```csharp
[DataPermission(null, "UserID={#userId}")]
public class UserTokenController : EntityController<UserToken, UserTokenModel>
{
    // 列表、导出、详情、编辑、删除等动作统一按表达式过滤
}
```

### 特性定义

```csharp
public class DataPermissionAttribute : Attribute
{
    /// <summary>不受限的系统角色名。逗号或分号分隔</summary>
    public String SystemRoles { get; set; }

    /// <summary>条件表达式。如 linkid in {#SiteIds} or CreateUserID={$user.Id}</summary>
    public String Expression { get; set; }
}
```

- 当前登录用户为系统角色（`IRole.IsSystem`）或 `SystemRoles` 命中时跳过过滤
- 表达式由 XCode `WhereBuilder` 解析：支持 `=`、`!=`、`in` 与 `and`/`or` 串联，不支持括号
- `{#xxx}` 取 `HttpContext.Items`（如 `{#userId}`、`{#TenantId}`），`{$xxx}` 取 Session

### 生效范围

| 动作 | 入口 | 说明 |
|------|------|------|
| 列表 / 分页 | `SearchData` → `CreateWhere` → `p.State` | 条件随查询进入 SQL |
| 导出 | `ExportData` / `ExportDataByPage` / `ExportDataByDatetime` | 与列表同源，含时间分片导出 |
| 详情 / 编辑 / 删除 / 批量 / 导入 | `FindData` | 按表达式二次校验，不通过抛"非法访问数据" |
| 令牌接口（Html/Json/Xml/Csv/Excel） | `ValidToken` 注入用户后走 `SearchData` | 与页面一致 |
| 页面 AI 工具 | `IEntityAiContext.SearchData` | 复用控制器管道 |

---

## 9.3 DataScopeMiddleware 中间件

### 中间件职责

1. **租户上下文**：多租户开启时解析并设置 `TenantContext`（见多租户章节）
2. **宿主数据权限声明**：注入系统态上下文，声明"实体层以系统身份运行"

```csharp
// DataScopeMiddleware 内部：UseCube 已自动注册，无需手动 UseDataScope
if (DataScopeContext.Current == null)
{
    // 系统态上下文：实体层不参与行级过滤；保留用户身份供审计字段填充
    DataScopeContext.Current = CreateHostScope(ManageProvider.User);
    dataScopeChanged = true;
}
```

### 为什么注入系统态

XCode 数据权限拦截器在上下文缺失时会用 `ManageProvider.User` 兜底创建上下文。若宿主不声明，请求内**一切**实体查询（SSO、服务、自定义 Action）都会被隐式收窄；而实体层同时服务 Web、服务层与算法层等消费方，不应承担 Web 展示层的数据隔离职责。

宿主声明系统态后，数据权限只由接口层控制器特性（见 9.2）显式执行；拦截器能力保留，业务代码可显式调用 `DataScopeHelper.ApplyScope` / `CanAccess`（见 9.4）。

---

## 9.4 数据范围（DataScopes）

### XCode 数据范围枚举

| 值 | 名称 | 含义 |
|----|------|------|
| -1 | 默认 | 未设置，按角色取值 |
| 0 | 全部 | 不受限 |
| 1 | 本部门及下级 | 本部门及其所有下级部门 |
| 2 | 本部门 | 仅本部门 |
| 3 | 仅本人 | 仅用户本人 |
| 4 | 自定义 | 角色上配置的部门集合 |

来源：`XCode.Membership.DataScopes`（角色/菜单的 `DataScope` 字段）。

### 在魔方中的定位

- 魔方页面的行级过滤统一由控制器 `[DataPermission]` 特性表达，角色/菜单数据范围不再隐式作用于页面
- 数据范围能力保留在 XCode，可在业务代码中显式使用：

```csharp
// 显式按当前用户数据范围过滤（适用于"与用户视野一致"的查询）
var exp = new WhereExpression();
exp &= Order._.Status == 1;
exp = exp.ApplyScope<Order>();      // 合并租户和数据权限过滤
return Order.FindAll(exp, page);

// 单条数据校验
if (!DataScopeHelper.CanAccess(order)) throw new UnauthorizedAccessException();
```

---

## 9.5 数据权限配置与使用

### 给页面新增行级过滤

按页面语义选择表达式（`[DataPermission(SystemRoles, Expression)]`）：

| 场景 | 表达式示例 |
|------|-----------|
| 仅本人数据（令牌/参数/在线） | `UserID={#userId}` |
| 创建者本人（日志/附件） | `CreateUserID={#userId}` |
| 行归属为负责人（部门） | `ManagerID={#userId}` |
| 双方字段任一（委托代理） | `PrincipalId={#userId} or AgentId={#userId}` |
| 用户记录本人（用户页） | `ID={#userId}` |
| 站点/地区集合 | `SiteId in {#SiteIds}` |

> `{#userId}` 由控制器管线写入（`ControllerBaseX.OnActionExecuting`，令牌接口由 `ValidToken` 写入）；控制器之外没有该数据源。

### 业务代码直接操作实体

实体层不承担数据权限，业务代码（SSO、服务、后台任务）直接查询与写入，无需构造数据上下文：

```csharp
// openid 查重必须跨用户可见，禁止在此添加用户范围过滤
var uc = UserConnect.FindByProviderAndOpenID(client.Name, openid);
```

### 页面自定义校验

行归属语义无法用表达式表达时，重写 `FindData`：

```csharp
protected override Department FindData(Object key)
{
    var entity = Find(key);
    if (entity != null && !CanView(entity)) throw new InvalidOperationException($"非法访问数据[{key}]");
    return entity;
}
```

---

## 9.6 架构边界与迁移说明

### 边界

| 层 | 是否承担数据权限 | 说明 |
|----|------------------|------|
| XCode 实体层 | ❌ | 纯数据访问；宿主以系统身份运行，拦截器不参与行过滤 |
| 魔方控制器（接口层） | ✅ | `[DataPermission]` 特性 + `SearchData`/`FindData` 管道，唯一执行点 |
| 业务代码（SSO/服务/任务） | ❌ | 直接操作任意实体，不受数据权限影响 |
| 多租户 | — | 独立链路（`TenantContext` + `ITenantScope`），不受本章调整影响 |

### 历史与迁移

早期曾把数据权限下沉到实体层（实体实现 `IUserScope` 并注册 `DataScopeInterceptor`，全宿主自动过滤），带来三类问题：业务代码被隐式收窄（SSO 绑定需要临时提升数据范围）、同字段条件多次叠加、菜单数据范围配置引发全站失效事故。现已回退，数据权限回归接口层。

**下游应用迁移**：

- 依赖实体级过滤的页面 → 在对应控制器标注 `[DataPermission]`（表达式见 9.5）
- 需要在业务代码中按用户视野过滤 → 显式调用 `DataScopeHelper.ApplyScope` / `CanAccess`（见 9.4）
- 不再需要任何"临时提升数据范围"的代码（原 `DataScopeContext.Current = ...` 补丁已删除）

---

## 数据权限最佳实践

### 1. 实体设计

确保实体包含数据权限所需字段：

```csharp
public partial class Order : Entity<Order>
{
    /// <summary>租户ID</summary>
    public Int32 TenantId { get; set; }
    
    /// <summary>部门ID</summary>
    public Int32 DepartmentId { get; set; }
    
    /// <summary>创建人ID</summary>
    public Int32 CreateUserId { get; set; }
    
    /// <summary>创建人</summary>
    public String CreateUser { get; set; }
    
    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }
}
```

### 2. 自动填充字段

在保存数据时自动填充权限字段：

```csharp
protected override Int32 OnInsert(Order entity)
{
    var user = ManageProvider.User as User;
    
    // 自动填充权限字段
    entity.TenantId = user?.TenantId ?? 0;
    entity.DepartmentId = user?.DepartmentId ?? 0;
    entity.CreateUserId = user?.Id ?? 0;
    entity.CreateUser = user?.Name;
    entity.CreateTime = DateTime.Now;
    
    return base.OnInsert(entity);
}
```

### 3. 性能优化

数据权限条件会影响查询性能，建议：

```csharp
// 为数据权限字段添加索引
public class Order : Entity<Order>
{
    // 在 Model.xml 中定义索引
    // <Index Columns="TenantId" />
    // <Index Columns="DepartmentId" />
    // <Index Columns="CreateUserId" />
}
```

### 4. 联合查询的数据权限

```csharp
// 主从表联合查询时，需要对主表应用数据权限
var orders = Order.FindAll(
    Order._.Status == 1 & 
    Order._.DepartmentId == currentDeptId,  // 数据权限
    null, null, 0, 100);

// 从表通过主表关联，不需要单独的数据权限
foreach (var order in orders)
{
    var items = OrderItem.FindAllByOrderId(order.Id);
}
```

---

## 本章小结

通过本章学习，你应该掌握了：

1. **数据权限概念**：区分功能权限和数据权限
2. **DataPermissionAttribute**：控制器行级过滤，覆盖列表/导出/详情/编辑/删除/令牌接口
3. **DataScopeMiddleware**：租户上下文 + 宿主系统态声明（实体层以系统身份运行）
4. **数据范围（DataScopes）**：能力保留在 XCode，魔方页面不再隐式消费
5. **架构边界**：数据权限唯一执行点在接口层，业务代码直接操作实体

**下一步**：

- 学习 [多租户架构](PERM-多租户架构.md) 了解租户级数据隔离
- 了解 [安全与审计](SYS-安全与审计.md) 的安全增强措施

---

## 参考资源

- [角色菜单与权限控制](https://newlifex.com/cube/permission)

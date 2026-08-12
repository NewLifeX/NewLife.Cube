# 魔方多租户架构设计文档

## 一、概述

魔方（NewLife.Cube）支持多租户架构，允许在同一套系统中为多个租户提供隔离的数据和功能。本文档详细说明多租户的设计原理、核心组件及使用方式。

### 1.1 设计目标

- **数据隔离**：租户数据严格隔离，防止跨租户访问
- **灵活切换**：用户可属于多个租户，登录后可自由切换
- **统一管理**：管理后台可查看和管理所有租户数据
- **透明接入**：业务代码无需关心租户过滤，框架自动处理

### 1.2 架构特点

- 共享数据库 + 行级隔离（TenantId 字段）
- 基于 `AsyncLocal` 的上下文传递
- 支持 Web、API、小程序等多端接入
- 兼容无租户场景（`EnableTenant=false`）

---

## 二、应用场景

魔方多租户支持三种工作模式：

```
┌──────────────────────────────────────────────────────────────────────────┐
│                          魔方多租户场景矩阵                               │
├──────────────────┬────────────────┬────────────────┬────────────────────┤
│                  │ 无租户场景      │ 管理后台场景   │ 租户场景           │
│                  │ EnableTenant   │ TenantId=0    │ TenantId>0         │
│                  │   =false       │               │                    │
├──────────────────┼────────────────┼────────────────┼────────────────────┤
│ TenantContext    │ null           │ {TenantId:0}  │ {TenantId:123}     │
│ .Current         │                │               │                    │
├──────────────────┼────────────────┼────────────────┼────────────────────┤
│ 数据增删改校验   │ 不拦截         │ 不拦截        │ 拦截+校验          │
├──────────────────┼────────────────┼────────────────┼────────────────────┤
│ 数据查询过滤     │ 不追加条件     │ 不追加条件    │ 追加TenantId过滤   │
├──────────────────┼────────────────┼────────────────┼────────────────────┤
│ 菜单显示         │ 全部菜单       │ Admin模式菜单 │ Tenant模式菜单     │
├──────────────────┼────────────────┼────────────────┼────────────────────┤
│ 典型用户         │ 普通项目       │ 平台管理员    │ 租户用户           │
└──────────────────┴────────────────┴────────────────┴────────────────────┘
```

### 2.1 无租户场景

- 配置 `EnableTenant=false`
- 系统作为普通单租户应用运行
- 所有多租户逻辑被跳过

### 2.2 管理后台场景（TenantId=0）

- 平台管理员进入系统后台
- 可查看和管理所有租户的数据
- 可创建、编辑、禁用租户
- 不受租户数据隔离限制

### 2.3 租户场景（TenantId>0）

- 租户用户进入特定租户
- 只能看到本租户的数据
- 增删改操作受租户校验保护
- 菜单按租户权限过滤

---

## 三、核心组件

### 3.1 TenantContext（租户上下文）

位置：`XCode.Membership.TenantContext`

```csharp
/// <summary>租户上下文</summary>
public class TenantContext
{
    /// <summary>租户标识。0表示进入管理后台，没有进入任意租户</summary>
    public Int32 TenantId { get; set; }

    /// <summary>租户对象（延迟加载）</summary>
    public ITenant? Tenant { get; set; }

    /// <summary>当前租户上下文（AsyncLocal）</summary>
    public static TenantContext Current { get; set; }

    /// <summary>当前租户标识。无效时返回0</summary>
    public static Int32 CurrentId => Current?.TenantId ?? 0;
}
```

**核心特性**：
- 使用 `AsyncLocal<T>` 实现异步上下文传递
- `TenantId=0` 表示管理后台模式
- `TenantId>0` 表示进入特定租户
- `Current=null` 表示未启用多租户或未设置

### 3.2 ITenantSource（租户数据接口）

位置：`XCode.Membership.ITenantSource`

```csharp
/// <summary>租户数据源接口，指示该类带有租户标识TenantId</summary>
public interface ITenantSource
{
    /// <summary>租户标识</summary>
    Int32 TenantId { get; set; }
}
```

**使用方式**：
```csharp
// 实体类实现 ITenantSource 接口即可参与租户隔离
public partial class Order : Entity<Order>, ITenantSource
{
    public Int32 TenantId { get; set; }
    // ... 其他字段
}
```

### 3.3 TenantModule（租户过滤器）

位置：`XCode.Membership.TenantModule`

XCode 实体类的全局拦截器，在数据增删改时自动处理租户逻辑：

| 操作 | 行为 |
|------|------|
| **Insert** | 自动设置 TenantId；如已设置且不匹配则抛异常 |
| **Update** | 校验 TenantId 归属，不匹配则抛异常 |
| **Delete** | 校验 TenantId 归属，不匹配则抛异常 |

**注意**：当 `TenantContext.Current=null` 或 `TenantId=0`（管理后台）时，不执行校验。

### 3.4 TenantMiddleware（租户中间件）

位置：`NewLife.Cube.WebMiddleware.TenantMiddleware`

ASP.NET Core 中间件，负责在请求入口设置租户上下文：

```csharp
public async Task Invoke(HttpContext ctx)
{
    var changed = false;
    try
    {
        var set = CubeSetting.Current;
        if (set.EnableTenant && TenantContext.Current == null)
        {
            var tenantId = ctx.GetTenantId();  // 从 Header/Query/Cookie 获取
            if (tenantId >= 0)
            {
                ctx.SetTenant(tenantId);
                changed = true;
            }
        }
        await _next.Invoke(ctx);
    }
    finally
    {
        if (changed) TenantContext.Current = null;
    }
}
```

### 3.5 相关实体类

| 实体类 | 说明 |
|--------|------|
| `Tenant` | 租户信息表，存储租户名称、编码、管理员等 |
| `TenantUser` | 租户用户关系表，记录用户属于哪些租户 |

---

## 四、数据隔离机制

### 4.1 三层防线

```
┌─────────────────────────────────────────────────────────────────────┐
│                      第一层：XCode 数据层                             │
│  TenantModule (EntityModule)                                         │
│  ├─ OnCreate: 新建实体时自动设置 TenantId                             │
│  └─ OnValid:  Insert/Update/Delete 时校验 TenantId 归属              │
└─────────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      第二层：控制器应用层                             │
│  ReadOnlyEntityController2.CreateWhere()                             │
│  └─ 为 ITenantSource 实体自动追加 "TenantId={#TenantId}" 过滤条件    │
└─────────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      第三层：单实体校验                               │
│  WhereBuilder.Eval()                                                 │
│  └─ 校验单个实体的 TenantId 是否匹配当前上下文                        │
└─────────────────────────────────────────────────────────────────────┘
```

### 4.2 查询过滤逻辑（fail-closed）

`CreateWhere` 方法中的多租户处理逻辑（**多租户开启且实体为租户数据源时 fail-closed，绝不静默全量**）：

```csharp
if (set.EnableTenant && IsTenantSource)
{
    var ctxTenant = TenantContext.Current;

    // 无租户上下文（未设置/匿名请求）：拒绝查询，防止无租户场景看到全量数据
    if (ctxTenant == null)
        exp = "1=0";
    else if (ctxTenant.TenantId == 0)
    {
        // 管理后台模式（TenantId=0）：不限制租户数据，管理后台要能看到所有租户的数据
    }
    else
    {
        // 租户模式（TenantId>0）：校验租户存在且启用，无效则 fail-closed
        var tenant = ctxTenant.Tenant ?? Tenant.FindById(ctxTenant.TenantId);
        if (tenant == null || !tenant.Enable)
            exp = "1=0";   // 伪造/无效租户 → 空结果，而非全量
        else
            exp = "TenantId={#TenantId}";   // 追加租户过滤条件
    }
}
```

> **设计要点**：租户上下文缺失、租户不存在或已禁用时返回 `1=0` 空结果集（fail-closed），配合中间件的 400/403，杜绝"伪造租户ID绕过数据隔离"与"无租户上下文看到全量数据"两条泄漏路径。

### 4.3 写路径归属校验

控制器 `ReadOnlyEntityController2.Valid`（所有 Add/Edit/Delete/批量删除的统一写校验入口）在多租户开启且实体为租户数据源时强制校验归属：

```csharp
if (post && CubeSetting.Current.EnableTenant && IsTenantSource)
{
    var tenantId = TenantContext.CurrentId;
    if (tenantId > 0)
    {
        var entityTenantId = (entity as IEntity)?["TenantId"].ToInt() ?? 0;
        switch (type)
        {
            case DataObjectMethodType.Insert:
                (entity as IEntity)["TenantId"] = tenantId;   // 新增强制归属当前租户
                break;
            case DataObjectMethodType.Update:
            case DataObjectMethodType.Delete:
                if (entityTenantId != tenantId)               // 修改/删除校验归属
                    throw new NoPermissionException(...);
                break;
        }
    }
}
```

---

## 五、认证授权流程

### 5.1 用户登录流程

```
用户登录 → JWT令牌（不含TenantId） → ChooseTenant校验
                                        │
                                        ▼
                              TenantUser.FindAllByUserId(userId)
                                        │
                              过滤 Enable=true 的租户列表
                                        │
                              根据 Cookie 中 TenantId 选择租户
                                        │
                           ┌────────────┴────────────┐
                           │                         │
                       在列表中                   不在列表中
                           │                         │
                      进入该租户              进入第一个有效租户
```

### 5.2 租户标识获取优先级

`GetTenantId` 方法按以下顺序获取租户标识，**各来源语义不同**：

1. **Header `X-Tenant`**（优先，**租户编码 Code**，适合前后端分离/小程序）→ 按 `Tenant.FindByCode` 解析
2. **Header `X-Tenant-Id`**（已废弃，仅兼容老客户端，**数字ID**）→ 按 `Int32` 解析
3. **QueryString `tenantId`**（用于调试/回调，**数字ID**）→ 按 `Int32` 解析
4. **Cookie `TenantId-{SysName}`**（兼容浏览器模式，存的是**数字ID**）→ 按 `Int32` 解析

> ⚠️ **语义约定**：`X-Tenant` 传**租户编码**（如 `abc`），`X-Tenant-Id` 传**数字ID**（如 `123`），两者不混用。租户编码即使为纯数字字符串（如 `"123"`）也会按编码 `FindByCode` 查询，不会误判为数字ID。

多租户开启时，伪造、不存在或已禁用的租户标识会被拒绝（返回 400/403）；未开启多租户时不校验租户。

### 5.3 JWT 设计说明

JWT 令牌**不包含** TenantId，这是有意为之：
- 用户可能属于多个租户
- 登录后可以自由切换租户
- 租户信息通过 Cookie/Header 携带

**认证层租户校验（fail-closed）**：`TryLogin` 与 `Auth`（`ControllerBaseX`）在 token 校验通过后统一调用 `HttpContext.ValidateTenant(user)`：
- 未开启多租户 → 直接放行
- 系统管理员 → 可进入管理后台（租户0）
- 普通用户 → 必须处于有效租户上下文（`TenantId > 0`），无有效租户则拒绝访问（401/403），防止落到租户0/空上下文看到全量数据

**授权层模式校验**：`EntityAuthorizeAttribute` 在权限校验通过后按 `MenuHelper.CheckVisible(controllerType, isTenant)` 强制校验菜单模式——租户模式禁止访问纯 `MenuModes.Admin` 菜单（系统管理），管理后台禁止访问纯 `MenuModes.Tenant` 菜单（租户页面），URL 直达也会被拦截。

### 5.4 租户切换与安全加固

- **租户切换入口**（`IndexController ?TenantId=`）：管理后台（租户0）**仅系统管理员**可切换；普通用户仅能切换到其所属的有效租户（`TenantUser.Enable`）。
- **租户设置接口**（`UserController.TenantSetting`）：写入租户 Cookie 前校验 `TenantUser` 归属，无权限租户直接拒绝。
- **租户 Cookie 加固**：`SaveTenant` 写入的 `TenantId-{SysName}` Cookie 已设置 `HttpOnly`，禁止脚本读取，防止 XSS 改写租户。
- **租户上下文生命周期**：`DataScopeMiddleware` 在请求进入时保存旧租户上下文，`finally` **无条件恢复**，防止 AsyncLocal 值跨请求/后台任务泄漏。
- **注册入口统一**：`/Auth/Register`（UserService）与 `/Admin/User/Register`（表单）解析租户后均自动创建 `TenantUser` 绑定；未开启多租户时不解析也不设置租户上下文。
- **菜单树接口过滤**：`IndexController.GetMenu/GetMenuTree` 返回的菜单树同样应用 `MenuHelper.FilterByTenant`，与视图层菜单显示一致，防止前端拿到不该显示的菜单。
- **微信登录**：请求体 `appId` 与请求头 `X-App-Id` 同时存在且不一致时拒绝（避免租户归属歧义）；登录成功后通过响应头 `X-Tenant` 返回租户编码，供客户端后续请求携带。

---

## 六、配置说明

### 6.1 启用多租户

在 `CubeSetting` 中配置：

```csharp
/// <summary>多租户。是否支持多租户</summary>
[Description("多租户。是否支持多租户，租户模式禁止访问系统管理，平台管理模式禁止访问租户页面")]
[Category("系统功能")]
public Boolean EnableTenant { get; set; }
```

或在数据库参数表 `Parameter` 中设置 `Cube:EnableTenant = true`。

### 6.2 菜单模式配置

控制器通过 `MenuAttribute` 指定菜单在不同模式下的可见性：

```csharp
// 仅管理后台可见
[Menu(100, true, Icon = "fa-cog", Mode = MenuModes.Admin)]
public class SystemController : EntityController<Config> { }

// 仅租户可见
[Menu(100, true, Icon = "fa-list", Mode = MenuModes.Tenant)]
public class OrderController : EntityController<Order> { }

// 管理后台和租户都可见
[Menu(100, true, Icon = "fa-users", Mode = MenuModes.Admin | MenuModes.Tenant)]
public class TenantUserController : EntityController<TenantUser> { }
```

### 6.3 租户相关配置示例

```csharp
// OAuthConfig 已支持多租户隔离
var oauthConfigs = OAuthConfig.GetValids(TenantContext.CurrentId, GrantTypes.Password);
```

---

## 七、开发指南

### 7.1 创建租户隔离的实体

```csharp
// 1. 实体类实现 ITenantSource 接口
public partial class Product : Entity<Product>, ITenantSource
{
    /// <summary>租户</summary>
    [DisplayName("租户")]
    [Map(nameof(TenantId), typeof(Tenant), "Id")]
    public Int32 TenantId { get; set; }
    
    // ... 其他业务字段
}

// 2. 控制器无需特殊处理，框架自动过滤
public class ProductController : EntityController<Product>
{
    // CreateWhere 会自动追加 TenantId 过滤
    // TenantModule 会自动校验增删改操作
}
```

### 7.2 获取当前租户信息

```csharp
// 获取当前租户ID
var tenantId = TenantContext.CurrentId;

// 获取当前租户对象
var tenant = TenantContext.Current?.Tenant;

// 判断是否在租户模式
var isTenantMode = TenantContext.CurrentId > 0;

// 判断是否在管理后台
var isAdminMode = TenantContext.Current != null && TenantContext.CurrentId == 0;
```

### 7.3 手动设置租户上下文

```csharp
// 在后台任务中设置租户上下文
TenantContext.Current = new TenantContext { TenantId = 123 };
try
{
    // 执行租户相关业务逻辑
    var orders = Order.FindAll();  // 自动带上租户过滤
}
finally
{
    TenantContext.Current = null;
}
```

### 7.4 跳过租户校验

```csharp
// 管理后台模式下操作所有租户数据
TenantContext.Current = new TenantContext { TenantId = 0 };
try
{
    // TenantId=0 时 TenantModule 不执行校验
    var allOrders = Order.FindAll();
}
finally
{
    TenantContext.Current = null;
}
```

---

## 八、最佳实践

### 8.1 大租户处理

当某个租户数据量过大时，推荐方案：

1. **独立部署**：重新部署一套魔方系统
2. **数据迁移**：将该租户数据迁移到新系统
3. **单租户运行**：新系统可配置 `EnableTenant=false`

这种方式通过部署层面解决隔离需求，避免代码层面的复杂性。

### 8.2 前端租户切换

```javascript
// 切换租户（写入 Cookie）
await fetch('/Admin/Tenant/Switch?tenantId=123', { method: 'POST' });

// API 请求携带租户标识
fetch('/api/orders', {
    headers: {
        'Authorization': 'Bearer ' + token,
        'X-Tenant': '123'  // 或租户编码
    }
});
```

### 8.3 缓存说明

魔方多租户共享数据库，数据表主键ID是全局唯一的，因此：
- **无需按租户隔离缓存**
- 缓存 Key 不会冲突
- 实体缓存可正常使用

### 8.4 安全注意事项

1. **Cookie 篡改**：用户修改 Cookie 中的 TenantId 不会造成安全问题
   - `ChooseTenant` 会校验用户是否属于该租户
   - 无权限的租户会被拒绝或重定向

2. **数据校验**：`TenantModule` 在增删改时强制校验
   - 防止通过 API 直接操作其他租户数据

3. **查询过滤**：`CreateWhere` 自动追加过滤条件
   - 开发者无需手动处理

---

## 九、常见问题

### Q1：如何判断当前是否启用了多租户？

```csharp
var enableTenant = CubeSetting.Current.EnableTenant;
```

### Q2：如何判断当前在哪个模式？

```csharp
// 无租户或未设置
if (TenantContext.Current == null) { /* 无租户模式 */ }

// 管理后台
if (TenantContext.CurrentId == 0) { /* 管理后台模式 */ }

// 租户模式
if (TenantContext.CurrentId > 0) { /* 租户模式 */ }
```

### Q3：为什么 TenantModule 在 TenantId=0 时不校验？

设计如此。`TenantId=0` 表示管理后台模式，需要能够操作所有租户数据。

### Q4：如何让配置项支持多租户？

参考 `OAuthConfig` 的实现：
1. 实体类增加 `TenantId` 字段并实现 `ITenantSource`
2. 查询方法增加 `tenantId` 参数过滤

```csharp
public static IList<MyConfig> GetValids(Int32 tenantId) 
    => FindAllWithCache().Where(e => e.Enable && e.TenantId == tenantId).ToList();
```

---

## 十、版本历史

| 版本 | 日期 | 说明 |
|------|------|------|
| v1.0 | 2024 | 初始多租户架构设计 |
| v1.1 | 2025 | 统一 TenantContext 体系，增强 TenantModule 校验 |

---

## 附录：相关源码位置

| 组件 | 路径 |
|------|------|
| TenantContext | `XCode/Membership/ITenantSource.cs` |
| ITenantSource | `XCode/Membership/ITenantSource.cs` |
| TenantModule | `XCode/Membership/ITenantSource.cs` |
| TenantMiddleware | `NewLife.CubeNC/WebMiddleware/TenantMiddleware.cs` |
| ManagerProviderHelper | `NewLife.CubeNC/Membership/ManagerProviderHelper.cs` |
| CreateWhere | `NewLife.Cube/Common/ReadOnlyEntityController2.cs` |
| WhereBuilder | `XCode/Model/WhereBuilder.cs` |
| CubeSetting | `NewLife.Cube/Setting.cs` |

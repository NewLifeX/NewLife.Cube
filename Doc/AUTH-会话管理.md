# 会话管理

魔方提供独立于 ASP.NET Core Session 的轻量级会话管理机制，通过 `SessionProvider` 和 `RunTimeMiddleware` 协同工作，实现用户身份维持、在线状态跟踪和 SSO 令牌管理。

## 设计理念

魔方并未直接使用 ASP.NET Core 的 `ISession`，而是自行实现了基于内存字典的会话机制。主要原因：

| 对比项 | ASP.NET Core Session | 魔方 SessionProvider |
|--------|---------------------|---------------------|
| 存储方式 | 序列化到内存/Redis/数据库 | 内存字典直接存储对象引用 |
| 存储内容 | 仅字符串/字节数组 | 任意对象（IManageUser 等） |
| 初始化时机 | 需中间件参与，有延迟 | 在 RunTimeMiddleware 中立即可用 |
| 多源 ID | 仅 Cookie | Cookie + Token + ASP.NET Session |
| SSO 兼容 | 需额外适配 | 原生支持令牌作为 SessionId |

## SessionProvider

`SessionProvider` 使用内存字典存储会话数据，支持并发访问和自动过期清理。

```csharp
public class SessionProvider
{
    private static readonly Dictionary<String, IDictionary<String, Object>> _sessions
        = new();

    public IDictionary<String, Object> GetSession(String sessionId)
    {
        if (sessionId.IsNullOrEmpty()) return null;

        lock (_sessions)
        {
            if (!_sessions.ContainsKey(sessionId))
                _sessions[sessionId] = new Dictionary<String, Object>();
            return _sessions[sessionId];
        }
    }
}
```

### 特性

- **线程安全**：通过 `lock` 保证并发安全
- **空值友好**：`sessionId` 为空时返回 `null` 而非抛出异常
- **自动创建**：首次访问自动创建新的会话字典
- **对象存储**：直接存储 .NET 对象引用，无需序列化

## 会话创建流程

会话在 `RunTimeMiddleware.CreateSession()` 中创建，核心逻辑位于 `NewLife.Cube/WebMiddleware/RunTimeMiddleware.cs`。

### SessionId 获取优先级

```
1. ASP.NET Session 中的 .Cube.Session 键
    ↓ 无值
2. Cookie 中的 .Cube.Session
    ↓ 无值
3. Cookie 中的 .AspNetCore.Session
    ↓ 无值
4. Authorization Header 中的令牌（Token）
    ↓ 都无值
5. 生成 16 位随机字符串作为新 SessionId
```

### 完整实现

```csharp
private static IDictionary<String, Object> CreateSession(HttpContext ctx)
{
    // 获取原生 ASP.NET Session（如果启用）
    var ss = ctx.Features.Get<ISessionFeature>()?.Session;
    if (ss != null && !ss.IsAvailable) ss = null;

    var key = ".Cube.Session";
    var sid = "";

    // 按优先级获取 SessionId
    if (sid.IsNullOrEmpty()) sid = ss?.GetString(key);
    if (sid.IsNullOrEmpty()) sid = ctx.Request.Cookies[key];
    if (sid.IsNullOrEmpty()) sid = ctx.Request.Cookies[".AspNetCore.Session"];
    if (sid.IsNullOrEmpty()) sid = ctx.LoadToken();

    // 都没有则生成新的 SessionId
    if (sid.IsNullOrEmpty())
    {
        sid = Rand.NextString(16);
        if (ss != null)
            ss.SetString(key, sid);
        else
            ctx.Response.Cookies.Append(key, sid);
    }
    else
    {
        // 过长的令牌 MD5 化简（如 JWT）
        if (sid.Length > 32) sid = sid.MD5();
        ss?.SetString(key, sid);
    }

    // 获取或创建会话字典
    var session = _sessionProvider.GetSession(sid);
    ctx.Items["Session"] = session;

    return session;
}
```

### 关键细节

| 行为 | 说明 |
|------|------|
| Token 作为 SessionId | WebAPI 场景下，Bearer Token 直接作为 SessionId，无需 Cookie |
| 长令牌 MD5 化简 | 超过 32 位的令牌（如 JWT）自动 MD5 压缩为固定长度 |
| Cookie 写入 | 新生成的 SessionId 写入 `.Cube.Session` Cookie |
| Session 缓存 | 会话字典存入 `HttpContext.Items["Session"]`，同一请求内复用 |

## 用户身份存取

`ManageProvider2` 继承 `ManageProvider`，负责从会话中读写当前登录用户。

### 获取当前用户

```csharp
public override IManageUser GetCurrent(IServiceProvider context = null)
{
    var ctx = Context?.HttpContext;
    if (ctx == null) return null;

    try
    {
        // 优先从 HttpContext.Items 缓存读取（同一请求内只读一次 Session）
        if (ctx.Items["CurrentUser"] is IManageUser user) return user;

        // 从 Session 字典读取
        var session = ctx.Items["Session"] as IDictionary<String, Object>;
        user = session?[SessionKey] as IManageUser;

        // 缓存到 Items 避免重复读取
        ctx.Items["CurrentUser"] = user;
        return user;
    }
    catch (InvalidOperationException ex)
    {
        XTrace.WriteException(ex);
        return null;
    }
}
```

### 保存当前用户（登录/切换）

```csharp
public override void SetCurrent(IManageUser user, IServiceProvider context = null)
{
    using var span = DefaultTracer.Instance?.NewSpan(nameof(SetCurrent), user);

    var ctx = Context?.HttpContext;
    if (ctx == null) return;

    ctx.Items["CurrentUser"] = user;

    var session = ctx.Items["Session"] as IDictionary<String, Object>;
    if (session == null) return;

    var key = SessionKey;
    if (user == null)
    {
        // 注销时清空
        session.Remove(key);
        session.Remove("userId");
    }
    else
    {
        // 登录/切换时保存
        session[key] = user;
        session["userId"] = user.ID;
    }
}
```

### 注销

```csharp
public override void Logout()
{
    var context = Context?.HttpContext;
    var session = context?.Items["Session"] as IDictionary<String, Object>;
    session?.Clear();  // 全部清空

    base.Logout();
}
```

## 会话键值

| 键名 | 类型 | 说明 |
|------|------|------|
| `Admin` | IManageUser | 当前登录用户对象（默认 SessionKey） |
| `userId` | Int32 | 用户 ID（快速访问） |
| `Online` | UserOnline | 用户在线状态对象 |
| `Cube_OAuthId` | Int64 | OAuth 登录记录 ID |
| `.Cube.Session` | String | SessionId 标识（存于 Cookie） |

## 用户在线记录

`RunTimeMiddleware` 在每次请求中自动更新用户在线状态，存储在 `Session["Online"]` 中。

```csharp
var online = session == null || !session.ContainsKey("Online")
    ? new UserOnline()
    : session["Online"] as UserOnline;

if (set.EnableUserOnline == 2 ||
    (set.EnableUserOnline == 1 && user != null))
{
    var deviceId = WebHelper.FillDeviceId(ctx);
    var sessionId = deviceId;

    if (user == null)
        online = _userService.SetStatus(online, sessionId, deviceId,
            p, userAgent, ua, 0, WebHelper.GetUserByToken(ctx), ip);
    else
        online = _userService.SetWebStatus(online, sessionId, deviceId,
            p, userAgent, ua, user, ip);

    session["Online"] = online;
    ctx.Items["Cube_Online"] = online;
}
```

### 在线记录包含信息

| 信息 | 来源 | 说明 |
|------|------|------|
| 设备 ID | `WebHelper.FillDeviceId` | 浏览器指纹标识 |
| 请求路径 | 当前请求 URL | 用户正在访问的页面 |
| UserAgent | 请求头 | 浏览器和操作系统信息 |
| IP 地址 | 远程 IP | 用户的网络地址 |
| 用户信息 | Session 中的用户对象 | 已登录用户的身份 |

## 请求处理流程

完整的请求生命周期中会话的参与：

```
请求进入
    ↓
RunTimeMiddleware.Invoke()
    ↓
CreateSession(ctx) ─────→ 获取或生成 SessionId
    ↓                      ↓
    │              恢复/创建 Session 字典
    │                      ↓
    │              写入 ctx.Items["Session"]
    ↓
_next.Invoke(ctx) ─────→ 业务逻辑处理
    ↓                      ↓
    │              ManageProvider.User → 从 Session 读取用户
    │              权限过滤器 → 读取 Session["Admin"]
    │              业务代码 → 读写 Session 数据
    ↓
更新用户在线状态
    ↓
响应返回（SessionId 通过 Cookie 持久化）
```

## 配置参数

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `SessionTimeout` | Int32 | 0 | 会话超时时间（秒），0 表示仅依赖 Cookie 生命周期 |
| `EnableUserOnline` | Int32 | 2 | 用户在线记录：0=禁用，1=仅登录用户，2=所有访客 |
| `RefreshUserPeriod` | Int32 | 600 | 刷新用户信息周期（秒） |
| `TokenCookie` | Boolean | true | 是否在 Cookie 中存储令牌 |

### appsettings.json 配置示例

```json
{
  "CubeSetting": {
    "SessionTimeout": 0,
    "EnableUserOnline": 2,
    "RefreshUserPeriod": 600,
    "TokenCookie": true
  }
}
```

## 集成点

| 场景 | 操作 | 说明 |
|------|------|------|
| 用户登录 | `SetCurrent(user)` | 将用户对象保存到 Session |
| 委托代理 | `CheckAgent()` → `SetCurrent(principal)` | 切换为委托人身份 |
| 用户注销 | `Logout()` → `session.Clear()` | 清空全部会话数据 |
| 权限检查 | 读取 `Session["Admin"]` | 获取当前用户进行权限验证 |
| SSO/OAuth | Token 作为 SessionId | 统一的身份维持机制 |

## 分布式部署注意

由于 `SessionProvider` 基于内存存储，在多节点部署时需注意：

| 方案 | 说明 |
|------|------|
| 粘性会话 | 负载均衡配置 Session 亲和（推荐简单场景） |
| 共享 Token | WebAPI 模式下，Token 本身包含身份信息，无需共享 Session |
| Redis Session | 启用 ASP.NET Core 的分布式 Session，魔方会同步使用 |

## 最佳实践

1. **WebAPI 模式**：使用 Token 认证，Session 自动以 Token 为 ID，天然支持多节点
2. **MVC 模式**：Cookie 方式维持会话，单节点或配置粘性会话即可
3. **在线监控**：设置 `EnableUserOnline=2` 监控所有访客行为，`EnableUserOnline=1` 仅监控登录用户
4. **性能考虑**：`RefreshUserPeriod` 控制用户信息刷新频率，避免每次请求都查询数据库
5. **安全注销**：注销时 `session.Clear()` 清空全部数据，确保不残留敏感信息

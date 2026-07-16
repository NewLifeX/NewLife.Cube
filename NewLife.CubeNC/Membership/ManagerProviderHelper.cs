using System.Security.Principal;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Net.Http.Headers;
using NewLife.Common;
using NewLife.Cube.Entity;
using NewLife.Cube.Extensions;
using NewLife.Log;
using NewLife.Model;
using NewLife.Web;
using XCode.Membership;
using HttpContext = Microsoft.AspNetCore.Http.HttpContext;
using IServiceCollection = Microsoft.Extensions.DependencyInjection.IServiceCollection;
using JwtBuilder = NewLife.Web.JwtBuilder;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace NewLife.Cube;

/// <summary>管理提供者助手</summary>
public static class ManagerProviderHelper
{
    /// <summary>设置当前用户</summary>
    /// <param name="provider">提供者</param>
    /// <param name="context">Http上下文，兼容NetCore</param>
    public static void SetPrincipal(this IManageProvider provider, IServiceProvider context = null)
    {
        var ctx = ModelExtension.GetService<IHttpContextAccessor>(context)?.HttpContext;
        if (ctx == null) return;

        var user = provider.GetCurrent(context);
        if (user == null) return;

        if (user is not IIdentity id || ctx.User?.Identity == id) return;

        // 角色列表
        var roles = new List<String>();
        if (user is IUser user2) roles.AddRange(user2.Roles.Select(e => e + ""));

        var up = new GenericPrincipal(id, roles.ToArray());
        ctx.User = up;
        Thread.CurrentPrincipal = up;
    }

    /// <summary>尝试登录。如果Session未登录则借助Token，分别从Header/Query/Cookie获取令牌</summary>
    /// <param name="provider">提供者</param>
    /// <param name="context">Http上下文，兼容NetCore</param>
    public static IManageUser TryLogin(this IManageProvider provider, HttpContext context)
    {
        ISpan span = null;
        try
        {
            var serviceProvider = context?.RequestServices;

            // 判断当前登录用户
            var user = provider.GetCurrent(serviceProvider);
            if (user == null)
            {
                span = DefaultTracer.Instance?.NewSpan(nameof(TryLogin));

                // 尝试从Token登录
                var token = context.LoadToken();
                var (u, jwt) = provider.LoadUser(token);
                if ((user = u) != null)
                {
                    provider.SetCurrent(user, serviceProvider);

                    // 滑动刷新：JWT 剩余有效期低于阈值时自动续期并写入 Cookie
                    if (jwt != null)
                    {
                        var set = CubeSetting.Current;
                        var threshold = set.TokenRefreshThreshold;
                        if (threshold > 0)
                        {
                            var remaining = jwt.Expire - DateTime.Now;
                            if (remaining.TotalSeconds < threshold)
                            {
                                // 仅当令牌来自 Cookie 时才刷新，避免 API 请求误写 Cookie
                                var fromCookie = false;
                                if (set.TokenCookie && context?.Request != null)
                                {
                                    var key = $"token-{SysConfig.Current.Name}";
                                    fromCookie = context.Request.Cookies[key] == token;
                                }
                                if (fromCookie)
                                {
                                    // 与新令牌有效期一致：SessionTimeout > 0 用配置，否则用 IssueToken 兜底 2h
                                    var expire = set.SessionTimeout > 0 ?
                                        TimeSpan.FromSeconds(set.SessionTimeout) :
                                        TimeSpan.FromHours(2);
                                    provider.SaveCookie(user, expire, context);

                                    XTrace.WriteLine("滑动刷新：用户[{0}]令牌有效期剩余[{1}]秒，已刷新续期至[{2}]秒", user, remaining.TotalSeconds.ToInt(), expire.TotalSeconds.ToInt());
                                }
                            }
                        }
                    }

#if MVC
                    // 保存登录信息。如果是json请求，不用记录自动登录
                    var req = context?.Request;
                    if (user is IAuthUser mu && !req.IsAjaxRequest())
                    {
                        mu.SaveLogin(null);

                        LogProvider.Provider.WriteLog("用户", "自动登录", true, $"{user} IssuedAt={jwt.IssuedAt.ToFullString()} Expire={jwt.Expire.ToFullString()}", user.ID, user + "", ip: context.GetUserHost());
                    }
#endif
                }
            }

            // 如果Null直接返回
            if (user == null) return null;

            // 设置前端当前用户
            provider.SetPrincipal(serviceProvider);

            // 处理租户相关信息
            context.ChooseTenant(user.ID);

            return user;
        }
        finally
        {
            span?.Dispose();
        }
    }

    /// <summary>从令牌加载用户</summary>
    /// <param name="provider"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static (IManageUser, JwtBuilder) LoadUser(this IManageProvider provider, String token)
    {
        if (token.IsNullOrEmpty()) return (null, null);

        using var span = DefaultTracer.Instance?.NewSpan(nameof(LoadUser), token);

        //token = token.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
        //span?.AppendTag(token);

        var jwt = GetJwt();
        if (!jwt.TryDecode(token, out var msg))
        {
            span?.AppendTag($"令牌无效：{msg}");
            XTrace.WriteLine("令牌无效：{0}, token={1}", msg, token);

            return (null, jwt);
        }

        var user = jwt.Subject;
        if (user.IsNullOrEmpty()) return (null, jwt);
        span?.AppendTag($"账号：{user}");

        // 判断有效期
        if (jwt.Expire < DateTime.Now)
        {
            span?.AppendTag($"令牌过期：{jwt.Expire.ToFullString()}");
            XTrace.WriteLine("令牌过期：{0} {1}", jwt.Expire, token);

            return (null, jwt);
        }

        // 通过 jti 检查 UserToken 是否已被吊销
        if (jwt.Id.IsNullOrEmpty())
        {
            // 旧令牌可能不含 jti，写日志辅助排查，但仍放行（灰度期）
            span?.AppendTag("令牌缺少 jti（可能是旧令牌，建议重新登录）");
        }
        else
        {
            var utId = jwt.Id.ToInt();
            if (utId > 0)
            {
                var ut = UserToken.FindByID(utId);
                if (ut == null || !ut.Enable)
                {
                    span?.AppendTag($"UserToken 已吊销：{utId}");
                    XTrace.WriteLine("UserToken 已吊销：{0}", utId);
                    return (null, jwt);
                }
            }
        }

        var u = provider.FindByName(user);
        if (u == null || !u.Enable) return (null, jwt);
        span?.AppendTag($"用户：{u}");

        return (u, jwt);
    }

    /// <summary>设置租户</summary>
    /// <param name="context"></param>
    /// <param name="userId"></param>
    public static void ChooseTenant(this HttpContext context, Int32 userId)
    {
        var set = CubeSetting.Current;
        if (!set.EnableTenant) return;

        using var span = DefaultTracer.Instance?.NewSpan(nameof(ChooseTenant), new { userId });

        /*
         * 用户登录后的租户选择逻辑：
         *  已选租户且有效
         *      直接进入已选租户
         *  已选管理后台
         *      直接接入管理后台
         *  未选租户
         *      拥有租户
         *          进入第一个租户
         *      未有租户
         *          进入管理后台
         */
        var tlist = TenantUser.FindAllByUserId(userId).Where(e => e.Enable).ToList();
        var tenantId = GetTenantId(context);

        // 进入最后一次使用的租户
        if (tenantId > 0 && tlist.Any(e => e.TenantId == tenantId))
            SetTenant(context, tenantId); // 有效租户
        else if (tenantId == 0)
            SetTenant(context, 0); // 管理后台
        else
        {
            // 如果 tenantId > 0 但无效，则重新选择租户
            if (tlist.Count > 0)
                SaveTenant(context, tlist[0].TenantId); // 进入第一个租户
            else
                SaveTenant(context, 0); // 进入管理后台
        }

        CheckTenantRole();
    }

    private static Int32 _checkRole;
    /// <summary>检查并添加租户管理员角色</summary>
    private static void CheckTenantRole()
    {
        if (_checkRole > 0 || Interlocked.CompareExchange(ref _checkRole, 1, 0) != 0) return;

        var role = Role.FindByName("租户管理员");
        if (role == null)
        {
            role = new Role
            {
                Name = "租户管理员",
                Enable = true,
            };
            role.Insert();
        }
    }

    /// <summary>设置租户</summary>
    /// <param name="context"></param>
    /// <param name="tenantId"></param>
    public static void SetTenant(this HttpContext context, Int32 tenantId)
    {
        DefaultSpan.Current?.AppendTag($"SetTenant: {tenantId}");

        TenantContext.Current = new TenantContext { TenantId = tenantId };
        //ManageProvider.Provider.Tenant = Tenant.FindById(tenantId);
    }

    /// <summary>从当前请求的 JWT 中提取 jti（UserToken.Id），用于多设备模式精确吊销</summary>
    /// <param name="context">HTTP 上下文</param>
    /// <returns>UserToken.Id，未找到返回 0</returns>
    public static Int32 GetJti(this HttpContext context)
    {
        var token = context.LoadToken();
        if (token.IsNullOrEmpty()) return 0;

        try
        {
            var set = CubeSetting.Current;
            var ss = set.JwtSecret.Split(':');

            var jwt = new JwtBuilder
            {
                Algorithm = ss[0],
                Secret = ss[1],
            };

            if (jwt.TryDecode(token, out _) && !jwt.Id.IsNullOrEmpty())
                return jwt.Id.ToInt();
        }
        catch
        {
            // 解码失败不影响主流程
        }

        return 0;
    }

    /// <summary>生成令牌</summary>
    /// <returns></returns>
    public static JwtBuilder GetJwt()
    {
        var set = CubeSetting.Current;

        // 生成令牌
        var ss = set.JwtSecret?.Split(':');
        if (ss == null || ss.Length < 2) throw new InvalidOperationException("未设置JWT算法和密钥");

        var jwt = new JwtBuilder
        {
            Algorithm = ss[0],
            Secret = ss[1],
        };

        return jwt;
    }

    #region 用户Cookie
    /// <summary>从上下文加载令牌</summary>
    /// <param name="context">Http上下文，兼容NetCore</param>
    /// <returns></returns>
    public static String LoadToken(this HttpContext context)
    {
        //using var span = DefaultTracer.Instance?.NewSpan(nameof(LoadToken));

        var req = context?.Request;
        var token = "";

        // 尝试从头部获取token
        if (token.IsNullOrEmpty() || token.Split(".").Length != 3)
            token = req?.Headers[HeaderNames.Authorization].ToString().TrimPrefix("Bearer ");
        if (token.IsNullOrEmpty() || token.Split(".").Length != 3)
            token = req?.Headers["X-Token"].ToString().TrimPrefix("Bearer ");

        // 尝试从url中获取token
        if (token.IsNullOrEmpty() || token.Split(".").Length != 3) token = req?.Query["token"];
        if (token.IsNullOrEmpty() || token.Split(".").Length != 3) token = req?.Query["jwtToken"];

        // 尝试从Cookie获取token
        if (CubeSetting.Current.TokenCookie)
        {
            var key = $"token-{SysConfig.Current.Name}";
            if (token.IsNullOrEmpty() || token.Split(".").Length != 3) token = req?.Cookies[key];
        }

        if (token.IsNullOrEmpty() || token.Split(".").Length != 3) return null;

        return token;
    }

    /// <summary>给用户颁发令牌</summary>
    /// <param name="context"></param>
    /// <param name="user"></param>
    /// <param name="expire"></param>
    /// <returns></returns>
    public static String IssueToken(this HttpContext context, IManageUser user, TimeSpan expire)
    {
        var token = context.Items["jwtToken"] as String;
        if (!token.IsNullOrEmpty()) return token;

        if (user != null)
        {
            // 令牌有效期，默认2小时
            var exp = DateTime.Now.Add(expire.TotalSeconds > 0 ? expire : TimeSpan.FromHours(2));
            var jwt = GetJwt();
            jwt.Subject = user.Name;
            jwt.Expire = exp;

            token = jwt.Encode(null);
        }

        context.Items["jwtToken"] = token;

        return token;
    }

    /// <summary>统一登录令牌颁发。创建 UserToken 记录并嵌入 JWT 的 jti，所有认证路径（密码/验证码/SSO/MFA）均走此方法</summary>
    /// <remarks>
    /// 流程：① CreateRefreshToken → ② UserToken.Insert（获取自增 Id）→ ③ JWT { sub, jti=ut.Id, exp }
    /// accessToken(jti) 与 refreshToken(Token字段) 共用同一条 UserToken 记录，Enable=false 时同时失效。
    /// </remarks>
    /// <param name="context">HTTP 上下文</param>
    /// <param name="user">登录用户</param>
    /// <param name="expire">accessToken 有效期（秒）</param>
    /// <returns>令牌模型</returns>
    public static TokenModel IssueLoginToken(this HttpContext context, IManageUser user, TimeSpan expire)
    {
        // 1. 创建刷新令牌（纯字符串，尚未入库）
        var refreshToken = CreateRefreshToken(user, DateTime.Now.AddDays(7));

        // 2. 先插入 UserToken 记录（获取自增 Id）
        UserToken ut = null;
        if (user != null)
        {
            ut = new UserToken
            {
                Token = refreshToken,
                UserID = user.ID,
                Enable = true,
                Expire = DateTime.Now.AddDays(7),
                CreateIP = context.GetUserHost(),
            };
            ut.Insert();
        }

        // 3. 再颁发 JWT，jti = UserToken.Id
        String accessToken = null;
        if (user != null)
        {
            var exp = DateTime.Now.Add(expire.TotalSeconds > 0 ? expire : TimeSpan.FromHours(2));
            var jwt = GetJwt();
            jwt.Subject = user.Name;
            jwt.Expire = exp;
            if (ut != null) jwt.Id = ut.ID.ToString();
            accessToken = jwt.Encode(null);
        }
        context.Items["jwtToken"] = accessToken;

        return new TokenModel
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpireIn = expire.TotalSeconds.ToInt()
        };
    }

    // IssueTokenAndRefreshToken 已迁移至 IssueLoginToken，旧名不再保留

    /// <summary>刷新令牌</summary>
    /// <param name="context"></param>
    /// <param name="user"></param>
    /// <param name="refresh_token"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static IToken RefreshToken(this HttpContext context, IManageUser user, String refresh_token)
    {
        var set = CubeSetting.Current;
        var result = DecodeRefreshToken(refresh_token, out var username, out var expire);
        if (!result) throw new Exception($"刷新令牌异常：{refresh_token}");

        // 吊销旧令牌，实现令牌旋转
        var oldUt = UserToken.FindByToken(refresh_token);
        if (oldUt != null)
        {
            oldUt.Enable = false;
            oldUt.Update();
        }

        return context.IssueLoginToken(user, TimeSpan.FromSeconds(set.TokenExpire));
    }

    /// <summary>创建刷新令牌</summary>
    /// <param name="user"></param>
    /// <param name="expire"></param>
    /// <returns></returns>
    private static String CreateRefreshToken(IManageUser user, DateTime expire)
    {
        var tokenProverder = new TokenProvider();
        var r = tokenProverder.ReadKey("refresh.prvkey", true);
        return tokenProverder.Encode(user.Name, expire);
    }

    /// <summary></summary>
    /// <param name="token"></param>
    /// <param name="username"></param>
    /// <param name="expire"></param>
    /// <returns></returns>
    private static Boolean DecodeRefreshToken(String token, out String username, out DateTime expire)
    {
        var tokenProverder = new TokenProvider();
        _ = tokenProverder.ReadKey("refresh.pubkey");
        return tokenProverder.TryDecode(token, out username, out expire);
    }

    /// <summary>验证令牌是否有效，同时校验 UserToken 未被吊销</summary>
    /// <param name="access_token"></param>
    /// <param name="manageProvider"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IManageUser Auth(String access_token, IManageProvider manageProvider)
    {
        if (access_token.IsNullOrEmpty()) throw new ArgumentNullException(nameof(access_token));

        var username = Decode(access_token, out var jwt);
        // 通过 jti 检查 UserToken 是否已被吊销
        if (jwt != null && !jwt.Id.IsNullOrEmpty())
        {
            var utId = jwt.Id.ToInt();
            if (utId > 0)
            {
                var ut = UserToken.FindByID(utId);
                if (ut == null || !ut.Enable)
                {
                    XTrace.WriteLine("UserToken 已吊销：{0}", utId);
                    return null;
                }
            }
        }

        // 设置登录用户
        var user = manageProvider.FindByName(username);
        if (user == null) XTrace.WriteLine($"未查询到相关用户{username}");

        return user;
    }

    /// <summary>解码令牌</summary>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static String Decode(String token) => Decode(token, out _);

    /// <summary>解码令牌，同时返回 JwtBuilder 用于读取 jti 等声明</summary>
    /// <param name="token">JWT 字符串</param>
    /// <param name="jwt">解码后的 JWT 构建器</param>
    /// <returns>令牌主体（Subject）</returns>
    /// <exception cref="Exception">令牌无效或格式错误</exception>
    private static String Decode(String token, out JwtBuilder jwt)
    {
        // 区分访问令牌和内部刷新令牌
        var ts = token.Split('.');
        if (ts.Length == 3)
        {
            // 从配置加载密钥
            var set = CubeSetting.Current;
            var ss = set.JwtSecret.Split(':');

            jwt = new JwtBuilder
            {
                Algorithm = ss[0],
                Secret = ss[1],
            };

            if (!jwt.TryDecode(token, out var msg))
            {
                XTrace.WriteLine("令牌无效：{0}, token={1}", msg, token);
                throw new Exception(msg);
            }

            return jwt.Subject;
        }

        jwt = null;
        XTrace.WriteLine("令牌格式错误：{0}", token);
        throw new Exception("非法访问令牌");
    }

    /// <summary>保存用户信息到Cookie</summary>
    /// <remarks>
    /// 优先使用 context.Items["jwtToken"]（由 IssueLoginToken 设置），确保 Cookie 中的 JWT 也包含 jti。
    /// 若无则回退到 IssueToken（兼容旧调用方）。
    /// </remarks>
    /// <param name="provider">提供者</param>
    /// <param name="user">用户</param>
    /// <param name="expire">过期时间</param>
    /// <param name="context">Http上下文，兼容NetCore</param>
    public static void SaveCookie(this IManageProvider provider, IManageUser user, TimeSpan expire, HttpContext context)
    {
        var set = CubeSetting.Current;
        if (!set.TokenCookie) return;

        using var span = DefaultTracer.Instance?.NewSpan(nameof(SaveCookie), new { user?.Name, expire });

        var res = context?.Response;
        if (res == null) return;

        //var set = CubeSetting.Current;
        var option = new CookieOptions
        {
            SameSite = SameSiteMode.Unspecified,
        };
        // https时，SameSite使用None，此时可以让cookie写入有最好的兼容性，跨域也可以读取
        if (context.Request.GetRawUrl().Scheme.EqualIgnoreCase("https"))
        {
            //if (!set.CookieDomain.IsNullOrEmpty()) option.Domain = set.CookieDomain;
            option.SameSite = SameSiteMode.None;
            option.Secure = true;
        }

        var token = "";
        if (user != null)
        {
            // 优先使用已有 JWT（由 IssueLoginToken 预先颁发，含 jti）
            token = context.Items["jwtToken"] as String;
            if (token.IsNullOrEmpty())
                token = context.IssueToken(user, expire);

            if (expire.TotalSeconds > 0) option.Expires = DateTimeOffset.Now.Add(expire);
        }
        else
        {
            option.Expires = DateTimeOffset.MinValue;
        }

        var key = $"token-{SysConfig.Current.Name}";
        res.Cookies.Append(key, token, option);
    }
    #endregion

    #region 租户Cookie
    /// <summary>改变选中的租户</summary>
    /// <param name="context"></param>
    /// <param name="tenantId">0管理后台场景，大于0租户场景</param>
    public static void SaveTenant(this HttpContext context, Int32 tenantId)
    {
        var res = context?.Response;
        if (res == null) return;

        DefaultSpan.Current?.AppendTag($"SaveTenant: {tenantId}");

        var option = new CookieOptions
        {
            SameSite = SameSiteMode.Unspecified,
        };
        // https时，SameSite使用None，此时可以让cookie写入有最好的兼容性，跨域也可以读取
        if (context.Request.GetRawUrl().Scheme.EqualIgnoreCase("https"))
        {
            option.SameSite = SameSiteMode.None;
            option.Secure = true;
        }

        if (tenantId < 0) option.Expires = DateTimeOffset.MinValue;

        var key = $"TenantId-{SysConfig.Current.Name}";
        res.Cookies.Append(key, tenantId + "", option);

        SetTenant(context, tenantId);
    }

    /// <summary>获取cookie中tenantId</summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static Int32 GetTenantId(this HttpContext context)
    {
        var req = context?.Request;
        if (req == null) return -1;

        // Header优先，兼容前后端分离/小程序等不稳定Cookie场景
        var tenant = req.Headers["X-Tenant-Id"].ToString();
        //if (tenant.IsNullOrEmpty()) tenant = req.Headers["X-Tenant"].ToString();
        var id = ResolveTenantId(tenant);
        if (id >= 0) return id;

        // QueryString兜底（一般用于调试/回调等），优先级低于Header/Cookie
        tenant = req.Query["tenantId"].ToString();
        //if (tenant.IsNullOrEmpty()) tenant = req.Query["tenant"].ToString();
        id = ResolveTenantId(tenant);
        if (id >= 0) return id;

        // 最后从Cookie读取（兼容现有浏览器模式）
        var key = $"TenantId-{SysConfig.Current.Name}";
        return req.Cookies[key].ToInt(-1);
    }

    private static Int32 ResolveTenantId(String tenant)
    {
        if (tenant.IsNullOrEmpty()) return -1;

        // 允许传 0（管理后台）
        var id = tenant.ToInt(-1);
        if (id >= 0) return id;

        // 允许传租户名称
        var t = Tenant.FindByCode(tenant);
        return t != null ? t.Id : -1;
    }
    #endregion

    /// <summary>
    /// 添加管理提供者
    /// </summary>
    /// <param name="service"></param>
    public static void AddManageProvider(this IServiceCollection service)
    {
        service.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        service.TryAddSingleton<IManageProvider, ManageProvider2>();
    }

    /// <summary>
    /// 使用管理提供者
    /// </summary>
    /// <param name="app"></param>
    public static void UseManagerProvider(this IApplicationBuilder app)
    {
        XTrace.WriteLine("初始化ManageProvider");

        var provider = app.ApplicationServices;
        ManageProvider.Provider = ModelExtension.GetService<IManageProvider>(provider);
        //ManageProvider2.EndpointRoute = (IEndpointRouteBuilder)app.Properties["__EndpointRouteBuilder"];
        ManageProvider2.Context = ModelExtension.GetService<IHttpContextAccessor>(provider);

        //// 初始化数据库
        ////_ = Role.Meta.Count;
        //EntityFactory.InitConnection("Membership");
        //EntityFactory.InitConnection("Log");
        //EntityFactory.InitConnection("Cube");
    }
}

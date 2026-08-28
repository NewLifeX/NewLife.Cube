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
                    // 分享短令牌 / Url 锁定令牌：限制可访问路径，防止横向越权
                    var shareUt = UserToken.FindByToken(token);
                    if (shareUt == null && jwt != null && !jwt.Id.IsNullOrEmpty())
                        shareUt = UserToken.FindByID(jwt.Id.ToInt());
                    if (shareUt != null && !(shareUt.Url + "").IsNullOrEmpty()
                        && !IsShareRequestAllowed(context, shareUt))
                    {
                        span?.AppendTag($"分享令牌路径拒绝：{context.Request.Path}");
                        provider.SetCurrent(null, serviceProvider);
                        return null;
                    }

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

            // 认证层租户校验（fail-closed）：多租户开启时，已认证用户必须处于有效租户上下文，否则拒绝访问。
            // 校验失败时清除当前用户，避免 token 路径已 SetCurrent 的用户残留 session，导致下次请求跳过校验
            if (!context.ValidateTenant(user))
            {
                XTrace.WriteLine("租户校验失败，拒绝访问：用户[{0}]没有可用的有效租户", user);
                provider.SetCurrent(null, serviceProvider);
                return null;
            }

            // 设置前端当前用户
            provider.SetPrincipal(serviceProvider);

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
            // 回退：分享短令牌（UserToken.Token，非 JWT）
            var shareUser = TryLoadShareUserToken(token, span);
            if (shareUser != null) return (shareUser, null);

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

    /// <summary>从分享短令牌加载用户（Url 锁定的 UserToken.Token）</summary>
    static IManageUser TryLoadShareUserToken(String token, ISpan span)
    {
        if (token.IsNullOrEmpty() || token.Contains('.')) return null;
        var ut = UserToken.FindByToken(token);
        if (ut == null || !ut.Enable) return null;
        if ((ut.Url + "").StartsWithIgnoreCase("attachment:")) return null;
        if (ut.Expire.Year > 2000 && ut.Expire < DateTime.Now) return null;
        var user = ut.User as IManageUser;
        if (user == null || !user.Enable) return null;
        span?.AppendTag($"分享令牌用户：{user}");
        return user;
    }

    /// <summary>分享令牌 Url 锁定：仅允许目标实体路径及仪表盘/视图配置相关服务接口</summary>
    public static Boolean IsShareRequestAllowed(HttpContext context, UserToken ut)
    {
        if (ut == null) return false;
        var utUrl = ut.Url + "";
        if (utUrl.IsNullOrEmpty()) return true;
        if (utUrl.StartsWithIgnoreCase("attachment:")) return false;

        var tokenPath = utUrl.Split('?')[0].TrimEnd('/');
        if (tokenPath.IsNullOrEmpty()) return true;
        var req = context?.Request?.Path.Value + "";
        if (req.IsNullOrEmpty()) return false;

        if (req.StartsWithIgnoreCase("/api" + tokenPath) || req.StartsWithIgnoreCase(tokenPath))
            return true;

        // 分享页所需：Widget 查询、视图配置、菜单注册、登录态探测、当前用户 Info
        if (req.StartsWithIgnoreCase("/Cube/Widget") ||
            req.StartsWithIgnoreCase("/Cube/ViewProfile") ||
            req.StartsWithIgnoreCase("/Cube/ViewProfileTemplate") ||
            req.StartsWithIgnoreCase("/Cube/UserProfile") ||
            req.StartsWithIgnoreCase("/Cube/MenuTree") ||
            req.StartsWithIgnoreCase("/Cube/GetAiConfig") ||
            req.StartsWithIgnoreCase("/Cube/GetLoginConfig") ||
            req.StartsWithIgnoreCase("/Auth/") ||
            req.EndsWithIgnoreCase("/User/Info") ||
            req.EndsWithIgnoreCase("/User/GetLoginConfig"))
            return true;

        return false;
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
         *  已选管理后台（租户0）
         *      系统管理员直接进入管理后台
         *      普通用户强制进入第一个有效租户，无租户则不设置（防止越权看到全部数据）
         *  未选租户
         *      拥有租户
         *          进入第一个租户
         *      未有租户
         *          系统管理员进入管理后台，普通用户不设置
         */
        var tlist = TenantUser.FindAllByUserId(userId).Where(e => e.Enable).ToList();
        var tenantId = GetTenantId(context);

        // 判断是否系统管理员，管理后台（租户0）仅系统管理员可进入
        var user = User.FindByID(userId);
        var isAdmin = user != null && user.Roles.Any(e => e.IsSystem);

        // 进入最后一次使用的租户
        if (tenantId.GetTenantMode() == TenantMode.Tenant && tlist.Any(e => e.TenantId == tenantId))
            SetTenant(context, tenantId); // 有效租户
        else if (tenantId.GetTenantMode() == TenantMode.AdminBackend)
        {
            // 管理后台仅系统管理员可进入；普通用户请求管理后台时强制进入第一个有效租户
            if (isAdmin)
                SetTenant(context, 0); // 系统管理员进入管理后台
            else if (tlist.Count > 0)
                SetTenant(context, tlist[0].TenantId); // 普通用户进入第一个租户
        }
        else
        {
            // 如果 tenantId > 0 但无效，则重新选择租户
            if (tlist.Count > 0)
                SetTenant(context, tlist[0].TenantId); // 进入第一个租户
            else if (isAdmin)
                SetTenant(context, 0); // 系统管理员进入管理后台
            // 普通用户无有效租户，不设置租户上下文，防止进入管理后台越权
        }

        CheckTenantRole();
    }

    /// <summary>校验租户合法性（认证层 fail-closed）。多租户开启时，已认证用户必须处于有效租户上下文，否则拒绝访问；未开启多租户不校验</summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="user">已认证用户</param>
    /// <returns>是否允许继续访问</returns>
    public static Boolean ValidateTenant(this HttpContext context, IManageUser user)
    {
        var set = CubeSetting.Current;
        // 未开启多租户，不校验租户
        if (!set.EnableTenant) return true;

        if (user == null) return false;

        // 显式标识优先校验（2026-08-16）：请求显式携带租户标识（X-Tenant/X-App-Id/Query/Cookie）时，
        // 按所带租户校验成员——管理员豁免但上下文用所带租户（可进任意租户，数据按所带租户过滤，不落管理后台看全量）；
        // 普通用户必须是所带租户成员，非成员拒绝（403）。杜绝"请求 t2 被静默换 t1"（原 ChooseTenant 回落吞掉显式标识）。
        var resolution = context.ResolveTenant();
        if (resolution.HasIdentifier || context.HasExplicitTenantHeader())
        {
            // 显式但解析无效（X-Tenant 不存在/已禁用、X-App-Id 未配置等）→ 拒绝。
            // 生产路径中 X-Tenant/X-Tenant-Id/Query/Cookie 无效由 DataScopeMiddleware 先以 400 拦截，
            // 此处兜底 X-App-Id 未配置等中间件不解析的场景，及绕过中间件的直接调用。
            if (!resolution.IsValid)
            {
                // [TenantCompat] 影子期：X-App-Id 应用已配置但未设置租户（存量小程序过渡态）→ 兼容放行。
                // 应用未配置（真无效）或租户已禁用仍拒绝；切 Enforce 并稳定后移除该兼容分支（见评审方案 5.5）。
                var appId = context.Request.Headers["X-App-Id"].ToString();
                if (!appId.IsNullOrEmpty() && IsAppIdConfiguredWithoutTenant(appId) && set.TenantEnforceMode == TenantEnforceModes.Shadow)
                {
                    context.ChooseTenant(user.ID); // 已绑定回落第一个租户；未绑定不设上下文（防看全量）
                    XTrace.WriteLine("[TenantCompat] 应用[{0}]已配置但未设置租户，影子期兼容放行：{1} 用户[{2}]", appId, context.Request.Path, user);
                    // 数据日志（CreateLog 落库，管理后台审计日志可查）
                    WriteTenantCompatDataLog("影子兼容放行", $"应用[{appId}]已配置但未设置租户，影子期兼容放行 用户[{user}]", user, context.GetUserHost());
                    return true;
                }

                XTrace.WriteLine("租户校验失败：显式租户标识无效，拒绝访问：用户[{0}] resolution={1}", user, resolution.TenantId);
                return false;
            }

            // 系统管理员豁免成员校验，但上下文用所带租户
            if (user is IUser su && su.Roles.Any(e => e.IsSystem))
            {
                context.SetTenant(resolution.TenantId);
                return true;
            }

            // 普通用户：必须是所带租户的成员（管理后台 tenantId=0 由 IsMember 拒绝，普通用户进不了管理后台）
            if (!TenantAccessPolicy.IsMember(resolution.TenantId, user))
            {
                XTrace.WriteLine("租户校验失败：用户[{0}]不是租户[{1}]成员，拒绝访问", user, resolution.TenantId);
                return false;
            }

            context.SetTenant(resolution.TenantId);
            return true;
        }

        // 无租户标识：
        // [TenantCompat] 影子期规则A：旧客户端无租户标识时跳过 fail-closed 直接放行，仅记录影子日志。
        // 已绑定用户仍需 ChooseTenant 回落到第一个绑定租户（方案 5.2），避免看到未过滤数据。
        // 切 Enforce 并稳定后移除该兼容分支（见评审方案 5.5）
        if (set.TenantEnforceMode == TenantEnforceModes.Shadow)
        {
            context.ChooseTenant(user.ID);
            XTrace.WriteLine("[TenantCompat] 旧客户端无租户标识，兼容放行：{0} 用户[{1}]", context.Request.Path, user);
            WriteTenantCompatDataLog("影子兼容放行", $"旧客户端无租户标识，兼容放行 路径[{context.Request.Path}] 用户[{user}]", user, context.GetUserHost());
            return true;
        }

        // Enforce + 无租户标识：管理员本身不属任何租户、无需租户标识（直接进管理后台）；
        // 普通用户必须携带租户标识，否则拒绝。
        if (user is IUser au && au.Roles.Any(e => e.IsSystem))
        {
            context.SetTenant(0); // 管理员进管理后台，无需租户标识
            return true;
        }
        return false; // 普通用户缺租户标识，拒绝
    }

    /// <summary>记录影子期兼容放行的数据日志（CreateLog 落库，管理后台审计日志可查），与 [TenantCompat] 影子日志配套，供观察期统计存量无租户请求。
    /// 注：影子日志在观察期为高频路径（每个无标识请求一条），落库会持续产生数据，切 Enforce 或移除兼容分支时应一并评估</summary>
    /// <param name="scene">场景动作名</param>
    /// <param name="remark">详情</param>
    /// <param name="user">当前用户（无租户上下文的中间件/数据层场景可为 null）</param>
    /// <param name="ip">客户端IP</param>
    public static void WriteTenantCompatDataLog(String scene, String remark, IManageUser user, String ip)
    {
        try
        {
            var log = LogProvider.Provider.CreateLog("租户", scene, true, remark, user?.ID ?? 0, user + "", ip);
            log?.SaveAsync();
        }
        catch (Exception ex)
        {
            // 影子日志不应影响主流程，落库失败仅记跟踪日志
            XTrace.WriteLine("影子数据日志写入失败：{0}", ex.Message);
        }
    }

    /// <summary>是否显式提供了租户信息（请求头/查询参数/Cookie 非空即算，不校验有效性）。用于认证层区分"显式但无效"与"完全无标识"</summary>
    private static Boolean HasExplicitTenantHeader(this HttpContext context)
    {
        var req = context?.Request;
        if (req == null) return false;

        return !req.Headers["X-Tenant"].ToString().IsNullOrEmpty()
            || !req.Headers["X-Tenant-Id"].ToString().IsNullOrEmpty()
            || !req.Headers["X-App-Id"].ToString().IsNullOrEmpty()
            || !req.Query["tenantId"].ToString().IsNullOrEmpty()
            || !req.Cookies[$"TenantId-{SysConfig.Current.Name}"].IsNullOrEmpty();
    }

    /// <summary>解析 X-App-Id 到租户ID（OAuth 配置租户，校验存在且启用）。无 appId 或无效返回 -1</summary>
    private static Int32 ResolveTenantByAppId(String appId)
    {
        if (appId.IsNullOrEmpty()) return -1;
        var config = OAuthConfig.FindByAppId(appId);
        return config != null && IsValidTenant(config.TenantId) ? config.TenantId : -1;
    }

    /// <summary>X-App-Id 对应的应用（OAuthConfig）已配置但未设置租户（TenantId&le;0）。存量小程序过渡态，供影子期兼容放行判断。应用未配置返回 false</summary>
    /// <param name="appId">应用标识</param>
    /// <returns>应用存在且租户未设置返回 true</returns>
    private static Boolean IsAppIdConfiguredWithoutTenant(String appId)
    {
        if (appId.IsNullOrEmpty()) return false;
        var config = OAuthConfig.FindByAppId(appId);
        return config != null && config.TenantId <= 0;
    }

    /// <summary>单一租户解析入口：优先 X-App-Id（OAuth 配置租户），其次 X-Tenant/Query/Cookie，返回结构化结果</summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>租户解析结果</returns>
    public static TenantResolution ResolveTenant(this HttpContext context)
    {
        var req = context?.Request;
        if (req == null) return default;

        var appId = req.Headers["X-App-Id"] + "";
        if (!appId.IsNullOrEmpty())
        {
            var tid = ResolveTenantByAppId(appId);
            return new TenantResolution { HasIdentifier = true, TenantId = tid };
        }

        var id = context.GetTenantId();
        return new TenantResolution { HasIdentifier = id >= 0, TenantId = id };
    }

    /// <summary>解析登录/绑定租户：优先已建立的租户上下文，其次单一解析入口（X-App-Id / X-Tenant/Query/Cookie）。返回 -1 表示未解析到有效租户</summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>租户ID，未解析到返回 -1</returns>
    public static Int32 ResolveTenantForLogin(this HttpContext context)
    {
        if (context == null) return -1;

        // 已建立的租户上下文优先
        if (TenantContext.Current.GetTenantMode() == TenantMode.Tenant) return TenantContext.CurrentId;

        return context.ResolveTenant().TenantId;
    }

    /// <summary>校验租户ID是否存在且启用。0（管理后台）视为无效租户；未开启多租户时仅要求大于0</summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>有效返回true</returns>
    private static Boolean IsValidTenant(Int32 tenantId)
    {
        if (tenantId <= 0) return false;
        if (!CubeSetting.Current.EnableTenant) return true;

        var t = Tenant.FindById(tenantId);
        return t != null && t.Enable;
    }

    /// <summary>获取租户上下文模式（用枚举消灭 0/null 魔法数字，P1-5）</summary>
    /// <param name="tc">租户上下文</param>
    /// <returns>租户模式</returns>
    public static TenantMode GetTenantMode(this TenantContext tc)
        => tc == null ? TenantMode.None : tc.TenantId > 0 ? TenantMode.Tenant : tc.TenantId == 0 ? TenantMode.AdminBackend : TenantMode.None;

    /// <summary>获取租户ID对应模式。&gt;0=租户，0=管理后台，负=未提供</summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>租户模式</returns>
    public static TenantMode GetTenantMode(this Int32 tenantId)
        => tenantId > 0 ? TenantMode.Tenant : tenantId == 0 ? TenantMode.AdminBackend : TenantMode.None;

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

    /// <summary>解析注册租户。优先X-App-Id（参考SSO登录按AppId查找OAuth配置取租户），其次X-Tenant（租户编码，与GetTenantId语义一致）。
    /// Shadow 期（默认）：无租户标识的注册与登录规则A一致，兼容放行不绑定，仅记日志；Enforce 才强制要求有效租户。未开启多租户不解析</summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>错误信息，null表示成功</returns>
    public static String ResolveRegisterTenant(this HttpContext context)
    {
        var req = context?.Request;
        if (req == null) return null;

        var set = CubeSetting.Current;

        // 未开启多租户时不解析租户，保持原有注册逻辑，不设置租户上下文
        if (!set.EnableTenant) return null;

        // 方式1：X-App-Id，参考SSO登录逻辑，按AppId查找OAuth配置，取配置所属租户
        var appId = req.Headers["X-App-Id"] + "";
        if (!appId.IsNullOrEmpty())
        {
            var config = OAuthConfig.FindByAppId(appId);
            if (config == null)
                return $"应用{nameof(OAuthConfig.AppId)}未配置";

            // 应用已配置但租户未设置（存量小程序过渡态）：与访问路径 ValidateTenant 一致——
            // Shadow 兼容放行（不设租户上下文，避免误落管理后台）；Enforce 拒绝。切 Enforce 前须为应用设置租户。
            if (config.TenantId <= 0)
            {
                if (set.TenantEnforceMode == TenantEnforceModes.Shadow)
                {
                    XTrace.WriteLine("[TenantCompat] 应用[{0}]已配置但未设置租户，注册影子期兼容放行：{1}", appId, context.Request.Path);
                    WriteTenantCompatDataLog("影子兼容放行", $"应用[{appId}]已配置但未设置租户，注册影子期兼容放行 路径[{context.Request.Path}]", null, context.GetUserHost());
                    return null;
                }
                return $"应用[{appId}]未配置租户，无法注册";
            }

            // 校验配置所属租户有效
            var tenant = Tenant.FindById(config.TenantId);
            if (tenant == null || !tenant.Enable)
                return $"租户[{config.TenantId}]不存在或已禁用";

            context.SetTenant(config.TenantId);
            return null;
        }

        // 方式2：X-Tenant 租户编码（Code），与GetTenantId语义一致，内部已校验存在且启用
        var tenantStr = req.Headers["X-Tenant"] + "";
        if (!tenantStr.IsNullOrEmpty())
        {
            var tenantId = ResolveTenantByCode(tenantStr);
            if (tenantId < 0)
                return $"租户[{tenantStr}]不存在或已禁用";

            context.SetTenant(tenantId);
            return null;
        }

        // 方式3：回退到已建立的租户上下文（Cookie/中间件）。兼容 MVC 表单注册等无法携带自定义请求头的场景，
        // 此时租户已由 GetTenantId（Cookie）解析并校验过有效性，直接沿用
        if (TenantContext.Current.GetTenantMode() == TenantMode.Tenant) return null;

        // [TenantCompat] Shadow 期规则A：旧客户端无租户标识注册，与登录一致兼容放行（不绑定），仅记录影子日志。
        // 切 Enforce 并稳定后移除该兼容分支（见评审方案 5.5）
        if (set.TenantEnforceMode == TenantEnforceModes.Shadow)
        {
            XTrace.WriteLine("[TenantCompat] 旧客户端无租户标识注册，兼容放行：{0}", context.Request.Path);
            WriteTenantCompatDataLog("影子兼容放行", $"旧客户端无租户标识注册，兼容放行 路径[{context.Request.Path}]", null, context.GetUserHost());
            return null;
        }

        // Enforce：注册必须携带租户信息，否则拒绝注册
        return "多租户模式下，注册必须携带X-App-Id或X-Tenant租户信息";
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
    /// <returns>JWT（三段）或分享短令牌（UserToken.Token，无点号）；无有效令牌时返回 null</returns>
    /// <remarks>
    /// 显式凭证（Authorization / X-Token / Query）优先于 Cookie，避免旧 Cookie JWT 盖住分享短令牌。
    /// 分享页签发的是 Rand 短串而非 JWT；若此处只认三段 JWT，LoadUser/TryLoadShareUserToken 永远收不到令牌。
    /// </remarks>
    public static String LoadToken(this HttpContext context)
    {
        //using var span = DefaultTracer.Instance?.NewSpan(nameof(LoadToken));

        var req = context?.Request;

        static Boolean IsJwt(String t) => !t.IsNullOrEmpty() && t.Split('.').Length == 3;
        // 分享 UserToken.Token / 刷新令牌等：无 '.' 的不透明串
        static Boolean IsOpaque(String t) => !t.IsNullOrEmpty() && !t.Contains('.') && t.Length >= 8;
        static Boolean IsAcceptable(String t) => IsJwt(t) || IsOpaque(t);

        var auth = req?.Headers[HeaderNames.Authorization].ToString().TrimPrefix("Bearer ");
        var xToken = req?.Headers["X-Token"].ToString().TrimPrefix("Bearer ");
        var qToken = req?.Query["token"] + "";
        var qJwt = req?.Query["jwtToken"] + "";

        // 显式凭证优先（分享页前端把短令牌放在 Authorization / ?token=）
        foreach (var t in new[] { auth, xToken, qToken, qJwt })
        {
            if (IsAcceptable(t)) return t;
        }

        if (CubeSetting.Current.TokenCookie)
        {
            var key = $"token-{SysConfig.Current.Name}";
            var cookie = req?.Cookies[key];
            if (IsAcceptable(cookie)) return cookie;
        }

        return null;
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
        // 幂等：同一请求内已颁发过登录令牌时直接复用，避免重复插入 UserToken 产生孤儿记录（P2-10）
        var existingAccess = context.Items["jwtToken"] as String;
        var existingRefresh = context.Items["refreshToken"] as String;
        if (!existingAccess.IsNullOrEmpty() && !existingRefresh.IsNullOrEmpty())
            return new TokenModel { AccessToken = existingAccess, RefreshToken = existingRefresh, ExpireIn = expire.TotalSeconds.ToInt() };

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
        context.Items["refreshToken"] = refreshToken;

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

        // 分享短令牌（非 JWT）：UserToken.Token
        var ut = UserToken.FindByToken(token);
        if (ut != null && ut.Enable
            && (ut.Expire.Year < 2000 || ut.Expire > DateTime.Now)
            && !(ut.Url + "").StartsWithIgnoreCase("attachment:"))
        {
            jwt = null;
            var name = ut.User?.Name;
            if (!name.IsNullOrEmpty()) return name;
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
            // 租户Cookie仅服务端读取，禁止脚本访问，防止XSS改写租户
            HttpOnly = true,
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

        // Header优先，兼容前后端分离/小程序等不稳定Cookie场景。
        // X-Tenant 传租户编码（Code），X-Tenant-Id 传数字ID（已废弃兼容）
        var tenant = req.Headers["X-Tenant"].ToString();
        if (!tenant.IsNullOrEmpty()) return ResolveTenantByCode(tenant);

        // [已过期] X-Tenant-Id头已被X-Tenant租户编码替代，仅保留兼容读取，新客户端请改用X-Tenant
        var idStr = req.Headers["X-Tenant-Id"].ToString();
        if (!idStr.IsNullOrEmpty()) return ResolveTenantById(idStr);

        // QueryString兜底（一般用于调试/回调等），传数字ID
        idStr = req.Query["tenantId"].ToString();
        if (!idStr.IsNullOrEmpty()) return ResolveTenantById(idStr);

        // 最后从Cookie读取（兼容现有浏览器模式），Cookie存的是数字ID
        var key = $"TenantId-{SysConfig.Current.Name}";
        var str = req.Cookies[key];
        return str.IsNullOrEmpty() ? -1 : ResolveTenantById(str);
    }

    /// <summary>按租户编码（Code）解析租户。X-Tenant 头专用，避免纯数字编码被误判为ID；多租户开启时校验存在且启用，无效返回-1</summary>
    /// <param name="code">租户编码</param>
    /// <returns>租户ID，无效返回-1</returns>
    private static Int32 ResolveTenantByCode(String code)
    {
        if (code.IsNullOrEmpty()) return -1;
        code = code.Trim();

        var t = Tenant.FindByCode(code);
        if (t == null) return -1;

        // 多租户开启时，校验租户启用
        if (CubeSetting.Current.EnableTenant && !t.Enable) return -1;
        return t.Id;
    }

    /// <summary>按数字ID解析租户。X-Tenant-Id/Query/Cookie 专用；0表示管理后台；多租户开启时校验存在且启用，无效返回-1</summary>
    /// <param name="idStr">租户数字ID</param>
    /// <returns>租户ID，无效返回-1；0表示管理后台</returns>
    private static Int32 ResolveTenantById(String idStr)
    {
        if (idStr.IsNullOrEmpty()) return -1;

        // 允许传 0（管理后台）
        var id = idStr.ToInt(-1);
        if (id == 0) return 0;
        if (id < 0) return -1;

        // 多租户开启时，校验租户存在且启用，防止伪造租户ID绕过数据隔离
        if (CubeSetting.Current.EnableTenant)
        {
            var t = Tenant.FindById(id);
            if (t == null || !t.Enable) return -1;
        }
        return id;
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

/// <summary>租户模式。用枚举表达 0/null/&gt;0 三态，避免魔法数字（P1-5）</summary>
public enum TenantMode
{
    /// <summary>未设置/未启用租户上下文</summary>
    None = 0,

    /// <summary>管理后台（租户0，不过滤数据）</summary>
    AdminBackend = 1,

    /// <summary>租户模式（TenantId&gt;0）</summary>
    Tenant = 2,
}

/// <summary>多租户强制模式</summary>
public enum TenantEnforceModes
{
    /// <summary>兼容观察期：旧客户端无租户标识时放行并记录影子日志（过渡期使用，后期移除）</summary>
    Shadow = 0,

    /// <summary>严格执行：fail-closed，无租户标识一律拒绝（最终稳定态）</summary>
    Enforce = 1,
}

/// <summary>多租户查询策略</summary>
public enum TenantQueryPolicies
{
    /// <summary>无租户上下文返回空集（fail-closed，默认）</summary>
    DenyWithEmpty = 0,

    /// <summary>无租户上下文显式抛错，适合对外 API</summary>
    ThrowOnMissingTenant = 1,
}

/// <summary>租户解析结果。单一解析入口的返回值（P1 结果对象）</summary>
public readonly struct TenantResolution
{
    /// <summary>是否携带租户标识（无论有效与否）</summary>
    public Boolean HasIdentifier { get; init; }

    /// <summary>解析到的租户ID。&gt;0=租户，0=管理后台，-1=未解析到有效租户</summary>
    public Int32 TenantId { get; init; }

    /// <summary>租户模式</summary>
    public TenantMode Mode => TenantId.GetTenantMode();

    /// <summary>是否解析到有效租户（含管理后台 0）</summary>
    public Boolean IsValid => TenantId >= 0;
}

/// <summary>租户上下文（DI scoped）。封装静态 AsyncLocal，供 ASP.NET 组件读取；XCode 层由中间件同步 AsyncLocal 后读取（P2-6）</summary>
public interface ITenantContext
{
    /// <summary>租户模式</summary>
    TenantMode Mode { get; }

    /// <summary>租户ID。0=管理后台，&gt;0=租户</summary>
    Int32 TenantId { get; }

    /// <summary>租户</summary>
    ITenant Tenant { get; }
}

/// <summary>租户上下文实现。包装静态 <see cref="TenantContext"/>，后续逐步把 ASP.NET 读站点迁移到 DI（P2-6）</summary>
public class TenantContextService : ITenantContext
{
    /// <summary>租户模式</summary>
    public TenantMode Mode => TenantContext.Current.GetTenantMode();

    /// <summary>租户ID</summary>
    public Int32 TenantId => TenantContext.CurrentId;

    /// <summary>租户</summary>
    public ITenant Tenant => TenantContext.Current?.Tenant;
}

/// <summary>租户成员授权统一授权点（三段式之③"成员授权"，方案§4.2）。管理员豁免；普通用户须有指定租户的有效 TenantUser 绑定。
/// 认证层 ValidateTenant 与（未来的）授权过滤器共用此唯一逻辑，语义只有一份</summary>
public static class TenantAccessPolicy
{
    /// <summary>校验用户是否属于指定租户。管理员豁免（可进管理后台及任意租户）；普通用户须有有效 TenantUser 绑定，且不能进管理后台（tenantId=0）</summary>
    /// <param name="tenantId">租户ID（&gt;0 为租户；0 为管理后台，仅管理员）</param>
    /// <param name="user">已认证用户</param>
    /// <returns>属于该租户返回true</returns>
    public static Boolean IsMember(Int32 tenantId, IManageUser user)
    {
        if (user == null) return false;
        // 系统管理员豁免：可进管理后台（tenantId=0）及任意租户
        if (user is IUser u && u.Roles.Any(e => e.IsSystem)) return true;
        // 普通用户不能进管理后台（tenantId<=0）
        if (tenantId <= 0) return false;

        var tu = TenantUser.FindByTenantIdAndUserId(tenantId, user.ID);
        return tu != null && tu.Enable;
    }
}

using System.Text;
using NewLife.Caching;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Entity;
using NewLife.Cube.Enums;
using NewLife.Cube.Models;
using NewLife.Cube.Services.Sso;
using NewLife.Cube.Web;
using NewLife.Log;
using NewLife.Model;
using NewLife.Security;
using NewLife.Threading;
using NewLife.Web;
using XCode;
using XCode.Membership;
using HttpContext = Microsoft.AspNetCore.Http.HttpContext;

namespace NewLife.Cube.Services;

/// <summary>用户服务</summary>
/// <remarks>
/// 基础用户服务：用户名密码登录、注册（含三方）、会话、在线统计、账号注销。
/// 验证码登录/注册/找回/绑定等增强能力由 <see cref="VerifyCodeService"/> 与 <see cref="AuthEnhancedService"/> 承载（MVC精简版不编译）。
/// </remarks>
/// <param name="passwordService">密码服务</param>
/// <param name="cacheProvider">缓存提供者</param>
/// <param name="mfaService">MFA 服务</param>
/// <param name="tracer">追踪器</param>
/// <param name="bindingService">用户绑定服务</param>
/// <param name="tenantContext">租户上下文</param>
public class UserService(PasswordService passwordService, ICacheProvider cacheProvider, IMfaService mfaService, ITracer tracer, IUserBindingService bindingService, ITenantContext tenantContext)
{
    #region 缓存Key前缀常量
    /// <summary>OAuth回跳注册待处理缓存前缀</summary>
    private const String OAuthPendingPrefix = "OAuthPending:";

    /// <summary>可信设备缓存前缀。值=设备首次可信时的IP，用于防止设备ID被复制跨IP滥用</summary>
    private const String TrustedDevicePrefix = "TrustedDevice:";
    #endregion

    #region 属性
    private readonly ICache _cache = cacheProvider.Cache;
    private readonly IMfaService _mfa = mfaService;
    private readonly ITenantContext _tenantContext = tenantContext;
    #endregion

    #region 可信设备
    /// <summary>判断设备是否可信。设备ID在可信缓存中且记录的可信IP与当前IP一致</summary>
    /// <param name="deviceId">设备ID</param>
    /// <param name="ip">当前IP</param>
    /// <returns>可信返回 true</returns>
    public Boolean IsTrustedDevice(String deviceId, String ip)
    {
        if (deviceId.IsNullOrEmpty()) return false;

        var set = CubeSetting.Current;
        if (set.TrustedDeviceDays <= 0) return false;

        var key = $"{TrustedDevicePrefix}{deviceId}";
        var value = _cache.Get<String>(key);
        if (value.IsNullOrEmpty()) return false;

        // 可信IP与当前IP不一致时视为不可信，防止设备ID被复制跨IP滥用
        return value.EqualIgnoreCase(ip);
    }

    /// <summary>标记设备为可信设备。登录/注册成功后调用，有效期内免自适应验证码</summary>
    /// <param name="deviceId">设备ID</param>
    /// <param name="ip">当前IP</param>
    public void SetTrustedDevice(String deviceId, String ip)
    {
        if (deviceId.IsNullOrEmpty()) return;

        var set = CubeSetting.Current;
        if (set.TrustedDeviceDays <= 0) return;

        var key = $"{TrustedDevicePrefix}{deviceId}";
        _cache.Set(key, ip, set.TrustedDeviceDays * 24 * 3600);
    }
    #endregion

    #region 登录
    /// <summary>统一登录入口。基础服务仅支持用户名密码登录，验证码登录见 <see cref="AuthEnhancedService.Login"/></summary>
    /// <param name="loginModel">登录模型</param>
    /// <param name="httpContext">HTTP上下文</param>
    /// <returns>登录结果，包含Token信息或错误信息</returns>
    public ServiceResult<IToken> Login(LoginModel loginModel, HttpContext httpContext) => LoginByPassword(loginModel, httpContext);

    /// <summary>账号密码登录</summary>
    /// <remarks>验证并返回Token</remarks>
    internal ServiceResult<IToken> LoginByPassword(LoginModel loginModel, HttpContext httpContext)
    {
        var username = loginModel.Username;
        var password = loginModel.Password;
        var remember = loginModel.Remember;
        var ip = httpContext.GetUserHost();
        using var span = tracer?.NewSpan(nameof(LoginByPassword), new { username, ip });

        // 连续错误校验
        var key = $"{AuthCacheKeys.PasswordLoginUserPrefix}{username}";
        var errors = _cache.Get<Int32>(key);
        var ipKey = $"{AuthCacheKeys.PasswordLoginIpPrefix}{ip}";
        var ipErrors = _cache.Get<Int32>(ipKey);
        // 子网连续错误校验（仅IPv4）
        var ip24 = AuthHelper.GetSubnet24(ip);
        var ip16 = AuthHelper.GetSubnet16(ip);
        var ip24Key = ip24.IsNullOrEmpty() ? "" : $"{AuthCacheKeys.LoginIpSubnet24Prefix}{ip24}";
        var ip24Errors = ip24Key.IsNullOrEmpty() ? 0 : _cache.Get<Int32>(ip24Key);
        var ip16Key = ip16.IsNullOrEmpty() ? "" : $"{AuthCacheKeys.LoginIpSubnet16Prefix}{ip16}";
        var ip16Errors = ip16Key.IsNullOrEmpty() ? 0 : _cache.Get<Int32>(ip16Key);

        var set = CubeSetting.Current;
        try
        {
            if (username.IsNullOrEmpty()) throw new ArgumentNullException(nameof(username), "用户名不能为空！");
            if (password.IsNullOrEmpty()) throw new ArgumentNullException(nameof(password), "密码不能为空！");

            if (errors >= set.MaxLoginError && set.MaxLoginError > 0)
                throw new InvalidOperationException($"[{username}]登录错误过多，请在{set.LoginForbiddenTime}秒后再试！");
            if (ipErrors >= set.MaxLoginError && set.MaxLoginError > 0)
                throw new InvalidOperationException($"IP地址[{ip}]登录错误过多，请在{set.LoginForbiddenTime}秒后再试！");
            if (!ip24Key.IsNullOrEmpty() && ip24Errors >= set.MaxLoginErrorBySubnet24 && set.MaxLoginErrorBySubnet24 > 0)
                throw new InvalidOperationException($"IP段[{ip24}.*]登录错误过多，请在{set.LoginForbiddenTime}秒后再试！");
            if (!ip16Key.IsNullOrEmpty() && ip16Errors >= set.MaxLoginErrorBySubnet16 && set.MaxLoginErrorBySubnet16 > 0)
                throw new InvalidOperationException($"IP段[{ip16}.*.*]登录错误过多，请在{set.LoginForbiddenTime}秒后再试！");

            // 安全登录检查：若禁止明文密码且未携带挑战标识，则拒绝
            if (loginModel.ChallengeId.IsNullOrEmpty() && !set.AllowPlainPassword)
                throw new InvalidOperationException("禁止明文传输密码，请先调用 GET /Auth/Challenge 获取公钥进行加密登录");

            // 携带 challengeId 时必须能取到私钥；取不到（过期/伪造）时明确报错，
            // 避免把密文当明文密码验证，导致"密码不正确"掩盖密钥过期根因
            if (!loginModel.ChallengeId.IsNullOrEmpty())
            {
                var pdic = _cache.Get<Tuple<String, String>>(loginModel.ChallengeId);
                var rsaKey = pdic?.Item2;
                if (rsaKey.IsNullOrEmpty())
                    throw new InvalidOperationException("登录挑战已过期或无效，请重新获取公钥后重试");
                password = DecryptByPrivateKey(rsaKey, password);
            }

            var provider = ManageProvider.Provider;
            if (provider.Login(username, password, remember) == null)
            {
                // 未激活账号（需邮箱/手机验证注册）：明确提示而非"密码错误"，前端据此提供重发激活入口
                var u = User.FindByName(username) ?? User.FindByMail(username) ?? User.FindByMobile(username);
                if (u != null && !u.Enable)
                    return new ServiceResult<IToken> { IsSuccess = false, Message = "账号未激活，请先通过邮箱/手机激活后登录" };

                // 本地验证失败，尝试外部验证服务
                var extUser = ExternalAuthHelper.Validate(username, password, set.ExternalAuthUrl);
                if (extUser == null)
                    return new ServiceResult<IToken> { IsSuccess = false, Message = "提供的用户名或密码不正确。" };

                // 外部验证成功，创建或更新本地用户
                var extLocalUser = ExternalAuthHelper.CreateOrUpdateUser(extUser, ip, set);
                provider.Current = extLocalUser;

                // 清空错误计数
                if (errors > 0) _cache.Remove(key);
                if (ipErrors > 0) _cache.Remove(ipKey);

                // 移除挑战私钥信息，避免重放
                if (!loginModel.ChallengeId.IsNullOrEmpty()) _cache.Remove(loginModel.ChallengeId);

                return CompleteLogin(extLocalUser, httpContext, remember, "外部验证登录", username, ip);
            }

            // 本地登录成功，清空错误数
            if (errors > 0) _cache.Remove(key);
            if (ipErrors > 0) _cache.Remove(ipKey);

            // 移除挑战私钥信息，避免重放
            if (!loginModel.ChallengeId.IsNullOrEmpty()) _cache.Remove(loginModel.ChallengeId);

            return CompleteLogin(provider.Current, httpContext, remember, "密码登录", username, ip);
        }
        catch (Exception ex)
        {
            HandleLoginError(ex, "登录", username, ip, key, ipKey, errors, ipErrors, ip24Key, ip24Errors, ip16Key, ip16Errors, set.LoginForbiddenTime);
            throw;
        }
    }

    /// <summary>完成登录，记录统计并生成Token。若用户已开启 MFA 则中断，返回挂起令牌要求二步验证</summary>
    internal ServiceResult<IToken> CompleteLogin(IManageUser user, HttpContext httpContext, Boolean remember, String action, String username, String ip)
    {
        var set = CubeSetting.Current;

        // 登录/注册成功，标记设备可信，有效期内免自适应验证码
        SetTrustedDevice(AuthHelper.GetDeviceId(httpContext), ip);

        // 头像为空时，自动设置基于用户ID的默认头像
        if (user is User userAv && userAv.Avatar.IsNullOrEmpty())
        {
            userAv.Avatar = $"/Sso/Avatar?id={user.ID}";
            (userAv as IEntity).Update();
        }

        // 记录在线统计
        var stat = UserStat.GetOrAdd(DateTime.Today);
        if (stat != null)
        {
            stat.Logins++;
            stat.SaveAsync(5_000);
        }

        // 自动绑定用户到当前租户
        if (set.EnableTenant) EnsureTenantUser(httpContext, user.ID, ip);

        // 设置租户
        httpContext.ChooseTenant(user.ID);

        // 登录/注册时显式持久化选中的租户到 Cookie（认证路径的 ChooseTenant 不再写 Cookie，消除认证副作用）
        if (_tenantContext.Mode != TenantMode.None)
            httpContext.SaveTenant(_tenantContext.TenantId);

        LogProvider.Provider.WriteLog(typeof(User), action, true, $"用户：{username}", user.ID, user + "", ip);

        // MFA 拦截：账密通过但用户已开启 MFA，不下发正式令牌，改为下发挂起令牌
        if (set.EnableMfa && _mfa != null && user is IUser iuser && _mfa.IsEnabled(iuser))
        {
            var mfaToken = _mfa.IssuePendingToken(user.ID);
            return new ServiceResult<IToken>
            {
                IsSuccess = true,
                Message = "mfa_required",
                MfaToken = mfaToken,
            };
        }

        // 先颁发令牌，JWT 缓存在 context.Items["jwtToken"]
        // 记住登录状态（Remember）：JWT 有效期与 Cookie 一致延长到 365 天，
        // 前端（SPA 存 localStorage）在有效期内重开系统免登录；令牌带 jti，退出登录仍可吊销
        var tokenExpire = remember ? TimeSpan.FromDays(365) : TimeSpan.FromSeconds(set.TokenExpire);
        var tokens = httpContext.IssueLoginToken(user, tokenExpire);

        // 再存 Cookie（优先取 Items 中的 JWT，即包含 jti 的那个）
        var provider = ManageProvider.Provider;
        var expire = remember ? TimeSpan.FromDays(365) : TimeSpan.FromMinutes(0);
        if (set.SessionTimeout > 0 && !remember)
            expire = TimeSpan.FromSeconds(set.SessionTimeout);
        provider.SaveCookie(user, expire, httpContext);

        return new ServiceResult<IToken> { IsSuccess = true, Data = tokens, Message = "登录成功" };
    }

    /// <summary>确保用户已绑定到当前租户。用户从哪个租户登录/注册，自动添加绑定关系</summary>
    /// <param name="httpContext">HTTP上下文</param>
    /// <param name="userId">用户编号</param>
    /// <param name="ip">客户端IP</param>
    /// <returns>租户用户绑定记录，无需绑定时返回null</returns>
    private TenantUser EnsureTenantUser(HttpContext httpContext, Int32 userId, String ip)
    {
        // 规则B（永久能力，非影子期兼容）：优先 X-App-Id（OAuth 配置租户），其次 X-Tenant/Query/Cookie；
        // 有有效租户标识且用户未绑定时自动补建绑定。无租户标识返回 -1，不处理。
        var tenantId = httpContext.ResolveTenantForLogin();
        if (tenantId <= 0 || userId <= 0) return null;

        // 检查是否已绑定到该租户
        var tenantUser = TenantUser.FindByTenantIdAndUserId(tenantId, userId);
        if (tenantUser != null) return tenantUser;

        // 规则B 收紧：仅"存量无有效绑定用户"（无任何 Enable=true 的 TenantUser）才自动绑定。
        // 已属于其它租户的用户不自动自建加入，防止带可猜编码的水平越权；多租户归属走显式管理动作。
        if (TenantUser.FindAllByUserId(userId).Any(e => e.Enable))
        {
            XTrace.WriteLine("[TenantBind] 用户[{0}]已有有效租户归属，拒绝自动绑定到租户[{1}]", userId, tenantId);
            return null;
        }

        // 自动创建绑定关系
        tenantUser = new TenantUser
        {
            TenantId = tenantId,
            UserId = userId,
            Enable = true,
            CreateIP = ip,
            CreateTime = DateTime.Now,
        };
        tenantUser.Insert();

        // 审计：自动绑定租户（规则B），写入审计日志便于追溯自建绑定行为（P2-5）
        XTrace.WriteLine($"[{userId}]用户自动绑定到租户[{tenantId}]");
        LogProvider.Provider.WriteLog(typeof(TenantUser), "自动绑定", true, $"用户[{userId}]自动绑定到租户[{tenantId}]", userId, userId + "", ip);
        return tenantUser;
    }

    /// <summary>处理登录错误，记录日志并累加错误次数（含子网计数）</summary>
    internal void HandleLoginError(Exception ex, String action, String username, String ip, String key, String ipKey, Int32 errors, Int32 ipErrors, String ip24Key, Int32 ip24Errors, String ip16Key, Int32 ip16Errors, Int32 forbiddenTime)
    {
        var logAction = ex is InvalidOperationException ? "风控" : action;
        LogProvider.Provider.WriteLog(typeof(User), logAction, false, ex.Message, 0, username, ip);
        XTrace.WriteLine("[{0}]{1}失败！{2}", username, action, ex.Message);

        // 累加错误数，首次出错时设置过期时间
        _cache.Increment(key, 1);
        _cache.Increment(ipKey, 1);
        var time = forbiddenTime > 0 ? forbiddenTime : 300;
        if (errors <= 0) _cache.SetExpire(key, TimeSpan.FromSeconds(time));
        if (ipErrors <= 0) _cache.SetExpire(ipKey, TimeSpan.FromSeconds(time));

        // 累加子网错误数，首次出错时设置过期时间（ip24Key/ip16Key 为空则说明非IPv4，跳过）
        if (!ip24Key.IsNullOrEmpty())
        {
            _cache.Increment(ip24Key, 1);
            if (ip24Errors <= 0) _cache.SetExpire(ip24Key, TimeSpan.FromSeconds(time));
        }
        if (!ip16Key.IsNullOrEmpty())
        {
            _cache.Increment(ip16Key, 1);
            if (ip16Errors <= 0) _cache.SetExpire(ip16Key, TimeSpan.FromSeconds(time));
        }
    }

    private static String Decrypt(String privateKey, String decryptString)
    {
        var decryptedData = RSAHelper.Decrypt(Convert.FromBase64String(decryptString), privateKey, false);

        return Encoding.UTF8.GetString(decryptedData);
    }

    /// <summary>使用 RSA-PKCS1v15 解密（对应前端 JSEncrypt 默认加密）</summary>
    private static String DecryptPkcs1v15(String pemPrivateKey, String base64Encrypted)
    {
        using var rsa = System.Security.Cryptography.RSA.Create();
        rsa.ImportFromPem(pemPrivateKey);
        var decrypted = rsa.Decrypt(
            Convert.FromBase64String(base64Encrypted),
            System.Security.Cryptography.RSAEncryptionPadding.Pkcs1);
        return Encoding.UTF8.GetString(decrypted);
    }

    /// <summary>使用 RSA-OAEP 解密（对应前端 Web Crypto API RSA-OAEP 加密）</summary>
    private static String DecryptOAEP(String xmlPrivateKey, String base64Encrypted)
    {
        using var rsa = System.Security.Cryptography.RSA.Create();
        rsa.FromXmlString(xmlPrivateKey);
        var decrypted = rsa.Decrypt(
            Convert.FromBase64String(base64Encrypted),
            System.Security.Cryptography.RSAEncryptionPadding.OaepSHA256);
        return Encoding.UTF8.GetString(decrypted);
    }

    /// <summary>按私钥格式自动分派解密。PEM 私钥用 PKCS1v15（对应前端 JSEncrypt），XML 私钥用 OAEP/SHA-256（对应前端 Web Crypto RSA-OAEP）</summary>
    /// <param name="privateKey">PEM 或 XML 格式 RSA 私钥</param>
    /// <param name="base64Encrypted">前端用公钥加密后的 Base64 密文</param>
    /// <returns>解密后的原始密码</returns>
    internal static String DecryptByPrivateKey(String privateKey, String base64Encrypted)
        => privateKey.StartsWith("-----BEGIN")
            ? DecryptPkcs1v15(privateKey, base64Encrypted)
            : DecryptOAEP(privateKey, base64Encrypted);

    /// <summary>生成RSA密钥对并缓存，返回挑战标识和PEM格式公钥。客户端用公钥加密密码后携带 challengeId 提交登录</summary>
    /// <param name="ttl">密钥有效期（秒），默认300秒</param>
    /// <returns>挑战标识和PEM格式RSA公钥</returns>
    public (String challengeId, String publicKey) GetPublicKey(Int32 ttl = 300)
    {
        var ks = RSAHelper.GenerateKey(); // ks[0]=私钥XML, ks[1]=公钥XML
        var challengeId = Guid.NewGuid().ToString("N");
        // Item1=公钥XML（备查），Item2=私钥XML（DecryptOAEP解密用）
        _cache.Set(challengeId, Tuple.Create(ks[1], ks[0]), ttl);
        // 将 XML 公钥转换为标准 PEM(SPKI) 格式，供前端 Web Crypto API 使用
        var pemPublicKey = ConvertXmlPublicKeyToPem(ks[1]);
        return (challengeId, pemPublicKey);
    }

    /// <summary>将 .NET XML 格式RSA公钥转换为标准 PEM(SPKI) 格式，供前端 Web Crypto API 导入使用</summary>
    /// <param name="xmlPublicKey">XML格式RSA公钥</param>
    /// <returns>PEM(SPKI)格式RSA公钥字符串</returns>
    private static String ConvertXmlPublicKeyToPem(String xmlPublicKey)
    {
        using var rsa = System.Security.Cryptography.RSA.Create();
        rsa.FromXmlString(xmlPublicKey);
        var pkiBytes = rsa.ExportSubjectPublicKeyInfo();
        return "-----BEGIN PUBLIC KEY-----\n"
            + Convert.ToBase64String(pkiBytes, Base64FormattingOptions.InsertLineBreaks)
            + "\n-----END PUBLIC KEY-----";
    }
    #endregion

    #region 注册
    /// <summary>统一注册入口。基础服务支持用户名密码/OAuth注册，验证码注册见 <see cref="AuthEnhancedService.Register"/></summary>
    /// <param name="model">注册模型</param>
    /// <param name="httpContext">HTTP上下文</param>
    /// <returns>注册并登录结果</returns>
    public ServiceResult<IToken> Register(AuthRegisterModel model, HttpContext httpContext)
    {
        var set = CubeSetting.Current;
        if (!set.AllowRegister) return new ServiceResult<IToken> { IsSuccess = false, Message = "禁止注册" };
        if (model == null) return new ServiceResult<IToken> { IsSuccess = false, Message = "注册参数不能为空" };

        // 租户识别：优先X-App-Id（参考SSO登录按AppId查找OAuth配置取租户），其次X-Tenant租户编码；均未传时不强制，沿用原逻辑
        var tenantError = httpContext.ResolveRegisterTenant();
        if (tenantError != null) return new ServiceResult<IToken> { IsSuccess = false, Message = tenantError };

        var ip = httpContext.GetUserHost();
        using var span = tracer?.NewSpan(nameof(Register), new { model.Category, model.Username, model.Mobile, model.Email, ip });

        try
        {
            return model.Category switch
            {
                AuthCategory.OAuth => RegisterByOAuthBind(model, httpContext, ip),
                _ => RegisterByPassword(model, httpContext, ip),
            };
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            return new ServiceResult<IToken> { IsSuccess = false, Message = ex.Message };
        }
    }

    internal ServiceResult<IToken> RegisterByPassword(AuthRegisterModel model, HttpContext httpContext, String ip)
    {
        var username = model.Username?.Trim();
        var email = model.Email?.Trim();
        var mobile = model.Mobile?.Trim();

        var check = ValidatePasswordAndConfirm(model.Password, model.ConfirmPassword);
        if (!check.IsSuccess) return new ServiceResult<IToken> { IsSuccess = false, Message = check.Message };

        if (username.IsNullOrEmpty())
            return new ServiceResult<IToken> { IsSuccess = false, Message = "用户名不能为空" };

        var duplicate = CheckDuplicate(username, email, mobile);
        if (!duplicate.IsSuccess) return new ServiceResult<IToken> { IsSuccess = false, Message = duplicate.Message };

        var user = CreateUserAndBindContact(username, model.Password, email, mobile, ip);
        return CompleteLogin(user, httpContext, false, "注册", username, ip);
    }

    internal ServiceResult<IToken> RegisterByOAuthBind(AuthRegisterModel model, HttpContext httpContext, String ip)
    {
        if (model.OAuthToken.IsNullOrEmpty())
            return new ServiceResult<IToken> { IsSuccess = false, Message = "OAuth回跳令牌不能为空" };

        var pendingKey = $"{OAuthPendingPrefix}{model.OAuthToken}";
        var pending = _cache.Get<OAuthPendingInfoModel>(pendingKey);
        if (pending == null)
            return new ServiceResult<IToken> { IsSuccess = false, Message = "OAuth回跳信息已过期，请重新发起第三方登录" };

        // 密码可选：留空则生成随机密码（三方登录用户无需密码，登录后可再设置）
        var pwd = ResolveRegisterPassword(model);
        if (!pwd.IsSuccess) return new ServiceResult<IToken> { IsSuccess = false, Message = pwd.Message };

        var username = model.Username?.Trim();
        if (username.IsNullOrEmpty()) username = pending.Username?.Trim();

        var email = model.Email?.Trim();
        if (email.IsNullOrEmpty()) email = pending.Email?.Trim();

        var mobile = model.Mobile?.Trim();
        if (mobile.IsNullOrEmpty()) mobile = pending.Mobile?.Trim();

        if (username.IsNullOrEmpty())
        {
            if (!email.IsNullOrEmpty())
                username = email.Split('@')[0];
            else if (!mobile.IsNullOrEmpty())
                username = $"P{mobile}";
            else
                username = $"OAuth_{Rand.NextString(8)}";
        }

        var duplicate = CheckDuplicate(username, email, mobile);
        if (!duplicate.IsSuccess) return new ServiceResult<IToken> { IsSuccess = false, Message = duplicate.Message };

        var user = CreateUserAndBindContact(username, pwd.Data, email, mobile, ip);
        var result = CompleteLogin(user, httpContext, false, "OAuth回跳注册", username, ip);

        var oauthId = httpContext.Session?.GetString("Cube_OAuthId").ToLong() ?? 0;
        if (oauthId <= 0)
            return new ServiceResult<IToken> { IsSuccess = false, Message = "OAuth绑定会话已过期，请重新发起第三方登录" };

        var log = bindingService.BindAfterLogin(oauthId);
        if (log == null)
            return new ServiceResult<IToken> { IsSuccess = false, Message = "OAuth绑定失败，请重新发起第三方登录" };

        httpContext.Session.Remove("Cube_OAuthId");
        _cache.Remove(pendingKey);

        LogProvider.Provider.WriteLog(typeof(User), "OAuth回跳注册", true, $"提供商：{pending.Provider}", user.ID, user + "", ip);

        return result;
    }

    internal ServiceResult ValidatePasswordAndConfirm(String password, String confirmPassword)
    {
        if (password.IsNullOrEmpty()) return new ServiceResult { IsSuccess = false, Message = "密码不能为空" };
        if (confirmPassword.IsNullOrEmpty()) return new ServiceResult { IsSuccess = false, Message = "确认密码不能为空" };
        if (password != confirmPassword) return new ServiceResult { IsSuccess = false, Message = "两次输入密码不一致" };
        if (!passwordService.Valid(password)) return new ServiceResult { IsSuccess = false, Message = "密码太弱" };

        return new ServiceResult { IsSuccess = true };
    }

    /// <summary>注册密码解析：留空生成随机密码（验证码/OAuth 注册无需密码，仅验证码/三方登录，登录后可再设置），非空则校验强度与一致性</summary>
    /// <param name="model">注册模型</param>
    /// <returns>解析后的密码；失败时 Message 为错误信息</returns>
    internal ServiceResult<String> ResolveRegisterPassword(AuthRegisterModel model)
    {
        var password = model.Password?.Trim();
        if (password.IsNullOrEmpty())
        {
            // 未设置密码：生成随机密码，该账号无法使用密码登录，仅支持验证码/三方登录，后续可主动设置密码
            return new ServiceResult<String> { IsSuccess = true, Data = Rand.NextString(16) };
        }

        var check = ValidatePasswordAndConfirm(model.Password, model.ConfirmPassword);
        if (!check.IsSuccess) return new ServiceResult<String> { IsSuccess = false, Message = check.Message };

        return new ServiceResult<String> { IsSuccess = true, Data = password };
    }

    internal ServiceResult CheckDuplicate(String username, String email, String mobile)
    {
        if (!username.IsNullOrEmpty() && User.FindByName(username) != null)
            return new ServiceResult { IsSuccess = false, Message = $"用户[{username}]已存在" };

        if (!email.IsNullOrEmpty() && User.FindByMail(email) != null)
            return new ServiceResult { IsSuccess = false, Message = $"邮箱[{email}]已存在" };

        if (!mobile.IsNullOrEmpty() && User.FindByMobile(mobile) != null)
            return new ServiceResult { IsSuccess = false, Message = $"手机号[{mobile}]已存在" };

        return new ServiceResult { IsSuccess = true };
    }

    /// <summary>创建用户并绑定联系方式</summary>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <param name="email">邮箱</param>
    /// <param name="mobile">手机</param>
    /// <param name="ip">注册IP</param>
    /// <param name="mailVerified">邮箱已验证（仅验证码注册路径传入 true）</param>
    /// <param name="mobileVerified">手机已验证（仅验证码注册路径传入 true）</param>
    /// <param name="enable">是否启用。false 表示待激活（需邮箱/手机验证），由账号激活服务使用</param>
    /// <returns>新用户</returns>
    internal IManageUser CreateUserAndBindContact(String username, String password, String email, String mobile, String ip, Boolean mailVerified = false, Boolean mobileVerified = false, Boolean enable = true)
    {
        var set = CubeSetting.Current;

        foreach (var item in OAuthConfig.GetValids(_tenantContext.TenantId))
        {
            if (username.StartsWithIgnoreCase($"{item.Name}_"))
                throw new ArgumentException($"禁止使用[{item.Name}_]前缀！", nameof(username));
        }

        var role = Role.GetOrAdd(set.DefaultRole);
        var provider = ManageProvider.Provider;
        provider.Register(username, password, role?.ID ?? 0, enable);

        var user = provider.FindByName(username) as User ?? User.FindByName(username);
        if (user == null) throw new InvalidOperationException("注册失败，请稍后重试");

        var changed = false;
        // 只有经过验证码校验的联系方式才标记为已验证（mailVerified/mobileVerified 由各注册路径传入），
        // 用户名密码注册携带的联系方式未经校验，保持未验证状态，防止"未验证却标已验证"
        if (!email.IsNullOrEmpty() && !email.EqualIgnoreCase(user.Mail))
        {
            user.Mail = email;
            if (mailVerified) user.MailVerified = true;
            changed = true;
        }
        if (!mobile.IsNullOrEmpty() && !mobile.EqualIgnoreCase(user.Mobile))
        {
            user.Mobile = mobile;
            if (mobileVerified) user.MobileVerified = true;
            changed = true;
        }
        if (user.Enable != enable)
        {
            user.Enable = enable;
            changed = true;
        }
        if (user.RegisterIP.IsNullOrEmpty())
        {
            user.RegisterIP = ip;
            changed = true;
        }
        if (user.RegisterTime.Year < 2000)
        {
            user.RegisterTime = DateTime.Now;
            changed = true;
        }

        if (changed) user.Update();

        return user;
    }
    #endregion

    #region 账号管理（注销 / 导出）
    /// <summary>注销账号：禁用账号并清空个性化数据（依据《个人信息保护法》提供账号注销功能）</summary>
    /// <remarks>
    /// 软删除：保留 ID/Name（防重名、可审计），Enable=false 禁用；
    /// 清空 Mail/Mobile/DisplayName/Avatar/Password 等敏感字段；
    /// 吊销全部令牌、解绑第三方、清理在线记录。
    /// </remarks>
    /// <param name="user">当前用户</param>
    /// <param name="ip">客户端IP</param>
    /// <returns>注销结果</returns>
    public ServiceResult CloseAccount(IUser user, String ip)
    {
        using var span = tracer?.NewSpan(nameof(CloseAccount), new { user?.ID, ip });

        if (user == null || user.ID <= 0) return new ServiceResult { IsSuccess = false, Message = "用户未登录" };

        // 吊销全部令牌，使其立即失效
        UserToken.RevokeByUser(user.ID);

        // 解绑第三方
        var ucs = UserConnect.FindAllByUserID(user.ID);
        if (ucs.Count > 0) ucs.Delete();

        // 清理在线记录
        var onlines = UserOnline.FindAllByUserID(user.ID);
        if (onlines.Count > 0) onlines.Delete();

        // 禁用账号并清空个性化数据（保留 ID/Name 防重名与审计）
        var entity = User.FindByID(user.ID);
        if (entity != null)
        {
            entity.Enable = false;
            entity.Password = null;
            entity.Mail = null;
            entity.MailVerified = false;
            entity.Mobile = null;
            entity.MobileVerified = false;
            entity.DisplayName = null;
            entity.Avatar = null;
            entity.Code = null;
            entity.Age = 0;
            entity.Birthday = DateTime.MinValue;
            entity.LastLoginIP = null;
            entity.Update();
        }

        LogProvider.Provider.WriteLog(typeof(User), "注销账号", true, $"用户：{user}", user.ID, user + "", ip);

        return new ServiceResult { IsSuccess = true, Message = "账号已注销" };
    }
    #endregion

    #region 定时任务
    private TimerX _timer;
    private TimerX _timer2;
    private Int32 _onlines;

    private void StartTimer()
    {
        if (_timer != null) return;

        lock (this)
        {
            if (_timer != null) return;

            //!!! 临时关闭OnlineTime累加字段
            User.Meta.Factory.AdditionalFields.Remove(nameof(User.__.OnlineTime));

            _timer = new TimerX(s => ClearExpire(), null, 1000, 60 * 1000) { Async = true };
            _timer2 = new TimerX(DoStat, null, 3000, 60 * 1000) { Async = true };
        }
    }
    #endregion

    #region 用户在线
    /// <summary>设置会话状态</summary>
    /// <returns></returns>
    public UserOnline SetStatus(UserOnline online, String sessionId, String deviceId, String page, String status, UserAgentParser userAgent, Int32 userid = 0, String name = null, String ip = null)
    {
        // 网页使用一个定时器来清理过期
        StartTimer();

        if (online != null && online.SessionID != sessionId) online = null;

        // LastError 设计缺陷，非空设计导致无法在插入中忽略
        online ??= UserOnline.GetOrAdd(sessionId, UserOnline.FindBySessionID, k => new UserOnline
        {
            SessionID = k,
            LastError = new DateTime(1970, 1, 2),//MSSql不能使用1973年之前的日期
            CreateIP = ip,
            CreateTime = DateTime.Now
        });
        online.DeviceId = deviceId;
        online.Page = page;

        if (userAgent != null)
        {
            online.Platform = userAgent.Platform;
            online.OS = userAgent.OSorCPU;
            if (userAgent.Device.IsNullOrEmpty() || !userAgent.DeviceBuild.IsNullOrEmpty() && userAgent.DeviceBuild.Contains(userAgent.Device))
                online.Device = userAgent.DeviceBuild;
            else
                online.Device = userAgent.Device;
            online.Brower = userAgent.Brower;
            online.NetType = userAgent.NetType;
        }

        if (!status.IsNullOrEmpty() || online.LastError.AddMinutes(3) < DateTime.Now) online.Status = status;

        online.Times++;
        if (userid > 0) online.UserID = userid;
        if (!name.IsNullOrEmpty()) online.Name = name;

        online.Address = ip.IPToAddress();

        // 累加在线时间
        online.UpdateTime = DateTime.Now;
        online.UpdateIP = ip;
        online.OnlineTime = (Int32)(online.UpdateTime - online.CreateTime).TotalSeconds;
        online.TraceId = DefaultSpan.Current?.TraceId;
        online.SaveAsync(5_000);

        if (_onlines == 0 || online.Times <= 1)
            Interlocked.Increment(ref _onlines);

        return online;
    }

    /// <summary>设置网页会话状态</summary>
    /// <returns></returns>
    public UserOnline SetWebStatus(UserOnline online, String sessionId, String deviceId, String page, String status, UserAgentParser userAgent, IUser user, String ip)
    {
        // 网页使用一个定时器来清理过期
        StartTimer();

        if (user == null) return SetStatus(online, sessionId, deviceId, page, status, userAgent, 0, null, ip);

        // 根据IP修正用户城市
        if (user is User user2 && (user2.AreaId == 0 || user2.AreaId % 10000 == 0))
        {
            try
            {
                var rs = Area.SearchIP(ip, 2);
                if (rs.Count > 0)
                {
                    user2.AreaId = rs[rs.Count - 1].ID;
                    user2.SaveAsync();
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }

        return SetStatus(online, sessionId, deviceId, page, status, userAgent, user.ID, user + "", ip);
    }

    /// <summary>删除过期，指定过期时间</summary>
    /// <param name="secTimeout">超时时间，20 * 60秒</param>
    /// <returns></returns>
    public IList<UserOnline> ClearExpire(Int32 secTimeout = 20 * 60)
    {
        // 无在线则不执行
        if (_onlines == 0) return [];

        using var span = tracer?.NewSpan("ClearExpireOnline");

        var set = CubeSetting.Current;

        // 减少Sql日志
        var dal = UserOnline.Meta.Session.Dal;
        var oldSql = dal.Session.ShowSQL;
        dal.Session.ShowSQL = false;
        try
        {
            // 10分钟不活跃将会被删除
            var exp = UserOnline._.UpdateTime < DateTime.Now.AddSeconds(-secTimeout);
            var list = UserOnline.FindAll(exp, null, null, 0, 0);
            list.Delete();

            // 修正在线数
            var total = UserOnline.Meta.Count;

            // 设置统计
            UserStat stat = null;
            if (total != _onlines || list.Count > 0)
            {
                if (set.EnableUserStat)
                {
                    stat = UserStat.GetOrAdd(DateTime.Today);
                    if (stat != null)
                    {
                        if (total > stat.MaxOnline) stat.MaxOnline = total;
                    }
                }
            }

            _onlines = total - list.Count;

            // 设置离线
            foreach (var item in list)
            {
                var user = ManageProvider.Provider.FindByID(item.UserID);
                if (user is User user2)
                {
                    user2.Online = false;
                    user2.OnlineTime += item.OnlineTime;
                    user2.Save();
                }

                if (stat != null) stat.OnlineTime += item.OnlineTime;
            }
            stat?.Update();

            return list;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
        finally
        {
            dal.Session.ShowSQL = oldSql;
        }
    }

    /// <summary>
    /// 注销用户时，更新在线表和统计表
    /// </summary>
    /// <param name="user"></param>
    public static void ClearOnline(User user)
    {
        var set = CubeSetting.Current;

        // 在线表删除
        var olts = UserOnline.FindAllByUserID(user.ID);
        if (olts.Count > 0)
        {
            foreach (var olt in olts)
            {
                user.OnlineTime += olt.OnlineTime;
                olt.Delete();
            }
            if (set.EnableUserStat)
            {
                var stat = UserStat.GetOrAdd(DateTime.Today);
                foreach (var olt in olts)
                {
                    stat.OnlineTime += olt.OnlineTime;
                }
                stat.Update();
            }
        }

        user.Online = false;
        user.SaveAsync();
    }
    #endregion

    #region 用户统计
    private void DoStat(Object state)
    {
        // 无在线则不执行
        if (_onlines == 0) return;

        var set = CubeSetting.Current;
        if (!set.EnableUserStat) return;

        using var span = tracer?.NewSpan("UserStat");

        var t1 = DateTime.Today.AddDays(-0);
        var t7 = DateTime.Today.AddDays(-7);
        var t30 = DateTime.Today.AddDays(-30);

        var selects = UserStat._.ID.Count();
        selects &= User._.LastLogin.SumLarge($"'{t1:yyyy-MM-dd}'", "activeT1");
        selects &= User._.LastLogin.SumLarge($"'{t7:yyyy-MM-dd}'", "activeT7");
        selects &= User._.LastLogin.SumLarge($"'{t30:yyyy-MM-dd}'", "activeT30");
        selects &= User._.RegisterTime.SumLarge($"'{t1:yyyy-MM-dd}'", "newT1");
        selects &= User._.RegisterTime.SumLarge($"'{t7:yyyy-MM-dd}'", "newT7");
        selects &= User._.RegisterTime.SumLarge($"'{t30:yyyy-MM-dd}'", "newT30");

        // 减少Sql日志
        var dal = UserOnline.Meta.Session.Dal;
        var oldSql = dal.Session.ShowSQL;
        dal.Session.ShowSQL = false;
        try
        {
            var list = User.FindAll(null, null, selects, 0, 1);
            if (list.Count > 0)
            {
                var user = list[0];

                var st = UserStat.GetOrAdd(DateTime.Today);
                st.Total = user.ID;
                st.Actives = user["activeT1"].ToInt();
                st.ActivesT7 = user["activeT7"].ToInt();
                st.ActivesT30 = user["activeT30"].ToInt();
                st.News = user["newT1"].ToInt();
                st.NewsT7 = user["newT7"].ToInt();
                st.NewsT30 = user["newT30"].ToInt();

                st.Update();
            }
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
        finally
        {
            dal.Session.ShowSQL = oldSql;
        }
    }
    #endregion
}

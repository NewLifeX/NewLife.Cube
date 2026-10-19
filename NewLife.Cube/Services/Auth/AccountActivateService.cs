using NewLife.Caching;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Common;
using NewLife.Cube.Enums;
using NewLife.Cube.Models;
using NewLife.Log;
using NewLife.Security;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Services;

/// <summary>账号激活服务。支撑邮箱/手机验证：注册待激活、激活（链接/验证码）、重发激活、已登录验证联系方式</summary>
/// <remarks>对应 issue #112 邮箱验证 / #113 手机验证。注册需验证时账号未激活（Enable=false），激活后置 true 方可登录。验证过程记录进验证表 VerifyCodeRecord（Action=Activate）</remarks>
/// <param name="userService">基础用户服务</param>
/// <param name="verifyCode">验证码服务</param>
/// <param name="cacheProvider">缓存提供者</param>
/// <param name="tracer">追踪器</param>
public class AccountActivateService(UserService userService, VerifyCodeService verifyCode, ICacheProvider cacheProvider, ITracer tracer)
{
    private readonly ICache _cache = cacheProvider.Cache;

    /// <summary>激活有效期（秒）。默认 1 小时</summary>
    public const Int32 DefaultExpire = 3600;

    #region 注册待激活
    /// <summary>注册待激活：创建未激活账号（Enable=false），发送激活（邮箱链接+验证码/手机短信验证码），不自动登录</summary>
    /// <param name="model">注册模型</param>
    /// <param name="httpContext">HTTP上下文（用于拼激活链接）</param>
    /// <param name="ip">客户端IP</param>
    /// <returns>待激活信息；失败时 Message 为错误说明</returns>
    public async Task<ServiceResult<ActivatePendingModel>> RegisterPendingActivation(AuthRegisterModel model, HttpContext httpContext, String ip)
    {
        using var span = tracer?.NewSpan(nameof(RegisterPendingActivation), new { model.Category, model.Username, model.Email, model.Mobile, ip });

        var set = CubeSetting.Current;
        var requireMail = set.RequireMailVerify;
        var requireMobile = set.RequireMobileVerify;

        // 需要验证的渠道必须提供对应联系方式
        var email = model.Email?.Trim();
        var mobile = model.Mobile?.Trim();
        if (requireMail && email.IsNullOrEmpty())
            return new ServiceResult<ActivatePendingModel> { IsSuccess = false, Message = L.T("Email verification is required; please provide an email address.") };
        if (requireMobile && mobile.IsNullOrEmpty())
            return new ServiceResult<ActivatePendingModel> { IsSuccess = false, Message = L.T("Mobile verification is required; please provide a mobile number.") };
        if (!email.IsNullOrEmpty() && !ValidFormatHelper.IsEmail(email))
            return new ServiceResult<ActivatePendingModel> { IsSuccess = false, Message = L.T("Invalid email format.") };
        if (!mobile.IsNullOrEmpty() && !ValidFormatHelper.IsMobile(mobile))
            return new ServiceResult<ActivatePendingModel> { IsSuccess = false, Message = L.T("Invalid mobile number format.") };

        // 密码可选：留空则生成随机密码（激活后无法密码登录，仅验证码登录，登录后可再设置密码）
        var pwd = userService.ResolveRegisterPassword(model);
        if (!pwd.IsSuccess) return new ServiceResult<ActivatePendingModel> { IsSuccess = false, Message = pwd.Message };

        // 手机/邮箱验证码注册需先通过注册验证码（激活码独立于注册码）
        if (model.Category == AuthCategory.Mobile)
        {
            var codeKey = $"{AuthCacheKeys.SmsRegisterCodePrefix}{mobile}";
            var cached = _cache.Get<String>(codeKey);
            if (cached.IsNullOrEmpty() || !cached.EqualIgnoreCase(model.Code))
                return new ServiceResult<ActivatePendingModel> { IsSuccess = false, Message = L.T("The registration verification code is incorrect or has expired. Please request a new one.") };
            _cache.Remove(codeKey);
        }
        else if (model.Category == AuthCategory.Mail)
        {
            var codeKey = $"{AuthCacheKeys.MailRegisterCodePrefix}{email}";
            var cached = _cache.Get<String>(codeKey);
            if (cached.IsNullOrEmpty() || !cached.EqualIgnoreCase(model.Code))
                return new ServiceResult<ActivatePendingModel> { IsSuccess = false, Message = L.T("The registration verification code is incorrect or has expired. Please request a new one.") };
            _cache.Remove(codeKey);
        }

        // 用户名：注册填写的用户名，缺省回退邮箱前缀/手机号
        var username = model.Username?.Trim();
        if (username.IsNullOrEmpty())
        {
            if (!email.IsNullOrEmpty()) username = email.Split('@')[0];
            else if (!mobile.IsNullOrEmpty()) username = $"P{mobile}";
        }
        if (username.IsNullOrEmpty())
            return new ServiceResult<ActivatePendingModel> { IsSuccess = false, Message = L.T("Username is required.") };

        var duplicate = userService.CheckDuplicate(username, email, mobile);
        if (!duplicate.IsSuccess) return new ServiceResult<ActivatePendingModel> { IsSuccess = false, Message = duplicate.Message };

        // 创建未激活账号（Enable=false），激活后置 true 方可登录
        var user = userService.CreateUserAndBindContact(username, pwd.Data, email, mobile, ip, false, false, false);
        if (user == null || user.ID <= 0)
            return new ServiceResult<ActivatePendingModel> { IsSuccess = false, Message = L.T("Registration failed. Please try again later.") };

        try
        {
            var channels = new List<String>();
            var targets = new List<String>();

            // 发送激活：邮箱=激活链接+验证码，手机=短信验证码
            if (requireMail && !email.IsNullOrEmpty())
            {
                await SendMailActivation(email, user.ID, httpContext, ip);
                channels.Add("mail");
                targets.Add(MaskTarget(email));
            }
            if (requireMobile && !mobile.IsNullOrEmpty())
            {
                await verifyCode.SendActivateCode("Sms", mobile, null, ip);
                channels.Add("sms");
                targets.Add(MaskTarget(mobile));
            }

            LogProvider.Provider.WriteLog(typeof(User), L.T("Registration pending activation"), true, L.T("Email: {0}, mobile: {1}", email, mobile), user.ID, user + "", ip);

            return new ServiceResult<ActivatePendingModel>
            {
                IsSuccess = true,
                Message = L.T("Registered successfully. Please activate your email/phone."),
                Data = new ActivatePendingModel { Channels = [.. channels], Targets = [.. targets], ExpireIn = DefaultExpire },
            };
        }
        catch (Exception ex)
        {
            // 激活发送失败（如渠道未配置），回滚已创建的未激活账号，避免产生无法激活的孤儿账号
            try { (user as IEntity)?.Delete(); } catch { }
            span?.SetError(ex, null);
            return new ServiceResult<ActivatePendingModel> { IsSuccess = false, Message = L.T("Registration failed: {0}", ex.Message) };
        }
    }
    #endregion

    #region 激活
    /// <summary>通过邮箱激活链接激活账号</summary>
    /// <param name="token">一次性激活令牌</param>
    /// <param name="account">邮箱</param>
    /// <param name="ip">客户端IP</param>
    /// <returns>激活结果</returns>
    public ServiceResult ActivateByMailToken(String token, String account, String ip)
    {
        using var span = tracer?.NewSpan(nameof(ActivateByMailToken), new { account, ip });

        if (token.IsNullOrEmpty()) return new ServiceResult { IsSuccess = false, Message = L.T("The activation link is invalid.") };
        if (account.IsNullOrEmpty()) return new ServiceResult { IsSuccess = false, Message = L.T("Email is required.") };
        if (!ValidFormatHelper.IsEmail(account)) return new ServiceResult { IsSuccess = false, Message = L.T("Invalid email format.") };

        // 校验一次性令牌
        var key = $"{AuthCacheKeys.MailActivateLinkPrefix}{account}";
        var cached = _cache.Get<String>(key);
        if (cached.IsNullOrEmpty() || !String.Equals(cached, token, StringComparison.Ordinal))
            return new ServiceResult { IsSuccess = false, Message = L.T("The activation link is invalid or has expired. Please resend the activation email.") };

        var user = User.FindByMail(account);
        if (user == null || user.ID <= 0) return new ServiceResult { IsSuccess = false, Message = L.T("User does not exist.") };

        // 幂等：已激活直接成功
        if (user.Enable && user.MailVerified)
            return new ServiceResult { IsSuccess = true, Message = L.T("Account activated.") };

        Activate(user, true, account, "Email activation", ip);

        _cache.Remove(key);
        return new ServiceResult { IsSuccess = true, Message = L.T("Activation succeeded.") };
    }

    /// <summary>通过验证码激活账号（邮箱验证码/手机短信验证码）</summary>
    /// <param name="channel">渠道。mail/sms</param>
    /// <param name="account">邮箱或手机号</param>
    /// <param name="code">验证码</param>
    /// <param name="ip">客户端IP</param>
    /// <returns>激活结果</returns>
    public ServiceResult ActivateByCode(String channel, String account, String code, String ip)
    {
        using var span = tracer?.NewSpan(nameof(ActivateByCode), new { channel, account, ip });

        if (account.IsNullOrEmpty()) return new ServiceResult { IsSuccess = false, Message = L.T("Mobile number or email is required.") };
        if (code.IsNullOrEmpty()) return new ServiceResult { IsSuccess = false, Message = L.T("Verification code is required.") };

        // 按渠道取缓存验证码
        var codeKey = channel.EqualIgnoreCase("Sms")
            ? $"{AuthCacheKeys.SmsActivateCodePrefix}{account}"
            : $"{AuthCacheKeys.MailActivateCodePrefix}{account}";
        var cached = _cache.Get<String>(codeKey);
        if (cached.IsNullOrEmpty()) return new ServiceResult { IsSuccess = false, Message = L.T("Verification code has expired or does not exist. Please request a new one.") };
        if (!cached.EqualIgnoreCase(code)) return new ServiceResult { IsSuccess = false, Message = L.T("Incorrect verification code.") };

        var user = FindByContact(account);
        if (user == null || user.ID <= 0) return new ServiceResult { IsSuccess = false, Message = L.T("User does not exist.") };

        // 幂等：已激活直接成功
        var verified = channel.EqualIgnoreCase("Sms") ? user.MobileVerified : user.MailVerified;
        if (user.Enable && verified)
            return new ServiceResult { IsSuccess = true, Message = L.T("Account activated.") };

        var action = channel.EqualIgnoreCase("Sms") ? "Mobile activation" : "Email activation";
        Activate(user, channel.EqualIgnoreCase("Sms"), account, action, ip);

        _cache.Remove(codeKey);
        return new ServiceResult { IsSuccess = true, Message = L.T("Activation succeeded.") };
    }

    /// <summary>激活账号：Enable=true + 对应联系方式标记已验证</summary>
    /// <param name="user">用户</param>
    /// <param name="isMobile">是否手机激活</param>
    /// <param name="target">目标联系方式</param>
    /// <param name="action">操作说明</param>
    /// <param name="ip">客户端IP</param>
    private static void Activate(User user, Boolean isMobile, String target, String action, String ip)
    {
        user.Enable = true;
        if (isMobile)
            user.MobileVerified = true;
        else
            user.MailVerified = true;
        (user as IEntity).Update();

        LogProvider.Provider.WriteLog(typeof(User), L.T(action), true, L.T("Target: {0}", target), user.ID, user + "", ip);
    }

    /// <summary>重发激活：未激活账号重新发送激活邮件/短信</summary>
    /// <param name="channel">渠道。mail/sms</param>
    /// <param name="account">邮箱或手机号</param>
    /// <param name="httpContext">HTTP上下文（用于拼激活链接）</param>
    /// <param name="ip">客户端IP</param>
    /// <returns>发送结果，Data 为脱敏目标</returns>
    public async Task<ServiceResult<String>> ResendActivation(String channel, String account, HttpContext httpContext, String ip)
    {
        using var span = tracer?.NewSpan(nameof(ResendActivation), new { channel, account, ip });

        if (account.IsNullOrEmpty()) return new ServiceResult<String> { IsSuccess = false, Message = L.T("Mobile number or email is required.") };

        var user = FindByContact(account);
        if (user == null || user.ID <= 0) return new ServiceResult<String> { IsSuccess = false, Message = L.T("Account does not exist.") };
        if (user.Enable) return new ServiceResult<String> { IsSuccess = false, Message = L.T("The account is already activated; no repeated verification is needed.") };

        try
        {
            if (channel.EqualIgnoreCase("Sms") || ValidFormatHelper.IsMobile(account))
            {
                await verifyCode.SendActivateCode("Sms", account, null, ip);
                return new ServiceResult<String> { IsSuccess = true, Message = L.T("Activation SMS sent."), Data = MaskTarget(account) };
            }
            if (channel.EqualIgnoreCase("Mail") || ValidFormatHelper.IsEmail(account))
            {
                await SendMailActivation(account, user.ID, httpContext, ip);
                return new ServiceResult<String> { IsSuccess = true, Message = L.T("Activation email sent."), Data = MaskTarget(account) };
            }

            return new ServiceResult<String> { IsSuccess = false, Message = L.T("Please enter a valid mobile number or email.") };
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            return new ServiceResult<String> { IsSuccess = false, Message = L.T("Send failed: {0}", ex.Message) };
        }
    }
    #endregion

    #region 已登录验证联系方式
    /// <summary>已登录用户验证/更换邮箱或手机。验证码经 POST /Auth/SendCode（action=bind）发送</summary>
    /// <param name="user">当前用户</param>
    /// <param name="channel">渠道。mail/sms</param>
    /// <param name="account">新邮箱或手机号</param>
    /// <param name="code">验证码</param>
    /// <param name="ip">客户端IP</param>
    /// <returns>验证结果，Data 为更新后的验证状态</returns>
    public ServiceResult<VerifyStatusModel> VerifyContact(User user, String channel, String account, String code, String ip)
    {
        using var span = tracer?.NewSpan(nameof(VerifyContact), new { channel, account, ip });

        if (user == null || user.ID <= 0) return new ServiceResult<VerifyStatusModel> { IsSuccess = false, Message = L.T("Please sign in first.") };
        if (account.IsNullOrEmpty()) return new ServiceResult<VerifyStatusModel> { IsSuccess = false, Message = L.T("Mobile number or email is required.") };
        if (code.IsNullOrEmpty()) return new ServiceResult<VerifyStatusModel> { IsSuccess = false, Message = L.T("Verification code is required.") };

        // 校验 bind 验证码（用户通过 SendCode action=bind 获取）
        var codeKey = channel.EqualIgnoreCase("Sms")
            ? $"{AuthCacheKeys.SmsBindCodePrefix}{account}"
            : $"{AuthCacheKeys.MailBindCodePrefix}{account}";
        var cached = _cache.Get<String>(codeKey);
        if (cached.IsNullOrEmpty()) return new ServiceResult<VerifyStatusModel> { IsSuccess = false, Message = L.T("Verification code has expired or does not exist. Please request a new one.") };
        if (!cached.EqualIgnoreCase(code)) return new ServiceResult<VerifyStatusModel> { IsSuccess = false, Message = L.T("Incorrect verification code.") };

        // 目标联系方式不得被其他账号占用
        var entity = User.FindByID(user.ID);
        if (entity == null) return new ServiceResult<VerifyStatusModel> { IsSuccess = false, Message = L.T("User does not exist.") };

        var changed = false;
        if (channel.EqualIgnoreCase("Sms"))
        {
            if (!ValidFormatHelper.IsMobile(account)) return new ServiceResult<VerifyStatusModel> { IsSuccess = false, Message = L.T("Invalid mobile number format.") };
            var other = User.FindByMobile(account);
            if (other != null && other.ID != entity.ID)
                return new ServiceResult<VerifyStatusModel> { IsSuccess = false, Message = L.T("This mobile number is already bound to another account.") };
            if (entity.Mobile != account) { entity.Mobile = account; changed = true; }
            if (!entity.MobileVerified) { entity.MobileVerified = true; changed = true; }
        }
        else
        {
            if (!ValidFormatHelper.IsEmail(account)) return new ServiceResult<VerifyStatusModel> { IsSuccess = false, Message = L.T("Invalid email format.") };
            var other = User.FindByMail(account);
            if (other != null && other.ID != entity.ID)
                return new ServiceResult<VerifyStatusModel> { IsSuccess = false, Message = L.T("This email is already bound to another account.") };
            if (entity.Mail != account) { entity.Mail = account; changed = true; }
            if (!entity.MailVerified) { entity.MailVerified = true; changed = true; }
        }

        if (changed) (entity as IEntity).Update();
        _cache.Remove(codeKey);

        LogProvider.Provider.WriteLog(typeof(User), channel.EqualIgnoreCase("Sms") ? "Verify mobile" : "Verify email", true, L.T("Target: {0}", account), entity.ID, entity + "", ip);

        return new ServiceResult<VerifyStatusModel>
        {
            IsSuccess = true,
            Message = L.T("Verification succeeded."),
            Data = new VerifyStatusModel { MailVerified = entity.MailVerified, MobileVerified = entity.MobileVerified },
        };
    }
    #endregion

    #region 辅助
    /// <summary>发送邮箱激活：生成一次性令牌与激活链接，发送激活邮件（链接+验证码）</summary>
    /// <param name="mail">邮箱</param>
    /// <param name="userId">用户编号</param>
    /// <param name="httpContext">HTTP上下文</param>
    /// <param name="ip">客户端IP</param>
    private async Task SendMailActivation(String mail, Int32 userId, HttpContext httpContext, String ip)
    {
        // 一次性激活令牌，缓存校验，有效期同验证码
        var token = Rand.NextString(32);
        var link = BuildActivateLink(mail, token, httpContext);
        _cache.Set($"{AuthCacheKeys.MailActivateLinkPrefix}{mail}", token, DefaultExpire);

        await verifyCode.SendActivateCode("Mail", mail, link, ip);
    }

    /// <summary>拼邮箱激活链接。优先 CubeSetting.ActivateUrl，为空时按请求 Host 拼接 /activate</summary>
    /// <param name="mail">邮箱</param>
    /// <param name="token">一次性令牌</param>
    /// <param name="httpContext">HTTP上下文</param>
    /// <returns>激活链接</returns>
    private static String BuildActivateLink(String mail, String token, HttpContext httpContext)
    {
        var set = CubeSetting.Current;
        var url = set.ActivateUrl;
        if (url.IsNullOrEmpty())
        {
            var req = httpContext.Request;
            url = $"{req.Scheme}://{req.Host}/activate";
        }

        return $"{url}?token={token}&account={Uri.EscapeDataString(mail)}";
    }

    /// <summary>按邮箱或手机号查找用户</summary>
    /// <param name="account">邮箱或手机号</param>
    /// <returns>用户，未找到返回 null</returns>
    private static User FindByContact(String account)
    {
        if (ValidFormatHelper.IsEmail(account)) return User.FindByMail(account);
        if (ValidFormatHelper.IsMobile(account)) return User.FindByMobile(account);
        return null;
    }

    /// <summary>脱敏联系方式：邮箱 zhangsan@e.com → zh***n@e.com；手机 13800138000 → 138****8000</summary>
    /// <param name="target">邮箱或手机号</param>
    /// <returns>脱敏后的联系方式</returns>
    internal static String MaskTarget(String target)
    {
        if (target.IsNullOrEmpty()) return target;

        if (ValidFormatHelper.IsEmail(target))
        {
            var at = target.IndexOf('@');
            if (at <= 0) return target;

            // 本地部分至少保留首尾各 1 个字符；过短（≤2 字符）时整体脱敏，仅保留 @域名后缀
            if (at > 2)
                return target[..1] + "***" + target[(at - 1)..];
            return "***" + target[at..];
        }

        if (target.Length == 11)
            return target[..3] + "****" + target[7..];
        if (target.Length >= 8)
            return target[..3] + "****" + target[^2..];

        return target[..1] + "****";
    }
    #endregion
}

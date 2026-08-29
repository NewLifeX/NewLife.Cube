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
/// <param name="tenantContext">租户上下文</param>
public class AccountActivateService(UserService userService, VerifyCodeService verifyCode, ICacheProvider cacheProvider, ITracer tracer, ITenantContext tenantContext)
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
            return new ServiceResult<ActivatePendingModel> { IsSuccess = false, Message = "需要邮箱验证，请填写邮箱" };
        if (requireMobile && mobile.IsNullOrEmpty())
            return new ServiceResult<ActivatePendingModel> { IsSuccess = false, Message = "需要手机验证，请填写手机号" };
        if (!email.IsNullOrEmpty() && !ValidFormatHelper.IsEmail(email))
            return new ServiceResult<ActivatePendingModel> { IsSuccess = false, Message = "邮箱格式不正确" };
        if (!mobile.IsNullOrEmpty() && !ValidFormatHelper.IsMobile(mobile))
            return new ServiceResult<ActivatePendingModel> { IsSuccess = false, Message = "手机号格式不正确" };

        // 密码可选：留空则生成随机密码（激活后无法密码登录，仅验证码登录，登录后可再设置密码）
        var pwd = userService.ResolveRegisterPassword(model);
        if (!pwd.IsSuccess) return new ServiceResult<ActivatePendingModel> { IsSuccess = false, Message = pwd.Message };

        // 手机/邮箱验证码注册需先通过注册验证码（激活码独立于注册码）
        if (model.Category == AuthCategory.Mobile)
        {
            var codeKey = $"{AuthCacheKeys.SmsRegisterCodePrefix}{mobile}";
            var cached = _cache.Get<String>(codeKey);
            if (cached.IsNullOrEmpty() || !cached.EqualIgnoreCase(model.Code))
                return new ServiceResult<ActivatePendingModel> { IsSuccess = false, Message = "注册验证码错误或已过期，请重新获取" };
            _cache.Remove(codeKey);
        }
        else if (model.Category == AuthCategory.Mail)
        {
            var codeKey = $"{AuthCacheKeys.MailRegisterCodePrefix}{email}";
            var cached = _cache.Get<String>(codeKey);
            if (cached.IsNullOrEmpty() || !cached.EqualIgnoreCase(model.Code))
                return new ServiceResult<ActivatePendingModel> { IsSuccess = false, Message = "注册验证码错误或已过期，请重新获取" };
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
            return new ServiceResult<ActivatePendingModel> { IsSuccess = false, Message = "用户名不能为空" };

        var duplicate = userService.CheckDuplicate(username, email, mobile);
        if (!duplicate.IsSuccess) return new ServiceResult<ActivatePendingModel> { IsSuccess = false, Message = duplicate.Message };

        // 创建未激活账号（Enable=false），激活后置 true 方可登录
        var user = userService.CreateUserAndBindContact(username, pwd.Data, email, mobile, ip, false, false, false);
        if (user == null || user.ID <= 0)
            return new ServiceResult<ActivatePendingModel> { IsSuccess = false, Message = "注册失败，请稍后重试" };

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

            LogProvider.Provider.WriteLog(typeof(User), "注册待激活", true, $"邮箱：{email} 手机：{mobile}", user.ID, user + "", ip);

            return new ServiceResult<ActivatePendingModel>
            {
                IsSuccess = true,
                Message = "注册成功，请激活邮箱/手机",
                Data = new ActivatePendingModel { Channels = [.. channels], Targets = [.. targets], ExpireIn = DefaultExpire },
            };
        }
        catch (Exception ex)
        {
            // 激活发送失败（如渠道未配置），回滚已创建的未激活账号，避免产生无法激活的孤儿账号
            try { (user as IEntity)?.Delete(); } catch { }
            span?.SetError(ex, null);
            return new ServiceResult<ActivatePendingModel> { IsSuccess = false, Message = $"注册失败：{ex.Message}" };
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

        if (token.IsNullOrEmpty()) return new ServiceResult { IsSuccess = false, Message = "激活链接无效" };
        if (account.IsNullOrEmpty()) return new ServiceResult { IsSuccess = false, Message = "邮箱不能为空" };
        if (!ValidFormatHelper.IsEmail(account)) return new ServiceResult { IsSuccess = false, Message = "邮箱格式不正确" };

        // 校验一次性令牌
        var key = $"{AuthCacheKeys.MailActivateLinkPrefix}{account}";
        var cached = _cache.Get<String>(key);
        if (cached.IsNullOrEmpty() || !String.Equals(cached, token, StringComparison.Ordinal))
            return new ServiceResult { IsSuccess = false, Message = "激活链接无效或已过期，请重新发送激活邮件" };

        var user = User.FindByMail(account);
        if (user == null || user.ID <= 0) return new ServiceResult { IsSuccess = false, Message = "用户不存在" };

        // 幂等：已激活直接成功
        if (user.Enable && user.MailVerified)
            return new ServiceResult { IsSuccess = true, Message = "账号已激活" };

        Activate(user, true, account, "邮箱激活", ip);

        _cache.Remove(key);
        return new ServiceResult { IsSuccess = true, Message = "激活成功" };
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

        if (account.IsNullOrEmpty()) return new ServiceResult { IsSuccess = false, Message = "邮箱/手机号不能为空" };
        if (code.IsNullOrEmpty()) return new ServiceResult { IsSuccess = false, Message = "验证码不能为空" };

        // 按渠道取缓存验证码
        var codeKey = channel.EqualIgnoreCase("Sms")
            ? $"{AuthCacheKeys.SmsActivateCodePrefix}{account}"
            : $"{AuthCacheKeys.MailActivateCodePrefix}{account}";
        var cached = _cache.Get<String>(codeKey);
        if (cached.IsNullOrEmpty()) return new ServiceResult { IsSuccess = false, Message = "验证码已过期或不存在，请重新获取" };
        if (!cached.EqualIgnoreCase(code)) return new ServiceResult { IsSuccess = false, Message = "验证码错误" };

        var user = FindByContact(account);
        if (user == null || user.ID <= 0) return new ServiceResult { IsSuccess = false, Message = "用户不存在" };

        // 幂等：已激活直接成功
        var verified = channel.EqualIgnoreCase("Sms") ? user.MobileVerified : user.MailVerified;
        if (user.Enable && verified)
            return new ServiceResult { IsSuccess = true, Message = "账号已激活" };

        var action = channel.EqualIgnoreCase("Sms") ? "手机激活" : "邮箱激活";
        Activate(user, channel.EqualIgnoreCase("Sms"), account, action, ip);

        _cache.Remove(codeKey);
        return new ServiceResult { IsSuccess = true, Message = "激活成功" };
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

        LogProvider.Provider.WriteLog(typeof(User), action, true, $"目标：{target}", user.ID, user + "", ip);
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

        if (account.IsNullOrEmpty()) return new ServiceResult<String> { IsSuccess = false, Message = "邮箱/手机号不能为空" };

        var user = FindByContact(account);
        if (user == null || user.ID <= 0) return new ServiceResult<String> { IsSuccess = false, Message = "账号不存在" };
        if (user.Enable) return new ServiceResult<String> { IsSuccess = false, Message = "账号已激活，无需重复验证" };

        try
        {
            if (channel.EqualIgnoreCase("Sms") || ValidFormatHelper.IsMobile(account))
            {
                await verifyCode.SendActivateCode("Sms", account, null, ip);
                return new ServiceResult<String> { IsSuccess = true, Message = "激活短信已发送", Data = MaskTarget(account) };
            }
            if (channel.EqualIgnoreCase("Mail") || ValidFormatHelper.IsEmail(account))
            {
                await SendMailActivation(account, user.ID, httpContext, ip);
                return new ServiceResult<String> { IsSuccess = true, Message = "激活邮件已发送", Data = MaskTarget(account) };
            }

            return new ServiceResult<String> { IsSuccess = false, Message = "请输入正确的手机号或邮箱" };
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            return new ServiceResult<String> { IsSuccess = false, Message = $"发送失败：{ex.Message}" };
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

        if (user == null || user.ID <= 0) return new ServiceResult<VerifyStatusModel> { IsSuccess = false, Message = "用户未登录，请先登录" };
        if (account.IsNullOrEmpty()) return new ServiceResult<VerifyStatusModel> { IsSuccess = false, Message = "邮箱/手机号不能为空" };
        if (code.IsNullOrEmpty()) return new ServiceResult<VerifyStatusModel> { IsSuccess = false, Message = "验证码不能为空" };

        // 校验 bind 验证码（用户通过 SendCode action=bind 获取）
        var codeKey = channel.EqualIgnoreCase("Sms")
            ? $"{AuthCacheKeys.SmsBindCodePrefix}{account}"
            : $"{AuthCacheKeys.MailBindCodePrefix}{account}";
        var cached = _cache.Get<String>(codeKey);
        if (cached.IsNullOrEmpty()) return new ServiceResult<VerifyStatusModel> { IsSuccess = false, Message = "验证码已过期或不存在，请重新获取" };
        if (!cached.EqualIgnoreCase(code)) return new ServiceResult<VerifyStatusModel> { IsSuccess = false, Message = "验证码错误" };

        // 目标联系方式不得被其他账号占用
        var entity = User.FindByID(user.ID);
        if (entity == null) return new ServiceResult<VerifyStatusModel> { IsSuccess = false, Message = "用户不存在" };

        var changed = false;
        if (channel.EqualIgnoreCase("Sms"))
        {
            if (!ValidFormatHelper.IsMobile(account)) return new ServiceResult<VerifyStatusModel> { IsSuccess = false, Message = "手机号格式不正确" };
            var other = User.FindByMobile(account);
            if (other != null && other.ID != entity.ID)
                return new ServiceResult<VerifyStatusModel> { IsSuccess = false, Message = "该手机号已被其他账户绑定" };
            if (entity.Mobile != account) { entity.Mobile = account; changed = true; }
            if (!entity.MobileVerified) { entity.MobileVerified = true; changed = true; }
        }
        else
        {
            if (!ValidFormatHelper.IsEmail(account)) return new ServiceResult<VerifyStatusModel> { IsSuccess = false, Message = "邮箱格式不正确" };
            var other = User.FindByMail(account);
            if (other != null && other.ID != entity.ID)
                return new ServiceResult<VerifyStatusModel> { IsSuccess = false, Message = "该邮箱已被其他账户绑定" };
            if (entity.Mail != account) { entity.Mail = account; changed = true; }
            if (!entity.MailVerified) { entity.MailVerified = true; changed = true; }
        }

        if (changed) (entity as IEntity).Update();
        _cache.Remove(codeKey);

        LogProvider.Provider.WriteLog(typeof(User), channel.EqualIgnoreCase("Sms") ? "验证手机" : "验证邮箱", true, $"目标：{account}", entity.ID, entity + "", ip);

        return new ServiceResult<VerifyStatusModel>
        {
            IsSuccess = true,
            Message = "验证成功",
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

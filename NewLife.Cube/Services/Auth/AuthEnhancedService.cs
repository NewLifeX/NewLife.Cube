using Microsoft.AspNetCore.Http;
using NewLife.Caching;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Common;
using NewLife.Cube.Enums;
using NewLife.Cube.Models;
using NewLife.Log;
using NewLife.Model;
using NewLife.Security;
using NewLife.Web;
using XCode;
using XCode.Membership;
using HttpContext = Microsoft.AspNetCore.Http.HttpContext;

namespace NewLife.Cube.Services;

/// <summary>增强认证服务。验证码登录/注册/找回/绑定等能力，供用户中心（API版）使用；MVC精简版不编译本类</summary>
/// <param name="userService">基础用户服务</param>
/// <param name="verifyCodeService">验证码服务</param>
/// <param name="cacheProvider">缓存提供者</param>
/// <param name="passwordService">密码服务</param>
/// <param name="tracer">追踪器</param>
/// <param name="tenantContext">租户上下文</param>
public class AuthEnhancedService(UserService userService, VerifyCodeService verifyCodeService, ICacheProvider cacheProvider, PasswordService passwordService, ITracer tracer, ITenantContext tenantContext)
{
    #region 属性
    private readonly UserService _userService = userService;
    private readonly VerifyCodeService _verifyCode = verifyCodeService;
    private readonly ICache _cache = cacheProvider.Cache;
    private readonly PasswordService _passwordService = passwordService;
    private readonly ITenantContext _tenantContext = tenantContext;
    #endregion

    #region 登录
    /// <summary>统一登录入口，支持账号密码、手机验证码、邮箱验证码登录。密码登录委托基础服务，验证码登录在本类实现</summary>
    /// <param name="loginModel">登录模型</param>
    /// <param name="httpContext">HTTP上下文</param>
    /// <returns>登录结果，包含Token信息或错误信息</returns>
    public ServiceResult<IToken> Login(LoginModel loginModel, HttpContext httpContext)
    {
        // 图片验证码校验（场景强制 CaptchaScene 或 风险自适应 CaptchaRisk）
        var ip = httpContext.GetUserHost();
        if (_verifyCode.RequireCaptcha(1, ip, loginModel.Username, AuthHelper.GetDeviceId(httpContext)))
        {
            if (!_verifyCode.ValidateCaptcha(loginModel.CaptchaId, loginModel.CaptchaCode))
                return new ServiceResult<IToken> { IsSuccess = false, Message = "验证码错误或已过期，请刷新后重试" };
        }

        switch (loginModel.Category)//登录方式
        {
            case AuthCategory.Mobile://手机验证码登录
                {
                    return !ValidFormatHelper.IsMobile(loginModel.Username)
                        ? new ServiceResult<IToken> { IsSuccess = false, Message = "手机号码格式不正确" }
                        : LoginBySms(loginModel, httpContext);
                }
            case AuthCategory.Mail://邮箱验证码登录
                {
                    return !ValidFormatHelper.IsEmail(loginModel.Username)
                        ? new ServiceResult<IToken> { IsSuccess = false, Message = "邮箱格式不正确" }
                        : LoginByMail(loginModel, httpContext);
                }
            case AuthCategory.OAuth:
            case AuthCategory.Password:
            default:
                return _userService.LoginByPassword(loginModel, httpContext);
        }
    }

    /// <summary>手机验证码登录</summary>
    /// <remarks>验证并返回Token</remarks>
    private ServiceResult<IToken> LoginBySms(LoginModel loginModel, HttpContext httpContext)
    {
        var mobile = loginModel.Username?.Trim() ?? "";
        var code = loginModel.Password?.Trim() ?? "";
        var remember = loginModel.Remember;
        var ip = httpContext.GetUserHost();
        using var span = tracer?.NewSpan(nameof(LoginBySms), new { mobile, ip });

        if (mobile.IsNullOrEmpty()) throw new ArgumentNullException(nameof(mobile), "手机号不能为空");
        if (!ValidFormatHelper.IsMobile(mobile)) throw new XException("手机号格式不正确");
        if (code.IsNullOrEmpty()) throw new ArgumentNullException(nameof(code), "验证码不能为空");

        var key = $"{AuthCacheKeys.SmsLoginErrorPrefix}{mobile}";
        var errors = _cache.Get<Int32>(key);
        var ipKey = $"{AuthCacheKeys.SmsLoginErrorIpPrefix}{ip}";
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
            // 错误次数检查
            if (errors >= set.MaxLoginError && set.MaxLoginError > 0)
                throw new InvalidOperationException($"[{mobile}]验证错误过多，请在{set.LoginForbiddenTime}秒后再试！");
            if (ipErrors >= set.MaxLoginError && set.MaxLoginError > 0)
                throw new InvalidOperationException($"IP地址[{ip}]验证错误过多，请在{set.LoginForbiddenTime}秒后再试！");
            if (!ip24Key.IsNullOrEmpty() && ip24Errors >= set.MaxLoginErrorBySubnet24 && set.MaxLoginErrorBySubnet24 > 0)
                throw new InvalidOperationException($"IP段[{ip24}.*]验证错误过多，请在{set.LoginForbiddenTime}秒后再试！");
            if (!ip16Key.IsNullOrEmpty() && ip16Errors >= set.MaxLoginErrorBySubnet16 && set.MaxLoginErrorBySubnet16 > 0)
                throw new InvalidOperationException($"IP段[{ip16}.*.*]验证错误过多，请在{set.LoginForbiddenTime}秒后再试！");

            // 校验验证码
            var codeKey = $"{AuthCacheKeys.SmsLoginCodePrefix}{mobile}";
            var cachedCode = _cache.Get<String>(codeKey);
            if (cachedCode.IsNullOrEmpty()) throw new InvalidOperationException("验证码已过期，请重新获取");
            if (!cachedCode.EqualIgnoreCase(code)) throw new InvalidOperationException("验证码错误");

            // 验证通过，移除验证码缓存
            _cache.Remove(codeKey);

            // 查找用户（按手机号）
            var user = User.FindByMobile(mobile);
            if (user == null)
            {
                // 自动注册
                if (!set.AutoRegister) throw new InvalidOperationException("用户不存在，且未开启自动注册");

                user = new User
                {
                    Name = $"P{mobile}",//添加一个P（phone）前缀，区分登录方式
                    DisplayName = $"手机用户{mobile[^4..]}",
                    Mobile = mobile,
                    Enable = true,
                    MobileVerified = true,
                };

                // 设置默认角色
                if (!set.DefaultRole.IsNullOrEmpty())
                {
                    var role = Role.FindByName(set.DefaultRole);
                    if (role != null) user.RoleID = role.ID;
                }

                user.RegisterIP = ip;
                user.RegisterTime = DateTime.Now;
                user.Insert();

                LogProvider.Provider.WriteLog(typeof(User), "短信注册", true, $"手机号：{mobile} 自动注册", user.ID, user + "", ip);
            }

            if (!user.Enable) throw new InvalidOperationException("用户已禁用");

            // 验证通过，执行登录
            var provider = ManageProvider.Provider;
            provider.Current = user;

            // 清空错误计数
            if (errors > 0) _cache.Remove(key);
            if (ipErrors > 0) _cache.Remove(ipKey);

            return _userService.CompleteLogin(user, httpContext, remember, "短信登录", mobile, ip);
        }
        catch (Exception ex)
        {
            _userService.HandleLoginError(ex, "短信登录", mobile, ip, key, ipKey, errors, ipErrors, ip24Key, ip24Errors, ip16Key, ip16Errors, set.LoginForbiddenTime);
            throw;
        }
    }

    /// <summary>邮箱验证码登录</summary>
    /// <remarks>验证并返回Token</remarks>
    private ServiceResult<IToken> LoginByMail(LoginModel loginModel, HttpContext httpContext)
    {
        var mail = loginModel.Username?.Trim() ?? "";
        var code = loginModel.Password?.Trim() ?? "";
        var remember = loginModel.Remember;
        var ip = httpContext.GetUserHost();
        using var span = tracer?.NewSpan(nameof(LoginByMail), new { mail, ip });

        if (mail.IsNullOrEmpty()) throw new ArgumentNullException(nameof(mail), "邮箱不能为空");
        if (!ValidFormatHelper.IsEmail(mail)) throw new XException("邮箱格式不正确");
        if (code.IsNullOrEmpty()) throw new ArgumentNullException(nameof(code), "验证码不能为空");

        var key = $"{AuthCacheKeys.MailLoginErrorPrefix}{mail}";
        var errors = _cache.Get<Int32>(key);
        var ipKey = $"{AuthCacheKeys.MailLoginErrorIpPrefix}{ip}";
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
            // 错误次数检查
            if (errors >= set.MaxLoginError && set.MaxLoginError > 0)
                throw new InvalidOperationException($"[{mail}]验证错误过多，请在{set.LoginForbiddenTime}秒后再试！");
            if (ipErrors >= set.MaxLoginError && set.MaxLoginError > 0)
                throw new InvalidOperationException($"IP地址[{ip}]验证错误过多，请在{set.LoginForbiddenTime}秒后再试！");
            if (!ip24Key.IsNullOrEmpty() && ip24Errors >= set.MaxLoginErrorBySubnet24 && set.MaxLoginErrorBySubnet24 > 0)
                throw new InvalidOperationException($"IP段[{ip24}.*]验证错误过多，请在{set.LoginForbiddenTime}秒后再试！");
            if (!ip16Key.IsNullOrEmpty() && ip16Errors >= set.MaxLoginErrorBySubnet16 && set.MaxLoginErrorBySubnet16 > 0)
                throw new InvalidOperationException($"IP段[{ip16}.*.*]验证错误过多，请在{set.LoginForbiddenTime}秒后再试！");

            // 校验验证码
            var codeKey = $"{AuthCacheKeys.MailLoginCodePrefix}{mail}";
            var cachedCode = _cache.Get<String>(codeKey);
            if (cachedCode.IsNullOrEmpty()) throw new InvalidOperationException("验证码已过期，请重新获取");
            if (!cachedCode.EqualIgnoreCase(code)) throw new InvalidOperationException("验证码错误");

            // 验证通过，移除验证码缓存
            _cache.Remove(codeKey);

            // 查找用户（按邮箱）
            var user = User.FindByMail(mail);
            if (user == null)
            {
                // 自动注册
                if (!set.AutoRegister) throw new InvalidOperationException("用户不存在，且未开启自动注册");

                user = new User
                {
                    Name = mail.Split('@')[0],
                    DisplayName = $"邮箱用户",
                    Mail = mail,
                    Enable = true,
                    MailVerified = true,
                };

                // 设置默认角色
                if (!set.DefaultRole.IsNullOrEmpty())
                {
                    var role = Role.FindByName(set.DefaultRole);
                    if (role != null) user.RoleID = role.ID;
                }

                user.RegisterIP = ip;
                user.RegisterTime = DateTime.Now;
                user.Insert();

                LogProvider.Provider.WriteLog(typeof(User), "邮箱注册", true, $"邮箱：{mail} 自动注册", user.ID, user + "", ip);
            }

            if (!user.Enable) throw new InvalidOperationException("用户已禁用");

            // 验证通过，执行登录
            var provider = ManageProvider.Provider;
            provider.Current = user;

            // 清空错误计数
            if (errors > 0) _cache.Remove(key);
            if (ipErrors > 0) _cache.Remove(ipKey);

            return _userService.CompleteLogin(user, httpContext, remember, "邮箱登录", mail, ip);
        }
        catch (Exception ex)
        {
            _userService.HandleLoginError(ex, "邮箱登录", mail, ip, key, ipKey, errors, ipErrors, ip24Key, ip24Errors, ip16Key, ip16Errors, set.LoginForbiddenTime);
            throw;
        }
    }
    #endregion

    #region 注册
    /// <summary>统一注册入口，支持用户名密码、手机验证码、邮箱验证码注册。用户名/OAuth注册委托基础服务，验证码注册在本类实现</summary>
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

        // 图片验证码校验（场景强制 CaptchaScene 或 风险自适应 CaptchaRisk）
        var regIp = httpContext.GetUserHost();
        if (_verifyCode.RequireCaptcha(2, regIp, model?.Username, AuthHelper.GetDeviceId(httpContext)))
        {
            if (!_verifyCode.ValidateCaptcha(model.CaptchaId, model.CaptchaCode))
                return new ServiceResult<IToken> { IsSuccess = false, Message = "验证码错误或已过期，请刷新后重试" };
        }

        var ip = httpContext.GetUserHost();
        using var span = tracer?.NewSpan(nameof(Register), new { model.Category, model.Username, model.Mobile, model.Email, ip });

        try
        {
            return model.Category switch
            {
                AuthCategory.Mobile => RegisterByPhoneCode(model, httpContext, ip),
                AuthCategory.Mail => RegisterByMailCode(model, httpContext, ip),
                AuthCategory.OAuth => _userService.RegisterByOAuthBind(model, httpContext, ip),
                _ => _userService.RegisterByPassword(model, httpContext, ip),
            };
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            return new ServiceResult<IToken> { IsSuccess = false, Message = ex.Message };
        }
    }

    private ServiceResult<IToken> RegisterByPhoneCode(AuthRegisterModel model, HttpContext httpContext, String ip)
    {
        var mobile = (model.Mobile ?? model.Username)?.Trim();
        if (mobile.IsNullOrEmpty()) return new ServiceResult<IToken> { IsSuccess = false, Message = "手机号不能为空" };
        if (!ValidFormatHelper.IsMobile(mobile)) return new ServiceResult<IToken> { IsSuccess = false, Message = "手机号格式不正确" };
        if (model.Code.IsNullOrEmpty()) return new ServiceResult<IToken> { IsSuccess = false, Message = "验证码不能为空" };

        // 密码可选：留空则生成随机密码（无法密码登录，仅验证码登录，登录后可再设置密码）
        var pwd = _userService.ResolveRegisterPassword(model);
        if (!pwd.IsSuccess) return new ServiceResult<IToken> { IsSuccess = false, Message = pwd.Message };

        var set = CubeSetting.Current;
        if (!set.EnableSms) return new ServiceResult<IToken> { IsSuccess = false, Message = "短信验证码功能未启用" };

        var codeKey = $"{AuthCacheKeys.SmsRegisterCodePrefix}{mobile}";
        var cachedCode = _cache.Get<String>(codeKey);
        if (cachedCode.IsNullOrEmpty()) return new ServiceResult<IToken> { IsSuccess = false, Message = "验证码已过期或不存在，请重新获取" };
        if (!cachedCode.EqualIgnoreCase(model.Code)) return new ServiceResult<IToken> { IsSuccess = false, Message = "验证码错误" };

        var username = model.Username?.Trim();
        if (username.IsNullOrEmpty()) username = mobile;

        var duplicate = _userService.CheckDuplicate(username, model.Email?.Trim(), mobile);
        if (!duplicate.IsSuccess) return new ServiceResult<IToken> { IsSuccess = false, Message = duplicate.Message };

        var user = _userService.CreateUserAndBindContact(username, pwd.Data, model.Email?.Trim(), mobile, ip, false, true);

        _cache.Remove(codeKey);
        LogProvider.Provider.WriteLog(typeof(User), "手机注册", true, $"手机号：{mobile}", user.ID, user + "", ip);

        return _userService.CompleteLogin(user, httpContext, false, "手机注册", username, ip);
    }

    private ServiceResult<IToken> RegisterByMailCode(AuthRegisterModel model, HttpContext httpContext, String ip)
    {
        var mail = (model.Email ?? model.Username)?.Trim();
        if (mail.IsNullOrEmpty()) return new ServiceResult<IToken> { IsSuccess = false, Message = "邮箱不能为空" };
        if (!ValidFormatHelper.IsEmail(mail)) return new ServiceResult<IToken> { IsSuccess = false, Message = "邮箱格式不正确" };
        if (model.Code.IsNullOrEmpty()) return new ServiceResult<IToken> { IsSuccess = false, Message = "验证码不能为空" };

        // 密码可选：留空则生成随机密码（无法密码登录，仅验证码登录，登录后可再设置密码）
        var pwd = _userService.ResolveRegisterPassword(model);
        if (!pwd.IsSuccess) return new ServiceResult<IToken> { IsSuccess = false, Message = pwd.Message };

        var set = CubeSetting.Current;
        if (!set.EnableMail) return new ServiceResult<IToken> { IsSuccess = false, Message = "邮件验证码功能未启用" };

        var codeKey = $"{AuthCacheKeys.MailRegisterCodePrefix}{mail}";
        var cachedCode = _cache.Get<String>(codeKey);
        if (cachedCode.IsNullOrEmpty()) return new ServiceResult<IToken> { IsSuccess = false, Message = "验证码已过期或不存在，请重新获取" };
        if (!cachedCode.EqualIgnoreCase(model.Code)) return new ServiceResult<IToken> { IsSuccess = false, Message = "验证码错误" };

        var username = model.Username?.Trim();
        if (username.IsNullOrEmpty()) username = mail;

        var duplicate = _userService.CheckDuplicate(username, mail, model.Mobile?.Trim());
        if (!duplicate.IsSuccess) return new ServiceResult<IToken> { IsSuccess = false, Message = duplicate.Message };

        var user = _userService.CreateUserAndBindContact(username, pwd.Data, mail, model.Mobile?.Trim(), ip, true, false);

        _cache.Remove(codeKey);
        LogProvider.Provider.WriteLog(typeof(User), "邮箱注册", true, $"邮箱：{mail}", user.ID, user + "", ip);

        return _userService.CompleteLogin(user, httpContext, false, "邮箱注册", username, ip);
    }
    #endregion

    #region 绑定账号
    /// <summary>绑定手机号或邮箱到当前登录用户</summary>
    /// <param name="account">手机号或邮箱</param>
    /// <param name="code">验证码</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="ip">客户端IP</param>
    /// <returns>绑定结果</returns>
    public ServiceResult BindByVerifyCode(String account, String code, IUser currentUser, String ip)
    {
        using var span = tracer?.NewSpan(nameof(BindByVerifyCode), new { account, ip });

        // 1. 公共参数校验
        if (account.IsNullOrEmpty())
            return new ServiceResult { IsSuccess = false, Message = "手机号或邮箱不能为空" };

        if (code.IsNullOrEmpty())
            return new ServiceResult { IsSuccess = false, Message = "验证码不能为空" };

        if (currentUser == null || currentUser.ID <= 0)
            return new ServiceResult { IsSuccess = false, Message = "用户未登录，请先登录" };

        // 2. 根据账号类型分发到具体处理方法
        if (ValidFormatHelper.IsEmail(account))
            return BindByMailCode(account, code, currentUser, ip);

        if (ValidFormatHelper.IsMobile(account))
            return BindBySmsCode(account, code, currentUser, ip);

        return new ServiceResult { IsSuccess = false, Message = "请输入正确的手机号或邮箱" };
    }

    /// <summary>通过短信验证码绑定手机号</summary>
    private ServiceResult BindBySmsCode(String mobile, String code, IUser currentUser, String ip)
    {
        var set = CubeSetting.Current;
        if (!set.EnableSms) return new ServiceResult { IsSuccess = false, Message = "短信验证码功能未启用" };

        // 验证验证码
        var codeKey = $"{AuthCacheKeys.SmsBindCodePrefix}{mobile}";
        var cachedCode = _cache.Get<String>(codeKey);
        if (cachedCode.IsNullOrEmpty()) return new ServiceResult { IsSuccess = false, Message = "验证码已过期或不存在，请重新获取" };
        if (!cachedCode.EqualIgnoreCase(code)) return new ServiceResult { IsSuccess = false, Message = "验证码错误" };

        // 检查手机号是否已被其他用户绑定
        var existingUser = User.FindByMobile(mobile);
        if (existingUser != null && existingUser.ID > 0 && existingUser.ID != currentUser.ID)
            return new ServiceResult { IsSuccess = false, Message = "该手机号已被其他账户绑定" };

        // 绑定到当前用户
        var user = User.FindByID(currentUser.ID);
        if (user == null) return new ServiceResult { IsSuccess = false, Message = "用户不存在" };

        if (user.Mobile != mobile)
        {
            user.Mobile = mobile;
            user.MobileVerified = true;
            var updated = user.Update();
            if (updated <= 0) return new ServiceResult { IsSuccess = false, Message = "绑定失败，请重试" };
        }

        // 验证成功后删除缓存验证码，防止重复使用
        _cache.Remove(codeKey);

        LogProvider.Provider.WriteLog(typeof(User), "绑定手机", true, $"手机号：{mobile}", currentUser.ID, currentUser + "", ip);

        return new ServiceResult { IsSuccess = true, Message = "手机号绑定成功" };
    }

    /// <summary>通过邮件验证码绑定邮箱</summary>
    private ServiceResult BindByMailCode(String mail, String code, IUser currentUser, String ip)
    {
        var set = CubeSetting.Current;
        if (!set.EnableMail) return new ServiceResult { IsSuccess = false, Message = "邮件验证码功能未启用" };

        // 验证验证码
        var codeKey = $"{AuthCacheKeys.MailBindCodePrefix}{mail}";
        var cachedCode = _cache.Get<String>(codeKey);
        if (cachedCode.IsNullOrEmpty()) return new ServiceResult { IsSuccess = false, Message = "验证码已过期或不存在，请重新获取" };
        if (!cachedCode.EqualIgnoreCase(code)) return new ServiceResult { IsSuccess = false, Message = "验证码错误" };

        // 检查邮箱是否已被其他用户绑定
        var existingUser = User.FindByMail(mail);
        if (existingUser != null && existingUser.ID > 0 && existingUser.ID != currentUser.ID)
            return new ServiceResult { IsSuccess = false, Message = "该邮箱已被其他账户绑定" };

        // 绑定到当前用户
        var user = User.FindByID(currentUser.ID);
        if (user == null) return new ServiceResult { IsSuccess = false, Message = "用户不存在" };

        if (user.Mail != mail)
        {
            user.Mail = mail;
            user.MailVerified = true;
            var updated = user.Update();
            if (updated <= 0) return new ServiceResult { IsSuccess = false, Message = "绑定失败，请重试" };
        }

        // 验证成功后删除缓存验证码，防止重复使用
        _cache.Remove(codeKey);

        LogProvider.Provider.WriteLog(typeof(User), "绑定邮箱", true, $"邮箱：{mail}", currentUser.ID, currentUser + "", ip);

        return new ServiceResult { IsSuccess = true, Message = "邮箱绑定成功" };
    }
    #endregion

    #region 重置密码
    /// <summary>通过手机或邮箱验证码重置密码</summary>
    /// <param name="account">手机号或邮箱</param>
    /// <param name="code">验证码</param>
    /// <param name="newPassword">新密码</param>
    /// <param name="confirmPassword">确认密码</param>
    /// <param name="challengeId">挑战标识</param>
    /// <param name="ip">客户端IP</param>
    /// <returns>重置结果</returns>
    public ServiceResult ResetPassword(String account, String code, String newPassword, String confirmPassword, String challengeId, String ip)
    {
        using var span = tracer?.NewSpan(nameof(ResetPassword), new { account, ip });

        // 1. 公共参数校验
        if (account.IsNullOrEmpty()) return new ServiceResult { IsSuccess = false, Message = "手机号或邮箱不能为空" };
        if (code.IsNullOrEmpty()) return new ServiceResult { IsSuccess = false, Message = "验证码不能为空" };
        if (newPassword.IsNullOrEmpty()) return new ServiceResult { IsSuccess = false, Message = "新密码不能为空" };

        var set = CubeSetting.Current;
        // 安全重置检查：若禁止明文密码且未携带挑战标识，则拒绝
        if (challengeId.IsNullOrEmpty() && !set.AllowPlainPassword)
            return new ServiceResult { IsSuccess = false, Message = "禁止明文传输密码，请先获取公钥加密" };

        // 挑战解密：携带 challengeId 时必须能取到私钥；取不到（过期/伪造）时明确报错，禁止把密文当作明文新密码继续落库
        if (!challengeId.IsNullOrEmpty())
        {
            var pdic = _cache.Get<Tuple<String, String>>(challengeId);
            var rsaKey = pdic?.Item2;
            if (rsaKey.IsNullOrEmpty())
                return new ServiceResult { IsSuccess = false, Message = "登录挑战已过期或无效，请重新获取公钥后重试" };

            newPassword = UserService.DecryptByPrivateKey(rsaKey, newPassword);
            if (!confirmPassword.IsNullOrEmpty())
                confirmPassword = UserService.DecryptByPrivateKey(rsaKey, confirmPassword);

            // 移除挑战私钥信息，避免重放
            _cache.Remove(challengeId);
        }

        if (!confirmPassword.IsNullOrEmpty() && newPassword != confirmPassword)
            return new ServiceResult { IsSuccess = false, Message = "两次输入密码不一致" };
        if (!_passwordService.Valid(newPassword)) return new ServiceResult { IsSuccess = false, Message = "密码太弱" };

        // 2. 根据账号类型分发到具体处理方法
        if (ValidFormatHelper.IsEmail(account))
            return ResetByMailCode(account, code, newPassword, ip);

        if (ValidFormatHelper.IsMobile(account))
            return ResetBySmsCode(account, code, newPassword, ip);

        return new ServiceResult { IsSuccess = false, Message = "请输入正确的手机号或邮箱" };
    }

    /// <summary>通过短信验证码重置密码</summary>
    private ServiceResult ResetBySmsCode(String mobile, String code, String newPassword, String ip)
    {
        var set = CubeSetting.Current;
        if (!set.EnableSms) return new ServiceResult { IsSuccess = false, Message = "短信验证码功能未启用" };

        // 验证验证码
        var codeKey = $"{AuthCacheKeys.SmsResetCodePrefix}{mobile}";
        var cachedCode = _cache.Get<String>(codeKey);
        if (cachedCode.IsNullOrEmpty()) return new ServiceResult { IsSuccess = false, Message = "验证码已过期或不存在，请重新获取" };
        if (!cachedCode.EqualIgnoreCase(code)) return new ServiceResult { IsSuccess = false, Message = "验证码错误" };

        // 查找用户并更新密码
        var user = User.FindByMobile(mobile);
        if (user == null || user.ID <= 0)
            return new ServiceResult { IsSuccess = false, Message = "该手机号未注册" };

        var newPassHash = ManageProvider.Provider.PasswordProvider.Hash(newPassword);
        if (user.Password != newPassHash)
        {
            user.Password = newPassHash;
            var updated = user.Update();
            if (updated <= 0) return new ServiceResult { IsSuccess = false, Message = "密码重置失败，请重试" };
        }

        // 验证成功后删除缓存验证码，防止重复使用
        _cache.Remove(codeKey);

        LogProvider.Provider.WriteLog(typeof(User), "重置密码", true, $"手机号：{mobile}", user.ID, user + "", ip);

        return new ServiceResult { IsSuccess = true, Message = "密码重置成功" };
    }

    /// <summary>通过邮件验证码重置密码</summary>
    private ServiceResult ResetByMailCode(String mail, String code, String newPassword, String ip)
    {
        var set = CubeSetting.Current;
        if (!set.EnableMail) return new ServiceResult { IsSuccess = false, Message = "邮件验证码功能未启用" };

        // 验证验证码
        var codeKey = $"{AuthCacheKeys.MailResetCodePrefix}{mail}";
        var cachedCode = _cache.Get<String>(codeKey);
        if (cachedCode.IsNullOrEmpty()) return new ServiceResult { IsSuccess = false, Message = "验证码已过期或不存在，请重新获取" };
        if (!cachedCode.EqualIgnoreCase(code)) return new ServiceResult { IsSuccess = false, Message = "验证码错误" };

        // 查找用户并更新密码
        var user = User.FindByMail(mail);
        if (user == null || user.ID <= 0)
            return new ServiceResult { IsSuccess = false, Message = "该邮箱未注册" };

        var newPassHash = ManageProvider.Provider.PasswordProvider.Hash(newPassword);
        if (user.Password != newPassHash)
        {
            user.Password = newPassHash;
            var updated = user.Update();
            if (updated <= 0) return new ServiceResult { IsSuccess = false, Message = "密码重置失败，请重试" };
        }

        // 验证成功后删除缓存验证码，防止重复使用
        _cache.Remove(codeKey);

        LogProvider.Provider.WriteLog(typeof(User), "重置密码", true, $"邮箱：{mail}", user.ID, user + "", ip);

        return new ServiceResult { IsSuccess = true, Message = "密码重置成功" };
    }
    #endregion
}

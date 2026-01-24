using System.Text;
using NewLife.Caching;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Common;
using NewLife.Cube.Entity;
using NewLife.Cube.Models;
using NewLife.Cube.Web;
using NewLife.Log;
using NewLife.Model;
using NewLife.Security;
using NewLife.Threading;
using XCode;
using XCode.Membership;
using HttpContext = Microsoft.AspNetCore.Http.HttpContext;

namespace NewLife.Cube.Services;

/// <summary>用户服务</summary>
/// <param name="smsService">短信服务</param>
/// <param name="mailService">邮件服务</param>
/// <param name="passwordService">密码服务</param>
/// <param name="cacheProvider">缓存提供者</param>
/// <param name="tracer">追踪器</param>
public class UserService(SmsService smsService, MailService mailService, PasswordService passwordService, ICacheProvider cacheProvider, ITracer tracer)
{
    #region 缓存Key前缀常量
    /// <summary>密码登录用户名错误次数缓存前缀</summary>
    private const String PasswordLoginUserPrefix = "CubeLogin:";
    /// <summary>密码登录IP错误次数缓存前缀</summary>
    private const String PasswordLoginIpPrefix = "CubeLogin:";

    /// <summary>短信登录IP发送限制缓存前缀</summary>
    private const String SmsLoginIpPrefix = "SmsLogin:IP:";
    /// <summary>短信登录最后发送时间缓存前缀</summary>
    private const String SmsLoginLastSendPrefix = "SmsLogin:LastSend:";
    /// <summary>短信登录验证码缓存前缀</summary>
    private const String SmsLoginCodePrefix = "SmsLogin:Code:";
    /// <summary>短信登录错误次数缓存前缀</summary>
    private const String SmsLoginErrorPrefix = "SmsLogin:Error:";
    /// <summary>短信登录IP错误次数缓存前缀</summary>
    private const String SmsLoginErrorIpPrefix = "SmsLogin:Error:IP:";

    /// <summary>邮件登录IP发送限制缓存前缀</summary>
    private const String MailLoginIpPrefix = "MailLogin:IP:";
    /// <summary>邮件登录最后发送时间缓存前缀</summary>
    private const String MailLoginLastSendPrefix = "MailLogin:LastSend:";
    /// <summary>邮件登录验证码缓存前缀</summary>
    private const String MailLoginCodePrefix = "MailLogin:Code:";
    /// <summary>邮件登录错误次数缓存前缀</summary>
    private const String MailLoginErrorPrefix = "MailLogin:Error:";
    /// <summary>邮件登录IP错误次数缓存前缀</summary>
    private const String MailLoginErrorIpPrefix = "MailLogin:Error:IP:";

    /// <summary>短信绑定手机IP发送限制缓存前缀</summary>
    private const String SmsBindIpPrefix = "SmsBind:IP:";
    /// <summary>短信绑定手机最后发送时间缓存前缀</summary>
    private const String SmsBindLastSendPrefix = "SmsBind:LastSend:";
    /// <summary>短信绑定手机验证码缓存前缀</summary>
    private const String SmsBindCodePrefix = "SmsBind:Code:";

    /// <summary>短信重置密码IP发送限制缓存前缀</summary>
    private const String SmsResetIpPrefix = "SmsReset:IP:";
    /// <summary>短信重置密码最后发送时间缓存前缀</summary>
    private const String SmsResetLastSendPrefix = "SmsReset:LastSend:";
    /// <summary>短信重置密码验证码缓存前缀</summary>
    private const String SmsResetCodePrefix = "SmsReset:Code:";

    /// <summary>邮件绑定IP发送限制缓存前缀</summary>
    private const String MailBindIpPrefix = "MailBind:IP:";
    /// <summary>邮件绑定最后发送时间缓存前缀</summary>
    private const String MailBindLastSendPrefix = "MailBind:LastSend:";
    /// <summary>邮件绑定验证码缓存前缀</summary>
    private const String MailBindCodePrefix = "MailBind:Code:";

    /// <summary>邮件重置密码IP发送限制缓存前缀</summary>
    private const String MailResetIpPrefix = "MailReset:IP:";
    /// <summary>邮件重置密码最后发送时间缓存前缀</summary>
    private const String MailResetLastSendPrefix = "MailReset:LastSend:";
    /// <summary>邮件重置密码验证码缓存前缀</summary>
    private const String MailResetCodePrefix = "MailReset:Code:";

    /// <summary>短信通知IP发送限制缓存前缀</summary>
    private const String SmsNotifyIpPrefix = "SmsNotify:IP:";
    /// <summary>短信通知最后发送时间缓存前缀</summary>
    private const String SmsNotifyLastSendPrefix = "SmsNotify:LastSend:";
    /// <summary>短信通知验证码缓存前缀</summary>
    private const String SmsNotifyCodePrefix = "SmsNotify:Code:";

    /// <summary>邮件通知IP发送限制缓存前缀</summary>
    private const String MailNotifyIpPrefix = "MailNotify:IP:";
    /// <summary>邮件通知最后发送时间缓存前缀</summary>
    private const String MailNotifyLastSendPrefix = "MailNotify:LastSend:";
    /// <summary>邮件通知验证码缓存前缀</summary>
    private const String MailNotifyCodePrefix = "MailNotify:Code:";
    #endregion

    #region 属性
    private readonly ICache _cache = cacheProvider.Cache;
    #endregion

    #region 登录
    /// <summary>统一登录入口，支持账号密码、手机验证码、邮箱验证码登录</summary>
    /// <param name="loginModel">登录模型</param>
    /// <param name="httpContext">HTTP上下文</param>
    /// <returns>登录结果，包含Token信息或错误信息</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="XException"></exception>
    public LoginResult Login(LoginModel loginModel, HttpContext httpContext)
    {



        if (ValidFormatHelper.IsMobile(loginModel.Username))
            return LoginBySms(loginModel, httpContext);
        else if (ValidFormatHelper.IsEmail(loginModel.Username))
            return LoginByMail(loginModel, httpContext);
        else return LoginByPassword(loginModel, httpContext);
    }

    /// <summary>账号密码登录</summary>
    private LoginResult LoginByPassword(LoginModel loginModel, HttpContext httpContext)
    {
        var username = loginModel.Username;
        var password = loginModel.Password;
        var remember = loginModel.Remember;
        var ip = httpContext.GetUserHost();
        using var span = tracer?.NewSpan(nameof(LoginByPassword), new { username, ip });

        // 连续错误校验
        var key = $"{PasswordLoginUserPrefix}{username}";
        var errors = _cache.Get<Int32>(key);
        var ipKey = $"{PasswordLoginIpPrefix}{ip}";
        var ipErrors = _cache.Get<Int32>(ipKey);

        var set = CubeSetting.Current;
        try
        {
            if (username.IsNullOrEmpty()) throw new ArgumentNullException(nameof(username), "用户名不能为空！");
            if (password.IsNullOrEmpty()) throw new ArgumentNullException(nameof(password), "密码不能为空！");

            if (errors >= set.MaxLoginError && set.MaxLoginError > 0)
                throw new InvalidOperationException($"[{username}]登录错误过多，请在{set.LoginForbiddenTime}秒后再试！");
            if (ipErrors >= set.MaxLoginError && set.MaxLoginError > 0)
                throw new InvalidOperationException($"IP地址[{ip}]登录错误过多，请在{set.LoginForbiddenTime}秒后再试！");

            var pdic = loginModel.Pkey.IsNullOrEmpty()
              ? new Tuple<String, String>(null, null)
              : _cache.Get<Tuple<String, String>>(loginModel.Pkey);
            var rsaKey = pdic?.Item2;
            password = rsaKey.IsNullOrEmpty() ? password : Decrypt(rsaKey, password);

            var provider = ManageProvider.Provider;
            if (provider.Login(username, password, remember) == null)
                return new LoginResult { Result = "提供的用户名或密码不正确。" };

            // 登录成功，清空错误数
            if (errors > 0) _cache.Remove(key);
            if (ipErrors > 0) _cache.Remove(ipKey);

            // 移除秘钥私钥信息，避免重放
            if (!loginModel.Pkey.IsNullOrEmpty()) _cache.Remove(loginModel.Pkey);

            return CompleteLogin(provider.Current, httpContext, remember, "密码登录", username, ip);
        }
        catch (Exception ex)
        {
            HandleLoginError(ex, "登录", username, ip, key, ipKey, errors, ipErrors, set.LoginForbiddenTime);
            throw;
        }
    }

    /// <summary>手机验证码登录</summary>
    private LoginResult LoginBySms(LoginModel loginModel, HttpContext httpContext)
    {
        var mobile = loginModel.Username?.Trim() ?? "";
        var code = loginModel.Password?.Trim() ?? "";
        var remember = loginModel.Remember;
        var ip = httpContext.GetUserHost();
        using var span = tracer?.NewSpan(nameof(LoginBySms), new { mobile, ip });

        if (mobile.IsNullOrEmpty()) throw new ArgumentNullException(nameof(mobile), "手机号不能为空");
        if (!ValidFormatHelper.IsMobile(mobile)) throw new XException("手机号格式不正确");
        if (code.IsNullOrEmpty()) throw new ArgumentNullException(nameof(code), "验证码不能为空");

        var key = $"{SmsLoginErrorPrefix}{mobile}";
        var errors = _cache.Get<Int32>(key);
        var ipKey = $"{SmsLoginErrorIpPrefix}{ip}";
        var ipErrors = _cache.Get<Int32>(ipKey);

        var set = CubeSetting.Current;
        try
        {
            // 错误次数检查
            if (errors >= set.MaxLoginError && set.MaxLoginError > 0)
                throw new InvalidOperationException($"[{mobile}]验证错误过多，请在{set.LoginForbiddenTime}秒后再试！");
            if (ipErrors >= set.MaxLoginError && set.MaxLoginError > 0)
                throw new InvalidOperationException($"IP地址[{ip}]验证错误过多，请在{set.LoginForbiddenTime}秒后再试！");

            // 校验验证码
            var codeKey = $"{SmsLoginCodePrefix}{mobile}";
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

            return CompleteLogin(user, httpContext, remember, "短信登录", mobile, ip);
        }
        catch (Exception ex)
        {
            HandleLoginError(ex, "短信登录", mobile, ip, key, ipKey, errors, ipErrors, set.LoginForbiddenTime);
            throw;
        }
    }

    /// <summary>邮箱验证码登录</summary>
    private LoginResult LoginByMail(LoginModel loginModel, HttpContext httpContext)
    {
        var mail = loginModel.Username?.Trim() ?? "";
        var code = loginModel.Password?.Trim() ?? "";
        var remember = loginModel.Remember;
        var ip = httpContext.GetUserHost();
        using var span = tracer?.NewSpan(nameof(LoginByMail), new { mail, ip });

        if (mail.IsNullOrEmpty()) throw new ArgumentNullException(nameof(mail), "邮箱不能为空");
        if (!ValidFormatHelper.IsEmail(mail)) throw new XException("邮箱格式不正确");
        if (code.IsNullOrEmpty()) throw new ArgumentNullException(nameof(code), "验证码不能为空");

        var key = $"{MailLoginErrorPrefix}{mail}";
        var errors = _cache.Get<Int32>(key);
        var ipKey = $"{MailLoginErrorIpPrefix}{ip}";
        var ipErrors = _cache.Get<Int32>(ipKey);

        var set = CubeSetting.Current;
        try
        {
            // 错误次数检查
            if (errors >= set.MaxLoginError && set.MaxLoginError > 0)
                throw new InvalidOperationException($"[{mail}]验证错误过多，请在{set.LoginForbiddenTime}秒后再试！");
            if (ipErrors >= set.MaxLoginError && set.MaxLoginError > 0)
                throw new InvalidOperationException($"IP地址[{ip}]验证错误过多，请在{set.LoginForbiddenTime}秒后再试！");

            // 校验验证码
            var codeKey = $"{MailLoginCodePrefix}{mail}";
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

            return CompleteLogin(user, httpContext, remember, "邮箱登录", mail, ip);
        }
        catch (Exception ex)
        {
            HandleLoginError(ex, "邮箱登录", mail, ip, key, ipKey, errors, ipErrors, set.LoginForbiddenTime);
            throw;
        }
    }

    /// <summary>完成登录，记录统计并生成Token</summary>
    private LoginResult CompleteLogin(IManageUser user, HttpContext httpContext, Boolean remember, String action, String username, String ip)
    {
        var set = CubeSetting.Current;

        // 保存Cookie
        var provider = ManageProvider.Provider;
        var expire = remember ? TimeSpan.FromDays(365) : TimeSpan.FromMinutes(0);
        if (set.SessionTimeout > 0 && !remember)
            expire = TimeSpan.FromSeconds(set.SessionTimeout);
        provider.SaveCookie(user, expire, httpContext);

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

        LogProvider.Provider.WriteLog(typeof(User), action, true, $"用户：{username}", user.ID, user + "", ip);

        var tokens = httpContext.IssueTokenAndRefreshToken(user, TimeSpan.FromSeconds(set.TokenExpire));

        return new LoginResult { AccessToken = tokens.AccessToken, RefreshToken = tokens.RefreshToken, ExpireIn = tokens.ExpireIn };
    }

    /// <summary>确保用户已绑定到当前租户。用户从哪个租户登录/注册，自动添加绑定关系</summary>
    /// <param name="httpContext">HTTP上下文</param>
    /// <param name="userId">用户编号</param>
    /// <param name="ip">客户端IP</param>
    /// <returns>租户用户绑定记录，无需绑定时返回null</returns>
    private TenantUser EnsureTenantUser(HttpContext httpContext, Int32 userId, String ip)
    {
        // 获取当前请求的租户ID（从Header、QueryString或Cookie）
        var tenantId = httpContext.GetTenantId();
        if (tenantId <= 0 || userId <= 0) return null;

        // 检查是否已绑定
        var tenantUser = TenantUser.FindByTenantIdAndUserId(tenantId, userId);
        if (tenantUser != null) return tenantUser;

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

        XTrace.WriteLine($"[{userId}]用户自动绑定到租户[{tenantId}]");
        return tenantUser;
    }

    /// <summary>处理登录错误，记录日志并累加错误次数</summary>
    private void HandleLoginError(Exception ex, String action, String username, String ip, String key, String ipKey, Int32 errors, Int32 ipErrors, Int32 forbiddenTime)
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
    }

    private static String Decrypt(String privateKey, String decryptString)
    {
        var decryptedData = RSAHelper.Decrypt(Convert.FromBase64String(decryptString), privateKey, false);

        return Encoding.UTF8.GetString(decryptedData);
    }
    #endregion

    #region 验证码
    /// <summary>发送登录验证码</summary>
    public async Task<VerifyCodeRecord> SendVerifyCode(VerifyCodeModel model, String ip)
    {
        var user = model.Username?.Trim() ?? "";
        if (user.IsNullOrEmpty()) throw new XException("账号不能为空");

        if (model.Channel.EqualIgnoreCase("Mail") || Common.ValidFormatHelper.IsEmail(user))
            return await SendMailCode(model, ip);

        if (model.Channel.EqualIgnoreCase("Sms") || Common.ValidFormatHelper.IsMobile(user))
            return await SendSmsCode(model, ip);

        throw new NotSupportedException();
    }
    /// <summary>短信验证码发送逻辑</summary>
    /// <param name="model">验证码模型</param>
    /// <param name="ip">客户端IP</param>
    /// <returns></returns>
    /// <exception cref="XException"></exception>
    private async Task<VerifyCodeRecord> SendSmsCode(VerifyCodeModel model, String ip)
    {
        var mobile = model.Username?.Trim() ?? "";
        if (mobile.IsNullOrEmpty()) throw new XException("手机号不能为空");

        // 校验手机号格式
        if (!ValidFormatHelper.IsMobile(mobile)) throw new XException("手机号格式不正确");

        // 检查短信服务是否启用
        var set = CubeSetting.Current;
        if (!set.EnableSms) throw new XException("短信验证码功能未启用");

        var config = smsService.GetConfig(TenantContext.CurrentId, model.Action);
        if (config == null) throw new XException("短信服务未配置");

        // 检查短信配置是否完整
        if (config.AppKey.IsNullOrEmpty() || config.AppSecret.IsNullOrEmpty())
            throw new XException("短信AccessKey未配置，请在系统参数中配置AppKey和AppSecret");

        if (config.SignName.IsNullOrEmpty())
            throw new XException("短信签名未配置，请在系统参数中配置SignName");

        // 根据 Action 类型选择缓存 key 前缀
        var (ipPrefix, lastSendPrefix, codePrefix) = GetSmsCachePrefix(model.Action);

        var ipKey = $"{ipPrefix}{ip}";

        // 防止频繁发送（IP限制）
        var ipCount = _cache.Get<Int32>(ipKey);
        if (ipCount >= 5) throw new XException("发送频繁，请稍后再试");

        // 防止频繁发送（手机号限制，60秒内只能发一次）
        var lastSend = _cache.Get<DateTime>($"{lastSendPrefix}{mobile}");
        if (lastSend > DateTime.MinValue && (DateTime.Now - lastSend).TotalSeconds < 60)
        {
            var wait = 60 - (Int32)(DateTime.Now - lastSend).TotalSeconds;
            throw new XException($"请{wait}秒后再试");
        }

        try
        {
            // 发送短信验证码
            var code = SmsService.GenerateVerifyCode();
            var rs = await smsService.SendVerifyCode(model.Action, mobile, code, config);
            if (rs == null || !rs.Success)
                throw new XException("短信发送失败");

            // 缓存验证码用于校验
            var codeKey = $"{codePrefix}{mobile}";
            _cache.Set(codeKey, code, config.Expire);

            // 记录发送时间
            _cache.Set($"{lastSendPrefix}{mobile}", DateTime.Now, 60);

            // 累计IP发送次数
            _cache.Increment(ipKey, 1);
            if (ipCount <= 0) _cache.SetExpire(ipKey, TimeSpan.FromMinutes(10));

            LogProvider.Provider.WriteLog(typeof(User), "发送验证码", true, $"手机号：{mobile}", 0, mobile, ip);

            return rs;
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
            LogProvider.Provider.WriteLog(typeof(User), "发送验证码", false, $"手机号：{mobile}，错误：{ex.Message}", 0, mobile, ip);
            throw;
        }
    }

    /// <summary>邮箱发送逻辑</summary>
    /// <param name="model">验证码模型</param>
    /// <param name="ip">客户端IP</param>
    /// <returns></returns>
    /// <exception cref="XException"></exception>
    private async Task<VerifyCodeRecord> SendMailCode(VerifyCodeModel model, String ip)
    {
        var mail = model.Username?.Trim() ?? "";
        if (mail.IsNullOrEmpty()) throw new XException("邮件地址不能为空");

        // 检查邮件服务是否启用
        var set = CubeSetting.Current;
        if (!set.EnableMail) throw new XException("邮件验证码功能未启用");

        var config = mailService.GetConfig(TenantContext.CurrentId, model.Action);
        //if (config == null) throw new XException("邮件服务未配置");


        // 根据 Action 类型选择缓存 key 前缀
        var (ipPrefix, lastSendPrefix, codePrefix) = GetMailCachePrefix(model.Action);

        var ipKey = $"{ipPrefix}{ip}";

        // 防止频繁发送（IP限制）
        var ipCount = _cache.Get<Int32>(ipKey);
        if (ipCount >= 5) throw new XException("发送频繁，请稍后再试");

        // 防止频繁发送 
        var lastSend = _cache.Get<DateTime>($"{lastSendPrefix}{mail}");
        if (lastSend > DateTime.MinValue && (DateTime.Now - lastSend).TotalSeconds < 60)
        {
            var wait = 60 - (Int32)(DateTime.Now - lastSend).TotalSeconds;
            throw new XException($"请{wait}秒后再试");
        }

        try
        {
            // 发送邮件验证码
            var code = MailService.GenerateVerifyCode();
            var rs = await mailService.SendVerifyCode(model.Action, mail, code, config);
            if (rs == null || !rs.Success) throw new XException("邮件发送失败");

            // 缓存验证码用于校验
            var codeKey = $"{codePrefix}{mail}";
            _cache.Set(codeKey, code, config.Expire);

            // 记录发送时间
            _cache.Set($"{lastSendPrefix}{mail}", DateTime.Now, 60);

            // 累计IP发送次数
            _cache.Increment(ipKey, 1);
            if (ipCount <= 0) _cache.SetExpire(ipKey, TimeSpan.FromMinutes(10));

            LogProvider.Provider.WriteLog(typeof(User), "发送验证码", true, $"邮箱：{mail}", 0, mail, ip);

            return rs;
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
            LogProvider.Provider.WriteLog(typeof(User), "发送验证码", false, $"邮箱：{mail}，错误：{ex.Message}", 0, mail, ip);
            throw;
        }
    }

    /// <summary>根据Action获取短信缓存前缀</summary>
    /// <param name="action">操作类型：login/bind/reset</param>
    /// <returns>IP前缀、最后发送前缀、验证码前缀</returns>
    private (String ipPrefix, String lastSendPrefix, String codePrefix) GetSmsCachePrefix(String action)
    {
        return action?.ToLower() switch
        {
            "bind" => (SmsBindIpPrefix, SmsBindLastSendPrefix, SmsBindCodePrefix),
            "reset" => (SmsResetIpPrefix, SmsResetLastSendPrefix, SmsResetCodePrefix),
            "login" => (SmsLoginIpPrefix, SmsLoginLastSendPrefix, SmsLoginCodePrefix),
            "notify" => (SmsNotifyIpPrefix, SmsNotifyLastSendPrefix, SmsNotifyCodePrefix),
            _ => (SmsNotifyIpPrefix, SmsNotifyLastSendPrefix, SmsNotifyCodePrefix),
        };
    }

    /// <summary>根据Action获取邮件缓存前缀</summary>
    /// <param name="action">操作类型：login/bind/reset</param>
    /// <returns>IP前缀、最后发送前缀、验证码前缀</returns>
    private (String ipPrefix, String lastSendPrefix, String codePrefix) GetMailCachePrefix(String action)
    {
        return action?.ToLower() switch
        {
            "bind" => (MailBindIpPrefix, MailBindLastSendPrefix, MailBindCodePrefix),
            "reset" => (MailResetIpPrefix, MailResetLastSendPrefix, MailResetCodePrefix),
            "login" => (MailLoginIpPrefix, MailLoginLastSendPrefix, MailLoginCodePrefix),
            "notify" => (MailNotifyIpPrefix, MailNotifyLastSendPrefix, MailNotifyCodePrefix),
            _ => (MailNotifyIpPrefix, MailNotifyLastSendPrefix, MailNotifyCodePrefix),
        };
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
    /// <param name="online"></param>
    /// <param name="sessionId"></param>
    /// <param name="deviceId"></param>
    /// <param name="page"></param>
    /// <param name="status"></param>
    /// <param name="userAgent"></param>
    /// <param name="userid"></param>
    /// <param name="name"></param>
    /// <param name="ip"></param>
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
        //var online = FindBySessionID(sessionid) ?? new UserOnline();
        //online.SessionID = sessionid;
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
    /// <param name="online"></param>
    /// <param name="sessionId"></param>
    /// <param name="deviceId"></param>
    /// <param name="page"></param>
    /// <param name="status"></param>
    /// <param name="userAgent"></param>
    /// <param name="user"></param>
    /// <param name="ip"></param>
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
        //selects &= User._.OnlineTime.Sum();

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

                //var sty = UserStat.FindByDate(DateTime.Today.AddDays(-1));
                //if (sty != null)
                //    st.OnlineTime = user.OnlineTime - sty.OnlineTime;
                //else
                //    st.OnlineTime = user.OnlineTime;

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

    #region 绑定账号
    /// <summary>绑定手机号或邮箱到当前登录用户</summary>
    /// <param name="account">手机号或邮箱</param>
    /// <param name="code">验证码</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="ip">客户端IP</param>
    /// <returns>绑定结果</returns>
    public BindResult BindMobile(String account, String code, IUser currentUser, String ip)
    {
        using var span = tracer?.NewSpan(nameof(BindMobile), new { account, ip });

        // 1. 公共参数校验
        if (account.IsNullOrEmpty())
            return new BindResult { Success = false, Message = "手机号或邮箱不能为空" };

        if (code.IsNullOrEmpty())
            return new BindResult { Success = false, Message = "验证码不能为空" };

        if (currentUser == null || currentUser.ID <= 0)
            return new BindResult { Success = false, Message = "用户未登录，请先登录" };

        // 2. 根据账号类型分发到具体处理方法
        if (ValidFormatHelper.IsEmail(account))
            return BindByMailCode(account, code, currentUser, ip);

        if (ValidFormatHelper.IsMobile(account))
            return BindBySmsCode(account, code, currentUser, ip);

        return new BindResult { Success = false, Message = "请输入正确的手机号或邮箱" };
    }

    /// <summary>通过短信验证码绑定手机号</summary>
    /// <param name="mobile">手机号</param>
    /// <param name="code">验证码</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="ip">客户端IP</param>
    /// <returns>绑定结果</returns>
    private BindResult BindBySmsCode(String mobile, String code, IUser currentUser, String ip)
    {
        var set = CubeSetting.Current;
        if (!set.EnableSms) return new BindResult { Success = false, Message = "短信验证码功能未启用" };

        // 验证验证码
        var codeKey = $"{SmsBindCodePrefix}{mobile}";
        var cachedCode = _cache.Get<String>(codeKey);
        if (cachedCode.IsNullOrEmpty()) return new BindResult { Success = false, Message = "验证码已过期或不存在，请重新获取" };
        if (!cachedCode.EqualIgnoreCase(code)) return new BindResult { Success = false, Message = "验证码错误" };

        // 检查手机号是否已被其他用户绑定
        var existingUser = User.FindByMobile(mobile);
        if (existingUser != null && existingUser.ID > 0 && existingUser.ID != currentUser.ID)
            return new BindResult { Success = false, Message = "该手机号已被其他账户绑定" };

        // 绑定到当前用户
        var user = User.FindByID(currentUser.ID);
        if (user == null) return new BindResult { Success = false, Message = "用户不存在" };

        if (user.Mobile != mobile)
        {
            user.Mobile = mobile;
            user.MobileVerified = true;
            var updated = user.Update();
            if (updated <= 0) return new BindResult { Success = false, Message = "绑定失败，请重试" };
        }

        // 验证成功后删除缓存验证码，防止重复使用
        _cache.Remove(codeKey);

        LogProvider.Provider.WriteLog(typeof(User), "绑定手机", true, $"手机号：{mobile}", currentUser.ID, currentUser + "", ip);

        return new BindResult { Success = true, Message = "手机号绑定成功" };
    }

    /// <summary>通过邮件验证码绑定邮箱</summary>
    /// <param name="mail">邮箱</param>
    /// <param name="code">验证码</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="ip">客户端IP</param>
    /// <returns>绑定结果</returns>
    private BindResult BindByMailCode(String mail, String code, IUser currentUser, String ip)
    {
        var set = CubeSetting.Current;
        if (!set.EnableMail) return new BindResult { Success = false, Message = "邮件验证码功能未启用" };

        // 验证验证码
        var codeKey = $"{MailBindCodePrefix}{mail}";
        var cachedCode = _cache.Get<String>(codeKey);
        if (cachedCode.IsNullOrEmpty()) return new BindResult { Success = false, Message = "验证码已过期或不存在，请重新获取" };
        if (!cachedCode.EqualIgnoreCase(code)) return new BindResult { Success = false, Message = "验证码错误" };

        // 检查邮箱是否已被其他用户绑定
        var existingUser = User.FindByMail(mail);
        if (existingUser != null && existingUser.ID > 0 && existingUser.ID != currentUser.ID)
            return new BindResult { Success = false, Message = "该邮箱已被其他账户绑定" };

        // 绑定到当前用户
        var user = User.FindByID(currentUser.ID);
        if (user == null) return new BindResult { Success = false, Message = "用户不存在" };

        if (user.Mail != mail)
        {
            user.Mail = mail;
            user.MailVerified = true;
            var updated = user.Update();
            if (updated <= 0) return new BindResult { Success = false, Message = "绑定失败，请重试" };
        }

        // 验证成功后删除缓存验证码，防止重复使用
        _cache.Remove(codeKey);

        LogProvider.Provider.WriteLog(typeof(User), "绑定邮箱", true, $"邮箱：{mail}", currentUser.ID, currentUser + "", ip);

        return new BindResult { Success = true, Message = "邮箱绑定成功" };
    }
    #endregion

    #region 重置密码
    /// <summary>通过手机或邮箱验证码重置密码</summary>
    /// <param name="account">手机号或邮箱</param>
    /// <param name="code">验证码</param>
    /// <param name="newPassword">新密码</param>
    /// <param name="confirmPassword">确认密码</param>
    /// <param name="ip">客户端IP</param>
    /// <returns>重置结果</returns>
    public ResetResult ResetPassword(String account, String code, String newPassword, String confirmPassword, String ip)
    {
        using var span = tracer?.NewSpan(nameof(ResetPassword), new { account, ip });

        // 1. 公共参数校验
        if (account.IsNullOrEmpty()) return new ResetResult { Success = false, Message = "手机号或邮箱不能为空" };
        if (code.IsNullOrEmpty()) return new ResetResult { Success = false, Message = "验证码不能为空" };
        if (newPassword.IsNullOrEmpty()) return new ResetResult { Success = false, Message = "新密码不能为空" };
        if (!confirmPassword.IsNullOrEmpty() && newPassword != confirmPassword)
            return new ResetResult { Success = false, Message = "两次输入密码不一致" };
        if (!passwordService.Valid(newPassword)) return new ResetResult { Success = false, Message = "密码太弱" };

        // 2. 根据账号类型分发到具体处理方法
        if (ValidFormatHelper.IsEmail(account))
            return ResetByMailCode(account, code, newPassword, ip);

        if (ValidFormatHelper.IsMobile(account))
            return ResetBySmsCode(account, code, newPassword, ip);

        return new ResetResult { Success = false, Message = "请输入正确的手机号或邮箱" };
    }

    /// <summary>通过短信验证码重置密码</summary>
    /// <param name="mobile">手机号</param>
    /// <param name="code">验证码</param>
    /// <param name="newPassword">新密码</param>
    /// <param name="ip">客户端IP</param>
    /// <returns>重置结果</returns>
    private ResetResult ResetBySmsCode(String mobile, String code, String newPassword, String ip)
    {
        var set = CubeSetting.Current;
        if (!set.EnableSms) return new ResetResult { Success = false, Message = "短信验证码功能未启用" };

        // 验证验证码
        var codeKey = $"{SmsResetCodePrefix}{mobile}";
        var cachedCode = _cache.Get<String>(codeKey);
        if (cachedCode.IsNullOrEmpty()) return new ResetResult { Success = false, Message = "验证码已过期或不存在，请重新获取" };
        if (!cachedCode.EqualIgnoreCase(code)) return new ResetResult { Success = false, Message = "验证码错误" };

        // 查找用户并更新密码
        var user = User.FindByMobile(mobile);
        if (user == null || user.ID <= 0)
            return new ResetResult { Success = false, Message = "该手机号未注册" };

        var newPassHash = ManageProvider.Provider.PasswordProvider.Hash(newPassword);
        if (user.Password != newPassHash)
        {
            user.Password = newPassHash;
            var updated = user.Update();
            if (updated <= 0) return new ResetResult { Success = false, Message = "密码重置失败，请重试" };
        }

        // 验证成功后删除缓存验证码，防止重复使用
        _cache.Remove(codeKey);

        LogProvider.Provider.WriteLog(typeof(User), "重置密码", true, $"手机号：{mobile}", user.ID, user + "", ip);

        return new ResetResult { Success = true, Message = "密码重置成功" };
    }

    /// <summary>通过邮件验证码重置密码</summary>
    /// <param name="mail">邮箱</param>
    /// <param name="code">验证码</param>
    /// <param name="newPassword">新密码</param>
    /// <param name="ip">客户端IP</param>
    /// <returns>重置结果</returns>
    private ResetResult ResetByMailCode(String mail, String code, String newPassword, String ip)
    {
        var set = CubeSetting.Current;
        if (!set.EnableMail) return new ResetResult { Success = false, Message = "邮件验证码功能未启用" };

        // 验证验证码
        var codeKey = $"{MailResetCodePrefix}{mail}";
        var cachedCode = _cache.Get<String>(codeKey);
        if (cachedCode.IsNullOrEmpty()) return new ResetResult { Success = false, Message = "验证码已过期或不存在，请重新获取" };
        if (!cachedCode.EqualIgnoreCase(code)) return new ResetResult { Success = false, Message = "验证码错误" };

        // 查找用户并更新密码
        var user = User.FindByMail(mail);
        if (user == null || user.ID <= 0)
            return new ResetResult { Success = false, Message = "该邮箱未注册" };

        var newPassHash = ManageProvider.Provider.PasswordProvider.Hash(newPassword);
        if (user.Password != newPassHash)
        {
            user.Password = newPassHash;
            var updated = user.Update();
            if (updated <= 0) return new ResetResult { Success = false, Message = "密码重置失败，请重试" };
        }

        // 验证成功后删除缓存验证码，防止重复使用
        _cache.Remove(codeKey);

        LogProvider.Provider.WriteLog(typeof(User), "重置密码", true, $"邮箱：{mail}", user.ID, user + "", ip);

        return new ResetResult { Success = true, Message = "密码重置成功" };
    }
    #endregion
}
using System.Diagnostics;
using System.Text;
using NewLife.Caching;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Entity;
using NewLife.Cube.Extensions;
using NewLife.Cube.Models;
using NewLife.Cube.Web;
using NewLife.Log;
using NewLife.Security;
using NewLife.Threading;
using NewLife.Web;
using XCode;
using XCode.Membership;
using HttpContext = Microsoft.AspNetCore.Http.HttpContext;

namespace NewLife.Cube.Services;

/// <summary>用户服务</summary>
/// <param name="cacheProvider"></param>
/// <param name="tracer"></param>
public class UserService(SmsService smsService, MailService mailService, ICacheProvider cacheProvider, ITracer tracer)
{
    #region 属性
    private readonly ICache _cache = cacheProvider.Cache;
    #endregion

    #region 登录
    /// <summary>登录</summary>
    /// <param name="loginModel"></param>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="XException"></exception>
    public LoginResult Login(LoginModel loginModel, HttpContext httpContext)
    {
        var username = loginModel.Username;
        var password = loginModel.Password;
        var remember = loginModel.Remember;
        var ip = httpContext.GetUserHost();
        using var span = tracer?.NewSpan(nameof(Login), new { username, ip });

        // 连续错误校验
        var key = $"CubeLogin:{username}";
        var errors = _cache.Get<Int32>(key);
        var ipKey = $"CubeLogin:{ip}";
        var ipErrors = _cache.Get<Int32>(ipKey);

        var set = CubeSetting.Current;
        var req = httpContext.Request;
        var returnUrl = req.GetRequestValue("r");
        if (returnUrl.IsNullOrEmpty()) returnUrl = req.GetRequestValue("ReturnUrl");
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
            var rsaKey = pdic.Item2;
            password = rsaKey.IsNullOrEmpty() ? password : Decrypt(rsaKey, password);

            var provider = ManageProvider.Provider;
            if (provider.Login(username, password, remember) == null)
                return new LoginResult { Result = "提供的用户名或密码不正确。" };

            // 登录成功，清空错误数
            if (errors > 0) _cache.Remove(key);
            if (ipErrors > 0) _cache.Remove(ipKey);

            // 移除秘钥私钥信息，避免重放
            if (!loginModel.Pkey.IsNullOrEmpty()) _cache.Remove(loginModel.Pkey);

            // 记录在线统计
            var stat = UserStat.GetOrAdd(DateTime.Today);
            if (stat != null)
            {
                stat.Logins++;
                stat.SaveAsync(5_000);
            }

            // 设置租户
            httpContext.ChooseTenant(provider.Current.ID);

            var tokens = httpContext.IssueTokenAndRefreshToken(provider.Current, TimeSpan.FromSeconds(set.TokenExpire));
            return new LoginResult { AccessToken = tokens.Item1, RefreshToken = tokens.Item2 };
        }
        catch (Exception ex)
        {
            // 登录失败比较重要，记录一下
            var action = ex is InvalidOperationException ? "风控" : "登录";
            LogProvider.Provider.WriteLog(typeof(User), action, false, ex.Message, 0, username, ip);
            XTrace.WriteLine("[{0}]登录失败！{1}", username, ex.Message);
            XTrace.WriteException(ex);

            // 累加错误数，首次出错时设置过期时间
            _cache.Increment(key, 1);
            _cache.Increment(ipKey, 1);
            var time = 300;
            if (set.LoginForbiddenTime > 0) time = set.LoginForbiddenTime;
            if (errors <= 0) _cache.SetExpire(key, TimeSpan.FromSeconds(time));
            if (ipErrors <= 0) _cache.SetExpire(ipKey, TimeSpan.FromSeconds(time));

            throw;
        }
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

        if (model.Channel.EqualIgnoreCase("Mail") || user.Contains('@') && user.Contains('.'))
            return await SendMailCode(model, ip);

        if (model.Channel.EqualIgnoreCase("Sms") || SmsService.IsValidPhone(user))
            return await SendSmsCode(model, ip);

        throw new NotSupportedException();
    }

    private async Task<VerifyCodeRecord> SendSmsCode(VerifyCodeModel model, String ip)
    {
        var mobile = model.Username?.Trim() ?? "";
        if (mobile.IsNullOrEmpty()) throw new XException("手机号不能为空");

        // 校验手机号格式
        if (!SmsService.IsValidPhone(mobile)) throw new XException("手机号格式不正确");

        // 检查短信服务是否启用
        var set = CubeSetting.Current;
        if (!set.EnableSms) throw new XException("短信验证码功能未启用");

        //if (_smsVerifyCode == null) throw new XException("短信服务未配置");
        var config = smsService.GetConfig(TenantContext.CurrentId, model.Action);

        // 检查短信配置是否完整
        if (config.AppKey.IsNullOrEmpty() || config.AppSecret.IsNullOrEmpty())
            throw new XException("短信AccessKey未配置，请在系统参数中配置AppKey和AppSecret");

        if (config.SignName.IsNullOrEmpty())
            throw new XException("短信签名未配置，请在系统参数中配置SignName");

        //var ip = UserHost;
        var ipKey = $"SmsLogin:IP:{ip}";

        // 防止频繁发送（IP限制）
        var ipCount = _cache.Get<Int32>(ipKey);
        if (ipCount >= 5) throw new XException("发送频繁，请稍后再试");

        // 防止频繁发送（手机号限制，60秒内只能发一次）
        var lastSend = _cache.Get<DateTime>($"SmsLogin:LastSend:{mobile}");
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
            var codeKey = $"SmsLogin:Code:{mobile}";
            _cache.Set(codeKey, code, config.Expire);

            // 记录发送时间
            _cache.Set($"SmsLogin:LastSend:{mobile}", DateTime.Now, 60);

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

    private async Task<VerifyCodeRecord> SendMailCode(VerifyCodeModel model, String ip)
    {
        var mail = model.Username?.Trim() ?? "";
        if (mail.IsNullOrEmpty()) throw new XException("邮件地址不能为空");

        // 检查短信服务是否启用
        var set = CubeSetting.Current;
        if (!set.EnableMail) throw new XException("邮件验证码功能未启用");

        var config = mailService.GetConfig(TenantContext.CurrentId, model.Action);

        var ipKey = $"MailLogin:IP:{ip}";

        // 防止频繁发送（IP限制）
        var ipCount = _cache.Get<Int32>(ipKey);
        if (ipCount >= 5) throw new XException("发送频繁，请稍后再试");

        // 防止频繁发送（手机号限制，60秒内只能发一次）
        var lastSend = _cache.Get<DateTime>($"MailLogin:LastSend:{mail}");
        if (lastSend > DateTime.MinValue && (DateTime.Now - lastSend).TotalSeconds < 60)
        {
            var wait = 60 - (Int32)(DateTime.Now - lastSend).TotalSeconds;
            throw new XException($"请{wait}秒后再试");
        }

        try
        {
            // 发送短信验证码
            var code = SmsService.GenerateVerifyCode();
            var rs = await mailService.SendVerifyCode(model.Action, mail, code, config);
            if (rs == null || !rs.Success)
                throw new XException("邮件发送失败");

            // 缓存验证码用于校验
            var codeKey = $"MailLogin:Code:{mail}";
            _cache.Set(codeKey, code, config.Expire);

            // 记录发送时间
            _cache.Set($"MailLogin:LastSend:{mail}", DateTime.Now, 60);

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
}
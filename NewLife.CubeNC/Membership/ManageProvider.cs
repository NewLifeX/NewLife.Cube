using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NewLife.Common;
using NewLife.Cube.Extensions;
using NewLife.Log;
using NewLife.Model;
using XCode.Membership;
using IServiceCollection = Microsoft.Extensions.DependencyInjection.IServiceCollection;

namespace NewLife.Cube
{
    /// <inheritdoc />
    public class ManageProvider2 : ManageProvider
    {
        #region 静态实例
        static ManageProvider2()
        {
            // 此处实例化会触发父类静态构造函数
            var ioc = ObjectContainer.Current;
            ioc.Register<IManageProvider, ManageProvider2>()
            .Register<IManageUser, UserX>();
        }

        internal static IHttpContextAccessor Context;
        #endregion

        #region 属性
        /// <summary>保存于Cookie的凭证</summary>
        public String CookieKey { get; set; } = "Admin";

        /// <summary>保存于Session的凭证</summary>
        public String SessionKey { get; set; } = "Admin";
        #endregion

        /// <summary>当前管理提供者</summary>
        public new static IManageProvider Provider => ObjectContainer.Current.ResolveInstance<IManageProvider>();

        #region IManageProvider 接口
        /// <summary>获取当前用户</summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IManageUser GetCurrent(IServiceProvider context = null)
        {
            var ctx = (context?.GetService<IHttpContextAccessor>() ?? Context)
                ?.HttpContext;
            if (ctx == null) return null;

            try
            {
                var user = ctx.Items["CurrentUser"] as IManageUser;
                if (user != null) return user;

                var session = ctx.Items["Session"] as IDictionary<String, Object>;
                //var type = ObjectContainer.Current.ResolveType<IManageUser>();

                user = session?[SessionKey] as IManageUser;
                ctx.Items["CurrentUser"] = user;

                return user;
            }
            catch (InvalidOperationException ex)
            {
                // 这里捕获一下，防止初始化应用中session还没初始化好报的异常
                // 这里有个问题就是这里的ctx会有两个不同的值
                XTrace.WriteException(ex);
                return null;
            }
        }

        /// <summary>设置当前用户</summary>
        /// <param name="user"></param>
        /// <param name="context"></param>
        public override void SetCurrent(IManageUser user, IServiceProvider context = null)
        {
            var ctx = (context?.GetService<IHttpContextAccessor>() ?? Context)
                ?.HttpContext;
            if (ctx == null) return;

            ctx.Items["CurrentUser"] = user;

            var session = ctx.Items["Session"] as IDictionary<String, Object>;
            if (session == null) return;

            var key = SessionKey;
            // 特殊处理注销
            if (user == null)
            {
                // 修改Session
                session.Remove(key);

                //// 下线功能暂时失效，通过接口取值报错
                //if (ss.Get<IAuthUser>(key) is IAuthUser au)
                //{
                //    au.Online = false;
                //    au.Save();
                //}
            }
            else
            {
                // 修改Session
                session[key] = user;
            }
        }

        /// <summary>登录</summary>
        /// <param name="name"></param>
        /// <param name="password"></param>
        /// <param name="rememberme">是否记住密码</param>
        /// <returns></returns>
        public override IManageUser Login(String name, String password, Boolean rememberme)
        {
            var user = UserX.Login(name, password, rememberme);
            Current = user;

            var expire = TimeSpan.FromMinutes(0);
            if (rememberme && user != null)
            {
                expire = TimeSpan.FromDays(365);
            }
            else
            {
                var set = Setting.Current;
                if (set.SessionTimeout > 0)
                    expire = TimeSpan.FromSeconds(set.SessionTimeout);
            }

            var context = Context?.HttpContext;
            this.SaveCookie(user, expire, context);

            return user;
        }

        /// <summary>注销</summary>
        public override void Logout()
        {
            // 注销时销毁所有Session
            var context = Context?.HttpContext;
            var session = context.Items["Session"] as IDictionary<String, Object>;
            session?.Clear();

            // 销毁Cookie
            this.SaveCookie(null, TimeSpan.FromDays(-1), context);
            base.Logout();
        }
        #endregion
    }

    /// <summary>管理提供者助手</summary>
    public static class ManagerProviderHelper
    {
        /// <summary>设置当前用户</summary>
        /// <param name="provider">提供者</param>
        /// <param name="context">Http上下文，兼容NetCore</param>
        public static void SetPrincipal(this IManageProvider provider, IServiceProvider context = null)
        {
            var ctx = context.GetService<IHttpContextAccessor>()?.HttpContext;
            //var ctx = context as HttpContext ?? HttpContext.Current;
            if (ctx == null) return;

            var user = provider.GetCurrent(context);
            if (user == null) return;

            if (!(user is IIdentity id) || ctx.User?.Identity == id) return;

            // 角色列表
            var roles = new List<String>();
            if (user is IUser user2) roles.AddRange(user2.Roles.Select(e => e + ""));

            var up = new GenericPrincipal(id, roles.ToArray());
            ctx.User = up;
            Thread.CurrentPrincipal = up;
        }

        /// <summary>尝试登录。如果Session未登录则借助Cookie</summary>
        /// <param name="provider">提供者</param>
        /// <param name="context">Http上下文，兼容NetCore</param>
        public static IManageUser TryLogin(this IManageProvider provider, IServiceProvider context = null)
        {
            // 判断当前登录用户
            var user = provider.GetCurrent(context);
            if (user == null)
            {
                // 尝试从Cookie登录
                user = provider.LoadCookie(true, context);
                if (user != null) provider.SetCurrent(user, context);
            }

            // 设置前端当前用户
            if (user != null) provider.SetPrincipal(context);

            return user;
        }

        #region Cookie
        private static String GetCookieKey(IManageProvider provider)
        {
            var key = (provider as ManageProvider2)?.CookieKey;
            if (key.IsNullOrEmpty()) key = "cube_user";

            return key;
        }

        /// <summary>从Cookie加载用户信息</summary>
        /// <param name="provider">提供者</param>
        /// <param name="autologin">是否自动登录</param>
        /// <param name="context">Http上下文，兼容NetCore</param>
        /// <returns></returns>
        public static IManageUser LoadCookie(this IManageProvider provider, Boolean autologin = true, IServiceProvider context = null)
        {
            var key = GetCookieKey(provider);

            if (context == null) return null;
            var req = context.GetService<IHttpContextAccessor>()?.HttpContext?.Request;
            var cookie = req?.Cookies[key];
            if (cookie == null) return null;

            var m = new CookieModel();
            if (!m.Read(req.Cookies, key, SysConfig.Current.InstallTime.ToFullString())) return null;

            var user = HttpUtility.UrlDecode(m.UserName);
            //var user = HttpUtility.UrlDecode(cookie["u"]);
            //var pass = cookie["p"];
            //var exp = cookie["e"].ToInt(-1);
            if (user.IsNullOrEmpty() || m.Password.IsNullOrEmpty()) return null;

            // 判断有效期
            //var expire = exp.ToDateTime();
            if (m.Expire < DateTime.Now) return null;

            var u = provider.FindByName(user);
            if (u == null || !u.Enable) return null;

            var mu = u as IAuthUser;
            if (!m.Password.EqualIgnoreCase(mu.Password.MD5())) return null;

            // 保存登录信息
            if (autologin)
            {
                mu.SaveLogin(null);

                var ctx = req.HttpContext;
                LogProvider.Provider.WriteLog("用户", "自动登录", $"{user} Time={m.Time} Expire={m.Expire}", u.ID, u + "", ip: ctx.GetUserHost());
            }

            return u;
        }

        /// <summary>保存用户信息到Cookie</summary>
        /// <param name="provider">提供者</param>
        /// <param name="user">用户</param>
        /// <param name="expire">过期时间</param>
        /// <param name="context">Http上下文，兼容NetCore</param>
        public static void SaveCookie(this IManageProvider provider, IManageUser user, TimeSpan expire, HttpContext context)
        {
            if (context == null) return;

            var req = context.Request;
            var res = context.Response;
            if (req == null || res == null) return;

            var key = GetCookieKey(provider);
            if (user is IAuthUser au)
            {
                var u = HttpUtility.UrlEncode(user.Name);
                var p = !au.Password.IsNullOrEmpty() ? au.Password.MD5() : null;

                var m = new CookieModel
                {
                    UserName = u,
                    Password = p,
                    Time = DateTime.Now,
                    Expire = DateTime.Now.Add(expire)
                };
                m.Write(res.Cookies, key, SysConfig.Current.InstallTime.ToFullString());
            }
            else
            {
                res.Cookies.Append(key, "", new CookieOptions()
                {
                    Expires = DateTime.Now.AddYears(-1)
                });
            }
        }

        class CookieModel
        {
            #region 属性
            public String UserName { get; set; }
            public String Password { get; set; }
            public DateTime Time { get; set; }
            public DateTime Expire { get; set; }
            public String Sign { get; set; }
            #endregion

            #region 方法
            public Boolean Read(IRequestCookieCollection cookie, String cookieKey, String key)
            {
                var cookies = cookie[cookieKey];
                var cookieDic = cookies.SplitAsDictionary("=", "&");

                UserName = cookieDic["u"];
                Password = cookieDic["p"];
                Time = (cookieDic["t"] + "").ToInt().ToDateTime();
                Expire = (cookieDic["e"] + "").ToInt().ToDateTime();
                Sign = cookieDic["s"];

                var str = $"u={UserName}&p={Password}&t={Time.ToInt()}&e={Expire.ToInt()}&k={key}";

                return str.MD5() == Sign;
            }

            public void Write(IResponseCookies cookie, String cookieKey, String key)
            {
                var cookieOptions = new CookieOptions
                {
                    //HttpOnly = true,
                    Expires = Expire
                };

                var str = $"u={UserName}&p={Password}&t={Time.ToInt()}&e={Expire.ToInt()}&k={key}";
                Sign = str.MD5();

                str = str + "&s=" + Sign;

                cookie.Append(cookieKey, str, cookieOptions);
            }
            #endregion
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
            //service.TryAddSingleton(ManageProvider2.Provider);
        }

        /// <summary>
        /// 使用管理提供者
        /// </summary>
        /// <param name="app"></param>
        public static void UseManagerProvider(this IApplicationBuilder app)
        {
            ManageProvider2.Context = app.ApplicationServices.GetService<IHttpContextAccessor>();
        }
    }
}
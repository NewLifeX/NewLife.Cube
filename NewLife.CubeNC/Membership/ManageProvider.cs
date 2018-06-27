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
using NewLife.CubeNC.Extensions;
using NewLife.Model;
using XCode.Membership;
using IServiceCollection = Microsoft.Extensions.DependencyInjection.IServiceCollection;

namespace NewLife.CubeNC.Membership
{
    public class DefaultManageProviderForCore : ManageProvider<UserX>
    {

        #region 静态实例
        static DefaultManageProviderForCore()
        {
            var ioc = ObjectContainer.Current;
            ioc.Register<IManageProvider, DefaultManageProviderForCore>();//此处实例化会触发父类静态构造函数
        }

        internal static IHttpContextAccessor Context;

        #endregion

        /// <summary>当前管理提供者</summary>
        public new static IManageProvider Provider => ObjectContainer.Current.ResolveInstance<IManageProvider>();

        #region IManageProvider 接口

        //此静态成员要重写，否则访问父类Current的时候会调用父类的GetCurrent、SetCurrent
        public override IManageUser Current { get => GetCurrent(); set => SetCurrent(value); }

        /// <summary>获取当前用户</summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IManageUser GetCurrent(IServiceProvider context = null) => 
            (context?.GetService<IHttpContextAccessor>()?? Context)
                ?.HttpContext
                ?.Session
                ?.Get<IManageUser>(SessionKey);

        /// <summary>设置当前用户</summary>
        /// <param name="user"></param>
        /// <param name="context"></param>
        public override void SetCurrent(IManageUser user, IServiceProvider context = null)
        {
            var ss = ((context)?.GetService<IHttpContextAccessor>()?? Context)
                ?.HttpContext.Session;
            if (ss == null) return;

            var key = SessionKey;
            // 特殊处理注销
            if (user == null)
            {
                // 修改Session
                ss.Remove(key);

                if (ss.Get<IAuthUser>(key) is IAuthUser au)
                {
                    au.Online = false;
                    au.Save();
                }
            }
            else
            {
                // 修改Session
                ss.Set(key, user.ToBytes());
            }
        }

        public override IManageUser Login(string name, string password, bool rememberme, IServiceProvider context = null)
        {
            var user = UserX.Login(name, password, rememberme);
            Current = user;

            var expire = TimeSpan.FromMinutes(60*2);//有效期两个小时
                //TimeSpan.FromDays(0);
            if (rememberme && user != null) expire = TimeSpan.FromDays(365);
            this.SaveCookie(user, expire, context);
            return user;
        }

        public override void Logout(IServiceProvider ctx = null)
        {
            // 注销时销毁所有Session
            var context = ctx?.GetService<IHttpContextAccessor>()
                ?.HttpContext;
            var ss = context?.Session;
            ss?.Clear();

            // 销毁Cookie
            this.SaveCookie(null, TimeSpan.FromDays(-1), ctx);
            base.Logout(ctx);
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
            var key = (provider as ManageProvider)?.CookieKey;
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
                LogProvider.Provider.WriteLog("用户", "自动登录", $"{user} Time={m.Time} Expire={m.Expire}", u.ID, u + "");
            }

            return u;
        }

        /// <summary>保存用户信息到Cookie</summary>
        /// <param name="provider">提供者</param>
        /// <param name="user">用户</param>
        /// <param name="expire">过期时间</param>
        /// <param name="context">Http上下文，兼容NetCore</param>
        public static void SaveCookie(this IManageProvider provider, IManageUser user, TimeSpan expire, IServiceProvider context = null)
        {
            if (context == null) return;

            var httpContext = context?.GetService<IHttpContextAccessor>()?.HttpContext;

            var req = httpContext?.Request;
            var res = httpContext?.Response;
            if (req == null || res == null) return;

            var key = GetCookieKey(provider);
            var reqcookie = req.Cookies[key];
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
                res.Cookies.Append(key, null, new CookieOptions()
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

            public void Write(IResponseCookies cookie,String cookieKey, String key)
            {
                var cookieOptions = new CookieOptions();

                cookieOptions.HttpOnly = true;
                cookieOptions.Expires = Expire;

                var str = $"u={UserName}&p={Password}&t={Time.ToInt()}&e={Expire.ToInt()}&k={key}";
                Sign = str.MD5();

                str = str + "&s=" + Sign;

                cookie.Append(cookieKey, str, cookieOptions);
            }
            #endregion
        }

        /// <summary>
        /// 添加管理提供者
        /// </summary>
        /// <param name="service"></param>
        public static void AddManageProvider(this IServiceCollection service)
        {
            service.TryAddSingleton<IHttpContextAccessor,HttpContextAccessor>();
            Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions
                .AddSingleton<ManageProvider, DefaultManageProviderForCore>(service);
        }

        /// <summary>
        /// 使用管理提供者
        /// </summary>
        /// <param name="app"></param>
        public static void UseManagerProvider(this IApplicationBuilder app)
        {
            DefaultManageProviderForCore.Context = app.ApplicationServices.GetService<IHttpContextAccessor>();
        }
        #endregion
    }
}

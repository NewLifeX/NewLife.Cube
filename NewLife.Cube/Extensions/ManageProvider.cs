using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Web;
using NewLife.Common;
using NewLife.Model;
#if !__CORE__
using System.Web.SessionState;
#endif

namespace XCode.Membership
{
    /// <summary>管理提供者</summary>
    public class DefaultManageProvider : ManageProvider
    {
        #region 静态实例
        //static DefaultManageProvider()
        //{
        //    var ioc = ObjectContainer.Current;
        //    // 外部管理提供者需要手工覆盖
        //    ioc.Register<IManageProvider, DefaultManageProvider>();
        //}
        #endregion

        #region 属性
        /// <summary>保存于Cookie的凭证</summary>
        public String CookieKey { get; set; } = "Admin";

        /// <summary>保存于Session的凭证</summary>
        public String SessionKey { get; set; } = "Admin";
        #endregion

        #region IManageProvider 接口
        /// <summary>获取当前用户</summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IManageUser GetCurrent(IServiceProvider context = null)
        {
#if !__CORE__
            if (context == null) context = HttpContext.Current;
            var ss = context.GetService<HttpSessionState>();
            if (ss == null) return null;

            // 从Session中获取
            return ss[SessionKey] as IManageUser;
#else
            return null;
#endif
        }

        /// <summary>设置当前用户</summary>
        /// <param name="user"></param>
        /// <param name="context"></param>
        public override void SetCurrent(IManageUser user, IServiceProvider context = null)
        {
#if !__CORE__
            if (context == null) context = HttpContext.Current;
            var ss = context.GetService<HttpSessionState>();
            if (ss == null) return;

            var key = SessionKey;
            // 特殊处理注销
            if (user == null)
            {
                // 修改Session
                ss.Remove(key);

                if (ss[key] is IAuthUser au)
                {
                    au.Online = false;
                    au.Save();
                }
            }
            else
            {
                // 修改Session
                ss[key] = user;
            }
#else
#endif
        }

        /// <summary>登录</summary>
        /// <param name="name"></param>
        /// <param name="password"></param>
        /// <param name="rememberme">是否记住密码</param>
        /// <returns></returns>
        public override IManageUser Login(String name, String password, Boolean rememberme)
        {
            var user = base.Login(name, password, rememberme);
            if (user == null) return null;

            var expire = TimeSpan.FromDays(0);
            if (rememberme && user != null) expire = TimeSpan.FromDays(365);
            this.SaveCookie(user, expire);

            return user;
        }

        /// <summary>注销</summary>
        public override void Logout()
        {
            base.Logout();

#if !__CORE__
            // 注销时销毁所有Session
            var context = HttpContext.Current;
            var ss = context?.Session;
            ss?.Clear();
#endif

            // 销毁Cookie
            this.SaveCookie(null, TimeSpan.FromDays(-1));
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
#if !__CORE__
            //var ctx = context as Microsoft.AspNetCore.Http.HttpContext;
            var ctx = context as HttpContext ?? HttpContext.Current;
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
#endif
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
            var key = (provider as DefaultManageProvider)?.CookieKey;
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
#if !__CORE__
            var key = GetCookieKey(provider);

            if (context == null) context = HttpContext.Current;
            var req = context.GetService<HttpRequest>();
            var cookie = req?.Cookies[key];
            if (cookie == null) return null;

            var m = new CookieModel();
            if (!m.Read(cookie, SysConfig.Current.InstallTime.ToFullString())) return null;

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
#else
            return null;
#endif
        }

        /// <summary>保存用户信息到Cookie</summary>
        /// <param name="provider">提供者</param>
        /// <param name="user">用户</param>
        /// <param name="expire">过期时间</param>
        /// <param name="context">Http上下文，兼容NetCore</param>
        public static void SaveCookie(this IManageProvider provider, IManageUser user, TimeSpan expire, IServiceProvider context = null)
        {
#if !__CORE__
            if (context == null) context = HttpContext.Current;

            var req = context?.GetService<HttpRequest>();
            var res = context?.GetService<HttpResponse>();
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
                m.Write(res.Cookies[key], SysConfig.Current.InstallTime.ToFullString());
            }
            else
            {
                var cookie = res.Cookies[key];
                cookie.Value = null;
                cookie.Expires = DateTime.Now.AddYears(-1);
            }
#endif
        }

#if !__CORE__
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
            public Boolean Read(HttpCookie cookie, String key)
            {
                UserName = cookie["u"];
                Password = cookie["p"];
                Time = (cookie["t"] + "").ToInt().ToDateTime();
                Expire = (cookie["e"] + "").ToInt().ToDateTime();
                Sign = cookie["s"];

                var str = $"u={UserName}&p={Password}&t={Time.ToInt()}&e={Expire.ToInt()}&k={key}";

                return str.MD5() == Sign;
            }

            public void Write(HttpCookie cookie, String key)
            {
                cookie.HttpOnly = true;
                cookie["u"] = UserName;
                cookie["p"] = Password;

                var dt = Time;
                cookie["t"] = dt.ToInt() + "";

                var exp = Expire;
                cookie.Expires = exp;
                cookie["e"] = exp.ToInt() + "";

                var str = $"u={UserName}&p={Password}&t={Time.ToInt()}&e={Expire.ToInt()}&k={key}";
                Sign = str.MD5();

                cookie["s"] = Sign;
            }
            #endregion
        }
#endif
        #endregion
    }
}
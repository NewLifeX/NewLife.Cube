using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Web;
using NewLife.Common;
using NewLife.Model;
using NewLife.Cube;
using System.Web.SessionState;
using JwtBuilder = NewLife.Web.JwtBuilder;
using NewLife.Log;

namespace XCode.Membership
{
    /// <summary>管理提供者</summary>
    public class DefaultManageProvider : ManageProvider
    {
        #region 静态实例
        #endregion

        #region 属性
        /// <summary>保存于Session的凭证</summary>
        public String SessionKey { get; set; } = "Admin";
        #endregion

        #region IManageProvider 接口
        /// <summary>获取当前用户</summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IManageUser GetCurrent(IServiceProvider context = null)
        {
            if (context == null) context = HttpContext.Current;
            var ss = context.GetService<HttpSessionState>();
            if (ss == null) return null;

            // 从Session中获取
            return ss[SessionKey] as IManageUser;
        }

        /// <summary>设置当前用户</summary>
        /// <param name="user"></param>
        /// <param name="context"></param>
        public override void SetCurrent(IManageUser user, IServiceProvider context = null)
        {
            if (context == null) context = HttpContext.Current;
            var ss = context.GetService<HttpSessionState>();
            if (ss == null) return;

            var key = SessionKey;
            // 特殊处理注销
            if (user == null)
                ss.Remove(key);
            else
                ss[key] = user;
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
            if (rememberme)
            {
                expire = TimeSpan.FromDays(365);
            }
            else
            {
                var set = NewLife.Cube.Setting.Current;
                if (set.SessionTimeout > 0)
                    expire = TimeSpan.FromSeconds(set.SessionTimeout);
            }

            var context = HttpContext.Current;
            this.SaveCookie(user, expire, context);

            return user;
        }

        /// <summary>注销</summary>
        public override void Logout()
        {
            base.Logout();

            // 注销时销毁所有Session
            var context = HttpContext.Current;
            var ss = context?.Session;
            ss?.Clear();

            // 销毁Cookie
            this.SaveCookie(null, TimeSpan.FromDays(-1), context);
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
        }

        /// <summary>尝试登录。如果Session未登录则借助Cookie</summary>
        /// <param name="provider">提供者</param>
        /// <param name="context">Http上下文，兼容NetCore</param>
        public static IManageUser TryLogin(this IManageProvider provider, HttpContext context)
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

        /// <summary>生成令牌</summary>
        /// <returns></returns>
        private static JwtBuilder GetJwt()
        {
            var set = NewLife.Cube.Setting.Current;

            // 生成令牌
            var ss = set.JwtSecret.Split(':');
            var jwt = new JwtBuilder
            {
                Algorithm = ss[0],
                Secret = ss[1],
            };

            return jwt;
        }

        #region Cookie
        /// <summary>从Cookie加载用户信息</summary>
        /// <param name="provider">提供者</param>
        /// <param name="autologin">是否自动登录</param>
        /// <param name="context">Http上下文，兼容NetCore</param>
        /// <returns></returns>
        public static IManageUser LoadCookie(this IManageProvider provider, Boolean autologin, HttpContext context)
        {
            var key = "token";
            var req = context?.Request;
            var token = req?.Cookies[key]?.Value;
            if (token.IsNullOrEmpty()) return null;

            var jwt = GetJwt();
            if (!jwt.TryDecode(token, out var msg))
            {
                XTrace.WriteLine("令牌无效：{0}, token={1}", msg, token);

                return null;
            }

            var user = jwt.Subject;
            if (user.IsNullOrEmpty()) return null;

            //// 判断有效期
            //if (jwt.Expire < DateTime.Now)
            //{
            //    XTrace.WriteLine("令牌过期：{0} {1}", jwt.Expire, token);

            //    return null;
            //}

            var u = provider.FindByName(user);
            if (u == null || !u.Enable) return null;

            // 保存登录信息
            if (autologin && u is IAuthUser mu)
            {
                mu.SaveLogin(null);

                LogProvider.Provider.WriteLog("用户", "自动登录", true, $"{user} Time={jwt.IssuedAt} Expire={jwt.Expire} Token={token}", u.ID, u + "", ip: req.RequestContext.HttpContext.GetUserHost());
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
            var res = context?.Response;
            if (res == null) return;

            var key = "token";
            if (user == null)
            {
                var cookie = new HttpCookie(key) { Value = null, Expires = DateTime.Now.AddDays(-1) };
                res.Cookies.Set(cookie);
            }
            else
            {
                // 令牌有效期，默认2小时
                var exp = DateTime.Now.Add(expire.TotalSeconds > 0 ? expire : TimeSpan.FromHours(2));
                var jwt = GetJwt();
                jwt.Subject = user.Name;
                jwt.Expire = exp;

                var token = jwt.Encode(null);
                var cookie = new HttpCookie(key) { Value = token };
                if (expire.TotalSeconds > 0) cookie.Expires = DateTime.Now.Add(expire);
                res.Cookies.Set(cookie);
            }
        }
        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Net.Http.Headers;
using NewLife.Cube.Entity;
using NewLife.Cube.Extensions;
using NewLife.Cube.Web;
using NewLife.Log;
using NewLife.Model;
using NewLife.Serialization;
using XCode;
using XCode.Membership;
using IServiceCollection = Microsoft.Extensions.DependencyInjection.IServiceCollection;
using JwtBuilder = NewLife.Web.JwtBuilder;

namespace NewLife.Cube
{
    /// <inheritdoc />
    public class ManageProvider2 : ManageProvider
    {
        #region 静态实例
        internal static IHttpContextAccessor Context;

        /// <summary>
        /// 节点路由
        /// </summary>
        public static IEndpointRouteBuilder EndpointRoute { get; set; }
        #endregion

        #region 属性
        /// <summary>保存于Session的凭证</summary>
        public String SessionKey { get; set; } = "Admin";
        #endregion

        ///// <summary>当前管理提供者</summary>
        //public new static IManageProvider Provider => ObjectContainer.Current.ResolveInstance<IManageProvider>();

        #region IManageProvider 接口
        /// <summary>获取当前用户</summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IManageUser GetCurrent(IServiceProvider context = null)
        {
            var ctx = (ModelExtension.GetService<IHttpContextAccessor>(context) ?? Context)?.HttpContext;
            if (ctx == null) return null;

            try
            {
                if (ctx.Items["CurrentUser"] is IManageUser user) return user;

                var session = ctx.Items["Session"] as IDictionary<String, Object>;

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
            var ctx = (ModelExtension.GetService<IHttpContextAccessor>(context) ?? Context)
                ?.HttpContext;
            if (ctx == null) return;

            ctx.Items["CurrentUser"] = user;

            var session = ctx.Items["Session"] as IDictionary<String, Object>;
            if (session == null) return;

            var key = SessionKey;
            // 特殊处理注销
            if (user == null)
            {
                session.Remove(key);
                session.Remove("userId");
            }
            else
            {
                session[key] = user;
                session["userId"] = user.ID;
            }
        }

        /// <summary>登录</summary>
        /// <param name="name"></param>
        /// <param name="password"></param>
        /// <param name="remember">是否记住密码</param>
        /// <returns></returns>
        public override IManageUser Login(String name, String password, Boolean remember)
        {
            IManageUser user = null;

            // OAuth密码模式登录
            var oauths = OAuthConfig.GetValids(GrantTypes.Password);
            if (oauths.Count > 0)
                user = LoginByOAuth(oauths[0], name, password);
            else
                user = base.Login(name, password, remember);

            user = CheckAgent(user) as User;
            Current = user;

            // 过期时间
            var set = Setting.Current;
            var expire = TimeSpan.FromMinutes(0);
            if (remember && user != null)
            {
                expire = TimeSpan.FromDays(365);
            }
            else
            {
                if (set.SessionTimeout > 0)
                    expire = TimeSpan.FromSeconds(set.SessionTimeout);
            }

            // 保存Cookie
            var context = Context?.HttpContext;
            this.SaveCookie(user, expire, context);

            return user;
        }

        private SsoClient _client;
        private IManageUser LoginByOAuth(OAuthConfig oa, String username, String password)
        {
            if (_client == null)
            {
                _client = new SsoClient
                {
                    Server = oa.Server,
                    AppId = oa.AppId,
                    Secret = oa.Secret,
                    SecurityKey = oa.SecurityKey,
                };
            }

            //var ti = _client.GetToken(username, password).Result;
            //var ui = _client.GetUser(ti.AccessToken).Result as User;
            var ui = _client.UserAuth(username, password).Result;

            var set = Setting.Current;
            var log = LogProvider.Provider;

            // 仅验证登录，不要角色信息
            if (FindByName(username) is not User user)
            {
                if (!set.AutoRegister && !oa.AutoRegister)
                {
                    log.WriteLog(typeof(User), "SSO登录", false, $"无法找到[{username}]，且没有打开自动注册", 0, username);

                    throw new XException($"无法找到[{username}]，且没有打开自动注册");
                }

                user = new User
                {
                    Code = ui["usercode"] as String,
                    Name = username,
                    DisplayName = ui["nickname"] as String,

                    Enable = true,
                    RegisterTime = DateTime.Now,
                };

                // 新注册用户采用魔方默认角色
                var defRole = oa.AutoRole;
                if (defRole.IsNullOrEmpty()) defRole = set.DefaultRole;
                user.RoleID = Role.GetOrAdd(defRole).ID;
            }

            user.Logins++;
            user.LastLogin = DateTime.Now;
            user.Save();

            log.WriteLog(user.GetType(), "OAuth登录", true, $"用户[{user}]使用[{username}]登录[{_client.AppId}]成功！" + Environment.NewLine + ui.ToJson());

            return user;
        }

        /// <summary>检查委托代理</summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public IManageUser CheckAgent(IManageUser user)
        {
            if (user == null) return user;

            // 查找该用户是否有可用待立项，按照创建代理的先后顺序
            var list = PrincipalAgent.GetAllValidByAgentId(user.ID);
            if (list.Count == 0) return user;

            // 脏数据检查
            foreach (var item in list)
            {
                // 没有次数或者已过期，则禁用
                if (item.Enable && (item.Times == 0 || item.Expire.Year > 2000 && item.Expire < DateTime.Now))
                {
                    item.Enable = false;
                    item.Update();
                }
            }

            // 查找一个可用项
            var pa = list.FirstOrDefault(e => e.Enable);
            if (pa == null || pa.Principal == null) return user;

            var roles = pa.Principal?.Roles;
            if (roles != null && roles.Any(e => e.IsSystem))
            {
                pa.Enable = false;
                pa.Remark = "安全起见，不得代理系统管理员";
                pa.Update();

                LogProvider.Provider.WriteLog("用户", "代理", false, $"安全起见，[{pa.AgentName}]不得代理系统管理员[{pa.PrincipalName}]的身份权限", pa.AgentId, pa.AgentName);

                return user;
            }

            pa.Times--;
            if (pa.Times == 0) pa.Enable = false;
            pa.Update();

            LogProvider.Provider.WriteLog("用户", "委托", true, $"委托[{pa.AgentName}]使用[{pa.PrincipalName}]的身份权限", pa.PrincipalId, pa.PrincipalName);
            LogProvider.Provider.WriteLog("用户", "代理", true, $"[{pa.AgentName}]代理使用[{pa.PrincipalName}]的身份权限", pa.AgentId, pa.AgentName);

            return pa.Principal as IManageUser;
        }

        /// <summary>注销</summary>
        public override void Logout()
        {
            // 注销时销毁所有Session
            var context = Context?.HttpContext;
            var session = context.Items["Session"] as IDictionary<String, Object>;
            session?.Clear();

            // 销毁Cookie
            this.SaveCookie(null, TimeSpan.Zero, context);

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
            var ctx = ModelExtension.GetService<IHttpContextAccessor>(context)?.HttpContext;
            if (ctx == null) return;

            var user = provider.GetCurrent(context);
            if (user == null) return;

            if (user is not IIdentity id || ctx.User?.Identity == id) return;

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
            var serviceProvider = context?.RequestServices;

            // 判断当前登录用户
            var user = provider.GetCurrent(serviceProvider);
            if (user == null)
            {
                // 尝试从Cookie登录
                user = provider.LoadCookie(true, context);
                if (user != null) provider.SetCurrent(user, serviceProvider);
            }

            // 设置前端当前用户
            if (user != null) provider.SetPrincipal(serviceProvider);

            return user;
        }

        /// <summary>生成令牌</summary>
        /// <returns></returns>
        public static JwtBuilder GetJwt()
        {
            var set = Setting.Current;

            // 生成令牌
            var ss = set.JwtSecret?.Split(':');
            if (ss == null || ss.Length < 2) throw new InvalidOperationException("未设置JWT算法和密钥");

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
            var token = req?.Cookies[key];

            // 尝试从url中获取token
            if (token.IsNullOrEmpty() || token.Split(".").Length != 3) token = req?.Query["token"];
            if (token.IsNullOrEmpty() || token.Split(".").Length != 3) token = req?.Query["jwtToken"];

            // 尝试从头部获取token
            if (token.IsNullOrEmpty() || token.Split(".").Length != 3) token = req?.Headers[HeaderNames.Authorization];

            if (token.IsNullOrEmpty() || token.Split(".").Length != 3) return null;

            token = token.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);

            var jwt = GetJwt();
            if (!jwt.TryDecode(token, out var msg))
            {
                XTrace.WriteLine("令牌无效：{0}, token={1}", msg, token);

                return null;
            }

            var user = jwt.Subject;
            if (user.IsNullOrEmpty()) return null;

            // 判断有效期
            if (jwt.Expire < DateTime.Now)
            {
                XTrace.WriteLine("令牌过期：{0} {1}", jwt.Expire, token);

                return null;
            }

            var u = provider.FindByName(user);
            if (u == null || !u.Enable) return null;

            // 保存登录信息。如果是json请求，不用记录自动登录
            if (autologin && u is IAuthUser mu && !req.IsAjaxRequest())
            {
                mu.SaveLogin(null);

                LogProvider.Provider.WriteLog("用户", "自动登录", true, $"{user} Time={jwt.IssuedAt} Expire={jwt.Expire} Token={token}", u.ID, u + "", ip: context.GetUserHost());
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

            var option = new CookieOptions();
            option.SameSite = (Microsoft.AspNetCore.Http.SameSiteMode)Setting.Current.SameSiteMode;

            var token = "";
            if (user != null)
            {
                // 令牌有效期，默认2小时
                var exp = DateTime.Now.Add(expire.TotalSeconds > 0 ? expire : TimeSpan.FromHours(2));
                var jwt = GetJwt();
                jwt.Subject = user.Name;
                jwt.Expire = exp;

                token = jwt.Encode(null);
                if (expire.TotalSeconds > 0) option.Expires = DateTimeOffset.Now.Add(expire);
            }
            else
            {
                option.Expires = DateTimeOffset.MinValue;
            }
            res.Cookies.Append("token", token, option);

            context.Items["jwtToken"] = token;
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
        }

        /// <summary>
        /// 使用管理提供者
        /// </summary>
        /// <param name="app"></param>
        public static void UseManagerProvider(this IApplicationBuilder app)
        {
            XTrace.WriteLine("初始化ManageProvider");

            var provider = app.ApplicationServices;
            ManageProvider.Provider = ModelExtension.GetService<IManageProvider>(provider);
            //ManageProvider2.EndpointRoute = (IEndpointRouteBuilder)app.Properties["__EndpointRouteBuilder"];
            ManageProvider2.Context = ModelExtension.GetService<IHttpContextAccessor>(provider);

            // 初始化数据库
            //_ = Role.Meta.Count;
            EntityFactory.InitConnection("Membership");
            EntityFactory.InitConnection("Log");
            EntityFactory.InitConnection("Cube");
        }
    }
}
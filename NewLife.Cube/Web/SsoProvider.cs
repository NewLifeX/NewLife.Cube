using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NewLife.Cube.Entity;
using NewLife.Web;
using XCode.Membership;
using System.Net.Http;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Security;
using XCode;
using NewLife.Cube.Web.Models;

#if __CORE__
using Microsoft.AspNetCore.Http;
using IHttpRequest = Microsoft.AspNetCore.Http.HttpRequest;
#else
using IHttpRequest = System.Web.HttpRequestBase;
using HttpRequest = System.Web.HttpRequest;
#endif

namespace NewLife.Cube.Web
{
    /// <summary>单点登录提供者</summary>
    public class SsoProvider
    {
        #region 属性
        /// <summary>用户管理提供者</summary>
        public IManageProvider Provider { get; set; }

        ///// <summary>重定向地址。~/Sso/LoginInfo</summary>
        //public String RedirectUrl { get; set; }

        /// <summary>登录成功后跳转地址。~/Admin</summary>
        public String SuccessUrl { get; set; }

        /// <summary>本地登录检查地址。~/Admin/User/Login</summary>
        public String LoginUrl { get; set; }

        /// <summary>安全密钥。keyName$keyValue</summary>
        /// <remarks>
        /// 公钥，用于RSA加密用户密码，在通信链路上保护用户密码安全，可以写死在代码里面。
        /// 密钥前面可以增加keyName，形成keyName$keyValue，用于向服务端指示所使用的密钥标识，方便未来更换密钥。
        /// </remarks>
        public String SecurityKey { get; set; }

        /// <summary>已登录用户</summary>
        public IManageUser Current => Provider.Current;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public SsoProvider()
        {
            Provider = ManageProvider.Provider;
            //RedirectUrl = "~/Sso/LoginInfo";
            SuccessUrl = "~/Admin";
            LoginUrl = "~/Admin/User/Login";
        }

        static SsoProvider()
        {
            // 生成密钥
            new SsoProvider().GetKey(null);
        }
        #endregion

        #region 方法
        /// <summary>获取OAuth客户端</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual OAuthClient GetClient(String name) => OAuthClient.Create(name);

        /// <summary>获取返回地址</summary>
        /// <param name="request">请求对象</param>
        /// <param name="referr">是否使用引用</param>
        /// <returns></returns>
        public virtual String GetReturnUrl(IHttpRequest request, Boolean referr)
        {
            var url = request.Get("r");
            if (url.IsNullOrEmpty()) url = request.Get("redirect_uri");
            if (url.IsNullOrEmpty() && referr)
            {
#if __CORE__
                url = request.Headers["Referer"].FirstOrDefault() + "";
#else
                url = request.UrlReferrer + "";
#endif
            }
            if (!url.IsNullOrEmpty() && url.StartsWithIgnoreCase("http"))
            {
                var baseUri = request.GetRawUrl();

                var uri = new Uri(url);
                if (uri != null && uri.Authority.EqualIgnoreCase(baseUri.Authority)) url = uri.PathAndQuery;
            }

            // 过滤环回重定向
            if (!url.IsNullOrEmpty() && url.StartsWithIgnoreCase("/Sso/Login/")) url = null;

            return url;
        }

        /// <summary>获取登录回跳地址</summary>
        /// <param name="logId"></param>
        /// <returns></returns>
        public virtual String GetLoginUrl(String logId)
        {
            var url = LoginUrl;

            var log = AppLog.FindByID(logId.ToLong());
            if (log != null)
            {
                url += url.Contains("?") ? "&" : "?";
                url += $"ssoAppId={log.AppId}";
            }

            return url.AppendReturn("/Sso/Auth2?id=" + logId);
        }

        /// <summary>获取回调地址</summary>
        /// <param name="request"></param>
        /// <param name="redirectUrl"></param>
        /// <returns></returns>
        public virtual String GetRedirect(IHttpRequest request, String redirectUrl) => redirectUrl.AsUri(request.GetRawUrl()) + "";

        /// <summary>获取连接信息</summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public virtual UserConnect GetConnect(OAuthClient client)
        {
            var openid = client.OpenID;
            if (openid.IsNullOrEmpty()) openid = client.UserName;

            // 根据OpenID找到用户绑定信息
            var uc = UserConnect.FindByProviderAndOpenID(client.Name, openid);
            if (uc == null) uc = new UserConnect { Provider = client.Name, OpenID = openid };

            return uc;
        }

        /// <summary>登录成功</summary>
        /// <param name="client">OAuth客户端</param>
        /// <param name="context">服务提供者。可用于获取HttpContext成员</param>
        /// <param name="uc">用户链接</param>
        /// <param name="forceBind">强行绑定，把第三方账号强行绑定到当前已登录账号</param>
        /// <returns></returns>
        public virtual String OnLogin(OAuthClient client, IServiceProvider context, UserConnect uc, Boolean forceBind)
        {
            //// 强行绑定，把第三方账号强行绑定到当前已登录账号
            //var forceBind = false;
#if __CORE__
            var httpContext = ModelExtension.GetService<IHttpContextAccessor>(context).HttpContext;
            var req = httpContext.Request;
            var ip = httpContext.GetUserHost();
#else
            var req = context.GetService<HttpRequest>();
            var httpContext = req.RequestContext.HttpContext;
            var ip = httpContext.GetUserHost();
#endif

            // 可能因为初始化顺序的问题，导致前面没能给Provider赋值
            var prv = Provider;
            if (prv == null) prv = Provider = ManageProvider.Provider;

            // 检查绑定，新用户的uc.UserID为0
            var user = prv.FindByID(uc.UserID);
            if (forceBind || user == null || !uc.Enable) user = OnBind(uc, client);

            try
            {
                uc.UpdateTime = DateTime.Now;
                uc.Save();
            }
            catch (Exception ex)
            {
                //为了防止某些特殊数据导致的无法正常登录，把所有异常记录到日志当中。忽略错误
                XTrace.WriteException(ex);
            }

            // 如果应用不支持自动注册，此时将得不到用户，跳回去登录页面
            if (user == null) return null;

            // 填充昵称等数据
            Fill(client, user);

            if (user is IAuthUser user3)
            {
                user3.Logins++;
                user3.LastLogin = DateTime.Now;
                user3.LastLoginIP = ip;
                //user3.Save();
                //(user3 as IEntity).Update();
            }
            if (user is IUser user4) user4.Online = true;
            if (user is IEntity entity) entity.Update();

            // 写日志
            var log = LogProvider.Provider;
            log?.WriteLog(typeof(User), "SSO登录", true, $"[{user}]从[{client.Name}]的[{client.UserName ?? client.NickName}]登录", user.ID, user + "");

            if (!user.Enable) throw new InvalidOperationException($"用户[{user}]已禁用！");

            // 登录成功，保存当前用户
            if (prv is ManageProvider2 prv2) user = prv2.CheckAgent(user);
            prv.Current = user;

            // 单点登录不要保存Cookie，让它在Session过期时请求认证中心
            //prv.SaveCookie(user);
            var set = Setting.Current;
            if (set.SessionTimeout > 0)
            {
                var expire = TimeSpan.FromSeconds(set.SessionTimeout);
#if __CORE__
                prv.SaveCookie(user, expire, httpContext);
#else
                prv.SaveCookie(user, expire, httpContext.ApplicationInstance.Context);
#endif
            }

            return SuccessUrl;
        }

        /// <summary>填充用户，登录成功并获取用户信息之后</summary>
        /// <param name="client"></param>
        /// <param name="user"></param>
        protected virtual void Fill(OAuthClient client, IManageUser user)
        {
            client.Fill(user);

            var dic = client.Items;
            // 用户信息
            if (dic != null && user is User user2)
            {
                if (user2.Code.IsNullOrEmpty()) user2.Code = client.UserCode;
                if (user2.Mobile.IsNullOrEmpty()) user2.Mobile = client.Mobile;
                if (user2.Mail.IsNullOrEmpty()) user2.Mail = client.Mail;

                if (user2.Sex == SexKinds.未知 && client.Sex != 0) user2.Sex = (SexKinds)client.Sex;
                if (user2.Remark.IsNullOrEmpty()) user2.Remark = client.Detail;

                var set = Setting.Current;
                var roleId = 0;
                List<Int32> roleIds = null;

                // 使用认证中心的角色
                if (set.UseSsoRole)
                {
                    // 跟本地系统角色合并
                    var sys = user2.Roles.Where(e => e.IsSystem).Select(e => e.ID).ToList();
                    if (sys.Count > 0)
                    {
                        roleId = user2.RoleID;
                        if (roleIds == null) roleIds = new List<Int32>();
                        roleIds.AddRange(sys);
                    }
                    roleId = GetRole(dic, true);
                    if (roleId > 0)
                    {
                        user2.RoleID = roleId;

                        var ids = GetRoles(client.Items, true).ToList();
                        if (roleIds == null) roleIds = new List<Int32>();
                        roleIds.AddRange(ids);
                    }
                }
                // 使用本地角色
                if (user2.RoleID <= 0 && !set.DefaultRole.IsNullOrEmpty())
                    user2.RoleID = roleId = Role.GetOrAdd(set.DefaultRole).ID;

                // OAuth提供者的自动角色
                var cfg = OAuthConfig.FindAllWithCache().FirstOrDefault(e => e.Name.EqualIgnoreCase(client.Name));
                if (cfg != null && !cfg.AutoRole.IsNullOrEmpty())
                {
                    var ids = GetRoles(cfg.AutoRole, true).ToList();
                    if (roleIds == null) roleIds = new List<Int32>();
                    roleIds.AddRange(ids);
                }

                if (roleIds != null)
                {
                    roleIds = roleIds.Distinct().ToList();
                    if (roleIds.Contains(roleId)) roleIds.Remove(roleId);
                    if (roleIds.Count == 0)
                        user2.RoleIds = null;
                    else
                        user2.RoleIds = "," + roleIds.OrderBy(e => e).Join() + ",";
                }

                // 部门
                if (set.UseSsoDepartment && !client.DepartmentCode.IsNullOrEmpty() && !client.DepartmentName.IsNullOrEmpty())
                {
                    var dep = Department.FindByCode(client.DepartmentCode);
                    if (dep == null)
                    {
                        dep = new Department
                        {
                            Code = client.DepartmentCode,
                            Name = client.DepartmentName,
                            Enable = true
                        };
                        dep.Insert();
                    }

                    user2.DepartmentID = dep.ID;
                }

                // 头像。有可能是相对路径，需要转为绝对路径
                var av = client.Avatar;
                if (av != null && av.StartsWith("/") && client.Server.StartsWithIgnoreCase("http"))
                    av = new Uri(new Uri(client.Server), av) + "";

                if (user2.Avatar.IsNullOrEmpty())
                    user2.Avatar = av;
                // 本地头像，如果不存在，也要更新
                else if (user2.Avatar.StartsWithIgnoreCase("/Sso/Avatar/", "/Sso/Avatar?"))
                {
                    var av2 = Setting.Current.AvatarPath.CombinePath(user2.ID + ".png").GetBasePath();
                    if (!File.Exists(av2))
                    {
                        LogProvider.Provider?.WriteLog(user.GetType(), "更新头像", true, $"{user2.Avatar} => {av}", user.ID, user + "");

                        user2.Avatar = av;
                    }
                }

                // 下载远程头像到本地，Avatar还是保存远程头像地址
                if (user2.Avatar.StartsWithIgnoreCase("http") && !set.AvatarPath.IsNullOrEmpty()) Task.Run(() => FetchAvatar(user, av));
            }
        }

        /// <summary>绑定用户，用户未有效绑定或需要强制绑定时</summary>
        /// <param name="uc"></param>
        /// <param name="client"></param>
        public virtual IManageUser OnBind(UserConnect uc, OAuthClient client)
        {
            var log = LogProvider.Provider;
            var prv = Provider;
            var mode = "";

            // 如果未登录，需要注册一个
            var user = prv.Current;
            if (user == null)
            {
                // 匹配UnionId
                if (user == null && !client.UnionID.IsNullOrEmpty())
                {
                    var list = UserConnect.FindAllByUnionId(client.UnionID);

                    //// 排除当前项，选择登录次数最多的用户
                    //list = list.Where(e => e.ID != uc.ID && e.UserID > 0).ToList();
                    // 选择登录次数最多的用户
                    var ids = list.Where(e => e.Enable && e.UserID > 0).Select(e => e.UserID).Distinct().ToArray();
                    var users = ids.Select(e => User.FindByID(e)).Where(e => e != null).ToList();
                    if (users.Count > 0)
                    {
                        mode = "UnionID";
                        user = users.OrderByDescending(e => e.Logins).FirstOrDefault();
                    }
                }

                var set = Setting.Current;
                var cfg = OAuthConfig.FindByName(client.Name);
                //if (!cfg.AutoRegister) throw new InvalidOperationException($"绑定[{cfg}]要求本地已登录！");
                if (user == null && !set.AutoRegister && !cfg.AutoRegister)
                {
                    log?.WriteLog(typeof(User), "SSO登录", false, $"无法找到[{client.Name}]的[{client.NickName}]在本地的绑定，且没有打开自动注册，准备进入登录页面，利用其它登录方式后再绑定", 0, user + "");

                    return null;
                }

                // 先找用户名，如果存在，就加上提供者前缀，直接覆盖
                var name = client.UserName;
                if (name.IsNullOrEmpty()) name = client.NickName;
                if (user == null && !name.IsNullOrEmpty())
                {
                    // 强制绑定本地用户时，没有前缀
                    if (set.ForceBindUser)
                    {
                        mode = "UserName";
                        user = prv.FindByName(name);
                    }
                    else
                    {
                        mode = "Provider-UserName";
                        name = client.Name + "_" + name;
                        user = prv.FindByName(name);
                    }
                }

                // 匹配Code
                if (user == null && set.ForceBindUserCode)
                {
                    mode = "UserCode";
                    if (!client.UserCode.IsNullOrEmpty()) user = User.FindByCode(client.UserCode);
                }

                // 匹配Mobile
                if (user == null && set.ForceBindUserMobile)
                {
                    mode = "UserMobile";
                    if (!client.Mobile.IsNullOrEmpty()) user = User.FindByMobile(client.Mobile);
                }

                // 匹配Mail
                if (user == null && set.ForceBindUserMail)
                {
                    mode = "UserMail";
                    if (!client.Mail.IsNullOrEmpty()) user = User.FindByMail(client.Mail);
                }

                // QQ、微信 等不返回用户名
                if (user == null && name.IsNullOrEmpty())
                {
                    // OpenID和AccessToken不可能同时为空
                    var openid = client.OpenID;
                    if (openid.IsNullOrEmpty()) openid = client.AccessToken;

                    // 过长，需要随机一个较短的
                    var num = openid.GetBytes().Crc();

                    mode = "OpenID-Crc";
                    name = client.Name + "_" + num.ToString("X8");
                    user = prv.FindByName(name);
                }

                if (user == null)
                {
                    mode = "Register";

                    // 新注册用户采用魔方默认角色
                    var rid = Role.GetOrAdd(set.DefaultRole).ID;
                    //if (rid == 0 && client.Items.TryGetValue("roleid", out var roleid)) rid = roleid.ToInt();
                    //if (rid <= 0) rid = GetRole(client.Items, rid < -1);

                    // 注册用户，随机密码
                    user = prv.Register(name, Rand.NextString(16), rid, true);
                    //if (user is User user2) user2.RoleIDs = GetRoles(client.Items, rid < -2).Join();
                }
            }

            uc.UserID = user.ID;
            uc.Enable = true;

            // 写日志
            log?.WriteLog(typeof(User), "绑定", true, $"[{user}]依据[{mode}]绑定到[{client.Name}]的[{client.NickName}]", user.ID, user + "");

            return user;
        }

        /// <summary>登录后绑定当前用户</summary>
        public virtual OAuthLog BindAfterLogin(Int64 oauthId)
        {
            var prv = Provider;
            var mode = nameof(BindAfterLogin);

            var user = prv.Current;
            if (user == null) return null;

            var log = OAuthLog.FindById(oauthId);
            if (log == null) return null;

            var uc = UserConnect.FindByID(log.ConnectId);
            if (uc == null) return null;

            uc.UserID = user.ID;
            uc.Enable = true;
            uc.UpdateTime = DateTime.Now;
            uc.Update();

            log.UserId = user.ID;
            log.SaveAsync();

            // 写日志
            LogProvider.Provider?.WriteLog(typeof(User), "绑定", true, $"[{user}]依据[{mode}]绑定到[{uc.Provider}]的[{uc.NickName}]", user.ID, user + "");

            return log;

        }

        /// <summary>注销</summary>
        /// <returns></returns>
        public virtual void Logout() => Provider?.Logout();
        #endregion

        #region 服务端
        /// <summary>获取访问令牌</summary>
        /// <param name="sso"></param>
        /// <param name="client_id"></param>
        /// <param name="client_secret"></param>
        /// <param name="code"></param>
        /// <param name="ip"></param>
        /// <returns></returns>
        public virtual TokenInfo GetAccessToken(OAuthServer sso, String client_id, String client_secret, String code, String ip)
        {
            sso.Auth(client_id, client_secret, ip);

            var token = sso.GetToken(code);
            token.Scope = "basic,UserInfo";

            return token;
        }

        /// <summary>密码式获取令牌</summary>
        /// <param name="sso"></param>
        /// <param name="client_id">应用标识</param>
        /// <param name="username">用户名</param>
        /// <param name="password">密码。支持md5密码，以md5#开头</param>
        /// <param name="ip"></param>
        /// <returns></returns>
        public virtual TokenInfo GetAccessTokenByPassword(OAuthServer sso, String client_id, String username, String password, String ip)
        {
            var log = new AppLog
            {
                Action = "Password",
                Success = true,

                ClientId = client_id,
                ResponseType = "password",
                CreateIP = ip,
            };

            try
            {
                var app = sso.Auth(client_id, null, ip);
                log.AppId = app.ID;

                // 验证应用能力
                var scopes = app.Scopes?.Split(",");
                if (scopes == null || !"password".EqualIgnoreCase(scopes)) throw new InvalidOperationException($"应用[{app}]没有使用password密码凭证的能力！");

                IManageUser user = null;
                if (password.StartsWithIgnoreCase("md5#"))
                {
                    var pass = password.Substring("md5#".Length);
                    user = User.Login(username, u =>
                    {
                        if (!u.Password.IsNullOrEmpty() && !u.Password.EqualIgnoreCase(pass))
                            throw new InvalidOperationException($"密码不正确！");
                    });
                }
                else if (password.StartsWithIgnoreCase("$rsa$"))
                {
                    var ss = password.Split('$');
                    var key = GetKey(ss[2]);
                    var pass = ss[ss.Length - 1];
                    pass = RSAHelper.Decrypt(pass.ToBase64(), key).ToStr();

                    if (Provider is ManageProvider prv)
                        user = prv.LoginCore(username, pass);
                    else
                        user = User.Login(username, pass, false);
                }
                else
                {
                    // 不能使用 ManagerProvider，它会写cookie
                    //var user = Provider.Login(username, password, false);
                    if (Provider is ManageProvider prv)
                        user = prv.LoginCore(username, password);
                    else
                        user = User.Login(username, password, false);
                }
                if (user == null) throw new XException("用户{0}验证失败", username);

                var token = sso.CreateToken(app, user.Name, null, $"{client_id}#{user.Name}");

                log.AccessToken = token.AccessToken;
                log.RefreshToken = token.RefreshToken;

                log.CreateUser = user.Name;
                log.Scope = token.Scope;

                return token;
            }
            catch (Exception ex)
            {
                log.Success = false;
                log.Remark = ex.GetTrue()?.Message;

                throw;
            }
            finally
            {
                log.Insert();
            }
        }

        /// <summary>凭证式获取令牌</summary>
        /// <param name="sso"></param>
        /// <param name="client_id">应用标识</param>
        /// <param name="client_secret">密钥</param>
        /// <param name="username">用户名。可以是设备编码等唯一使用者标识</param>
        /// <param name="ip"></param>
        /// <returns></returns>
        public virtual TokenInfo GetAccessTokenByClientCredentials(OAuthServer sso, String client_id, String client_secret, String username, String ip)
        {
            var log = new AppLog
            {
                Action = "ClientCredentials",
                Success = true,

                ClientId = client_id,
                ResponseType = "client_credentials",
                CreateIP = ip,
            };

            try
            {
                var app = App.FindByName(client_id);
                if (app != null) log.AppId = app.ID;

                app = sso.Auth(client_id, client_secret, ip);
                log.AppId = app.ID;

                // 验证应用能力
                var scopes = app.Scopes?.Split(",");
                if (scopes == null || !"client_credentials".EqualIgnoreCase(scopes)) throw new InvalidOperationException($"应用[{app}]没有使用client_credentials客户端凭证的能力！");

                var code = !username.IsNullOrEmpty() ? username : ("_" + Rand.NextString(7));
                var token = sso.CreateToken(app, code, null, $"{client_id}#{code}");
                //token.Scope = "basic,UserInfo";

                log.AccessToken = token.AccessToken;
                log.RefreshToken = token.RefreshToken;

                log.CreateUser = code;
                log.Scope = token.Scope;

                return token;
            }
            catch (Exception ex)
            {
                log.Success = false;
                log.Remark = ex.GetTrue()?.Message;

                throw;
            }
            finally
            {
                log.Insert();
            }
        }

        /// <summary>凭证式获取令牌</summary>
        /// <param name="sso"></param>
        /// <param name="client_id">应用标识</param>
        /// <param name="refresh_token">刷新令牌</param>
        /// <param name="ip"></param>
        /// <returns></returns>
        public virtual TokenInfo RefreshToken(OAuthServer sso, String client_id, String refresh_token, String ip)
        {
            var log = new AppLog
            {
                Action = "RefreshToken",
                Success = true,

                ClientId = client_id,
                ResponseType = "refresh_token",
                CreateIP = ip,
            };

            try
            {
                var app = App.FindByName(client_id);
                if (app != null) log.AppId = app.ID;

                app = sso.Auth(client_id, null, ip);
                log.AppId = app.ID;

                var name = sso.Decode(refresh_token);
                var ss = name.Split("#");
                if (ss.Length != 2 || ss[0] != client_id) throw new Exception("非法令牌");

                // 使用者标识保持不变
                var code = ss[1];
                var token = sso.CreateToken(app, code, null, $"{client_id}#{code}");

                log.AccessToken = token.AccessToken;
                log.RefreshToken = token.RefreshToken;

                log.CreateUser = code;
                log.Scope = token.Scope;

                return token;
            }
            catch (Exception ex)
            {
                log.Success = false;
                log.Remark = ex.GetTrue()?.Message;

                throw;
            }
            finally
            {
                log.Insert();
            }
        }
        /// <summary>获取用户信息</summary>
        /// <param name="sso"></param>
        /// <param name="username"></param>
        /// <returns></returns>
        public virtual IManageUser GetUser(OAuthServer sso, String username)
        {
            var user = Provider?.FindByName(username);
            // 两级单点登录可能因缓存造成查不到用户
            if (user == null) user = User.FindForLogin(username);

            return user;
        }

        /// <summary>获取用户信息</summary>
        /// <param name="sso"></param>
        /// <param name="token"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual Object GetUserInfo(OAuthServer sso, String token, IManageUser user)
        {
            // 返回用户资源，可作为子系统数据权限
            var res = Parameter.FindAllByUserID(user.ID, "Resources");
            var dic = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in res.Where(e => e.Enable))
            {
                dic[item.Name] = item.Value;
            }

            if (user is User user2)
                return new
                {
                    userid = user.ID,
                    username = user.Name,
                    nickname = user.NickName,
                    sex = user2.Sex,
                    mail = user2.Mail,
                    mobile = user2.Mobile,
                    code = user2.Code,
                    roleid = user2.RoleID,
                    rolename = user2.RoleName,
                    roleids = user2.RoleIds,
                    rolenames = user2.Roles.Skip(1).Join(",", e => e + ""),
                    departmentCode = user2.Department?.Code,
                    departmentName = user2.Department?.Name,
                    avatar = "/Cube/Avatar?id=" + user2.ID,
                    detail = user2.Remark,
                    resources = dic,
                };
            else
                return new
                {
                    userid = user.ID,
                    username = user.Name,
                    nickname = user.NickName,
                    resources = dic,
                };
        }
        #endregion

        #region 辅助
        /// <summary>抓取远程头像</summary>
        /// <param name="user"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public virtual Boolean FetchAvatar(IManageUser user, String url = null)
        {
            if (url.IsNullOrEmpty()) url = user.GetValue("Avatar") as String;
            //if (av.IsNullOrEmpty()) throw new Exception("用户头像不存在 " + user);

            // 尝试从用户链接获取头像地址
            if (url.IsNullOrEmpty() || !url.StartsWithIgnoreCase("http"))
            {
                var list = UserConnect.FindAllByUserID(user.ID);
                url = list.OrderByDescending(e => e.UpdateTime)
                    .Where(e => !e.Avatar.IsNullOrEmpty() && e.Avatar.StartsWithIgnoreCase("http"))
                    .FirstOrDefault()?.Avatar;
            }

            if (url.IsNullOrEmpty()) return false;
            if (!url.StartsWithIgnoreCase("http")) return false;

            // 不要扩展名
            var set = Setting.Current;
            var dest = set.AvatarPath.CombinePath(user.ID + ".png").GetBasePath();

            //// 头像是否已存在
            //if (File.Exists(dest)) return false;

            LogProvider.Provider?.WriteLog(user.GetType(), "抓取头像", true, $"{url} => {dest}", user.ID, user + "");

            dest.EnsureDirectory(true);

            try
            {
                var client = new HttpClient();
                var rs = client.GetAsync(url).Result;
                var buf = rs.Content.ReadAsByteArrayAsync().Result;
                File.WriteAllBytes(dest, buf);

                // 更新头像
                user.SetValue("Avatar", "/Sso/Avatar?id=" + user.ID);
                (user as IEntity)?.Update();

                return true;
            }
            catch (Exception ex)
            {
                XTrace.WriteLine("抓取头像失败，{0}, {1}", user, url);
                XTrace.WriteException(ex);
            }

            return false;
        }

        private Int32 GetRole(IDictionary<String, String> dic, Boolean create)
        {
            // 先找RoleName，再找RoleID
            if (dic.TryGetValue("RoleName", out var name) && !name.IsNullOrEmpty())
            {
                var r = Role.FindByName(name);
                if (r != null) return r.ID;

                if (create)
                {
                    r = new Role { Name = name };
                    r.Insert();
                    return r.ID;
                }
            }

            //// 判断角色有效
            //if (dic.TryGetValue("RoleID", out var rid) && Role.FindByID(rid.ToInt()) != null) return rid.ToInt();

            return 0;
        }

        private Int32[] GetRoles(IDictionary<String, String> dic, Boolean create)
        {
            if (dic.TryGetValue("RoleNames", out var roleNames)) return GetRoles(roleNames, create);

            return new Int32[0];
        }

        private Int32[] GetRoles(String roleNames, Boolean create)
        {
            var names = roleNames.Split(',');
            var rs = new List<Int32>();
            foreach (var item in names)
            {
                if (item.IsNullOrEmpty()) continue;

                var r = Role.FindByName(item);
                if (r != null)
                    rs.Add(r.ID);
                else if (create)
                {
                    r = new Role { Name = item };
                    r.Insert();
                    rs.Add(r.ID);
                }
            }

            if (rs.Count > 0) return rs.Distinct().ToArray();

            //// 判断角色有效
            //if (dic.TryGetValue("RoleIDs", out var rids)) return rids.SplitAsInt().Where(e => Role.FindByID(e) != null).ToArray();

            return new Int32[0];
        }

        /// <summary>获取指定Key，默认实现从SecurityKey解析</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual String GetKey(String name)
        {
            var key = SecurityKey;
            if (key.IsNullOrEmpty())
            {
                var file = "..\\Keys\\SsoSecurity.prvkey".GetFullPath();
                if (File.Exists(file)) key = File.ReadAllText(file);

                if (key.IsNullOrEmpty())
                {
                    file.EnsureDirectory(true);

                    var ks = RSAHelper.GenerateParameters();
                    File.WriteAllText(file, ks[0]);
                    File.WriteAllText(file.TrimEnd(".prvkey") + ".pubkey", ks[1]);
                    key = ks[0];
                }
            }
            if (key.IsNullOrEmpty()) throw new ArgumentNullException(nameof(SecurityKey), $"无法找到名为[{name}]的密钥");

            var name2 = "";
            var p = key.IndexOf('$');
            if (p >= 0)
            {
                name2 = key.Substring(0, p);
                key = key.Substring(p + 1);
            }

            if (!name.IsNullOrEmpty() && !name2.IsNullOrEmpty() && !name.EqualIgnoreCase(name2))
                throw new ArgumentOutOfRangeException(nameof(SecurityKey), $"无法找到名为[{name}]的密钥");

            return key;
        }
        #endregion
    }
}
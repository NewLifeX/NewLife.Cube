using System;
using System.Linq;
using System.Web;
using NewLife.Cube.Entity;
using NewLife.Cube.Web;
using NewLife.Cube.Web.Models;
using NewLife.Log;
using NewLife.Model;
using NewLife.Serialization;

namespace NewLife.Web
{
    /// <summary>单点登录服务端</summary>
    public class OAuthServer
    {
        #region 属性
        ///// <summary>缓存</summary>
        //public ICache Cache { get; set; } = NewLife.Caching.Cache.Default;

        /// <summary>令牌提供者</summary>
        public TokenProvider TokenProvider { get; set; } = new TokenProvider();

        ///// <summary>令牌有效期。默认24小时</summary>
        //public Int32 Expire { get; set; } = 24 * 3600;
        #endregion

        #region 静态
        /// <summary>实例</summary>
        public static OAuthServer Instance { get; set; } = new OAuthServer();
        #endregion

        #region 构造
        #endregion

        #region 方法
        /// <summary>验证应用</summary>
        /// <param name="client_id"></param>
        /// <param name="client_secret"></param>
        /// <returns></returns>
        public virtual App Auth(String client_id, String client_secret)
        {
            var app = App.FindByName(client_id);
            //if (app == null) throw new XException("未找到应用[{0}]", appid);
            // 找不到应用时自动创建，但处于禁用状态
            if (app == null)
            {
                app = new App { Name = client_id };
                app.Insert();
            }

            if (!app.Enable) throw new XException("应用[{0}]不可用", client_id);
            if (!client_secret.IsNullOrEmpty())
            {
                if (!app.Secret.IsNullOrEmpty() && !app.Secret.EqualIgnoreCase(client_secret)) throw new XException("应用密钥错误");
            }

            return app;
        }

        /// <summary>验证应用</summary>
        /// <param name="client_id"></param>
        /// <param name="client_secret"></param>
        /// <param name="ip"></param>
        /// <returns></returns>
        public virtual App Auth(String client_id, String client_secret, String ip)
        {
            var app = App.FindByName(client_id);
            //if (app == null) throw new XException("未找到应用[{0}]", appid);
            // 找不到应用时自动创建，但处于禁用状态
            if (app == null)
            {
                app = new App { Name = client_id };
                app.Insert();
            }

            if (!app.Enable) throw new XException("应用[{0}]不可用", client_id);

            if (!app.ValidSource(ip)) throw new XException("来源地址不合法 {0}", ip);
            
            if (!client_secret.IsNullOrEmpty())
            {
                if (!app.Secret.IsNullOrEmpty() && !app.Secret.EqualIgnoreCase(client_secret)) throw new XException("应用密钥错误");
            }

            return app;
        }

        /// <summary>验证用户身份</summary>
        /// <remarks>
        /// 子系统需要验证访问者身份时，引导用户跳转到这里。
        /// 用户登录完成后，得到一个独一无二的code，并跳转回去子系统。
        /// </remarks>
        /// <param name="client_id">应用标识</param>
        /// <param name="redirect_uri">回调地址</param>
        /// <param name="response_type">响应类型。默认code</param>
        /// <param name="scope">授权域</param>
        /// <param name="state">用户状态数据</param>
        /// <returns></returns>
        public virtual String Authorize(String client_id, String redirect_uri, String response_type = null, String scope = null, String state = null)
        {
            var log = new AppLog
            {
                Action = nameof(Authorize),
                Success = true,

                ClientId = client_id,
                RedirectUri = redirect_uri,
                ResponseType = response_type,
                Scope = scope,
                State = state
            };

            try
            {
                //if (!response_type.EqualIgnoreCase("code")) throw new NotSupportedException(nameof(response_type));

                var app = App.FindByName(client_id);
                if (app != null) log.AppId = app.ID;

                app = Auth(client_id, null);
                log.AppId = app.ID;

                // 验证回调地址
                if (!app.ValidCallback(redirect_uri)) throw new XException("回调地址不合法 {0}", redirect_uri);

                // 统计次数
                app.Auths++;
                app.LastAuth = DateTime.Now;
                app.SaveAsync(5_000);

                if (Log != null) WriteLog("Authorize client_id={0} redirect_uri={1} response_type={2}", client_id, redirect_uri, response_type);
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

            return log.ID + "";
        }

        /// <summary>根据验证结果获取跳转回子系统的Url</summary>
        /// <param name="key"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual String GetResult(String key, IManageUser user)
        {
            var log = AppLog.FindByID(key.ToLong());
            if (log == null) throw new ArgumentOutOfRangeException(nameof(key), "操作超时，请重试！");

            //var prv = GetProvider();
            var code = log.ID + "";

            var token = CreateToken(log.App, user.Name, null, $"{log.App?.Name}#{user.Name}");

            // 建立令牌
            log.AccessToken = token.AccessToken;
            log.RefreshToken = token.RefreshToken;
            log.CreateUser = user + "";
            log.Action = nameof(GetResult);

            if (Log != null) WriteLog("Authorize appid={0} code={2} redirect_uri={1} {3}", log.AppName, log.RedirectUri, code, user);

            // 校验角色
            var ids = log.App?.RoleIds?.SplitAsInt();
            if (ids != null && ids.Length > 0 && user is XCode.Membership.User user2)
            {
                if (!user2.Roles.Any(r => ids.Contains(r.ID)))
                {
                    log.Success = false;
                    log.Remark = $"该应用[{log.AppName}]不支持用户所属角色登录！";
                    log.Update();

                    throw new InvalidOperationException(log.Remark);
                }
            }

            log.Update();

            var url = log.RedirectUri;

            switch ((log.ResponseType + "").ToLower())
            {
                case "token":
                    if (url.Contains("?"))
                        url += "&";
                    else
                        url += "?";
                    if (!log.State.IsNullOrEmpty()) url += "state=" + HttpUtility.UrlEncode(log.State);
                    url += "#token=" + HttpUtility.UrlEncode(log.AccessToken);
                    break;
                case "code":
                default:
                    if (url.Contains("?"))
                        url += "&";
                    else
                        url += "?";
                    url += "code=" + code;
                    if (!log.State.IsNullOrEmpty()) url += "&state=" + HttpUtility.UrlEncode(log.State);
                    break;
            }

            return url;
        }

        /// <summary>创建令牌</summary>
        /// <param name="app"></param>
        /// <param name="name"></param>
        /// <param name="payload"></param>
        /// <param name="refreshName"></param>
        /// <returns></returns>
        public virtual TokenInfo CreateToken(App app, String name, Object payload, String refreshName)
        {
            var prv = GetProvider();

            // 计算有效期，优先应用指定有效期，再使用全局有效期
            var expire = 0;
            if (app != null) expire = app.TokenExpire;

            var set = NewLife.Cube.Setting.Current;
            if (expire <= 0) expire = set.TokenExpire;
            var exp = DateTime.Now.AddSeconds(expire);

            // 颁发JWT令牌，优先应用密钥HS256，同时也是子应用请求sso的密钥。再使用全局密钥
            var jwt = new JwtBuilder
            {
                Algorithm = "HS256",
                Secret = app.Secret,

                Subject = name,
                Expire = exp,
                //Issuer = Environment.MachineName,
                Audience = app.Name,
            };
            if (jwt.Secret.IsNullOrEmpty())
            {
                var ss = set.JwtSecret.Split(':');
                jwt.Algorithm = ss[0];
                jwt.Secret = ss[1];
            }

            // 建立令牌
            return new TokenInfo
            {
                AccessToken = jwt.Encode(payload),
                RefreshToken = prv.Encode(refreshName, exp),
                Expire = expire
            };
        }

        /// <summary>根据Code获取令牌</summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public virtual TokenInfo GetToken(String code)
        {
            var log = AppLog.FindByID(code.ToLong());
            if (log == null || log.CreateTime.AddMinutes(5) < DateTime.Now) throw new ArgumentOutOfRangeException(nameof(code), "Code已过期！");

            if (Log != null) WriteLog("Token appid={0} code={1} token={2} {3}", log.AppName, code, log.AccessToken, log.CreateUser);

            log.Action = nameof(GetToken);
            log.Update();

            var expire = 0;
            if (log.App != null) expire = log.App.TokenExpire;

            var set = NewLife.Cube.Setting.Current;
            if (expire <= 0) expire = set.TokenExpire;

            return new TokenInfo
            {
                AccessToken = log.AccessToken,
                RefreshToken = log.RefreshToken,
                Expire = expire
            };
        }

        /// <summary>解码令牌</summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public String Decode(String token)
        {
            // 区分访问令牌和内部刷新令牌
            var ts = token.Split('.');
            if (ts.Length == 3)
            {
                var secret = "";

                // 从头部找到颁发者，拿它的密钥
                var header = JsonParser.Decode(ts[1].ToBase64().ToStr());
                if (header.TryGetValue("aud", out var str))
                {
                    var app = App.FindByName(str as String);
                    secret = app?.Secret;
                }

                // 从配置加载密钥
                var set = NewLife.Cube.Setting.Current;
                var ss = set.JwtSecret.Split(':');

                var jwt = new JwtBuilder
                {
                    Algorithm = ss[0],
                    Secret = ss[1],
                };
                if (!secret.IsNullOrEmpty())
                {
                    jwt.Algorithm = "HS256";
                    jwt.Secret = secret;
                }

                if (!jwt.TryDecode(token, out var msg))
                {
                    XTrace.WriteLine("令牌无效：{0}, token={1}", msg, token);
                    throw new Exception(msg);

                    //return null;
                }

                return jwt.Subject;
            }
            else
            {
                var prv = GetProvider();

                var rs = prv.TryDecode(token, out var name, out var expire);
                if (!rs || name.IsNullOrEmpty()) throw new Exception("非法访问令牌");
                if (expire < DateTime.Now) throw new Exception("令牌已过期");

                return name;
            }
        }
        #endregion

        #region 辅助
        /// <summary>获取令牌提供者</summary>
        /// <returns></returns>
        public TokenProvider GetProvider()
        {
            var prv = TokenProvider;
            if (prv == null) prv = TokenProvider = new TokenProvider();
            if (prv.Key.IsNullOrEmpty()) prv.ReadKey("..\\Keys\\OAuth.prvkey", true);

            return prv;
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; }

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
        #endregion
    }
}
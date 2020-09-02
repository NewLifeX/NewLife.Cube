using System;
using NewLife.Caching;
using NewLife.Cube.Entity;
using NewLife.Log;
using NewLife.Model;
using NewLife.Security;

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

        /// <summary>令牌有效期。默认24小时</summary>
        public Int32 Expire { get; set; } = 24 * 3600;
        #endregion

        #region 静态
        /// <summary>实例</summary>
        public static OAuthServer Instance { get; set; } = new OAuthServer();
        #endregion

        #region 构造
        #endregion

        #region 方法
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
                //if (client_id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(client_id));
                //if (redirect_uri.IsNullOrEmpty()) throw new ArgumentNullException(nameof(redirect_uri));
                //if (response_type.IsNullOrEmpty()) response_type = "code";

                if (!response_type.EqualIgnoreCase("code")) throw new NotSupportedException(nameof(response_type));

                var app = App.FindByName(client_id);
                //if (app == null) throw new XException("未找到应用[{0}]", appid);
                // 找不到应用时自动创建，但处于禁用状态
                if (app == null)
                {
                    app = new App { Name = client_id };
                    app.Insert();
                }

                log.AppId = app.ID;
                if (!app.Enable) throw new XException("应用[{0}]不可用", client_id);

                // 验证回调地址
                if (!app.ValidCallback(redirect_uri)) throw new XException("回调地址不合法 {0}", redirect_uri);

                // 统计次数
                app.Auths++;
                app.LastAuth = DateTime.Now;
                app.SaveAsync(5_000);

                if (Log != null) WriteLog("Authorize client_id={0} redirect_uri={1}", client_id, redirect_uri);
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

            var prv = GetProvider();
            var code = log.ID + "";

            // 建立令牌
            log.AccessToken = prv.Encode(user.Name, DateTime.Now.AddSeconds(Expire));
            log.RefreshToken = code + "." + Rand.NextString(16);
            log.CreateUser = user + "";

            if (Log != null) WriteLog("Authorize appid={0} code={2} redirect_uri={1} {3}", log.AppName, log.RedirectUri, code, user);

            log.Action = nameof(GetResult);
            log.Update();

            var url = log.RedirectUri;
            if (url.Contains("?"))
                url += "&";
            else
                url += "?";
            url += "code=" + code;
            if (!log.State.IsNullOrEmpty()) url += "&state=" + log.State;

            return url;
        }

        /// <summary>根据Code获取令牌</summary>
        /// <param name="client_id"></param>
        /// <param name="client_secret"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public virtual String[] GetTokens(String client_id, String client_secret, String code)
        {
            var log = AppLog.FindByID(code.ToLong());
            if (log == null) throw new ArgumentOutOfRangeException(nameof(code), "Code已过期！");

            if (Log != null) WriteLog("Token appid={0} code={1} token={2} {3}", log.AppName, code, log.AccessToken, log.CreateUser);

            log.Action = nameof(GetTokens);
            log.Update();

            return new[] { log.AccessToken, log.RefreshToken };
        }

        /// <summary>解码令牌</summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public String Decode(String token)
        {
            var prv = GetProvider();

            var rs = prv.TryDecode(token, out var username, out var expire);
            if (!rs || username.IsNullOrEmpty()) throw new Exception("非法访问令牌");
            if (expire < DateTime.Now) throw new Exception("令牌已过期");

            return username;
        }
        #endregion

        #region 辅助
        private TokenProvider GetProvider()
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
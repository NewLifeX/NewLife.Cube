using System;
using NewLife.Cube.Entity;
using NewLife.Model;
using NewLife.Web;

namespace NewLife.Cube.Web
{
    /// <summary>单点登录服务端</summary>
    public class OAuthServer2 : OAuthServer
    {
        #region 静态
        ///// <summary>初始化</summary>
        //public static void Init()
        //{
        //    if (!(Instance is OAuthServer2))
        //    {
        //        Instance = new OAuthServer2();
        //    }
        //}
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
        public override Int32 Authorize(String client_id, String redirect_uri, String response_type = null, String scope = null, String state = null)
        {
            var id = 0;
            var rs = true;
            var msg = "";
            try
            {
                if (!response_type.EqualIgnoreCase("code")) throw new NotSupportedException(nameof(response_type));

                var app = App.FindByName(client_id);
                //if (app == null) throw new XException("未找到应用[{0}]", appid);
                // 找不到应用时自动创建，但处于禁用状态
                if (app == null)
                {
                    app = new App { Name = client_id };
                    app.Insert();
                }

                id = app.ID;
                if (!app.Enable) throw new XException("应用[{0}]不可用", client_id);

                // 验证回调地址
                if (!app.ValidCallback(redirect_uri)) throw new XException("回调地址不合法 {0}", redirect_uri);

                var key = base.Authorize(client_id, redirect_uri, response_type, scope, state);

                msg = $"key={key},redirect_uri={redirect_uri},scope={scope},state={state}";

                // 统计次数
                app.Auths++;
                app.LastAuth = DateTime.Now;
                app.SaveAsync(5_000);

                return key;
            }
            catch (Exception ex)
            {
                rs = false;
                msg = ex.GetTrue()?.Message;

                throw;
            }
            finally
            {
                AppLog.Create(id, nameof(Authorize), rs, msg);
            }
        }

        /// <summary>根据验证结果获取跳转回子系统的Url</summary>
        /// <param name="key"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public override String GetResult(Int32 key, IManageUser user)
        {
            var id = 0;
            var rs = true;
            var msg = "";
            try
            {
                var url = base.GetResult(key, user);

                msg = $"key={key},user={user.ID}/{user},url={url}";

                return url;
            }
            catch (Exception ex)
            {
                rs = false;
                msg = ex.GetTrue()?.Message;

                throw;
            }
            finally
            {
                AppLog.Create(id, nameof(GetResult), rs, msg);
            }
        }

        /// <summary>根据Code获取令牌</summary>
        /// <param name="client_id"></param>
        /// <param name="client_secret"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public override String GetToken(String client_id, String client_secret, String code)
        {
            var id = 0;
            var rs = true;
            var msg = "";
            try
            {
                var app = App.FindByName(client_id);
                if (app == null) throw new XException("未找到应用[{0}]", client_id);

                id = app.ID;
                if (!app.Enable) throw new XException("应用[{0}]不可用", client_id);

                // 密钥为空时跳过验证
                if (!app.Secret.IsNullOrEmpty() && !app.Secret.EqualIgnoreCase(client_secret)) throw new XException("[{0}]密钥错误", client_id);

                // 验证来源地址
                var ip = WebHelper.UserHost;
                if (!ip.IsNullOrEmpty() && !app.ValidSource(ip)) throw new XException("来源地址不合法 {0}", ip);

                var token = base.GetToken(client_id, client_secret, code);

                msg = $"code={code},access_token={token}";

                return token;
            }
            catch (Exception ex)
            {
                rs = false;
                msg = ex.GetTrue()?.Message;

                throw;
            }
            finally
            {
                AppLog.Create(id, nameof(GetToken), rs, msg);
            }
        }
        #endregion
    }
}
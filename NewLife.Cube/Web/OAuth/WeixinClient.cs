using System;
using System.Collections.Generic;

namespace NewLife.Web.OAuth
{
    /// <summary>微信公众号</summary>
    /// <remarks>
    /// 文档 https://developers.weixin.qq.com/doc/offiaccount/OA_Web_Apps/Wechat_webpage_authorization.html
    /// 微信开放平台和微信公众号是两套接口体系，前者主要用于扫码登录，后者主要用于微信客户端（含手机和PC）免登
    /// </remarks>
    public class WeixinClient : OAuthClient
    {
        /// <summary>实例化</summary>
        public WeixinClient()
        {
            Server = "https://open.weixin.qq.com/connect/";

            AuthUrl = "oauth2/authorize?appid={key}&redirect_uri={redirect}&response_type={response_type}&scope={scope}&state={state}#wechat_redirect";
            AccessUrl = "https://api.weixin.qq.com/sns/oauth2/access_token?appid={key}&secret={secret}&code={code}&grant_type=authorization_code";
            UserUrl = "https://api.weixin.qq.com/sns/userinfo?access_token={token}&openid={openid}&lang=zh_CN";

            // 获取用户基本信息(UnionID机制)，这里使用普通access_token，而不是OAuth2.0的网页access_token
            //UserUrl = "https://api.weixin.qq.com/cgi-bin/user/info?access_token={token}&openid={openid}&lang=zh_CN";

            // snsapi_base 静默授权，用户无感知
            // snsapi_userinfo 授权需要用户手动同意
            Scope = "snsapi_base";
        }

        /// <summary>是否支持指定用户端，也就是判断是否在特定应用内打开，例如QQ/DingDing/WeiXin</summary>
        /// <param name="userAgent"></param>
        /// <returns></returns>
        public override Boolean Support(String userAgent) =>
            !userAgent.IsNullOrEmpty() && !userAgent.Contains(" wxwork/") &&
            (userAgent.Contains(" MicroMessenger/") || userAgent.Contains(" MICROMESSENGER/"));

        /// <summary>只有snsapi_userinfo获取用户信息</summary>
        /// <returns></returns>
        public override String GetUserInfo()
        {
            if (Scope == "snsapi_base") return null;

            return base.GetUserInfo();
        }

        /// <summary>从响应数据中获取信息</summary>
        /// <param name="dic"></param>
        protected override void OnGetInfo(IDictionary<String, String> dic)
        {
            base.OnGetInfo(dic);

            if (dic.TryGetValue("headimgurl", out var str)) Avatar = str.Trim();
        }
    }
}
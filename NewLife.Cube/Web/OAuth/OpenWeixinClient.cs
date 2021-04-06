using System;
using System.Collections.Generic;

namespace NewLife.Web.OAuth
{
    /// <summary>微信开放平台</summary>
    /// <remarks>
    /// 文档 https://developers.weixin.qq.com/doc/oplatform/Website_App/WeChat_Login/Wechat_Login.html
    /// 微信开放平台和微信公众号是两套接口体系，前者主要用于扫码登录，后者主要用于微信客户端（含手机和PC）免登
    /// </remarks>
    public class OpenWeixinClient : OAuthClient
    {
        /// <summary>实例化</summary>
        public OpenWeixinClient()
        {
            Server = "https://open.weixin.qq.com/connect/";

            // 扫码登录
            AuthUrl = "qrconnect?appid={key}&redirect_uri={redirect}&response_type={response_type}&scope={scope}&state={state}#wechat_redirect";
            AccessUrl = "https://api.weixin.qq.com/sns/oauth2/access_token?appid={key}&secret={secret}&code={code}&grant_type=authorization_code";
            UserUrl = "https://api.weixin.qq.com/sns/userinfo?access_token={token}&openid={openid}&lang=zh_CN";

            Scope = "snsapi_login";
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
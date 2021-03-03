using System;
using System.Collections.Generic;

namespace NewLife.Web.OAuth
{
    /// <summary>百度身份验证提供者</summary>
    /// <remarks>
    /// 平台 http://developer.baidu.com/console
    /// </remarks>
    public class BaiduClient : OAuthClient
    {
        /// <summary>实例化</summary>
        public BaiduClient()
        {
            Server = "https://openapi.baidu.com/oauth/2.0/";

            AuthUrl = "authorize?response_type={response_type}&client_id={key}&redirect_uri={redirect}&state={state}&scope={scope}";
            AccessUrl = "token?grant_type=authorization_code&client_id={key}&client_secret={secret}&code={code}&state={state}&redirect_uri={redirect}";
            //UserUrl = "https://openapi.baidu.com/rest/2.0/passport/users/getLoggedInUser?access_token={token}";
            UserUrl = "https://openapi.baidu.com/rest/2.0/passport/users/getInfo?access_token={token}";
        }

        /// <summary>从响应数据中获取信息</summary>
        /// <param name="dic"></param>
        protected override void OnGetInfo(IDictionary<String, String> dic)
        {
            base.OnGetInfo(dic);

            if (dic.TryGetValue("uname", out var str)) UserName = str.Trim();
            if (dic.TryGetValue("realname", out str)) NickName = str.Trim();
            //if (dic.TryGetValue("userdetail", out str)) Detail = str.Trim();

            // 修改性别数据，1男0女，而本地是1男2女
            if (dic.TryGetValue("sex", out str)) Sex = str.ToInt() == 1 ? 1 : 2;

            // small image: http://tb.himg.baidu.com/sys/portraitn/item/{$portrait}
            // large image: http://tb.himg.baidu.com/sys/portrait/item/{$portrait}
            if (dic.TryGetValue("portrait", out str)) Avatar = "http://tb.himg.baidu.com/sys/portrait/item/" + str.Trim();

            // 百度升级协议后，用户名带有星号，不能要
            if (!UserName.IsNullOrEmpty() && UserName.Contains("*"))
            {
                if (NickName.IsNullOrEmpty()) NickName = UserName;
                UserName = null;
            }
        }

        ///// <summary>根据授权码获取访问令牌</summary>
        ///// <param name="code"></param>
        ///// <returns></returns>
        //public override String GetAccessToken(String code)
        //{
        //    var html = base.GetAccessToken(code);

        //    var dic = Items;
        //    if (dic.TryGetValue("error", out var error)) throw new Exception($"{error} {dic["error_description"]}");

        //    return html;
        //}
    }
}
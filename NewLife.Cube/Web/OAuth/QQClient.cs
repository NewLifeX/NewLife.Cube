using System;
using System.Collections.Generic;

namespace NewLife.Web.OAuth
{
    /// <summary>身份验证提供者</summary>
    /// <remarks>
    /// 平台 https://connect.qq.com/manage.html
    /// </remarks>
    public class QQClient : OAuthClient
    {
        /// <summary>实例化</summary>
        public QQClient()
        {
            Server = "https://graph.qq.com/oauth2.0/";

            AuthUrl = "authorize?response_type={response_type}&client_id={key}&redirect_uri={redirect}&state={state}&scope={scope}";
            AccessUrl = "token?grant_type=authorization_code&client_id={key}&client_secret={secret}&code={code}&state={state}&redirect_uri={redirect}";
            OpenIDUrl = "me?access_token={token}";
            UserUrl = "https://graph.qq.com/user/get_user_info?access_token={token}&oauth_consumer_key={key}&openid={openid}";
        }

        /// <summary>是否支持指定用户端，也就是判断是否在特定应用内打开，例如QQ/DingDing/WeiXin</summary>
        /// <param name="userAgent"></param>
        /// <returns></returns>
        public override Boolean Support(String userAgent) => !userAgent.IsNullOrEmpty() && userAgent.Contains(" QQ/");

        /// <summary>发起请求，获取内容</summary>
        /// <param name="action"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        protected override String GetHtml(String action, String url)
        {
            var html = base.GetHtml(action, url);

            // 去掉js回调函数
            if (!html.IsNullOrEmpty() && html.StartsWithIgnoreCase("callback("))
            {
                html = html.Substring("callback(").TrimEnd(");").Trim();
            }

            return html;
        }

        /// <summary>从响应数据中获取信息</summary>
        /// <param name="dic"></param>
        protected override void OnGetInfo(IDictionary<String, String> dic)
        {
            // 获取用户信息出错时抛出异常
            if (dic.TryGetValue("error", out var str) && str.ToInt() != 0 &&
                dic.TryGetValue("error_description", out str)) throw new InvalidOperationException(str);

            base.OnGetInfo(dic);

            //if (dic.TryGetValue("nickname", out var str)) NickName = str.Trim();
            //if (dic.TryGetValue("client_id", out var str)) UserID = str.ToLong();
            // 修改性别数据，本地是1男2女
            if (dic.TryGetValue("gender", out str)) Sex = str == "男" ? 1 : (str == "女" ? 2 : 0);

            // 从大到小找头像
            var avs = "figureurl_qq_2,figureurl_qq_1,figureurl_2,figureurl_1,figureurl".Split(",");
            foreach (var item in avs)
            {
                if (dic.TryGetValue(item, out var av) && !av.IsNullOrEmpty())
                {
                    Avatar = av.Trim();
                    break;
                }
            }

            AreaName = null;

            //// 生日
            //if (dic.TryGetValue("year", out str) && str.ToInt() > 0) Birthday = new DateTime(str.ToInt(), 1, 1);
        }
    }
}
using System;
using System.Collections.Generic;

namespace NewLife.Web.OAuth
{
    /// <summary>微博身份验证提供者</summary>
    /// <remarks>
    /// 文档 https://open.weibo.com/wiki/Oauth2/authorize
    /// 用户 https://open.weibo.com/wiki/2/users/show
    /// </remarks>
    public class WeiboClient : OAuthClient
    {
        /// <summary>实例化</summary>
        public WeiboClient()
        {
            var url = "https://api.weibo.com/oauth2/";

            AuthUrl = url + "authorize?response_type={response_type}&client_id={key}&redirect_uri={redirect}&state={state}&scope={scope}&display=default";
            AccessUrl = "#" + url + "access_token?grant_type=authorization_code&client_id={key}&client_secret={secret}&code={code}&state={state}&redirect_uri={redirect}";
            UserUrl = "https://api.weibo.com/2/users/show.json?uid={userid}&access_token={token}";
        }

        /// <summary>从响应数据中获取信息</summary>
        /// <param name="dic"></param>
        protected override void OnGetInfo(IDictionary<String, String> dic)
        {
            base.OnGetInfo(dic);

            if (dic.TryGetValue("screen_name", out var str)) NickName = str.Trim();
            if (dic.TryGetValue("gender", out str)) Sex = str == "m" ? 1 : (str == "f" ? 2 : 0);
            if (dic.TryGetValue("description", out str)) Detail = str.Trim();

            // 地区
            AreaName = null;
            var rid = 0;
            if (dic.TryGetValue("province", out str)) rid = str.ToInt() * 10000;
            if (dic.TryGetValue("city", out str)) rid += str.ToInt() * 100;
            if (rid > 0)
                AreaId = rid;
            else if (dic.TryGetValue("location", out str))
                AreaName = str.Trim().Replace(' ', '/');

            if (Avatar.IsNullOrEmpty() && dic.TryGetValue("avatar_hd", out str)) Avatar = str.Trim();
            if (Avatar.IsNullOrEmpty() && dic.TryGetValue("avatar_large", out str)) Avatar = str.Trim();
            if (Avatar.IsNullOrEmpty() && dic.TryGetValue("profile_image_url", out str)) Avatar = str.Trim();
        }
    }
}
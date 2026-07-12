using NewLife.Model;

namespace NewLife.Web.OAuth;

/// <summary>微信小程序登录客户端</summary>
/// <remarks>
/// 文档 https://developers.weixin.qq.com/miniprogram/dev/OpenApiDoc/user-login/code2Session.html
/// 微信小程序登录流程与标准OAuth2.0不同，使用 jscode2session 接口换取 session_key 和 openid。
/// 小程序端调用 wx.login() 获取 code，传给后端，后端使用 code 换取 session_key。
/// </remarks>
public class WxOpenClient : OAuthClient
{
    /// <summary>会话密钥</summary>
    public String SessionKey { get; set; }

    /// <summary>实例化</summary>
    public WxOpenClient()
    {
        Name = "WxOpen";

        // 小程序不需要跳转授权，直接使用 jscode2session 接口
        Server = "https://api.weixin.qq.com/";
        AuthUrl = null; // 小程序无跳转授权
        AccessUrl = "sns/jscode2session?appid={key}&secret={secret}&js_code={code}&grant_type=authorization_code";
        UserUrl = null; // 用户信息通过前端解密获取，不需要后端获取
    }

    /// <summary>根据小程序登录码获取会话信息</summary>
    /// <param name="code">wx.login() 返回的 code</param>
    /// <returns>返回原始响应内容</returns>
    public override String GetAccessToken(String code)
    {
        if (code.IsNullOrEmpty()) throw new ArgumentNullException(nameof(code), "登录码不能为空");

        Code = code;

        var url = GetUrl(nameof(GetAccessToken), AccessUrl);
        WriteLog("GetAccessToken {0}", url);

        var html = GetHtml(nameof(GetAccessToken), url);
        if (html.IsNullOrEmpty()) return null;

        var dic = GetNameValues(html);
        if (dic != null)
        {
            // 解析返回结果
            if (dic.TryGetValue("session_key", out var v) && v is String sk) SessionKey = sk.Trim();
            if (dic.TryGetValue("openid", out v) && v is String oid) OpenID = oid.Trim();
            if (dic.TryGetValue("unionid", out v) && v is String uid) UnionID = uid.Trim();

            // 检查错误
            if (dic.TryGetValue("errcode", out v) && v != null && v.ToString() != "0")
            {
                var err = v.ToString();
                var msg = dic.TryGetValue("errmsg", out v) && v != null ? v.ToString() : "未知错误";
                throw new InvalidOperationException($"微信小程序登录失败[{err}]: {msg}");
            }

            var strDic = dic.Where(e => e.Value is String).ToDictionary(e => e.Key, e => (String)e.Value);
            OnGetInfo(strDic);
        }
        Items = dic;

        // 小程序不返回 access_token，使用 session_key 作为标识
        AccessToken = SessionKey;

        return html;
    }

    /// <summary>从响应数据中获取信息</summary>
    /// <param name="dic"></param>
    protected override void OnGetInfo(IDictionary<String, String> dic)
    {
        base.OnGetInfo(dic);

        if (dic.TryGetValue("session_key", out var str)) SessionKey = str.Trim();
    }

    /// <summary>填充用户信息</summary>
    /// <param name="user"></param>
    public override void Fill(IManageUser user)
    {
        base.Fill(user);

        // 小程序用户默认使用 OpenID 作为用户名
        if (user.Name.IsNullOrEmpty() && !OpenID.IsNullOrEmpty())
            user.Name = $"小程序用户{OpenID[^8..]}";
    }
}

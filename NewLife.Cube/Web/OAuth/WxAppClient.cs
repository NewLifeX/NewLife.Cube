using NewLife.Model;

namespace NewLife.Web.OAuth;

/// <summary>微信移动APP登录客户端</summary>
/// <remarks>
/// 文档 https://developers.weixin.qq.com/doc/oplatform/Mobile_App/WeChat_Login/Development_Guide.html
/// 微信移动应用使用 OAuth2.0 授权码模式登录。
/// APP端调用微信SDK获取 code，传给后端，后端使用 code 换取 access_token 和用户信息。
/// </remarks>
public class WxAppClient : OAuthClient
{
    /// <summary>实例化</summary>
    public WxAppClient()
    {
        Name = "WxApp";

        // 移动应用登录使用标准OAuth2.0流程
        Server = "https://api.weixin.qq.com/";
        AuthUrl = null; // APP端不需要跳转，SDK调起微信授权
        AccessUrl = "sns/oauth2/access_token?appid={key}&secret={secret}&code={code}&grant_type=authorization_code";
        UserUrl = "sns/userinfo?access_token={token}&openid={openid}&lang=zh_CN";
    }

    /// <summary>从响应数据中获取信息</summary>
    /// <param name="dic"></param>
    protected override void OnGetInfo(IDictionary<String, String> dic)
    {
        base.OnGetInfo(dic);

        // 微信返回的头像字段
        if (dic.TryGetValue("headimgurl", out var str)) Avatar = str.Trim();

        // 检查错误
        if (dic.TryGetValue("errcode", out var err) && err != "0")
        {
            var msg = dic.TryGetValue("errmsg", out var m) ? m : "未知错误";
            throw new InvalidOperationException($"微信APP登录失败[{err}]: {msg}");
        }
    }

    /// <summary>填充用户信息</summary>
    /// <param name="user"></param>
    public override void Fill(IManageUser user)
    {
        base.Fill(user);

        // 微信昵称可能包含通用名称
        if (!NickName.IsNullOrEmpty() && (user.NickName == "微信用户"))
            user.NickName = NickName;

        // APP用户默认使用 OpenID 作为用户名
        if (user.Name.IsNullOrEmpty() && !OpenID.IsNullOrEmpty())
            user.Name = $"微信用户{OpenID[^8..]}";
    }
}

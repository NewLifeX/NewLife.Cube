using System.Net.Http.Headers;
using NewLife.Http;
using NewLife.Log;

namespace NewLife.Web.OAuth;

/// <summary>身份验证提供者</summary>
/// <remarks>
/// 平台 https://github.com/settings/developers
/// </remarks>
public class GithubClient : OAuthClient
{
    /// <summary>令牌类型</summary>
    public String TokenType { get; set; }

    /// <summary>实例化</summary>
    public GithubClient()
    {
        Server = "https://github.com/login/oauth/";

        AuthUrl = "authorize?response_type={response_type}&client_id={key}&redirect_uri={redirect}&state={state}&scope={scope}";
        AccessUrl = "access_token?grant_type=authorization_code&code={code}&state={state}&redirect_uri={redirect}";
        UserUrl = "https://api.github.com/user";
    }

    /// <summary>从响应数据中获取信息</summary>
    /// <param name="dic"></param>
    protected override void OnGetInfo(IDictionary<String, String> dic)
    {
        base.OnGetInfo(dic);

        if (dic.TryGetValue("id", out var str)) UserID = str.Trim('\"').ToLong();
        if (dic.TryGetValue("login", out str)) UserName = str.Trim();
        if (dic.TryGetValue("name", out str)) NickName = str.Trim();
        if (dic.TryGetValue("avatar_url", out str)) Avatar = str.Trim();
        if (dic.TryGetValue("bio", out str)) Detail = str.Trim();
        if (dic.TryGetValue("token_type", out str)) TokenType = str.Trim();
    }

    /// <summary>创建客户端。github要求必须有useragent</summary>
    /// <returns></returns>
    protected override HttpClient CreateClient()
    {
        var client = base.CreateClient();

        // Request forbidden by administrative rules.
        // Please make sure your request has a User-Agent header (https://docs.github.com/en/rest/overview/resources-in-the-rest-api#user-agent-required).
        // Check https://developer.github.com for other possible causes.
        client.SetUserAgent();

        return client;
    }

    /// <summary>发起请求，获取内容</summary>
    /// <param name="action"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    protected override String GetHtml(String action, String url)
    {
        // 部分提供者密钥写在头部
        var client = GetClient();
        var html = "";
        if (action == nameof(GetAccessToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{Key}:{Secret}".GetBytes().ToBase64());
            html = client.SendAsync(request).Result.Content.ReadAsStringAsync().Result;
        }
        else if (!AccessToken.IsNullOrEmpty())
        {
            //var type = TokenType;
            //if (type.IsNullOrEmpty()) type = "Bearer";
            var type = "Bearer";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue(type, AccessToken);
            html = client.SendAsync(request).Result.Content.ReadAsStringAsync().Result;
        }
        else
        {
            html = client.GetStringAsync(url).Result;
        }
        if (html.IsNullOrEmpty()) return null;

        html = html.Trim();
        if (Log != null && Log.Enable) WriteLog(html);

        if (!html.IsNullOrEmpty())
            DefaultSpan.Current?.AppendTag(html);

        return html;
    }
}
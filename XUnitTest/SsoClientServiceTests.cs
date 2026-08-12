using System;
using Microsoft.AspNetCore.Http;
using NewLife.Cube.Services.Sso;
using Xunit;

namespace XUnitTest;

/// <summary>SSO客户端服务单元测试</summary>
/// <remarks>验证 GetRedirect 构建的OAuth回跳地址。SsoController 在 /Sso 路由（无 /api 前缀），回跳地址不带前缀。
/// 传 providerName=null 跳过 OAuthConfig 数据库查询，聚焦URL拼接逻辑。</remarks>
public class SsoClientServiceTests
{
    private static HttpRequest BuildRequest(String path)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "http";
        ctx.Request.Host = new HostString("localhost:8080");
        ctx.Request.Path = path;
        return ctx.Request;
    }

    [Fact(DisplayName = "GetRedirect 回跳地址不带API前缀")]
    public void GetRedirect_NoApiPrefix()
    {
        var svc = new SsoClientService();
        var req = BuildRequest("/Sso/Login?name=NewLife");

        var url = svc.GetRedirect(req, "~/Sso/LoginInfo/NewLife", null);

        Assert.Equal("http://localhost:8080/Sso/LoginInfo/NewLife", url);
    }

    [Fact(DisplayName = "GetRedirect 请求路径带 /api 时回跳地址按绝对路径解析")]
    public void GetRedirect_AbsolutePath_IgnoresRequestApiPrefix()
    {
        var svc = new SsoClientService();
        var req = BuildRequest("/api/Sso/Login?name=NewLife");

        // redirectUrl 是绝对路径（~/Sso/LoginInfo/NewLife → /Sso/LoginInfo/NewLife），与请求是否带 /api 无关
        var url = svc.GetRedirect(req, "~/Sso/LoginInfo/NewLife", null);

        Assert.Equal("http://localhost:8080/Sso/LoginInfo/NewLife", url);
    }
}

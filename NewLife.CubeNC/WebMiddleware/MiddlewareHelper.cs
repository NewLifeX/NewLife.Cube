using NewLife.Web;
using HttpContext = Microsoft.AspNetCore.Http.HttpContext;

namespace NewLife.Cube.WebMiddleware;

/// <summary>中间件助手</summary>
public static class MiddlewareHelper
{
    /// <summary>检查是否强制跳转</summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    public static Boolean CheckForceRedirect(HttpContext ctx)
    {
        if (ctx.Request.Method != "GET") return false;

        var set = CubeSetting.Current;
        if (set.ForceRedirect.IsNullOrEmpty()) return false;

        // 分解跳转地址
        var ss = set.ForceRedirect.Split("://", ":");
        var scheme = ss.Length > 0 ? ss[0] : "";
        var host = ss.Length > 1 ? ss[1] : ss[0];
        var port = ss.Length > 2 ? ss[2].ToInt() : 0;
        if (ss.Length == 1) scheme = null;
        if (host == "*") host = null;

        if (scheme.IsNullOrEmpty() && host.IsNullOrEmpty() && port == 0) return false;

        var uri = ctx.Request.GetRawUrl();
        if ((scheme.IsNullOrEmpty() || uri.Scheme.EqualIgnoreCase(scheme)) &&
            (host.IsNullOrEmpty() || uri.Host.EqualIgnoreCase(host)) &&
            (port == 0 || port == uri.Port)) return false;

        // 重建url
        if (scheme.IsNullOrEmpty()) scheme = uri.Scheme;
        if (host.IsNullOrEmpty()) host = uri.Host;
        if (port == 0) port = uri.Port;

        var url = $"{scheme}://{host}:{port}{uri.PathAndQuery}";

        ctx.Response.Redirect(url);

        return true;
    }
}

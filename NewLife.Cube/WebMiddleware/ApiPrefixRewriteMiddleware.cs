using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NewLife;
using NewLife.Cube;
using NewLife.Log;

namespace NewLife.Cube.WebMiddleware;

/// <summary>
/// API 前缀重写中间件。
/// 读取 CubeSetting 中配置的 API 前缀（如 /api,/api/v1），提供两件事：
/// 1. 命中前缀的请求：自动去掉前缀，转发到去掉前缀后的真实路由。
///    例如：配置前缀 /api，请求 /api/Admin/User/Index → 重写为 /Admin/User/Index 继续后续管道（路由/鉴权/静态文件）。
/// 2. 已配置前缀时，未命中前缀且“无文件扩展名”的 GET 请求（前端路由，而非 .css/.js 等静态资源）
///    回退到 wwwroot 默认页（如 index.html），用于支持历史模式（History Mode）前端项目的刷新。
///    例如部署了 Vue/React 等 SPA 后，刷新 /Admin/User 不再命中后端接口返回 JSON，
///    而是返回 SPA 外壳 HTML，由前端路由接管。
/// 采用路径重写（Path Rewrite）而非 3xx 重定向，对客户端透明。
/// 回退仅在默认页真实存在时生效，避免未部署 SPA 时破坏原有后台页面（不会 404）。
/// </summary>
public class ApiPrefixRewriteMiddleware
{
    private readonly RequestDelegate _next;

    // 已解析的前缀集合（标准化：以 / 开头、去尾部 /）
    private static String[] _prefixes = Array.Empty<String>();
    // 缓存键（前缀 + 默认页），用于检测运行时变更并重置默认页存在性缓存
    private static String _cacheKey;
    // 默认页文件名，仅在配置中“显式非空”时使用，否则回退到 index.html
    private static String _defaultPage = "index.html";
    // 默认页是否存在（null 表示尚未探测），存在才做 SPA 回退
    private static Boolean? _defaultPageExists;

    /// <summary>实例化</summary>
    public ApiPrefixRewriteMiddleware(RequestDelegate next) => _next = next;

    /// <summary>中间件处理</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // 每次请求读取最新配置，配置变更时重新解析（无需重启生效）
        var set = CubeSetting.Current;
        var apiPrefixes = set.ApiPrefixes;
        var spaDefaultPage = set.SpaDefaultPage;
        var key = apiPrefixes + "|" + spaDefaultPage;
        if (!String.Equals(key, _cacheKey, StringComparison.Ordinal))
        {
            _cacheKey = key;
            _prefixes = (apiPrefixes ?? "")
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim().EnsureStart("/").TrimEnd('/'))
                .Where(p => p.Length > 1)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            _defaultPage = spaDefaultPage.Trim().TrimStart('/');
            if (_defaultPage.IsNullOrEmpty()) _defaultPage = "index.html";
            // 配置变更时重置默认页存在性缓存
            _defaultPageExists = null;
            if (_prefixes.Length > 0)
                XTrace.WriteLine("API 前缀中间件已加载前缀：{0}，SPA 默认页：{1}", String.Join(",", _prefixes), _defaultPage);
        }

        var path = context.Request.Path.Value ?? "";

        // 1. 命中 API 前缀：去掉前缀，转发到真实路由
        var isApi = false;
        if (_prefixes.Length > 0)
        {
            foreach (var prefix in _prefixes)
            {
                // 前缀精确匹配：prefix 后紧跟 / 或正好等于完整路径，
                // 避免 /api/v1X 误匹配 /api/v1
                if (path.Length >= prefix.Length &&
                    path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    (path.Length == prefix.Length || path[prefix.Length] == '/'))
                {
                    var after = path[prefix.Length..].TrimStart('/');
                    context.Request.Path = "/" + after;
                    isApi = true;
                    break;
                }
            }
        }

        // 2. SPA 历史模式回退：已配置前缀，且未命中 API 前缀的 GET 请求
        if (_prefixes.Length > 0 && !isApi && context.Request.Method.EqualIgnoreCase("GET"))
        {
            // 仅对“无扩展名”的路径回退，带扩展名的静态资源（.css/.js/.png 等）交给静态文件中间件
            if (!Path.HasExtension(path))
            {
                if (_defaultPageExists == null)
                {
                    var env = context.RequestServices.GetService<IWebHostEnvironment>();
                    var fp = env?.WebRootFileProvider;
                    _defaultPageExists = fp?.GetFileInfo(_defaultPage).Exists == true;
                    if (_defaultPageExists == true)
                        XTrace.WriteLine("API 前缀中间件启用 SPA 回退，默认页：{0}", _defaultPage);
                }

                if (_defaultPageExists == true)
                {
                    // 浏览器地址栏仍为 /Admin/User，仅服务端重写到默认页以返回 SPA 外壳
                    context.Request.Path = "/" + _defaultPage;
                }
            }
        }

        await _next(context);
    }
}

/// <summary>API 前缀重写中间件扩展方法</summary>
public static class ApiPrefixRewriteMiddlewareExtensions
{
    /// <summary>注册 API 前缀重写中间件</summary>
    public static IApplicationBuilder UseApiPrefixRewrite(this IApplicationBuilder builder)
        => builder.UseMiddleware<ApiPrefixRewriteMiddleware>();
}

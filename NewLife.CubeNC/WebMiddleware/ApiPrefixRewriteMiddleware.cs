using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using NewLife;
using NewLife.Cube;
using NewLife.Log;

namespace NewLife.Cube.WebMiddleware;

/// <summary>
/// API 前缀重写中间件。
/// 读取 CubeSetting 中配置的 API 前缀（如 /api,/api/v1），
/// 命中前缀的请求自动去掉前缀，转发到去掉前缀后的真实路由。
/// 例如：配置前缀 /api，请求 /api/Admin/User/Index
///      → 重写为 /Admin/User/Index 继续后续管道（路由/鉴权/静态文件）。
/// 采用路径重写（Path Rewrite）而非 3xx 重定向，对客户端透明，
/// 与参考中间件 AdminApiRewriteMiddleware 行为一致。
/// </summary>
public class ApiPrefixRewriteMiddleware
{
    private readonly RequestDelegate _next;

    // 已解析的前缀集合（标准化：以 / 开头、去尾部 /）
    private static String[] _prefixes = Array.Empty<String>();
    // 上一次读取的原始配置，用于检测运行时变更
    private static String _raw;

    /// <summary>实例化</summary>
    public ApiPrefixRewriteMiddleware(RequestDelegate next) => _next = next;

    /// <summary>中间件处理</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // 每次请求读取最新配置，配置变更时重新解析（无需重启生效）
        var setting = CubeSetting.Current.ApiPrefixes;
        if (!String.Equals(setting, _raw, StringComparison.Ordinal))
        {
            _raw = setting;
            _prefixes = (setting ?? "")
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim().EnsureStart("/").TrimEnd('/'))
                .Where(p => p.Length > 1)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (_prefixes.Length > 0)
                XTrace.WriteLine("API 前缀中间件已加载前缀：{0}", String.Join(",", _prefixes));
        }

        if (_prefixes.Length > 0)
        {
            var path = context.Request.Path.Value ?? "";
            foreach (var prefix in _prefixes)
            {
                // 前缀精确匹配：prefix 后紧跟 / 或正好等于完整路径，
                // 避免 /apiX 误匹配 /api
                if (path.Length >= prefix.Length &&
                    path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    (path.Length == prefix.Length || path[prefix.Length] == '/'))
                {
                    var after = path[prefix.Length..].TrimStart('/');
                    context.Request.Path = "/" + after;
                    break;
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

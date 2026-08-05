using System.Net.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NewLife.Cube.Services;

/// <summary>默认 <see cref="IHttpClientFactory"/> 实现。
/// 用于在未调用 <c>AddHttpClient()</c> 的环境中为 <see cref="DefaultLovListDataProxy"/> 提供 HTTP 客户端。
/// 复用单一 <see cref="HttpClient"/> 实例（避免频繁创建导致 Socket 耗尽）；若使用者已通过 <c>AddHttpClient()</c> 注册了工厂，
/// 本实现以 TryAddSingleton 注册，不会被覆盖。</summary>
public class DefaultHttpClientFactory : IHttpClientFactory
{
    // 复用的单一实例。简易默认实现，长连接下 DNS 变更不敏感场景可接受；需精细生命周期管理时请使用 AddHttpClient()。
    private static readonly HttpClient _sharedClient = new HttpClient();

    /// <inheritdoc />
    public HttpClient CreateClient(String name) => _sharedClient;
}

/// <summary>为 <see cref="DefaultHttpClientFactory"/> 提供的注册扩展</summary>
public static class HttpClientFactoryExtensions
{
    /// <summary>若尚未注册 <see cref="IHttpClientFactory"/>，则注册 <see cref="DefaultHttpClientFactory"/>（不覆盖 AddHttpClient 的注册）。</summary>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection TryAddDefaultHttpClientFactory(this IServiceCollection services)
    {
        services.TryAddSingleton<IHttpClientFactory, DefaultHttpClientFactory>();
        return services;
    }
}

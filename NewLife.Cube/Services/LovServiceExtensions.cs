using NewLife.Cube.Services;
using NewLife.Log;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace NewLife.Cube;

/// <summary>Lov 服务扩展</summary>
public static class LovServiceExtensions
{
    /// <summary>添加值集（Lov）服务</summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">配置委托</param>
    /// <returns></returns>
    public static IServiceCollection AddCubeLov(this IServiceCollection services, Action<LovServiceOptions> configure)
    {
        var options = new LovServiceOptions();
        configure(options);
        services.AddSingleton(options);

        var registerService = new LovAutoRegisterService();
        foreach (var ns in options.NamespacePrefixes)
        {
            registerService.NamespacePrefixes.Add(ns);
        }
        services.AddSingleton(registerService);

        // 注册列表型值集数据代理。使用 TryAddSingleton：使用者可在本调用之前注册自己的 ILovListDataProxy 实现来覆盖默认转发逻辑；
        // 若需在之后覆盖，也可再次调用 services.Replace(ServiceDescriptor.Singleton<ILovListDataProxy, 自定义实现>())。
        services.TryAddSingleton<ILovListDataProxy, DefaultLovListDataProxy>();

        // 注册 IHttpClientFactory（DefaultLovListDataProxy 依赖）。若使用者已通过 AddHttpClient() 注册则保留其实现。
        services.TryAddDefaultHttpClientFactory();

        return services;
    }
}

/// <summary>Lov 服务配置选项</summary>
public class LovServiceOptions
{
    /// <summary>命名空间前缀列表</summary>
    internal List<String> NamespacePrefixes { get; } = new List<String>();

    /// <summary>添加要扫描的命名空间前缀</summary>
    /// <param name="namespacePrefix">命名空间前缀，如 SmartMES.Data</param>
    /// <returns></returns>
    public LovServiceOptions ScanNamespace(String namespacePrefix)
    {
        NamespacePrefixes.Add(namespacePrefix);
        return this;
    }
}

    /// <summary>Lov 数据库初始化扩展</summary>
    public static class LovDatabaseExtensions
    {
        /// <summary>初始化 Lov 表并执行枚举/列表型值集自动注册</summary>
        /// <remarks>
        /// 注册必须在应用真正启动、数据库表创建完成之后进行（首跑建库耗时较长）。
        /// 因此挂在 ApplicationStarted 事件上，并辅以重试，避免因建表未完成导致
        /// "no such table" 而中断自动注册。
        /// </remarks>
        /// <param name="app">应用构建器</param>
        /// <returns></returns>
        public static IApplicationBuilder UseCubeLov(this IApplicationBuilder app)
        {
            var lifetime = app.ApplicationServices.GetService<IHostApplicationLifetime>();
            if (lifetime != null)
            {
                lifetime.ApplicationStarted.Register(() => _ = Task.Run(() => RunRegistration(app)));
            }
            else
            {
                // 兜底：无 Lifetime 时退化为固定延迟
                _ = Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    RunRegistration(app);
                });
            }

            return app;
        }

        private static async Task RunRegistration(IApplicationBuilder app)
        {
            // 首跑建库可能耗时较长，等待 LOV 相关表就绪后重试
            for (var attempt = 0; attempt < 30; attempt++)
            {
                try
                {
                    var provider = app.ApplicationServices;
                    var service = provider.GetService<LovAutoRegisterService>();
                    if (service != null)
                    {
                        var count = service.ScanAndRegister();
                        if (count > 0)
                            XTrace.WriteLine("Lov: 自动注册完成，共注册 {0} 个值集", count);
                        else
                            XTrace.WriteLine("Lov: 自动注册执行完成，无新增值集（可能已存在）");
                    }
                    return;
                }
                catch (Exception ex)
                {
                    XTrace.WriteLine("Lov: 自动注册第 {0} 次尝试失败，将在 2 秒后重试：{1}", attempt + 1, ex.Message);
                    await Task.Delay(2000);
                }
            }

            XTrace.WriteLine("Lov: 自动注册在多次重试后仍失败，请检查数据库连接与表结构");
        }
    }

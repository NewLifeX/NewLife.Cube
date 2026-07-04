using NewLife.Cube.Services;
using NewLife.Log;

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
    /// <summary>初始化 Lov 表并执行枚举自动注册</summary>
    /// <param name="app">应用构建器</param>
    /// <returns></returns>
    public static IApplicationBuilder UseCubeLov(this IApplicationBuilder app)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                // 等待应用启动完成
                await Task.Delay(3000);

                var provider = app.ApplicationServices;
                var service = provider.GetService<LovAutoRegisterService>();
                if (service != null)
                {
                    var count = service.ScanAndRegister();
                    if (count > 0)
                        XTrace.WriteLine("Lov: 自动注册完成，共注册 {0} 个值集", count);
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        });

        return app;
    }
}

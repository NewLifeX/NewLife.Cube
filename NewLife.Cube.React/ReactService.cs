using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.FileProviders;
using NewLife.Cube.Extensions;
using NewLife.Cube.Modules;

namespace NewLife.Cube.React;

/// <summary>React前端皮肤服务</summary>
public static class ReactService
{
    /// <summary>使用React前端皮肤。替换WebRootFileProvider以注册嵌入式静态资源，并添加SPA回退路由，提供React+Ant Design管理后台前端</summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    /// <returns></returns>
    public static WebApplication UseReact(this WebApplication app, IWebHostEnvironment env)
    {
        // 嵌入在 DLL 中的 wwwroot 文件，作为静态资源
        var assembly = Assembly.GetExecutingAssembly();
        var embeddedProvider = new CubeEmbeddedFileProvider(assembly, "NewLife.Cube.React.wwwroot");

        if (!env.WebRootPath.IsNullOrEmpty() && Directory.Exists(env.WebRootPath) && env.WebRootFileProvider != null)
        {
            // 嵌入资源优先，再到主机的 WebRootFileProvider
            env.WebRootFileProvider = new CompositeFileProvider(
                embeddedProvider,
                env.WebRootFileProvider);
        }
        else
        {
            env.WebRootFileProvider = embeddedProvider;
        }

        app.UseStaticFiles();

        // SPA 回退路由：所有未匹配 endpoint 的请求回退到 index.html（React 使用 Browser History 路由）
        app.MapFallbackToFile("index.html");

        return app;
    }
}

/// <summary>React前端皮肤模块</summary>
[DisplayName("React前端皮肤")]
internal class ReactModule : IModule
{
    /// <summary>添加模块</summary>
    /// <param name="services"></param>
    public void Add(IServiceCollection services) { }

    /// <summary>使用模块</summary>
    /// <remarks>Module模式仅注册静态文件，SPA回退路由需通过 app.UseReact() 显式调用</remarks>
    /// <param name="app"></param>
    /// <param name="env"></param>
    public void Use(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Module接口签名为IApplicationBuilder，无法调用MapFallbackToFile
        // SPA回退路由通过用户在Program.cs显式调用 app.UseReact(env) 注册
        var assembly = Assembly.GetExecutingAssembly();
        var embeddedProvider = new CubeEmbeddedFileProvider(assembly, "NewLife.Cube.React.wwwroot");

        if (!env.WebRootPath.IsNullOrEmpty() && Directory.Exists(env.WebRootPath) && env.WebRootFileProvider != null)
        {
            env.WebRootFileProvider = new CompositeFileProvider(
                embeddedProvider,
                env.WebRootFileProvider);
        }
        else
        {
            env.WebRootFileProvider = embeddedProvider;
        }

        app.UseStaticFiles();
    }
}

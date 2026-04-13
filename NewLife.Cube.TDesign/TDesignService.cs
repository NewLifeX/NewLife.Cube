using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.FileProviders;
using NewLife.Cube.Extensions;
using NewLife.Cube.Modules;

namespace NewLife.Cube.TDesign;

/// <summary>TDesign前端皮肤服务</summary>
public static class TDesignService
{
    /// <summary>使用TDesign前端皮肤。替换WebRootFileProvider以注册嵌入式静态资源，并添加SPA回退路由，提供Vue3+TDesign管理后台前端</summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    /// <returns></returns>
    public static WebApplication UseTDesign(this WebApplication app, IWebHostEnvironment env)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var embeddedProvider = new CubeEmbeddedFileProvider(assembly, "NewLife.Cube.TDesign.wwwroot");

        if (!env.WebRootPath.IsNullOrEmpty() && Directory.Exists(env.WebRootPath) && env.WebRootFileProvider != null)
        {
            env.WebRootFileProvider = new CompositeFileProvider(embeddedProvider, env.WebRootFileProvider);
        }
        else
        {
            env.WebRootFileProvider = embeddedProvider;
        }

        app.UseStaticFiles();
        app.MapFallbackToFile("index.html");

        return app;
    }
}

/// <summary>TDesign前端皮肤模块</summary>
[DisplayName("TDesign前端皮肤")]
internal class TDesignModule : IModule
{
    /// <summary>添加模块</summary>
    /// <param name="services"></param>
    public void Add(IServiceCollection services) { }

    /// <summary>使用模块</summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    public void Use(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var embeddedProvider = new CubeEmbeddedFileProvider(assembly, "NewLife.Cube.TDesign.wwwroot");

        if (!env.WebRootPath.IsNullOrEmpty() && Directory.Exists(env.WebRootPath) && env.WebRootFileProvider != null)
        {
            env.WebRootFileProvider = new CompositeFileProvider(embeddedProvider, env.WebRootFileProvider);
        }
        else
        {
            env.WebRootFileProvider = embeddedProvider;
        }

        app.UseStaticFiles();
    }
}

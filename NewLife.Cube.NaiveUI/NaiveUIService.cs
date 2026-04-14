using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.FileProviders;
using NewLife.Cube.Extensions;
using NewLife.Cube.Modules;

namespace NewLife.Cube.NaiveUI;

/// <summary>NaiveUI前端皮肤服务</summary>
public static class NaiveUIService
{
    /// <summary>使用NaiveUI前端皮肤。替换WebRootFileProvider以注册嵌入式静态资源，并添加SPA回退路由，提供Vue3+Naive UI管理后台前端</summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    /// <returns></returns>
    public static WebApplication UseNaiveUI(this WebApplication app, IWebHostEnvironment env)
    {
        // 嵌入在 DLL 中的 wwwroot 文件，作为静态资源
        var assembly = Assembly.GetExecutingAssembly();
        var embeddedProvider = new CubeEmbeddedFileProvider(assembly, "NewLife.Cube.NaiveUI.wwwroot");

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

        // SPA 回退路由：所有未匹配 endpoint 的请求回退到 index.html，支持 Vue History 路由模式
        app.MapFallbackToFile("index.html");

        return app;
    }
}

/// <summary>NaiveUI前端皮肤模块</summary>
[DisplayName("NaiveUI前端皮肤")]
internal class NaiveUIModule : IModule
{
    /// <summary>添加模块</summary>
    /// <param name="services"></param>
    public void Add(IServiceCollection services) { }

    /// <summary>使用模块</summary>
    /// <remarks>Module模式仅注册静态文件，SPA回退路由需通过 app.UseNaiveUI() 显式调用</remarks>
    /// <param name="app"></param>
    /// <param name="env"></param>
    public void Use(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Module接口签名为IApplicationBuilder，无法调用MapFallbackToFile
        // SPA回退路由通过用户在Program.cs显式调用 app.UseNaiveUI(env) 注册
        var assembly = Assembly.GetExecutingAssembly();
        var embeddedProvider = new CubeEmbeddedFileProvider(assembly, "NewLife.Cube.NaiveUI.wwwroot");

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

using System.ComponentModel;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using NewLife.Cube.Extensions;
using NewLife.Cube.Modules;
using NewLife.Cube.Services;

namespace NewLife.Cube.LayuiAdmin;

/// <summary>LayuiAdmin服务</summary>
public static class LayuiAdminService
{
    /// <summary>使用魔方UI</summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseLayuiAdmin(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        // 独立静态文件设置，魔方自己的静态资源内嵌在程序集里面
        var options = new StaticFileOptions();
        {
            var embeddedProvider = new CubeEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "NewLife.Cube.LayuiAdmin.wwwroot");
            if (!env.WebRootPath.IsNullOrEmpty() && Directory.Exists(env.WebRootPath))
                options.FileProvider = new CompositeFileProvider(new PhysicalFileProvider(env.WebRootPath), embeddedProvider);
            else
                options.FileProvider = embeddedProvider;
        }
        app.UseStaticFiles(options);

        var ui = app.ApplicationServices.GetService<UIService>();
        if (ui != null)
        {
            ui.AddTheme("LayuiAdmin");
            ui.AddSkin("LayuiAdmin");
        }

        return app;
    }
}

[DisplayName("LayuiAdmin皮肤")]
internal class LayuiAdminModule : IModule
{
    public void Add(IServiceCollection services) { }
    public void Use(IApplicationBuilder app, IWebHostEnvironment env) => app.UseLayuiAdmin(env);
}
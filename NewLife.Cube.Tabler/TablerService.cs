using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using NewLife.Cube.Extensions;
using NewLife.Cube.Services;
using NewLife.Model;

namespace NewLife.Cube
{
    /// <summary>Tabler服务</summary>
    public static class TablerService
    {
        /// <summary>使用魔方UI</summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseTabler(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            // 独立静态文件设置，魔方自己的静态资源内嵌在程序集里面
            var options = new StaticFileOptions();
            {
                var embeddedProvider = new CubeEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "NewLife.Cube.Tabler.wwwroot");
                options.FileProvider = embeddedProvider;
            }
            app.UseStaticFiles(options);

            var ui = app.ApplicationServices.GetService<UIService>();
            if (ui != null)
            {
                ui.AddTheme("Tabler");
                ui.AddSkin("Tabler");
            }

            return app;
        }
    }
}
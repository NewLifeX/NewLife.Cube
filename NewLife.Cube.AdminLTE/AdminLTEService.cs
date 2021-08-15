using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using NewLife.Cube.Extensions;
using NewLife.Cube.Services;
using NewLife.Model;

namespace NewLife.Cube
{
    /// <summary>AdminLTE服务</summary>
    public static class AdminLTEService
    {
        /// <summary>使用魔方UI</summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseAdminLTE(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            // 独立静态文件设置，魔方自己的静态资源内嵌在程序集里面
            var options = new StaticFileOptions();
            {
                var embeddedProvider = new CubeEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "NewLife.Cube.AdminLTE.wwwroot");
                options.FileProvider = embeddedProvider;
            }
            app.UseStaticFiles(options);

            var ui = ModelExtension.GetService<UIService>(app.ApplicationServices);
            if (ui != null)
            {
                ui.AddTheme("AdminLTE");
                ui.AddSkin("AdminLTE");
            }

            return app;
        }
    }
}
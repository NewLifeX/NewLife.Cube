using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using NewLife.Cube.Extensions;
using NewLife.Cube.Services;
using NewLife.Model;

namespace NewLife.Cube
{
    /// <summary>ElementUI服务</summary>
    public static class ElementUIService
    {
        /// <summary>使用魔方UI</summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseElementUI(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            // 独立静态文件设置，魔方自己的静态资源内嵌在程序集里面
            var options = new StaticFileOptions();
            {
                var physicalProvider = new PhysicalFileProvider(env.WebRootPath);
                var embeddedProvider = new CubeEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "NewLife.Cube.ElementUI.wwwroot");
                var compositeProvider = new CompositeFileProvider(physicalProvider, embeddedProvider);

                options.FileProvider = compositeProvider;
            }
            app.UseStaticFiles(options);

            var ui = ModelExtension.GetService<UIService>(app.ApplicationServices);
            if (ui != null)
            {
                ui.AddTheme("ElementUI");
                ui.AddSkin("ElementUI");
            }

            return app;
        }
    }
}
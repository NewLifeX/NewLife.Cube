using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using NewLife.Cube.Extensions;

namespace NewLife.Cube
{
    /// <summary>Tabler服务</summary>
    public static class BlazorService
    {
        /// <summary>
        /// 添加Blazor方案
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns></returns>
        public static IServiceCollection AddBlazor(this IServiceCollection services)
        {
            // Blazor Server方式渲染
            services.AddRazorPages(options => { options.RootDirectory = "/Views/Blazor"; });
            services.AddServerSideBlazor().AddCircuitOptions(options => { options.DetailedErrors = true; });
            services.AddBootstrapBlazor();

            services.AddHttpContextAccessor();
            services.AddScoped<HttpContextAccessor>();

            services.AddHttpClient();
            services.AddScoped<HttpClient>();

            return services;
        }

        /// <summary>使用魔方UI</summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseBlazor(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            // 独立静态文件设置，魔方自己的静态资源内嵌在程序集里面
            var options = new StaticFileOptions();
            {
                var physicalProvider = new PhysicalFileProvider(env.WebRootPath);
                var embeddedProvider = new CubeEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "NewLife.Cube.Blazor.wwwroot");
                var compositeProvider = new CompositeFileProvider(physicalProvider, embeddedProvider);

                options.FileProvider = compositeProvider;
            }
            app.UseStaticFiles(options);

            app.UseEndpoints(endpoint =>
            {
                // Razor路由方案
                var razor = endpoint.MapRazorPages();
                // Blazor通信方案
                var component = endpoint.MapBlazorHub();
                // 设置Blazor前缀的URL 都经过组件页
                //endpoint.MapFallbackToPage("/Blazor/{**segment}", "/CubeMain");
            });

            return app;
        }
    }
}
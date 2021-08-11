using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using NewLife.Cube.Services;
using NewLife.Log;

namespace NewLife.Cube.Extensions
{
    ///// <summary>默认UI配置选项</summary>
    //public class DefaultUIConfigureOptions : IPostConfigureOptions<StaticFileOptions>
    //{
    //    /// <summary>环境</summary>
    //    public IWebHostEnvironment Environment { get; }

    //    /// <summary>实例化</summary>
    //    /// <param name="environment"></param>
    //    public DefaultUIConfigureOptions(IWebHostEnvironment environment) => Environment = environment;

    //    /// <summary>提交配置</summary>
    //    /// <param name="name"></param>
    //    /// <param name="options"></param>
    //    public void PostConfigure(String name, StaticFileOptions options)
    //    {
    //        //if (name.IsNullOrEmpty()) throw new ArgumentException(nameof(name));
    //        if (options == null) throw new ArgumentException(nameof(options));

    //        // 如果没有被其他组件初始化，在这里初始化
    //        options.ContentTypeProvider ??= new FileExtensionContentTypeProvider();
    //        if (options.FileProvider == null && Environment.ContentRootFileProvider == null)
    //        {
    //            throw new InvalidOperationException("缺少FileProvider");
    //        }

    //        var phy = Environment.ContentRootFileProvider as PhysicalFileProvider;
    //        XTrace.WriteLine("Root={0}", phy.Root);

    //        // 添加我们的文件提供者。第二个参数指定开始查找的文件夹，比如文件都放在wwwroot，就填“wwwroot”
    //        var physicalProvider = Environment.ContentRootFileProvider;
    //        //var embeddedProvider = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "wwwroot");
    //        var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly(), "NewLife.Cube.wwwroot");
    //        var compositeProvider = new CompositeFileProvider(physicalProvider, embeddedProvider);

    //        options.FileProvider = compositeProvider;
    //    }
    //}

    /// <summary>Ui扩展</summary>
    public static class DefaultUIServiceCollectionExtensions
    {
        ///// <summary>添加魔方UI</summary>
        ///// <param name="services"></param>
        //public static void AddCubeDefaultUI(this IServiceCollection services) => services.ConfigureOptions(typeof(DefaultUIConfigureOptions));

        /// <summary>使用魔方UI</summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseCubeDefaultUI(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            // 强行设置WebRootPath，避免魔方首次启动下载资源文件后无法马上使用的问题
            var root = Setting.Current.WebRootPath.GetFullPath();

            env.WebRootPath = root;

            XTrace.WriteLine("WebRootPath={0}", env.WebRootPath);
            XTrace.WriteLine("ContentRootPath={0}", env.ContentRootPath);

            // 独立静态文件设置，魔方自己的静态资源内嵌在程序集里面
            var options = new StaticFileOptions();
            {
                //var physicalProvider = env.ContentRootFileProvider;
                var embeddedProvider = new CubeEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "NewLife.Cube.wwwroot");
                //var compositeProvider = new CompositeFileProvider(physicalProvider, embeddedProvider);

                options.FileProvider = embeddedProvider;
            }
            app.UseStaticFiles(options);

            var ui = app.ApplicationServices.GetService<UIService>();
            if (ui != null)
            {
                ui.AddTheme("Ace");
                ui.AddSkin("Ace");

                ui.AddTheme("layui");
                ui.AddSkin("layui");
            }

            return app;
        }
    }
}
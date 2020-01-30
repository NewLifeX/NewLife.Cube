using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace NewLife.Cube.Extensions
{
    /// <summary>默认UI配置选项</summary>
    public class DefaultUIConfigureOptions : IPostConfigureOptions<StaticFileOptions>
    {
        /// <summary>实例化</summary>
        /// <param name="environment"></param>
        public DefaultUIConfigureOptions(IWebHostEnvironment environment)
        {
            Environment = environment;
        }

        /// <summary>环境</summary>
        public IWebHostEnvironment Environment { get; }

        /// <summary>提交配置</summary>
        /// <param name="name"></param>
        /// <param name="options"></param>
        public void PostConfigure(String name, StaticFileOptions options)
        {
            name = name ?? throw new ArgumentException(nameof(name));
            options = options ?? throw new ArgumentException(nameof(options));

            // 如果没有被其他组件初始化，在这里初始化
            options.ContentTypeProvider = options.ContentTypeProvider ?? new FileExtensionContentTypeProvider();
            if (options.FileProvider == null && Environment.ContentRootFileProvider == null)
            {
                throw new InvalidOperationException("缺少FileProvider");
            }

            options.FileProvider = options.FileProvider ?? Environment.ContentRootFileProvider;

            // 添加我们的文件提供者
            // 第二个参数指定开始查找的文件夹，比如文件都放在wwwroot，就填“wwwroot”
            var filesProvider = new ManifestEmbeddedFileProvider(GetType().Assembly, Setting.Current.WebRootPath);
            options.FileProvider = new CompositeFileProvider(options.FileProvider, filesProvider);
        }
    }

    /// <summary>Ui扩展</summary>
    public static class DefaultUIServiceCollectionExtensions
    {
        /// <summary>添加魔方UI</summary>
        /// <param name="services"></param>
        /// <param name="env"></param>
        public static void AddCubeDefaultUI(this IServiceCollection services, IWebHostEnvironment env)
        {
            //services.ConfigureOptions<DefaultUIConfigureOptions>();
            var root = env.WebRootPath;
            if (!Directory.Exists(root.CombinePath("Content")))
            {
                root = Setting.Current.WebRootPath.GetFullPath();
                if (Directory.Exists(root))
                {
                    env.WebRootPath = root;
                    env.WebRootFileProvider = new PhysicalFileProvider(root);
                }
            }
        }
    }
}
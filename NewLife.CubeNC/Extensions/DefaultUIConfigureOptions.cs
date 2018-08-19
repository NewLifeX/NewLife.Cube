using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
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
        public DefaultUIConfigureOptions(IHostingEnvironment environment)
        {
            Environment = environment;
        }

        /// <summary>环境</summary>
        public IHostingEnvironment Environment { get; }

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
            var filesProvider = new ManifestEmbeddedFileProvider(GetType().Assembly, "wwwroot");
            options.FileProvider = new CompositeFileProvider(options.FileProvider, filesProvider);
        }
    }

    //public static class DefaultUIServiceCollectionExtensions
    //{
    //    public static void AddCubeDefaultUI(this IServiceCollection services)
    //    {
    //        services.ConfigureOptions<DefaultUIConfigureOptions>();
    //    }
    //}
}
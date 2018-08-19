using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace NewLife.CubeNC.Extensions
{
    internal class DefaultUIConfigureOptions : IPostConfigureOptions<StaticFileOptions>
    {
        public DefaultUIConfigureOptions(IHostingEnvironment environment)
        {
            Environment = environment;
        }

        public IHostingEnvironment Environment { get; }

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

            //添加我们的文件提供者
            var filesProvider = new ManifestEmbeddedFileProvider(GetType().Assembly, "wwwroot");// 第二个参数指定开始查找的文件夹，比如文件都放在wwwroot，就填“wwwroot”
            options.FileProvider = new CompositeFileProvider(options.FileProvider, filesProvider);
        }
    }

    public static class DefaultUIServiceCollectionExtensions
    {
        public static void AddCubeDefaultUI(this IServiceCollection services)
        {
            services.ConfigureOptions<DefaultUIConfigureOptions>();
        }
    }
}

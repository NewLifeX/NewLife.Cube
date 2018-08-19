using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewLife.CubeNC.Extensions;

namespace NewLife.CubeNC
{
    /// <summary>魔方初始化</summary>
    public class Startup
    {
        /// <summary>初始化配置</summary>
        /// <param name="configuration"></param>
        /// <param name="env"></param>
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            HostingEnvironment = env;
        }

        /// <summary>配置</summary>
        public IConfiguration Configuration { get; }

        /// <summary>主机环境</summary>
        public IHostingEnvironment HostingEnvironment { get; }

        /// <summary>添加服务到容器。运行时调用</summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services) => services.AddCubeDefaultServices(HostingEnvironment);

        /// <summary>配置Http请求管道。运行时调用</summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env) => app.UseCubeDefaultServices(env);
    }
}
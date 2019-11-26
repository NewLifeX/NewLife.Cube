1.添加nuget包源：https://www.myget.org/F/newlife-test/api/v3/index.json

2.新建一个 asp.net core mvc 项目，Startup.cs内容替换如下

    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            HostingEnvironment = env;
        }

        public IConfiguration Configuration { get; }
        public IHostingEnvironment HostingEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCubeDefaultServices(HostingEnvironment);
            // 静态资源读取方式注册
            services.AddCubeDefaultUI();
            // 添加魔方视图
            services.AddCustomApplicationParts(asmNaneList =>
            {
                asmNaneList.Add(typeof(Areas_Admin_Views_Index_Index).Assembly.FullName);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseCubeDefaultServices(env);
        }
    }
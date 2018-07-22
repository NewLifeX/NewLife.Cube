1.添加nuget包源：https://www.myget.org/F/newlife-test/api/v3/index.json

2.新建一个 asp.net core mvc 项目，Startup.cs内容替换如下

public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddCubeDefaultServices();

            services.AddCustomApplicationParts(asmNaneList => { asmNaneList.Add(typeof(Areas_Admin_Views_Index_Index).Assembly.FullName); });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseCubeDefaultServices(env);
        }
    }

3.生成一次，在输出目录（比如bin/debug/netcoreapp2.1/wwwroot），将里面的内容复制到项目目录的wwwroot，里面都是页面资源
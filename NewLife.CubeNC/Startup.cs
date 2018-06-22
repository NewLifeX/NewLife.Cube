using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NewLife.Cube.Controllers;
using NewLife.CubeNC.Com;
using NewLife.CubeNC.ViewsPreComplied;
using NewLife.CubeNC.WebMiddleware;
using NewLife.Log;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.CubeNC
{
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
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => false;//此项为true需要用户授权才能记录cookie
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddTransient<IConfigureOptions<MvcViewOptions>, RazorViewOPtionsSetup>();
            services.AddSingleton<RazorViewEngine, CompositePrecompiledMvcEngine>();

            services.AddMvc(opt =>
                {
                    //模型绑定
                    opt.ModelBinderProviders.Insert(0,new EntityModelBinderProvider());
                    //过滤器
                    opt.Filters.Add(new MvcHandleErrorAttribute());

                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                //视图文件查找选项设置
                .AddRazorOptions(opt =>
                {
                    opt.ViewLocationFormats.Clear();
                    opt.AreaViewLocationFormats.Clear();
                    opt.ViewLocationFormats.Add("~/Views/{1}/{0}.cshtml");
                    opt.ViewLocationFormats.Add("~/Views/Shared/{0}.cshtml");
                    opt.AreaViewLocationFormats.Add("~/Areas/{2}/Views/{1}/{0}.cshtml");
                    opt.AreaViewLocationFormats.Add("~/Areas/{2}/Views/Shared/{0}.cshtml");
                })
                .AddViewOptions(opt =>
                {
                    //opt.ViewEngines.Clear();
                    //var item = services.
                    //opt.ViewEngines.Add(new CompositePrecompiledMvcEngine());
                })
                ;

            //添加Http上下文访问器
            StaticHttpContextExtensions.AddHttpContextAccessor(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

          

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                //routes.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
                //routes.IgnoreRoute("Content/{*relpath}");
                //routes.IgnoreRoute("Scripts/{*relpath}");
                //routes.IgnoreRoute("Images/{*relpath}");

                //区域路由注册
                routes.MapRoute(
                    name: "areas",
                    template: "{area=Admin}/{controller=Index}/{action=Index}/{id?}"
                );

                //if (routes.MapGet.Routes.FirstOrDefault(f=>f.)["Cube"] == null)
                {
                    // 为魔方注册默认首页，启动魔方站点时能自动跳入后台，同时为Home预留默认过度视图页面
                    routes.MapRoute(
                        name: "Cube",
                        template: "{controller=CubeHome}/{action=Index}/{id?}"
                    );
                }




                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            //注册http模块中间件
            app.UseErrorModuleMiddleware();

            //配置静态Http上下文访问器
            app.UseStaticHttpContext();

            // 自动检查并添加菜单
            XTrace.WriteLine("初始化权限管理体系");
            //var user = ManageProvider.User;
            ManageProvider.Provider.GetService<IUser>();
        }
    }
}

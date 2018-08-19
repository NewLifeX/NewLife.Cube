using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Cube;
using NewLife.CubeNC.Com;
using NewLife.CubeNC.Membership;
using NewLife.CubeNC.WebMiddleware;
using NewLife.Web;

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

        #region ConfigureServices
        /// <summary>添加服务到容器。运行时调用</summary>
        /// <param name="services"></param>
        public virtual void ConfigureServices(IServiceCollection services)
        {
            var env = HostingEnvironment;
            // 配置Cookie策略
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => false;//此项为true需要用户授权才能记录cookie
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            // 添加Session会话支持
            services.AddSession();

            // 添加标识用户支持
            services.AddDefaultIdentity<IdentityUser>();

            //services.AddSingleton<IRazorViewEngine, CompositePrecompiledMvcEngine>();
            //services.AddSingleton<IView, PrecompiledMvcView>();

            services.AddMvc(opt =>
            {
                // 模型绑定
                opt.ModelBinderProviders.Insert(0, new EntityModelBinderProvider());
                // 过滤器
                opt.Filters.Add<MvcHandleErrorAttribute>();

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
                    opt.AreaViewLocationFormats.Add("~/Views/{1}/{0}.cshtml");
                    opt.AreaViewLocationFormats.Add("~/Views/Shared/{0}.cshtml");
                })
                .AddViewOptions(opt =>
                {
                    //opt.ViewEngines.Clear();
                    //var item = services.
                    //opt.ViewEngines.Add(new CompositePrecompiledMvcEngine());
                })
                ;

            AddCustomApplicationParts(services);

            //services.AddSingleton<IRazorViewEngine, CompositePrecompiledMvcEngine>();
            //services.AddTransient<IConfigureOptions<MvcViewOptions>, RazorViewOPtionsSetup>();

            // 注册Cookie认证服务
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();

            // 添加魔方模块
            // 添加管理提供者
            services.AddManageProvider();
            // 添加Http上下文访问器
            StaticHttpContextExtensions.AddHttpContextAccessor(services);

            //if (env.IsDevelopment())
            //{

            //}
            //else
            //{
            //    services.AddCubeDefaultUI();
            //}
        }

        /// <summary>添加自定义应用部分，即添加外部引用的控制器、视图的Assemly，作为本应用的一部分</summary>
        /// <param name="services"></param>
        /// <param name="addEntryAssemblyName"></param>
        public void AddCustomApplicationParts(IServiceCollection services, Action<List<String>> addEntryAssemblyName = null)
        {
            var manager = GetServiceFromCollection<ApplicationPartManager>(services) ?? new ApplicationPartManager();

            //var entryAssemblyName = ;//"NewLife.Cube";
            var list = new List<String>(4) { typeof(Program).Assembly.FullName };

            addEntryAssemblyName?.Invoke(list);

            foreach (var entryAssemblyName in list)
            {
                PopulateCustomParts(manager, entryAssemblyName);
            }
        }

        private T GetServiceFromCollection<T>(IServiceCollection services)
        {
            return (T)services
                .LastOrDefault(d => d.ServiceType == typeof(T))
                ?.ImplementationInstance;
        }

        private void PopulateCustomParts(ApplicationPartManager manager, String entryAssemblyName)
        {
            var entryAssembly = Assembly.Load(new AssemblyName(entryAssemblyName));
            //var assembliesProvider = new ApplicationAssembliesProvider();
            //var applicationAssemblies = assembliesProvider.ResolveAssemblies(entryAssembly);//获取对应视图assembly

            //foreach (var assembly in applicationAssemblies)
            //{
            var assembly = entryAssembly;
            var partFactory = ApplicationPartFactory.GetApplicationPartFactory(assembly);
            foreach (var part in partFactory.GetApplicationParts(assembly))
            {
                if (!manager.ApplicationParts.Contains(part)) manager.ApplicationParts.Add(part);
            }
            //}
        }
        #endregion

        #region Configure
        /// <summary>配置Http请求管道。运行时调用</summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public virtual void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                //app.UseExceptionHandler("/Home/Error");
                //app.UseHsts();
            }

            // 添加自定义中间件
            // 注册错误处理模块中间件
            app.UseErrorModule();
            // 注册请求执行时间中间件
            app.UseDbRunTimeModule();

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseSession();
            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                {
                    // 为魔方注册默认首页，启动魔方站点时能自动跳入后台，同时为Home预留默认过度视图页面
                    routes.MapRoute(
                        name: "Cube",
                        template: "{controller=CubeHome}/{action=Index}/{id?}"
                    );
                }
            });

            // 配置魔方的MVC选项
            app.UseRouter(routes =>
            {
                if (routes.DefaultHandler == null)
                {
                    routes.DefaultHandler = app.ApplicationServices.GetRequiredService<MvcRouteHandler>();
                }
                //区域路由注册
                routes.MapRoute(
                    name: "CubeAreas",
                    template: "{area=Admin}/{controller=Index}/{action=Index}/{id?}"
                );
            });

            // 配置静态Http上下文访问器
            app.UseStaticHttpContext();
            // 使用管理提供者
            app.UseManagerProvider();

            // 自动检查并添加菜单
            //XTrace.WriteLine("初始化权限管理体系");
            //var user = ManageProvider.User;
            //ManageProvider.Provider.GetService<IUser>();
            //ScanControllerExtensions.ScanController();
        }
        #endregion
    }
}
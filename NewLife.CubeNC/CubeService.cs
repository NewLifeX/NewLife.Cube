using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.WebEncoders;
using NewLife.Common;
using NewLife.Cube.Common;
using NewLife.Cube.Extensions;
using NewLife.Cube.WebMiddleware;
using NewLife.Reflection;
using NewLife.Web;

namespace NewLife.Cube
{
    /// <summary>魔方服务</summary>
    public static class CubeService
    {
        #region 配置魔方
        /// <summary>添加魔方</summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddCube(this IServiceCollection services)
        {
            // 修正系统名，确保可运行
            var set = SysConfig.Current;
            if (set.IsNew || set.Name == "NewLife.Cube.Views")
            {
                set.Name = "NewLife.Cube";
                set.Save();
            }

            // 配置Cookie策略
            services.Configure<CookiePolicyOptions>(options =>
            {
                // 此项为true需要用户授权才能记录cookie
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            // 添加Session会话支持
            services.AddSession();

            //// 添加标识用户支持
            //services.AddDefaultIdentity<IdentityUser>();

            //services.AddSingleton<IRazorViewEngine, CompositePrecompiledMvcEngine>();
            //services.AddSingleton<IView, PrecompiledMvcView>();

            var provider = services.BuildServiceProvider();
            //var env = provider.GetService<IHostingEnvironment>();
            var env = provider.GetService(typeof(IHostingEnvironment)) as IHostingEnvironment;
            if (env != null) services.AddCubeDefaultUI(env);

            services.AddMvc(opt =>
            {
                // 分页器绑定
                opt.ModelBinderProviders.Insert(0, new PagerModelBinderProvider());

                // 模型绑定
                opt.ModelBinderProviders.Insert(0, new EntityModelBinderProvider());

                // 过滤器
                //opt.Filters.Add<MvcHandleErrorAttribute>();

            })
            // 添加版本兼容性，显示声明当前应用版本为2.1
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
            //// 视图文件查找选项设置
            //.AddRazorOptions(opt =>
            //{
            //    opt.ViewLocationFormats.Clear();
            //    opt.AreaViewLocationFormats.Clear();
            //    opt.ViewLocationFormats.Add("~/Views/{1}/{0}.cshtml");
            //    opt.ViewLocationFormats.Add("~/Views/Shared/{0}.cshtml");
            //    opt.AreaViewLocationFormats.Add("~/Areas/{2}/Views/{1}/{0}.cshtml");
            //    opt.AreaViewLocationFormats.Add("~/Areas/{2}/Views/Shared/{0}.cshtml");
            //    opt.AreaViewLocationFormats.Add("~/Views/{1}/{0}.cshtml");
            //    opt.AreaViewLocationFormats.Add("~/Views/Shared/{0}.cshtml");
            //})
            .AddViewOptions(opt =>
            {
                //opt.ViewEngines.Clear();
                //var item = services.
                //opt.ViewEngines.Add(new CompositePrecompiledMvcEngine());
            });

            services.AddCustomApplicationParts();

            //services.AddSingleton<IRazorViewEngine, CompositePrecompiledMvcEngine>();
            //services.AddTransient<IConfigureOptions<MvcViewOptions>, RazorViewOPtionsSetup>();

            //// 注册Cookie认证服务
            //services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();

            // 添加魔方模块
            // 添加管理提供者
            services.AddManageProvider();

            //// 添加Http上下文访问器
            //services.AddHttpContextAccessor();

            //services.AddCubeDefaultUI(HostingEnvironment);

            // 添加压缩
            services.AddResponseCompression();

            // 防止汉字被自动编码
            services.Configure<WebEncoderOptions>(options =>
            {
                options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
            });

            //// 添加OData
            //services.AddOData();

            //services.AddSingleton<ILogger, Logger>();
            services.AddLogging(configure =>
            {
                configure.AddFilter((str1, str2, level) =>
                {
                    return true;
                })
                    .AddConsole(cfg =>
                    {
                        cfg.DisableColors = true;
                    })
                    .AddProvider(new CubeLoggerProvider());
            });

            return services;
        }



        /// <summary>添加自定义应用部分，即添加外部引用的控制器、视图的Assemly，作为本应用的一部分</summary>
        /// <param name="services"></param>
        public static void AddCustomApplicationParts(this IServiceCollection services)
        {
            var manager = services.LastOrDefault(e => e.ServiceType == typeof(ApplicationPartManager))?.ImplementationInstance as ApplicationPartManager;
            if (manager == null) manager = new ApplicationPartManager();

            var list = FindAllArea();

            foreach (var asm in list)
            {
                var factory = ApplicationPartFactory.GetApplicationPartFactory(asm);
                foreach (var part in factory.GetApplicationParts(asm))
                {
                    if (!manager.ApplicationParts.Contains(part)) manager.ApplicationParts.Add(part);
                }
            }
        }

        /// <summary>遍历所有引用了AreaRegistrationBase的程序集</summary>
        /// <returns></returns>
        static List<Assembly> FindAllArea()
        {
            var list = new List<Assembly>();
            var cs = typeof(ControllerBaseX).GetAllSubclasses(true).ToArray();
            foreach (var item in cs)
            {
                var asm = item.Assembly;
                if (!list.Contains(asm))
                {
                    list.Add(asm);
                }
            }
            cs = typeof(RazorPage).GetAllSubclasses(true).ToArray();
            foreach (var item in cs)
            {
                var asm = item.Assembly;
                if (!list.Contains(asm))
                {
                    list.Add(asm);
                }
            }

            // 为了能够实现模板覆盖，程序集相互引用需要排序，父程序集在前
            list.Sort((x, y) =>
            {
                if (x == y) return 0;
                if (x != null && y == null) return 1;
                if (x == null && y != null) return -1;

                //return x.GetReferencedAssemblies().Any(e => e.FullName == y.FullName) ? 1 : -1;
                // 对程序集引用进行排序时，不能使用全名，当魔方更新而APP没有重新编译时，版本的不同将会导致全名不同，无法准确进行排序
                var yname = y.GetName().Name;
                return x.GetReferencedAssemblies().Any(e => e.Name == yname) ? 1 : -1;
            });

            return list;
        }
        #endregion

        #region 使用魔方
        /// <summary>使用魔方</summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseCube(this IApplicationBuilder app)
        {
            //var loggerFactory = app.ApplicationServices.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
            //loggerFactory.CreateLogger("");

            // 配置静态Http上下文访问器
            app.UseStaticHttpContext();

            var set = Setting.Current;

            // 添加自定义中间件
            // 注册错误处理模块中间件
            app.UseErrorModule();

            // 压缩配置
            if (set.EnableCompress) app.UseResponseCompression();

            // 注册请求执行时间中间件
            app.UseDbRunTimeModule();

            if (set.SslMode > SslModes.Disable) app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseSession();
            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                //var builder = new ODataConventionModelBuilder();
                //builder.EntitySet<UserX>("UserXs");
                //// OData路由放在最前面
                //routes.MapODataServiceRoute("ODataRoute","OData", builder.GetEdmModel());

                // 区域路由注册
                routes.MapRoute(
                    name: "CubeAreas",
                    template: "{area=Admin}/{controller=Index}/{action=Index}/{id?}"
                );

                // 为魔方注册默认首页，启动魔方站点时能自动跳入后台，同时为Home预留默认过度视图页面
                routes.MapRoute(
                    name: "Cube",
                    template: "{controller=CubeHome}/{action=Index}/{id?}"
                );
            });

            // 使用管理提供者
            app.UseManagerProvider();

            // 自动检查并添加菜单
            //XTrace.WriteLine("初始化权限管理体系");
            //var user = ManageProvider.User;
            //ManageProvider.Provider.GetService<IUser>();
            //ScanControllerExtensions.ScanController();
            AreaBaseX.RegisterArea<Admin.AdminArea>();

            return app;
        }
        #endregion
    }
}
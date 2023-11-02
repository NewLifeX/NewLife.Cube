using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using NewLife;
using NewLife.Caching.Services;
using NewLife.Caching;
using NewLife.Cube;
using NewLife.Cube.AdminLTE;
using NewLife.Cube.Extensions;
using NewLife.Cube.WebMiddleware;
using CubeSetting = NewLife.Cube.CubeSetting;
using NewLife.Cube.Services;
using NewLife.Redis.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Log;
using Microsoft.AspNetCore.DataProtection;

namespace CubeDemoNC;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration) => Configuration = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        // 引入星尘，设置监控中间件
        var star = services.AddStardust(null);
        TracerMiddleware.Tracer = star?.Tracer;

        // 分布式服务，使用配置中心RedisCache配置
        services.AddSingleton<ICacheProvider, RedisCacheProvider>();

        var config = star.GetConfig();
        var cacheConn = config["RedisCache"];
        Redis redis = null;
        if (!cacheConn.IsNullOrEmpty())
        {
            redis = new FullRedis { Log = XTrace.Log, Tracer = star.Tracer };
            redis.Init(cacheConn);
            services.AddSingleton(redis);
        }

        // 启用接口响应压缩
        services.AddResponseCompression();

        services.AddControllersWithViews();
        services.AddCube();
        //services.AddBlazor();

        if (redis != null)
        {
            services.AddDataProtection().PersistKeysToRedis(redis);
        }
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var set = CubeSetting.Current;
        if (env.IsDevelopment() || set.Debug)
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            //app.UseExceptionHandler("/CubeHome/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        //app.UseHttpsRedirection();
        app.UseStaticFiles();

        /* 新ui通道，以下需同时满足条件
         * 1、配置启用新ui
         * 2、GET请求
         * 3、非json请求
         * 4、不带query参数。如果真的是新ui的页面请求，且带了query参数，不能与魔方旧有接口一样，否则不会命中
         * */
        if (set.EnableNewUI)
            app.UseWhen(
            context => set.EnableNewUI && context.Request.Method.EqualIgnoreCase("GET") &&
                       !context.Request.IsAjaxRequest() &&
                       context.Request.Query.Count < 1,
            a =>
            {
                var staticFileOptions = new StaticFileOptions()
                {
                    FileProvider = new PhysicalFileProvider(env.WebRootPath),
                    OnPrepareResponse = (context =>
                    {
                        if (!context.Context.Response.Headers[HeaderNames.ContentType].Contains("text/html"))
                            return;
                        context.Context.Response.Headers.Remove(HeaderNames.ETag);
                        context.Context.Response.Headers.Remove(HeaderNames.LastModified);
                    })
                };

                a.UseDefaultFiles();

                a.UseSpa(options =>
                {
                    a.UseSpaStaticFiles(staticFileOptions);
                    options.Options.DefaultPageStaticFileOptions = staticFileOptions;
                });
            });

        //app.UseRouting();

        //app.UseMiddleware<TracerMiddleware>();

        app.UseCube(env);
        app.UseAdminLTE(env);
        //app.UseTabler(env);
        //app.UseMetronic(env);
        //app.UseElementUI(env);
        //app.UseMetronic8(env);
        //app.UseLayuiAdmin(env);
        //app.UseBlazor(env);

        app.UseAuthorization();

        app.UseResponseCompression();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        });

        //// 探测星尘
        //ThreadPoolX.QueueUserWorkItem(() =>
        //{
        //    var client = new LocalStarClient();
        //    client.ProbeAndInstall(null, "1.1");
        //});

        // 所有请求没有命中的，统一在这里处理
        if (set.EnableNewUI)
            app.UseWhen(context => set.EnableNewUI,
            a =>
            {
                var staticFileOptions = new StaticFileOptions()
                {
                    FileProvider = new PhysicalFileProvider(env.WebRootPath),
                    OnPrepareResponse = (context =>
                    {
                        if (!context.Context.Response.Headers[HeaderNames.ContentType].Contains("text/html"))
                            return;
                        context.Context.Response.Headers.Remove(HeaderNames.ETag);
                        context.Context.Response.Headers.Remove(HeaderNames.LastModified);
                    })
                };

                a.UseDefaultFiles();

                a.UseSpa(options =>
                {
                    a.UseSpaStaticFiles(staticFileOptions);
                    options.Options.DefaultPageStaticFileOptions = staticFileOptions;
                });
            });
    }
}
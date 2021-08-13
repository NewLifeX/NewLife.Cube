using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using NewLife;
using NewLife.Cube;
using NewLife.Cube.Extensions;
using NewLife.Cube.WebMiddleware;
using NewLife.Log;
using Stardust.Monitors;
using Setting = NewLife.Cube.Setting;

namespace CubeDemoNC
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration) => Configuration = configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            // APM跟踪器
            var tracer = new StarTracer("http://star.newlifex.com:6600") { Log = XTrace.Log };
            DefaultTracer.Instance = tracer;
            //ApiHelper.Tracer = tracer;
            //DAL.GlobalTracer = tracer;
            //OAuthClient.Tracer = tracer;
            TracerMiddleware.Tracer = tracer;

            services.AddSingleton<ITracer>(tracer);

            services.AddControllersWithViews();
            services.AddCube();

            //services.AddHttpContextAccessor();
            // Blazor Server方式渲染
            //services.AddBootstrapBlazor();
            services.AddRazorPages();
            services.AddServerSideBlazor().AddCircuitOptions(options => { options.DetailedErrors = true; });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var set = Setting.Current;
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

#if !NET60
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
#endif

            //app.UseRouting();

            //app.UseMiddleware<TracerMiddleware>();

            app.UseCube(env);
            app.UseAdminLTE(env);
            app.UseTabler(env);
            app.UseMetronic(env);
            app.UseBlazor(env);

            app.UseAuthorization();

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

#if !NET60
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
#endif
        }
    }
}
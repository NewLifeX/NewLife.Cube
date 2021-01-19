using System;
using System.Collections.Generic;
using System.IO;
using IGeekFan.AspNetCore.Knife4jUI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using NewLife;
using NewLife.Cube;
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

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Swagger", Version = "v1" });
                c.CustomOperationIds(apiDesc =>
                {
                    var controllerAction = apiDesc.ActionDescriptor as ControllerActionDescriptor;
                    return controllerAction.ControllerName + "-" + controllerAction.ActionName;
                });
                c.CustomSchemaIds(t => t.FullName);

                var basePath = AppContext.BaseDirectory;
                var files = Directory.GetFiles(basePath, "*.xml");

                foreach (var file in files)
                {
                    c.IncludeXmlComments(file, true);
                }

                // 添加控制器注释
                c.TagActionsBy(api =>
                {
                    var controllerActionDescriptor = (ControllerActionDescriptor)api.ActionDescriptor;
                    var displayName = controllerActionDescriptor.ControllerTypeInfo.GetDisplayName();
                    displayName = displayName.IsNullOrWhiteSpace() ? "" : "-" + displayName;
                    var tag = controllerActionDescriptor.ControllerName + displayName;
                    return new List<string>()
                    {
                        tag
                    };
                });

            });
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

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Swagger v1"));
            app.UseKnife4UI(c =>
            {
                c.RoutePrefix = "";
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            });


            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            //app.UseRouting();

            //app.UseMiddleware<TracerMiddleware>();

            app.UseCube(env);

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

            });
        }
    }
}
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewLife.Cube;
using NewLife.Cube.WebMiddleware;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Web;
using Stardust.Monitors;
using XCode.DataAccessLayer;

namespace CubeDemoNC
{
    public class Startup
    {
        public Startup() { }

        public void ConfigureServices(IServiceCollection services)
        {
            // APM跟踪器
            var tracer = new StarTracer("http://star.newlifex.com:6600") { Log = XTrace.Log };
            DefaultTracer.Instance = tracer;
            ApiHelper.Tracer = tracer;
            DAL.GlobalTracer = tracer;
            OAuthClient.Tracer = tracer;

            services.AddSingleton<ITracer>(tracer);

            services.AddCube();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // 使用Cube前添加自己的管道
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/CubeHome/Error");

            //// 启用https
            //app.UseHttpsRedirection();

            app.UseMiddleware<TracerMiddleware>();

            app.UseStaticFiles();
            
            app.UseCube(env);
        }
    }
}
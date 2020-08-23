using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
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
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // APM跟踪器
            var tracer = new StarTracer("http://star.newlifex.com:6600") { Log = XTrace.Log };
            DefaultTracer.Instance = tracer;
            //ApiHelper.Tracer = tracer;
            //DAL.GlobalTracer = tracer;
            OAuthClient.Tracer = tracer;
            TracerMiddleware.Tracer = tracer;

            services.AddSingleton<ITracer>(tracer);

            services.AddCube();
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

            app.UseRouting();

            app.UseAuthorization();

            app.UseMiddleware<TracerMiddleware>();

            app.UseCube(env);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
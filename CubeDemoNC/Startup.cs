using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewLife.Cube;

namespace CubeDemoNC
{
    public class Startup
    {
        public Startup() { }

        public void ConfigureServices(IServiceCollection services) => services.AddCube();

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // 使用Cube前添加自己的管道
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/CubeHome/Error");

            //// 启用https
            //app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseCube(env);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    "Default",
                    "{controller=CubeHome}/{action=Index}/{id?}"
                    );
                endpoints.MapRazorPages();
            })
            .Build();
        }
    }
}
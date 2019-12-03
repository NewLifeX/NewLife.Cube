using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewLife.Cube;

namespace CubeDemoNC
{
    public class Startup
    {
        public Startup()
        {

        }

        public void ConfigureServices(IServiceCollection services)
        {

            services.AddCube();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // 使用Cube前添加自己的管道
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/Home/Error");
            // 启用https
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseCube();
        }
    }
}
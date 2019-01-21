using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Cube;

namespace CubeDemoNC
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCube();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseCube();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
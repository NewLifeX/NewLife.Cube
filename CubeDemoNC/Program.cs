using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NewLife.Log;
using NewLife.Cube;

namespace CubeDemoNC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            XTrace.UseConsole();

            //CreateWebHostBuilder(args).Build().Run();
            CreateHostBuilder(args).Build().Run();
        }

        //public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        //    WebHost.CreateDefaultBuilder(args)
        //        .UseStartup<Startup>();

        public static IHostBuilder CreateHostBuilder(String[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging => { logging.AddXLog(); })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://*:5000;https://*:5001");
                    webBuilder.UseStartup<Startup>();
                });
        }
    }
}

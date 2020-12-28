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
using NewLife;
using NewLife.Log;
using NewLife.Cube;
using NewLife.Serialization;
using XCode.Cache;

namespace CubeDemoNC
{
    public class Program
    {
        public static void Main(string[] args)
        { 
            Environment.SetEnvironmentVariable("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1");

            XTrace.UseConsole();

            CacheBase.Debug = true;
            //CreateWebHostBuilder(args).Build().Run();  
            var app = ApplicationManager.Load();

            do
            {
                app.Start(CreateHostBuilder(args).Build());
            } while ( app.Restarting);
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
                    //webBuilder.UseUrls("http://*:5000;https://*:5001");
                    webBuilder.UseStartup<Startup>();
                });
        }
    }
}

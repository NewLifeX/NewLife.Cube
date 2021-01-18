using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using NewLife.Log;

namespace NewLife.Cube
{
    public class Program
    {
        public static void Main(string[] args)
        { 
            Environment.SetEnvironmentVariable("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1");

            XTrace.UseConsole();

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

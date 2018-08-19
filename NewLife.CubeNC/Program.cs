using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace NewLife.Cube
{
    /// <summary>应用程序</summary>
    public class Program
    {
        /// <summary>入口主函数</summary>
        /// <param name="args"></param>
        public static void Main(String[] args) => CreateWebHostBuilder(args).Build().Run();

        static IWebHostBuilder CreateWebHostBuilder(String[] args) => WebHost.CreateDefaultBuilder(args).UseStartup<Startup>();
    }
}
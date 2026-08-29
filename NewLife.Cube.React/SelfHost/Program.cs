using NewLife.Log;

namespace NewLife.Cube.React;

/// <summary>React前端皮肤自托管调试入口</summary>
/// <remarks>
/// Debug 构建时项目输出为可执行程序，直接 <c>dotnet run</c> 或 VS F5 即可独立运行本皮肤调试，
/// 不依赖 CubeDemo 宿主；Release 构建输出类库用于 NuGet 打包，本文件不参与编译。
/// </remarks>
public static class Program
{
    /// <summary>自托管调试入口</summary>
    /// <param name="args">命令行参数</param>
    public static void Main(String[] args)
    {
        XTrace.UseConsole();

        var builder = WebApplication.CreateBuilder(args);
        var services = builder.Services;

        // 引入星尘，注册 ILog/ITracer/IConfig 等基础设施，AddCube 内部服务依赖它们
        services.AddStardust(null);

        services.AddControllers()
            // 显式注册魔方核心程序集为应用部件，否则 base SDK 项目默认只发现入口程序集的控制器
            .AddApplicationPart(typeof(CubeService).Assembly);
        services.AddCube();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseCube(builder.Environment);

        app.UseAuthorization();

        app.MapControllerRoute(name: "default", pattern: "{controller=Index}/{action=Index}/{id?}");
        app.MapControllers();

        // UseReact 必须在 MapControllers 之后，确保 API endpoint 优先匹配，SPA 回退兜底
        app.UseReact(builder.Environment);

        // 启动完成后自动打开浏览器（仅开发环境）
        // 本工程 Debug 输出控制台 Exe（非 Web SDK），VS 的 launchBrowser 不生效，改由应用自启浏览器
        // watch 重启时 DOTNET_WATCH_ITERATION 递增（首次=1），只在首次启动时打开，避免每次保存弹新标签页
        // 设置 DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER=1 可关闭（E2E/CI 等场景）
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            if (!builder.Environment.IsDevelopment()) return;
            if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER"))) return;
            var iteration = Environment.GetEnvironmentVariable("DOTNET_WATCH_ITERATION");
            if (!iteration.IsNullOrEmpty() && iteration != "1") return;

            "http://localhost:7081".ShellExecute();
        });

        app.Run();
    }
}

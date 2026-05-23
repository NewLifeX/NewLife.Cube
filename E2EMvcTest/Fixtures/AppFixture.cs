using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Fixtures;

/// <summary>全局 E2E 测试夹具，负责启动 CubeSSO 进程并初始化 Playwright</summary>
public sealed class AppFixture : IAsyncLifetime
{
    #region 属性

    /// <summary>本次运行目录，由环境变量 E2E_RUN_DIR 注入，脚本负责创建</summary>
    public static String RunDir { get; } =
        Environment.GetEnvironmentVariable("E2E_RUN_DIR")
        ?? System.IO.Path.GetFullPath(System.IO.Path.Combine(
            AppContext.BaseDirectory, "..", "..", "Bin", "E2ETest",
            "Run-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")));

    /// <summary>CubeSSO 发布目录，由环境变量 E2E_APP_DIR 注入（= RunDir\app）</summary>
    public static String AppDir { get; } =
        Environment.GetEnvironmentVariable("E2E_APP_DIR")
        ?? System.IO.Path.Combine(RunDir, "app");

    /// <summary>数据目录（SQLite 文件位于此处）</summary>
    public static String DataDir => System.IO.Path.Combine(AppDir, "Data");

    /// <summary>截图输出目录</summary>
    public static String ScreenshotDir => System.IO.Path.Combine(RunDir, "screenshots");

    /// <summary>基础 URL，默认 http://localhost:8080</summary>
    public static String BaseUrl { get; } =
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:8080";

    /// <summary>管理员用户名</summary>
    public static String AdminUser { get; } =
        Environment.GetEnvironmentVariable("E2E_ADMIN_USER") ?? "admin";

    /// <summary>管理员密码</summary>
    public static String AdminPass { get; } =
        Environment.GetEnvironmentVariable("E2E_ADMIN_PASS") ?? "Admin@2025";

    /// <summary>OAuth 测试账号用户名</summary>
    public static String OAuthUser { get; } =
        Environment.GetEnvironmentVariable("E2E_OAUTH_USER") ?? "test";

    /// <summary>OAuth 测试账号密码</summary>
    public static String OAuthPass { get; } =
        Environment.GetEnvironmentVariable("E2E_OAUTH_PASS") ?? "test";

    private Process? _process;
    private IPlaywright? _playwright;

    /// <summary>Playwright 浏览器实例（Chromium）</summary>
    public IBrowser Browser { get; private set; } = null!;

    #endregion

    #region 生命周期

    /// <summary>初始化：确保输出目录存在，若进程未运行则启动 CubeSSO，等待端口就绪，初始化 Playwright</summary>
    public async Task InitializeAsync()
    {
        EnsureDirectories();

        var alreadyRunning = await IsServerReadyAsync(TimeSpan.Zero);
        if (!alreadyRunning)
        {
            _process = StartCubeSSO();
            await WaitForServerReadyAsync(TimeSpan.FromSeconds(30));
        }

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !String.Equals(
                Environment.GetEnvironmentVariable("E2E_HEADLESS"), "false",
                StringComparison.OrdinalIgnoreCase),
        });
    }

    /// <summary>清理：关闭浏览器，停止测试启动的 CubeSSO 进程</summary>
    public async Task DisposeAsync()
    {
        if (Browser != null) await Browser.DisposeAsync();
        _playwright?.Dispose();

        if (_process != null && !_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
            _process.Dispose();
        }
    }

    #endregion

    #region 辅助

    private static void EnsureDirectories()
    {
        System.IO.Directory.CreateDirectory(RunDir);
        System.IO.Directory.CreateDirectory(AppDir);
        System.IO.Directory.CreateDirectory(DataDir);
        System.IO.Directory.CreateDirectory(ScreenshotDir);
    }

    private static Process StartCubeSSO()
    {
        // 优先运行编排脚本已发布的程序；回退到源码目录 dotnet run（直接调试时使用）
        var dllPath = System.IO.Path.Combine(AppDir, "CubeSSO.dll");
        String procFile;
        String procArgs;
        String workDir;

        if (System.IO.File.Exists(dllPath))
        {
            procFile = "dotnet";
            procArgs = $"\"{dllPath}\"";
            workDir  = AppDir;
        }
        else
        {
            // 回退：从 Bin/E2ETest 上两级得到 repo root，再找 CubeSSO 源码目录
            var repoRoot = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(AppContext.BaseDirectory, "..", ".."));
            var cubeSsoDir = System.IO.Path.Combine(repoRoot, "CubeSSO");
            procFile = "dotnet";
            procArgs = $"run --project \"{cubeSsoDir}\"";
            workDir  = cubeSsoDir;
        }

        var psi = new ProcessStartInfo(procFile, procArgs)
        {
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            WorkingDirectory       = workDir,
        };

        // 覆盖数据库路径指向本次运行目录
        psi.Environment["ConnectionStrings__Membership"] =
            $"Data Source={DataDir}\\Membership.db;provider=sqlite";
        psi.Environment["ConnectionStrings__Cube"] =
            $"Data Source={DataDir}\\Cube.db;provider=sqlite";
        psi.Environment["ConnectionStrings__Log"] =
            $"Data Source={DataDir}\\Log.db;provider=sqlite";
        psi.Environment["URLS"] = BaseUrl;

        var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        proc.Start();
        return proc;
    }

    private static async Task WaitForServerReadyAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await IsServerReadyAsync(TimeSpan.FromSeconds(2)))
                return;

            await Task.Delay(1000);
        }

        throw new TimeoutException(
            $"CubeSSO 未在 {timeout.TotalSeconds} 秒内就绪，请检查应用是否正常启动。BaseUrl={BaseUrl}");
    }

    private static async Task<Boolean> IsServerReadyAsync(TimeSpan timeout)
    {
        try
        {
            using var client = new HttpClient { Timeout = timeout == TimeSpan.Zero ? TimeSpan.FromMilliseconds(500) : timeout };
            var response = await client.GetAsync(BaseUrl);
            return (Int32)response.StatusCode < 500;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}

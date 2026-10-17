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
    /// <remarks>CubeSSO appsettings.json 使用 ..\\Data\\ 相对路径，DB 实际在 RunDir\Data 而非 AppDir\Data</remarks>
    public static String DataDir => System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDir, "..", "Data"));

    /// <summary>截图输出目录</summary>
    public static String ScreenshotDir => System.IO.Path.Combine(RunDir, "screenshots");

    /// <summary>基础 URL，默认 http://localhost:8080</summary>
    public static String BaseUrl { get; } =
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:8080";

    /// <summary>管理员用户名</summary>
    public static String AdminUser { get; } =
        Environment.GetEnvironmentVariable("E2E_ADMIN_USER") ?? "admin";

    /// <summary>管理员密码（CubeSSO 默认初始密码为 admin，见登录页提示"默认用户admin/admin"）</summary>
    public static String AdminPass { get; } =
        Environment.GetEnvironmentVariable("E2E_ADMIN_PASS") ?? "admin";

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

        // 预热登录页：首次请求会触发模型建表/菜单权限扫描，慢速 CI 上可能超过测试等待时间
        await WarmUpLoginPageAsync(TimeSpan.FromSeconds(180));

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
        // 优先使用构建输出（Bin/SSO），复制到本次运行目录后启动：
        // appsettings.json 使用 ..\Data 相对路径，只有把工作目录定在 AppDir，数据库才会落到 RunDir\Data
        var repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", ".."));
        var buildDir = System.IO.Path.Combine(repoRoot, "Bin", "SSO");
        var dllPath = System.IO.Path.Combine(AppDir, "CubeSSO.dll");

        if (System.IO.File.Exists(System.IO.Path.Combine(buildDir, "CubeSSO.dll")))
        {
            CopyDirectory(buildDir, AppDir);
            RewriteConnectionStrings(System.IO.Path.Combine(AppDir, "appsettings.json"));
        }

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
            // 回退：从源码目录 dotnet run（直接调试时使用）
            var cubeSsoDir = System.IO.Path.Combine(repoRoot, "CubeSSO");
            procFile = "dotnet";
            procArgs = $"run --project \"{cubeSsoDir}\"";
            workDir  = AppDir;
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
            $"Data Source={System.IO.Path.Combine(DataDir, "Membership.db")};provider=sqlite";
        psi.Environment["ConnectionStrings__Cube"] =
            $"Data Source={System.IO.Path.Combine(DataDir, "Cube.db")};provider=sqlite";
        psi.Environment["ConnectionStrings__Log"] =
            $"Data Source={System.IO.Path.Combine(DataDir, "Log.db")};provider=sqlite";
        psi.Environment["URLS"] = BaseUrl;

        var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };

        var outLog = System.IO.Path.Combine(RunDir, "server-out.log");
        var errLog = System.IO.Path.Combine(RunDir, "server-err.log");
        var outWriter = new System.IO.StreamWriter(outLog, false) { AutoFlush = true };
        var errWriter = new System.IO.StreamWriter(errLog, false) { AutoFlush = true };

        // 必须异步消费 stdout/stderr，否则管道缓冲区写满后应用日志线程会阻塞
        proc.OutputDataReceived += (_, e) => { if (e.Data != null) outWriter.WriteLine(e.Data); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data != null) errWriter.WriteLine(e.Data); };

        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        return proc;
    }

    /// <summary>递归复制构建输出，跳过运行时数据目录</summary>
    private static void CopyDirectory(String source, String target)
    {
        System.IO.Directory.CreateDirectory(target);

        foreach (var file in System.IO.Directory.GetFiles(source))
        {
            System.IO.File.Copy(file, System.IO.Path.Combine(target, System.IO.Path.GetFileName(file)), true);
        }

        foreach (var dir in System.IO.Directory.GetDirectories(source))
        {
            var name = System.IO.Path.GetFileName(dir);
            if (name.Equals("Data", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Log", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Uploads", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Avatars", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Backup", StringComparison.OrdinalIgnoreCase))
                continue;

            CopyDirectory(dir, System.IO.Path.Combine(target, name));
        }
    }

    /// <summary>把 appsettings.json 的连接字符串改写为本次运行目录的绝对路径</summary>
    private static void RewriteConnectionStrings(String file)
    {
        if (!System.IO.File.Exists(file)) return;

        var text = System.IO.File.ReadAllText(file);
        foreach (var name in new[] { "Membership", "Cube", "Log" })
        {
            var db = System.IO.Path.Combine(DataDir, name + ".db").Replace("\\", "\\\\");
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                $"(\"{name}\"\\s*:\\s*\")[^\"]*(\")",
                "$1Data Source=" + db + ";provider=sqlite$2");
        }
        System.IO.File.WriteAllText(file, text);
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

    /// <summary>预热登录页，等待服务端渲染出用户名输入框后再开始测试</summary>
    private static async Task WarmUpLoginPageAsync(TimeSpan timeout)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var url = BaseUrl + "/Admin/User/Login";
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var html = await client.GetStringAsync(url);
                if (html.Contains("id=\"username\"", StringComparison.OrdinalIgnoreCase)) return;
            }
            catch
            {
                // 应用尚未完成初始化或暂时不可用，继续重试
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException(
            $"登录页未在 {timeout.TotalSeconds} 秒内完成预热，请检查应用是否正常启动。Url={url}");
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

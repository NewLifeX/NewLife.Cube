using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace E2EApiTest.Fixtures;

/// <summary>全局 E2E 测试夹具，负责启动 CubeDemo（WebAPI 版）进程</summary>
public sealed class ApiAppFixture : IAsyncLifetime
{
    #region 属性

    /// <summary>本次运行目录，由环境变量 E2E_API_RUN_DIR 注入</summary>
    public static String RunDir { get; } =
        Environment.GetEnvironmentVariable("E2E_API_RUN_DIR")
        ?? System.IO.Path.GetFullPath(System.IO.Path.Combine(
            AppContext.BaseDirectory, "..", "..", "Bin", "E2ETest",
            "Api-Run-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")));
    /// <summary>CubeDemo 运行目录，由环境变量 E2E_API_APP_DIR 注入（= RunDir\app）</summary>
    public static String AppDir { get; } =
        Environment.GetEnvironmentVariable("E2E_API_APP_DIR")
        ?? System.IO.Path.Combine(RunDir, "app");

    /// <summary>数据目录（SQLite 文件位于此处）</summary>
    /// <remarks>CubeDemo appsettings.json 使用 ..\Data 相对路径，数据库落在 RunDir\Data 而非 AppDir\Data</remarks>
    public static String DataDir => System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDir, "..", "Data"));

    /// <summary>基础 URL，默认 http://localhost:8082</summary>
    public static String BaseUrl { get; } =
        Environment.GetEnvironmentVariable("E2E_API_BASE_URL") ?? "http://localhost:8082";

    /// <summary>管理员用户名</summary>
    public static String AdminUser { get; } =
        Environment.GetEnvironmentVariable("E2E_API_ADMIN_USER") ?? "admin";

    /// <summary>管理员密码（CubeDemo 内置账号）</summary>
    public static String AdminPass { get; } =
        Environment.GetEnvironmentVariable("E2E_API_ADMIN_PASS") ?? "admin";

    /// <summary>接口文档 Basic 认证用户名</summary>
    public static String SwaggerUser { get; } =
        Environment.GetEnvironmentVariable("E2E_API_SWAGGER_USER") ?? "newlife";

    /// <summary>接口文档 Basic 认证密码</summary>
    public static String SwaggerPass { get; } =
        Environment.GetEnvironmentVariable("E2E_API_SWAGGER_PASS") ?? "newlife@2026";

    private Process? _process;

    #endregion

    #region 生命周期

    /// <summary>初始化：准备运行目录，启动 CubeDemo，等待健康检查通过</summary>
    public async Task InitializeAsync()
    {
        EnsureDirectories();

        if (await IsServerReadyAsync(TimeSpan.FromSeconds(2)))
        {
            // 复用手工启动的调试实例，不负责其生命周期
            return;
        }

        _process = StartCubeDemo();
        await WaitForServerReadyAsync(TimeSpan.FromSeconds(180));
    }

    /// <summary>清理：停止测试启动的 CubeDemo 进程</summary>
    public Task DisposeAsync()
    {
        if (_process != null && !_process.HasExited)
            _process.Kill(entireProcessTree: true);
        _process?.Dispose();

        return Task.CompletedTask;
    }

    #endregion

    #region 辅助

    private static void EnsureDirectories()
    {
        System.IO.Directory.CreateDirectory(RunDir);
        System.IO.Directory.CreateDirectory(AppDir);
        System.IO.Directory.CreateDirectory(DataDir);
    }

    private static Process StartCubeDemo()
    {
        // 优先复制构建输出（Bin/CubeDemo）：appsettings.json 使用 ..\Data 相对路径，
        // 只有把工作目录定在 AppDir，数据库才会落到 RunDir\Data
        var repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", ".."));
        var buildDir = System.IO.Path.Combine(repoRoot, "Bin", "CubeDemo");
        var dllPath = System.IO.Path.Combine(AppDir, "CubeDemo.dll");

        if (System.IO.File.Exists(System.IO.Path.Combine(buildDir, "CubeDemo.dll")))
        {
            CopyDirectory(buildDir, AppDir);
            RewriteConnectionStrings(System.IO.Path.Combine(AppDir, "appsettings.json"));
        }

        if (!System.IO.File.Exists(dllPath))
            throw new InvalidOperationException($"未找到 CubeDemo 构建输出，请先执行 dotnet build CubeDemo。查找路径：{dllPath}");

        var psi = new ProcessStartInfo("dotnet", $"\"{dllPath}\"")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = AppDir,
        };
        psi.Environment["URLS"] = BaseUrl;
        psi.Environment["ASPNETCORE_URLS"] = BaseUrl;

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

    /// <summary>递归复制构建输出，跳过运行期数据目录</summary>
    private static void CopyDirectory(String source, String target)
    {
        System.IO.Directory.CreateDirectory(target);

        foreach (var file in System.IO.Directory.GetFiles(source))
        {
            // 日志文件可能被正在运行的调试实例占用，跳过
            if (System.IO.Path.GetExtension(file).Equals(".log", StringComparison.OrdinalIgnoreCase)) continue;

            System.IO.File.Copy(file, System.IO.Path.Combine(target, System.IO.Path.GetFileName(file)), true);
        }

        foreach (var dir in System.IO.Directory.GetDirectories(source))
        {
            var name = System.IO.Path.GetFileName(dir);
            // Data/Log/Uploads 等属于运行期数据；Config 会固化 UI 语言等宿主设置
            if (name.Equals("Data", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Log", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Config", StringComparison.OrdinalIgnoreCase) ||
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
            text = Regex.Replace(
                text,
                $"(\"{name}\"\\s*:\\s*\")[^\"]*(\")",
                "$1Data Source=" + db + ";provider=sqlite$2");
        }

        // 示例项目未声明的业务连接（School/Bill）也固定到运行目录，避免写入构建输出目录
        foreach (var name in new[] { "School", "Bill" })
        {
            var db = System.IO.Path.Combine(DataDir, name + ".db").Replace("\\", "\\\\");
            var line = $"    \"{name}\": \"Data Source={db};provider=sqlite\",\n";
            if (text.Contains($"\"{name}\"", StringComparison.OrdinalIgnoreCase))
                text = Regex.Replace(text, $"(\"{name}\"\\s*:\\s*\")[^\"]*(\")", "$1Data Source=" + db + ";provider=sqlite$2");
            else
                text = Regex.Replace(text, "(\"ConnectionStrings\"\\s*:\\s*\\{)", "$1\n" + line);
        }

        System.IO.File.WriteAllText(file, text);
    }

    private static async Task WaitForServerReadyAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await IsServerReadyAsync(TimeSpan.FromSeconds(3)))
                return;

            await Task.Delay(1000);
        }

        throw new TimeoutException(
            $"CubeDemo 未在 {timeout.TotalSeconds} 秒内就绪，请检查应用是否正常启动。BaseUrl={BaseUrl}");
    }

    private static async Task<Boolean> IsServerReadyAsync(TimeSpan timeout)
    {
        try
        {
            using var client = new HttpClient { Timeout = timeout };
            var response = await client.GetAsync(BaseUrl + "/Cube/Health");
            if (!response.IsSuccessStatusCode) return false;

            var html = await response.Content.ReadAsStringAsync();
            return html.Contains("\"code\":0", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    #endregion
}

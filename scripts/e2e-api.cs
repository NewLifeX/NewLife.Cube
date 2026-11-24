#!/usr/bin/env dotnet-script
// e2e-api.cs — .NET 10 WebAPI 版 E2E 测试编排入口
// 用法：dotnet run scripts/e2e-api.cs [--filter Category=Crud]
//
// 功能：
//   1. 创建本次运行目录 Bin/E2ETest/Api-Run-{yyyyMMdd-HHmmss}/
//   2. 发布 CubeDemo 到运行目录
//   3. 设置环境变量，启动 dotnet test（E2EApiTest）
//   4. 生成 TRX + HTML 报告
//   5. 显示摘要

#:package NewLife.Core@*

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using NewLife;
using NewLife.Log;

// ============================================================
// 参数解析
// ============================================================

XTrace.UseConsole();
var args2 = args.ToList();

var filterValue = "";
var filterIdx = args2.IndexOf("--filter");
if (filterIdx >= 0 && filterIdx + 1 < args2.Count)
{
    filterValue = args2[filterIdx + 1];
    args2.RemoveAt(filterIdx + 1);
    args2.RemoveAt(filterIdx);
}

// ============================================================
// 目录初始化
// ============================================================

var repoRoot = Directory.GetCurrentDirectory();
var runTag   = DateTime.Now.ToString("yyyyMMdd-HHmmss");
var runDir   = Path.Combine(repoRoot, "Bin", "E2ETest", $"Api-Run-{runTag}");
var appDir   = Path.Combine(runDir, "app");
var dataDir  = Path.Combine(runDir, "Data");
var trxPath  = Path.Combine(runDir, $"results-{runTag}.trx");
var htmlPath = Path.Combine(runDir, $"report-{runTag}.html");

Directory.CreateDirectory(runDir);
Directory.CreateDirectory(appDir);
Directory.CreateDirectory(dataDir);

Console.WriteLine($"[E2E-API] 本次运行目录: {runDir}");

// ============================================================
// 发布 CubeDemo
// ============================================================

var demoDir = Path.Combine(repoRoot, "CubeDemo");

Console.WriteLine("[E2E-API] 发布 CubeDemo...");
var publishResult = RunProcess("dotnet",
    $"publish \"{demoDir}\" -c Release -o \"{appDir}\" --nologo -v minimal -p:SkipOpenApiContractCheck=true");

if (publishResult != 0)
{
    // 宿主正在调试运行 CubeDemo 时会锁定 Bin/CubeDemo 的输出文件，导致发布失败。
    // 回退为直接复制既有构建输出（测试夹具运行期亦采用同样方式）。
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("[E2E-API] ⚠ CubeDemo 发布失败，回退为复制既有构建输出 Bin/CubeDemo。");
    Console.ResetColor();

    var fallback = Path.Combine(repoRoot, "Bin", "CubeDemo");
    if (!File.Exists(Path.Combine(fallback, "CubeDemo.dll")))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[E2E-API] 未找到可用构建输出，退出。");
        Console.ResetColor();
        return 1;
    }

    CopyDirectory(fallback, appDir);
}

Console.WriteLine("[E2E-API] CubeDemo 就绪。");

// SQLite 驱动：优先随发布输出携带，缺失时从既有运行目录缓存复制
var pluginsDir = Path.Combine(appDir, "Plugins");
Directory.CreateDirectory(pluginsDir);
if (!File.Exists(Path.Combine(pluginsDir, "System.Data.SQLite.dll")))
{
    var cache = Directory.GetDirectories(Path.Combine(repoRoot, "Bin", "E2ETest"), "Api-Run-*")
        .Where(d => !String.Equals(d, runDir, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(d => d)
        .Select(d => Path.Combine(d, "app", "Plugins", "System.Data.SQLite.dll"))
        .FirstOrDefault(File.Exists);
    if (cache != null)
    {
        var src = Path.GetDirectoryName(cache);
        if (src != null)
        {
            foreach (var f in Directory.GetFiles(src))
                File.Copy(f, Path.Combine(pluginsDir, Path.GetFileName(f)), true);
            Console.WriteLine("[E2E-API] 已从缓存复制 SQLite 驱动到 Plugins。");
        }
    }
    else
        Console.WriteLine("[E2E-API] ⚠ 未找到 SQLite 驱动缓存，CubeDemo 可能因插件缺失无法启动。");
}

// ============================================================
// 构建测试项目
// ============================================================

var testProjectDir = Path.Combine(repoRoot, "E2EApiTest");

Console.WriteLine("[E2E-API] 构建 E2EApiTest...");
var buildResult = RunProcess("dotnet",
    $"build \"{testProjectDir}\" -c Release --nologo -v minimal");

if (buildResult != 0)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("[E2E-API] E2EApiTest 构建失败，退出。");
    Console.ResetColor();
    return 1;
}

// ============================================================
// 启动环境变量 → 运行 dotnet test
// ============================================================

var env = new Dictionary<String, String>
{
    ["E2E_API_RUN_DIR"]  = runDir,
    ["E2E_API_APP_DIR"]  = appDir,
    ["E2E_API_BASE_URL"] = "http://localhost:8082",
    ["E2E_API_ADMIN_USER"] = "admin",
    ["E2E_API_ADMIN_PASS"] = "admin",
};

var testArgs = new StringBuilder();
testArgs.Append($"test \"{testProjectDir}\" -c Release --no-build --nologo");
testArgs.Append($" --logger \"trx;LogFileName={trxPath}\"");
testArgs.Append($" --logger \"console;verbosity=normal\"");
if (!String.IsNullOrEmpty(filterValue))
    testArgs.Append($" --filter \"{filterValue}\"");

// 清理端口 8082 的残留进程，避免复用含旧 DB 状态的进程
Console.WriteLine("[E2E-API] 清理端口 8082 的残留进程...");
KillProcessOnPort(8082);
Thread.Sleep(1500);

Console.WriteLine($"[E2E-API] 运行测试：dotnet {testArgs}");

var testResult = RunProcess("dotnet", testArgs.ToString(), env);

// ============================================================
// 生成 HTML 报告
// ============================================================

Console.WriteLine("[E2E-API] 生成 HTML 报告...");
GenerateHtmlReport(trxPath, htmlPath);

Console.WriteLine();
Console.WriteLine("===========================================");
Console.WriteLine($"  TRX  报告: {trxPath}");
Console.WriteLine($"  HTML 报告: {htmlPath}");
Console.WriteLine("===========================================");

return testResult;

// ============================================================
// 辅助函数
// ============================================================

static void CopyDirectory(String source, String target)
{
    Directory.CreateDirectory(target);

    foreach (var file in Directory.GetFiles(source))
    {
        // 日志文件可能被正在运行的调试实例占用，跳过
        if (String.Equals(Path.GetExtension(file), ".log", StringComparison.OrdinalIgnoreCase)) continue;

        File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
    }

    foreach (var dir in Directory.GetDirectories(source))
    {
        var name = Path.GetFileName(dir);
        if (name.Equals("Data", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Log", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Config", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Uploads", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Avatars", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Backup", StringComparison.OrdinalIgnoreCase))
            continue;

        CopyDirectory(dir, Path.Combine(target, name));
    }
}

static void KillProcessOnPort(Int32 port)
{
    try
    {
        var psi = new ProcessStartInfo("cmd.exe", "/c netstat -ano")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
        };
        using var proc = Process.Start(psi)!;
        var output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();

        foreach (var line in output.Split('\n'))
        {
            if (!line.Contains($":{port}") || !line.Contains("LISTENING")) continue;
            var parts = line.Trim().Split([' '], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && Int32.TryParse(parts[^1], out var pid) && pid > 4)
            {
                Console.WriteLine($"[E2E-API] 终止端口 {port} 残留进程 PID={pid}");
                RunProcess("taskkill", $"/F /PID {pid}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[E2E-API] 端口 {port} 清理出错（可忽略）: {ex.Message}");
    }
}

static Int32 RunProcess(String fileName, String arguments, Dictionary<String, String>? env = null)
{
    try
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = false,
        };

        if (env != null)
        {
            foreach (var kv in env)
                psi.Environment[kv.Key] = kv.Value;
        }

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException($"无法启动进程: {fileName}");

        proc.WaitForExit();
        return proc.ExitCode;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[E2E-API] 进程启动失败 [{fileName}]: {ex.Message}");
        return -1;
    }
}

static void GenerateHtmlReport(String trxPath, String htmlPath)
{
    if (!File.Exists(trxPath))
    {
        Console.WriteLine($"[E2E-API] 未找到 TRX 文件，跳过 HTML 报告生成: {trxPath}");
        return;
    }

    try
    {
        var doc = XDocument.Load(trxPath);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
        var tests = doc.Descendants(ns + "UnitTestResult").ToList();

        var passed = tests.Count(t => t.Attribute("outcome")?.Value == "Passed");
        var failed = tests.Count(t => t.Attribute("outcome")?.Value == "Failed");
        var skipped = tests.Count(t => t.Attribute("outcome")?.Value == "NotExecuted");
        var total = tests.Count;

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='zh-CN'><head><meta charset='UTF-8'/>");
        sb.AppendLine("<title>WebAPI E2E 测试报告</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:Arial,sans-serif;margin:20px;color:#333}");
        sb.AppendLine("h1{color:#333}.summary{display:flex;gap:20px;margin:20px 0}");
        sb.AppendLine(".card{padding:15px;border-radius:8px;text-align:center;min-width:100px}");
        sb.AppendLine(".pass{background:#d4edda}.fail{background:#f8d7da}.skip{background:#fff3cd}.total{background:#cce5ff}");
        sb.AppendLine("table{border-collapse:collapse;width:100%}");
        sb.AppendLine("th,td{border:1px solid #ddd;padding:8px;text-align:left}");
        sb.AppendLine("th{background:#f2f2f2}tr:nth-child(even){background:#fafafa}");
        sb.AppendLine(".passed{color:green}.failed{color:red}.notexecuted{color:#888}");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine($"<h1>WebAPI E2E 测试报告 — {DateTime.Now:yyyy-MM-dd HH:mm:ss}</h1>");
        sb.AppendLine("<div class='summary'>");
        sb.AppendLine($"<div class='card total'><div style='font-size:2em'>{total}</div><div>合计</div></div>");
        sb.AppendLine($"<div class='card pass'><div style='font-size:2em'>{passed}</div><div>通过</div></div>");
        sb.AppendLine($"<div class='card fail'><div style='font-size:2em'>{failed}</div><div>失败</div></div>");
        sb.AppendLine($"<div class='card skip'><div style='font-size:2em'>{skipped}</div><div>跳过</div></div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<h2>用例明细</h2>");
        sb.AppendLine("<table><tr><th>测试名称</th><th>结果</th><th>耗时</th><th>错误信息</th></tr>");

        foreach (var t in tests)
        {
            var name = t.Attribute("testName")?.Value ?? "-";
            var outcome = t.Attribute("outcome")?.Value ?? "-";
            var duration = t.Attribute("duration")?.Value ?? "-";
            var css = outcome.ToLowerInvariant();

            var durationMs = "-";
            if (TimeSpan.TryParse(duration, out var ts))
                durationMs = ((Int32)ts.TotalMilliseconds).ToString();

            var errorMsg = t.Descendants(ns + "Message").FirstOrDefault()?.Value ?? "";
            if (errorMsg.Length > 300) errorMsg = errorMsg.Substring(0, 300) + "...";
            errorMsg = System.Net.WebUtility.HtmlEncode(errorMsg);

            sb.AppendLine($"<tr><td>{System.Net.WebUtility.HtmlEncode(name)}</td>");
            sb.AppendLine($"<td class='{css}'>{outcome}</td>");
            sb.AppendLine($"<td>{durationMs} ms</td>");
            sb.AppendLine($"<td><small>{errorMsg}</small></td></tr>");
        }

        sb.AppendLine("</table></body></html>");
        File.WriteAllText(htmlPath, sb.ToString(), Encoding.UTF8);

        Console.WriteLine($"[E2E-API] HTML 报告已生成: {htmlPath}");
        Console.WriteLine($"[E2E-API] 通过={passed} 失败={failed} 跳过={skipped} 合计={total}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[E2E-API] 生成 HTML 报告时出错: {ex.Message}");
    }
}

#!/usr/bin/env dotnet-script
// e2e-mvc.cs — .NET 10 E2E 测试编排入口
// 用法：dotnet run Test/e2e-mvc.cs [--ci] [--headless false] [--filter Category=Auth]
//
// 功能：
//   1. 创建本次运行目录 Bin/E2ETest/Run-{yyyyMMdd-HHmmss}/
//   2. 发布 CubeSSO 到运行目录
//   3. 设置环境变量，启动 dotnet test（E2EMvcTest）
//   4. 生成 TRX + HTML 报告
//   5. 显示摘要

#:package NewLife.Core@*

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NewLife;
using NewLife.Log;

// ============================================================
// 参数解析
// ============================================================

XTrace.UseConsole();
var args2 = args.ToList();

var isCi = args2.Remove("--ci");

var headlessValue = "true";
var headlessIdx = args2.IndexOf("--headless");
if (headlessIdx >= 0 && headlessIdx + 1 < args2.Count)
{
    headlessValue = args2[headlessIdx + 1];
    args2.RemoveAt(headlessIdx + 1);
    args2.RemoveAt(headlessIdx);
}

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
var runDir   = Path.Combine(repoRoot, "Bin", "E2ETest", $"Run-{runTag}");
var appDir   = Path.Combine(runDir, "app");
var dataDir  = Path.Combine(appDir, "Data");
var ssDir    = Path.Combine(runDir, "screenshots");
var trxPath  = Path.Combine(runDir, $"results-{runTag}.trx");
var htmlPath = Path.Combine(runDir, $"report-{runTag}.html");

Directory.CreateDirectory(runDir);
Directory.CreateDirectory(appDir);
Directory.CreateDirectory(dataDir);
Directory.CreateDirectory(ssDir);

Console.WriteLine($"[E2E] 本次运行目录: {runDir}");

// ============================================================
// 发布 CubeSSO
// ============================================================

var cubeSsoDir = Path.Combine(repoRoot, "CubeSSO");

Console.WriteLine("[E2E] 发布 CubeSSO...");
var publishResult = RunProcess("dotnet",
    $"publish \"{cubeSsoDir}\" -c Release -o \"{appDir}\" --nologo -v minimal");

if (publishResult != 0)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("[E2E] CubeSSO 发布失败，退出。");
    Console.ResetColor();
    return 1;
}

Console.WriteLine("[E2E] CubeSSO 发布完成。");

// ============================================================
// 构建测试项目
// ============================================================

var testProjectDir = Path.Combine(repoRoot, "E2EMvcTest");

Console.WriteLine("[E2E] 构建 E2EMvcTest...");
var buildResult = RunProcess("dotnet",
    $"build \"{testProjectDir}\" -c Release --nologo -v minimal");

if (buildResult != 0)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("[E2E] E2EMvcTest 构建失败，退出。");
    Console.ResetColor();
    return 1;
}

// ============================================================
// 安装 Playwright 浏览器（全自动，三级回退）
// ============================================================

Console.WriteLine("[E2E] 确认 Playwright Chromium 已安装...");
var binOutput = Path.Combine(repoRoot, "Bin", "E2ETest");
var playwrightInstalled = false;

// 策略 1：playwright.ps1（优先 pwsh，回退 powershell.exe）
var pwScripts = Directory.GetFiles(binOutput, "playwright.ps1", SearchOption.AllDirectories);
if (pwScripts.Length > 0)
{
    var pwScript = pwScripts[0];
    var scriptArgs = $"-ExecutionPolicy Bypass -File \"{pwScript}\" install chromium";
    Console.WriteLine($"[E2E] 执行 playwright.ps1 安装 Chromium: {pwScript}");
    foreach (var shell in new[] { "pwsh", "powershell" })
    {
        if (RunProcess(shell, scriptArgs) == 0)
        {
            playwrightInstalled = true;
            break;
        }
    }
}

// 策略 2：dotnet exec Microsoft.Playwright.dll install chromium
if (!playwrightInstalled)
{
    var pwDlls = Directory.GetFiles(binOutput, "Microsoft.Playwright.dll", SearchOption.AllDirectories);
    if (pwDlls.Length > 0)
    {
        Console.WriteLine($"[E2E] 使用 dotnet exec 安装 Chromium: {pwDlls[0]}");
        if (RunProcess("dotnet", $"exec \"{pwDlls[0]}\" install chromium") == 0)
            playwrightInstalled = true;
    }
}

if (playwrightInstalled)
    Console.WriteLine("[E2E] Playwright Chromium 已就绪。");
else
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("[E2E] ⚠ Playwright 浏览器自动安装失败，测试可能报浏览器启动错误。");
    Console.ResetColor();
}

// ============================================================
// 启动环境变量 → 运行 dotnet test
// ============================================================

var env = new Dictionary<String, String>
{
    ["E2E_RUN_DIR"]      = runDir,
    ["E2E_APP_DIR"]      = appDir,
    ["E2E_BASE_URL"]     = "http://localhost:8080",
    ["E2E_ADMIN_USER"]   = "admin",
    ["E2E_ADMIN_PASS"]   = "Admin@2025",
    ["E2E_OAUTH_USER"]   = "test",
    ["E2E_OAUTH_PASS"]   = "test",
    ["E2E_HEADLESS"]     = headlessValue,
    ["ConnectionStrings__Membership"] = $"Data Source={dataDir}\\Membership.db;provider=sqlite",
    ["ConnectionStrings__Cube"]       = $"Data Source={dataDir}\\Cube.db;provider=sqlite",
    ["ConnectionStrings__Log"]        = $"Data Source={dataDir}\\Log.db;provider=sqlite",
};

var testArgs = new StringBuilder();
testArgs.Append($"test \"{testProjectDir}\" -c Release --no-build --nologo");
testArgs.Append($" --logger \"trx;LogFileName={trxPath}\"");
testArgs.Append($" --logger \"console;verbosity=normal\"");
if (!String.IsNullOrEmpty(filterValue))
    testArgs.Append($" --filter \"{filterValue}\"");
if (isCi)
    testArgs.Append(" --no-progress");

Console.WriteLine($"[E2E] 运行测试：dotnet {testArgs}");

var testResult = RunProcess("dotnet", testArgs.ToString(), env);

// ============================================================
// 生成 HTML 报告
// ============================================================

Console.WriteLine("[E2E] 生成 HTML 报告...");
GenerateHtmlReport(trxPath, htmlPath, ssDir);

// ============================================================
// 输出摘要
// ============================================================

Console.WriteLine();
Console.WriteLine("===========================================");
Console.WriteLine($"  TRX  报告: {trxPath}");
Console.WriteLine($"  HTML 报告: {htmlPath}");
Console.WriteLine($"  截图目录:  {ssDir}");
Console.WriteLine("===========================================");

return testResult;

// ============================================================
// 辅助函数
// ============================================================

static Int32 RunProcess(String fileName, String arguments,
    Dictionary<String, String>? env = null)
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
        Console.WriteLine($"[E2E] 进程启动失败 [{fileName}]: {ex.Message}");
        return -1;
    }
}

static void GenerateHtmlReport(String trxPath, String htmlPath, String screenshotDir)
{
    if (!File.Exists(trxPath))
    {
        Console.WriteLine($"[E2E] 未找到 TRX 文件，跳过 HTML 报告生成: {trxPath}");
        return;
    }

    try
    {
        var doc   = XDocument.Load(trxPath);
        var ns    = doc.Root?.Name.Namespace ?? XNamespace.None;
        var tests = doc.Descendants(ns + "UnitTestResult").ToList();

        var passed  = tests.Count(t => t.Attribute("outcome")?.Value == "Passed");
        var failed  = tests.Count(t => t.Attribute("outcome")?.Value == "Failed");
        var skipped = tests.Count(t => t.Attribute("outcome")?.Value == "NotExecuted");
        var total   = tests.Count;

        var screenshots = Directory.GetFiles(screenshotDir, "*.png", SearchOption.TopDirectoryOnly);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='zh-CN'><head><meta charset='UTF-8'/>");
        sb.AppendLine("<title>E2E 测试报告</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:Arial,sans-serif;margin:20px;color:#333}");
        sb.AppendLine("h1{color:#333}.summary{display:flex;gap:20px;margin:20px 0}");
        sb.AppendLine(".card{padding:15px;border-radius:8px;text-align:center;min-width:100px}");
        sb.AppendLine(".pass{background:#d4edda}.fail{background:#f8d7da}.skip{background:#fff3cd}.total{background:#cce5ff}");
        sb.AppendLine("table{border-collapse:collapse;width:100%}");
        sb.AppendLine("th,td{border:1px solid #ddd;padding:8px;text-align:left}");
        sb.AppendLine("th{background:#f2f2f2}tr:nth-child(even){background:#fafafa}");
        sb.AppendLine(".passed{color:green}.failed{color:red}.notexecuted{color:#888}");
        sb.AppendLine(".screenshots{display:flex;flex-wrap:wrap;gap:10px;margin-top:20px}");
        sb.AppendLine(".screenshots img{max-width:300px;border:1px solid #ccc;border-radius:4px}");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine($"<h1>E2E 测试报告 — {DateTime.Now:yyyy-MM-dd HH:mm:ss}</h1>");
        sb.AppendLine("<div class='summary'>");
        sb.AppendLine($"<div class='card total'><div style='font-size:2em'>{total}</div><div>合计</div></div>");
        sb.AppendLine($"<div class='card pass'><div style='font-size:2em'>{passed}</div><div>通过</div></div>");
        sb.AppendLine($"<div class='card fail'><div style='font-size:2em'>{failed}</div><div>失败</div></div>");
        sb.AppendLine($"<div class='card skip'><div style='font-size:2em'>{skipped}</div><div>跳过</div></div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<h2>用例明细</h2>");
        sb.AppendLine("<table><tr><th>测试名称</th><th>结果</th><th>耗时(ms)</th><th>错误信息</th></tr>");

        foreach (var t in tests)
        {
            var name    = t.Attribute("testName")?.Value ?? "-";
            var outcome = t.Attribute("outcome")?.Value ?? "-";
            var duration = t.Attribute("duration")?.Value ?? "-";
            var css     = outcome.ToLowerInvariant();

            // 解析 duration 为毫秒
            String durationMs = "-";
            if (TimeSpan.TryParse(duration, out var ts))
                durationMs = ((Int32)ts.TotalMilliseconds).ToString();

            var errorMsg = t.Descendants(ns + "Message").FirstOrDefault()?.Value ?? "";
            if (errorMsg.Length > 200) errorMsg = errorMsg.Substring(0, 200) + "...";
            errorMsg = System.Net.WebUtility.HtmlEncode(errorMsg);

            sb.AppendLine($"<tr><td>{System.Net.WebUtility.HtmlEncode(name)}</td>");
            sb.AppendLine($"<td class='{css}'>{outcome}</td>");
            sb.AppendLine($"<td>{durationMs}</td>");
            sb.AppendLine($"<td><small>{errorMsg}</small></td></tr>");
        }

        sb.AppendLine("</table>");

        if (screenshots.Length > 0)
        {
            sb.AppendLine("<h2>失败截图</h2><div class='screenshots'>");
            foreach (var ss in screenshots)
            {
                var fileName = Path.GetFileName(ss);
                var bytes    = File.ReadAllBytes(ss);
                var b64      = Convert.ToBase64String(bytes);
                sb.AppendLine($"<div><div><small>{System.Net.WebUtility.HtmlEncode(fileName)}</small></div>");
                sb.AppendLine($"<img src='data:image/png;base64,{b64}' alt='{fileName}'/></div>");
            }
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</body></html>");
        File.WriteAllText(htmlPath, sb.ToString(), Encoding.UTF8);

        Console.WriteLine($"[E2E] HTML 报告已生成: {htmlPath}");
        Console.WriteLine($"[E2E] 通过={passed} 失败={failed} 跳过={skipped} 合计={total}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[E2E] 生成 HTML 报告时出错: {ex.Message}");
    }
}

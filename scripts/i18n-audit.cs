// i18n-audit.cs — 多语言巡检：切换语言后抓取全站页面，找出仍显示中文的界面细节
// 用法：dotnet run scripts/i18n-audit.cs -- [--base http://localhost:8095] [--lang en-US] [--out <dir>] [--max 150]
//
// 输出：<out>/i18n-audit-<lang>.md（人读）与 .json（机读）
// 判定：界面元素（文本/占位符/标题/按钮值/选项）出现中日韩字符即记为问题；
//       表格数据单元（tbody td）视为数据库数据，单独统计不判错；
//       另按 DOM 区域分类：menu 菜单 / form 表单 / table 表头 / nav 页签 / other 其它。

#:package Microsoft.Playwright@1.63.0
#:property JsonSerializerIsReflectionEnabledByDefault=true
#:property PublishAot=false

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;

var baseUrl = "http://localhost:8095";
var lang = "en-US";
var outDir = Path.Combine(Directory.GetCurrentDirectory(), ".tmp", "i18n-audit");
var maxPages = 150;
var user = "admin";
var pass = "admin";

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--base": baseUrl = args[++i]; break;
        case "--lang": lang = args[++i]; break;
        case "--out": outDir = args[++i]; break;
        case "--max": maxPages = Int32.Parse(args[++i]); break;
        case "--user": user = args[++i]; break;
        case "--pass": pass = args[++i]; break;
    }
}

baseUrl = baseUrl.TrimEnd('/');
Directory.CreateDirectory(outDir);
var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

Console.WriteLine($"巡检目标：{baseUrl}  语言：{lang}  最大页面数：{maxPages}");

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
{
    Locale = lang,
    IgnoreHTTPSErrors = true,
});
var page = await context.NewPageAsync();

// ===== 登录 =====
var loginUrl = $"{baseUrl}/Admin/User/Login?lang={lang}";
await page.GotoAsync(loginUrl, new PageGotoOptions { Timeout = 60000 });
Console.WriteLine("登录页标题：" + await page.TitleAsync());

var loginOk = false;
try
{
    await page.FillAsync("#username", user, new PageFillOptions { Timeout = 15000 });
    await page.FillAsync("#password", pass);
    await page.ClickAsync("#login-btn");
    await page.WaitForURLAsync("**/Admin/**", new PageWaitForURLOptions { Timeout = 45000 });
    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    loginOk = !page.Url.Contains("/User/Login");
}
catch (Exception ex)
{
    Console.WriteLine("登录异常：" + ex.Message);
}

Console.WriteLine($"登录{(loginOk ? "成功" : "失败")}，当前地址：{page.Url}");
if (!loginOk)
{
    Console.WriteLine("登录失败，无法继续巡检。");
    return 2;
}

// ===== 采集待巡检页面 =====
var queue = new Queue<String>();
var seen = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

String Normalize(String path)
{
    // 数字/GUID 段归一，避免同一页面的不同记录被重复巡检
    var segs = path.Split('/');
    for (var i = 0; i < segs.Length; i++)
    {
        if (Regex.IsMatch(segs[i], @"^\d+$") || Regex.IsMatch(segs[i], @"^[0-9a-fA-F\-]{16,}$")) segs[i] = "{id}";
    }
    return String.Join('/', segs);
}

void Enqueue(String? href)
{
    if (String.IsNullOrEmpty(href)) return;
    if (!href.StartsWith("/") || href.StartsWith("//")) return;
    var path = href.Split('?')[0];
    if (String.IsNullOrEmpty(path) || path == "/") return;
    // 导出端点与静态资源，避免触发 404 扫描限流
    if (Regex.IsMatch(path, @"^/(Html|Json|Xml|Csv|Excel|Zip|Pdf)$")) return;
    foreach (var skip in new[] { "/Logout", "/Delete", "/Export", "/Download", "/Clear", "/Revoke", "/UnBind", "/Bind" })
    {
        if (path.Contains(skip, StringComparison.OrdinalIgnoreCase)) return;
    }
    var key = Normalize(path).ToLowerInvariant();
    if (!seen.Add(key)) return;
    queue.Enqueue(path);
}

Enqueue("/Admin/Index/Dashboard");

var results = new List<PageResult>();
const String collectJs = @"() => {
    const isCjk = (t) => /[\u4e00-\u9fff\u3400-\u4dbf\u3000-\u303f\uff00-\uffef\u3040-\u30ff]/.test(t);
    const out = [];
    const path = (el) => {
        let s = el.tagName.toLowerCase();
        if (el.id) s += '#' + el.id;
        if (el.classList && el.classList.length) s += '.' + Array.from(el.classList).slice(0, 2).join('.');
        return s;
    };
    const region = (el) => {
        if (el.closest('#sidebar, .sidebar, .navmenu, .main-menu, .breadcrumb')) return 'menu';
        if (el.closest('.pagination, .pager')) return 'pager';
        if (el.closest('.nav-tabs, .profile-tabs, .navbar, .nav-pills, .tabs')) return 'nav';
        if (el.closest('tbody')) return 'tableData';
        if (el.closest('table')) return 'table';
        if (el.closest('form, .form-horizontal, .form-group, .controls')) return 'form';
        if (el.closest('.modal, .dialog')) return 'modal';
        return 'other';
    };
    const push = (el, kind, text) => {
        if (!text) return;
        const t = String(text).replace(/\s+/g, ' ').trim();
        if (!t || !isCjk(t)) return;
        out.push({ where: path(el), kind, text: t, region: region(el) });
    };
    const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, {
        acceptNode: (n) => {
            const p = n.parentElement;
            if (!p) return NodeFilter.FILTER_REJECT;
            const tag = p.tagName.toLowerCase();
            if (tag === 'script' || tag === 'style' || tag === 'noscript') return NodeFilter.FILTER_REJECT;
            return NodeFilter.FILTER_ACCEPT;
        }
    });
    let n;
    while ((n = walker.nextNode())) push(n.parentElement, 'text', n.nodeValue);
    for (const el of document.querySelectorAll('[placeholder],[title],[alt],[aria-label]')) {
        for (const a of ['placeholder','title','alt','aria-label']) {
            const v = el.getAttribute(a);
            if (v) push(el, a, v);
        }
    }
    for (const el of document.querySelectorAll('input[type=submit],input[type=button],input[type=reset]')) {
        push(el, 'value', el.value);
    }
    for (const el of document.querySelectorAll('option')) {
        push(el, 'option', el.textContent);
    }
    return out;
}";

while (queue.Count > 0 && results.Count < maxPages)
{
    var path = queue.Dequeue();
    var url = $"{baseUrl}{path}{(path.Contains('?') ? "&" : "?")}lang={lang}";

    var r = new PageResult { Path = path };
    try
    {
        var resp = await page.GotoAsync(url, new PageGotoOptions { Timeout = 60000, WaitUntil = WaitUntilState.DOMContentLoaded });
        r.Status = resp?.Status ?? 0;
        r.FinalUrl = page.Url;
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(150);

        r.Title = await page.TitleAsync();
        r.HtmlLang = await page.EvaluateAsync<String>("() => document.documentElement.getAttribute('lang') || ''");

        var raw = await page.EvaluateAsync<String>($"() => JSON.stringify(({collectJs})())");
        foreach (var it in JsonSerializer.Deserialize<List<TextItem>>(raw, jsonOpts) ?? [])
        {
            if (it.Region == "tableData") r.DataCjkCount++;
            else r.Items.Add(it);
        }

        var links = await page.EvaluateAsync<String>("() => JSON.stringify(Array.from(document.querySelectorAll('a[href]')).map(a => a.getAttribute('href')))");
        foreach (var link in JsonSerializer.Deserialize<List<String>>(links, jsonOpts) ?? [])
        {
            Enqueue(link);
        }
    }
    catch (Exception ex)
    {
        r.Error = ex.Message;
    }

    results.Add(r);
    Console.WriteLine($"[{results.Count,3}] {path}  status={r.Status} 界面中文={r.Items.Count} 数据中文={r.DataCjkCount}{(String.IsNullOrEmpty(r.Error) ? "" : "  错误:" + r.Error)}");
}

// ===== 输出 =====
var sb = new StringBuilder();
sb.AppendLine($"# 多语言巡检报告（{lang}）");
sb.AppendLine();
sb.AppendLine($"> 目标：{baseUrl}　时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}　页面数：{results.Count}");
sb.AppendLine($"> 判定：界面元素出现中文即问题；表格数据单元（tbody td）视为数据库数据，仅统计不判错。");
sb.AppendLine();

var bad = results.Where(e => e.Items.Count > 0).OrderByDescending(e => e.Items.Count).ToList();
var dataBad = results.Where(e => e.DataCjkCount > 0).OrderByDescending(e => e.DataCjkCount).ToList();
var errs = results.Where(e => !String.IsNullOrEmpty(e.Error) || e.Status >= 400).ToList();

sb.AppendLine("## 汇总");
sb.AppendLine();
sb.AppendLine("| 指标 | 数量 |");
sb.AppendLine("|---|---|");
sb.AppendLine($"| 巡检页面 | {results.Count} |");
sb.AppendLine($"| 仍有中文界面文案的页面 | {bad.Count} |");
sb.AppendLine($"| 中文界面文案条目 | {bad.Sum(e => e.Items.Count)} |");
sb.AppendLine($"| 含中文数据的页面（忽略） | {dataBad.Count} |");
sb.AppendLine($"| 访问异常页面 | {errs.Count} |");
sb.AppendLine();

if (errs.Count > 0)
{
    sb.AppendLine("## 访问异常");
    sb.AppendLine();
    foreach (var e in errs)
    {
        sb.AppendLine($"- `{e.Path}` status={e.Status} {e.Error}");
    }
    sb.AppendLine();
}

sb.AppendLine("## 界面仍是中文的页面");
sb.AppendLine();
foreach (var e in bad)
{
    sb.AppendLine($"### `{e.Path}`（{e.Items.Count} 条）");
    sb.AppendLine();
    sb.AppendLine($"标题：`{e.Title}`　html lang：`{e.HtmlLang}`");
    sb.AppendLine();
    foreach (var g in e.Items.GroupBy(i => i.Region))
    {
        sb.AppendLine($"- **{g.Key}**");
        foreach (var it in g)
        {
            sb.AppendLine($"  - `{it.Where}` {it.Kind}：`{it.Text}`");
        }
    }
    sb.AppendLine();
}

if (dataBad.Count > 0)
{
    sb.AppendLine("## 数据区中文（数据库数据，仅供参考）");
    sb.AppendLine();
    foreach (var e in dataBad)
    {
        sb.AppendLine($"- `{e.Path}`：{e.DataCjkCount} 处");
    }
}

var mdPath = Path.Combine(outDir, $"i18n-audit-{lang}.md");
await File.WriteAllTextAsync(mdPath, sb.ToString(), new UTF8Encoding(false));
var jsonPath = Path.Combine(outDir, $"i18n-audit-{lang}.json");
await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(results, new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
}), new UTF8Encoding(false));

Console.WriteLine($"报告：{mdPath}");
return bad.Count > 0 ? 1 : 0;

class PageResult
{
    public String Path { get; set; } = "";
    public String FinalUrl { get; set; } = "";
    public Int32 Status { get; set; }
    public String Title { get; set; } = "";
    public String HtmlLang { get; set; } = "";
    public String Error { get; set; } = "";
    public List<TextItem> Items { get; set; } = [];
    public Int32 DataCjkCount { get; set; }
}

class TextItem
{
    public String Where { get; set; } = "";
    public String Kind { get; set; } = "";
    public String Text { get; set; } = "";
    public String Region { get; set; } = "";
}

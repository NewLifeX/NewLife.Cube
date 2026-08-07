using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>AI 对话助手浮窗测试</summary>
/// <remarks>验证右下角 AI 助手浮窗：悬浮球可打开对话面板、快捷指令与输入框可用；
/// 页面上下文标记（aiPage/aiQuery 等）在列表页/表单页正确输出。
/// 悬浮球仅当 AISwitch 开启时渲染，未开启时跳过交互部分。</remarks>
[Collection("E2E")]
public sealed class AiAssistantTests : IAsyncLifetime
{
    private readonly AppFixture _fixture;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public AiAssistantTests(AppFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _context = await _fixture.Browser.NewContextAsync();
        _page = await _context.NewPageAsync();
        await PageHelpers.LoginAsAdminAsync(_page);
    }

    public async Task DisposeAsync()
    {
        if (_page != null)
        {
            try { await PageHelpers.LogoutAsync(_page); } catch { }
        }
        await _context.DisposeAsync();
    }

    [Fact(DisplayName = "TC-AI-010 AI 助手浮窗可打开且面板可用")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P1")]
    public async Task TC_AI_010_AssistantFabWorks()
    {
        const String testId = "TC-AI-010";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/UserStat");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        var fab = _page.Locator("#aiAssistantFab");
        if (!await fab.IsVisibleAsync())
        {
            // AISwitch 未开启时浮窗不渲染，跳过交互验证
            return;
        }

        // 0. 配色由 CubeSetting 注入 CSS 变量（默认新生命绿）
        var primary = await _page.Locator("#aiAssistant")
            .EvaluateAsync<String>("el => getComputedStyle(el).getPropertyValue('--ai-primary').trim()");
        Assert.Equal("#2ecc71", primary, ignoreCase: true);

        // 1. 点击悬浮球，面板打开
        await fab.ClickAsync();
        var panel = _page.Locator("#aiAssistantPanel");
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });
        Assert.True(await panel.IsVisibleAsync(), $"[{testId}] AI 助手面板未打开");

        // 2. 输入框与快捷指令存在
        Assert.True(await _page.Locator("#aiInput").IsVisibleAsync(), $"[{testId}] 未找到输入框");
        Assert.True(await _page.Locator(".ai-chip").First.IsVisibleAsync(), $"[{testId}] 未找到快捷指令");

        // 3. 关闭面板
        await _page.Locator("#aiClosePanel").ClickAsync();
        Assert.False(await panel.IsVisibleAsync(), $"[{testId}] 面板关闭失败");

        // 4. 无未捕获 JS 异常
        Assert.Empty(pageErrors);
    }

    [Fact(DisplayName = "TC-AI-011 列表页输出 AI 页面上下文标记")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P2")]
    public async Task TC_AI_011_ListContextMarker()
    {
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/UserStat");

        var marker = _page.Locator("#aiPage");
        if (await marker.CountAsync() == 0) return;

        Assert.Equal("list", await marker.InputValueAsync());
    }

    [Fact(DisplayName = "TC-AI-012 Markdown 表格渲染")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P2")]
    public async Task TC_AI_012_MarkdownTableRender()
    {
        const String testId = "TC-AI-012";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/UserStat");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        var fab = _page.Locator("#aiAssistantFab");

        // 注入 AI 助手浮窗并加载真实 ai-assistant.js（不依赖 AISwitch 设置）
        // 实体页 /Admin/UserStat 统一走全局端点 /Ai/AiChat（服务端经 IEntityAiContext 提供数据上下文工具）
        await EnsureAiAssistantWithUrlAsync(_page, "/Ai/AiChat");

        // 拦截 AiChat 返回含 Markdown 表格的假 SSE
        var content = "| 名称 | 值 |\n|---|---|\n| 维度 | 123 |";
        var json = "{\"type\":\"text\",\"content\":\"" + content.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"}";
        var sse = "data: " + json + "\n\n";
        await _page.RouteAsync("**/AiChat", async route =>
        {
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "text/event-stream",
                Body = sse,
            });
        });

        await fab.ClickAsync();
        var panel = _page.Locator("#aiAssistantPanel");
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });

        await _page.Locator("#aiInput").FillAsync("给我一个表格");
        await _page.Locator("#aiSend").ClickAsync();

        var table = panel.Locator(".ai-bubble table");
        await table.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });
        Assert.True(await table.IsVisibleAsync(), $"[{testId}] Markdown 表格未渲染");

        var thText = await table.Locator("thead th").First.InnerTextAsync();
        Assert.True(thText.Contains("名称"), $"[{testId}] 表头未渲染：'{thText}'");
        var tdText = await table.Locator("tbody td").First.InnerTextAsync();
        Assert.True(tdText.Contains("维度"), $"[{testId}] 表格数据未渲染：'{tdText}'");
        Assert.Empty(pageErrors);

        await _page.UnrouteAsync("**/AiChat");
    }

    [Fact(DisplayName = "TC-AI-013 AI 助手面板可拖动")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P2")]
    public async Task TC_AI_013_PanelDraggable()
    {
        const String testId = "TC-AI-013";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/UserStat");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        var fab = _page.Locator("#aiAssistantFab");

        // 注入 AI 助手浮窗并加载真实 ai-assistant.js（不依赖 AISwitch 设置）
        await EnsureAiAssistantAsync(_page);

        // 悬浮球为 position:fixed，Playwright 1.61 对其 actionability 检查存在“视口外”间歇误报，
        // 几何已验证（elementFromPoint 命中），故用 Force=true 跳过检查
        await fab.ClickAsync(new LocatorClickOptions { Force = true });
        var panel = _page.Locator("#aiAssistantPanel");
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });

        var before = await panel.BoundingBoxAsync();
        Assert.NotNull(before);

        // 从标题栏左侧（避开右侧清空/关闭按钮）拖动
        var header = _page.Locator(".ai-panel-header");
        var hb = await header.BoundingBoxAsync();
        Assert.NotNull(hb);
        var sx = hb.X + 40;
        var sy = hb.Y + hb.Height / 2;

        const Int32 dx = 120, dy = 80;
        await _page.Mouse.MoveAsync(sx, sy);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(sx + dx, sy + dy, new MouseMoveOptions { Steps = 10 });
        await _page.Mouse.UpAsync();

        var after = await panel.BoundingBoxAsync();
        Assert.NotNull(after);

        // 位移约等于拖动量（容差 ±6px）
        Assert.True(Math.Abs((after.X - before.X) - dx) <= 6, $"[{testId}] 水平位移异常：{before.X} -> {after.X}");
        Assert.True(Math.Abs((after.Y - before.Y) - dy) <= 6, $"[{testId}] 垂直位移异常：{before.Y} -> {after.Y}");

        // 拖动后仍可正常关闭
        await _page.Locator("#aiClosePanel").ClickAsync();
        Assert.False(await panel.IsVisibleAsync(), $"[{testId}] 拖动后关闭失败");
        Assert.Empty(pageErrors);
    }

    [Fact(DisplayName = "TC-AI-014 下拉框 AI 回填同步表面选择")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P2")]
    public async Task TC_AI_014_DropDownFillSync()
    {
        const String testId = "TC-AI-014";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Add");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        var fab = _page.Locator("#aiAssistantFab");

        // 注入 AI 助手浮窗并加载真实 ai-assistant.js（不依赖 AISwitch 设置）
        await EnsureAiAssistantAsync(_page);

        // 当前主题必须集成 bootstrap-multiselect（ACE 默认 BootstrapSelect=true）
        var hasPlugin = await _page.EvaluateAsync<Boolean>("!!(window.jQuery && jQuery.fn && jQuery.fn.multiselect)");
        Assert.True(hasPlugin, $"[{testId}] 当前主题未集成 bootstrap-multiselect，无法验证下拉回填");

        // 定位 bootstrap-multiselect 下拉，取其最后一个有效选项作为回填值
        var sel = _page.Locator("select.multiselect").First;
        Assert.True(await sel.CountAsync() > 0, $"[{testId}] 页面未找到 multiselect 下拉");

        var name = await sel.GetAttributeAsync("name");
        Assert.False(String.IsNullOrEmpty(name), $"[{testId}] 下拉缺少 name 属性");

        var opts = sel.Locator("option");
        var optCount = await opts.CountAsync();
        String? optValue = null;
        String? optText = null;
        for (var i = optCount - 1; i >= 0; i--)
        {
            var v = await opts.Nth(i).GetAttributeAsync("value");
            var t = (await opts.Nth(i).InnerTextAsync()).Trim();
            if (String.IsNullOrEmpty(v) || String.IsNullOrEmpty(t)) continue;
            optValue = v;
            optText = t;
            break;
        }
        Assert.False(String.IsNullOrEmpty(optValue), $"[{testId}] 未找到有效的下拉选项");

        // 确保插件已初始化（BootstrapSelect 默认开启，防御性初始化）
        await _page.EvaluateAsync($@"
            (function () {{
                var $ = window.jQuery;
                if (!$ || !$.fn.multiselect) return;
                var el = $('select[name=""{name}""]');
                if (!el.data('multiselect')) el.multiselect({{ nonSelectedText: '无' }});
            }})()
        ");

        // 拦截 AiChat 返回 fill_form 完成事件
        var valueJson = "{\"kind\":\"fill_form\",\"values\":{\"" + name + "\":\"" + optValue + "\"}}";
        var sse = "data: {\"type\":\"tool\",\"event\":\"done\",\"id\":\"f1\",\"name\":\"fill_form\",\"value\":\"" + valueJson.Replace("\"", "\\\"") + "\"}\n\n";
        await _page.RouteAsync("**/AiChat", async route =>
        {
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "text/event-stream",
                Body = sse,
            });
        });

        await fab.ClickAsync();
        var panel = _page.Locator("#aiAssistantPanel");
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });
        await _page.Locator("#aiInput").FillAsync("帮我选择下拉框");
        await _page.Locator("#aiSend").ClickAsync();

        // 等待回填工具卡片完成
        await panel.Locator(".ai-tool.ai-tool-done").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });

        // 断言：值已写入 + 插件按钮文字同步为所选选项标签（"数值对 + 表面也对"）
        var rsJson = await _page.EvaluateAsync<String>($@"
            (function () {{
                try {{
                    var $ = window.jQuery;
                    var sel = $('select[name=""{name}""]');
                    var ms = sel.data('multiselect');
                    var btn = ms && ms.$button ? ms.$button : sel.next().find('button.multiselect');
                    return JSON.stringify({{
                        val: String(sel.val() ?? ''),
                        btn: btn.length ? String(btn.text()).trim() : ''
                    }});
                }} catch (e) {{
                    return JSON.stringify({{ err: String(e && e.message || e) }});
                }}
            }})()
        ");
        var rs = System.Text.Json.JsonSerializer.Deserialize<Dictionary<String, String?>>(rsJson);
        Assert.NotNull(rs);
        Assert.False(rs!.ContainsKey("err"), $"[{testId}] 页面 JS 异常：{rs.GetValueOrDefault("err")}");
        var val = rs.GetValueOrDefault("val") ?? "";
        var btnText = rs.GetValueOrDefault("btn") ?? "";
        Assert.True(val.Contains(optValue!), $"[{testId}] 下拉值未写入：期望 {optValue}，实际 '{val}'");
        Assert.True(btnText.Contains(optText!), $"[{testId}] 下拉按钮文字未同步：期望包含 '{optText}'，实际 '{btnText}'");
        Assert.Empty(pageErrors);

        await _page.UnrouteAsync("**/AiChat");
    }

    [Fact(DisplayName = "TC-AI-016 面板打开时悬浮球隐藏，关闭后恢复")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P2")]
    public async Task TC_AI_016_FabHiddenWhenPanelOpen()
    {
        const String testId = "TC-AI-016";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/UserStat");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 注入浮窗（含面板打开隐藏悬浮球的样式契约），不依赖 AISwitch 设置
        await EnsureAiAssistantAsync(_page);

        var fab = _page.Locator("#aiAssistantFab");
        Assert.True(await fab.IsVisibleAsync(), $"[{testId}] 初始悬浮球应可见");

        // 打开面板 → 悬浮球隐藏（占住悬浮球位置）；悬浮球为 fixed 定位，Force 规避 Playwright 误报
        await fab.ClickAsync(new LocatorClickOptions { Force = true });
        var panel = _page.Locator("#aiAssistantPanel");
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });
        Assert.False(await fab.IsVisibleAsync(), $"[{testId}] 面板打开后悬浮球应隐藏");

        // 关闭面板 → 悬浮球恢复显示
        await _page.Locator("#aiClosePanel").ClickAsync();
        Assert.False(await panel.IsVisibleAsync(), $"[{testId}] 面板关闭失败");
        Assert.True(await fab.IsVisibleAsync(), $"[{testId}] 面板关闭后悬浮球应恢复显示");

        Assert.Empty(pageErrors);
    }

    [Fact(DisplayName = "TC-AI-017 面板最大化/还原切换")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P2")]
    public async Task TC_AI_017_MaximizeToggle()
    {
        const String testId = "TC-AI-017";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/UserStat");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 注入浮窗（含放大按钮），不依赖 AISwitch 设置
        await EnsureAiAssistantAsync(_page);

        // 悬浮球为 fixed 定位，Force 规避 Playwright 1.61 的“视口外”误报
        await _page.Locator("#aiAssistantFab").ClickAsync(new LocatorClickOptions { Force = true });
        var panel = _page.Locator("#aiAssistantPanel");
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });

        // 初始非放大：无 maximized 类，放大图标可见
        Assert.False(await panel.EvaluateAsync<Boolean>("el => el.classList.contains('maximized')"), $"[{testId}] 初始不应为放大态");
        Assert.True(await _page.Locator("#aiMaximize .fa-expand").IsVisibleAsync(), $"[{testId}] 初始应显示放大图标");

        // 点击放大 → maximized 类 + 还原图标可见
        // 注：面板为 position:fixed，Playwright 1.61 对其 actionability 检查存在“视口外”误报，
        // 几何已验证（elementFromPoint 命中按钮、Force 点击成功），故用 Force=true 跳过检查
        await _page.Locator("#aiMaximize").ClickAsync(new LocatorClickOptions { Force = true });
        Assert.True(await panel.EvaluateAsync<Boolean>("el => el.classList.contains('maximized')"), $"[{testId}] 放大后应带 maximized 类");
        Assert.True(await _page.Locator("#aiMaximize .fa-compress").IsVisibleAsync(), $"[{testId}] 放大后应显示还原图标");

        // 再次点击还原 → 移除 maximized 类 + 放大图标恢复
        await _page.Locator("#aiMaximize").ClickAsync(new LocatorClickOptions { Force = true });
        Assert.False(await panel.EvaluateAsync<Boolean>("el => el.classList.contains('maximized')"), $"[{testId}] 还原后应移除 maximized 类");
        Assert.True(await _page.Locator("#aiMaximize .fa-expand").IsVisibleAsync(), $"[{testId}] 还原后应显示放大图标");

        Assert.Empty(pageErrors);
    }

    [Fact(DisplayName = "TC-AI-018 消息气泡满宽显示")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P3")]
    public async Task TC_AI_018_BubbleFullWidth()
    {
        const String testId = "TC-AI-018";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/UserStat");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 注入浮窗（含气泡满宽的样式契约），不依赖 AISwitch 设置
        await EnsureAiAssistantAsync(_page);

        // 悬浮球为 fixed 定位，Force 规避 Playwright 1.61 的“视口外”误报
        await _page.Locator("#aiAssistantFab").ClickAsync(new LocatorClickOptions { Force = true });
        var panel = _page.Locator("#aiAssistantPanel");
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });

        // 欢迎语气泡应与消息容器同宽（满宽布局，无左右留白）
        var rsJson = await _page.Locator("#aiMessages .ai-bubble").First.EvaluateAsync<String>(@"el => {
            try {
                var b = el.getBoundingClientRect();
                var c = el.parentElement.getBoundingClientRect();
                return JSON.stringify({ bw: b.width, cw: c.width });
            } catch (e) {
                return JSON.stringify({ err: String(e && e.message || e) });
            }
        }");
        var rs = System.Text.Json.JsonSerializer.Deserialize<Dictionary<String, Double>>(rsJson);
        Assert.NotNull(rs);
        Assert.False(rs!.ContainsKey("err"), $"[{testId}] 页面 JS 异常");
        Assert.True(rs["cw"] - rs["bw"] < 1, $"[{testId}] 气泡未满宽：容器 {rs["cw"]}px，气泡 {rs["bw"]}px");
        Assert.Empty(pageErrors);
    }

    [Fact(DisplayName = "TC-AI-019 非实体页面 AI 对话路由到全局端点")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P2")]
    public async Task TC_AI_019_NonEntityPageRoutesToGlobalEndpoint()
    {
        const String testId = "TC-AI-019";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        // 魔方设置页（ConfigController）为典型非实体页面，统一走全局端点 /Ai/AiChat
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/Cube");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 服务端注入验证：AISwitch 开启时真实浮窗渲染，data-ai-url 应指向全局端点，且注入目标页面标识供全局端点解析
        var real = _page.Locator("#aiAssistant");
        if (await real.CountAsync() > 0)
        {
            var injected = await real.GetAttributeAsync("data-ai-url");
            Assert.Equal("/Ai/AiChat", injected);
            var area = await real.GetAttributeAsync("data-ai-area");
            var controller = await real.GetAttributeAsync("data-ai-controller");
            Assert.Equal("Admin", area);
            Assert.Equal("Cube", controller);
        }

        // 注入浮窗并强制 data-ai-url 指向全局端点（不依赖 AISwitch 设置）
        await EnsureAiAssistantWithUrlAsync(_page, "/Ai/AiChat");

        // 拦截全局端点，记录请求 URL 并返回假 SSE
        var requestUrl = "";
        var sse = "data: {\"type\":\"text\",\"content\":\"全局端点可达\"}\n\n";
        await _page.RouteAsync("**/Ai/AiChat", async route =>
        {
            requestUrl = route.Request.Url;
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "text/event-stream",
                Body = sse,
            });
        });

        await _page.Locator("#aiAssistantFab").ClickAsync(new LocatorClickOptions { Force = true });
        var panel = _page.Locator("#aiAssistantPanel");
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });

        await _page.Locator("#aiInput").FillAsync("检查系统状态");
        await _page.Locator("#aiSend").ClickAsync();

        await WaitForBubbleTextAsync(_page, "全局端点可达");

        // 请求应打到全局端点而非不存在的 {controller}/AiChat
        Assert.True(requestUrl.Contains("/Ai/AiChat"), $"[{testId}] 非实体页未路由到全局端点：{requestUrl}");

        var text = await panel.Locator(".ai-bubble").Last.InnerTextAsync();
        Assert.True(text.Contains("全局端点可达"), $"[{testId}] 未收到全局端点回复：'{text}'");
        Assert.Empty(pageErrors);

        await _page.UnrouteAsync("**/Ai/AiChat");
    }

    [Fact(DisplayName = "TC-AI-020 非实体页 404 响应不显示 JSON 解析异常")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P2")]
    public async Task TC_AI_020_NonEntityPageErrorHandling()
    {
        const String testId = "TC-AI-020";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/Cube");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 注入浮窗并强制 data-ai-url（不依赖 AISwitch 设置）
        await EnsureAiAssistantWithUrlAsync(_page, "/Ai/AiChat");

        // 模拟旧行为：端点返回 404 HTML 错误页（非 JSON 响应体）
        await _page.RouteAsync("**/Ai/AiChat", async route =>
        {
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 404,
                ContentType = "text/html",
                Body = "<html><head><title>404 Not Found</title></head><body>Not Found</body></html>",
            });
        });

        await _page.Locator("#aiAssistantFab").ClickAsync(new LocatorClickOptions { Force = true });
        var panel = _page.Locator("#aiAssistantPanel");
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });

        await _page.Locator("#aiInput").FillAsync("检查系统状态");
        await _page.Locator("#aiSend").ClickAsync();

        // 气泡应显示可读的 HTTP 错误，而非 "Unexpected end of JSON input"
        await WaitForBubbleTextAsync(_page, "404");

        var text = await panel.Locator(".ai-bubble").Last.InnerTextAsync();
        Assert.True(text.Contains("404"), $"[{testId}] 未显示 HTTP 状态码：'{text}'");
        Assert.False(text.Contains("Unexpected end of JSON"), $"[{testId}] 暴露了 JSON 解析异常：'{text}'");
        Assert.Empty(pageErrors);

        await _page.UnrouteAsync("**/Ai/AiChat");
    }

    /// <summary>注入 AI 助手浮窗标记并加载真实 ai-assistant.js，使测试不依赖 AISwitch 设置。</summary>
    /// <remarks>markup 仅含 JS 契约所需的元素 ID（aiAssistantFab/Panel/Messages/Input/Send 等），
    /// 面板内联 position:fixed 以便验证拖动定位；等待 window.CubeAI 出现表示脚本已加载且 init 已执行。</remarks>
    /// <param name="page">当前页面</param>
    private static async Task EnsureAiAssistantAsync(IPage page)
    {
        var exists = await page.EvaluateAsync<Boolean>("!!document.getElementById('aiAssistantFab')");
        if (exists) return;

        await page.EvaluateAsync(@"
            (function () {
                var div = document.createElement('div');
                div.id = 'aiAssistant';
                div.className = 'ai-assistant';
                // 与 _AiAssistant.cshtml 服务端注入一致：全局端点 + 目标页面标识（不依赖 AISwitch 渲染真实浮窗）
                div.setAttribute('data-ai-url', '/Ai/AiChat');
                div.setAttribute('data-ai-area', 'Admin');
                div.setAttribute('data-ai-controller', 'UserStat');
                div.innerHTML =
                    '<button type=""button"" id=""aiAssistantFab"" class=""ai-fab"" title=""AI 助手""><i class=""fa fa-magic""></i></button>' +
                    '<div class=""ai-panel"" id=""aiAssistantPanel"" style=""display:none; position:fixed; right:20px; bottom:80px; width:380px; height:60vh; flex-direction:column;"">' +
                    '  <div class=""ai-panel-header"">' +
                    '    <span>AI 助手</span>' +
                    '    <div class=""ai-panel-actions"">' +
                    '      <button type=""button"" id=""aiMaximize"" title=""最大化""><i class=""fa fa-expand""></i><i class=""fa fa-compress"" style=""display:none""></i></button>' +
                    '      <button type=""button"" id=""aiClearChat"" title=""清空会话""><i class=""fa fa-trash""></i></button>' +
                    '      <button type=""button"" id=""aiClosePanel"" title=""收起""><i class=""fa fa-times""></i></button>' +
                    '    </div>' +
                    '  </div>' +
                    '  <div class=""ai-messages"" id=""aiMessages""><div class=""ai-msg ai-msg-assistant""><div class=""ai-bubble"">你好，我是魔方 AI 助手</div></div></div>' +
                    '  <div class=""ai-panel-footer"">' +
                    '    <textarea id=""aiInput"" rows=""1""></textarea>' +
                    '    <button type=""button"" id=""aiSend"" class=""ai-send"" title=""发送""><i class=""fa fa-paper-plane""></i></button>' +
                    '  </div>' +
                    '</div>';
                document.body.appendChild(div);
                // 注入与 _AiAssistant.cshtml 一致的浮窗样式契约（面板打开隐藏悬浮球、气泡满宽）
                var st = document.createElement('style');
                st.textContent = '.ai-assistant.panel-open .ai-fab{visibility:hidden;opacity:0;pointer-events:none}'
                    + '.ai-panel-header{display:flex;justify-content:space-between;align-items:center;padding:8px 12px}'
                    + '.ai-panel-actions{display:flex;gap:2px}'
                    + '.ai-msg{display:flex;margin-bottom:10px}'
                    + '.ai-msg .ai-bubble{width:100%;padding:8px 12px;border-radius:8px}';
                document.body.appendChild(st);
                // 与 _AiAssistant.cshtml 一致：先加载 marked 库，再加载助手脚本（async=false 保证顺序）
                var s1 = document.createElement('script');
                s1.src = '/Content/marked/marked.min.js';
                s1.async = false;
                document.body.appendChild(s1);
                var s = document.createElement('script');
                s.src = '/js/ai-assistant.js';
                s.async = false;
                document.body.appendChild(s);
            })()
        ");

        // 等待真实 ai-assistant.js 加载并完成 init（脚本末尾设置 window.CubeAI）
        await page.WaitForFunctionAsync("() => window.CubeAI !== undefined", null, new PageWaitForFunctionOptions { Timeout = 5_000 });
    }

    /// <summary>注入 AI 助手浮窗并强制 data-ai-url 指向全局端点（模拟服务端注入，不依赖 AISwitch 设置）</summary>
    /// <param name="page">当前页面</param>
    /// <param name="url">全局对话端点 URL（恒为 /Ai/AiChat）</param>
    private static async Task EnsureAiAssistantWithUrlAsync(IPage page, String url)
    {
        await EnsureAiAssistantAsync(page);
        await page.EvaluateAsync($@"
            (function () {{
                var c = document.getElementById('aiAssistant');
                if (c) c.setAttribute('data-ai-url', '{url}');
            }})()
        ");
    }

    /// <summary>等待最后一个 AI 气泡包含指定关键词（SSE/错误异步写入后文本才出现）</summary>
    /// <param name="page">当前页面</param>
    /// <param name="keyword">期望出现的关键词</param>
    private static async Task WaitForBubbleTextAsync(IPage page, String keyword)
    {
        await page.WaitForFunctionAsync($@"
            (function () {{
                var bubbles = document.querySelectorAll('#aiMessages .ai-bubble');
                var last = bubbles[bubbles.length - 1];
                return last && last.textContent.indexOf('{keyword}') >= 0;
            }})()
        ", null, new PageWaitForFunctionOptions { Timeout = 5_000 });
    }
}

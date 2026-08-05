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
        await EnsureAiAssistantAsync(_page);

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

        await fab.ClickAsync();
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
                div.innerHTML =
                    '<button type=""button"" id=""aiAssistantFab"" class=""ai-fab"" title=""AI 助手""><i class=""fa fa-magic""></i></button>' +
                    '<div class=""ai-panel"" id=""aiAssistantPanel"" style=""display:none; position:fixed; right:20px; bottom:80px; width:380px; height:60vh;"">' +
                    '  <div class=""ai-panel-header"">' +
                    '    <span>AI 助手</span>' +
                    '    <div class=""ai-panel-actions"">' +
                    '      <button type=""button"" id=""aiClearChat"" title=""清空会话""><i class=""fa fa-trash""></i></button>' +
                    '      <button type=""button"" id=""aiClosePanel"" title=""收起""><i class=""fa fa-times""></i></button>' +
                    '    </div>' +
                    '  </div>' +
                    '  <div class=""ai-messages"" id=""aiMessages""></div>' +
                    '  <div class=""ai-panel-footer"">' +
                    '    <textarea id=""aiInput"" rows=""1""></textarea>' +
                    '    <button type=""button"" id=""aiSend"" class=""ai-send"" title=""发送""><i class=""fa fa-paper-plane""></i></button>' +
                    '  </div>' +
                    '</div>';
                document.body.appendChild(div);
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
}

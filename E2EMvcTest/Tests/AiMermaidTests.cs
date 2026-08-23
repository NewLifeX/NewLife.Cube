using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>AI 助手 Mermaid 图表渲染回归测试：CDN 懒加载 + 渲染 SVG + 失败回退源码 + 源码切换</summary>
/// <remarks>
/// 不依赖真实 LLM 与真实 mermaid CDN，全部用 mock 确定性验证：
/// - 拦截 AiChat 返回含 ```mermaid 代码块的假 SSE（content_delta 流式）
/// - 拦截 mermaid.min.js 返回 mock 脚本（window.mermaid 的 initialize/parse/render），离线可跑
/// - mock parse 对含 "INVALID" 的代码返回 false，用于验证解析失败回退源码
/// </remarks>
[Collection("E2E")]
public sealed class AiMermaidTests : IAsyncLifetime
{
    private readonly AppFixture _fixture;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public AiMermaidTests(AppFixture fixture) => _fixture = fixture;

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

    /// <summary>mock mermaid 脚本：暴露 window.mermaid，parse 对含 "INVALID" 的代码返回 false，render 返回固定 SVG</summary>
    private const String MockMermaidJs = @"
window.mermaid = {
    initialize: function () {},
    parse: function (code) { return Promise.resolve(code.indexOf('INVALID') < 0); },
    render: function (id, code) { return Promise.resolve({ svg: '<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 120 60"" width=""120"" height=""60""><rect width=""120"" height=""60"" fill=""#eef2ff""></rect></svg>' }); }
};
";

    /// <summary>JSON 字符串转义（用于构造 SSE data 行）</summary>
    private static String JsonEscape(String s) => s
        .Replace("\\", "\\\\")
        .Replace("\"", "\\\"")
        .Replace("\r", "\\r")
        .Replace("\n", "\\n");

    /// <summary>构造含 mermaid 代码块的 SSE 响应体：单个 content_delta + done</summary>
    private static String BuildMermaidSse(String prefix, String mermaidCode)
    {
        var content = prefix + "\n\n```mermaid\n" + mermaidCode + "\n```\n\n完";
        var json = "{\"type\":\"content_delta\",\"content\":\"" + JsonEscape(content) + "\"}";
        return "data: " + json + "\n\ndata: {\"type\":\"done\"}\n\n";
    }

    /// <summary>拦截 mermaid.min.js（CDN）返回 mock 脚本；返回 false 表示模拟 CDN 不可用（404）</summary>
    private async Task RouteMockMermaidAsync(Boolean cdnUnavailable = false)
    {
        await _page.RouteAsync("**/mermaid.min.js", async route =>
        {
            if (cdnUnavailable)
            {
                await route.FulfillAsync(new RouteFulfillOptions { Status = 404 });
                return;
            }
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "application/javascript",
                Body = MockMermaidJs,
            });
        });
    }

    /// <summary>打开面板并发送一条消息，等待 AI 回复流式结束</summary>
    private async Task SendAndWaitDoneAsync()
    {
        await _page.Locator("#aiAssistantFab").ClickAsync(new LocatorClickOptions { Force = true });
        var panel = _page.Locator("#aiAssistantPanel");
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });
        await _page.Locator("#aiInput").FillAsync("画一个流程图");
        await _page.Locator("#aiSend").ClickAsync();
        // 等待 mock SSE done 后 AI 回复气泡出现（流式结束后 renderMermaidBlocks 异步执行）
        await _page.Locator(".ai-msg-assistant .ai-bubble pre code.language-mermaid, .ai-msg-assistant .ai-mermaid")
            .First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Attached,
                Timeout = 10_000,
            });
    }

    [Fact(DisplayName = "TC-AI-030 合法 mermaid 代码块渲染为 SVG")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P1")]
    public async Task TC_AI_030_ValidMermaidRendersSvg()
    {
        const String testId = "TC-AI-030";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/UserStat");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        await EnsureAiAssistantAsync(_page, "Admin", "UserStat", includeQuick: false);

        var mermaidCode = "flowchart TD\n    A[开始] --> B{判断}\n    B -->|是| C[处理]\n    B -->|否| D[结束]";
        await _page.RouteAsync("**/AiChat", async route =>
        {
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "text/event-stream",
                Body = BuildMermaidSse("这是系统流程：", mermaidCode),
            });
        });
        await RouteMockMermaidAsync();

        await SendAndWaitDoneAsync();

        // 流式结束后 mermaid 代码块被替换为图表 SVG
        var svg = _page.Locator(".ai-msg-assistant .ai-mermaid svg");
        await svg.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 10_000,
        });
        Assert.True(await svg.IsVisibleAsync(), $"[{testId}] Mermaid SVG 未渲染");

        // 源码代码块已被图表替换，不再残留
        var sourceCount = await _page.Locator(".ai-msg-assistant pre code.language-mermaid").CountAsync();
        Assert.Equal(0, sourceCount);

        // 源码切换控件存在
        var summary = _page.Locator(".ai-msg-assistant .ai-mermaid details summary");
        Assert.True(await summary.CountAsync() > 0, $"[{testId}] 缺少「查看源码」切换");
        Assert.Equal("查看源码", await summary.First.InnerTextAsync());
        Assert.Empty(pageErrors);

        await _page.UnrouteAsync("**/AiChat");
        await _page.UnrouteAsync("**/mermaid.min.js");
    }

    [Fact(DisplayName = "TC-AI-031 非法 mermaid 代码回退源码展示")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P1")]
    public async Task TC_AI_031_InvalidMermaidFallsBackToSource()
    {
        const String testId = "TC-AI-031";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/UserStat");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        await EnsureAiAssistantAsync(_page, "Admin", "UserStat", includeQuick: false);

        // 含 INVALID 的非法 mermaid 代码，mock parse 返回 false
        var mermaidCode = "flowchart TD\n    A[开始] --> B[INVALID 非法语法]";
        await _page.RouteAsync("**/AiChat", async route =>
        {
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "text/event-stream",
                Body = BuildMermaidSse("以下是图：", mermaidCode),
            });
        });
        await RouteMockMermaidAsync();

        await SendAndWaitDoneAsync();

        // 解析失败回退：源码代码块保留可见，无图表占位
        var source = _page.Locator(".ai-msg-assistant pre code.language-mermaid");
        await source.First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 10_000,
        });
        Assert.True(await source.First.IsVisibleAsync(), $"[{testId}] 非法 mermaid 未回退源码");
        var text = await source.First.InnerTextAsync();
        Assert.True(text.Contains("flowchart"), $"[{testId}] 回退源码内容缺失：'{text}'");
        Assert.Equal(0, await _page.Locator(".ai-msg-assistant .ai-mermaid").CountAsync());
        Assert.Empty(pageErrors);

        await _page.UnrouteAsync("**/AiChat");
        await _page.UnrouteAsync("**/mermaid.min.js");
    }

    [Fact(DisplayName = "TC-AI-032 图表源码切换可展开")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P2")]
    public async Task TC_AI_032_SourceToggleExpandable()
    {
        const String testId = "TC-AI-032";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/UserStat");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        await EnsureAiAssistantAsync(_page, "Admin", "UserStat", includeQuick: false);

        var mermaidCode = "sequenceDiagram\n    Alice->>John: 你好\n    John-->>Alice: 你好呀";
        await _page.RouteAsync("**/AiChat", async route =>
        {
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "text/event-stream",
                Body = BuildMermaidSse("时序如下：", mermaidCode),
            });
        });
        await RouteMockMermaidAsync();

        await SendAndWaitDoneAsync();

        var svg = _page.Locator(".ai-msg-assistant .ai-mermaid svg");
        await svg.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 10_000,
        });

        // 点击「查看源码」展开，内部 pre 显示原始 mermaid 源码
        var summary = _page.Locator(".ai-msg-assistant .ai-mermaid details summary");
        await summary.First.ClickAsync();
        var srcPre = _page.Locator(".ai-msg-assistant .ai-mermaid details pre");
        await srcPre.First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });
        var srcText = await srcPre.First.InnerTextAsync();
        Assert.True(srcText.Contains("sequenceDiagram"), $"[{testId}] 源码内容缺失：'{srcText}'");
        Assert.Empty(pageErrors);

        await _page.UnrouteAsync("**/AiChat");
        await _page.UnrouteAsync("**/mermaid.min.js");
    }

    [Fact(DisplayName = "TC-AI-033 CDN 不可用时回退源码")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P2")]
    public async Task TC_AI_033_CdnUnavailableFallsBackToSource()
    {
        const String testId = "TC-AI-033";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/UserStat");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        await EnsureAiAssistantAsync(_page, "Admin", "UserStat", includeQuick: false);

        var mermaidCode = "flowchart TD\n    A --> B";
        await _page.RouteAsync("**/AiChat", async route =>
        {
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "text/event-stream",
                Body = BuildMermaidSse("图如下：", mermaidCode),
            });
        });
        await RouteMockMermaidAsync(cdnUnavailable: true);

        await SendAndWaitDoneAsync();

        // CDN 404：mermaid 加载失败，源码代码块保持展示，无图表占位
        var source = _page.Locator(".ai-msg-assistant pre code.language-mermaid");
        await source.First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 10_000,
        });
        Assert.True(await source.First.IsVisibleAsync(), $"[{testId}] CDN 不可用时未回退源码");
        Assert.Equal(0, await _page.Locator(".ai-msg-assistant .ai-mermaid").CountAsync());
        Assert.Empty(pageErrors);

        await _page.UnrouteAsync("**/AiChat");
        await _page.UnrouteAsync("**/mermaid.min.js");
    }

    #region 辅助：注入 AI 助手（与 AiBasicFeaturesTests 同款）
    /// <summary>注入 AI 助手浮窗 DOM 与真实 ai-assistant.js，不依赖 AISwitch 设置</summary>
    private static async Task EnsureAiAssistantAsync(IPage page, String area, String controller, Boolean includeQuick)
    {
        var exists = await page.EvaluateAsync<Boolean>("!!document.getElementById('aiAssistantFab')");
        if (exists) return;

        await page.EvaluateAsync($@"
            (function () {{
                var div = document.createElement('div');
                div.id = 'aiAssistant';
                div.className = 'ai-assistant';
                div.setAttribute('data-ai-url', '/Ai/AiChat');
                div.setAttribute('data-ai-area', '{area}');
                div.setAttribute('data-ai-controller', '{controller}');
                var quick = {includeQuick.ToString().ToLower()} ? (
                    '<div class=""ai-quick"" id=""aiQuickActions"">' +
                    '  <button type=""button"" class=""ai-chip"" data-prompt=""分析当前列表数据"">📊 分析当前数据</button>' +
                    '  <button type=""button"" class=""ai-chip"" data-prompt=""帮我填写当前表单"">📝 帮我填表</button>' +
                    '  <button type=""button"" class=""ai-chip"" data-prompt=""分析当前记录"">🔍 分析当前记录</button>' +
                    '  <button type=""button"" class=""ai-chip"" data-prompt=""检查系统运行状态"">🩺 系统诊断</button>' +
                    '</div>'
                ) : '';
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
                    '  <div class=""ai-messages"" id=""aiMessages"">' +
                    '    <div class=""ai-msg ai-msg-assistant""><div class=""ai-bubble"">你好，我是魔方 AI 助手</div></div>' +
                    quick +
                    '  </div>' +
                    '  <div class=""ai-panel-footer"">' +
                    '    <textarea id=""aiInput"" rows=""1""></textarea>' +
                    '    <button type=""button"" id=""aiSend"" class=""ai-send"" title=""发送""><i class=""fa fa-paper-plane""></i></button>' +
                    '  </div>' +
                    '</div>';
                document.body.appendChild(div);
                var st = document.createElement('style');
                st.textContent = '.ai-assistant.panel-open .ai-fab{{visibility:hidden;opacity:0;pointer-events:none}}'
                    + '.ai-panel-header{{display:flex;justify-content:space-between;align-items:center;padding:8px 12px}}'
                    + '.ai-panel-actions{{display:flex;gap:2px}}'
                    + '.ai-msg{{display:flex;margin-bottom:10px}}'
                    + '.ai-msg .ai-bubble{{width:100%;padding:8px 12px;border-radius:8px}}';
                document.body.appendChild(st);
                var s1 = document.createElement('script');
                s1.src = '/Content/marked/marked.min.js';
                s1.async = false;
                document.body.appendChild(s1);
                var s = document.createElement('script');
                s.src = '/js/ai-assistant.js';
                s.async = false;
                document.body.appendChild(s);
            }})()
        ");

        // 等待真实 ai-assistant.js 加载并完成 init（脚本末尾设置 window.CubeAI）
        await page.WaitForFunctionAsync("() => window.CubeAI !== undefined", null, new PageWaitForFunctionOptions { Timeout = 5_000 });
    }
    #endregion
}

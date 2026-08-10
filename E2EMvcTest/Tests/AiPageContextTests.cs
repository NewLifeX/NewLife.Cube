using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using NewLife.Cube.AI;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>AI 页面上下文采集与 run_js 回传链路测试</summary>
/// <remarks>
/// 验证 get_page_context 浏览器采集层依赖的两段关键链路：
/// ① 标准采集脚本在真实 Cube 页面可执行并返回结构化上下文契约（标题/表格/表单等键）；
/// ② 前端对后端下发的 run_js 事件执行脚本并 POST 回传检查点结果（事件总线检查点依赖此回传）。
/// 无需真实 LLM：AiChat 端点以 mock SSE 模拟后端下发。
/// </remarks>
[Collection("E2E")]
public sealed class AiPageContextTests : IAsyncLifetime
{
    private readonly AppFixture _fixture;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public AiPageContextTests(AppFixture fixture) => _fixture = fixture;

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

    [Fact(DisplayName = "TC-PC-010 采集脚本与 run_js 回传链路")]
    [Trait("Category", "AiPageContext")]
    [Trait("Priority", "P1")]
    public async Task TC_PC_010_CollectorAndRunJs()
    {
        const String testId = "TC-PC-010";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/UserStat");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // ① 执行框架标准采集脚本（单一事实源：PageContextCollector.BuildScript），断言返回完整 JSON 契约。
        // 不依赖页面具体内容（权限模型差异下页面可能为拒绝提示），只验证脚本可执行且契约完整。
        var script = PageContextCollector.BuildScript();
        var json = await _page.EvaluateAsync<String>("JSON.stringify(" + script + ")");

        Assert.False(String.IsNullOrEmpty(json), $"[{testId}] 采集脚本未返回结果");
        foreach (var key in new[] { "\"title\"", "\"url\"", "\"path\"", "\"headings\"", "\"tables\"", "\"forms\"", "\"dataAttrs\"", "\"hints\"" })
        {
            Assert.True(json.Contains(key, StringComparison.Ordinal), $"[{testId}] 采集结果缺少键 {key}，结果={json.Substring(0, Math.Min(json.Length, 200))}");
        }

        // ② run_js 回传链路：mock AiChat SSE 下发 run_js 事件，前端执行后应 POST 回传检查点结果
        String? posted = null;
        await _page.RouteAsync("**/Ai/OperationResult", async route =>
        {
            posted = route.Request.PostData;
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "application/json",
                Body = "{\"code\":0}",
            });
        });

        // mock AiChat SSE：checkpointId 与工具调用 ID 一致（即后端 ToolCallContext.ToolCallId）
        var sse = "data: {\"type\":\"tool\",\"event\":\"start\",\"id\":\"call_pc1\",\"name\":\"run_js\",\"value\":\"{}\"}\n\n"
                + "data: {\"type\":\"run_js\",\"checkpointId\":\"call_pc1\",\"script\":\"return document.title;\"}\n\n";
        await _page.RouteAsync("**/AiChat", async route =>
        {
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "text/event-stream",
                Body = sse,
            });
        });

        var fab = _page.Locator("#aiAssistantFab");
        await EnsureAiAssistantAsync(_page);

        await fab.ClickAsync();
        var panel = _page.Locator("#aiAssistantPanel");
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });
        await _page.Locator("#aiInput").FillAsync("读取页面标题");
        await _page.Locator("#aiSend").ClickAsync();

        // 等待回传（脚本执行 + POST 回传为异步链路）
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (posted == null && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
        }

        Assert.NotNull(posted);
        Assert.True(posted.Contains("call_pc1", StringComparison.Ordinal), $"[{testId}] 回传缺少检查点 call_pc1，实际回传={posted}");
        // 脚本已真实执行：回传 result 内的 value 应为页面标题（执行结果而非脚本源码）
        var title = await _page.TitleAsync();
        Assert.True(posted.Contains(title, StringComparison.Ordinal), $"[{testId}] 回传 value 不等于页面标题，实际回传={posted}");
        Assert.Empty(pageErrors);

        await _page.UnrouteAsync("**/AiChat");
        await _page.UnrouteAsync("**/Ai/OperationResult");
    }

    /// <summary>注入 AI 助手浮窗标记并加载真实 ai-assistant.js，使测试不依赖 AISwitch 设置</summary>
    /// <remarks>markup 仅含 JS 契约所需的元素 ID，等待 window.CubeAI 出现表示脚本已加载且 init 已执行</remarks>
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
                    '<div class=""ai-panel"" id=""aiAssistantPanel"" style=""display:none; position:fixed; right:20px; bottom:80px; width:380px; height:60vh; flex-direction:column;"">' +
                    '  <div class=""ai-panel-header""><span>AI 助手</span>' +
                    '    <div class=""ai-panel-actions"">' +
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
                var st = document.createElement('style');
                st.textContent = '.ai-assistant.panel-open .ai-fab{visibility:hidden;opacity:0;pointer-events:none}'
                    + '.ai-panel-header{display:flex;justify-content:space-between;align-items:center;padding:8px 12px}'
                    + '.ai-panel-actions{display:flex;gap:2px}'
                    + '.ai-msg{display:flex;margin-bottom:10px}'
                    + '.ai-msg .ai-bubble{width:100%;padding:8px 12px;border-radius:8px}';
                document.body.appendChild(st);
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

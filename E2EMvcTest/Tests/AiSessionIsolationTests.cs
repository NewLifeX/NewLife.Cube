using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>AI 会话按页面隔离回归测试 — 同页多轮共享、跨页隔离、返回恢复、清空只作用当前页</summary>
/// <remarks>
/// 验证会话号作用域（sessionStorage + 页面路径）：
/// - 同页面多轮提问共享同一会话号
/// - 不同页面（不同 URL）使用不同会话号，互不串话
/// - 返回已访问页面恢复该页会话
/// - 清空会话仅重置当前页面会话，不影响其他页面
/// </remarks>
[Collection("E2E")]
public sealed class AiSessionIsolationTests : IAsyncLifetime
{
    private readonly AppFixture _fixture;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public AiSessionIsolationTests(AppFixture fixture) => _fixture = fixture;

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

    [Fact(DisplayName = "TC-AI-028 同页多轮共享会话，跨页隔离，返回恢复")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P1")]
    public async Task TC_AI_028_PageSessionIsolation()
    {
        const String testId = "TC-AI-028";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        // 捕获 AiChat 请求体，返回假 SSE（不依赖真实 LLM）
        var bodies = new List<String>();
        await _page.RouteAsync("**/AiChat", async route =>
        {
            bodies.Add(route.Request.PostData ?? "");
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "text/event-stream",
                Body = "data: {\"type\":\"text\",\"content\":\"ok\"}\n\ndata: {\"type\":\"done\"}\n\n",
            });
        });

        // 页面 A：/Admin/UserStat，注入助手并打开面板
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/UserStat");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        await EnsureAiAssistantAsync(_page, "Admin", "UserStat", false);
        await OpenPanelAsync();

        // 同页多轮：两次提问共享同一会话号
        await SendAsync("第一轮问题", bodies, 1);
        var sidA1 = GetSessionId(bodies[0]);
        await SendAsync("第二轮问题", bodies, 2);
        var sidA2 = GetSessionId(bodies[1]);
        Assert.False(String.IsNullOrEmpty(sidA1), $"[{testId}] 会话号不应为空");
        Assert.Equal(sidA1, sidA2);
        // 请求体携带页面路径（后端按用户+url 作用域会话键）
        Assert.Equal("/Admin/UserStat", GetField(bodies[0], "url"));

        // 页面 A 的 sessionStorage 键存在且与会话号一致
        var keyA = await GetPageSessionKeyAsync();
        Assert.Equal(sidA1, keyA);

        // 页面 B：/Admin/User，跨页会话号应不同
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        await EnsureAiAssistantAsync(_page, "Admin", "User", false);
        await OpenPanelAsync();
        await SendAsync("另一页面问题", bodies, 3);
        var sidB = GetSessionId(bodies[2]);
        Assert.NotEqual(sidA1, sidB);
        // 另一页面 url 不同
        Assert.Equal("/Admin/User", GetField(bodies[2], "url"));

        // 返回页面 A：会话恢复（sessionStorage 保留）
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/UserStat");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        await EnsureAiAssistantAsync(_page, "Admin", "UserStat", false);
        await OpenPanelAsync();
        await SendAsync("返回后的问题", bodies, 4);
        var sidA3 = GetSessionId(bodies[3]);
        Assert.Equal(sidA1, sidA3);

        Assert.Empty(pageErrors);
        await _page.UnrouteAsync("**/AiChat");
    }

    [Fact(DisplayName = "TC-AI-029 清空会话仅重置当前页，不影响其他页面")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P2")]
    public async Task TC_AI_029_ClearChatOnlyCurrentPage()
    {
        const String testId = "TC-AI-029";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        var bodies = new List<String>();
        await _page.RouteAsync("**/AiChat", async route =>
        {
            bodies.Add(route.Request.PostData ?? "");
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "text/event-stream",
                Body = "data: {\"type\":\"text\",\"content\":\"ok\"}\n\ndata: {\"type\":\"done\"}\n\n",
            });
        });

        // 页面 A：先建立会话
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/UserStat");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        await EnsureAiAssistantAsync(_page, "Admin", "UserStat", false);
        await OpenPanelAsync();
        await SendAsync("清空前问题", bodies, 1);
        var sidBefore = GetSessionId(bodies[0]);
        var keyBefore = await GetPageSessionKeyAsync();
        Assert.Equal(sidBefore, keyBefore);

        // 清空会话：当前页会话号重置
        await _page.Locator("#aiClearChat").ClickAsync();
        var keyAfter = await GetPageSessionKeyAsync();
        Assert.NotEqual(keyBefore, keyAfter);

        // 清空后新会话延续（多轮共享新会话号）
        await SendAsync("清空后问题", bodies, 2);
        var sidAfter = GetSessionId(bodies[1]);
        Assert.NotEqual(sidBefore, sidAfter);
        Assert.Equal(keyAfter, sidAfter);

        // 其他页面不受影响：切到 /Admin/User 再返回，页面 A 会话仍是清空后的新会话
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        await EnsureAiAssistantAsync(_page, "Admin", "User", false);
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/UserStat");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        await EnsureAiAssistantAsync(_page, "Admin", "UserStat", false);
        await OpenPanelAsync();
        await SendAsync("返回后问题", bodies, 3);
        var sidBack = GetSessionId(bodies[2]);
        Assert.Equal(keyAfter, sidBack);

        Assert.Empty(pageErrors);
        await _page.UnrouteAsync("**/AiChat");
    }

    #region 辅助

    /// <summary>打开 AI 助手面板（点击悬浮球并等待面板可见）</summary>
    private async Task OpenPanelAsync()
    {
        await _page.Locator("#aiAssistantFab").ClickAsync(new LocatorClickOptions { Force = true });
        await _page.Locator("#aiAssistantPanel").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });
    }

    /// <summary>发送消息并等待请求被捕获</summary>
    /// <param name="message">消息内容</param>
    /// <param name="bodies">请求体列表</param>
    /// <param name="count">期望捕获数量</param>
    private async Task SendAsync(String message, List<String> bodies, Int32 count)
    {
        await _page.Locator("#aiInput").FillAsync(message);
        await _page.Locator("#aiSend").ClickAsync();
        await WaitForRequestAsync(bodies, count);
        Assert.True(bodies.Count >= count, $"未捕获到 AiChat 请求（期望 {count}，实际 {bodies.Count}）");
    }

    /// <summary>读取当前页面作用域的会话号（sessionStorage）</summary>
    private async Task<String> GetPageSessionKeyAsync()
    {
        return await _page.EvaluateAsync<String>(
            "sessionStorage.getItem('cube-ai-session:' + (location.pathname || '/')) || ''");
    }

    /// <summary>从 AiChat 请求体提取字段值</summary>
    private static String GetField(String body, String name)
    {
        if (String.IsNullOrEmpty(body)) return "";
        var req = JsonSerializer.Deserialize<Dictionary<String, Object?>>(body);
        return req != null && req.TryGetValue(name, out var v) ? v?.ToString() ?? "" : "";
    }

    /// <summary>从 AiChat 请求体提取会话号</summary>
    private static String GetSessionId(String body) => GetField(body, "sessionId");

    /// <summary>等待请求被捕获（轮询，最多 5 秒）</summary>
    private static async Task WaitForRequestAsync(List<String> bodies, Int32 count)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (bodies.Count < count && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
        }
    }

    /// <summary>注入 AI 助手浮窗标记并加载真实 ai-assistant.js，不依赖 AISwitch 设置</summary>
    /// <remarks>markup 与 _AiAssistant.cshtml 服务端注入契约一致：全局端点 + 目标页面标识。</remarks>
    /// <param name="page">当前页面</param>
    /// <param name="area">目标页面区域</param>
    /// <param name="controller">目标页面控制器名</param>
    /// <param name="includeQuick">是否包含快捷指令容器</param>
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

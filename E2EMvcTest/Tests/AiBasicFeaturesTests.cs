using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>AI 基础功能回归测试：快捷指令页面适配、清空会话保留快捷指令、非实体页面支持基础分析</summary>
/// <remarks>
/// 覆盖"AI 对话基础功能"的确定性行为（不依赖真实 LLM，用 mock SSE）：
/// - 清空会话后快捷指令保留（回归：innerHTML 替换会销毁 #aiQuickActions）
/// - 快捷指令按页面类型适配：实体列表/表单 vs 非实体页面（魔方设置/服务器信息/数据库信息）
/// - 非实体页面注入 AI 助手且 get_page_context 采集链路可用（设计预期：支持各项基础功能分析）
/// </remarks>
[Collection("E2E")]
public sealed class AiBasicFeaturesTests : IAsyncLifetime
{
    private readonly AppFixture _fixture;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public AiBasicFeaturesTests(AppFixture fixture) => _fixture = fixture;

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

    [Fact(DisplayName = "TC-AI-021 清空会话后快捷指令保留")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P1")]
    public async Task TC_AI_021_ClearChatKeepsQuickActions()
    {
        const String testId = "TC-AI-021";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/UserStat");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        await EnsureAiAssistantAsync(_page, "Admin", "UserStat", includeQuick: true);

        var fab = _page.Locator("#aiAssistantFab");
        await fab.ClickAsync(new LocatorClickOptions { Force = true });
        var panel = _page.Locator("#aiAssistantPanel");
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });

        // 清空前：快捷指令存在
        Assert.True(await _page.Locator("#aiQuickActions").CountAsync() > 0, $"[{testId}] 清空前快捷指令应存在");
        Assert.True(await _page.Locator(".ai-chip").First.IsVisibleAsync(), $"[{testId}] 清空前快捷指令应可见");

        // 点击清空会话
        await _page.Locator("#aiClearChat").ClickAsync();
        await Task.Delay(300);

        // 清空后：快捷指令保留且可见（回归：旧实现 innerHTML 替换会销毁 #aiQuickActions）
        Assert.True(await _page.Locator("#aiQuickActions").CountAsync() > 0, $"[{testId}] 清空会话后快捷指令应保留（回归 bug）");
        Assert.True(await _page.Locator("#aiQuickActions").IsVisibleAsync(), $"[{testId}] 清空会话后快捷指令应可见（回归 bug）");
        var chips = await _page.Locator(".ai-chip").CountAsync();
        Assert.True(chips >= 2, $"[{testId}] 清空会话后快捷指令应保留：实际 {chips} 个");

        // 欢迎语已恢复
        var bubble = await _page.Locator("#aiMessages .ai-bubble").Last.InnerTextAsync();
        Assert.True(bubble.Contains("会话已清空"), $"[{testId}] 未显示清空提示：'{bubble}'");
        Assert.Empty(pageErrors);
    }

    [Fact(DisplayName = "TC-AI-022 实体列表页快捷指令页面适配与请求")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P1")]
    public async Task TC_AI_022_ListPageQuickActionsAndRequest()
    {
        const String testId = "TC-AI-022";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/UserStat");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 实体列表页标记由视图渲染（与 AISwitch 无关）
        Assert.Equal("list", await _page.Locator("#aiPage").InputValueAsync());

        await EnsureAiAssistantAsync(_page, "Admin", "UserStat", includeQuick: true);

        // 拦截 AiChat 捕获请求体，返回假 SSE
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

        var fab = _page.Locator("#aiAssistantFab");
        await fab.ClickAsync(new LocatorClickOptions { Force = true });
        var panel = _page.Locator("#aiAssistantPanel");
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });

        // 列表页：分析当前数据可见，帮我填表/分析当前记录隐藏
        Assert.True(await ChipVisibleAsync("分析当前列表数据"), $"[{testId}] 列表页应显示「分析当前数据」");
        Assert.False(await ChipVisibleAsync("帮我填写当前表单"), $"[{testId}] 列表页不应显示「帮我填表」");
        Assert.False(await ChipVisibleAsync("分析当前记录"), $"[{testId}] 列表页不应显示「分析当前记录」");
        Assert.True(await ChipVisibleAsync("检查系统运行状态"), $"[{testId}] 列表页应显示「系统诊断」");

        // 点击分析当前数据，断言请求体携带页面上下文
        await _page.Locator(".ai-chip[data-prompt='分析当前列表数据']").ClickAsync();
        await WaitForRequestAsync(bodies, 1);

        Assert.True(bodies.Count > 0, $"[{testId}] 未捕获到 AiChat 请求");
        var req = JsonSerializer.Deserialize<Dictionary<String, Object?>>(bodies[0]);
        Assert.NotNull(req);
        Assert.Equal("list", req!["page"]?.ToString());
        Assert.Equal("Admin", req["area"]?.ToString());
        Assert.Equal("UserStat", req["controller"]?.ToString());
        Assert.Equal("分析当前列表数据", req["message"]?.ToString());
        Assert.Empty(pageErrors);

        await _page.UnrouteAsync("**/AiChat");
    }

    [Fact(DisplayName = "TC-AI-023 实体表单页快捷指令页面适配与请求")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P1")]
    public async Task TC_AI_023_FormPageQuickActionsAndRequest()
    {
        const String testId = "TC-AI-023";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Add");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 实体表单页标记由视图渲染（与 AISwitch 无关）
        Assert.Equal("form", await _page.Locator("#aiPage").InputValueAsync());
        Assert.Equal("add", await _page.Locator("#aiMode").InputValueAsync());

        await EnsureAiAssistantAsync(_page, "Admin", "User", includeQuick: true);

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

        var fab = _page.Locator("#aiAssistantFab");
        await fab.ClickAsync(new LocatorClickOptions { Force = true });
        var panel = _page.Locator("#aiAssistantPanel");
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });

        // 表单页：帮我填表可见，分析当前数据隐藏，分析当前记录（新增模式）隐藏
        Assert.True(await ChipVisibleAsync("帮我填写当前表单"), $"[{testId}] 表单页应显示「帮我填表」");
        Assert.False(await ChipVisibleAsync("分析当前列表数据"), $"[{testId}] 表单页不应显示「分析当前数据」");
        Assert.False(await ChipVisibleAsync("分析当前记录"), $"[{testId}] 新增表单不应显示「分析当前记录」");
        Assert.True(await ChipVisibleAsync("检查系统运行状态"), $"[{testId}] 表单页应显示「系统诊断」");

        await _page.Locator(".ai-chip[data-prompt='帮我填写当前表单']").ClickAsync();
        await WaitForRequestAsync(bodies, 1);

        Assert.True(bodies.Count > 0, $"[{testId}] 未捕获到 AiChat 请求");
        var req = JsonSerializer.Deserialize<Dictionary<String, Object?>>(bodies[0]);
        Assert.NotNull(req);
        Assert.Equal("form", req!["page"]?.ToString());
        Assert.Equal("add", req["mode"]?.ToString());
        Assert.Equal("Admin", req["area"]?.ToString());
        Assert.Equal("User", req["controller"]?.ToString());
        Assert.Equal("帮我填写当前表单", req["message"]?.ToString());
        Assert.Empty(pageErrors);

        await _page.UnrouteAsync("**/AiChat");
    }

    [Fact(DisplayName = "TC-AI-024 非实体页面快捷指令页面适配与请求（设计预期）")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P1")]
    public async Task TC_AI_024_NonEntityPageQuickActionsAndRequest()
    {
        const String testId = "TC-AI-024";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        // 数据库信息页：典型非实体页面（无 #aiPage 标记）
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/Db");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        Assert.Equal(0, await _page.Locator("#aiPage").CountAsync());
        Assert.True(await _page.Locator("table").CountAsync() > 0, $"[{testId}] 数据库信息页应有数据表");

        await EnsureAiAssistantAsync(_page, "Admin", "Db", includeQuick: true);

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

        var fab = _page.Locator("#aiAssistantFab");
        await fab.ClickAsync(new LocatorClickOptions { Force = true });
        var panel = _page.Locator("#aiAssistantPanel");
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });

        // 非实体页：分析当前数据（页面分析，提示词已自动切换为"分析当前页面内容"）与系统诊断可见，填表/记录分析隐藏
        Assert.True(await ChipVisibleAsync("分析当前页面内容"), $"[{testId}] 非实体页应显示「分析当前数据」（提示词已适配为页面分析）");
        Assert.True(await ChipVisibleAsync("检查系统运行状态"), $"[{testId}] 非实体页应显示「系统诊断」");
        Assert.False(await ChipVisibleAsync("帮我填写当前表单"), $"[{testId}] 非实体页不应显示「帮我填表」");
        Assert.False(await ChipVisibleAsync("分析当前记录"), $"[{testId}] 非实体页不应显示「分析当前记录」");

        // 点击分析当前数据：非实体页提示词自动切换为"分析当前页面内容"，引导 LLM 调用 get_page_context
        await _page.Locator(".ai-chip[data-prompt='分析当前页面内容']").ClickAsync();
        await WaitForRequestAsync(bodies, 1);

        Assert.True(bodies.Count > 0, $"[{testId}] 未捕获到 AiChat 请求");
        var req = JsonSerializer.Deserialize<Dictionary<String, Object?>>(bodies[0]);
        Assert.NotNull(req);
        Assert.Equal("", req!["page"]?.ToString());
        Assert.Equal("Admin", req["area"]?.ToString());
        Assert.Equal("Db", req["controller"]?.ToString());
        Assert.Equal("分析当前页面内容", req["message"]?.ToString());
        Assert.Empty(pageErrors);

        await _page.UnrouteAsync("**/AiChat");
    }

    [Fact(DisplayName = "TC-AI-025 非实体页面 AI 助手注入与页面标记（魔方设置/服务器信息/数据库信息）")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P2")]
    public async Task TC_AI_025_NonEntityPageInjectionContract()
    {
        const String testId = "TC-AI-025";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        // 设计预期：魔方设置 / 服务器信息 / 数据库信息 等非实体页面也支持 AI 基础功能分析
        var pages = new Dictionary<String, String>
        {
            ["/Admin/Cube"] = "Cube",
            ["/Admin/Index/Main"] = "Index",
            ["/Admin/Db"] = "Db",
        };

        foreach (var kv in pages)
        {
            await PageHelpers.GotoAndWaitAsync(_page, kv.Key);
            await PageHelpers.AssertNoServerErrorAsync(_page, testId);

            // 非实体页面不应有实体页上下文标记（#aiPage 由实体列表/表单/详情视图渲染）
            var pageMarker = await _page.Locator("#aiPage").CountAsync();
            Assert.True(pageMarker == 0, $"[{testId}] 非实体页 {kv.Key} 不应有 #aiPage 标记");

            // AISwitch 开启时真实浮窗渲染，验证注入契约（端点/目标页面标识）
            var real = _page.Locator("#aiAssistant");
            if (await real.CountAsync() > 0)
            {
                var url = await real.GetAttributeAsync("data-ai-url");
                Assert.True(url == "/Ai/AiChat", $"[{testId}] {kv.Key} data-ai-url 应为全局端点，实际 '{url}'");
                var area = await real.GetAttributeAsync("data-ai-area");
                Assert.True(area == "Admin", $"[{testId}] {kv.Key} data-ai-area 应为 Admin，实际 '{area}'");
                var ctrl = await real.GetAttributeAsync("data-ai-controller");
                Assert.True(ctrl == kv.Value, $"[{testId}] {kv.Key} data-ai-controller 应为 {kv.Value}，实际 '{ctrl}'");
            }
        }

        Assert.Empty(pageErrors);
    }

    [Fact(DisplayName = "TC-AI-026 非实体页面 get_page_context 采集链路可用")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P2")]
    public async Task TC_AI_026_NonEntityPagePageContextChain()
    {
        const String testId = "TC-AI-026";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        // 数据库信息页（非实体）：get_page_context 经 run_js 检查点链路在浏览器采集页面数据
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/Db");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        await EnsureAiAssistantAsync(_page, "Admin", "Db", includeQuick: true);

        // 捕获 OperationResult 回传
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

        // mock AiChat SSE：下发 run_js 采集脚本，前端执行后应回传检查点结果
        var sse = "data: {\"type\":\"tool\",\"event\":\"start\",\"id\":\"call_pc2\",\"name\":\"run_js\",\"value\":\"{}\"}\n\n"
                + "data: {\"type\":\"run_js\",\"checkpointId\":\"call_pc2\",\"script\":\"return {tables: document.querySelectorAll('table').length, path: location.pathname};\"}\n\n";
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
        await fab.ClickAsync(new LocatorClickOptions { Force = true });
        var panel = _page.Locator("#aiAssistantPanel");
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });
        await _page.Locator("#aiInput").FillAsync("分析当前页面");
        await _page.Locator("#aiSend").ClickAsync();

        // 等待回传
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (posted == null && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
        }

        Assert.NotNull(posted);
        Assert.True(posted!.Contains("call_pc2", StringComparison.Ordinal), $"[{testId}] 回传缺少检查点 call_pc2：{posted}");
        Assert.True(posted.Contains("/Admin/Db", StringComparison.Ordinal), $"[{testId}] 采集脚本未在非实体页执行：{posted}");
        Assert.Empty(pageErrors);

        await _page.UnrouteAsync("**/AiChat");
        await _page.UnrouteAsync("**/Ai/OperationResult");
    }

    [Fact(DisplayName = "TC-AI-027 配置表单页（魔方设置）支持帮我填表")]
    [Trait("Category", "AiAssistant")]
    [Trait("Priority", "P1")]
    public async Task TC_AI_027_ConfigFormPageFillForm()
    {
        const String testId = "TC-AI-027";
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        // 魔方设置页：非实体页面（无 #aiPage）但含配置表单控件，应支持"帮我填表"
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/Cube");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        Assert.Equal(0, await _page.Locator("#aiPage").CountAsync());
        var inputCount = await _page.Locator("input[name]").CountAsync();
        Assert.True(inputCount > 0, $"[{testId}] 魔方设置页应有表单控件，实际 {inputCount} 个");

        await EnsureAiAssistantAsync(_page, "Admin", "Cube", includeQuick: true);

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

        var fab = _page.Locator("#aiAssistantFab");
        await fab.ClickAsync(new LocatorClickOptions { Force = true });
        var panel = _page.Locator("#aiAssistantPanel");
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });

        // 配置表单页（含表单控件）：帮我填表 / 分析当前数据 / 系统诊断 可见，记录分析隐藏
        Assert.True(await ChipVisibleAsync("帮我填写当前表单"), $"[{testId}] 魔方设置页应显示「帮我填表」（配置表单）");
        Assert.True(await ChipVisibleAsync("分析当前页面内容"), $"[{testId}] 魔方设置页应显示「分析当前数据」（页面分析）");
        Assert.True(await ChipVisibleAsync("检查系统运行状态"), $"[{testId}] 魔方设置页应显示「系统诊断」");
        Assert.False(await ChipVisibleAsync("分析当前记录"), $"[{testId}] 魔方设置页不应显示「分析当前记录」");

        // 点击帮我填表，断言请求体携带目标页面标识（非实体页 controller=Cube）
        await _page.Locator(".ai-chip[data-prompt='帮我填写当前表单']").ClickAsync();
        await WaitForRequestAsync(bodies, 1);

        Assert.True(bodies.Count > 0, $"[{testId}] 未捕获到 AiChat 请求");
        var req = JsonSerializer.Deserialize<Dictionary<String, Object?>>(bodies[0]);
        Assert.NotNull(req);
        Assert.Equal("", req!["page"]?.ToString());
        Assert.Equal("Admin", req["area"]?.ToString());
        Assert.Equal("Cube", req["controller"]?.ToString());
        Assert.Equal("帮我填写当前表单", req["message"]?.ToString());
        Assert.Empty(pageErrors);

        await _page.UnrouteAsync("**/AiChat");
    }

    #region 辅助

    /// <summary>检查指定提示词的快捷指令是否可见</summary>
    /// <param name="prompt">data-prompt 值</param>
    private async Task<Boolean> ChipVisibleAsync(String prompt)
    {
        var chip = _page.Locator($".ai-chip[data-prompt='{prompt}']");
        if (await chip.CountAsync() == 0) return false;
        return await chip.IsVisibleAsync();
    }

    /// <summary>等待请求被捕获（轮询，最多 5 秒）</summary>
    /// <param name="bodies">请求体列表</param>
    /// <param name="count">期望数量</param>
    private static async Task WaitForRequestAsync(List<String> bodies, Int32 count)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (bodies.Count < count && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
        }
    }

    /// <summary>注入 AI 助手浮窗标记并加载真实 ai-assistant.js（含快捷指令），不依赖 AISwitch 设置</summary>
    /// <remarks>markup 与 _AiAssistant.cshtml 服务端注入契约一致：全局端点 + 目标页面标识 + 快捷指令。</remarks>
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

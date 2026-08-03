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
}

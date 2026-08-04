using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>AI 入口统一回归测试（AiInsight 已合并到 AiChat）</summary>
/// <remarks>AI 洞察已随重构合并到 AI 对话助手（AiChat），高级菜单「AI 分析」入口移除。测试验证：
/// 高级菜单不再包含「AI 分析」、AI 助手浮窗在列表页渲染（AISwitch 开启时）、无未捕获 JS 异常。
/// 当 AISwitch 未开启时浮窗不渲染，仅验证高级菜单部分。</remarks>
[Collection("E2E")]
public sealed class AiInsightTests : IAsyncLifetime
{
    private readonly AppFixture _fixture;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public AiInsightTests(AppFixture fixture) => _fixture = fixture;

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

    [Fact(DisplayName = "TC-AI-001 AI 入口统一：高级菜单无 AI 分析、浮窗助手可用、无 JS 报错")]
    [Trait("Category", "AiInsight")]
    [Trait("Priority", "P1")]
    public async Task TC_AI_001_AiEntryUnified()
    {
        const String testId = "TC-AI-001";

        // 收集页面未捕获 JS 异常
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/UserStat");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 1. 高级菜单存在且不再包含「AI 分析」（已合并到 AI 助手浮窗）
        var advBtn = _page.Locator("button:has-text('高级')").First;
        Assert.True(await advBtn.IsVisibleAsync(),
            $"[{testId}] 未找到 高级 菜单按钮。当前URL: {_page.Url}");

        await advBtn.ClickAsync();
        await _page.WaitForSelectorAsync(".dropdown-menu li a", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });
        var aiItem = _page.Locator(".dropdown-menu li a:has-text('AI 分析')").First;
        Assert.False(await aiItem.IsVisibleAsync(),
            $"[{testId}] 高级菜单不应再包含「AI 分析」入口（已合并到 AI 助手浮窗）");

        // 2. AI 助手浮窗（AISwitch 开启时渲染）
        var fab = _page.Locator("#aiAssistantFab");
        if (await fab.CountAsync() > 0)
        {
            Assert.True(await fab.IsVisibleAsync(), $"[{testId}] AI 助手浮球不可见");

            // 打开面板，验证快捷指令「分析当前数据」
            await fab.ClickAsync();
            var panel = _page.Locator("#aiAssistantPanel");
            await panel.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 5_000,
            });
            Assert.True(await panel.IsVisibleAsync(), $"[{testId}] AI 助手面板未打开");
            var quick = _page.Locator(".ai-chip[data-prompt='分析当前列表数据']").First;
            Assert.True(await quick.CountAsync() > 0, $"[{testId}] 浮窗缺少「分析当前数据」快捷指令");
        }

        // 3. 无未捕获 JS 异常
        Assert.Empty(pageErrors);
    }
}

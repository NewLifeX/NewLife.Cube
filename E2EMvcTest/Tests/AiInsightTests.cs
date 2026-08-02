using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>Session D — AI 洞察（AI-5 回归测试）</summary>
/// <remarks>验证列表页「AI洞察」按钮可用：ai-insight.js 已加载（window.CubeAI 已定义）、
/// 点击后弹窗打开、无未捕获 JS 异常（修复前点击抛 "CubeAI is not defined"）。</remarks>
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

    [Fact(DisplayName = "TC-AI-001 列表页 AI洞察 按钮可用且无 JS 报错")]
    [Trait("Category", "AiInsight")]
    [Trait("Priority", "P1")]
    public async Task TC_AI_001_AiInsightButtonWorks()
    {
        const String testId = "TC-AI-001";

        // 收集页面未捕获 JS 异常（修复前点击会抛 "CubeAI is not defined"）
        var pageErrors = new List<String>();
        _page.PageError += (_, msg) => pageErrors.Add(msg);

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/UserStat");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 1. ai-insight.js 已加载，window.CubeAI 已定义
        var hasCubeAI = await _page.EvaluateAsync<Boolean>(
            "typeof window.CubeAI === 'object' && typeof window.CubeAI.insight === 'function'");
        Assert.True(hasCubeAI,
            $"[{testId}] window.CubeAI 未定义，ai-insight.js 未加载。当前URL: {_page.Url}");

        // 2. AI洞察 按钮可见
        var aiBtn = _page.Locator(".btn-ai");
        Assert.True(await aiBtn.IsVisibleAsync(),
            $"[{testId}] 未找到 AI洞察 按钮。当前URL: {_page.Url}");

        // 3. 点击按钮展开下拉，点击「快速洞察」
        await aiBtn.ClickAsync();
        await _page.WaitForSelectorAsync(".dropdown-ai a",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        await _page.Locator(".dropdown-ai a:has-text('快速洞察')").First.ClickAsync();

        // 4. 弹窗打开
        var modal = _page.Locator("#aiInsightModal");
        await modal.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 10_000,
        });
        Assert.True(await modal.IsVisibleAsync(), $"[{testId}] AI 洞察弹窗未打开");

        // 5. 无未捕获 JS 异常（重点：修复前为 "CubeAI is not defined"）
        Assert.Empty(pageErrors);

        // 等待一小段，确认 SSE 流式消费过程也不产生 JS 异常
        await Task.Delay(500);
        Assert.Empty(pageErrors);
    }
}

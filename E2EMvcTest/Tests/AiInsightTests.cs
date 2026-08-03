using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>AI 洞察（收敛到高级菜单后回归测试）</summary>
/// <remarks>AI洞察 已从工具栏独立按钮收敛为高级菜单「AI 分析」（快速分析）。测试验证：
/// ai-insight.js 加载、高级菜单打开、「AI 分析」触发弹窗、无未捕获 JS 异常。
/// 当 AISwitch 未开启时菜单项不渲染，仅验证基础部分。</remarks>
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

    [Fact(DisplayName = "TC-AI-001 高级菜单 AI 分析入口可用且无 JS 报错")]
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

        // 2. 高级菜单按钮可见
        var advBtn = _page.Locator("button:has-text('高级')").First;
        Assert.True(await advBtn.IsVisibleAsync(),
            $"[{testId}] 未找到 高级 菜单按钮。当前URL: {_page.Url}");

        // 3. 展开高级菜单，若存在「AI 分析」（AISwitch 开启时）则点击并验证弹窗
        await advBtn.ClickAsync();
        await _page.WaitForSelectorAsync(".dropdown-menu li a", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000,
        });
        var aiItem = _page.Locator(".dropdown-menu li a:has-text('AI 分析')").First;
        if (await aiItem.IsVisibleAsync())
        {
            await aiItem.ClickAsync();
            var modal = _page.Locator("#aiInsightModal");
            await modal.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 10_000,
            });
            Assert.True(await modal.IsVisibleAsync(), $"[{testId}] AI 洞察弹窗未打开");
        }

        // 4. 无未捕获 JS 异常（重点：修复前为 "CubeAI is not defined"）
        Assert.Empty(pageErrors);
    }
}

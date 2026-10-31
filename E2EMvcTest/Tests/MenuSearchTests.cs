using System;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>ACE 外壳菜单搜索（TC-MENU-SEARCH-001 ~ 003）</summary>
/// <remarks>搜索数据随外壳页面下发，选择结果后切换 iframe 并定位左侧菜单。</remarks>
[Collection("E2E")]
public sealed class MenuSearchTests : IAsyncLifetime
{
    private readonly AppFixture _fixture;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    /// <summary>实例化测试类</summary>
    /// <param name="fixture">全局 E2E 夹具</param>
    public MenuSearchTests(AppFixture fixture) => _fixture = fixture;

    /// <summary>建立浏览器上下文并以管理员身份登录</summary>
    public async Task InitializeAsync()
    {
        _context = await _fixture.Browser.NewContextAsync();
        _page = await _context.NewPageAsync();
        await PageHelpers.LoginAsAdminAsync(_page);
    }

    /// <summary>关闭浏览器上下文</summary>
    public async Task DisposeAsync()
    {
        if (_page != null)
        {
            try { await PageHelpers.LogoutAsync(_page); } catch { }
        }
        if (_context != null) await _context.DisposeAsync();
    }

    /// <summary>打开后台外壳并等待菜单搜索框渲染</summary>
    private async Task OpenShellAsync()
    {
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/Index");
        await _page.WaitForSelectorAsync(".cube-menu-search-input", new PageWaitForSelectorOptions { Timeout = 30_000 });
    }

    [Fact(DisplayName = "TC-MENU-SEARCH-001 输入关键字出现匹配菜单")]
    [Trait("Category", "MenuSearch")]
    [Trait("Priority", "P0")]
    public async Task TC_MENU_SEARCH_001_Input_Shows_Results()
    {
        await OpenShellAsync();

        // 用 URL 片段搜索，避免依赖界面语言下的菜单显示名
        await _page.FillAsync(".cube-menu-search-input", "Admin/User");
        await _page.WaitForSelectorAsync(".cube-menu-search-result", new PageWaitForSelectorOptions { Timeout = 10_000 });

        var count = await _page.Locator(".cube-menu-search-result").CountAsync();
        Assert.True(count > 0, "搜索 URL 片段应至少返回一条菜单");
        Assert.True(await _page.Locator(".cube-menu-search-path").First.IsVisibleAsync(), "结果应显示层级路径");
    }

    [Fact(DisplayName = "TC-MENU-SEARCH-002 选择结果打开目标页并定位左侧菜单")]
    [Trait("Category", "MenuSearch")]
    [Trait("Priority", "P0")]
    public async Task TC_MENU_SEARCH_002_Select_Opens_Page_And_Locates_Menu()
    {
        await OpenShellAsync();

        await _page.FillAsync(".cube-menu-search-input", "Admin/User");
        var result = _page.Locator(".cube-menu-search-result").First;
        await result.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });
        // 结果项监听 mousedown，直接派发可绕开页面持续加载导致的可点击性等待
        await result.DispatchEventAsync("mousedown");

        await _page.WaitForFunctionAsync(
            "() => (document.querySelector('#main')?.getAttribute('src') || '').includes('/Admin/User')",
            null,
            new PageWaitForFunctionOptions { Timeout = 10_000 });

        var src = await _page.GetAttributeAsync("#main", "src");
        Assert.Contains("/Admin/User", src, StringComparison.OrdinalIgnoreCase);

        var active = _page.Locator(".nav-list li.active a[data-url='/Admin/User']");
        Assert.True(await active.CountAsync() > 0, "左侧菜单应定位到用户菜单");
    }

    [Fact(DisplayName = "TC-MENU-SEARCH-004 拼音全拼与首字母可命中中文菜单")]
    [Trait("Category", "MenuSearch")]
    [Trait("Priority", "P1")]
    public async Task TC_MENU_SEARCH_004_Pinyin_Matches()
    {
        await OpenShellAsync();

        // 拼音按库中源文（中文）显示名计算，英文界面下同样可命中
        await _page.FillAsync(".cube-menu-search-input", "yonghu");
        await _page.WaitForSelectorAsync(".cube-menu-search-result", new PageWaitForSelectorOptions { Timeout = 10_000 });
        Assert.True(await _page.Locator(".cube-menu-search-result").CountAsync() > 0, "全拼 yonghu 应命中用户相关菜单");

        await _page.FillAsync(".cube-menu-search-input", "yh");
        await _page.WaitForSelectorAsync(".cube-menu-search-result", new PageWaitForSelectorOptions { Timeout = 10_000 });
        Assert.True(await _page.Locator(".cube-menu-search-result").CountAsync() > 0, "首字母 yh 应命中用户相关菜单");
    }

    [Fact(DisplayName = "TC-MENU-SEARCH-005 清空输入后展示最近使用菜单")]
    [Trait("Category", "MenuSearch")]
    [Trait("Priority", "P2")]
    public async Task TC_MENU_SEARCH_005_Recent_Menus()
    {
        await OpenShellAsync();

        await _page.FillAsync(".cube-menu-search-input", "Admin/User");
        var result = _page.Locator(".cube-menu-search-result").First;
        await result.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });
        await result.DispatchEventAsync("mousedown");
        await _page.WaitForFunctionAsync(
            "() => (document.querySelector('#main')?.getAttribute('src') || '').includes('/Admin/User')",
            null,
            new PageWaitForFunctionOptions { Timeout = 10_000 });

        // 清空关键字后展示最近使用（本次会话刚打开过用户菜单）
        await _page.FillAsync(".cube-menu-search-input", "");
        await _page.WaitForSelectorAsync(".cube-menu-search-result", new PageWaitForSelectorOptions { Timeout = 5_000 });
        Assert.True(await _page.Locator(".cube-menu-search-result").CountAsync() > 0, "最近使用列表不应为空");
    }

    [Fact(DisplayName = "TC-MENU-SEARCH-003 无匹配显示空状态，Esc 关闭面板")]
    [Trait("Category", "MenuSearch")]
    [Trait("Priority", "P1")]
    public async Task TC_MENU_SEARCH_003_No_Match_And_Escape()
    {
        await OpenShellAsync();

        await _page.FillAsync(".cube-menu-search-input", "zzzz-no-such-menu");
        await _page.WaitForSelectorAsync(".cube-menu-search-empty:not([hidden])", new PageWaitForSelectorOptions { Timeout = 10_000 });

        await _page.Locator(".cube-menu-search-input").DispatchEventAsync("keydown", new { key = "Escape" });
        await _page.WaitForSelectorAsync(".cube-menu-search-panel[hidden]", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Attached,
            Timeout = 5_000,
        });
    }
}

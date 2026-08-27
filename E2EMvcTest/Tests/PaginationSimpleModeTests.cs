using System;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>免查总数分页（PageSetting.EnableTotalCount=false）验证</summary>
/// <remarks>
/// 演示控制器 CubeSSO/Areas/Demo/Controllers/UserListController 关闭总数查询，
/// 列表页不显示"共 X 条"与页码，仅提供上一页/下一页翻页。
/// </remarks>
[Collection("E2E")]
public sealed class PaginationSimpleModeTests : IAsyncLifetime
{
    private readonly AppFixture _fixture;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public PaginationSimpleModeTests(AppFixture fixture) => _fixture = fixture;

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

    [Fact(DisplayName = "TC-PAGE-001 免查总数列表不显示总条数与页码，仅上一页/下一页")]
    [Trait("Category", "Pagination")]
    [Trait("Priority", "P1")]
    public async Task TC_PAGE_001_SimplePagerNoTotalCount()
    {
        const String testId = "TC-PAGE-001";

        // 确保至少3个用户，PageSize=2 时可翻页
        await EnsureUsersAsync(3);

        await PageHelpers.GotoAndWaitAsync(_page, "/Demo/UserList?PageSize=2");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 简单分页器已渲染，但不含"共 X 条"
        Assert.True(await _page.IsVisibleAsync(".pager-info"), $"[{testId}] 简单分页器 .pager-info 未渲染");
        var info = await _page.Locator(".pager-info").InnerTextAsync();
        Assert.DoesNotContain("共", info, StringComparison.OrdinalIgnoreCase);

        // 无页码按钮（无当前页高亮）
        Assert.Equal(0, await _page.Locator(".pager-btn-current").CountAsync());

        // 第一页：上一页禁用，下一页可用
        var disabled = await _page.Locator(".pager-btns .pager-btn.disabled").CountAsync();
        Assert.True(disabled >= 1, $"[{testId}] 第一页上一页应禁用");
        Assert.Equal(1, await _page.Locator(".pager-btns a.pager-btn[title=下一页]").CountAsync());

        // 每页显示 PageSize 行
        var rowCount = await _page.Locator(".table-data-list tbody tr").CountAsync();
        Assert.Equal(2, rowCount);
    }

    [Fact(DisplayName = "TC-PAGE-002 免查总数列表可翻到下一页")]
    [Trait("Category", "Pagination")]
    [Trait("Priority", "P1")]
    public async Task TC_PAGE_002_SimplePagerNextPage()
    {
        const String testId = "TC-PAGE-002";

        await EnsureUsersAsync(3);

        await PageHelpers.GotoAndWaitAsync(_page, "/Demo/UserList?PageSize=2");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 点击下一页
        await _page.ClickAsync(".pager-btns a.pager-btn[title=下一页]");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Assert.Contains("PageIndex=2", _page.Url);
        // 第二页：上一页可用
        Assert.Equal(1, await _page.Locator(".pager-btns a.pager-btn[title=上一页]").CountAsync());
        // 第二页仍有数据
        Assert.True(await _page.Locator(".table-data-list tbody tr").CountAsync() > 0, $"[{testId}] 第二页无数据行");
    }

    [Fact(DisplayName = "TC-PAGE-003 常规列表仍显示总条数与页码（回归）")]
    [Trait("Category", "Pagination")]
    [Trait("Priority", "P1")]
    public async Task TC_PAGE_003_NormalPagerStillShowsTotal()
    {
        const String testId = "TC-PAGE-003";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        var info = await _page.Locator(".pager-info").InnerTextAsync();
        Assert.Contains("共", info);
        Assert.True(await _page.IsVisibleAsync(".pager-btn-current"), $"[{testId}] 常规分页应有当前页高亮");
    }

    /// <summary>确保用户表行数不少于指定值，不足时通过公开注册接口补充</summary>
    /// <param name="count">需要的用户总数</param>
    private async Task EnsureUsersAsync(Int32 count)
    {
        var existing = DatabaseHelper.CountAllUsers();
        if (existing >= count) return;

        // 注册是公开功能，使用未登录的独立上下文注册，避免登录态干扰
        var ctx = await _fixture.Browser.NewContextAsync();
        try
        {
            var page = await ctx.NewPageAsync();
            for (var i = 0; existing < count; i++)
            {
                var username = $"e2e_page_{DateTime.Now:HHmmss}_{i}";
                await RegisterUserAsync(page, username);
                existing = DatabaseHelper.CountAllUsers();
            }
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    /// <summary>通过注册表单创建一个新用户</summary>
    /// <param name="page">页面对象（无需登录）</param>
    /// <param name="username">用户名</param>
    private static async Task RegisterUserAsync(IPage page, String username)
    {
        await page.GotoAsync(AppFixture.BaseUrl + "/Admin/User/Login");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.ClickAsync(".login-tabs a[data-tab=Register]");
        await page.WaitForSelectorAsync("#Register.active, #Register.in");
        await page.FillAsync("#reg_pwd_username", username);
        await page.FillAsync("#reg_pwd_password", "Test@2026!");
        await page.FillAsync("#reg_pwd_password2", "Test@2026!");
        // 前端强制勾选《用户协议》《隐私政策》，未勾选提交被拦截
        await page.Locator("#reg-pwd input[name=agreement]").EvaluateAsync("el => el.checked = true");
        await page.ClickAsync("#Register button[type=submit]");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}

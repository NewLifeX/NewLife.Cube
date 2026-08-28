using System;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>Session — 跨 Area 表间链接（TC-CAL-001）</summary>
/// <remarks>
/// 对应 GitHub Issue #66：列表页 Map 外键跳转链接应使用菜单地址（含 Area 前缀），实现跨 Area 跳转。
/// 通过种子数据在 OAuthConfig 列表制造一条绑定租户的记录，验证"租户"列链接指向 /Admin/Tenant?Id=N，
/// 且点击后能正常跳转到租户列表页。
/// </remarks>
[Collection("E2E")]
public sealed class CrossAreaLinkTests : IAsyncLifetime
{
    private readonly AppFixture _fixture;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public CrossAreaLinkTests(AppFixture fixture) => _fixture = fixture;

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

    [Fact(DisplayName = "TC-CAL-001 OAuthConfig 列表租户列生成跨 Area 链接并可跳转")]
    [Trait("Category", "CrossAreaLink")]
    [Trait("Priority", "P1")]
    public async Task TC_CAL_001_OAuthConfigTenantLink_CrossArea()
    {
        const String testId = "TC-CAL-001";

        // 先访问租户列表与 OAuthConfig 列表页，确保 Tenant / OAuthConfig 表已由应用创建
        // （登录与 OAuthConfig 列表均不触发 Tenant 表创建，需显式访问租户页）
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/Tenant");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/OAuthConfig");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 种子数据：租户 + 绑定该租户的 OAuthConfig
        var stamp = DateTime.Now.ToString("HHmmss");
        var tenantName = $"E2E租户{stamp}";
        var tenantId = DatabaseHelper.EnsureTenant(tenantName, $"e2e_{stamp}");
        Assert.True(tenantId > 0, "租户种子插入失败");

        var oauthName = $"E2E{stamp}";
        var oauthId = DatabaseHelper.EnsureOAuthConfigWithTenant(oauthName, tenantId);
        Assert.True(oauthId > 0, "OAuthConfig 种子插入失败");
        Assert.Equal(tenantId, DatabaseHelper.GetOAuthConfigTenantId(oauthName));

        // 重新加载列表，验证"租户"列链接含 Area 前缀
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/OAuthConfig");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        var link = _page.Locator($"a:has-text('{tenantName}')").First;
        await link.WaitForAsync(new LocatorWaitForOptions { Timeout = 5_000 });
        await PageHelpers.TakeScreenshotAsync(_page, testId);

        var href = await link.GetAttributeAsync("href");
        Assert.NotNull(href);
        Assert.Contains($"/Admin/Tenant?Id={tenantId}", href);

        // 点击链接，应跳转到租户列表页
        await link.ClickAsync();
        await PageHelpers.WaitForContentAreaAsync(_page);
        await PageHelpers.AssertNoServerErrorAsync(_page, testId + "-nav");
        await PageHelpers.AssertUrlContainsAsync(_page, "/Admin/Tenant", testId + "-nav");
    }
}

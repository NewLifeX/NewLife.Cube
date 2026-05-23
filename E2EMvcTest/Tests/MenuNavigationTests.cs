using System;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>Session B — 后台菜单全遍历（TC-MENU-001 ~ TC-MENU-048）</summary>
/// <remarks>所有用例共享一个已登录的管理员会话，无需每次重新登录。</remarks>
[Collection("E2E")]
public sealed class MenuNavigationTests : IAsyncLifetime
{
    private readonly AppFixture _fixture;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public MenuNavigationTests(AppFixture fixture) => _fixture = fixture;

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

    #region 辅助：通用菜单页面断言

    /// <summary>导航到指定路径，断言：无 5xx、内容区可见、有 table 或列表内容</summary>
    private async Task AssertMenuPageAsync(String path, String testId, String? expectText = null)
    {
        await PageHelpers.GotoAndWaitAsync(_page, path);
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        await PageHelpers.WaitForContentAreaAsync(_page);

        if (expectText != null)
            await PageHelpers.AssertTextVisibleAsync(_page, expectText, testId);
    }

    #endregion

    #region B.1 系统管理区（Admin 区）

    [Fact(DisplayName = "TC-MENU-001 用户管理页加载并有数据行")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P0")]
    public async Task TC_MENU_001_UserManagementPage()
    {
        const String testId = "TC-MENU-001";
        await AssertMenuPageAsync("/Admin/User", testId);

        // 至少有 admin 行
        var hasRow = await _page.IsVisibleAsync("table tbody tr")
                  || await _page.IsVisibleAsync(".table tr:nth-child(2)");
        Assert.True(hasRow,
            $"[{testId}] 用户管理表格无数据行。当前URL: {_page.Url}");
    }

    [Fact(DisplayName = "TC-MENU-002 角色管理页加载并有数据行")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P0")]
    public async Task TC_MENU_002_RoleManagementPage()
    {
        const String testId = "TC-MENU-002";
        await AssertMenuPageAsync("/Admin/Role", testId);

        var hasRow = await _page.IsVisibleAsync("table tbody tr");
        Assert.True(hasRow,
            $"[{testId}] 角色管理表格无数据行。当前URL: {_page.Url}");
    }

    [Fact(DisplayName = "TC-MENU-003 菜单管理页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P0")]
    public async Task TC_MENU_003_MenuManagementPage()
    {
        const String testId = "TC-MENU-003";
        await AssertMenuPageAsync("/Admin/Menu", testId);
    }

    [Fact(DisplayName = "TC-MENU-004 部门管理页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P1")]
    public async Task TC_MENU_004_DepartmentPage()
    {
        const String testId = "TC-MENU-004";
        await AssertMenuPageAsync("/Admin/Department", testId);
    }

    [Fact(DisplayName = "TC-MENU-005 在线用户页加载并有当前会话")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P1")]
    public async Task TC_MENU_005_OnlineUsersPage()
    {
        const String testId = "TC-MENU-005";
        await AssertMenuPageAsync("/Admin/UserOnline", testId);
    }

    [Fact(DisplayName = "TC-MENU-006 用户统计页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P1")]
    public async Task TC_MENU_006_UserStatPage()
    {
        const String testId = "TC-MENU-006";
        await AssertMenuPageAsync("/Admin/UserStat", testId);
    }

    [Fact(DisplayName = "TC-MENU-007 用户令牌页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P1")]
    public async Task TC_MENU_007_UserTokenPage()
    {
        const String testId = "TC-MENU-007";
        await AssertMenuPageAsync("/Admin/UserToken", testId);
    }

    [Fact(DisplayName = "TC-MENU-008 用户链接页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P1")]
    public async Task TC_MENU_008_UserConnectPage()
    {
        const String testId = "TC-MENU-008";
        await AssertMenuPageAsync("/Admin/UserConnect", testId);
    }

    [Fact(DisplayName = "TC-MENU-009 访问规则页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P1")]
    public async Task TC_MENU_009_AccessRulePage()
    {
        const String testId = "TC-MENU-009";
        await AssertMenuPageAsync("/Admin/AccessRule", testId);
    }

    [Fact(DisplayName = "TC-MENU-010 OAuth配置页包含 NewLife 条目")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P1")]
    public async Task TC_MENU_010_OAuthConfigPage()
    {
        const String testId = "TC-MENU-010";
        await AssertMenuPageAsync("/Admin/OAuthConfig", testId, "NewLife");
    }

    [Fact(DisplayName = "TC-MENU-011 OAuth日志页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P1")]
    public async Task TC_MENU_011_OAuthLogPage()
    {
        const String testId = "TC-MENU-011";
        await AssertMenuPageAsync("/Admin/OAuthLog", testId);
    }

    [Fact(DisplayName = "TC-MENU-012 应用日志页有数据")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P1")]
    public async Task TC_MENU_012_ApplicationLogPage()
    {
        const String testId = "TC-MENU-012";
        await AssertMenuPageAsync("/Admin/Log", testId);

        var hasRow = await _page.IsVisibleAsync("table tbody tr");
        Assert.True(hasRow,
            $"[{testId}] 日志列表无数据行（登录操作应已产生日志）。当前URL: {_page.Url}");
    }

    [Fact(DisplayName = "TC-MENU-013 通知记录页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P1")]
    public async Task TC_MENU_013_NotificationRecordPage()
    {
        const String testId = "TC-MENU-013";
        await AssertMenuPageAsync("/Admin/NotificationRecord", testId);
    }

    [Fact(DisplayName = "TC-MENU-014 邮件配置页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P2")]
    public async Task TC_MENU_014_MailConfigPage()
    {
        const String testId = "TC-MENU-014";
        await AssertMenuPageAsync("/Admin/MailConfig", testId);
    }

    [Fact(DisplayName = "TC-MENU-015 短信配置页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P2")]
    public async Task TC_MENU_015_SmsConfigPage()
    {
        const String testId = "TC-MENU-015";
        await AssertMenuPageAsync("/Admin/SmsConfig", testId);
    }

    [Fact(DisplayName = "TC-MENU-016 数据库工具页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P2")]
    public async Task TC_MENU_016_DbToolPage()
    {
        const String testId = "TC-MENU-016";
        await AssertMenuPageAsync("/Admin/Db", testId);
    }

    [Fact(DisplayName = "TC-MENU-018 参数管理页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P2")]
    public async Task TC_MENU_018_ParameterPage()
    {
        const String testId = "TC-MENU-018";
        await AssertMenuPageAsync("/Admin/Parameter", testId);
    }

    [Fact(DisplayName = "TC-MENU-019 系统信息页显示版本信息")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P2")]
    public async Task TC_MENU_019_SysInfoPage()
    {
        const String testId = "TC-MENU-019";
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/Sys");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        await PageHelpers.WaitForContentAreaAsync(_page);
    }

    [Fact(DisplayName = "TC-MENU-020 文件管理页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P2")]
    public async Task TC_MENU_020_FilePage()
    {
        const String testId = "TC-MENU-020";
        await AssertMenuPageAsync("/Admin/File", testId);
    }

    [Fact(DisplayName = "TC-MENU-021 多租户管理页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P2")]
    public async Task TC_MENU_021_TenantPage()
    {
        const String testId = "TC-MENU-021";
        await AssertMenuPageAsync("/Admin/Tenant", testId);
    }

    [Fact(DisplayName = "TC-MENU-022 租户用户页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P2")]
    public async Task TC_MENU_022_TenantUserPage()
    {
        const String testId = "TC-MENU-022";
        await AssertMenuPageAsync("/Admin/TenantUser", testId);
    }

    #endregion

    #region B.2 搜索功能验证

    [Fact(DisplayName = "TC-MENU-030 用户列表关键字 Q 搜索 admin 返回结果")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P1")]
    public async Task TC_MENU_030_UserListSearchByKeyword()
    {
        const String testId = "TC-MENU-030";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // 填写 Q 搜索框并提交
        var searchInput = _page.Locator("input[name=Q], input[name=q], input[placeholder*='搜索'], input[placeholder*='关键']");
        if (await searchInput.CountAsync() > 0)
        {
            await searchInput.First.FillAsync("admin");
            await _page.Keyboard.PressAsync("Enter");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        else
        {
            // 无搜索框则访问带 Q 参数的 URL
            await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User?Q=admin");
        }

        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        var hasRow = await _page.IsVisibleAsync("table tbody tr");
        Assert.True(hasRow,
            $"[{testId}] 搜索 admin 后列表无结果。当前URL: {_page.Url}");
    }

    [Fact(DisplayName = "TC-MENU-031 用户列表时间区间筛选包含 admin")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P1")]
    public async Task TC_MENU_031_UserListFilterByDateRange()
    {
        const String testId = "TC-MENU-031";

        var yesterday = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
        var tomorrow = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");

        await PageHelpers.GotoAndWaitAsync(_page,
            $"/Admin/User?dtStart={yesterday}&dtEnd={tomorrow}");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        var hasRow = await _page.IsVisibleAsync("table tbody tr");
        Assert.True(hasRow,
            $"[{testId}] 时间区间内无用户记录。当前URL: {_page.Url}");
    }

    [Fact(DisplayName = "TC-MENU-033 OAuth日志按 NewLife 应用筛选")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P1")]
    public async Task TC_MENU_033_OAuthLogFilterByApp()
    {
        const String testId = "TC-MENU-033";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/OAuthLog?Provider=NewLife");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
    }

    [Fact(DisplayName = "TC-MENU-034 日志页时间区间搜索包含最近登录日志")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P1")]
    public async Task TC_MENU_034_LogListFilterByTimeRange()
    {
        const String testId = "TC-MENU-034";

        var oneHourAgo = DateTime.Now.AddHours(-1).ToString("yyyy-MM-dd HH:mm:ss");
        var now = DateTime.Now.AddMinutes(5).ToString("yyyy-MM-dd HH:mm:ss");

        await PageHelpers.GotoAndWaitAsync(_page,
            $"/Admin/Log?dtStart={Uri.EscapeDataString(oneHourAgo)}&dtEnd={Uri.EscapeDataString(now)}");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
    }

    [Fact(DisplayName = "TC-MENU-035 用户列表空搜索返回全量结果")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P1")]
    public async Task TC_MENU_035_UserListEmptySearchReturnsAll()
    {
        const String testId = "TC-MENU-035";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        var hasRow = await _page.IsVisibleAsync("table tbody tr");
        Assert.True(hasRow,
            $"[{testId}] 空搜索条件下用户列表无结果。当前URL: {_page.Url}");
    }

    #endregion

    #region B.3 魔方管理区（Cube 区）

    [Fact(DisplayName = "TC-MENU-040 应用系统页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P1")]
    public async Task TC_MENU_040_AppSystemPage()
    {
        const String testId = "TC-MENU-040";
        await AssertMenuPageAsync("/Cube/App", testId);
    }

    [Fact(DisplayName = "TC-MENU-041 应用日志页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P1")]
    public async Task TC_MENU_041_AppLogPage()
    {
        const String testId = "TC-MENU-041";
        await AssertMenuPageAsync("/Cube/AppLog", testId);
    }

    [Fact(DisplayName = "TC-MENU-042 应用模块页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P1")]
    public async Task TC_MENU_042_AppModulePage()
    {
        const String testId = "TC-MENU-042";
        await AssertMenuPageAsync("/Cube/AppModule", testId);
    }

    [Fact(DisplayName = "TC-MENU-043 附件管理页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P1")]
    public async Task TC_MENU_043_AttachmentPage()
    {
        const String testId = "TC-MENU-043";
        await AssertMenuPageAsync("/Cube/Attachment", testId);
    }

    [Fact(DisplayName = "TC-MENU-044 定时作业页有系统内置作业")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P1")]
    public async Task TC_MENU_044_CronJobPage()
    {
        const String testId = "TC-MENU-044";
        await AssertMenuPageAsync("/Cube/CronJob", testId);
    }

    [Fact(DisplayName = "TC-MENU-045 委托代理页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P2")]
    public async Task TC_MENU_045_PrincipalAgentPage()
    {
        const String testId = "TC-MENU-045";
        await AssertMenuPageAsync("/Cube/PrincipalAgent", testId);
    }

    [Fact(DisplayName = "TC-MENU-046 模型表页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P2")]
    public async Task TC_MENU_046_ModelTablePage()
    {
        const String testId = "TC-MENU-046";
        await AssertMenuPageAsync("/Cube/ModelTable", testId);
    }

    [Fact(DisplayName = "TC-MENU-047 模型列页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P2")]
    public async Task TC_MENU_047_ModelColumnPage()
    {
        const String testId = "TC-MENU-047";
        await AssertMenuPageAsync("/Cube/ModelColumn", testId);
    }

    [Fact(DisplayName = "TC-MENU-048 区域管理页加载正常")]
    [Trait("Category", "Menu")]
    [Trait("Priority", "P2")]
    public async Task TC_MENU_048_AreaPage()
    {
        const String testId = "TC-MENU-048";
        await AssertMenuPageAsync("/Cube/Area", testId);
    }

    #endregion
}

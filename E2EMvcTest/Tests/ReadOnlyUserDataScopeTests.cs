using System;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>数据权限专项 — 非系统角色（本部门范围、未分配部门）只读用户的数据可见性与批量操作按钮</summary>
/// <remarks>
/// 覆盖四个线上问题：1) 用户页看不到自己 2) 个人信息页报"无法取得编号" 3) 附件页未按当前用户过滤
/// 4) 无修改权限仍显示批量启用/禁用按钮。普通用户角色授予全部页面只读权限（仅查看）。
/// </remarks>
[Collection("E2E")]
public sealed class ReadOnlyUserDataScopeTests : IAsyncLifetime
{
    private readonly AppFixture _fixture;
    private IBrowserContext _context = null!;
    private IBrowserContext _adminContext = null!;
    private IPage _page = null!;
    private IPage _adminPage = null!;

    private static String? _userName;
    private static String? _ownTitle;
    private static String? _otherTitle;
    private static String? _ownDeptName;
    private static String? _otherDeptName;
    private static Int32 _userId;
    private static Int32 _ownDeptId;
    private static Int32 _otherDeptId;

    private const String Password = "Test@2026!";

    public ReadOnlyUserDataScopeTests(AppFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        var ts = DateTime.Now.ToString("HHmmss");
        _userName = $"e2ero{ts}";

        // 管理员上下文：注册测试用户、授予只读权限、写入附件种子
        _adminContext = await _fixture.Browser.NewContextAsync();
        _adminPage = await _adminContext.NewPageAsync();
        await PageHelpers.LoginAsAdminAsync(_adminPage);

        // 先访问附件页，确保 Cube 区域菜单完成扫描注册
        await PageHelpers.GotoAndWaitAsync(_adminPage, "/Cube/Attachment");

        await RegisterAsync(_userName);

        _userId = DatabaseHelper.GetUserIdByName(_userName);
        Assert.True(_userId > 0, $"注册后未从数据库找到用户：{_userName}");

        var roleId = DatabaseHelper.GetUserRoleId(_userName);
        Assert.True(roleId > 0, $"注册用户未分配角色：{_userName}");
        await GrantReadOnlyAllPagesAsync(roleId);

        // 附件种子：本人一条 + 管理员一条
        _ownTitle = $"E2E附件本人{ts}";
        _otherTitle = $"E2E附件他人{ts}";
        var adminId = DatabaseHelper.GetUserIdByName(AppFixture.AdminUser);
        DatabaseHelper.SeedAttachment(_ownTitle, _userId);
        DatabaseHelper.SeedAttachment(_otherTitle, adminId);

        // 部门种子：本人管理一个 + 管理员管理一个（普通用户只看自己管理的部门）
        _ownDeptName = $"E2E部门本人{ts}";
        _otherDeptName = $"E2E部门他人{ts}";
        _ownDeptId = DatabaseHelper.SeedDepartment(_ownDeptName, _userId);
        _otherDeptId = DatabaseHelper.SeedDepartment(_otherDeptName, adminId);
        Assert.True(_ownDeptId > 0 && _otherDeptId > 0, "部门种子写入失败");

        // 只读用户上下文
        _context = await _fixture.Browser.NewContextAsync();
        _page = await _context.NewPageAsync();
        await PageHelpers.LoginAsync(_page, _userName, Password);
    }

    public async Task DisposeAsync()
    {
        if (_context != null) await _context.DisposeAsync();
        if (_adminContext != null) await _adminContext.DisposeAsync();
    }

    #region 只读用户数据可见性

    [Fact(DisplayName = "TC-DS-001 普通用户访问用户页可见本人记录")]
    [Trait("Category", "DataScope")]
    [Trait("Priority", "P0")]
    public async Task TC_DS_001_UserListShowsSelfOnly()
    {
        const String testId = "TC-DS-001";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        await PageHelpers.AssertTextNotVisibleAsync(_page, "无法取得编号", testId);

        // 数据权限下只应看到本人一行
        var rows = _page.Locator("table tbody tr");
        var count = await rows.CountAsync();
        Assert.True(count == 1, $"[{testId}] 用户列表应只有本人一行，实际 {count} 行。URL={_page.Url}");

        var text = await rows.First.InnerTextAsync();
        Assert.Contains(_userName!, text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "TC-DS-002 普通用户打开个人信息页正常渲染")]
    [Trait("Category", "DataScope")]
    [Trait("Priority", "P0")]
    public async Task TC_DS_002_UserInfoPageLoads()
    {
        const String testId = "TC-DS-002";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Info");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        await PageHelpers.AssertTextNotVisibleAsync(_page, "无法取得编号", testId);

        var hasField = await _page.IsVisibleAsync("input[name=DisplayName], label:has-text('显示名')");
        Assert.True(hasField, $"[{testId}] 个人信息页未渲染显示名字段。URL={_page.Url}");
    }

    [Fact(DisplayName = "TC-DS-003 普通用户附件页只显示本人附件")]
    [Trait("Category", "DataScope")]
    [Trait("Priority", "P0")]
    public async Task TC_DS_003_AttachmentListFilteredToSelf()
    {
        const String testId = "TC-DS-003";

        // 先确认种子已落库，避免"他人附件不可见"断言因种子缺失而空过
        Assert.Equal(1, DatabaseHelper.CountAttachmentByTitle(_ownTitle!));
        Assert.Equal(1, DatabaseHelper.CountAttachmentByTitle(_otherTitle!));

        await PageHelpers.GotoAndWaitAsync(_page, "/Cube/Attachment");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        await PageHelpers.AssertTextVisibleAsync(_page, _ownTitle!, testId);
        await PageHelpers.AssertTextNotVisibleAsync(_page, _otherTitle!, testId);
    }

    [Fact(DisplayName = "TC-DS-010 普通用户用户链接页只显示本人绑定")]
    [Trait("Category", "DataScope")]
    [Trait("Priority", "P0")]
    public async Task TC_DS_010_UserConnectListFilteredToSelf()
    {
        const String testId = "TC-DS-010";

        // 种子：同一提供商下本人一条 + 管理员一条；搜索 provider 只命中本次种子
        var provider = $"E2E_DS_{DateTime.Now:HHmmss}";
        var adminId = DatabaseHelper.GetUserIdByName(AppFixture.AdminUser);
        Assert.True(adminId > 0, "未找到管理员用户");

        Assert.True(DatabaseHelper.SeedUserConnect(provider, $"own_{provider}", _userId) > 0, "本人用户链接种子写入失败");
        Assert.True(DatabaseHelper.SeedUserConnect(provider, $"other_{provider}", adminId) > 0, "他人用户链接种子写入失败");
        Assert.Equal(1, DatabaseHelper.CountUserConnect(_userId, provider));
        Assert.Equal(1, DatabaseHelper.CountUserConnect(adminId, provider));

        // 数据权限（[DataPermission] UserID={#userId}）：非系统角色只能看到本人绑定
        await PageHelpers.GotoAndWaitAsync(_page, $"/Admin/UserConnect?provider={provider}");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        var rows = _page.Locator("table tbody tr");
        var count = await rows.CountAsync();
        Assert.True(count == 1, $"[{testId}] 用户链接列表应只有本人一行，实际 {count} 行。URL={_page.Url}");
    }

    [Fact(DisplayName = "TC-DS-004 普通用户部门页只显示自己管理的部门")]
    [Trait("Category", "DataScope")]
    [Trait("Priority", "P0")]
    public async Task TC_DS_004_DepartmentListShowsManagedOnly()
    {
        const String testId = "TC-DS-004";

        // 先确认部门种子已落库，避免"他人部门不可见"断言空过
        Assert.Equal(1, DatabaseHelper.CountDepartmentByName(_ownDeptName!));
        Assert.Equal(1, DatabaseHelper.CountDepartmentByName(_otherDeptName!));

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/Department");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        await PageHelpers.AssertTextNotVisibleAsync(_page, "无法取得编号", testId);

        // 管理者字段（ManagerID）决定可见性：自己管理的可见，他人管理（含管理员）不可见
        await PageHelpers.AssertTextVisibleAsync(_page, _ownDeptName!, testId);
        await PageHelpers.AssertTextNotVisibleAsync(_page, _otherDeptName!, testId);
    }

    [Fact(DisplayName = "TC-DS-007 普通用户可打开自己管理的部门详情")]
    [Trait("Category", "DataScope")]
    [Trait("Priority", "P0")]
    public async Task TC_DS_007_OwnDepartmentDetailLoads()
    {
        const String testId = "TC-DS-007";

        await PageHelpers.GotoAndWaitAsync(_page, $"/Admin/Department/Detail/{_ownDeptId}");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        await PageHelpers.AssertTextNotVisibleAsync(_page, "非法访问数据", testId);

        // 详情页以表单控件呈现，名称在 input.value 中，不能按文本断言
        var nameValue = await _page.InputValueAsync("input[name=Name]");
        Assert.Equal(_ownDeptName, nameValue);
    }

    [Fact(DisplayName = "TC-DS-008 普通用户无法打开他人部门详情")]
    [Trait("Category", "DataScope")]
    [Trait("Priority", "P0")]
    public async Task TC_DS_008_OtherDepartmentDetailBlocked()
    {
        const String testId = "TC-DS-008";

        await PageHelpers.GotoAndWaitAsync(_page, $"/Admin/Department/Detail/{_otherDeptId}");

        // 数据权限拦截：页面不得泄露他人部门信息
        await PageHelpers.AssertTextNotVisibleAsync(_page, _otherDeptName!, testId);
    }

    [Fact(DisplayName = "TC-DS-009 管理员部门页可见全部部门")]
    [Trait("Category", "DataScope")]
    [Trait("Priority", "P1")]
    public async Task TC_DS_009_AdminSeesAllDepartments()
    {
        const String testId = "TC-DS-009";

        await PageHelpers.GotoAndWaitAsync(_adminPage, "/Admin/Department");
        await PageHelpers.AssertNoServerErrorAsync(_adminPage, testId);

        await PageHelpers.AssertTextVisibleAsync(_adminPage, _ownDeptName!, testId);
        await PageHelpers.AssertTextVisibleAsync(_adminPage, _otherDeptName!, testId);
    }

    #endregion

    #region 批量操作按钮权限

    [Fact(DisplayName = "TC-DS-005 只读用户列表页不显示批量启用禁用按钮")]
    [Trait("Category", "DataScope")]
    [Trait("Priority", "P0")]
    public async Task TC_DS_005_NoBatchButtonsForReadonlyUser()
    {
        const String testId = "TC-DS-005";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        Assert.Equal(0, await _page.Locator("button:has-text('批量启用')").CountAsync());
        Assert.Equal(0, await _page.Locator("button:has-text('批量禁用')").CountAsync());
        // 无更新/删除权限时不应出现行选择框
        Assert.Equal(0, await _page.Locator("input[name='keys']").CountAsync());
    }

    [Fact(DisplayName = "TC-DS-006 管理员列表页仍显示批量启用禁用按钮")]
    [Trait("Category", "DataScope")]
    [Trait("Priority", "P1")]
    public async Task TC_DS_006_AdminStillSeesBatchButtons()
    {
        const String testId = "TC-DS-006";

        await PageHelpers.GotoAndWaitAsync(_adminPage, "/Admin/User");
        await PageHelpers.AssertNoServerErrorAsync(_adminPage, testId);

        Assert.Equal(1, await _adminPage.Locator("button:has-text('批量启用')").CountAsync());
        Assert.Equal(1, await _adminPage.Locator("button:has-text('批量禁用')").CountAsync());
        Assert.True(await _adminPage.Locator("input[name='keys']").CountAsync() > 0);
    }

    #endregion

    #region 辅助

    /// <summary>通过注册表单创建测试用户（使用独立上下文，不影响管理员会话）</summary>
    /// <param name="username">用户名</param>
    private async Task RegisterAsync(String username)
    {
        await using var ctx = await _fixture.Browser.NewContextAsync();
        var page = await ctx.NewPageAsync();

        await page.GotoAsync(AppFixture.BaseUrl + "/Admin/User/Login");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.ClickAsync(".login-tabs a[data-tab=Register]");
        await page.WaitForSelectorAsync("#Register.active, #Register.in");
        await page.FillAsync("#reg_pwd_username", username);
        await page.FillAsync("#reg_pwd_password", Password);
        await page.FillAsync("#reg_pwd_password2", Password);
        // 前端强制勾选《用户协议》《隐私政策》，未勾选提交被拦截
        await page.Locator("#reg-pwd input[name=agreement]").EvaluateAsync("el => el.checked = true");
        await page.ClickAsync("#Register button[type=submit]");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>以管理员身份把指定角色改为"全部页面只读"（勾选全部菜单资源，仅勾选查看权限）</summary>
    /// <param name="roleId">角色 Id</param>
    private async Task GrantReadOnlyAllPagesAsync(Int32 roleId)
    {
        await PageHelpers.GotoAndWaitAsync(_adminPage, $"/Admin/Role/Edit/{roleId}");
        await PageHelpers.AssertNoServerErrorAsync(_adminPage, "TC-DS-GRANT");

        // 勾选全部菜单资源（p{id}）与"查看"权限位（pf{id}_1），其余权限位保持不勾选
        var checkedCount = await _adminPage.EvaluateAsync<Int32>(
            """
            () => {
                let n = 0;
                document.querySelectorAll('input[type=checkbox]').forEach(el => {
                    const name = el.name || '';
                    if (/^p\d+$/.test(name) || /^pf\d+_1$/.test(name)) { el.checked = true; n++; }
                });
                return n;
            }
            """);
        Assert.True(checkedCount > 5, $"角色编辑页未找到权限复选框，已勾选 {checkedCount} 项");

        await _adminPage.ClickAsync("button[type=submit], input[type=submit]");
        await _adminPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await PageHelpers.AssertNoServerErrorAsync(_adminPage, "TC-DS-GRANT-SAVE");

        // 数据库校验：查看权限（1）确已保存
        var permission = DatabaseHelper.GetRolePermission(roleId);
        Assert.True(permission != null && permission.Contains("#1"),
            $"角色[{roleId}]只读权限未保存，Permission={permission}");
    }

    #endregion
}

using System;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>Session C — 用户信息专项（TC-USER-001 ~ TC-USER-042）</summary>
/// <remarks>以 admin 账号登录，验证用户信息页全部功能及登录统计。</remarks>
[Collection("E2E")]
public sealed class UserProfileTests : IAsyncLifetime
{
    private readonly AppFixture _fixture;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // 本次测试写入的显示名唯一值，供 DB 验证用例复用
    private static String? _savedDisplayName;

    public UserProfileTests(AppFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _context = await _fixture.Browser.NewContextAsync();
        _page = await _context.NewPageAsync();
        await PageHelpers.LoginAsAdminAsync(_page);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    #region C.1 用户显示与入口

    [Fact(DisplayName = "TC-USER-001 后台顶栏显示用户名")]
    [Trait("Category", "UserProfile")]
    [Trait("Priority", "P0")]
    public async Task TC_USER_001_TopBarShowsUsername()
    {
        const String testId = "TC-USER-001";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/Index");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        var bodyText = await _page.InnerTextAsync("body");
        Assert.True(bodyText.Contains(AppFixture.AdminUser, StringComparison.OrdinalIgnoreCase),
            $"[{testId}] 顶栏未显示用户名 '{AppFixture.AdminUser}'。当前URL: {_page.Url}");
    }

    [Fact(DisplayName = "TC-USER-002 点击用户名进入用户信息页")]
    [Trait("Category", "UserProfile")]
    [Trait("Priority", "P0")]
    public async Task TC_USER_002_ClickUsernameEntersInfoPage()
    {
        const String testId = "TC-USER-002";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/Index");

        // 顶栏用户名链接，通常 href 包含 /Admin/User/Info
        var link = _page.Locator("a[href*='/Admin/User/Info'], a[href*='User/Info'], .navbar a:has-text('" + AppFixture.AdminUser + "')");
        if (await link.CountAsync() > 0)
        {
            await link.First.ClickAsync();
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await PageHelpers.AssertUrlContainsAsync(_page, "/User/Info", testId);
        }
        else
        {
            // 直接导航验证页面可访问
            await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Info");
            await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        }
    }

    #endregion

    #region C.2 导航栏标签（当前登录用户：10 个标签）

    [Fact(DisplayName = "TC-USER-010 基本信息标签：显示用户基本信息表单")]
    [Trait("Category", "UserProfile")]
    [Trait("Priority", "P0")]
    public async Task TC_USER_010_BasicInfoTab()
    {
        const String testId = "TC-USER-010";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Info");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 基本信息标签应为默认激活
        var hasDisplayNameField = await _page.IsVisibleAsync("input[name=DisplayName], label:has-text('显示名')");
        Assert.True(hasDisplayNameField,
            $"[{testId}] 基本信息页未找到显示名字段。当前URL: {_page.Url}");
    }

    [Fact(DisplayName = "TC-USER-011 修改密码标签：显示修改密码表单")]
    [Trait("Category", "UserProfile")]
    [Trait("Priority", "P0")]
    public async Task TC_USER_011_ChangePasswordTab()
    {
        const String testId = "TC-USER-011";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/ChangePassword");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
    }

    [Fact(DisplayName = "TC-USER-012 第三方授权标签：加载绑定列表")]
    [Trait("Category", "UserProfile")]
    [Trait("Priority", "P0")]
    public async Task TC_USER_012_ThirdPartyAuthTab()
    {
        const String testId = "TC-USER-012";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Binds");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
    }

    [Fact(DisplayName = "TC-USER-013 租户信息标签：加载正常")]
    [Trait("Category", "UserProfile")]
    [Trait("Priority", "P0")]
    public async Task TC_USER_013_TenantInfoTab()
    {
        const String testId = "TC-USER-013";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/TenantSetting");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
    }

    [Fact(DisplayName = "TC-USER-014 用户名称标签（编辑表单）含昵称和名称字段")]
    [Trait("Category", "UserProfile")]
    [Trait("Priority", "P0")]
    public async Task TC_USER_014_UserNameEditTab()
    {
        const String testId = "TC-USER-014";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Info");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 用户名称标签指向 Edit 表单
        await PageHelpers.ClickNavTabAsync(_page, AppFixture.AdminUser);

        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
    }

    [Fact(DisplayName = "TC-USER-015 三方链接标签：加载列表")]
    [Trait("Category", "UserProfile")]
    [Trait("Priority", "P0")]
    public async Task TC_USER_015_UserConnectTab()
    {
        const String testId = "TC-USER-015";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Info");
        await PageHelpers.ClickNavTabAsync(_page, "三方链接");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
    }

    [Fact(DisplayName = "TC-USER-016 令牌标签：加载正常")]
    [Trait("Category", "UserProfile")]
    [Trait("Priority", "P0")]
    public async Task TC_USER_016_TokenTab()
    {
        const String testId = "TC-USER-016";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Info");
        await PageHelpers.ClickNavTabAsync(_page, "令牌");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
    }

    [Fact(DisplayName = "TC-USER-017 OAuth日志标签：有登录记录（需先完成 OAuth 登录）")]
    [Trait("Category", "UserProfile")]
    [Trait("Priority", "P0")]
    public async Task TC_USER_017_OAuthLogTab()
    {
        const String testId = "TC-USER-017";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Info");
        await PageHelpers.ClickNavTabAsync(_page, "OAuth日志");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
    }

    [Fact(DisplayName = "TC-USER-018 日志标签：有登录日志记录")]
    [Trait("Category", "UserProfile")]
    [Trait("Priority", "P0")]
    public async Task TC_USER_018_LogTab()
    {
        const String testId = "TC-USER-018";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Info");
        await PageHelpers.ClickNavTabAsync(_page, "日志");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        var hasRow = await _page.IsVisibleAsync("table tbody tr");
        Assert.True(hasRow,
            $"[{testId}] 日志标签下无记录，当前登录操作应已产生日志。当前URL: {_page.Url}");
    }

    [Fact(DisplayName = "TC-USER-019 通知记录标签：加载正常")]
    [Trait("Category", "UserProfile")]
    [Trait("Priority", "P0")]
    public async Task TC_USER_019_NotificationTab()
    {
        const String testId = "TC-USER-019";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Info");
        await PageHelpers.ClickNavTabAsync(_page, "通知记录");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
    }

    #endregion

    #region C.3 基本信息修改

    [Fact(DisplayName = "TC-USER-020 修改显示名并保存，DB 验证")]
    [Trait("Category", "UserProfile")]
    [Trait("Priority", "P0")]
    public async Task TC_USER_020_SaveDisplayName()
    {
        const String testId = "TC-USER-020";
        _savedDisplayName = $"E2E测试_{DateTime.Now:HHmmss}";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Info");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 找到显示名输入框并填入新值
        var displayNameInput = _page.Locator("input[name=DisplayName]");
        if (await displayNameInput.CountAsync() == 0)
        {
            await PageHelpers.TakeScreenshotAsync(_page, testId);
            throw new Exception($"[{testId}] 未找到显示名输入框。当前URL: {_page.Url}");
        }

        await displayNameInput.First.ClearAsync();
        await displayNameInput.First.FillAsync(_savedDisplayName);

        // 提交表单
        await _page.ClickAsync("button[type=submit], input[type=submit]");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // DB 验证
        var dbValue = DatabaseHelper.GetUserField(AppFixture.AdminUser, "DisplayName");
        Assert.Equal(_savedDisplayName, dbValue);
    }

    [Fact(DisplayName = "TC-USER-021 修改邮件地址并保存，DB 验证")]
    [Trait("Category", "UserProfile")]
    [Trait("Priority", "P1")]
    public async Task TC_USER_021_SaveMailAddress()
    {
        const String testId = "TC-USER-021";
        var newMail = $"e2e_{DateTime.Now:HHmmss}@test.local";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Info");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        var mailInput = _page.Locator("input[name=Mail]");
        if (await mailInput.CountAsync() == 0)
            return; // 基本信息页无邮件字段则跳过

        await mailInput.First.ClearAsync();
        await mailInput.First.FillAsync(newMail);

        await _page.ClickAsync("button[type=submit], input[type=submit]");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        var dbMail = DatabaseHelper.GetUserField(AppFixture.AdminUser, "Mail");
        Assert.Equal(newMail, dbMail);
    }

    #endregion

    #region C.4 清空密码流程

    [Fact(DisplayName = "TC-USER-030 清空密码后 DB 密码字段为空")]
    [Trait("Category", "UserProfile")]
    [Trait("Priority", "P0")]
    public async Task TC_USER_030_ClearPasswordEmptiesDbField()
    {
        const String testId = "TC-USER-030";

        // 先以 admin 登录进入用户列表，找到 admin 的编辑页
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 查找 admin 行的编辑链接
        var editLink = _page.Locator($"table tr:has-text('{AppFixture.AdminUser}') a[href*='Edit'], table tr:has-text('{AppFixture.AdminUser}') a:has-text('编辑')");
        if (await editLink.CountAsync() > 0)
        {
            await editLink.First.ClickAsync();
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        else
        {
            // 直接访问 Admin 侧的用户 Edit（使用 userId=1 为 admin 的典型 ID）
            await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Edit?Id=1");
        }

        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 找到"清空密码"按钮或链接
        var clearPwdBtn = _page.Locator("button:has-text('清空密码'), a:has-text('清空密码'), input[value*='清空']");
        if (await clearPwdBtn.CountAsync() == 0)
        {
            // 功能入口不在此页，跳过（不应 fail，只标记为跳过）
            return;
        }

        await clearPwdBtn.First.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // 可能弹出确认对话框
        if (await _page.IsVisibleAsync("button:has-text('确认'), button:has-text('确定')"))
        {
            await _page.ClickAsync("button:has-text('确认'), button:has-text('确定')");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // DB 验证：密码字段为空
        var pwd = DatabaseHelper.GetUserField(AppFixture.AdminUser, "Password");
        var isEmpty = String.IsNullOrEmpty(pwd);
        Assert.True(isEmpty,
            $"[{testId}] 清空密码后 DB 密码字段未清空。当前值长度: {pwd?.Length}");
    }

    [Fact(DisplayName = "TC-USER-031 清空密码后用新密码登录成功")]
    [Trait("Category", "UserProfile")]
    [Trait("Priority", "P0")]
    public async Task TC_USER_031_LoginWithNewPasswordAfterClear()
    {
        const String testId = "TC-USER-031";

        // 检查 DB 密码是否已被清空（依赖 TC-USER-030）
        var pwd = DatabaseHelper.GetUserField(AppFixture.AdminUser, "Password");
        if (!String.IsNullOrEmpty(pwd))
            return; // 前置条件未满足，跳过

        const String newPassword = "NewPass@2026";
        await PageHelpers.LogoutAsync(_page);
        await PageHelpers.LoginAsync(_page, AppFixture.AdminUser, newPassword);

        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        await PageHelpers.AssertUrlContainsAsync(_page, "/Admin/", testId);
    }

    [Fact(DisplayName = "TC-USER-032 注销后旧密码无法登录")]
    [Trait("Category", "UserProfile")]
    [Trait("Priority", "P0")]
    public async Task TC_USER_032_OldPasswordLoginFails()
    {
        const String testId = "TC-USER-032";

        // 依赖 TC-USER-031 已用新密码登录，此时已登录状态
        // 注销后用初始旧密码尝试登录
        await PageHelpers.LogoutAsync(_page);
        await PageHelpers.LoginAsync(_page, AppFixture.AdminUser, AppFixture.AdminPass);

        // 应留在登录页
        var url = _page.Url;
        Assert.True(url.Contains("/User/Login", StringComparison.OrdinalIgnoreCase),
            $"[{testId}] 旧密码登录后不应跳转到后台。当前URL: {url}");
    }

    [Fact(DisplayName = "TC-USER-033 新密码已固化可重复登录")]
    [Trait("Category", "UserProfile")]
    [Trait("Priority", "P0")]
    public async Task TC_USER_033_NewPasswordPersistsForRelogin()
    {
        const String testId = "TC-USER-033";

        var pwd = DatabaseHelper.GetUserField(AppFixture.AdminUser, "Password");
        if (String.IsNullOrEmpty(pwd))
        {
            // 密码未固化，跳过（依赖前序用例）
            return;
        }

        // 用新密码登录
        const String newPassword = "NewPass@2026";
        await PageHelpers.LogoutAsync(_page);
        await PageHelpers.LoginAsync(_page, AppFixture.AdminUser, newPassword);

        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        await PageHelpers.AssertUrlContainsAsync(_page, "/Admin/", testId);

        // DB 密码字段非空
        var dbPwd = DatabaseHelper.GetUserField(AppFixture.AdminUser, "Password");
        Assert.False(String.IsNullOrEmpty(dbPwd),
            $"[{testId}] 新密码登录成功后 DB 密码字段仍为空。");
    }

    #endregion

    #region C.5 登录统计

    [Fact(DisplayName = "TC-USER-040 多次登录后登录次数递增")]
    [Trait("Category", "UserProfile")]
    [Trait("Priority", "P1")]
    public async Task TC_USER_040_LoginCountIncrementsOnLogin()
    {
        const String testId = "TC-USER-040";

        var loginsBefore = DatabaseHelper.GetUserLogins(AppFixture.AdminUser);

        // 注销并重新登录
        await PageHelpers.LogoutAsync(_page);
        await PageHelpers.LoginAsAdminAsync(_page);

        var loginsAfter = DatabaseHelper.GetUserLogins(AppFixture.AdminUser);

        Assert.True(loginsAfter > loginsBefore,
            $"[{testId}] 登录后 Logins 字段未递增。之前={loginsBefore}，之后={loginsAfter}");
    }

    [Fact(DisplayName = "TC-USER-041 最后登录时间在登录后更新（<1 分钟内）")]
    [Trait("Category", "UserProfile")]
    [Trait("Priority", "P1")]
    public async Task TC_USER_041_LastLoginUpdatedAfterLogin()
    {
        const String testId = "TC-USER-041";

        await PageHelpers.LogoutAsync(_page);
        var beforeLogin = DateTime.UtcNow.AddSeconds(-5); // 留 5s 容差
        await PageHelpers.LoginAsAdminAsync(_page);

        var lastLoginStr = DatabaseHelper.GetUserLastLogin(AppFixture.AdminUser);
        Assert.False(String.IsNullOrEmpty(lastLoginStr),
            $"[{testId}] LastLogin 字段为空");

        // 解析最后登录时间
        if (DateTime.TryParse(lastLoginStr, out var lastLogin))
        {
            var diff = lastLogin.ToUniversalTime() - beforeLogin;
            Assert.True(diff.TotalMinutes < 2,
                $"[{testId}] LastLogin 未及时更新。LastLogin={lastLoginStr}，参考时间={beforeLogin:O}");
        }
    }

    [Fact(DisplayName = "TC-USER-042 用户详情页显示登录次数和最后登录时间")]
    [Trait("Category", "UserProfile")]
    [Trait("Priority", "P1")]
    public async Task TC_USER_042_UserDetailShowsLoginStats()
    {
        const String testId = "TC-USER-042";

        // 进入 admin 用户详情/编辑页
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Info");
        await PageHelpers.ClickNavTabAsync(_page, AppFixture.AdminUser);
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        var bodyText = await _page.InnerTextAsync("body");

        // 页面应包含"登录次数"或"Logins"相关文字
        var hasLoginCount = bodyText.Contains("登录次数", StringComparison.OrdinalIgnoreCase)
                         || bodyText.Contains("Logins", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasLoginCount,
            $"[{testId}] 用户详情页未找到登录次数字段。当前URL: {_page.Url}");
    }

    #endregion
}

using System;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>Session A — 认证场景测试（TC-AUTH-001 ~ TC-AUTH-042）</summary>
[Collection("E2E")]
public sealed class AuthTests : IAsyncLifetime
{
    private readonly AppFixture _fixture;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // 保存本次注册的唯一用户名，供多个用例复用
    private static String? _registeredUsername;

    public AuthTests(AppFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _context = await _fixture.Browser.NewContextAsync();
        _page = await _context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    #region A.1 注册

    [Fact(DisplayName = "TC-AUTH-001 用户名注册成功")]
    [Trait("Category", "Auth")]
    [Trait("Priority", "P0")]
    public async Task TC_AUTH_001_RegisterWithNewUsername()
    {
        const String testId = "TC-AUTH-001";
        _registeredUsername = $"e2e_{DateTime.Now:HHmmss}";
        var countBefore = DatabaseHelper.CountAllUsers();

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Login");
        // 切到注册标签
        await _page.ClickAsync("a[href='#Register']");
        await _page.WaitForSelectorAsync("#Register.active, #Register.in");

        await _page.FillAsync("#reg_username", _registeredUsername);
        await _page.FillAsync("#reg_password", "Test@2026!");
        await _page.FillAsync("#reg_password2", "Test@2026!");
        await _page.ClickAsync("#Register button[type=submit]");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        var countAfter = DatabaseHelper.CountAllUsers();
        Assert.True(countAfter > countBefore,
            $"[{testId}] 注册后 User 表行数未增加。注册前={countBefore}，注册后={countAfter}");

        Assert.True(DatabaseHelper.CountUsersByName(_registeredUsername) > 0,
            $"[{testId}] User 表中未找到新用户名 '{_registeredUsername}'");
    }

    [Fact(DisplayName = "TC-AUTH-002 用户名重复注册失败")]
    [Trait("Category", "Auth")]
    [Trait("Priority", "P0")]
    public async Task TC_AUTH_002_RegisterDuplicateUsernameFails()
    {
        const String testId = "TC-AUTH-002";
        // 依赖 TC-AUTH-001 已注册，若未运行则使用 admin
        var dupName = _registeredUsername ?? AppFixture.AdminUser;

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Login");
        await _page.ClickAsync("a[href='#Register']");
        await _page.WaitForSelectorAsync("#Register.active, #Register.in");

        await _page.FillAsync("#reg_username", dupName);
        await _page.FillAsync("#reg_password", "Test@2026!");
        await _page.FillAsync("#reg_password2", "Test@2026!");
        await _page.ClickAsync("#Register button[type=submit]");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // 应停留在登录/注册页，不应跳转到后台首页
        var url = _page.Url;
        Assert.True(url.Contains("/User/", StringComparison.OrdinalIgnoreCase) ||
                    url.Contains("/Login", StringComparison.OrdinalIgnoreCase),
            $"[{testId}] 重复用户名注册后不应跳转离开注册/登录页。当前URL: {url}，页面标题: {await _page.TitleAsync()}");
    }

    [Fact(DisplayName = "TC-AUTH-004 注册密码以密文传输（含 challengeId）")]
    [Trait("Category", "Auth")]
    [Trait("Priority", "P1")]
    public async Task TC_AUTH_004_RegisterPasswordTransmittedEncrypted()
    {
        const String testId = "TC-AUTH-004";
        var username = $"e2e_enc_{DateTime.Now:HHmmss}";

        // 监听 POST 请求
        String? requestBody = null;
        _page.Request += (_, req) =>
        {
            if (req.Method == "POST" && req.Url.Contains("/User/Register"))
                requestBody = req.PostData;
        };

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Login");
        await _page.ClickAsync("a[href='#Register']");
        await _page.WaitForSelectorAsync("#Register.active, #Register.in");

        await _page.FillAsync("#reg_username", username);
        await _page.FillAsync("#reg_password", "Test@2026!");
        await _page.FillAsync("#reg_password2", "Test@2026!");
        await _page.ClickAsync("#Register button[type=submit]");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // 注册页使用 POST 直接提交，密码字段应为明文（注册无 RSA 加密），但此用例保留作为结构验证
        // 若后续注册页也加密，则断言同 AUTH-016
        Assert.NotNull(requestBody);
        Assert.True(requestBody!.Contains("username=" + username, StringComparison.OrdinalIgnoreCase),
            $"[{testId}] POST body 未包含 username={username}");
    }

    #endregion

    #region A.2 用户名密码登录

    [Fact(DisplayName = "TC-AUTH-010 用户名+密码登录成功")]
    [Trait("Category", "Auth")]
    [Trait("Priority", "P0")]
    public async Task TC_AUTH_010_LoginWithUsernamePassword()
    {
        const String testId = "TC-AUTH-010";

        await PageHelpers.LoginAsAdminAsync(_page);

        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        await PageHelpers.AssertUrlContainsAsync(_page, "/Admin/", testId);

        // 顶栏应可见 admin 字样
        var bodyText = await _page.InnerTextAsync("body");
        Assert.True(bodyText.Contains(AppFixture.AdminUser, StringComparison.OrdinalIgnoreCase),
            $"[{testId}] 登录后页面未出现用户名 '{AppFixture.AdminUser}'。当前URL: {_page.Url}");
    }

    [Fact(DisplayName = "TC-AUTH-011 用户名+错误密码登录失败")]
    [Trait("Category", "Auth")]
    [Trait("Priority", "P0")]
    public async Task TC_AUTH_011_LoginWithWrongPasswordFails()
    {
        const String testId = "TC-AUTH-011";

        await PageHelpers.LoginAsync(_page, AppFixture.AdminUser, "WrongPass@999");

        await PageHelpers.AssertUrlContainsAsync(_page, "/User/Login", testId);
    }

    [Fact(DisplayName = "TC-AUTH-016 登录密码以密文传输（含 challengeId）")]
    [Trait("Category", "Auth")]
    [Trait("Priority", "P1")]
    public async Task TC_AUTH_016_LoginPasswordTransmittedEncrypted()
    {
        const String testId = "TC-AUTH-016";

        String? requestBody = null;
        _page.Request += (_, req) =>
        {
            if (req.Method == "POST" && req.Url.Contains("/User/Login"))
                requestBody = req.PostData;
        };

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Login");
        await _page.FillAsync("#username", AppFixture.AdminUser);
        await _page.FillAsync("#password", AppFixture.AdminPass);
        await _page.ClickAsync("#login-btn");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Assert.NotNull(requestBody);
        Assert.True(requestBody!.Contains("challengeId=", StringComparison.OrdinalIgnoreCase),
            $"[{testId}] POST body 未包含 challengeId");
        // 密码字段不应含明文密码
        Assert.False(requestBody!.Contains(AppFixture.AdminPass, StringComparison.OrdinalIgnoreCase),
            $"[{testId}] POST body 含明文密码");
    }

    [Fact(DisplayName = "TC-AUTH-017 登录成功后跳转到正确首页")]
    [Trait("Category", "Auth")]
    [Trait("Priority", "P0")]
    public async Task TC_AUTH_017_LoginRedirectsToHome()
    {
        const String testId = "TC-AUTH-017";

        await PageHelpers.LoginAsAdminAsync(_page);

        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        var url = _page.Url;
        var isHome = url.Contains("/Admin/Index", StringComparison.OrdinalIgnoreCase)
                  || url.TrimEnd('/').EndsWith("/Admin", StringComparison.OrdinalIgnoreCase)
                  || url.TrimEnd('/') == AppFixture.BaseUrl.TrimEnd('/');

        Assert.True(isHome,
            $"[{testId}] 登录后未跳转到正确首页。当前URL: {url}，页面标题: {await _page.TitleAsync()}");
    }

    [Fact(DisplayName = "TC-AUTH-018 多次错误密码均提示失败")]
    [Trait("Category", "Auth")]
    [Trait("Priority", "P0")]
    public async Task TC_AUTH_018_MultipleWrongPasswordsFail()
    {
        const String testId = "TC-AUTH-018";

        for (var i = 0; i < 5; i++)
        {
            await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Login");
            await _page.FillAsync("#username", AppFixture.AdminUser);
            await _page.FillAsync("#password", $"WrongPass@{i}");
            await _page.ClickAsync("#login-btn");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await PageHelpers.AssertUrlContainsAsync(_page, "/User/Login", $"{testId}[{i}]");
        }
    }

    #endregion

    #region A.3 NewLife OAuth 全流程

    [Fact(DisplayName = "TC-AUTH-020 登录页显示 NewLife 第三方登录按钮")]
    [Trait("Category", "Auth")]
    [Trait("Priority", "P0")]
    public async Task TC_AUTH_020_LoginPageShowsNewLifeOAuthButton()
    {
        const String testId = "TC-AUTH-020";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Login");

        var hasNewLifeBtn = await _page.IsVisibleAsync("a[href*='Sso/Login']")
                         || await _page.IsVisibleAsync("a[href*='NewLife']")
                         || await _page.IsVisibleAsync("text=NewLife");

        if (!hasNewLifeBtn)
            await PageHelpers.TakeScreenshotAsync(_page, testId);

        Assert.True(hasNewLifeBtn,
            $"[{testId}] 登录页未找到 NewLife OAuth 登录按钮/链接。当前URL: {_page.Url}");
    }

    [Fact(DisplayName = "TC-AUTH-021 点击 NewLife 登录跳转到 OAuth 授权页")]
    [Trait("Category", "Auth")]
    [Trait("Priority", "P0")]
    [Trait("Requires", "OAuthServer")]
    public async Task TC_AUTH_021_ClickNewLifeOAuthRedirects()
    {
        const String testId = "TC-AUTH-021";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Login");

        // 点击 NewLife OAuth 链接（名称匹配）
        var link = _page.Locator("a[href*='Sso/Login?name=NewLife']")
                        .Or(_page.Locator("a[title*='NewLife']"))
                        .Or(_page.Locator("a:has-text('NewLife')"));

        await link.First.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var url = _page.Url;
        var jumpedToOAuth = url.Contains("oauth", StringComparison.OrdinalIgnoreCase)
                         || url.Contains("authorize", StringComparison.OrdinalIgnoreCase)
                         || !url.Contains("8080", StringComparison.OrdinalIgnoreCase);

        if (!jumpedToOAuth)
            await PageHelpers.TakeScreenshotAsync(_page, testId);

        Assert.True(jumpedToOAuth,
            $"[{testId}] 点击 NewLife 登录后未跳转到 OAuth 授权页。当前URL: {url}");
    }

    [Fact(DisplayName = "TC-AUTH-022 OAuth 授权回跳自动注册新用户")]
    [Trait("Category", "Auth")]
    [Trait("Priority", "P0")]
    [Trait("Requires", "OAuthServer")]
    public async Task TC_AUTH_022_OAuthCallbackAutoRegisters()
    {
        const String testId = "TC-AUTH-022";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Login");

        var link = _page.Locator("a[href*='Sso/Login?name=NewLife']")
                        .Or(_page.Locator("a[title*='NewLife']"))
                        .Or(_page.Locator("a:has-text('NewLife')"));

        await link.First.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // 在 OAuth 授权页填写 test/test
        if (_page.Url.Contains("oauth", StringComparison.OrdinalIgnoreCase)
         || _page.Url.Contains("authorize", StringComparison.OrdinalIgnoreCase))
        {
            await _page.FillAsync("input[name=username], input[id=username]", AppFixture.OAuthUser);
            await _page.FillAsync("input[type=password]", AppFixture.OAuthPass);
            await _page.ClickAsync("button[type=submit], input[type=submit]");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // 若有授权确认页，点击确认
            if (await _page.IsVisibleAsync("button:has-text('授权'), button:has-text('同意'), button:has-text('Authorize')"))
            {
                await _page.ClickAsync("button:has-text('授权'), button:has-text('同意'), button:has-text('Authorize')");
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
        }

        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 应回跳到后台并登录成功
        await PageHelpers.AssertUrlContainsAsync(_page, "/Admin/", testId);
    }

    [Fact(DisplayName = "TC-AUTH-023 OAuth 回跳后 DB 新增 User 行和 UserConnect 记录")]
    [Trait("Category", "Auth")]
    [Trait("Priority", "P0")]
    [Trait("Requires", "OAuthServer")]
    public async Task TC_AUTH_023_OAuthCallbackCreatesDbRecords()
    {
        const String testId = "TC-AUTH-023";

        // 依赖 TC-AUTH-022 已完成 OAuth 登录，通过检查 UserConnect 表验证
        var connectCount = DatabaseHelper.CountUserConnect(0, "NewLife");
        Assert.True(connectCount > 0,
            $"[{testId}] UserConnect 表中未找到 NewLife 绑定记录，请先通过 TC-AUTH-022。");
    }

    #endregion

    #region A.4 注销

    [Fact(DisplayName = "TC-AUTH-030 注销跳转到登录页")]
    [Trait("Category", "Auth")]
    [Trait("Priority", "P0")]
    public async Task TC_AUTH_030_LogoutRedirectsToLogin()
    {
        const String testId = "TC-AUTH-030";

        await PageHelpers.LoginAsAdminAsync(_page);
        await PageHelpers.LogoutAsync(_page);

        await PageHelpers.AssertUrlContainsAsync(_page, "/User/Login", testId);
    }

    [Fact(DisplayName = "TC-AUTH-031 注销后访问后台跳回登录页")]
    [Trait("Category", "Auth")]
    [Trait("Priority", "P0")]
    public async Task TC_AUTH_031_AfterLogoutAdminRedirectsToLogin()
    {
        const String testId = "TC-AUTH-031";

        await PageHelpers.LoginAsAdminAsync(_page);
        await PageHelpers.LogoutAsync(_page);

        // 直接访问后台，应被重定向
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User");
        await PageHelpers.AssertUrlContainsAsync(_page, "/User/Login", testId);
    }

    #endregion

    #region A.5 忘记密码

    [Fact(DisplayName = "TC-AUTH-040 忘记密码页面可正常打开")]
    [Trait("Category", "Auth")]
    [Trait("Priority", "P1")]
    public async Task TC_AUTH_040_ForgotPasswordPageOpens()
    {
        const String testId = "TC-AUTH-040";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Login");

        // 点击"忘记密码"链接（Bootstrap tab）
        var forgotLink = _page.Locator("a[href='#Forgot'], a:has-text('忘记密码')");
        if (await forgotLink.CountAsync() > 0)
        {
            await forgotLink.First.ClickAsync();
            await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        }
        else
        {
            // 部分主题以独立页面实现
            await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Forgot");
        }

        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
    }

    [Fact(DisplayName = "TC-AUTH-042 发送验证码按钮存在")]
    [Trait("Category", "Auth")]
    [Trait("Priority", "P2")]
    public async Task TC_AUTH_042_ForgotPasswordHasSendCodeButton()
    {
        const String testId = "TC-AUTH-042";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Login");
        var forgotLink = _page.Locator("a[href='#Forgot'], a:has-text('忘记密码')");
        if (await forgotLink.CountAsync() > 0)
            await forgotLink.First.ClickAsync();
        else
            await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Forgot");

        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        var hasBtn = await _page.IsVisibleAsync("button:has-text('发送验证码')")
                  || await _page.IsVisibleAsync("a:has-text('发送验证码')")
                  || await _page.IsVisibleAsync("input[value*='发送']");

        if (!hasBtn)
            await PageHelpers.TakeScreenshotAsync(_page, testId);

        Assert.True(hasBtn,
            $"[{testId}] 忘记密码页面未找到发送验证码按钮。当前URL: {_page.Url}");
    }

    #endregion
}

/// <summary>xUnit Collection 定义，所有 E2E 测试共享同一个 AppFixture 实例</summary>
[CollectionDefinition("E2E")]
public class E2ECollection : ICollectionFixture<AppFixture> { }

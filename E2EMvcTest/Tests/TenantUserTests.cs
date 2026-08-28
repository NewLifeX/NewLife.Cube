using System;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>Session E — 租户成员友好管理界面（Issue #61）</summary>
/// <remarks>验证搜索式用户选择器（Part A）与租户成员管理页（Part B）：添加/重复拦截/移除成员全流程，含 DB 落库验证。</remarks>
[Collection("E2E")]
public sealed class TenantUserTests : IAsyncLifetime
{
    private readonly AppFixture _fixture;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // 类内共享测试数据（创建后复用，避免重复建租户/注册用户）
    private static Boolean _tenantReady;
    private static Int32 _tenantId;
    private static String _tenantName = "";

    private static Boolean _userReady;
    private static Int32 _userId;
    private static String _userName = "";

    public TenantUserTests(AppFixture fixture) => _fixture = fixture;

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

    #region 数据准备（幂等）

    /// <summary>通过后台新增租户，幂等创建并返回租户 Id</summary>
    private async Task<Int32> EnsureTenantAsync()
    {
        if (_tenantReady) return _tenantId;

        _tenantName = $"E2E租户{DateTime.Now:HHmmss}";
        var code = $"E2E{DateTime.Now:HHmmss}";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/Tenant/Add");
        await PageHelpers.AssertNoServerErrorAsync(_page, "EnsureTenant");

        await _page.FillAsync("input[name=Name]", _tenantName);
        await _page.FillAsync("input[name=Code]", code);

        // 勾选启用（chkSwitch 复选框被 label 拦截点击，改用 JS 赋值）
        var enable = _page.Locator("input[name=Enable]").First;
        if (await enable.CountAsync() > 0)
            await enable.EvaluateAsync("el => el.checked = true");

        await _page.ClickAsync("button[type=submit], input[type=submit]");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await PageHelpers.AssertNoServerErrorAsync(_page, "EnsureTenant");

        _tenantId = DatabaseHelper.GetTenantIdByName(_tenantName);
        Assert.True(_tenantId > 0, $"租户创建后未能从 DB 查询到：{_tenantName}，当前URL: {_page.Url}");
        _tenantReady = true;
        return _tenantId;
    }

    /// <summary>通过注册页创建测试用户，幂等创建并返回用户 Id</summary>
    private async Task<Int32> EnsureUserAsync()
    {
        if (_userReady) return _userId;

        _userName = $"e2etu{DateTime.Now:HHmmss}";

        // 独立上下文注册（注册成功会登录为新用户，不影响当前 admin 上下文）
        await using var ctx = await _fixture.Browser.NewContextAsync();
        var page = await ctx.NewPageAsync();
        await PageHelpers.GotoAndWaitAsync(page, "/Admin/User/Login");
        await page.ClickAsync(".login-tabs a[data-tab=Register]");
        await page.WaitForSelectorAsync("#Register.active, #Register.in");

        await page.FillAsync("#reg_pwd_username", _userName);
        await page.FillAsync("#reg_pwd_password", "Test@2026!");
        await page.FillAsync("#reg_pwd_password2", "Test@2026!");
        await page.Locator("#reg-pwd input[name=agreement]").EvaluateAsync("el => el.checked = true");
        await page.ClickAsync("#Register button[type=submit]");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        _userId = DatabaseHelper.GetUserIdByName(_userName);
        Assert.True(_userId > 0, $"注册后未能从 DB 查询到用户：{_userName}");
        _userReady = true;
        return _userId;
    }

    /// <summary>在成员管理页搜索并选中用户（bootstrap-suggest 交互）</summary>
    /// <param name="keyword">搜索关键字（用户名）</param>
    private async Task SelectUserInSearchAsync(String keyword)
    {
        // 逐键输入以触发 keyup（bootstrap-suggest 依赖 keyup 触发搜索）
        await _page.Locator("#userId_select").PressSequentiallyAsync(keyword);

        // 等待建议下拉出现。bootstrap-suggest 把结果渲染为表格行（ul>table>tbody>tr，非 li）
        await _page.WaitForSelectorAsync("#addMemberForm .dropdown-menu tbody tr",
            new PageWaitForSelectorOptions { Timeout = 8_000 });

        // 点击包含关键字的建议行（mousedown 触发选中并写入隐藏域 userId）
        var item = _page.Locator("#addMemberForm .dropdown-menu tbody tr", new PageLocatorOptions { HasText = keyword });
        Assert.True(await item.CountAsync() > 0, $"搜索'{keyword}'未出现匹配建议行");
        await item.First.ClickAsync();
    }

    /// <summary>轮询等待租户绑定关系达到期望状态（SQLite 跨进程写入可见性存在竞态，需轮询而非单次断言）</summary>
    /// <param name="tenantId">租户 Id</param>
    /// <param name="userId">用户 Id</param>
    /// <param name="exists">期望存在（true）还是不存在（false）</param>
    /// <param name="testId">测试用例 Id</param>
    /// <param name="timeoutMs">超时毫秒</param>
    private static async Task WaitTenantUserAsync(Int32 tenantId, Int32 userId, Boolean exists, String testId, Int32 timeoutMs = 5_000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            var count = DatabaseHelper.CountTenantUser(tenantId, userId);
            if (exists && count > 0) return;
            if (!exists && count == 0) return;
            await Task.Delay(300);
        }

        throw new Exception($"[{testId}] 等待租户绑定{(exists ? "出现" : "消失")}超时（租户{tenantId} 用户{userId}）");
    }

    #endregion

    #region E.1 搜索式用户选择器（Part A）

    [Fact(DisplayName = "TC-TU-010 租户用户新增表单使用搜索式用户选择器")]
    [Trait("Category", "TenantUser")]
    [Trait("Priority", "P0")]
    public async Task TC_TU_010_AddFormUsesSearchPicker()
    {
        const String testId = "TC-TU-010";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/TenantUser/Add");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 搜索输入框
        Assert.True(await _page.IsVisibleAsync("#UserId_select"),
            $"[{testId}] 未找到搜索式用户选择器输入框 #UserId_select。当前URL: {_page.Url}");
        // 隐藏域 UserId（回绑实体）。type=hidden 无盒模型不可见，需检查存在性
        Assert.True(await _page.Locator("input[type=hidden][name=UserId]").CountAsync() > 0,
            $"[{testId}] 未找到隐藏域 UserId。当前URL: {_page.Url}");
        // 旧的大下拉框应被替换
        var selectCount = await _page.Locator("select[name=UserId]").CountAsync();
        Assert.True(selectCount == 0, $"[{testId}] UserId 下拉框未被替换为搜索式选择器（数量{selectCount}）");
    }

    #endregion

    #region E.2 租户成员管理页（Part B）

    [Fact(DisplayName = "TC-TU-020 创建租户后管理员自动绑定，成员管理页加载")]
    [Trait("Category", "TenantUser")]
    [Trait("Priority", "P0")]
    public async Task TC_TU_020_TenantManagerAutoBoundAndManagePageLoads()
    {
        const String testId = "TC-TU-020";
        var tid = await EnsureTenantAsync();
        var adminId = DatabaseHelper.GetUserIdByName(AppFixture.AdminUser);

        // 管理员作为租户管理者应自动绑定
        Assert.True(DatabaseHelper.CountTenantUser(tid, adminId) > 0,
            $"[{testId}] 租户创建后管理员未被自动绑定为成员");

        // 成员管理页加载
        await PageHelpers.GotoAndWaitAsync(_page, $"/Admin/TenantUser/Manage?tenantId={tid}");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        await PageHelpers.AssertTextVisibleAsync(_page, "成员管理", testId);

        // 添加成员面板
        Assert.True(await _page.IsVisibleAsync("#addMemberForm"), $"[{testId}] 未找到添加成员面板");
        // 成员表格至少一行（管理员）
        Assert.True(await _page.IsVisibleAsync("table tbody tr"), $"[{testId}] 成员表格无数据行");
        // 管理员行显示「租户管理员」标识
        Assert.True(await _page.IsVisibleAsync("text=租户管理员"), $"[{testId}] 未显示租户管理员标识");
    }

    [Fact(DisplayName = "TC-TU-030 成员管理页通过搜索添加用户，DB 落库")]
    [Trait("Category", "TenantUser")]
    [Trait("Priority", "P0")]
    public async Task TC_TU_030_AddMemberViaSearch()
    {
        const String testId = "TC-TU-030";
        var tid = await EnsureTenantAsync();
        var uid = await EnsureUserAsync();

        // 若已绑定（顺序变化/重复运行）则直接验证
        if (DatabaseHelper.CountTenantUser(tid, uid) > 0)
        {
            await PageHelpers.GotoAndWaitAsync(_page, $"/Admin/TenantUser/Manage?tenantId={tid}");
            await PageHelpers.AssertTextVisibleAsync(_page, _userName, testId);
            return;
        }

        await PageHelpers.GotoAndWaitAsync(_page, $"/Admin/TenantUser/Manage?tenantId={tid}");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 搜索选择用户并添加
        await SelectUserInSearchAsync(_userName);
        await _page.ClickAsync("button:has-text('添加成员')");

        // 等待页面刷新后成员行出现（添加成功的确定性信号），再轮询 DB 落库
        await _page.WaitForSelectorAsync($"table tbody tr:has-text('{_userName}')",
            new PageWaitForSelectorOptions { Timeout = 10_000 });
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        await WaitTenantUserAsync(tid, uid, true, testId);
    }

    [Fact(DisplayName = "TC-TU-040 重复添加同一用户被拦截")]
    [Trait("Category", "TenantUser")]
    [Trait("Priority", "P1")]
    public async Task TC_TU_040_DuplicateAddRejected()
    {
        const String testId = "TC-TU-040";
        var tid = await EnsureTenantAsync();
        var adminId = DatabaseHelper.GetUserIdByName(AppFixture.AdminUser);

        String? alertMsg = null;
        _page.Dialog += (_, d) => { alertMsg = d.Message; _ = d.DismissAsync(); };

        await PageHelpers.GotoAndWaitAsync(_page, $"/Admin/TenantUser/Manage?tenantId={tid}");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 管理员已是租户管理员，重复添加应被拦截
        await SelectUserInSearchAsync(AppFixture.AdminUser);
        await _page.ClickAsync("button:has-text('添加成员')");

        // 等待 alert 出现
        await _page.WaitForTimeoutAsync(1_500);

        Assert.NotNull(alertMsg);
        Assert.Contains("已存在", alertMsg, StringComparison.OrdinalIgnoreCase);

        // DB 中管理员绑定数不变（仍 1）
        Assert.True(DatabaseHelper.CountTenantUser(tid, adminId) == 1,
            $"[{testId}] 重复添加后管理员绑定数不应增加");
    }

    [Fact(DisplayName = "TC-TU-050 成员管理页移除成员，DB 删除")]
    [Trait("Category", "TenantUser")]
    [Trait("Priority", "P1")]
    public async Task TC_TU_050_RemoveMember()
    {
        const String testId = "TC-TU-050";
        var tid = await EnsureTenantAsync();
        var uid = await EnsureUserAsync();

        // 若未绑定则先通过 UI 添加（顺序无关性）
        if (DatabaseHelper.CountTenantUser(tid, uid) == 0)
        {
            await PageHelpers.GotoAndWaitAsync(_page, $"/Admin/TenantUser/Manage?tenantId={tid}");
            await SelectUserInSearchAsync(_userName);
            await _page.ClickAsync("button:has-text('添加成员')");
            // 等待成员行出现后轮询 DB 落库
            await _page.WaitForSelectorAsync($"table tbody tr:has-text('{_userName}')",
                new PageWaitForSelectorOptions { Timeout = 10_000 });
            await WaitTenantUserAsync(tid, uid, true, testId);
        }

        // 接受 confirm 弹窗
        _page.Dialog += (_, d) => _ = d.AcceptAsync();

        await PageHelpers.GotoAndWaitAsync(_page, $"/Admin/TenantUser/Manage?tenantId={tid}");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 找到该用户所在行的移除按钮并点击
        var removeBtn = _page.Locator($"table tr:has-text('{_userName}') button:has-text('移除')");
        Assert.True(await removeBtn.CountAsync() > 0, $"[{testId}] 未找到用户[{_userName}]的移除按钮");
        await removeBtn.First.ClickAsync();

        // 等待成员行从表格消失（移除成功的确定性信号），再轮询 DB 删除
        await _page.WaitForSelectorAsync($"table tbody tr:has-text('{_userName}')",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Detached, Timeout = 10_000 });
        await WaitTenantUserAsync(tid, uid, false, testId);
    }

    #endregion
}

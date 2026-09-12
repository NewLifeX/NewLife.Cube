using System;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>用户页只读规则 — 非系统角色（即使授予修改权限）在用户页只读，编辑入口重定向用户中心</summary>
/// <remarks>
/// 覆盖规则：用户页（/Admin/User）对非系统角色只读——管理表单不可进入（重定向 /Admin/User/Info），
/// 资料编辑统一走用户中心；列表仍可见本人；系统角色不受影响。
/// </remarks>
[Collection("E2E")]
public sealed class UserPageReadOnlyTests : IAsyncLifetime
{
    private readonly AppFixture _fixture;
    private IBrowserContext _context = null!;
    private IBrowserContext _adminContext = null!;
    private IPage _page = null!;
    private IPage _adminPage = null!;

    private static String? _userName;
    private static Int32 _userId;
    private static Int32 _roleId;

    private const String Password = "Test@2026!";

    public UserPageReadOnlyTests(AppFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        var ts = DateTime.Now.ToString("HHmmss");
        _userName = $"e2upr{ts}";

        // 管理员上下文：注册测试用户、授予"查看+修改"权限
        _adminContext = await _fixture.Browser.NewContextAsync();
        _adminPage = await _adminContext.NewPageAsync();
        await PageHelpers.LoginAsAdminAsync(_adminPage);

        // 先访问一个实体页，确保 Cube 区域菜单完成扫描注册
        await PageHelpers.GotoAndWaitAsync(_adminPage, "/Cube/Attachment");

        await RegisterAsync(_userName);

        _userId = DatabaseHelper.GetUserIdByName(_userName);
        Assert.True(_userId > 0, $"注册后未从数据库找到用户：{_userName}");

        var roleId = DatabaseHelper.GetUserRoleId(_userName);
        Assert.True(roleId > 0, $"注册用户未分配角色：{_userName}");
        _roleId = roleId;
        await SetRolePermissionAsync(roleId, true);

        // 普通用户上下文
        _context = await _fixture.Browser.NewContextAsync();
        _page = await _context.NewPageAsync();
        await PageHelpers.LoginAsync(_page, _userName, Password);
    }

    public async Task DisposeAsync()
    {
        // 恢复角色为只读基线，避免影响其它用例（如只读用户的批量按钮断言）
        if (_roleId > 0 && _adminPage != null)
        {
            try { await SetRolePermissionAsync(_roleId, false); }
            catch { /* 收尾失败不改变测试结论 */ }
        }

        if (_context != null) await _context.DisposeAsync();
        if (_adminContext != null) await _adminContext.DisposeAsync();
    }

    [Fact(DisplayName = "TC-UPRO-001 有修改权限的普通用户访问编辑页重定向到用户中心")]
    [Trait("Category", "DataScope")]
    [Trait("Priority", "P0")]
    public async Task TC_UPRO_001_EditRedirectsToUserCenter()
    {
        const String testId = "TC-UPRO-001";

        await PageHelpers.GotoAndWaitAsync(_page, $"/Admin/User/Edit?id={_userId}");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 编辑入口应重定向到用户中心基本信息页
        Assert.Contains("/Admin/User/Info", _page.Url, StringComparison.OrdinalIgnoreCase);
        await PageHelpers.AssertTextNotVisibleAsync(_page, "无法取得编号", testId);

        // 用户中心特征：显示名可编辑
        var hasField = await _page.IsVisibleAsync("input[name=DisplayName], label:has-text('显示名')");
        Assert.True(hasField, $"[{testId}] 未渲染用户中心资料页。URL={_page.Url}");

        // 管理表单字段（角色选择）不应出现
        Assert.Equal(0, await _page.Locator("select[name=RoleID]").CountAsync());

        // 非系统角色不显示管理表单入口标签
        Assert.Equal(0, await _page.Locator("a[href*='/Admin/User/Edit']").CountAsync());
    }

    [Fact(DisplayName = "TC-UPRO-002 普通用户用户页列表仍可见本人")]
    [Trait("Category", "DataScope")]
    [Trait("Priority", "P0")]
    public async Task TC_UPRO_002_ListStillShowsSelf()
    {
        const String testId = "TC-UPRO-002";

        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        var rows = _page.Locator("table tbody tr");
        var count = await rows.CountAsync();
        Assert.True(count == 1, $"[{testId}] 用户列表应只有本人一行，实际 {count} 行。URL={_page.Url}");

        var text = await rows.First.InnerTextAsync();
        Assert.Contains(_userName!, text, StringComparison.OrdinalIgnoreCase);
    }

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

    /// <summary>把指定角色设为“全部页面查看 + 可选修改”。先重置其它权限位，保证结果与前置状态无关</summary>
    /// <param name="roleId">角色 Id</param>
    /// <param name="withUpdate">是否同时授予“修改”权限位</param>
    private async Task SetRolePermissionAsync(Int32 roleId, Boolean withUpdate)
    {
        await PageHelpers.GotoAndWaitAsync(_adminPage, $"/Admin/Role/Edit/{roleId}");
        await PageHelpers.AssertNoServerErrorAsync(_adminPage, "TC-UPRO-GRANT");

        // 统一重置：菜单资源 p{id} 与“查看” pf{id}_1 勾选；“修改” pf{id}_4 按需；“其余权限位全部取消
        var updateFlag = withUpdate ? "true" : "false";
        var checkedCount = await _adminPage.EvaluateAsync<Int32>(
            """
            (updateFlag) => {
                let n = 0;
                document.querySelectorAll('input[type=checkbox]').forEach(el => {
                    const name = el.name || '';
                    if (/^p\d+$/.test(name) || /^pf\d+_1$/.test(name)) { el.checked = true; n++; }
                    else if (/^pf\d+_4$/.test(name)) { el.checked = updateFlag === 'true'; n++; }
                    else if (/^pf\d+_\d+$/.test(name)) { el.checked = false; n++; }
                });
                return n;
            }
            """, updateFlag);
        Assert.True(checkedCount > 5, $"角色编辑页未找到权限复选框，已处理 {checkedCount} 项");

        await _adminPage.ClickAsync("button[type=submit], input[type=submit]");
        await _adminPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await PageHelpers.AssertNoServerErrorAsync(_adminPage, "TC-UPRO-GRANT-SAVE");

        // 数据库校验：查看（1）必存，修改（4）按参数存在。父级菜单以 -1（全部）表示，不参与位判断
        var permission = DatabaseHelper.GetRolePermission(roleId);
        Assert.True(permission != null, $"角色[{roleId}]权限为空");

        var hasDetail = false;
        var hasUpdate = false;
        foreach (var item in permission!.Split(','))
        {
            var ps = item.Split('#');
            if (ps.Length != 2 || !Int32.TryParse(ps[1], out var v) || v == -1) continue;

            if ((v & 1) != 0) hasDetail = true;
            if ((v & 1) != 0 && (v & 4) != 0) hasUpdate = true;
        }

        Assert.True(hasDetail, $"角色[{roleId}]查看权限未保存，Permission={permission}");
        Assert.Equal(withUpdate, hasUpdate);
    }

    #endregion
}

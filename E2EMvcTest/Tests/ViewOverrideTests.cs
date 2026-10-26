using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>视图覆写机制验证（TC-OVR-001 ~ TC-OVR-007）</summary>
/// <remarks>
/// 验证魔方七级视图覆盖优先级中两级关键链路在真实运行的应用中生效：
/// 主题级（/Views/{Theme}/ 覆盖 /Views/Shared/）与控制器级（/Areas/{Area}/Views/{Controller}/ 覆盖主题与共享分片），
/// 并验证控制器级覆写的作用域隔离（只影响目标控制器）。断言基于服务端原始 HTML 与渲染后的 DOM，不依赖界面语言。
/// </remarks>
[Collection("E2E")]
public sealed class ViewOverrideTests : IAsyncLifetime
{
    /// <summary>ACE 主题操作列分片标记：按钮化后带 op-btn 类</summary>
    private const String AceActionClass = "op-btn-op-edit";

    /// <summary>共享操作列分片标记：纯图标 + 蓝色内联样式</summary>
    private const String SharedActionMarker = "<i class=\"glyphicon glyphicon-edit\" style=\"color: blue;\"></i>";

    /// <summary>共享分页分片标记：GO 提交按钮</summary>
    private const String SharedPagerMarker = "value=\"GO\"";

    /// <summary>控制器级覆写标记：用户表单按钮区的清空密码链接</summary>
    private const String ClearPasswordMarker = "ClearPassword";

    private readonly AppFixture _fixture;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    /// <summary>实例化测试类</summary>
    /// <param name="fixture">全局 E2E 夹具</param>
    public ViewOverrideTests(AppFixture fixture) => _fixture = fixture;

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

    #region 辅助

    /// <summary>取服务端原始 HTML（携带当前登录 Cookie，不执行页面脚本）</summary>
    /// <param name="path">相对路径</param>
    /// <returns>状态码与响应正文</returns>
    private async Task<(Int32 Status, String Html)> GetRawAsync(String path)
    {
        var resp = await _page.APIRequest.GetAsync(AppFixture.BaseUrl + path);
        var html = await resp.TextAsync();

        return (resp.Status, html);
    }

    /// <summary>提取表单按钮区容器的 class 属性值</summary>
    /// <param name="html">响应正文</param>
    /// <returns>class 属性值，未渲染按钮区时返回空串</returns>
    private static String ExtractFormActionsClass(String html)
    {
        var m = Regex.Match(html, "class=\"([^\"]*form-actions[^\"]*)\"");

        return m.Success ? m.Groups[1].Value : "";
    }

    #endregion

    #region 主题级覆写

    [Fact(DisplayName = "TC-OVR-001 主题级覆写：列表操作列与分页器使用 ACE 主题分片")]
    [Trait("Category", "ViewOverride")]
    [Trait("Priority", "P0")]
    public async Task TC_OVR_001_ThemeLevelOverride_ListFragments()
    {
        const String testId = "TC-OVR-001";
        var (status, html) = await GetRawAsync("/Admin/User");
        Assert.True(status == 200, $"[{testId}] 期望 200，实际 {status}");

        Assert.True(html.Contains(AceActionClass, StringComparison.Ordinal), $"[{testId}] 操作列未命中 ACE 主题分片");
        Assert.True(html.Contains("pager-btns", StringComparison.Ordinal), $"[{testId}] 分页器未命中 ACE 主题分片");
        Assert.False(html.Contains(SharedActionMarker, StringComparison.Ordinal), $"[{testId}] 操作列同时渲染了共享兜底分片，说明主题分片未优先");
        Assert.False(html.Contains(SharedPagerMarker, StringComparison.Ordinal), $"[{testId}] 分页器同时渲染了共享兜底分片，说明主题分片未优先");
    }

    [Fact(DisplayName = "TC-OVR-002 主题级覆写：表单按钮区使用 ACE 主题分片")]
    [Trait("Category", "ViewOverride")]
    [Trait("Priority", "P0")]
    public async Task TC_OVR_002_ThemeLevelOverride_FormAction()
    {
        const String testId = "TC-OVR-002";
        var (status, html) = await GetRawAsync("/Admin/Role/Add");
        Assert.True(status == 200, $"[{testId}] 期望 200，实际 {status}");

        // 角色控制器没有控制器级覆写，按钮区应命中主题分片：去掉共享分版的 grid 占位 label
        var css = ExtractFormActionsClass(html);
        Assert.True(css.Length > 0, $"[{testId}] 未渲染表单按钮区");
        Assert.DoesNotContain("col-xs-12", css);
        Assert.DoesNotContain("col-sm-12", css);
    }

    #endregion

    #region 控制器级覆写

    [Fact(DisplayName = "TC-OVR-003 控制器级覆写：用户表单按钮区命中控制器视图")]
    [Trait("Category", "ViewOverride")]
    [Trait("Priority", "P0")]
    public async Task TC_OVR_003_ControllerLevelOverride_UserFormAction()
    {
        const String testId = "TC-OVR-003";
        var (status, html) = await GetRawAsync("/Admin/User/Add");
        Assert.True(status == 200, $"[{testId}] 期望 200，实际 {status}");

        Assert.True(html.Contains(ClearPasswordMarker, StringComparison.Ordinal),
            $"[{testId}] 未命中 /Areas/Admin/Views/User/_Form_Action.cshtml 的控制器级覆写");

        // 控制器分片保留了共享分版的 grid 占位，可与主题分片区分
        var css = ExtractFormActionsClass(html);
        Assert.Contains("form-actions", css);
        Assert.Contains("col-sm-12", css);
    }

    [Fact(DisplayName = "TC-OVR-004 控制器级覆写作用域隔离：其它控制器不受影响")]
    [Trait("Category", "ViewOverride")]
    [Trait("Priority", "P0")]
    public async Task TC_OVR_004_ControllerLevelOverride_Scoped()
    {
        const String testId = "TC-OVR-004";
        var (status, html) = await GetRawAsync("/Admin/Role/Add");
        Assert.True(status == 200, $"[{testId}] 期望 200，实际 {status}");

        Assert.False(html.Contains(ClearPasswordMarker, StringComparison.Ordinal),
            $"[{testId}] 角色表单不应出现用户控制器专属的覆写内容");
    }

    [Fact(DisplayName = "TC-OVR-005 控制器级覆写：用户列表搜索区命中控制器视图")]
    [Trait("Category", "ViewOverride")]
    [Trait("Priority", "P1")]
    public async Task TC_OVR_005_ControllerLevelOverride_UserListSearch()
    {
        const String testId = "TC-OVR-005";
        var (status, html) = await GetRawAsync("/Admin/User");
        Assert.True(status == 200, $"[{testId}] 期望 200，实际 {status}");

        Assert.True(html.Contains("toolbar-link", StringComparison.Ordinal), $"[{testId}] 未渲染控制器级搜索区快捷链接");
        Assert.True(html.Contains("name=\"roleIds\"", StringComparison.Ordinal), $"[{testId}] 未渲染控制器级角色筛选");
        Assert.True(html.Contains("name=\"departmentId\"", StringComparison.Ordinal), $"[{testId}] 未渲染控制器级部门筛选");

        var (roleStatus, roleHtml) = await GetRawAsync("/Admin/Role");
        Assert.True(roleStatus == 200, $"[{testId}] 角色列表期望 200，实际 {roleStatus}");
        Assert.False(roleHtml.Contains("roleIds", StringComparison.Ordinal), $"[{testId}] 角色列表不应出现用户控制器的搜索项");
    }

    [Fact(DisplayName = "TC-OVR-006 控制器级覆写：日志列表搜索区命中控制器视图")]
    [Trait("Category", "ViewOverride")]
    [Trait("Priority", "P1")]
    public async Task TC_OVR_006_ControllerLevelOverride_LogListSearch()
    {
        const String testId = "TC-OVR-006";
        var (status, html) = await GetRawAsync("/Admin/Log");
        Assert.True(status == 200, $"[{testId}] 期望 200，实际 {status}");

        Assert.True(html.Contains("name=\"category\"", StringComparison.Ordinal), $"[{testId}] 未渲染分类筛选");
        Assert.True(html.Contains("name=\"act\"", StringComparison.Ordinal), $"[{testId}] 未渲染操作类型筛选");
        Assert.True(html.Contains("name=\"userId\"", StringComparison.Ordinal), $"[{testId}] 未渲染用户筛选");
    }

    [Fact(DisplayName = "TC-OVR-007 控制器级覆写在渲染后的页面中可见")]
    [Trait("Category", "ViewOverride")]
    [Trait("Priority", "P1")]
    public async Task TC_OVR_007_ControllerLevelOverride_RenderedOnPage()
    {
        const String testId = "TC-OVR-007";
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        Assert.True(await _page.IsVisibleAsync("a.toolbar-link"), $"[{testId}] 控制器级覆写的快捷链接不可见");
        // 角色多选由 bootstrap-multiselect 渲染：原生 select 被隐藏，页面上可见的是其下拉按钮
        Assert.True(await _page.IsVisibleAsync("button.multiselect.dropdown-toggle"), $"[{testId}] 控制器级覆写的角色筛选不可见");
    }

    #endregion
}

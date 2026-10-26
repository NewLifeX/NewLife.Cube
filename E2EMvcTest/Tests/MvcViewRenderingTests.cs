using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>MVC Razor 视图引擎实际渲染验证（TC-MVC-001 ~ TC-MVC-006）</summary>
/// <remarks>
/// 单元测试只覆盖视图定位辅助（候选路径优先级、插入规则），本组用例补齐浏览器级的真实渲染验证：
/// 直接读取服务端原始 HTML（不执行任何前端脚本），证明列表/表单/详情/布局均由 Razor 服务端渲染出结构，
/// 而不是依赖前端脚本拼装。页面文案随界面语言变化，断言只使用结构与主题标记，避免语言耦合。
/// </remarks>
[Collection("E2E")]
public sealed class MvcViewRenderingTests : IAsyncLifetime
{
    private readonly AppFixture _fixture;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    /// <summary>实例化测试类</summary>
    /// <param name="fixture">全局 E2E 夹具</param>
    public MvcViewRenderingTests(AppFixture fixture) => _fixture = fixture;

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

    /// <summary>断言响应为 200 且 HTML 同时包含全部片段</summary>
    /// <param name="testId">用例编号，用于失败信息</param>
    /// <param name="status">HTTP 状态码</param>
    /// <param name="html">响应正文</param>
    /// <param name="snippets">必须出现的片段</param>
    private static void AssertHtmlContains(String testId, Int32 status, String html, params String[] snippets)
    {
        Assert.True(status == 200, $"[{testId}] 期望 200，实际 {status}");
        foreach (var item in snippets)
        {
            Assert.True(html.Contains(item, StringComparison.OrdinalIgnoreCase), $"[{testId}] 服务端渲染的 HTML 缺少片段：{item}");
        }
    }

    #endregion

    #region Razor 渲染

    [Fact(DisplayName = "TC-MVC-001 列表页由 Razor 服务端渲染表格、数据行与分页分片")]
    [Trait("Category", "MvcView")]
    [Trait("Priority", "P0")]
    public async Task TC_MVC_001_ListPageServerRendered()
    {
        const String testId = "TC-MVC-001";
        var (status, html) = await GetRawAsync("/Admin/User");

        AssertHtmlContains(testId, status, html,
            "table-data-list",      // 列表主表格
            "<thead>",
            "href=\"?Sort=",        // 字段表头排序链接由服务端生成
            "<td",                  // 数据单元格已随响应下发
            "id=\"chkAll\"",        // 批量选择列
            "pager-btns",           // 分页分片（主题版本）
            "id=\"breadcrumbs\"");  // 面包屑分片
    }

    [Fact(DisplayName = "TC-MVC-002 新增表单页由 Razor 服务端渲染表单控件与防伪令牌")]
    [Trait("Category", "MvcView")]
    [Trait("Priority", "P0")]
    public async Task TC_MVC_002_AddFormServerRendered()
    {
        const String testId = "TC-MVC-002";
        var (status, html) = await GetRawAsync("/Admin/User/Add");

        AssertHtmlContains(testId, status, html,
            "<form",
            "method=\"post\"",
            "name=\"__RequestVerificationToken\"",  // 防伪令牌由服务端注入
            "name=\"Name\"",                        // 字段控件
            "nav nav-tabs",                         // 分组标签页
            "type=\"submit\"");
    }

    [Fact(DisplayName = "TC-MVC-003 编辑表单页由服务端回填当前记录值")]
    [Trait("Category", "MvcView")]
    [Trait("Priority", "P0")]
    public async Task TC_MVC_003_EditFormPrefilledByServer()
    {
        const String testId = "TC-MVC-003";

        // 以详情页的字段值为基准，避免用例依赖具体账号数据
        var (detailStatus, detailHtml) = await GetRawAsync("/Admin/User/Detail?id=1");
        Assert.True(detailStatus == 200, $"[{testId}] 详情页期望 200，实际 {detailStatus}");
        var expected = Regex.Match(detailHtml, "id=\"Name\"[^>]*value=\"([^\"]*)\"");
        Assert.True(expected.Success, $"[{testId}] 详情页未渲染出 Name 字段值");

        var (status, html) = await GetRawAsync("/Admin/User/Edit?id=1");
        AssertHtmlContains(testId, status, html, "<form", "name=\"Name\"");
        var actual = Regex.Match(html, "id=\"Name\"[^>]*value=\"([^\"]*)\"");
        Assert.True(actual.Success, $"[{testId}] 编辑页未渲染出 Name 输入框");
        Assert.Equal(expected.Groups[1].Value, actual.Groups[1].Value);
    }

    [Fact(DisplayName = "TC-MVC-004 详情页由 Razor 服务端渲染字段标签与值")]
    [Trait("Category", "MvcView")]
    [Trait("Priority", "P1")]
    public async Task TC_MVC_004_DetailPageServerRendered()
    {
        const String testId = "TC-MVC-004";
        var (status, html) = await GetRawAsync("/Admin/User/Detail?id=1");

        AssertHtmlContains(testId, status, html,
            "form-horizontal",
            "nav nav-tabs",     // 分组标签页
            "tab-pane",         // 分组内容区
            "<label",           // 字段标签
            "id=\"Name\"");     // 字段值（只读控件）
    }

    [Fact(DisplayName = "TC-MVC-005 主题布局由 Razor 渲染（页面标题后缀与主题样式）")]
    [Trait("Category", "MvcView")]
    [Trait("Priority", "P1")]
    public async Task TC_MVC_005_LayoutRenderedByRazor()
    {
        const String testId = "TC-MVC-005";
        var (status, html) = await GetRawAsync("/Admin/User");

        AssertHtmlContains(testId, status, html,
            "<!DOCTYPE html>",
            "<html lang=",
            "<title>",
            "id=\"main-ace-style\"",    // ACE 主题样式表（主题布局生效）
            "ace-ui.css");

        var title = Regex.Match(html, "<title>(.*?)</title>", RegexOptions.Singleline).Groups[1].Value;
        Assert.True(title.Contains(" - "), $"[{testId}] 页面标题未按配置追加站点名后缀：{title}");
    }

    [Fact(DisplayName = "TC-MVC-006 框架页 iframe 结构与导航由 Razor 服务端渲染")]
    [Trait("Category", "MvcView")]
    [Trait("Priority", "P1")]
    public async Task TC_MVC_006_FramePageServerRendered()
    {
        const String testId = "TC-MVC-006";
        var (status, html) = await GetRawAsync("/Admin");

        AssertHtmlContains(testId, status, html,
            "id=\"navbar\"",
            "id=\"sidebar\"",
            "id=\"main\"",
            "<iframe");

        // 首页皮肤由 ThemeViewLocationExpander 特殊处理（Admin/Index/Index 取 Skin）
        Assert.True(html.Contains("/Admin/Index/", StringComparison.OrdinalIgnoreCase), $"[{testId}] 框架页未渲染内容区入口");
    }

    #endregion
}

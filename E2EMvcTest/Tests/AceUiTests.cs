using System;
using System.Linq;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>Session D — ACE 默认皮肤 UI/UX 界面正确性验证（TC-ACE-001 ~ TC-ACE-0xx）</summary>
/// <remarks>
/// 验证 ACE 主题各界面正确显示、无严重视觉偏差：
/// 后台框架布局、列表页（表格/操作列/分页/搜索）、表单页（新增/编辑）、首页仪表盘、皮肤资源与关键样式生效。
/// 被测应用 CubeSSO 引用 NewLife.CubeNC，默认主题 ACE（未显式配置 Theme 时回退 ACE）。
/// </remarks>
[Collection("E2E")]
public sealed class AceUiTests : IAsyncLifetime
{
    private readonly AppFixture _fixture;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public AceUiTests(AppFixture fixture) => _fixture = fixture;

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

    #region D.1 后台框架布局

    [Fact(DisplayName = "TC-ACE-001 后台框架页正常加载（导航栏/侧边栏/iframe）")]
    [Trait("Category", "AceUi")]
    [Trait("Priority", "P0")]
    public async Task TC_ACE_001_FrameworkLayout()
    {
        const String testId = "TC-ACE-001";
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 顶部导航栏
        Assert.True(await _page.IsVisibleAsync("#navbar"), $"[{testId}] 顶部导航栏 #navbar 不存在");
        // 左侧菜单
        Assert.True(await _page.IsVisibleAsync("#sidebar"), $"[{testId}] 左侧菜单 #sidebar 不存在");
        // iframe 内容区
        Assert.True(await _page.IsVisibleAsync("#main"), $"[{testId}] iframe 内容区 #main 不存在");
        // 品牌区显示系统名
        var brand = await _page.Locator("#navbar .navbar-brand").TextContentAsync();
        Assert.False(String.IsNullOrWhiteSpace(brand), $"[{testId}] 导航栏品牌区为空");
        // 侧边栏菜单项存在
        var menuCount = await _page.Locator("#sidebar .nav-list > li").CountAsync();
        Assert.True(menuCount > 0, $"[{testId}] 侧边栏无菜单项");
    }

    [Fact(DisplayName = "TC-ACE-002 ACE 主题资源加载且关键样式生效")]
    [Trait("Category", "AceUi")]
    [Trait("Priority", "P0")]
    public async Task TC_ACE_002_ThemeResources()
    {
        const String testId = "TC-ACE-002";
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // ace-ui.css 已加载
        var hasAceUi = await _page.Locator("link[rel=stylesheet][href*='ace-ui.css']").CountAsync();
        Assert.True(hasAceUi > 0, $"[{testId}] ACE 主题样式 ace-ui.css 未加载");

        // body 浅灰蓝背景（#f0f2f5）
        var bodyBg = await _page.EvaluateAsync<String>("getComputedStyle(document.body).backgroundColor");
        Assert.Equal("rgb(240, 242, 245)", bodyBg);

        // 内容区白卡片
        var contentBg = await _page.EvaluateAsync<String>(
            "(() => { const el = document.querySelector('.content-wrapper'); return el ? getComputedStyle(el).backgroundColor : ''; })()");
        Assert.Equal("rgb(255, 255, 255)", contentBg);

        // 内容卡片圆角
        var contentRadius = await _page.EvaluateAsync<Int32>(
            "(() => { const el = document.querySelector('.content-wrapper'); if (!el) return 0; return parseFloat(getComputedStyle(el).borderRadius) || 0; })()");
        Assert.True(contentRadius > 0, $"[{testId}] 内容卡片圆角为 0，卡片化样式未生效");
    }

    [Fact(DisplayName = "TC-ACE-003 框架页导航栏与侧边栏交互")]
    [Trait("Category", "AceUi")]
    [Trait("Priority", "P1")]
    public async Task TC_ACE_003_NavbarSidebar()
    {
        const String testId = "TC-ACE-003";
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 导航栏为 ACE 原生蓝色（#438eb9）
        var navbarBg = await _page.EvaluateAsync<String>("getComputedStyle(document.querySelector('#navbar')).backgroundColor");
        Assert.Equal("rgb(67, 142, 185)", navbarBg);

        // 用户/租户菜单项为 ACE 原生浅蓝（#62a8d1，未展开状态下）
        var userABg = await _page.EvaluateAsync<String>(
            "getComputedStyle(document.querySelector('#navbar .ace-nav > li.light-blue > a')).backgroundColor");
        Assert.Equal("rgb(98, 168, 209)", userABg);

        // 侧边栏为 ACE 原生浅灰（#f2f2f2）
        var sidebarBg = await _page.EvaluateAsync<String>("getComputedStyle(document.querySelector('#sidebar')).backgroundColor");
        Assert.Equal("rgb(242, 242, 242)", sidebarBg);

        // 一级菜单项为 ACE 原生浅灰背景（#f8f8f8）
        var topBg = await _page.EvaluateAsync<String>(
            "(() => { const a = document.querySelector('#sidebar .nav-list > li:not(.active) > a'); return a ? getComputedStyle(a).backgroundColor : ''; })()");
        Assert.Equal("rgb(248, 248, 248)", topBg);

        // 二级菜单图标显示（功能修复：ACE 原生隐藏了 submenu 图标）
        var subIconDisplay = await _page.EvaluateAsync<String>(
            "(() => { const el = document.querySelector('#sidebar .submenu > li > a .menu-icon'); return el ? getComputedStyle(el).display : ''; })()");
        Assert.True(!String.IsNullOrEmpty(subIconDisplay) && subIconDisplay != "none",
            $"[{testId}] 二级菜单图标未显示（display={subIconDisplay}）");

        // 用户菜单下拉可用（点击后出现菜单项）
        var userMenu = _page.Locator("#navbar .navbar-buttons .ace-nav > li:last-child > a");
        Assert.True(await userMenu.IsVisibleAsync(), $"[{testId}] 导航栏用户菜单不存在");
        await userMenu.EvaluateAsync("el => el.click()");
        await _page.WaitForTimeoutAsync(300);
        Assert.True(await _page.IsVisibleAsync("#navbar .dropdown-menu"), $"[{testId}] 用户菜单下拉未展开");

        // 侧边栏菜单展开态：ACE 默认首个一级菜单 active open，断言存在可见子菜单（nav-show）
        var visibleSub = _page.Locator("#sidebar .submenu.nav-show").First;
        Assert.True(await visibleSub.CountAsync() > 0 && await visibleSub.IsVisibleAsync(),
            $"[{testId}] 侧边栏无可见子菜单");
    }

    #endregion

    #region D.2 列表页

    [Fact(DisplayName = "TC-ACE-010 列表页表格与操作列正确渲染")]
    [Trait("Category", "AceUi")]
    [Trait("Priority", "P0")]
    public async Task TC_ACE_010_ListTableAndAction()
    {
        const String testId = "TC-ACE-010";
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 表格存在
        Assert.True(await _page.IsVisibleAsync(".table-data-list"), $"[{testId}] 列表表格 .table-data-list 不存在");
        // 表头存在
        Assert.True(await _page.IsVisibleAsync(".table-data-list thead th"), $"[{testId}] 表头不存在");
        // 有数据行
        var rowCount = await _page.Locator(".table-data-list tbody tr").CountAsync();
        Assert.True(rowCount > 0, $"[{testId}] 用户列表无数据行");
        // 操作列按钮（编辑/删除）存在
        var opCount = await _page.Locator(".table-data-list .op-btn").CountAsync();
        Assert.True(opCount > 0, $"[{testId}] 操作列未按钮化（无 .op-btn）");
        // 编辑按钮保留 editcell（双击进入表单依赖）
        Assert.True(await _page.Locator(".op-btn.editcell").CountAsync() > 0, $"[{testId}] 编辑按钮缺少 editcell 类");
    }

    [Fact(DisplayName = "TC-ACE-011 列表页分页器与搜索区正常")]
    [Trait("Category", "AceUi")]
    [Trait("Priority", "P1")]
    public async Task TC_ACE_011_ListPagerAndSearch()
    {
        const String testId = "TC-ACE-011";
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 分页信息区
        Assert.True(await _page.IsVisibleAsync(".pager-info"), $"[{testId}] 分页信息区 .pager-info 不存在");
        // 分页按钮（页码/上下页）
        var pagerCount = await _page.Locator(".pager-btn").CountAsync();
        Assert.True(pagerCount > 0, $"[{testId}] 分页按钮 .pager-btn 不存在");
        // 当前页高亮（第 1 页）
        Assert.True(await _page.IsVisibleAsync(".pager-btn-current"), $"[{testId}] 当前页高亮 .pager-btn-current 不存在");
        // 跳页输入框与页大小下拉
        Assert.True(await _page.IsVisibleAsync(".pager-jump input[name=PageIndex]"), $"[{testId}] 跳页输入框不存在");
        Assert.True(await _page.IsVisibleAsync("#PageSize"), $"[{testId}] 页大小下拉不存在");
        // 搜索框与查询按钮
        Assert.True(await _page.IsVisibleAsync("input[name=q]"), $"[{testId}] 搜索框 input[name=q] 不存在");
        Assert.True(await _page.IsVisibleAsync(".input-group-btn .btn"), $"[{testId}] 查询按钮不存在");

        // 搜索交互：输入关键词后结果行变化
        var before = await _page.Locator(".table-data-list tbody tr").CountAsync();
        await _page.FillAsync("input[name=q]", "不存在关键词xyz");
        // Enter/点击在个别布局下不稳定，用 requestSubmit 确保 POST 提交
        await _page.EvaluateAsync("document.querySelector('input[name=q]')?.closest('form')?.requestSubmit()");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var after = await _page.Locator(".table-data-list tbody tr").CountAsync();
        Assert.True(after <= before, $"[{testId}] 搜索后结果行未减少（before={before}, after={after}）");
    }

    [Fact(DisplayName = "TC-ACE-012 列表页视觉无严重偏差（样式生效）")]
    [Trait("Category", "AceUi")]
    [Trait("Priority", "P1")]
    public async Task TC_ACE_012_ListVisualStyle()
    {
        const String testId = "TC-ACE-012";
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 表格圆角 > 0
        var tableRadius = await _page.EvaluateAsync<Int32>(
            "(() => { const el = document.querySelector('.table-data-list'); return el ? parseFloat(getComputedStyle(el).borderRadius) || 0 : 0; })()");
        Assert.True(tableRadius > 0, $"[{testId}] 表格圆角为 0");
        // 表头浅蓝底（#f5f8fc）
        var theadBg = await _page.EvaluateAsync<String>(
            "(() => { const el = document.querySelector('.table-data-list thead th'); return el ? getComputedStyle(el).backgroundColor : ''; })()");
        Assert.Equal("rgb(245, 248, 252)", theadBg);
        // 操作按钮圆角 > 0
        var opRadius = await _page.EvaluateAsync<Int32>(
            "(() => { const el = document.querySelector('.op-btn'); return el ? parseFloat(getComputedStyle(el).borderRadius) || 0 : 0; })()");
        Assert.True(opRadius > 0, $"[{testId}] 操作按钮圆角为 0");
        // 查询按钮回归 ACE 原生 btn-purple 紫色（#9583bf）
        var searchBg = await _page.EvaluateAsync<String>(
            "(() => { const el = document.querySelector('.input-group-btn .btn'); return el ? getComputedStyle(el).backgroundColor : ''; })()");
        Assert.Equal("rgb(149, 133, 191)", searchBg);
    }

    #endregion

    #region D.3 表单页

    [Fact(DisplayName = "TC-ACE-020 新增表单页结构完整（Tab/字段/按钮区）")]
    [Trait("Category", "AceUi")]
    [Trait("Priority", "P0")]
    public async Task TC_ACE_020_AddForm()
    {
        const String testId = "TC-ACE-020";
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Add");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 表单容器
        Assert.True(await _page.IsVisibleAsync(".form-horizontal"), $"[{testId}] 表单容器 .form-horizontal 不存在");
        // 分组 Tab（用户表单有 默认/登录信息/扩展）
        var tabCount = await _page.Locator(".form-horizontal .nav-tabs > li").CountAsync();
        Assert.True(tabCount > 0, $"[{testId}] 表单分组 Tab 不存在");
        // 字段存在
        Assert.True(await _page.IsVisibleAsync(".form-horizontal .form-group"), $"[{testId}] 表单项 .form-group 不存在");
        // 输入控件
        Assert.True(await _page.IsVisibleAsync(".form-horizontal .form-control"), $"[{testId}] 表单输入控件不存在");
        // 按钮区（新增/取消）
        Assert.True(await _page.IsVisibleAsync(".form-actions"), $"[{testId}] 表单按钮区 .form-actions 不存在");
        var btnTexts = await _page.Locator(".form-actions .btn").AllTextContentsAsync();
        var joined = String.Join("|", btnTexts);
        Assert.True(joined.Contains("新增"), $"[{testId}] 新增按钮缺失，实际按钮: {joined}");
        Assert.True(joined.Contains("取消"), $"[{testId}] 取消按钮缺失，实际按钮: {joined}");

        // 表单视觉：按钮圆角 > 0、输入框圆角 > 0
        var btnRadius = await _page.EvaluateAsync<Int32>(
            "(() => { const el = document.querySelector('.form-actions .btn'); return el ? parseFloat(getComputedStyle(el).borderRadius) || 0 : 0; })()");
        Assert.True(btnRadius > 0, $"[{testId}] 表单按钮圆角为 0");
        var inputRadius = await _page.EvaluateAsync<Int32>(
            "(() => { const el = document.querySelector('.form-horizontal input.form-control'); return el ? parseFloat(getComputedStyle(el).borderRadius) || 0 : 0; })()");
        Assert.True(inputRadius > 0, $"[{testId}] 表单输入框圆角为 0");
    }

    [Fact(DisplayName = "TC-ACE-021 编辑表单页值回填与 Tab 切换")]
    [Trait("Category", "AceUi")]
    [Trait("Priority", "P1")]
    public async Task TC_ACE_021_EditForm()
    {
        const String testId = "TC-ACE-021";
        // 编辑 admin 用户
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Edit?id=1");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 表单加载
        Assert.True(await _page.IsVisibleAsync(".form-horizontal"), $"[{testId}] 编辑表单未加载");
        // 名称字段回填 admin
        var nameVal = await _page.InputValueAsync("input[name=Name]");
        Assert.False(String.IsNullOrWhiteSpace(nameVal), $"[{testId}] 编辑表单名称字段为空（未回填）");

        // Tab 切换：点击"登录信息"
        var loginTab = _page.Locator(".form-horizontal .nav-tabs a", new PageLocatorOptions { HasText = "登录信息" });
        if (await loginTab.CountAsync() > 0)
        {
            await loginTab.EvaluateAsync("el => el.click()");
            await _page.WaitForTimeoutAsync(300);
            // 激活态蓝色下划线
            var activeBorder = await _page.EvaluateAsync<String>(
                "(() => { const el = document.querySelector('.form-horizontal .nav-tabs > li.active > a'); return el ? getComputedStyle(el).borderBottomColor : ''; })()");
            Assert.Equal("rgb(43, 125, 188)", activeBorder);
        }

        // 按钮区（保存/取消）
        var btnTexts = await _page.Locator(".form-actions .btn").AllTextContentsAsync();
        var joined = String.Join("|", btnTexts);
        Assert.True(joined.Contains("保存"), $"[{testId}] 保存按钮缺失，实际按钮: {joined}");
    }

    #endregion

    #region D.4 首页仪表盘

    [Fact(DisplayName = "TC-ACE-030 首页服务器信息页卡片化显示")]
    [Trait("Category", "AceUi")]
    [Trait("Priority", "P1")]
    public async Task TC_ACE_030_IndexMain()
    {
        const String testId = "TC-ACE-030";
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/Index/Main");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 服务器信息表格存在
        var tableCount = await _page.Locator(".table-data-list").CountAsync();
        Assert.True(tableCount >= 2, $"[{testId}] 服务器信息页表格数量异常: {tableCount}");
        // 名称列存在
        Assert.True(await _page.IsVisibleAsync(".table-data-list td.name"), $"[{testId}] 服务器信息名称列 td.name 不存在");
        // 名称列右对齐（卡片样式生效）
        var nameAlign = await _page.EvaluateAsync<String>(
            "(() => { const el = document.querySelector('.table-data-list td.name'); return el ? getComputedStyle(el).textAlign : ''; })()");
        Assert.Equal("right", nameAlign);
        // AI 诊断按钮存在
        Assert.True(await _page.IsVisibleAsync("button[onclick*='CubeAI.diagnose']"), $"[{testId}] AI 诊断按钮不存在");
    }

    #endregion

    #region D.5 表单视觉偏差（编辑页）

    [Fact(DisplayName = "TC-ACE-031 表单页视觉无严重偏差（Tab/输入框样式）")]
    [Trait("Category", "AceUi")]
    [Trait("Priority", "P2")]
    public async Task TC_ACE_031_FormVisualStyle()
    {
        const String testId = "TC-ACE-031";
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Add");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 字段 label 高对比色（#262a2e）+ 加粗
        var labelColor = await _page.EvaluateAsync<String>(
            "(() => { const el = document.querySelector('.form-horizontal .control-label'); return el ? getComputedStyle(el).color : ''; })()");
        Assert.Equal("rgb(38, 42, 46)", labelColor);
        var labelWeight = await _page.EvaluateAsync<String>(
            "(() => { const el = document.querySelector('.form-horizontal .control-label'); return el ? getComputedStyle(el).fontWeight : ''; })()");
        Assert.Equal("600", labelWeight);
        // Tab 激活蓝色（默认第一个激活）
        var tabColor = await _page.EvaluateAsync<String>(
            "(() => { const el = document.querySelector('.form-horizontal .nav-tabs > li.active > a'); return el ? getComputedStyle(el).color : ''; })()");
        Assert.Equal("rgb(43, 125, 188)", tabColor);
        // 按钮区浅灰底 + 底部圆角
        var faBg = await _page.EvaluateAsync<String>(
            "(() => { const el = document.querySelector('.form-actions'); return el ? getComputedStyle(el).backgroundColor : ''; })()");
        Assert.Equal("rgb(250, 251, 252)", faBg);
    }

    #endregion

    #region D.6 列表页优化回归（菜单按钮化/表格细线/工具栏统一）

    [Fact(DisplayName = "TC-ACE-040 菜单树形表操作按钮按钮化样式生效")]
    [Trait("Category", "AceUi")]
    [Trait("Priority", "P1")]
    public async Task TC_ACE_040_MenuOpBtnStyled()
    {
        const String testId = "TC-ACE-040";
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/Menu");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 树形表（无 table-data-list 类）操作按钮存在且已按钮化：op-btn 样式不限定 table-data-list
        var opCount = await _page.Locator(".op-btn").CountAsync();
        Assert.True(opCount > 0, $"[{testId}] 菜单页无 .op-btn 操作按钮");
        var display = await _page.EvaluateAsync<String>(
            "(() => { const el = document.querySelector('.op-btn'); return el ? getComputedStyle(el).display : ''; })()");
        Assert.Equal("inline-flex", display);
        var radius = await _page.EvaluateAsync<Int32>(
            "(() => { const el = document.querySelector('.op-btn'); return el ? parseFloat(getComputedStyle(el).borderRadius) || 0 : 0; })()");
        Assert.True(radius > 0, $"[{testId}] 菜单页操作按钮圆角为 0（op-btn 样式未生效）");
        // 操作按钮有内边距（按钮化而非纯文字链接）
        var pad = await _page.EvaluateAsync<String>(
            "(() => { const el = document.querySelector('.op-btn'); return el ? getComputedStyle(el).paddingTop : ''; })()");
        Assert.True(pad != "0px", $"[{testId}] 菜单页操作按钮无内边距（未按钮化）");
    }

    [Fact(DisplayName = "TC-ACE-041 数据列表表格细分割线（无双重边框）")]
    [Trait("Category", "AceUi")]
    [Trait("Priority", "P1")]
    public async Task TC_ACE_041_TableThinDividers()
    {
        const String testId = "TC-ACE-041";
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // tbody 单元格上/左边框应为 none（消除 separate 模式双重分割线），右/下保留 1px 单线
        var borderTop = await _page.EvaluateAsync<String>(
            "(() => { const el = document.querySelector('.table-data-list tbody tr td'); return el ? getComputedStyle(el).borderTopWidth : ''; })()");
        Assert.Equal("0px", borderTop);
        var borderLeft = await _page.EvaluateAsync<String>(
            "(() => { const el = document.querySelector('.table-data-list tbody tr td'); return el ? getComputedStyle(el).borderLeftWidth : ''; })()");
        Assert.Equal("0px", borderLeft);
        var borderRight = await _page.EvaluateAsync<String>(
            "(() => { const el = document.querySelector('.table-data-list tbody tr td'); return el ? getComputedStyle(el).borderRightWidth : ''; })()");
        Assert.Equal("1px", borderRight);
        var borderBottom = await _page.EvaluateAsync<String>(
            "(() => { const el = document.querySelector('.table-data-list tbody tr td'); return el ? getComputedStyle(el).borderBottomWidth : ''; })()");
        Assert.Equal("1px", borderBottom);
    }

    [Fact(DisplayName = "TC-ACE-042 工具栏按钮统一高度（添加/批量/查询/高级同高）")]
    [Trait("Category", "AceUi")]
    [Trait("Priority", "P1")]
    public async Task TC_ACE_042_ToolbarButtonsUniformHeight()
    {
        const String testId = "TC-ACE-042";
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 收集工具栏所有按钮高度（添加/批量启用/批量禁用/查询/高级）。
        // 注意：EvaluateAllAsync<T> 的 T 是返回值整体类型（数组），不能写 String
        var heights = await _page.EvaluateAsync<String[]>(
            "() => Array.from(document.querySelectorAll('.tableTools-container .btn')).map(el => getComputedStyle(el).height)");
        Assert.True(heights.Length >= 4, $"[{testId}] 工具栏按钮数量异常: {heights.Length}");
        // 全部同高且为 30px
        foreach (var h in heights)
        {
            Assert.Equal("30px", h);
        }
        // 搜索输入框与按钮同高，避免 input-group 撑高整行
        var inputHeight = await _page.EvaluateAsync<String>(
            "(() => { const el = document.querySelector('.tableTools-container .input-group .form-control'); return el ? getComputedStyle(el).height : ''; })()");
        Assert.Equal("30px", inputHeight);
    }

    [Fact(DisplayName = "TC-ACE-043 工具栏 flex 布局垂直居中 + 关键字框去图标缩短")]
    [Trait("Category", "AceUi")]
    [Trait("Priority", "P1")]
    public async Task TC_ACE_043_ToolbarFlexAndKeyword()
    {
        const String testId = "TC-ACE-043";
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 工具栏 form 为 flex 布局（垂直居中 + 整齐换行）
        var formDisplay = await _page.EvaluateAsync<String>(
            "(() => { const el = document.querySelector('.tableTools-container form'); return el ? getComputedStyle(el).display : ''; })()");
        Assert.Equal("flex", formDisplay);

        // 关键字输入框：不再有左侧打勾图标（input-group-addon 内 fa-check）
        var checkIcon = await _page.Locator(".tableTools-container .input-group-addon .fa-check").CountAsync();
        Assert.Equal(0, checkIcon);
        // 关键字输入框缩短为 150px
        var kwWidth = await _page.EvaluateAsync<String>(
            "(() => { const el = document.querySelector('.tableTools-container input[name=q]'); return el ? getComputedStyle(el).width : ''; })()");
        Assert.Equal("150px", kwWidth);

        // 用户在线/用户统计 chip 链接存在（toolbar-link）
        var chipCount = await _page.Locator(".tableTools-container .toolbar-link").CountAsync();
        Assert.True(chipCount >= 2, $"[{testId}] 用户在线/用户统计 chip 链接缺失: {chipCount}");
    }

    [Fact(DisplayName = "TC-ACE-044 导航栏头像单显（PC 与移动端刷新均无双头像）")]
    [Trait("Category", "AceUi")]
    [Trait("Priority", "P1")]
    public async Task TC_ACE_044_NavbarAvatarSingle()
    {
        const String testId = "TC-ACE-044";
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // PC：只显示一个（头像 img 或 fallback 图标，绝不同时）
        await AssertSingleAvatarAsync(testId + "-PC");

        // 切手机模式（CDP 模拟 UA/DPR/触屏）并刷新，仍应单显
        var cdp = await _page.Context.NewCDPSessionAsync(_page);
        await cdp.SendAsync("Network.setUserAgentOverride", new System.Collections.Generic.Dictionary<String, Object>
        {
            ["userAgent"] = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1",
            ["platform"] = "iPhone",
        });
        await cdp.SendAsync("Emulation.setDeviceMetricsOverride", new System.Collections.Generic.Dictionary<String, Object>
        {
            ["width"] = 375,
            ["height"] = 667,
            ["deviceScaleFactor"] = 3,
            ["mobile"] = true,
            ["screenWidth"] = 375,
            ["screenHeight"] = 667,
        });
        await _page.ReloadAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.WaitForTimeoutAsync(800);
        await AssertSingleAvatarAsync(testId + "-Mobile");
    }

    private async Task AssertSingleAvatarAsync(String tag)
    {
        var state = await _page.EvaluateAsync<String>(
            "() => { const img = document.querySelector('#navbar .nav-user-photo'); const icon = document.querySelector('#navbar .nav-user-photo-icon'); const imgVisible = img ? getComputedStyle(img).display !== 'none' : false; const iconVisible = icon ? getComputedStyle(icon).display !== 'none' : false; return JSON.stringify({ imgVisible, iconVisible, imgCount: document.querySelectorAll('#navbar .nav-user-photo').length, iconCount: document.querySelectorAll('#navbar .nav-user-photo-icon').length }); }");
        var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(state);
        Assert.True(json.GetProperty("imgCount").GetInt32() <= 1, $"[{tag}] 头像 img 数量异常: {state}");
        Assert.True(json.GetProperty("iconCount").GetInt32() <= 1, $"[{tag}] 图标数量异常: {state}");
        var imgVisible = json.GetProperty("imgVisible").GetBoolean();
        var iconVisible = json.GetProperty("iconVisible").GetBoolean();
        Assert.True(imgVisible != iconVisible, $"[{tag}] 双头像（img 与图标同时可见）: {state}");
    }

    [Fact(DisplayName = "TC-ACE-045 工具栏两端布局（左操作按钮 + 右搜索区右对齐）")]
    [Trait("Category", "AceUi")]
    [Trait("Priority", "P1")]
    public async Task TC_ACE_045_ToolbarTwoZone()
    {
        const String testId = "TC-ACE-045";
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.WaitForTimeoutAsync(400);

        var info = await _page.EvaluateAsync<String>(
            "() => { const pull = document.querySelector('.tableTools-container .pull-right'); const container = document.querySelector('.tableTools-container'); const add = document.querySelector('.tableTools-container a.btn-success'); if (!pull || !container) return JSON.stringify({ error: 'not found' }); const pr = pull.getBoundingClientRect(); const cr = container.getBoundingClientRect(); return JSON.stringify({ pullRight: Math.round(pr.right), containerRight: Math.round(cr.right), rightGap: Math.round(cr.right - pr.right), addX: add ? Math.round(add.getBoundingClientRect().x) : null }); }");
        var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(info);
        Assert.True(!json.TryGetProperty("error", out var _), $"[{testId}] 工具栏元素缺失: {info}");
        // 添加按钮在左侧
        var addX = json.GetProperty("addX").GetInt32();
        Assert.True(addX < 100, $"[{testId}] 添加按钮未在左侧: {info}");
        // 搜索区右对齐：右边缘贴近工具栏容器右边缘（gap 很小，说明被推到了右侧而非左对齐）
        var rightGap = json.GetProperty("rightGap").GetInt32();
        Assert.True(rightGap >= 0 && rightGap <= 40, $"[{testId}] 搜索区未右对齐（右缘与容器右缘差 {rightGap}px）: {info}");
    }

    #endregion
}

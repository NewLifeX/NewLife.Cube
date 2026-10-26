using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>MVC 界面本地化验证（TC-I18N-501 ~ TC-I18N-505）</summary>
/// <remarks>
/// 以 en-US 打开关键页面，断言界面骨架（菜单、面包屑、表头、工具栏、分页、表单标签与按钮、标签页、
/// 地区选择器层级标签）不再出现中日韩字符。数据库业务数据（角色名、部门名、地区名、日志内容）与
/// 元数据选项（下拉/单选/多选的取值）属于「源文即数据」，不在本组用例的判定范围内。
/// </remarks>
[Collection("E2E")]
public sealed class LocalizationUiTests : IAsyncLifetime
{
    /// <summary>扫描区域：界面骨架选择器</summary>
    private const String ChromeSelectors = "#sidebar a, #breadcrumbs a, table.table-data-list thead th, " +
        ".tableTools-container a.btn, .tableTools-container button.btn, .nav-tabs a, .form-actions button, " +
        ".form-actions a.btn, form label.control-label, .pager-info, .pager-btn, .city-select-tab a";

    private readonly AppFixture _fixture;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    /// <summary>实例化测试类</summary>
    /// <param name="fixture">全局 E2E 夹具</param>
    public LocalizationUiTests(AppFixture fixture) => _fixture = fixture;

    /// <summary>建立浏览器上下文（英文语言区域）</summary>
    public async Task InitializeAsync()
    {
        _context = await _fixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            Locale = "en-US",
        });
        _page = await _context.NewPageAsync();
    }

    /// <summary>关闭浏览器上下文</summary>
    public async Task DisposeAsync()
    {
        if (_context != null) await _context.DisposeAsync();
    }

    #region 辅助

    /// <summary>收集界面骨架中仍含中日韩字符的文本</summary>
    /// <param name="testId">用例编号</param>
    /// <returns>问题清单，格式为「选择器 => 文本」</returns>
    private async Task<List<String>> CollectCjkAsync(String testId)
    {
        var script = @"(selectors) => {
            const cjk = /[\u4e00-\u9fff\u3400-\u4dbf\u3000-\u303f\uff00-\uffef]/;
            const out = [];
            for (const sel of selectors) {
                for (const el of document.querySelectorAll(sel)) {
                    if (el.closest('select, .multiselect-container, tbody, .runinfo, .city-select-content, #notifyPanel, .ai-panel, .ai-assistant')) continue;
                    const t = (el.innerText || el.textContent || '').replace(/\s+/g, ' ').trim();
                    if (t && cjk.test(t)) out.push(sel + ' => ' + t.substring(0, 80));
                }
            }
            return out;
        }";
        var selectors = ChromeSelectors.Split(", ", StringSplitOptions.RemoveEmptyEntries);
        var items = await _page.EvaluateAsync<String[]>(script, selectors);

        return [.. items ?? []];
    }

    /// <summary>在英文界面打开指定页面并断言界面骨架无中文残留</summary>
    /// <param name="testId">用例编号</param>
    /// <param name="path">相对路径</param>
    /// <param name="checkCjk">未登录也可扫描时传 false 跳过登录</param>
    private async Task AssertPageLocalizedAsync(String testId, String path, Boolean checkCjk = true)
    {
        var sep = path.Contains('?') ? '&' : '?';
        await PageHelpers.GotoAndWaitAsync(_page, path + sep + "lang=en-US");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        var html = await _page.ContentAsync();
        Assert.Contains("lang=\"en-US\"", html);

        if (!checkCjk) return;

        var items = await CollectCjkAsync(testId);
        if (items.Count > 0)
        {
            await PageHelpers.TakeScreenshotAsync(_page, testId);
            Assert.Fail($"[{testId}] 英文界面仍出现中文界面文案（{items.Count} 处）：{String.Join(" | ", items[..Math.Min(items.Count, 8)])}");
        }
    }

    #endregion

    #region 用例

    [Fact(DisplayName = "TC-I18N-501 登录页英文界面无中文残留")]
    [Trait("Category", "Localization")]
    [Trait("Priority", "P0")]
    public async Task TC_I18N_501_LoginPageLocalized()
    {
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/User/Login?lang=en-US");
        await PageHelpers.AssertNoServerErrorAsync(_page, "TC-I18N-501");

        var items = await CollectCjkAsync("TC-I18N-501");
        Assert.True(items.Count == 0, $"[TC-I18N-501] 登录页仍存在中文：{String.Join(" | ", items)}");
    }

    [Fact(DisplayName = "TC-I18N-502 后台框架页英文界面无中文残留（菜单与面包屑）")]
    [Trait("Category", "Localization")]
    [Trait("Priority", "P0")]
    public async Task TC_I18N_502_FrameLocalized()
    {
        await PageHelpers.LoginAsAdminAsync(_page);
        await AssertPageLocalizedAsync("TC-I18N-502", "/Admin");
    }

    [Fact(DisplayName = "TC-I18N-503 列表页英文界面无中文残留（表头/工具栏/分页/搜索标签）")]
    [Trait("Category", "Localization")]
    [Trait("Priority", "P0")]
    public async Task TC_I18N_503_ListPageLocalized()
    {
        await PageHelpers.LoginAsAdminAsync(_page);
        await AssertPageLocalizedAsync("TC-I18N-503", "/Admin/User");
        await AssertPageLocalizedAsync("TC-I18N-503", "/Admin/Role");
        await AssertPageLocalizedAsync("TC-I18N-503", "/Admin/Log");
    }

    [Fact(DisplayName = "TC-I18N-504 表单页英文界面无中文残留（字段标签/按钮/地区层级）")]
    [Trait("Category", "Localization")]
    [Trait("Priority", "P0")]
    public async Task TC_I18N_504_FormPageLocalized()
    {
        await PageHelpers.LoginAsAdminAsync(_page);
        await AssertPageLocalizedAsync("TC-I18N-504", "/Admin/Role/Add");
        await AssertPageLocalizedAsync("TC-I18N-504", "/Admin/User/Add");
        await AssertPageLocalizedAsync("TC-I18N-504", "/Admin/Department/Add");
    }

    [Fact(DisplayName = "TC-I18N-505 设置页与非实体页英文界面无中文残留")]
    [Trait("Category", "Localization")]
    [Trait("Priority", "P1")]
    public async Task TC_I18N_505_SettingsPageLocalized()
    {
        await PageHelpers.LoginAsAdminAsync(_page);
        await AssertPageLocalizedAsync("TC-I18N-505", "/Admin/Cube");
        await AssertPageLocalizedAsync("TC-I18N-505", "/Admin/Index/Main");
    }

    #endregion
}

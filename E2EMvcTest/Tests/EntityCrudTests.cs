using System;
using System.Linq;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>通用实体 CRUD 标准场景（以角色管理为样本）</summary>
/// <remarks>
/// 覆盖可复用的十类场景：页面加载、列表、搜索、新增、编辑、删除、表单校验、空状态、
/// 错误状态、未登录拦截。角色实体字段少、无外部依赖，适合作为跨主题 CRUD 基线。
/// 所有数据使用 E2E_ 前缀且自建自清。
/// </remarks>
[Collection("E2E")]
public sealed class EntityCrudTests : IAsyncLifetime
{
    private const String ListUrl = "/Admin/Role";
    private const String AddUrl = "/Admin/Role/Add";

    private readonly AppFixture _fixture;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public EntityCrudTests(AppFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _context = await _fixture.Browser.NewContextAsync();
        _page = await _context.NewPageAsync();
        await PageHelpers.LoginAsAdminAsync(_page);
    }

    public async Task DisposeAsync()
    {
        if (_page != null) await _page.CloseAsync();
        if (_context != null) await _context.DisposeAsync();
    }

    private static String NewName(String tag) => $"E2E-{tag}-{DateTime.Now:HHmmssfff}";

    private async Task GotoListAsync() => await PageHelpers.GotoAndWaitAsync(_page, ListUrl);

    private async Task<IElementHandle?> FindRowAsync(String name)
    {
        var row = _page.Locator($"table.table-data-list tbody tr:has-text('{name}')").First;
        if (await row.CountAsync() == 0) return null;

        return await row.ElementHandleAsync();
    }

    private async Task CreateRoleAsync(String name, String remark = "E2E created")
    {
        await PageHelpers.GotoAndWaitAsync(_page, AddUrl);
        await _page.FillAsync("input[name=Name]", name);
        // Remark 使用 Markdown 编辑器（原生 textarea 被隐藏），非必填，不在此处填写
        await _page.ClickAsync(".form-actions button[type=submit]");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Fact(DisplayName = "TC-CRUD-001 列表页加载无脚本错误")]
    [Trait("Category", "Crud")]
    [Trait("Priority", "P0")]
    public async Task TC_CRUD_001_ListPageLoads()
    {
        const String testId = "TC-CRUD-001";
        var errors = new System.Collections.Generic.List<String>();
        _page.Console += (_, msg) => { if (msg.Type == "error") errors.Add(msg.Text); };
        _page.PageError += (_, ex) => errors.Add(ex);

        await GotoListAsync();
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);
        Assert.True(await _page.IsVisibleAsync("table"), $"[{testId}] 列表页未渲染 table");
        Assert.True(errors.Count == 0, $"[{testId}] 页面存在 JS 错误：{String.Join(" | ", errors.Take(5))}");
    }

    [Fact(DisplayName = "TC-CRUD-002 列表展示数据行")]
    [Trait("Category", "Crud")]
    [Trait("Priority", "P0")]
    public async Task TC_CRUD_002_ListRows()
    {
        const String testId = "TC-CRUD-002";
        var name = NewName("list");
        try
        {
            await CreateRoleAsync(name);
            await GotoListAsync();
            Assert.NotNull(await FindRowAsync(name));
        }
        finally
        {
            DatabaseHelper.DeleteRoleByName(name);
        }
    }

    [Fact(DisplayName = "TC-CRUD-003 关键字搜索命中目标记录")]
    [Trait("Category", "Crud")]
    [Trait("Priority", "P1")]
    public async Task TC_CRUD_003_Search()
    {
        const String testId = "TC-CRUD-003";
        var name = NewName("search");
        try
        {
            await CreateRoleAsync(name);
            Assert.Equal(1, DatabaseHelper.CountRolesByName(name));
            await PageHelpers.GotoAndWaitAsync(_page, $"{ListUrl}?Q={name}");
            await PageHelpers.AssertNoServerErrorAsync(_page, testId);
            if (await FindRowAsync(name) == null)
            {
                var qv = await _page.InputValueAsync("input[name=q]");
                var cnt = await _page.Locator("table.table-data-list tbody tr").CountAsync();
                var first = cnt > 0 ? await _page.Locator("table.table-data-list tbody tr").First.TextContentAsync() : "";
                Assert.Fail($"[{testId}] 搜索未命中。q={qv}, rows={cnt}, first={first}, url={_page.Url}, name={name}");
            }
        }
        finally
        {
            DatabaseHelper.DeleteRoleByName(name);
        }
    }

    [Fact(DisplayName = "TC-CRUD-004 新增后落库并可在列表查到")]
    [Trait("Category", "Crud")]
    [Trait("Priority", "P0")]
    public async Task TC_CRUD_004_Add()
    {
        const String testId = "TC-CRUD-004";
        var name = NewName("add");
        try
        {
            await CreateRoleAsync(name);
            await PageHelpers.AssertNoServerErrorAsync(_page, testId);
            Assert.Equal(1, DatabaseHelper.CountRolesByName(name));

            await GotoListAsync();
            Assert.NotNull(await FindRowAsync(name));
        }
        finally
        {
            DatabaseHelper.DeleteRoleByName(name);
        }
    }

    [Fact(DisplayName = "TC-CRUD-005 编辑后落库且值更新")]
    [Trait("Category", "Crud")]
    [Trait("Priority", "P0")]
    public async Task TC_CRUD_005_Edit()
    {
        const String testId = "TC-CRUD-005";
        var name = NewName("edit");
        try
        {
            await CreateRoleAsync(name);
            var id = DatabaseHelper.GetRoleIdByName(name);
            Assert.True(id > 0, $"[{testId}] 新增后未查到角色编号");

            await PageHelpers.GotoAndWaitAsync(_page, $"{ListUrl}/Edit/{id}");
            await PageHelpers.AssertNoServerErrorAsync(_page, testId);

            var renamed = name + "_R";
            await _page.FillAsync("input[name=Name]", renamed);
            await _page.ClickAsync(".form-actions button[type=submit]");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await PageHelpers.AssertNoServerErrorAsync(_page, testId);

            Assert.Equal(0, DatabaseHelper.CountRolesByName(name));
            Assert.Equal(1, DatabaseHelper.CountRolesByName(renamed));

            await GotoListAsync();
            Assert.NotNull(await FindRowAsync(renamed));
        }
        finally
        {
            DatabaseHelper.DeleteRoleByName(name);
            DatabaseHelper.DeleteRoleByName(name + "_R");
        }
    }

    [Fact(DisplayName = "TC-CRUD-006 删除后记录消失")]
    [Trait("Category", "Crud")]
    [Trait("Priority", "P1")]
    public async Task TC_CRUD_006_Delete()
    {
        const String testId = "TC-CRUD-006";
        var name = NewName("del");
        try
        {
            await CreateRoleAsync(name);
            Assert.Equal(1, DatabaseHelper.CountRolesByName(name));
            var id = DatabaseHelper.GetRoleIdByName(name);

            _page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
            await PageHelpers.GotoAndWaitAsync(_page, $"{ListUrl}/Delete/{id}");
            if (await _page.IsVisibleAsync("button[type=submit]"))
                await _page.ClickAsync("button[type=submit]");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await PageHelpers.AssertNoServerErrorAsync(_page, testId);

            Assert.Equal(0, DatabaseHelper.CountRolesByName(name));
        }
        finally
        {
            DatabaseHelper.DeleteRoleByName(name);
        }
    }

    [Fact(DisplayName = "TC-CRUD-007 必填项为空时提交被拦截")]
    [Trait("Category", "Crud")]
    [Trait("Priority", "P0")]
    public async Task TC_CRUD_007_Validation()
    {
        const String testId = "TC-CRUD-007";
        await PageHelpers.GotoAndWaitAsync(_page, AddUrl);
        await _page.FillAsync("input[name=Name]", "");
        await _page.ClickAsync(".form-actions button[type=submit]");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        // 服务端校验失败后仍停留在表单页，并给出可见错误提示
        Assert.Contains("/Add", _page.Url, StringComparison.OrdinalIgnoreCase);
        var hasError = await _page.IsVisibleAsync(".text-danger")
                    || await _page.IsVisibleAsync(".alert-danger")
                    || await _page.IsVisibleAsync(".validation-summary-errors")
                    || await _page.IsVisibleAsync("text=不可以为空");
        Assert.True(hasError, $"[{testId}] 未看到表单校验错误提示。URL={_page.Url}");
    }

    [Fact(DisplayName = "TC-CRUD-008 搜索无结果时展示空状态")]
    [Trait("Category", "Crud")]
    [Trait("Priority", "P2")]
    public async Task TC_CRUD_008_EmptyState()
    {
        const String testId = "TC-CRUD-008";
        await PageHelpers.GotoAndWaitAsync(_page, $"{ListUrl}?Q=E2E-NOSUCHROLE-{DateTime.Now:HHmmssfff}");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        var rows = await _page.Locator("table.table-data-list tbody tr").CountAsync();
        var hasEmptyText = await _page.IsVisibleAsync("text=没有数据")
                        || await _page.IsVisibleAsync("text=暂无数据")
                        || await _page.IsVisibleAsync("text=No data");
        if (rows > 0 && !hasEmptyText)
        {
            var qv = await _page.InputValueAsync("input[name=q]");
            Assert.Fail($"[{testId}] 空结果未展示友好空状态，行数={rows}, q={qv}, url={_page.Url}");
        }
    }

    [Fact(DisplayName = "TC-CRUD-009 访问不存在的记录展示错误且页面不崩")]
    [Trait("Category", "Crud")]
    [Trait("Priority", "P1")]
    public async Task TC_CRUD_009_ErrorState()
    {
        const String testId = "TC-CRUD-009";
        await PageHelpers.GotoAndWaitAsync(_page, $"{ListUrl}/Edit/999999999");
        await PageHelpers.AssertNoServerErrorAsync(_page, testId);

        var hasError = await _page.IsVisibleAsync(".text-danger")
                    || await _page.IsVisibleAsync(".alert-danger")
                    || await _page.IsVisibleAsync("text=不存在")
                    || await _page.IsVisibleAsync("text=not exist");
        Assert.True(hasError, $"[{testId}] 不存在的记录未展示错误提示。URL={_page.Url}");
    }

    [Fact(DisplayName = "TC-CRUD-010 未登录访问后台被重定向到登录页")]
    [Trait("Category", "Crud")]
    [Trait("Priority", "P0")]
    public async Task TC_CRUD_010_Unauthorized()
    {
        const String testId = "TC-CRUD-010";
        var anonymous = await _fixture.Browser.NewContextAsync();
        try
        {
            var page = await anonymous.NewPageAsync();
            await page.GotoAsync(AppFixture.BaseUrl + ListUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            Assert.Contains("/Login", page.Url, StringComparison.OrdinalIgnoreCase);
            await PageHelpers.AssertNoServerErrorAsync(page, testId);
        }
        finally
        {
            await anonymous.DisposeAsync();
        }
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>MVC/ACE 皮肤规范中与 MVC 相关的验证（TC-SKIN-11xx ~ TC-SKIN-13xx）</summary>
/// <remarks>
/// 覆盖 SKIN-11（屏幕宽度基础兼容）与 SKIN-13（工作台卡片三能力）在 MVC/ACE 侧的缺口：
/// 2K/4K 与窄屏不出现页面级横向溢出、窄屏侧边栏可折叠；工作台卡片可隐藏/恢复、可拖拽排序并持久化。
/// 列宽规范（SKIN-12）与列表页合规（SKIN-5）的 MVC 侧已由 AceUiTests TC-ACE-010/011/012 覆盖。
/// </remarks>
[Collection("E2E")]
public sealed class SkinMvcComplianceTests : IAsyncLifetime
{
    private readonly AppFixture _fixture;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    /// <summary>实例化测试类</summary>
    /// <param name="fixture">全局 E2E 夹具</param>
    public SkinMvcComplianceTests(AppFixture fixture) => _fixture = fixture;

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

    /// <summary>导航到指定页面（忽略登录后重定向导致的偶发导航中断）</summary>
    /// <param name="path">相对路径</param>
    private async Task GotoAsync(String path)
    {
        for (var i = 0; i < 3; i++)
        {
            try
            {
                await PageHelpers.GotoAndWaitAsync(_page, path);
                return;
            }
            catch (PlaywrightException) when (i < 2)
            {
                await _page.WaitForTimeoutAsync(500);
            }
        }
    }

    /// <summary>读取页面级横向溢出量（文档宽度 - 视口宽度）</summary>
    /// <returns>溢出像素数，0 表示无页面级横向滚动</returns>
    private async Task<Int32> GetHorizontalOverflowAsync()
    {
        var value = await _page.EvaluateAsync<Double>(@"() => {
            const de = document.documentElement;
            return Math.max(de.scrollWidth, document.body ? document.body.scrollWidth : 0) - window.innerWidth;
        }");

        return (Int32)Math.Max(0, Math.Round(value));
    }

    /// <summary>读取工作台卡片名称顺序</summary>
    /// <returns>卡片 data-name 顺序</returns>
    private async Task<String[]> GetWidgetOrderAsync()
    {
        return await _page.Locator("#widget-grid .widget-col").EvaluateAllAsync<String[]>(
            "els => els.map(e => e.getAttribute('data-name'))");
    }

    /// <summary>把卡片拖到目标卡片的位置</summary>
    /// <param name="name">源卡片 data-name</param>
    /// <param name="beforeOrAtName">目标卡片 data-name，落点为其中心</param>
    /// <param name="insertBefore">true 时落在目标卡片之前（瞄准上半区），否则落在其后</param>
    private async Task DragWidgetAsync(String name, String beforeOrAtName, Boolean insertBefore = false)
    {
        var handle = _page.Locator($"#widget-grid .widget-col[data-name='{name}'] .widget-drag");
        var target = _page.Locator($"#widget-grid .widget-col[data-name='{beforeOrAtName}']");

        var hb = await handle.BoundingBoxAsync() ?? throw new InvalidOperationException($"未找到卡片 {name} 的拖拽手柄");
        var tb = await target.BoundingBoxAsync() ?? throw new InvalidOperationException($"未找到卡片 {beforeOrAtName}");

        var sx = hb.X + hb.Width / 2;
        var sy = hb.Y + hb.Height / 2;
        var tx = tb.X + tb.Width / 2;
        // jQuery UI sortable 按指针相对目标的上/下半区决定插入到目标之前还是之后
        var ty = (Single)(tb.Y + tb.Height * (insertBefore ? 0.25 : 0.75));

        await _page.Mouse.MoveAsync(sx, sy);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(sx + 12, sy + 8, new MouseMoveOptions { Steps = 8 });
        await _page.Mouse.MoveAsync(tx, ty, new MouseMoveOptions { Steps = 24 });
        await _page.Mouse.MoveAsync(tx, ty + (insertBefore ? -4 : 4), new MouseMoveOptions { Steps = 6 });
        await _page.Mouse.UpAsync();

        await _page.WaitForTimeoutAsync(800);
    }

    #endregion

    #region SKIN-11 屏幕宽度基础兼容

    [Fact(DisplayName = "TC-SKIN-1101 常规屏与 2K/4K 屏无页面级横向溢出")]
    [Trait("Category", "SkinMvc")]
    [Trait("Priority", "P1")]
    public async Task TC_SKIN_1101_NoHorizontalOverflowOnWideScreens()
    {
        const String testId = "TC-SKIN-1101";
        foreach (var width in new[] { 1366, 1920, 2560 })
        {
            await _page.SetViewportSizeAsync(width, 900);

            await GotoAsync("/Admin");
            var frameOverflow = await GetHorizontalOverflowAsync();
            Assert.True(frameOverflow <= 1, $"[{testId}] 框架页在 {width}px 出现横向溢出 {frameOverflow}px");

            await GotoAsync("/Admin/User");
            var listOverflow = await GetHorizontalOverflowAsync();
            Assert.True(listOverflow <= 1, $"[{testId}] 列表页在 {width}px 出现横向溢出 {listOverflow}px");
        }
    }

    [Fact(DisplayName = "TC-SKIN-1102 窄屏布局不破版且侧边栏可折叠展开")]
    [Trait("Category", "SkinMvc")]
    [Trait("Priority", "P1")]
    public async Task TC_SKIN_1102_NarrowScreenSidebarCollapse()
    {
        const String testId = "TC-SKIN-1102";
        await _page.SetViewportSizeAsync(1024, 900);
        await GotoAsync("/Admin");

        var overflow = await GetHorizontalOverflowAsync();
        Assert.True(overflow <= 1, $"[{testId}] 窄屏框架页出现横向溢出 {overflow}px");
        Assert.True(await _page.IsVisibleAsync("#sidebar"), $"[{testId}] 窄屏下侧边栏不可见");

        var expanded = await _page.Locator("#sidebar").EvaluateAsync<Double>("el => el.getBoundingClientRect().width");
        await _page.ClickAsync("#sidebar-collapse");
        await _page.WaitForTimeoutAsync(600);
        var collapsed = await _page.Locator("#sidebar").EvaluateAsync<Double>("el => el.getBoundingClientRect().width");
        Assert.True(collapsed < expanded - 50, $"[{testId}] 折叠后侧边栏宽度未收窄：{expanded} → {collapsed}");

        await _page.ClickAsync("#sidebar-collapse");
        await _page.WaitForTimeoutAsync(600);
        var restored = await _page.Locator("#sidebar").EvaluateAsync<Double>("el => el.getBoundingClientRect().width");
        Assert.True(Math.Abs(restored - expanded) < 5, $"[{testId}] 展开后侧边栏宽度未恢复：{expanded} → {restored}");
    }

    #endregion

    #region SKIN-13 工作台卡片三能力

    [Fact(DisplayName = "TC-SKIN-1301 工作台卡片可隐藏并从隐藏面板恢复")]
    [Trait("Category", "SkinMvc")]
    [Trait("Priority", "P0")]
    public async Task TC_SKIN_1301_HideAndRestoreWidget()
    {
        const String testId = "TC-SKIN-1301";
        const String widget = "QuickLink";

        await GotoAsync("/Admin/Index/Dashboard");
        Assert.True(await _page.Locator($"#widget-grid .widget-col[data-name='{widget}']").CountAsync() > 0,
            $"[{testId}] 工作台缺少卡片 {widget}");

        try
        {
            // 展开卡片菜单并点击隐藏
            await _page.Locator($"#widget-grid .widget-col[data-name='{widget}'] .widget-menu").ClickAsync();
            await _page.Locator($"#widget-grid .widget-col[data-name='{widget}'] .widget-menu-drop a").ClickAsync();
            await _page.WaitForTimeoutAsync(2500);

            Assert.True(await _page.Locator($"#widget-grid .widget-col[data-name='{widget}']").CountAsync() == 0,
                $"[{testId}] 隐藏后卡片仍在工作台");
            Assert.True(await _page.IsVisibleAsync("#btn-widget-hidden"), $"[{testId}] 隐藏后未出现「隐藏卡片」入口");

            // 从隐藏面板恢复
            await _page.ClickAsync("#btn-widget-hidden");
            var restore = _page.Locator("#wb-hidden-pop button.btn-primary").First;
            Assert.True(await restore.IsVisibleAsync(), $"[{testId}] 隐藏面板中未列出可恢复卡片");
            await restore.ClickAsync();
            await _page.WaitForTimeoutAsync(2500);

            Assert.True(await _page.Locator($"#widget-grid .widget-col[data-name='{widget}']").CountAsync() > 0,
                $"[{testId}] 恢复后卡片未回到工作台");
        }
        finally
        {
            // 兜底恢复：用例中途失败时避免把隐藏状态留给后续用例
            await GotoAsync("/Admin/Index/Dashboard");
            if (await _page.Locator($"#widget-grid .widget-col[data-name='{widget}']").CountAsync() == 0 &&
                await _page.IsVisibleAsync("#btn-widget-hidden"))
            {
                await _page.ClickAsync("#btn-widget-hidden");
                var restore = _page.Locator("#wb-hidden-pop button.btn-primary").First;
                if (await restore.CountAsync() > 0)
                {
                    await restore.ClickAsync();
                    await _page.WaitForTimeoutAsync(2000);
                }
            }
        }
    }

    [Fact(DisplayName = "TC-SKIN-1302 工作台卡片可拖拽排序并持久化")]
    [Trait("Category", "SkinMvc")]
    [Trait("Priority", "P0")]
    public async Task TC_SKIN_1302_DragSortWidgetsPersisted()
    {
        const String testId = "TC-SKIN-1302";
        await GotoAsync("/Admin/Index/Dashboard");

        var original = await GetWidgetOrderAsync();
        Assert.True(original.Length >= 3, $"[{testId}] 工作台卡片不足，无法验证排序：{original.Length}");
        var moving = original[0];
        var anchor = original[2];

        try
        {
            await DragWidgetAsync(moving, anchor);
            await GotoAsync("/Admin/Index/Dashboard");
            var moved = await GetWidgetOrderAsync();
            Assert.True(moved.Length == original.Length, $"[{testId}] 排序后卡片数量变化：{original.Length} → {moved.Length}");
            Assert.NotEqual(moving, moved[0]);
            Assert.True(moved.Contains(moving), $"[{testId}] 拖拽后卡片丢失：{moving}");
        }
        finally
        {
            // 还原为初始顺序：拖回首位（落在当前首卡之前）
            await GotoAsync("/Admin/Index/Dashboard");
            var current = await GetWidgetOrderAsync();
            if (current.Length > 1 && current[0] != original[0])
            {
                await DragWidgetAsync(original[0], current[0], insertBefore: true);
                await GotoAsync("/Admin/Index/Dashboard");
            }
            var restored = await GetWidgetOrderAsync();
            Assert.Equal(original, restored);
        }
    }

    #endregion
}

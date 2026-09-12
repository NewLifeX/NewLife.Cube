using System;
using System.Diagnostics;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using E2EMvcTest.Helpers;
using Microsoft.Playwright;
using Xunit;

namespace E2EMvcTest.Tests;

/// <summary>通知铃铛 — 未读站内信点击就地展开全文，停留数秒自动已读且内容不消失</summary>
/// <remarks>回归问题：点击未读通知后条目立即从下拉消失，用户再也看不到通知内容。</remarks>
[Collection("E2E")]
public sealed class NotificationBellTests : IAsyncLifetime
{
    private readonly AppFixture _fixture;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public NotificationBellTests(AppFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _context = await _fixture.Browser.NewContextAsync();
        _page = await _context.NewPageAsync();
        await PageHelpers.LoginAsAdminAsync(_page);

        // 访问通知记录列表页：确保 NotificationRecord 实体完成初始化与建表，测试可直接写入种子数据
        await PageHelpers.GotoAndWaitAsync(_page, "/Admin/NotificationRecord");
    }

    public async Task DisposeAsync()
    {
        if (_page != null)
        {
            try { await PageHelpers.LogoutAsync(_page); } catch { }
        }
        await _context.DisposeAsync();
    }

    [Fact(DisplayName = "TC-NOTI-001 点击铃铛条目展开全文，停留自动已读且内容不消失")]
    [Trait("Category", "Notification")]
    [Trait("Priority", "P1")]
    public async Task TC_NOTI_001_ExpandFullTextThenAutoRead()
    {
        const String testId = "TC-NOTI-001";

        var stamp = DateTime.Now.ToString("HHmmss");
        var title = $"E2E通知{stamp}";
        var tail = $"E2E全文结尾{stamp}";
        var content = "这是一条端到端测试通知正文，长度超过收起态单行省略的可见范围，用于验证点击条目后能够展开查看完整内容。"
                    + "前半部分仅用于占位，保证正文长度足够，关键断言特征串位于结尾：" + tail;

        var id = DatabaseHelper.SeedInAppNotification(title, content);
        Assert.True(id > 0, $"[{testId}] 站内信种子写入失败");

        try
        {
            // 重新加载后台页面，触发铃铛首屏未读数刷新，红点出现
            await PageHelpers.GotoAndWaitAsync(_page, "/Admin");
            await _page.WaitForSelectorAsync("#notifyBadge:visible",
                new PageWaitForSelectorOptions { Timeout = 10_000 });

            // 打开铃铛下拉，等待种子通知出现在未读列表
            await _page.ClickAsync("#notifyBellToggle");
            var item = _page.Locator($"li.cube-notify-item:has-text('{title}')").First;
            await item.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10_000 });

            // 点击条目 → 就地展开全文（回归：不再点击即标记已读并移出列表）
            await item.ClickAsync();
            var text = _page.Locator($"li.cube-notify-item:has-text('{title}') .cube-notify-text").First;
            var shown = await text.InnerTextAsync();
            Assert.Contains(tail, shown, StringComparison.Ordinal);

            // 停留数秒后自动已读：等待面板内出现已读标记
            await _page.WaitForSelectorAsync($"li.cube-notify-item.read:has-text('{title}')",
                new PageWaitForSelectorOptions { Timeout = 15_000 });

            // 数据库确认已读落库（UI 已读标记来自接口成功响应，此处再轮询确认）
            var read = await WaitUntilAsync(() => Task.FromResult(DatabaseHelper.IsNotificationRead(id)), TimeSpan.FromSeconds(10));
            Assert.True(read, $"[{testId}] 延时已读后数据库 Read 仍为 0");

            // 关键回归：已读后条目与全文仍留在当前面板中，用户可继续查看
            Assert.True(await item.IsVisibleAsync(), $"[{testId}] 已读后条目被移出面板，用户看不到通知内容");
            Assert.Contains(tail, await text.InnerTextAsync(), StringComparison.Ordinal);

            // 关闭再打开面板：列表按"仅未读"过滤，已读条目不再出现
            await _page.ClickAsync("#notifyBellToggle");
            await _page.ClickAsync("#notifyBellToggle");
            var gone = await WaitUntilAsync(
                async () => await _page.Locator($"li.cube-notify-item:has-text('{title}')").CountAsync() == 0,
                TimeSpan.FromSeconds(10));
            Assert.True(gone, $"[{testId}] 重新打开面板后已读条目仍显示在未读列表中");
        }
        finally
        {
            DatabaseHelper.DeleteNotification(id);
        }
    }

    /// <summary>轮询等待条件成立，超时返回 false</summary>
    /// <param name="condition">判定条件</param>
    /// <param name="timeout">超时时间</param>
    /// <returns>是否在超时前成立</returns>
    private static async Task<Boolean> WaitUntilAsync(Func<Task<Boolean>> condition, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            if (await condition()) return true;
            await Task.Delay(300);
        }
        return false;
    }
}

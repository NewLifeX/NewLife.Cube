using System;
using System.IO;
using System.Threading.Tasks;
using E2EMvcTest.Fixtures;
using Microsoft.Playwright;

namespace E2EMvcTest.Helpers;

/// <summary>Playwright 页面操作公共辅助，统一选择器和等待策略</summary>
public static class PageHelpers
{
    #region 登录辅助

    /// <summary>以用户名+密码登录，完成 RSA 加密提交，等待页面跳转</summary>
    /// <param name="page">Playwright 页面对象</param>
    /// <param name="username">用户名</param>
    /// <param name="password">密码（明文，由前端 JS 加密后提交）</param>
    /// <param name="verifySuccess">是否验证登录成功；测试故意用错误密码时传 false</param>
    public static async Task LoginAsync(IPage page, String username, String password, Boolean verifySuccess = true)
    {
        await page.GotoAsync(AppFixture.BaseUrl + "/Admin/User/Login");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.FillAsync("#username", username);
        await page.FillAsync("#password", password);

        // 点击 #login-btn，由页面 JS 完成 RSA 加密并提交表单
        // 使用 RunAndWaitForResponseAsync 等待 POST 响应，避免 <a href="#"> 的 hash 导航被误识别为表单提交完成的竞态条件
        // RunAndWaitForNavigationAsync 会捕获 href="#" 触发的 hash 变化（而非真正的 POST 跳转），导致 session cookie 未设置即提前返回
        await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("#login-btn"),
            resp => resp.Url.Contains("/User/Login", StringComparison.OrdinalIgnoreCase)
                     && resp.Request.Method == "POST",
            new PageRunAndWaitForResponseOptions { Timeout = 15_000 });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        // 验证登录成功；若仍在登录页则抛出异常，使调用方测试明确失败而非静默跳过
        if (verifySuccess && page.Url.Contains("/User/Login", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"登录失败，仍停留在登录页。URL={page.Url}，用户名={username}");
    }

    /// <summary>使用 admin 账号登录</summary>
    /// <param name="page">Playwright 页面对象</param>
    public static Task LoginAsAdminAsync(IPage page) =>
        LoginAsync(page, AppFixture.AdminUser, AppFixture.AdminPass);

    /// <summary>访问注销地址并等待跳转</summary>
    /// <param name="page">Playwright 页面对象</param>
    public static async Task LogoutAsync(IPage page)
    {
        await page.GotoAsync(AppFixture.BaseUrl + "/Admin/User/Logout");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>清除当前页面所有 Cookie 并重新导航（模拟未登录状态）</summary>
    /// <param name="context">浏览器上下文</param>
    public static async Task ClearSessionAsync(IBrowserContext context)
    {
        await context.ClearCookiesAsync();
    }

    #endregion

    #region 页面断言辅助

    /// <summary>断言当前页面无 5xx 服务器错误（检查 URL 和页面 title）</summary>
    /// <param name="page">Playwright 页面对象</param>
    /// <param name="testId">测试用例 ID，用于截图命名</param>
    public static async Task AssertNoServerErrorAsync(IPage page, String testId)
    {
        var title = await page.TitleAsync();
        var url = page.Url;

        var has5xx = title.Contains("500") || title.Contains("Error") ||
                     await page.IsVisibleAsync("text=An unhandled exception") ||
                     await page.IsVisibleAsync("text=Internal Server Error");

        if (has5xx)
        {
            await TakeScreenshotAsync(page, testId);
            throw new Exception(
                $"[{testId}] 页面出现服务器错误。当前URL: {url}, 页面标题: {title}");
        }
    }

    /// <summary>等待内容区可见（页面主体加载完成）</summary>
    /// <param name="page">Playwright 页面对象</param>
    public static async Task WaitForContentAreaAsync(IPage page)
    {
        // 魔方后台页面通用内容容器
        try
        {
            await page.WaitForSelectorAsync(".content, #content, main, .container-fluid",
                new PageWaitForSelectorOptions { Timeout = 10_000 });
        }
        catch
        {
            // 如果没有标准容器，等待 DOMContentLoaded 即可
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        }
    }

    /// <summary>断言 URL 包含指定路径片段</summary>
    /// <param name="page">Playwright 页面对象</param>
    /// <param name="expectedPath">期望包含的路径片段</param>
    /// <param name="testId">测试用例 ID</param>
    public static async Task AssertUrlContainsAsync(IPage page, String expectedPath, String testId)
    {
        var url = page.Url;
        if (!url.Contains(expectedPath, StringComparison.OrdinalIgnoreCase))
        {
            await TakeScreenshotAsync(page, testId);
            throw new Exception(
                $"[{testId}] URL 不含预期路径 '{expectedPath}'。当前URL: {url}, 页面标题: {await page.TitleAsync()}");
        }
    }

    /// <summary>断言页面可见指定文本，失败时截图</summary>
    /// <param name="page">Playwright 页面对象</param>
    /// <param name="text">期望出现的文本</param>
    /// <param name="testId">测试用例 ID</param>
    public static async Task AssertTextVisibleAsync(IPage page, String text, String testId)
    {
        var isVisible = await page.IsVisibleAsync($"text={text}");
        if (!isVisible)
        {
            await TakeScreenshotAsync(page, testId);
            throw new Exception(
                $"[{testId}] 页面中未找到文本 '{text}'。当前URL: {page.Url}, 页面标题: {await page.TitleAsync()}");
        }
    }

    /// <summary>断言页面不可见指定文本（反向断言）</summary>
    /// <param name="page">Playwright 页面对象</param>
    /// <param name="text">期望不出现的文本</param>
    /// <param name="testId">测试用例 ID</param>
    public static async Task AssertTextNotVisibleAsync(IPage page, String text, String testId)
    {
        var isVisible = await page.IsVisibleAsync($"text={text}");
        if (isVisible)
        {
            await TakeScreenshotAsync(page, testId);
            throw new Exception(
                $"[{testId}] 页面中出现了不应存在的文本 '{text}'。当前URL: {page.Url}");
        }
    }

    #endregion

    #region 截图辅助

    /// <summary>在失败时截图并保存到截图目录</summary>
    /// <param name="page">Playwright 页面对象</param>
    /// <param name="testId">测试用例 ID，用于文件命名</param>
    public static async Task TakeScreenshotAsync(IPage page, String testId)
    {
        try
        {
            var fileName = $"{testId}-{DateTime.Now:yyyyMMddHHmmss}.png";
            var path = Path.Combine(AppFixture.ScreenshotDir, fileName);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
        }
        catch
        {
            // 截图失败不影响测试异常抛出
        }
    }

    #endregion

    #region 导航辅助

    /// <summary>导航到指定路径并等待内容区</summary>
    /// <param name="page">Playwright 页面对象</param>
    /// <param name="path">相对路径，如 /Admin/User</param>
    public static async Task GotoAndWaitAsync(IPage page, String path)
    {
        await page.GotoAsync(AppFixture.BaseUrl + path);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>点击导航标签并等待内容区刷新</summary>
    /// <param name="page">Playwright 页面对象</param>
    /// <param name="tabText">标签文字</param>
    public static async Task ClickNavTabAsync(IPage page, String tabText)
    {
        await page.ClickAsync($".nav-pills a:has-text('{tabText}')");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    #endregion
}

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using NewLife.AI.Tools;
using NewLife.Cube;
using NewLife.Cube.AI;
using Xunit;

namespace XUnitTest;

/// <summary>页面数据上下文工具测试 — 验证 get_page_context 服务端优先/浏览器兜底/异常降级，及 ToolCallContext 上下文通道</summary>
public class PageDataContextToolTests
{
    #region 测试用控制器
    /// <summary>实现 IPageDataContext 的假页面控制器</summary>
    private sealed class FakePageController : ControllerBaseX, IPageDataContext
    {
        public Task<String> GetPageDataContextAsync() => Task.FromResult("{\"page\":\"demo\",\"server\":true}");
    }

    /// <summary>实现 IPageDataContext 但服务端抛异常的控制器</summary>
    private sealed class ThrowingPageController : ControllerBaseX, IPageDataContext
    {
        public Task<String> GetPageDataContextAsync() => throw new InvalidOperationException("boom");
    }

    /// <summary>重写浏览器采集的假浏览器工具服务，记录最近一次上下文</summary>
    private sealed class FakeBrowser : BrowserToolService
    {
        public FakeBrowser() : base(0) { }

        /// <summary>最近一次收到的工具调用上下文</summary>
        public ToolCallContext? LastContext { get; private set; }

        public override Task<String> CollectPageContextAsync(ToolCallContext? context = null)
        {
            LastContext = context;
            return Task.FromResult("{\"browser\":true}");
        }
    }
    #endregion

    [Fact]
    [DisplayName("get_page_context_控制器实现接口_优先服务端")]
    public async Task GetPageContext_ServerFirst()
    {
        var svc = new PageDataContextToolService(new FakePageController(), new FakeBrowser());

        var rs = await svc.GetPageContextAsync();

        Assert.Contains("\"server\":true", rs);
        Assert.DoesNotContain("\"browser\":true", rs);
    }

    [Fact]
    [DisplayName("get_page_context_未实现接口_浏览器采集兜底")]
    public async Task GetPageContext_BrowserFallback()
    {
        var svc = new PageDataContextToolService(new ControllerBaseX(), new FakeBrowser());

        var rs = await svc.GetPageContextAsync();

        Assert.Contains("\"browser\":true", rs);
    }

    [Fact]
    [DisplayName("get_page_context_服务端异常_降级浏览器采集")]
    public async Task GetPageContext_ServerError_FallbackToBrowser()
    {
        var svc = new PageDataContextToolService(new ThrowingPageController(), new FakeBrowser());

        var rs = await svc.GetPageContextAsync();

        Assert.Contains("\"browser\":true", rs);
    }

    [Fact]
    [DisplayName("get_page_context_浏览器采集_透传ToolCallContext")]
    public async Task GetPageContext_Browser_ContextPassed()
    {
        // 浏览器层应收到框架注入的上下文（含 ToolCallId）
        var browser = new FakeBrowser();
        var svc = new PageDataContextToolService(new ControllerBaseX(), browser);

        var ctx = new ToolCallContext { ToolCallId = "call_ctx" };
        _ = await svc.GetPageContextAsync(ctx);

        Assert.NotNull(browser.LastContext);
        Assert.Equal("call_ctx", browser.LastContext!.ToolCallId);
    }
}

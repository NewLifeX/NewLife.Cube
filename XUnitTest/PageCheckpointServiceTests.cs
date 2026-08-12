using System;
using System.ComponentModel;
using System.Threading.Tasks;
using NewLife.AI.Tools;
using NewLife.Cube.AI;
using Xunit;

namespace XUnitTest;

/// <summary>页面检查点服务单元测试 — 挂起等待、结果回传、超时、用户绑定、一次性消费、ToolCallContext 通道</summary>
public class PageCheckpointServiceTests
{
    /// <summary>从 run_js 事件 JSON 提取检查点编号</summary>
    private static String GetCheckpointId(String ev)
    {
        var key = "\"checkpointId\":\"";
        var start = ev.IndexOf(key, StringComparison.Ordinal) + key.Length;
        var end = ev.IndexOf('"', start);
        return ev.Substring(start, end - start);
    }

    [Fact]
    [DisplayName("WaitForChoiceAsync_回传结果_返回前端结果")]
    public async Task WaitForChoiceAsync_Respond_ReturnsResult()
    {
        var svc = new BrowserToolService(1);
        String? ev = null;
        svc.Writer = json => { ev = json; return Task.CompletedTask; };

        var task = svc.RunJs("document.title");

        // 等待事件下发，断言脚本与检查点编号已携带
        await Task.Delay(50);
        Assert.NotNull(ev);
        Assert.Contains("\"type\":\"run_js\"", ev);
        Assert.Contains("document.title", ev);

        // 前端回传结果，完成等待中的工具调用
        var checkpointId = GetCheckpointId(ev!);
        Assert.True(await PageCheckpointService.Instance.Respond(checkpointId, 1, "{\"ok\":true,\"value\":\"魔方\"}"));

        var result = await task;
        Assert.Contains("\"ok\":true", result);
        Assert.Contains("魔方", result);
    }

    [Fact]
    [DisplayName("WaitForChoiceAsync_不回传_超时返回错误")]
    public async Task WaitForChoiceAsync_NoResponse_Timeout()
    {
        var svc = new BrowserToolService(1) { TimeoutSeconds = 1 };
        svc.Writer = json => Task.CompletedTask;

        var result = await svc.RunJs("1");

        Assert.Contains("\"ok\":false", result);
        Assert.Contains("超时", result);
    }

    [Fact]
    [DisplayName("Respond_用户不匹配_等待不被完成")]
    public async Task Respond_UserMismatch_WaitNotCompleted()
    {
        var svc = new BrowserToolService(1);
        String? ev = null;
        svc.Writer = json => { ev = json; return Task.CompletedTask; };

        var task = svc.RunJs("1");
        await Task.Delay(50);

        var checkpointId = GetCheckpointId(ev!);
        // 其它用户回传：事件发布成功但被等待方忽略（跨用户防串扰），等待不被完成
        Assert.True(await PageCheckpointService.Instance.Respond(checkpointId, 999, "{\"ok\":true}"));
        Assert.False(task.IsCompleted);

        // 原用户回传成功完成等待
        Assert.True(await PageCheckpointService.Instance.Respond(checkpointId, 1, "{\"ok\":true}"));
        var result = await task;
        Assert.Contains("\"ok\":true", result);
    }

    [Fact]
    [DisplayName("Respond_一次性消费_重复回传返回false")]
    public async Task Respond_OneShot_SecondReturnsFalse()
    {
        var svc = new BrowserToolService(1);
        String? ev = null;
        svc.Writer = json => { ev = json; return Task.CompletedTask; };

        var task = svc.RunJs("1");
        await Task.Delay(50);

        var checkpointId = GetCheckpointId(ev!);
        Assert.True(await PageCheckpointService.Instance.Respond(checkpointId, 1, "{\"ok\":true}"));
        // 已完成的操作再次回传被拒绝（一次性订阅已退订）
        Assert.False(await PageCheckpointService.Instance.Respond(checkpointId, 1, "{\"ok\":true}"));
        await task;
    }

    [Fact]
    [DisplayName("RunJs_空脚本_返回错误")]
    public async Task RunJs_EmptyScript_ReturnsError()
    {
        var svc = new BrowserToolService(1);

        var result = await svc.RunJs("");

        Assert.Contains("\"ok\":false", result);
        Assert.Contains("脚本不能为空", result);
    }

    [Fact]
    [DisplayName("RunJs_Writer未注入_返回错误")]
    public async Task RunJs_WriterNotInjected_ReturnsError()
    {
        // 未设置 Writer（宿主未注入），返回友好错误
        var svc = new BrowserToolService(1);

        var result = await svc.RunJs("document.title");

        Assert.Contains("\"ok\":false", result);
        Assert.Contains("Writer 未注入", result);
    }

    [Fact]
    [DisplayName("RunJs_ToolCallContext_检查点编号使用ToolCallId")]
    public async Task RunJs_Context_ToolCallId()
    {
        var svc = new BrowserToolService(1) { TimeoutSeconds = 1 };
        String? ev = null;
        svc.Writer = json => { ev = json; return Task.CompletedTask; };

        // 框架注入的上下文携带 ToolCallId，检查点编号与其一致（前端 tool 事件一一对应）
        var ctx = new ToolCallContext { ToolCallId = "call_123" };
        var result = await svc.RunJs("1", ctx);

        Assert.NotNull(ev);
        Assert.Contains("\"checkpointId\":\"call_123\"", ev);
        Assert.Contains("超时", result);
    }

    [Fact]
    [DisplayName("RunJs_ToolCallContext_经Items读取浏览器通道")]
    public async Task RunJs_Context_ChannelFromItems()
    {
        // 实例未注入 Writer，但上下文经 Items 携带 CubeBrowserContext → 应从上下文读取通道
        var svc = new BrowserToolService(0) { TimeoutSeconds = 1 };
        String? ev = null;

        var bc = new CubeBrowserContext
        {
            Writer = json => { ev = json; return Task.CompletedTask; },
            UserId = 9,
            TimeoutSeconds = 1,
        };
        var ctx = new ToolCallContext { ToolCallId = "call_ch" };
        ctx[CubeBrowserContext.BrowserContextKey] = bc;

        var result = await svc.RunJs("1", ctx);

        Assert.NotNull(ev);
        Assert.Contains("call_ch", ev);
        Assert.Contains("超时", result);
    }

    [Fact]
    [DisplayName("PageContextCollector_BuildScript_包含关键选择器")]
    public void PageContextCollector_BuildScript_ContainsSelectors()
    {
        var script = PageContextCollector.BuildScript();

        Assert.False(String.IsNullOrEmpty(script));
        Assert.Contains("document.title", script);
        Assert.Contains("querySelectorAll('table')", script);
        Assert.Contains("querySelectorAll('input,select,textarea')", script);
        Assert.Contains("data-ai-context", script);
    }

    [Fact]
    [DisplayName("CubeBrowserContext_含Writer_可被SystemTextJson序列化")]
    public void CubeBrowserContext_WithWriter_Serializable()
    {
        // 回归：本类常驻 ChatOptions.Items，AI 客户端序列化请求体时会展开 Items 序列化本对象。
        // Writer 为委托、CheckpointService 为服务实例，均不可序列化，必须被 [JsonIgnore] 跳过，否则抛 NotSupportedException（Path: $.Writer）
        var bc = new CubeBrowserContext
        {
            Writer = json => Task.CompletedTask,
            CheckpointService = PageCheckpointService.Instance,
            UserId = 9,
            TimeoutSeconds = 30,
        };

        var json = System.Text.Json.JsonSerializer.Serialize(bc);

        Assert.DoesNotContain("Writer", json);
        Assert.DoesNotContain("CheckpointService", json);
        Assert.Contains("UserId", json);
    }

    [Fact]
    [DisplayName("BrowserToolService_含Writer_可被SystemTextJson序列化")]
    public void BrowserToolService_WithWriter_Serializable()
    {
        var svc = new BrowserToolService(9)
        {
            Writer = json => Task.CompletedTask,
            CheckpointService = PageCheckpointService.Instance,
            TimeoutSeconds = 30,
        };

        var json = System.Text.Json.JsonSerializer.Serialize(svc);

        Assert.DoesNotContain("Writer", json);
        Assert.DoesNotContain("CheckpointService", json);
        Assert.Contains("TimeoutSeconds", json);
    }
}

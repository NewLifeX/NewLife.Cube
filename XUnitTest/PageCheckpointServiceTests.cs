using System;
using System.ComponentModel;
using System.Threading.Tasks;
using NewLife.Cube.AI;
using Xunit;

namespace XUnitTest;

/// <summary>页面检查点服务单元测试 — 挂起等待、结果回传、超时、用户绑定、一次性消费</summary>
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
        Assert.True(PageCheckpointService.Instance.Respond(checkpointId, 1, "{\"ok\":true,\"value\":\"魔方\"}"));

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
    [DisplayName("Respond_用户不匹配_返回false")]
    public async Task Respond_UserMismatch_ReturnsFalse()
    {
        var svc = new BrowserToolService(1);
        String? ev = null;
        svc.Writer = json => { ev = json; return Task.CompletedTask; };

        var task = svc.RunJs("1");
        await Task.Delay(50);

        var checkpointId = GetCheckpointId(ev!);
        // 其它用户回传被拒绝
        Assert.False(PageCheckpointService.Instance.Respond(checkpointId, 999, "{\"ok\":true}"));

        // 原用户回传成功
        Assert.True(PageCheckpointService.Instance.Respond(checkpointId, 1, "{\"ok\":true}"));
        await task;
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
        Assert.True(PageCheckpointService.Instance.Respond(checkpointId, 1, "{\"ok\":true}"));
        // 已完成的操作再次回传被拒绝
        Assert.False(PageCheckpointService.Instance.Respond(checkpointId, 1, "{\"ok\":true}"));
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
}

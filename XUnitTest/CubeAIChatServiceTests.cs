using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NewLife.AI.Clients;
using NewLife.AI.Models;
using NewLife.AI.Tools;
using NewLife.Cube;
using NewLife.Cube.AI;
using Xunit;

namespace XUnitTest;

/// <summary>AI 对话服务单元测试 — 会话管理、事件输出、空响应兜底</summary>
/// <remarks>通过 Stub IAIService 隔离 LLM 调用，验证 CubeAIChatService 的编排逻辑</remarks>
public class CubeAIChatServiceTests
{
    #region Stub
    /// <summary>Stub AI 服务。记录每次调用的消息列表，按配置返回流式/非流式响应</summary>
    private class StubAIService : IAIService
    {
        /// <summary>流式调用收到的消息列表（按调用顺序）</summary>
        public IList<IList<ChatMessage>> StreamCalls { get; } = [];

        /// <summary>非流式调用收到的消息列表</summary>
        public IList<IList<ChatMessage>> SyncCalls { get; } = [];

        /// <summary>流式响应块（按序 yield）</summary>
        public IList<IChatResponse>? StreamResponses { get; set; }

        /// <summary>非流式响应</summary>
        public IChatResponse? SyncResponse { get; set; }

        public Task<String> ChatAsync(String prompt, String data, CancellationToken cancellationToken = default)
            => Task.FromResult(String.Empty);

        public Task<String> PolishNotificationAsync(String title, String content, String style, CancellationToken cancellationToken = default)
            => Task.FromResult(String.Empty);

        public Task<String> DiagnoseSystemAsync(String sysInfoJson, CancellationToken cancellationToken = default)
            => Task.FromResult(String.Empty);

        public async IAsyncEnumerable<String> DiagnoseSystemStreamAsync(String sysInfoJson, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public async IAsyncEnumerable<IChatResponse> ChatAgentStreamAsync(IList<ChatMessage> messages, IList<IToolProvider>? providers = null, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            StreamCalls.Add(messages);
            if (StreamResponses != null)
            {
                foreach (var item in StreamResponses)
                    yield return item;
            }
            await Task.CompletedTask;
        }

        public Task<IChatResponse> ChatAgentAsync(IList<ChatMessage> messages, IList<IToolProvider>? providers = null, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            SyncCalls.Add(messages);
            return Task.FromResult<IChatResponse>(SyncResponse ?? TextResponse("非流式回复"));
        }
    }
    #endregion

    #region 辅助
    /// <summary>执行一次对话并收集全部事件 JSON</summary>
    private static async Task<List<String>> RunAsync(CubeAIChatService svc, AiChatRequest req, String systemPrompt = "系统提示词")
    {
        var events = new List<String>();
        await svc.ChatAsync(req, systemPrompt, [new ToolRegistry()], json => { events.Add(json); return Task.CompletedTask; });
        return events;
    }

    private static CubeSetting CreateSetting(Boolean aiSwitch = true) => new()
    {
        AISwitch = aiSwitch,
        AIModel = "test-model",
        AIDefaultThink = false,
    };
    #endregion

    /// <summary>构造带文本的响应（Text 属性只读，经 Add 写入）</summary>
    private static ChatResponse TextResponse(String text)
    {
        var r = new ChatResponse();
        r.Add(text, "assistant");
        return r;
    }

    [Fact(DisplayName = "AISwitch关闭_产出错误事件")]
    public async Task AISwitchOff_ErrorEvent()
    {
        var stub = new StubAIService();
        var svc = new CubeAIChatService(stub, CreateSetting(false));

        var events = await RunAsync(svc, new AiChatRequest { SessionId = "s1", Message = "你好" });

        Assert.NotEmpty(events);
        Assert.Contains("error", events[0]);
        // 不调用 LLM
        Assert.Empty(stub.StreamCalls);
        Assert.Empty(stub.SyncCalls);
    }

    [Fact(DisplayName = "流式对话_产出meta_text_done事件序列")]
    public async Task StreamChat_EventSequence()
    {
        var stub = new StubAIService
        {
            StreamResponses = [TextResponse("分析结果")],
        };
        var svc = new CubeAIChatService(stub, CreateSetting());

        var events = await RunAsync(svc, new AiChatRequest { SessionId = "s2", Message = "分析一下", Stream = true });

        Assert.Equal(3, events.Count);
        Assert.Contains("\"meta\"", events[0]);
        Assert.Contains("\"text\"", events[1]);
        Assert.Contains("分析结果", events[1]);
        Assert.Contains("\"done\"", events[^1]);
        // 流式调用一次，且消息含 system + 用户消息
        Assert.Single(stub.StreamCalls);
        Assert.Equal("系统提示词", (stub.StreamCalls[0][0].Content as String));
        Assert.Equal("user", stub.StreamCalls[0][^1].Role);
    }

    [Fact(DisplayName = "工具事件_透传start_done")]
    public async Task ToolEvents_PassThrough()
    {
        var stub = new StubAIService
        {
            StreamResponses =
            [
                new ChatResponse { ToolCallEvents = [new ToolCallEventInfo("start", "c1", "get_form_schema", null)] },
                new ChatResponse { ToolCallEvents = [new ToolCallEventInfo("done", "c1", "get_form_schema", "{\"mode\":\"add\"}")] },
            ],
        };
        var svc = new CubeAIChatService(stub, CreateSetting());

        var events = await RunAsync(svc, new AiChatRequest { SessionId = "s3", Message = "帮我填表", Stream = true });

        Assert.Contains(events, e => e.Contains("\"tool\"") && e.Contains("\"start\"") && e.Contains("get_form_schema"));
        Assert.Contains(events, e => e.Contains("\"tool\"") && e.Contains("\"done\""));
        Assert.Contains(events, e => e.Contains("\"done\"") && e.Contains("c1"));
    }

    [Fact(DisplayName = "空响应_产出兜底提示")]
    public async Task EmptyResponse_FallbackNote()
    {
        // 流式返回空块
        var stub = new StubAIService { StreamResponses = [] };
        var svc = new CubeAIChatService(stub, CreateSetting());

        var events = await RunAsync(svc, new AiChatRequest { SessionId = "s4", Message = "分析", Stream = true });

        // meta + 兜底文本 + done
        Assert.Equal(3, events.Count);
        Assert.Contains("\"text\"", events[1]);
        Assert.Contains("AI 未返回有效结果", events[1]);
    }

    [Fact(DisplayName = "会话历史_第二次调用携带上次消息")]
    public async Task SessionHistory_Accumulated()
    {
        var stub = new StubAIService
        {
            StreamResponses = [TextResponse("第一次回复")],
        };
        var svc = new CubeAIChatService(stub, CreateSetting());

        var req = new AiChatRequest { SessionId = "s5", Message = "你好", Stream = true };
        await RunAsync(svc, req);
        req.Message = "继续";
        await RunAsync(svc, req);

        Assert.Equal(2, stub.StreamCalls.Count);
        var second = stub.StreamCalls[1];
        // system + 第一轮 user + 第一轮 assistant + 第二轮 user
        Assert.Equal(4, second.Count);
        Assert.Equal("user", second[1].Role);
        Assert.Equal("你好", second[1].Content as String);
        Assert.Equal("assistant", second[2].Role);
        Assert.Equal("第一次回复", second[2].Content as String);
        Assert.Equal("user", second[3].Role);
        Assert.Equal("继续", second[3].Content as String);
    }

    [Fact(DisplayName = "非流式_一次返回完整响应")]
    public async Task NonStream_FullResponse()
    {
        var stub = new StubAIService
        {
            SyncResponse = TextResponse("完整回复"),
        };
        var svc = new CubeAIChatService(stub, CreateSetting());

        var events = await RunAsync(svc, new AiChatRequest { SessionId = "s6", Message = "总结", Stream = false });

        Assert.Single(stub.SyncCalls);
        Assert.Contains(events, e => e.Contains("\"text\"") && e.Contains("完整回复"));
        Assert.Contains("\"done\"", events[^1]);
    }
}

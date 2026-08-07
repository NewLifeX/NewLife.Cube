using System.ComponentModel;
using NewLife.AI.Tools;
using NewLife.Log;
using NewLife.Serialization;

namespace NewLife.Cube.AI;

/// <summary>浏览器工具服务。把 LLM 生成的脚本/操作下发到用户浏览器当前页面执行，并回传执行结果</summary>
/// <remarks>
/// 与前端 ai-assistant.js / AiAssistant.vue 配合，参考 StarChat ask_user 架构：
/// 工具调用时生成检查点编号并经 <see cref="PageCheckpointService"/> 挂起等待，
/// 同时经 <see cref="Writer"/>（SSE 写回调，由宿主在构造后注入）下发
/// <c>{"type":"run_js","checkpointId":...,"script":...}</c> 事件；
/// 前端以 <c>new Function(script)</c> 执行后 <c>POST /Ai/OperationResult</c> 回传结果，
/// 工具返回给 LLM 继续对话。脚本运行在用户自己浏览器、自己登录会话内，等价用户在 DevTools 中执行。
/// 工具实例为每请求新建，Writer 为实例属性（请求内隔离，无全局可变状态）。
/// run_js 是首个浏览器工具，后续可在此类追加 read_element / write_element / click 等页面操作工具。
/// 工具方法均为 virtual，二次开发者可继承重写或新增。
/// </remarks>
/// <param name="userId">当前用户编号，回传时校验防止跨用户串扰</param>
public class BrowserToolService(Int64 userId)
{
    /// <summary>等待前端回传的超时秒数，默认 30</summary>
    public Int32 TimeoutSeconds { get; set; } = 30;

    /// <summary>SSE 写回调。由宿主（实体控制器 AiChat）在构造后注入，用于下发 run_js 事件到前端</summary>
    public Func<String, Task>? Writer { get; set; }

    /// <summary>在当前页面执行 JavaScript 并返回结果。可读取或操作页面 DOM、调用页面脚本等</summary>
    /// <param name="script">要执行的 JavaScript 代码</param>
    /// <returns>执行结果 JSON（<c>{ok,value,error}</c>）</returns>
    [ToolDescription("run_js")]
    [DisplayName("执行页面脚本")]
    [Description("在当前页面执行 JavaScript 并返回结果，可读取或操作页面元素。例如 document.title、document.querySelector('input[name=Name]').value 等")]
    public virtual async Task<String> RunJs([Description("要在当前页面执行的 JavaScript 代码")] String script)
    {
        if (script.IsNullOrEmpty()) return new { ok = false, error = "脚本不能为空" }.ToJson();

        // 审计：记录下发脚本内容，便于追溯
        XTrace.WriteLine("[AI][run_js] 下发脚本：{0}", script);

        // 生成检查点编号并挂起等待前端执行结果；SSE 事件经 Writer（宿主注入的实例属性，请求内隔离）下发
        var writer = Writer;
        if (writer == null) return new { ok = false, error = "浏览器通道未就绪（Writer 未注入）" }.ToJson();

        var checkpointId = PageCheckpointService.NewCheckpointId();
        await writer(new { type = "run_js", checkpointId, script }.ToJson());

        return await PageCheckpointService.Instance.WaitForChoiceAsync(checkpointId, userId, TimeoutSeconds);
    }
}

using System.ComponentModel;
using System.Text.Json.Serialization;
using NewLife.AI.Tools;
using NewLife.Log;
using NewLife.Serialization;

namespace NewLife.Cube.AI;

/// <summary>浏览器通道上下文。宿主在对话请求 <c>Items</c> 中注入（键 <see cref="BrowserContextKey"/>），
/// 经 <see cref="ToolCallContext.Items"/> 透传给浏览器工具，实现宿主与工具的松耦合</summary>
/// <remarks>
/// 宿主（<see cref="Controllers.AiController"/>）在发起 LLM 调用前把浏览器通道上下文写入 ChatOptions.Items，
/// NewLife.AI 框架把请求级 Items 播种进每次工具调用的 ToolCallContext，
/// 工具方法经 <c>context?[BrowserContextKey]</c> 读取，无需在构造工具实例时逐个注入。
/// </remarks>
public class CubeBrowserContext
{
    /// <summary>上下文键。宿主写入 ChatOptions.Items 与工具读取 ToolCallContext.Items 使用同一键</summary>
    public const String BrowserContextKey = "Cube.BrowserContext";

    /// <summary>SSE 写回调。下发 run_js 等事件到前端</summary>
    // 委托不可被 System.Text.Json 序列化，标注忽略（本类常驻 ChatOptions.Items，会被 AI 客户端序列化）
    [JsonIgnore]
    public Func<String, Task>? Writer { get; init; }

    /// <summary>页面检查点服务。挂起等待前端回传（经事件总线广播，支持分布式）</summary>
    [JsonIgnore]
    public PageCheckpointService? CheckpointService { get; init; }

    /// <summary>当前用户编号。回传时校验防跨用户串扰</summary>
    public Int64 UserId { get; init; }

    /// <summary>等待前端回传的超时秒数，默认 30</summary>
    public Int32 TimeoutSeconds { get; init; } = 30;
}

/// <summary>浏览器工具服务。把 LLM 生成的脚本/操作下发到用户浏览器当前页面执行，并回传执行结果</summary>
/// <remarks>
/// 与前端 ai-assistant.js / AiAssistant.vue 配合，参考 StarChat ask_user 架构：
/// 工具调用时生成检查点编号（优先 <c>ToolCallContext.ToolCallId</c>）并经 <see cref="PageCheckpointService"/> 挂起等待，
/// 同时经 <see cref="Writer"/>（SSE 写回调，可由宿主注入或经 <see cref="CubeBrowserContext"/> 提供）下发
/// <c>{"type":"run_js","checkpointId":...,"script":...}</c> 事件；
/// 前端以 <c>new Function(script)</c> 执行后 <c>POST /Admin/Ai/OperationResult</c> 回传结果，
/// 工具返回给 LLM 继续对话。脚本运行在用户自己浏览器、自己登录会话内，等价用户在 DevTools 中执行。
/// 工具方法接受 <c>ToolCallContext? context = null</c> 由框架注入：检查点编号使用 ToolCallId（前后端一一对应），
/// 通道上下文（Writer/检查点服务/用户编号）经 <c>context.Items</c> 读取，兜底回退到实例属性。
/// run_js 是首个浏览器工具，后续可在此类追加 read_element / write_element / click 等页面操作工具。
/// 工具方法均为 virtual，二次开发者可继承重写或新增。
/// </remarks>
/// <param name="userId">当前用户编号，回传时校验防止跨用户串扰</param>
public class BrowserToolService(Int64 userId)
{
    /// <summary>等待前端回传的超时秒数，默认 30</summary>
    public Int32 TimeoutSeconds { get; set; } = 30;

    /// <summary>SSE 写回调。由宿主（实体控制器 AiChat）在构造后注入，用于下发 run_js 事件到前端</summary>
    // 委托不可被 System.Text.Json 序列化，标注忽略（防止被序列化时抛 NotSupportedException）
    [JsonIgnore]
    public Func<String, Task>? Writer { get; set; }

    /// <summary>页面检查点服务。null 时使用 <see cref="PageCheckpointService.Instance"/>（进程内兜底）</summary>
    [JsonIgnore]
    public PageCheckpointService? CheckpointService { get; set; }

    /// <summary>在当前页面执行 JavaScript 并返回结果。可读取或操作页面 DOM、调用页面脚本等</summary>
    /// <param name="script">要执行的 JavaScript 代码</param>
    /// <param name="context">工具调用上下文（由框架注入）。检查点编号优先使用 <c>ToolCallId</c>；通道上下文经 <c>Items</c> 读取</param>
    /// <returns>执行结果 JSON（<c>{ok,value,error}</c>）</returns>
    [ToolDescription("run_js")]
    [DisplayName("执行页面脚本")]
    [Description("在当前页面执行 JavaScript 并返回结果，可读取或操作页面元素。例如 document.title、document.querySelector('input[name=Name]').value 等")]
    public virtual async Task<String> RunJs([Description("要在当前页面执行的 JavaScript 代码")] String script, ToolCallContext? context = null)
    {
        if (script.IsNullOrEmpty()) return new { ok = false, error = "脚本不能为空" }.ToJson();

        // 审计：记录下发脚本内容，便于追溯
        XTrace.WriteLine("[AI][run_js] 下发脚本：{0}", script);

        var (writer, cp, uid, timeout) = ResolveChannel(context);
        if (writer == null) return new { ok = false, error = "浏览器通道未就绪（Writer 未注入）" }.ToJson();

        // 检查点编号：优先使用工具调用 ID（前端 tool 事件已携带，前后端一一对应），兜底生成
        var checkpointId = context?.ToolCallId;
        if (checkpointId.IsNullOrEmpty()) checkpointId = PageCheckpointService.NewCheckpointId();
        await writer(new { type = "run_js", checkpointId, script }.ToJson());

        return await cp.WaitForChoiceAsync(checkpointId, uid, timeout);
    }

    /// <summary>采集当前页面主要数据上下文（标准脚本）。复用 run_js 检查点管道，返回结构化 JSON</summary>
    /// <param name="context">工具调用上下文（由框架注入），透传给 RunJs</param>
    /// <returns>采集结果 JSON（<c>{ok,value,error}</c>），value 为结构化页面数据</returns>
    public virtual async Task<String> CollectPageContextAsync(ToolCallContext? context = null)
        => await RunJs(PageContextCollector.BuildScript(), context);

    /// <summary>解析浏览器通道：优先上下文注入的 <see cref="CubeBrowserContext"/>，兜底实例属性</summary>
    /// <param name="context">工具调用上下文</param>
    /// <returns>通道四元组（写回调、检查点服务、用户编号、超时秒数）</returns>
    private (Func<String, Task>? writer, PageCheckpointService cp, Int64 uid, Int32 timeout) ResolveChannel(ToolCallContext? context)
    {
        var bc = context?[CubeBrowserContext.BrowserContextKey] as CubeBrowserContext;
        return (
            bc?.Writer ?? Writer,
            bc?.CheckpointService ?? CheckpointService ?? PageCheckpointService.Instance,
            bc?.UserId ?? userId,
            bc?.TimeoutSeconds > 0 ? bc.TimeoutSeconds : TimeoutSeconds
        );
    }
}

/// <summary>页面上下文采集器。生成标准浏览器采集脚本，抓取当前页面的主要数据（标题/表格/表单/扩展钩子）</summary>
/// <remarks>
/// 脚本在用户浏览器执行（经 run_js 检查点管道），返回结构化 JSON：
/// <code>
/// { title, url, path, headings[], tables[{caption,headers,rows,rowCount}], forms[{name,label,type,value}], dataAttrs{}, hints[] }
/// </code>
/// 页面可用 <c>data-ai-context</c> 属性显式暴露自定义数据（属性值作为键，元素文本作为值）。
/// 采集结果控制在 8192 字符内（前端回传截断阈值），表/行/列与单元格文本均设上限。
/// 主题差异大时，二次开发者可替换采集脚本（继承/重写 <see cref="BrowserToolService.CollectPageContextAsync"/>）。
/// </remarks>
public static class PageContextCollector
{
    /// <summary>构建标准页面采集脚本（IIFE，返回结构化页面数据对象）</summary>
    /// <returns>可执行 JavaScript 脚本字符串</returns>
    public static String BuildScript() => """
        (function () {
            function txt(el) {
                if (!el) return '';
                var s = (el.textContent || '').replace(/\s+/g, ' ').trim();
                return s.length > 60 ? s.substring(0, 60) : s;
            }
            var rs = { title: document.title || '', url: location.href || '', path: location.pathname || '', headings: [], tables: [], forms: [], dataAttrs: {}, hints: [] };
            var hds = document.querySelectorAll('h1,h2');
            for (var i = 0; i < hds.length && i < 10; i++) rs.headings.push(txt(hds[i]));
            var tables = document.querySelectorAll('table');
            for (var ti = 0; ti < tables.length && ti < 5; ti++) {
                var t = tables[ti];
                var cap = t.caption ? txt(t.caption) : (t.getAttribute('aria-label') || '');
                var rows = t.querySelectorAll('tr');
                var hr = t.querySelector('thead tr') || (rows.length ? rows[0] : null);
                var header = [];
                if (hr) {
                    var hc = hr.querySelectorAll('th,td');
                    for (var i = 0; i < hc.length && i < 8; i++) header.push(txt(hc[i]));
                }
                var data = [];
                for (var ri = 0; ri < rows.length && data.length < 5; ri++) {
                    if (rows[ri] === hr) continue;
                    var cells = [];
                    var cc = rows[ri].querySelectorAll('td,th');
                    for (var i = 0; i < cc.length && i < 8; i++) cells.push(txt(cc[i]));
                    if (cells.length) data.push(cells);
                }
                rs.tables.push({ caption: cap, headers: header, rows: data, rowCount: rows.length - (hr ? 1 : 0) });
            }
            var fields = [];
            var inputs = document.querySelectorAll('input,select,textarea');
            for (var i = 0; i < inputs.length && fields.length < 20; i++) {
                var el = inputs[i];
                var name = el.name || el.id || '';
                if (!name) continue;
                var label = '';
                if (el.id) {
                    var l = document.querySelector('label[for="' + el.id + '"]');
                    if (l) label = txt(l);
                }
                if (!label) label = el.getAttribute('aria-label') || el.getAttribute('placeholder') || '';
                var type = el.tagName.toLowerCase();
                if (el.tagName === 'INPUT') type += ':' + (el.type || 'text');
                var val = '';
                if (el.tagName === 'SELECT') {
                    var so = el.options[el.selectedIndex];
                    val = so ? so.text : '';
                } else if (el.type === 'checkbox' || el.type === 'radio') {
                    val = el.checked ? 'true' : 'false';
                } else {
                    var v = el.value || '';
                    val = v.length > 100 ? v.substring(0, 100) : v;
                }
                fields.push({ name: name, label: label || name, type: type, value: val });
            }
            rs.forms = fields;
            var attrs = document.querySelectorAll('[data-ai-context]');
            for (var i = 0; i < attrs.length; i++) {
                var k = attrs[i].getAttribute('data-ai-context') || 'value';
                rs.dataAttrs[k] = txt(attrs[i]);
            }
            var trows = document.querySelectorAll('table tbody tr').length;
            if (trows > 0) rs.hints.push('数据行数:' + trows);
            return rs;
        })()
        """;
}

/// <summary>页面数据上下文工具服务。提供 get_page_context 工具：优先调用宿主控制器的服务端实现（<see cref="IPageDataContext"/>），否则浏览器采集兜底</summary>
/// <remarks>
/// 两级自动降级：
/// <list type="number">
/// <item>宿主控制器实现 <see cref="IPageDataContext"/> → 调用其服务端实现（结构化、权威数据，如图表/报表/配置摘要）</item>
/// <item>否则（或服务端抛异常）→ 经 <see cref="BrowserToolService.CollectPageContextAsync"/> 在用户浏览器执行标准采集脚本抓取页面主要数据</item>
/// </list>
/// 浏览器层复用 run_js 检查点管道（SSE 下发 + POST 回传 + 事件总线广播），任何页面零后端改动即可获得"当前页面数据"。
/// 由 <see cref="Controllers.AiController"/> 统一注册到所有端点。
/// 工具方法为 virtual，二次开发者可继承重写。
/// </remarks>
/// <param name="ctrl">宿主控制器（当前页面控制器），用于检测 <see cref="IPageDataContext"/> 实现</param>
/// <param name="browser">浏览器工具服务，提供浏览器采集兜底</param>
public class PageDataContextToolService(ControllerBaseX ctrl, BrowserToolService browser)
{
    /// <summary>获取当前页面上下文：优先页面提供的服务端实现，否则自动采集用户浏览器当前页面内容</summary>
    /// <param name="context">工具调用上下文（由框架注入），透传给浏览器采集层</param>
    /// <returns>页面数据上下文 JSON 字符串</returns>
    [ToolDescription("get_page_context", ReadOnly = true)]
    [DisplayName("获取页面上下文")]
    [Description("获取当前页面的主要数据与页面结构（标题、表格、表单等）。优先使用页面提供的服务端上下文；无服务端实现时自动采集用户浏览器当前页面内容")]
    public virtual async Task<String> GetPageContextAsync(ToolCallContext? context = null)
    {
        // 一级：宿主控制器实现服务端上下文接口，返回结构化权威数据
        if (ctrl is IPageDataContext pdc)
        {
            try
            {
                var data = await pdc.GetPageDataContextAsync();
                if (!data.IsNullOrEmpty()) return data;
            }
            catch (Exception ex)
            {
                XTrace.WriteLine("[AI][get_page_context] 服务端上下文失败，回退浏览器采集：{0}", ex.Message);
            }
        }

        // 二级：浏览器采集兜底（复用 run_js 检查点管道，任何页面可用）
        return await browser.CollectPageContextAsync(context);
    }
}

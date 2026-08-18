using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using NewLife;
using NewLife.AI.Models;
using NewLife.AI.Services;
using NewLife.AI.Tools;
using NewLife.Collections;
using NewLife.Common;
using NewLife.Cube.AI;
using NewLife.Log;
using NewLife.Serialization;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Controllers;

/// <summary>AI 全局接口。统一承载所有页面的 AI 对话端点与浏览器操作结果回传</summary>
/// <remarks>
/// 所有页面的 AI 对话统一走本控制器 <c>/Ai/AiChat</c> 端点（无区域前缀）：
/// 请求体携带目标页面（area/controller），服务端解析并实例化目标控制器，
/// 按能力接口分级注册工具：
/// <list type="number">
/// <item>实现 <see cref="IEntityAiContext"/>（实体控制器）：注册实体数据上下文工具
/// （get_data_context / get_form_schema / fill_form）并使用其定制提示词（保留子类重载 SearchData / CreateCubeTools / BuildChatSystemPrompt）</item>
/// <item>实现 <see cref="IFormAiContext"/>（配置表单页，如魔方设置）：注册表单工具（get_form_schema / fill_form）并使用表单提示词</item>
/// <item>其他非实体页面：注册通用工具（get_system_info / get_page_context / run_js）并使用通用提示词</item>
/// </list>
/// 目标控制器实现 <see cref="IPageDataContext"/> 时，get_page_context 优先调用其服务端实现。
/// 浏览器操作回传亦放本控制器（<see cref="OperationResult"/>），避免为每个页面重复增加接口。
/// 本控制器为全局控制器（不标记 <see cref="AdminArea"/>），路由统一为 <c>/Ai/[action]</c>（无区域前缀），
/// 所有调用方（MVC _AiAssistant / ai-assistant.js / Vue AiAssistant.vue）统一使用该地址；
/// 非区域控制器命中不了 <c>{area}/{controller}/{action}</c> 约定路由，只能靠属性路由显式声明。
/// 请求校验、_query 解码与 SSE 输出管道（原 AiChatEndpoint 公共逻辑）已内聚在本控制器。
/// 文件位于 AI 目录，命名空间保持 <c>NewLife.Cube.Controllers</c>（菜单 FullName 依赖，避免权限失效）。
/// </remarks>
[DisplayName("AI")]
[Route("Ai/[action]")]
public class AiController : ControllerBaseX
{
    /// <summary>SSE 事件的 JSON 序列化选项（规范协议，camelCase + 忽略 null + 中文直出 + Int64 超 JS 安全整数转字符串）</summary>
    private static readonly JsonSerializerOptions _sseJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // 允许中文等非 ASCII 字符直接输出，避免 SSE 数据中出现 \uXXXX 转义
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new SafeInt64Converter() },
    };

    #region AI 对话
    /// <summary>AI 对话（全局统一端点）。按请求目标解析页面控制器能力，SSE 流式返回</summary>
    /// <returns></returns>
    [DisplayName("AI 对话")]
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpPost]
    public async Task<ActionResult> AiChat()
    {
        // 请求校验（AISwitch/请求体）与 SSE 输出管道由本控制器公共逻辑统一提供
        var (error, req) = await ParseAsync(this);
        if (error != null || req == null) return error!;

        // 当前查询条件（_query Base64 解码）
        var pager = DecodePager(req);

        // 解析目标页面控制器：实现 IEntityAiContext 则使用实体工具与定制提示词，否则通用
        var target = ResolveTarget(req);

        var registry = new ToolRegistry();
        registry.AddTools(new SystemInfoToolService());
        registry.AddTools(new BuiltinToolService());
        // 联网工具：网页抓取/搜索/IP 归属地/天气/翻译（NewLife.AI 内置，经 DI 解析免费实现）
        registry.AddTools(new NetworkToolService(HttpContext.RequestServices));

        String systemPrompt;
        if (target is IEntityAiContext eac)
        {
            // 实体控制器：按目标实体页 Detail 权限校验（等价原 {controller}/AiChat 的 EntityAuthorize），拒绝越权对话
            if (!CheckEntityPermission(target.GetType())) return Json(403, null, "无权访问该页面数据");

            // 实体上下文工具集由目标控制器自建（保留子类重载 CreateCubeTools），数据查询委托走其 SearchData
            registry.AddTools(eac.CreateCubeTools(pager, req.Id));
            systemPrompt = eac.BuildChatSystemPrompt(req, pager);
        }
        else if (target is IFormAiContext fac)
        {
            // 配置表单页（如魔方设置）：注册表单工具（get_form_schema / fill_form）+ 表单提示词，支持 AI 填表
            registry.AddTools(new ConfigFormToolService(fac));
            systemPrompt = fac.BuildFormSystemPrompt(req);
        }
        else
        {
            // 其他非实体页面（或解析失败）：通用工具 + 通用提示词
            systemPrompt = BuildChatSystemPrompt(req);
        }

        // SSE 输出（含浏览器操作工具注册；get_page_context 以目标控制器为宿主检测 IPageDataContext）
        await RunSseAsync(this, req, systemPrompt, registry, target);

        return new EmptyResult();
    }

    /// <summary>构建 AI 对话系统提示词（全局端点，无实体上下文）</summary>
    /// <param name="req">对话请求</param>
    /// <returns></returns>
    private static String BuildChatSystemPrompt(AiChatRequest req)
    {
        var sysName = SysConfig.Current?.DisplayName;
        if (sysName.IsNullOrEmpty()) sysName = "魔方后台管理系统";

        var sb = Pool.StringBuilder.Get();
        sb.AppendLine($"你是{sysName}的 AI 助手，正在协助管理员操作当前页面。");
        sb.AppendLine();
        sb.AppendLine("当前页面不属于实体数据页面（可能是系统设置、首页或系统信息等），无实体表上下文。");
        sb.AppendLine();
        sb.AppendLine("可用工具：get_page_context / get_system_info / run_js（详细说明见函数定义，按需调用）");
        sb.AppendLine();
        sb.AppendLine("规则：");
        sb.AppendLine("1. 使用简体中文回答，语言简洁专业");
        sb.AppendLine("2. 用户询问当前页面内容/数据/结构时，调用 get_page_context 获取页面上下文（自动采集用户浏览器当前页面内容），再给出分析与结论");
        sb.AppendLine("3. 用户询问系统状态/诊断时，调用 get_system_info 获取数据，再给出分析结论与建议");
        sb.AppendLine("4. 用户要求读取或操作当前页面元素（填写输入框、点击按钮、读取标题等）时，可调用 run_js 执行 JavaScript；脚本在用户浏览器当前页面执行，可用 document.querySelector 等定位元素；修改页面内容或提交表单等写操作前，先向用户说明将执行的操作");
        sb.AppendLine("5. 不要编造数据；信息不足时主动询问用户澄清");

        return sb.Return(true);
    }
    #endregion

    #region 端点公共逻辑
    // 请求校验 / _query 解码 / SSE 输出管道。仅本端点使用，内聚于本控制器（原 AiChatEndpoint 公共逻辑合并而来）

    /// <summary>解析并校验 AI 对话请求</summary>
    /// <param name="ctrl">当前控制器</param>
    /// <returns>校验失败返回错误响应与空值；成功返回对话请求</returns>
    private static async Task<(ActionResult? Error, CubeAiChatRequest? Req)> ParseAsync(ControllerBaseX ctrl)
    {
        var set = CubeSetting.Current;
        if (!set.AISwitch) return (ctrl.Json(500, null, "AI 未启用，请联系系统管理员开启 AISwitch"), null);

        // 读取 JSON 请求体
        var body = await new StreamReader(ctrl.Request.Body).ReadToEndAsync();
        if (body.IsNullOrEmpty()) return (ctrl.Json(500, null, "请求体为空"), null);
        var req = body.ToJsonEntity<CubeAiChatRequest>();
        if (req == null || req.Message.IsNullOrEmpty()) return (ctrl.Json(500, null, "消息不能为空"), null);

        return (null, req);
    }

    /// <summary>解析对话请求中的查询条件（_query Base64 解码），失败返回 null</summary>
    /// <param name="req">对话请求</param>
    /// <returns>分页查询条件；无查询或解析失败时返回 null</returns>
    private static Pager? DecodePager(CubeAiChatRequest req)
    {
        if (req.Query.IsNullOrEmpty()) return null;

        try
        {
            var queryData = req.Query.ToBase64().ToStr();
            var pager = new Pager();
            pager.Parse(queryData);
            return pager;
        }
        catch (Exception ex)
        {
            XTrace.WriteLine("AiChat 解析 _query 失败：{0}", ex.Message);
            return null;
        }
    }

    /// <summary>执行 SSE 流式对话。设置响应头、注册浏览器工具、内联对话核心逻辑（原 CubeAIChatService 合并）</summary>
    /// <param name="ctrl">当前控制器</param>
    /// <param name="req">对话请求</param>
    /// <param name="systemPrompt">系统提示词（由宿主构建以保留子类重载）</param>
    /// <param name="registry">工具注册表（宿主可先追加实体上下文工具与内置工具）</param>
    /// <param name="contextCtrl">目标页面控制器。用于 get_page_context 检测 <see cref="IPageDataContext"/> 服务端实现；为空时回退 ctrl</param>
    private static async Task RunSseAsync(ControllerBaseX ctrl, AiChatRequest req, String systemPrompt, ToolRegistry registry, ControllerBaseX? contextCtrl = null)
    {
        // SSE 写回调：浏览器工具服务经它下发 run_js 事件到前端，对话事件同样经它输出
        Func<String, Task> writeEvent = async json =>
        {
            await ctrl.Response.WriteAsync($"data: {json}\n\n", ctrl.HttpContext.RequestAborted);
            await ctrl.Response.Body.FlushAsync(ctrl.HttpContext.RequestAborted);
        };

        // 浏览器工具服务：run_js 等工具经页面检查点服务下发脚本到前端执行，结果回传后端继续对话（本控制器 OperationResult 端点回传）。
        // 检查点服务优先取 DI（事件总线广播，支持分布式集群），兜底进程内实例。
        var user = ctrl.HttpContext.User.Identity as IUser;
        var cp = ctrl.HttpContext.RequestServices.GetService<PageCheckpointService>();
        var browser = new BrowserToolService(user?.ID ?? 0) { Writer = writeEvent, CheckpointService = cp };
        registry.AddTools(browser);

        // 页面数据上下文工具：优先目标页面控制器服务端实现（IPageDataContext），否则浏览器采集兜底
        registry.AddTools(new PageDataContextToolService(contextCtrl ?? ctrl, browser));

        // SSE 输出
        ctrl.Response.Headers["Content-Type"] = "text/event-stream; charset=utf-8";
        ctrl.Response.Headers["Cache-Control"] = "no-cache";
        ctrl.Response.Headers["X-Accel-Buffering"] = "no";

        // 对话核心：NAI 轻量编排服务（会话历史 + 工具循环 + 空响应兜底 + 规范事件流）。会话存储经 DI 单例（MemoryCache 1h 过期）
        var set = CubeSetting.Current;
        var think = req.Think || set.AIDefaultThink;

        // 浏览器通道上下文：工具经 ToolCallContext.Items 读取（SSE 写回调/检查点服务/用户编号），实现工具与宿主的松耦合
        var options = new ChatOptions
        {
            Model = set.AIModel,
            EnableThinking = think,
            Temperature = think ? 0.5 : 0.3,
        };
        options.Items[CubeBrowserContext.BrowserContextKey] = new CubeBrowserContext
        {
            Writer = writeEvent,
            CheckpointService = cp,
            UserId = user?.ID ?? 0,
            TimeoutSeconds = 30,
        };

        var aiSvc = ctrl.HttpContext.RequestServices.GetService<IAIService>();
        if (aiSvc == null)
        {
            await writeEvent(new { type = "error", message = "AI 服务未注册" }.ToJson());
            return;
        }
        // 每请求以当前配置创建编排服务（AI 客户端随配置变化重建，运行中修改配置即时生效）；会话历史经单例 ChatSessionService 保持
        var sessions = ctrl.HttpContext.RequestServices.GetService<ChatSessionService>();
        var ai = new AiChatService(aiSvc.Client, sessions);

        await foreach (var ev in ai.ChatAsync(req, systemPrompt, [registry], options, ctrl.HttpContext.RequestAborted))
        {
            await writeEvent(JsonSerializer.Serialize(ev, _sseJsonOptions));
        }
    }
    #endregion

    #region 目标控制器解析
    private static readonly ConcurrentDictionary<String, (Type Type, ControllerActionDescriptor? Descriptor)> _targets = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>解析对话请求的目标页面控制器，并初始化其实例（共享当前请求上下文）</summary>
    /// <param name="req">对话请求</param>
    /// <returns>目标控制器实例；解析或初始化失败返回 null（按通用流程处理）</returns>
    private ControllerBaseX? ResolveTarget(CubeAiChatRequest req)
    {
        var (type, ad) = ResolveTargetInfo(req);
        if (type == null) return null;

        try
        {
            // 优先 DI 创建（支持构造注入），兜底无参构造
            var ctrl = ActivatorUtilities.CreateInstance(HttpContext.RequestServices, type) as ControllerBaseX;
            if (ctrl == null) return null;

            // 共享当前请求上下文：HttpContext 复用，路由数据与动作描述符指向目标控制器
            var routeData = new RouteData();
            if (!req.Area.IsNullOrEmpty()) routeData.Values["area"] = req.Area;
            routeData.Values["controller"] = req.Controller;
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = HttpContext,
                RouteData = routeData,
                ActionDescriptor = ad ?? new ControllerActionDescriptor
                {
                    ControllerTypeInfo = type.GetTypeInfo(),
                    // 仅用于 OnActionExecuting 的匿名判断，取当前端点方法即可
                    MethodInfo = typeof(AiController).GetMethod(nameof(AiChat)),
                },
            };

            // 初始化控制器状态（Session/Menu/CurrentUser/Token），与 MVC 动作执行前一致
            ctrl.OnActionExecuting(new ActionExecutingContext(ctrl.ControllerContext, [], new Dictionary<String, Object?>(), ctrl));

            return ctrl;
        }
        catch (Exception ex)
        {
            // 目标控制器初始化失败不影响对话可用性，降级为通用流程
            XTrace.WriteLine("[AI] 初始化目标控制器[{0}]失败：{1}", type.FullName, ex.Message);
            return null;
        }
    }

    /// <summary>按区域与控制器名解析目标控制器类型与动作描述符</summary>
    /// <param name="req">对话请求</param>
    /// <returns>控制器类型与描述符；未找到返回空</returns>
    private (Type? Type, ControllerActionDescriptor? Descriptor) ResolveTargetInfo(CubeAiChatRequest req)
    {
        if (req.Controller.IsNullOrEmpty()) return (null, null);

        var key = (req.Area ?? "") + "." + req.Controller;
        if (_targets.TryGetValue(key, out var cached)) return cached;

        // 优先从 MVC 动作描述符集合解析（覆盖全部区域与程序集，含真实 MethodInfo）
        var provider = HttpContext.RequestServices.GetService<IActionDescriptorCollectionProvider>();
        ControllerActionDescriptor? ad = null;
        if (provider != null)
        {
            foreach (var item in provider.ActionDescriptors.Items.OfType<ControllerActionDescriptor>())
            {
                if (!item.RouteValues.TryGetValue("area", out var area)) area = "";
                if (!item.RouteValues.TryGetValue("controller", out var ctrl)) ctrl = "";
                if (!ctrl.EqualIgnoreCase(req.Controller) || !area.EqualIgnoreCase(req.Area ?? "")) continue;

                ad = item;
                break;
            }
        }

        Type? type = ad?.ControllerTypeInfo;
        if (type == null)
        {
            // 兜底：程序集扫描 {controller}Controller 类型
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); } catch { continue; }

                type = types.FirstOrDefault(t => t.IsClass && !t.IsAbstract && t.Name.EqualIgnoreCase(req.Controller + "Controller"));
                if (type != null) break;
            }
        }

        if (type != null) _targets.TryAdd(key, (type, ad));
        return (type, ad);
    }

    /// <summary>校验当前用户对目标实体控制器的 Detail 权限。实体页对话等价原 {controller}/AiChat 的 EntityAuthorize 语义</summary>
    /// <param name="type">目标控制器类型</param>
    /// <returns>有权限或无菜单时返回 true</returns>
    private Boolean CheckEntityPermission(Type type)
    {
        var user = HttpContext.User.Identity as IUser;
        user ??= ManageProvider.User;
        if (user == null) return false;

        // 实体控制器菜单由 MenuHelper.ScanController 注册，FullName 为控制器类型全名
        var menu = ManageProvider.Menu?.FindByFullName(type.FullName);
        if (menu == null) return true; // 无菜单不拦截（非实体页仅全局 AI 权限）

        return user.Has(menu, PermissionFlags.Detail);
    }
    #endregion

    /// <summary>浏览器操作结果回传。前端执行 run_js 脚本后回传结果，完成等待中的工具调用</summary>
    /// <returns></returns>
    [DisplayName("浏览器操作结果回传")]
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpPost]
    public async Task<ActionResult> OperationResult()
    {
        var body = await new StreamReader(Request.Body).ReadToEndAsync();
        if (body.IsNullOrEmpty()) return Json(500, null, "请求体为空");
        var req = body.ToJsonEntity<BrowserOpResult>();
        if (req == null || req.CheckpointId.IsNullOrEmpty()) return Json(500, null, "参数错误");

        // 分布式：经 DI 解析检查点服务（事件总线广播，POST 命中任意实例都能唤醒等待方），兜底进程内实例
        var cp = HttpContext.RequestServices.GetService<PageCheckpointService>() ?? PageCheckpointService.Instance;
        var user = HttpContext.User.Identity as IUser;
        var ok = await cp.Respond(req.CheckpointId, user?.ID ?? 0, req.Result ?? "{}");

        return ok ? Json(0, null, "ok") : Json(500, null, "操作不存在或已过期");
    }
}

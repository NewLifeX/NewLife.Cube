using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using NewLife;
using NewLife.AI.Tools;
using NewLife.Collections;
using NewLife.Common;
using NewLife.Cube.AI;
using NewLife.Log;
using NewLife.Serialization;
using XCode.Membership;

namespace NewLife.Cube.Controllers;

/// <summary>AI 全局接口。统一承载所有页面的 AI 对话端点与浏览器操作结果回传</summary>
/// <remarks>
/// 所有页面的 AI 对话统一走本控制器 <c>/Ai/AiChat</c> 端点（无区域前缀）：
/// 请求体携带目标页面（area/controller），服务端解析并实例化目标控制器，
/// 若其实现 <see cref="IEntityAiContext"/>（实体控制器）则注册实体数据上下文工具
/// （get_data_context / get_form_schema / fill_form）并使用其定制提示词（保留子类重载 SearchData / CreateCubeTools / BuildChatSystemPrompt）；
/// 否则注册通用工具（get_system_info / get_page_context / run_js）并使用通用提示词。
/// 目标控制器实现 <see cref="IPageDataContext"/> 时，get_page_context 优先调用其服务端实现。
/// 浏览器操作回传亦放本控制器（<see cref="OperationResult"/>），避免为每个页面重复增加接口。
/// 本控制器为全局控制器（不标记 <see cref="AdminArea"/>），路由统一为 <c>/Ai/[action]</c>（无区域前缀），
/// 所有调用方（MVC _AiAssistant / ai-assistant.js / Vue AiAssistant.vue）统一使用该地址；
/// 非区域控制器命中不了 <c>{area}/{controller}/{action}</c> 约定路由，只能靠属性路由显式声明。
/// </remarks>
[DisplayName("AI")]
[Route("Ai/[action]")]
public class AiController : ControllerBaseX
{
    #region AI 对话
    /// <summary>AI 对话（全局统一端点）。按请求目标解析页面控制器能力，SSE 流式返回</summary>
    /// <returns></returns>
    [DisplayName("AI 对话")]
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpPost]
    public async Task<ActionResult> AiChat()
    {
        // 请求校验（AISwitch/服务注册/请求体）与 SSE 输出管道由 AiChatEndpoint 统一提供
        var (error, svc, req) = await AiChatEndpoint.ParseAsync(this);
        if (error != null || svc == null || req == null) return error!;

        // 当前查询条件（_query Base64 解码）
        var pager = AiChatEndpoint.DecodePager(req);

        // 解析目标页面控制器：实现 IEntityAiContext 则使用实体工具与定制提示词，否则通用
        var target = ResolveTarget(req);

        var registry = new ToolRegistry();
        registry.AddTools(new SystemInfoToolService());
        registry.AddTools(new BuiltinToolService());

        String systemPrompt;
        if (target is IEntityAiContext eac)
        {
            // 实体控制器：按目标实体页 Detail 权限校验（等价原 {controller}/AiChat 的 EntityAuthorize），拒绝越权对话
            if (!CheckEntityPermission(target.GetType())) return Json(403, null, "无权访问该页面数据");

            // 实体上下文工具集由目标控制器自建（保留子类重载 CreateCubeTools），数据查询委托走其 SearchData
            registry.AddTools(eac.CreateCubeTools(pager, req.Id));
            systemPrompt = eac.BuildChatSystemPrompt(req, pager);
        }
        else
        {
            // 非实体页面（或解析失败）：通用工具 + 通用提示词
            systemPrompt = BuildChatSystemPrompt(req);
        }

        // SSE 输出（含浏览器操作工具注册；get_page_context 以目标控制器为宿主检测 IPageDataContext）
        await AiChatEndpoint.RunSseAsync(this, svc, req, systemPrompt, registry, target);

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

    #region 目标控制器解析
    private static readonly ConcurrentDictionary<String, (Type Type, ControllerActionDescriptor? Descriptor)> _targets = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>解析对话请求的目标页面控制器，并初始化其实例（共享当前请求上下文）</summary>
    /// <param name="req">对话请求</param>
    /// <returns>目标控制器实例；解析或初始化失败返回 null（按通用流程处理）</returns>
    private ControllerBaseX? ResolveTarget(AiChatRequest req)
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
    private (Type? Type, ControllerActionDescriptor? Descriptor) ResolveTargetInfo(AiChatRequest req)
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

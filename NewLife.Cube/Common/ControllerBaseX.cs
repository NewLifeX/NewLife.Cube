using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Cube.Extensions;
using NewLife.Log;
using NewLife.Model;
using NewLife.Remoting;
using NewLife.Security;
using NewLife.Serialization;
using XCode.Membership;
using IActionFilter = Microsoft.AspNetCore.Mvc.Filters.IActionFilter;

namespace NewLife.Cube;

/// <summary>控制器基类。WebAPI版实体/后台控制器路由固定 /api 前缀；Sso/Cube/Auth 等服务控制器自带 [Route] 覆盖，不使用前缀</summary>
[ApiController]
[Produces("application/json")]
[Route("api/[area]/[controller]/[action]")]
public class ControllerBaseX : ControllerBase, IActionFilter
{

    #region 属性
    /// <summary>临时会话扩展信息。仅限本地内存，不支持分布式共享</summary>
    public IDictionary<String, Object> Session { get; private set; }

    /// <summary>令牌</summary>
    public String Token { get; set; }

    /// <summary>当前页面菜单。用于权限控制</summary>
    public IMenu Menu { get; set; }

    /// <summary>当前用户</summary>
    public IManageUser CurrentUser { get; set; }

    /// <summary>当前租户</summary>
    public ITenantUser CurrentTenant { get; set; }

    /// <summary>用户主机</summary>
    public String UserHost => HttpContext.GetUserHost();

    /// <summary>页面设置。这一轮请求专用，复制来自PageSetting.Global</summary>
    public PageSetting PageSetting { get; set; }

    private IDictionary<String, Object> _args;
    #endregion

    #region 构造
    /// <summary>实例化控制器</summary>
    public ControllerBaseX() => PageSetting = PageSetting.Global.Clone();

    /// <summary>动作执行前</summary>
    /// <param name="context"></param>
    [NonAction]
    public virtual void OnActionExecuting(ActionExecutingContext context)
    {
        _args = context.ActionArguments;

        var ctx = HttpContext;
        Session = ctx.Items["Session"] as IDictionary<String, Object>;
        Menu = ctx.Items["CurrentMenu"] as IMenu;

        // 仅用于测试，跳过报错
        Session ??= new Dictionary<String, Object>();

        // 没有用户时无权
        var user = ManageProvider.User;
        if (user != null && CurrentUser == null)
        {
            CurrentUser = user as IManageUser;

            // // 设置变量，数据权限使用，最后面统一设置
            // HttpContext.Items["userId"] = user.ID;

            // 没有菜单时不做权限控制
            //if (Menu != null)
            //{
            //    PageSetting.EnableSelect = user.Has(Menu, PermissionFlags.Update, PermissionFlags.Delete);
            //}
        }

        // 当前租户。匿名请求没有用户时不查询租户关系，避免空引用
        var tid = ModelExtension.GetService<ITenantContext>(HttpContext.RequestServices)?.TenantId ?? 0;
        if (tid > 0 && user != null)
        {
            var tus = TenantUser.FindAllByUserId(user.ID);
            CurrentTenant = tus.FirstOrDefault(e => e.TenantId == tid);
        }

        // 访问令牌
        Token = context.HttpContext.LoadToken();
        try
        {
            if (CurrentUser == null && !Token.IsNullOrEmpty())
            {
                CurrentUser = ManagerProviderHelper.Auth(Token, ManageProvider.Provider);

                // 认证层租户校验（fail-closed）：多租户开启时，已认证用户必须处于有效租户上下文，否则拒绝访问
                if (CurrentUser != null && !HttpContext.ValidateTenant(CurrentUser))
                {
                    throw new ApiException(403, L.T("No permission to access the current tenant."));
                }
            }

            if (CurrentUser == null && context.ActionDescriptor is ControllerActionDescriptor act && !act.MethodInfo.IsDefined(typeof(AllowAnonymousAttribute)))
            {
                throw new ApiException(403, L.T("Authentication failed."));
            }

            if (CurrentUser != null)
            {
                // 设置变量，数据权限使用
                HttpContext.Items["userId"] = CurrentUser.ID;

                // 选择列是否出现由页面能力决定；批量修改/删除的用户权限由前端结合用户信息计算
                // PageSetting.EnableSelect 不在这里按权限改写，避免权限变化后被 /meta 缓存成旧值
            }
        }
        catch (Exception ex)
        {
            // 匿名接口忽略令牌验证失败（如登录页调用 GetLoginConfig 时携带了过期令牌），继续以匿名身份执行
            if (context.ActionDescriptor is ControllerActionDescriptor actAnon && actAnon.MethodInfo.IsDefined(typeof(AllowAnonymousAttribute)))
                return;

            WriteLog(null, false, ex.ToString());

            context.Result = Json(0, null, ex);
        }
    }

    /// <summary>动作执行后</summary>
    /// <param name="context"></param>
    [NonAction]
    public virtual void OnActionExecuted(ActionExecutedContext context)
    {
        var ex = context.Exception?.GetTrue();
        if (ex != null && !context.ExceptionHandled)
        {
            WriteLog(null, false, ex.ToString());
        }

        var traceId = DefaultSpan.Current?.TraceId;

        // 304 条件请求：动作已写入状态码，直接返回空结果，避免被包装成 200 JSON
        if (HttpContext.Response.StatusCode == 304 && !HttpContext.Response.HasStarted)
        {
            context.Result = new StatusCodeResult(304);
            return;
        }

        if (context.Result != null)
        {
            if (context.Result is ObjectResult obj)
            {
                if (obj.Value is IApiResponse response)
                {
                    context.Result = new ContentResult
                    {
                        Content = OnJsonSerialize(response),
                        ContentType = "application/json",
                        StatusCode = GetHttpStatusCode(response.Code)
                    };
                }
                else
                {
                    // 与 Json() 统一响应包保持同一字段集：code/message/data/traceId
                    var code = obj.StatusCode ?? 0;
                    var rs = new { code, message = (String)null, data = obj.Value, traceId };
                    context.Result = new ContentResult
                    {
                        Content = OnJsonSerialize(rs),
                        ContentType = "application/json",
                        StatusCode = GetHttpStatusCode(code),
                    };
                }
            }
            else if (context.Result is EmptyResult && !HttpContext.Response.HasStarted)
            {
                // SSE 等流式响应已开始（Headers 只读）后不能再转 JsonResult，保持原样以完整结束流
                context.Result = new JsonResult(new { code = 0, message = (String)null, data = new { }, traceId });
            }
        }
        else if (context.Exception != null && !context.ExceptionHandled && !HttpContext.Response.HasStarted)
        {
            // 与实体控制器 BuildFailResponse 保持同一套映射：ApiException 用自身码，其余异常统一 500
            var code = ApiCode.InternalServerError.ToInt();
            var message = NewLife.Common.SysConfig.Current.Develop ? ex.Message : L.T("The server is busy. Please try again later.");
            if (ex is ApiException aex)
            {
                code = aex.Code;
                message = aex.Message;
            }
            else if (ex is XException)
            {
                // XException 视为已知业务异常，消息对用户可读，生产环境也保留
                message = ex.Message;
            }

            context.Result = new JsonResult(new { code, message, traceId }) { StatusCode = GetHttpStatusCode(code) };

            context.ExceptionHandled = true;

            // 输出异常日志
            if (XTrace.Debug) XTrace.WriteException(ex);
        }
    }
    #endregion

    #region 兼容处理
    /// <summary>获取请求值</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    protected virtual String GetRequest(String key) => Request.GetRequestValue(key);
    #endregion

    #region Json结果
    /// <summary>响应Json结果</summary>
    /// <param name="code">代码。0成功，其它为错误代码</param>
    /// <param name="message">消息，成功或失败时的文本消息</param>
    /// <param name="data">数据对象</param>
    /// <param name="extend">扩展数据</param>
    /// <returns></returns>
    [NonAction]
    public virtual ActionResult Json(Int32 code, String message, Object data = null, Object extend = null)
    {
        if (data is Exception ex)
        {
            if (code == 0 && data is ApiException aex) code = aex.Code;
            if (code == 0) code = 500;
            if (message.IsNullOrEmpty()) message = ex.GetTrue()?.Message;
            data = null;
        }

        Object rs = new { code, message, data, traceId = DefaultSpan.Current?.TraceId };
        if (extend != null)
        {
            var dic = rs.ToDictionary();
            dic.Merge(extend);
            rs = dic;
        }

        var json = OnJsonSerialize(rs);
        DefaultSpan.Current?.AppendTag(json);

        return new ContentResult
        {
            Content = json,
            ContentType = "application/json; charset=utf-8",
            StatusCode = GetHttpStatusCode(code),
        };
    }

    /// <summary>由业务码取得协议层 HTTP 状态码</summary>
    /// <remarks>
    /// 仅在开启 <see cref="CubeSetting.EnableHttpStatusCodes"/> 时生效，用于让网关/监控识别 401/403/404/409/422/429/500；
    /// 其余业务码（0 与负数）保持 200，业务判错仍以响应体 code 为准。
    /// </remarks>
    /// <param name="code">业务返回码</param>
    /// <returns>HTTP 状态码，200 表示不改变</returns>
    protected static Int32 GetHttpStatusCode(Int32 code)
    {
        if (!CubeSetting.Current.EnableHttpStatusCodes) return 200;

        return code switch
        {
            401 or 403 or 404 or 409 or 422 or 429 or 500 => code,
            _ => 200,
        };
    }

    /// <summary>Json序列化。默认使用FastJson</summary>
    /// <param name="data"></param>
    /// <returns></returns>
    protected virtual String OnJsonSerialize(Object data)
    {
        var host = new SystemJson();
        var json = host.Write(data, new NewLife.Serialization.JsonOptions
        {
            WriteIndented = false,
            IgnoreNullValues = false,
            PropertyNaming = PropertyNaming.CamelCase,
            Int64AsString = true,
            IgnoreCycles = true,//忽略循环引用
            FullTime = false// yyyy-MM-dd HH:mm:ss
        });
        return json;
    }

    /// <summary>校验条件请求。数据哈希与 If-None-Match 匹配时写入 304 并返回 true</summary>
    /// <param name="data">响应数据（不含信封与 TraceId）</param>
    /// <returns>是否命中缓存，调用方应返回 null</returns>
    protected virtual Boolean CheckNotModified(Object data)
    {
        if (data == null) return false;

        String json;
        try
        {
            json = data.ToJson();
        }
        catch
        {
            // 序列化失败时放弃 ETag，不影响正常响应
            return false;
        }

        var etag = "\"" + json.MD5() + "\"";
        Response.Headers["ETag"] = etag;
        Response.Headers["Cache-Control"] = "private, no-cache";
        // meta 含用户列偏好、权限标志与租户上下文；条件缓存必须按这些维度区分
        Response.Headers["Vary"] = "Accept-Language, Authorization, Cookie, X-Tenant, X-App-Id";

        var inm = Request.Headers.IfNoneMatch;
        foreach (var item in inm)
        {
            if (item.IsNullOrEmpty()) continue;

            foreach (var part in item.Split(','))
            {
                var tag = part.Trim();
                if (tag == "*" || tag == etag || tag == "W/" + etag)
                {
                    Response.StatusCode = 304;
                    return true;
                }
            }
        }

        return false;
    }
    #endregion

    #region 辅助
    /// <summary>写审计日志</summary>
    /// <param name="action"></param>
    /// <param name="success"></param>
    /// <param name="remark"></param>
    protected virtual void WriteLog(String action, Boolean success, String remark)
    {
        action ??= GetControllerAction().LastOrDefault();

        var type = GetType();
        if (type.BaseType.IsGenericType) type = type.BaseType.GetGenericArguments().FirstOrDefault();

        LogProvider.Provider?.WriteLog(type, action, success, remark, 0, null, UserHost);
    }

    /// <summary>获取控制器名称，返回 area, controller, action</summary>
    /// <returns></returns>
    protected virtual String[] GetControllerAction()
    {
        var act = ControllerContext.ActionDescriptor;
        var controller = act.ControllerName;
        var action = act.ActionName;
        act.RouteValues.TryGetValue("Area", out var area);

        return new[] { area, controller, action };
    }
    #endregion
}
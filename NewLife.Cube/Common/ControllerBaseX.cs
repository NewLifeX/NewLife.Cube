using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Extensions;
using NewLife.Remoting;
using NewLife.Serialization;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>控制器基类</summary>
public class ControllerBaseX : Controller
{
    #region 属性
    /// <summary>临时会话扩展信息。仅限本地内存，不支持分布式共享</summary>
    public IDictionary<String, Object> Session { get; private set; }

    /// <summary>当前页面菜单。用于权限控制</summary>
    public IMenu Menu { get; set; }

    /// <summary>用户主机</summary>
    public String UserHost => HttpContext.GetUserHost();

    /// <summary>页面设置</summary>
    public PageSetting PageSetting { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化控制器</summary>
    public ControllerBaseX() => PageSetting = PageSetting.Global.Clone();

    /// <summary>动作执行前</summary>
    /// <param name="context"></param>
    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
    {
        // 页面设置
        ViewBag.PageSetting = PageSetting;

        var ctx = context.HttpContext;
        Session = ctx.Items["Session"] as IDictionary<String, Object>;
        Menu = ctx.Items["CurrentMenu"] as IMenu;
        ViewBag.Menu = Menu;

        // 没有用户时无权
        var user = ManageProvider.User;
        if (user != null)
        {
            // 设置变量，数据权限使用
            HttpContext.Items["userId"] = user.ID;

            // 没有菜单时不做权限控制
            if (Menu != null)
            {
                PageSetting.EnableSelect = user.Has(Menu, PermissionFlags.Update, PermissionFlags.Delete);
            }
        }

        base.OnActionExecuting(context);
    }

    /// <summary>动作执行后</summary>
    /// <param name="context"></param>
    public override void OnActionExecuted(Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext context)
    {
        var ex = context.Exception?.GetTrue();
        if (ex != null && !context.ExceptionHandled)
        {
            // 控制器发生异常时，写审计日志
            var act = GetControllerAction().LastOrDefault();
            WriteLog(act, false, ex.ToString());
        }

        if (IsJsonRequest)
        {
            if (ex != null && !context.ExceptionHandled)
            {
                var code = 500;
                var message = ex.Message;
                if (ex is ApiException aex)
                {
                    code = aex.Code;
                    message = aex.Message;
                }

                context.Result = Json(code, message, null);
                context.ExceptionHandled = true;
            }
        }

        base.OnActionExecuted(context);
    }
    #endregion

    #region 兼容处理
    /// <summary>获取请求值</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    protected virtual String GetRequest(String key) => Request.GetRequestValue(key);
    #endregion

    #region 权限菜单
    /// <summary>获取可用于生成权限菜单的Action集合</summary>
    /// <param name="menu">该控制器所在菜单</param>
    /// <returns></returns>
    [Obsolete("=>MenuAttribute")]
    protected virtual IDictionary<MethodInfo, Int32> ScanActionMenu(IMenu menu) => new Dictionary<MethodInfo, Int32>();
    #endregion

    #region Ajax处理
    /// <summary>返回结果并跳转</summary>
    /// <param name="data">结果。可以是错误文本、成功文本、其它结构化数据</param>
    /// <param name="url">提示信息后跳转的目标地址，[refresh]表示刷新当前页</param>
    /// <returns></returns>
    protected virtual ActionResult JsonTips(Object data, String url = null) => Json(0, data as String, data, new { url });

    /// <summary>返回结果并刷新</summary>
    /// <param name="data">消息</param>
    /// <returns></returns>
    protected virtual ActionResult JsonRefresh(Object data) => Json(0, data as String, data, new { url = "[refresh]" });

    /// <summary>
    /// 返回结果并刷新
    /// </summary>
    /// <param name="data">消息</param>
    /// <param name="time">延迟刷新秒数</param>
    /// <returns></returns>
    protected virtual ActionResult JsonRefresh(Object data, Int32 time) => Json(0, data as String, data, new { url = "[refresh]", time });

    /// <summary>是否Json请求</summary>
    protected virtual Boolean IsJsonRequest
    {
        get
        {
            if (Request.ContentType.EqualIgnoreCase("application/json")) return true;

            if (Request.Headers["Accept"].Any(e => e.Split(',').Any(a => a.Trim() == "application/json"))) return true;

            if (GetRequest("output").EqualIgnoreCase("json")) return true;
            if ((RouteData.Values["output"] + "").EqualIgnoreCase("json")) return true;

            return false;
        }
    }
    #endregion

    #region Json结果
    /// <summary>成功响应Json结果</summary>
    /// <param name="message">消息，成功或失败时的文本消息</param>
    /// <param name="data">数据对象</param>
    /// <returns></returns>
    [NonAction]
    public virtual ActionResult Ok(String message = "ok", Object data = null) => Json(0, message, data);

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

        Object rs = new { code, message, data };
        if (extend != null)
        {
            var dic = rs.ToDictionary();
            dic.Merge(extend);
            rs = dic;
        }

        return Content(OnJsonSerialize(rs), "application/json", Encoding.UTF8);
    }

    /// <summary>Json序列化。默认使用FastJson</summary>
    /// <param name="data"></param>
    /// <returns></returns>
    protected virtual String OnJsonSerialize(Object data)
    {
        //data.ToJson(false, true, true);
        var writer = new JsonWriter
        {
            Indented = false,
            IgnoreNullValues = false,
            CamelCase = true,
            Int64AsString = true
        };

        writer.Write(data);

        return writer.GetString();
    }
    #endregion

    #region 辅助
    /// <summary>写审计日志</summary>
    /// <param name="action"></param>
    /// <param name="success"></param>
    /// <param name="remark"></param>
    protected virtual void WriteLog(String action, Boolean success, String remark)
    {
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
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NewLife.Reflection;
using NewLife.Serialization;
using XCode.Membership;
using NewLife.Remoting;
using System.Linq;
using NewLife.Security;
#if __CORE__
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Extensions;
#else
using System.Web.Mvc;
#endif

namespace NewLife.Cube
{
    /// <summary>控制器基类</summary>
    public class ControllerBaseX : Controller
    {
        #region 属性
#if __CORE__
        ///// <summary></summary>
        //static readonly SessionProvider _sessionProvider = new SessionProvider();

        /// <summary>临时会话扩展信息</summary>
        public IDictionary<String, Object> Session { get; private set; }
#endif

        /// <summary>用户主机</summary>
        public String UserHost => HttpContext.GetUserHost();

        /// <summary>页面设置</summary>
        public PageSetting PageSetting { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化控制器</summary>
        public ControllerBaseX()
        {
            // 页面设置
            PageSetting = PageSetting.Global.Clone();
        }

        /// <summary>动作执行前</summary>
        /// <param name="context"></param>
#if __CORE__
        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
#else
        protected override void OnActionExecuting(ActionExecutingContext context)
#endif
        {
            // 页面设置
            ViewBag.PageSetting = PageSetting;

#if __CORE__
            //// 准备Session
            //var ss = context.HttpContext.Session;
            //if (ss != null)
            //{
            //    //var token = Request.Cookies["Token"];
            //    var token = ss.GetString("Cube_Token");
            //    if (token.IsNullOrEmpty())
            //    {
            //        token = Rand.NextString(16);
            //        //Response.Cookies.Append("Token", token, new CookieOptions { });
            //        ss.SetString("Cube_Token", token);
            //    }

            //    //Session = _sessionProvider.GetSession(ss.Id);
            //    Session = _sessionProvider.GetSession(token);
            //    context.HttpContext.Items["Session"] = Session;
            //}

            Session = context.HttpContext.Items["Session"] as IDictionary<String, Object>;
#endif

            // 没有用户时无权
            var user = ManageProvider.User;
            if (user != null)
            {
                // 没有菜单时不做权限控制
                //var menu = ManageProvider.Menu;
                var ctx = context.HttpContext;
                if (ctx.Items["CurrentMenu"] is IMenu menu)
                {
                    PageSetting.EnableSelect = user.Has(menu, PermissionFlags.Update, PermissionFlags.Delete);
                }
            }

            base.OnActionExecuting(context);
        }

        /// <summary>动作执行后</summary>
        /// <param name="context"></param>
#if __CORE__
        public override void OnActionExecuted(Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext context)
#else
        protected override void OnActionExecuted(ActionExecutedContext context)
#endif
        {
            if (IsJsonRequest)
            {
                var ex = context.Exception?.GetTrue();
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
#if __CORE__
        /// <summary>获取请求值</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected virtual String GetRequest(String key) => Request.GetRequestValue(key);

        /// <summary>获取Session值</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        protected virtual T GetSession<T>(String key) where T : class => HttpContext.Session.Get<T>(key);

        /// <summary>设置Session值</summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        protected virtual void SetSession(String key, Object value) => HttpContext.Session.Set(key, value);
#else
        /// <summary>获取请求值</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected virtual String GetRequest(String key) => Request[key];

        /// <summary>获取Session值</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        protected virtual T GetSession<T>(String key) => (T)Session[key];

        /// <summary>设置Session值</summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        protected virtual void SetSession(String key, Object value) => Session[key] = value;
#endif
        #endregion

        #region 权限菜单
        /// <summary>获取可用于生成权限菜单的Action集合</summary>
        /// <param name="menu">该控制器所在菜单</param>
        /// <returns></returns>
        protected virtual IDictionary<MethodInfo, Int32> ScanActionMenu(IMenu menu)
        {
            var dic = new Dictionary<MethodInfo, Int32>();

            var type = GetType();

            // 添加该类型下的所有Action
            foreach (var method in type.GetMethods())
            {
                if (method.IsStatic || !method.IsPublic) continue;

                if (!method.ReturnType.As<ActionResult>()) continue;

                //if (method.GetCustomAttribute<HttpPostAttribute>() != null) continue;
                if (method.GetCustomAttribute<AllowAnonymousAttribute>() != null) continue;

                var att = method.GetCustomAttribute<EntityAuthorizeAttribute>();
                if (att != null && att.Permission > PermissionFlags.None) dic.Add(method, (Int32)att.Permission);
            }

            return dic;
        }
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
        protected virtual ActionResult JsonRefresh(Object data, Int32 time) => Json(0, data as String, data, new { url = "[refresh]", time});

        /// <summary>是否Json请求</summary>
        protected virtual Boolean IsJsonRequest
        {
            get
            {
                if (Request.ContentType.EqualIgnoreCase("application/json")) return true;

#if __CORE__
                if (Request.Headers["Accept"].Any(e => e.Split(',').Any(a => a.Trim() == "application/json"))) return true;
#else
                if (Request.AcceptTypes.Any(e => e == "application/json")) return true;
#endif

                if (GetRequest("output").EqualIgnoreCase("json")) return true;
                if ((RouteData.Values["output"] + "").EqualIgnoreCase("json")) return true;

                return false;
            }
        }
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

#if __CORE__
            return new JsonResult(rs);
#else
            return Content(OnJsonSerialize(rs), "application/json", Encoding.UTF8);
#endif
        }

        /// <summary>返回Json数据</summary>
        /// <param name="data">数据对象，作为data成员返回</param>
        /// <param name="extend">与data并行的其它顶级成员</param>
        /// <returns></returns>
        [Obsolete("=>Json(code,message,data)")]
        protected virtual ActionResult JsonOK(Object data, Object extend = null)
        {
            var rs = new { result = true, data };
            var json = "";

            if (extend == null)
                json = OnJsonSerialize(rs);
            else
            {
                var dic = rs.ToDictionary();
                dic.Merge(extend);
                json = OnJsonSerialize(dic);
            }

            return Content(json, "application/json", Encoding.UTF8);
        }

        /// <summary>返回Json错误</summary>
        /// <param name="data">数据对象或异常对象，作为data成员返回</param>
        /// <param name="extend">与data并行的其它顶级成员</param>
        /// <returns></returns>
        [Obsolete("=>Json(code,message,data)")]
        protected virtual ActionResult JsonError(Object data, Object extend = null)
        {
            if (data is Exception ex) data = ex.GetTrue().Message;

            var rs = new { result = false, data };
            var json = "";

            if (extend == null)
                json = OnJsonSerialize(rs);
            else
            {
                var dic = rs.ToDictionary();
                dic.Merge(extend);
                json = OnJsonSerialize(dic);
            }

            return Content(json, "application/json", Encoding.UTF8);
        }

        /// <summary>Json序列化。默认使用FastJson</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual String OnJsonSerialize(Object data) => data.ToJson();
        #endregion
    }
}
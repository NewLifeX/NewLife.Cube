using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NewLife.Cube;
using NewLife.Cube.Extensions;
using NewLife.Log;

namespace NewLife.Cube.Com
{
    /// <summary>拦截MVC流程错误的特性</summary>
    public class MvcHandleErrorAttribute : ExceptionFilterAttribute
    {
        static MvcHandleErrorAttribute()
        {
            XTrace.WriteLine("注册MVC错误过滤器：{0}", typeof(MvcHandleErrorAttribute).FullName);
        }

        /// <summary>拦截异常</summary>
        /// <param name="ctx"></param>
        public override void OnException(ExceptionContext ctx)
        {
            if (ctx.ExceptionHandled) return;

            XTrace.WriteException(ctx.Exception);
            var ex = ctx.Exception?.GetTrue();
            if (ex != null)
            {
                //以下异常处理不属于mvc流程，需要使用中间件拦截处理！！！

                //// 避免反复出现缺少文件
                //if (ex is HttpException hex && (UInt32)hex.ErrorCode == 0x80004005)
                //{
                //    var url = HttpContext.Current.Request.RawUrl + "";
                //    if (!NotFoundFiles.Contains(url))
                //        NotFoundFiles.Add(url);
                //    else
                //        ex = null;
                //}

                // 拦截没有权限
                if (ex is NoPermissionException nex)
                {
                    ctx.Result = ctx.NoPermission(nex);
                    ctx.ExceptionHandled = true;
                }

                if (ex != null) XTrace.WriteException(ex);
            }
            if (ctx.ExceptionHandled) return;

            // 判断控制器是否在管辖范围之内，不拦截其它控制器的异常信息
            if (/*Setting.Current.CatchAllException ||*/ AreaBaseX.Contains((ControllerActionDescriptor)ctx.ActionDescriptor))
            {
                ctx.ExceptionHandled = true;

                var ctrl = "";
                var act = "";
                if (ctx.RouteData.Values.ContainsKey("controller")) ctrl = ctx.RouteData.Values["controller"] + "";
                if (ctx.RouteData.Values.ContainsKey("action")) act = ctx.RouteData.Values["action"] + "";

                if (ctx.HttpContext.Request.IsAjaxRequest())
                {
                    if (act.IsNullOrEmpty()) act = "操作";
                    ctx.Result = ControllerHelper.JsonTips("[{0}]失败！{1}".F(act, ex.Message));
                }
                else
                {
                    var vr = new ViewResult
                    {
                        ViewName = "CubeError"
                    };

                    vr.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), ctx.ModelState)
                    {
                        ["Context"] = ctx,
                        ["Exception"] = ex,
                        ["Ctrl"] = ctrl,
                        ["Act"] = act
                    };

                    ctx.Result = vr;
                }
            }

            base.OnException(ctx);
        }
    }
}

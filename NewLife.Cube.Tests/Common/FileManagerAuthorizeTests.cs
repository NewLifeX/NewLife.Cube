using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NewLife.Cube;
using NewLife.Cube.Areas.Admin.Controllers;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// 文件管理安全开关测试：EnableFileManager=false（默认）时，FileController 全部接口
/// （浏览/上传/下载/删除等）必须被授权过滤器拦截，防止越权操作服务器文件。
/// </summary>
public class FileManagerAuthorizeTests
{
    private static AuthorizationFilterContext CreateContext()
    {
        var method = typeof(FileController).GetMethod(nameof(FileController.Index));
        Assert.NotNull(method);

        var action = new ControllerActionDescriptor
        {
            ControllerName = "File",
            ActionName = "Index",
            ControllerTypeInfo = typeof(FileController).GetTypeInfo(),
            MethodInfo = method,
        };

        var ctx = new ActionContext(new DefaultHttpContext(), new RouteData(), action);
        return new AuthorizationFilterContext(ctx, new List<IFilterMetadata>());
    }

    /// <summary>默认关闭：FileController 接口返回 403</summary>
    [Fact(DisplayName = "默认关闭文件管理：FileController 接口被拒绝(403)")]
    public void Disabled_Rejects_FileController()
    {
        var prev = CubeSetting.Current.EnableFileManager;
        CubeSetting.Current.EnableFileManager = false;
        try
        {
            var fctx = CreateContext();
            new FileManagerAuthorizeAttribute().OnAuthorization(fctx);

            Assert.NotNull(fctx.Result);
            Assert.Equal(403, fctx.HttpContext.Response.StatusCode);
        }
        finally
        {
            CubeSetting.Current.EnableFileManager = prev;
        }
    }

    /// <summary>开启文件管理后放行，不误伤</summary>
    [Fact(DisplayName = "开启文件管理：FileController 接口放行")]
    public void Enabled_Passes()
    {
        var prev = CubeSetting.Current.EnableFileManager;
        CubeSetting.Current.EnableFileManager = true;
        try
        {
            var fctx = CreateContext();
            new FileManagerAuthorizeAttribute().OnAuthorization(fctx);

            Assert.Null(fctx.Result);
        }
        finally
        {
            CubeSetting.Current.EnableFileManager = prev;
        }
    }
}

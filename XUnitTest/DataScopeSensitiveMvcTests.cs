using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube;
using NewLife.Cube.Areas.Admin.Controllers;
using NewLife.Cube.ViewModels;
using XCode.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>CubeNC（MVC）脱敏出口测试（OSC-2608273d95 · 实现审计 N5）</summary>
/// <remarks>
/// MVC 侧经 &lt;Compile Include&gt; 共用 <c>ReadOnlyEntityControllerScope.cs</c>（GetSensitiveFieldNames / MaskSensitiveFields /
/// MaskSensitiveList / ExportDataMasked），本类在 CubeNC 程序集上下文里验证：
/// ① GetPage 列级 sensitive 标记；② 列表遮蔽用真实用户上下文（不读 DataScopeContext.Current 宿主态）。
/// </remarks>
public class DataScopeSensitiveMvcTests
{
    /// <summary>构造无构造函数依赖的 CubeNC UserController，并注入请求级数据权限上下文</summary>
    private static UserController CreateController(DataScopeContext scope)
    {
        var ctrl = (UserController)RuntimeHelpers.GetUninitializedObject(typeof(UserController));
        var http = new DefaultHttpContext();
        http.Items["DataScopeContext"] = scope;
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };

        return ctrl;
    }

    private static MethodInfo GetBaseMethod(String name) =>
        typeof(ReadOnlyEntityController<User>).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException($"未找到 {name} 方法");

    [Fact(DisplayName = "CubeNC_GetPage字段_无接ViewSensitive时标记sensitive")]
    public void PrepareFieldsForApi_MarksSensitive()
    {
        var ctrl = CreateController(new DataScopeContext { UserId = 2, ViewSensitive = false });
        var fields = new List<DataField>
        {
            new() { Name = "Password", Type = typeof(String) },
            new() { Name = "Name", Type = typeof(String) },
        };

        var rs = (IList<DataField>)GetBaseMethod("PrepareFieldsForApi").Invoke(ctrl, [fields])!;

        Assert.True(rs.First(e => e.Name == "Password").Sensitive);
        Assert.False(rs.First(e => e.Name == "Name").Sensitive);
    }

    [Fact(DisplayName = "CubeNC_GetPage字段_已接ViewSensitive时不标记")]
    public void PrepareFieldsForApi_ViewSensitive_NotMarked()
    {
        var ctrl = CreateController(new DataScopeContext { UserId = 2, ViewSensitive = true });
        var fields = new List<DataField> { new() { Name = "Password", Type = typeof(String) } };

        var rs = (IList<DataField>)GetBaseMethod("PrepareFieldsForApi").Invoke(ctrl, [fields])!;

        Assert.False(rs.First(e => e.Name == "Password").Sensitive);
    }

    [Fact(DisplayName = "CubeNC_列表脱敏_用真实上下文_不用宿主态")]
    public void MaskSensitiveList_UsesRealUserContext()
    {
        var old = DataScopeContext.Current;
        try
        {
            // 宿主态在 Current（模拟中间件注入），真实用户态在请求缓存
            DataScopeContext.Current = new DataScopeContext { UserId = 99, DataScope = DataScopes.全部 };

            var ctrl = CreateController(new DataScopeContext { UserId = 1, DataScope = DataScopes.仅本人 });
            var list = new List<User>
            {
                new() { ID = 1, Password = "self" },
                new() { ID = 2, Password = "other" },
            };

            var rs = (IList<User>)GetBaseMethod("MaskSensitiveList").Invoke(ctrl, [list])!;

            Assert.Equal(2, rs.Count);
            Assert.Equal("self", list[0].Password);
            Assert.Equal("***", list[1].Password);
        }
        finally
        {
            DataScopeContext.Current = old;
        }
    }

    [Fact(DisplayName = "CubeNC_详情脱敏_无接ViewSensitive时他人密码置掩码")]
    public void MaskSensitiveFields_OtherUser_Masked()
    {
        var ctrl = CreateController(new DataScopeContext { UserId = 1, DataScope = DataScopes.仅本人 });
        var user = new User { ID = 2, Password = "other" };

        GetBaseMethod("MaskSensitiveFields").Invoke(ctrl, [user]);

        Assert.Equal("***", user.Password);
    }
}

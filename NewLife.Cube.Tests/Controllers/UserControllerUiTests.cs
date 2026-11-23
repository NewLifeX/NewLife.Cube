using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NewLife.Cube;
using NewLife.Cube.Areas.Admin.Controllers;
using NewLife.Cube.Services;
using Xunit;

namespace NewLife.Cube.Tests.Controllers;

/// <summary>用户页 UI 扩展契约测试</summary>
public class UserControllerUiTests
{
    [Fact(DisplayName = "用户页：meta.ui 下发 6 个关联维度契约")]
    public void OnGetPageUi_ReturnsUserCardRelations()
    {
        // OnGetPageUi 只使用 L.T 与静态常量，不依赖构造注入的服务
        var controller = new UserController(null!, null!, null!, null!);
        var method = typeof(UserController).GetMethod("OnGetPageUi", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var ui = Assert.IsAssignableFrom<IDictionary<String, Object>>(method!.Invoke(controller, null));
        Assert.True(ui.ContainsKey("userCard"));

        var card = ui["userCard"]!;
        var relations = Assert.IsAssignableFrom<IEnumerable<Object>>(
            card.GetType().GetProperty("relations")!.GetValue(card));
        var keys = relations.Select(e => (String)e.GetType().GetProperty("key")!.GetValue(e)!).ToArray();

        Assert.Equal(new[] { "binds", "tokens", "sessions", "logs", "oauth", "notices" }, keys.Take(6));
        // 多租户部署才追加 tenants，单租户下不下发
        Assert.True(keys.Length == 6 || keys[^1] == "tenants");
    }
}

using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Common;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>树形控制器契约测试。验证继承关系与移动端点的路由/权限声明（不依赖数据库）</summary>
public class TreeControllerContractTests
{
    [Fact(DisplayName = "树形控制器：继承标准实体控制器")]
    public void TreeController_InheritsEntityController()
    {
        // API 版：EntityTreeApiController → EntityTreeController → EntityController
        var api = typeof(EntityTreeApiController<,>);
        var tree = api.BaseType;
        Assert.NotNull(tree);
        Assert.Equal("EntityTreeController`2", tree!.Name);
        Assert.Equal("EntityController`2", tree.BaseType!.Name);
    }

    [Fact(DisplayName = "树形 API 控制器：上移/下移为 POST 且要求修改权限")]
    public void TreeApiController_MoveEndpoints_HavePostAndUpdatePermission()
    {
        var type = typeof(EntityTreeApiController<,>);

        foreach (var name in new[] { "Up", "Down" })
        {
            var method = type.GetMethod(name, [typeof(Int32)]);
            Assert.NotNull(method);

            Assert.NotNull(method!.GetCustomAttribute<HttpPostAttribute>());

            var auth = method.GetCustomAttribute<EntityAuthorizeAttribute>();
            Assert.NotNull(auth);
            Assert.Contains(PermissionFlags.Update, auth!.Permissions);
        }
    }
}

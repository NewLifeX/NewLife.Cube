using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>只读实体控制器详情接口路由测试</summary>
public class ReadOnlyEntityControllerRouteTests
{
    [Fact(DisplayName = "详情仅保留 RESTful 风格 {id} 路由")]
    public void Detail_HasOnlyRestfulIdRoute()
    {
        var method = typeof(ReadOnlyEntityController<>).GetMethod(nameof(ReadOnlyEntityController<XCode.Membership.User>.Detail));
        var routes = method!.GetCustomAttributes<HttpGetAttribute>().Select(e => e.Template).ToArray();

        // 只保留 /api/[area]/[controller]/{id}；旧 Action 风格 /Detail?id= 已移除
        Assert.Equal(["/api/[area]/[controller]/{id}"], routes);
    }

    [Fact(DisplayName = "修改只保留路径主键路由，删除同样使用路径主键")]
    public void UpdateAndDelete_HaveOnlyRestfulIdRoute()
    {
        var update = typeof(EntityController<,>).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(e => e.Name == "Update")
            .ToArray();
        Assert.Single(update);
        var putRoutes = update[0].GetCustomAttributes<HttpPutAttribute>().Select(e => e.Template).ToArray();
        Assert.Equal(["/api/[area]/[controller]/{id}"], putRoutes);

        var delete = typeof(EntityController<,>).GetMethod("Delete");
        var delRoutes = delete!.GetCustomAttributes<HttpDeleteAttribute>().Select(e => e.Template).ToArray();
        Assert.Equal(["/api/[area]/[controller]/{id}"], delRoutes);

        // 旧的路径转发动作已并入 Update
        Assert.Null(typeof(EntityController<,>).GetMethod("UpdateById"));
    }

    [Fact(DisplayName = "批量与条件删除共用基路径删除动作")]
    public void DeleteAll_HasBaseRouteAndIdsParameter()
    {
        var deleteAll = typeof(EntityController<,>).GetMethod("DeleteAll");
        var routes = deleteAll!.GetCustomAttributes<HttpDeleteAttribute>().Select(e => e.Template).ToArray();

        // 基路径：DELETE /api/{area}/{controller}?ids= 或 ?<搜索条件>
        Assert.Contains("/api/[area]/[controller]", routes);

        // ids 走查询串，需显式 [FromQuery]；有默认值以兼容"纯条件删除"调用
        var param = deleteAll.GetParameters().Single(e => e.Name == "ids");
        Assert.NotNull(param.GetCustomAttribute<FromQueryAttribute>());
        Assert.True(param.HasDefaultValue);
        Assert.Null(param.DefaultValue);
    }

    [Fact(DisplayName = "旧的删除专用端点已移除")]
    public void LegacyDeleteEndpoints_Removed()
    {
        var routes = typeof(EntityController<,>).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(e => e.GetCustomAttributes<HttpDeleteAttribute>())
            .Select(e => e.Template ?? "")
            .ToArray();

        Assert.DoesNotContain(routes, e => e.Contains("DeleteSelect") || e.Contains("DeleteAll"));
        Assert.Null(typeof(EntityController<,>).GetMethod("DeleteById"));
        Assert.Null(typeof(EntityController<,>).GetMethod("DeleteSelect"));
    }

    [Fact(DisplayName = "批量删除上限与后端常量一致")]
    public void MaxBatchDelete_IsDocumented()
    {
        Assert.Equal(100, EntityController<XCode.Membership.User, XCode.Membership.UserModel>.MaxBatchDelete);
    }
}
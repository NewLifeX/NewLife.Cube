using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>只读实体控制器详情接口路由测试</summary>
public class ReadOnlyEntityControllerRouteTests
{
    [Fact(DisplayName = "详情接口 id 参数带默认值，避免隐式必填拦截查询式调用")]
    public void Detail_IdHasDefaultValue()
    {
        // 回归守护：Detail 同时挂 {id} 模板时，[ApiController] 会把 id 推断为 [FromRoute] 并隐式加 [Required]，
        // 导致前端传统调用 /Detail?id=xxx 报 “The id field is required.”（HTTP 200 + code=-2）。
        // 修复：id 必须给默认值 null，走方法内 Request.Query 兜底；RESTful /{id} 路径不受影响。
        var method = typeof(ReadOnlyEntityController<>).GetMethod(nameof(ReadOnlyEntityController<XCode.Membership.User>.Detail));
        var param = method!.GetParameters().Single(e => e.Name == "id");

        Assert.True(param.HasDefaultValue, "Detail.id 必须带默认值 null，否则 [ApiController] 隐式必填会拦截 /Detail?id=xxx 查询式调用");
        Assert.Null(param.DefaultValue);
    }

    [Fact(DisplayName = "详情动作保留 RESTful 风格 {id} 路由")]
    public void Detail_HasRestfulIdRoute()
    {
        var method = typeof(ReadOnlyEntityController<>).GetMethod(nameof(ReadOnlyEntityController<XCode.Membership.User>.Detail));
        var routes = method!.GetCustomAttributes<HttpGetAttribute>().Select(e => e.Template).ToArray();

        // RESTful：/api/[area]/[controller]/{id}
        Assert.Contains("/api/[area]/[controller]/{id}", routes);
        // 传统 Action 风格：无模板 [HttpGet] 由类级 [Route("api/[area]/[controller]/[action]")] 生成 /Detail 字面量路由
        Assert.Contains((String)null, routes);
    }

    [Fact(DisplayName = "修改与删除同时提供 RESTful 路径主键路由")]
    public void UpdateAndDelete_HaveRestfulIdRoute()
    {
        var update = typeof(EntityController<,>).GetMethod("UpdateById");
        var putRoutes = update!.GetCustomAttributes<HttpPutAttribute>().Select(e => e.Template).ToArray();
        Assert.Contains("/api/[area]/[controller]/{id}", putRoutes);

        var delete = typeof(EntityController<,>).GetMethod("Delete");
        var delRoutes = delete!.GetCustomAttributes<HttpDeleteAttribute>().Select(e => e.Template).ToArray();
        Assert.Contains("/api/[area]/[controller]/{id}", delRoutes);
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
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
}
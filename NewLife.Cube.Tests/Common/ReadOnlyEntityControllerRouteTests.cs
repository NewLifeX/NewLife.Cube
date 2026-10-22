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

    [Fact(DisplayName = "元数据端点使用 meta 路由且 GetFields 已删除")]
    public void Meta_HasRestfulRoute_And_GetFieldsRemoved()
    {
        var ctrl = typeof(ReadOnlyEntityController<>);

        // GetFields 已并入 GetPage(meta)，实体控制器与单对象控制器都不再提供
        Assert.Null(ctrl.GetMethod("GetFields"));
        Assert.Null(typeof(ObjectController<>).GetMethod("GetFields"));

        var meta = ctrl.GetMethod("Meta")!;
        Assert.Equal(["/api/[area]/[controller]/meta"], meta.GetCustomAttributes<HttpGetAttribute>().Select(e => e.Template).ToArray());

        // 元数据只要求登录，不绑定具体权限位
        var auth = meta.GetCustomAttribute<EntityAuthorizeAttribute>()!;
        Assert.All(auth.Permissions, e => Assert.Equal(XCode.Membership.PermissionFlags.None, e));

        var objectMeta = typeof(ObjectController<>).GetMethod("Meta")!;
        Assert.Equal(["/api/[area]/[controller]/meta"], objectMeta.GetCustomAttributes<HttpGetAttribute>().Select(e => e.Template).ToArray());
    }

    [Fact(DisplayName = "图表与导出端点使用 charts/export 路由")]
    public void ChartsAndExport_HaveRestfulRoutes()
    {
        var ctrl = typeof(ReadOnlyEntityController<>);

        var charts = ctrl.GetMethod("Charts")!;
        Assert.Equal(["/api/[area]/[controller]/charts"], charts.GetCustomAttributes<HttpGetAttribute>().Select(e => e.Template).ToArray());

        var export = ctrl.GetMethod("Export")!;
        Assert.Equal(["/api/[area]/[controller]/export"], export.GetCustomAttributes<HttpGetAttribute>().Select(e => e.Template).ToArray());
    }

    [Fact(DisplayName = "导入与上传端点使用 import/attachments 路由且上传需写权限")]
    public void ImportAndAttachments_HaveRestfulRoutes()
    {
        var ctrl = typeof(EntityController<,>);

        var import = ctrl.GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(e => e.Name == "Import");
        Assert.Equal(["/api/[area]/[controller]/import"], import.GetCustomAttributes<HttpPostAttribute>().Select(e => e.Template).ToArray());

        var att = ctrl.GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(e => e.Name == "Attachments");
        Assert.Equal(["/api/[area]/[controller]/attachments"], att.GetCustomAttributes<HttpPostAttribute>().Select(e => e.Template).ToArray());

        // 新增或修改任一权限即可上传
        var auth = att.GetCustomAttribute<EntityAuthorizeAttribute>()!;
        Assert.Contains(XCode.Membership.PermissionFlags.Insert, auth.Permissions);
        Assert.Contains(XCode.Membership.PermissionFlags.Update, auth.Permissions);
    }
}
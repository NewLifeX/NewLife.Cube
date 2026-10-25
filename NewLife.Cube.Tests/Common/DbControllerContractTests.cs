using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using NewLife.Cube;
using NewLife.Cube.Areas.Admin.Controllers;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// 数据库管理页接口契约
///
/// 页面是静态页（表结构/实体结构/模型差异无法用实体元数据表达），后端动作必须显式声明 [Http*] 路由，
/// 否则会被 OpenAPI 生成器静默丢弃、前端点了 404。
/// </summary>
public class DbControllerContractTests
{
    private static String[] GetRoutes<T>(string methodName) where T : Attribute
        => typeof(DbController).GetMethod(methodName)!
            .GetCustomAttributes<T>()
            .Select(e => (e as HttpMethodAttribute)?.Template ?? "")
            .ToArray();

    [Fact(DisplayName = "数据库管理 - 列表与三个诊断端点齐备")]
    public void Diagnostics_HaveRoutes()
    {
        Assert.Contains("/api/[area]/[controller]", GetRoutes<HttpGetAttribute>(nameof(DbController.Index)));
        Assert.Contains("/api/[area]/[controller]/tables", GetRoutes<HttpGetAttribute>(nameof(DbController.ShowTables)));
        Assert.Contains("/api/[area]/[controller]/entities", GetRoutes<HttpGetAttribute>(nameof(DbController.ShowEntities)));
        Assert.Contains("/api/[area]/[controller]/diff", GetRoutes<HttpGetAttribute>(nameof(DbController.ModelDiff)));
    }

    [Fact(DisplayName = "数据库管理 - 压缩走 POST，备份/下载沿用既有端点")]
    public void Maintenance_HaveRoutes()
    {
        Assert.Contains("/api/[area]/[controller]/compact", GetRoutes<HttpPostAttribute>(nameof(DbController.Compact)));
        Assert.NotEmpty(GetRoutes<HttpPostAttribute>(nameof(DbController.Backup)));
        Assert.NotEmpty(GetRoutes<HttpPostAttribute>(nameof(DbController.BackupAndCompress)));
        Assert.NotEmpty(GetRoutes<HttpGetAttribute>(nameof(DbController.Download)));
    }

    [Fact(DisplayName = "数据库管理 - 页面动作都需要登录授权")]
    public void Actions_RequireAuthorize()
    {
        foreach (var name in new[] { nameof(DbController.ShowTables), nameof(DbController.ShowEntities), nameof(DbController.ModelDiff), nameof(DbController.Compact) })
        {
            var method = typeof(DbController).GetMethod(name, BindingFlags.Public | BindingFlags.Instance)!;
            Assert.NotNull(method.GetCustomAttribute<EntityAuthorizeAttribute>());
        }
    }
}

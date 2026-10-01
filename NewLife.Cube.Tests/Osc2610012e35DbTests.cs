using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Areas.Admin.Controllers;
using XCode.DataAccessLayer;
using Xunit;

namespace NewLife.Cube.Tests;

/// <summary>OSC-2610012e35 数据库管理 JSON 接口测试</summary>
public class Osc2610012e35DbTests : IDisposable
{
    private const String ConnName = "Osc2610012e35";
    private readonly String _dbFile = Path.Combine(Path.GetTempPath(), $"CubeDb_{Guid.NewGuid():N}.db");

    public Osc2610012e35DbTests() => DAL.AddConnStr(ConnName, $"Data Source={_dbFile}", null, "SQLite");

    [Theory]
    [InlineData("ShowTables")]
    [InlineData("ShowEntities")]
    [InlineData("ModelDiff")]
    [InlineData("Compact")]
    public void DatabaseTools_InvalidConnection_ReturnNonZeroJson(String action)
    {
        var controller = new DbController();
        var result = Assert.IsType<ContentResult>(action switch
        {
            "ShowTables" => controller.ShowTables("../x"),
            "ShowEntities" => controller.ShowEntities("../x"),
            "ModelDiff" => controller.ModelDiff("../x"),
            "Compact" => controller.Compact("../x"),
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        });

        Assert.StartsWith("application/json", result.ContentType);
        Assert.Contains("\"code\":1", result.Content);
        Assert.DoesNotContain("<html", result.Content!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShowTables_ConfiguredSqlite_ReturnsTablesArray()
    {
        var result = Assert.IsType<ContentResult>(new DbController().ShowTables(ConnName));

        Assert.StartsWith("application/json", result.ContentType);
        Assert.Contains("\"code\":0", result.Content);
        Assert.Contains("\"tables\":[", result.Content);
    }

    [Theory]
    [InlineData(nameof(DbController.ShowTables), "/api/[area]/[controller]/ShowTables")]
    [InlineData(nameof(DbController.ShowEntities), "/api/[area]/[controller]/ShowEntities")]
    [InlineData(nameof(DbController.ModelDiff), "/api/[area]/[controller]/ModelDiff")]
    public void ReadActions_UseExplicitApiRoutes(String action, String route)
    {
        var method = typeof(DbController).GetMethod(action, BindingFlags.Instance | BindingFlags.Public);
        var attribute = Assert.IsType<HttpGetAttribute>(method!.GetCustomAttribute<HttpGetAttribute>());

        Assert.Equal(route, attribute.Template);
    }

    [Fact]
    public void Compact_UsesExplicitApiRoute()
    {
        var method = typeof(DbController).GetMethod(nameof(DbController.Compact), BindingFlags.Instance | BindingFlags.Public);
        var attribute = Assert.IsType<HttpPostAttribute>(method!.GetCustomAttribute<HttpPostAttribute>());

        Assert.Equal("/api/[area]/[controller]/Compact", attribute.Template);
    }

    public void Dispose()
    {
        try { DAL.Create(ConnName).Reset(); } catch { }
        DAL.ConnStrs?.TryRemove(ConnName, out _);
        try { File.Delete(_dbFile); } catch { }
    }
}

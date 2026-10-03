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

    public Osc2610012e35DbTests()
    {
        DAL.AddConnStr(ConnName, $"Data Source={_dbFile}", null, "SQLite");
        // ShowEntityFields 需要按连接名加载实体类，Membership 为 XCode.Membership 固定连接名
        DAL.AddConnStr("Membership", "Data Source=Osc2e35Membership;Mode=Memory;Cache=Shared", null, "SQLite");
    }

    [Theory]
    [InlineData("ShowTables")]
    [InlineData("ShowEntities")]
    [InlineData("ShowEntityFields")]
    [InlineData("ShowTableFields")]
    [InlineData("ModelDiff")]
    [InlineData("Compact")]
    public void DatabaseTools_InvalidConnection_ReturnNonZeroJson(String action)
    {
        var controller = new DbController();
        var result = Assert.IsType<ContentResult>(action switch
        {
            "ShowTables" => controller.ShowTables("../x"),
            "ShowEntities" => controller.ShowEntities("../x"),
            "ShowEntityFields" => controller.ShowEntityFields("../x", "User"),
            "ShowTableFields" => controller.ShowTableFields("../x", "User"),
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

    [Fact]
    public void ShowTables_IncludesDescriptionField()
    {
        var dal = DAL.Create(ConnName);
        dal.Execute("Create Table Sample(Id Integer Primary Key, Name Text)");
        dal.Tables = null;

        var result = Assert.IsType<ContentResult>(new DbController().ShowTables(ConnName));

        Assert.Contains("\"code\":0", result.Content);
        Assert.Contains("Sample", result.Content);
        Assert.Contains("\"description\":", result.Content);
    }

    [Fact]
    public void ShowEntityFields_UnknownEntity_ReturnsNonZero()
    {
        var result = Assert.IsType<ContentResult>(new DbController().ShowEntityFields("Membership", "NoSuchEntity"));

        Assert.Contains("\"code\":1", result.Content);
    }

    [Fact]
    public void ShowEntityFields_MembershipUser_ReturnsFieldSchema()
    {
        var result = Assert.IsType<ContentResult>(new DbController().ShowEntityFields("Membership", "User"));

        Assert.StartsWith("application/json", result.ContentType);
        Assert.Contains("\"code\":0", result.Content);
        Assert.Contains("\"fields\":[", result.Content);
        Assert.Contains("\"name\":\"ID\"", result.Content);
        Assert.Contains("\"key\":\"AI\"", result.Content);
    }

    [Fact]
    public void ShowTableFields_UnknownTable_ReturnsNonZero()
    {
        var result = Assert.IsType<ContentResult>(new DbController().ShowTableFields(ConnName, "NoSuchTable"));

        Assert.Contains("\"code\":1", result.Content);
    }

    [Fact]
    public void ShowTableFields_CreatedTable_ReturnsFieldSchema()
    {
        var dal = DAL.Create(ConnName);
        dal.Execute("Create Table Sample2(Id Integer Primary Key, Name Text, Amount Decimal(18,2))");
        dal.Tables = null;

        var result = Assert.IsType<ContentResult>(new DbController().ShowTableFields(ConnName, "Sample2"));

        Assert.Contains("\"code\":0", result.Content);
        Assert.Contains("\"fields\":[", result.Content);
        Assert.Contains("\"name\":\"Id\"", result.Content);
        Assert.True(result.Content!.Contains("\"key\":\"AI\"") || result.Content.Contains("\"key\":\"PK\""));
    }

    [Theory]
    [InlineData(nameof(DbController.ShowTables), "/api/[area]/[controller]/ShowTables")]
    [InlineData(nameof(DbController.ShowEntities), "/api/[area]/[controller]/ShowEntities")]
    [InlineData(nameof(DbController.ShowEntityFields), "/api/[area]/[controller]/ShowEntityFields")]
    [InlineData(nameof(DbController.ShowTableFields), "/api/[area]/[controller]/ShowTableFields")]
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

using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using E2EApiTest.Helpers;
using Xunit;

namespace E2EApiTest.Tests;

/// <summary>页面元数据 E2E：实体视图自描述、字段控件类型、值集与自定义动作</summary>
public sealed class ApiMetaTests : ApiTestBase
{
    [Fact(DisplayName = "TC-API-META-001 实体 meta 下发 entity/setting/list/form/search")]
    [Trait("Category", "Meta")]
    public async Task TC_API_META_001_StudentMeta()
    {
        var rs = await Admin.GetAsync("/api/School/Student/meta");

        ApiAssert.Ok(rs, "TC-API-META-001");
        Assert.Equal("Student", ApiResult.GetString(rs.Data?["entity"]?["name"]));
        Assert.Equal("Id", ApiResult.GetString(rs.Data?["entity"]?["primaryKey"]));
        Assert.NotNull(rs.Data?["setting"]?["requiredPermissions"]?["add"]);
        Assert.True((rs.Data?["list"] as JsonArray)?.Count > 0, "list 字段为空");
        Assert.True((rs.Data?["form"] as JsonArray)?.Count > 0, "form 字段为空");
        Assert.True((rs.Data?["search"] as JsonArray)?.Count > 0, "search 字段为空");

        var name = (rs.Data?["list"] as JsonArray)!.FirstOrDefault(e => ApiResult.GetString(e?["name"]) == "Name");
        Assert.NotNull(name);
        Assert.False(String.IsNullOrWhiteSpace(ApiResult.GetString(name!["displayName"])));
        Assert.Equal("String", ApiResult.GetString(name["typeName"]));
    }

    [Fact(DisplayName = "TC-API-META-002 主时间字段输出区间查询项")]
    [Trait("Category", "Meta")]
    public async Task TC_API_META_002_SearchRange()
    {
        var rs = await Admin.GetAsync("/api/School/Student/meta");

        ApiAssert.Ok(rs, "TC-API-META-002");
        var search = (rs.Data?["search"] as JsonArray)!;
        var range = search.FirstOrDefault(e => ApiResult.GetString(e?["kind"]) == "range");
        Assert.NotNull(range);
        Assert.Equal("dtStart", ApiResult.GetString(range!["name"]));
        Assert.Equal("dtEnd", ApiResult.GetString(range["end"]));
        Assert.False(String.IsNullOrWhiteSpace(ApiResult.GetString(range["field"])));
    }

    [Fact(DisplayName = "TC-API-META-003 字段控件类型与值集编码完整下发")]
    [Trait("Category", "Meta")]
    public async Task TC_API_META_003_FieldTypes()
    {
        var rs = await Admin.GetAsync("/api/Test/TestField/meta");

        ApiAssert.Ok(rs, "TC-API-META-003");
        var form = (rs.Data?["form"] as JsonArray)!;
        var itemTypes = form.Select(e => ApiResult.GetString(e?["itemType"])).Where(e => !String.IsNullOrEmpty(e)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var expected in new[] { "color", "file", "html", "icon", "image", "json", "lovTable", "lovTableMulti", "mail", "markdown", "mobile", "multipleSelect", "singleSelect", "url" })
            Assert.Contains(expected, itemTypes);

        var lovCodes = form.Where(e => !String.IsNullOrEmpty(ApiResult.GetString(e?["lovCode"])))
                           .Select(e => ApiResult.GetString(e?["lovCode"])!)
                           .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Enum.CubeDemo.Areas.Test.测试枚举", lovCodes);
        Assert.Contains("List.CubeDemo.Role", lovCodes);

        var search = (rs.Data?["search"] as JsonArray)!;
        Assert.Contains(search, e => ApiResult.GetString(e?["itemType"]) == "area3");
    }

    [Fact(DisplayName = "TC-API-META-004 列表型值集支持分页与名称过滤")]
    [Trait("Category", "Meta")]
    public async Task TC_API_META_004_RoleList()
    {
        var rs = await Admin.GetAsync("/api/Test/TestField/RoleList?pageIndex=1&pageSize=5&name=演示");

        ApiAssert.Ok(rs, "TC-API-META-004");
        var rows = rs.Data?["data"] as JsonArray;
        Assert.NotNull(rows);
        Assert.Equal(5, rows!.Count);
        Assert.Equal(21, ApiResult.GetInt32(rs.Data?["total"]));
        Assert.All(rows, e => Assert.Contains("演示", ApiResult.GetString(e?["name"])));
    }

    [Fact(DisplayName = "TC-API-META-005 自定义动作列返回业务数据")]
    [Trait("Category", "Meta")]
    public async Task TC_API_META_005_CustomAction()
    {
        var rs = await Admin.GetAsync("/api/Test/TestField/Touch/42");

        ApiAssert.Ok(rs, "TC-API-META-005");
        Assert.Contains("42", ApiResult.GetString(rs.Data?["message"]));
    }

    [Fact(DisplayName = "TC-API-META-006 meta 响应带 ETag 与私有缓存头")]
    [Trait("Category", "Meta")]
    public async Task TC_API_META_006_CacheHeaders()
    {
        var rs = await Admin.GetAsync("/api/School/Student/meta");

        ApiAssert.Ok(rs, "TC-API-META-006");
        Assert.True(rs.Headers.ContainsKey("ETag"), "缺少 ETag");
        Assert.Contains("no-cache", rs.Headers["Cache-Control"]);
        Assert.Contains("Authorization", rs.Headers["Vary"]);
    }

    [Fact(DisplayName = "TC-API-META-007 未登录访问 meta 返回 401")]
    [Trait("Category", "Meta")]
    public async Task TC_API_META_007_Anonymous()
    {
        using var client = CreateAnonymous();
        var rs = await client.GetAsync("/api/School/Student/meta");

        ApiAssert.HttpStatus(rs, 401, "TC-API-META-007");
        ApiAssert.Fail(rs, 401, "TC-API-META-007");
    }
}

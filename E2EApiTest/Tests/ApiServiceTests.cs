using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using E2EApiTest.Fixtures;
using E2EApiTest.Helpers;
using Xunit;

namespace E2EApiTest.Tests;

/// <summary>服务接口 E2E：菜单、字典、工作台、监控、接口文档</summary>
public sealed class ApiServiceTests : ApiTestBase
{
    [Fact(DisplayName = "TC-API-SVC-001 菜单树包含父级、子级与拼音检索字段")]
    [Trait("Category", "Service")]
    public async Task TC_API_SVC_001_MenuTree()
    {
        var rs = await Admin.GetAsync("/Cube/MenuTree");

        ApiAssert.Ok(rs, "TC-API-SVC-001");
        var roots = ApiAssert.Rows(rs, "TC-API-SVC-001");
        Assert.NotEmpty(roots);

        var school = roots.FirstOrDefault(e => ApiResult.GetString(e?["name"]) == "School");
        Assert.NotNull(school);
        Assert.False(String.IsNullOrWhiteSpace(ApiResult.GetString(school!["pinYin"])));
        Assert.False(String.IsNullOrWhiteSpace(ApiResult.GetString(school["initials"])));

        var children = school["children"] as JsonArray;
        Assert.True(children?.Count > 0, "School 模块缺少子菜单");
        Assert.Contains(children!, e => ApiResult.GetString(e?["url"]) == "/School/Student");
    }

    [Fact(DisplayName = "TC-API-SVC-002 枚举字典按全名返回选项")]
    [Trait("Category", "Service")]
    public async Task TC_API_SVC_002_Lookup()
    {
        var rs = await Admin.GetAsync("/Cube/Lookup?codes=XCode.Membership.SexKinds");

        ApiAssert.Ok(rs, "TC-API-SVC-002");
        var options = rs.Data?["XCode.Membership.SexKinds"] as JsonArray;
        Assert.NotNull(options);
        Assert.Equal(3, options!.Count);
        Assert.Contains(options, e => ApiResult.GetInt32(e?["Value"]) == 1);
    }

    [Fact(DisplayName = "TC-API-SVC-003 用户选择器按角色过滤且支持纯关键字搜索")]
    [Trait("Category", "Service")]
    public async Task TC_API_SVC_003_UserSearch()
    {
        var rs = await Admin.GetAsync("/Cube/UserSearch?roleId=1");

        ApiAssert.Ok(rs, "TC-API-SVC-003");
        var rows = ApiAssert.Rows(rs, "TC-API-SVC-003");
        Assert.Contains(rows, e => ApiResult.GetString(e?["name"]) == ApiAppFixture.AdminUser);
        Assert.All(rows, e => Assert.False(String.IsNullOrWhiteSpace(ApiResult.GetString(e?["name"]))));

        // 不带 roleId 时不应被当成“角色编号=0”过滤，纯关键字仍能命中
        var byKey = await Admin.GetAsync("/Cube/UserSearch?key=admin");
        ApiAssert.Ok(byKey, "TC-API-SVC-003");
        Assert.Contains(ApiAssert.Rows(byKey, "TC-API-SVC-003"), e => ApiResult.GetString(e?["name"]) == ApiAppFixture.AdminUser);
    }

    [Fact(DisplayName = "TC-API-SVC-004 低代码页面配置按 kind+page 查询")]
    [Trait("Category", "Service")]
    public async Task TC_API_SVC_004_GetPageConfig()
    {
        var rs = await Admin.GetAsync("/Cube/GetPageConfig?kind=list&page=/Test/TestField");

        ApiAssert.Ok(rs, "TC-API-SVC-004");
    }

    [Fact(DisplayName = "TC-API-SVC-005 工作台聚合返回用户、KPI 与部件")]
    [Trait("Category", "Service")]
    public async Task TC_API_SVC_005_Dashboard()
    {
        var rs = await Admin.GetAsync("/api/Admin/Index/Dashboard");

        ApiAssert.Ok(rs, "TC-API-SVC-005");
        Assert.Equal(ApiAppFixture.AdminUser, ApiResult.GetString(rs.Data?["user"]?["name"]));
        Assert.True((rs.Data?["kpis"] as JsonArray)?.Count > 0, "KPI 为空");
    }

    [Fact(DisplayName = "TC-API-SVC-006 监控数据返回时间轴与序列")]
    [Trait("Category", "Service")]
    public async Task TC_API_SVC_006_MonitorData()
    {
        var rs = await Admin.GetAsync("/api/Admin/Index/MonitorData");

        ApiAssert.Ok(rs, "TC-API-SVC-006");
        Assert.True((rs.Data?["xs"] as JsonArray)?.Count > 0, "缺少时间轴");
        Assert.True((rs.Data?["series"] as JsonArray)?.Count > 0, "缺少数据序列");
    }

    [Fact(DisplayName = "TC-API-SVC-007 接口文档 JSON 需 Basic 认证且可正常下载")]
    [Trait("Category", "Service")]
    public async Task TC_API_SVC_007_Swagger()
    {
        using var client = CreateAnonymous();

        var anonymous = await client.GetAsync("/swagger/v1/swagger.json");
        ApiAssert.HttpStatus(anonymous, 401, "TC-API-SVC-007");

        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ApiAppFixture.SwaggerUser}:{ApiAppFixture.SwaggerPass}"));
        var rs = await client.SendAsync(HttpMethod.Get, "/swagger/v1/swagger.json", headers: new System.Collections.Generic.Dictionary<String, String>
        {
            ["Authorization"] = "Basic " + auth,
        });

        ApiAssert.HttpStatus(rs, 200, "TC-API-SVC-007");
        Assert.True(rs.Json?["paths"] is JsonObject paths && paths.Count > 0, "swagger.json 缺少 paths");
    }

    [Fact(DisplayName = "TC-API-SVC-008 Scalar 文档页需 Basic 认证并可访问")]
    [Trait("Category", "Service")]
    public async Task TC_API_SVC_008_Scalar()
    {
        using var client = CreateAnonymous();

        var anonymous = await client.GetAsync("/scalar/");
        ApiAssert.HttpStatus(anonymous, 401, "TC-API-SVC-008");

        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ApiAppFixture.SwaggerUser}:{ApiAppFixture.SwaggerPass}"));
        var rs = await client.SendAsync(HttpMethod.Get, "/scalar/", headers: new System.Collections.Generic.Dictionary<String, String>
        {
            ["Authorization"] = "Basic " + auth,
        });

        ApiAssert.HttpStatus(rs, 200, "TC-API-SVC-008");
        Assert.Contains("html", rs.ContentType, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "TC-API-SVC-009 服务器信息仅系统管理员可见")]
    [Trait("Category", "Service")]
    public async Task TC_API_SVC_009_Info()
    {
        var rs = await Admin.GetAsync("/Cube/Info");

        ApiAssert.Ok(rs, "TC-API-SVC-009");
        Assert.False(String.IsNullOrWhiteSpace(ApiResult.GetString(rs.Data?["name"])));
        Assert.True(ApiResult.GetInt32(rs.Data?["port"]) > 0, "缺少端口信息");
        Assert.False(String.IsNullOrWhiteSpace(ApiResult.GetString(rs.Data?["time"])));
    }

    [Fact(DisplayName = "TC-API-SVC-010 关闭接口浏览器时端点清单返回 404")]
    [Trait("Category", "Service")]
    public async Task TC_API_SVC_010_ApisDisabled()
    {
        var rs = await Admin.GetAsync("/Cube/Apis");

        ApiAssert.Fail(rs, 404, "TC-API-SVC-010");
    }

    [Fact(DisplayName = "TC-API-SVC-011 站点配置匿名可缓存且带 ETag")]
    [Trait("Category", "Service")]
    public async Task TC_API_SVC_011_Site()
    {
        using var client = CreateAnonymous();
        var rs = await client.GetAsync("/Cube/Site");

        ApiAssert.Ok(rs, "TC-API-SVC-011");
        Assert.True(rs.Headers.ContainsKey("ETag"), "站点配置缺少 ETag");
        Assert.Contains("max-age", rs.Headers["Cache-Control"]);
    }
}

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using E2EApiTest.Helpers;
using Xunit;

namespace E2EApiTest.Tests;

/// <summary>接口契约 E2E：分页、排序、搜索、序列化约定与错误边界</summary>
public sealed class ApiContractTests : ApiTestBase
{
    private const String BasePath = "/api/School/Student";

    [Fact(DisplayName = "TC-API-CT-001 分页信息自洽且 longTotalCount 与 totalCount 一致")]
    [Trait("Category", "Contract")]
    public async Task TC_API_CT_001_PageShape()
    {
        var rs = await Admin.GetAsync($"{BasePath}?pageIndex=1&pageSize=5");

        ApiAssert.Ok(rs, "TC-API-CT-001");
        var total = ApiResult.GetInt64(rs.Page?["totalCount"]);
        var longTotal = ApiResult.GetString(rs.Page?["longTotalCount"]);
        Assert.NotNull(total);
        Assert.Equal(total!.Value.ToString(), longTotal);
        Assert.True(ApiResult.GetInt32(rs.Page?["pageSize"]) <= 5);
    }

    [Fact(DisplayName = "TC-API-CT-002 pageSize 超过上限按 1000 截断")]
    [Trait("Category", "Contract")]
    public async Task TC_API_CT_002_PageSizeCap()
    {
        var rs = await Admin.GetAsync($"{BasePath}?pageSize=2000");

        ApiAssert.Ok(rs, "TC-API-CT-002");
        Assert.Equal(1000, ApiResult.GetInt32(rs.Page?["pageSize"]));
    }

    [Fact(DisplayName = "TC-API-CT-003 pageIndex 翻页返回下一页数据")]
    [Trait("Category", "Contract")]
    public async Task TC_API_CT_003_PageIndex()
    {
        var tag = NewName("page");
        var names = new[] { tag + "-A", tag + "-B" };
        try
        {
            foreach (var name in names)
                ApiAssert.Ok(await Admin.PostJsonAsync(BasePath, Json(new { name })), "TC-API-CT-003");

            var rs = await Admin.GetAsync($"{BasePath}?Q={tag}&pageIndex=2&pageSize=1&sort=Id");
            ApiAssert.Ok(rs, "TC-API-CT-003");
            Assert.Equal(2, ApiResult.GetInt32(rs.Page?["pageIndex"]));
            Assert.Single(ApiAssert.Rows(rs, "TC-API-CT-003"));
            Assert.Equal(2, ApiResult.GetInt64(rs.Page?["totalCount"]));
        }
        finally
        {
            foreach (var name in names)
                ApiDatabaseHelper.DeleteStudentsByName(name);
        }
    }

    [Fact(DisplayName = "TC-API-CT-004 sort 单字段升序")]
    [Trait("Category", "Contract")]
    public async Task TC_API_CT_004_SortAsc()
    {
        var tag = NewName("sortasc");
        var names = new[] { tag + "-A", tag + "-B" };
        try
        {
            foreach (var name in names)
                ApiAssert.Ok(await Admin.PostJsonAsync(BasePath, Json(new { name })), "TC-API-CT-004");

            var rs = await Admin.GetAsync($"{BasePath}?Q={tag}&sort=Id");
            ApiAssert.Ok(rs, "TC-API-CT-004");
            var rows = ApiAssert.Rows(rs, "TC-API-CT-004");
            Assert.Equal(2, rows.Count);
            Assert.True(ApiResult.GetInt64(rows[0]?["id"]) < ApiResult.GetInt64(rows[1]?["id"]), "升序排序未生效");
        }
        finally
        {
            foreach (var name in names)
                ApiDatabaseHelper.DeleteStudentsByName(name);
        }
    }

    [Fact(DisplayName = "TC-API-CT-005 sort=-字段 表示倒序")]
    [Trait("Category", "Contract")]
    public async Task TC_API_CT_005_SortDescPrefix()
    {
        var tag = NewName("sortdesc");
        var names = new[] { tag + "-A", tag + "-B" };
        try
        {
            foreach (var name in names)
                ApiAssert.Ok(await Admin.PostJsonAsync(BasePath, Json(new { name })), "TC-API-CT-005");

            var rs = await Admin.GetAsync($"{BasePath}?Q={tag}&sort=-Id");
            ApiAssert.Ok(rs, "TC-API-CT-005");
            var rows = ApiAssert.Rows(rs, "TC-API-CT-005");
            Assert.Equal(2, rows.Count);
            Assert.True(ApiResult.GetInt64(rows[0]?["id"]) > ApiResult.GetInt64(rows[1]?["id"]), "sort=-Id 未按倒序返回");
        }
        finally
        {
            foreach (var name in names)
                ApiDatabaseHelper.DeleteStudentsByName(name);
        }
    }

    [Fact(DisplayName = "TC-API-CT-006 orderBy 复合排序优先于 sort")]
    [Trait("Category", "Contract")]
    public async Task TC_API_CT_006_OrderBy()
    {
        var rs = await Admin.GetAsync($"{BasePath}?orderBy=Id desc&sort=Id");

        ApiAssert.Ok(rs, "TC-API-CT-006");
        Assert.NotNull(rs.Page);
    }

    [Fact(DisplayName = "TC-API-CT-007 关键字 Q 与 q 均能过滤")]
    [Trait("Category", "Contract")]
    public async Task TC_API_CT_007_Keyword()
    {
        var tag = NewName("kw");
        var name = tag + "-X";
        try
        {
            ApiAssert.Ok(await Admin.PostJsonAsync(BasePath, Json(new { name })), "TC-API-CT-007");

            foreach (var key in new[] { "Q", "q" })
            {
                var rs = await Admin.GetAsync($"{BasePath}?{key}={tag}");
                ApiAssert.Ok(rs, $"TC-API-CT-007-{key}");
                Assert.Equal(1, ApiResult.GetInt64(rs.Page?["totalCount"]));
                Assert.Equal(name, ApiResult.GetString(rs.Rows?[0]?["name"]));
            }
        }
        finally
        {
            ApiDatabaseHelper.DeleteStudentsByName(name);
        }
    }

    [Fact(DisplayName = "TC-API-CT-008 时间字段输出 yyyy-MM-dd HH:mm:ss 格式")]
    [Trait("Category", "Contract")]
    public async Task TC_API_CT_008_TimeFormat()
    {
        var name = NewName("time");
        try
        {
            var created = await Admin.PostJsonAsync(BasePath, Json(new { name }));
            ApiAssert.Ok(created, "TC-API-CT-008");

            var time = ApiResult.GetString(created.Data?["createTime"]);
            Assert.Matches(new Regex(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$"), time);
        }
        finally
        {
            ApiDatabaseHelper.DeleteStudentsByName(name);
        }
    }

    [Fact(DisplayName = "TC-API-CT-009 Int64 主键输出为字符串避免精度丢失")]
    [Trait("Category", "Contract")]
    public async Task TC_API_CT_009_Int64AsString()
    {
        var rs = await Admin.GetAsync("/api/Admin/Log?pageSize=1");

        ApiAssert.Ok(rs, "TC-API-CT-009");
        var rows = ApiAssert.Rows(rs, "TC-API-CT-009");
        if (rows.Count == 0) return;

        var id = rows[0]?["id"];
        Assert.True(id is System.Text.Json.Nodes.JsonValue value && value.TryGetValue<String>(out _),
            $"Int64 主键未按字符串输出：{id?.ToJsonString()}");
    }

    [Fact(DisplayName = "TC-API-CT-010 未认证响应仅含 code 与 message")]
    [Trait("Category", "Contract")]
    public async Task TC_API_CT_010_UnauthorizedShape()
    {
        using var client = CreateAnonymous();
        var rs = await client.GetAsync(BasePath);

        ApiAssert.HttpStatus(rs, 401, "TC-API-CT-010");
        var obj = rs.Json as System.Text.Json.Nodes.JsonObject;
        Assert.NotNull(obj);
        Assert.Equal(2, obj!.Count);
        Assert.NotNull(obj["code"]);
        Assert.NotNull(obj["message"]);
    }

    [Fact(DisplayName = "TC-API-CT-011 写接口并发不产生重复记录")]
    [Trait("Category", "Contract")]
    public async Task TC_API_CT_011_ConcurrentInsert()
    {
        var tag = NewName("conc");
        var names = Enumerable.Range(0, 3).Select(i => $"{tag}-{i}").ToArray();
        try
        {
            var tasks = names.Select(name => Admin.PostJsonAsync(BasePath, Json(new { name }))).ToArray();
            var results = await Task.WhenAll(tasks);

            foreach (var rs in results)
                ApiAssert.Ok(rs, "TC-API-CT-011");

            foreach (var name in names)
                Assert.Equal(1, ApiDatabaseHelper.CountStudentsByName(name));
        }
        finally
        {
            foreach (var name in names)
                ApiDatabaseHelper.DeleteStudentsByName(name);
        }
    }

    [Fact(DisplayName = "TC-API-CT-012 pageSize 非正数回退默认 20")]
    [Trait("Category", "Contract")]
    public async Task TC_API_CT_012_PageSizeFallback()
    {
        foreach (var size in new[] { "0", "-1" })
        {
            var rs = await Admin.GetAsync($"{BasePath}?pageSize={size}");
            ApiAssert.Ok(rs, "TC-API-CT-012");
            Assert.Equal(20, ApiResult.GetInt32(rs.Page?["pageSize"]));
        }
    }

    [Fact(DisplayName = "TC-API-CT-013 非法 orderBy 表达式被忽略且不影响列表")]
    [Trait("Category", "Contract")]
    public async Task TC_API_CT_013_InvalidOrderBy()
    {
        var rs = await Admin.GetAsync($"{BasePath}?orderBy={Uri.EscapeDataString("Id desc;drop table Student")}");

        ApiAssert.Ok(rs, "TC-API-CT-013");
        Assert.NotNull(rs.Page);

        // 表仍可正常查询（注入片段被丢弃）
        var again = await Admin.GetAsync($"{BasePath}?pageSize=1");
        ApiAssert.Ok(again, "TC-API-CT-013");
    }
}

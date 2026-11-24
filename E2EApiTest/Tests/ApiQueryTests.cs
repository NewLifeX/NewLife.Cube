using System;
using System.Linq;
using System.Threading.Tasks;
using E2EApiTest.Helpers;
using Xunit;

namespace E2EApiTest.Tests;

/// <summary>实体列表查询语义 E2E：等值过滤、关键字搜索、排序、分页边界与时间区间</summary>
public sealed class ApiQueryTests : ApiTestBase
{
    private const String BasePath = "/api/School/Student";

    [Fact(DisplayName = "TC-API-QRY-001 按字段等值过滤只返回匹配行")]
    [Trait("Category", "Query")]
    public async Task TC_API_QRY_001_FieldFilter()
    {
        var nameA = NewName("qa");
        var nameB = NewName("qb");
        try
        {
            await CreateAsync(nameA);
            await CreateAsync(nameB);

            var rs = await Admin.GetAsync($"{BasePath}?name={Uri.EscapeDataString(nameA)}");
            ApiAssert.Ok(rs, "TC-API-QRY-001");
            Assert.NotNull(rs.Rows);
            Assert.True(rs.Rows!.Count >= 1, "等值过滤未命中新建记录");
            Assert.All(rs.Rows, row => Assert.Equal(nameA, ApiResult.GetString(row?["name"])));
        }
        finally
        {
            ApiDatabaseHelper.DeleteStudentsByName(nameA);
            ApiDatabaseHelper.DeleteStudentsByName(nameB);
        }
    }

    [Fact(DisplayName = "TC-API-QRY-002 关键字搜索命中目标记录")]
    [Trait("Category", "Query")]
    public async Task TC_API_QRY_002_KeywordSearch()
    {
        var name = NewName("qk");
        try
        {
            await CreateAsync(name);

            var rs = await Admin.GetAsync($"{BasePath}?Q={Uri.EscapeDataString(name)}");
            ApiAssert.Ok(rs, "TC-API-QRY-002");
            Assert.Contains(name, rs.Rows?.ToJsonString() ?? "");
        }
        finally
        {
            ApiDatabaseHelper.DeleteStudentsByName(name);
        }
    }

    [Fact(DisplayName = "TC-API-QRY-003 分页越界返回空数据但总数正确")]
    [Trait("Category", "Query")]
    public async Task TC_API_QRY_003_PagingBounds()
    {
        var firstName = NewName("qp1");
        var secondName = NewName("qp2");
        try
        {
            await CreateAsync(firstName);
            await CreateAsync(secondName);

            var first = await Admin.GetAsync($"{BasePath}?pageIndex=1&pageSize=1");
            ApiAssert.Ok(first, "TC-API-QRY-003 first page");
            Assert.Single(first.Rows!);
            var total = ApiResult.GetInt32(first.Page?["totalCount"]);
            Assert.True(total >= 2, $"总数应至少包含新建的两条，实际 {total}");

            var far = await Admin.GetAsync($"{BasePath}?pageIndex=9999&pageSize=1");
            ApiAssert.Ok(far, "TC-API-QRY-003 far page");
            Assert.Empty(far.Rows!);
            Assert.Equal(total, ApiResult.GetInt32(far.Page?["totalCount"]));
        }
        finally
        {
            ApiDatabaseHelper.DeleteStudentsByName(firstName);
            ApiDatabaseHelper.DeleteStudentsByName(secondName);
        }
    }

    [Fact(DisplayName = "TC-API-QRY-004 正序与倒序排序首行相反")]
    [Trait("Category", "Query")]
    public async Task TC_API_QRY_004_Sort()
    {
        var firstName = NewName("qs1");
        var secondName = NewName("qs2");
        try
        {
            await CreateAsync(firstName);
            await CreateAsync(secondName);

            var asc = await Admin.GetAsync($"{BasePath}?sort=name&pageIndex=1&pageSize=1");
            ApiAssert.Ok(asc, "TC-API-QRY-004 asc");
            var desc = await Admin.GetAsync($"{BasePath}?sort=-name&pageIndex=1&pageSize=1");
            ApiAssert.Ok(desc, "TC-API-QRY-004 desc");

            var ascName = ApiResult.GetString(asc.Rows?[0]?["name"]);
            var descName = ApiResult.GetString(desc.Rows?[0]?["name"]);
            Assert.False(String.IsNullOrEmpty(ascName));
            Assert.False(String.IsNullOrEmpty(descName));
            Assert.True(String.CompareOrdinal(ascName, descName) <= 0, $"正序首行应不大于倒序首行：{ascName} / {descName}");
        }
        finally
        {
            ApiDatabaseHelper.DeleteStudentsByName(firstName);
            ApiDatabaseHelper.DeleteStudentsByName(secondName);
        }
    }

    [Fact(DisplayName = "TC-API-QRY-005 时间区间过滤排除区间外数据")]
    [Trait("Category", "Query")]
    public async Task TC_API_QRY_005_TimeRange()
    {
        var name = NewName("qt");
        try
        {
            await CreateAsync(name);

            var future = DateTime.Now.AddDays(2).ToString("yyyy-MM-dd HH:mm:ss");
            var none = await Admin.GetAsync($"{BasePath}?dtStart={Uri.EscapeDataString(future)}");
            ApiAssert.Ok(none, "TC-API-QRY-005 future");
            Assert.DoesNotContain(name, none.Rows?.ToJsonString() ?? "");

            var past = DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd HH:mm:ss");
            var hit = await Admin.GetAsync($"{BasePath}?dtStart={Uri.EscapeDataString(past)}");
            ApiAssert.Ok(hit, "TC-API-QRY-005 past");
            Assert.Contains(name, hit.Rows?.ToJsonString() ?? "");
        }
        finally
        {
            ApiDatabaseHelper.DeleteStudentsByName(name);
        }
    }

    [Fact(DisplayName = "TC-API-QRY-006 未知查询参数被忽略且不影响结果")]
    [Trait("Category", "Query")]
    public async Task TC_API_QRY_006_UnknownParam()
    {
        var name = NewName("qu");
        try
        {
            await CreateAsync(name);

            var rs = await Admin.GetAsync($"{BasePath}?notAField=1&name={Uri.EscapeDataString(name)}&pageSize=1");

            ApiAssert.Ok(rs, "TC-API-QRY-006");
            Assert.Single(rs.Rows!);
            Assert.Equal(name, ApiResult.GetString(rs.Rows?[0]?["name"]));
        }
        finally
        {
            ApiDatabaseHelper.DeleteStudentsByName(name);
        }
    }

    private async Task<Int32> CreateAsync(String name)
    {
        var rs = await Admin.PostJsonAsync(BasePath, Json(new { name }));
        ApiAssert.Ok(rs, "创建测试数据 " + name);
        var id = ApiResult.GetInt32(rs.Data?["id"]) ?? 0;
        Assert.True(id > 0, $"新增未返回主键：{rs.Content}");
        return id;
    }
}

using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using E2EApiTest.Helpers;
using Xunit;

namespace E2EApiTest.Tests;

/// <summary>站内信接口 E2E：未读数、最近、分页、增量、偏好与已读/删除边界</summary>
public sealed class ApiNotifyTests : ApiTestBase
{
    [Fact(DisplayName = "TC-API-NOTI-001 未读数、最近与增量接口返回统一信封")]
    [Trait("Category", "Notify")]
    public async Task TC_API_NOTI_001_CountRecentIncremental()
    {
        var count = await Admin.GetAsync("/Notify/Count");
        ApiAssert.Ok(count, "TC-API-NOTI-001");
        Assert.NotNull(ApiResult.GetInt32(count.Data?["count"]));

        var recent = await Admin.GetAsync("/Notify/Recent?size=5");
        ApiAssert.Ok(recent, "TC-API-NOTI-001");
        Assert.NotNull(recent.Data as JsonArray);

        var incremental = await Admin.GetAsync("/Notify/Incremental?lastId=0&size=5");
        ApiAssert.Ok(incremental, "TC-API-NOTI-001");
        Assert.NotNull(incremental.Data as JsonArray);
    }

    [Fact(DisplayName = "TC-API-NOTI-002 分页列表接收查询串参数（回归 415）")]
    [Trait("Category", "Notify")]
    public async Task TC_API_NOTI_002_List()
    {
        var rs = await Admin.GetAsync("/Notify/List?pageIndex=1&pageSize=5");

        ApiAssert.Ok(rs, "TC-API-NOTI-002");
        Assert.Equal(1, ApiResult.GetInt32(rs.Data?["pageIndex"]));
        Assert.Equal(5, ApiResult.GetInt32(rs.Data?["pageSize"]));
        Assert.NotNull(ApiResult.GetInt64(rs.Data?["totalCount"]));
        Assert.NotNull(rs.Data?["items"] as JsonArray);
    }

    [Fact(DisplayName = "TC-API-NOTI-003 通知偏好返回分类与渠道")]
    [Trait("Category", "Notify")]
    public async Task TC_API_NOTI_003_Preferences()
    {
        var rs = await Admin.GetAsync("/Notify/Preferences");

        ApiAssert.Ok(rs, "TC-API-NOTI-003");
        var rows = ApiAssert.Rows(rs, "TC-API-NOTI-003");
        Assert.NotEmpty(rows);
        Assert.All(rows, e =>
        {
            Assert.False(String.IsNullOrWhiteSpace(ApiResult.GetString(e?["category"])));
            Assert.NotNull(e?["channels"] as JsonArray);
        });
    }

    [Fact(DisplayName = "TC-API-NOTI-004 已读/删除不存在的通知返回 404")]
    [Trait("Category", "Notify")]
    public async Task TC_API_NOTI_004_Missing()
    {
        var read = await Admin.PostJsonAsync("/Notify/Read", Json(new { id = 999999999L }));
        ApiAssert.Fail(read, 404, "TC-API-NOTI-004");

        var del = await Admin.PostJsonAsync("/Notify/Delete", Json(new { id = 999999999L }));
        ApiAssert.Fail(del, 404, "TC-API-NOTI-004");
    }

    [Fact(DisplayName = "TC-API-NOTI-005 匿名访问站内信接口返回 401")]
    [Trait("Category", "Notify")]
    public async Task TC_API_NOTI_005_Anonymous()
    {
        using var client = CreateAnonymous();

        var count = await client.GetAsync("/Notify/Count");
        Assert.True(count.Code == 401, $"[TC-API-NOTI-005] 期望 code=401，实际 {count.Code}。响应：{count.Content}");
        Assert.True(count.StatusCode is 200 or 401, $"[TC-API-NOTI-005] 意外 HTTP 状态码 {count.StatusCode}");

        var list = await client.GetAsync("/Notify/List?pageSize=5");
        Assert.True(list.Code == 401, $"[TC-API-NOTI-005] 期望 code=401，实际 {list.Code}。响应：{list.Content}");
    }
}

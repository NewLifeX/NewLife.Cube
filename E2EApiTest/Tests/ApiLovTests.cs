using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using E2EApiTest.Helpers;
using Xunit;

namespace E2EApiTest.Tests;

/// <summary>值集（LOV）手工定义管理 E2E：增删改查、编码规则、主键一致性</summary>
public sealed class ApiLovTests : ApiTestBase
{
    [Fact(DisplayName = "TC-API-LOV-001 手工值集定义可新增、查询、改名与删除")]
    [Trait("Category", "Lov")]
    public async Task TC_API_LOV_001_Crud()
    {
        var code = $"List.E2E{Random.Shared.Next(100000, 999999)}";
        try
        {
            var insert = await Admin.PostJsonAsync("/api/Admin/Lov", Json(new { lovCode = code, name = "E2E值集", type = "LIST", remark = "e2e" }));
            ApiAssert.Ok(insert, "TC-API-LOV-001 insert");
            Assert.Equal(code, ApiResult.GetString(insert.Data?["id"]));

            var list = await Admin.GetAsync($"/api/Admin/Lov?Q={code}");
            ApiAssert.Ok(list, "TC-API-LOV-001 list");
            Assert.Contains(code, list.Data?.ToJsonString() ?? "");

            var detail = await Admin.GetAsync($"/api/Admin/Lov/{code}");
            ApiAssert.Ok(detail, "TC-API-LOV-001 detail");
            Assert.Equal("E2E值集", ApiResult.GetString(detail.Data?["name"]));

            var update = await Admin.PutJsonAsync($"/api/Admin/Lov/{code}", Json(new { lovCode = code, name = "E2E改名", type = "LIST" }));
            ApiAssert.Ok(update, "TC-API-LOV-001 update");

            var renamed = await Admin.GetAsync($"/api/Admin/Lov/{code}");
            Assert.Equal("E2E改名", ApiResult.GetString(renamed.Data?["name"]));

            var delete = await Admin.DeleteAsync($"/api/Admin/Lov/{code}");
            ApiAssert.Ok(delete, "TC-API-LOV-001 delete");

            var gone = await Admin.GetAsync($"/api/Admin/Lov/{code}");
            Assert.NotEqual(0, gone.Code);
            Assert.False(String.IsNullOrWhiteSpace(gone.Message), "删除后再查应提示不存在");
        }
        finally
        {
            await Admin.DeleteAsync($"/api/Admin/Lov/{code}");
        }
    }

    [Fact(DisplayName = "TC-API-LOV-002 编码前缀与类型不匹配、重复编码、主键不一致均被拒绝")]
    [Trait("Category", "Lov")]
    public async Task TC_API_LOV_002_Validation()
    {
        var wrongPrefix = await Admin.PostJsonAsync("/api/Admin/Lov", Json(new { lovCode = $"Enum.E2E{Random.Shared.Next(1000, 9999)}", name = "前缀错", type = "LIST" }));
        Assert.NotEqual(0, wrongPrefix.Code);
        Assert.False(String.IsNullOrWhiteSpace(wrongPrefix.Message), "前缀不匹配应有提示");

        var badType = await Admin.PostJsonAsync("/api/Admin/Lov", Json(new { lovCode = $"List.E2EBad{Random.Shared.Next(1000, 9999)}", name = "类型错", type = "SQL" }));
        Assert.NotEqual(0, badType.Code);

        var code = $"List.E2EDup{Random.Shared.Next(100000, 999999)}";
        try
        {
            var first = await Admin.PostJsonAsync("/api/Admin/Lov", Json(new { lovCode = code, name = "首个", type = "LIST" }));
            ApiAssert.Ok(first, "TC-API-LOV-002 insert");

            var dup = await Admin.PostJsonAsync("/api/Admin/Lov", Json(new { lovCode = code, name = "重复", type = "LIST" }));
            Assert.NotEqual(0, dup.Code);
            Assert.False(String.IsNullOrWhiteSpace(dup.Message), "重复编码应有提示");

            var mismatch = await Admin.PutJsonAsync($"/api/Admin/Lov/{code}", Json(new { id = "List.Other", lovCode = code, name = "主键不一致", type = "LIST" }));
            ApiAssert.Fail(mismatch, -2, "TC-API-LOV-002 mismatch");
        }
        finally
        {
            await Admin.DeleteAsync($"/api/Admin/Lov/{code}");
        }

        var empty = await Admin.DeleteAsync("/api/Admin/Lov");
        Assert.NotEqual(0, empty.Code);
        Assert.False(String.IsNullOrWhiteSpace(empty.Message), "未指定编码时应有提示");
    }
}

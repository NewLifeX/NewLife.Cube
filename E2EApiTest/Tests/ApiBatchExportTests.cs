using System;
using System.Threading.Tasks;
using E2EApiTest.Helpers;
using Xunit;

namespace E2EApiTest.Tests;

/// <summary>批量启用/禁用与多格式导出 E2E</summary>
public sealed class ApiBatchExportTests : ApiTestBase
{
    private const String BasePath = "/api/School/Student";

    [Fact(DisplayName = "TC-API-BAT-001 批量禁用与启用按主键生效并落库")]
    [Trait("Category", "Batch")]
    public async Task TC_API_BAT_001_EnableDisable()
    {
        var firstName = NewName("bt1");
        var secondName = NewName("bt2");
        try
        {
            var firstId = await CreateAsync(firstName);
            var secondId = await CreateAsync(secondName);

            var disable = await Admin.PostJsonAsync($"{BasePath}/disableSelect?keys={firstId},{secondId}", "{}");
            ApiAssert.Ok(disable, "TC-API-BAT-001 disable");
            Assert.Equal(2, ApiResult.GetInt32(disable.Data));
            Assert.Equal(0, Convert.ToInt32(ApiDatabaseHelper.GetStudentField(firstName, "Enable") ?? 0));
            Assert.Equal(0, Convert.ToInt32(ApiDatabaseHelper.GetStudentField(secondName, "Enable") ?? 0));

            var enable = await Admin.PostJsonAsync($"{BasePath}/enableSelect?keys={firstId},{secondId}", "{}");
            ApiAssert.Ok(enable, "TC-API-BAT-001 enable");
            Assert.Equal(2, ApiResult.GetInt32(enable.Data));
            Assert.Equal(1, Convert.ToInt32(ApiDatabaseHelper.GetStudentField(firstName, "Enable") ?? 0));
        }
        finally
        {
            ApiDatabaseHelper.DeleteStudentsByName(firstName);
            ApiDatabaseHelper.DeleteStudentsByName(secondName);
        }
    }

    [Fact(DisplayName = "TC-API-BAT-002 未指定主键时批量操作给出参数错误")]
    [Trait("Category", "Batch")]
    public async Task TC_API_BAT_002_NoKeys()
    {
        var rs = await Admin.PostJsonAsync($"{BasePath}/disableSelect", "{}");

        ApiAssert.Fail(rs, -2, "TC-API-BAT-002");
        Assert.False(String.IsNullOrWhiteSpace(rs.Message), "未选择数据时应给出提示");
    }

    [Fact(DisplayName = "TC-API-EXP-001 Excel 导出返回 xlsx 文件流")]
    [Trait("Category", "Export")]
    public async Task TC_API_EXP_001_Excel()
    {
        var rs = await Admin.GetAsync($"{BasePath}/export?format=excel");

        Assert.Equal(200, rs.StatusCode);
        Assert.StartsWith("application/vnd.openxmlformats-officedocument", rs.ContentType ?? "");
        Assert.StartsWith("PK", rs.Content);
        Assert.DoesNotContain("\"code\"", rs.Content);
    }

    [Fact(DisplayName = "TC-API-EXP-002 XML 导出返回 XML 文本且不支持的格式被拒绝")]
    [Trait("Category", "Export")]
    public async Task TC_API_EXP_002_XmlAndUnsupported()
    {
        var xml = await Admin.GetAsync($"{BasePath}/export?format=xml");
        Assert.Equal(200, xml.StatusCode);
        Assert.Contains("<", xml.Content);

        var bad = await Admin.GetAsync($"{BasePath}/export?format=unknown");
        Assert.NotEqual(0, bad.Code);
        Assert.False(String.IsNullOrWhiteSpace(bad.Message), "不支持的格式应给出提示");
    }

    [Fact(DisplayName = "TC-API-EXP-003 导出文件不包含只写凭据字段")]
    [Trait("Category", "Export")]
    public async Task TC_API_EXP_003_NoCredentialInExport()
    {
        var name = NewName("exp");
        var id = 0;
        try
        {
            var create = await Admin.PostJsonAsync("/api/Admin/OAuthConfig", Json(new { provider = "Sso", name, secret = "ExportS3cr3t", appId = "appid-x", enable = false }));
            ApiAssert.Ok(create, "TC-API-EXP-003 create");
            id = ApiResult.GetInt32(create.Data?["id"]) ?? 0;

            var csv = await Admin.GetAsync($"/api/Admin/OAuthConfig/export?format=csv&name={Uri.EscapeDataString(name)}");
            Assert.Equal(200, csv.StatusCode);
            Assert.DoesNotContain("ExportS3cr3t", csv.Content);
        }
        finally
        {
            if (id > 0) await Admin.DeleteAsync($"/api/Admin/OAuthConfig/{id}");
        }
    }

    private async Task<Int32> CreateAsync(String name)
    {
        var rs = await Admin.PostJsonAsync(BasePath, Json(new { name, enable = true }));
        ApiAssert.Ok(rs, "创建测试数据 " + name);
        return ApiResult.GetInt32(rs.Data?["id"]) ?? 0;
    }
}

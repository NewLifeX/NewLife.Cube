using System;
using System.Text;
using System.Threading.Tasks;
using E2EApiTest.Helpers;
using Xunit;

namespace E2EApiTest.Tests;

/// <summary>数据导入容错 E2E：文件类型、解析失败、空文件与非法数值</summary>
public sealed class ApiImportTests : ApiTestBase
{
    private const String ImportPath = "/api/School/Student/import";

    [Fact(DisplayName = "TC-API-IMP-001 未上传文件与不支持的扩展名返回参数错误")]
    [Trait("Category", "Import")]
    public async Task TC_API_IMP_001_NoFileAndBadExt()
    {
        var none = await Admin.PostMultipartAsync(ImportPath, "file", "empty.json", [], "application/json");
        ApiAssert.Fail(none, -2, "TC-API-IMP-001 none");
        Assert.NotNull(none.FieldErrors);

        var badExt = await Admin.PostMultipartAsync(ImportPath, "file", "note.txt", Encoding.UTF8.GetBytes("hello"), "text/plain");
        ApiAssert.Fail(badExt, -2, "TC-API-IMP-001 bad ext");
        Assert.False(String.IsNullOrWhiteSpace(badExt.Message), "应提示文件类型不支持");
    }

    [Fact(DisplayName = "TC-API-IMP-002 内容解析失败返回参数错误且不暴露解析细节")]
    [Trait("Category", "Import")]
    public async Task TC_API_IMP_002_BadJson()
    {
        var rs = await Admin.PostMultipartAsync(ImportPath, "file", "bad.json", Encoding.UTF8.GetBytes("{ oops "), "application/json");

        ApiAssert.Fail(rs, -2, "TC-API-IMP-002");
        Assert.False(String.IsNullOrWhiteSpace(rs.Message), "应提示解析失败");
        Assert.DoesNotContain("解析字符串", rs.Message ?? "");
    }

    [Fact(DisplayName = "TC-API-IMP-003 空 CSV 视为成功导入 0 行")]
    [Trait("Category", "Import")]
    public async Task TC_API_IMP_003_EmptyCsv()
    {
        var rs = await Admin.PostMultipartAsync(ImportPath, "file", "empty.csv", Encoding.UTF8.GetBytes("Name,Age\n"), "text/csv");

        ApiAssert.Ok(rs, "TC-API-IMP-003");
        Assert.Contains("0", rs.Message ?? "");
    }

    [Fact(DisplayName = "TC-API-IMP-004 CSV 含非法数值时按容错语义落库并给出提示")]
    [Trait("Category", "Import")]
    public async Task TC_API_IMP_004_BadValue()
    {
        var name = "E2E-badcsv-" + Random.Shared.Next(1000, 9999);
        try
        {
            var csv = Encoding.UTF8.GetBytes($"Name,Age\n{name},not-a-number\n");
            var rs = await Admin.PostMultipartAsync(ImportPath, "file", "bad.csv", csv, "text/csv");

            ApiAssert.Ok(rs, "TC-API-IMP-004");
            Assert.Equal(1, ApiDatabaseHelper.CountStudentsByName(name));
        }
        finally
        {
            ApiDatabaseHelper.DeleteStudentsByName(name);
        }
    }
}

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using E2EApiTest.Helpers;
using Xunit;

namespace E2EApiTest.Tests;

/// <summary>实体 CRUD E2E（以 Student 为样本，覆盖标准十类场景与写操作语义）</summary>
public sealed class ApiEntityCrudTests : ApiTestBase
{
    private const String BasePath = "/api/School/Student";

    [Fact(DisplayName = "TC-API-CRUD-001 列表返回信封与分页信息")]
    [Trait("Category", "Crud")]
    public async Task TC_API_CRUD_001_List()
    {
        var rs = await Admin.GetAsync(BasePath);

        ApiAssert.Ok(rs, "TC-API-CRUD-001");
        Assert.NotNull(rs.Page);
        Assert.Equal(1, ApiResult.GetInt32(rs.Page?["pageIndex"]));
        Assert.Equal(20, ApiResult.GetInt32(rs.Page?["pageSize"]));
        Assert.NotNull(rs.Rows);
    }

    [Fact(DisplayName = "TC-API-CRUD-002 仅提交必填字段即可新增并落库")]
    [Trait("Category", "Crud")]
    public async Task TC_API_CRUD_002_InsertMinimal()
    {
        var name = NewName("add");
        try
        {
            var rs = await Admin.PostJsonAsync(BasePath, Json(new { name }));

            ApiAssert.Ok(rs, "TC-API-CRUD-002");
            var id = ApiResult.GetInt32(rs.Data?["id"]);
            Assert.True(id > 0, $"新增未返回主键。响应：{rs.Content}");
            Assert.Equal(1, ApiDatabaseHelper.CountStudentsByName(name));
        }
        finally
        {
            ApiDatabaseHelper.DeleteStudentsByName(name);
        }
    }

    [Fact(DisplayName = "TC-API-CRUD-003 新增携带自增主键被拒绝")]
    [Trait("Category", "Crud")]
    public async Task TC_API_CRUD_003_InsertWithId()
    {
        var name = NewName("addid");
        try
        {
            var rs = await Admin.PostJsonAsync(BasePath, Json(new { id = 123456, name }));

            Assert.NotEqual(0, rs.Code);
            Assert.Equal(0, ApiDatabaseHelper.CountStudentsByName(name));
        }
        finally
        {
            ApiDatabaseHelper.DeleteStudentsByName(name);
        }
    }

    [Fact(DisplayName = "TC-API-CRUD-004 详情回读字段与新增一致")]
    [Trait("Category", "Crud")]
    public async Task TC_API_CRUD_004_Detail()
    {
        var name = NewName("detail");
        try
        {
            var created = await Admin.PostJsonAsync(BasePath, Json(new { name, age = 12, mobile = "13800000000" }));
            ApiAssert.Ok(created, "TC-API-CRUD-004");
            var id = ApiResult.GetInt32(created.Data?["id"]);
            Assert.True(id > 0);

            var rs = await Admin.GetAsync($"{BasePath}/{id}");
            ApiAssert.Ok(rs, "TC-API-CRUD-004");
            Assert.Equal(name, ApiResult.GetString(rs.Data?["name"]));
            Assert.Equal(12, ApiResult.GetInt32(rs.Data?["age"]));
            Assert.Equal("13800000000", ApiResult.GetString(rs.Data?["mobile"]));
        }
        finally
        {
            ApiDatabaseHelper.DeleteStudentsByName(name);
        }
    }

    [Fact(DisplayName = "TC-API-CRUD-005 修改只覆盖提交字段，未提交字段保持原值")]
    [Trait("Category", "Crud")]
    public async Task TC_API_CRUD_005_PartialUpdate()
    {
        var name = NewName("edit");
        try
        {
            var created = await Admin.PostJsonAsync(BasePath, Json(new { name, age = 10, mobile = "13900000000" }));
            ApiAssert.Ok(created, "TC-API-CRUD-005");
            var id = ApiResult.GetInt32(created.Data?["id"]);
            Assert.True(id > 0);

            var rs = await Admin.PutJsonAsync($"{BasePath}/{id}", Json(new { age = 11 }));
            ApiAssert.Ok(rs, "TC-API-CRUD-005");

            var detail = await Admin.GetAsync($"{BasePath}/{id}");
            ApiAssert.Ok(detail, "TC-API-CRUD-005");
            Assert.Equal(11, ApiResult.GetInt32(detail.Data?["age"]));
            Assert.Equal("13900000000", ApiResult.GetString(detail.Data?["mobile"]));
            Assert.Equal(name, ApiResult.GetString(detail.Data?["name"]));
            Assert.Equal(11, Convert.ToInt32(ApiDatabaseHelper.GetStudentField(name, "Age")));
        }
        finally
        {
            ApiDatabaseHelper.DeleteStudentsByName(name);
        }
    }

    [Fact(DisplayName = "TC-API-CRUD-006 删除单条后记录不可查")]
    [Trait("Category", "Crud")]
    public async Task TC_API_CRUD_006_Delete()
    {
        var name = NewName("del");
        try
        {
            var created = await Admin.PostJsonAsync(BasePath, Json(new { name }));
            ApiAssert.Ok(created, "TC-API-CRUD-006");
            var id = ApiResult.GetInt32(created.Data?["id"]);
            Assert.True(id > 0);

            var rs = await Admin.DeleteAsync($"{BasePath}/{id}");
            ApiAssert.Ok(rs, "TC-API-CRUD-006");
            Assert.Equal(0, ApiDatabaseHelper.CountStudentsByName(name));

            var detail = await Admin.GetAsync($"{BasePath}/{id}");
            Assert.NotEqual(0, detail.Code);
        }
        finally
        {
            ApiDatabaseHelper.DeleteStudentsByName(name);
        }
    }

    [Fact(DisplayName = "TC-API-CRUD-007 删除不存在的记录返回 404")]
    [Trait("Category", "Crud")]
    public async Task TC_API_CRUD_007_DeleteMissing()
    {
        var rs = await Admin.DeleteAsync($"{BasePath}/987654321");

        ApiAssert.Fail(rs, 404, "TC-API-CRUD-007");
    }

    [Fact(DisplayName = "TC-API-CRUD-008 按 ids 批量删除选中记录")]
    [Trait("Category", "Crud")]
    public async Task TC_API_CRUD_008_BatchDelete()
    {
        var name1 = NewName("batchA");
        var name2 = NewName("batchB");
        try
        {
            var c1 = await Admin.PostJsonAsync(BasePath, Json(new { name = name1 }));
            var c2 = await Admin.PostJsonAsync(BasePath, Json(new { name = name2 }));
            ApiAssert.Ok(c1, "TC-API-CRUD-008");
            ApiAssert.Ok(c2, "TC-API-CRUD-008");
            var id1 = ApiResult.GetInt32(c1.Data?["id"]);
            var id2 = ApiResult.GetInt32(c2.Data?["id"]);

            var rs = await Admin.DeleteAsync($"{BasePath}?ids={id1},{id2}");
            ApiAssert.Ok(rs, "TC-API-CRUD-008");

            Assert.Equal(0, ApiDatabaseHelper.CountStudentsByName(name1));
            Assert.Equal(0, ApiDatabaseHelper.CountStudentsByName(name2));
        }
        finally
        {
            ApiDatabaseHelper.DeleteStudentsByName(name1);
            ApiDatabaseHelper.DeleteStudentsByName(name2);
        }
    }

    [Fact(DisplayName = "TC-API-CRUD-009 无条件的条件删除被拒绝")]
    [Trait("Category", "Crud")]
    public async Task TC_API_CRUD_009_DeleteWithoutCondition()
    {
        var rs = await Admin.DeleteAsync(BasePath);

        ApiAssert.Fail(rs, -2, "TC-API-CRUD-009");
        Assert.False(String.IsNullOrWhiteSpace(rs.Message), "拒绝删除时未给出提示");
    }

    [Fact(DisplayName = "TC-API-CRUD-010 字段类型错误返回 -2 与字段级错误")]
    [Trait("Category", "Crud")]
    public async Task TC_API_CRUD_010_Validation()
    {
        var rs = await Admin.PostJsonAsync(BasePath, Json(new { name = NewName("badtype"), age = "not-a-number" }));

        ApiAssert.Fail(rs, -2, "TC-API-CRUD-010");
        ApiAssert.HasFieldError(rs, "$.age", "TC-API-CRUD-010");
    }

    [Fact(DisplayName = "TC-API-CRUD-011 非法 JSON 返回 -2 且 root 字段报错")]
    [Trait("Category", "Crud")]
    public async Task TC_API_CRUD_011_InvalidJson()
    {
        var rs = await Admin.PostJsonAsync(BasePath, "not-a-json-object");

        ApiAssert.Fail(rs, -2, "TC-API-CRUD-011");
        ApiAssert.HasFieldError(rs, "$", "TC-API-CRUD-011");
    }

    [Fact(DisplayName = "TC-API-CRUD-012 非 JSON Content-Type 返回 415")]
    [Trait("Category", "Crud")]
    public async Task TC_API_CRUD_012_UnsupportedMediaType()
    {
        var rs = await Admin.SendAsync(HttpMethod.Post, BasePath, "name=x", "text/plain");

        ApiAssert.HttpStatus(rs, 415, "TC-API-CRUD-012");
    }

    [Fact(DisplayName = "TC-API-CRUD-013 未匹配的 /api 路由返回 404")]
    [Trait("Category", "Crud")]
    public async Task TC_API_CRUD_013_UnknownRoute()
    {
        var rs = await Admin.GetAsync("/api/Unknown/Unknown");

        ApiAssert.HttpStatus(rs, 404, "TC-API-CRUD-013");
    }

    [Fact(DisplayName = "TC-API-CRUD-014 导出 CSV 返回文件流而非信封")]
    [Trait("Category", "Crud")]
    public async Task TC_API_CRUD_014_ExportCsv()
    {
        var name = NewName("export");
        try
        {
            var created = await Admin.PostJsonAsync(BasePath, Json(new { name }));
            ApiAssert.Ok(created, "TC-API-CRUD-014");

            var rs = await Admin.GetAsync($"{BasePath}/export?format=csv");
            ApiAssert.HttpStatus(rs, 200, "TC-API-CRUD-014");
            Assert.Contains("csv", rs.ContentType);
            Assert.Contains("\"Name\"", rs.Content);
            Assert.Contains(name, rs.Content);
        }
        finally
        {
            ApiDatabaseHelper.DeleteStudentsByName(name);
        }
    }

    [Fact(DisplayName = "TC-API-CRUD-015 导出 JSON 返回数组文件而非信封")]
    [Trait("Category", "Crud")]
    public async Task TC_API_CRUD_015_ExportJson()
    {
        var name = NewName("exportjson");
        try
        {
            var created = await Admin.PostJsonAsync(BasePath, Json(new { name }));
            ApiAssert.Ok(created, "TC-API-CRUD-015");

            var rs = await Admin.GetAsync($"{BasePath}/export?format=json");
            ApiAssert.HttpStatus(rs, 200, "TC-API-CRUD-015");
            Assert.Contains("json", rs.ContentType);
            Assert.True(rs.Json is System.Text.Json.Nodes.JsonArray, $"导出 JSON 不是数组。响应：{rs.Content}");
            Assert.Contains(name, rs.Content);
        }
        finally
        {
            ApiDatabaseHelper.DeleteStudentsByName(name);
        }
    }

    [Fact(DisplayName = "TC-API-CRUD-016 修改时路径与请求体主键不一致返回 -2")]
    [Trait("Category", "Crud")]
    public async Task TC_API_CRUD_016_UpdateIdMismatch()
    {
        var name = NewName("mismatch");
        try
        {
            var created = await Admin.PostJsonAsync(BasePath, Json(new { name }));
            ApiAssert.Ok(created, "TC-API-CRUD-016");
            var id = ApiResult.GetInt32(created.Data?["id"]);
            Assert.True(id > 0);

            var rs = await Admin.PutJsonAsync($"{BasePath}/{id}", Json(new { id = id + 1000, age = 5 }));

            ApiAssert.Fail(rs, -2, "TC-API-CRUD-016");
        }
        finally
        {
            ApiDatabaseHelper.DeleteStudentsByName(name);
        }
    }

    [Fact(DisplayName = "TC-API-CRUD-017 查看不存在的记录返回 500 且页面不崩")]
    [Trait("Category", "Crud")]
    public async Task TC_API_CRUD_017_DetailMissing()
    {
        var rs = await Admin.GetAsync($"{BasePath}/987654322");

        ApiAssert.Fail(rs, 500, "TC-API-CRUD-017");
        Assert.False(String.IsNullOrWhiteSpace(rs.Message), "缺少错误提示");
    }

    [Fact(DisplayName = "TC-API-CRUD-018 批量删除超过 100 个主键被拒绝")]
    [Trait("Category", "Crud")]
    public async Task TC_API_CRUD_018_BatchTooMany()
    {
        var ids = String.Join(",", System.Linq.Enumerable.Range(1, 101));

        var rs = await Admin.DeleteAsync($"{BasePath}?ids={ids}");

        ApiAssert.Fail(rs, -2, "TC-API-CRUD-018");
    }

    [Fact(DisplayName = "TC-API-CRUD-019 导入 JSON 与 CSV 均可落库")]
    [Trait("Category", "Crud")]
    public async Task TC_API_CRUD_019_Import()
    {
        var jsonNames = new[] { NewName("imporJ1"), NewName("imporJ2") };
        var csvNames = new[] { NewName("imporC1"), NewName("imporC2") };
        try
        {
            var json = System.Text.Encoding.UTF8.GetBytes(
                Json(new[] { new { name = jsonNames[0] }, new { name = jsonNames[1] } }));
            var rs = await Admin.PostMultipartAsync($"{BasePath}/import", "file", "e2e.json", json, "application/json");
            ApiAssert.Ok(rs, "TC-API-CRUD-019");
            foreach (var name in jsonNames)
                Assert.Equal(1, ApiDatabaseHelper.CountStudentsByName(name));

            var csv = System.Text.Encoding.UTF8.GetBytes("Name\n" + String.Join("\n", csvNames));
            rs = await Admin.PostMultipartAsync($"{BasePath}/import", "file", "e2e.csv", csv, "text/csv");
            ApiAssert.Ok(rs, "TC-API-CRUD-019");
            foreach (var name in csvNames)
                Assert.Equal(1, ApiDatabaseHelper.CountStudentsByName(name));
        }
        finally
        {
            foreach (var name in jsonNames.Concat(csvNames))
                ApiDatabaseHelper.DeleteStudentsByName(name);
        }
    }

    [Fact(DisplayName = "TC-API-CRUD-020 附件上传后可下载，匿名上传被拒")]
    [Trait("Category", "Crud")]
    public async Task TC_API_CRUD_020_Attachment()
    {
        var name = NewName("attach");
        try
        {
            var created = await Admin.PostJsonAsync(BasePath, Json(new { name }));
            ApiAssert.Ok(created, "TC-API-CRUD-020");
            var id = ApiResult.GetInt32(created.Data?["id"]);
            Assert.True(id > 0);

            var content = System.Text.Encoding.UTF8.GetBytes("hello cube e2e");
            var rs = await Admin.PostMultipartAsync($"{BasePath}/attachments", "file", "e2e.txt", content, "text/plain",
                new System.Collections.Generic.Dictionary<String, String>
                {
                    ["id"] = id.GetValueOrDefault().ToString(),
                    ["title"] = "E2E附件",
                });

            ApiAssert.Ok(rs, "TC-API-CRUD-020");
            var filePath = ApiResult.GetString(rs.Data?["filePath"]);
            Assert.False(String.IsNullOrWhiteSpace(ApiResult.GetString(rs.Data?["attId"])), "缺少附件编号");
            Assert.False(String.IsNullOrWhiteSpace(filePath), "缺少下载地址");

            var download = await Admin.GetAsync(filePath!);
            ApiAssert.HttpStatus(download, 200, "TC-API-CRUD-020");
            Assert.Equal("hello cube e2e", download.Content);

            using var anonymous = CreateAnonymous();
            var denied = await anonymous.PostMultipartAsync($"{BasePath}/attachments", "file", "e2e.txt", content, "text/plain");
            ApiAssert.HttpStatus(denied, 401, "TC-API-CRUD-020");
        }
        finally
        {
            ApiDatabaseHelper.DeleteStudentsByName(name);
        }
    }
}

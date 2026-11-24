using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using E2EApiTest.Helpers;
using Xunit;

namespace E2EApiTest.Tests;

/// <summary>附件访问控制 E2E：匿名拦截、公开分类、仅本人分类</summary>
public sealed class ApiAttachmentAccessTests : ApiTestBase
{
    private const String Category = "Student";

    [Fact(DisplayName = "TC-API-ATT-001 非公开附件匿名访问被拒，登录后可下载")]
    [Trait("Category", "Attachment")]
    public async Task TC_API_ATT_001_AnonymousDenied()
    {
        var attId = await UploadAsync();

        using var anon = CreateAnonymous();
        var denied = await anon.GetAsync("/Cube/File?id=" + attId);
        Assert.Equal(401, denied.StatusCode);

        var ok = await Admin.GetAsync("/Cube/File?id=" + attId);
        Assert.Equal(200, ok.StatusCode);
        Assert.Contains("probe", ok.Content);
    }

    [Fact(DisplayName = "TC-API-ATT-002 配置为公开分类后匿名可下载，还原配置后重新拦截")]
    [Trait("Category", "Attachment")]
    public async Task TC_API_ATT_002_PublicCategory()
    {
        var attId = await UploadAsync();
        try
        {
            await SetAttachmentCategoriesAsync("public", Category);

            using var anon = CreateAnonymous();
            var open = await anon.GetAsync("/Cube/File?id=" + attId);
            Assert.Equal(200, open.StatusCode);
        }
        finally
        {
            await SetAttachmentCategoriesAsync("public", null);
        }

        using var again = CreateAnonymous();
        var closed = await again.GetAsync("/Cube/File?id=" + attId);
        Assert.Equal(401, closed.StatusCode);
    }

    [Fact(DisplayName = "TC-API-ATT-003 配置为仅本人分类后其他用户被拒，上传者仍可下载")]
    [Trait("Category", "Attachment")]
    public async Task TC_API_ATT_003_OwnerOnlyCategory()
    {
        var attId = await UploadAsync();
        var userName = NewName("att");
        try
        {
            await SetAttachmentCategoriesAsync("owner", Category);

            using var other = CreateAnonymous();
            var register = await other.PostJsonAsync("/Auth/Register", Json(new { username = userName, password = "Test@123", confirmPassword = "Test@123" }));
            ApiAssert.Ok(register, "TC-API-ATT-003 register");
            var login = await other.PostJsonAsync("/Auth/Login", Json(new { username = userName, password = "Test@123" }));
            ApiAssert.Ok(login, "TC-API-ATT-003 login");

            var denied = await other.GetAsync("/Cube/File?id=" + attId);
            Assert.Equal(403, denied.StatusCode);

            var owner = await Admin.GetAsync("/Cube/File?id=" + attId);
            Assert.Equal(200, owner.StatusCode);
        }
        finally
        {
            await SetAttachmentCategoriesAsync("owner", null);
            ApiDatabaseHelper.DeleteUserByName(userName);
        }
    }

    [Fact(DisplayName = "TC-API-ATT-004 有效分享令牌可让匿名用户下载对应附件，令牌错配或过期无效")]
    [Trait("Category", "Attachment")]
    public async Task TC_API_ATT_004_ShareToken()
    {
        var attId = await UploadAsync();
        var token = "E2E-SHARE-" + Guid.NewGuid().ToString("N")[..16];
        var expired = token + "-X";
        try
        {
            ApiDatabaseHelper.InsertUserToken(token, 1, $"attachment:{attId}", DateTime.Now.AddHours(1));
            ApiDatabaseHelper.InsertUserToken(expired, 1, $"attachment:{attId}", DateTime.Now.AddHours(-1));

            using var anon = CreateAnonymous();
            var shared = await anon.GetAsync($"/Cube/File?id={attId}&token={token}");
            Assert.Equal(200, shared.StatusCode);
            Assert.Contains("probe", shared.Content);

            var badToken = await anon.GetAsync($"/Cube/File?id={attId}&token=not-a-token");
            Assert.Equal(401, badToken.StatusCode);

            var expiredRs = await anon.GetAsync($"/Cube/File?id={attId}&token={expired}");
            Assert.Equal(401, expiredRs.StatusCode);
        }
        finally
        {
            ApiDatabaseHelper.DeleteUserToken(token);
            ApiDatabaseHelper.DeleteUserToken(expired);
        }
    }

    /// <summary>上传一个普通文本附件，返回附件编号</summary>
    private async Task<String> UploadAsync()
    {
        var content = Encoding.UTF8.GetBytes("probe attachment " + Guid.NewGuid().ToString("N"));
        var upload = await Admin.PostMultipartAsync("/api/School/Student/attachments", "file", "probe.txt", content, "text/plain");
        ApiAssert.Ok(upload, "上传附件");

        var attId = ApiResult.GetString(upload.Data?["attId"]);
        Assert.False(String.IsNullOrWhiteSpace(attId), $"上传未返回附件编号：{upload.Content}");
        return attId!;
    }

    /// <summary>切换附件分类配置。kind=public 对应公开分类，kind=owner 对应仅本人分类；value 为空表示清空</summary>
    private async Task SetAttachmentCategoriesAsync(String kind, String? value)
    {
        var get = await Admin.GetAsync("/api/Admin/Cube");
        ApiAssert.Ok(get, "读取魔方设置");

        var settings = get.Data as JsonObject ?? throw new InvalidOperationException("魔方设置响应不是对象：" + get.Content);
        var key = kind == "public" ? "publicAttachmentCategories" : "ownerOnlyAttachmentCategories";
        settings[key] = value;

        var put = await Admin.PutJsonAsync("/api/Admin/Cube", settings.ToJsonString());
        ApiAssert.Ok(put, "保存魔方设置");
        Assert.Equal(value, ApiResult.GetString(put.Data?[key]));
    }
}

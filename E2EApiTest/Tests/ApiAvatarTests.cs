using System;
using System.Threading.Tasks;
using E2EApiTest.Helpers;
using Xunit;

namespace E2EApiTest.Tests;

/// <summary>头像上传 E2E：图片白名单、仅本人可传、上传后可回填资料</summary>
public sealed class ApiAvatarTests : ApiTestBase
{
    // 1x1 透明 PNG
    private static readonly Byte[] _png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    [Fact(DisplayName = "TC-API-AVATAR-001 上传图片成功并可通过附件接口下载")]
    [Trait("Category", "Account")]
    public async Task TC_API_AVATAR_001_UploadImage()
    {
        var rs = await Admin.PostMultipartAsync("/api/Admin/User/attachments", "file", "E2E.png", _png, "image/png");

        ApiAssert.Ok(rs, "TC-API-AVATAR-001");
        var path = ApiResult.GetString(rs.Data?["filePath"]);
        Assert.False(String.IsNullOrWhiteSpace(path), $"上传未返回文件路径：{rs.Content}");
        Assert.StartsWith("image/png", ApiResult.GetString(rs.Data?["contentType"]) ?? "", StringComparison.OrdinalIgnoreCase);

        var download = await Admin.GetAsync("/Cube/Image?id=" + ApiResult.GetString(rs.Data?["attId"]));
        Assert.Equal(200, download.StatusCode);
        Assert.StartsWith("image/", download.ContentType ?? "");
    }

    [Fact(DisplayName = "TC-API-AVATAR-002 非图片文件与越权目标用户均被拒绝")]
    [Trait("Category", "Account")]
    public async Task TC_API_AVATAR_002_Rejections()
    {
        var txt = System.Text.Encoding.UTF8.GetBytes("not an image");
        var nonImage = await Admin.PostMultipartAsync("/api/Admin/User/attachments", "file", "E2E.txt", txt, "text/plain");
        Assert.NotEqual(0, nonImage.Code);
        Assert.False(String.IsNullOrWhiteSpace(nonImage.Message), "非图片应有提示");

        // 目标用户取自查询串（[ApiController] 下简单类型默认不绑定表单字段）
        var otherUser = await Admin.PostMultipartAsync("/api/Admin/User/attachments?id=99999999", "file", "E2E.png", _png, "image/png");
        ApiAssert.Fail(otherUser, -2, "TC-API-AVATAR-002 other user");
        Assert.False(String.IsNullOrWhiteSpace(otherUser.Message), "越权上传应有提示");
    }

    [Fact(DisplayName = "TC-API-AVATAR-003 上传后的文件路径可回填资料并出现在用户信息中")]
    [Trait("Category", "Account")]
    public async Task TC_API_AVATAR_003_SaveToProfile()
    {
        var upload = await Admin.PostMultipartAsync("/api/Admin/User/attachments", "file", "E2E-avatar.png", _png, "image/png");
        ApiAssert.Ok(upload, "TC-API-AVATAR-003 upload");
        var path = ApiResult.GetString(upload.Data?["filePath"]);
        Assert.False(String.IsNullOrWhiteSpace(path), "上传未返回文件路径");

        var save = await Admin.PostJsonAsync("/api/Admin/User/Info", Json(new { id = 1, avatar = path }));
        ApiAssert.Ok(save, "TC-API-AVATAR-003 save");

        var info = await Admin.GetAsync("/api/Admin/User/Info");
        Assert.Equal(path, ApiResult.GetString(info.Data?["avatar"]));
    }
}

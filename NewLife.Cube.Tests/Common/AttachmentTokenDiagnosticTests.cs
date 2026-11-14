using System;
using Microsoft.AspNetCore.Http;
using NewLife.Cube.Controllers;
using NewLife.Cube.Entity;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>附件分享令牌诊断覆盖拒绝原因与查询参数读取。</summary>
public class AttachmentTokenDiagnosticTests
{
    [Theory]
    [InlineData("missing", "token_not_found")]
    [InlineData("disabled", "token_disabled")]
    [InlineData("expired", "token_expired")]
    [InlineData("mismatch", "attachment_mismatch")]
    [InlineData("valid", "accepted")]
    public void TokenResult_DistinguishesRejectionReasons(String state, String expected)
    {
        var now = DateTime.Now;
        var token = state == "missing" ? null : new UserToken
        {
            Token = "private-value",
            Enable = state != "disabled",
            Expire = state == "expired" ? now : now.AddHours(1),
            Url = state == "mismatch" ? "attachment:456" : "attachment:123",
        };
        var result = CubeController.GetAttachmentTokenResult(token!, 123, now);
        Assert.Equal(expected, result);
        Assert.DoesNotContain("private-value", result);
    }

    [Fact]
    public void GetToken_LowercaseQueryOverridesAuthorization()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?id=123.png&token=share-key");
        context.Request.Headers.Authorization = "Bearer other-key";
        Assert.Equal("share-key", CubeController.GetToken(context));
    }
}
